using IBS.Models.Enums;
using IBS.Services.AccessControl;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using System.Security.Claims;
using Microsoft.Extensions.DependencyInjection;

namespace IBS.Services.Attributes
{
    /// <summary>
    /// Declarative access control attribute that checks user permissions before action execution.
    /// Implements IAsyncAuthorizationFilter to run in the authorization filter phase.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
    public class RequireAccessAttribute(
        ProcedureEnum procedure,
        string errorMessage = "Access denied. You don't have permission to perform this action.",
        string redirectController = "Home",
        string redirectAction = "Index",
        string redirectArea = "User")
        : Attribute, IAsyncAuthorizationFilter
    {
        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            var httpContext = context.HttpContext;

            // 1. Resolve user identity from claims
            var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || string.IsNullOrWhiteSpace(userIdClaim.Value))
            {
                SetErrorAndRedirect(context, "You must be logged in to access this resource.");
                return;
            }

            // 2. Resolve IAccessControlService from DI (scoped via RequestServices)
            var accessControl = httpContext.RequestServices.GetRequiredService<IAccessControlService>();

            // 3. Check permission
            var hasAccess = await accessControl.HasAccessAsync(userIdClaim.Value, procedure);
            if (!hasAccess)
            {
                SetErrorAndRedirect(context, errorMessage);
            }
        }

        private void SetErrorAndRedirect(AuthorizationFilterContext context, string message)
        {
            // Set TempData error message
            var tempDataFactory = context.HttpContext.RequestServices
                .GetRequiredService<ITempDataDictionaryFactory>();
            var tempData = tempDataFactory.GetTempData(context.HttpContext);
            tempData["Denied"] = message;
            tempData.Save();

            // Redirect to configured target
            context.Result = new RedirectToActionResult(
                redirectAction,
                redirectController,
                new { area = redirectArea }
            );
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
        private readonly string _redirectController;
        private readonly string _redirectAction;
        private readonly string _redirectArea;

        public RequireAnyAccessAttribute(
            params ProcedureEnum[] procedures)
        {
            _procedures = procedures ?? Array.Empty<ProcedureEnum>();
            _errorMessage = "Access denied. You don't have permission to perform this action.";
            _redirectController = "Home";
            _redirectAction = "Index";
            _redirectArea = "User";
        }

        public RequireAnyAccessAttribute(
            string errorMessage,
            params ProcedureEnum[] procedures)
        {
            _procedures = procedures ?? Array.Empty<ProcedureEnum>();
            _errorMessage = errorMessage;
            _redirectController = "Home";
            _redirectAction = "Index";
            _redirectArea = "User";
        }

        public RequireAnyAccessAttribute(
            string errorMessage,
            string redirectController,
            string redirectAction,
            string redirectArea,
            params ProcedureEnum[] procedures)
        {
            _procedures = procedures ?? Array.Empty<ProcedureEnum>();
            _errorMessage = errorMessage;
            _redirectController = redirectController;
            _redirectAction = redirectAction;
            _redirectArea = redirectArea;
        }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            var httpContext = context.HttpContext;

            // 1. Resolve user identity from claims
            var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || string.IsNullOrWhiteSpace(userIdClaim.Value))
            {
                SetErrorAndRedirect(context, "You must be logged in to access this resource.");
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
            SetErrorAndRedirect(context, _errorMessage);
        }

        private void SetErrorAndRedirect(AuthorizationFilterContext context, string message)
        {
            // Set TempData error message
            var tempDataFactory = context.HttpContext.RequestServices
                .GetRequiredService<ITempDataDictionaryFactory>();
            var tempData = tempDataFactory.GetTempData(context.HttpContext);
            tempData["Denied"] = message;
            tempData.Save();

            // Redirect to configured target
            context.Result = new RedirectToActionResult(
                _redirectAction,
                _redirectController,
                new { area = _redirectArea }
            );
        }
    }
}
