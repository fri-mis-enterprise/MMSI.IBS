using IBS.Models;
using IBS.Models.Enums;
using IBS.Models.MSAP;
using IBS.Models.MSAP.ViewModels;
using IBS.Services.Attributes;
using IBS.Utility.Helpers;
using IBSWeb.Hubs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using IBS.Services;
using IBS.Utility.Constants;

namespace IBSWeb.Areas.User.Controllers
{
    /// <summary>
    /// Controller for managing Job Orders in the MMSI system.
    /// </summary>
    [Area("User")]
    public class JobOrderController(
        IJobOrderService jobOrderService,
        IDispatchTicketService dispatchTicketService,
        ITerminalService terminalService,
        ILogger<JobOrderController> logger,
        IHubContext<TugboatHub> hubContext,
        IHubContext<PlanningHub> planningHubContext) : Controller
    {
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

        #region AJAX Endpoints

        /// <summary>
        /// Retrieves terminals associated with a specific port. Used for dynamic dropdowns.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> ChangeTerminal(int portId, CancellationToken cancellationToken)
        {
            var list = await terminalService.GetTerminalsByPortAsync(portId, cancellationToken);
            return Json(list);
        }

        /// <summary>
        /// Retrieves detailed information for a specific Dispatch Ticket.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetTicketDetails(int id, CancellationToken cancellationToken)
        {
            var details = await dispatchTicketService.GetTicketDetailsAsync(id, cancellationToken);
            if (details == null)
            {
                return NotFound();
            }

            return Json(details);
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

        /// <summary>
        /// Assigns a preferred tugboat to a Job Order.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> AssignTugboat(int jobOrderId, int tugboatId, CancellationToken cancellationToken)
        {
            var result = await jobOrderService.AssignTugboatAsync(jobOrderId, tugboatId, User.Identity?.Name ?? "Unknown", cancellationToken);

            if (result.IsSuccess)
            {
                // Notify planning subscribers
                var jobOrder = await jobOrderService.GetJobOrderByIdAsync(jobOrderId, cancellationToken);
                if (jobOrder is { PortId: > 0 })
                {
                    await hubContext.Clients.All.SendAsync("TimelineChanged", cancellationToken);
                    await planningHubContext.Clients.All.SendAsync("OnPlanUpdated", jobOrder.PortId, cancellationToken);
                }

                return Json(new { success = true });
            }

            return Json(new { success = false, message = result.Message });
        }

        /// <summary>
        /// Unassigns a tugboat from a Job Order.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> UnassignTugboat(int jobOrderId, int tugboatId, CancellationToken cancellationToken)
        {
            var result = await jobOrderService.UnassignTugboatAsync(jobOrderId, tugboatId, User.Identity?.Name ?? "Unknown", cancellationToken);

            if (result.IsSuccess)
            {
                var jobOrder = await jobOrderService.GetJobOrderByIdAsync(jobOrderId, cancellationToken);
                if (jobOrder is { PortId: > 0 })
                {
                    await hubContext.Clients.All.SendAsync("TimelineChanged", cancellationToken);
                    await planningHubContext.Clients.All.SendAsync("OnPlanUpdated", jobOrder.PortId, cancellationToken);
                }

                return Json(new { success = true });
            }

            return Json(new { success = false, message = result.Message });
        }

        #endregion
    }
}
