using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using IBS.Models.MMSI.MasterFile;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace IBSWeb.Areas.User.Controllers
{
    [Area("User")]
        public class TugboatOwnerController(
        ApplicationDbContext dbContext,
        ILogger<TugboatOwnerController> logger,
        IUnitOfWork unitOfWork,
        UserManager<ApplicationUser> userManager)
        : Controller
    {
        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            var companyOwners = await unitOfWork.TugboatOwner.GetAllAsync(null,
                cancellationToken);
            return View(companyOwners);
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
                TempData["warning"] = "Invalid entry, please try again.";
                return View(model);
            }

            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                await unitOfWork.TugboatOwner.AddAsync(model,
                    cancellationToken);

                #region -- Audit Trail Recording --

                AuditTrail auditTrailBook = new(userManager.GetUserName(User)!,
                    $"Created new Tugboat Owner #{model.TugboatOwnerNumber}",
                    "Tugboat Owner");
                await unitOfWork.AuditTrail.AddAsync(auditTrailBook,
                    cancellationToken);

                #endregion -- Audit Trail Recording --

                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = "Creation Succeed!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Failed to create tugboat owner.");
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                return View(model);
            }
        }
        public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
        {
            try
            {
                var model = await unitOfWork.TugboatOwner.GetAsync(i => i.TugboatOwnerId == id,
                    cancellationToken);

                if (model == null)
                {
                    return NotFound();
                }

                await unitOfWork.TugboatOwner.RemoveAsync(model,
                    cancellationToken);
                TempData["success"] = "Entry deleted successfully";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception)
            {
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
        {
            var model = await unitOfWork.TugboatOwner.GetAsync(a => a.TugboatOwnerId == id,
                cancellationToken);
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(TugboatOwner model, CancellationToken cancellationToken)
        {
            var currentModel = await unitOfWork.TugboatOwner.GetAsync(t => t.TugboatOwnerId == model.TugboatOwnerId,
                cancellationToken);

            if (currentModel == null)
            {
                TempData["info"] = "Entry not found, please try again.";
                return View(model);
            }

            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                #region -- Audit Trail Recording --

                AuditTrail auditTrailBook = new(userManager.GetUserName(User)!,
                    $"Edited Tugboat Owner #{currentModel.TugboatOwnerNumber} => {model.TugboatOwnerNumber}",
                    "Tugboat Owner");
                await unitOfWork.AuditTrail.AddAsync(auditTrailBook,
                    cancellationToken);

                #endregion -- Audit Trail Recording --

                currentModel.TugboatOwnerNumber = model.TugboatOwnerNumber;
                currentModel.TugboatOwnerName = model.TugboatOwnerName;
                currentModel.FixedRate = model.FixedRate;
                await unitOfWork.SaveAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = "Edited successfully";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Failed to edit tugboat owner.");
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                return View(model);
            }
        }
    }
}
