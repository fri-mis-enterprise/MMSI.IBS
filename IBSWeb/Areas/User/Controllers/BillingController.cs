using System.Linq.Dynamic.Core;
using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using IBS.Models.Enums;
using IBS.Models.MMSI;
using IBS.Services;
using IBS.Models.MMSI.ViewModels;
using IBS.Utility.Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using IBS.Services.Attributes;

namespace IBSWeb.Areas.User.Controllers
{
    [Area("User")]
    public class BillingController(
        IUnitOfWork unitOfWork,
        ApplicationDbContext dbContext,
        UserManager<ApplicationUser> userManager,
        ILogger<BillingController> logger)
        : Controller
    {
        [RequireAccess(ProcedureEnum.CreateBilling)]
        public async Task<IActionResult> Index(string filterType, CancellationToken cancellationToken)
        {
            ViewBag.FilterType = filterType;
            return View(Enumerable.Empty<Billing>());
        }

        [HttpGet]
        [RequireAccess(ProcedureEnum.CreateBilling)]
        public async Task<IActionResult> Create(CancellationToken cancellationToken)
        {
            var viewModel = await GetBillingSelectLists(new CreateBillingViewModel(), cancellationToken);
            return View(viewModel);
        }

        [HttpPost]
        [RequireAccess(ProcedureEnum.CreateBilling)]
        public async Task<IActionResult> Create(CreateBillingViewModel viewModel, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return Failure(message: "Can't create entry, please review your input.", data: new { errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });
            }

            try
            {
                var model = CreateBillingVmToBillingModel(viewModel);

                if (model.CustomerId == null)
                {
                    throw new InvalidOperationException("Customer is required.");
                }

                model.Customer = await unitOfWork.Customer.GetAsync(c => c.CustomerId == model.CustomerId, cancellationToken)
                                 ?? throw new InvalidOperationException("Customer not found.");

                model.IsVatable = model.Customer.VatType == "Vatable";
                model.Status = "For Collection";
                model.CreatedBy = await GetUserNameAsync() ?? "System";
                model.Company = await GetCompanyClaimAsync() ?? "MMSI";
                model.CreatedDate = DateTimeHelper.GetCurrentPhilippineTime();

                model.Terms = model.PrincipalId != null
                    ? (await unitOfWork.Principal.GetAsync(p => p.PrincipalId == model.PrincipalId, cancellationToken))?.Terms
                    : model.Customer?.CustomerTerms;

                if (string.IsNullOrEmpty(model.Terms))
                {
                    model.Terms = "COD";
                }

                model.DueDate = await unitOfWork.Billing.ComputeDueDateAsync(model.Terms, model.Date, cancellationToken);

                if (model.IsUndocumented)
                {
                    model.MMSIBillingNumber = await unitOfWork.Billing.GenerateBillingNumber(cancellationToken);
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(viewModel.MMSIBillingNumber))
                    {
                        throw new InvalidOperationException("Billing Number is required.");
                    }

                    model.MMSIBillingNumber = viewModel.MMSIBillingNumber;
                }

                if (model.ToBillDispatchTickets == null || !model.ToBillDispatchTickets.Any())
                {
                    throw new InvalidOperationException("At least one dispatch ticket must be selected.");
                }

                await unitOfWork.Billing.AddAsync(model, cancellationToken);

                await unitOfWork.AuditTrail.AddAsync(new AuditTrail(model.CreatedBy, $"Create billing #{model.MMSIBillingNumber} for tickets #{string.Join(", #", model.ToBillDispatchTickets!)}", "Billing"), cancellationToken);

                decimal total = 0, dispatch = 0, baf = 0;
                foreach (var ticketIdStr in model.ToBillDispatchTickets!)
                {
                    var dt = await unitOfWork.DispatchTicket.GetAsync(t => t.DispatchTicketId == int.Parse(ticketIdStr), cancellationToken)
                        ?? throw new InvalidOperationException($"Dispatch ticket #{ticketIdStr} not found.");

                    total += dt.TotalNetRevenue;
                    dispatch += dt.DispatchNetRevenue;
                    baf += dt.BAFNetRevenue;

                    dt.Status = "Billed";
                    dt.BillingId = model.MMSIBillingId;
                    dt.BillingNumber = model.MMSIBillingNumber;
                }

                model.Amount = model.Balance = total;
                model.DispatchAmount = dispatch;
                model.BAFAmount = baf;
                model.IsPaid = false;

                await unitOfWork.SaveAsync(cancellationToken);
                await unitOfWork.Billing.PostAsync(model, cancellationToken);

                return Success(model.IsUndocumented ? $"Created. Control No: {model.MMSIBillingNumber}" : $"Billing #{model.MMSIBillingNumber} created.",
                    new { redirectUrl = Url.Action(nameof(Index)) });
            }
            catch (Exception ex)
            {
                return Failure(ex, "Failed to create billing.");
            }
        }

        public Billing CreateBillingVmToBillingModel(CreateBillingViewModel viewModel)
        {
            var model = new Billing
            {
                Date = viewModel.Date,
                IsUndocumented = viewModel.IsUndocumented,
                BilledTo = viewModel.BilledTo,
                VoyageNumber = viewModel.VoyageNumber,
                Amount = viewModel.Amount,
                IsPrincipal = viewModel.IsPrincipal,
                CustomerId = viewModel.CustomerId,
                PrincipalId = viewModel.PrincipalId,
                VesselId = viewModel.VesselId,
                PortId = viewModel.PortId,
                TerminalId = viewModel.TerminalId,
                ToBillDispatchTickets = viewModel.ToBillDispatchTickets,
                ApOtherTug = viewModel.ApOtherTug
            };

            if (viewModel.MMSIBillingId != null)
            {
                model.MMSIBillingId = viewModel.MMSIBillingId ?? 0;

                if (model.MMSIBillingId == 0)
                {
                    throw new NullReferenceException("MMSIBillingId cannot be zero.");
                }
            }

            return model;
        }

        public CreateBillingViewModel BillingModelToCreateBillingVm(Billing model)
        {
            var viewModel = new CreateBillingViewModel
            {
                MMSIBillingId = model.MMSIBillingId,
                MMSIBillingNumber = model.MMSIBillingNumber,
                Date = model.Date,
                IsUndocumented = model.IsUndocumented,
                BilledTo = model.BilledTo,
                VoyageNumber = model.VoyageNumber,
                Amount = model.Amount,
                IsPrincipal = model.IsPrincipal,
                CustomerId = model.CustomerId,
                PrincipalId = model.PrincipalId,
                VesselId = model.VesselId,
                PortId = model.PortId,
                TerminalId = model.TerminalId,
                ToBillDispatchTickets = model.ToBillDispatchTickets,
                ApOtherTug = model.ApOtherTug
            };

            return viewModel;
        }

        [HttpPost]
        [RequireAnyAccess(ProcedureEnum.CreateBilling, ProcedureEnum.EditBilling)]
        public async Task<IActionResult> GetDispatchTickets(List<string> dispatchTicketIds)
        {
            try
            {
                var intDispatchTicketIds = dispatchTicketIds.Select(int.Parse).ToList();
                var dispatchTickets = await unitOfWork.DispatchTicket
                    .GetAllAsync(t => intDispatchTicketIds.Contains(t.DispatchTicketId));

                return Json(new
                {
                    success = true,
                    data = dispatchTickets
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to get dispatch tickets.");

                return Json(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        [HttpPost]
        [RequireAccess(ProcedureEnum.CreateBilling)]
        public async Task<IActionResult> GetBillingList([FromForm] DataTablesParameters parameters, CancellationToken cancellationToken)
        {
            try
            {
                var queried = dbContext.Billings
                    .Include(b => b.Customer)
                    .Include(b => b.Terminal)
                    .ThenInclude(b => b!.Port)
                    .Include(b => b.Vessel)
                    .Where(b => b.Status != "For Posting" && b.Status != "Cancelled");

                if (!string.IsNullOrEmpty(parameters.Search.Value))
                {
                    var searchValue = parameters.Search.Value.ToLower();

                    queried = queried
                        .Where(dt =>
                            dt.Date.Day.ToString().Contains(searchValue) == true ||
                            dt.Date.Month.ToString().Contains(searchValue) == true ||
                            dt.Date.Year.ToString().Contains(searchValue) == true ||
                            dt.MMSIBillingNumber.ToLower().Contains(searchValue) == true ||
                            dt.Amount.ToString().Contains(searchValue) == true ||
                            dt.Customer!.CustomerName.ToLower().Contains(searchValue) == true ||
                            dt.Terminal!.TerminalName!.ToLower().Contains(searchValue) == true ||
                            dt.Terminal.Port!.PortName!.ToLower().Contains(searchValue) == true ||
                            dt.Vessel!.VesselName.ToLower().Contains(searchValue) == true ||
                            dt.Status.ToLower().Contains(searchValue) == true
                        );
                }

                foreach (var column in parameters.Columns)
                {
                    if (!string.IsNullOrEmpty(column.Search.Value))
                    {
                        var searchValue = column.Search.Value.ToLower();

                        switch (column.Data)
                        {
                            case "status":
                                if (searchValue == "for collection")
                                {
                                    queried = queried.Where(s => s.Status == "For Collection");
                                }
                                if (searchValue == "collected")
                                {
                                    queried = queried.Where(s => s.Status == "Collected");
                                }
                                break;
                        }
                    }
                }

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
                logger.LogError(ex, "Failed to get billings.");
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpGet]
        [RequireAccess(ProcedureEnum.EditBilling)]
        public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
        {
            var model = await unitOfWork.Billing
                .GetAsync(b => b.MMSIBillingId == id, cancellationToken) ?? throw new NullReferenceException();

            var viewModel = BillingModelToCreateBillingVm(model);
            viewModel = await GetBillingSelectLists(viewModel, cancellationToken);
            viewModel.UnbilledDispatchTickets = await GetEditTickets(viewModel.CustomerId, viewModel.MMSIBillingId ?? 0, cancellationToken);
            if(model.CustomerId != null)
            {
                viewModel.CustomerPrincipal = await GetPrincipals(model.CustomerId.ToString(), cancellationToken);
            }

            viewModel.Terminals = await unitOfWork.Terminal
                .GetMMSITerminalsSelectList(viewModel.PortId, cancellationToken);

            viewModel.ToBillDispatchTickets = await unitOfWork.Billing
                .GetToBillDispatchTicketListAsync(model.MMSIBillingId, cancellationToken);

            if (viewModel.CustomerPrincipal?.Count == 0 || viewModel.CustomerPrincipal == null)
            {
                ViewData["HasPrincipal"] = false;
            }
            else
            {
                ViewData["HasPrincipal"] = true;
            }

            // Customer Details for display
            if (model.Customer != null)
            {
                ViewData["CustomerAddress"] = model.Customer.CustomerAddress;
                ViewData["CustomerTIN"] = model.Customer.CustomerTin;
                ViewData["CustomerTerms"] = model.Customer.CustomerTerms;
                ViewData["CustomerBusinessStyle"] = model.Customer.BusinessStyle ?? "-";
                ViewData["CustomerVatType"] = model.Customer.VatType;
                ViewData["CustomerType"] = model.Customer.Type;
            }

            return View(viewModel);
        }

        [HttpPost]
        [RequireAccess(ProcedureEnum.EditBilling)]
        public async Task<IActionResult> Edit(CreateBillingViewModel viewModel, IFormFile? file, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return Failure(message: "Can't update entry, please review your input.", data: new { errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });
            }

            try
            {
                var model = CreateBillingVmToBillingModel(viewModel);
                var currentModel = await unitOfWork.Billing.GetAsync(b => b.MMSIBillingId == model.MMSIBillingId, cancellationToken)
                    ?? throw new InvalidOperationException("Billing not found.");

                model.Customer = await unitOfWork.Customer.GetAsync(c => c.CustomerId == model.CustomerId, cancellationToken)
                    ?? throw new InvalidOperationException("Customer not found.");
                model.IsVatable = model.Customer.VatType == "Vatable";

                // Revert old tickets
                var oldTickets = await unitOfWork.DispatchTicket.GetAllAsync(dt => dt.BillingId == model.MMSIBillingId, cancellationToken);
                foreach (var dt in oldTickets)
                {
                    dt.Status = "For Billing";
                    dt.BillingId = null;
                    dt.BillingNumber = null;
                }
                await unitOfWork.SaveAsync(cancellationToken);

                // Update current model
                currentModel.CustomerId = model.CustomerId;
                currentModel.PrincipalId = model.PrincipalId;
                currentModel.VoyageNumber = model.VoyageNumber;
                currentModel.Date = model.Date;
                currentModel.PortId = model.PortId;
                currentModel.TerminalId = model.TerminalId;
                currentModel.VesselId = model.VesselId;
                currentModel.BilledTo = model.BilledTo;
                currentModel.IsVatable = model.IsVatable;

                decimal total = 0, dispatch = 0, baf = 0;
                foreach (var ticketIdStr in model.ToBillDispatchTickets!)
                {
                    var dt = await unitOfWork.DispatchTicket.GetAsync(t => t.DispatchTicketId == int.Parse(ticketIdStr), cancellationToken)
                        ?? throw new InvalidOperationException($"Dispatch ticket #{ticketIdStr} not found.");

                    total += dt.TotalNetRevenue;
                    dispatch += dt.DispatchNetRevenue;
                    baf += dt.BAFNetRevenue;

                    dt.Status = "Billed";
                    dt.BillingId = model.MMSIBillingId;
                    dt.BillingNumber = currentModel.MMSIBillingNumber;
                }

                currentModel.Amount = currentModel.Balance = total;
                currentModel.DispatchAmount = dispatch;
                currentModel.BAFAmount = baf;

                await unitOfWork.AuditTrail.AddAsync(new AuditTrail(await GetUserNameAsync() ?? "System", $"Edit billing #{currentModel.MMSIBillingNumber}", "Billing"), cancellationToken);
                await unitOfWork.SaveAsync(cancellationToken);

                return Success("Entry edited successfully!", new { redirectUrl = Url.Action(nameof(Index)) });
            }
            catch (Exception ex)
            {
                return Failure(ex, "Failed to edit billing.");
            }
        }

        private JsonResult Success(string message, object? data = null)
        {
            var redirectUrl = data?.GetType().GetProperty("redirectUrl")?.GetValue(data);
            return Json(new { success = true, message, redirectUrl });
        }

        private JsonResult Failure(Exception? ex = null, string? message = null, object? data = null)
        {
            if (ex != null)
            {
                logger.LogError(ex, message ?? "An error occurred.");
            }

            var finalMessage = message ?? "Operation failed.";
            if (ex != null)
            {
                var errorMsg = ex.InnerException?.Message ?? ex.Message;
                if (errorMsg.Contains("unique") || errorMsg.Contains("23505"))
                {
                    finalMessage = "Billing number already exists.";
                }
                else if (errorMsg.Contains("foreign key") || errorMsg.Contains("23503"))
                {
                    finalMessage = "Invalid reference selected.";
                }
                else
                {
                    finalMessage = ex.Message;
                }
            }

            var errors = data?.GetType().GetProperty("errors")?.GetValue(data);
            return Json(new { success = false, message = finalMessage, errors });
        }

        [RequireAccess(ProcedureEnum.DeleteBilling)]
        public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
        {
            try
            {
                var model = await unitOfWork.Billing
                    .GetAsync(b => b.MMSIBillingId == id, cancellationToken);

                if (model != null)
                {
                    await unitOfWork.Billing.RemoveAsync(model, cancellationToken);
                    TempData["success"] = "Billing deleted successfully!";
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    TempData["error"] = "Can't find entry.";
                    return RedirectToAction(nameof(Index));
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to delete billing.");
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        [RequireAccess(ProcedureEnum.CreateBilling)]
        public async Task<IActionResult> Preview(int id, CancellationToken cancellationToken)
        {
            var model = await unitOfWork.Billing.GetAsync(b => b.MMSIBillingId == id, cancellationToken);

            if (model == null)
            {
                return NotFound();
            }

            model.ToBillDispatchTickets = await unitOfWork.Billing
                .GetToBillDispatchTicketListAsync(model.MMSIBillingId, cancellationToken);

            model.PaidDispatchTickets = await unitOfWork.Billing
                .GetPaidDispatchTicketsAsync(model.MMSIBillingId, cancellationToken);

            model.UniqueTugboats = await unitOfWork.Billing
                .GetUniqueTugboatsListAsync(model.MMSIBillingId, cancellationToken) ?? throw new NullReferenceException();

            model = unitOfWork.Billing.ProcessAddress(model, cancellationToken);
            return View(model);
        }

        [RequireAccess(ProcedureEnum.CreateBilling)]
        public async Task<IActionResult> Print(int id, CancellationToken cancellationToken)
        {
            try
            {
                var billing = await unitOfWork.Billing
                    .GetAsync(b => b.MMSIBillingId == id, cancellationToken);

                if (billing == null)
                {
                    TempData["error"] = "Billing not found";
                    return RedirectToAction(nameof(Index));
                }

                billing.ToBillDispatchTickets = await unitOfWork.Billing
                    .GetToBillDispatchTicketListAsync(billing.MMSIBillingId, cancellationToken);

                billing.PaidDispatchTickets = await unitOfWork.Billing
                    .GetPaidDispatchTicketsAsync(billing.MMSIBillingId, cancellationToken) ?? throw new NullReferenceException();

                billing.UniqueTugboats = await unitOfWork.Billing
                    .GetUniqueTugboatsListAsync(billing.MMSIBillingId, cancellationToken) ?? throw new NullReferenceException();

                using var package = new ExcelPackage();
                var worksheet = package.Workbook.Worksheets.Add($"Billing #{billing.MMSIBillingNumber}");
                worksheet.Cells.Style.Font.Name = "Calibri";
                worksheet.Cells["B2"].Value = $"{billing.Customer?.CustomerName}";
                worksheet.Cells["E2"].Value = $"{billing.Date}";
                worksheet.Cells["E2"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                worksheet.Cells["B3"].Value = $"{billing.Customer?.CustomerAddress}                              TERMS: {billing.Customer?.CustomerTerms}";
                worksheet.Cells["B4"].Value = $"{billing.Customer?.CustomerTin}";
                worksheet.Cells["E4"].Value = $"VOYAGE NO. {billing.VoyageNumber}";
                worksheet.Cells["B6"].Value = $"FOR THE SERVICE RE: {billing.Vessel?.VesselName}";
                worksheet.Cells["B7"].Value = $"LOCATION PORT: {billing.Port!.PortName}";
                var rowStart = 9;
                var row = rowStart;

                foreach (var tugboat in billing.UniqueTugboats)
                {
                    worksheet.Cells[row, 2].Value = $"NAME OF TUGBOAT: {tugboat}";
                    row++;

                    foreach (var ticket in billing.PaidDispatchTickets.Where(t => t.Tugboat?.TugboatName == tugboat))
                    {
                        worksheet.Cells[row, 1].Value = "1";
                        worksheet.Cells[row, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                        worksheet.Cells[row, 2].Value = $"{ticket.Service?.ServiceName}          {ticket.DateLeft} {ticket.TimeLeft}          {ticket.DateArrived} {ticket.TimeArrived}";
                        worksheet.Cells[row, 4].Value = $"{ticket.DispatchRate}";
                        worksheet.Cells[row, 4].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                        worksheet.Cells[row, 5].Value = $"{ticket.DispatchBillingAmount}";
                        worksheet.Cells[row, 5].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                        row++;
                    }

                    row++;
                }

                var ticketsWithBaf = billing.PaidDispatchTickets;

                if (ticketsWithBaf != null)
                {
                    foreach (var ticket in billing.PaidDispatchTickets.Where(t => t.BAFNetRevenue != 0))
                    {
                        worksheet.Cells[row, 2].Value = $"NAME OF TUGBOAT: BUNKER ADJUSTMENT FACTOR";
                        row++;

                        foreach (var record in billing.PaidDispatchTickets.Where(t => t.BAFNetRevenue != 0))
                        {
                            worksheet.Cells[row, 1].Value = "1";
                            worksheet.Cells[row, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                            worksheet.Cells[row, 2].Value = $"{ticket.Service?.ServiceName}          {ticket.DateLeft} {ticket.TimeLeft}          {ticket.DateArrived} {ticket.TimeArrived}";
                            worksheet.Cells[row, 4].Value = $"{ticket.BAFRate}";
                            worksheet.Cells[row, 4].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                            worksheet.Cells[row, 5].Value = $"{ticket.BAFNetRevenue}";
                            worksheet.Cells[row, 5].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                            row++;
                        }

                        row++;
                    }
                }

                worksheet.Cells[1, 1, row, 7].Style.Font.Name = "Calibri";
                worksheet.Column(1).Width = 8;
                worksheet.Column(2).Width = 53;
                worksheet.Column(3).Width = 9;
                worksheet.Column(4).Width = 8.5;
                worksheet.Column(5).Width = 16;
                var excelBytes = await package.GetAsByteArrayAsync(cancellationToken);
                return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"DotMatrix_{DateTimeHelper.GetCurrentPhilippineTime():yyyyddMMHHmmss}.xlsx");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to print billing.");
                TempData["error"] = ex.Message;
                logger.LogError(ex, "Error generating sales report. Error: {ErrorMessage}, Stack: {StackTrace}. Posted by: {UserName}",
                ex.Message, ex.StackTrace, userManager.GetUserAsync(User));
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [RequireAnyAccess(ProcedureEnum.CreateBilling, ProcedureEnum.EditBilling)]
        public async Task<JsonResult> GetCustomerDetails(int customerId)
        {
            var customerDetails = await unitOfWork.Customer
                .GetAsync(c => c.CustomerId == customerId);

            if (customerDetails == null)
            {
                return new JsonResult("Customer not found");
            }

            var principal = await unitOfWork.Principal
                .GetAsync(c => c.CustomerId == customerId);

            var hasPrincipal = principal != null;

            var customerDetailsJson = new
            {
                terms = customerDetails.CustomerTerms,
                address = customerDetails.CustomerAddress,
                tinNo = customerDetails.CustomerTin,
                businessStyle = customerDetails.BusinessStyle,
                hasPrincipal,
                vatType = customerDetails.VatType,
                isUndoc = customerDetails.Type
            };

            return Json(customerDetailsJson);
        }

        [HttpGet]
        [RequireAnyAccess(ProcedureEnum.CreateBilling, ProcedureEnum.EditBilling)]
        public async Task<JsonResult> SearchCustomers(string? term, CancellationToken cancellationToken)
        {
            var query = dbContext.Customers.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(term))
            {
                var lowerTerm = term.ToLower();
                query = query.Where(c => c.CustomerName.ToLower().Contains(lowerTerm) ||
                                         c.CustomerCode!.ToLower().Contains(lowerTerm));
            }

            var customers = await query
                .OrderBy(c => c.CustomerName)
                .Take(10)
                .Select(c => new
                {
                    value = c.CustomerId,
                    name = c.CustomerName,
                    vatType = c.VatType,
                    isUndoc = c.Type
                })
                .ToListAsync(cancellationToken);

            var customerIds = customers.Select(c => c.value).ToList();
            var principalsExist = await dbContext.MMSIPrincipals
                .Where(p => customerIds.Contains(p.CustomerId))
                .Select(p => p.CustomerId)
                .Distinct()
                .ToListAsync(cancellationToken);

            var result = customers.Select(c => new
            {
                c.value,
                c.name,
                hasPrincipal = principalsExist.Contains(c.value),
                c.vatType,
                c.isUndoc
            }).ToList();

            return Json(result);
        }

        [HttpGet]
        [RequireAnyAccess(ProcedureEnum.CreateBilling, ProcedureEnum.EditBilling)]
        public async Task<JsonResult> SearchPrincipals(string? term, int customerId, CancellationToken cancellationToken)
        {
            var query = dbContext.MMSIPrincipals.AsNoTracking().Where(p => p.CustomerId == customerId);

            if (!string.IsNullOrWhiteSpace(term))
            {
                var lowerTerm = term.ToLower();
                query = query.Where(p => p.PrincipalName.ToLower().Contains(lowerTerm) ||
                                         p.PrincipalNumber.ToLower().Contains(lowerTerm));
            }

            var result = await query
                .OrderBy(p => p.PrincipalName)
                .Take(10)
                .Select(p => new
                {
                    value = p.PrincipalId,
                    name = p.PrincipalName
                })
                .ToListAsync(cancellationToken);

            return Json(result);
        }

        [HttpGet]
        [RequireAnyAccess(ProcedureEnum.CreateBilling, ProcedureEnum.EditBilling)]
        public async Task<JsonResult> GetPrincipalDetails(int principalId)
        {
            var customerDetails = await unitOfWork.Principal
                .GetAsync(c => c.PrincipalId == principalId);

            if (customerDetails == null)
            {
                return new JsonResult("Principal not found");
            }

            var customerDetailsJson = new
            {
                terms = customerDetails.Terms,
                address = customerDetails.Address,
                tinNo = customerDetails.TIN,
                businessStyle = customerDetails.BusinessType
            };

            return Json(customerDetailsJson);
        }

        [HttpGet]
        [RequireAnyAccess(ProcedureEnum.CreateBilling, ProcedureEnum.EditBilling)]
        public async Task<JsonResult> SearchJobOrders(string? term, int customerId, CancellationToken cancellationToken)
        {
            var query = dbContext.MMSIJobOrders.AsNoTracking()
                .Where(j => j.CustomerId == customerId &&
                            j.DispatchTickets.Any(dt => dt.Status == "For Billing" && dt.BillingId == null));

            if (!string.IsNullOrWhiteSpace(term))
            {
                var lowerTerm = term.ToLower();
                query = query.Where(j => j.JobOrderNumber.ToLower().Contains(lowerTerm));
            }

            var result = await query
                .OrderByDescending(j => j.Date)
                .Take(10)
                .Select(j => new
                {
                    value = j.JobOrderId,
                    name = j.JobOrderNumber,
                    description = j.Remarks ?? ""
                })
                .ToListAsync(cancellationToken);

            return Json(result);
        }

        [HttpGet]
        [RequireAnyAccess(ProcedureEnum.CreateBilling, ProcedureEnum.EditBilling)]
        public async Task<JsonResult> GetDispatchTicketsByJobOrder(int jobOrderId, CancellationToken cancellationToken)
        {
            var tickets = await unitOfWork.DispatchTicket
                .GetAllAsync(t => t.JobOrderId == jobOrderId &&
                    t.Status == "For Billing" &&
                    t.BillingId == null,
                    cancellationToken);

            var result = tickets.Select(t => new
            {
                dispatchTicketId = t.DispatchTicketId,
                dispatchNo = t.DispatchNumber,
                tugboat = t.Tugboat?.TugboatName ?? "N/A",
                service = t.Service?.ServiceName ?? "N/A",
                duration = t.TotalHours,
                dispatchAmount = t.DispatchBillingAmount,
                bafAmount = t.BAFBillingAmount,
                totalAmount = t.TotalBilling
            }).ToList();

            return Json(result);
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

        private async Task<string?> GetUserNameAsync()
        {
            var user = await userManager.GetUserAsync(User);
            return user?.UserName;
        }

        [HttpGet]
        [RequireAnyAccess(ProcedureEnum.CreateBilling, ProcedureEnum.EditBilling)]
        public async Task<IActionResult> GetPrincipalsJson(string customerId, CancellationToken cancellationToken)
        {
            var principalsList = await GetPrincipals(customerId, cancellationToken);
            return Json(principalsList);
        }

        [HttpGet]
        public async Task<List<SelectListItem>?> GetPrincipals(string? customerId, CancellationToken cancellationToken)
        {
            if (customerId == null)
            {
                return null;
            }

            var principals = await unitOfWork.Principal
                .GetAllAsync(t => t.CustomerId == int.Parse(customerId), cancellationToken);

            var principalsList = principals.Select(t => new SelectListItem
            {
                Value = t.PrincipalId.ToString(),
                Text = t.PrincipalName
            }).ToList();

            return principalsList;
        }

        [HttpGet]
        [RequireAnyAccess(ProcedureEnum.CreateBilling, ProcedureEnum.EditBilling)]
        public async Task<IActionResult> GetDispatchTicketsByCustomer(string customerId, CancellationToken cancellationToken)
        {
            //order by dispatch number
            var dispatchTickets = await unitOfWork.DispatchTicket
                .GetAllAsync(t => t.CustomerId == int.Parse(customerId) && t.Status == "For Billing", cancellationToken);

            var principalsList = dispatchTickets.Select(t => new SelectListItem
            {
                Value = t.DispatchTicketId.ToString(),
                Text = t.DispatchNumber
            }).ToList();

            return Json(principalsList);
        }

        [HttpPost]
        public async Task<List<SelectListItem>?> GetEditTickets(int? customerId, int billingId, CancellationToken cancellationToken = default)
        {
            var listToReturn = await unitOfWork.Billing.GetMMSIUnbilledTicketsByCustomer(customerId, cancellationToken);
            IEnumerable<DispatchTicket>? billedTickets = null;

            if (billingId != 0)
            {
                billedTickets = await unitOfWork.DispatchTicket
                    .GetAllAsync(dt => dt.BillingId == billingId, cancellationToken);
            }

            if (billedTickets != null && billedTickets.FirstOrDefault()?.CustomerId == customerId)
            {
                listToReturn?.AddRange(await unitOfWork.Billing.GetMMSIBilledTicketsById(billingId, cancellationToken));
            }

            return listToReturn;
        }

        public async Task<CreateBillingViewModel> GetBillingSelectLists(CreateBillingViewModel viewModel, CancellationToken cancellationToken = default)
        {
            viewModel.Vessels = await unitOfWork.Vessel.GetMMSIVesselsSelectList(cancellationToken);
            viewModel.Ports = await unitOfWork.Port.GetMMSIPortsSelectList(cancellationToken);
            viewModel.Customers = await unitOfWork.Billing.GetMMSICustomersWithBillablesSelectList(viewModel.CustomerId, "", cancellationToken);

            if (viewModel.PortId != 0)
            {
                viewModel.Terminals = await unitOfWork.Terminal.GetMMSITerminalsSelectList(viewModel.PortId, cancellationToken);
            }

            return viewModel;
        }
    }
}
