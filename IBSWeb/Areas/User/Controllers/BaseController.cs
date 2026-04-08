using IBS.Models;
using IBS.Services.AccessControl;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace IBSWeb.Areas.User.Controllers
{
    /// <summary>
    /// Base controller for all MSAP controllers with centralized access control
    /// </summary>
    public abstract class BaseController : Controller
    {
        protected readonly IAccessControlService AccessControl;
        protected readonly UserManager<ApplicationUser> UserManager;

        protected BaseController(IAccessControlService accessControl, UserManager<ApplicationUser> userManager)
        {
            AccessControl = accessControl;
            UserManager = userManager;
        }

        protected string GetUserId() => UserManager.GetUserId(User)!;

        /// <summary>
        /// Check if user has access to ANY of the specified procedures
        /// </summary>
        private async Task<bool> HasAccessAsync(params IBS.Models.Enums.ProcedureEnum[] procedures)
        {
            return await AccessControl.HasAnyAccessAsync(GetUserId(), procedures);
        }

        /// <summary>
        /// Check if user has access to ALL the specified procedures
        /// </summary>
        protected async Task<bool> HasAllAccessAsync(params IBS.Models.Enums.ProcedureEnum[] procedures)
        {
            return await AccessControl.HasAllAccessAsync(GetUserId(), procedures);
        }

        /// <summary>
        /// Redirect to home if user doesn't have access
        /// </summary>
        protected async Task<IActionResult> RequireAccessAsync(params IBS.Models.Enums.ProcedureEnum[] procedures)
        {
            if (!await HasAccessAsync(procedures))
            {
                TempData["error"] = "Access denied.";
                return RedirectToAction("Index", "Home", new { area = "User" });
            }

            return null!;
        }

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
