using IBS.Models;
using IBS.Models.MSAP.MasterFile;
using IBS.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace IBSWeb.Areas.User.Controllers
{
    [Area("User")]
    public class PrincipalController(
        IPrincipalService principalService,
        UserManager<ApplicationUser> userManager)
        : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GetPrincipalList(CancellationToken cancellationToken)
        {
            var list = await principalService.GetAllAsync(cancellationToken);
            return Json(new { data = list });
        }

        [HttpGet]
        public async Task<IActionResult> Create(CancellationToken cancellationToken)
        {
            var model = await principalService.PopulateSelectListsAsync(null, cancellationToken);
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Create(Principal model, CancellationToken cancellationToken = default)
        {
            if (!ModelState.IsValid)
            {
                await principalService.PopulateSelectListsAsync(model, cancellationToken);
                return View(model);
            }

            var result = await principalService.CreateAsync(model, userManager.GetUserName(User)!, cancellationToken);

            if (result.IsSuccess)
            {
                TempData["success"] = result.Message;
                return RedirectToAction(nameof(Index));
            }

            TempData["error"] = result.Message;
            await principalService.PopulateSelectListsAsync(model, cancellationToken);
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
        {
            var model = await principalService.GetByIdAsync(id, cancellationToken);
            if (model == null)
            {
                return NotFound();
            }

            await principalService.PopulateSelectListsAsync(model, cancellationToken);
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(Principal model, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                await principalService.PopulateSelectListsAsync(model, cancellationToken);
                return View(model);
            }

            var result = await principalService.UpdateAsync(model, userManager.GetUserName(User)!, cancellationToken);

            if (result.IsSuccess)
            {
                TempData["success"] = result.Message;
                return RedirectToAction(nameof(Index));
            }

            TempData["error"] = result.Message;
            await principalService.PopulateSelectListsAsync(model, cancellationToken);
            return View(model);
        }

        public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
        {
            var result = await principalService.DeleteAsync(id, userManager.GetUserName(User)!, cancellationToken);

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
