using IBS.DataAccess.Repository.IRepository;
using IBS.Models.Enums;
using IBS.Models.MMSI;
using IBS.Models.MMSI.ViewModels;
using IBS.Models;
using IBS.Services.AccessControl;
using IBS.Services.Attributes;
using IBS.Utility.Constants;
using IBS.Utility.Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IBSWeb.Areas.User.Controllers
{
    [Area("User")]
    [CompanyAuthorize(SD.Company_MMSI)]
    public class JobOrderController : MmsiBaseController
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<JobOrderController> _logger;

        public JobOrderController(
            IAccessControlService accessControl,
            UserManager<ApplicationUser> userManager,
            IUnitOfWork unitOfWork,
            ILogger<JobOrderController> logger)
            : base(accessControl, userManager)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        #region Index

        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            if (!await HasJobOrderAccessAsync())
                return AccessDenied();

            var jobOrders = await _unitOfWork.JobOrder.GetAllJobOrdersWithDetailsAsync(cancellationToken);

            return View(jobOrders
                .OrderByDescending(j => j.JobOrderNumber)
                .ToList());
        }

        #endregion

        #region Create

        [HttpGet]
        public async Task<IActionResult> Create(CancellationToken cancellationToken)
        {
            if (!await AccessControl.HasAccessAsync(GetUserId(), ProcedureEnum.CreateJobOrder))
                return AccessDenied();

            var viewModel = new JobOrderViewModel();
            await PopulateSelectListsAsync(viewModel, cancellationToken);

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(JobOrderViewModel viewModel, CancellationToken cancellationToken)
        {
            if (!await AccessControl.HasAccessAsync(GetUserId(), ProcedureEnum.CreateJobOrder))
                return AccessDenied();

            if (!ModelState.IsValid)
            {
                await PopulateSelectListsAsync(viewModel, cancellationToken);
                return View(viewModel);
            }

            try
            {
                var currentUser = await GetCurrentUserAsync();

                var jobOrder = new MMSIJobOrder
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
                    JobOrderNumber = await _unitOfWork.JobOrder.GenerateJobOrderNumber(cancellationToken),
                    CreatedBy      = currentUser.UserName ?? "Unknown",
                    CreatedDate    = DateTimeHelper.GetCurrentPhilippineTime()
                };

                await _unitOfWork.JobOrder.AddAsync(jobOrder, cancellationToken);

                await RecordAuditAsync(
                    activity: $"Created Job Order #{jobOrder.JobOrderNumber}",
                    username: currentUser.UserName!,
                    cancellationToken: cancellationToken);

                await _unitOfWork.SaveAsync(cancellationToken);

                TempData["success"] = "Job Order created successfully.";
                return RedirectToAction(nameof(Details), new { id = jobOrder.JobOrderId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating Job Order");
                TempData["error"] = "Error creating Job Order.";
            }

            await PopulateSelectListsAsync(viewModel, cancellationToken);
            return View(viewModel);
        }

        #endregion

        #region Details

        public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
        {
            if (!await HasJobOrderAccessAsync())
                return AccessDenied();

            var jobOrder = await _unitOfWork.JobOrder.GetJobOrderWithDetailsAsync(id, cancellationToken);
            if (jobOrder == null)
                return NotFound();

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

            ticketViewModel = await _unitOfWork.ServiceRequest.GetDispatchTicketSelectLists(ticketViewModel, cancellationToken);
            ticketViewModel.Customers = await _unitOfWork.GetCustomerListAsyncById(companyClaim!, cancellationToken);

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
                return AccessDenied();

            var jobOrder = await _unitOfWork.JobOrder.GetAsync(j => j.JobOrderId == id, cancellationToken);
            if (jobOrder == null)
                return NotFound();

            var viewModel = MapToViewModel(jobOrder);
            await PopulateSelectListsAsync(viewModel, cancellationToken);

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(JobOrderViewModel viewModel, CancellationToken cancellationToken)
        {
            if (!await AccessControl.HasAccessAsync(GetUserId(), ProcedureEnum.EditJobOrder))
                return AccessDenied();

            if (!ModelState.IsValid)
            {
                await PopulateSelectListsAsync(viewModel, cancellationToken);
                return View(viewModel);
            }

            try
            {
                var jobOrder = await _unitOfWork.JobOrder.GetAsync(j => j.JobOrderId == viewModel.JobOrderId, cancellationToken);
                if (jobOrder == null)
                    return NotFound();

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

                await _unitOfWork.SaveAsync(cancellationToken);

                TempData["success"] = "Job Order updated successfully.";
                return RedirectToAction(nameof(Details), new { id = jobOrder.JobOrderId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating Job Order {JobOrderId}", viewModel.JobOrderId);
                TempData["error"] = "Error updating Job Order.";
            }

            await PopulateSelectListsAsync(viewModel, cancellationToken);
            return View(viewModel);
        }

        #endregion

        #region Delete

        [HttpGet]
        public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
        {
            if (!await AccessControl.HasAccessAsync(GetUserId(), ProcedureEnum.DeleteJobOrder))
                return AccessDenied();

            var jobOrder = await _unitOfWork.JobOrder.GetJobOrderWithDetailsAsync(id, cancellationToken);
            if (jobOrder == null)
                return NotFound();

            if (jobOrder.DispatchTickets.Any())
            {
                TempData["error"] = $"Cannot delete Job Order. It has {jobOrder.DispatchTickets.Count} dispatch ticket(s) associated with it.";
                return RedirectToAction(nameof(Details), new { id });
            }

            return View(new JobOrderViewModel
            {
                JobOrderId     = jobOrder.JobOrderId,
                JobOrderNumber = jobOrder.JobOrderNumber,
                Date           = jobOrder.Date,
                CustomerName   = jobOrder.Customer?.CustomerName,
                VesselName     = jobOrder.Vessel?.VesselName
            });
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id, CancellationToken cancellationToken)
        {
            if (!await AccessControl.HasAccessAsync(GetUserId(), ProcedureEnum.DeleteJobOrder))
                return AccessDenied();

            var jobOrder = await _unitOfWork.JobOrder.GetJobOrderWithDetailsAsync(id, cancellationToken);
            if (jobOrder == null)
                return NotFound();

            if (jobOrder.DispatchTickets.Any())
            {
                TempData["error"] = "Cannot delete Job Order. It has dispatch tickets associated with it.";
                return RedirectToAction(nameof(Details), new { id });
            }

            try
            {
                var currentUser = await GetCurrentUserAsync();

                await RecordAuditAsync(
                    activity: $"Deleted Job Order #{jobOrder.JobOrderNumber}",
                    username: currentUser.UserName!,
                    cancellationToken: cancellationToken);

                await _unitOfWork.JobOrder.RemoveAsync(jobOrder, cancellationToken);
                await _unitOfWork.SaveAsync(cancellationToken);

                TempData["success"] = $"Job Order {jobOrder.JobOrderNumber} deleted successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting Job Order {JobOrderId}", id);
                TempData["error"] = "Error deleting Job Order.";
                return RedirectToAction(nameof(Details), new { id });
            }
        }

        #endregion

        #region Close

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Close(int id, bool confirmed = false, CancellationToken cancellationToken = default)
        {
            if (!await AccessControl.HasAccessAsync(GetUserId(), ProcedureEnum.CloseJobOrder))
                return AccessDenied();

            var jobOrder = await _unitOfWork.JobOrder.GetJobOrderWithDetailsAsync(id, cancellationToken);
            if (jobOrder == null)
                return NotFound();

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

            await _unitOfWork.SaveAsync(cancellationToken);

            TempData["success"] = $"Job Order #{jobOrder.JobOrderNumber} has been closed.";
            return RedirectToAction(nameof(Details), new { id = jobOrder.JobOrderId });
        }

        #endregion

        #region AJAX Endpoints

        [HttpGet]
        public async Task<IActionResult> ChangeTerminal(int portId, CancellationToken cancellationToken)
        {
            var terminals = await _unitOfWork.Terminal.GetAllAsync(t => t.PortId == portId, cancellationToken);

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
            var ticket = await _unitOfWork.DispatchTicket.GetAsync(dt => dt.DispatchTicketId == id, cancellationToken);
            if (ticket == null)
                return NotFound();

            return Json(new
            {
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
                status  = ticket.Status
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

        private IActionResult AccessDenied()
        {
            TempData["error"] = "Access denied.";
            return RedirectToAction("Index", "Home", new { area = "User" });
        }

        private async Task RecordAuditAsync(
            string activity,
            string username,
            CancellationToken cancellationToken)
        {
            var companyClaim = await GetCompanyClaimAsync()
                ?? throw new InvalidOperationException("Company claim not found.");

            var audit = new AuditTrail
            {
                Date         = DateTimeHelper.GetCurrentPhilippineTime(),
                Username     = username,
                MachineName  = Environment.MachineName,
                Activity     = activity,
                DocumentType = "Job Order",
                Company      = companyClaim
            };

            await _unitOfWork.AuditTrail.AddAsync(audit, cancellationToken);
        }

        private static JobOrderViewModel MapToViewModel(MMSIJobOrder jobOrder) => new()
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

            viewModel.Customers = await _unitOfWork.GetCustomerListAsyncById(companyClaim!, cancellationToken);

            var vessels = await _unitOfWork.Vessel.GetAllAsync(cancellationToken: cancellationToken);
            viewModel.Vessels = vessels
                .OrderBy(v => v.VesselName)
                .Select(v => new SelectListItem
                {
                    Value = v.VesselId.ToString(),
                    Text  = $"{v.VesselName} ({v.VesselType})"
                })
                .ToList();

            var ports = await _unitOfWork.Port.GetAllAsync(cancellationToken: cancellationToken);
            viewModel.Ports = ports
                .OrderBy(p => p.PortName)
                .Select(p => new SelectListItem
                {
                    Value = p.PortId.ToString(),
                    Text  = p.PortName
                })
                .ToList();

            viewModel.Terminals = viewModel.PortId.HasValue
                ? (await _unitOfWork.Terminal.GetAllAsync(t => t.PortId == viewModel.PortId, cancellationToken: cancellationToken))
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
        public const string Open   = "Open";
        public const string Closed = "Closed";
    }

    #endregion
}