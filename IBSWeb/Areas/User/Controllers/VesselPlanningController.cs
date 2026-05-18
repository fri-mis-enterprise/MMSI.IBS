using IBS.Services;
using IBSWeb.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace IBSWeb.Areas.User.Controllers
{
    [Area("User")]
    [Authorize]
    public class VesselPlanningController(
        IVesselPlanningService planningService,
        IBS.DataAccess.Repository.IRepository.IUnitOfWork unitOfWork,
        IHubContext<PlanningHub> hubContext) : Controller
    {
        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            ViewBag.Ports = await unitOfWork.Port.GetMMSIPortsSelectList(cancellationToken);
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetData(int? portId, string? date, CancellationToken cancellationToken)
        {
            if (!DateTime.TryParse(date, out var targetDate))
            {
                targetDate = DateTime.Today;
            }

            var startOfMonth = new DateTime(targetDate.Year, targetDate.Month, 1);
            var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1).AddHours(23).AddMinutes(59).AddSeconds(59);

            var data = await planningService.GetVesselPlanningDataAsync(portId, startOfMonth, endOfMonth, cancellationToken);
            return Json(data);
        }

        [HttpPost]
        public async Task<IActionResult> UpdatePlannedTime(int id, string type, DateTime start, DateTime end, int requiredTugs, CancellationToken cancellationToken)
        {
            if (type == "JO")
            {
                var jobOrder = await unitOfWork.JobOrder.GetAsync(j => j.JobOrderId == id, cancellationToken);
                if (jobOrder != null)
                {
                    jobOrder.PlannedStartTime = start;
                    jobOrder.PlannedEndTime = end;
                    jobOrder.RequiredTugCount = requiredTugs;
                    await unitOfWork.SaveAsync(cancellationToken);

                    if (jobOrder.PortId > 0)
                    {
                        await hubContext.Clients.Group(jobOrder.PortId.ToString()).SendAsync("OnPlanUpdated", jobOrder.PortId, cancellationToken: cancellationToken);
                    }

                    return Json(new { success = true });
                }
            }

            return Json(new { success = false, message = "Update failed or not supported." });
        }
    }
}
