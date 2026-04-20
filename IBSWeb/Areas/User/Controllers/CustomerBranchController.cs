using IBS.Models.MasterFile;
using System.Linq.Dynamic.Core;
using System.Security.Claims;
using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace IBSWeb.Areas.User.Controllers
{
    [Area("User")]
    public class CustomerBranchController(
        IUnitOfWork unitOfWork,
        ILogger<CustomerController> logger,
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext dbContext)
        : Controller
    {
        private string GetUserFullName()
        {
            return User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.GivenName)?.Value
                   ?? User.Identity?.Name!;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Create(CancellationToken cancellationToken = default)
        {

            var model = new CustomerBranch
            {
                CustomerSelectList = await unitOfWork.GetCustomerListAsyncById(cancellationToken)
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CustomerBranch model, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                ModelState.AddModelError("",
                    "Make sure to fill all the required details.");
                model.CustomerSelectList =
                    await unitOfWork.GetCustomerListAsyncById(cancellationToken);
                return View(model);
            }

            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var customer = await unitOfWork.Customer
                    .GetAsync(x => x.CustomerId == model.CustomerId,
                        cancellationToken);

                if (customer == null)
                {
                    return NotFound();
                }

                customer.HasBranch = true;
                await unitOfWork.CustomerBranch.AddAsync(model,
                    cancellationToken);

                #region --Audit Trail Recording

                AuditTrail auditTrailBook = new (GetUserFullName(),
                    $"Created Customer Branch #{model.Id}",
                    "Customer Branch");
                await unitOfWork.AuditTrail.AddAsync(auditTrailBook,
                    cancellationToken);

                #endregion --Audit Trail Recording

                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = "Customer branch created successfully";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Failed to create customer branch master file. Created by: {UserName}",
                    userManager.GetUserName(User));
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                model.CustomerSelectList = await unitOfWork.GetCustomerListAsyncById(cancellationToken);
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int? id, CancellationToken cancellationToken)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }

            var branch = await unitOfWork.CustomerBranch.GetAsync(b => b.Id == id,
                cancellationToken);

            if (branch == null)
            {
                return NotFound();
            }

            branch.CustomerSelectList = await unitOfWork.GetCustomerListAsyncById(cancellationToken);
            return View(branch);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(CustomerBranch model, CancellationToken cancellationToken)
        {
            await GetCompanyClaimAsync();

            if (!ModelState.IsValid)
            {
                ModelState.AddModelError("",
                    "Make sure to fill all the required details.");
                model.CustomerSelectList =
                    await unitOfWork.GetCustomerListAsyncById(cancellationToken);
                return View(model);
            }

            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                await unitOfWork.CustomerBranch.UpdateAsync(model,
                    cancellationToken);

                #region --Audit Trail Recording

                AuditTrail auditTrailBook = new (GetUserFullName(),
                    $"Edited Customer Branch #{model.Id}",
                    "Customer Branch");
                await unitOfWork.AuditTrail.AddAsync(auditTrailBook,
                    cancellationToken);

                #endregion --Audit Trail Recording

                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = "Customer branch updated successfully";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Failed to edit customer branch. Created by: {UserName}",
                    userManager.GetUserName(User));
                TempData["error"] = $"Error: '{ex.Message}'";
                await transaction.RollbackAsync(cancellationToken);
                model.CustomerSelectList =
                    await unitOfWork.GetCustomerListAsyncById(cancellationToken);
                return View(model);
            }
        }

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

        [HttpPost]
        public async Task<IActionResult> GetCustomerBranchesList([FromForm] DataTablesParameters parameters, CancellationToken cancellationToken)
        {
            try
            {
                var query = await unitOfWork.CustomerBranch
                    .GetAllAsync(null,
                        cancellationToken);

                // Global search
                if (!string.IsNullOrEmpty(parameters.Search.Value))
                {
                    var searchValue = parameters.Search.Value.ToLower();

                    query = query
                    .Where(b =>
                        b.BranchName.ToLower().Contains(searchValue) ||
                        b.BranchAddress.ToLower().Contains(searchValue) ||
                        b.BranchTin.ToLower().Contains(searchValue) ||
                        b.Customer!.CustomerName.ToLower().Contains(searchValue)
                        ).ToList();
                }

                // Sorting
                if (parameters.Order?.Count > 0)
                {
                    var orderColumn = parameters.Order[0];
                    var columnName = parameters.Columns[orderColumn.Column].Name;
                    var sortDirection = orderColumn.Dir.ToLower() == "asc" ? "ascending" : "descending";
                    query = query
                        .AsQueryable()
                        .OrderBy($"{columnName} {sortDirection}");
                }

                var totalRecords = query.Count();
                var pagedData = query
                    .Select(b  => new
                    {
                        b.Id,
                        b.Customer!.CustomerName,
                        b.BranchName,
                        b.BranchAddress,
                        b.BranchTin,
                    })
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
                logger.LogError(ex,
                    "Failed to get customer branches.");
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetCustomerDetails(int customerId, CancellationToken cancellationToken)
        {
            try
            {
                var customer = await unitOfWork.Customer
                    .GetAsync(c => c.CustomerId == customerId,
                        cancellationToken);

                if (customer == null)
                {
                    TempData["error"] = "Customer not found";
                }

                return Json(new
                {
                    address = customer!.CustomerAddress,
                    tin = customer.CustomerTin,
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Failed to get dispatch tickets.");
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }
    }
}
