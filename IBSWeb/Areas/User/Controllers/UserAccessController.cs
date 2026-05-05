using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using IBS.Models.MMSI.MasterFile;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace IBSWeb.Areas.User.Controllers
{
    [Area("User")]
        [Authorize(Roles = "Admin")]
    public class UserAccessController(
        ApplicationDbContext dbContext,
        IUnitOfWork unitOfWork,
        ILogger<UserAccessController> logger,
        UserManager<ApplicationUser> userManager)
        : Controller
    {
        // GET
        public async Task<IActionResult> Index(CancellationToken cancellationToken = default)
        {
            var model = await unitOfWork.UserAccess.GetAllAsync(null,
                cancellationToken);
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Create(CancellationToken cancellationToken = default)
        {
            UserAccess model = new UserAccess
            {
                Users = await unitOfWork.Msap.GetMMSIUsersSelectListById(cancellationToken)
            };

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

            var tempModel = await unitOfWork.UserAccess.GetAsync(ua => ua.UserId == model.UserId,
                cancellationToken);

            if (tempModel != null)
            {
                throw new Exception($"Access for {tempModel.UserName} already exists.");
            }

            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var selectedUser = dbContext.Users.FirstOrDefault(u => u.Id == model.UserId);
                model.UserName = selectedUser!.UserName;
                await unitOfWork.UserAccess.AddAsync(model,
                    cancellationToken);

                #region -- Audit Trail Recording --

                AuditTrail auditTrailBook = new(userManager.GetUserName(User)!,
                    $"Created User Access for {model.UserName}",
                    "User Access");
                await unitOfWork.AuditTrail.AddAsync(auditTrailBook,
                    cancellationToken);

                #endregion -- Audit Trail Recording --

                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = "User access created successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Failed to create user access.");
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                model.Users = await unitOfWork.Msap.GetMMSIUsersSelectListById(cancellationToken);
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken = default)
        {
            var model = await unitOfWork.UserAccess.GetAsync(ua => ua.Id == id,
                cancellationToken);

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

            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var tempModel = await unitOfWork.UserAccess.GetAsync(ua => ua.Id == model.Id,
                    cancellationToken);

                if (tempModel == null)
                {
                    return NotFound();
                }

                #region -- Audit Trail Recording --

                AuditTrail auditTrailBook = new(userManager.GetUserName(User)!,
                    $"Edited User Access for {model.UserName}",
                    "User Access");
                await unitOfWork.AuditTrail.AddAsync(auditTrailBook,
                    cancellationToken);

                #endregion -- Audit Trail Recording --

                tempModel.CanCreateServiceRequest = model.CanCreateServiceRequest;
                tempModel.CanPostServiceRequest = model.CanPostServiceRequest;
                tempModel.CanCreateDispatchTicket = model.CanCreateDispatchTicket;
                tempModel.CanEditDispatchTicket = model.CanEditDispatchTicket;
                tempModel.CanCancelDispatchTicket = model.CanCancelDispatchTicket;
                tempModel.CanSetTariff = model.CanSetTariff;
                tempModel.CanApproveTariff = model.CanApproveTariff;
                tempModel.CanCreateBilling = model.CanCreateBilling;
                tempModel.CanCreateCollection = model.CanCreateCollection;
                tempModel.CanCreateJobOrder = model.CanCreateJobOrder;
                tempModel.CanEditJobOrder = model.CanEditJobOrder;
                tempModel.CanDeleteJobOrder = model.CanDeleteJobOrder;
                tempModel.CanCloseJobOrder = model.CanCloseJobOrder;

                // Treasury permissions
                tempModel.CanAccessTreasury = model.CanAccessTreasury;
                tempModel.CanCreateDisbursement = model.CanCreateDisbursement;

                // MSAP Import permissions
                tempModel.CanManageMsapImport = model.CanManageMsapImport;

                // Reports permissions
                tempModel.CanViewGeneralLedger = model.CanViewGeneralLedger;
                tempModel.CanViewInventoryReport = model.CanViewInventoryReport;
                tempModel.CanViewMaritimeReport = model.CanViewMaritimeReport;

                await unitOfWork.SaveAsync(cancellationToken);

                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = "User access edited successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Failed to edit user access.");
                TempData["error"] = ex.Message;
                await transaction.RollbackAsync(cancellationToken);
                model.Users = await unitOfWork.Msap.GetMMSIUsersSelectListById(cancellationToken);
                return View(model);
            }
        }
    }
}
