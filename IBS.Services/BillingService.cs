using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using IBS.Models.Books;
using IBS.Models.Enums;
using IBS.Models.MSAP;
using IBS.Utility.Constants;
using IBS.Utility.Helpers;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;
using IBS.DTOs;
using static IBS.Utility.Constants.TaxConstants;

namespace IBS.Services
{
    public class BillingService(
        IUnitOfWork unitOfWork,
        JobOrderService jobOrderService,
        ILogger<BillingService> logger)
    {
        public async Task<Billing?> GetBillingByIdAsync(int id, CancellationToken cancellationToken)
        {
            return await unitOfWork.Billing.GetAsync(b => b.MsapBillingId == id, cancellationToken);
        }

        public async Task<ServiceResult<int>> CreateBillingAsync(Billing model, string username, string company, CancellationToken cancellationToken)
        {
            try
            {
                var guard = await GuardClosedPeriodAsync(model.Date, cancellationToken);
                if (guard != null) return ServiceResult<int>.Failure(guard!.Message!);

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
                model.Year = model.Date.Year;
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
                    model.MsapBillingNumber = await unitOfWork.Billing.GenerateBillingNumber(model.Year, cancellationToken);
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
                }

                    model.Amount = model.Balance = model.IsVatable && !model.IsVatInclusive ? total * VatMultiplier : total;
                model.DispatchAmount = dispatch;
                model.BAFAmount = baf;
                model.IsPaid = false;

                await unitOfWork.Billing.AddAsync(model, cancellationToken);
                await unitOfWork.AuditTrail.AddAsync(new AuditTrail(username, $"Created Billing #{model.MsapBillingNumber}", "Billing", model.MsapBillingId, model.MsapBillingNumber), cancellationToken);
                await unitOfWork.SaveAsync(cancellationToken);

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
                var billing = await unitOfWork.Billing.GetAsync(b => b.MsapBillingId == id, cancellationToken);
                if (billing == null)
                {
                    return ServiceResult.Failure("Billing not found.", ServiceResultStatus.NotFound);
                }

                var guard = await GuardClosedPeriodAsync(billing.Date, cancellationToken);
                if (guard != null) return guard;

                await unitOfWork.ExecuteInTransactionAsync(async () =>
                {
                    var model = billing;

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

                var guard = await GuardClosedPeriodAsync(currentModel.Date, cancellationToken);
                if (guard != null) return guard;

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
                    }
                }

                // Update properties
                currentModel.IsVatable = model.IsVatable;
                currentModel.CustomerId = model.CustomerId;
                currentModel.PrincipalId = model.PrincipalId;
                currentModel.VoyageNumber = model.VoyageNumber;
                currentModel.COSNumber = model.COSNumber;
                currentModel.Date = model.Date;
                currentModel.Year = model.Date.Year;
                currentModel.PortId = model.PortId;
                currentModel.TerminalId = model.TerminalId;
                currentModel.VesselId = model.VesselId;
                currentModel.BilledTo = model.BilledTo;
                currentModel.JobOrderId = model.JobOrderId;
                currentModel.IsVatInclusive = model.IsVatInclusive;
                currentModel.PrintWht = model.PrintWht;
                currentModel.ApOtherTug = model.ApOtherTug;

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
                    }

                    currentModel.Amount = currentModel.Balance = currentModel.IsVatable && !currentModel.IsVatInclusive ? total * VatMultiplier : total;
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

                var guard = await GuardClosedPeriodAsync(model.Date, cancellationToken);
                if (guard != null) return guard;

                var linkedTickets = await unitOfWork.DispatchTicket
                    .GetAllAsync(dt => dt.BillingId == id, cancellationToken);
                foreach (var dt in linkedTickets)
                {
                    dt.BillingId = null;
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
        public async Task<ServiceResult> ReverseBillingAsync(int id, string username, string? remarks, CancellationToken cancellationToken)
        {
            try
            {
                var billing = await unitOfWork.Billing.GetAsync(b => b.MsapBillingId == id, cancellationToken);
                if (billing == null)
                {
                    return ServiceResult.Failure("Billing not found.", ServiceResultStatus.NotFound);
                }

                var guard = await GuardClosedPeriodAsync(billing.Date, cancellationToken);
                if (guard != null) return guard;

                string? billingNumber = billing.MsapBillingNumber;

                await unitOfWork.ExecuteInTransactionAsync(async () =>
                {
                    billing = await unitOfWork.Billing.GetAsync(b => b.MsapBillingId == id, cancellationToken);
                    billingNumber = billing!.MsapBillingNumber;

                    if (billing.Status != SD.BillingStatus.ForCollection)
                        throw new InvalidOperationException($"Only billings with '{SD.BillingStatus.ForCollection}' status can be reversed.");

                    if (billing.CollectionId.HasValue)
                        throw new InvalidOperationException($"Billing #{billingNumber} cannot be reversed because it is linked to a Collection (CR#{billing.CollectionNumber}).");

                    var linkedTickets = await unitOfWork.DispatchTicket.GetAllAsync(dt => dt.BillingId == billing.MsapBillingId, cancellationToken);
                    foreach (var dt in linkedTickets)
                    {
                        dt.Status = SD.DispatchTicketStatus.ForBilling;
                        dt.BillingId = null;
                    }

                    billing.Status = SD.BillingStatus.ForPosting;
                    billing.UnpostedBy = username;
                    billing.UnpostedDate = DateTimeHelper.GetCurrentPhilippineTime();
                    billing.UnpostRemarks = remarks;

                    // ponytail: GL contra entries skipped — GL module is still TODO
                    // When GL posting is fixed, create reverse entries here:
                    // Debit Credit (original) → Credit Debit (contra) with reference "REV-#{number}"

                    await unitOfWork.AuditTrail.AddAsync(
                        new AuditTrail(username, $"Reversed (unposted) Billing #{billingNumber}", "Billing", billing.MsapBillingId, billingNumber),
                        cancellationToken);

                    if (billing.JobOrderId.HasValue)
                    {
                        var jobOrder = await unitOfWork.JobOrder.GetAsync(jo => jo.JobOrderId == billing.JobOrderId, cancellationToken);
                        if (jobOrder != null && jobOrder.Status == SD.JobOrderStatus.Closed)
                        {
                            jobOrder.Status = SD.JobOrderStatus.Open;
                        }
                    }

                    await unitOfWork.SaveAsync(cancellationToken);
                }, cancellationToken);

                return ServiceResult.Success($"Billing #{billingNumber} reversed successfully and is back to '{SD.BillingStatus.ForPosting}'.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to reverse billing {BillingId}", id);
                return ServiceResult.Failure($"Failed to reverse billing: {ExceptionHelper.GetErrorMessage(ex)}");
            }
        }

        public async Task<(IEnumerable<Billing> Data, int RecordsFiltered, int TotalRecords)> GetPagedBillingsAsync(DataTablesParameters parameters, CancellationToken cancellationToken)
        {
            return await unitOfWork.Billing.GetPagedBillingsAsync(parameters, cancellationToken);
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

        public async Task<List<object>> GetBillableJobOrdersAsync(int customerId, CancellationToken cancellationToken)
        {
            var result = await unitOfWork.JobOrder.SearchBillableJobOrdersAsync("", customerId, 999, cancellationToken);

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

            var principals = await unitOfWork.Principal.GetAllAsync(p => p.CustomerId == customerId, cancellationToken);
            var hasPrincipal = principals.Any();

            return ServiceResult<object>.Success(new
            {
                value = customer.CustomerId,
                name = customer.CustomerName,
                address = customer.CustomerAddress,
                tinNo = customer.CustomerTin,
                terms = customer.CustomerTerms,
                vatType = customer.VatType,
                isUndoc = customer.Type,
                businessStyle = customer.BusinessStyle ?? "-",
                withholdingTax = customer.WithHoldingTax,
                withholdingVat = customer.WithHoldingVat,
                hasPrincipal
            });
        }

        public async Task<List<object>> GetPrincipalsByCustomerAsync(int customerId, CancellationToken cancellationToken)
        {
            var principals = await unitOfWork.Principal.GetAllAsync(p => p.CustomerId == customerId, cancellationToken);
            return principals.Select(p => (object)new
            {
                value = p.PrincipalId,
                name = p.PrincipalName,
                address = p.Address1,
                tinNo = p.TIN,
                businessStyle = p.BusinessType,
                terms = p.Terms
            }).ToList();
        }

        private async Task<ServiceResult?> GuardClosedPeriodAsync(DateOnly date, CancellationToken ct)
        {
            if (await unitOfWork.PostedPeriod.IsMonthClosedAsync(date.Year, date.Month, ct))
                return ServiceResult.Failure($"Cannot modify: {date:MMMM yyyy} is closed.");
            return null;
        }
    }
}


