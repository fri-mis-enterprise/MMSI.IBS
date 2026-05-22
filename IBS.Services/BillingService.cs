using System.Linq.Dynamic.Core;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using IBS.Models.Books;
using IBS.Models.MSAP;
using IBS.Utility.Constants;
using IBS.Utility.Helpers;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using IBS.DataAccess.Data;
using IBS.DTOs;
using IBS.Models.Enums;

namespace IBS.Services
{
    public class BillingService(
        IUnitOfWork unitOfWork,
        ApplicationDbContext dbContext,
        IJobOrderService jobOrderService,
        ILogger<BillingService> logger) : IBillingService
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

                    dt.Status = SD.DispatchTicketStatus.Billed;
                    dt.Billing = model;
                    dt.BillingNumber = model.MsapBillingNumber;
                }

                model.Amount = model.Balance = total;
                model.DispatchAmount = dispatch;
                model.BAFAmount = baf;
                model.IsPaid = false;

                await unitOfWork.Billing.AddAsync(model, cancellationToken);
                await unitOfWork.AuditTrail.AddAsync(new AuditTrail(username, $"Created Billing #{model.MsapBillingNumber}", "Billing"), cancellationToken);
                await unitOfWork.SaveAsync(cancellationToken);

                return ServiceResult<int>.Success(model.MsapBillingId, "Billing created successfully. Status: For Posting");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to create billing.");
                return ServiceResult<int>.Failure(ex.Message);
            }
        }

        public async Task<ServiceResult> PostBillingAsync(int id, string username, CancellationToken cancellationToken)
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var model = await unitOfWork.Billing.GetAsync(b => b.MsapBillingId == id, cancellationToken);
                if (model == null)
                {
                    return ServiceResult.Failure("Billing not found.", ServiceResultStatus.NotFound);
                }

                if (model.Status != SD.BillingStatus.ForPosting)
                {
                    return ServiceResult.Failure($"Billing #{model.MsapBillingNumber} is already {model.Status}.");
                }

                var customer = await unitOfWork.Customer.GetAsync(c => c.CustomerId == model.CustomerId, cancellationToken);
                if (customer == null) return ServiceResult.Failure("Customer not found.");
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

                var arTrade = accountTitlesDto.Find(c => c.AccountNumber == "101020100"); // AR Trade
                var revenue = accountTitlesDto.Find(c => c.AccountNumber == "401020100"); // Maritime Service Revenue
                var outputVat = accountTitlesDto.Find(c => c.AccountNumber == "201010101"); // Output VAT

                if (arTrade == null) return ServiceResult.Failure("Accounting setup incomplete: Account '101020100' (AR Trade) not found in Chart of Accounts.");
                if (revenue == null) return ServiceResult.Failure("Accounting setup incomplete: Account '401020100' (Service Revenue) not found in Chart of Accounts.");
                if (model.IsVatable && outputVat == null) return ServiceResult.Failure("Accounting setup incomplete: Account '201010101' (Output VAT) not found in Chart of Accounts.");

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
                    return ServiceResult.Failure("Accounting error: Journal entries are not balanced.");
                }

                model.Status = SD.BillingStatus.ForCollection;

                await dbContext.SalesBooks.AddAsync(salesBook, cancellationToken);
                await dbContext.GeneralLedgerBooks.AddRangeAsync(ledgers, cancellationToken);
                await unitOfWork.AuditTrail.AddAsync(new AuditTrail(username, $"Posted Billing #{model.MsapBillingNumber}", "Billing"), cancellationToken);
                await unitOfWork.SaveAsync(cancellationToken);

                // --- Automatic Job Order Closure ---
                if (model.JobOrderId.HasValue)
                {
                    var closeResult = await jobOrderService.CloseJobOrderAsync(model.JobOrderId.Value, username, false, cancellationToken);
                    if (!closeResult.IsSuccess)
                    {
                        logger.LogWarning("Billing #{BillingNumber} posted, but associated Job Order #{JobOrderId} could not be closed: {ErrorMessage}",
                            model.MsapBillingNumber, model.JobOrderId, closeResult.Message);
                    }
                }

                await transaction.CommitAsync(cancellationToken);

                return ServiceResult.Success($"Billing #{model.MsapBillingNumber} posted successfully.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                logger.LogError(ex, "Failed to post billing {BillingId}", id);
                return ServiceResult.Failure(ex.Message);
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

                var customer = await unitOfWork.Customer.GetAsync(c => c.CustomerId == model.CustomerId, cancellationToken);
                if (customer == null)
                {
                    return ServiceResult.Failure("Customer not found.");
                }

                currentModel.IsVatable = customer.VatType == SD.VatType_Vatable;

                // Revert old tickets
                var oldTickets = await unitOfWork.DispatchTicket.GetAllAsync(dt => dt.BillingId == model.MsapBillingId, cancellationToken);
                foreach (var dt in oldTickets)
                {
                    dt.Status = SD.DispatchTicketStatus.ForBilling;
                    dt.BillingId = null;
                    dt.BillingNumber = null;
                }

                // Update properties
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

                decimal total = 0, dispatch = 0, baf = 0;
                if (model.ToBillDispatchTickets != null)
                {
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

                        dt.Status = SD.DispatchTicketStatus.Billed;
                        dt.Billing = currentModel;
                        dt.BillingNumber = currentModel.MsapBillingNumber;
                    }
                }

                currentModel.Amount = currentModel.Balance = total;
                currentModel.DispatchAmount = dispatch;
                currentModel.BAFAmount = baf;

                await unitOfWork.AuditTrail.AddAsync(new AuditTrail(username, $"Edit billing #{currentModel.MsapBillingNumber}", "Billing"), cancellationToken);
                await unitOfWork.SaveAsync(cancellationToken);

                return ServiceResult.Success("Entry edited successfully!");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to edit billing.");
                return ServiceResult.Failure(ex.Message);
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

                // Revert all linked dispatch tickets back to "For Billing" so they can be re-billed.
                // Without this, deleting a billing leaves tickets stuck in "Billed" status with a
                // null BILLNUM foreign key (orphaned), making them invisible to the billing queue.
                var linkedTickets = await unitOfWork.DispatchTicket
                    .GetAllAsync(dt => dt.BillingId == id, cancellationToken);
                foreach (var dt in linkedTickets)
                {
                    dt.Status = SD.DispatchTicketStatus.ForBilling;
                    dt.BillingId = null;
                    dt.BillingNumber = null;
                }

                if (model.Status != SD.BillingStatus.ForPosting && model.Status != SD.BillingStatus.Cancelled)
                {
                    var salesBook = await dbContext.SalesBooks.FirstOrDefaultAsync(s => s.DocumentId == id && s.SerialNo == model.MsapBillingNumber, cancellationToken);
                    if (salesBook != null)
                    {
                        dbContext.SalesBooks.Remove(salesBook);
                    }
                }

                await unitOfWork.Billing.RemoveAsync(model, cancellationToken);
                await unitOfWork.SaveAsync(cancellationToken);

                return ServiceResult.Success("Billing deleted successfully!");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to delete billing.");
                return ServiceResult.Failure(ex.Message);
            }
        }

        public async Task<(IEnumerable<Billing> Data, int RecordsFiltered, int TotalRecords)> GetPagedBillingsAsync(DataTablesParameters parameters, CancellationToken cancellationToken)
        {
            var query = dbContext.MsapBillings
                .Include(b => b.Customer)
                .Include(b => b.Terminal).ThenInclude(b => b.Port)
                .Include(b => b.Vessel)
                .Where(b => b.Status != SD.BillingStatus.Cancelled);

            if (!string.IsNullOrEmpty(parameters.Search.Value))
            {
                var s = parameters.Search.Value.ToLower();
                query = query.Where(dt =>
                    dt.Date.Day.ToString().Contains(s) ||
                    dt.Date.Month.ToString().Contains(s) ||
                    dt.Date.Year.ToString().Contains(s) ||
                    dt.MsapBillingNumber.ToLower().Contains(s) ||
                    dt.Amount.ToString().Contains(s) ||
                    dt.Customer.CustomerName.ToLower().Contains(s) ||
                    dt.Terminal.TerminalName.ToLower().Contains(s) ||
                    dt.Terminal.Port.PortName.ToLower().Contains(s) ||
                    dt.Vessel.VesselName.ToLower().Contains(s) ||
                    dt.Status.ToLower().Contains(s)
                );
            }

            foreach (var column in parameters.Columns)
            {
                if (!string.IsNullOrEmpty(column.Search.Value))
                {
                    var s = column.Search.Value.ToLower();
                    if (column.Data == "status")
                    {
                        if (s == "for posting")
                        {
                            query = query.Where(x => x.Status == SD.BillingStatus.ForPosting);
                        }

                        if (s == "for collection")
                        {
                            query = query.Where(x => x.Status == SD.BillingStatus.ForCollection);
                        }

                        if (s == "collected")
                        {
                            query = query.Where(x => x.Status == SD.BillingStatus.Collected);
                        }
                    }
                }
            }

            var totalRecords = await query.CountAsync(cancellationToken);

            if (parameters.Order?.Count > 0)
            {
                var col = parameters.Columns[parameters.Order[0].Column].Data;
                var dir = parameters.Order[0].Dir.ToLower() == "asc" ? "ascending" : "descending";
                query = query.OrderBy($"{col} {dir}");
            }

            var data = await query
                .Skip(parameters.Start)
                .Take(parameters.Length)
                .ToListAsync(cancellationToken);

            return (data, totalRecords, totalRecords);
        }

        public async Task<byte[]> GenerateExcelForPrintingAsync(int id, CancellationToken cancellationToken)
        {
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
                    row++;
                }
            }

            worksheet.Cells[1, 1, row, 7].Style.Font.Name = "Calibri";
            worksheet.Column(1).Width = 8;
            worksheet.Column(2).Width = 53;
            worksheet.Column(3).Width = 9;
            worksheet.Column(4).Width = 8.5;
            worksheet.Column(5).Width = 16;

            return await package.GetAsByteArrayAsync(cancellationToken);
        }

        public async Task<List<object>> SearchCustomersAsync(string? term, CancellationToken cancellationToken)
        {
            var query = dbContext.Customers.AsNoTracking();
            if (!string.IsNullOrWhiteSpace(term))
            {
                var s = term.ToLower();
                query = query.Where(c => c.CustomerName.ToLower().Contains(s) || c.CustomerCode.ToLower().Contains(s));
            }

            var customers = await query
                .OrderBy(c => c.CustomerName)
                .Take(10)
                .Select(c => new
                {
                    value = c.CustomerId,
                    name = c.CustomerName,
                    vatType = c.VatType,
                    isUndoc = c.Type,
                    address = c.CustomerAddress,
                    tinNo = c.CustomerTin,
                    terms = c.CustomerTerms,
                    businessStyle = c.BusinessStyle ?? "-"
                })
                .ToListAsync(cancellationToken);

            var ids = customers.Select(c => c.value).ToList();
            var principalsExist = await dbContext.MsapPrincipals
                .Where(p => ids.Contains(p.CustomerId))
                .Select(p => p.CustomerId)
                .Distinct()
                .ToListAsync(cancellationToken);

            return customers.Select(c => (object)new
            {
                c.value,
                c.name,
                hasPrincipal = principalsExist.Contains(c.value),
                c.vatType,
                c.isUndoc,
                c.address,
                c.tinNo,
                c.terms,
                c.businessStyle
            }).ToList();
        }

        public async Task<List<object>> SearchPrincipalsAsync(string? term, int customerId, CancellationToken cancellationToken)
        {
            var query = dbContext.MsapPrincipals.AsNoTracking().Where(p => p.CustomerId == customerId);
            if (!string.IsNullOrWhiteSpace(term))
            {
                var s = term.ToLower();
                query = query.Where(p => p.PrincipalName.ToLower().Contains(s) || p.PrincipalNumber.ToLower().Contains(s));
            }

            var result = await query
                .OrderBy(p => p.PrincipalName)
                .Take(10)
                .Select(p => new
                {
                    value = p.PrincipalId,
                    name = p.PrincipalName,
                    address = p.Address1,
                    tinNo = p.TIN,
                    businessStyle = p.BusinessType,
                    terms = p.Terms
                })
                .ToListAsync(cancellationToken);

            return result.Select(r => (object)r).ToList();
        }

        public async Task<List<object>> SearchJobOrdersAsync(string? term, int customerId, CancellationToken cancellationToken)
        {
            var query = dbContext.MsapJobOrders.AsNoTracking()
                .Where(j => j.CustomerId == customerId &&
                            j.DispatchTickets.Any(dt => dt.Status == SD.DispatchTicketStatus.ForBilling && dt.BillingId == null) &&
                            !j.DispatchTickets.Any(dt => dt.Status == SD.DispatchTicketStatus.Pending || dt.Status == SD.DispatchTicketStatus.ForTariff || dt.Status == SD.DispatchTicketStatus.ForApproval));

            if (!string.IsNullOrWhiteSpace(term))
            {
                var s = term.ToLower();
                query = query.Where(j => j.JobOrderNumber.ToLower().Contains(s));
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

            return result.Select(r => (object)r).ToList();
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


