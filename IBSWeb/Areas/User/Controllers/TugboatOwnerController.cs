using IBS.Models;
using IBS.Models.MSAP.MasterFile;
using IBS.Services;
using IBS.Models.Enums;
using IBS.Services.Attributes;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace IBSWeb.Areas.User.Controllers
{
    [Area("User")]
    [RequireAnyAccess("Access denied. You don't have permission to manage maritime master files.", ProcedureEnum.ManageMaritimeMasterFile)]
    public class TugboatOwnerController(
        ITugboatOwnerService tugboatOwnerService,
        UserManager<ApplicationUser> userManager)
        : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GetTugboatOwnerList(CancellationToken cancellationToken)
        {
            var list = await tugboatOwnerService.GetAllAsync(cancellationToken);
            return Json(new { data = list });
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(TugboatOwner model, CancellationToken cancellationToken = default)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var result = await tugboatOwnerService.CreateAsync(model, userManager.GetUserName(User)!, cancellationToken);

            if (result.IsSuccess)
            {
                TempData["success"] = result.Message;
                return RedirectToAction(nameof(Index));
            }

            TempData["error"] = result.Message;
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
        {
            var model = await tugboatOwnerService.GetByIdAsync(id, cancellationToken);
            if (model == null)
            {
                return NotFound();
            }
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(TugboatOwner model, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var result = await tugboatOwnerService.UpdateAsync(model, userManager.GetUserName(User)!, cancellationToken);

            if (result.IsSuccess)
            {
                TempData["success"] = result.Message;
                return RedirectToAction(nameof(Index));
            }

            TempData["error"] = result.Message;
            return View(model);
        }

        public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
        {
            var result = await tugboatOwnerService.DeleteAsync(id, userManager.GetUserName(User)!, cancellationToken);

            if (result.IsSuccess)
            {
                TempData["success"] = result.Message;
            }
            else
            {
                TempData["error"] = result.Message;
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
