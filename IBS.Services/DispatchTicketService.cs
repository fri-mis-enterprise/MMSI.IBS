using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using IBS.Models.MSAP;
using IBS.Models.MSAP.ViewModels;
using IBS.Utility.Constants;
using IBS.Utility.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace IBS.Services
{
    public class DispatchTicketService(
        IUnitOfWork unitOfWork,
        ICloudStorageService cloudStorageService,
        ILogger<DispatchTicketService> logger) : IDispatchTicketService
    {
        public async Task<DispatchTicket?> GetDispatchTicketByIdAsync(int id, CancellationToken cancellationToken)
        {
            return await unitOfWork.DispatchTicket.GetDispatchTicketWithDetailsAsync(id, cancellationToken);
        }

        public async Task<ServiceRequestViewModel> PopulateServiceRequestViewModelAsync(ServiceRequestViewModel? viewModel, int? jobOrderId, CancellationToken cancellationToken)
        {
            viewModel ??= new ServiceRequestViewModel();

            if (jobOrderId.HasValue)
            {
                var jobOrder = await unitOfWork.JobOrder.GetAsync(j => j.JobOrderId == jobOrderId.Value, cancellationToken);
                if (jobOrder != null)
                {
                    viewModel.JobOrderId = jobOrderId;
                    viewModel.CustomerId = jobOrder.CustomerId;
                    viewModel.VesselId = jobOrder.VesselId;
                    viewModel.PortId = jobOrder.PortId;
                    viewModel.TerminalId = jobOrder.TerminalId;
                    viewModel.VoyageNumber = jobOrder.VoyageNumber;
                    viewModel.COSNumber = jobOrder.COSNumber;
                    viewModel.Date = jobOrder.Date;
                }
            }

            viewModel = await unitOfWork.ServiceRequest.GetDispatchTicketSelectLists(viewModel, cancellationToken);
            viewModel.Customers = await unitOfWork.GetCustomerListAsyncById(cancellationToken);

            return viewModel;
        }

        public async Task<ServiceResult<int>> CreateDispatchTicketAsync(ServiceRequestViewModel viewModel, IFormFile? imageFile, IFormFile? videoFile, string username, CancellationToken cancellationToken)
        {
            try
            {
                if (viewModel.JobOrderId.HasValue && !await IsJobOrderEditableAsync(viewModel.JobOrderId, cancellationToken))
                {
                    return ServiceResult<int>.Failure("Cannot add ticket â€” parent Job Order is cancelled or closed.");
                }

                var model = viewModel.ToEntity();

                if (imageFile is { Length: > 0 })
                {
                    var ext = Path.GetExtension(imageFile.FileName);
                    var name = Path.GetFileNameWithoutExtension(imageFile.FileName);
                    model.ImageName = $"{name}-img-{DateTimeHelper.GetCurrentPhilippineTime():yyyyMMddHHmmss}{ext}";
                    model.ImageSavedUrl = await cloudStorageService.UploadFileAsync(imageFile, model.ImageName);
                }

                if (videoFile is { Length: > 0 })
                {
                    var ext = Path.GetExtension(videoFile.FileName);
                    var name = Path.GetFileNameWithoutExtension(videoFile.FileName);
                    model.VideoName = $"{name}-vid-{DateTimeHelper.GetCurrentPhilippineTime():yyyyMMddHHmmss}{ext}";
                    model.VideoSavedUrl = await cloudStorageService.UploadFileAsync(videoFile, model.VideoName);
                }

                model.CreatedBy = username;
                model.CreatedDate = DateTimeHelper.GetCurrentPhilippineTime();

                // Logic from Repository.AddAsync
                if (model.JobOrderId.HasValue)
                {
                    var jobOrder = await unitOfWork.JobOrder.GetJobOrderWithDetailsAsync(model.JobOrderId.Value, cancellationToken);
                    if (jobOrder != null)
                    {
                        model.CustomerId = jobOrder.CustomerId;
                        model.VesselId = jobOrder.VesselId;
                        model.PortId = jobOrder.PortId;
                        model.TerminalId = jobOrder.TerminalId;
                        model.VoyageNumber = jobOrder.VoyageNumber;
                        model.COSNumber = jobOrder.COSNumber;
                        model.Date = jobOrder.Date;
                    }
                }

                if (model is { DateLeft: not null, DateArrived: not null, TimeLeft: not null, TimeArrived: not null })
                {
                    var start = model.DateLeft.Value.ToDateTime(model.TimeLeft.Value);
                    var end = model.DateArrived.Value.ToDateTime(model.TimeArrived.Value);

                    if (end <= start)
                    {
                        return ServiceResult<int>.Failure("Arrival Date/Time must be strictly after Departure Date/Time.");
                    }

                    model.Status = SD.DispatchTicketStatus.ForTariff;
                    var duration = (decimal)(end - start).TotalHours;
                    model.TotalHours = Math.Max(duration, 0.5m);
                }
                else
                {
                    model.Status = SD.DispatchTicketStatus.Pending;
                }

                await unitOfWork.DispatchTicket.AddAsync(model, cancellationToken);
                await unitOfWork.AuditTrail.AddAsync(new AuditTrail(username, $"Create dispatch ticket #{model.DispatchNumber}", "Dispatch Ticket"), cancellationToken);
                await unitOfWork.SaveAsync(cancellationToken);

                return ServiceResult<int>.Success(model.DispatchTicketId, $"Dispatch Ticket #{model.DispatchNumber} was successfully created.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to create dispatch ticket.");
                return ServiceResult<int>.Failure(ex.Message);
            }
        }

        public async Task<ServiceResult> UpdateDispatchTicketAsync(ServiceRequestViewModel viewModel, IFormFile? imageFile, IFormFile? videoFile, string username, CancellationToken cancellationToken)
        {
            try
            {
                if (!await IsTicketJobOrderEditableAsync(viewModel.DispatchTicketId!.Value, cancellationToken))
                {
                    return ServiceResult.Failure("Cannot edit ticket â€” parent Job Order is cancelled or closed.");
                }

                var currentModel = await unitOfWork.DispatchTicket.GetAsync(dt => dt.DispatchTicketId == viewModel.DispatchTicketId, cancellationToken);
                if (currentModel == null)
                {
                    return ServiceResult.Failure("Ticket not found.", ServiceResultStatus.NotFound);
                }

                var originalTotalHours = currentModel.TotalHours;
                var changes = new List<string>();

                void AddChange(string name, object? oldVal, object? newVal)
                {
                    if (!Equals(oldVal, newVal))
                    {
                        changes.Add($"{name}: '{oldVal}' -> '{newVal}'");
                    }
                }

                // File management
                if (imageFile != null)
                {
                    if (!string.IsNullOrEmpty(currentModel.ImageName))
                    {
                        await cloudStorageService.DeleteFileAsync(currentModel.ImageName);
                    }

                    var ext = Path.GetExtension(imageFile.FileName);
                    var name = Path.GetFileNameWithoutExtension(imageFile.FileName);
                    currentModel.ImageName = $"{name}-img-{DateTimeHelper.GetCurrentPhilippineTime():yyyyMMddHHmmss}{ext}";
                    currentModel.ImageSavedUrl = await cloudStorageService.UploadFileAsync(imageFile, currentModel.ImageName);
                    changes.Add("Image updated");
                }

                if (videoFile != null)
                {
                    if (!string.IsNullOrEmpty(currentModel.VideoName))
                    {
                        await cloudStorageService.DeleteFileAsync(currentModel.VideoName);
                    }

                    var ext = Path.GetExtension(videoFile.FileName);
                    var name = Path.GetFileNameWithoutExtension(videoFile.FileName);
                    currentModel.VideoName = $"{name}-vid-{DateTimeHelper.GetCurrentPhilippineTime():yyyyMMddHHmmss}{ext}";
                    currentModel.VideoSavedUrl = await cloudStorageService.UploadFileAsync(videoFile, currentModel.VideoName);
                    changes.Add("Video updated");
                }

                // Date validation and total hours
                if (viewModel is { DateLeft: not null, DateArrived: not null, TimeLeft: not null, TimeArrived: not null })
                {
                    var departure = viewModel.DateLeft.Value.ToDateTime(viewModel.TimeLeft.Value);
                    var arrival = viewModel.DateArrived.Value.ToDateTime(viewModel.TimeArrived.Value);
                    if (arrival <= departure)
                    {
                        return ServiceResult.Failure("Date/Time Left cannot be later than Date/Time Arrived!");
                    }

                    var newTotalHours = Math.Max((decimal)(arrival - departure).TotalHours, 0.5m);
                    AddChange("TotalHours", originalTotalHours, newTotalHours);
                    currentModel.TotalHours = newTotalHours;
                }

                // Map other fields and track changes
                AddChange(nameof(viewModel.Date), currentModel.Date, viewModel.Date);
                AddChange(nameof(viewModel.DispatchNumber), currentModel.DispatchNumber, viewModel.DispatchNumber);
                AddChange(nameof(viewModel.COSNumber), currentModel.COSNumber, viewModel.COSNumber);
                AddChange(nameof(viewModel.VoyageNumber), currentModel.VoyageNumber, viewModel.VoyageNumber);
                AddChange(nameof(viewModel.CustomerId), currentModel.CustomerId, viewModel.CustomerId);
                AddChange(nameof(viewModel.DateLeft), currentModel.DateLeft, viewModel.DateLeft);
                AddChange(nameof(viewModel.TimeLeft), currentModel.TimeLeft, viewModel.TimeLeft);
                AddChange(nameof(viewModel.DateArrived), currentModel.DateArrived, viewModel.DateArrived);
                AddChange(nameof(viewModel.TimeArrived), currentModel.TimeArrived, viewModel.TimeArrived);
                AddChange(nameof(viewModel.TerminalId), currentModel.TerminalId, viewModel.TerminalId);
                AddChange(nameof(viewModel.PortId), currentModel.PortId, viewModel.PortId);
                AddChange(nameof(viewModel.ServiceId), currentModel.ServiceId, viewModel.ServiceId);
                AddChange(nameof(viewModel.TugBoatId), currentModel.TugBoatId, viewModel.TugBoatId);
                AddChange(nameof(viewModel.TugMasterId), currentModel.TugMasterId, viewModel.TugMasterId);
                AddChange(nameof(viewModel.VesselId), currentModel.VesselId, viewModel.VesselId);
                AddChange(nameof(viewModel.Remarks), currentModel.Remarks, viewModel.Remarks);

                currentModel.Date = viewModel.Date;
                currentModel.DispatchNumber = viewModel.DispatchNumber;
                currentModel.COSNumber = viewModel.COSNumber;
                currentModel.VoyageNumber = viewModel.VoyageNumber;
                currentModel.CustomerId = viewModel.CustomerId;
                currentModel.DateLeft = viewModel.DateLeft;
                currentModel.TimeLeft = viewModel.TimeLeft;
                currentModel.DateArrived = viewModel.DateArrived;
                currentModel.TimeArrived = viewModel.TimeArrived;
                currentModel.TerminalId = viewModel.TerminalId;
                currentModel.PortId = viewModel.PortId;
                currentModel.ServiceId = viewModel.ServiceId;
                currentModel.TugBoatId = viewModel.TugBoatId;
                currentModel.TugMasterId = viewModel.TugMasterId;
                currentModel.VesselId = viewModel.VesselId;
                currentModel.Remarks = viewModel.Remarks;
                currentModel.JobOrderId = viewModel.JobOrderId;
                currentModel.EditedBy = username;
                currentModel.EditedDate = DateTimeHelper.GetCurrentPhilippineTime();

                // Smart Reset Logic: Only reset tariff if critical fields changed
                var criticalFieldsChanged = changes.Any(c => 
                    c.StartsWith("ServiceId:") || 
                    c.StartsWith("TugBoatId:") || 
                    c.StartsWith("DateLeft:") || 
                    c.StartsWith("TimeLeft:") || 
                    c.StartsWith("DateArrived:") || 
                    c.StartsWith("TimeArrived:") ||
                    c.StartsWith("TotalHours:"));

                if (criticalFieldsChanged)
                {
                    currentModel.Status = SD.DispatchTicketStatus.ForTariff;
                    currentModel.DispatchRate = 0;
                    currentModel.DispatchBillingAmount = 0;
                    currentModel.DispatchDiscount = 0;
                    currentModel.DispatchNetRevenue = 0;
                    currentModel.BAFRate = 0;
                    currentModel.BAFBillingAmount = 0;
                    currentModel.BAFDiscount = 0;
                    currentModel.BAFNetRevenue = 0;
                    currentModel.TotalBilling = 0;
                    currentModel.TotalNetRevenue = 0;
                    currentModel.ApOtherTugs = 0;
                    currentModel.TariffBy = string.Empty;
                    currentModel.TariffDate = default;
                    currentModel.TariffEditedBy = string.Empty;
                    currentModel.TariffEditedDate = null;
                    changes.Add("Tariff data reset due to critical field change");
                }

                var auditMessage = changes.Any()
                    ? $"Edit dispatch ticket #{currentModel.DispatchNumber}, {string.Join(", ", changes)}"
                    : $"No changes detected for #{currentModel.DispatchNumber}";

                await unitOfWork.AuditTrail.AddAsync(new AuditTrail(username, auditMessage, "Dispatch Ticket"), cancellationToken);
                await unitOfWork.SaveAsync(cancellationToken);

                return ServiceResult.Success("Entry edited successfully!");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to edit ticket.");
                return ServiceResult.Failure(ex.Message);
            }
        }

        public async Task<ServiceResult> SaveTariffAsync(DispatchTicket model, string chargeType, string chargeType2, string username, bool isEdit, CancellationToken cancellationToken)
        {
            try
            {
                if (!await IsTicketJobOrderEditableAsync(model.DispatchTicketId, cancellationToken))
                {
                    return ServiceResult.Failure("Cannot set/edit tariff â€” parent Job Order is cancelled or closed.");
                }

                var currentModel = await unitOfWork.DispatchTicket.GetAsync(dt => dt.DispatchTicketId == model.DispatchTicketId, cancellationToken);
                if (currentModel == null)
                {
                    return ServiceResult.Failure("Ticket not found.", ServiceResultStatus.NotFound);
                }

                string auditMessage;
                if (isEdit)
                {
                    var changes = new List<string>();
                    void AddChange(string name, object? oldVal, object? newVal)
                    {
                        if (!Equals(oldVal, newVal))
                        {
                            changes.Add($"{name}: {oldVal} -> {newVal}");
                        }
                    }

                    AddChange("CustomerId", currentModel.CustomerId, model.CustomerId);
                    AddChange("DispatchChargeType", currentModel.DispatchChargeType, chargeType);
                    AddChange("BAFChargeType", currentModel.BAFChargeType, chargeType2);
                    AddChange("DispatchRate", currentModel.DispatchRate, model.DispatchRate);
                    AddChange("BAFRate", currentModel.BAFRate, model.BAFRate);
                    AddChange("DispatchDiscount", currentModel.DispatchDiscount, model.DispatchDiscount);
                    AddChange("BAFDiscount", currentModel.BAFDiscount, model.BAFDiscount);
                    AddChange("DispatchBillingAmount", currentModel.DispatchBillingAmount, model.DispatchBillingAmount);
                    AddChange("BAFBillingAmount", currentModel.BAFBillingAmount, model.BAFBillingAmount);
                    AddChange("DispatchNetRevenue", currentModel.DispatchNetRevenue, model.DispatchNetRevenue);
                    AddChange("BAFNetRevenue", currentModel.BAFNetRevenue, model.BAFNetRevenue);
                    AddChange("ApOtherTugs", currentModel.ApOtherTugs, model.ApOtherTugs);
                    AddChange("TotalBilling", currentModel.TotalBilling, model.TotalBilling);
                    AddChange("TotalNetRevenue", currentModel.TotalNetRevenue, model.TotalNetRevenue);

                    currentModel.TariffEditedBy = username;
                    currentModel.TariffEditedDate = DateTimeHelper.GetCurrentPhilippineTime();
                    auditMessage = changes.Any() ? $"Edit tariff #{currentModel.DispatchNumber} {string.Join(", ", changes)}" : $"No changes detected for tariff details #{currentModel.DispatchNumber}";
                }
                else
                {
                    currentModel.TariffBy = username;
                    currentModel.TariffDate = DateTimeHelper.GetCurrentPhilippineTime();
                    auditMessage = $"Set Tariff #{currentModel.DispatchTicketId}";
                }

                currentModel.Status = SD.DispatchTicketStatus.ForApproval;
                currentModel.CustomerId = model.CustomerId;
                currentModel.DispatchChargeType = chargeType;
                currentModel.BAFChargeType = chargeType2;
                currentModel.DispatchRate = model.DispatchRate;
                currentModel.BAFRate = model.BAFRate;
                currentModel.DispatchDiscount = model.DispatchDiscount;
                currentModel.BAFDiscount = model.BAFDiscount;
                currentModel.DispatchBillingAmount = model.DispatchBillingAmount;
                currentModel.BAFBillingAmount = model.BAFBillingAmount;
                currentModel.DispatchNetRevenue = model.DispatchNetRevenue;
                currentModel.BAFNetRevenue = model.BAFNetRevenue;
                currentModel.ApOtherTugs = model.ApOtherTugs;
                currentModel.TotalBilling = model.TotalBilling;
                currentModel.TotalNetRevenue = model.TotalNetRevenue;

                await unitOfWork.AuditTrail.AddAsync(new AuditTrail(username, auditMessage, "Tariff"), cancellationToken);
                await unitOfWork.SaveAsync(cancellationToken);

                return ServiceResult.Success(isEdit ? "Tariff updated successfully." : "Tariff set successfully.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to save tariff.");
                return ServiceResult.Failure(ex.Message);
            }
        }

        public async Task<ServiceResult> ApproveTariffAsync(int id, string username, CancellationToken cancellationToken)
        {
            try
            {
                if (!await IsTicketJobOrderEditableAsync(id, cancellationToken))
                {
                    return ServiceResult.Failure("Cannot approve tariff â€” parent Job Order is cancelled or closed.");
                }

                var model = await unitOfWork.DispatchTicket.GetAsync(dt => dt.DispatchTicketId == id, cancellationToken);
                if (model == null)
                {
                    return ServiceResult.Failure("Ticket not found.", ServiceResultStatus.NotFound);
                }

                model.Status = SD.DispatchTicketStatus.ForBilling;
                model.EditedBy = username;
                model.EditedDate = DateTimeHelper.GetCurrentPhilippineTime();

                await unitOfWork.AuditTrail.AddAsync(new AuditTrail(username, $"Approved tariff for dispatch ticket #{model.DispatchNumber}", "Dispatch Ticket"), cancellationToken);
                await unitOfWork.SaveAsync(cancellationToken);

                return ServiceResult.Success("Tariff approved successfully.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to approve tariff.");
                return ServiceResult.Failure(ex.Message);
            }
        }

        public async Task<ServiceResult> DisapproveTariffAsync(int id, string reason, string username, CancellationToken cancellationToken)
        {
            try
            {
                if (!await IsTicketJobOrderEditableAsync(id, cancellationToken))
                {
                    return ServiceResult.Failure("Cannot disapprove tariff â€” parent Job Order is cancelled or closed.");
                }

                if (string.IsNullOrWhiteSpace(reason) || reason.Length < 10)
                {
                    return ServiceResult.Failure("Please provide a detailed reason (at least 10 characters)");
                }

                var model = await unitOfWork.DispatchTicket.GetAsync(dt => dt.DispatchTicketId == id, cancellationToken);
                if (model == null)
                {
                    return ServiceResult.Failure("Ticket not found.", ServiceResultStatus.NotFound);
                }

                model.Status = SD.DispatchTicketStatus.Disapproved;
                model.EditedBy = username;
                model.EditedDate = DateTimeHelper.GetCurrentPhilippineTime();
                model.Remarks = string.IsNullOrEmpty(model.Remarks)
                    ? $"Disapproved: {reason}"
                    : $"{model.Remarks} | Disapproved: {reason}";

                await unitOfWork.AuditTrail.AddAsync(new AuditTrail(username, $"Disapproved tariff for dispatch ticket #{model.DispatchNumber}. Reason: {reason}", "Dispatch Ticket"), cancellationToken);
                await unitOfWork.SaveAsync(cancellationToken);

                return ServiceResult.Success("Tariff disapproved successfully.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to disapprove tariff.");
                return ServiceResult.Failure(ex.Message);
            }
        }

        public async Task<ServiceResult> DeleteImageAsync(int id, CancellationToken cancellationToken)
        {
            try
            {
                var model = await unitOfWork.DispatchTicket.GetAsync(dt => dt.DispatchTicketId == id, cancellationToken);
                if (model == null)
                {
                    return ServiceResult.Failure("Ticket not found.", ServiceResultStatus.NotFound);
                }

                if (!string.IsNullOrEmpty(model.ImageName))
                {
                    await cloudStorageService.DeleteFileAsync(model.ImageName);
                }

                model.ImageName = null;
                model.ImageSavedUrl = null;
                await unitOfWork.SaveAsync(cancellationToken);

                return ServiceResult.Success("Image Deleted Successfully!");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to delete image.");
                return ServiceResult.Failure(ex.Message);
            }
        }

        public async Task<(IEnumerable<DispatchTicket> Data, int RecordsFiltered, int TotalRecords)> GetPagedDispatchTicketsAsync(DataTablesParameters parameters, string filterType, CancellationToken cancellationToken)
        {
            return await unitOfWork.DispatchTicket.GetPagedDispatchTicketsAsync(parameters, filterType, cancellationToken);
        }

        public async Task<ServiceResult<object>> CheckForTariffRateAsync(int customerId, int dispatchTicketId, CancellationToken cancellationToken)
        {
            var dispatchModel = await unitOfWork.DispatchTicket.GetAsync(dt => dt.DispatchTicketId == dispatchTicketId, cancellationToken);
            if (dispatchModel == null)
            {
                return ServiceResult<object>.Failure("Ticket not found.", ServiceResultStatus.NotFound);
            }

            var tariffRate =
                await unitOfWork.TariffTable.GetAsync(t =>
                        t.CustomerId == customerId &&
                        t.TerminalId == dispatchModel.TerminalId &&
                        t.ServiceId == dispatchModel.ServiceId &&
                        t.AsOfDate <= dispatchModel.DateLeft,
                    cancellationToken)
                ??
                await unitOfWork.TariffTable.GetAsync(t =>
                        t.CustomerId == customerId &&
                        t.TerminalId == dispatchModel.TerminalId &&
                        t.AsOfDate <= dispatchModel.DateLeft,
                    cancellationToken)
                ??
                await unitOfWork.TariffTable.GetAsync(t =>
                        t.CustomerId == customerId &&
                        t.AsOfDate <= dispatchModel.DateLeft,
                    cancellationToken);

            if (tariffRate != null)
            {
                return ServiceResult<object>.Success(new
                {
                    tariffRate.Dispatch,
                    tariffRate.BAF,
                    tariffRate.DispatchDiscount,
                    tariffRate.BAFDiscount,
                    Exists = true
                });
            }

            return ServiceResult<object>.Success(new { Exists = false });
        }

        public async Task<bool> IsJobOrderEditableAsync(int? jobOrderId, CancellationToken cancellationToken)
        {
            return await unitOfWork.DispatchTicket.IsJobOrderEditableAsync(jobOrderId, cancellationToken);
        }

        public async Task<bool> IsTicketJobOrderEditableAsync(int dispatchTicketId, CancellationToken cancellationToken)
        {
            var ticket = await unitOfWork.DispatchTicket.GetAsync(dt => dt.DispatchTicketId == dispatchTicketId, cancellationToken);
            return ticket != null && await IsJobOrderEditableAsync(ticket.JobOrderId, cancellationToken);
        }

        public async Task<List<object>> SearchCustomersAsync(string? term, CancellationToken cancellationToken)
        {
            var customers = await unitOfWork.Customer.SearchCustomersAsync(term ?? string.Empty, 10, cancellationToken);

            return customers.Select(c => (object)new
            {
                value = c.CustomerId,
                name = c.CustomerName
            }).ToList();
        }
    }
}


