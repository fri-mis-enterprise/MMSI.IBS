using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using IBS.Models.Enums;
using IBS.Models.MMSI;
using IBS.Models.MMSI.MasterFile;
using IBS.Models.MMSI.ViewModels;
using IBS.Services;
using IBS.Services.Attributes;
using IBS.Utility.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Linq.Dynamic.Core;

namespace IBSWeb.Areas.User.Controllers
{
    [Area("User")]
    public class DispatchTicketController(
        ApplicationDbContext dbContext,
        IUnitOfWork unitOfWork,
        ICloudStorageService cloudStorageService,
        ILogger<DispatchTicketController> logger)
        : Controller
    {
        // ── Status constants ────────────────────────────────────────────────────
        private const string _statusPending     = "Pending";
        private const string _statusForTariff   = "For Tariff";
        private const string _statusForApproval = "For Approval";
        private const string _statusForBilling  = "For Billing";
        private const string _statusDisapproved = "Disapproved";
        private const string _statusCancelled   = "Cancelled";
        private const string _statusForPosting  = "For Posting";


        // ════════════════════════════════════════════════════════════════════════
        // INDEX
        // ════════════════════════════════════════════════════════════════════════

        [RequireAnyAccess(
            "Access denied. You don't have permission to access Dispatch Tickets.",
            "DispatchTicket",
            "Index",
            "User",
            ProcedureEnum.CreateDispatchTicket,
            ProcedureEnum.EditDispatchTicket,
            ProcedureEnum.CancelDispatchTicket)]
        public Task<IActionResult> Index(string filterType)
        {
            ViewBag.FilterType = filterType;
            return Task.FromResult<IActionResult>(View(Enumerable.Empty<DispatchTicket>()));
        }

        // ════════════════════════════════════════════════════════════════════════
        // CREATE
        // ════════════════════════════════════════════════════════════════════════

        [HttpGet]
        [RequireAccess(ProcedureEnum.CreateDispatchTicket, "Access denied. You don't have permission to create Dispatch Tickets.", "JobOrder")]
        public async Task<IActionResult> Create(int? jobOrderId, CancellationToken cancellationToken = default)
        {
            var viewModel = new ServiceRequestViewModel();
            ViewData["PortId"] = 0;

            if (jobOrderId.HasValue)
            {
                viewModel = await PreFillFromJobOrderAsync(viewModel, jobOrderId.Value, cancellationToken);
                viewModel = await unitOfWork.ServiceRequest.GetDispatchTicketSelectLists(viewModel, cancellationToken);

                if (viewModel.TerminalId.HasValue)
                {
                    ViewData["PortId"] = viewModel.PortId;
                }
            }
            else
            {
                viewModel = await unitOfWork.ServiceRequest.GetDispatchTicketSelectLists(viewModel, cancellationToken);
            }

            viewModel.Customers = await unitOfWork.GetCustomerListAsyncById(cancellationToken);
            return View(viewModel);
        }

        [HttpPost]
        [RequireAccess(ProcedureEnum.CreateDispatchTicket, "Access denied. You don't have permission to create Dispatch Tickets.", "JobOrder")]
        public async Task<IActionResult> Create(
            ServiceRequestViewModel viewModel,
            IFormFile? imageFile,
            IFormFile? videoFile,
            CancellationToken cancellationToken = default)
        {
            if (viewModel.JobOrderId.HasValue && !await IsJobOrderEditableAsync(viewModel.JobOrderId, cancellationToken))
            {
                TempData["error"] = "Cannot add ticket — parent Job Order is cancelled or closed.";
                return RedirectToAction("Index", "JobOrder", new { area = "User" });
            }

            if (!ModelState.IsValid)
            {
                viewModel           = await unitOfWork.ServiceRequest.GetDispatchTicketSelectLists(viewModel, cancellationToken);
                viewModel.Customers = await unitOfWork.GetCustomerListAsyncById(cancellationToken);
                TempData["warning"] = "Can't create entry, please review your input.";
                ViewData["PortId"]  = viewModel.Terminal?.Port?.PortId ?? viewModel.PortId;
                return View(viewModel);
            }

            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            var model = ServiceRequestVmToDispatchTicket(viewModel);

            try
            {
                if (model.TerminalId.HasValue)
                {
                    model.Terminal = await unitOfWork.Terminal.GetAsync(t => t.TerminalId == model.TerminalId, cancellationToken);
                    if (model.Terminal != null)
                    {
                        model.Terminal.Port = await unitOfWork.Port.GetAsync(p => p.PortId == model.Terminal.PortId, cancellationToken);
                    }
                }

                model          = await unitOfWork.DispatchTicket.GetDispatchTicketLists(model, cancellationToken);
                model.Customer = await unitOfWork.Customer.GetAsync(c => c.CustomerId == model.CustomerId, cancellationToken);

                // FIX: extracted to shared static helper (was duplicated with EditTicket POST)
                if (!IsArrivalAfterDeparture(model))
                {
                    await transaction.RollbackAsync(cancellationToken);
                    viewModel           = await unitOfWork.ServiceRequest.GetDispatchTicketSelectLists(viewModel, cancellationToken);
                    viewModel.Customers = await unitOfWork.GetCustomerListAsyncById(cancellationToken);
                    TempData["warning"] = "Start Date/Time should be earlier than End Date/Time!";
                    ViewData["PortId"]  = model.Terminal?.Port?.PortId;
                    return View(viewModel);
                }

                model.CreatedBy   = User.Identity?.Name ?? "Unknown";
                model.CreatedDate = DateTimeHelper.GetCurrentPhilippineTime();

                if (imageFile is { Length: > 0 })
                {
                    model.ImageName     = GenerateFileNameToSave(imageFile.FileName, "img");
                    model.ImageSavedUrl = await cloudStorageService.UploadFileAsync(imageFile, model.ImageName!);
                }

                if (videoFile is { Length: > 0 })
                {
                    model.VideoName     = GenerateFileNameToSave(videoFile.FileName, "vid");
                    model.VideoSavedUrl = await cloudStorageService.UploadFileAsync(videoFile, model.VideoName!);
                }

                if (model is { DateLeft: not null, DateArrived: not null, TimeLeft: not null, TimeArrived: not null })
                {
                    model.Status     = _statusForTariff;
                    // FIX: apply the same 0.5m floor as EditTicket POST (was missing, could produce TotalHours = 0)
                    model.TotalHours = Math.Max(CalculateTotalHours(model), 0.5m);
                }
                else
                {
                    model.Status = _statusPending;
                }

                await unitOfWork.DispatchTicket.AddAsync(model, cancellationToken);
                await unitOfWork.AuditTrail.AddAsync(
                    BuildAudit(User.Identity?.Name ?? "", $"Create dispatch ticket #{model.DispatchNumber}", "Dispatch Ticket"),
                    cancellationToken);

                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = $"Dispatch Ticket #{model.DispatchNumber} was successfully created.";
                return RedirectToAction("Details", "JobOrder", new { id = viewModel.JobOrderId });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                logger.LogError(ex, "Failed to create dispatch ticket.");
                viewModel           = await unitOfWork.ServiceRequest.GetDispatchTicketSelectLists(viewModel, cancellationToken);
                viewModel.Customers = await unitOfWork.GetCustomerListAsyncById(cancellationToken);
                TempData["error"]   = ex.Message;
                ViewData["PortId"]  = model.Terminal?.Port?.PortId;
                return View(viewModel);
            }
        }

        // ════════════════════════════════════════════════════════════════════════
        // PREVIEW
        // ════════════════════════════════════════════════════════════════════════

        [RequireAnyAccess(
            "Access denied. You don't have permission to view Dispatch Tickets.",
            "DispatchTicket",
            "Index",
            "User",
            ProcedureEnum.CreateDispatchTicket,
            ProcedureEnum.EditDispatchTicket,
            ProcedureEnum.CancelDispatchTicket)]
        public async Task<IActionResult> Preview(int id, CancellationToken cancellationToken)
        {
            var model = await unitOfWork.DispatchTicket.GetAsync(dt => dt.DispatchTicketId == id, cancellationToken);
            if (model == null)
            {
                return NotFound();
            }

            await GenerateSignedUrl(model);
            return View(model);
        }

        // ════════════════════════════════════════════════════════════════════════
        // SET TARIFF
        // ════════════════════════════════════════════════════════════════════════

        [HttpGet]
        [RequireAccess(ProcedureEnum.SetTariff, "Access denied. You don't have permission to set Tariff.", "DispatchTicket")]
        public async Task<IActionResult> SetTariff(int id, string filterType, CancellationToken cancellationToken)
        {
            ViewBag.FilterType = filterType;
            var model = await dbContext.MMSIDispatchTickets
                .Include(dt => dt.Customer)
                .Include(dt => dt.TugMaster)
                .Include(dt => dt.Tugboat).ThenInclude(t => t!.TugboatOwner)
                .Include(dt => dt.Vessel)
                .Include(dt => dt.Terminal).ThenInclude(t => t!.Port)
                .FirstOrDefaultAsync(dt => dt.DispatchTicketId == id, cancellationToken);
            if (model == null)
            {
                return NotFound();
            }

            ViewBag.Customers = await unitOfWork.GetCustomerListAsyncById(cancellationToken);
            return View(model);
        }

        [HttpPost]
        [RequireAccess(ProcedureEnum.SetTariff, "Access denied. You don't have permission to set Tariff.", "DispatchTicket")]
        public async Task<IActionResult> SetTariff(
            [Bind("DispatchTicketId,JobOrderId,CustomerId,DispatchRate,DispatchDiscount,DispatchBillingAmount,DispatchNetRevenue,BAFRate,BAFDiscount,BAFBillingAmount,BAFNetRevenue,TotalBilling,TotalNetRevenue,ApOtherTugs")] DispatchTicket model,
            string chargeType, string chargeType2, string filterType, CancellationToken cancellationToken)
        {
            if (!await IsTicketJobOrderEditableAsync(model.DispatchTicketId, cancellationToken))
            {
                TempData["error"] = "Cannot set tariff — parent Job Order is cancelled or closed.";
                return RedirectToAction(nameof(SetTariff), new { id = model.DispatchTicketId });
            }

            return await SaveTariffAsync(
                model, chargeType, chargeType2, filterType,
                isEdit: false,
                cancellationToken: cancellationToken);
        }

        // ════════════════════════════════════════════════════════════════════════
        // EDIT TARIFF
        // ════════════════════════════════════════════════════════════════════════

        [HttpGet]
        [RequireAccess(ProcedureEnum.SetTariff, "Access denied. You don't have permission to edit Tariff.", "DispatchTicket")]
        public async Task<IActionResult> EditTariff(int id, string filterType, CancellationToken cancellationToken)
        {
            ViewBag.FilterType = filterType;
            var model = await dbContext.MMSIDispatchTickets
                .Include(dt => dt.Customer)
                .Include(dt => dt.TugMaster)
                .Include(dt => dt.Tugboat).ThenInclude(t => t!.TugboatOwner)
                .Include(dt => dt.Vessel)
                .Include(dt => dt.Terminal).ThenInclude(t => t!.Port)
                .FirstOrDefaultAsync(dt => dt.DispatchTicketId == id, cancellationToken);
            if (model == null)
            {
                return NotFound();
            }

            ViewBag.Customers = await unitOfWork.GetCustomerListAsyncById(cancellationToken);
            return View(model);
        }

        [HttpPost]
        [RequireAccess(ProcedureEnum.SetTariff, "Access denied. You don't have permission to edit Tariff.", "DispatchTicket")]
        public async Task<IActionResult> EditTariff(
            [Bind("DispatchTicketId,JobOrderId,CustomerId,DispatchRate,DispatchDiscount,DispatchBillingAmount,DispatchNetRevenue,BAFRate,BAFDiscount,BAFBillingAmount,BAFNetRevenue,TotalBilling,TotalNetRevenue,ApOtherTugs")] DispatchTicket model,
            string chargeType, string chargeType2, string filterType, CancellationToken cancellationToken)
        {
            if (!await IsTicketJobOrderEditableAsync(model.DispatchTicketId, cancellationToken))
            {
                TempData["error"] = "Cannot edit tariff — parent Job Order is cancelled or closed.";
                return RedirectToAction(nameof(EditTariff), new { id = model.DispatchTicketId });
            }

            return await SaveTariffAsync(
                model, chargeType, chargeType2, filterType,
                isEdit: true,
                cancellationToken: cancellationToken);
        }

        // ════════════════════════════════════════════════════════════════════════
        // EDIT TICKET
        // ════════════════════════════════════════════════════════════════════════

        [HttpGet]
        [RequireAccess(ProcedureEnum.EditDispatchTicket, "Access denied. You don't have permission to edit Dispatch Tickets.", "DispatchTicket")]
        public async Task<IActionResult> EditTicket(
            int id, int? jobOrderId, string filterType, CancellationToken cancellationToken = default)
        {
            ViewBag.FilterType = filterType;

            // FIX: load the ticket once and reuse it for the editability check,
            // avoiding the extra DB hit that IsTicketJobOrderEditableAsync would cause.
            var model = await unitOfWork.DispatchTicket.GetAsync(dt => dt.DispatchTicketId == id, cancellationToken);
            if (model == null)
            {
                return NotFound();
            }

            if (!await IsJobOrderEditableAsync(model.JobOrderId, cancellationToken))
            {
                TempData["error"] = "Cannot edit ticket — parent Job Order is cancelled or closed.";
                return RedirectToAction(nameof(Index), new { filterType });
            }

            var viewModel       = DispatchTicketModelToServiceRequestVm(model);
            viewModel           = await unitOfWork.ServiceRequest.GetDispatchTicketSelectLists(viewModel, cancellationToken);
            viewModel.Customers = await unitOfWork.GetCustomerListAsyncById(cancellationToken);

            if (!string.IsNullOrEmpty(model.ImageName))
            {
                viewModel.ImageSignedUrl = await GenerateSignedUrl(model.ImageName);
            }

            if (!string.IsNullOrEmpty(model.VideoName))
            {
                viewModel.VideoSignedUrl = await GenerateSignedUrl(model.VideoName);
            }

            viewModel.JobOrderId   = jobOrderId ?? model.JobOrderId;
            ViewData["PortId"]     = model.Terminal?.Port?.PortId;
            ViewData["JobOrderId"] = viewModel.JobOrderId;
            return View(viewModel);
        }

        [HttpPost]
        [RequireAccess(ProcedureEnum.EditDispatchTicket, "Access denied. You don't have permission to edit Dispatch Tickets.", "DispatchTicket")]
        public async Task<IActionResult> EditTicket(
            ServiceRequestViewModel viewModel,
            IFormFile? imageFile,
            IFormFile? videoFile,
            string filterType,
            CancellationToken cancellationToken = default)
        {
            if (!await IsTicketJobOrderEditableAsync(viewModel.DispatchTicketId!.Value, cancellationToken))
            {
                TempData["error"] = "Cannot edit ticket — parent Job Order is cancelled or closed.";
                return RedirectToAction("EditTicket", new { id = viewModel.DispatchTicketId, jobOrderId = viewModel.JobOrderId });
            }

            if (!ModelState.IsValid)
            {
                TempData["warning"] = "Can't apply edit, please review your input.";
                return RedirectToAction("EditTicket", new { id = viewModel.DispatchTicketId });
            }

            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            var model = ServiceRequestVmToDispatchTicket(viewModel);

            try
            {
                // FIX: extracted to shared static helper (was duplicated with Create POST)
                if (!IsArrivalAfterDeparture(model))
                {
                    await transaction.RollbackAsync(cancellationToken);
                    TempData["warning"] = "Date/Time Left cannot be later than Date/Time Arrived!";
                    return RedirectToAction("EditTicket", new { id = viewModel.DispatchTicketId, jobOrderId = viewModel.JobOrderId });
                }

                var currentModel = await unitOfWork.DispatchTicket
                    .GetAsync(dt => dt.DispatchTicketId == model.DispatchTicketId, cancellationToken);
                if (currentModel == null)
                {
                    return NotFound();
                }

                model.Tugboat  = await unitOfWork.Tugboat.GetAsync(t => t.TugboatId == model.TugBoatId, cancellationToken);
                model.Customer = await unitOfWork.Customer.GetAsync(t => t.CustomerId == model.CustomerId, cancellationToken);

                if (model is { DateLeft: not null, DateArrived: not null, TimeLeft: not null, TimeArrived: not null })
                {
                    // FIX: Math.Max consolidates the zero-check (no behavior change)
                    var totalHours           = Math.Max(CalculateTotalHours(model), 0.5m);
                    model.TotalHours         = totalHours;
                    currentModel.TotalHours  = totalHours;
                }

                if (imageFile != null)
                {
                    if (!string.IsNullOrEmpty(currentModel.ImageName))
                    {
                        await cloudStorageService.DeleteFileAsync(currentModel.ImageName);
                    }

                    model.ImageName     = GenerateFileNameToSave(imageFile.FileName, "img");
                    model.ImageSavedUrl = await cloudStorageService.UploadFileAsync(imageFile, model.ImageName!);
                }

                if (videoFile != null)
                {
                    if (!string.IsNullOrEmpty(currentModel.VideoName))
                    {
                        await cloudStorageService.DeleteFileAsync(currentModel.VideoName);
                    }

                    model.VideoName     = GenerateFileNameToSave(videoFile.FileName, "vid");
                    model.VideoSavedUrl = await cloudStorageService.UploadFileAsync(videoFile, model.VideoName!);
                }

                // --- Change tracking ---
                var changes = new List<string>();
                TrackChange(changes, nameof(model.Date),           currentModel.Date,           model.Date);
                TrackChange(changes, nameof(model.DispatchNumber), currentModel.DispatchNumber, model.DispatchNumber);
                TrackChange(changes, nameof(model.COSNumber),      currentModel.COSNumber,      model.COSNumber);
                TrackChange(changes, nameof(model.VoyageNumber),   currentModel.VoyageNumber,   model.VoyageNumber);
                TrackChange(changes, nameof(model.CustomerId),     currentModel.CustomerId,     model.CustomerId);
                TrackChange(changes, nameof(model.DateLeft),       currentModel.DateLeft,       model.DateLeft);
                TrackChange(changes, nameof(model.TimeLeft),       currentModel.TimeLeft,       model.TimeLeft);
                TrackChange(changes, nameof(model.DateArrived),    currentModel.DateArrived,    model.DateArrived);
                TrackChange(changes, nameof(model.TimeArrived),    currentModel.TimeArrived,    model.TimeArrived);
                TrackChange(changes, nameof(model.TotalHours),     currentModel.TotalHours,     model.TotalHours);
                TrackChange(changes, nameof(model.TerminalId),     currentModel.TerminalId,     model.TerminalId);
                TrackChange(changes, nameof(model.ServiceId),      currentModel.ServiceId,      model.ServiceId);
                TrackChange(changes, nameof(model.TugBoatId),      currentModel.TugBoatId,      model.TugBoatId);
                TrackChange(changes, nameof(model.TugMasterId),    currentModel.TugMasterId,    model.TugMasterId);
                TrackChange(changes, nameof(model.VesselId),       currentModel.VesselId,       model.VesselId);
                TrackChange(changes, nameof(model.Remarks),        currentModel.Remarks,        model.Remarks);

                if (imageFile != null && currentModel.ImageName != model.ImageName)
                {
                    changes.Add($"ImageName: '{currentModel.ImageName}' -> '{model.ImageName}'");
                }

                if (videoFile != null && currentModel.VideoName != model.VideoName)
                {
                    changes.Add($"VideoName: '{currentModel.VideoName}' -> '{model.VideoName}'");
                }

                if (currentModel.TugBoatId != model.TugBoatId && model.Tugboat!.IsCompanyOwned && currentModel.ApOtherTugs != 0)
                {
                    changes.Add($"ApOtherTugs: '{currentModel.ApOtherTugs}' -> '0'");
                    currentModel.ApOtherTugs = 0;
                }

                // --- Apply field updates ---
                currentModel.EditedBy       = User.Identity?.Name ?? "";
                currentModel.EditedDate     = DateTimeHelper.GetCurrentPhilippineTime();
                currentModel.Date           = model.Date;
                currentModel.DispatchNumber = model.DispatchNumber;
                currentModel.COSNumber      = model.COSNumber;
                currentModel.VoyageNumber   = model.VoyageNumber;
                currentModel.CustomerId     = model.CustomerId;
                currentModel.DateLeft       = model.DateLeft;
                currentModel.TimeLeft       = model.TimeLeft;
                currentModel.DateArrived    = model.DateArrived;
                currentModel.TimeArrived    = model.TimeArrived;
                currentModel.TerminalId     = model.TerminalId;
                currentModel.ServiceId      = model.ServiceId;
                currentModel.TugBoatId      = model.TugBoatId;
                currentModel.TugMasterId    = model.TugMasterId;
                currentModel.VesselId       = model.VesselId;
                currentModel.Remarks        = model.Remarks;
                currentModel.JobOrderId     = model.JobOrderId;

                // --- Reset tariff state ---
                currentModel.Status                = _statusForTariff;
                currentModel.DispatchRate          = 0;
                currentModel.DispatchBillingAmount = 0;
                currentModel.DispatchDiscount      = 0;
                currentModel.DispatchNetRevenue    = 0;
                currentModel.BAFRate               = 0;
                currentModel.BAFBillingAmount      = 0;
                currentModel.BAFDiscount           = 0;
                currentModel.BAFNetRevenue         = 0;
                currentModel.TotalBilling          = 0;
                currentModel.TotalNetRevenue       = 0;
                currentModel.ApOtherTugs           = 0;
                currentModel.TariffBy              = string.Empty;
                currentModel.TariffDate            = default;
                currentModel.TariffEditedBy        = string.Empty;
                currentModel.TariffEditedDate      = null;

                if (imageFile != null)
                {
                    currentModel.ImageName      = model.ImageName;
                    currentModel.ImageSignedUrl = model.ImageSignedUrl;
                    currentModel.ImageSavedUrl  = model.ImageSavedUrl;
                }
                if (videoFile != null)
                {
                    currentModel.VideoName      = model.VideoName;
                    currentModel.VideoSignedUrl = model.VideoSignedUrl;
                    currentModel.VideoSavedUrl  = model.VideoSavedUrl;
                }

                await unitOfWork.SaveAsync(cancellationToken);
                await unitOfWork.AuditTrail.AddAsync(
                    BuildAudit(
                        User.Identity?.Name ?? "",
                        changes.Any()
                            ? $"Edit dispatch ticket #{currentModel.DispatchNumber}, {string.Join(", ", changes)}"
                            : $"No changes detected for #{currentModel.DispatchNumber}",
                        "Dispatch Ticket"),
                    cancellationToken);

                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = "Entry edited successfully!";

                return currentModel.JobOrderId.HasValue
                    ? RedirectToAction("Details", "JobOrder", new { id = currentModel.JobOrderId.Value })
                    : RedirectToAction(nameof(Index), new { filterType });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                logger.LogError(ex, "Failed to edit ticket.");
                TempData["error"] = ex.Message;
                return RedirectToAction("EditTicket", new { id = viewModel.DispatchTicketId, jobOrderId = viewModel.JobOrderId });
            }
        }

        // ════════════════════════════════════════════════════════════════════════
        // STATUS-CHANGE ACTIONS  (Approve / RevokeApproval / Disapprove / Cancel)
        // ════════════════════════════════════════════════════════════════════════

        [HttpGet]
        [RequireAccess(ProcedureEnum.ApproveTariff, "Access denied. You don't have permission to approve Tariff.", "DispatchTicket")]
        public Task<IActionResult> Approve(int id, CancellationToken cancellationToken) =>
            ChangeTicketStatusAsync(id, _statusForBilling,  "Approve tariff",        "Tariff",           "Entry Approved!",               cancellationToken);

        [HttpGet]
        [RequireAccess(ProcedureEnum.ApproveTariff, "Access denied. You don't have permission to approve Tariff.", "DispatchTicket")]
        public Task<IActionResult> RevokeApproval(int id, CancellationToken cancellationToken) =>
            ChangeTicketStatusAsync(id, _statusForApproval, "Revoke Approval",       "Tariff",           "Approval revoked successfully!", cancellationToken);

        [HttpGet]
        [RequireAccess(ProcedureEnum.ApproveTariff, "Access denied. You don't have permission to approve Tariff.", "DispatchTicket")]
        public Task<IActionResult> Disapprove(int id, CancellationToken cancellationToken) =>
            ChangeTicketStatusAsync(id, _statusDisapproved, "Disapprove Tariff",     "Tariff",           "Entry Disapproved!",             cancellationToken);

        [RequireAccess(ProcedureEnum.CancelDispatchTicket, "Access denied. You don't have permission to cancel Dispatch Tickets.", "DispatchTicket")]
        public Task<IActionResult> Cancel(int id, CancellationToken cancellationToken) =>
            ChangeTicketStatusAsync(id, _statusCancelled,   "Cancel dispatch ticket","Dispatch Ticket",  "Dispatch ticket cancelled.",     cancellationToken);

        // ════════════════════════════════════════════════════════════════════════
        // CHANGE TERMINAL  (JSON endpoint for cascading dropdown)
        // ════════════════════════════════════════════════════════════════════════

        [HttpGet]
        public async Task<IActionResult> ChangeTerminal(int portId, CancellationToken cancellationToken)
        {
            var terminals = await unitOfWork.Terminal.GetAllAsync(t => t.PortId == portId, cancellationToken);
            var list      = terminals.Select(t => new SelectListItem
            {
                Value = t.TerminalId.ToString(),
                Text  = t.TerminalName
            }).ToList();

            return Json(list);
        }

        // ════════════════════════════════════════════════════════════════════════
        // GET DISPATCH TICKET LIST  (simple JSON — not DataTables)
        // ════════════════════════════════════════════════════════════════════════

        [HttpGet]
        public async Task<IActionResult> GetDispatchTicketList(string status, CancellationToken cancellationToken)
        {
            var items = status == "All"
                ? await unitOfWork.DispatchTicket.GetAllAsync(
                    dt => dt.Status != _statusCancelled && dt.Status != _statusForPosting, cancellationToken)
                : await unitOfWork.DispatchTicket.GetAllAsync(
                    dt => dt.Status == status, cancellationToken);

            return Json(items);
        }

        // ════════════════════════════════════════════════════════════════════════
        // GET DISPATCH TICKET LISTS  (DataTables POST)
        // ════════════════════════════════════════════════════════════════════════

        [HttpPost]
        public async Task<IActionResult> GetDispatchTicketLists(
            [FromForm] DataTablesParameters parameters,
            string filterType,
            CancellationToken cancellationToken)
        {
            try
            {
                var queried = dbContext.MMSIDispatchTickets
                    .Include(dt => dt.Service)
                    .Include(dt => dt.Terminal).ThenInclude(dt => dt!.Port)
                    .Include(dt => dt.Tugboat)
                    .Include(dt => dt.TugMaster)
                    .Include(dt => dt.Vessel)
                    .Include(dt => dt.Customer)
                    .Where(dt =>
                        dt.Status != _statusForPosting &&
                        dt.Status != _statusCancelled  &&
                        dt.Status != "Incomplete");

                if (!string.IsNullOrEmpty(filterType))
                {
                    queried = filterType.ToLower() switch
                    {
                        "for tariff"   => queried.Where(dt => dt.Status == _statusForTariff),
                        "for approval" => queried.Where(dt => dt.Status == _statusForApproval),
                        "disapproved"  => queried.Where(dt => dt.Status == _statusDisapproved),
                        "for billing"  => queried.Where(dt => dt.Status == _statusForBilling),
                        "billed"       => queried.Where(dt => dt.Status == "Billed"),
                        _              => queried
                    };
                }

                if (!string.IsNullOrEmpty(parameters.Search.Value))
                {
                    var s = parameters.Search.Value.ToLower();
                    queried = queried.Where(dt =>
                        (dt.Date.HasValue && (
                            dt.Date.Value.Day.ToString().Contains(s)   ||
                            dt.Date.Value.Month.ToString().Contains(s) ||
                            dt.Date.Value.Year.ToString().Contains(s))) ||
                        (dt.COSNumber != null && dt.COSNumber.ToLower().Contains(s)) ||
                        (dt.DispatchNumber.ToLower().Contains(s)) ||
                        (dt.DateLeft.HasValue && (
                            dt.DateLeft.Value.Day.ToString().Contains(s)   ||
                            dt.DateLeft.Value.Month.ToString().Contains(s) ||
                            dt.DateLeft.Value.Year.ToString().Contains(s))) ||
                        (dt.TimeLeft.HasValue && (
                            dt.TimeLeft.Value.Hour.ToString().Contains(s)   ||
                            dt.TimeLeft.Value.Minute.ToString().Contains(s))) ||
                        (dt.DateArrived.HasValue && (
                            dt.DateArrived.Value.Day.ToString().Contains(s)   ||
                            dt.DateArrived.Value.Month.ToString().Contains(s) ||
                            dt.DateArrived.Value.Year.ToString().Contains(s))) ||
                        (dt.TimeArrived.HasValue && (
                            dt.TimeArrived.Value.Hour.ToString().Contains(s)   ||
                            dt.TimeArrived.Value.Minute.ToString().Contains(s))) ||
                        (dt.Service   != null && dt.Service.ServiceName.ToLower().Contains(s)) ||
                        (dt.Terminal  != null && dt.Terminal.Port != null && dt.Terminal.Port.PortName!.ToLower().Contains(s)) ||
                        (dt.Terminal  != null && dt.Terminal.TerminalName!.ToLower().Contains(s)) ||
                        (dt.Tugboat   != null && dt.Tugboat.TugboatName.ToLower().Contains(s)) ||
                        (dt.Customer  != null && dt.Customer.CustomerName.ToLower().Contains(s)) ||
                        (dt.Vessel    != null && dt.Vessel.VesselName.ToLower().Contains(s)) ||
                        (dt.Status.ToLower().Contains(s)));
                }

                foreach (var column in parameters.Columns)
                {
                    if (string.IsNullOrEmpty(column.Search.Value))
                    {
                        continue;
                    }

                    var s = column.Search.Value.ToLower();
                    if (column.Data == "status")
                    {
                        queried = s switch
                        {
                            "for tariff"   => queried.Where(dt => dt.Status == _statusForTariff),
                            "for approval" => queried.Where(dt => dt.Status == _statusForApproval),
                            "disapproved"  => queried.Where(dt => dt.Status == _statusDisapproved),
                            "for billing"  => queried.Where(dt => dt.Status == _statusForBilling),
                            "billed"       => queried.Where(dt => dt.Status == "Billed"),
                            _              => queried
                        };
                    }
                }

                if (parameters.Order?.Count > 0)
                {
                    var col = parameters.Columns[parameters.Order[0].Column].Data;
                    var dir = parameters.Order[0].Dir.ToLower() == "asc" ? "ascending" : "descending";
                    queried = queried.AsQueryable().OrderBy($"{col} {dir}");
                }

                var recordsFiltered = await queried.CountAsync(cancellationToken);
                var totalRecords    = await dbContext.MMSIDispatchTickets
                    .CountAsync(dt =>
                        dt.Status != _statusForPosting &&
                        dt.Status != _statusCancelled  &&
                        dt.Status != "Incomplete",
                        cancellationToken);

                var pagedData = await queried
                    .Skip(parameters.Start)
                    .Take(parameters.Length)
                    .ToListAsync(cancellationToken);

                foreach (var dt in pagedData.Where(dt => !string.IsNullOrEmpty(dt.ImageName)))
                {
                    dt.ImageSignedUrl = await GenerateSignedUrl(dt.ImageName!);
                }

                foreach (var dt in pagedData.Where(dt => !string.IsNullOrEmpty(dt.VideoName)))
                {
                    dt.VideoSignedUrl = await GenerateSignedUrl(dt.VideoName!);
                }

                return Json(new
                {
                    draw            = parameters.Draw,
                    recordsTotal    = totalRecords,
                    recordsFiltered,
                    data            = pagedData
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to get dispatch tickets.");
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        // ════════════════════════════════════════════════════════════════════════
        // DELETE IMAGE
        // ════════════════════════════════════════════════════════════════════════

        // FIX: accept filterType so the user lands back on the correct filtered view
        public async Task<IActionResult> DeleteImage(int id, string filterType, CancellationToken cancellationToken)
        {
            var model = await unitOfWork.DispatchTicket.GetAsync(dt => dt.DispatchTicketId == id, cancellationToken);
            if (model == null)
            {
                return NotFound();
            }

            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                if (!string.IsNullOrEmpty(model.ImageName))
                {
                    await cloudStorageService.DeleteFileAsync(model.ImageName);
                }

                model.ImageName = null;
                await unitOfWork.SaveAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = "Image Deleted Successfully!";
                return RedirectToAction(nameof(Index), new { filterType });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                logger.LogError(ex, "Failed to delete image.");
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Index), new { filterType });
            }
        }

        // ════════════════════════════════════════════════════════════════════════
        // CHECK FOR TARIFF RATE
        // ════════════════════════════════════════════════════════════════════════

        public async Task<IActionResult> CheckForTariffRate(
            int customerId, int dispatchTicketId, CancellationToken cancellationToken)
        {
            var dispatchModel = await unitOfWork.DispatchTicket
                .GetAsync(dt => dt.DispatchTicketId == dispatchTicketId, cancellationToken);
            if (dispatchModel == null)
            {
                return NotFound();
            }

            var tariffRate =
                await unitOfWork.TariffTable.GetAsync(t =>
                        t.CustomerId == customerId &&
                        t.TerminalId == dispatchModel.TerminalId &&
                        t.ServiceId  == dispatchModel.ServiceId &&
                        t.AsOfDate   <= dispatchModel.DateLeft,
                    cancellationToken)
                ??
                await unitOfWork.TariffTable.GetAsync(t =>
                        t.CustomerId == customerId &&
                        t.TerminalId == dispatchModel.TerminalId &&
                        t.AsOfDate   <= dispatchModel.DateLeft,
                    cancellationToken)
                ??
                await unitOfWork.TariffTable.GetAsync(t =>
                        t.CustomerId == customerId &&
                        t.AsOfDate   <= dispatchModel.DateLeft,
                    cancellationToken);

            if (tariffRate != null)
            {
                return Json(new
                {
                    tariffRate.Dispatch,
                    tariffRate.BAF,
                    tariffRate.DispatchDiscount,
                    tariffRate.BAFDiscount,
                    Exists = true
                });
            }

            return Json(new { Exists = false });
        }

        // ════════════════════════════════════════════════════════════════════════
        // APPROVE/DISAPPROVE TARIFF (POST actions for modal)
        // FIX: shared the same transaction/audit scaffold. Unified via
        //      ChangeTicketStatusJsonAsync.
        // ════════════════════════════════════════════════════════════════════════

        [HttpPost]
        [RequireAccess(ProcedureEnum.ApproveTariff, "Access denied. You don't have permission to approve Tariff.", "DispatchTicket")]
        public async Task<IActionResult> ApproveTariff(int id, CancellationToken cancellationToken)
        {
            if (!await IsTicketJobOrderEditableAsync(id, cancellationToken))
            {
                return Json(new { success = false, message = "Cannot approve tariff — parent Job Order is cancelled or closed." });
            }

            return await ChangeTicketStatusJsonAsync(
                id,
                newStatus:     _statusForBilling,
                auditActivity: m => $"Approved tariff for dispatch ticket #{m.DispatchNumber}",
                cancellationToken: cancellationToken);
        }

        [HttpPost]
        [RequireAccess(ProcedureEnum.ApproveTariff, "Access denied. You don't have permission to approve Tariff.", "DispatchTicket")]
        public async Task<IActionResult> DisapproveTariff(int id, string reason, CancellationToken cancellationToken)
        {
            if (!await IsTicketJobOrderEditableAsync(id, cancellationToken))
            {
                return Json(new { success = false, message = "Cannot disapprove tariff — parent Job Order is cancelled or closed." });
            }

            if (string.IsNullOrWhiteSpace(reason) || reason.Length < 10)
            {
                return Json(new { success = false, message = "Please provide a detailed reason (at least 10 characters)" });
            }

            return await ChangeTicketStatusJsonAsync(
                id,
                newStatus: _statusDisapproved,
                auditActivity: m => $"Disapproved tariff for dispatch ticket #{m.DispatchNumber}. Reason: {reason}",
                cancellationToken: cancellationToken,
                beforeSave: m =>
                {
                    m.Remarks = string.IsNullOrEmpty(m.Remarks)
                        ? $"Disapproved: {reason}"
                        : $"{m.Remarks} | Disapproved: {reason}";
                });
        }

        // ════════════════════════════════════════════════════════════════════════
        // PRIVATE HELPERS
        // ════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Shared scaffold for Approve / RevokeApproval / Disapprove / Cancel.
        /// </summary>
        private async Task<IActionResult> ChangeTicketStatusAsync(
            int id,
            string newStatus,
            string activityPrefix,
            string documentType,
            string successMessage,
            CancellationToken cancellationToken)
        {
            if (!await IsTicketJobOrderEditableAsync(id, cancellationToken))
            {
                TempData["error"] = "Cannot change ticket status — parent Job Order is cancelled or closed.";
                return RedirectToAction(nameof(Index));
            }

            var model = await unitOfWork.DispatchTicket.GetAsync(dt => dt.DispatchTicketId == id, cancellationToken);
            if (model == null)
            {
                TempData["error"] = "Can't find entry, please try again.";
                return RedirectToAction(nameof(Index));
            }

            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                model.Status = newStatus;
                await unitOfWork.SaveAsync(cancellationToken);
                await unitOfWork.AuditTrail.AddAsync(
                    BuildAudit(User.Identity?.Name ?? "", $"{activityPrefix} #{model.DispatchTicketId}", documentType),
                    cancellationToken);

                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = successMessage;

                return model.JobOrderId.HasValue
                    ? RedirectToAction("Details", "JobOrder", new { id = model.JobOrderId.Value })
                    : RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                logger.LogError(ex, "Failed to change ticket status to {Status}.", newStatus);
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// Shared scaffold for ApproveTariff / DisapproveTariff (JSON modal endpoints).
        /// </summary>
        private async Task<IActionResult> ChangeTicketStatusJsonAsync(
            int id,
            string newStatus,
            Func<DispatchTicket, string> auditActivity,
            CancellationToken cancellationToken,
            Action<DispatchTicket>? beforeSave = null)
        {
            var model = await unitOfWork.DispatchTicket.GetAsync(dt => dt.DispatchTicketId == id, cancellationToken);
            if (model == null)
            {
                return Json(new { success = false, message = "Ticket not found" });
            }

            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                model.Status = newStatus;
                beforeSave?.Invoke(model);

                await unitOfWork.SaveAsync(cancellationToken);
                await unitOfWork.AuditTrail.AddAsync(
                    BuildAudit(User.Identity?.Name ?? "", auditActivity(model), "Dispatch Ticket"),
                    cancellationToken);

                await transaction.CommitAsync(cancellationToken);
                return Json(new { success = true, message = $"Status updated to {newStatus} successfully" });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                logger.LogError(ex, "Failed to change ticket status to {Status}.", newStatus);
                return Json(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Shared scaffold for SetTariff POST and EditTariff POST.
        /// isEdit=false → sets TariffBy; isEdit=true → sets TariffEditedBy/Date and tracks changes.
        /// </summary>
        private async Task<IActionResult> SaveTariffAsync(
            DispatchTicket model,
            string chargeType,
            string chargeType2,
            string filterType,
            bool isEdit,
            CancellationToken cancellationToken)
        {
            var actionName = isEdit ? nameof(EditTariff) : nameof(SetTariff);
            var currentModel = await unitOfWork.DispatchTicket
                .GetAsync(dt => dt.DispatchTicketId == model.DispatchTicketId, cancellationToken);

            if (currentModel == null)
            {
                return NotFound();
            }

            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                string auditMessage;

                if (isEdit)
                {
                    var changes = new List<string>();
                    TrackChange(changes, nameof(currentModel.CustomerId),            currentModel.CustomerId,            model.CustomerId);
                    TrackChange(changes, nameof(currentModel.DispatchChargeType),    currentModel.DispatchChargeType,    chargeType);
                    TrackChange(changes, nameof(currentModel.BAFChargeType),         currentModel.BAFChargeType,         chargeType2);
                    TrackChange(changes, nameof(currentModel.DispatchRate),          currentModel.DispatchRate,          model.DispatchRate);
                    TrackChange(changes, nameof(currentModel.BAFRate),               currentModel.BAFRate,               model.BAFRate);
                    TrackChange(changes, nameof(currentModel.DispatchDiscount),      currentModel.DispatchDiscount,      model.DispatchDiscount);
                    TrackChange(changes, nameof(currentModel.BAFDiscount),           currentModel.BAFDiscount,           model.BAFDiscount);
                    TrackChange(changes, nameof(currentModel.DispatchBillingAmount), currentModel.DispatchBillingAmount, model.DispatchBillingAmount);
                    TrackChange(changes, nameof(currentModel.BAFBillingAmount),      currentModel.BAFBillingAmount,      model.BAFBillingAmount);
                    TrackChange(changes, nameof(currentModel.DispatchNetRevenue),    currentModel.DispatchNetRevenue,    model.DispatchNetRevenue);
                    TrackChange(changes, nameof(currentModel.BAFNetRevenue),         currentModel.BAFNetRevenue,         model.BAFNetRevenue);
                    TrackChange(changes, nameof(currentModel.ApOtherTugs),           currentModel.ApOtherTugs,           model.ApOtherTugs);
                    TrackChange(changes, nameof(currentModel.TotalBilling),          currentModel.TotalBilling,          model.TotalBilling);
                    TrackChange(changes, nameof(currentModel.TotalNetRevenue),       currentModel.TotalNetRevenue,       model.TotalNetRevenue);
                    TrackChange(changes, nameof(currentModel.ServiceId),             currentModel.ServiceId,             model.ServiceId);

                    currentModel.TariffEditedBy   = User.Identity?.Name ?? "";
                    currentModel.TariffEditedDate = DateTimeHelper.GetCurrentPhilippineTime();

                    auditMessage = changes.Any()
                        ? $"Edit tariff #{currentModel.DispatchNumber} {string.Join(", ", changes)}"
                        : $"No changes detected for tariff details #{currentModel.DispatchNumber}";
                }
                else
                {
                    currentModel.TariffBy = User.Identity?.Name ?? "";
                    auditMessage          = $"Set Tariff #{currentModel.DispatchTicketId}";
                }

                currentModel.Status                = _statusForApproval;
                currentModel.DispatchChargeType    = chargeType;
                currentModel.BAFChargeType         = chargeType2;
                currentModel.DispatchRate          = model.DispatchRate;
                currentModel.BAFRate               = model.BAFRate;
                currentModel.DispatchDiscount      = model.DispatchDiscount;
                currentModel.BAFDiscount           = model.BAFDiscount;
                currentModel.DispatchBillingAmount = model.DispatchBillingAmount;
                currentModel.BAFBillingAmount      = model.BAFBillingAmount;
                currentModel.DispatchNetRevenue    = model.DispatchNetRevenue;
                currentModel.BAFNetRevenue         = model.BAFNetRevenue;
                currentModel.ApOtherTugs           = model.ApOtherTugs;
                currentModel.TotalBilling          = model.TotalBilling;
                currentModel.TotalNetRevenue       = model.TotalNetRevenue;

                await unitOfWork.SaveAsync(cancellationToken);
                await unitOfWork.AuditTrail.AddAsync(
                    BuildAudit(User.Identity?.Name ?? "", auditMessage, "Tariff"),
                    cancellationToken);

                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = isEdit ? "Tariff edited successfully!" : "Tariff entered successfully!";

                return currentModel.JobOrderId.HasValue
                    ? RedirectToAction("Details", "JobOrder", new { id = currentModel.JobOrderId.Value })
                    : RedirectToAction(nameof(Index), new { filterType });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                logger.LogError(ex, "Failed to {Action} tariff.", isEdit ? "edit" : "set");
                TempData["error"] = ex.Message;
                return RedirectToAction(actionName, new { id = model.DispatchTicketId });
            }
        }

        private async Task<ServiceRequestViewModel> PreFillFromJobOrderAsync(
            ServiceRequestViewModel viewModel, int jobOrderId, CancellationToken cancellationToken)
        {
            var jobOrder = await unitOfWork.JobOrder.GetAsync(j => j.JobOrderId == jobOrderId, cancellationToken);
            if (jobOrder == null)
            {
                return viewModel;
            }

            viewModel.JobOrderId   = jobOrderId;
            viewModel.CustomerId   = jobOrder.CustomerId;
            viewModel.VesselId     = jobOrder.VesselId;
            viewModel.PortId       = jobOrder.PortId;
            viewModel.TerminalId   = jobOrder.TerminalId;
            viewModel.COSNumber    = jobOrder.COSNumber;
            viewModel.VoyageNumber = jobOrder.VoyageNumber;
            viewModel.Date         = jobOrder.Date;

            if (jobOrder.TerminalId.HasValue)
            {
                viewModel.Terminal = new Terminal { PortId = jobOrder.PortId ?? 0 };
            }

            return viewModel;
        }

        // FIX: extracted from Create POST and EditTicket POST — was copy-pasted verbatim
        private static bool IsArrivalAfterDeparture(DispatchTicket model) =>
            model.DateLeft < model.DateArrived ||
            (model.DateLeft == model.DateArrived && model.TimeLeft < model.TimeArrived);

        private static decimal CalculateTotalHours(DispatchTicket model)
        {
            var dateTimeLeft    = model.DateLeft!.Value.ToDateTime(model.TimeLeft!.Value);
            var dateTimeArrived = model.DateArrived!.Value.ToDateTime(model.TimeArrived!.Value);
            var totalHours      = Math.Round((decimal)(dateTimeArrived - dateTimeLeft).TotalHours, 2);

            if (model.Customer?.CustomerName == "PHIL-CEB MARINE SERVICES INC.")
            {
                var whole      = Math.Truncate(totalHours);
                var fractional = totalHours - whole;

                totalHours = fractional >= 0.75m ? whole + 1.0m
                           : fractional >= 0.25m ? whole + 0.5m
                           : whole;
            }

            return totalHours;
        }

        // FIX: consolidated change-tracking from EditTicket and EditTariff into one generic helper
        private static void TrackChange<T>(List<string> changes, string field, T oldVal, T newVal)
        {
            if (!EqualityComparer<T>.Default.Equals(oldVal, newVal))
            {
                changes.Add($"{field}: {oldVal} -> {newVal}");
            }
        }

        private static AuditTrail BuildAudit(string username, string activity, string documentType) =>
            new AuditTrail(username, activity, documentType);

        private async Task GenerateSignedUrl(DispatchTicket model)
        {
            if (!string.IsNullOrWhiteSpace(model.ImageName))
            {
                model.ImageSignedUrl = await cloudStorageService.GetSignedUrlAsync(model.ImageName);
            }

            if (!string.IsNullOrWhiteSpace(model.VideoName))
            {
                model.VideoSignedUrl = await cloudStorageService.GetSignedUrlAsync(model.VideoName);
            }
        }

        private async Task<string> GenerateSignedUrl(string uploadName)
        {
            if (!string.IsNullOrWhiteSpace(uploadName))
            {
                return await cloudStorageService.GetSignedUrlAsync(uploadName);
            }

            throw new InvalidOperationException("Upload name is null or empty.");
        }

        private static string GenerateFileNameToSave(string incomingFileName, string type)
        {
            var name = Path.GetFileNameWithoutExtension(incomingFileName);
            var ext  = Path.GetExtension(incomingFileName);
            return $"{name}-{type}-{DateTimeHelper.GetCurrentPhilippineTime():yyyyMMddHHmmss}{ext}";
        }

        // ════════════════════════════════════════════════════════════════════════
        // JOB ORDER STATUS VALIDATION
        // ════════════════════════════════════════════════════════════════════════

        private async Task<bool> IsJobOrderEditableAsync(int? jobOrderId, CancellationToken cancellationToken)
        {
            if (jobOrderId == null)
            {
                return true;
            }

            var jobOrder = await unitOfWork.JobOrder.GetAsync(j => j.JobOrderId == jobOrderId.Value, cancellationToken);
            return jobOrder?.Status == JobOrderStatus.Open;
        }

        private async Task<bool> IsTicketJobOrderEditableAsync(int ticketId, CancellationToken cancellationToken)
        {
            var ticket = await unitOfWork.DispatchTicket.GetAsync(dt => dt.DispatchTicketId == ticketId, cancellationToken);
            if (ticket?.JobOrderId == null)
            {
                return true;
            }

            return await IsJobOrderEditableAsync(ticket.JobOrderId, cancellationToken);
        }

        // ════════════════════════════════════════════════════════════════════════
        // MAPPER METHODS
        // ════════════════════════════════════════════════════════════════════════

        private static DispatchTicket ServiceRequestVmToDispatchTicket(ServiceRequestViewModel vm) =>
            new()
            {
                DispatchTicketId   = vm.DispatchTicketId ?? 0,
                Date               = vm.Date,
                COSNumber          = vm.COSNumber,
                DispatchNumber     = vm.DispatchNumber,
                VoyageNumber       = vm.VoyageNumber,
                CustomerId         = vm.CustomerId,
                DateLeft           = vm.DateLeft,
                TimeLeft           = vm.TimeLeft,
                DateArrived        = vm.DateArrived,
                TimeArrived        = vm.TimeArrived,
                TerminalId         = vm.TerminalId,
                ServiceId          = vm.ServiceId,
                TugBoatId          = vm.TugBoatId,
                TugMasterId        = vm.TugMasterId,
                VesselId           = vm.VesselId,
                Remarks            = vm.Remarks,
                DispatchChargeType = string.Empty,
                BAFChargeType      = string.Empty,
                TariffBy           = string.Empty,
                TariffEditedBy     = string.Empty,
                JobOrderId         = vm.JobOrderId
            };

        private static ServiceRequestViewModel DispatchTicketModelToServiceRequestVm(DispatchTicket model) =>
            new()
            {
                Date             = model.Date,
                COSNumber        = model.COSNumber,
                DispatchNumber   = model.DispatchNumber,
                VoyageNumber     = model.VoyageNumber,
                CustomerId       = model.CustomerId,
                DateLeft         = model.DateLeft,
                TimeLeft         = model.TimeLeft,
                DateArrived      = model.DateArrived,
                TimeArrived      = model.TimeArrived,
                TerminalId       = model.TerminalId,
                ServiceId        = model.ServiceId,
                TugBoatId        = model.TugBoatId,
                TugMasterId      = model.TugMasterId,
                VesselId         = model.VesselId,
                Terminal         = model.Terminal,
                PortId           = model.Terminal?.PortId,
                Remarks          = model.Remarks,
                ImageName        = model.ImageName,
                ImageSignedUrl   = model.ImageSignedUrl,
                VideoName        = model.VideoName,
                VideoSignedUrl   = model.VideoSignedUrl,
                DispatchTicketId = model.DispatchTicketId,
                JobOrderId       = model.JobOrderId
            };
    }
}
