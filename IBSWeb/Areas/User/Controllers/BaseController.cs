using IBS.Models;
using IBS.Services.AccessControl;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace IBSWeb.Areas.User.Controllers
{
    /// <summary>
    /// Base controller for all MSAP controllers with centralized access control
    /// </summary>
    public abstract class BaseController(IAccessControlService accessControl, UserManager<ApplicationUser> userManager)
        : Controller
    {
        protected readonly IAccessControlService AccessControl = accessControl;
        protected readonly UserManager<ApplicationUser> UserManager = userManager;

        protected string GetUserId() => UserManager.GetUserId(User)!;

        /// <summary>
        /// Returns a permission denied modal partial view
        /// </summary>
        protected IActionResult PermissionDenied(string? message = null, string? requiredPermission = null)
        {
            ViewData["message"] = message ?? "You don't have permission to perform this action.";
            ViewData["requiredPermission"] = requiredPermission;
            return PartialView("_PermissionDeniedModal");
        }

        // Module-specific access helpers using extension methods

        protected async Task<bool> HasJobOrderAccessAsync()
            => await AccessControl.HasJobOrderAccessAsync(GetUserId());

        protected async Task<bool> HasServiceRequestAccessAsync()
            => await AccessControl.HasServiceRequestAccessAsync(GetUserId());

        protected async Task<bool> HasDispatchTicketAccessAsync()
            => await AccessControl.HasDispatchTicketAccessAsync(GetUserId());

        protected async Task<bool> HasBillingAccessAsync()
            => await AccessControl.HasBillingAccessAsync(GetUserId());

        protected async Task<bool> HasCollectionAccessAsync()
            => await AccessControl.HasCollectionAccessAsync(GetUserId());

        protected async Task<bool> HasTariffAccessAsync()
            => await AccessControl.HasTariffAccessAsync(GetUserId());

        protected async Task<bool> HasMsapAccessAsync()
            => await AccessControl.HasMsapAccessAsync(GetUserId());

        // Treasury access helpers

        protected async Task<bool> HasTreasuryAccessAsync()
            => await AccessControl.HasTreasuryAccessAsync(GetUserId());

        protected async Task<bool> HasDisbursementAccessAsync()
            => await AccessControl.HasDisbursementAccessAsync(GetUserId());

        // MSAP Import access helpers

        protected async Task<bool> HasMsapImportAccessAsync()
            => await AccessControl.HasMsapImportAccessAsync(GetUserId());

        // Reports access helpers

        protected async Task<bool> HasGeneralLedgerReportAccessAsync()
            => await AccessControl.HasGeneralLedgerReportAccessAsync(GetUserId());

        protected async Task<bool> HasInventoryReportAccessAsync()
            => await AccessControl.HasInventoryReportAccessAsync(GetUserId());

        protected async Task<bool> HasMaritimeReportAccessAsync()
            => await AccessControl.HasMaritimeReportAccessAsync(GetUserId());
    }
}
