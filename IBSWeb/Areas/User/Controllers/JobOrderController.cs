using IBS.DataAccess.Repository.IRepository;
using IBS.Models.Enums;
using IBS.Models.MMSI;
using IBS.Models.MMSI.ViewModels;
using IBS.Models;
using IBS.Services.AccessControl;
using IBS.Services.Attributes;
using IBS.Utility.Constants;
using IBS.Utility.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IBSWeb.Areas.User.Controllers
{
    [Area("User")]
    [CompanyAuthorize(SD.Company_MMSI)]
    public class JobOrderController(
        IAccessControlService accessControl,
        UserManager<ApplicationUser> userManager,
        IUnitOfWork unitOfWork,
        ILogger<JobOrderController> logger)
        : BaseController(accessControl, userManager)
    {
        #region Index

        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            if (!await HasJobOrderAccessAsync())
            {
                TempData["error"] = "Access denied. You don't have permission to access Job Orders.";
                return RedirectToAction("Index", "Home", new { area = "User" });
            }

            var jobOrders = await unitOfWork.JobOrder.GetAllJobOrdersWithDetailsAsync(cancellationToken);

            // Populate create modal view model if user has create access
            if (await AccessControl.HasAccessAsync(GetUserId(), ProcedureEnum.CreateJobOrder))
            {
                var createViewModel = new JobOrderViewModel();
                await PopulateSelectListsAsync(createViewModel, cancellationToken);
                ViewBag.CreateViewModel = createViewModel;
            }

            return View(jobOrders
                .OrderByDescending(j => j.JobOrderNumber)
                .ToList());
        }

        #endregion

        #region Create

        [HttpGet]
        public async Task<IActionResult> CreateModal(CancellationToken cancellationToken)
        {
            if (!await AccessControl.HasAccessAsync(GetUserId(), ProcedureEnum.CreateJobOrder))
            {
                return PartialView("_ErrorModal", new { message = "You don't have permission to create Job Orders." });
            }

            var viewModel = new JobOrderViewModel();
            await PopulateSelectListsAsync(viewModel, cancellationToken);

            return PartialView("_CreateModal", viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(JobOrderViewModel viewModel, CancellationToken cancellationToken)
        {
            if (!await AccessControl.HasAccessAsync(GetUserId(), ProcedureEnum.CreateJobOrder))
            {
                TempData["error"] = "Access denied. You don't have permission to create Job Orders.";
                return RedirectToAction("Index", "Home", new { area = "User" });
            }

            if (!ModelState.IsValid)
            {
                await PopulateSelectListsAsync(viewModel, cancellationToken);
                return PartialView("_CreateModal", viewModel);
            }

            try
            {
                var currentUser = await GetCurrentUserAsync();

                var jobOrder = new JobOrder
                {
                    Date           = viewModel.Date,
                    CustomerId     = viewModel.CustomerId,
                    VesselId       = viewModel.VesselId,
                    PortId         = viewModel.PortId,
                    TerminalId     = viewModel.TerminalId,
                    COSNumber      = viewModel.COSNumber,
                    VoyageNumber   = viewModel.VoyageNumber,
                    Remarks        = viewModel.Remarks,
                    Status         = JobOrderStatus.Open,
                    JobOrderNumber = await unitOfWork.JobOrder.GenerateJobOrderNumber(cancellationToken),
                    CreatedBy      = currentUser.UserName ?? "Unknown",
                    CreatedDate    = DateTimeHelper.GetCurrentPhilippineTime()
                };

                await unitOfWork.JobOrder.AddAsync(jobOrder, cancellationToken);

                await RecordAuditAsync(
                    activity: $"Created Job Order #{jobOrder.JobOrderNumber}",
                    username: currentUser.UserName!,
                    cancellationToken: cancellationToken);

                await unitOfWork.SaveAsync(cancellationToken);

                TempData["success"] = "Job Order created successfully.";
                return Json(new { success = true, redirectUrl = Url.Action("Details", new { id = jobOrder.JobOrderId }) });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error creating Job Order");
                TempData["error"] = "Error creating Job Order.";
            }

            await PopulateSelectListsAsync(viewModel, cancellationToken);
            return PartialView("_CreateModal", viewModel);
        }

        #endregion

        #region Details

        public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
        {
            if (!await HasJobOrderAccessAsync())
            {
                TempData["error"] = "Access denied. You don't have permission to view Job Orders.";
                return RedirectToAction("Index", "Home", new { area = "User" });
            }

            var jobOrder = await unitOfWork.JobOrder.GetJobOrderWithDetailsAsync(id, cancellationToken);
            if (jobOrder == null)
            {
                return NotFound();
            }

            var companyClaim = await GetCompanyClaimAsync();

            var ticketViewModel = new ServiceRequestViewModel
            {
                JobOrderId   = jobOrder.JobOrderId,
                CustomerId   = jobOrder.CustomerId,
                VesselId     = jobOrder.VesselId,
                PortId       = jobOrder.PortId,
                TerminalId   = jobOrder.TerminalId,
                COSNumber    = jobOrder.COSNumber,
                VoyageNumber = jobOrder.VoyageNumber,
                Date         = jobOrder.Date
            };

            ticketViewModel = await unitOfWork.ServiceRequest.GetDispatchTicketSelectLists(ticketViewModel, cancellationToken);
            ticketViewModel.Customers = await unitOfWork.GetCustomerListAsyncById(companyClaim!, cancellationToken);

            var viewModel = new JobOrderViewModel
            {
                JobOrderId      = jobOrder.JobOrderId,
                JobOrderNumber  = jobOrder.JobOrderNumber,
                Date            = jobOrder.Date,
                Status          = jobOrder.Status,
                CustomerId      = jobOrder.CustomerId,
                CustomerName    = jobOrder.Customer?.CustomerName,
                VesselId        = jobOrder.VesselId,
                VesselName      = jobOrder.Vessel?.VesselName,
                PortId          = jobOrder.PortId,
                PortName        = jobOrder.Port?.PortName,
                TerminalId      = jobOrder.TerminalId,
                TerminalName    = jobOrder.Terminal?.TerminalName,
                COSNumber       = jobOrder.COSNumber,
                VoyageNumber    = jobOrder.VoyageNumber,
                Remarks         = jobOrder.Remarks,
                DispatchTickets = jobOrder.DispatchTickets.ToList()
            };

            ViewData["TicketViewModel"] = ticketViewModel;

            return View(viewModel);
        }

        #endregion

        #region Edit

        [HttpGet]
        public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
        {
            if (!await AccessControl.HasAccessAsync(GetUserId(), ProcedureEnum.EditJobOrder))
            {
                TempData["error"] = "Access denied. You don't have permission to edit Job Orders.";
                return RedirectToAction("Index", "Home", new { area = "User" });
            }

            var jobOrder = await unitOfWork.JobOrder.GetAsync(j => j.JobOrderId == id, cancellationToken);
            if (jobOrder == null)
            {
                return NotFound();
            }

            var viewModel = MapToViewModel(jobOrder);
            await PopulateSelectListsAsync(viewModel, cancellationToken);

            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> EditModal(int id, CancellationToken cancellationToken)
        {
            if (!await AccessControl.HasAccessAsync(GetUserId(), ProcedureEnum.EditJobOrder))
            {
                return PartialView("_ErrorModal", new { message = "You don't have permission to edit this Job Order." });
            }

            var jobOrder = await unitOfWork.JobOrder.GetJobOrderWithDetailsAsync(id, cancellationToken);
            if (jobOrder == null)
            {
                return NotFound();
            }

            var viewModel = MapToViewModel(jobOrder);
            await PopulateSelectListsAsync(viewModel, cancellationToken);

            // Pass ticket count for warning display
            ViewData["HasTickets"] = jobOrder.DispatchTickets.Any();

            return PartialView("_EditModal", viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(JobOrderViewModel viewModel, CancellationToken cancellationToken)
        {
            if (!await AccessControl.HasAccessAsync(GetUserId(), ProcedureEnum.EditJobOrder))
            {
                TempData["error"] = "Access denied. You don't have permission to edit Job Orders.";
                return RedirectToAction("Index", "Home", new { area = "User" });
            }

            // Validate required fields
            if (viewModel.CustomerId <= 0)
            {
                TempData["error"] = "Customer is required.";
                await PopulateSelectListsAsync(viewModel, cancellationToken);
                return PartialView("_EditModal", viewModel);
            }

            if (viewModel.VesselId <= 0)
            {
                TempData["error"] = "Vessel is required.";
                await PopulateSelectListsAsync(viewModel, cancellationToken);
                return PartialView("_EditModal", viewModel);
            }

            if (!ModelState.IsValid)
            {
                await PopulateSelectListsAsync(viewModel, cancellationToken);
                return PartialView("_EditModal", viewModel);
            }

            try
            {
                var jobOrder = await unitOfWork.JobOrder.GetAsync(j => j.JobOrderId == viewModel.JobOrderId, cancellationToken);
                if (jobOrder == null)
                {
                    return NotFound();
                }

                var currentUser = await GetCurrentUserAsync();

                jobOrder.Date         = viewModel.Date;
                jobOrder.CustomerId   = viewModel.CustomerId;
                jobOrder.VesselId     = viewModel.VesselId;
                jobOrder.PortId       = viewModel.PortId;
                jobOrder.TerminalId   = viewModel.TerminalId;
                jobOrder.COSNumber    = viewModel.COSNumber;
                jobOrder.VoyageNumber = viewModel.VoyageNumber;
                jobOrder.Remarks      = viewModel.Remarks;
                jobOrder.EditedBy     = currentUser.UserName ?? "Unknown";
                jobOrder.EditedDate   = DateTimeHelper.GetCurrentPhilippineTime();

                await RecordAuditAsync(
                    activity: $"Edited Job Order #{jobOrder.JobOrderNumber}",
                    username: currentUser.UserName!,
                    cancellationToken: cancellationToken);

                await unitOfWork.SaveAsync(cancellationToken);

                TempData["success"] = "Job Order updated successfully.";
                return RedirectToAction(nameof(Details), new { id = jobOrder.JobOrderId });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error updating Job Order {JobOrderId}", viewModel.JobOrderId);
                TempData["error"] = "Error updating Job Order.";
            }

            await PopulateSelectListsAsync(viewModel, cancellationToken);
            return PartialView("_EditModal", viewModel);
        }

        #endregion

        #region Cancel

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> Cancel(int id, CancellationToken cancellationToken)
        {
            if (!await AccessControl.HasAccessAsync(GetUserId(), ProcedureEnum.DeleteJobOrder))
            {
                return PermissionDenied("You don't have permission to cancel Job Orders.");
            }

            var jobOrder = await unitOfWork.JobOrder.GetJobOrderWithDetailsAsync(id, cancellationToken);
            if (jobOrder == null)
            {
                return NotFound();
            }

            if (jobOrder.Status == JobOrderStatus.Cancelled)
            {
                TempData["error"] = $"Job Order #{jobOrder.JobOrderNumber} is already cancelled.";
                return RedirectToAction(nameof(Details), new { id });
            }

            // Count tickets by status for validation
            var ticketsForBillingOrBilled = jobOrder.DispatchTickets
                .Count(dt => dt.Status == "For Billing" || dt.Status == "Billed");

            var ticketsForApproval = jobOrder.DispatchTickets
                .Count(dt => dt.Status == "For Approval");

            var ticketsDisapproved = jobOrder.DispatchTickets
                .Count(dt => dt.Status == "Disapproved");

            var ticketsWithoutTariff = jobOrder.DispatchTickets
                .Count(dt => dt.Status == "Pending" || dt.Status == "For Tariff");

            // Build warning message
            var warnings = new List<string>();
            if (ticketsForApproval > 0)
            {
                warnings.Add($"{ticketsForApproval} ticket(s) pending approval will be affected");
            }

            if (ticketsDisapproved > 0)
            {
                warnings.Add($"{ticketsDisapproved} disapproved ticket(s) need attention");
            }

            if (ticketsWithoutTariff > 0)
            {
                warnings.Add($"{ticketsWithoutTariff} ticket(s) without tariff will be orphaned");
            }

            ViewData["HasTickets"] = jobOrder.DispatchTickets.Any();
            ViewData["TicketsForBilling"] = ticketsForBillingOrBilled;
            ViewData["TicketsForApproval"] = ticketsForApproval;
            ViewData["TicketsDisapproved"] = ticketsDisapproved;
            ViewData["TicketsWithoutTariff"] = ticketsWithoutTariff;
            ViewData["WarningMessage"] = warnings.Any()
                ? "Warning: Cancelling this Job Order will affect existing tickets:<br/>• " +
                  string.Join("<br/>• ", warnings)
                : "";

            return View(new JobOrderViewModel
            {
                JobOrderId     = jobOrder.JobOrderId,
                JobOrderNumber = jobOrder.JobOrderNumber,
                Date           = jobOrder.Date,
                CustomerName   = jobOrder.Customer?.CustomerName,
                VesselName     = jobOrder.Vessel?.VesselName
            });
        }

        [Authorize(Roles = "Admin")]
        [HttpPost, ActionName("Cancel")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelConfirmed(int id, bool confirmed = false, CancellationToken cancellationToken = default)
        {
            if (!await AccessControl.HasAccessAsync(GetUserId(), ProcedureEnum.DeleteJobOrder))
            {
                return PermissionDenied("You don't have permission to cancel Job Orders.");
            }

            var jobOrder = await unitOfWork.JobOrder.GetJobOrderWithDetailsAsync(id, cancellationToken);
            if (jobOrder == null)
            {
                return NotFound();
            }

            if (jobOrder.Status == JobOrderStatus.Cancelled)
            {
                TempData["error"] = $"Job Order #{jobOrder.JobOrderNumber} is already cancelled.";
                return RedirectToAction(nameof(Details), new { id });
            }

            // Count tickets by status for validation
            var ticketsForBillingOrBilled = jobOrder.DispatchTickets
                .Count(dt => dt.Status == "For Billing" || dt.Status == "Billed");

            var ticketsForApproval = jobOrder.DispatchTickets
                .Count(dt => dt.Status == "For Approval");

            var ticketsDisapproved = jobOrder.DispatchTickets
                .Count(dt => dt.Status == "Disapproved");

            var ticketsWithoutTariff = jobOrder.DispatchTickets
                .Count(dt => dt.Status == "Pending" || dt.Status == "For Tariff");

            // BLOCK if tickets are in billing process
            if (ticketsForBillingOrBilled > 0)
            {
                TempData["error"] = $"Cannot cancel Job Order. {ticketsForBillingOrBilled} ticket(s) are already in the billing process (For Billing/Billed). These tickets must be completed or removed before cancellation.";
                return RedirectToAction(nameof(Details), new { id });
            }

            // WARN if there are pending approvals or disapproved tickets (only if not confirmed)
            if ((ticketsForApproval > 0 || ticketsDisapproved > 0) && !confirmed)
            {
                var warnings = new List<string>();
                if (ticketsForApproval > 0)
                {
                    warnings.Add($"{ticketsForApproval} ticket(s) pending approval will be affected");
                }

                if (ticketsDisapproved > 0)
                {
                    warnings.Add($"{ticketsDisapproved} disapproved ticket(s) need attention");
                }

                if (ticketsWithoutTariff > 0)
                {
                    warnings.Add($"{ticketsWithoutTariff} ticket(s) without tariff will be orphaned");
                }

                TempData["warning"] = "<strong>Warning: Affected Tickets</strong><br/>" +
                                      string.Join("<br/>• ", warnings) +
                                      "<br/><br/>Are you sure you want to proceed?";
                return RedirectToAction(nameof(Details), new { id });
            }

            var currentUser = await GetCurrentUserAsync();

            jobOrder.Status = JobOrderStatus.Cancelled;

            // Build detailed audit message
            var auditDetails = new List<string>();
            if (ticketsForApproval > 0)
            {
                auditDetails.Add($"{ticketsForApproval} for approval");
            }

            if (ticketsDisapproved > 0)
            {
                auditDetails.Add($"{ticketsDisapproved} disapproved");
            }

            if (ticketsWithoutTariff > 0)
            {
                auditDetails.Add($"{ticketsWithoutTariff} without tariff");
            }

            var auditMessage = $"Cancelled Job Order #{jobOrder.JobOrderNumber}";
            if (auditDetails.Any())
            {
                auditMessage += $". Affected tickets: {string.Join(", ", auditDetails)}";
            }

            await RecordAuditAsync(
                activity: auditMessage,
                username: currentUser.UserName!,
                cancellationToken: cancellationToken);

            await unitOfWork.SaveAsync(cancellationToken);

            TempData["success"] = $"Job Order #{jobOrder.JobOrderNumber} has been cancelled.";
            return RedirectToAction(nameof(Details), new { id = jobOrder.JobOrderId });
        }

        #endregion

        #region Close

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Close(int id, bool confirmed = false, CancellationToken cancellationToken = default)
        {
            if (!await AccessControl.HasAccessAsync(GetUserId(), ProcedureEnum.CloseJobOrder))
            {
                TempData["error"] = "Access denied. You don't have permission to close Job Orders.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var jobOrder = await unitOfWork.JobOrder.GetJobOrderWithDetailsAsync(id, cancellationToken);
            if (jobOrder == null)
            {
                return NotFound();
            }

            if (jobOrder.Status == JobOrderStatus.Closed)
            {
                TempData["error"] = $"Job Order #{jobOrder.JobOrderNumber} is already closed.";
                return RedirectToAction(nameof(Details), new { id });
            }

            // Validate dispatch tickets before closing
            if (jobOrder.DispatchTickets.Any())
            {
                var ticketsWithoutTariff = jobOrder.DispatchTickets
                    .Count(dt => dt.Status == "Pending" || dt.Status == "For Tariff");

                var ticketsForApproval = jobOrder.DispatchTickets
                    .Count(dt => dt.Status == "For Approval");

                var ticketsDisapproved = jobOrder.DispatchTickets
                    .Count(dt => dt.Status == "Disapproved");

                // Block closing if there are tickets without tariff
                if (ticketsWithoutTariff > 0)
                {
                    TempData["error"] = $"Cannot close Job Order. {ticketsWithoutTariff} dispatch ticket(s) have no tariff set. Please set tariff rates for all tickets before closing.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                // Block closing if there are disapproved tickets
                if (ticketsDisapproved > 0)
                {
                    TempData["error"] = $"Cannot close Job Order. {ticketsDisapproved} dispatch ticket(s) are disapproved. Please edit and re-approve all disapproved tickets before closing.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                // Warn if there are tickets pending approval (only proceed if user confirmed)
                if (ticketsForApproval > 0 && !confirmed)
                {
                    TempData["warning"] = $"Warning: {ticketsForApproval} dispatch ticket(s) are pending approval. These tickets will not be included in billing until approved. Are you sure you want to close this Job Order?";
                    return RedirectToAction(nameof(Details), new { id });
                }
            }

            var currentUser = await GetCurrentUserAsync();

            jobOrder.Status = JobOrderStatus.Closed;

            await RecordAuditAsync(
                activity: $"Closed Job Order #{jobOrder.JobOrderNumber}",
                username: currentUser.UserName!,
                cancellationToken: cancellationToken);

            await unitOfWork.SaveAsync(cancellationToken);

            TempData["success"] = $"Job Order #{jobOrder.JobOrderNumber} has been closed.";
            return RedirectToAction(nameof(Details), new { id = jobOrder.JobOrderId });
        }

        #endregion

        #region AJAX Endpoints

        [HttpGet]
        public async Task<IActionResult> ChangeTerminal(int portId, CancellationToken cancellationToken)
        {
            var terminals = await unitOfWork.Terminal.GetAllAsync(t => t.PortId == portId, cancellationToken);

            var list = terminals
                .OrderBy(t => t.TerminalName)
                .Select(t => new SelectListItem
                {
                    Value = t.TerminalId.ToString(),
                    Text  = t.TerminalName
                });

            return Json(list);
        }

        [HttpGet]
        public async Task<IActionResult> GetTicketDetails(int id, CancellationToken cancellationToken)
        {
            var ticket = await unitOfWork.DispatchTicket.GetAsync(dt => dt.DispatchTicketId == id, cancellationToken);
            if (ticket == null)
            {
                return NotFound();
            }

            return Json(new
            {
                id = ticket.DispatchTicketId,
                dispatchNumber = ticket.DispatchNumber,
                date           = ticket.Date?.ToString("MMM dd, yyyy") ?? "-",
                serviceName    = ticket.Service?.ServiceName,
                tugboatName    = ticket.Tugboat?.TugboatName,
                tugMasterName  = ticket.TugMaster?.TugMasterName,
                location       = ticket.Terminal != null
                    ? $"{ticket.Terminal.Port?.PortName} - {ticket.Terminal.TerminalName}"
                    : "N/A",
                timeStart = ticket.DateLeft.HasValue && ticket.TimeLeft.HasValue
                    ? $"{ticket.DateLeft.Value:MMM dd, yyyy} {ticket.TimeLeft.Value:HH:mm}"
                    : "-",
                timeEnd = ticket.DateArrived.HasValue && ticket.TimeArrived.HasValue
                    ? $"{ticket.DateArrived.Value:MMM dd, yyyy} {ticket.TimeArrived.Value:HH:mm}"
                    : "-",
                remarks = ticket.Remarks ?? "No remarks",
                status  = ticket.Status,
                totalHours = ticket.TotalHours.ToString("N2"),

                // Tariff details (if available)
                dispatchRate = ticket.DispatchRate.ToString("N2") ?? "-",
                dispatchDiscount = ticket.DispatchDiscount.ToString("N2") ?? "0",
                dispatchBilling = ticket.DispatchBillingAmount.ToString("N2") ?? "-",
                bafRate = ticket.BAFRate.ToString("N2") ?? "-",
                bafDiscount = ticket.BAFDiscount.ToString("N2") ?? "0",
                bafBilling = ticket.BAFBillingAmount.ToString("N2") ?? "-",
                totalBilling = ticket.TotalBilling.ToString("N2") ?? "-",
                totalNetRevenue = ticket.TotalNetRevenue.ToString("N2") ?? "-"
            });
        }

        #endregion

        #region Private Helpers

        private async Task<ApplicationUser> GetCurrentUserAsync()
        {
            return await UserManager.GetUserAsync(User)
                ?? throw new InvalidOperationException("Authenticated user not found.");
        }

        private async Task<string?> GetCompanyClaimAsync()
        {
            var user   = await GetCurrentUserAsync();
            var claims = await UserManager.GetClaimsAsync(user);
            return claims.FirstOrDefault(c => c.Type == "Company")?.Value;
        }

        private IActionResult AccessDenied(string? returnUrl = null)
        {
            TempData["error"] = "Access denied. You don't have permission to perform this action.";

            // If return URL is provided (for modal actions), redirect back there
            if (!string.IsNullOrEmpty(returnUrl))
            {
                return Redirect(returnUrl);
            }

            // Otherwise redirect to Home
            return RedirectToAction("Index", "Home", new { area = "User" });
        }

        private async Task RecordAuditAsync(
            string activity,
            string username,
            CancellationToken cancellationToken)
        {
            var companyClaim = await GetCompanyClaimAsync()
                ?? throw new InvalidOperationException("Company claim not found.");

            var audit = new AuditTrail(username, activity, "Job Order", companyClaim);

            await unitOfWork.AuditTrail.AddAsync(audit, cancellationToken);
        }

        private static JobOrderViewModel MapToViewModel(JobOrder jobOrder) => new()
        {
            JobOrderId     = jobOrder.JobOrderId,
            JobOrderNumber = jobOrder.JobOrderNumber,
            Date           = jobOrder.Date,
            Status         = jobOrder.Status,
            CustomerId     = jobOrder.CustomerId,
            VesselId       = jobOrder.VesselId,
            PortId         = jobOrder.PortId,
            TerminalId     = jobOrder.TerminalId,
            COSNumber      = jobOrder.COSNumber,
            VoyageNumber   = jobOrder.VoyageNumber,
            Remarks        = jobOrder.Remarks
        };

        private async Task PopulateSelectListsAsync(JobOrderViewModel viewModel, CancellationToken cancellationToken)
        {
            var companyClaim = await GetCompanyClaimAsync();

            viewModel.Customers = await unitOfWork.GetCustomerListAsyncById(companyClaim!, cancellationToken);

            var vessels = await unitOfWork.Vessel.GetAllAsync(cancellationToken: cancellationToken);
            viewModel.Vessels = vessels
                .OrderBy(v => v.VesselName)
                .Select(v => new SelectListItem
                {
                    Value = v.VesselId.ToString(),
                    Text  = $"{v.VesselName} ({v.VesselType})"
                })
                .ToList();

            var ports = await unitOfWork.Port.GetAllAsync(cancellationToken: cancellationToken);
            viewModel.Ports = ports
                .OrderBy(p => p.PortName)
                .Select(p => new SelectListItem
                {
                    Value = p.PortId.ToString(),
                    Text  = p.PortName
                })
                .ToList();

            viewModel.Terminals = viewModel.PortId.HasValue
                ? (await unitOfWork.Terminal.GetAllAsync(t => t.PortId == viewModel.PortId, cancellationToken: cancellationToken))
                    .OrderBy(t => t.TerminalName)
                    .Select(t => new SelectListItem
                    {
                        Value = t.TerminalId.ToString(),
                        Text  = t.TerminalName
                    })
                    .ToList()
                : new List<SelectListItem>();
        }

        #endregion
    }

    #region Status Constants

    public static class JobOrderStatus
    {
        public const string Open      = "Open";
        public const string Closed    = "Closed";
        public const string Cancelled = "Cancelled";
    }

    #endregion
}
