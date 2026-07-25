using IBS.DataAccess.Data;
using IBS.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace IBS.Services.Attributes
{
    public class DepartmentAuthorizeAttribute(params string[] departments): AuthorizeAttribute, IAsyncAuthorizationFilter
    {
        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            var userManager = context.HttpContext.RequestServices.GetService(typeof(UserManager<ApplicationUser>)) as UserManager<ApplicationUser>;
            var dbContext = context.HttpContext.RequestServices.GetService(typeof(ApplicationDbContext)) as ApplicationDbContext;

            if (userManager != null && dbContext != null)
            {
                var user = await userManager.GetUserAsync(context.HttpContext.User);

                var userDepartment = dbContext.ApplicationUsers
                    .Where(u => u.Id == user!.Id)
                    .Select(u => u.Department)
                    .FirstOrDefault();

                if (userDepartment == null || !departments.Contains(userDepartment))
                {
                    context.Result = new ForbidResult();
                }
            }
            else
            {
                context.Result = new ForbidResult();
            }
        }
    }
}
