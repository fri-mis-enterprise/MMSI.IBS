using System.Linq.Dynamic.Core;
using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using IBS.Models.Enums;
using IBS.Models.MSAP;
using IBS.Models.MSAP.ViewModels;
using IBS.Services;
using IBS.Services.AccessControl;
using IBS.Services.Attributes;
using IBS.Utility.Constants;
using IBS.Utility.Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace IBSWeb.Areas.User.Controllers
{
    [Area("User")]
    [RequireAnyAccess(
        "Access denied. You don't have permission to access Service Requests.",
        ProcedureEnum.CreateServiceRequest,
        ProcedureEnum.PostServiceRequest)]
    public class ServiceRequestController(
        ApplicationDbContext dbContext,
        IUnitOfWork unitOfWork,
        UserManager<ApplicationUser> userManager,
        ICloudStorageService cloudStorageService,
        IAccessControlService accessControl,
        ILogger<ServiceRequestController> logger)
        : Controller
    {
        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            ViewBag.CanEdit = userId != null && await accessControl.HasAccessAsync(userId, ProcedureEnum.CreateServiceRequest);
            return View(Enumerable.Empty<DispatchTicket>());
        }

        private async Task PopulateJobOrdersList(ServiceRequestViewModel viewModel, CancellationToken cancellationToken)
        {
            var openJobOrders = await dbContext.MsapJobOrders
                .Where(j => j.Status == SD.JobOrderStatus.Open &&
                            !dbContext.MsapBillings.Any(b => b.JobOrderId == j.JobOrderId && b.Status == SD.BillingStatus.ForPosting))
                .Include(j => j.Vessel)
                .Include(j => j.Customer)
                .OrderByDescending(j => j.JobOrderNumber)
                .Select(j => new SelectListItem
                {
                    Value = j.JobOrderId.ToString(),
                    Text = $"{j.JobOrderNumber} - {j.Vessel.VesselName} ({j.Customer.CustomerName})"
                }).ToListAsync(cancellationToken);
            viewModel.JobOrders = openJobOrders;
        }

        [HttpGet]
        [RequireAccess(ProcedureEnum.CreateServiceRequest, "Access denied. You don't have permission to create Service Requests.")]
        public async Task<IActionResult> Create(int? jobOrderId, CancellationToken cancellationToken = default)
        {
            var viewModel = new ServiceRequestViewModel { JobOrderId = jobOrderId };
            viewModel = await unitOfWork.ServiceRequest.GetDispatchTicketSelectLists(viewModel,
                cancellationToken);
            viewModel.Customers = await unitOfWork.GetCustomerListAsyncById(cancellationToken);
            await PopulateJobOrdersList(viewModel, cancellationToken);
            ViewData["PortId"] = 0;
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequireAccess(ProcedureEnum.CreateServiceRequest, "Access denied. You don't have permission to create Service Requests.")]
        public async Task<IActionResult> Create(ServiceRequestViewModel viewModel, IFormFile? imageFile, IFormFile? videoFile, CancellationToken cancellationToken = default)
        {
            viewModel = await unitOfWork.ServiceRequest.GetDispatchTicketSelectLists(viewModel,
                cancellationToken);
            viewModel.Customers = await unitOfWork.GetCustomerListAsyncById(cancellationToken);
            await PopulateJobOrdersList(viewModel, cancellationToken);
            ViewData["PortId"] = viewModel.PortId;

            var workflowError = await GetWorkflowErrorAsync(viewModel.JobOrderId, viewModel.Date, cancellationToken);
            if (workflowError != null)
            {
                TempData["warning"] = workflowError;
                return View(viewModel);
            }

            if (imageFile == null || imageFile.Length == 0)
            {
                TempData["warning"] = "An image of the Dispatch/Mooring Ticket is strictly required!";
                return View(viewModel);
            }

            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var model = viewModel.ToEntity();

                model.CreatedBy = await GetUserNameAsync() ?? throw new InvalidOperationException();
                model.CreatedDate = DateTimeHelper.GetCurrentPhilippineTime();

                if (imageFile is { Length: > 0 })
                {
                    model.ImageName = GenerateFileNameToSave(imageFile.FileName,
                        "img");
                    model.ImageSavedUrl = await cloudStorageService.UploadFileAsync(imageFile,
                        model.ImageName!);
                }

                if (videoFile is { Length: > 0 })
                {
                    model.VideoName = GenerateFileNameToSave(videoFile.FileName,
                        "vid");
                    model.VideoSavedUrl = await cloudStorageService.UploadFileAsync(videoFile,
                        model.VideoName!);
                }

                if (model is { DateLeft: not null, DateArrived: not null, TimeLeft: not null, TimeArrived: not null })
                {
                    if (model.DateLeft < model.DateArrived || (model.DateLeft == model.DateArrived && model.TimeLeft < model.TimeArrived))
                    {
                        var dateTimeLeft = model.DateLeft.Value.ToDateTime(model.TimeLeft.Value);
                        var dateTimeArrived = model.DateArrived.Value.ToDateTime(model.TimeArrived.Value);
                        var timeDifference = dateTimeArrived - dateTimeLeft;
                        model.TotalHours = Math.Round(Math.Max((decimal)timeDifference.TotalHours, 1m),
                            2);
                    }
                    else
                    {
                        await transaction.RollbackAsync(cancellationToken);
                        TempData["warning"] = "Start Date/Time should be earlier than End Date/Time!";
                        return View(viewModel);
                    }
                }

                model.Status = IsReadyToPost(model) ? SD.ServiceRequestStatus.Requested : SD.ServiceRequestStatus.Draft;

                await unitOfWork.DispatchTicket.AddAsync(model,
                    cancellationToken);

                #region -- Audit Trail

                var audit = new AuditTrail(
                    await GetUserNameAsync() ?? throw new InvalidOperationException(),
                    model.JobOrderId.HasValue
                        ? $"Create service request #{model.DispatchNumber} (Job Order #{model.JobOrderId})"
                        : $"Create service request #{model.DispatchNumber}",
                    "Service Request"
                );

                await unitOfWork.AuditTrail.AddAsync(audit,
                    cancellationToken);

                #endregion --Audit Trail

                await unitOfWork.SaveAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = $"Service Request #{model.DispatchNumber} was successfully created.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                logger.LogError(ex,
                    "Failed to create service request");
                TempData["error"] = ex.Message;
                return View(viewModel);
            }
        }

        [HttpGet]
        [RequireAccess(ProcedureEnum.CreateServiceRequest, "Access denied. You don't have permission to edit Service Requests.")]
        public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken = default)
        {
            var model = await unitOfWork.DispatchTicket.GetAsync(dt => dt.DispatchTicketId == id,
                cancellationToken);

            if (model == null)
            {
                return NotFound();
            }

            var viewModel = new ServiceRequestViewModel();
            viewModel.FromEntity(model);
            viewModel = await unitOfWork.ServiceRequest.GetDispatchTicketSelectLists(viewModel,
                cancellationToken);
            viewModel.Customers = await unitOfWork.GetCustomerListAsyncById(cancellationToken);
            await PopulateJobOrdersList(viewModel, cancellationToken);

            if (!string.IsNullOrEmpty(viewModel.ImageName))
            {
                viewModel.ImageSignedUrl = await GenerateSignedUrl(viewModel.ImageName);
            }
            if (!string.IsNullOrEmpty(viewModel.VideoName))
            {
                viewModel.VideoSignedUrl = await GenerateSignedUrl(viewModel.VideoName);
            }

            ViewData["PortId"] = viewModel.Terminal?.Port.PortId;
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequireAccess(ProcedureEnum.CreateServiceRequest, "Access denied. You don't have permission to edit Service Requests.")]
        public async Task<IActionResult> Edit(ServiceRequestViewModel viewModel, IFormFile? imageFile, IFormFile? videoFile, CancellationToken cancellationToken = default)
        {
            viewModel = await unitOfWork.ServiceRequest.GetDispatchTicketSelectLists(viewModel,
                cancellationToken);
            viewModel.Customers = await unitOfWork.GetCustomerListAsyncById(cancellationToken);
            await PopulateJobOrdersList(viewModel, cancellationToken);
            ViewData["PortId"] = viewModel.PortId;

            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var incoming = viewModel.ToEntity();
                var currentModel = await unitOfWork.DispatchTicket.GetAsync(dt =>
                        dt.DispatchTicketId == incoming.DispatchTicketId,
                    cancellationToken);

                if (currentModel == null)
                {
                    throw new NullReferenceException("Current record not found.");
                }

                // Only allow editing while still in pre-approval states
                if (currentModel.Status != SD.ServiceRequestStatus.Draft &&
                    currentModel.Status != SD.ServiceRequestStatus.Requested)
                {
                    TempData["error"] = "Service request can no longer be edited — it has already been accepted into the workflow.";
                    return RedirectToAction(nameof(Index));
                }

                currentModel.EditedBy = await GetUserNameAsync() ?? throw new InvalidOperationException();
                currentModel.EditedDate = DateTimeHelper.GetCurrentPhilippineTime();

                var workflowError = await GetWorkflowErrorAsync(currentModel.JobOrderId, currentModel.Date, cancellationToken)
                    ?? await GetWorkflowErrorAsync(incoming.JobOrderId, incoming.Date, cancellationToken);
                if (workflowError != null)
                {
                    TempData["error"] = workflowError;
                    return View(viewModel);
                }

                if (imageFile != null)
                {
                    if (!string.IsNullOrEmpty(currentModel.ImageName))
                    {
                        await cloudStorageService.DeleteFileAsync(currentModel.ImageName);
                    }

                    incoming.ImageName = GenerateFileNameToSave(imageFile.FileName, "img");
                    incoming.ImageSavedUrl = await cloudStorageService.UploadFileAsync(imageFile, incoming.ImageName!);
                }

                if (videoFile != null)
                {
                    if (!string.IsNullOrEmpty(currentModel.VideoName))
                    {
                        await cloudStorageService.DeleteFileAsync(currentModel.VideoName);
                    }

                    incoming.VideoName = GenerateFileNameToSave(videoFile.FileName, "vid");
                    incoming.VideoSavedUrl = await cloudStorageService.UploadFileAsync(videoFile, incoming.VideoName!);
                }

                if (incoming is { DateLeft: not null, DateArrived: not null, TimeLeft: not null, TimeArrived: not null })
                {
                    if (incoming.DateLeft < incoming.DateArrived || (incoming.DateLeft == incoming.DateArrived && incoming.TimeLeft < incoming.TimeArrived))
                    {
                        var dateTimeLeft = incoming.DateLeft.Value.ToDateTime(incoming.TimeLeft.Value);
                        var dateTimeArrived = incoming.DateArrived.Value.ToDateTime(incoming.TimeArrived.Value);
                        var timeDifference = dateTimeArrived - dateTimeLeft;
                        incoming.TotalHours = Math.Round(Math.Max((decimal)timeDifference.TotalHours, 1m), 2);
                    }
                    else
                    {
                        await transaction.RollbackAsync(cancellationToken);
                        TempData["warning"] = "Date/Time Left cannot be later than Date/Time Arrived!";
                        return View(viewModel);
                    }
                }

                #region -- Audit changes

                var changes = new List<string>();
                if (currentModel.Date != incoming.Date) { changes.Add($"CreateDate: {currentModel.Date} -> {incoming.Date}"); }
                if (currentModel.DispatchNumber != incoming.DispatchNumber) { changes.Add($"DispatchNumber: {currentModel.DispatchNumber} -> {incoming.DispatchNumber}"); }
                if (currentModel.COSNumber != incoming.COSNumber) { changes.Add($"COSNumber: {currentModel.COSNumber} -> {incoming.COSNumber}"); }
                if (currentModel.VoyageNumber != incoming.VoyageNumber) { changes.Add($"VoyageNumber: {currentModel.VoyageNumber} -> {incoming.VoyageNumber}"); }
                if (currentModel.CustomerId != incoming.CustomerId) { changes.Add($"CustomerId: {currentModel.CustomerId} -> {incoming.CustomerId}"); }
                if (currentModel.DateLeft != incoming.DateLeft) { changes.Add($"DateLeft: {currentModel.DateLeft} -> {incoming.DateLeft}"); }
                if (currentModel.TimeLeft != incoming.TimeLeft) { changes.Add($"TimeLeft: {currentModel.TimeLeft} -> {incoming.TimeLeft}"); }
                if (currentModel.DateArrived != incoming.DateArrived) { changes.Add($"DateArrived: {currentModel.DateArrived} -> {incoming.DateArrived}"); }
                if (currentModel.TimeArrived != incoming.TimeArrived) { changes.Add($"TimeArrived: {currentModel.TimeArrived} -> {incoming.TimeArrived}"); }
                if (currentModel.TotalHours != incoming.TotalHours) { changes.Add($"TotalHours: {currentModel.TotalHours} -> {incoming.TotalHours}"); }
                if (currentModel.TerminalId != incoming.TerminalId) { changes.Add($"TerminalId: {currentModel.TerminalId} -> {incoming.TerminalId}"); }
                if (currentModel.ServiceId != incoming.ServiceId) { changes.Add($"ServiceId: {currentModel.ServiceId} -> {incoming.ServiceId}"); }
                if (currentModel.TugBoatId != incoming.TugBoatId) { changes.Add($"TugBoatId: {currentModel.TugBoatId} -> {incoming.TugBoatId}"); }
                if (currentModel.TugMasterId != incoming.TugMasterId) { changes.Add($"TugMasterId: {currentModel.TugMasterId} -> {incoming.TugMasterId}"); }
                if (currentModel.VesselId != incoming.VesselId) { changes.Add($"VesselId: {currentModel.VesselId} -> {incoming.VesselId}"); }
                if (currentModel.Remarks != incoming.Remarks) { changes.Add($"Remarks: '{currentModel.Remarks}' -> '{incoming.Remarks}'"); }
                if (imageFile != null && currentModel.ImageName != incoming.ImageName) { changes.Add($"ImageName: '{currentModel.ImageName}' -> '{incoming.ImageName}'"); }
                if (videoFile != null && currentModel.VideoName != incoming.VideoName) { changes.Add($"VideoName: '{currentModel.VideoName}' -> '{incoming.VideoName}'"); }

                #endregion

                #region -- Apply changes

                currentModel.Date = incoming.Date;
                currentModel.JobOrderId = incoming.JobOrderId;
                currentModel.DispatchNumber = incoming.DispatchNumber;
                currentModel.COSNumber = incoming.COSNumber;
                currentModel.VoyageNumber = incoming.VoyageNumber;
                currentModel.CustomerId = incoming.CustomerId;
                currentModel.DateLeft = incoming.DateLeft;
                currentModel.TimeLeft = incoming.TimeLeft;
                currentModel.DateArrived = incoming.DateArrived;
                currentModel.TimeArrived = incoming.TimeArrived;
                currentModel.TotalHours = incoming.TotalHours;
                currentModel.TerminalId = incoming.TerminalId;
                currentModel.ServiceId = incoming.ServiceId;
                currentModel.TugBoatId = incoming.TugBoatId;
                currentModel.TugMasterId = incoming.TugMasterId;
                currentModel.VesselId = incoming.VesselId;
                currentModel.PortId = incoming.PortId;
                currentModel.Remarks = incoming.Remarks;

                if (imageFile != null)
                {
                    currentModel.ImageName = incoming.ImageName;
                    currentModel.ImageSavedUrl = incoming.ImageSavedUrl;
                }

                if (videoFile != null)
                {
                    currentModel.VideoName = incoming.VideoName;
                    currentModel.VideoSavedUrl = incoming.VideoSavedUrl;
                }

                #endregion -- Apply changes

                #region -- Audit Trail

                var activity = changes.Any()
                    ? $"Edit service request #{currentModel.DispatchNumber}, {string.Join(", ", changes)}"
                    : $"No changes detected: id#{currentModel.DispatchNumber}";

                var audit = new AuditTrail(
                    await GetUserNameAsync() ?? throw new InvalidOperationException(),
                    activity,
                    "Service Request"
                );

                await unitOfWork.AuditTrail.AddAsync(audit,
                    cancellationToken);

                #endregion --Audit Trail

                currentModel.Status = IsReadyToPost(currentModel) ? SD.ServiceRequestStatus.Requested : SD.ServiceRequestStatus.Draft;
                await unitOfWork.SaveAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = "Entry edited successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                logger.LogError(ex,
                    "Failed to edit service request");
                TempData["error"] = ex.Message;
                return View(viewModel);
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetJobOrderDetails(int jobOrderId, CancellationToken cancellationToken = default)
        {
            var jobOrder = await dbContext.MsapJobOrders
                .Include(j => j.Customer)
                .Include(j => j.Vessel)
                .Include(j => j.Port)
                .Include(j => j.Terminal)
                .FirstOrDefaultAsync(j => j.JobOrderId == jobOrderId, cancellationToken);

            if (jobOrder == null)
            {
                return Json(new { success = false });
            }

            return Json(new
            {
                success = true,
                customerId = jobOrder.CustomerId,
                customerName = jobOrder.Customer.CustomerName,
                vesselId = jobOrder.VesselId,
                vesselName = jobOrder.Vessel.VesselName,
                portId = jobOrder.PortId,
                portName = jobOrder.Port.PortName,
                terminalId = jobOrder.TerminalId,
                terminalName = jobOrder.Terminal.TerminalName,
                voyageNumber = jobOrder.VoyageNumber,
                cosNumber = jobOrder.COSNumber,
                date = jobOrder.Date.ToString("yyyy-MM-dd")
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetVesselVoyageType(int vesselId)
        {
            var vesselType = await dbContext.MsapVessels
                .Where(v => v.VesselId == vesselId)
                .Select(v => v.VesselType)
                .FirstOrDefaultAsync();
            var voyageType = vesselType == "FOREIGN" ? "Foreign" : "Local";
            return Json(voyageType);
        }

        [HttpGet]
        public async Task<IActionResult> ChangeTerminal(int portId, CancellationToken cancellationToken = default)
        {
            var terminals = await unitOfWork.Terminal.GetAllAsync(t => t.PortId == portId,
                cancellationToken);

            var terminalsList = terminals.Select(t => new SelectListItem
            {
                Value = t.TerminalId.ToString(),
                Text = t.TerminalName
            }).ToList();

            return Json(terminalsList);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GetDispatchTicketLists([FromForm] DataTablesParameters parameters, CancellationToken cancellationToken)
        {
            var currentUser = await userManager.GetUserAsync(User);

            try
            {
                var queriedBase = dbContext.MsapDispatchTickets
                    .Include(dt => dt.Service)
                    .Include(dt => dt.Terminal)
                    .ThenInclude(dt => dt.Port)
                    .Include(dt => dt.Tugboat)
                    .Include(dt => dt.TugMaster)
                    .Include(dt => dt.Vessel)
                    .Include(dt => dt.Customer)
                    .Where(dt => dt.Status == SD.ServiceRequestStatus.Draft ||
                                 dt.Status == SD.ServiceRequestStatus.Requested ||
                                 dt.Status == SD.ServiceRequestStatus.ServiceRequestDeleted);

                // Port Coordinators can only see their own requests
                if (User.IsInRole("PortCoordinator"))
                {
                    queriedBase = queriedBase.Where(dt => dt.CreatedBy == currentUser!.UserName);
                }

                var queried = await queriedBase.ToListAsync(cancellationToken);
                var totalRecords = queried.Count;

                // Global search
                if (!string.IsNullOrEmpty(parameters.Search.Value))
                {
                    var searchValue = parameters.Search.Value.ToLower();

                    queried = queried
                    .Where(dt =>
                        dt.COSNumber?.Contains(searchValue, StringComparison.OrdinalIgnoreCase) == true ||
                        dt.DispatchNumber?.Contains(searchValue, StringComparison.OrdinalIgnoreCase) == true ||
                        dt.Service?.ServiceName?.Contains(searchValue, StringComparison.OrdinalIgnoreCase) == true ||
                        dt.Terminal?.TerminalName?.Contains(searchValue, StringComparison.OrdinalIgnoreCase) == true ||
                        dt.Terminal?.Port?.PortName?.Contains(searchValue, StringComparison.OrdinalIgnoreCase) == true ||
                        dt.Tugboat?.TugboatName?.Contains(searchValue, StringComparison.OrdinalIgnoreCase) == true ||
                        dt.TugMaster?.TugMasterName?.Contains(searchValue, StringComparison.OrdinalIgnoreCase) == true ||
                        dt.Vessel?.VesselName?.Contains(searchValue, StringComparison.OrdinalIgnoreCase) == true ||
                        dt.Status.Contains(searchValue, StringComparison.OrdinalIgnoreCase)
                        )
                        .ToList();
                }

                // Column-specific search
                foreach (var column in parameters.Columns)
                {
                    if (!string.IsNullOrEmpty(column.Search.Value))
                    {
                        var searchValue = column.Search.Value.ToLower();
                        switch (column.Data)
                        {
                            case "status":
                                queried = searchValue switch
                                {
                                    "requested" => queried.Where(s => s.Status == SD.ServiceRequestStatus.Requested).ToList(),
                                    "draft" => queried.Where(s => s.Status == SD.ServiceRequestStatus.Draft).ToList(),
                                    "service request deleted" => queried.Where(s => s.Status == SD.ServiceRequestStatus.ServiceRequestDeleted).ToList(),
                                    _ => queried.Where(s => !string.IsNullOrEmpty(s.Status)).ToList()
                                };
                                break;
                            case "date":
                                if (DateOnly.TryParse(searchValue, out var date))
                                    queried = queried.Where(s => s.Date == date).ToList();
                                break;
                        }
                    }
                }

                // Sorting
                if (parameters.Order?.Count > 0)
                {
                    var orderColumn = parameters.Order[0];
                    var columnName = parameters.Columns[orderColumn.Column].Data;
                    var sortDirection = orderColumn.Dir.ToLower() == "asc" ? "ascending" : "descending";

                    queried = queried
                        .AsQueryable()
                        .OrderBy($"{columnName} {sortDirection}")
                        .ToList();
                }

                var filteredRecords = queried.Count;

                var pagedData = queried
                    .Skip(parameters.Start)
                    .Take(parameters.Length)
                    .ToList();

                foreach (var dispatchTicket in pagedData.Where(dt => !string.IsNullOrEmpty(dt.ImageName)))
                {
                    dispatchTicket.ImageSignedUrl = await GenerateSignedUrl(dispatchTicket.ImageName!);
                }
                foreach (var dispatchTicket in pagedData.Where(dt => !string.IsNullOrEmpty(dt.VideoName)))
                {
                    dispatchTicket.VideoSignedUrl = await GenerateSignedUrl(dispatchTicket.VideoName!);
                }

                return Json(new
                {
                    draw = parameters.Draw,
                    recordsTotal = totalRecords,
                    recordsFiltered = filteredRecords,
                    data = pagedData
                });

            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Failed to dispatch tickets");
                return Json(new { draw = parameters.Draw, error = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequireAccess(ProcedureEnum.CreateServiceRequest, "Access denied. You don't have permission to edit Service Request attachments.")]
        public async Task<IActionResult> DeleteImage(int id, CancellationToken cancellationToken = default)
        {
            try
            {
                var model = await unitOfWork.DispatchTicket.GetAsync(dt => dt.DispatchTicketId == id,
                    cancellationToken);

                if (model == null)
                {
                    return NotFound();
                }

                if (model.Status is not (SD.ServiceRequestStatus.Draft or SD.ServiceRequestStatus.Requested))
                {
                    TempData["error"] = "Attachments can only be deleted from Draft or Requested service requests.";
                    return RedirectToAction(nameof(Index));
                }

                var workflowError = await GetWorkflowErrorAsync(model.JobOrderId, model.Date, cancellationToken);
                if (workflowError != null)
                {
                    TempData["error"] = workflowError;
                    return RedirectToAction(nameof(Edit), new { id });
                }

                if (string.IsNullOrEmpty(model.ImageName)) return RedirectToAction(nameof(Edit), new { id });
                await cloudStorageService.DeleteFileAsync(model.ImageName);
                model.ImageName = null;
                model.ImageSignedUrl = null;
                model.ImageSavedUrl = null;
                model.Status = SD.ServiceRequestStatus.Draft;
                model.EditedBy = User.Identity?.Name ?? "System";
                model.EditedDate = DateTimeHelper.GetCurrentPhilippineTime();
                await unitOfWork.AuditTrail.AddAsync(new AuditTrail(model.EditedBy,
                    $"Deleted image from service request #{model.DispatchNumber}", "Service Request", model.DispatchTicketId, model.DispatchNumber), cancellationToken);
                await unitOfWork.SaveAsync(cancellationToken);
                TempData["success"] = "Image Deleted Successfully!";
                return RedirectToAction(nameof(Edit),
                    new
                    {
                        id = model.DispatchTicketId
                    });
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Failed to delete image");
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Edit),
                    new
                    {
                        id
                    });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequireAccess(ProcedureEnum.CreateServiceRequest, "Access denied. You don't have permission to edit Service Request attachments.")]
        public async Task<IActionResult> DeleteVideo(int id, CancellationToken cancellationToken = default)
        {
            try
            {
                var model = await unitOfWork.DispatchTicket.GetAsync(dt => dt.DispatchTicketId == id,
                    cancellationToken);

                if (model == null)
                {
                    return NotFound();
                }

                if (model.Status is not (SD.ServiceRequestStatus.Draft or SD.ServiceRequestStatus.Requested))
                {
                    TempData["error"] = "Attachments can only be deleted from Draft or Requested service requests.";
                    return RedirectToAction(nameof(Index));
                }

                var workflowError = await GetWorkflowErrorAsync(model.JobOrderId, model.Date, cancellationToken);
                if (workflowError != null)
                {
                    TempData["error"] = workflowError;
                    return RedirectToAction(nameof(Edit), new { id });
                }

                if (string.IsNullOrEmpty(model.VideoName)) return RedirectToAction(nameof(Edit), new { id });
                await cloudStorageService.DeleteFileAsync(model.VideoName);
                model.VideoName = null;
                model.VideoSignedUrl = null;
                model.VideoSavedUrl = null;
                model.EditedBy = User.Identity?.Name ?? "System";
                model.EditedDate = DateTimeHelper.GetCurrentPhilippineTime();
                await unitOfWork.AuditTrail.AddAsync(new AuditTrail(model.EditedBy,
                    $"Deleted video from service request #{model.DispatchNumber}", "Service Request", model.DispatchTicketId, model.DispatchNumber), cancellationToken);
                await unitOfWork.SaveAsync(cancellationToken);
                TempData["success"] = "Video Deleted Successfully!";
                return RedirectToAction(nameof(Edit),
                    new
                    {
                        id = model.DispatchTicketId
                    });
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Failed to delete video");
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Edit),
                    new
                    {
                        id
                    });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequireAccess(ProcedureEnum.PostServiceRequest, "Access denied. You don't have permission to accept Service Requests.")]
        public async Task<IActionResult> Post(int id, int? jobOrderId, CancellationToken cancellationToken = default)
        {
            var record = await unitOfWork.DispatchTicket.GetAsync(dt => dt.DispatchTicketId == id, cancellationToken);
            if (record is { Status: SD.ServiceRequestStatus.Requested })
            {
                var workflowError = await GetWorkflowErrorAsync(record.JobOrderId, record.Date, cancellationToken);
                if (workflowError != null || !IsReadyToPost(record))
                {
                    TempData["error"] = workflowError ?? "Complete the Service Request and attach a ticket image before acceptance.";
                    return RedirectToAction(nameof(Index));
                }

                record.Status = SD.DispatchTicketStatus.ForTariff;
                record.EditedBy = User.Identity?.Name ?? "System";
                record.EditedDate = DateTimeHelper.GetCurrentPhilippineTime();

                var auditMsg = record.JobOrderId.HasValue
                    ? $"Accepted service request #{record.DispatchNumber} (Job Order #{record.JobOrderId})"
                    : $"Accepted service request #{record.DispatchNumber}";

                var audit = new AuditTrail(
                    await GetUserNameAsync() ?? throw new InvalidOperationException(),
                    auditMsg,
                    "Service Request"
                );
                await unitOfWork.AuditTrail.AddAsync(audit, cancellationToken);
                await unitOfWork.SaveAsync(cancellationToken);

                TempData["success"] = $"Service Request #{record.DispatchNumber} has been accepted.";
            }

            if (jobOrderId.HasValue)
            {
                return RedirectToAction("Details", "JobOrder", new { id = jobOrderId.Value });
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequireAccess(ProcedureEnum.CreateServiceRequest, "Access denied. You don't have permission to delete Service Requests.")]
        public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
        {
            try
            {
                var model = await unitOfWork.DispatchTicket.GetAsync(dt => dt.DispatchTicketId == id, cancellationToken);
                if (model == null)
                {
                    return Json(new { success = false, message = "Service request not found." });
                }

                if (model.Status != SD.ServiceRequestStatus.Draft &&
                    model.Status != SD.ServiceRequestStatus.Requested)
                {
                    return Json(new { success = false, message = $"Cannot delete — status is '{model.Status}'. Only Draft and Requested can be deleted." });
                }

                model.Status = SD.ServiceRequestStatus.ServiceRequestDeleted;
                model.EditedBy = User.Identity?.Name ?? "System";
                model.EditedDate = DateTimeHelper.GetCurrentPhilippineTime();

                await unitOfWork.AuditTrail.AddAsync(
                    new AuditTrail(model.EditedBy, $"Deleted service request #{model.DispatchNumber}", "Service Request", model.DispatchTicketId, model.DispatchNumber),
                    cancellationToken);

                await unitOfWork.SaveAsync(cancellationToken);
                return Json(new { success = true, message = $"Service request #{model.DispatchNumber} deleted." });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to delete service request");
                return Json(new { success = false, message = $"Failed to delete: {ex.Message}" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequireAccess(ProcedureEnum.CreateServiceRequest, "Access denied. You don't have permission to restore Service Requests.")]
        public async Task<IActionResult> Restore(int id, CancellationToken cancellationToken)
        {
            try
            {
                var model = await unitOfWork.DispatchTicket.GetAsync(dt => dt.DispatchTicketId == id, cancellationToken);
                if (model == null)
                {
                    return Json(new { success = false, message = "Service request not found." });
                }

                if (model.Status != SD.ServiceRequestStatus.ServiceRequestDeleted)
                {
                    return Json(new { success = false, message = $"Cannot restore — status is '{model.Status}'. Only deleted requests can be restored." });
                }

                model.Status = IsReadyToPost(model) ? SD.ServiceRequestStatus.Requested : SD.ServiceRequestStatus.Draft;
                model.EditedBy = User.Identity?.Name ?? "System";
                model.EditedDate = DateTimeHelper.GetCurrentPhilippineTime();

                await unitOfWork.AuditTrail.AddAsync(
                    new AuditTrail(model.EditedBy, $"Restored service request #{model.DispatchNumber}", "Service Request", model.DispatchTicketId, model.DispatchNumber),
                    cancellationToken);

                await unitOfWork.SaveAsync(cancellationToken);
                return Json(new { success = true, message = $"Service request #{model.DispatchNumber} restored." });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to restore service request");
                return Json(new { success = false, message = $"Failed to restore: {ex.Message}" });
            }
        }

        private static bool IsReadyToPost(DispatchTicket model) =>
            model is { JobOrderId: > 0, CustomerId: > 0, TerminalId: > 0, ServiceId: > 0, TugBoatId: > 0, TugMasterId: > 0, VesselId: > 0,
                DateLeft: not null, TimeLeft: not null, DateArrived: not null, TimeArrived: not null }
            && !string.IsNullOrWhiteSpace(model.ImageName)
            && model.DateArrived.Value.ToDateTime(model.TimeArrived.Value) > model.DateLeft.Value.ToDateTime(model.TimeLeft.Value);

        private async Task<string?> GetWorkflowErrorAsync(int? jobOrderId, DateOnly date, CancellationToken cancellationToken)
        {
            if (!jobOrderId.HasValue || await unitOfWork.JobOrder.GetAsync(j => j.JobOrderId == jobOrderId && j.Status == SD.JobOrderStatus.Open, cancellationToken) == null)
                return "Select an open Job Order.";

            if (await unitOfWork.Billing.GetAsync(b => b.JobOrderId == jobOrderId && b.Status == SD.BillingStatus.ForPosting, cancellationToken) != null
                || await unitOfWork.DispatchTicket.GetAsync(t => t.JobOrderId == jobOrderId && t.Status == SD.DispatchTicketStatus.Billed, cancellationToken) != null)
                return "Cannot modify a Service Request under a Job Order with pending billing or billed tickets.";

            if (await unitOfWork.PostedPeriod.IsMonthClosedAsync(date.Year, date.Month, cancellationToken))
                return $"Cannot modify: {date:MMMM yyyy} is closed.";

            return null;
        }

        private string GenerateFileNameToSave(string incomingFileName, string type)
        {
            var fileName = Path.GetFileNameWithoutExtension(incomingFileName);
            var extension = Path.GetExtension(incomingFileName);
            return $"{fileName}-{type}-{DateTimeHelper.GetCurrentPhilippineTime():yyyyMMddHHmmss}{extension}";
        }

        private async Task<string?> GetUserNameAsync()
        {
            var user = await userManager.GetUserAsync(User);
            return user?.UserName;
        }

        private async Task<string> GenerateSignedUrl(string uploadName)
        {
            if (!string.IsNullOrWhiteSpace(uploadName))
            {
                return await cloudStorageService.GetSignedUrlAsync(uploadName);
            }
            throw new ArgumentException("Upload name is null or empty.", nameof(uploadName));
        }
    }
}


