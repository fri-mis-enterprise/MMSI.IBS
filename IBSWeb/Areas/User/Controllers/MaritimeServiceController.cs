using IBS.Utility.Constants;
using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using IBS.Models.MMSI.MasterFile;
using IBS.Services.Attributes;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace IBSWeb.Areas.User.Controllers
{
    [Area("User")]
    [CompanyAuthorize(SD.Company_MMSI)]
    public class MaritimeServiceController(
        ApplicationDbContext dbContext,
        ILogger<ServiceController> logger,
        IUnitOfWork unitOfWork,
        UserManager<ApplicationUser> userManager)
        : Controller
    {
        public IActionResult Index()
        {
            var activitiesServices = dbContext.MMSIServices.ToList();
            return View(activitiesServices);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(Service model, CancellationToken cancellationToken = default)
        {
            if (!ModelState.IsValid)
            {
                TempData["warning"] = "Invalid entry, please try again.";
                return View(model);
            }

            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                await unitOfWork.Service.AddAsync(model, cancellationToken);

                #region -- Audit Trail Recording --

                AuditTrail auditTrailBook = new(userManager.GetUserName(User)!,
                    $"Created new Service #{model.ServiceNumber}", "Service", SD.Company_MMSI);
                await unitOfWork.AuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion -- Audit Trail Recording --

                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = "Creation Succeed!";
                return RedirectToAction(nameof(Index));
            }

            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to create service.");
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                return View(model);
            }
        }

        public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
        {
            try
            {
                var model = await unitOfWork.Service.GetAsync( i => i.ServiceId == id, cancellationToken);

                if (model == null)
                {
                    return NotFound();
                }

                dbContext.MMSIServices.Remove(model);
                await dbContext.SaveChangesAsync(cancellationToken);
                TempData["success"] = "Entry deleted successfully";
                return RedirectToAction(nameof(Index));
            }

            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to delete service.");
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
        {
            var model = await unitOfWork.Service.GetAsync(a => a.ServiceId == id, cancellationToken);
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(Service model, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                TempData["warning"] = "Invalid entry, please try again.";
                return View(model);
            }

            var currentModel = await unitOfWork.Service.GetAsync(s => s.ServiceId == model.ServiceId, cancellationToken);

            if (currentModel == null)
            {
                return NotFound();
            }

            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                #region -- Audit Trail Recording --

                AuditTrail auditTrailBook = new(userManager.GetUserName(User)!,
                    $"Edited Service #{currentModel.ServiceNumber} => {model.ServiceNumber}", "Service", SD.Company_MMSI);
                await unitOfWork.AuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion -- Audit Trail Recording --

                currentModel.ServiceNumber = model.ServiceNumber;
                currentModel.ServiceName = model.ServiceName;
                await unitOfWork.Service.SaveAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = "Edited successfully";
                return RedirectToAction(nameof(Index));
            }

            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to edit service.");
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                return View(model);
            }
        }
    }
}
