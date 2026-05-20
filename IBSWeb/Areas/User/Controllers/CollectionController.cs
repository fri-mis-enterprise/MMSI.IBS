using System.Globalization;
using System.Linq.Dynamic.Core;
using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using IBS.Models.Enums;
using IBS.Models.MMSI;
using IBS.Models.MMSI.ViewModels;
using IBS.Services;
using IBS.Utility.Constants;
using IBS.Utility.Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace IBSWeb.Areas.User.Controllers
{
    /// <summary>
    /// Controller for managing Collections and payment allocations in the MMSI system.
    /// </summary>
    [Area("User")]
    public class CollectionController(
        ApplicationDbContext dbContext,
        IUnitOfWork unitOfWork,
        UserManager<ApplicationUser> userManager,
        ILogger<CollectionController> logger,
        IUserAccessService userAccessService)
        : Controller
    {
        #region Index

        /// <summary>
        /// Displays the list of Collections.
        /// </summary>
        public IActionResult Index()
        {
            return View();
        }

        #endregion

        #region Create

        /// <summary>
        /// Displays the form to create a new Collection.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Create(CancellationToken cancellationToken = default)
        {
            if (!await userAccessService.CheckAccess(userManager.GetUserId(User)!,
                    ProcedureEnum.CreateCollection,
                    cancellationToken))
            {
                TempData["error"] = "Access denied.";
                return RedirectToAction(nameof(Index));
            }

            var model = new CreateCollectionViewModel
            {
                Customers = await unitOfWork.Collection.GetMMSICustomersWithCollectiblesSelectList(0,
                    String.Empty,
                    cancellationToken),
                BankAccounts = await unitOfWork.GetBankAccountListById(cancellationToken)
            };

            return View(model);
        }

        /// <summary>
        /// Processes the creation of a new Collection, including billing allocation and accounting posting.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Create(CreateCollectionViewModel viewModel, CancellationToken cancellationToken = default)
        {
            if (!ModelState.IsValid)
            {
                TempData["warning"] = "There was an error creating the collection.";
                viewModel.Customers = await unitOfWork.Collection.GetMMSICustomersWithCollectiblesSelectList(0,
                    String.Empty,
                    cancellationToken);
                return View(viewModel);
            }

            try
            {
                var model = await CreateCollectionVmToCollectionModel(viewModel, cancellationToken);
                var dateNow = DateTimeHelper.GetCurrentPhilippineTime();
                model.CreatedBy = await GetUserNameAsync() ?? throw new InvalidOperationException();
                model.CreatedDate = dateNow;
                model.Status = "Create";

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

                // Fetch the saved model to ensure all properties (like ID) are available for associated logic.
                var refetchModel = await unitOfWork.Collection.GetAsync(c =>
                        c.MMSICollectionId == model.MMSICollectionId,
                    cancellationToken);

                var audit = new AuditTrail(
                    await GetUserNameAsync() ?? throw new InvalidOperationException(),
                    $"Create collection #{model.MMSICollectionNumber} for billings #{string.Join(", #", viewModel.ToCollectBillings!)}",
                    "Collection"
                );

                await unitOfWork.AuditTrail.AddAsync(audit, cancellationToken);

                // Allocate payment to selected billings.
                foreach (var collectBills in viewModel.ToCollectBillings!)
                {
                    var billingId = int.Parse(collectBills);
                    var billingChosen = await unitOfWork.Billing.GetAsync(b => b.MMSIBillingId == billingId,
                        cancellationToken);
                    billingChosen!.Status = "Collected";
                    billingChosen.CollectionId = refetchModel!.MMSICollectionId;

                    // Standard allocation: marks the billing as fully paid in the ledger.
                    await unitOfWork.Collection.UpdateBillingPayment(billingId,
                        billingChosen.Amount,
                        cancellationToken);
                }

                // Post the collection to the accounting books.
                refetchModel = await unitOfWork.Collection.GetAsync(c => c.MMSICollectionId == model.MMSICollectionId,
                    cancellationToken);
                await unitOfWork.Collection.PostAsync(refetchModel!,
                    new List<Offsettings>(),
                    cancellationToken);

                TempData["success"] = model.IsUndocumented
                    ? $"Collection was successfully created. Control Number: {model.MMSICollectionNumber}"
                    : $"Collection #{model.MMSICollectionNumber} was successfully created.";

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to create collection.");
                TempData["error"] = ex.Message;
                viewModel.Customers = await unitOfWork.Collection.GetMMSICustomersWithCollectiblesSelectList(0,
                    String.Empty,
                    cancellationToken);
                return View(viewModel);
            }
        }

        #endregion

        #region Edit

        /// <summary>
        /// Displays the form to edit an existing Collection.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken = default)
        {
            var model = await unitOfWork.Collection
                .GetAsync(c => c.MMSICollectionId == id,
                    cancellationToken);

            if (model == null)
            {
                return NotFound();
            }

            var viewModel = CollectionModelToCreateCollectionVm(model);

            // Populate currently associated billings.
            viewModel.ToCollectBillings = await dbContext.Billings
                .Where(b => b.CollectionId == model.MMSICollectionId)
                .Select(b => b.MMSIBillingId.ToString())
                .ToListAsync(cancellationToken);

            // Populate selection lists.
            viewModel.Customers = await unitOfWork.Collection.GetMMSICustomersWithCollectiblesSelectList(id,
                model.Customer.Type,
                cancellationToken);

            viewModel.Billings = await GetEditBillings(model.CustomerId,
                model.MMSICollectionId,
                cancellationToken);

            viewModel.BankAccounts = await unitOfWork.GetBankAccountListById(cancellationToken);

            return View(viewModel);
        }

        /// <summary>
        /// Processes the update of an existing Collection, including reverting old allocations and applying new ones.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Edit(CreateCollectionViewModel viewModel, CancellationToken cancellationToken = default)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var model = await CreateCollectionVmToCollectionModel(viewModel, cancellationToken);

                    // Revert previously collected billings back to 'For Collection' status.
                    var previousCollectedBills = await unitOfWork.Billing
                        .GetAllAsync(b => b.CollectionId == model.MMSICollectionId,
                            cancellationToken);

                    var previousCollectedBillsString = await dbContext.Billings
                        .Where(b => b.CollectionId == model.MMSICollectionId)
                        .Select(b => b.MMSIBillingId.ToString())
                        .ToListAsync(cancellationToken);

                    foreach (var previousBilling in previousCollectedBills)
                    {
                        var billing = await unitOfWork.Billing
                            .GetAsync(b => b.MMSIBillingId == previousBilling.MMSIBillingId,
                                cancellationToken);
                        if (billing == null)
                        {
                            throw new NullReferenceException("Billing not found.");
                        }

                        billing.Status = "For Collection";
                        billing.CollectionId = 0;

                        await unitOfWork.Collection.RemoveBillingPayment(billing.MMSIBillingId,
                            billing.Amount,
                            0,
                            cancellationToken);
                    }
                    await unitOfWork.Billing.SaveAsync(cancellationToken);

                    if (viewModel.ToCollectBillings == null)
                    {
                        throw new NullReferenceException("No Billing was selected.");
                    }

                    // Apply new billing associations to the collection.
                    foreach (var newBilling in viewModel.ToCollectBillings)
                    {
                        var billing = await unitOfWork.Billing
                            .GetAsync(b => b.MMSIBillingId == int.Parse(newBilling),
                                cancellationToken);
                        if (billing == null)
                        {
                            throw new NullReferenceException("Billing not found.");
                        }

                        billing.Status = "Collected";
                        billing.CollectionId = model.MMSICollectionId;

                        await unitOfWork.Collection.UpdateBillingPayment(billing.MMSIBillingId,
                            billing.Amount,
                            cancellationToken);
                    }
                    await unitOfWork.Billing.SaveAsync(cancellationToken);

                    var currentModel = await unitOfWork.Collection.GetAsync(c =>
                            c.MMSICollectionId == model.MMSICollectionId,
                        cancellationToken);

                    if (currentModel == null)
                    {
                        throw new NullReferenceException("The collection does not exist.");
                    }

                    // Track changes for audit trail.
                    var changes = new List<string>();
                    if (currentModel.CheckNumber != model.CheckNumber) { changes.Add($"CheckNumber: {currentModel.CheckNumber} -> {model.CheckNumber}"); }
                    if (currentModel.Date != model.Date) { changes.Add($"Date: {currentModel.Date} -> {model.Date}"); }
                    if (currentModel.CustomerId != model.CustomerId) { changes.Add($"CustomerId: {currentModel.CustomerId} -> {model.CustomerId}"); }
                    if (currentModel.Amount != model.Amount) { changes.Add($"Amount: {currentModel.Amount} -> {model.Amount}"); }
                    if (currentModel.EWT != model.EWT) { changes.Add($"EWT: {currentModel.EWT} -> {model.EWT}"); }
                    if (currentModel.CheckDate != model.CheckDate) { changes.Add($"CheckDate: {currentModel.CheckDate} -> {model.CheckDate}"); }
                    if (currentModel.DepositDate != model.DepositDate) { changes.Add($"DepositDate: {currentModel.DepositDate} -> {model.DepositDate}"); }
                    if (!previousCollectedBillsString.OrderBy(x => x).SequenceEqual(viewModel.ToCollectBillings.OrderBy(x => x)))
                    { changes.Add($"ToBillDispatchTickets: #{string.Join(", #", previousCollectedBillsString)} -> #{string.Join(", #", viewModel.ToCollectBillings)}"); }

                    var audit = new AuditTrail(
                        await GetUserNameAsync() ?? throw new InvalidOperationException(),
                        $"Edit collection #{currentModel.MMSICollectionNumber} {string.Join(", ", changes)}",
                        "Billing"
                    );

                    await unitOfWork.AuditTrail.AddAsync(audit, cancellationToken);

                    // Update current model properties.
                    currentModel.Date = model.Date;
                    currentModel.CustomerId = model.CustomerId;
                    currentModel.CheckNumber = model.CheckNumber;
                    currentModel.CheckDate = model.CheckDate;
                    currentModel.DepositDate = model.DepositDate;
                    currentModel.Amount = model.Amount;
                    currentModel.EWT = model.EWT;

                    await unitOfWork.Collection.SaveAsync(cancellationToken);
                    TempData["success"] = "Collection modified successfully";
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    var customer = await unitOfWork.Customer
                        .GetAsync(c => c.CustomerId == viewModel.CustomerId,
                            cancellationToken);

                    TempData["warning"] = "There was an error updating the collection.";
                    viewModel.Customers = await unitOfWork.Collection.GetMMSICustomersWithCollectiblesSelectList(
                        viewModel.MMSICollectionId ?? 0,
                        customer!.Type,
                        cancellationToken);
                    return View(viewModel);
                }
            }
            catch (Exception ex)
            {
                var customer = await unitOfWork.Customer
                    .GetAsync(c => c.CustomerId == viewModel.CustomerId,
                        cancellationToken);

                logger.LogError(ex, "Failed to edit collection.");
                TempData["error"] = ex.Message;
                viewModel.Customers = await unitOfWork.Collection.GetMMSICustomersWithCollectiblesSelectList(
                    viewModel.MMSICollectionId ?? 0,
                    customer!.Type,
                    cancellationToken);
                return View(viewModel);
            }
        }

        #endregion

        #region Preview & Listing

        /// <summary>
        /// Displays the details of a specific Collection, including associated paid billings.
        /// </summary>
        public async Task<IActionResult> Preview(int id, CancellationToken cancellationToken = default)
        {
            var collection = await unitOfWork.Collection
                .GetAsync(c => c.MMSICollectionId == id,
                    cancellationToken);

            if (collection != null)
            {
                collection.PaidBills = (await unitOfWork.Billing
                    .GetAllAsync(b => b.CollectionId == collection.MMSICollectionId,
                        cancellationToken)).ToList();
                return View(collection);
            }
            else
            {
                TempData["Error"] = "Error: collection record not found.";
                return RedirectToAction("Index");
            }
        }

        /// <summary>
        /// Retrieves all collections as a JSON list.
        /// </summary>
        public async Task<IActionResult> GetCollections(CancellationToken cancellationToken = default)
        {
            var collections = await unitOfWork.Collection.GetAllAsync(null, cancellationToken);
            return Json(collections);
        }

        /// <summary>
        /// Retrieves a paged and filtered list of Collections for DataTables.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GetCollectionList([FromForm] DataTablesParameters parameters, CancellationToken cancellationToken)
        {
            try
            {
                var queried = await unitOfWork.Collection.GetAllAsync(null, cancellationToken);

                // Global search
                if (!string.IsNullOrEmpty(parameters.Search.Value))
                {
                    var searchValue = parameters.Search.Value.ToLower();
                    queried = queried
                    .Where(c =>
                        c.Date.ToString("MM/dd/yyyy").Contains(searchValue) ||
                        (c.CheckDate.HasValue && c.CheckDate.Value.ToString("MM/dd/yyyy").Contains(searchValue)) ||
                        (c.DepositDate.HasValue && c.DepositDate.Value.ToString("MM/dd/yyyy").Contains(searchValue)) ||
                        c.Amount.ToString(CultureInfo.CurrentCulture).Contains(searchValue) ||
                        c.MMSICollectionNumber.ToLower().Contains(searchValue) ||
                        c.Customer.CustomerName.ToLower().Contains(searchValue) ||
                        c.Status?.ToLower().Contains(searchValue) == true
                        )
                    .ToList();
                }

                // Sorting
                if (parameters.Order?.Count > 0)
                {
                    var orderColumn = parameters.Order[0];
                    var columnName = parameters.Columns[orderColumn.Column].Data;
                    var sortDirection = orderColumn.Dir.ToLower() == "asc" ? "ascending" : "descending";
                    queried = queried
                        .AsQueryable()
                        .OrderBy($"{columnName} {sortDirection}")
                        .ToList();
                }

                IEnumerable<Collection> collections = queried as Collection[] ?? queried.ToArray();
                var totalRecords = collections.Count();
                var pagedData = collections
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
                logger.LogError(ex, "Failed to get collections");
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        #endregion

        #region AJAX Endpoints

        /// <summary>
        /// Retrieves detailed information for a list of selected billings.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> GetSelectedBillings(List<string> billingIds, CancellationToken cancellationToken = default)
        {
            try
            {
                var intBillingIds = billingIds.Select(int.Parse).ToList();
                var billings = await unitOfWork.Billing
                    .GetAllAsync(b => intBillingIds.Contains(b.MMSIBillingId),
                        cancellationToken);
                return Json(new
                {
                    success = true,
                    data = billings
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to get billings.");
                return Json(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Checks if a customer is vatable based on their ID.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> IsCustomerVatable(int customerId, CancellationToken cancellationToken = default)
        {
            try
            {
                var customer = await unitOfWork.Customer.GetAsync(c => c.CustomerId == customerId, cancellationToken);

                if (customer != null)
                {
                    return Json(customer.VatType == SD.VatType_Vatable);
                }

                throw new NullReferenceException("Customer not found.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Customer not found.");
                return Json(new { success = false, message = "Customer not found" });
            }
        }

        /// <summary>
        /// Retrieves detailed information for a specific bank account.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetBankAccountDetails(int bankId, CancellationToken cancellationToken = default)
        {
            try
            {
                var bank = await unitOfWork.BankAccount.GetAsync(b => b.BankAccountId == bankId, cancellationToken);
                if (bank != null)
                {
                    return Json(new { success = true, bank = bank.Bank, accountNo = bank.AccountNo, accountName = bank.AccountName });
                }
                return Json(new { success = false, message = "Bank not found" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        #endregion

        #region Mapper & Helpers

        /// <summary>
        /// Maps a CreateCollectionViewModel to a Collection entity.
        /// </summary>
        public async Task<Collection> CreateCollectionVmToCollectionModel(CreateCollectionViewModel viewModel, CancellationToken cancellationToken = default)
        {
            var model = new Collection
            {
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
                Customer = (await unitOfWork.Customer
                    .GetAsync(c => c.CustomerId == viewModel.CustomerId,
                        cancellationToken))!
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

            if (viewModel.MMSICollectionId != null)
            {
                model.MMSICollectionId = viewModel.MMSICollectionId ?? 0;
            }

            return model;
        }

        /// <summary>
        /// Maps a Collection entity to a CreateCollectionViewModel.
        /// </summary>
        public CreateCollectionViewModel CollectionModelToCreateCollectionVm(Collection model)
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

        /// <summary>
        /// Retrieves the username of the current user.
        /// </summary>
        private async Task<string?> GetUserNameAsync()
        {
            var user = await userManager.GetUserAsync(User);
            return user?.UserName;
        }

        /// <summary>
        /// Retrieves uncollected billings (and optionally currently associated ones) for a specific customer in a detailed format for DataTables.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetUncollectedBillingsForTable(int customerId, int? collectionId, CancellationToken cancellationToken = default)
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
                        Ewt = b.BilledTo == "LOCAL" ? (b.Amount / 1.12m) * 0.02m : 0m,
                        Net = b.BilledTo == "LOCAL" ? b.Amount - ((b.Amount / 1.12m) * 0.02m) : b.Amount,
                        IsSelected = collectionId.HasValue && b.CollectionId == collectionId.Value
                    });

                return Json(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to get billings for table.");
                return Json(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Retrieves billings for a customer during edit mode, including already collected ones.
        /// </summary>
        public async Task<List<SelectListItem>?> GetEditBillings(int? customerId, int? collectionId, CancellationToken cancellationToken = default)
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

        /// <summary>
        /// Retrieves uncollected billings for a specific customer.
        /// </summary>
        public async Task<List<SelectListItem>?> GetUncollectedBillings(int? customerId, CancellationToken cancellationToken = default)
        {
            return await unitOfWork.Collection.GetMMSIUncollectedBillingsByCustomer(customerId, cancellationToken);
        }

        #endregion
    }
}
