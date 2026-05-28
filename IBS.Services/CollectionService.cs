using System.Linq.Dynamic.Core;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using IBS.Models.MSAP;
using IBS.Models.MSAP.ViewModels;
using IBS.Utility.Constants;
using IBS.Utility.Helpers;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using IBS.DataAccess.Data;

namespace IBS.Services
{
    public class CollectionService : ICollectionService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<CollectionService> _logger;

        public CollectionService(IUnitOfWork unitOfWork, ILogger<CollectionService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Collection?> GetCollectionByIdAsync(int id, CancellationToken cancellationToken)
        {
            var collection = await _unitOfWork.Collection.GetAsync(c => c.MsapCollectionId == id, cancellationToken);
            if (collection != null)
            {
                collection.PaidBills = (await _unitOfWork.Billing.GetAllAsync(b => b.CollectionId == collection.MsapCollectionId, cancellationToken)).ToList();
            }
            return collection;
        }

        public async Task<ServiceResult<int>> CreateCollectionAsync(CreateCollectionViewModel viewModel, string username, CancellationToken cancellationToken)
        {
            try
            {
                int collectionId = 0;
                await _unitOfWork.ExecuteInTransactionAsync(async () =>
                {
                    var model = await MapToEntityAsync(viewModel, cancellationToken);
                    model.CreatedBy = username;
                    model.CreatedDate = DateTimeHelper.GetCurrentPhilippineTime();
                    model.Status = SD.CollectionStatus.Create;

                    if (model.IsUndocumented)
                    {
                        model.MsapCollectionNumber = await _unitOfWork.Collection.GenerateCollectionNumber(cancellationToken);
                    }
                    else
                    {
                        model.MsapCollectionNumber = viewModel.MsapCollectionNumber ?? string.Empty;
                    }

                    await _unitOfWork.Collection.AddAsync(model, cancellationToken);
                    await _unitOfWork.SaveAsync(cancellationToken);
                    collectionId = model.MsapCollectionId;

                    // Allocate payment
                    if (viewModel.BillingPayments != null)
                    {
                        foreach (var payment in viewModel.BillingPayments)
                        {
                            var billing = await _unitOfWork.Billing.GetAsync(b => b.MsapBillingId == payment.BillingId, cancellationToken);
                            if (billing != null)
                            {
                                billing.Status = SD.BillingStatus.Collected;
                                billing.CollectionId = model.MsapCollectionId;
                                await _unitOfWork.Collection.UpdateBillingPayment(payment.BillingId, payment.AmountToPay, cancellationToken);
                            }
                        }
                    }

                    // Post to books
                    await _unitOfWork.Collection.PostAsync(model, new List<Offsettings>(), cancellationToken);

                    // Final save for all changes
                    await _unitOfWork.SaveAsync(cancellationToken);

                    // Audit trail
                    var billIds = viewModel.BillingPayments?.Select(p => p.BillingId) ?? new List<int>();
                    var audit = new AuditTrail(username, $"Create collection #{model.MsapCollectionNumber} for billings #{string.Join(", #", billIds)}", "Collection");
                    await _unitOfWork.AuditTrail.AddAsync(audit, cancellationToken);
                    await _unitOfWork.SaveAsync(cancellationToken);
                }, cancellationToken);

                return ServiceResult<int>.Success(collectionId, "Collection created successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create collection.");
                return ServiceResult<int>.Failure(ex.Message);
            }
        }

        public async Task<ServiceResult> UpdateCollectionAsync(CreateCollectionViewModel viewModel, string username, CancellationToken cancellationToken)
        {
            try
            {
                await _unitOfWork.ExecuteInTransactionAsync(async () =>
                {
                    var currentModel = await _unitOfWork.Collection.GetAsync(c => c.MsapCollectionId == viewModel.MsapCollectionId, cancellationToken);
                    if (currentModel == null)
                    {
                        throw new InvalidOperationException("Collection not found.");
                    }

                    // Revert old allocations
                    var oldBillings = await _unitOfWork.Billing.GetAllAsync(b => b.CollectionId == currentModel.MsapCollectionId, cancellationToken);
                    foreach (var billing in oldBillings)
                    {
                        billing.Status = SD.BillingStatus.ForCollection;
                        billing.CollectionId = 0;
                        await _unitOfWork.Collection.RemoveBillingPayment(billing.MsapBillingId, billing.Amount, 0, cancellationToken);
                    }

                    // Apply new allocations
                    if (viewModel.BillingPayments != null)
                    {
                        foreach (var payment in viewModel.BillingPayments)
                        {
                            var billing = await _unitOfWork.Billing.GetAsync(b => b.MsapBillingId == payment.BillingId, cancellationToken);
                            if (billing != null)
                            {
                                billing.Status = SD.BillingStatus.Collected;
                                billing.CollectionId = currentModel.MsapCollectionId;
                                await _unitOfWork.Collection.UpdateBillingPayment(payment.BillingId, payment.AmountToPay, cancellationToken);
                            }
                        }
                    }

                    // Track changes for audit
                    var changes = new List<string>();
                    if (currentModel.CheckNumber != viewModel.CheckNumber)
                    {
                        changes.Add($"CheckNumber: {currentModel.CheckNumber} -> {viewModel.CheckNumber}");
                    }

                    if (currentModel.Amount != viewModel.Amount)
                    {
                        changes.Add($"Amount: {currentModel.Amount} -> {viewModel.Amount}");
                    }

                    var audit = new AuditTrail(username, $"Edit collection #{currentModel.MsapCollectionNumber} {string.Join(", ", changes)}", "Collection");
                    await _unitOfWork.AuditTrail.AddAsync(audit, cancellationToken);

                    // Update entity
                    currentModel.Date = viewModel.Date;
                    currentModel.CustomerId = viewModel.CustomerId;
                    currentModel.CheckNumber = viewModel.CheckNumber;
                    currentModel.CheckDate = viewModel.CheckDate;
                    currentModel.DepositDate = viewModel.DepositDate;
                    currentModel.Amount = viewModel.Amount;
                    currentModel.EWT = viewModel.EWT;
                    currentModel.WVAT = viewModel.WVAT;
                    currentModel.Total = viewModel.Amount + viewModel.EWT + viewModel.WVAT;

                    await _unitOfWork.SaveAsync(cancellationToken);
                }, cancellationToken);

                return ServiceResult.Success("Collection modified successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to edit collection.");
                return ServiceResult.Failure(ex.Message);
            }
        }


        public async Task<(IEnumerable<Collection> Data, int RecordsFiltered, int TotalRecords)> GetPagedCollectionsAsync(DataTablesParameters parameters, CancellationToken cancellationToken)
        {
            return await _unitOfWork.Collection.GetPagedCollectionsAsync(parameters, cancellationToken);
        }

        public async Task<CreateCollectionViewModel> PopulateCreateViewModelAsync(CancellationToken cancellationToken)
        {
            return new CreateCollectionViewModel
            {
                Customers = await _unitOfWork.Collection.GetMsapCustomersWithCollectiblesSelectList(0, string.Empty, cancellationToken),
                BankAccounts = await _unitOfWork.GetBankAccountListById(cancellationToken)
            };
        }

        public async Task<CreateCollectionViewModel?> PopulateEditViewModelAsync(int id, CancellationToken cancellationToken)
        {
            var model = await _unitOfWork.Collection.GetAsync(c => c.MsapCollectionId == id, cancellationToken);
            if (model == null)
            {
                return null;
            }

            var viewModel = MapToViewModel(model);
            var billings = await _unitOfWork.Billing.GetBillingsByCollectionIdAsync(id, cancellationToken);
            viewModel.ToCollectBillings = billings
                .Select(b => b.MsapBillingId.ToString())
                .ToList();

            viewModel.Customers = await _unitOfWork.Collection.GetMsapCustomersWithCollectiblesSelectList(id, model.Customer.Type, cancellationToken);
            viewModel.Billings = await GetEditBillingsAsync(model.CustomerId, model.MsapCollectionId, cancellationToken);
            viewModel.BankAccounts = await _unitOfWork.GetBankAccountListById(cancellationToken);

            return viewModel;
        }

        public async Task<ServiceResult<object>> GetUncollectedBillingsForTableAsync(int customerId, int? collectionId, CancellationToken cancellationToken)
        {
            try
            {
                var billings = await _unitOfWork.Collection.GetMsapUncollectedBillingsByCustomerList(customerId, cancellationToken);
                if (collectionId.HasValue && collectionId.Value != 0)
                {
                    var alreadyCollected = await _unitOfWork.Billing.GetAllAsync(b => b.CollectionId == collectionId.Value, cancellationToken);
                    billings.AddRange(alreadyCollected);
                }

                var result = billings
                    .DistinctBy(b => b.MsapBillingId)
                    .Select(b => new
                    {
                        b.MsapBillingId,
                        b.MsapBillingNumber,
                        b.Date,
                        b.Amount,
                        b.Balance,
                        Ewt = b.BilledTo == SD.BilledTo_Local ? (b.Amount / 1.12m) * 0.02m : 0m,
                        Net = b.BilledTo == SD.BilledTo_Local ? b.Amount - ((b.Amount / 1.12m) * 0.02m) : b.Amount,
                        IsSelected = collectionId.HasValue && b.CollectionId == collectionId.Value
                    });

                return ServiceResult<object>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get billings for table.");
                return ServiceResult<object>.Failure(ex.Message);
            }
        }

        public async Task<ServiceResult<IEnumerable<Billing>>> GetSelectedBillingsAsync(List<string> billingIds, CancellationToken cancellationToken)
        {
            var ids = billingIds.Select(int.Parse).ToList();
            var billings = await _unitOfWork.Billing.GetAllAsync(b => ids.Contains(b.MsapBillingId), cancellationToken);
            return ServiceResult<IEnumerable<Billing>>.Success(billings);
        }

        public async Task<bool> IsCustomerVatableAsync(int customerId, CancellationToken cancellationToken)
        {
            var customer = await _unitOfWork.Customer.GetAsync(c => c.CustomerId == customerId, cancellationToken);
            return customer?.VatType == SD.VatType_Vatable;
        }

        public async Task<ServiceResult<object>> GetBankAccountDetailsAsync(int bankId, CancellationToken cancellationToken)
        {
            var bank = await _unitOfWork.BankAccount.GetAsync(b => b.BankAccountId == bankId, cancellationToken);
            if (bank == null)
            {
                return ServiceResult<object>.Failure("Bank not found.", ServiceResultStatus.NotFound);
            }

            return ServiceResult<object>.Success(new { bank = bank.Bank, accountNo = bank.AccountNo, accountName = bank.AccountName });
        }

        public async Task<List<SelectListItem>?> GetUncollectedBillingsSelectListAsync(int? customerId, CancellationToken cancellationToken)
        {
            return await _unitOfWork.Collection.GetMsapUncollectedBillingsByCustomer(customerId, cancellationToken);
        }

        public async Task<List<object>> SearchCustomersAsync(string? term, CancellationToken cancellationToken)
        {
            var customers = await _unitOfWork.Customer.SearchCustomersAsync(term ?? string.Empty, 10, cancellationToken);

            return customers.Select(c => (object)new
            {
                value = c.CustomerId,
                name = c.CustomerName,
                vatType = c.VatType,
                isUndoc = c.Type,
                address = c.CustomerAddress,
                tinNo = c.CustomerTin,
                terms = c.CustomerTerms,
                businessStyle = c.BusinessStyle ?? "-"
            }).ToList();
        }

        private async Task<Collection> MapToEntityAsync(CreateCollectionViewModel viewModel, CancellationToken cancellationToken)
        {
            var model = new Collection
            {
                MsapCollectionId = viewModel.MsapCollectionId ?? 0,
                IsUndocumented = viewModel.IsUndocumented,
                Date = viewModel.Date,
                CustomerId = viewModel.CustomerId,
                Amount = viewModel.Amount,
                EWT = viewModel.EWT,
                WVAT = viewModel.WVAT,
                Total = viewModel.Amount + viewModel.EWT + viewModel.WVAT,
                CashAmount = viewModel.CashAmount,
                CheckAmount = viewModel.CheckAmount,
                CheckNumber = viewModel.CheckNumber,
                CheckDate = viewModel.CheckDate,
                CheckBank = viewModel.CheckBank,
                CheckBranch = viewModel.CheckBranch,
                BankId = viewModel.BankId,
                ReferenceNo = viewModel.ReferenceNo,
                Remarks = viewModel.Remarks,
                DepositDate = viewModel.DepositDate,
                Customer = (await _unitOfWork.Customer.GetAsync(c => c.CustomerId == viewModel.CustomerId, cancellationToken))!
            };

            if (viewModel.BankId.HasValue)
            {
                var bank = await _unitOfWork.BankAccount.GetAsync(b => b.BankAccountId == viewModel.BankId.Value, cancellationToken);
                if (bank != null)
                {
                    model.BankAccountNumber = bank.AccountNo;
                    model.BankAccountName = bank.AccountName;
                }
            }

            return model;
        }

        private CreateCollectionViewModel MapToViewModel(Collection model)
        {
            return new CreateCollectionViewModel
            {
                MsapCollectionId = model.MsapCollectionId,
                MsapCollectionNumber = model.MsapCollectionNumber,
                IsUndocumented = model.IsUndocumented,
                Date = model.Date,
                CustomerId = model.CustomerId,
                Amount = model.Amount,
                EWT = model.EWT,
                WVAT = model.WVAT,
                CashAmount = model.CashAmount,
                CheckAmount = model.CheckAmount,
                CheckNumber = model.CheckNumber,
                CheckDate = model.CheckDate,
                CheckBank = model.CheckBank,
                CheckBranch = model.CheckBranch,
                BankId = model.BankId,
                ReferenceNo = model.ReferenceNo,
                Remarks = model.Remarks,
                DepositDate = model.DepositDate,
            };
        }

        private async Task<List<SelectListItem>?> GetEditBillingsAsync(int? customerId, int? collectionId, CancellationToken cancellationToken)
        {
            var list = await _unitOfWork.Collection.GetMsapUncollectedBillingsByCustomer(customerId, cancellationToken);
            if (collectionId.HasValue && collectionId.Value != 0)
            {
                var model = await _unitOfWork.Collection.GetAsync(c => c.MsapCollectionId == collectionId.Value, cancellationToken);
                if (model?.CustomerId == customerId)
                {
                    list?.AddRange(await _unitOfWork.Collection.GetMsapCollectedBillsById(collectionId.Value, cancellationToken));
                }
            }
            return list;
        }
    }
}
