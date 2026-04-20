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
    public class PrincipalController(
        ApplicationDbContext dbContext,
        IUnitOfWork unitOfWork,
        UserManager<ApplicationUser> userManager,
        ILogger<PrincipalController> logger)
        : Controller
    {
        private async Task<string?> GetCompanyClaimAsync()
        {
            var user = await userManager.GetUserAsync(User);
            if (user == null)
            {
                return null;
            }

            var claims = await userManager.GetClaimsAsync(user);
            return claims.FirstOrDefault(c => c.Type == "Company")?.Value;
        }

        public async Task<IActionResult> Index(CancellationToken cancellationToken = default)
        {
            var principals = await unitOfWork.Principal.GetAllAsync(null, cancellationToken);
            return View(principals);
        }

        [HttpGet]
        public async Task<IActionResult> Create(CancellationToken cancellationToken)
        {
            var companyClaims = await GetCompanyClaimAsync();
            var model = new Principal
            {
                CustomerSelectList = await unitOfWork.GetCustomerListAsyncById(cancellationToken)
            };
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Create(Principal model, CancellationToken cancellationToken = default)
        {
            if (!ModelState.IsValid)
            {
                TempData["warning"] = "Invalid entry, please try again.";
                return View(model);
            }

            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var customer = await unitOfWork.Customer
                    .GetAsync(c => c.CustomerId == model.CustomerId, cancellationToken) ?? throw new NullReferenceException("Customer not found");
                model.CustomerId = customer.CustomerId;
                await unitOfWork.Principal.AddAsync(model, cancellationToken);

                #region -- Audit Trail Recording --

                AuditTrail auditTrailBook = new(userManager.GetUserName(User)!,
                    $"Created new Principal #{model.PrincipalNumber}", "Principal", SD.Company_MMSI);
                await unitOfWork.AuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion -- Audit Trail Recording --

                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = "Creation Succeed!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to create principal.");
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                return View(model);
            }
        }

        public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken = default)
        {
            try
            {
                var model = await unitOfWork.Principal
                    .GetAsync(p => p.PrincipalId == id, cancellationToken);

                if (model == null)
                {
                    return NotFound();
                }

                await unitOfWork.Principal.RemoveAsync(model, cancellationToken);
                await unitOfWork.Principal.SaveAsync(cancellationToken);
                TempData["success"] = "Entry deleted successfully";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to delete principal.");
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
        {
            var companyClaims = await GetCompanyClaimAsync();
            var model = await unitOfWork.Principal.GetAsync(p => p.PrincipalId == id, cancellationToken);
            if (model == null)
            {
                return NotFound();
            }

            model.CustomerSelectList = await unitOfWork.GetCustomerListAsyncById(cancellationToken);
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(Principal model, CancellationToken cancellationToken = default)
        {
            if (!ModelState.IsValid)
            {
                TempData["warning"] = "Invalid entry, please try again.";
                return View(model);
            }

            var currentModel = await unitOfWork.Principal.GetAsync(p => p.PrincipalId == model.PrincipalId, cancellationToken);

            if (currentModel == null)
            {
                TempData["error"] = "Principal not found.";
                return View(model);
            }

            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                #region -- Audit Trail Recording --

                AuditTrail auditTrailBook = new(userManager.GetUserName(User)!,
                    $"Edited Principal #{currentModel.PrincipalNumber} => {model.PrincipalNumber}", "Principal", SD.Company_MMSI);
                await unitOfWork.AuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion -- Audit Trail Recording --

                currentModel.Address = model.Address;
                currentModel.PrincipalName = model.PrincipalName;
                currentModel.PrincipalNumber = model.PrincipalNumber;
                currentModel.TIN = model.TIN;
                currentModel.BusinessType = model.BusinessType;
                currentModel.PrincipalName = model.PrincipalName;
                currentModel.Terms = model.Terms;
                currentModel.Mobile1 = model.Mobile1;
                currentModel.Mobile2 = model.Mobile2;
                currentModel.Landline1 = model.Landline1;
                currentModel.Landline2 = model.Landline2;
                currentModel.IsVatable = model.IsVatable;
                currentModel.IsActive = model.IsActive;
                currentModel.CustomerId = model.CustomerId;
                await unitOfWork.Principal.SaveAsync(cancellationToken);

                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = "Edited successfully";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to edit principal.");
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                return View(model);
            }
        }
    }
}
