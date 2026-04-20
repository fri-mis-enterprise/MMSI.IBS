using IBS.Utility.Constants;
using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using IBS.Models.MMSI.MasterFile;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace IBSWeb.Areas.User.Controllers
{
    [Area("User")]
        public class TerminalController(
        ApplicationDbContext dbContext,
        IUnitOfWork unitOfWork,
        ILogger<TerminalController> logger,
        UserManager<ApplicationUser> userManager)
        : Controller
    {
        public async Task<IActionResult> Index(CancellationToken cancellationToken = default)
        {
            var terminals = await unitOfWork.Terminal.GetAllAsync(null,
                cancellationToken);
            return View(terminals);
        }

        [HttpGet]
        public async Task<IActionResult> Create(CancellationToken cancellationToken)
        {
            Terminal model = new()
            {
                Ports = await unitOfWork.Port.GetMMSIPortsSelectList(cancellationToken)
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Create(Terminal model, CancellationToken cancellationToken = default)
        {
            if (!ModelState.IsValid)
            {
                TempData["warning"] = "Invalid entry, please try again.";
                return View(model);
            }

            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                await unitOfWork.Terminal.AddAsync(model,
                    cancellationToken);

                #region -- Audit Trail Recording --

                AuditTrail auditTrailBook = new(userManager.GetUserName(User)!,
                    $"Create new Terminal #{model.TerminalNumber}",
                    "Terminal");
                await unitOfWork.AuditTrail.AddAsync(auditTrailBook,
                    cancellationToken);

                #endregion -- Audit Trail Recording --

                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = "Creation Succeed!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                await transaction.RollbackAsync(cancellationToken);
                logger.LogError(ex,
                    "Failed to create terminal.");
                return View(model);
            }
        }

        public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
        {
            try
            {
                var model = await unitOfWork.Terminal.GetAsync(i => i.TerminalId == id,
                    cancellationToken);

                if (model == null)
                {
                    return NotFound();
                }

                await unitOfWork.Terminal.RemoveAsync(model,
                    cancellationToken);
                TempData["success"] = "Entry deleted successfully";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Failed to delete terminal.");
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
        {
            var model = await unitOfWork.Terminal.GetAsync(a => a.TerminalId == id,
                cancellationToken);

            if (model == null)
            {
                return NotFound();
            }

            model.Ports = await unitOfWork.Port.GetMMSIPortsSelectList(cancellationToken);
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(Terminal model, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                TempData["warning"] = "Invalid entry, please try again.";
                return View(model);
            }

            var currentModel = await unitOfWork.Terminal.GetAsync(t => t.TerminalId == model.TerminalId,
                cancellationToken);

            if (currentModel == null)
            {
                TempData["error"] = "Entry not found, please try again.";
                return View(model);
            }

            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {

                #region -- Audit Trail Recording --

                AuditTrail auditTrailBook = new(userManager.GetUserName(User)!,
                    $"Edited Terminal #{currentModel.TerminalNumber} => {model.TerminalNumber}",
                    "Terminal");
                await unitOfWork.AuditTrail.AddAsync(auditTrailBook,
                    cancellationToken);

                #endregion -- Audit Trail Recording --

                currentModel.TerminalNumber = model.TerminalNumber;
                currentModel.TerminalName = model.TerminalName;
                currentModel.PortId = model.PortId;
                await unitOfWork.SaveAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = "Edited successfully";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                logger.LogError(ex,
                    "Failed to delete terminal.");
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }

        }
    }
}
