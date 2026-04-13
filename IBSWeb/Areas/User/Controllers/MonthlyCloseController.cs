using IBS.Models;
using IBS.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace IBSWeb.Areas.User.Controllers
{
    [Area("User")]
    [Authorize(Roles = "Admin")]
    public class MonthlyCloseController(
        ILogger<MonthlyCloseController> logger,
        IMonthlyClosureService monthlyClosureService,
        UserManager<ApplicationUser> userManager)
        : Controller
    {
        private async Task<string?> GetCompanyClaimAsync()
        {
            var user = await userManager.GetUserAsync(User);

            if (user == null)
            {
                return null;
            }

            var claims = await userManager.GetClaimsAsync(user);
            return claims.FirstOrDefault(c => c.Type == "Company")?.Value;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> TriggerMonthlyClosure(DateOnly monthDate, CancellationToken cancellationToken)
        {
            var companyClaim = await GetCompanyClaimAsync();

            if (companyClaim == null)
            {
                return BadRequest();
            }

            try
            {
                await monthlyClosureService.Execute(monthDate, companyClaim, User.Identity!.Name!, cancellationToken);

                TempData["success"] = $"Month of {monthDate:MMM yyyy} closed successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                logger.LogError(ex, "Failed to close period. Posted by: {Username}", User.Identity!.Name);
                return RedirectToAction(nameof(Index));
            }
        }

    }
}
