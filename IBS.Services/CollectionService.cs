using System.Linq.Dynamic.Core;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using IBS.Models.MMSI;
using IBS.Models.MMSI.ViewModels;
using IBS.Utility.Constants;
using IBS.Utility.Helpers;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using IBS.DataAccess.Data;

namespace IBS.Services
{
    public class CollectionService(
        IUnitOfWork unitOfWork,
        ApplicationDbContext dbContext,
        ILogger<CollectionService> logger) : ICollectionService
    {
        public async Task<Collection?> GetCollectionByIdAsync(int id, CancellationToken cancellationToken)
        {
            var collection = await unitOfWork.Collection.GetAsync(c => c.MMSICollectionId == id, cancellationToken);
            if (collection != null)
            {
                collection.PaidBills = (await unitOfWork.Billing.GetAllAsync(b => b.CollectionId == collection.MMSICollectionId, cancellationToken)).ToList();
            }
            return collection;
        }

        public async Task<ServiceResult<int>> CreateCollectionAsync(CreateCollectionViewModel viewModel, string username, CancellationToken cancellationToken)
        {
            try
            {
                var model = await MapToEntityAsync(viewModel, cancellationToken);
                model.CreatedBy = username;
                model.CreatedDate = DateTimeHelper.GetCurrentPhilippineTime();
                model.Status = SD.CollectionStatus.Create; // Using SD constant

                if (model.IsUndocumented)
                {
                    model.MMSICollectionNumber = await unitOfWork.Collection.GenerateCollectionNumber(cancellationToken);
                }
                else
                {
                    model.MMSICollectionNumber = viewModel.MMSICollectionNumber ?? string.Empty;
                }

                await unitOfWork.Collection.AddAsync(model, cancellationToken);
                await unitOfWork.SaveAsync(cancellationToken);

                // Audit trail
                var audit = new AuditTrail(username, $"Create collection #{model.MMSICollectionNumber} for billings #{string.Join(", #", viewModel.ToCollectBillings!)}", "Collection");
                await unitOfWork.AuditTrail.AddAsync(audit, cancellationToken);

                // Allocate payment
                foreach (var billingIdStr in viewModel.ToCollectBillings!)
                {
                    var billingId = int.Parse(billingIdStr);
                    var billing = await unitOfWork.Billing.GetAsync(b => b.MMSIBillingId == billingId, cancellationToken);
                    if (billing != null)
                    {
                        billing.Status = SD.BillingStatus.Collected;
                        billing.CollectionId = model.MMSICollectionId;
                        await unitOfWork.Collection.UpdateBillingPayment(billingId, billing.Amount, cancellationToken);
                    }
                }

                // Post to books
                await unitOfWork.Collection.PostAsync(model, new List<Offsettings>(), cancellationToken);

                return ServiceResult<int>.Success(model.MMSICollectionId, "Collection created successfully.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to create collection.");
                return ServiceResult<int>.Failure(ex.Message);
            }
        }

        public async Task<ServiceResult> UpdateCollectionAsync(CreateCollectionViewModel viewModel, string username, CancellationToken cancellationToken)
        {
            try
            {
                var currentModel = await unitOfWork.Collection.GetAsync(c => c.MMSICollectionId == viewModel.MMSICollectionId, cancellationToken);
                if (currentModel == null)
                {
                    return ServiceResult.Failure("Collection not found.", ServiceResultStatus.NotFound);
                }

                // Revert old allocations
                var oldBillings = await unitOfWork.Billing.GetAllAsync(b => b.CollectionId == currentModel.MMSICollectionId, cancellationToken);
                foreach (var billing in oldBillings)
                {
                    billing.Status = SD.BillingStatus.ForCollection;
                    billing.CollectionId = 0;
                    await unitOfWork.Collection.RemoveBillingPayment(billing.MMSIBillingId, billing.Amount, 0, cancellationToken);
                }
                await unitOfWork.SaveAsync(cancellationToken);

                // Apply new allocations
                foreach (var billingIdStr in viewModel.ToCollectBillings!)
                {
                    var billingId = int.Parse(billingIdStr);
                    var billing = await unitOfWork.Billing.GetAsync(b => b.MMSIBillingId == billingId, cancellationToken);
                    if (billing != null)
                    {
                        billing.Status = SD.BillingStatus.Collected;
                        billing.CollectionId = currentModel.MMSICollectionId;
                        await unitOfWork.Collection.UpdateBillingPayment(billingId, billing.Amount, cancellationToken);
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
                // ... more change tracking if needed

                var audit = new AuditTrail(username, $"Edit collection #{currentModel.MMSICollectionNumber} {string.Join(", ", changes)}", "Collection");
                await unitOfWork.AuditTrail.AddAsync(audit, cancellationToken);

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

                await unitOfWork.SaveAsync(cancellationToken);
                return ServiceResult.Success("Collection modified successfully");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to edit collection.");
                return ServiceResult.Failure(ex.Message);
            }
        }

        public async Task<(IEnumerable<Collection> Data, int RecordsFiltered, int TotalRecords)> GetPagedCollectionsAsync(DataTablesParameters parameters, CancellationToken cancellationToken)
        {
            var query = dbContext.MMSICollections
                .Include(c => c.Customer)
                .Include(c => c.BankAccount)
                .AsQueryable();

            if (!string.IsNullOrEmpty(parameters.Search.Value))
            {
                var s = parameters.Search.Value.ToLower();
                query = query.Where(c =>
                    c.MMSICollectionNumber.ToLower().Contains(s) ||
                    c.Customer.CustomerName.ToLower().Contains(s) ||
                    (c.Status != null && c.Status.ToLower().Contains(s))
                );
            }

            var totalRecords = await dbContext.MMSICollections.CountAsync(cancellationToken);
            var recordsFiltered = await query.CountAsync(cancellationToken);

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

            return (data, recordsFiltered, totalRecords);
        }

        public async Task<CreateCollectionViewModel> PopulateCreateViewModelAsync(CancellationToken cancellationToken)
        {
            return new CreateCollectionViewModel
            {
                Customers = await unitOfWork.Collection.GetMMSICustomersWithCollectiblesSelectList(0, string.Empty, cancellationToken),
                BankAccounts = await unitOfWork.GetBankAccountListById(cancellationToken)
            };
        }

        public async Task<CreateCollectionViewModel?> PopulateEditViewModelAsync(int id, CancellationToken cancellationToken)
        {
            var model = await unitOfWork.Collection.GetAsync(c => c.MMSICollectionId == id, cancellationToken);
            if (model == null)
            {
                return null;
            }

            var viewModel = MapToViewModel(model);
            viewModel.ToCollectBillings = await dbContext.Billings
                .Where(b => b.CollectionId == model.MMSICollectionId)
                .Select(b => b.MMSIBillingId.ToString())
                .ToListAsync(cancellationToken);

            viewModel.Customers = await unitOfWork.Collection.GetMMSICustomersWithCollectiblesSelectList(id, model.Customer.Type, cancellationToken);
            viewModel.Billings = await GetEditBillingsAsync(model.CustomerId, model.MMSICollectionId, cancellationToken);
            viewModel.BankAccounts = await unitOfWork.GetBankAccountListById(cancellationToken);

            return viewModel;
        }

        public async Task<ServiceResult<object>> GetUncollectedBillingsForTableAsync(int customerId, int? collectionId, CancellationToken cancellationToken)
        {
            try
            {
                var billings = await unitOfWork.Collection.GetMMSIUncollectedBillingsByCustomerList(customerId, cancellationToken);
                if (collectionId.HasValue && collectionId.Value != 0)
                {
                    var alreadyCollected = await unitOfWork.Billing.GetAllAsync(b => b.CollectionId == collectionId.Value, cancellationToken);
                    billings.AddRange(alreadyCollected);
                }

                var result = billings
                    .DistinctBy(b => b.MMSIBillingId)
                    .Select(b => new
                    {
                        b.MMSIBillingId,
                        b.MMSIBillingNumber,
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
                logger.LogError(ex, "Failed to get billings for table.");
                return ServiceResult<object>.Failure(ex.Message);
            }
        }

        public async Task<ServiceResult<IEnumerable<Billing>>> GetSelectedBillingsAsync(List<string> billingIds, CancellationToken cancellationToken)
        {
            var ids = billingIds.Select(int.Parse).ToList();
            var billings = await unitOfWork.Billing.GetAllAsync(b => ids.Contains(b.MMSIBillingId), cancellationToken);
            return ServiceResult<IEnumerable<Billing>>.Success(billings);
        }

        public async Task<bool> IsCustomerVatableAsync(int customerId, CancellationToken cancellationToken)
        {
            var customer = await unitOfWork.Customer.GetAsync(c => c.CustomerId == customerId, cancellationToken);
            return customer?.VatType == SD.VatType_Vatable;
        }

        public async Task<ServiceResult<object>> GetBankAccountDetailsAsync(int bankId, CancellationToken cancellationToken)
        {
            var bank = await unitOfWork.BankAccount.GetAsync(b => b.BankAccountId == bankId, cancellationToken);
            if (bank == null)
            {
                return ServiceResult<object>.Failure("Bank not found.", ServiceResultStatus.NotFound);
            }

            return ServiceResult<object>.Success(new { bank = bank.Bank, accountNo = bank.AccountNo, accountName = bank.AccountName });
        }

        public async Task<List<SelectListItem>?> GetUncollectedBillingsSelectListAsync(int? customerId, CancellationToken cancellationToken)
        {
            return await unitOfWork.Collection.GetMMSIUncollectedBillingsByCustomer(customerId, cancellationToken);
        }

        private async Task<Collection> MapToEntityAsync(CreateCollectionViewModel viewModel, CancellationToken cancellationToken)
        {
            var model = new Collection
            {
                MMSICollectionId = viewModel.MMSICollectionId ?? 0,
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
                Customer = (await unitOfWork.Customer.GetAsync(c => c.CustomerId == viewModel.CustomerId, cancellationToken))!
            };

            if (viewModel.BankId.HasValue)
            {
                var bank = await unitOfWork.BankAccount.GetAsync(b => b.BankAccountId == viewModel.BankId.Value, cancellationToken);
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
                MMSICollectionId = model.MMSICollectionId,
                MMSICollectionNumber = model.MMSICollectionNumber,
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
            var list = await unitOfWork.Collection.GetMMSIUncollectedBillingsByCustomer(customerId, cancellationToken);
            if (collectionId.HasValue && collectionId.Value != 0)
            {
                var model = await unitOfWork.Collection.GetAsync(c => c.MMSICollectionId == collectionId.Value, cancellationToken);
                if (model?.CustomerId == customerId)
                {
                    list?.AddRange(await unitOfWork.Collection.GetMMSICollectedBillsById(collectionId.Value, cancellationToken));
                }
            }
            return list;
        }
    }
}
