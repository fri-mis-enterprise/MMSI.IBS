using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using IBS.Models.Enums;
using IBS.Models.MMSI;
using IBS.Models.MMSI.ViewModels;
using IBS.Services;
using IBS.Services.Attributes;
using IBS.Utility.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

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
                    ViewData["PortId"] = jobOrder.PortId;
                }
            }

            viewModel = await unitOfWork.ServiceRequest.GetDispatchTicketSelectLists(viewModel, cancellationToken);
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
                viewModel = await unitOfWork.ServiceRequest.GetDispatchTicketSelectLists(viewModel, cancellationToken);
                viewModel.Customers = await unitOfWork.GetCustomerListAsyncById(cancellationToken);
                TempData["warning"] = "Can't create entry, please review your input.";
                return View(viewModel);
            }

            var model = viewModel.ToEntity();

            try
            {
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

                model.CreatedBy = User.Identity?.Name ?? "System";
                await unitOfWork.DispatchTicket.AddAsync(model, cancellationToken);

                await unitOfWork.AuditTrail.AddAsync(
                    new AuditTrail(model.CreatedBy, $"Create dispatch ticket #{model.DispatchNumber}", "Dispatch Ticket"),
                    cancellationToken);

                await unitOfWork.SaveAsync(cancellationToken);

                TempData["success"] = $"Dispatch Ticket #{model.DispatchNumber} was successfully created.";
                return RedirectToAction("Details", "JobOrder", new { id = viewModel.JobOrderId });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to create dispatch ticket.");
                viewModel = await unitOfWork.ServiceRequest.GetDispatchTicketSelectLists(viewModel, cancellationToken);
                viewModel.Customers = await unitOfWork.GetCustomerListAsyncById(cancellationToken);
                TempData["error"] = ex.Message;
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

            if (!string.IsNullOrEmpty(model.ImageName))
                model.ImageSignedUrl = await cloudStorageService.GetSignedUrlAsync(model.ImageName);
            if (!string.IsNullOrEmpty(model.VideoName))
                model.VideoSignedUrl = await cloudStorageService.GetSignedUrlAsync(model.VideoName);

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
                .Include(dt => dt.Tugboat).ThenInclude(t => t.TugboatOwner)
                .Include(dt => dt.Vessel)
                .Include(dt => dt.Terminal).ThenInclude(t => t.Port)
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
                .Include(dt => dt.Tugboat).ThenInclude(t => t.TugboatOwner)
                .Include(dt => dt.Vessel)
                .Include(dt => dt.Terminal).ThenInclude(t => t.Port)
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

            var viewModel = new ServiceRequestViewModel
            {
                DispatchTicketId = model.DispatchTicketId,
                Date = model.Date,
                COSNumber = model.COSNumber,
                DispatchNumber = model.DispatchNumber,
                VoyageNumber = model.VoyageNumber,
                CustomerId = model.CustomerId,
                DateLeft = model.DateLeft,
                TimeLeft = model.TimeLeft,
                DateArrived = model.DateArrived,
                TimeArrived = model.TimeArrived,
                TerminalId = model.TerminalId,
                ServiceId = model.ServiceId,
                TugBoatId = model.TugBoatId,
                TugMasterId = model.TugMasterId,
                VesselId = model.VesselId,
                Terminal = model.Terminal,
                Remarks = model.Remarks,
                JobOrderId = jobOrderId ?? model.JobOrderId,
                ImageName = model.ImageName,
                ImageSignedUrl = !string.IsNullOrEmpty(model.ImageName)
                    ? await cloudStorageService.GetSignedUrlAsync(model.ImageName)
                    : null,
                VideoName = model.VideoName,
                VideoSignedUrl = !string.IsNullOrEmpty(model.VideoName)
                    ? await cloudStorageService.GetSignedUrlAsync(model.VideoName)
                    : null
            };

            if (model.Terminal?.Port != null)
            {
                viewModel.PortId = model.Terminal.Port.PortId;
            }

            viewModel = await unitOfWork.ServiceRequest.GetDispatchTicketSelectLists(viewModel, cancellationToken);
            viewModel.Customers = await unitOfWork.GetCustomerListAsyncById(cancellationToken);

            ViewData["PortId"] = model.Terminal?.Port?.PortId;
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

            try
            {
                var model = viewModel.ToEntity();

                if (imageFile != null)
                {
                    var existing = await unitOfWork.DispatchTicket.GetAsync(dt => dt.DispatchTicketId == viewModel.DispatchTicketId, cancellationToken);
                    if (!string.IsNullOrEmpty(existing?.ImageName))
                    {
                        await cloudStorageService.DeleteFileAsync(existing.ImageName);
                    }

                    var ext = Path.GetExtension(imageFile.FileName);
                    var name = Path.GetFileNameWithoutExtension(imageFile.FileName);
                    model.ImageName = $"{name}-img-{DateTimeHelper.GetCurrentPhilippineTime():yyyyMMddHHmmss}{ext}";
                    model.ImageSavedUrl = await cloudStorageService.UploadFileAsync(imageFile, model.ImageName);
                }

                if (videoFile != null)
                {
                    var existing = await unitOfWork.DispatchTicket.GetAsync(dt => dt.DispatchTicketId == viewModel.DispatchTicketId, cancellationToken);
                    if (!string.IsNullOrEmpty(existing?.VideoName))
                    {
                        await cloudStorageService.DeleteFileAsync(existing.VideoName);
                    }

                    var ext = Path.GetExtension(videoFile.FileName);
                    var name = Path.GetFileNameWithoutExtension(videoFile.FileName);
                    model.VideoName = $"{name}-vid-{DateTimeHelper.GetCurrentPhilippineTime():yyyyMMddHHmmss}{ext}";
                    model.VideoSavedUrl = await cloudStorageService.UploadFileAsync(videoFile, model.VideoName);
                }

                var updatedBy = User.Identity?.Name ?? "System";
                await unitOfWork.DispatchTicket.UpdateAsync(model, updatedBy, cancellationToken);

                TempData["success"] = "Entry edited successfully!";

                return model.JobOrderId.HasValue
                    ? RedirectToAction("Details", "JobOrder", new { id = model.JobOrderId.Value })
                    : RedirectToAction(nameof(Index), new { filterType });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to edit ticket.");
                TempData["error"] = ex.Message;
                return RedirectToAction("EditTicket", new { id = viewModel.DispatchTicketId, jobOrderId = viewModel.JobOrderId });
            }
        }

        // ════════════════════════════════════════════════════════════════════════
        // STATUS-CHANGE ACTIONS  (Approve / RevokeApproval / Disapprove / Cancel)
        // ════════════════════════════════════════════════════════════════════════

        [HttpPost]
        [RequireAccess(ProcedureEnum.ApproveTariff)]
        public async Task<IActionResult> ChangeStatus(int id, string status, string activity, string docType, string successMessage, CancellationToken cancellationToken)
        {
            try
            {
                await unitOfWork.DispatchTicket.UpdateStatusAsync(id, status, User.Identity?.Name ?? "System", activity, docType, cancellationToken);
                TempData["success"] = successMessage;
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

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
                    dt => dt.Status != "Cancelled" && dt.Status != "For Posting", cancellationToken)
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
                var (data, recordsFiltered, totalRecords) = await unitOfWork.DispatchTicket.GetPagedDispatchTicketsAsync(parameters, filterType, cancellationToken);

                foreach (var dt in data.Where(dt => !string.IsNullOrEmpty(dt.ImageName)))
                {
                    dt.ImageSignedUrl = await cloudStorageService.GetSignedUrlAsync(dt.ImageName!);
                }

                foreach (var dt in data.Where(dt => !string.IsNullOrEmpty(dt.VideoName)))
                {
                    dt.VideoSignedUrl = await cloudStorageService.GetSignedUrlAsync(dt.VideoName!);
                }

                return Json(new
                {
                    draw = parameters.Draw,
                    recordsTotal = totalRecords,
                    recordsFiltered,
                    data
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
                newStatus:     "For Billing",
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
                newStatus: "Disapproved",
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
        // PRIVATE HELPERS (minimal set)
        // ════════════════════════════════════════════════════════════════════════

        private static bool IsArrivalAfterDeparture(DispatchTicket model)
        {
            if (model.DateLeft == null || model.DateArrived == null || model.TimeLeft == null || model.TimeArrived == null)
                return true;

            var departure = model.DateLeft.Value.ToDateTime(model.TimeLeft.Value);
            var arrival = model.DateArrived.Value.ToDateTime(model.TimeArrived.Value);
            return arrival > departure;
        }

        private async Task<IActionResult> SaveTariffAsync(
            DispatchTicket model,
            string chargeType,
            string chargeType2,
            string filterType,
            bool isEdit,
            CancellationToken cancellationToken)
        {
            var userName = User.Identity?.Name ?? "System";
            await unitOfWork.DispatchTicket.SaveTariffAsync(
                model, chargeType, chargeType2, userName, isEdit, cancellationToken);

            TempData["success"] = isEdit ? "Tariff updated successfully." : "Tariff set successfully.";

            if (model.JobOrderId.HasValue)
            {
                return RedirectToAction("Details", "JobOrder", new { id = model.JobOrderId.Value });
            }

            return RedirectToAction(nameof(Index), new { filterType });
        }

        private async Task<bool> IsJobOrderEditableAsync(int? jobOrderId, CancellationToken cancellationToken)
        {
            return await unitOfWork.DispatchTicket.IsJobOrderEditableAsync(jobOrderId, cancellationToken);
        }

        private async Task<bool> IsTicketJobOrderEditableAsync(int dispatchTicketId, CancellationToken cancellationToken)
        {
            var ticket = await unitOfWork.DispatchTicket.GetAsync(
                dt => dt.DispatchTicketId == dispatchTicketId, cancellationToken);
            return ticket != null && await IsJobOrderEditableAsync(ticket.JobOrderId, cancellationToken);
        }

        private async Task<IActionResult> ChangeTicketStatusJsonAsync(
            int id,
            string newStatus,
            Func<DispatchTicket, string> auditActivity,
            CancellationToken cancellationToken,
            Action<DispatchTicket>? beforeSave = null)
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var model = await unitOfWork.DispatchTicket.GetAsync(
                    dt => dt.DispatchTicketId == id, cancellationToken);

                if (model == null)
                {
                    return Json(new { success = false, message = "Ticket not found." });
                }

                model.Status = newStatus;
                model.EditedBy = User.Identity?.Name ?? "System";
                model.EditedDate = DateTimeHelper.GetCurrentPhilippineTime();

                beforeSave?.Invoke(model);

                await unitOfWork.AuditTrail.AddAsync(
                    new AuditTrail(model.EditedBy, auditActivity(model), "Dispatch Ticket"),
                    cancellationToken);

                await unitOfWork.SaveAsync(cancellationToken);

                await transaction.CommitAsync(cancellationToken);
                return Json(new { success = true, message = "Status updated successfully." });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                logger.LogError(ex, "Failed to change ticket status.");
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}
