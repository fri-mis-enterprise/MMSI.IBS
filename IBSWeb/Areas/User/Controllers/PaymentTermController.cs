using IBS.Models.Books;
using IBS.Models.Integrated;
using IBS.Models.MasterFile;
using IBS.Utility.Constants;
using System.Linq.Dynamic.Core;
using System.Security.Claims;
using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using IBS.Utility.Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IBSWeb.Areas.User.Controllers
{
    [Area("User")]
    public class PaymentTermController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<SupplierController> _logger;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _dbContext;

        public PaymentTermController(IUnitOfWork unitOfWork,
            ILogger<SupplierController> logger,
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext dbContext)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _userManager = userManager;
            _dbContext = dbContext;
        }

        private string GetUserFullName()
        {
            return User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.GivenName)?.Value
                   ?? User.Identity?.Name!;
        }

        private async Task<string?> GetCompanyClaimAsync()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return null;
            }

            var claims = await _userManager.GetClaimsAsync(user);
            return claims.FirstOrDefault(c => c.Type == "Company")?.Value;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> GetPaymentTerms([FromForm] DataTablesParameters parameters, CancellationToken cancellationToken)
        {
            try
            {
                var queried = await _unitOfWork.Terms
                    .GetAllAsync(null, cancellationToken);

                // Global search
                if (!string.IsNullOrEmpty(parameters.Search.Value))
                {
                    var searchValue = parameters.Search.Value.ToLower();

                    queried = queried
                    .Where(s =>
                        s.TermsCode.ToLower().Contains(searchValue) ||
                        s.NumberOfDays.ToString("N0").Contains(searchValue) ||
                        s.NumberOfMonths.ToString("N0").Contains(searchValue)
                        ).ToList();
                }

                // Sorting
                if (parameters.Order?.Count > 0)
                {
                    var orderColumn = parameters.Order[0];
                    var columnName = parameters.Columns[orderColumn.Column].Data;
                    var sortDirection = orderColumn.Dir.ToLower() == "asc" ? "ascending" : "descending";
                    queried = queried
                        .AsQueryable()
                        .OrderBy($"{columnName} {sortDirection}");
                }

                var totalRecords = queried.Count();
                var pagedData = queried
                    .Skip(parameters.Start)
                    .Take(parameters.Length)
                    .ToList();

                return Json(new
                {
                    draw = parameters.Draw,
                    recordsTotal = totalRecords,
                    recordsFiltered = totalRecords,
                    data = pagedData
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get suppliers.");
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpGet]
        public IActionResult Create()
        {
            Terms viewModel = new();

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Terms model, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                ModelState.AddModelError("", "Make sure to fill all the required details.");
                return View(model);
            }

            var getUserFullName = GetUserFullName();
            var companyClaims = await GetCompanyClaimAsync();

            if (companyClaims == null)
            {
                return BadRequest();
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                model.TermsCode = model.TermsCode.Trim();
                model.NumberOfDays = model.NumberOfDays;
                model.NumberOfMonths = model.NumberOfMonths;
                model.CreatedBy = getUserFullName;
                model.CreatedDate = DateTimeHelper.GetCurrentPhilippineTime();
                model.EditedBy = string.Empty;
                await _unitOfWork.Terms.AddAsync(model, cancellationToken);
                await _unitOfWork.SaveAsync(cancellationToken);

                #region -- Audit Trail Recording --

                AuditTrail auditTrailBook = new(getUserFullName,
                    $"Create new Terms #{model.TermsCode}", "Terms", companyClaims);
                await _unitOfWork.AuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion -- Audit Trail Recording --

                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = "Terms created successfully";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create terms master file. Created by: {UserName}", getUserFullName);
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = $"Error: '{ex.Message}'";
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string code, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(code))
            {
                return NotFound();
            }

            var supplier = await _unitOfWork.Terms.GetAsync(c => c.TermsCode == code, cancellationToken);

            if (supplier == null)
            {
                return NotFound();
            }

            return View(supplier);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Terms model, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                ModelState.AddModelError("", "Make sure to fill all the required details.");
                return View(model);
            }

            var getUserFullName = GetUserFullName();
            var companyClaims = await GetCompanyClaimAsync();

            if (companyClaims == null)
            {
                return BadRequest();
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                model.EditedBy = getUserFullName;
                await _unitOfWork.Terms.UpdateAsync(model, cancellationToken);

                #region -- Audit Trail Recording --

                AuditTrail auditTrailBook = new (getUserFullName,
                    $"Edited Terms #{model.TermsCode}", "Terms", companyClaims);
                await _unitOfWork.AuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion -- Audit Trail Recording --

                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = "Terms updated successfully";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to edit supplier master file. Edited by: {UserName}", _userManager.GetUserName(User));
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = $"Error: '{ex.Message}'";
                return View(model);
            }
        }

        [HttpPost]
        public async Task<IActionResult> Delete(string code, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(code))
            {
                return BadRequest();
            }

            var getUserFullName = GetUserFullName();
            var companyClaims = await GetCompanyClaimAsync();

            if (companyClaims == null)
            {
                return BadRequest();
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var existingTerms = await _dbContext.Terms
                                        .FirstOrDefaultAsync(x => x.TermsCode == code, cancellationToken)
                                    ?? throw new InvalidOperationException("Terms with code not found.");

                _dbContext.Remove(existingTerms);
                await _dbContext.SaveChangesAsync(cancellationToken);

                #region -- Audit Trail Recording --

                AuditTrail auditTrailBook = new (getUserFullName,
                    $"Deleted Terms #{code}", "Terms", companyClaims);
                await _unitOfWork.AuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion -- Audit Trail Recording --

                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = "Terms deleted successfully";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete terms master file. Deleted by: {UserName}", getUserFullName);
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = $"Error: '{ex.Message}'";
                return View(nameof(Index));
            }
        }
    }
}
