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
    public class TugboatController(
        ITugboatService tugboatService,
        UserManager<ApplicationUser> userManager)
        : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GetTugboatList(CancellationToken cancellationToken)
        {
            var list = await tugboatService.GetAllAsync(cancellationToken);
            return Json(new { data = list });
        }

        [HttpGet]
        public async Task<IActionResult> Create(CancellationToken cancellationToken)
        {
            var model = await tugboatService.PopulateSelectListsAsync(null, cancellationToken);
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Create(Tugboat model, CancellationToken cancellationToken = default)
        {
            if (!ModelState.IsValid)
            {
                await tugboatService.PopulateSelectListsAsync(model, cancellationToken);
                return View(model);
            }

            var result = await tugboatService.CreateAsync(model, userManager.GetUserName(User)!, cancellationToken);

            if (result.IsSuccess)
            {
                TempData["success"] = result.Message;
                return RedirectToAction(nameof(Index));
            }

            TempData["error"] = result.Message;
            await tugboatService.PopulateSelectListsAsync(model, cancellationToken);
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
        {
            var model = await tugboatService.GetByIdAsync(id, cancellationToken);
            if (model == null)
            {
                return NotFound();
            }

            await tugboatService.PopulateSelectListsAsync(model, cancellationToken);
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(Tugboat model, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                await tugboatService.PopulateSelectListsAsync(model, cancellationToken);
                return View(model);
            }

            var result = await tugboatService.UpdateAsync(model, userManager.GetUserName(User)!, cancellationToken);

            if (result.IsSuccess)
            {
                TempData["success"] = result.Message;
                return RedirectToAction(nameof(Index));
            }

            TempData["error"] = result.Message;
            await tugboatService.PopulateSelectListsAsync(model, cancellationToken);
            return View(model);
        }

        public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
        {
            var result = await tugboatService.DeleteAsync(id, userManager.GetUserName(User)!, cancellationToken);

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
