using IBS.DataAccess.Repository.IRepository;
using IBS.Models.MSAP;
using IBS.Models;
using IBS.Utility.Constants;
using IBS.Utility.Helpers;
using Microsoft.Extensions.Logging;

namespace IBS.Services
{
    public class JobOrderService(IUnitOfWork unitOfWork, ILogger<JobOrderService> logger) : IJobOrderService
    {
        public async Task<IEnumerable<JobOrder>> GetAllJobOrdersAsync(CancellationToken cancellationToken)
        {
            var jobOrders = await unitOfWork.JobOrder.GetAllJobOrdersWithDetailsAsync(cancellationToken);
            return jobOrders.OrderByDescending(j => j.JobOrderNumber);
        }

        public async Task<JobOrder?> GetJobOrderByIdAsync(int id, CancellationToken cancellationToken)
        {
            return await unitOfWork.JobOrder.GetJobOrderWithDetailsAsync(id, cancellationToken);
        }

        public async Task<ServiceResult<int>> CreateJobOrderAsync(JobOrder jobOrder, string username, CancellationToken cancellationToken)
        {
            try
            {
                if (jobOrder.PlannedStartTime.HasValue && jobOrder.PlannedEndTime.HasValue)
                {
                    if (jobOrder.PlannedEndTime <= jobOrder.PlannedStartTime)
                    {
                        return ServiceResult<int>.Failure("Planned End Time must be strictly after Planned Start Time.");
                    }
                }

                jobOrder.Status = SD.JobOrderStatus.Open;
                jobOrder.JobOrderNumber = await unitOfWork.JobOrder.GenerateJobOrderNumber(cancellationToken);
                jobOrder.CreatedBy = username;
                jobOrder.CreatedDate = DateTimeHelper.GetCurrentPhilippineTime();

                await unitOfWork.JobOrder.AddAsync(jobOrder, cancellationToken);
                await RecordAuditAsync($"Created Job Order #{jobOrder.JobOrderNumber}", username, cancellationToken);
                await unitOfWork.SaveAsync(cancellationToken);

                return ServiceResult<int>.Success(jobOrder.JobOrderId, $"Job Order #{jobOrder.JobOrderNumber} created successfully.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error creating Job Order");
                return ServiceResult<int>.Failure("An unexpected error occurred while creating the Job Order.");
            }
        }

        public async Task<ServiceResult> UpdateJobOrderAsync(JobOrder model, string username, CancellationToken cancellationToken)
        {
            try
            {
                var jobOrder = await unitOfWork.JobOrder.GetAsync(j => j.JobOrderId == model.JobOrderId, cancellationToken);
                if (jobOrder == null)
                {
                    return ServiceResult.Failure("Job Order not found.", ServiceResultStatus.NotFound);
                }

                if (jobOrder.Status == SD.JobOrderStatus.Closed || jobOrder.Status == SD.JobOrderStatus.Cancelled)
                {
                    return ServiceResult.Failure($"Job Order #{jobOrder.JobOrderNumber} is {jobOrder.Status.ToLower()} and cannot be edited.");
                }

                if (model.PlannedStartTime.HasValue && model.PlannedEndTime.HasValue)
                {
                    if (model.PlannedEndTime <= model.PlannedStartTime)
                    {
                        return ServiceResult.Failure("Planned End Time must be strictly after Planned Start Time.");
                    }
                }

                jobOrder.Date = model.Date;
                jobOrder.CustomerId = model.CustomerId;
                jobOrder.VesselId = model.VesselId;
                jobOrder.PortId = model.PortId;
                jobOrder.TerminalId = model.TerminalId;
                jobOrder.COSNumber = model.COSNumber;
                jobOrder.VoyageNumber = model.VoyageNumber;
                jobOrder.PlannedStartTime = model.PlannedStartTime;
                jobOrder.PlannedEndTime = model.PlannedEndTime;
                jobOrder.PreferredTugboatId = model.PreferredTugboatId;
                jobOrder.RequiredTugCount = model.RequiredTugCount;
                jobOrder.IsConfirmed = model.IsConfirmed;
                jobOrder.Remarks = model.Remarks;
                jobOrder.EditedBy = username;
                jobOrder.EditedDate = DateTimeHelper.GetCurrentPhilippineTime();

                // Cascade updates to related records
                await SyncRelatedRecordsAsync(jobOrder, cancellationToken);

                await RecordAuditAsync($"Edited Job Order #{jobOrder.JobOrderNumber}", username, cancellationToken);
                await unitOfWork.SaveAsync(cancellationToken);

                return ServiceResult.Success($"Job Order #{jobOrder.JobOrderNumber} updated successfully.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error updating Job Order {JobOrderId}", model.JobOrderId);
                return ServiceResult.Failure("An unexpected error occurred while updating the Job Order.");
            }
        }

