using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using IBS.Models.Enums;
using IBS.Models.MSAP;
using IBS.Utility.Helpers;
using Microsoft.AspNetCore.Mvc;
using IBS.Services.Attributes;
using IBS.Services;
using IBS.Utility.Constants;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace IBSWeb.Areas.User.Controllers
{
    /// <summary>
    /// Controller for managing Billing in the MMSI system.
    /// </summary>
    [Area("User")]
    public class BillingController(
        IUnitOfWork unitOfWork,
        BillingService billingService,
        ILogger<BillingController> logger)
        : Controller
    {
        #region Index

        /// <summary>
        /// Displays the list of Billings.
        /// </summary>
        [RequireAnyAccess(
            "Access denied. You don't have permission to access Billings.",
            ProcedureEnum.CreateBilling,
            ProcedureEnum.EditBilling,
            ProcedureEnum.DeleteBilling)]
        public Task<IActionResult> Index(string filterType, CancellationToken cancellationToken)
        {
            try
            {
                ViewBag.FilterType = filterType;
                return Task.FromResult<IActionResult>(View(Enumerable.Empty<Billing>()));
            }
            catch (Exception exception)
            {
                return Task.FromException<IActionResult>(exception);
            }
        }

        #endregion

        #region Create

        /// <summary>
        /// Displays the form to create a new Billing.
        /// </summary>
        [HttpGet]
        [RequireAccess(ProcedureEnum.CreateBilling, "Access denied. You don't have permission to create Billings.")]
        public async Task<IActionResult> Create(CancellationToken cancellationToken)
        {
            var model = await billingService.PopulateBillingSelectListsAsync(new Billing(), cancellationToken);
            return View(model);
        }

        /// <summary>
        /// Processes the creation of a new Billing and posts it immediately.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequireAccess(ProcedureEnum.CreateBilling, "Access denied. You don't have permission to create Billings.")]
        public async Task<IActionResult> Create(Billing model, CancellationToken cancellationToken)
        {
            try
            {
                var username = User.Identity?.Name ?? "System";
                var company = User.Claims.FirstOrDefault(c => c.Type == "Company")?.Value ?? SD.Company_MMSI;

                var result = await billingService.CreateBillingAsync(model, username, company, cancellationToken);

                if (result.IsSuccess)
                {
                    var msg = model.IsUndocumented ? $"Created. Control No: {model.MsapBillingNumber}" : "Billing created successfully.";
                    return Json(new { success = true, message = msg, redirectUrl = Url.Action(nameof(Index)) });
                }

                return Json(new { success = false, message = result.Message });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to create billing.");
                return Json(new { success = false, message = ExceptionHelper.GetErrorMessage(ex) });
            }
        }

        #endregion

        #region Edit

        /// <summary>
        /// Displays the form to edit an existing Billing.
        /// </summary>
        [HttpGet]
        [RequireAccess(ProcedureEnum.EditBilling, "Access denied. You don't have permission to edit Billings.")]
        public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
        {
            var model = await billingService.GetBillingByIdAsync(id, cancellationToken);
            if (model == null)
            {
                return NotFound();
            }

            if (model.Status != SD.BillingStatus.ForPosting)
            {
                TempData["error"] = "Only billings with 'For Posting' status can be edited.";
                return RedirectToAction(nameof(Index));
            }

            model = await billingService.PopulateBillingSelectListsAsync(model, cancellationToken);
            model.UnbilledDispatchTickets = await billingService.GetEditTicketsSelectListAsync(model.CustomerId, model.MsapBillingId, cancellationToken);

            if (model.CustomerId != 0)
            {
                model.CustomerPrincipal = await billingService.GetPrincipalsSelectListAsync(model.CustomerId, cancellationToken);
            }

            model = await billingService.PopulateTicketListsAsync(model, cancellationToken);

            ViewData["HasPrincipal"] = model.CustomerPrincipal is { Count: > 0 };

            ViewData["CustomerAddress"] = model.Customer.CustomerAddress;
            ViewData["CustomerTIN"] = model.Customer.CustomerTin;
            ViewData["CustomerTerms"] = model.Customer.CustomerTerms;
            ViewData["CustomerBusinessStyle"] = model.Customer.BusinessStyle ?? "-";
            ViewData["CustomerVatType"] = model.Customer.VatType;
            ViewData["CustomerType"] = model.Customer.Type;
            ViewData["CustomerWht"] = model.Customer.WithHoldingTax;

            return View(model);
        }

        /// <summary>
        /// Processes the update of an existing Billing, including ticket reallocation.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequireAccess(ProcedureEnum.EditBilling, "Access denied. You don't have permission to edit Billings.")]
        public async Task<IActionResult> Edit([Bind("MsapBillingId,Date,IsUndocumented,BilledTo,VoyageNumber,COSNumber,Amount,IsPrincipal,CustomerId,PrincipalId,VesselId,PortId,TerminalId,ToBillDispatchTickets,ApOtherTug,JobOrderId,IsVatable,IsVatInclusive,PrintWht")] Billing model, IFormFile? file, CancellationToken cancellationToken)
        {
            try
            {
                var result = await billingService.UpdateBillingAsync(model, User.Identity?.Name ?? "System", cancellationToken);

                if (result.IsSuccess)
                {
                    TempData["success"] = result.Message ?? "Entry edited successfully!";
                    return RedirectToAction(nameof(Index));
                }

                if (result.Status == ServiceResultStatus.NotFound)
                {
                    return NotFound();
                }

                TempData["error"] = result.Message;
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to edit billing.");
                TempData["error"] = ExceptionHelper.GetErrorMessage(ex);
                return RedirectToAction(nameof(Index));
            }
        }

        #endregion

        #region Delete

        /// <summary>
        /// Deletes a specific Billing.
        /// </summary>
        [RequireAccess(ProcedureEnum.DeleteBilling, "Access denied. You don't have permission to delete Billings.")]
        public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
        {
            var result = await billingService.DeleteBillingAsync(id, cancellationToken);

            if (result.IsSuccess)
            {
                TempData["success"] = result.Message;
            }
            else
            {
                TempData["error"] = result.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        #endregion

        #region Post to Books

        /// <summary>
        /// Posts a specific Billing to the Sales Book and General Ledger.
        /// </summary>
        [RequireAccess(ProcedureEnum.CreateBilling, "Access denied. You don't have permission to post Billings.")]
        public async Task<IActionResult> Post(int id, CancellationToken cancellationToken)
        {
            var result = await billingService.PostBillingAsync(id, User.Identity?.Name ?? "Unknown", cancellationToken);

            if (result.IsSuccess)
            {
                TempData["success"] = result.Message;
            }
            else
            {
                TempData["error"] = result.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        #endregion

        #region Preview & Print

        /// <summary>
        /// Displays a preview of the Billing, including associated tickets and tugboats.
        /// </summary>
        [RequireAnyAccess("Access denied. You don't have permission to view Billings.", ProcedureEnum.CreateBilling)]
        public async Task<IActionResult> Preview(int id, CancellationToken cancellationToken)
        {
            var model = await billingService.GetBillingByIdAsync(id, cancellationToken);

            if (model == null)
            {
                return NotFound();
            }

            model = await billingService.PopulateTicketListsAsync(model, cancellationToken);
            return View(model);
        }

        /// <summary>
        /// Generates an Excel file for dot-matrix printing of the Billing.
        /// </summary>
        [RequireAccess(ProcedureEnum.CreateBilling, "Access denied. You don't have permission to print Billings.")]
        public async Task<IActionResult> Print(int id, CancellationToken cancellationToken)
        {
            try
            {
                var billing = await unitOfWork.Billing.GetAsync(b => b.MsapBillingId == id, cancellationToken);
                if (billing == null)
                {
                    return NotFound();
                }

                billing.PaidDispatchTickets = await unitOfWork.Billing.GetPaidDispatchTicketsAsync(billing.MsapBillingId, cancellationToken);
                billing.UniqueTugboats = await unitOfWork.Billing.GetUniqueTugboatsListAsync(billing.MsapBillingId, cancellationToken);

                using var package = new ExcelPackage();
                var worksheet = package.Workbook.Worksheets.Add($"Billing #{billing.MsapBillingNumber}");
                worksheet.Cells.Style.Font.Name = "Calibri";
                worksheet.Cells["B2"].Value = $"{billing.Customer?.CustomerName}";
                worksheet.Cells["E2"].Value = $"{billing.Date}";
                worksheet.Cells["E2"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                worksheet.Cells["B3"].Value = $"{billing.Customer?.CustomerAddress}                              TERMS: {billing.Customer?.CustomerTerms}";
                worksheet.Cells["B4"].Value = $"{billing.Customer?.CustomerTin}";
                worksheet.Cells["E4"].Value = $"VOYAGE NO. {billing.VoyageNumber}";
                worksheet.Cells["B6"].Value = $"FOR THE SERVICE RE: {billing.Vessel?.VesselName}";
                worksheet.Cells["B7"].Value = $"LOCATION PORT: {billing.Port?.PortName}";

                var row = 9;
                if (billing.UniqueTugboats != null)
                {
                    foreach (var tugboat in billing.UniqueTugboats)
                    {
                        worksheet.Cells[row, 2].Value = $"NAME OF TUGBOAT: {tugboat}";
                        row++;

                        foreach (var ticket in billing.PaidDispatchTickets!.Where(t => t.Tugboat?.TugboatName == tugboat))
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
                }

                if (billing.PaidDispatchTickets != null)
                {
                    foreach (var ticket in billing.PaidDispatchTickets.Where(t => t.BAFNetRevenue != 0))
                    {
                        worksheet.Cells[row, 2].Value = "NAME OF TUGBOAT: BUNKER ADJUSTMENT FACTOR";
                        row++;
                        worksheet.Cells[row, 1].Value = "1";
                        worksheet.Cells[row, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                        worksheet.Cells[row, 2].Value = $"{ticket.Service?.ServiceName}          {ticket.DateLeft} {ticket.TimeLeft}          {ticket.DateArrived} {ticket.TimeArrived}";
                        worksheet.Cells[row, 4].Value = $"{ticket.BAFRate}";
                        worksheet.Cells[row, 4].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                        worksheet.Cells[row, 5].Value = $"{ticket.BAFNetRevenue}";
                        worksheet.Cells[row, 5].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                        row++;
                    }
                }

                row++;

                var subTotal = billing.Amount;
                var vatAmount = 0m;
                var vatableSales = 0m;

                if (billing.IsVatable)
                {
                    vatableSales = subTotal / 1.12m;
                    vatAmount = subTotal - vatableSales;
                }

                worksheet.Cells[row, 4].Value = "SUBTOTAL";
                worksheet.Cells[row, 5].Value = subTotal;
                worksheet.Cells[row, 5].Style.Numberformat.Format = "#,##0.00";
                row++;

                if (billing.IsVatable)
                {
                    worksheet.Cells[row, 4].Value = "12% VAT";
                    worksheet.Cells[row, 5].Value = vatAmount;
                    worksheet.Cells[row, 5].Style.Numberformat.Format = "#,##0.00";
                    row++;
                }

                if (billing.PrintWht)
                {
                    var whtAmount = vatableSales * 0.02m;
                    worksheet.Cells[row, 4].Value = "LESS 2% WHT";
                    worksheet.Cells[row, 5].Value = whtAmount;
                    worksheet.Cells[row, 5].Style.Numberformat.Format = "(#,##0.00)";
                    row++;

                    decimal wvatAmount = 0;
                    if (billing.Customer?.WithHoldingVat == true)
                    {
                        wvatAmount = vatableSales * 0.05m;
                        worksheet.Cells[row, 4].Value = "LESS 5% WVAT";
                        worksheet.Cells[row, 5].Value = wvatAmount;
                        worksheet.Cells[row, 5].Style.Numberformat.Format = "(#,##0.00)";
                        row++;
                    }

                    worksheet.Cells[row, 4].Value = "NET AMOUNT DUE";
                    worksheet.Cells[row, 5].Value = subTotal - whtAmount - wvatAmount;
                    worksheet.Cells[row, 5].Style.Numberformat.Format = "#,##0.00";
                    worksheet.Cells[row, 5].Style.Font.Bold = true;
                }
                else
                {
                    worksheet.Cells[row, 4].Value = "TOTAL AMOUNT DUE";
                    worksheet.Cells[row, 5].Value = subTotal;
                    worksheet.Cells[row, 5].Style.Numberformat.Format = "#,##0.00";
                    worksheet.Cells[row, 5].Style.Font.Bold = true;
                }

                worksheet.Cells[1, 1, row, 7].Style.Font.Name = "Calibri";
                worksheet.Column(1).Width = 8;
                worksheet.Column(2).Width = 53;
                worksheet.Column(3).Width = 9;
                worksheet.Column(4).Width = 8.5;
                worksheet.Column(5).Width = 16;

                var bytes = await package.GetAsByteArrayAsync(cancellationToken);
                return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"DotMatrix_{DateTimeHelper.GetCurrentPhilippineTime():yyyyddMMHHmmss}.xlsx");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to print billing.");
                TempData["error"] = ExceptionHelper.GetErrorMessage(ex);
                return RedirectToAction(nameof(Index));
            }
        }

        #endregion

        #region AJAX Endpoints

        /// <summary>
        /// Retrieves detailed information for a list of Dispatch Tickets.
        /// </summary>
        [HttpPost]
        [RequireAnyAccess("Access denied.", ProcedureEnum.CreateBilling, ProcedureEnum.EditBilling)]
        public async Task<IActionResult> GetDispatchTickets(List<string> dispatchTicketIds)
        {
            try
            {
                var intDispatchTicketIds = dispatchTicketIds.Select(int.Parse).ToList();
                var dispatchTickets = await billingService.GetDispatchTicketsByIdsAsync(intDispatchTicketIds, cancellationToken: default);

                return Json(new { success = true, data = dispatchTickets });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to get dispatch tickets.");
                return Json(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Retrieves a paged and filtered list of Billings for DataTables.
        /// </summary>
        [HttpPost]
        [RequireAccess(ProcedureEnum.CreateBilling, "Access denied. You don't have permission to access Billings.")]
        public async Task<IActionResult> GetBillingList([FromForm] DataTablesParameters parameters, CancellationToken cancellationToken)
        {
            try
            {
                var (data, filtered, total) = await billingService.GetPagedBillingsAsync(parameters, cancellationToken);

                return Json(new
                {
                    draw = parameters.Draw,
                    recordsTotal = total,
                    recordsFiltered = filtered,
                    data
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to get billings.");
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// Searches for customers matching a search term.
        /// </summary>
        [HttpGet]
        [RequireAnyAccess("Access denied.", ProcedureEnum.CreateBilling, ProcedureEnum.EditBilling)]
        public async Task<JsonResult> SearchCustomers(string? term, CancellationToken cancellationToken)
        {
            var result = await unitOfWork.Customer.SearchCustomersDtoAsync(term ?? string.Empty, 10, cancellationToken);
            return Json(result);
        }

        /// <summary>
        /// Searches for principals associated with a specific customer.
        /// </summary>
        [HttpGet]
        [RequireAnyAccess("Access denied.", ProcedureEnum.CreateBilling, ProcedureEnum.EditBilling)]
        public async Task<JsonResult> SearchPrincipals(string? term, int customerId, CancellationToken cancellationToken)
        {
            var result = await billingService.SearchPrincipalsAsync(term, customerId, cancellationToken);
            return Json(result);
        }

        /// <summary>
        /// Searches for Job Orders for a customer that have unbilled tickets and are ready for billing.
        /// </summary>
        [HttpGet]
        [RequireAnyAccess("Access denied.", ProcedureEnum.CreateBilling, ProcedureEnum.EditBilling)]
        public async Task<JsonResult> SearchJobOrders(string? term, int customerId, CancellationToken cancellationToken)
        {
            var result = await billingService.SearchJobOrdersAsync(term, customerId, cancellationToken);
            return Json(result);
        }

        /// <summary>
        /// Retrieves unbilled 'For Billing' tickets associated with a specific Job Order.
        /// </summary>
        [HttpGet]
        [RequireAnyAccess("Access denied.", ProcedureEnum.CreateBilling, ProcedureEnum.EditBilling)]
        public async Task<JsonResult> GetDispatchTicketsByJobOrder(int jobOrderId, CancellationToken cancellationToken)
        {
            var result = await billingService.GetDispatchTicketsByJobOrderAsync(jobOrderId, cancellationToken);

            if (result is { IsSuccess: true, Data: not null })
            {
                return Json(new
                {
                    success = true,
                    header = result.Data.Header,
                    tickets = result.Data.Tickets
                });
            }

            return Json(new { success = false, message = result.Message });
        }

        /// <summary>
        /// Retrieves principals for a customer as a JSON list.
        /// </summary>
        [HttpGet]
        [RequireAnyAccess("Access denied.", ProcedureEnum.CreateBilling, ProcedureEnum.EditBilling)]
        public async Task<IActionResult> GetPrincipalsJson(string customerId, CancellationToken cancellationToken)
        {
            var principalsList = await billingService.GetPrincipalsSelectListAsync(int.Parse(customerId), cancellationToken);
            return Json(principalsList);
        }

        /// <summary>
        /// Retrieves unbilled tickets for a customer that have no job order.
        /// </summary>
        [HttpGet]
        [RequireAnyAccess("Access denied.", ProcedureEnum.CreateBilling, ProcedureEnum.EditBilling)]
        public async Task<IActionResult> GetDispatchTicketsByCustomer(string customerId, CancellationToken cancellationToken)
        {
            var result = await billingService.GetDispatchTicketsByCustomerAsync(int.Parse(customerId), cancellationToken);

            if (result is { IsSuccess: true, Data: not null })
            {
                return Json(new
                {
                    success = true,
                    header = result.Data.Header,
                    tickets = result.Data.Tickets
                });
            }

            return Json(new { success = false, message = result.Message });
        }

        /// <summary>
        /// Retrieves detailed information for a specific customer.
        /// </summary>
        [HttpPost]
        [RequireAnyAccess("Access denied.", ProcedureEnum.CreateBilling, ProcedureEnum.EditBilling)]
        public async Task<IActionResult> GetCustomerDetails(int customerId, CancellationToken cancellationToken)
        {
            var result = await billingService.GetCustomerDetailsAsync(customerId, cancellationToken);
            if (result.IsSuccess)
            {
                return Json(result.Data);
            }

            return result.Status == ServiceResultStatus.NotFound ? NotFound() : BadRequest(result.Message);
        }

        #endregion


    }
}


