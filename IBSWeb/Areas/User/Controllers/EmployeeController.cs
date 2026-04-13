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
    public class EmployeeController(
        ApplicationDbContext dbContext,
        UserManager<ApplicationUser> userManager,
        IUnitOfWork unitOfWork,
        ILogger<EmployeeController> logger)
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

        private string GetUserFullName()
        {
            return User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.GivenName)?.Value
                   ?? User.Identity?.Name!;
        }

        public IActionResult Index()
        {
            var getEmployeeModel = dbContext.Employees
                .Where(x => x.IsActive)
                .ToList();
            return View(getEmployeeModel);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Employee model, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                TempData["warning"] = "The submitted information is invalid.";
                return View(model);
            }

            var companyClaims = await GetCompanyClaimAsync();

            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                model.Company = companyClaims;
                await unitOfWork.Employee.AddAsync(model, cancellationToken);

                #region --Audit Trail Recording

                AuditTrail auditTrailBook = new (GetUserFullName(),
                    $"Created new Employee #{model.EmployeeNumber}", "Employee", (await GetCompanyClaimAsync())! );
                await unitOfWork.AuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion --Audit Trail Recording

                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = $"Employee {model.EmployeeNumber} created successfully";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to create employee. Error: {ErrorMessage}, Stack: {StackTrace}. Created by: {UserName}", ex.Message, ex.StackTrace, User.Identity!.Name);
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                return View(model);
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetEmployeesList([FromForm] DataTablesParameters parameters, CancellationToken cancellationToken)
        {
            try
            {
                var queried = await unitOfWork.Employee
                    .GetAllAsync(null, cancellationToken);

                // Global search
                if (!string.IsNullOrEmpty(parameters.Search.Value))
                {
                    var searchValue = parameters.Search.Value.ToLower();

                    queried = queried
                    .Where(e =>
                        e.EmployeeNumber.ToLower().Contains(searchValue) ||
                        e.Initial?.ToLower().Contains(searchValue) == true ||
                        e.FirstName.ToLower().Contains(searchValue) ||
                        e.LastName.ToLower().Contains(searchValue) ||
                        e.BirthDate?.ToString().Contains(searchValue) == true ||
                        e.TelNo?.ToLower().Contains(searchValue) == true ||
                        e.Department?.ToLower().Contains(searchValue) == true ||
                        e.Position.ToLower().Contains(searchValue)
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
                logger.LogError(ex, "Failed to get employee.");
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
        {
            var existingEmployee = await unitOfWork.Employee
                .GetAsync(x => x.EmployeeId == id, cancellationToken);

            return View(existingEmployee);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Employee model, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                TempData["warning"] = "The submitted information is invalid.";
                return View(model);
            }

            var existingModel = await unitOfWork.Employee
                .GetAsync(x => x.EmployeeId == model.EmployeeId, cancellationToken);

            if (existingModel == null)
            {
                return NotFound();
            }

            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                #region --Audit Trail Recording

                AuditTrail auditTrailBook = new (GetUserFullName(),
                    $"Edited Employee #{existingModel.EmployeeNumber} => {model.EmployeeNumber}", "Employee", (await GetCompanyClaimAsync())! );
                await unitOfWork.AuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion --Audit Trail Recording

                #region -- Saving Default

                existingModel.EmployeeNumber = model.EmployeeNumber;
                existingModel.Initial = model.Initial;
                existingModel.FirstName = model.FirstName;
                existingModel.MiddleName = model.MiddleName;
                existingModel.LastName = model.LastName;
                existingModel.Suffix = model.Suffix;
                existingModel.BirthDate = model.BirthDate;
                existingModel.TelNo = model.TelNo;
                existingModel.SssNo = model.SssNo;
                existingModel.TinNo = model.TinNo;
                existingModel.PhilhealthNo = model.PhilhealthNo;
                existingModel.PagibigNo = model.PagibigNo;
                existingModel.Department = model.Department;
                existingModel.DateHired = model.DateHired;
                existingModel.DateResigned = model.DateResigned;
                existingModel.Position = model.Position;
                existingModel.IsManagerial = model.IsManagerial;
                existingModel.Supervisor = model.Supervisor;
                existingModel.Salary = model.Salary;
                existingModel.IsActive = model.IsActive;
                existingModel.Status = model.Status;
                existingModel.Address = model.Address;
                await unitOfWork.SaveAsync(cancellationToken);

                #endregion -- Saving Default

                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = "Employee edited successfully";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to edit employee. Error: {ErrorMessage}, Stack: {StackTrace}. Edited by: {UserName}", ex.Message, ex.StackTrace, userManager.GetUserName(User));
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                return View(model);
            }
        }
    }
}
