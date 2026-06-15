using IBS.Models;
using IBS.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace IBSWeb.Areas.Admin.Controllers
{
    [Area(nameof(Admin))]
    [Authorize(Roles = "Admin")]
    public class AppRoleController(IRoleService roleService)
        : Controller
    {
        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            var roles = await roleService.GetAllRolesAsync(cancellationToken);
            return View(roles);
        }

        [HttpPost]
        public async Task<IActionResult> Upsert([FromBody] IdentityRole model, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(model.Name))
            {
                return Json(new { success = false, message = "Role name is required" });
            }

            var result = await roleService.CreateRoleAsync(model.Name, cancellationToken);
            return Json(new { success = result.IsSuccess, message = result.Message });
        }

        [HttpPost]
        public async Task<IActionResult> GetRolesList([FromForm] DataTablesParameters parameters, CancellationToken cancellationToken)
        {
            try
            {
                var (data, totalRecords) = await roleService.GetPagedRolesAsync(parameters, cancellationToken);

                return Json(new
                {
                    draw = parameters.Draw,
                    recordsTotal = totalRecords,
                    recordsFiltered = totalRecords,
                    data
                });
            }
            catch (Exception)
            {
                return Json(new { error = "Internal server error" });
            }
        }
    }
}
