using IBS.Models;
using IBS.Models.MSAP;
using IBS.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace IBSWeb.Areas.User.Controllers
{
    [Area("User")]
    public class TariffRateController(
        ITariffRateService tariffRateService,
        UserManager<ApplicationUser> userManager)
        : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GetTariffRateList(CancellationToken cancellationToken)
        {
            var list = await tariffRateService.GetAllAsync(cancellationToken);
            return Json(new { data = list });
        }

        [HttpGet]
        public async Task<IActionResult> Create(CancellationToken cancellationToken)
        {
            var model = await tariffRateService.PopulateSelectListsAsync(null, cancellationToken);
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Create(TariffRate model, CancellationToken cancellationToken = default)
        {
            await tariffRateService.PopulateSelectListsAsync(model, cancellationToken);

            if (model is { Dispatch: <= 0, BAF: <= 0 })
            {
                ModelState.AddModelError(string.Empty, "Dispatch and BAF value cannot be both zero.");
                await tariffRateService.PopulateSelectListsAsync(model, cancellationToken);
                return View(model);
            }

            var result = await tariffRateService.UpsertAsync(model, userManager.GetUserName(User)!, cancellationToken);

            if (result.IsSuccess)
            {
                TempData["success"] = result.Message;
                return RedirectToAction(nameof(Index));
            }

            TempData["error"] = result.Message;
            await tariffRateService.PopulateSelectListsAsync(model, cancellationToken);
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
        {
            var model = await tariffRateService.GetByIdAsync(id, cancellationToken);
            if (model == null)
            {
                return NotFound();
            }

            await tariffRateService.PopulateSelectListsAsync(model, cancellationToken);
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(TariffRate model, CancellationToken cancellationToken)
        {
            await tariffRateService.PopulateSelectListsAsync(model, cancellationToken);

            var result = await tariffRateService.UpsertAsync(model, userManager.GetUserName(User)!, cancellationToken);

            if (result.IsSuccess)
            {
                TempData["success"] = result.Message;
                return RedirectToAction(nameof(Index));
            }

            TempData["error"] = result.Message;
            await tariffRateService.PopulateSelectListsAsync(model, cancellationToken);
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> ChangeTerminal(int portId, CancellationToken cancellationToken)
        {
            var list = await tariffRateService.GetTerminalsByPortAsync(portId, cancellationToken);
            return Json(list);
        }

        public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
        {
            var result = await tariffRateService.DeleteAsync(id, userManager.GetUserName(User)!, cancellationToken);

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
