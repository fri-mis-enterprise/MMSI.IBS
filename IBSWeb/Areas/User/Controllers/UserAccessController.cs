using IBS.Models;
using IBS.Models.MSAP.MasterFile;
using IBS.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace IBSWeb.Areas.User.Controllers
{
    [Area("User")]
    [Authorize(Roles = "Admin")]
    public class UserAccessController(
        IUserAccessService userAccessService,
        UserManager<ApplicationUser> userManager)
        : Controller
    {
        public async Task<IActionResult> Index(CancellationToken cancellationToken = default)
        {
            var model = await userAccessService.GetAllAsync(cancellationToken);
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Create(CancellationToken cancellationToken = default)
        {
            var model = await userAccessService.PopulateUsersAsync(null, cancellationToken);
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Create(UserAccess model, CancellationToken cancellationToken = default)
        {
            if (!ModelState.IsValid)
            {
                TempData["warning"] = "Invalid input please try again.";
                return RedirectToAction(nameof(Index));
            }

            var result = await userAccessService.CreateAsync(model, userManager.GetUserName(User)!, cancellationToken);

            if (result.IsSuccess)
            {
                TempData["success"] = result.Message;
                return RedirectToAction(nameof(Index));
            }

            TempData["error"] = result.Message;
            await userAccessService.PopulateUsersAsync(model, cancellationToken);
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken = default)
        {
            var model = await userAccessService.GetByIdAsync(id, cancellationToken);

            if (model == null)
            {
                TempData["info"] = "User access not found.";
                return RedirectToAction(nameof(Index));
            }

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(UserAccess model, CancellationToken cancellationToken = default)
        {
            if (!ModelState.IsValid)
            {
                TempData["warning"] = "Invalid input please try again.";
                return RedirectToAction(nameof(Index));
            }

            var result = await userAccessService.UpdateAsync(model, userManager.GetUserName(User)!, cancellationToken);

            if (result.IsSuccess)
            {
                TempData["success"] = result.Message;
                return RedirectToAction(nameof(Index));
            }

            TempData["error"] = result.Message;
            await userAccessService.PopulateUsersAsync(model, cancellationToken);
            return View(model);
        }
    }
}
