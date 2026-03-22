using IBS.DataAccess.Repository.IRepository;
using IBS.Models.MMSI;
using IBS.Models.MMSI.ViewModels;
using IBS.Models;
using IBS.Services.Attributes;
using IBS.Utility.Constants;
using IBS.Utility.Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace IBSWeb.Areas.User.Controllers
{
    [Area("User")]
    [CompanyAuthorize(SD.Company_MMSI)]
    public class JobOrderController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<JobOrderController> _logger;

        public JobOrderController(IUnitOfWork unitOfWork, UserManager<ApplicationUser> userManager, ILogger<JobOrderController> logger)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            var jobOrders = await _unitOfWork.JobOrder.GetAllJobOrdersWithDetailsAsync(cancellationToken);
            return View(jobOrders.OrderByDescending(j => j.JobOrderNumber).ToList());
        }

        [HttpGet]
        public async Task<IActionResult> Create(CancellationToken cancellationToken)
        {
            var viewModel = new JobOrderViewModel();
            await PopulateSelectLists(viewModel, cancellationToken);
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(JobOrderViewModel viewModel, CancellationToken cancellationToken)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var jobOrder = new MMSIJobOrder
                    {
                        Date = viewModel.Date,
                        CustomerId = viewModel.CustomerId,
                        VesselId = viewModel.VesselId,
                        PortId = viewModel.PortId,
                        TerminalId = viewModel.TerminalId,
                        COSNumber = viewModel.COSNumber,
                        VoyageNumber = viewModel.VoyageNumber,
                        Remarks = viewModel.Remarks,
                        Status = "Open",
                        JobOrderNumber = await _unitOfWork.JobOrder.GenerateJobOrderNumber(cancellationToken),
                        CreatedBy = (await _userManager.GetUserAsync(User))?.UserName ?? "Unknown",
                        CreatedDate = DateTimeHelper.GetCurrentPhilippineTime()
                    };

                    await _unitOfWork.JobOrder.AddAsync(jobOrder, cancellationToken);
                    await _unitOfWork.SaveAsync(cancellationToken);

                    TempData["success"] = "Job Order created successfully.";
                    return RedirectToAction(nameof(Details), new { id = jobOrder.JobOrderId });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating Job Order");
                    TempData["error"] = "Error creating Job Order.";
                }
            }

            await PopulateSelectLists(viewModel, cancellationToken);
            return View(viewModel);
        }
public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
{
    var jobOrder = await _unitOfWork.JobOrder.GetJobOrderWithDetailsAsync(id, cancellationToken);
    if (jobOrder == null)
    {
        return NotFound();
    }

    var viewModel = new JobOrderViewModel
    {
        JobOrderId = jobOrder.JobOrderId,
        JobOrderNumber = jobOrder.JobOrderNumber,
        Date = jobOrder.Date,
        Status = jobOrder.Status,
        CustomerId = jobOrder.CustomerId,
        CustomerName = jobOrder.Customer?.CustomerName,
        VesselId = jobOrder.VesselId,
        VesselName = jobOrder.Vessel?.VesselName,
        PortId = jobOrder.PortId,
        TerminalId = jobOrder.TerminalId,
        COSNumber = jobOrder.COSNumber,
        VoyageNumber = jobOrder.VoyageNumber,
        Remarks = jobOrder.Remarks,
        DispatchTickets = jobOrder.DispatchTickets.ToList()
    };

    // Prepare the view model for the "Add Ticket" modal
    var companyClaims = await GetCompanyClaimAsync();
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

    ticketViewModel = await _unitOfWork.ServiceRequest.GetDispatchTicketSelectLists(ticketViewModel, cancellationToken);
    ticketViewModel.Customers = await _unitOfWork.GetCustomerListAsyncById(companyClaims!, cancellationToken);

    ViewData["TicketViewModel"] = ticketViewModel;

    return View(viewModel);
}

