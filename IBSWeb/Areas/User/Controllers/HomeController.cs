using IBS.DataAccess.Data;
using IBS.Models;
using IBS.Models.Enums;
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
                .Where(user => user.Id == userManager.GetUserId(this.User))
                .FirstOrDefaultAsync();

            ViewBag.GetUserDepartment = findUser?.Department;
            var companyClaims = findUser != null ? await GetCompanyClaimAsync() : string.Empty;

            var dashboardCounts = new DashboardCountViewModel
            {
                #region -- Filpride

                SupplierAppointmentCount = await dbContext.CustomerOrderSlips
                        .Where(cos =>
                            (cos.Status == nameof(CosStatus.HaulerAppointed) || cos.Status == nameof(CosStatus.Created))
                            && cos.Company == companyClaims)
                        .CountAsync(),

                HaulerAppointmentCount = await dbContext.CustomerOrderSlips
                        .Where(cos =>
                        (cos.Status == nameof(CosStatus.SupplierAppointed) || cos.Status == nameof(CosStatus.Created))
                            && cos.Company == companyClaims)
                        .CountAsync(),

                ATLBookingCount = await dbContext.CustomerOrderSlips
                        .Where(cos => !cos.IsCosAtlFinalized
                                      && !string.IsNullOrEmpty(cos.Depot)
                                      && cos.Status != nameof(CosStatus.Closed)
                                      && cos.Status != nameof(CosStatus.Disapproved)
                                      && cos.Status != nameof(CosStatus.Expired)
                                      && cos.Company == companyClaims)
                        .CountAsync(),

                OMApprovalCOSCount = await dbContext.CustomerOrderSlips
                        .Where(cos => cos.Status == nameof(CosStatus.ForApprovalOfOM)
                                      && cos.Company == companyClaims)
                        .CountAsync(),

                OMApprovalDRCount = await dbContext.DeliveryReceipts
                        .Where(dr => dr.Status == nameof(CosStatus.ForApprovalOfOM)
                                     && dr.Company == companyClaims)
                        .CountAsync(),

                CNCApprovalCount = await dbContext.CustomerOrderSlips
                    .Where(cos => cos.Status == nameof(CosStatus.ForApprovalOfCNC)
                                  && cos.Company == companyClaims)
                    .CountAsync(),

                FMApprovalCount = await dbContext.CustomerOrderSlips
                        .Where(cos => cos.Status == nameof(CosStatus.ForApprovalOfFM)
                                      && cos.Company == companyClaims)
                        .CountAsync(),

                DRCount = await dbContext.CustomerOrderSlips
                        .Where(cos => cos.Status == nameof(CosStatus.ForDR)
                                      && cos.Company == companyClaims)
                        .CountAsync(),

                InTransitCount = await dbContext.DeliveryReceipts
                        .Where(dr => dr.Status == nameof(DRStatus.PendingDelivery)
                                     && dr.Company == companyClaims)
                        .CountAsync(),

                ForInvoiceCount = await dbContext.DeliveryReceipts
                        .Where(dr => dr.Status == nameof(DRStatus.ForInvoicing)
                                     && dr.Company == companyClaims)
                        .CountAsync(),

                #endregion -- Filpride

                #region -- MMSI

                MMSIServiceRequestForPosting = await dbContext.MMSIDispatchTickets
                        .Where(po => po.Status == "For Posting")
                        .CountAsync(),

                MMSIDispatchTicketForTariff = await dbContext.MMSIDispatchTickets
                        .Where(po => po.Status == "For Tariff")
                        .CountAsync(),

                MMSIDispatchTicketForApproval = await dbContext.MMSIDispatchTickets
                        .Where(po => po.Status == "For Approval")
                        .CountAsync(),

                MMSIDispatchTicketForBilling = await dbContext.MMSIDispatchTickets
                        .Where(po => po.Status == "For Billing")
                        .CountAsync(),

                MMSIBillingForCollection = await dbContext.MMSIBillings
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
