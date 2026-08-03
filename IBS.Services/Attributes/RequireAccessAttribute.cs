using IBS.Models.Enums;
using IBS.Services.AccessControl;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;
using Microsoft.Extensions.DependencyInjection;

namespace IBS.Services.Attributes
{
    /// <summary>
    /// Declarative access control attribute that checks user permissions before action execution.
    /// Implements IAsyncAuthorizationFilter to run in the authorization filter phase.
    /// Denial is always returned as a JSON envelope ({ success = false, message }) matching the
    /// response contract used across all MSAP workflow actions and consumed by ModernAlert.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
    public class RequireAccessAttribute(
        ProcedureEnum procedure,
        string errorMessage = "Access denied. You don't have permission to perform this action.")
        : Attribute, IAsyncAuthorizationFilter
    {
        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            var httpContext = context.HttpContext;

            // 1. Resolve user identity from claims
            var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || string.IsNullOrWhiteSpace(userIdClaim.Value))
            {
                Deny(context, "You must be logged in to access this resource.");
                return;
            }

            // 2. Resolve IAccessControlService from DI (scoped via RequestServices)
            var accessControl = httpContext.RequestServices.GetRequiredService<IAccessControlService>();

            // 3. Check permission
            var hasAccess = await accessControl.HasAccessAsync(userIdClaim.Value, procedure);
            if (!hasAccess)
            {
                Deny(context, errorMessage);
            }
        }

        private static void Deny(AuthorizationFilterContext context, string message)
        {
            context.Result = new JsonResult(new { success = false, message });
        }
    }

    /// <summary>
    /// Declarative access control attribute that checks if user has access to ANY of the specified procedures.
    /// Use this when an action should be accessible if the user has at least one of the required permissions.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
    public class RequireAnyAccessAttribute : Attribute, IAsyncAuthorizationFilter
    {
        private readonly ProcedureEnum[] _procedures;
        private readonly string _errorMessage;

        public RequireAnyAccessAttribute(
            params ProcedureEnum[] procedures)
        {
            _procedures = procedures ?? Array.Empty<ProcedureEnum>();
            _errorMessage = "Access denied. You don't have permission to perform this action.";
        }

        public RequireAnyAccessAttribute(
            string errorMessage,
            params ProcedureEnum[] procedures)
        {
            _procedures = procedures ?? Array.Empty<ProcedureEnum>();
            _errorMessage = errorMessage;
        }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            var httpContext = context.HttpContext;

            // 1. Resolve user identity from claims
            var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || string.IsNullOrWhiteSpace(userIdClaim.Value))
            {
                Deny(context, "You must be logged in to access this resource.");
                return;
            }

            // 2. Resolve IAccessControlService from DI (scoped via RequestServices)
            var accessControl = httpContext.RequestServices.GetRequiredService<IAccessControlService>();

            // 3. Check if user has access to ANY of the procedures
            foreach (var procedure in _procedures)
            {
                if (await accessControl.HasAccessAsync(userIdClaim.Value, procedure))
                {
                    // At least one permission granted — allow through
                    return;
                }
            }

            // None of the procedures passed — deny access
            Deny(context, _errorMessage);
        }

        private static void Deny(AuthorizationFilterContext context, string message)
        {
            context.Result = new JsonResult(new { success = false, message });
        }
    }
}
