using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using IBS.Models.Books;
using IBS.Models.Enums;
using IBS.Models.MSAP;
using IBS.Utility.Constants;
using IBS.Utility.Helpers;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using IBS.DTOs;

namespace IBS.Services
{
    public class BillingService(
        IUnitOfWork unitOfWork,
        JobOrderService jobOrderService,
        ILogger<BillingService> logger,
        INotificationService notificationService)
    {
        public async Task<Billing?> GetBillingByIdAsync(int id, CancellationToken cancellationToken)
        {
            return await unitOfWork.Billing.GetAsync(b => b.MsapBillingId == id, cancellationToken);
        }

        public async Task<ServiceResult<int>> CreateBillingAsync(Billing model, string username, string company, CancellationToken cancellationToken)
        {
            try
            {
                if (model.JobOrderId is 0)
                {
                    model.JobOrderId = null;
                }

                if (model.JobOrderId.HasValue)
                {
                    var jobOrder = await unitOfWork.JobOrder.GetJobOrderWithDetailsAsync(model.JobOrderId.Value, cancellationToken);
                    if (jobOrder != null)
                    {
                        if (model.CustomerId == 0)
                        {
                            model.CustomerId = jobOrder.CustomerId;
                        }

                        if (model.VesselId == 0)
                        {
                            model.VesselId = jobOrder.VesselId;
                        }

                        if (model.PortId == 0)
                        {
                            model.PortId = jobOrder.PortId;
                        }

                        if (model.TerminalId == 0)
                        {
                            model.TerminalId = jobOrder.TerminalId;
                        }

                        if (string.IsNullOrWhiteSpace(model.VoyageNumber))
                        {
                            model.VoyageNumber = jobOrder.VoyageNumber;
                        }

                        if (string.IsNullOrWhiteSpace(model.COSNumber))
                        {
                            model.COSNumber = jobOrder.COSNumber;
                        }

                        // Block billing if any ticket under this Job Order is not yet ready for billing
                        var unreadyStatuses = new[]
                        {
                            SD.DispatchTicketStatus.Draft,
                            SD.DispatchTicketStatus.Requested,
                            SD.DispatchTicketStatus.Pending,
                            SD.DispatchTicketStatus.ForTariff,
                            SD.DispatchTicketStatus.ForApproval
                        };

                        var hasUnreadyTickets = jobOrder.DispatchTickets?
                            .Any(dt => unreadyStatuses.Contains(dt.Status)) == true;

                        if (hasUnreadyTickets)
                        {
                            var unreadyList = jobOrder.DispatchTickets!
                                .Where(dt => unreadyStatuses.Contains(dt.Status))
                                .Select(dt => $"#{dt.DispatchNumber} ({dt.Status})")
                                .ToList();

                            return ServiceResult<int>.Failure(
                                $"Cannot create billing — the following ticket(s) under Job Order #{jobOrder.JobOrderNumber} are not yet ready: {string.Join(", ", unreadyList)}.");
                        }
                    }
                }


                var customer = await unitOfWork.Customer.GetAsync(c => c.CustomerId == model.CustomerId, cancellationToken);
                if (customer == null)
                {
                    return ServiceResult<int>.Failure("Customer not found.");
                }

                model.Customer = customer;
                model.IsVatable = customer.VatType == SD.VatType_Vatable;
                model.Status = SD.BillingStatus.ForPosting; // Changed from ForCollection
                model.CreatedBy = username;
                model.CreatedDate = DateTimeHelper.GetCurrentPhilippineTime();
                model.Company = company;

                if (model.PrincipalId.HasValue && model.PrincipalId != 0)
                {
                    var principal = await unitOfWork.Principal.GetAsync(p => p.PrincipalId == model.PrincipalId.Value, cancellationToken);
                    if (principal != null)
                    {
                        model.Principal = principal;
                    }
                }

                model.Terms = model.PrincipalId != null && model.PrincipalId != 0
                    ? model.Principal?.Terms
                    : model.Customer?.CustomerTerms;

                if (string.IsNullOrEmpty(model.Terms))
                {
                    model.Terms = "COD";
                }

                model.DueDate = await unitOfWork.Billing.ComputeDueDateAsync(model.Terms, model.Date, cancellationToken);

                if (model.IsUndocumented)
                {
                    model.MsapBillingNumber = await unitOfWork.Billing.GenerateBillingNumber(cancellationToken);
                }
                else if (string.IsNullOrWhiteSpace(model.MsapBillingNumber))
                {
                    return ServiceResult<int>.Failure("Billing Number is required.");
                }

                if (model.ToBillDispatchTickets == null || !model.ToBillDispatchTickets.Any())
                {
                    return ServiceResult<int>.Failure("At least one dispatch ticket must be selected.");
                }

                decimal total = 0, dispatch = 0, baf = 0;
                foreach (var ticketIdStr in model.ToBillDispatchTickets)
                {
                    var dt = await unitOfWork.DispatchTicket.GetAsync(t => t.DispatchTicketId == int.Parse(ticketIdStr), cancellationToken);
                    if (dt == null)
                    {
                        return ServiceResult<int>.Failure($"Dispatch ticket #{ticketIdStr} not found.");
                    }

                    if (model.JobOrderId.HasValue && dt.JobOrderId != model.JobOrderId)
                    {
                        return ServiceResult<int>.Failure($"Ticket #{dt.DispatchNumber} does not belong to the selected Job Order.");
                    }

                    total += dt.TotalNetRevenue;
                    dispatch += dt.DispatchNetRevenue;
                    baf += dt.BAFNetRevenue;

                    dt.Billing = model;
                    dt.BillingNumber = model.MsapBillingNumber;
                }

                model.Amount = model.Balance = model.IsVatable && !model.IsVatInclusive ? total * 1.12m : total;
                model.DispatchAmount = dispatch;
                model.BAFAmount = baf;
                model.IsPaid = false;

                await unitOfWork.Billing.AddAsync(model, cancellationToken);
                await unitOfWork.AuditTrail.AddAsync(new AuditTrail(username, $"Created Billing #{model.MsapBillingNumber}", "Billing", model.MsapBillingId, model.MsapBillingNumber), cancellationToken);
                await unitOfWork.SaveAsync(cancellationToken);

                // Notify Accounting for Posting
                await notificationService.NotifyByAccessAsync(
                    ProcedureEnum.ViewGeneralLedger,
                    $"New Billing <b>#{model.MsapBillingNumber}</b> for <b>{customer.CustomerName}</b> has been created. Ready for Posting.",
                    targetUrl: "/User/Billing/Index",
                    cancellationToken: cancellationToken);

                return ServiceResult<int>.Success(model.MsapBillingId, "Billing created successfully. Status: For Posting");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to create billing.");
                return ServiceResult<int>.Failure($"Failed to create billing: {ExceptionHelper.GetErrorMessage(ex)}");
            }
        }

        public async Task<ServiceResult> PostBillingAsync(int id, string username, CancellationToken cancellationToken)
        {
            try
            {
                await unitOfWork.ExecuteInTransactionAsync(async () =>
                {
                    var model = await unitOfWork.Billing.GetAsync(b => b.MsapBillingId == id, cancellationToken);
                    if (model == null)
                    {
                        throw new InvalidOperationException("Billing not found.");
                    }

                    if (model.Status != SD.BillingStatus.ForPosting)
                    {
                        throw new InvalidOperationException($"Billing #{model.MsapBillingNumber} is already {model.Status}.");
                    }

                    // Mark linked tickets as Billed
                    var linkedTickets = await unitOfWork.DispatchTicket.GetAllAsync(dt => dt.BillingId == model.MsapBillingId, cancellationToken);
                    foreach (var dt in linkedTickets)
                    {
                        dt.Status = SD.DispatchTicketStatus.Billed;
                    }

                    var customer = await unitOfWork.Customer.GetAsync(c => c.CustomerId == model.CustomerId, cancellationToken);
                    if (customer == null) throw new InvalidOperationException("Customer not found.");
                    model.Customer = customer;

                    if (model.PrincipalId.HasValue && model.PrincipalId != 0)
                    {
                        model.Principal = await unitOfWork.Principal.GetAsync(p => p.PrincipalId == model.PrincipalId.Value, cancellationToken);
                    }
                    model.Vessel = await unitOfWork.Vessel.GetAsync(v => v.VesselId == model.VesselId, cancellationToken) ?? null!;

                    var soldToName = (model.PrincipalId != null ? model.Principal?.PrincipalName : customer.CustomerName) ?? string.Empty;
                    var tinNo = (model.PrincipalId != null ? model.Principal?.TIN : customer.CustomerTin) ?? string.Empty;
                    var address = (model.PrincipalId != null ? model.Principal?.Address1 : customer.CustomerAddress) ?? string.Empty;

                    var salesBook = new SalesBook
                    {
                        TransactionDate = model.Date,
                        SerialNo = model.MsapBillingNumber,
                        SoldTo = soldToName,
                        TinNo = tinNo,
                        Address = address,
                        Description = model.Vessel?.VesselName ?? "Maritime Services",
                        Amount = model.Amount - model.Discount
                    };

                    if (model.IsVatable)
                    {
                        salesBook.VatableSales = unitOfWork.Billing.ComputeNetOfVat(salesBook.Amount);
                        salesBook.VatAmount = unitOfWork.Billing.ComputeVatAmount(salesBook.VatableSales);
                        salesBook.NetSales = salesBook.VatableSales - salesBook.Discount;
                    }
                    else
                    {
                        salesBook.ZeroRated = salesBook.Amount;
                        salesBook.NetSales = salesBook.ZeroRated - salesBook.Discount;
                    }

                    salesBook.Discount = model.Discount;
                    salesBook.CreatedBy = username;
                    salesBook.CreatedDate = DateTimeHelper.GetCurrentPhilippineTime();
                    salesBook.DueDate = model.DueDate;
                    salesBook.DocumentId = model.MsapBillingId;
                    salesBook.Company = model.Company;

                    // --- General Ledger Posting ---
                    var ledgers = new List<GeneralLedgerBook>();
                    var accountTitlesDto = await unitOfWork.Billing.GetListOfAccountTitleDto(cancellationToken);

                    var arTrade = accountTitlesDto.Find(c => c.AccountNumber == SD.MsapAccounts.ArTrade);
                    var revenue = accountTitlesDto.Find(c => c.AccountNumber == SD.MsapAccounts.MaritimeServiceRevenue);
                    var outputVat = accountTitlesDto.Find(c => c.AccountNumber == SD.MsapAccounts.OutputVat);

                    if (arTrade == null) throw new InvalidOperationException($"Accounting setup incomplete: Account '{SD.MsapAccounts.ArTrade}' (AR Trade) not found in Chart of Accounts.");
                    if (revenue == null) throw new InvalidOperationException($"Accounting setup incomplete: Account '{SD.MsapAccounts.MaritimeServiceRevenue}' (Service Revenue) not found in Chart of Accounts.");
                    if (model.IsVatable && outputVat == null) throw new InvalidOperationException($"Accounting setup incomplete: Account '{SD.MsapAccounts.OutputVat}' (Output VAT) not found in Chart of Accounts.");

                    // 1. Debit AR Trade
                    ledgers.Add(new GeneralLedgerBook
                    {
                        Date = model.Date,
                        Reference = model.MsapBillingNumber,
                        Description = $"Billing for {model.Vessel?.VesselName ?? "Maritime Services"}",
                        AccountId = arTrade!.AccountId,
                        AccountNo = arTrade.AccountNumber,
                        AccountTitle = arTrade.AccountName,
                        Debit = salesBook.Amount,
                        Credit = 0,
                        Company = model.Company,
                        CreatedBy = username,
                        CreatedDate = salesBook.CreatedDate,
                        ModuleType = nameof(ModuleType.Sales),
                        SubAccountType = SubAccountType.Customer,
                        SubAccountId = model.CustomerId,
                        SubAccountName = customer.CustomerName ?? string.Empty
                    });

                    // 2. Credit Service Revenue
                    ledgers.Add(new GeneralLedgerBook
                    {
                        Date = model.Date,
                        Reference = model.MsapBillingNumber,
                        Description = $"Billing for {model.Vessel?.VesselName ?? "Maritime Services"}",
                        AccountId = revenue!.AccountId,
                        AccountNo = revenue.AccountNumber,
                        AccountTitle = revenue.AccountName,
                        Debit = 0,
                        Credit = model.IsVatable ? salesBook.VatableSales : salesBook.ZeroRated,
                        Company = model.Company,
                        CreatedBy = username,
                        CreatedDate = salesBook.CreatedDate,
                        ModuleType = nameof(ModuleType.Sales)
                    });

                    // 3. Credit Output VAT
                    if (model.IsVatable)
                    {
                        ledgers.Add(new GeneralLedgerBook
                        {
                            Date = model.Date,
                            Reference = model.MsapBillingNumber,
                            Description = $"Output VAT for {model.MsapBillingNumber}",
                            AccountId = outputVat!.AccountId,
                            AccountNo = outputVat.AccountNumber,
                            AccountTitle = outputVat.AccountName,
                            Debit = 0,
                            Credit = salesBook.VatAmount,
                            Company = model.Company,
                            CreatedBy = username,
                            CreatedDate = salesBook.CreatedDate,
                            ModuleType = nameof(ModuleType.Sales)
                        });
                    }

                    if (!unitOfWork.Billing.IsJournalEntriesBalanced(ledgers))
                    {
                        throw new InvalidOperationException("Accounting error: Journal entries are not balanced.");
                    }

                    model.Status = SD.BillingStatus.ForCollection;

                    await unitOfWork.AuditTrail.AddAsync(new AuditTrail(username, $"Posted Billing #{model.MsapBillingNumber}", "Billing", model.MsapBillingId, model.MsapBillingNumber), cancellationToken);
                    await unitOfWork.SaveAsync(cancellationToken);

                    // Notify Collection
                    await notificationService.NotifyByAccessAsync(
                        ProcedureEnum.CreateCollection,
                        $"Billing <b>#{model.MsapBillingNumber}</b> for <b>{customer.CustomerName}</b> has been posted. Ready for Collection.",
                        targetUrl: "/User/Collection/Index",
                        cancellationToken: cancellationToken);

                    if (model.JobOrderId.HasValue)
                    {
                        await jobOrderService.TryAutoCloseAsync(model.JobOrderId.Value, username, cancellationToken);
                    }
                }, cancellationToken);

                return ServiceResult.Success($"Billing posted successfully.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to post billing {BillingId}", id);
                return ServiceResult.Failure($"Failed to post billing: {ExceptionHelper.GetErrorMessage(ex)}");
            }
        }

        public async Task<ServiceResult> UpdateBillingAsync(Billing model, string username, CancellationToken cancellationToken)
        {
            try
            {
                var currentModel = await unitOfWork.Billing.GetAsync(b => b.MsapBillingId == model.MsapBillingId, cancellationToken);
                if (currentModel == null)
                {
                    return ServiceResult.Failure("Billing not found.", ServiceResultStatus.NotFound);
                }

                if (currentModel.Status != SD.BillingStatus.ForPosting)
                {
                    return ServiceResult.Failure("Only billings with 'For Posting' status can be edited.");
                }

                var customer = await unitOfWork.Customer.GetAsync(c => c.CustomerId == model.CustomerId, cancellationToken);
                if (customer == null)
                {
                    return ServiceResult.Failure("Customer not found.");
                }

                // Update ticket billing references only when ticket selection was submitted
                if (model.ToBillDispatchTickets != null)
                {
                    var oldTickets = await unitOfWork.DispatchTicket.GetAllAsync(dt => dt.BillingId == model.MsapBillingId, cancellationToken);
                    foreach (var dt in oldTickets)
                    {
                        dt.BillingId = null;
                        dt.BillingNumber = null;
                    }
                }

                // Update properties
                currentModel.IsVatable = model.IsVatable;
                currentModel.CustomerId = model.CustomerId;
                currentModel.PrincipalId = model.PrincipalId;
                currentModel.VoyageNumber = model.VoyageNumber;
                currentModel.COSNumber = model.COSNumber;
                currentModel.Date = model.Date;
                currentModel.PortId = model.PortId;
                currentModel.TerminalId = model.TerminalId;
                currentModel.VesselId = model.VesselId;
                currentModel.BilledTo = model.BilledTo;
                currentModel.JobOrderId = model.JobOrderId;
                currentModel.IsVatInclusive = model.IsVatInclusive;
                currentModel.PrintWht = model.PrintWht;

                if (model.ToBillDispatchTickets != null)
                {
                    decimal total = 0, dispatch = 0, baf = 0;
                    foreach (var ticketIdStr in model.ToBillDispatchTickets)
                    {
                        var dt = await unitOfWork.DispatchTicket.GetAsync(t => t.DispatchTicketId == int.Parse(ticketIdStr), cancellationToken);
                        if (dt == null)
                        {
                            return ServiceResult.Failure($"Dispatch ticket #{ticketIdStr} not found.");
                        }

                        total += dt.TotalNetRevenue;
                        dispatch += dt.DispatchNetRevenue;
                        baf += dt.BAFNetRevenue;

                        dt.BillingId = currentModel.MsapBillingId;
                        dt.Billing = currentModel;
                        dt.BillingNumber = currentModel.MsapBillingNumber;
                    }

                    currentModel.Amount = currentModel.Balance = currentModel.IsVatable && !currentModel.IsVatInclusive ? total * 1.12m : total;
                    currentModel.DispatchAmount = dispatch;
                    currentModel.BAFAmount = baf;
                }

                await unitOfWork.AuditTrail.AddAsync(new AuditTrail(username, $"Edit billing #{currentModel.MsapBillingNumber}", "Billing", currentModel.MsapBillingId, currentModel.MsapBillingNumber), cancellationToken);
                await unitOfWork.SaveAsync(cancellationToken);

                return ServiceResult.Success("Entry edited successfully!");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to edit billing.");
                return ServiceResult.Failure($"Failed to edit billing: {ExceptionHelper.GetErrorMessage(ex)}");
            }
        }

        public async Task<ServiceResult> DeleteBillingAsync(int id, CancellationToken cancellationToken)
        {
            try
            {
                var model = await unitOfWork.Billing.GetAsync(b => b.MsapBillingId == id, cancellationToken);
                if (model == null)
                {
                    return ServiceResult.Failure("Billing not found.", ServiceResultStatus.NotFound);
                }

                var linkedTickets = await unitOfWork.DispatchTicket
                    .GetAllAsync(dt => dt.BillingId == id, cancellationToken);
                foreach (var dt in linkedTickets)
                {
                    dt.BillingId = null;
                    dt.BillingNumber = null;
                }

                await unitOfWork.Billing.RemoveAsync(model, cancellationToken);
                await unitOfWork.SaveAsync(cancellationToken);

                // Re-open the Job Order if it was auto-closed
                var jobOrderId = linkedTickets.FirstOrDefault()?.JobOrderId;
                if (jobOrderId.HasValue)
                {
                    var jobOrder = await unitOfWork.JobOrder.GetAsync(jo => jo.JobOrderId == jobOrderId.Value, cancellationToken);
                    if (jobOrder?.Status == SD.JobOrderStatus.Closed)
                    {
                        jobOrder.Status = SD.JobOrderStatus.Open;
                        await unitOfWork.SaveAsync(cancellationToken);
                    }
                }

                return ServiceResult.Success("Billing deleted successfully!");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to delete billing.");
                return ServiceResult.Failure($"Failed to delete billing: {ExceptionHelper.GetErrorMessage(ex)}");
            }
        }

        public async Task<(IEnumerable<Billing> Data, int RecordsFiltered, int TotalRecords)> GetPagedBillingsAsync(DataTablesParameters parameters, CancellationToken cancellationToken)
        {
            return await unitOfWork.Billing.GetPagedBillingsAsync(parameters, cancellationToken);
        }

        public async Task<byte[]> GenerateExcelForPrintingAsync(int id, CancellationToken cancellationToken)
        {
            // ... (rest of GenerateExcelForPrintingAsync implementation)
            var billing = await unitOfWork.Billing.GetAsync(b => b.MsapBillingId == id, cancellationToken);
            if (billing == null)
            {
                throw new InvalidOperationException("Billing not found");
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
                    worksheet.Cells[row, 2].Value = $"NAME OF TUGBOAT: BUNKER ADJUSTMENT FACTOR";
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
                if (billing.IsVatInclusive)
                {
                    vatableSales = subTotal / 1.12m;
                    vatAmount = subTotal - vatableSales;
                }
                else
                {
                    vatableSales = subTotal / 1.12m; // Amount already multiplied by 1.12 in Create
                    vatAmount = subTotal - vatableSales;
                }
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

            return await package.GetAsByteArrayAsync(cancellationToken);
        }

        public async Task<List<object>> SearchPrincipalsAsync(string? term, int customerId, CancellationToken cancellationToken)
        {
            var result = await unitOfWork.Principal.SearchPrincipalsAsync(term ?? string.Empty, customerId, 10, cancellationToken);

            return result.Select(p => (object)new
            {
                value = p.PrincipalId,
                name = p.PrincipalName,
                address = p.Address1,
                tinNo = p.TIN,
                businessStyle = p.BusinessType,
                terms = p.Terms
            }).ToList();
        }

        public async Task<List<object>> SearchJobOrdersAsync(string? term, int customerId, CancellationToken cancellationToken)
        {
            var result = await unitOfWork.JobOrder.SearchBillableJobOrdersAsync(term ?? string.Empty, customerId, 10, cancellationToken);

            return result.Select(j => (object)new
            {
                value = j.JobOrderId,
                name = j.JobOrderNumber,
                description = j.Remarks ?? ""
            }).ToList();
        }

        public async Task<ServiceResult<JobOrderBillingDto>> GetDispatchTicketsByJobOrderAsync(int jobOrderId, CancellationToken cancellationToken)
        {
            var jobOrder = await unitOfWork.JobOrder.GetJobOrderWithDetailsAsync(jobOrderId, cancellationToken);
            if (jobOrder == null)
            {
                return ServiceResult<JobOrderBillingDto>.Failure("Job Order not found");
            }

            var tickets = jobOrder.DispatchTickets
                .Where(t => t is { Status: SD.DispatchTicketStatus.ForBilling, BillingId: null })
                .Select(t => new JobOrderTicketDto
                {
                    DispatchTicketId = t.DispatchTicketId,
                    DispatchNo = t.DispatchNumber,
                    Tugboat = t.Tugboat?.TugboatName ?? "N/A",
                    Service = t.Service?.ServiceName ?? "N/A",
                    Duration = t.TotalHours,
                    DispatchAmount = t.DispatchBillingAmount,
                    BAFAmount = t.BAFBillingAmount,
                    TotalAmount = t.TotalBilling
                }).ToList();

            return ServiceResult<JobOrderBillingDto>.Success(new JobOrderBillingDto
            {
                Header = new JobOrderHeaderDto
                {
                    VesselId = jobOrder.VesselId,
                    PortId = jobOrder.PortId,
                    TerminalId = jobOrder.TerminalId,
                    VoyageNumber = jobOrder.VoyageNumber,
                    COSNumber = jobOrder.COSNumber
                },
                Tickets = tickets
            });
        }

        public async Task<ServiceResult<JobOrderBillingDto>> GetDispatchTicketsByCustomerAsync(int customerId, CancellationToken cancellationToken)
        {
            var tickets = await unitOfWork.DispatchTicket.GetAllAsync(t =>
                t.CustomerId == customerId &&
                t.Status == SD.DispatchTicketStatus.ForBilling &&
                t.BillingId == null &&
                t.JobOrderId == null,
                cancellationToken);

            var ticketDtos = tickets.Select(t => new JobOrderTicketDto
            {
                DispatchTicketId = t.DispatchTicketId,
                DispatchNo = t.DispatchNumber,
                Tugboat = t.Tugboat?.TugboatName ?? "N/A",
                Service = t.Service?.ServiceName ?? "N/A",
                Duration = t.TotalHours,
                DispatchAmount = t.DispatchBillingAmount,
                BAFAmount = t.BAFBillingAmount,
                TotalAmount = t.TotalBilling
            }).ToList();

            return ServiceResult<JobOrderBillingDto>.Success(new JobOrderBillingDto
            {
                Header = new JobOrderHeaderDto(),
                Tickets = ticketDtos
            });
        }

        public async Task<List<SelectListItem>?> GetPrincipalsSelectListAsync(int customerId, CancellationToken cancellationToken)
        {
            var principals = await unitOfWork.Principal.GetAllAsync(t => t.CustomerId == customerId, cancellationToken);
            return principals.Select(t => new SelectListItem
            {
                Value = t.PrincipalId.ToString(),
                Text = t.PrincipalName
            }).ToList();
        }

        public async Task<List<SelectListItem>?> GetEditTicketsSelectListAsync(int? customerId, int billingId, CancellationToken cancellationToken)
        {
            var list = await unitOfWork.Billing.GetMsapUnbilledTicketsByCustomer(customerId, cancellationToken);
            if (billingId != 0)
            {
                var billedTickets = await unitOfWork.DispatchTicket.GetAllAsync(dt => dt.BillingId == billingId, cancellationToken);
                if (billedTickets.Any() && billedTickets.First().CustomerId == customerId)
                {
                    list?.AddRange(await unitOfWork.Billing.GetMsapBilledTicketsById(billingId, cancellationToken));
                }
            }
            return list;
        }

        public async Task<Billing> PopulateTicketListsAsync(Billing model, CancellationToken cancellationToken)
        {
            model.ToBillDispatchTickets = await unitOfWork.Billing
                .GetToBillDispatchTicketListAsync(model.MsapBillingId, cancellationToken);

            model.PaidDispatchTickets = await unitOfWork.Billing
                .GetPaidDispatchTicketsAsync(model.MsapBillingId, cancellationToken);

            model.UniqueTugboats = await unitOfWork.Billing
                .GetUniqueTugboatsListAsync(model.MsapBillingId, cancellationToken);

            unitOfWork.Billing.ProcessAddress(model, cancellationToken);
            return model;
        }

        public async Task<IEnumerable<DispatchTicket>> GetDispatchTicketsByIdsAsync(List<int> dispatchTicketIds, CancellationToken cancellationToken)
        {
            return await unitOfWork.DispatchTicket
                .GetAllAsync(t => dispatchTicketIds.Contains(t.DispatchTicketId), cancellationToken);
        }

        public async Task<Billing> PopulateBillingSelectListsAsync(Billing model, CancellationToken cancellationToken)
        {
            model.Vessels = await unitOfWork.Vessel.GetMsapVesselsSelectList(cancellationToken);
            model.Ports = await unitOfWork.Port.GetMsapPortsSelectList(cancellationToken);
            model.Customers = await unitOfWork.Billing.GetMsapCustomersWithBillablesSelectList(model.CustomerId, "", cancellationToken);

            if (model.PortId != 0)
            {
                model.Terminals = await unitOfWork.Terminal.GetMsapTerminalsSelectList(model.PortId, cancellationToken);
            }

            return model;
        }

        public async Task<ServiceResult<object>> GetCustomerDetailsAsync(int customerId, CancellationToken cancellationToken)
        {
            var customer = await unitOfWork.Customer.GetAsync(c => c.CustomerId == customerId, cancellationToken);
            if (customer == null)
            {
                return ServiceResult<object>.Failure("Customer not found.", ServiceResultStatus.NotFound);
            }

            return ServiceResult<object>.Success(new
            {
                address = customer.CustomerAddress,
                tin = customer.CustomerTin,
                isUndoc = customer.Type
            });
        }
    }
}