        private async Task SyncRelatedRecordsAsync(JobOrder jobOrder, CancellationToken cancellationToken)
        {
            // 1. Update unbilled dispatch tickets
            var tickets = await unitOfWork.DispatchTicket.GetAllAsync(
                dt => dt.JobOrderId == jobOrder.JobOrderId && 
                      dt.Status != SD.DispatchTicketStatus.Billed && 
                      dt.Status != SD.DispatchTicketStatus.Cancelled, 
                cancellationToken);

            foreach (var ticket in tickets)
            {
                ticket.CustomerId = jobOrder.CustomerId;
                ticket.VesselId = jobOrder.VesselId;
                ticket.VoyageNumber = jobOrder.VoyageNumber;
                ticket.COSNumber = jobOrder.COSNumber;
                ticket.PortId = jobOrder.PortId;
                ticket.TerminalId = jobOrder.TerminalId;
                ticket.Date = jobOrder.Date;
            }

            // 2. Update unposted/uncollected billings
            var billings = await unitOfWork.Billing.GetAllAsync(
                b => b.JobOrderId == jobOrder.JobOrderId && 
                     (b.Status == SD.BillingStatus.ForPosting || b.Status == SD.BillingStatus.ForCollection), 
                cancellationToken);

            foreach (var billing in billings)
            {
                billing.CustomerId = jobOrder.CustomerId;
                billing.VesselId = jobOrder.VesselId;
                billing.VoyageNumber = jobOrder.VoyageNumber;
                billing.COSNumber = jobOrder.COSNumber;
                billing.PortId = jobOrder.PortId;
                billing.TerminalId = jobOrder.TerminalId;
                billing.Date = jobOrder.Date;
            }
        }

        public async Task<ServiceResult> CancelJobOrderAsync(int id, string username, CancellationToken cancellationToken)
        {
            try
            {
                var jobOrder = await unitOfWork.JobOrder.GetJobOrderWithDetailsAsync(id, cancellationToken);
                if (jobOrder == null)
                {
                    return ServiceResult.Failure("Job Order not found.", ServiceResultStatus.NotFound);
                }

                if (jobOrder.Status == SD.JobOrderStatus.Cancelled)
                {
                    return ServiceResult.Failure($"Job Order #{jobOrder.JobOrderNumber} is already cancelled.");
                }

                if (jobOrder.Status == SD.JobOrderStatus.Closed)
                {
                    return ServiceResult.Failure($"Job Order #{jobOrder.JobOrderNumber} is closed and cannot be cancelled.");
                }

                var activeTicketsCount = jobOrder.DispatchTickets
                    .Count(dt => dt.Status != SD.DispatchTicketStatus.Cancelled);

                if (activeTicketsCount > 0)
                {
                    return ServiceResult.Failure($"Cannot cancel Job Order. There are {activeTicketsCount} active dispatch ticket(s). Please cancel them first.");
                }

                jobOrder.Status = SD.JobOrderStatus.Cancelled;
                await RecordAuditAsync($"Cancelled Job Order #{jobOrder.JobOrderNumber}", username, cancellationToken);
                await unitOfWork.SaveAsync(cancellationToken);

                return ServiceResult.Success($"Job Order #{jobOrder.JobOrderNumber} has been cancelled.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error cancelling Job Order {JobOrderId}", id);
                return ServiceResult.Failure("An unexpected error occurred while cancelling the Job Order.");
            }
        }

        public async Task<ServiceResult> CloseJobOrderAsync(int id, string username, bool forceClose, CancellationToken cancellationToken)
        {
            try
            {
                var jobOrder = await unitOfWork.JobOrder.GetJobOrderWithDetailsAsync(id, cancellationToken);
                if (jobOrder == null)
                {
                    return ServiceResult.Failure("Job Order not found.", ServiceResultStatus.NotFound);
                }

                if (jobOrder.Status == SD.JobOrderStatus.Closed)
                {
                    return ServiceResult.Failure($"Job Order #{jobOrder.JobOrderNumber} is already closed.");
                }

                if (jobOrder.DispatchTickets.Any())
                {
                    var nonTerminalTickets = jobOrder.DispatchTickets
                        .Where(dt => dt.Status != SD.DispatchTicketStatus.Billed && 
                                     dt.Status != SD.DispatchTicketStatus.Cancelled && 
                                     dt.Status != SD.DispatchTicketStatus.Disapproved)
                        .ToList();

                    if (nonTerminalTickets.Any())
                    {
                        var statuses = string.Join(", ", nonTerminalTickets.Select(t => t.Status).Distinct());
                        return ServiceResult.Failure($"Cannot close Job Order. {nonTerminalTickets.Count} ticket(s) are in non-terminal states ({statuses}). All tickets must be Billed, Cancelled, or Disapproved.");
                    }
                }

                jobOrder.Status = SD.JobOrderStatus.Closed;
                await RecordAuditAsync($"Closed Job Order #{jobOrder.JobOrderNumber}", username, cancellationToken);
                await unitOfWork.SaveAsync(cancellationToken);

                return ServiceResult.Success($"Job Order #{jobOrder.JobOrderNumber} has been closed.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error closing Job Order {JobOrderId}", id);
                return ServiceResult.Failure("An unexpected error occurred while closing the Job Order.");
            }
        }

        public async Task<List<object>> SearchCustomersAsync(string? term, CancellationToken cancellationToken)
        {
            var customers = await unitOfWork.Customer.SearchCustomersAsync(term ?? string.Empty, 10, cancellationToken);

            return customers.Select(c => (object)new
            {
                value = c.CustomerId,
                name = c.CustomerName,
                address = c.CustomerAddress,
                tinNo = c.CustomerTin,
                terms = c.CustomerTerms
            }).ToList();
        }

        private async Task RecordAuditAsync(string activity, string username, CancellationToken cancellationToken)
        {
            var audit = new AuditTrail(username, activity, "Job Order");
            await unitOfWork.AuditTrail.AddAsync(audit, cancellationToken);
        }
    }
}


