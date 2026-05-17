using IBS.Services;
using Microsoft.AspNetCore.Mvc;

namespace IBSWeb.Areas.User.Controllers
{
    [Area("User")]
    public class TugboatMonitoringController(ITugboatMonitoringService monitoringService) : Controller
    {
        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            var now = DateTime.Now;
            var start = new DateTime(now.Year, now.Month, 1);
            var end = start.AddMonths(1).AddSeconds(-1);
            
            var data = await monitoringService.GetTugboatTimelineDataAsync(start, end, cancellationToken);
            ViewBag.TimelineData = data;
            
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetData(DateTime date, CancellationToken cancellationToken)
        {
            // Set range to the beginning and end of the month for the provided date
            var start = new DateTime(date.Year, date.Month, 1);
            var end = start.AddMonths(1).AddSeconds(-1);
            
            var data = await monitoringService.GetTugboatTimelineDataAsync(start, end, cancellationToken);
            return Json(data);
        }
    }
}
