using IBS.DataAccess.Data;
using IBS.Models;
using IBS.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace IBSWeb.Areas.User.Controllers
{
    [Area("User")]
    public class HomeController(
        ILogger<HomeController> logger,
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext dbContext)
        : Controller
    {
        private readonly ILogger<HomeController> _logger = logger;

        private async Task<string?> GetCompanyClaimAsync()
        {
            var user = await userManager.GetUserAsync(User);

            if (user == null)
            {
                return string.Empty;
            }

            var claims = await userManager.GetClaimsAsync(user);
            return claims.FirstOrDefault(c => c.Type == "Company")?.Value;
        }

        public async Task<IActionResult> Index()
        {
            var findUser = await dbContext.ApplicationUsers
                .Where(user => user.Id == userManager.GetUserId(User))
                .FirstOrDefaultAsync();

            ViewBag.GetUserDepartment = findUser?.Department;
            var companyClaims = findUser != null ? await GetCompanyClaimAsync() : string.Empty;

            var dashboardCounts = new DashboardCountViewModel
            {
                #region -- MMSI

                MsapServiceRequestForPosting = await dbContext.MsapDispatchTickets
                        .Where(po => po.Status == "For Posting")
                        .CountAsync(),

                MsapDispatchTicketForTariff = await dbContext.MsapDispatchTickets
                        .Where(po => po.Status == "For Tariff")
                        .CountAsync(),

                MsapDispatchTicketForApproval = await dbContext.MsapDispatchTickets
                        .Where(po => po.Status == "For Approval")
                        .CountAsync(),

                MsapDispatchTicketForBilling = await dbContext.MsapDispatchTickets
                        .Where(po => po.Status == "For Billing")
                        .CountAsync(),

                MsapBillingForCollection = await dbContext.MsapBillings
                        .Where(po => po.Status == "For Collection")
                        .CountAsync(),

                #endregion -- MMSI
            };

            return View(dashboardCounts);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [AllowAnonymous]
        public async Task<IActionResult> Maintenance()
        {
            if (await dbContext.AppSettings
                    .Where(s => s.SettingKey == "MaintenanceMode")
                    .Select(s => s.Value == "true")
                    .FirstOrDefaultAsync())
            {
                return View("Maintenance");
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
