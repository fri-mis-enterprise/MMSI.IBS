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
    public class TugMasterController(
        ITugMasterService tugMasterService,
        UserManager<ApplicationUser> userManager)
        : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GetTugMasterList(CancellationToken cancellationToken)
        {
            var list = await tugMasterService.GetAllAsync(cancellationToken);
            return Json(new { data = list });
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TugMaster model, CancellationToken cancellationToken = default)
        {
            var result = await tugMasterService.CreateAsync(model, userManager.GetUserName(User)!, cancellationToken);

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
            var model = await tugMasterService.GetByIdAsync(id, cancellationToken);
            if (model == null)
            {
                return NotFound();
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(TugMaster model, CancellationToken cancellationToken)
        {
            var result = await tugMasterService.UpdateAsync(model, userManager.GetUserName(User)!, cancellationToken);

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
            var result = await tugMasterService.DeleteAsync(id, userManager.GetUserName(User)!, cancellationToken);

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
