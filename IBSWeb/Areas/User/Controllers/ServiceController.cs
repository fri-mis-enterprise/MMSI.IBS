using IBS.Models.MasterFile;
using System.Linq.Dynamic.Core;
using System.Security.Claims;
using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using IBS.Models.Enums;
using IBS.Utility.Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;

namespace IBSWeb.Areas.User.Controllers
{
    [Area("User")]
    public class ServiceController(
        ApplicationDbContext dbContext,
        UserManager<ApplicationUser> userManager,
        IUnitOfWork unitOfWork,
        ILogger<ServiceController> logger)
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

        public async Task<IActionResult> Index(string? view, CancellationToken cancellationToken)
        {
            if (view == nameof(DynamicView.ServiceMaster))
            {
                return View("ExportIndex");
            }

            return View(Enumerable.Empty<ServiceMaster>());
        }

        [HttpGet]
        public async Task<IActionResult> Create(CancellationToken cancellationToken)
        {
            var viewModel = new ServiceMaster
            {
                CurrentAndPreviousTitles = await dbContext.ChartOfAccounts
                    .Where(coa => coa.Level == 4 || coa.Level == 5)
                    .OrderBy(coa => coa.AccountId)
                    .Select(s => new SelectListItem
                    {
                        Value = s.AccountId.ToString(),
                        Text = s.AccountNumber + " " + s.AccountName
                    })
                    .ToListAsync(cancellationToken),
                UnearnedTitles = await dbContext.ChartOfAccounts
                    .Where(coa => coa.Level == 4 || coa.Level == 5)
                    .OrderBy(coa => coa.AccountId)
                    .Select(s => new SelectListItem
                    {
                        Value = s.AccountId.ToString(),
                        Text = s.AccountNumber + " " + s.AccountName
                    })
                    .ToListAsync(cancellationToken)
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ServiceMaster services, CancellationToken cancellationToken)
        {
            services.CurrentAndPreviousTitles = await dbContext.ChartOfAccounts
                .Where(coa => coa.Level == 4 || coa.Level == 5)
                .OrderBy(coa => coa.AccountId)
                .Select(s => new SelectListItem
                {
                    Value = s.AccountId.ToString(),
                    Text = s.AccountNumber + " " + s.AccountName
                })
                .ToListAsync(cancellationToken);

            services.UnearnedTitles = await dbContext.ChartOfAccounts
                .Where(coa => coa.Level == 4 || coa.Level == 5)
                .OrderBy(coa => coa.AccountId)
                .Select(s => new SelectListItem
                {
                    Value = s.AccountId.ToString(),
                    Text = s.AccountNumber + " " + s.AccountName
                })
                .ToListAsync(cancellationToken);

            if (!ModelState.IsValid)
            {
                return View(services);
            }

            var companyClaims = await GetCompanyClaimAsync();

            if (companyClaims == null)
            {
                return BadRequest();
            }

            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                if (await unitOfWork.ServiceMaster.IsServicesExist(services.Name,
                        companyClaims,
                        cancellationToken))
                {
                    ModelState.AddModelError("Name",
                        "Services already exist!");
                    return View(services);
                }

                var currentAndPrevious = await unitOfWork.ChartOfAccount
                    .GetAsync(x => x.AccountId == services.CurrentAndPreviousId,
                        cancellationToken);

                var unearned = await unitOfWork.ChartOfAccount
                    .GetAsync(x => x.AccountId == services.UnearnedId,
                        cancellationToken);

                services.CurrentAndPreviousNo = currentAndPrevious!.AccountNumber;
                services.CurrentAndPreviousTitle = currentAndPrevious.AccountName;
                services.UnearnedNo = unearned!.AccountNumber;
                services.UnearnedTitle = unearned.AccountName;
                services.Company = companyClaims;
                services.CreatedBy = GetUserFullName();
                services.ServiceNo = await unitOfWork.ServiceMaster.GetLastNumber(cancellationToken);
                await unitOfWork.ServiceMaster.AddAsync(services,
                    cancellationToken);

                #region --Audit Trail Recording

                AuditTrail auditTrailBook = new (GetUserFullName(),
                    $"Create ServiceMaster #{services.ServiceNo}",
                    "ServiceMaster");
                await unitOfWork.AuditTrail.AddAsync(auditTrailBook,
                    cancellationToken);

                #endregion --Audit Trail Recording

                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = "Services created successfully";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Failed to create service master file. Created by: {UserName}",
                    userManager.GetUserName(User));
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = $"Error: '{ex.Message}'";
                return View(services);
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetServicesList([FromForm] DataTablesParameters parameters, CancellationToken cancellationToken)
        {
            try
            {
                var query = await unitOfWork.ServiceMaster
                    .GetAllAsync(null,
                        cancellationToken);

                // Global search
                if (!string.IsNullOrEmpty(parameters.Search.Value))
                {
                    var searchValue = parameters.Search.Value.ToLower();

                    query = query
                    .Where(s =>
                        s.ServiceNo!.ToLower().Contains(searchValue) ||
                        s.Name.ToLower().Contains(searchValue) ||
                        s.Percent.ToString().ToLower().Contains(searchValue) ||
                        s.CreatedBy!.ToLower().Contains(searchValue) ||
                        s.CreatedDate.ToString("MM dd, yyyy").ToLower().Contains(searchValue)
                        ).ToList();
                }

                // Sorting
                if (parameters.Order?.Count > 0)
                {
                    var orderColumn = parameters.Order[0];
                    var columnName = parameters.Columns[orderColumn.Column].Data;
                    var sortDirection = orderColumn.Dir.ToLower() == "asc" ? "ascending" : "descending";
                    query = query
                        .AsQueryable()
                        .OrderBy($"{columnName} {sortDirection}");
                }

                var totalRecords = query.Count();
                var pagedData = query
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
                    "Failed to get services.");
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int? id, CancellationToken cancellationToken)
        {
            if (id == null)
            {
                return NotFound();
            }

            var services = await unitOfWork.ServiceMaster
                .GetAsync(x => x.ServiceId == id,
                    cancellationToken);

            if (services == null)
            {
                return NotFound();
            }
            return View(services);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ServiceMaster services, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return View(services);
            }

            var existingModel =  await unitOfWork.ServiceMaster
                .GetAsync(x => x.ServiceId == services.ServiceId,
                    cancellationToken);

            if (existingModel == null)
            {
                return NotFound();
            }

            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                existingModel.Name = services.Name;
                existingModel.Percent = services.Percent;
                await unitOfWork.SaveAsync(cancellationToken);

                #region --Audit Trail Recording

                AuditTrail auditTrailBook = new (GetUserFullName(),
                    $"Edited ServiceMaster #{existingModel.ServiceNo}",
                    "ServiceMaster");
                await unitOfWork.AuditTrail.AddAsync(auditTrailBook,
                    cancellationToken);

                #endregion --Audit Trail Recording

                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = "Services updated successfully";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException ex)
            {
                logger.LogError(ex,
                    "Failed to edit service master file. Edited by: {UserName}",
                    userManager.GetUserName(User));
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GetServiceList(CancellationToken cancellationToken)
        {
            try
            {
                var services = (await dbContext.Services.ToListAsync(cancellationToken))
                    .Select(x => new
                    {
                        x.ServiceId,
                        x.ServiceNo,
                        x.Name,
                        x.Percent,
                        x.CreatedBy,
                        x.CreatedDate
                    });

                return Json(new
                {
                    data = services
                });
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        //Download as .xlsx file.(Export)

        #region -- export xlsx record --

        [HttpPost]
        public async Task<IActionResult> Export(string selectedRecord)
        {
            if (string.IsNullOrEmpty(selectedRecord))
            {
                // Handle the case where no invoices are selected
                return RedirectToAction(nameof(Index));
            }

            var recordIds = selectedRecord.Split(',').Select(int.Parse).ToList();

            // Retrieve the selected invoices from the database
            var selectedList = await dbContext.Services
                .Where(service => recordIds.Contains(service.ServiceId))
                .OrderBy(service => service.ServiceId)
                .ToListAsync();

            // Create the Excel package
            using var package = new ExcelPackage();
            // Add a new worksheet to the Excel package
            var worksheet = package.Workbook.Worksheets.Add("Services");

            worksheet.Cells["A1"].Value = "CurrentAndPreviousTitle";
            worksheet.Cells["B1"].Value = "UneranedTitle";
            worksheet.Cells["C1"].Value = "Name";
            worksheet.Cells["D1"].Value = "Percent";
            worksheet.Cells["E1"].Value = "CreatedBy";
            worksheet.Cells["F1"].Value = "CreatedDate";
            worksheet.Cells["G1"].Value = "CurrentAndPreviousNo";
            worksheet.Cells["H1"].Value = "UnearnedNo";
            worksheet.Cells["I1"].Value = "OriginalServiceId";

            int row = 2;

            foreach (var item in selectedList)
            {
                worksheet.Cells[row,
                    1].Value = item.CurrentAndPreviousTitle;
                worksheet.Cells[row,
                    2].Value = item.UnearnedTitle;
                worksheet.Cells[row,
                    3].Value = item.Name;
                worksheet.Cells[row,
                    4].Value = item.Percent;
                worksheet.Cells[row,
                    5].Value = item.CreatedBy;
                worksheet.Cells[row,
                    6].Value = item.CreatedDate.ToString("yyyy-MM-dd HH:mm:ss.ffffff");
                worksheet.Cells[row,
                    7].Value = item.CurrentAndPreviousNo;
                worksheet.Cells[row,
                    8].Value = item.UnearnedNo;
                worksheet.Cells[row,
                    9].Value = item.ServiceId;

                row++;
            }

            //Set password in Excel
            worksheet.Protection.IsProtected = true;
            worksheet.Protection.SetPassword("mis123");

            // Convert the Excel package to a byte array
            var excelBytes = await package.GetAsByteArrayAsync();

            return File(excelBytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"ServiceList_IBS_{DateTimeHelper.GetCurrentPhilippineTime():yyyyddMMHHmmss}.xlsx");
        }

        #endregion -- export xlsx record --

        [HttpGet]
        public IActionResult GetAllServiceIds()
        {
            var serviceIds = dbContext.Services
                .Select(s => s.ServiceId)
                .ToList();

            return Json(serviceIds);
        }
    }
}