private async Task<string?> GetCompanyClaimAsync()
{
    var user = await _userManager.GetUserAsync(User);
    return (await _userManager.GetClaimsAsync(user!)).FirstOrDefault(c => c.Type == "Company")?.Value;
}

        [HttpGet]
        public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
        {
            var jobOrder = await _unitOfWork.JobOrder.GetAsync(j => j.JobOrderId == id, cancellationToken);
            if (jobOrder == null)
            {
                return NotFound();
            }

            var viewModel = new JobOrderViewModel
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

            await PopulateSelectLists(viewModel, cancellationToken);
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(JobOrderViewModel viewModel, CancellationToken cancellationToken)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var jobOrder = await _unitOfWork.JobOrder.GetAsync(j => j.JobOrderId == viewModel.JobOrderId, cancellationToken);
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
                    jobOrder.EditedBy = (await _userManager.GetUserAsync(User))?.UserName ?? "Unknown";
                    jobOrder.EditedDate = DateTimeHelper.GetCurrentPhilippineTime();

                    await _unitOfWork.SaveAsync(cancellationToken);

                    TempData["success"] = "Job Order updated successfully.";
                    return RedirectToAction(nameof(Details), new { id = jobOrder.JobOrderId });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating Job Order");
                    TempData["error"] = "Error updating Job Order.";
                }
            }

            await PopulateSelectLists(viewModel, cancellationToken);
            return View(viewModel);
        }
        
        [HttpGet]
        public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
        {
            var jobOrder = await _unitOfWork.JobOrder.GetJobOrderWithDetailsAsync(id, cancellationToken);
            if (jobOrder == null)
            {
                return NotFound();
            }

            // Check if there are any dispatch tickets linked to this job order
            if (jobOrder.DispatchTickets.Any())
            {
                TempData["error"] = $"Cannot delete Job Order. It has {jobOrder.DispatchTickets.Count} dispatch ticket(s) associated with it.";
                return RedirectToAction(nameof(Details), new { id = id });
            }

            var viewModel = new JobOrderViewModel
            {
                JobOrderId = jobOrder.JobOrderId,
                JobOrderNumber = jobOrder.JobOrderNumber,
                Date = jobOrder.Date,
                CustomerName = jobOrder.Customer?.CustomerName,
                VesselName = jobOrder.Vessel?.VesselName
            };

            return View(viewModel);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id, CancellationToken cancellationToken)
        {
            try
            {
                var jobOrder = await _unitOfWork.JobOrder.GetAsync(j => j.JobOrderId == id, cancellationToken);
                if (jobOrder == null)
                {
                    return NotFound();
                }

                // Double-check: ensure no dispatch tickets are linked
                var dispatchTicketsCount = await _unitOfWork.DispatchTicket.GetAllAsync(dt => dt.JobOrderId == id, cancellationToken: cancellationToken)
                    .ContinueWith(t => t.Result.Count(), cancellationToken);

                if (dispatchTicketsCount > 0)
                {
                    TempData["error"] = "Cannot delete Job Order. It has dispatch tickets associated with it.";
                    return RedirectToAction(nameof(Details), new { id = id });
                }

                await _unitOfWork.JobOrder.RemoveAsync(jobOrder, cancellationToken);
                await _unitOfWork.SaveAsync(cancellationToken);

                TempData["success"] = $"Job Order {jobOrder.JobOrderNumber} deleted successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting Job Order {JobOrderId}", id);
                TempData["error"] = "Error deleting Job Order.";
                return RedirectToAction(nameof(Details), new { id = id });
            }
        }

        [HttpGet]
        public async Task<IActionResult> ChangeTerminal(int portId, CancellationToken cancellationToken)
        {
            var terminals = await _unitOfWork.Terminal.GetAllAsync(t => t.PortId == portId, cancellationToken);
            var list = terminals.Select(t => new SelectListItem
            {
                Value = t.TerminalId.ToString(),
                Text = t.TerminalName
            }).OrderBy(t => t.Text).ToList();

            return Json(list);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Close(int id, CancellationToken cancellationToken)
        {
            var jobOrder = await _unitOfWork.JobOrder.GetAsync(j => j.JobOrderId == id, cancellationToken);
            if (jobOrder == null)
            {
                return NotFound();
            }

            jobOrder.Status = "Closed";
            await _unitOfWork.SaveAsync(cancellationToken);

            #region -- Audit Trail

            var audit = new AuditTrail
            {
                Date = DateTimeHelper.GetCurrentPhilippineTime(),
                Username = await GetUserNameAsync() ?? throw new InvalidOperationException(),
                MachineName = Environment.MachineName,
                Activity = $"Closed Job Order #{jobOrder.JobOrderNumber}",
                DocumentType = "Job Order",
                Company = await GetCompanyClaimAsync() ?? throw new InvalidOperationException()
            };

            await _unitOfWork.AuditTrail.AddAsync(audit, cancellationToken);

            #endregion -- Audit Trail

            TempData["success"] = $"Job Order #{jobOrder.JobOrderNumber} has been closed.";
            return RedirectToAction(nameof(Details), new { id = jobOrder.JobOrderId });
        }

        [HttpGet]
        public async Task<IActionResult> GetTicketDetails(int id, CancellationToken cancellationToken)
        {
            var ticket = await _unitOfWork.DispatchTicket.GetAsync(dt => dt.DispatchTicketId == id, cancellationToken);
            if (ticket == null)
            {
                return NotFound();
            }

            // Manually include related data since GetAsync might not include all by default if not specified
            ticket.Service = await _unitOfWork.Service.GetAsync(s => s.ServiceId == ticket.ServiceId, cancellationToken);
            ticket.Tugboat = await _unitOfWork.Tugboat.GetAsync(t => t.TugboatId == ticket.TugBoatId, cancellationToken);
            ticket.TugMaster = await _unitOfWork.TugMaster.GetAsync(t => t.TugMasterId == ticket.TugMasterId, cancellationToken);
            ticket.Terminal = await _unitOfWork.Terminal.GetAsync(t => t.TerminalId == ticket.TerminalId, cancellationToken);
            if (ticket.Terminal != null)
            {
                ticket.Terminal.Port = await _unitOfWork.Port.GetAsync(p => p.PortId == ticket.Terminal.PortId, cancellationToken);
            }

            return Json(new
            {
                dispatchNumber = ticket.DispatchNumber,
                date = ticket.Date?.ToString("MMM dd, yyyy") ?? "-",
                serviceName = ticket.Service?.ServiceName,
                tugboatName = ticket.Tugboat?.TugboatName,
                tugMasterName = ticket.TugMaster?.TugMasterName,
                location = ticket.Terminal != null ? $"{ticket.Terminal.Port?.PortName} - {ticket.Terminal.TerminalName}" : "N/A",
                timeStart = ticket.DateLeft.HasValue && ticket.TimeLeft.HasValue 
                    ? $"{ticket.DateLeft.Value:MMM dd, yyyy} {ticket.TimeLeft.Value:HH:mm}" 
                    : "-",
                timeEnd = ticket.DateArrived.HasValue && ticket.TimeArrived.HasValue
                    ? $"{ticket.DateArrived.Value:MMM dd, yyyy} {ticket.TimeArrived.Value:HH:mm}"
                    : "-",
                remarks = ticket.Remarks ?? "No remarks",
                status = ticket.Status
            });
        }

        private async Task<string?> GetUserNameAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            return user?.UserName;
        }

        private async Task PopulateSelectLists(JobOrderViewModel viewModel, CancellationToken cancellationToken)

        {
            var user = await _userManager.GetUserAsync(User);
            var companyClaims = (await _userManager.GetClaimsAsync(user!)).FirstOrDefault(c => c.Type == "Company")?.Value;

            viewModel.Customers = await _unitOfWork.GetCustomerListAsyncById(companyClaims!, cancellationToken);

            var vessels = await _unitOfWork.Vessel.GetAllAsync(cancellationToken: cancellationToken);
            viewModel.Vessels = vessels.OrderBy(v => v.VesselName).Select(v => new SelectListItem { Value = v.VesselId.ToString(), Text = $"{v.VesselName} ({v.VesselType})" }).ToList();

            var ports = await _unitOfWork.Port.GetAllAsync(cancellationToken: cancellationToken);
            viewModel.Ports = ports.OrderBy(p => p.PortName).Select(p => new SelectListItem { Value = p.PortId.ToString(), Text = p.PortName }).ToList();

            if (viewModel.PortId.HasValue)
            {
                var terminals = await _unitOfWork.Terminal.GetAllAsync(t => t.PortId == viewModel.PortId, cancellationToken: cancellationToken);
                viewModel.Terminals = terminals.OrderBy(t => t.TerminalName).Select(t => new SelectListItem { Value = t.TerminalId.ToString(), Text = t.TerminalName }).ToList();
            }
            else
            {
                viewModel.Terminals = new List<SelectListItem>();
            }
        }
    }
}
