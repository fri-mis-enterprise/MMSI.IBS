using IBS.Models.MasterFile;
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
    public class PickupPointController(
        IUnitOfWork unitOfWork,
        ILogger<PickupPointController> logger,
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext dbContext)
        : Controller
    {
        private string GetUserFullName()
        {
            return User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.GivenName)?.Value
                   ?? User.Identity?.Name!;
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

        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            var pickupPoints = await dbContext.PickUpPoints
                .Include(p => p.Supplier)
                .ToListAsync(cancellationToken);

            return View(pickupPoints);
        }

        [HttpGet]
        public async Task<IActionResult> Create(CancellationToken cancellationToken)
        {
            var companyClaims = await GetCompanyClaimAsync();

            if (companyClaims == null)
            {
                return BadRequest();
            }

            var model = new PickUpPoint
            {
                Suppliers = await unitOfWork.Supplier.GetTradeSupplierListAsyncById(companyClaims, cancellationToken),
                Company = companyClaims
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PickUpPoint model, CancellationToken cancellationToken)
        {
            var companyClaims = await GetCompanyClaimAsync();

            if (companyClaims == null)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                model.Suppliers = await unitOfWork.Supplier.GetTradeSupplierListAsyncById(companyClaims, cancellationToken);
                ModelState.AddModelError("", "Make sure to fill all the required details.");
                return View(model);
            }

            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                model.CreatedBy = GetUserFullName();
                model.CreatedDate = DateTimeHelper.GetCurrentPhilippineTime();
                await unitOfWork.PickUpPoint.AddAsync(model, cancellationToken);
                await unitOfWork.SaveAsync(cancellationToken);

                #region --Audit Trail Recording

                AuditTrail auditTrailBook = new (GetUserFullName(),
                    $"Created Pickup Point #{model.Depot}","Pickup Point", (await GetCompanyClaimAsync())! );
                await unitOfWork.AuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion --Audit Trail Recording

                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = "Pickup point created successfully";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to create pickup point master file. Created by: {UserName}", userManager.GetUserName(User));
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                return View(model);
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetPickupPointsList([FromForm] DataTablesParameters parameters, CancellationToken cancellationToken)
        {
            try
            {
                var query = await unitOfWork.PickUpPoint
                    .GetAllAsync(null, cancellationToken);

                // Global search
                if (!string.IsNullOrEmpty(parameters.Search.Value))
                {
                    var searchValue = parameters.Search.Value.ToLower();

                    query = query
                    .Where(p =>
                        p.Depot.ToLower().Contains(searchValue) ||
                        p.Supplier!.SupplierName.ToLower().Contains(searchValue)
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
                    .Select(p => new
                    {
                        p.PickUpPointId,
                        p.Depot,
                        p.Supplier!.SupplierName
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
                logger.LogError(ex, "Failed to get pickup points.");
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int? id, CancellationToken cancellationToken)
        {
            try
            {
                if (id == null || id == 0)
                {
                    return NotFound();
                }

                var companyClaims = await GetCompanyClaimAsync();

                if (companyClaims == null)
                {
                    return BadRequest();
                }

                var model = await unitOfWork.PickUpPoint
                    .GetAsync(p => p.PickUpPointId == id, cancellationToken);

                if (model == null)
                {
                    return NotFound();
                }

                model.Suppliers = await unitOfWork.Supplier.GetTradeSupplierListAsyncById(companyClaims, cancellationToken);

                return View(model);
            }
            catch (Exception ex)
            {
                TempData["error"] = $"Error: '{ex.Message}'";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(PickUpPoint model, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var selected = await unitOfWork.PickUpPoint
                .GetAsync(p => p.PickUpPointId == model.PickUpPointId, cancellationToken);

            if (selected == null)
            {
                return NotFound();
            }

            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                #region -- Audit Trail Recording --

                AuditTrail auditTrailBook = new(GetUserFullName(),
                    $"Edited pickup point {selected.Depot} to {model.Depot}", "Customer", model.Company);
                await unitOfWork.AuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion --Audit Trail Recording --

                selected.Depot = model.Depot;
                selected.SupplierId = model.SupplierId;
                await unitOfWork.SaveAsync(cancellationToken);

                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = "Pickup point updated successfully";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to edit pickup point master file. Edited by: {UserName}", userManager.GetUserName(User));
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = $"Error: '{ex.Message}'";
                return View(model);
            }
        }
    }
}
