using IBS.Models;
using IBS.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace IBSWeb.Areas.Admin.Controllers
{
    [Area(nameof(Admin))]
    [Authorize(Roles = "Admin")]
    public class AppRoleController(IRoleService roleService, ILogger<AppRoleController> logger)
        : Controller
    {
        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            var roles = await roleService.GetAllRolesAsync(cancellationToken);
            return View(roles);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(IdentityRole model, CancellationToken cancellationToken)
        {
            var result = await roleService.CreateRoleAsync(model.Name!, cancellationToken);
            if (!result.IsSuccess)
            {
                TempData["error"] = result.Message;
            }
            return RedirectToAction(nameof(Index));
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
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to get roles.");
                return Json(new { error = "Internal server error" });
            }
        }
    }
}
