using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using IBS.Models.Enums;
using IBS.Models.MSAP;
using IBS.Models.MSAP.ViewModels;
using IBS.Services.Attributes;
using IBS.Utility.Helpers;
using IBSWeb.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.SignalR;
using IBS.Services;
using IBS.Utility.Constants;
using Microsoft.Extensions.Logging;

namespace IBSWeb.Areas.User.Controllers
{
    /// <summary>
    /// Controller for managing Job Orders in the MMSI system.
    /// </summary>
    [Area("User")]
    public class JobOrderController(
        IUnitOfWork unitOfWork,
        IJobOrderService jobOrderService,
        IDispatchTicketService dispatchTicketService,
        ILogger<JobOrderController> logger,
        IHubContext<TugboatHub> hubContext) : Controller
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
        public IActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// Returns a paged, filtered, sorted list of Job Orders for DataTables server-side processing.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequireAnyAccess(
            "Access denied.",
            ProcedureEnum.CreateJobOrder,
            ProcedureEnum.EditJobOrder,
            ProcedureEnum.DeleteJobOrder,
            ProcedureEnum.CloseJobOrder)]
        public async Task<IActionResult> GetJobOrderList([FromForm] DataTablesParameters parameters, CancellationToken cancellationToken)
        {
            try
            {
                var (data, filtered, total) = await jobOrderService.GetPagedJobOrdersAsync(parameters, cancellationToken);

                return Json(new
                {
                    draw = parameters.Draw,
                    recordsTotal = total,
                    recordsFiltered = filtered,
                    data
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to get job orders.");
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
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
            var viewModel = await jobOrderService.PopulateJobOrderViewModelAsync(null, cancellationToken);

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
                await jobOrderService.PopulateJobOrderViewModelAsync(viewModel, cancellationToken);
                return View(viewModel);
            }

            var jobOrder = new JobOrder
            {
                Date = viewModel.Date,
                CustomerId = viewModel.CustomerId,
                VesselId = viewModel.VesselId,
                PortId = viewModel.PortId,
                TerminalId = viewModel.TerminalId,
                COSNumber = viewModel.COSNumber,
                VoyageNumber = viewModel.VoyageNumber,
                PlannedStartTime = viewModel.PlannedStartTime,
                PlannedEndTime = viewModel.PlannedEndTime,
                PreferredTugboatId = viewModel.PreferredTugboatId,
                RequiredTugCount = viewModel.RequiredTugCount,
                IsConfirmed = viewModel.IsConfirmed,
                Remarks = viewModel.Remarks
            };

            var result = await jobOrderService.CreateJobOrderAsync(jobOrder, User.Identity?.Name ?? "Unknown", cancellationToken);

            if (result.IsSuccess)
            {
                await hubContext.Clients.All.SendAsync("TimelineChanged", cancellationToken);
                TempData["success"] = result.Message;
                return RedirectToAction(nameof(Details), new { id = result.Data });
            }

            ModelState.AddModelError(string.Empty, result.Message ?? "An error occurred.");
            await jobOrderService.PopulateJobOrderViewModelAsync(viewModel, cancellationToken);
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
            var jobOrder = await jobOrderService.GetJobOrderByIdAsync(id, cancellationToken);
            if (jobOrder == null)
            {
                return NotFound();
            }

            var ticketViewModel = await dispatchTicketService.PopulateServiceRequestViewModelAsync(null, id, cancellationToken);
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
            var jobOrder = await jobOrderService.GetJobOrderByIdAsync(id, cancellationToken);
            if (jobOrder == null)
            {
                return NotFound();
            }

            ViewData["JobOrderNumber"] = jobOrder.JobOrderNumber;

            // Prevent editing if the Job Order is already Closed.
            if (jobOrder.Status == SD.JobOrderStatus.Closed)
            {
                TempData["error"] = $"Job Order #{jobOrder.JobOrderNumber} is closed and cannot be edited.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var viewModel = new JobOrderViewModel
            {
                JobOrderId = jobOrder.JobOrderId,
                Date = jobOrder.Date,
                CustomerId = jobOrder.CustomerId,
                VesselId = jobOrder.VesselId,
                PortId = jobOrder.PortId,
                TerminalId = jobOrder.TerminalId,
                COSNumber = jobOrder.COSNumber,
                VoyageNumber = jobOrder.VoyageNumber,
                PlannedStartTime = jobOrder.PlannedStartTime,
                PlannedEndTime = jobOrder.PlannedEndTime,
                PreferredTugboatId = jobOrder.PreferredTugboatId,
                RequiredTugCount = jobOrder.RequiredTugCount,
                IsConfirmed = jobOrder.IsConfirmed,
                Remarks = jobOrder.Remarks
            };

            await jobOrderService.PopulateJobOrderViewModelAsync(viewModel, cancellationToken);
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
                await jobOrderService.PopulateJobOrderViewModelAsync(viewModel, cancellationToken);
                return View(viewModel);
            }

            var jobOrder = new JobOrder
            {
                JobOrderId = viewModel.JobOrderId,
                Date = viewModel.Date,
                CustomerId = viewModel.CustomerId,
                VesselId = viewModel.VesselId,
                PortId = viewModel.PortId,
                TerminalId = viewModel.TerminalId,
                COSNumber = viewModel.COSNumber,
                VoyageNumber = viewModel.VoyageNumber,
                PlannedStartTime = viewModel.PlannedStartTime,
                PlannedEndTime = viewModel.PlannedEndTime,
                PreferredTugboatId = viewModel.PreferredTugboatId,
                RequiredTugCount = viewModel.RequiredTugCount,
                IsConfirmed = viewModel.IsConfirmed,
                Remarks = viewModel.Remarks
            };

            var result = await jobOrderService.UpdateJobOrderAsync(jobOrder, User.Identity?.Name ?? "Unknown", cancellationToken);

            if (result.IsSuccess)
            {
                await hubContext.Clients.All.SendAsync("TimelineChanged", cancellationToken);
                TempData["success"] = result.Message;
                return RedirectToAction(nameof(Details), new { id = jobOrder.JobOrderId });
            }

            if (result.Status == ServiceResultStatus.NotFound)
            {
                return NotFound();
            }

            ModelState.AddModelError(string.Empty, result.Message ?? "An error occurred.");
            await jobOrderService.PopulateJobOrderViewModelAsync(viewModel, cancellationToken);
            return View(viewModel);
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
            bool forceClose = false;
            var pendingId = TempData[_closeConfirmKey] as int?;

            if (pendingId == id)
            {
                forceClose = true;
                TempData.Remove(_closeConfirmKey);
            }

            var result = await jobOrderService.CloseJobOrderAsync(id, User.Identity?.Name ?? "Unknown", forceClose, cancellationToken);

            if (result.IsSuccess)
            {
                if (result.Status == ServiceResultStatus.ConfirmationRequired)
                {
                    TempData[_closeConfirmKey] = id;
                    TempData["warning"] = result.Message;
                    return RedirectToAction(nameof(Details), new { id });
                }

                await hubContext.Clients.All.SendAsync("TimelineChanged", cancellationToken);
                TempData["success"] = result.Message;
                return RedirectToAction(nameof(Details), new { id });
            }

            if (result.Status == ServiceResultStatus.NotFound)
            {
                return NotFound();
            }

            TempData["error"] = result.Message;
            return RedirectToAction(nameof(Details), new { id });
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
                date = ticket.Date.ToString("MMM dd, yyyy"),
                serviceName = ticket.Service.ServiceName,
                tugboatName = ticket.Tugboat.TugboatName,
                tugMasterName = ticket.TugMaster?.TugMasterName,
                location = $"{ticket.Terminal.Port.PortName} - {ticket.Terminal.TerminalName}",
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

        /// <summary>
        /// Searches for customers matching a search term.
        /// </summary>
        [HttpGet]
        public async Task<JsonResult> SearchCustomers(string? term, CancellationToken cancellationToken)
        {
            var result = await jobOrderService.SearchCustomersAsync(term, cancellationToken);
            return Json(result);
        }

        #endregion
    }
}
