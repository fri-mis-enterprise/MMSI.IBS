using IBS.DataAccess.Repository.IRepository;
using IBS.Models.MSAP;
using IBS.Models.MSAP.ViewModels;
using IBS.Models;
using IBS.Models.Enums;
using IBS.Utility.Constants;
using IBS.Utility.Helpers;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;

namespace IBS.Services
{
    public class JobOrderService(IUnitOfWork unitOfWork, ILogger<JobOrderService> logger, INotificationService notificationService) : IJobOrderService
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

        public async Task<JobOrderViewModel> PopulateJobOrderViewModelAsync(JobOrderViewModel? viewModel, CancellationToken cancellationToken)
        {
            viewModel ??= new JobOrderViewModel { Date = DateOnly.FromDateTime(DateTimeHelper.GetCurrentPhilippineTime()) };

            viewModel.Customers = await unitOfWork.GetCustomerListAsyncById(cancellationToken);

            var vessels = await unitOfWork.Vessel.GetAllAsync(cancellationToken: cancellationToken);
            viewModel.Vessels = vessels
                .OrderBy(v => v.VesselName)
                .Select(v => new SelectListItem
                {
                    Value = v.VesselId.ToString(),
                    Text = $"{v.VesselName} ({v.VesselType})"
                })
                .ToList();

            var ports = await unitOfWork.Port.GetAllAsync(cancellationToken: cancellationToken);
            viewModel.Ports = ports
                .OrderBy(p => p.PortName)
                .Select(p => new SelectListItem
                {
                    Value = p.PortId.ToString(),
                    Text = p.PortName
                })
                .ToList();

            viewModel.Terminals = viewModel.PortId != 0
                ? (await unitOfWork.Terminal.GetAllAsync(t => t.PortId == viewModel.PortId, cancellationToken: cancellationToken))
                    .OrderBy(t => t.TerminalName)
                    .Select(t => new SelectListItem
                    {
                        Value = t.TerminalId.ToString(),
                        Text = t.TerminalName
                    })
                    .ToList()
                : new List<SelectListItem>();

            var tugboats = await unitOfWork.Tugboat.GetAllAsync(cancellationToken: cancellationToken);
            viewModel.Tugboats = tugboats
                .OrderBy(t => t.TugboatName)
                .Select(t => new SelectListItem
                {
                    Value = t.TugboatId.ToString(),
                    Text = t.TugboatName
                })
                .ToList();

            return viewModel;
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

                var vessel = await unitOfWork.Vessel.GetAsync(v => v.VesselId == jobOrder.VesselId, cancellationToken);
                await notificationService.NotifyByAccessAsync(
                    ProcedureEnum.CreateDispatchTicket,
                    $"A new Job Order <b>#{jobOrder.JobOrderNumber}</b> for <b>{vessel?.VesselName ?? "a vessel"}</b> has been created and is ready for Dispatch.",
                    targetUrl: $"/User/JobOrder/Details/{jobOrder.JobOrderId}",
                    cancellationToken: cancellationToken);

                return ServiceResult<int>.Success(jobOrder.JobOrderId, $"Job Order #{jobOrder.JobOrderNumber} created successfully.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error creating Job Order");
                return ServiceResult<int>.Failure($"Failed to create Job Order: {ExceptionHelper.GetErrorMessage(ex)}");
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

                if (jobOrder.Status == SD.JobOrderStatus.Closed)
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
                return ServiceResult.Failure($"Failed to update Job Order: {ExceptionHelper.GetErrorMessage(ex)}");
            }
        }

        private async Task SyncRelatedRecordsAsync(JobOrder jobOrder, CancellationToken cancellationToken)
        {
            // 1. Update unbilled dispatch tickets
            var tickets = await unitOfWork.DispatchTicket.GetAllAsync(
                dt => dt.JobOrderId == jobOrder.JobOrderId && 
                      dt.Status != SD.DispatchTicketStatus.Billed, 
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
                                     dt.Status != SD.DispatchTicketStatus.Disapproved)
                        .ToList();

                    if (nonTerminalTickets.Any())
                    {
                        var statuses = string.Join(", ", nonTerminalTickets.Select(t => t.Status).Distinct());
                        return ServiceResult.Failure($"Cannot close Job Order. {nonTerminalTickets.Count} ticket(s) are in non-terminal states ({statuses}). All tickets must be Billed or Disapproved.");
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
                return ServiceResult.Failure($"Failed to close Job Order: {ExceptionHelper.GetErrorMessage(ex)}");
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

        public async Task<(IEnumerable<JobOrder> Data, int RecordsFiltered, int TotalRecords)> GetPagedJobOrdersAsync(DataTablesParameters parameters, CancellationToken cancellationToken)
        {
            return await unitOfWork.JobOrder.GetPagedJobOrdersAsync(parameters, cancellationToken);
        }

        private async Task RecordAuditAsync(string activity, string username, CancellationToken cancellationToken)
        {
            var audit = new AuditTrail(username, activity, "Job Order");
            await unitOfWork.AuditTrail.AddAsync(audit, cancellationToken);
        }

        public async Task<ServiceResult> AssignTugboatAsync(int jobOrderId, int tugboatId, string username, CancellationToken cancellationToken)
        {
            try
            {
                var jobOrder = await unitOfWork.JobOrder.GetJobOrderWithDetailsAsync(jobOrderId, cancellationToken);
                if (jobOrder == null)
                {
                    return ServiceResult.Failure("Job Order not found.", ServiceResultStatus.NotFound);
                }

                var tugboat = await unitOfWork.Tugboat.GetAsync(t => t.TugboatId == tugboatId, cancellationToken);
                if (tugboat == null)
                {
                    return ServiceResult.Failure("Tugboat not found.", ServiceResultStatus.NotFound);
                }

                // Check if already assigned
                bool alreadyAssigned = jobOrder.PreferredTugboatId == tugboatId || jobOrder.DispatchTickets.Any(dt => dt.TugBoatId == tugboatId);
                if (alreadyAssigned)
                {
                    return ServiceResult.Failure("Tugboat is already assigned to this Job Order.");
                }

                if (jobOrder.PreferredTugboatId == null)
                {
                    jobOrder.PreferredTugboatId = tugboatId;
                }
                else
                {
                    // Assign as an additional tugboat by creating a pending DispatchTicket
                    var services = await unitOfWork.Service.GetAllAsync(cancellationToken: cancellationToken);
                    var defaultService = services.FirstOrDefault();
                    int serviceId = defaultService?.ServiceId ?? 1;

                    var ticket = new DispatchTicket
                    {
                        JobOrderId = jobOrder.JobOrderId,
                        TugBoatId = tugboatId,
                        CustomerId = jobOrder.CustomerId,
                        VesselId = jobOrder.VesselId,
                        PortId = jobOrder.PortId,
                        TerminalId = jobOrder.TerminalId,
                        ServiceId = serviceId,
                        Date = DateOnly.FromDateTime(DateTimeHelper.GetCurrentPhilippineTime()),
                        DispatchNumber = $"PL-{jobOrder.JobOrderId}-{tugboatId}",
                        Status = SD.DispatchTicketStatus.Pending,
                        CreatedBy = username,
                        CreatedDate = DateTimeHelper.GetCurrentPhilippineTime()
                    };

                    // Truncate DispatchNumber if it exceeds 20 characters
                    if (ticket.DispatchNumber.Length > 20)
                    {
                        ticket.DispatchNumber = ticket.DispatchNumber.Substring(0, 20);
                    }

                    await unitOfWork.DispatchTicket.AddAsync(ticket, cancellationToken);
                }

                await RecordAuditAsync($"Assigned tugboat {tugboat.TugboatName} to Job Order #{jobOrder.JobOrderNumber}", username, cancellationToken);
                await unitOfWork.SaveAsync(cancellationToken);

                return ServiceResult.Success("Tugboat assigned successfully.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error assigning tugboat");
                return ServiceResult.Failure($"Failed to assign tugboat: {ExceptionHelper.GetErrorMessage(ex)}");
            }
        }

        public async Task<ServiceResult> UnassignTugboatAsync(int jobOrderId, int tugboatId, string username, CancellationToken cancellationToken)
        {
            try
            {
                var jobOrder = await unitOfWork.JobOrder.GetJobOrderWithDetailsAsync(jobOrderId, cancellationToken);
                if (jobOrder == null)
                {
                    return ServiceResult.Failure("Job Order not found.", ServiceResultStatus.NotFound);
                }

                string? tugboatName = null;

                if (jobOrder.PreferredTugboatId == tugboatId)
                {
                    var tug = await unitOfWork.Tugboat.GetAsync(t => t.TugboatId == tugboatId, cancellationToken);
                    tugboatName = tug?.TugboatName;
                    jobOrder.PreferredTugboatId = null;
                }

                var ticketToRemove = jobOrder.DispatchTickets.FirstOrDefault(dt => dt.TugBoatId == tugboatId);
                if (ticketToRemove != null)
                {
                    if (ticketToRemove.Status != SD.DispatchTicketStatus.Pending && ticketToRemove.Status != SD.DispatchTicketStatus.ForTariff)
                    {
                        return ServiceResult.Failure("Cannot unassign a tugboat with an active or processed dispatch ticket.");
                    }
                    
                    if (tugboatName == null)
                    {
                        var tug = await unitOfWork.Tugboat.GetAsync(t => t.TugboatId == tugboatId, cancellationToken);
                        tugboatName = tug?.TugboatName;
                    }

                    await unitOfWork.DispatchTicket.RemoveAsync(ticketToRemove, cancellationToken);
                }

                await RecordAuditAsync($"Unassigned tugboat {tugboatName ?? "Unknown"} from Job Order #{jobOrder.JobOrderNumber}", username, cancellationToken);
                await unitOfWork.SaveAsync(cancellationToken);

                return ServiceResult.Success("Tugboat unassigned successfully.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error unassigning tugboat");
                return ServiceResult.Failure($"Failed to unassign tugboat: {ExceptionHelper.GetErrorMessage(ex)}");
            }
        }
    }
}


