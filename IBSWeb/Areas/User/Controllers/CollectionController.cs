using IBS.Models.Books;
using IBS.Models.Integrated;
using IBS.Models.MasterFile;
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
    [Area("User")]
    public class CollectionController : Controller
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<CollectionController> _logger;
        private readonly IUserAccessService _userAccessService;

        public CollectionController(ApplicationDbContext dbContext, IUnitOfWork unitOfWork,
            UserManager<ApplicationUser> userManager, ILogger<CollectionController> logger,
            IUserAccessService userAccessService)
        {
            _dbContext = dbContext;
            _unitOfWork = unitOfWork;
            _userManager = userManager;
            _logger = logger;
            _userAccessService = userAccessService;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Create(CancellationToken cancellationToken = default)
        {
            if (!await _userAccessService.CheckAccess(_userManager.GetUserId(User)!,
                    ProcedureEnum.CreateCollection,
                    cancellationToken))
            {
                TempData["error"] = "Access denied.";
                return RedirectToAction(nameof(Index));
            }

            var model = new CreateCollectionViewModel
            {
                Customers = await _unitOfWork.Collection.GetMMSICustomersWithCollectiblesSelectList(0,
                    String.Empty,
                    cancellationToken),
                BankAccounts = await _unitOfWork.GetBankAccountListById(cancellationToken)
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateCollectionViewModel viewModel, CancellationToken cancellationToken = default)
        {
            if (!ModelState.IsValid)
            {
                TempData["warning"] = "There was an error creating the collection.";
                viewModel.Customers = await _unitOfWork.Collection.GetMMSICustomersWithCollectiblesSelectList(0,
                    String.Empty,
                    cancellationToken);
                return View(viewModel);
            }

            try
            {
                var model = await CreateCollectionVmToCollectionModel(viewModel,
                    cancellationToken);
                var dateNow = DateTimeHelper.GetCurrentPhilippineTime();
                model.CreatedBy = await GetUserNameAsync() ?? throw new InvalidOperationException();
                model.CreatedDate = dateNow;
                model.Status = "Create";

                if (model.IsUndocumented)
                {
                    model.MMSICollectionNumber = await _unitOfWork.Collection.GenerateCollectionNumber(cancellationToken);
                }
                else
                {
                    model.MMSICollectionNumber = viewModel.MMSICollectionNumber ?? string.Empty;
                }

                await _unitOfWork.Collection.AddAsync(model,
                    cancellationToken);
                await _unitOfWork.SaveAsync(cancellationToken);

                // refetch to get the ID and other properties
                var refetchModel = await _unitOfWork.Collection.GetAsync(c =>
                        c.MMSICollectionId == model.MMSICollectionId,
                    cancellationToken);

                #region -- Audit Trail

                var audit = new AuditTrail(
                    await GetUserNameAsync() ?? throw new InvalidOperationException(),
                    $"Create collection #{model.MMSICollectionNumber} for billings #{string.Join(", #", viewModel.ToCollectBillings!)}",
                    "Collection"
                );

                await _unitOfWork.AuditTrail.AddAsync(audit,
                    cancellationToken);

                #endregion -- Audit Trail

                foreach (var collectBills in viewModel.ToCollectBillings!)
                {
                    var billingId = int.Parse(collectBills);
                    var billingChosen = await _unitOfWork.Billing.GetAsync(b => b.MMSIBillingId == billingId,
                        cancellationToken);
                    billingChosen!.Status = "Collected";
                    billingChosen.CollectionId = refetchModel!.MMSICollectionId;

                    // Simple allocation: full amount of billing is paid.
                    // In a real scenario, we might need more complex allocation if the collection doesn't match the sum of billings.
                    await _unitOfWork.Collection.UpdateBillingPayment(billingId,
                        billingChosen.Amount,
                        cancellationToken);
                }

                // Post to accounting books
                refetchModel = await _unitOfWork.Collection.GetAsync(c => c.MMSICollectionId == model.MMSICollectionId,
                    cancellationToken);
                await _unitOfWork.Collection.PostAsync(refetchModel!,
                    new List<Offsettings>(),
                    cancellationToken);

                if (model.IsUndocumented)
                {
                    TempData["success"] = $"Collection was successfully created. Control Number: {model.MMSICollectionNumber}";
                }
                else
                {
                    TempData["success"] = $"Collection #{model.MMSICollectionNumber} was successfully created.";
                }

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to create collection.");
                TempData["error"] = ex.Message;
                viewModel.Customers = await _unitOfWork.Collection.GetMMSICustomersWithCollectiblesSelectList(0,
                    String.Empty,
                    cancellationToken);
                return View(viewModel);
            }
        }

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
                Customer = await _unitOfWork.Customer
                    .GetAsync(c => c.CustomerId == viewModel.CustomerId,
                        cancellationToken)
            };

            if (viewModel.MMSICollectionId != null)
            {
                model.MMSICollectionId = viewModel.MMSICollectionId ?? 0;
            }

            return model;
        }

        public CreateCollectionViewModel CollectionModelToCreateCollectionVm(Collection model)
        {
            var viewModel = new CreateCollectionViewModel
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

            return viewModel;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GetCollectionList([FromForm] DataTablesParameters parameters, CancellationToken cancellationToken)
        {
            try
            {
                var queried = await _unitOfWork.Collection
                    .GetAllAsync(null,
                        cancellationToken);

                // Global search
                if (!string.IsNullOrEmpty(parameters.Search.Value))
                {
                    var searchValue = parameters.Search.Value.ToLower();
                    queried = queried
                    .Where(c =>
                        c.Date.ToString("MM/dd/yyyy").Contains(searchValue) ||
                        (c.CheckDate.HasValue && c.CheckDate.Value.ToString("MM/dd/yyyy").Contains(searchValue)) ||
                        (c.DepositDate.HasValue && c.DepositDate.Value.ToString("MM/dd/yyyy").Contains(searchValue)) ||
                        c.Amount.ToString().Contains(searchValue) ||
                        c.MMSICollectionNumber?.ToLower().Contains(searchValue) == true ||
                        c.Customer?.CustomerName?.ToLower().Contains(searchValue) == true ||
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
                _logger.LogError(ex,
                    "Failed to get collections");
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken = default)
        {
            var model = await _unitOfWork.Collection
                .GetAsync(c => c.MMSICollectionId == id,
                    cancellationToken);

            if (model == null)
            {
                return NotFound();
            }

            var viewModel = CollectionModelToCreateCollectionVm(model);

            // list of billing strings from previous create
            viewModel.ToCollectBillings = await _dbContext.Billings
                .Where(b => b.CollectionId == model.MMSICollectionId)
                .Select(b => b.MMSIBillingId.ToString())
                .ToListAsync(cancellationToken);

            // selection of customers
            viewModel.Customers = await _unitOfWork.Collection.GetMMSICustomersWithCollectiblesSelectList(id,
                model.Customer!.Type,
                cancellationToken);

            // get bills with same customer
            viewModel.Billings = await GetEditBillings(model.CustomerId,
                model.MMSICollectionId,
                cancellationToken);
            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(CreateCollectionViewModel viewModel, CancellationToken cancellationToken = default)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var model = await CreateCollectionVmToCollectionModel(viewModel,
                        cancellationToken);

                    //previous billings
                    var previousCollectedBills = await _unitOfWork.Billing
                        .GetAllAsync(b => b.CollectionId == model.MMSICollectionId,
                            cancellationToken);

                    //previous billings
                    var previousCollectedBillsString = await _dbContext.Billings
                        .Where(b => b.CollectionId == model.MMSICollectionId)
                        .Select(b => b.MMSIBillingId.ToString())
                        .ToListAsync(cancellationToken);

                    //revert old billings
                    foreach (var previousBilling in previousCollectedBills)
                    {
                        var billing = await _unitOfWork.Billing
                            .GetAsync(b => b.MMSIBillingId == previousBilling.MMSIBillingId,
                                cancellationToken);
                        if (billing == null) throw new NullReferenceException("Billing not found.");
                        billing.Status = "For Collection";
                        billing.CollectionId = 0;

                        await _unitOfWork.Collection.RemoveBillingPayment(billing.MMSIBillingId,
                            billing.Amount,
                            0,
                            cancellationToken);
                    }
                    await _unitOfWork.Billing.SaveAsync(cancellationToken);

                    if (viewModel.ToCollectBillings == null) throw new NullReferenceException("No Billing was selected.");

                    //relate new billings to collection
                    foreach (var newBilling in viewModel.ToCollectBillings)
                    {
                        var billing = await _unitOfWork.Billing
                            .GetAsync(b => b.MMSIBillingId == int.Parse(newBilling),
                                cancellationToken);
                        if (billing == null) throw new NullReferenceException("Billing not found.");
                        billing.Status = "Collected";
                        billing.CollectionId = model.MMSICollectionId;

                        await _unitOfWork.Collection.UpdateBillingPayment(billing.MMSIBillingId,
                            billing.Amount,
                            cancellationToken);
                    }
                    await _unitOfWork.Billing.SaveAsync(cancellationToken);

                    var currentModel = await _unitOfWork.Collection.GetAsync(c =>
                            c.MMSICollectionId == model.MMSICollectionId,
                        cancellationToken);

                    if (currentModel == null)
                    {
                        throw new NullReferenceException("The collection does not exist.");
                    }

                    #region -- Changes

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

                    #endregion -- Changes

                    #region -- Audit Trail

                    var audit = new AuditTrail(
                        await GetUserNameAsync() ?? throw new InvalidOperationException(),
                        $"Edit collection #{currentModel.MMSICollectionNumber} {string.Join(", ", changes)}",
                        "Billing"
                    );

                    await _unitOfWork.AuditTrail.AddAsync(audit,
                        cancellationToken);

                    #endregion -- Audit Trail

                    currentModel.Date = model.Date;
                    currentModel.CustomerId = model.CustomerId;
                    currentModel.CheckNumber = model.CheckNumber;
                    currentModel.CheckDate = model.CheckDate;
                    currentModel.DepositDate = model.DepositDate;
                    currentModel.Amount = model.Amount;
                    currentModel.EWT = model.EWT;

                    await _unitOfWork.Collection.SaveAsync(cancellationToken);
                    TempData["success"] = "Collection modified successfully";
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    var customer = await _unitOfWork.Customer
                        .GetAsync(c => c.CustomerId == viewModel.CustomerId,
                            cancellationToken);

                    TempData["warning"] = "There was an error updating the collection.";
                    viewModel.Customers = await _unitOfWork.Collection.GetMMSICustomersWithCollectiblesSelectList(
                        viewModel.MMSICollectionId ?? 0,
                        customer!.Type,
                        cancellationToken);
                    return View(viewModel);
                }
            }
            catch (Exception ex)
            {
                var customer = await _unitOfWork.Customer
                    .GetAsync(c => c.CustomerId == viewModel.CustomerId,
                        cancellationToken);

                _logger.LogError(ex,
                    "Failed to edit collection.");
                TempData["error"] = ex.Message;
                viewModel.Customers = await _unitOfWork.Collection.GetMMSICustomersWithCollectiblesSelectList(
                    viewModel.MMSICollectionId ?? 0,
                    customer!.Type,
                    cancellationToken);
                return View(viewModel);
            }
        }

        public async Task<IActionResult> Preview(int id, CancellationToken cancellationToken = default)
        {
            var collection = await _unitOfWork.Collection
                .GetAsync(c => c.MMSICollectionId == id,
                    cancellationToken);

            if (collection != null)
            {
                // list of dispatch tickets
                collection.PaidBills = (await _unitOfWork.Billing
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

        public async Task<IActionResult> GetCollections(CancellationToken cancellationToken = default)
        {
            var collections = await _unitOfWork.Collection.GetAllAsync(null,
                cancellationToken);
            return Json(collections);
        }

        [HttpPost]
        public async Task<IActionResult> GetSelectedBillings(List<string> billingIds, CancellationToken cancellationToken = default)
        {
            try
            {
                var intBillingIds = billingIds.Select(int.Parse).ToList();
                var billings = await _unitOfWork.Billing
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
                _logger.LogError(ex,
                    "Failed to get billings.");
                return Json(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        [HttpGet]
        public async Task<IActionResult> IsCustomerVatable(int customerId, CancellationToken cancellationToken = default)
        {
            try
            {
                var customer = await _unitOfWork.Customer
                    .GetAsync(c => c.CustomerId == customerId,
                        cancellationToken);

                if (customer != null)
                {
                    return Json(customer.VatType == SD.VatType_Vatable);
                }

                throw new NullReferenceException("Customer not found.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Customer not found.");
                return Json(new { success = false, message = "Customer not found" });
            }
        }

        private async Task<string?> GetCompanyClaimAsync()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return null;
            }

            var claims = await _userManager.GetClaimsAsync(user);
            return claims.FirstOrDefault(c => c.Type == "Company")?.Value;
        }

        private async Task<string?> GetUserNameAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            return user?.UserName;
        }

        public async Task<List<SelectListItem>?> GetEditBillings(int? customerId, int collectionId, CancellationToken cancellationToken = default)
        {
            // bills uncollected but with the same customers
            var list = await _unitOfWork.Collection.GetMMSIUncollectedBillingsByCustomer(customerId,
                cancellationToken);

            // get the current model
            var model = await _unitOfWork.Collection
                .GetAsync(c => c.MMSICollectionId == collectionId,
                    cancellationToken);

            // if the model WAS having previous customer, fetch it previous bills as well
            if (model?.CustomerId == customerId)
            {
                list?.AddRange(await _unitOfWork.Collection.GetMMSICollectedBillsById(collectionId,
                    cancellationToken));
            }

            return list;
        }

        public async Task<List<SelectListItem>?> GetUncollectedBillings(int? customerId, CancellationToken cancellationToken = default)
        {
            // bills uncollected by customer
            var list = await _unitOfWork.Collection.GetMMSIUncollectedBillingsByCustomer(customerId,
                cancellationToken);

            return list;
        }
    }
}
