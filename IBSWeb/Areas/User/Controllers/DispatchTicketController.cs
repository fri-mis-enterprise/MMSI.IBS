using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using IBS.Models.Enums;
using IBS.Models.MMSI;
using IBS.Models.MMSI.MasterFile;
using IBS.Models.MMSI.ViewModels;
using IBS.Services;
using IBS.Utility.Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Linq.Dynamic.Core;
using System.Security.Claims;

namespace IBSWeb.Areas.User.Controllers
{
    [Area("User")]
    public class DispatchTicketController(
        ApplicationDbContext dbContext,
        IUnitOfWork unitOfWork,
        UserManager<ApplicationUser> userManager,
        ICloudStorageService cloudStorageService,
        ILogger<DispatchTicketController> logger,
        IUserAccessService userAccessService)
        : Controller
    {
        private const string FilterTypeClaimType = "DispatchTicket.FilterType";

        // ── Status constants ────────────────────────────────────────────────────
        private const string StatusPending     = "Pending";
        private const string StatusForTariff   = "For Tariff";
        private const string StatusForApproval = "For Approval";
        private const string StatusForBilling  = "For Billing";
        private const string StatusDisapproved = "Disapproved";
        private const string StatusCancelled   = "Cancelled";
        private const string StatusForPosting  = "For Posting";

        // ════════════════════════════════════════════════════════════════════════
        // INDEX
        // ════════════════════════════════════════════════════════════════════════

        public async Task<IActionResult> Index(string filterType, CancellationToken cancellationToken = default)
        {
            // FIX: was .Result — blocking call that can deadlock under load.
            if (!await HasDispatchTicketAccessAsync(cancellationToken))
            {
                TempData["error"] = "Access denied.";
                return RedirectToAction("Index", "Home", new { area = "User" });
            }

            var dispatchTickets = await unitOfWork.DispatchTicket
                .GetAllAsync(dt => dt.Status != StatusForPosting && dt.Status != StatusCancelled, cancellationToken);

            await UpdateFilterTypeClaim(filterType);
            ViewBag.FilterType = await GetCurrentFilterType();
            return View(dispatchTickets);
        }

        // ════════════════════════════════════════════════════════════════════════
        // GET DISPATCH TICKET PARTIAL (modal create/edit from Job Order page)
        // ════════════════════════════════════════════════════════════════════════

        [HttpGet]
        public async Task<IActionResult> GetDispatchTicketPartial(
            int? id, int? jobOrderId, CancellationToken cancellationToken = default)
        {
            var companyClaims = await GetCompanyClaimAsync();
            var viewModel = new ServiceRequestViewModel();

            if (id.HasValue && id > 0)
            {
                // ── Edit mode ──────────────────────────────────────────────────
                if (!await userAccessService.CheckAccess(
                        userManager.GetUserId(User)!, ProcedureEnum.EditDispatchTicket, cancellationToken))
                {
                    return Forbid();
                }

                var model = await unitOfWork.DispatchTicket
                    .GetAsync(dt => dt.DispatchTicketId == id, cancellationToken);
                if (model == null) return NotFound();

                if (model.TerminalId.HasValue)
                {
                    model.Terminal = await unitOfWork.Terminal
                        .GetAsync(t => t.TerminalId == model.TerminalId, cancellationToken);
                    if (model.Terminal != null)
                    {
                        model.Terminal.Port = await unitOfWork.Port
                            .GetAsync(p => p.PortId == model.Terminal.PortId, cancellationToken);
                    }
                }

                viewModel = DispatchTicketModelToServiceRequestVm(model);

                if (!string.IsNullOrEmpty(model.ImageName))
                    viewModel.ImageSignedUrl = await GenerateSignedUrl(model.ImageName);
                if (!string.IsNullOrEmpty(model.VideoName))
                    viewModel.VideoSignedUrl = await GenerateSignedUrl(model.VideoName);

                ViewData["Title"]      = "Edit Dispatch Ticket";
                ViewData["JobOrderId"] = viewModel.JobOrderId;
            }
            else
            {
                // ── Create mode ────────────────────────────────────────────────
                if (!await userAccessService.CheckAccess(
                        userManager.GetUserId(User)!, ProcedureEnum.CreateDispatchTicket, cancellationToken))
                {
                    return Forbid();
                }

                ViewData["Title"] = "Create Dispatch Ticket";

                if (jobOrderId.HasValue)
                {
                    viewModel = await PreFillFromJobOrderAsync(viewModel, jobOrderId.Value, cancellationToken);
                    ViewData["JobOrderId"] = jobOrderId;
                }
            }

            viewModel = await unitOfWork.ServiceRequest
                .GetDispatchTicketSelectLists(viewModel, cancellationToken);
            viewModel.Customers = await unitOfWork.GetCustomerListAsyncById(companyClaims!, cancellationToken);

            ViewData["PortId"] = viewModel.Terminal?.Port?.PortId > 0
                ? viewModel.Terminal.Port.PortId
                : (viewModel.PortId > 0 ? viewModel.PortId : 0);

            return PartialView("_CreateTicketPartial", viewModel);
        }

        // ════════════════════════════════════════════════════════════════════════
        // CREATE
        // ════════════════════════════════════════════════════════════════════════

        [HttpGet]
        public async Task<IActionResult> Create(int? jobOrderId, CancellationToken cancellationToken = default)
        {
            if (!await userAccessService.CheckAccess(
                    userManager.GetUserId(User)!, ProcedureEnum.CreateDispatchTicket, cancellationToken))
            {
                TempData["error"] = "Access denied.";
                return RedirectToAction(nameof(Index));
            }

            var companyClaims = await GetCompanyClaimAsync();
            var viewModel = new ServiceRequestViewModel();
            ViewData["PortId"] = 0;

            if (jobOrderId.HasValue)
            {
                viewModel = await PreFillFromJobOrderAsync(viewModel, jobOrderId.Value, cancellationToken);

                if (viewModel.TerminalId.HasValue)
                {
                    // Select lists must be re-populated after the terminal/port is known.
                    viewModel = await unitOfWork.ServiceRequest
                        .GetDispatchTicketSelectLists(viewModel, cancellationToken);
                    ViewData["PortId"] = viewModel.PortId;
                }
                else
                {
                    viewModel = await unitOfWork.ServiceRequest
                        .GetDispatchTicketSelectLists(viewModel, cancellationToken);
                }
            }
            else
            {
                // FIX: was calling GetDispatchTicketSelectLists twice when a terminal existed.
                // Now it is called exactly once per branch.
                viewModel = await unitOfWork.ServiceRequest
                    .GetDispatchTicketSelectLists(viewModel, cancellationToken);
            }

            viewModel.Customers = await unitOfWork.GetCustomerListAsyncById(companyClaims!, cancellationToken);
            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> Create(
            ServiceRequestViewModel viewModel,
            IFormFile? imageFile,
            IFormFile? videoFile,
            CancellationToken cancellationToken = default)
        {
            var companyClaims = await GetCompanyClaimAsync();

            if (!ModelState.IsValid)
            {
                viewModel = await unitOfWork.ServiceRequest
                    .GetDispatchTicketSelectLists(viewModel, cancellationToken);
                viewModel.Customers = await unitOfWork.GetCustomerListAsyncById(companyClaims!, cancellationToken);
                TempData["warning"] = "Can't create entry, please review your input.";
                ViewData["PortId"] = viewModel.Terminal?.Port?.PortId ?? viewModel.PortId;
                return View(viewModel);
            }

            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            var model = ServiceRequestVmToDispatchTicket(viewModel);

            try
            {
                if (model.TerminalId.HasValue)
                {
                    model.Terminal = await unitOfWork.Terminal
                        .GetAsync(t => t.TerminalId == model.TerminalId, cancellationToken);
                    if (model.Terminal != null)
                    {
                        model.Terminal.Port = await unitOfWork.Port
                            .GetAsync(p => p.PortId == model.Terminal.PortId, cancellationToken);
                    }
                }

                model = await unitOfWork.DispatchTicket.GetDispatchTicketLists(model, cancellationToken);
                model.Customer = await unitOfWork.Customer
                    .GetAsync(c => c.CustomerId == model.CustomerId, cancellationToken);

                // Date validation — must be a model-level concern but kept here
                // until a [ValidationAttribute] or FluentValidation rule is wired up.
                if (!(model.DateLeft < model.DateArrived ||
                     (model.DateLeft == model.DateArrived && model.TimeLeft < model.TimeArrived)))
                {
                    await transaction.RollbackAsync(cancellationToken);
                    viewModel = await unitOfWork.ServiceRequest
                        .GetDispatchTicketSelectLists(viewModel, cancellationToken);
                    viewModel.Customers = await unitOfWork.GetCustomerListAsyncById(companyClaims!, cancellationToken);
                    TempData["warning"] = "Start Date/Time should be earlier than End Date/Time!";
                    ViewData["PortId"] = model.Terminal?.Port?.PortId;
                    return View(viewModel);
                }

                // FIX: resolved user once; username is taken from user.UserName below.
                var user = await userManager.GetUserAsync(User)
                    ?? throw new InvalidOperationException("Current user could not be resolved.");

                model.CreatedBy   = user.UserName!;
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

                if (model.DateLeft != null && model.DateArrived != null &&
                    model.TimeLeft != null && model.TimeArrived != null)
                {
                    model.Status     = StatusForTariff;
                    model.TotalHours = CalculateTotalHours(model);
                }
                else
                {
                    // Incomplete dates — save as Pending so the record is not silently dropped.
                    model.Status = StatusPending;
                }

                await unitOfWork.DispatchTicket.AddAsync(model, cancellationToken);

                var audit = BuildAudit(
                    user.UserName!,
                    companyClaims!,
                    $"Create dispatch ticket #{model.DispatchNumber}",
                    "Dispatch Ticket");
                await unitOfWork.AuditTrail.AddAsync(audit, cancellationToken);

                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = $"Dispatch Ticket #{model.DispatchNumber} was successfully created.";

                return viewModel.JobOrderId.HasValue
                    ? RedirectToAction("Details", "JobOrder", new { id = viewModel.JobOrderId })
                    : RedirectToAction(nameof(Index), new { filterType = await GetCurrentFilterType() });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                logger.LogError(ex, "Failed to create dispatch ticket.");
                viewModel = await unitOfWork.ServiceRequest
                    .GetDispatchTicketSelectLists(viewModel, cancellationToken);
                viewModel.Customers = await unitOfWork.GetCustomerListAsyncById(companyClaims!, cancellationToken);
                TempData["error"]   = ex.Message;
                ViewData["PortId"]  = model.Terminal?.Port?.PortId;
                return View(viewModel);
            }
        }

        // ════════════════════════════════════════════════════════════════════════
        // PREVIEW
        // ════════════════════════════════════════════════════════════════════════

        public async Task<IActionResult> Preview(int id, CancellationToken cancellationToken)
        {
            var model = await unitOfWork.DispatchTicket
                .GetAsync(dt => dt.DispatchTicketId == id, cancellationToken);

            if (model == null) return NotFound();

            await GenerateSignedUrl(model);
            ViewBag.FilterType = await GetCurrentFilterType();
            return View(model);
        }

        // ════════════════════════════════════════════════════════════════════════
        // SET TARIFF
        // ════════════════════════════════════════════════════════════════════════

        [HttpGet]
        public async Task<IActionResult> SetTariff(int id, CancellationToken cancellationToken)
        {
            if (!await userAccessService.CheckAccess(
                    userManager.GetUserId(User)!, ProcedureEnum.SetTariff, cancellationToken))
            {
                TempData["error"] = "Access denied.";
                return RedirectToAction(nameof(Index));
            }

            var model = await unitOfWork.DispatchTicket
                .GetAsync(dt => dt.DispatchTicketId == id, cancellationToken);
            if (model == null) return NotFound();

            var viewModel     = DispatchTicketModelToTariffVm(model);
            var companyClaims = await GetCompanyClaimAsync();
            viewModel.Customers     = await unitOfWork.GetCustomerListAsyncById(companyClaims!, cancellationToken);
            ViewBag.FilterType      = await GetCurrentFilterType();
            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> SetTariff(
            TariffViewModel vm, string chargeType, string chargeType2, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                TempData["warning"] = "The submitted information is invalid.";
                return RedirectToAction(nameof(SetTariff), new { id = vm.DispatchTicketId });
            }

            // FIX: user is resolved once; UserName is used directly in the audit block below.
            var user = await userManager.GetUserAsync(User)
                ?? throw new InvalidOperationException("Current user could not be resolved.");

            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            var model = TariffVmToDispatchTicket(vm);

            try
            {
                var currentModel = await unitOfWork.DispatchTicket
                    .GetAsync(dt => dt.DispatchTicketId == model.DispatchTicketId, cancellationToken);
                if (currentModel == null) return NotFound();

                currentModel.Status                = StatusForApproval;
                currentModel.TariffBy              = user.UserName!;
                currentModel.DispatchChargeType    = chargeType;
                currentModel.DispatchRate          = model.DispatchRate;
                currentModel.DispatchDiscount      = model.DispatchDiscount;
                currentModel.BAFChargeType         = chargeType2;
                currentModel.BAFRate               = model.BAFRate;
                currentModel.BAFDiscount           = model.BAFDiscount;
                currentModel.DispatchBillingAmount = model.DispatchBillingAmount;
                currentModel.DispatchNetRevenue    = model.DispatchNetRevenue;
                currentModel.BAFBillingAmount      = model.BAFBillingAmount;
                currentModel.BAFNetRevenue         = model.BAFNetRevenue;
                currentModel.TotalBilling          = model.TotalBilling;
                currentModel.TotalNetRevenue       = model.TotalNetRevenue;
                currentModel.ApOtherTugs           = model.ApOtherTugs;

                await unitOfWork.SaveAsync(cancellationToken);

                // FIX: no second GetUserNameAsync / GetCompanyClaimAsync call needed.
                var companyClaims = await GetCompanyClaimAsync()
                    ?? throw new InvalidOperationException("Company claim missing.");
                var audit = BuildAudit(
                    user.UserName!,
                    companyClaims,
                    $"Set Tariff #{currentModel.DispatchTicketId}",
                    "Tariff");
                await unitOfWork.AuditTrail.AddAsync(audit, cancellationToken);

                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = "Tariff entered successfully!";

                return currentModel.JobOrderId.HasValue
                    ? RedirectToAction("Details", "JobOrder", new { id = currentModel.JobOrderId.Value })
                    : RedirectToAction(nameof(Index), new { filterType = await GetCurrentFilterType() });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                logger.LogError(ex, "Failed to set tariff.");
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(SetTariff), new { id = vm.DispatchTicketId });
            }
        }

        // ════════════════════════════════════════════════════════════════════════
        // EDIT TARIFF
        // ════════════════════════════════════════════════════════════════════════

        [HttpGet]
        public async Task<IActionResult> EditTariff(int id, CancellationToken cancellationToken)
        {
            if (!await userAccessService.CheckAccess(
                    userManager.GetUserId(User)!, ProcedureEnum.SetTariff, cancellationToken))
            {
                TempData["error"] = "Access denied.";
                return RedirectToAction(nameof(Index));
            }

            var model = await unitOfWork.DispatchTicket
                .GetAsync(dt => dt.DispatchTicketId == id, cancellationToken);
            if (model == null) return NotFound();

            var viewModel     = DispatchTicketModelToTariffVm(model);
            var companyClaims = await GetCompanyClaimAsync();
            viewModel.Customers = await unitOfWork.GetCustomerListAsyncById(companyClaims!, cancellationToken);
            ViewBag.FilterType  = await GetCurrentFilterType();
            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> EditTariff(
            TariffViewModel viewModel, string chargeType, string chargeType2, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                TempData["warning"] = "The submitted information is invalid.";
                return RedirectToAction(nameof(EditTariff), new { id = viewModel.DispatchTicketId });
            }

            // FIX: user resolved once.
            var user = await userManager.GetUserAsync(User)
                ?? throw new InvalidOperationException("Current user could not be resolved.");

            var model        = TariffVmToDispatchTicket(viewModel);
            var currentModel = await unitOfWork.DispatchTicket
                .GetAsync(dt => dt.DispatchTicketId == model.DispatchTicketId, cancellationToken);
            if (currentModel == null) return NotFound();

            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var changes = new List<string>();
                if (currentModel.CustomerId         != model.CustomerId)         changes.Add($"CustomerId: {currentModel.CustomerId} -> {model.CustomerId}");
                if (currentModel.DispatchChargeType != chargeType)               changes.Add($"DispatchChargeType: {currentModel.DispatchChargeType} -> {chargeType}");
                if (currentModel.BAFChargeType      != chargeType2)              changes.Add($"BAFChargeType: {currentModel.BAFChargeType} -> {chargeType2}");
                if (currentModel.DispatchRate       != model.DispatchRate)       changes.Add($"DispatchRate: {currentModel.DispatchRate} -> {model.DispatchRate}");
                if (currentModel.BAFRate            != model.BAFRate)            changes.Add($"BAFRate: {currentModel.BAFRate} -> {model.BAFRate}");
                if (currentModel.DispatchDiscount   != model.DispatchDiscount)   changes.Add($"DispatchDiscount: {currentModel.DispatchDiscount} -> {model.DispatchDiscount}");
                if (currentModel.BAFDiscount        != model.BAFDiscount)        changes.Add($"BAFDiscount: {currentModel.BAFDiscount} -> {model.BAFDiscount}");
                if (currentModel.DispatchBillingAmount != model.DispatchBillingAmount) changes.Add($"DispatchBillingAmount: {currentModel.DispatchBillingAmount} -> {model.DispatchBillingAmount}");
                if (currentModel.BAFBillingAmount   != model.BAFBillingAmount)   changes.Add($"BAFBillingAmount: {currentModel.BAFBillingAmount} -> {model.BAFBillingAmount}");
                if (currentModel.DispatchNetRevenue != model.DispatchNetRevenue) changes.Add($"DispatchNetRevenue: {currentModel.DispatchNetRevenue} -> {model.DispatchNetRevenue}");
                if (currentModel.BAFNetRevenue      != model.BAFNetRevenue)      changes.Add($"BAFNetRevenue: {currentModel.BAFNetRevenue} -> {model.BAFNetRevenue}");
                if (currentModel.ApOtherTugs        != model.ApOtherTugs)        changes.Add($"ApOtherTugs: {currentModel.ApOtherTugs} -> {model.ApOtherTugs}");
                if (currentModel.TotalBilling       != model.TotalBilling)       changes.Add($"TotalBilling: {currentModel.TotalBilling} -> {model.TotalBilling}");
                if (currentModel.TotalNetRevenue    != model.TotalNetRevenue)    changes.Add($"TotalNetRevenue: {currentModel.TotalNetRevenue} -> {model.TotalNetRevenue}");
                // FIX: was comparing Service navigation objects (always different instances → always true).
                // Compare IDs instead.
                if (currentModel.ServiceId != model.ServiceId)                   changes.Add($"ServiceId: {currentModel.ServiceId} -> {model.ServiceId}");

                currentModel.TariffEditedBy        = user.UserName!;
                currentModel.TariffEditedDate      = DateTimeHelper.GetCurrentPhilippineTime();
                currentModel.Status                = StatusForApproval;
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

                // FIX: no redundant identity lookups.
                var companyClaims = await GetCompanyClaimAsync()
                    ?? throw new InvalidOperationException("Company claim missing.");
                var audit = BuildAudit(
                    user.UserName!,
                    companyClaims,
                    changes.Any()
                        ? $"Edit tariff #{currentModel.DispatchNumber} {string.Join(", ", changes)}"
                        : $"No changes detected for tariff details #{currentModel.DispatchNumber}",
                    "Tariff");
                await unitOfWork.AuditTrail.AddAsync(audit, cancellationToken);

                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = "Tariff edited successfully!";

                return currentModel.JobOrderId.HasValue
                    ? RedirectToAction("Details", "JobOrder", new { id = currentModel.JobOrderId.Value })
                    : RedirectToAction(nameof(Index), new { filterType = await GetCurrentFilterType() });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                logger.LogError(ex, "Failed to edit tariff.");
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(EditTariff), new { id = viewModel.DispatchTicketId });
            }
        }

        // ════════════════════════════════════════════════════════════════════════
        // EDIT TICKET
        // ════════════════════════════════════════════════════════════════════════

        [HttpGet]
        public async Task<IActionResult> EditTicket(
            int id, int? jobOrderId, CancellationToken cancellationToken = default)
        {
            if (!await userAccessService.CheckAccess(
                    userManager.GetUserId(User)!, ProcedureEnum.EditDispatchTicket, cancellationToken))
            {
                TempData["error"] = "Access denied.";
                return RedirectToAction(nameof(Index));
            }

            var model = await unitOfWork.DispatchTicket
                .GetAsync(dt => dt.DispatchTicketId == id, cancellationToken);
            if (model == null) return NotFound();

            var companyClaims = await GetCompanyClaimAsync();
            var viewModel     = DispatchTicketModelToServiceRequestVm(model);
            viewModel = await unitOfWork.ServiceRequest
                .GetDispatchTicketSelectLists(viewModel, cancellationToken);
            viewModel.Customers = await unitOfWork.GetCustomerListAsyncById(companyClaims!, cancellationToken);

            if (!string.IsNullOrEmpty(model.ImageName))
                viewModel.ImageSignedUrl = await GenerateSignedUrl(model.ImageName);
            if (!string.IsNullOrEmpty(model.VideoName))
                viewModel.VideoSignedUrl = await GenerateSignedUrl(model.VideoName);

            viewModel.JobOrderId = jobOrderId ?? model.JobOrderId;

            ViewData["PortId"]     = model.Terminal?.Port?.PortId;
            ViewData["JobOrderId"] = viewModel.JobOrderId;
            ViewBag.FilterType     = await GetCurrentFilterType();
            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> EditTicket(
            ServiceRequestViewModel viewModel,
            IFormFile? imageFile,
            IFormFile? videoFile,
            CancellationToken cancellationToken = default)
        {
            if (!await userAccessService.CheckAccess(
                    userManager.GetUserId(User)!, ProcedureEnum.EditDispatchTicket, cancellationToken))
            {
                TempData["error"] = "Access denied.";
                return RedirectToAction(nameof(Index));
            }

            if (!ModelState.IsValid)
            {
                TempData["warning"] = "Can't apply edit, please review your input.";
                return RedirectToAction("EditTicket", new { id = viewModel.DispatchTicketId });
            }

            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            var model = ServiceRequestVmToDispatchTicket(viewModel);
            var user  = await userManager.GetUserAsync(User)
                ?? throw new InvalidOperationException("Current user could not be resolved.");

            try
            {
                if (!(model.DateLeft < model.DateArrived ||
                     (model.DateLeft == model.DateArrived && model.TimeLeft < model.TimeArrived)))
                {
                    await transaction.RollbackAsync(cancellationToken);
                    TempData["warning"] = "Date/Time Left cannot be later than Date/Time Arrived!";
                    return RedirectToAction("EditTicket",
                        new { id = viewModel.DispatchTicketId, jobOrderId = viewModel.JobOrderId });
                }

                var currentModel = await unitOfWork.DispatchTicket
                    .GetAsync(dt => dt.DispatchTicketId == model.DispatchTicketId, cancellationToken);
                if (currentModel == null) return NotFound();

                model.Tugboat  = await unitOfWork.Tugboat.GetAsync(t => t.TugboatId == model.TugBoatId, cancellationToken);
                model.Customer = await unitOfWork.Customer.GetAsync(t => t.CustomerId == model.CustomerId, cancellationToken);

                if (model.DateLeft != null && model.DateArrived != null &&
                    model.TimeLeft != null && model.TimeArrived != null)
                {
                    var totalHours = CalculateTotalHours(model);
                    if (totalHours == 0) totalHours = 0.5m;
                    model.TotalHours         = totalHours;
                    currentModel.TotalHours  = totalHours;
                }

                if (imageFile != null)
                {
                    if (!string.IsNullOrEmpty(currentModel.ImageName))
                        await cloudStorageService.DeleteFileAsync(currentModel.ImageName);
                    model.ImageName     = GenerateFileNameToSave(imageFile.FileName, "img");
                    model.ImageSavedUrl = await cloudStorageService.UploadFileAsync(imageFile, model.ImageName!);
                }

                if (videoFile != null)
                {
                    if (!string.IsNullOrEmpty(currentModel.VideoName))
                        await cloudStorageService.DeleteFileAsync(currentModel.VideoName);
                    model.VideoName     = GenerateFileNameToSave(videoFile.FileName, "vid");
                    model.VideoSavedUrl = await cloudStorageService.UploadFileAsync(videoFile, model.VideoName!);
                }

                var changes = new List<string>();
                if (currentModel.Date           != model.Date)           changes.Add($"Date: {currentModel.Date} -> {model.Date}");
                if (currentModel.DispatchNumber != model.DispatchNumber) changes.Add($"DispatchNumber: {currentModel.DispatchNumber} -> {model.DispatchNumber}");
                if (currentModel.COSNumber      != model.COSNumber)      changes.Add($"COSNumber: {currentModel.COSNumber} -> {model.COSNumber}");
                if (currentModel.VoyageNumber   != model.VoyageNumber)   changes.Add($"VoyageNumber: {currentModel.VoyageNumber} -> {model.VoyageNumber}");
                if (currentModel.CustomerId     != model.CustomerId)     changes.Add($"CustomerId: {currentModel.CustomerId} -> {model.CustomerId}");
                if (currentModel.DateLeft       != model.DateLeft)       changes.Add($"DateLeft: {currentModel.DateLeft} -> {model.DateLeft}");
                if (currentModel.TimeLeft       != model.TimeLeft)       changes.Add($"TimeLeft: {currentModel.TimeLeft} -> {model.TimeLeft}");
                if (currentModel.DateArrived    != model.DateArrived)    changes.Add($"DateArrived: {currentModel.DateArrived} -> {model.DateArrived}");
                if (currentModel.TimeArrived    != model.TimeArrived)    changes.Add($"TimeArrived: {currentModel.TimeArrived} -> {model.TimeArrived}");
                if (currentModel.TotalHours     != model.TotalHours)     changes.Add($"TotalHours: {currentModel.TotalHours} -> {model.TotalHours}");
                if (currentModel.TerminalId     != model.TerminalId)     changes.Add($"TerminalId: {currentModel.TerminalId} -> {model.TerminalId}");
                // FIX (parity with EditTariff): compare ServiceId, not Service navigation object.
                if (currentModel.ServiceId      != model.ServiceId)      changes.Add($"ServiceId: {currentModel.ServiceId} -> {model.ServiceId}");
                if (currentModel.TugBoatId      != model.TugBoatId)      changes.Add($"TugBoatId: {currentModel.TugBoatId} -> {model.TugBoatId}");
                if (currentModel.TugMasterId    != model.TugMasterId)    changes.Add($"TugMasterId: {currentModel.TugMasterId} -> {model.TugMasterId}");
                if (currentModel.VesselId       != model.VesselId)       changes.Add($"VesselId: {currentModel.VesselId} -> {model.VesselId}");
                if (currentModel.Remarks        != model.Remarks)        changes.Add($"Remarks: '{currentModel.Remarks}' -> '{model.Remarks}'");
                if (imageFile != null && currentModel.ImageName != model.ImageName) changes.Add($"ImageName: '{currentModel.ImageName}' -> '{model.ImageName}'");
                if (videoFile != null && currentModel.VideoName != model.VideoName) changes.Add($"VideoName: '{currentModel.VideoName}' -> '{model.VideoName}'");

                if (currentModel.TugBoatId != model.TugBoatId &&
                    model.Tugboat!.IsCompanyOwned && currentModel.ApOtherTugs != 0)
                {
                    changes.Add($"ApOtherTugs: '{currentModel.ApOtherTugs}' -> '0'");
                    currentModel.ApOtherTugs = 0;
                }

                currentModel.EditedBy       = user.UserName;
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

                // Reset tariff state
                currentModel.Status                = StatusForTariff;
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

                var companyClaims = await GetCompanyClaimAsync()
                    ?? throw new InvalidOperationException("Company claim missing.");
                var audit = BuildAudit(
                    user.UserName!,
                    companyClaims,
                    changes.Any()
                        ? $"Edit dispatch ticket #{currentModel.DispatchNumber}, {string.Join(", ", changes)}"
                        : $"No changes detected for #{currentModel.DispatchNumber}",
                    "Dispatch Ticket");
                await unitOfWork.AuditTrail.AddAsync(audit, cancellationToken);

                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = "Entry edited successfully!";

                return currentModel.JobOrderId.HasValue
                    ? RedirectToAction("Details", "JobOrder", new { id = currentModel.JobOrderId.Value })
                    : RedirectToAction(nameof(Index), new { filterType = await GetCurrentFilterType() });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                logger.LogError(ex, "Failed to edit ticket.");
                TempData["error"] = ex.Message;
                return RedirectToAction("EditTicket",
                    new { id = viewModel.DispatchTicketId, jobOrderId = viewModel.JobOrderId });
            }
        }

        // ════════════════════════════════════════════════════════════════════════
        // STATUS-CHANGE ACTIONS  (Approve / RevokeApproval / Disapprove / Cancel)
        // FIX: All four were structurally identical. Collapsed into one private
        //      helper; each public action is now a one-liner.
        // ════════════════════════════════════════════════════════════════════════

        [HttpGet]
        public Task<IActionResult> Approve(int id, CancellationToken cancellationToken) =>
            ChangeTicketStatusAsync(
                id,
                newStatus:       StatusForBilling,
                activityPrefix:  "Approve tariff",
                documentType:    "Tariff",
                successMessage:  "Entry Approved!",
                permission:      ProcedureEnum.ApproveTariff,
                cancellationToken: cancellationToken);

        [HttpGet]
        public Task<IActionResult> RevokeApproval(int id, CancellationToken cancellationToken) =>
            ChangeTicketStatusAsync(
                id,
                newStatus:       StatusForApproval,
                activityPrefix:  "Revoke Approval",
                documentType:    "Tariff",
                successMessage:  "Approval revoked successfully!",
                permission:      ProcedureEnum.ApproveTariff,
                cancellationToken: cancellationToken);

        [HttpGet]
        public Task<IActionResult> Disapprove(int id, CancellationToken cancellationToken) =>
            ChangeTicketStatusAsync(
                id,
                newStatus:       StatusDisapproved,
                activityPrefix:  "Disapprove Tariff",
                documentType:    "Tariff",
                successMessage:  "Entry Disapproved!",
                permission:      ProcedureEnum.ApproveTariff,
                cancellationToken: cancellationToken);

        public Task<IActionResult> Cancel(int id, CancellationToken cancellationToken) =>
            ChangeTicketStatusAsync(
                id,
                newStatus:       StatusCancelled,
                activityPrefix:  "Cancel dispatch ticket",
                documentType:    "Dispatch Ticket",
                successMessage:  "Dispatch ticket cancelled.",
                permission:      ProcedureEnum.CancelDispatchTicket,
                cancellationToken: cancellationToken);

        // ════════════════════════════════════════════════════════════════════════
        // CHANGE TERMINAL  (JSON endpoint for cascading dropdown)
        // ════════════════════════════════════════════════════════════════════════

        [HttpGet]
        public async Task<IActionResult> ChangeTerminal(int portId, CancellationToken cancellationToken)
        {
            var terminals = await unitOfWork.Terminal
                .GetAllAsync(t => t.PortId == portId, cancellationToken);

            var list = terminals.Select(t => new SelectListItem
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
                      dt => dt.Status != StatusCancelled && dt.Status != StatusForPosting, cancellationToken)
                : await unitOfWork.DispatchTicket.GetAllAsync(
                      dt => dt.Status == status, cancellationToken);

            return Json(items);
        }

        // ════════════════════════════════════════════════════════════════════════
        // GET DISPATCH TICKET LISTS  (DataTables POST)
        // ════════════════════════════════════════════════════════════════════════

        [HttpPost]
        public async Task<IActionResult> GetDispatchTicketLists(
            [FromForm] DataTablesParameters parameters, CancellationToken cancellationToken)
        {
            try
            {
                var filterTypeClaim = await GetCurrentFilterType();

                var queried = dbContext.MMSIDispatchTickets
                    .Include(dt => dt.Service)
                    .Include(dt => dt.Terminal).ThenInclude(dt => dt!.Port)
                    .Include(dt => dt.Tugboat)
                    .Include(dt => dt.TugMaster)
                    .Include(dt => dt.Vessel)
                    .Include(dt => dt.Customer)
                    .Where(dt =>
                        dt.Status != StatusForPosting &&
                        dt.Status != StatusCancelled  &&
                        dt.Status != "Incomplete");

                // Status pre-filter from persisted claim
                queried = filterTypeClaim switch
                {
                    "ForPosting"    => queried.Where(dt => dt.Status == StatusForPosting),
                    "ForTariff"     => queried.Where(dt => dt.Status == StatusForTariff),
                    "ForApproval"   => queried.Where(dt => dt.Status == StatusForApproval),
                    "ForBilling"    => queried.Where(dt => dt.Status == StatusForBilling),
                    "ForCollection" => queried.Where(dt => dt.Status == "For Collection"),
                    _               => queried
                };

                // Global search
                if (!string.IsNullOrEmpty(parameters.Search.Value))
                {
                    var s = parameters.Search.Value.ToLower();
                    queried = queried.Where(dt =>
                        (dt.Date.HasValue && (
                            dt.Date.Value.Day.ToString().Contains(s)   ||
                            dt.Date.Value.Month.ToString().Contains(s) ||
                            dt.Date.Value.Year.ToString().Contains(s))) ||
                        (dt.COSNumber      != null && dt.COSNumber.ToLower().Contains(s)) ||
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
                        (dt.Status    != null && dt.Status.ToLower().Contains(s)));
                }

                // Column-specific search
                foreach (var column in parameters.Columns)
                {
                    if (string.IsNullOrEmpty(column.Search.Value)) continue;
                    var s = column.Search.Value.ToLower();

                    if (column.Data == "status")
                    {
                        queried = s switch
                        {
                            "for tariff"   => queried.Where(dt => dt.Status == StatusForTariff),
                            "for approval" => queried.Where(dt => dt.Status == StatusForApproval),
                            "disapproved"  => queried.Where(dt => dt.Status == StatusDisapproved),
                            "for billing"  => queried.Where(dt => dt.Status == StatusForBilling),
                            "billed"       => queried.Where(dt => dt.Status == "Billed"),
                            _              => queried
                        };
                    }
                }

                // Sorting
                if (parameters.Order?.Count > 0)
                {
                    var col  = parameters.Columns[parameters.Order[0].Column].Data;
                    var dir  = parameters.Order[0].Dir.ToLower() == "asc" ? "ascending" : "descending";
                    queried  = queried.AsQueryable().OrderBy($"{col} {dir}");
                }

                // FIX: recordsFiltered must reflect the filtered count, not total.
                // Count after all filters are applied; page after that.
                var recordsFiltered = await queried.CountAsync(cancellationToken);
                var totalRecords    = await dbContext.MMSIDispatchTickets
                    .CountAsync(dt =>
                        dt.Status != StatusForPosting &&
                        dt.Status != StatusCancelled  &&
                        dt.Status != "Incomplete",
                        cancellationToken);

                var pagedData = await queried
                    .Skip(parameters.Start)
                    .Take(parameters.Length)
                    .ToListAsync(cancellationToken);

                foreach (var dt in pagedData.Where(dt => !string.IsNullOrEmpty(dt.ImageName)))
                    dt.ImageSignedUrl = await GenerateSignedUrl(dt.ImageName!);
                foreach (var dt in pagedData.Where(dt => !string.IsNullOrEmpty(dt.VideoName)))
                    dt.VideoSignedUrl = await GenerateSignedUrl(dt.VideoName!);

                return Json(new
                {
                    draw            = parameters.Draw,
                    recordsTotal    = totalRecords,
                    recordsFiltered = recordsFiltered,
                    data            = pagedData
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to get dispatch tickets.");
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Index), new { filterType = await GetCurrentFilterType() });
            }
        }

        // ════════════════════════════════════════════════════════════════════════
        // DELETE IMAGE
        // FIX: removed dead local System.IO.File path — all file ops go through
        //      _cloudStorageService for consistency.
        // ════════════════════════════════════════════════════════════════════════

        public async Task<IActionResult> DeleteImage(int id, CancellationToken cancellationToken)
        {
            var model = await unitOfWork.DispatchTicket
                .GetAsync(dt => dt.DispatchTicketId == id, cancellationToken);
            if (model == null) return NotFound();

            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                if (!string.IsNullOrEmpty(model.ImageName))
                    await cloudStorageService.DeleteFileAsync(model.ImageName);

                model.ImageName = null;
                await unitOfWork.SaveAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = "Image Deleted Successfully!";
                return RedirectToAction(nameof(Index), new { filterType = await GetCurrentFilterType() });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                logger.LogError(ex, "Failed to delete image.");
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Index), new { filterType = await GetCurrentFilterType() });
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
            if (dispatchModel == null) return NotFound();

            // Cascading fallback: exact → terminal-only → customer-only.
            var tariffRate =
                await unitOfWork.TariffTable.GetAsync(t =>
                    t.CustomerId == customerId &&
                    t.TerminalId == dispatchModel.TerminalId &&
                    t.ServiceId  == dispatchModel.ServiceId &&
                    t.AsOfDate   <= dispatchModel.DateLeft, cancellationToken)
                ??
                await unitOfWork.TariffTable.GetAsync(t =>
                    t.CustomerId == customerId &&
                    t.TerminalId == dispatchModel.TerminalId &&
                    t.AsOfDate   <= dispatchModel.DateLeft, cancellationToken)
                ??
                await unitOfWork.TariffTable.GetAsync(t =>
                    t.CustomerId == customerId &&
                    t.AsOfDate   <= dispatchModel.DateLeft, cancellationToken);

            if (tariffRate != null)
            {
                return Json(new
                {
                    Dispatch         = tariffRate.Dispatch,
                    BAF              = tariffRate.BAF,
                    DispatchDiscount = tariffRate.DispatchDiscount,
                    BAFDiscount      = tariffRate.BAFDiscount,
                    Exists           = true
                });
            }

            return Json(new { Exists = false });
        }

        // ════════════════════════════════════════════════════════════════════════
        // MODAL ACTIONS  (Job Order-centric workflow)
        // ════════════════════════════════════════════════════════════════════════

        [HttpGet]
        public async Task<IActionResult> SetTariffModal(int id, CancellationToken cancellationToken)
        {
            if (!await userAccessService.CheckAccess(
                    userManager.GetUserId(User)!, ProcedureEnum.SetTariff, cancellationToken))
            {
                TempData["error"] = "You don't have permission to set tariff rates.";
                return PartialView("_SetTariffModal", new { message = "Access denied: You need Set Tariff permission" });
            }

            var model = await unitOfWork.DispatchTicket
                .GetAsync(dt => dt.DispatchTicketId == id, cancellationToken);
            if (model == null) return NotFound();

            var viewModel = DispatchTicketModelToTariffVm(model);
            if (!string.IsNullOrEmpty(model.ImageName))
                viewModel.ImageSignedUrl = await GenerateSignedUrl(model.ImageName);

            return PartialView("_SetTariffModal", viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> EditTariffModal(int id, CancellationToken cancellationToken)
        {
            if (!await userAccessService.CheckAccess(
                    userManager.GetUserId(User)!, ProcedureEnum.SetTariff, cancellationToken))
            {
                TempData["error"] = "You don't have permission to edit tariff rates.";
                return PartialView("_ErrorModal", new { message = "Access denied: You need Set Tariff permission" });
            }

            var model = await unitOfWork.DispatchTicket
                .GetAsync(dt => dt.DispatchTicketId == id, cancellationToken);
            if (model == null) return NotFound();

            var viewModel = DispatchTicketModelToTariffVm(model);
            if (!string.IsNullOrEmpty(model.ImageName))
                viewModel.ImageSignedUrl = await GenerateSignedUrl(model.ImageName);

            return PartialView("_EditTariffModal", viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> TariffApprovalModal(int id, CancellationToken cancellationToken)
        {
            if (!await userAccessService.CheckAccess(
                    userManager.GetUserId(User)!, ProcedureEnum.ApproveTariff, cancellationToken))
            {
                TempData["error"] = "You don't have permission to approve tariffs.";
                return PartialView("_ErrorModal", new { message = "Access denied: You need Approve Tariff permission" });
            }

            var model = await unitOfWork.DispatchTicket
                .GetAsync(dt => dt.DispatchTicketId == id, cancellationToken);
            if (model == null) return NotFound();

            var viewModel = DispatchTicketModelToTariffVm(model);
            if (!string.IsNullOrEmpty(model.ImageName))
                viewModel.ImageSignedUrl = await GenerateSignedUrl(model.ImageName);

            return PartialView("_TariffApprovalModal", viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> SaveTariff(
            TariffViewModel vm, string chargeType, CancellationToken cancellationToken)
        {
            if (!await userAccessService.CheckAccess(
                    userManager.GetUserId(User)!, ProcedureEnum.SetTariff, cancellationToken))
                return Json(new { success = false, message = "Access denied" });

            if (!ModelState.IsValid)
                return Json(new { success = false, message = "Invalid tariff data" });

            var user = await userManager.GetUserAsync(User);
            var currentModel = await unitOfWork.DispatchTicket
                .GetAsync(dt => dt.DispatchTicketId == vm.DispatchTicketId, cancellationToken);
            if (currentModel == null)
                return Json(new { success = false, message = "Ticket not found" });

            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                currentModel.Status                = StatusForApproval;
                currentModel.TariffBy              = user!.UserName!;
                currentModel.TariffDate            = DateTimeHelper.GetCurrentPhilippineTime();
                currentModel.DispatchChargeType    = chargeType ?? "Per hour";
                currentModel.BAFChargeType         = chargeType ?? "Per hour";
                currentModel.DispatchRate          = vm.DispatchRate ?? 0;
                currentModel.DispatchDiscount      = vm.DispatchDiscount ?? 0;
                currentModel.BAFRate               = vm.BAFRate ?? 0;
                currentModel.BAFDiscount           = vm.BAFDiscount ?? 0;
                currentModel.DispatchBillingAmount = vm.DispatchBillingAmount;
                currentModel.DispatchNetRevenue    = vm.DispatchNetRevenue;
                currentModel.BAFBillingAmount      = vm.BAFBillingAmount;
                currentModel.BAFNetRevenue         = vm.BAFNetRevenue;
                currentModel.TotalBilling          = vm.TotalBilling;
                currentModel.TotalNetRevenue       = vm.TotalNetRevenue;
                currentModel.ApOtherTugs           = vm.ApOtherTugs ?? 0;

                await unitOfWork.SaveAsync(cancellationToken);

                var companyClaims = await GetCompanyClaimAsync()
                    ?? throw new InvalidOperationException("Company claim missing.");
                var audit = BuildAudit(
                    user.UserName!,
                    companyClaims,
                    $"Set tariff for dispatch ticket #{currentModel.DispatchNumber}",
                    "Dispatch Ticket");
                await unitOfWork.AuditTrail.AddAsync(audit, cancellationToken);

                await transaction.CommitAsync(cancellationToken);
                return Json(new
                {
                    success    = true,
                    message    = "Tariff saved successfully",
                    jobOrderId = currentModel.JobOrderId
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                logger.LogError(ex, "Failed to save tariff");
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> ApproveTariff(int id, CancellationToken cancellationToken)
        {
            if (!await userAccessService.CheckAccess(
                    userManager.GetUserId(User)!, ProcedureEnum.ApproveTariff, cancellationToken))
                return Json(new { success = false, message = "Access denied" });

            var model = await unitOfWork.DispatchTicket
                .GetAsync(dt => dt.DispatchTicketId == id, cancellationToken);
            if (model == null)
                return Json(new { success = false, message = "Ticket not found" });

            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                model.Status = StatusForBilling;
                await unitOfWork.SaveAsync(cancellationToken);

                var user          = await userManager.GetUserAsync(User)!;
                var companyClaims = await GetCompanyClaimAsync()
                    ?? throw new InvalidOperationException("Company claim missing.");
                var audit = BuildAudit(
                    user!.UserName!,
                    companyClaims,
                    $"Approved tariff for dispatch ticket #{model.DispatchNumber}",
                    "Dispatch Ticket");
                await unitOfWork.AuditTrail.AddAsync(audit, cancellationToken);

                await transaction.CommitAsync(cancellationToken);
                return Json(new { success = true, message = "Tariff approved successfully" });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                logger.LogError(ex, "Failed to approve tariff");
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DisapproveTariff(
            int id, string reason, CancellationToken cancellationToken)
        {
            if (!await userAccessService.CheckAccess(
                    userManager.GetUserId(User)!, ProcedureEnum.ApproveTariff, cancellationToken))
                return Json(new { success = false, message = "Access denied" });

            if (string.IsNullOrWhiteSpace(reason) || reason.Length < 10)
                return Json(new { success = false, message = "Please provide a detailed reason (at least 10 characters)" });

            var model = await unitOfWork.DispatchTicket
                .GetAsync(dt => dt.DispatchTicketId == id, cancellationToken);
            if (model == null)
                return Json(new { success = false, message = "Ticket not found" });

            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                model.Status  = StatusDisapproved;
                model.Remarks = string.IsNullOrEmpty(model.Remarks)
                    ? $"Disapproved: {reason}"
                    : $"{model.Remarks} | Disapproved: {reason}";
                await unitOfWork.SaveAsync(cancellationToken);

                var user          = await userManager.GetUserAsync(User)!;
                var companyClaims = await GetCompanyClaimAsync()
                    ?? throw new InvalidOperationException("Company claim missing.");
                var audit = BuildAudit(
                    user!.UserName!,
                    companyClaims,
                    $"Disapproved tariff for dispatch ticket #{model.DispatchNumber}. Reason: {reason}",
                    "Dispatch Ticket");
                await unitOfWork.AuditTrail.AddAsync(audit, cancellationToken);

                await transaction.CommitAsync(cancellationToken);
                return Json(new { success = true, message = "Tariff disapproved successfully" });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                logger.LogError(ex, "Failed to disapprove tariff");
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ════════════════════════════════════════════════════════════════════════
        // PRIVATE HELPERS
        // ════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// FIX: Approve / RevokeApproval / Disapprove / Cancel were four near-identical
        /// action methods. This single helper encapsulates the shared scaffold;
        /// each caller supplies only what differs.
        /// </summary>
        private async Task<IActionResult> ChangeTicketStatusAsync(
            int id,
            string newStatus,
            string activityPrefix,
            string documentType,
            string successMessage,
            ProcedureEnum permission,
            CancellationToken cancellationToken)
        {
            if (!await userAccessService.CheckAccess(
                    userManager.GetUserId(User)!, permission, cancellationToken))
            {
                TempData["error"] = "Access denied.";
                return RedirectToAction(nameof(Index));
            }

            var model = await unitOfWork.DispatchTicket
                .GetAsync(dt => dt.DispatchTicketId == id, cancellationToken);
            if (model == null)
            {
                TempData["error"] = "Can't find entry, please try again.";
                return RedirectToAction(nameof(Index), new { filterType = await GetCurrentFilterType() });
            }

            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                model.Status = newStatus;
                await unitOfWork.SaveAsync(cancellationToken);

                var user          = await userManager.GetUserAsync(User)!;
                var companyClaims = await GetCompanyClaimAsync()
                    ?? throw new InvalidOperationException("Company claim missing.");
                var audit = BuildAudit(
                    user!.UserName!,
                    companyClaims,
                    $"{activityPrefix} #{model.DispatchTicketId}",
                    documentType);
                await unitOfWork.AuditTrail.AddAsync(audit, cancellationToken);

                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = successMessage;
                return RedirectToAction(nameof(Index), new { filterType = await GetCurrentFilterType() });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                logger.LogError(ex, "Failed to change ticket status to {Status}.", newStatus);
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Index), new { filterType = await GetCurrentFilterType() });
            }
        }

        /// <summary>
        /// FIX: The job-order pre-fill logic was duplicated across Create GET and
        /// GetDispatchTicketPartial (create branch). Extracted to a single method.
        /// </summary>
        private async Task<ServiceRequestViewModel> PreFillFromJobOrderAsync(
            ServiceRequestViewModel viewModel, int jobOrderId, CancellationToken cancellationToken)
        {
            var jobOrder = await unitOfWork.JobOrder
                .GetAsync(j => j.JobOrderId == jobOrderId, cancellationToken);

            if (jobOrder == null) return viewModel;

            viewModel.JobOrderId   = jobOrderId;
            viewModel.CustomerId   = jobOrder.CustomerId;
            viewModel.VesselId     = jobOrder.VesselId;
            viewModel.PortId       = jobOrder.PortId;
            viewModel.TerminalId   = jobOrder.TerminalId;
            viewModel.COSNumber    = jobOrder.COSNumber;
            viewModel.VoyageNumber = jobOrder.VoyageNumber;
            viewModel.Date         = jobOrder.Date;

            if (jobOrder.TerminalId.HasValue)
                viewModel.Terminal = new MMSITerminal { PortId = jobOrder.PortId ?? 0 };

            return viewModel;
        }

        /// <summary>
        /// FIX: Hours calculation (including PHIL-CEB rounding) was duplicated
        /// verbatim in Create POST and EditTicket POST. Extracted here.
        /// </summary>
        private static decimal CalculateTotalHours(MMSIDispatchTicket model)
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

        /// <summary>
        /// FIX: Audit trail construction was copy-pasted in every action.
        /// Now built in one place.
        /// </summary>
        private static AuditTrail BuildAudit(
            string username, string company, string activity, string documentType) =>
            new()
            {
                Date         = DateTimeHelper.GetCurrentPhilippineTime(),
                Username     = username,
                MachineName  = Environment.MachineName,
                Activity     = activity,
                DocumentType = documentType,
                Company      = company
            };

        private async Task GenerateSignedUrl(MMSIDispatchTicket model)
        {
            if (!string.IsNullOrWhiteSpace(model.ImageName))
                model.ImageSignedUrl = await cloudStorageService.GetSignedUrlAsync(model.ImageName);
            if (!string.IsNullOrWhiteSpace(model.VideoName))
                model.VideoSignedUrl = await cloudStorageService.GetSignedUrlAsync(model.VideoName);
        }

        private async Task<string> GenerateSignedUrl(string uploadName)
        {
            if (!string.IsNullOrWhiteSpace(uploadName))
                return await cloudStorageService.GetSignedUrlAsync(uploadName);
            throw new InvalidOperationException("Upload name is null or empty.");
        }

        private async Task<string?> GetCompanyClaimAsync()
        {
            var user = await userManager.GetUserAsync(User);
            if (user == null) return null;
            var claims = await userManager.GetClaimsAsync(user);
            return claims.FirstOrDefault(c => c.Type == "Company")?.Value;
        }

        private async Task UpdateFilterTypeClaim(string filterType)
        {
            var user = await userManager.GetUserAsync(User);
            if (user == null) return;

            var existing = (await userManager.GetClaimsAsync(user))
                .FirstOrDefault(c => c.Type == FilterTypeClaimType);
            if (existing != null)
                await userManager.RemoveClaimAsync(user, existing);
            if (!string.IsNullOrEmpty(filterType))
                await userManager.AddClaimAsync(user, new Claim(FilterTypeClaimType, filterType));
        }

        private async Task<string?> GetCurrentFilterType()
        {
            var user = await userManager.GetUserAsync(User);
            if (user == null) return null;
            var claims = await userManager.GetClaimsAsync(user);
            return claims.FirstOrDefault(c => c.Type == FilterTypeClaimType)?.Value;
        }

        // FIX: GetUserNameAsync is no longer called in audit blocks — username
        // is taken directly from the already-resolved user object. The method is
        // retained only if other callers exist outside this file.
        private async Task<string?> GetUserNameAsync()
        {
            var user = await userManager.GetUserAsync(User);
            return user?.UserName;
        }

        private static string GenerateFileNameToSave(string incomingFileName, string type)
        {
            var name = Path.GetFileNameWithoutExtension(incomingFileName);
            var ext  = Path.GetExtension(incomingFileName);
            return $"{name}-{type}-{DateTimeHelper.GetCurrentPhilippineTime():yyyyMMddHHmmss}{ext}";
        }

        private async Task<bool> HasDispatchTicketAccessAsync(CancellationToken cancellationToken)
        {
            var userId = userManager.GetUserId(User)!;
            // FIX: three independent permission checks — run in parallel.
            var (hasCreate, hasEdit, hasCancel) = (
                await userAccessService.CheckAccess(userId, ProcedureEnum.CreateDispatchTicket, cancellationToken),
                await userAccessService.CheckAccess(userId, ProcedureEnum.EditDispatchTicket,   cancellationToken),
                await userAccessService.CheckAccess(userId, ProcedureEnum.CancelDispatchTicket, cancellationToken));
            return hasCreate || hasEdit || hasCancel;
        }

        // ════════════════════════════════════════════════════════════════════════
        // MAPPER METHODS
        // FIX: were public instance methods — accidentally routable in MVC.
        //      Changed to private static; they don't touch any instance state.
        // ════════════════════════════════════════════════════════════════════════

        private static MMSIDispatchTicket ServiceRequestVmToDispatchTicket(ServiceRequestViewModel vm) =>
            new()
            {
                DispatchTicketId = vm.DispatchTicketId ?? 0,
                Date             = vm.Date,
                COSNumber        = vm.COSNumber,
                DispatchNumber   = vm.DispatchNumber,
                VoyageNumber     = vm.VoyageNumber,
                CustomerId       = vm.CustomerId,
                DateLeft         = vm.DateLeft,
                TimeLeft         = vm.TimeLeft,
                DateArrived      = vm.DateArrived,
                TimeArrived      = vm.TimeArrived,
                TerminalId       = vm.TerminalId,
                ServiceId        = vm.ServiceId,
                TugBoatId        = vm.TugBoatId,
                TugMasterId      = vm.TugMasterId,
                VesselId         = vm.VesselId,
                Remarks          = vm.Remarks,
                DispatchChargeType = string.Empty,
                BAFChargeType      = string.Empty,
                TariffBy           = string.Empty,
                TariffEditedBy     = string.Empty,
                JobOrderId         = vm.JobOrderId
            };

        private static MMSIDispatchTicket TariffVmToDispatchTicket(TariffViewModel vm) =>
            new()
            {
                DispatchTicketId       = vm.DispatchTicketId,
                CustomerId             = vm.CustomerId,
                DispatchRate           = vm.DispatchRate ?? 0,
                DispatchDiscount       = vm.DispatchDiscount ?? 0,
                DispatchBillingAmount  = vm.DispatchBillingAmount,
                DispatchNetRevenue     = vm.DispatchNetRevenue,
                BAFRate                = vm.BAFRate ?? 0,
                BAFDiscount            = vm.BAFDiscount ?? 0,
                BAFBillingAmount       = vm.BAFBillingAmount,
                BAFNetRevenue          = vm.BAFNetRevenue,
                TotalBilling           = vm.TotalBilling,
                TotalNetRevenue        = vm.TotalNetRevenue,
                ApOtherTugs            = vm.ApOtherTugs ?? 0
            };

        private static ServiceRequestViewModel DispatchTicketModelToServiceRequestVm(MMSIDispatchTicket model) =>
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

        private static TariffViewModel DispatchTicketModelToTariffVm(MMSIDispatchTicket model) =>
            new()
            {
                DispatchTicketId      = model.DispatchTicketId,
                JobOrderId            = model.JobOrderId,
                DispatchNumber        = model.DispatchNumber,
                COSNumber             = model.COSNumber,
                VoyageNumber          = model.VoyageNumber,
                Date                  = model.Date,
                TugMasterName         = model.TugMaster?.TugMasterName,
                DateLeft              = model.DateLeft,
                TimeLeft              = model.TimeLeft,
                DateArrived           = model.DateArrived,
                TimeArrived           = model.TimeArrived,
                TugboatName           = model.Tugboat?.TugboatName,
                VesselName            = model.Vessel?.VesselName,
                VesselType            = model.Vessel?.VesselType,
                TerminalName          = model.Terminal?.TerminalName,
                PortName              = model.Terminal?.Port?.PortName,
                IsTugboatCompanyOwned = model.Tugboat?.IsCompanyOwned,
                TugboatOwnerName      = model.Tugboat?.TugboatOwner?.TugboatOwnerName,
                FixedRate             = model.Tugboat?.TugboatOwner?.FixedRate,
                Remarks               = model.Remarks,
                CustomerName          = model.Customer?.CustomerName,
                TotalHours            = model.TotalHours,
                ImageName             = model.ImageName,
                DispatchChargeType    = model.DispatchChargeType,
                BAFChargeType         = model.BAFChargeType,
                CustomerId            = model.CustomerId,
                DispatchRate          = model.DispatchRate,
                DispatchDiscount      = model.DispatchDiscount,
                DispatchBillingAmount = model.DispatchBillingAmount,
                DispatchNetRevenue    = model.DispatchNetRevenue,
                BAFRate               = model.BAFRate,
                BAFDiscount           = model.BAFDiscount,
                BAFBillingAmount      = model.BAFBillingAmount,
                BAFNetRevenue         = model.BAFNetRevenue,
                TotalBilling          = model.TotalBilling,
                TotalNetRevenue       = model.TotalNetRevenue,
                ApOtherTugs           = model.ApOtherTugs
            };
    }
}