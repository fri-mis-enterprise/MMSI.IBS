using IBS.DataAccess.Repository.IRepository;
using IBS.Models.Enums;
using IBS.Models.MMSI;
using IBS.Models.MMSI.ViewModels;
using IBS.Models;
using IBS.Services.Attributes;
using IBS.Utility.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IBSWeb.Areas.User.Controllers
{
    /// <summary>
    /// Controller for managing Job Orders in the MMSI system.
    /// </summary>
    [Area("User")]
    public class JobOrderController(
        IUnitOfWork unitOfWork,
        ILogger<JobOrderController> logger) : Controller
    {
        private const string _closeConfirmKey = "JobOrder_PendingCloseId";

        #region Index

        /// <summary>
        /// Displays the list of all Job Orders.
        /// </summary>
        [RequireAnyAccess(
            "Access denied. You don't have permission to access Job Orders.",
            ProcedureEnum.CreateJobOrder,
            ProcedureEnum.EditJobOrder,
            ProcedureEnum.DeleteJobOrder,
            ProcedureEnum.CloseJobOrder)]
        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            var jobOrders = await unitOfWork.JobOrder.GetAllJobOrdersWithDetailsAsync(cancellationToken);

            var createViewModel = new JobOrderViewModel();
            await PopulateSelectListsAsync(createViewModel, cancellationToken);
            ViewBag.CreateViewModel = createViewModel;

            return View(jobOrders
                .OrderByDescending(j => j.JobOrderNumber)
                .ToList());
        }

        #endregion

        #region Create

        /// <summary>
        /// Displays the form to create a new Job Order.
        /// </summary>
        [HttpGet]
        [RequireAccess(ProcedureEnum.CreateJobOrder, "Access denied. You don't have permission to create Job Orders.")]
        public async Task<IActionResult> Create(CancellationToken cancellationToken)
        {
            var viewModel = new JobOrderViewModel();
            await PopulateSelectListsAsync(viewModel, cancellationToken);

            return View(viewModel);
        }

        /// <summary>
        /// Processes the creation of a new Job Order.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequireAccess(ProcedureEnum.CreateJobOrder, "Access denied. You don't have permission to create Job Orders.")]
        public async Task<IActionResult> Create(JobOrderViewModel viewModel, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                await PopulateSelectListsAsync(viewModel, cancellationToken);
                return View(viewModel);
            }

            try
            {
                var jobOrder = new JobOrder
                {
                    Date = viewModel.Date,
                    CustomerId = viewModel.CustomerId,
                    VesselId = viewModel.VesselId,
                    PortId = viewModel.PortId,
                    TerminalId = viewModel.TerminalId,
                    COSNumber = viewModel.COSNumber,
                    VoyageNumber = viewModel.VoyageNumber,
                    Remarks = viewModel.Remarks,
                    Status = JobOrderStatus.Open,
                    JobOrderNumber = await unitOfWork.JobOrder.GenerateJobOrderNumber(cancellationToken),
                    CreatedBy = User.Identity?.Name ?? "Unknown",
                    CreatedDate = DateTimeHelper.GetCurrentPhilippineTime()
                };

                await unitOfWork.JobOrder.AddAsync(jobOrder, cancellationToken);

                await RecordAuditAsync(
                    activity: $"Created Job Order #{jobOrder.JobOrderNumber}",
                    username: User.Identity?.Name ?? "Unknown",
                    cancellationToken: cancellationToken);

                await unitOfWork.SaveAsync(cancellationToken);

                TempData["success"] = $"Job Order #{jobOrder.JobOrderNumber} created successfully.";
                return RedirectToAction(nameof(Details), new { id = jobOrder.JobOrderId });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error creating Job Order");
                ModelState.AddModelError(string.Empty, "An unexpected error occurred while creating the Job Order.");
            }

            await PopulateSelectListsAsync(viewModel, cancellationToken);
            return View(viewModel);
        }

        #endregion

        #region Details

        /// <summary>
        /// Displays the details of a specific Job Order.
        /// </summary>
        [RequireAnyAccess(
            "Access denied. You don't have permission to view Job Orders.",
            ProcedureEnum.CreateJobOrder,
            ProcedureEnum.EditJobOrder,
            ProcedureEnum.DeleteJobOrder,
            ProcedureEnum.CloseJobOrder)]
        public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
        {
            var jobOrder = await unitOfWork.JobOrder.GetJobOrderWithDetailsAsync(id, cancellationToken);
            if (jobOrder == null)
            {
                return NotFound();
            }

            var ticketViewModel = new ServiceRequestViewModel
            {
                JobOrderId = jobOrder.JobOrderId,
                CustomerId = jobOrder.CustomerId,
                VesselId = jobOrder.VesselId,
                PortId = jobOrder.PortId,
                TerminalId = jobOrder.TerminalId,
                COSNumber = jobOrder.COSNumber,
                VoyageNumber = jobOrder.VoyageNumber,
                Date = jobOrder.Date
            };

            ticketViewModel = await unitOfWork.ServiceRequest.GetDispatchTicketSelectLists(ticketViewModel, cancellationToken);
            ViewData["TicketViewModel"] = ticketViewModel;

            return View(jobOrder);
        }

        #endregion

        #region Edit

        /// <summary>
        /// Displays the form to edit an existing Job Order.
        /// </summary>
        [HttpGet]
        [RequireAccess(ProcedureEnum.EditJobOrder, "Access denied. You don't have permission to edit Job Orders.")]
        public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
        {
            var jobOrder = await unitOfWork.JobOrder.GetJobOrderWithDetailsAsync(id, cancellationToken);
            if (jobOrder == null)
            {
                return NotFound();
            }

            // Prevent editing if the Job Order is already Closed or Cancelled.
            if (jobOrder.Status == JobOrderStatus.Closed || jobOrder.Status == JobOrderStatus.Cancelled)
            {
                TempData["error"] = $"Job Order #{jobOrder.JobOrderNumber} is {jobOrder.Status.ToLower()} and cannot be edited.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var viewModel = MapToViewModel(jobOrder);
            await PopulateSelectListsAsync(viewModel, cancellationToken);

            ViewData["HasTickets"] = jobOrder.DispatchTickets.Any();

            return View(viewModel);
        }

        /// <summary>
        /// Processes the update of an existing Job Order.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequireAccess(ProcedureEnum.EditJobOrder, "Access denied. You don't have permission to edit Job Orders.")]
        public async Task<IActionResult> Edit(JobOrderViewModel viewModel, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                await PopulateSelectListsAsync(viewModel, cancellationToken);
                return View(viewModel);
            }

            try
            {
                var jobOrder = await unitOfWork.JobOrder.GetAsync(j => j.JobOrderId == viewModel.JobOrderId, cancellationToken);
                if (jobOrder == null)
                {
                    return NotFound();
                }

                jobOrder.Date = viewModel.Date;
                jobOrder.CustomerId = viewModel.CustomerId;
                jobOrder.VesselId = viewModel.VesselId;
                jobOrder.PortId = viewModel.PortId;
                jobOrder.TerminalId = viewModel.TerminalId;
                jobOrder.COSNumber = viewModel.COSNumber;
                jobOrder.VoyageNumber = viewModel.VoyageNumber;
                jobOrder.Remarks = viewModel.Remarks;
                jobOrder.EditedBy = User.Identity?.Name ?? "Unknown";
                jobOrder.EditedDate = DateTimeHelper.GetCurrentPhilippineTime();

                await RecordAuditAsync(
                    activity: $"Edited Job Order #{jobOrder.JobOrderNumber}",
                    username: User.Identity?.Name ?? "Unknown",
                    cancellationToken: cancellationToken);

                await unitOfWork.SaveAsync(cancellationToken);

                TempData["success"] = $"Job Order #{jobOrder.JobOrderNumber} updated successfully.";
                return RedirectToAction(nameof(Details), new { id = jobOrder.JobOrderId });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error updating Job Order {JobOrderId}", viewModel.JobOrderId);
                ModelState.AddModelError(string.Empty, "An unexpected error occurred while updating the Job Order.");
            }

            await PopulateSelectListsAsync(viewModel, cancellationToken);
            return View(viewModel);
        }

        #endregion

        #region Cancel

        /// <summary>
        /// Cancels an Open Job Order. Only available to Admin users.
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpPost, ActionName("Cancel")]
        [ValidateAntiForgeryToken]
        [RequireAccess(ProcedureEnum.DeleteJobOrder, "You don't have permission to cancel Job Orders.")]
        public async Task<IActionResult> CancelConfirmed(int id, CancellationToken cancellationToken = default)
        {
            var jobOrder = await unitOfWork.JobOrder.GetJobOrderWithDetailsAsync(id, cancellationToken);
            if (jobOrder == null)
            {
                return NotFound();
            }

            switch (jobOrder.Status)
            {
                case JobOrderStatus.Cancelled:
                    TempData["error"] = $"Job Order #{jobOrder.JobOrderNumber} is already cancelled.";
                    return RedirectToAction(nameof(Details), new { id });
                case JobOrderStatus.Closed:
                    TempData["error"] = $"Job Order #{jobOrder.JobOrderNumber} is closed and cannot be cancelled.";
                    return RedirectToAction(nameof(Details), new { id });
            }

            // Check if any associated tickets are already Billed or For Billing.
            var ticketsForBillingOrBilled = jobOrder.DispatchTickets
                .Count(dt => dt.Status == "For Billing" || dt.Status == "Billed");

            if (ticketsForBillingOrBilled > 0)
            {
                TempData["error"] = $"Cannot cancel Job Order. {ticketsForBillingOrBilled} ticket(s) are already in the billing process.";
                return RedirectToAction(nameof(Details), new { id });
            }

            jobOrder.Status = JobOrderStatus.Cancelled;

            await RecordAuditAsync($"Cancelled Job Order #{jobOrder.JobOrderNumber}",
                User.Identity?.Name ?? "Unknown",
                cancellationToken);

            await unitOfWork.SaveAsync(cancellationToken);

            TempData["success"] = $"Job Order #{jobOrder.JobOrderNumber} has been cancelled.";
            return RedirectToAction(nameof(Details), new { id = jobOrder.JobOrderId });
        }

        #endregion

        #region Close

        /// <summary>
        /// Closes a Job Order, marking it as ready for billing.
        /// Includes validation for dispatch ticket statuses.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequireAccess(ProcedureEnum.CloseJobOrder, "Access denied. You don't have permission to close Job Orders.")]
        public async Task<IActionResult> Close(int id, CancellationToken cancellationToken = default)
        {
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

            if (jobOrder.DispatchTickets.Any())
            {
                // Ensure all tickets have tariff rates set and are not disapproved.
                var ticketsWithoutTariff = jobOrder.DispatchTickets
                    .Count(dt => dt.Status == "Pending" || dt.Status == "For Tariff");

                var ticketsForApproval = jobOrder.DispatchTickets
                    .Count(dt => dt.Status == "For Approval");

                var ticketsDisapproved = jobOrder.DispatchTickets
                    .Count(dt => dt.Status == "Disapproved");

                if (ticketsWithoutTariff > 0)
                {
                    TempData["error"] = $"Cannot close Job Order. {ticketsWithoutTariff} dispatch ticket(s) have no tariff set. Please set tariff rates for all tickets before closing.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                if (ticketsDisapproved > 0)
                {
                    TempData["error"] = $"Cannot close Job Order. {ticketsDisapproved} dispatch ticket(s) are disapproved. Please edit and re-approve all disapproved tickets before closing.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                // Server-side confirmation gate for tickets pending approval.
                // We use TempData to track the first attempt and show a warning.
                if (ticketsForApproval > 0)
                {
                    var pendingId = TempData[_closeConfirmKey] as int?;

                    if (pendingId != id)
                    {
                        // First visit — store intent and redirect back to Details with warning.
                        TempData[_closeConfirmKey] = id;
                        TempData["warning"] = $"Warning: {ticketsForApproval} dispatch ticket(s) are pending approval. These tickets will not be included in billing until approved. Please confirm below to proceed.";
                        return RedirectToAction(nameof(Details), new { id });
                    }

                    // Second visit — confirmed by the user clicking the button again.
                    TempData.Remove(_closeConfirmKey);
                }
            }

            jobOrder.Status = JobOrderStatus.Closed;

            await RecordAuditAsync(
                activity: $"Closed Job Order #{jobOrder.JobOrderNumber}",
                username: User.Identity?.Name ?? "Unknown",
                cancellationToken: cancellationToken);

            await unitOfWork.SaveAsync(cancellationToken);

            TempData["success"] = $"Job Order #{jobOrder.JobOrderNumber} has been closed.";
            return RedirectToAction(nameof(Details), new { id = jobOrder.JobOrderId });
        }

        #endregion

        #region AJAX Endpoints

        /// <summary>
        /// Retrieves terminals associated with a specific port. Used for dynamic dropdowns.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> ChangeTerminal(int portId, CancellationToken cancellationToken)
        {
            var terminals = await unitOfWork.Terminal.GetAllAsync(t => t.PortId == portId, cancellationToken);

            var list = terminals
                .OrderBy(t => t.TerminalName)
                .Select(t => new SelectListItem
                {
                    Value = t.TerminalId.ToString(),
                    Text = t.TerminalName
                });

            return Json(list);
        }

        /// <summary>
        /// Retrieves detailed information for a specific Dispatch Ticket.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetTicketDetails(int id, CancellationToken cancellationToken)
        {
            // Use GetDispatchTicketWithDetailsAsync to ensure navigation properties
            // (Service, Tugboat, TugMaster, Terminal, Port) are loaded.
            var ticket = await unitOfWork.DispatchTicket.GetDispatchTicketWithDetailsAsync(id, cancellationToken);
            if (ticket == null)
            {
                return NotFound();
            }

            return Json(new
            {
                id = ticket.DispatchTicketId,
                dispatchNumber = ticket.DispatchNumber,
                date = ticket.Date?.ToString("MMM dd, yyyy") ?? "-",
                serviceName = ticket.Service.ServiceName,
                tugboatName = ticket.Tugboat.TugboatName,
                tugMasterName = ticket.TugMaster?.TugMasterName,
                location = $"{ticket.Terminal.Port?.PortName} - {ticket.Terminal.TerminalName}",
                timeStart = ticket is { DateLeft: not null, TimeLeft: not null }
                    ? $"{ticket.DateLeft.Value:MMM dd, yyyy} {ticket.TimeLeft.Value:HH:mm}"
                    : "-",
                timeEnd = ticket is { DateArrived: not null, TimeArrived: not null }
                    ? $"{ticket.DateArrived.Value:MMM dd, yyyy} {ticket.TimeArrived.Value:HH:mm}"
                    : "-",
                remarks = ticket.Remarks ?? "No remarks",
                status = ticket.Status,
                totalHours = ticket.TotalHours.ToString("N2"),
                dispatchRate = ticket.DispatchRate.ToString("N2"),
                dispatchDiscount = ticket.DispatchDiscount.ToString("N2"),
                dispatchBilling = ticket.DispatchBillingAmount.ToString("N2"),
                bafRate = ticket.BAFRate.ToString("N2"),
                bafDiscount = ticket.BAFDiscount.ToString("N2"),
                bafBilling = ticket.BAFBillingAmount.ToString("N2"),
                totalBilling = ticket.TotalBilling.ToString("N2"),
                totalNetRevenue = ticket.TotalNetRevenue.ToString("N2")
            });
        }

        #endregion

        #region Private Helpers

        /// <summary>
        /// Records an activity in the audit trail.
        /// </summary>
        private async Task RecordAuditAsync(
            string activity,
            string username,
            CancellationToken cancellationToken)
        {
            var audit = new AuditTrail(username, activity, "Job Order");

            await unitOfWork.AuditTrail.AddAsync(audit, cancellationToken);
        }

        /// <summary>
        /// Maps a JobOrder entity to a JobOrderViewModel.
        /// </summary>
        private static JobOrderViewModel MapToViewModel(JobOrder jobOrder) => new()
        {
            JobOrderId = jobOrder.JobOrderId,
            JobOrderNumber = jobOrder.JobOrderNumber,
            Date = jobOrder.Date,
            Status = jobOrder.Status,
            CustomerId = jobOrder.CustomerId,
            VesselId = jobOrder.VesselId,
            PortId = jobOrder.PortId,
            TerminalId = jobOrder.TerminalId,
            COSNumber = jobOrder.COSNumber,
            VoyageNumber = jobOrder.VoyageNumber,
            Remarks = jobOrder.Remarks
        };

        /// <summary>
        /// Populates the dropdown lists in the JobOrderViewModel.
        /// </summary>
        private async Task PopulateSelectListsAsync(JobOrderViewModel viewModel, CancellationToken cancellationToken)
        {
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
        }

        #endregion
    }

    /// <summary>
    /// Constants for Job Order statuses.
    /// </summary>
    public static class JobOrderStatus
    {
        public const string Open = "Open";
        public const string Closed = "Closed";
        public const string Cancelled = "Cancelled";
    }
}
