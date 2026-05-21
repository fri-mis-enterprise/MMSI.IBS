using IBS.DataAccess.Repository.IRepository;
using IBS.Models.MMSI;
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
                jobOrder.ServiceType = model.ServiceType;
                jobOrder.IsConfirmed = model.IsConfirmed;
                jobOrder.Remarks = model.Remarks;
                jobOrder.EditedBy = username;
                jobOrder.EditedDate = DateTimeHelper.GetCurrentPhilippineTime();

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

                var ticketsForBillingOrBilled = jobOrder.DispatchTickets
                    .Count(dt => dt.Status == "For Billing" || dt.Status == "Billed");

                if (ticketsForBillingOrBilled > 0)
                {
                    return ServiceResult.Failure($"Cannot cancel Job Order. {ticketsForBillingOrBilled} ticket(s) are already in the billing process.");
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
                    var ticketsWithoutTariff = jobOrder.DispatchTickets
                        .Count(dt => dt.Status == "Pending" || dt.Status == "For Tariff");

                    if (ticketsWithoutTariff > 0)
                    {
                        return ServiceResult.Failure($"Cannot close Job Order. {ticketsWithoutTariff} dispatch ticket(s) have no tariff set.");
                    }

                    var ticketsDisapproved = jobOrder.DispatchTickets
                        .Count(dt => dt.Status == "Disapproved");

                    if (ticketsDisapproved > 0)
                    {
                        return ServiceResult.Failure($"Cannot close Job Order. {ticketsDisapproved} dispatch ticket(s) are disapproved.");
                    }

                    var ticketsForApproval = jobOrder.DispatchTickets
                        .Count(dt => dt.Status == "For Approval");

                    if (ticketsForApproval > 0 && !forceClose)
                    {
                        return ServiceResult.Failure($"Warning: {ticketsForApproval} dispatch ticket(s) are pending approval. These tickets will not be included in billing until approved.", ServiceResultStatus.ConfirmationRequired);
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

        private async Task RecordAuditAsync(string activity, string username, CancellationToken cancellationToken)
        {
            var audit = new AuditTrail(username, activity, "Job Order");
            await unitOfWork.AuditTrail.AddAsync(audit, cancellationToken);
        }
    }
}
