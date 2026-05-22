using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using IBS.Models.MSAP.MasterFile;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace IBSWeb.Areas.User.Controllers
{
    [Area("User")]
        public class TugboatController(
        ApplicationDbContext dbContext,
        IUnitOfWork unitOfWork,
        ILogger<TugboatController> logger,
        UserManager<ApplicationUser> userManager)
        : Controller
    {
        public async Task<IActionResult> Index(CancellationToken cancellationToken = default)
        {
            var tugboat = await unitOfWork.Tugboat.GetAllAsync(null,
                cancellationToken);
            return View(tugboat);
        }

        [HttpGet]
        public async Task<IActionResult> Create(CancellationToken cancellationToken)
        {
            var tugboat = new Tugboat
            {
                CompanyList = await unitOfWork.Tugboat.GetMsapCompanyOwnerSelectListById(cancellationToken),
                PortList = await unitOfWork.Port.GetMsapPortsSelectList(cancellationToken)
            };

            return View(tugboat);
        }

        [HttpPost]
        public async Task<IActionResult> Create(Tugboat model, CancellationToken cancellationToken = default)
        {
            if (!ModelState.IsValid)
            {
                model.CompanyList = await unitOfWork.Tugboat.GetMsapCompanyOwnerSelectListById(cancellationToken);
                model.PortList = await unitOfWork.Port.GetMsapPortsSelectList(cancellationToken);
                TempData["warning"] = "Invalid entry, please try again.";
                return View(model);
            }

            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                if (model.IsCompanyOwned)
                {
                    model.TugboatOwnerId = null;
                }

                await unitOfWork.Tugboat.AddAsync(model,
                    cancellationToken);

                #region -- Audit Trail Recording --

                AuditTrail auditTrailBook = new(userManager.GetUserName(User)!,
                    $"Created new Tugboat #{model.TugboatNumber}",
                    "Tugboat");
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
                    "Failed to create tugboat.");
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                return View(model);
            }
        }
        public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
        {
            try
            {
                var model = await unitOfWork.Tugboat.GetAsync(i => i.TugboatId == id,
                    cancellationToken);

                if (model == null)
                {
                    return NotFound();
                }

                await unitOfWork.Tugboat.RemoveAsync(model,
                    cancellationToken);
                TempData["success"] = "Entry deleted successfully";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Failed to delete tugboat.");
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
        {
            var model = await unitOfWork.Tugboat.GetAsync(a => a.TugboatId == id,
                cancellationToken);

            if (model == null)
            {
                return NotFound();
            }

            model.CompanyList = await unitOfWork.Tugboat.GetMsapCompanyOwnerSelectListById(cancellationToken);
            model.PortList = await unitOfWork.Port.GetMsapPortsSelectList(cancellationToken);
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(Tugboat model, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                model.CompanyList = await unitOfWork.Tugboat.GetMsapCompanyOwnerSelectListById(cancellationToken);
                model.PortList = await unitOfWork.Port.GetMsapPortsSelectList(cancellationToken);
                TempData["error"] = "Invalid entry, please try again.";
                return View(model);
            }

            var currentModel = await unitOfWork.Tugboat.GetAsync(t => t.TugboatId == model.TugboatId,
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
                    $"Edited Tugboat #{currentModel.TugboatNumber} => {model.TugboatNumber}",
                    "Tugboat");
                await unitOfWork.AuditTrail.AddAsync(auditTrailBook,
                    cancellationToken);

                #endregion -- Audit Trail Recording --

                currentModel.TugboatOwnerId = model.IsCompanyOwned ? null : model.TugboatOwnerId;
                currentModel.IsCompanyOwned = model.IsCompanyOwned;
                currentModel.TugboatNumber = model.TugboatNumber;
                currentModel.TugboatName = model.TugboatName;
                currentModel.PortId = model.PortId;
                await unitOfWork.Tugboat.SaveAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = "Edited successfully";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Failed to edit tugboat.");
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                return View(model);
            }
        }
    }
}


