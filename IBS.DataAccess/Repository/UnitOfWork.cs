using IBS.Models.Books;
using System.ComponentModel;
using System.Linq.Expressions;
using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.AccountsPayable;
using IBS.DataAccess.Repository.AccountsPayable.IRepository;
using IBS.DataAccess.Repository.AccountsReceivable;
using IBS.DataAccess.Repository.AccountsReceivable.IRepository;
using IBS.DataAccess.Repository.Books;
using IBS.DataAccess.Repository.Books.IRepository;
using IBS.DataAccess.Repository.Integrated;
using IBS.DataAccess.Repository.Integrated.IRepository;
using IBS.DataAccess.Repository.IRepository;
using IBS.DataAccess.Repository.MasterFile;
using IBS.DataAccess.Repository.MasterFile.IRepository;
using IBS.DataAccess.Repository.MMSI;
using IBS.DataAccess.Repository.MMSI.IRepository;
using IBS.Models.Enums;
using IBS.Models.MasterFile;
using IBS.Utility.Constants;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace IBS.DataAccess.Repository
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _db;

        public IProductRepository Product { get; private set; }
        public ICompanyRepository Company { get; private set; }

        public INotificationRepository Notifications { get; private set; }

        public async Task<bool> IsPeriodPostedAsync(DateOnly date, CancellationToken cancellationToken = default)
        {
            return await _db.PostedPeriods
                .AnyAsync(m => m.IsPosted
                               && m.Month == date.Month
                               && m.Year == date.Year, cancellationToken);
        }

        public async Task<DateTime> GetMinimumPeriodBasedOnThePostedPeriods(Module module, CancellationToken cancellationToken = default)
        {
            if (!Enum.IsDefined(typeof(Module), module))
            {
                throw new InvalidEnumArgumentException(nameof(module), (int)module, typeof(Module));
            }

            var period = await _db.PostedPeriods
                .OrderByDescending(x => x.Year)
                .ThenByDescending(x => x.Month)
                .FirstOrDefaultAsync(x => x.Module == module.ToString()
                                          && x.IsPosted, cancellationToken);

            if (period == null)
            {
                return DateTime.MinValue;
            }

            return new DateOnly(period.Year, period.Month, 1)
                .AddMonths(1)
                .ToDateTime(new TimeOnly(0, 0));
        }

        public async Task<bool> IsPeriodPostedAsync(Module module, DateOnly date, CancellationToken cancellationToken = default)
        {
            if (!Enum.IsDefined(typeof(Module), module))
            {
                throw new InvalidEnumArgumentException(nameof(module), (int)module, typeof(Module));
            }

            return await _db.PostedPeriods
                .AnyAsync(m =>
                    m.Module == module.ToString() &&
                    m.IsPosted &&
                    m.Year == date.Year &&
                    m.Month == date.Month,
                    cancellationToken);
        }

        public async Task ExecuteInTransactionAsync(Func<Task> action, CancellationToken cancellationToken = default)
        {
            var strategy = _db.Database.CreateExecutionStrategy();

            await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
                try
                {
                    await action();
                    await transaction.CommitAsync(cancellationToken);
                }
                catch
                {
                    await transaction.RollbackAsync(cancellationToken);
                    throw;
                }
            });
        }

        #region--Master Files

        public IChartOfAccountRepository ChartOfAccount { get; private set; }
        public ICustomerOrderSlipRepository CustomerOrderSlip { get; private set; }
        public IDeliveryReceiptRepository DeliveryReceipt { get; private set; }
        public ISupplierRepository Supplier { get; private set; }
        public ICustomerRepository Customer { get; private set; }
        public IAuditTrailRepository AuditTrail { get; private set; }
        public IEmployeeRepository Employee { get; private set; }
        public ICustomerBranchRepository CustomerBranch { get; private set; }
        public ITermsRepository Terms { get; }

        #endregion

        #region AAS

        #region Accounts Receivable
        public ISalesInvoiceRepository SalesInvoice { get; private set; }

        public IServiceInvoiceRepository ServiceInvoice { get; private set; }

        public ICollectionReceiptRepository CollectionReceipt { get; private set; }

        public IDebitMemoRepository DebitMemo { get; private set; }

        public ICreditMemoRepository CreditMemo { get; private set; }
        #endregion

        #region Accounts Payable
        public ICheckVoucherRepository CheckVoucher { get; private set; }

        public IJournalVoucherRepository JournalVoucher { get; private set; }

        public IPurchaseOrderRepository PurchaseOrder { get; private set; }

        public IReceivingReportRepository ReceivingReport { get; private set; }
        #endregion

        #region Books and Report
        public IInventoryRepository Inventory { get; private set; }

        public IReportRepository Report { get; private set; }
        #endregion

        #region Master File

        public IBankAccountRepository BankAccount { get; private set; }

        public IServiceMasterRepository ServiceMaster { get; private set; }

        public IPickUpPointRepository PickUpPoint { get; private set; }

        public IFreightRepository Freight { get; private set; }

        public IAuthorityToLoadRepository AuthorityToLoad { get; private set; }

        #endregion

        #endregion

        #region --MMSI

        public IMsapRepository Msap { get; private set; }
        public IServiceRequestRepository ServiceRequest { get; private set; }
        public IJobOrderRepository JobOrder { get; private set; }
        public IDispatchTicketRepository DispatchTicket { get; private set; }
        public IBillingRepository Billing { get; private set; }
        public ICollectionRepository Collection { get; private set; }
        public IMMSIReportRepository MMSIReport { get; private set; }
        public ITariffTableRepository TariffTable { get; private set; }
        public IPortRepository Port { get; private set; }
        public IPrincipalRepository Principal { get; private set; }
        public MMSI.IRepository.IServiceRepository Service { get; private set; }
        public ITerminalRepository Terminal { get; private set; }
        public ITugboatRepository Tugboat { get; private set; }
        public ITugMasterRepository TugMaster { get; private set; }
        public ITugboatOwnerRepository TugboatOwner { get; private set; }
        public IUserAccessRepository UserAccess { get; private set; }
        public IVesselRepository Vessel { get; private set; }

        #endregion

        public UnitOfWork(ApplicationDbContext db)
        {
            _db = db;

            Product = new ProductRepository(_db);
            Company = new CompanyRepository(_db);
            Notifications = new NotificationRepository(_db);

            #region--Master Files

            CustomerOrderSlip = new CustomerOrderSlipRepository(_db);
            DeliveryReceipt = new DeliveryReceiptRepository(_db);
            Customer = new CustomerRepository(_db);
            Supplier = new SupplierRepository(_db);
            PickUpPoint = new PickUpPointRepository(_db);
            Freight = new FreightRepository(_db);
            AuthorityToLoad = new AuthorityToLoadRepository(_db);
            ChartOfAccount = new ChartOfAccountRepository(_db);
            AuditTrail = new AuditTrailRepository(_db);
            Employee = new EmployeeRepository(_db);
            CustomerBranch = new CustomerBranchRepository(_db);
            Terms = new TermsRepository(_db);

            #endregion

            #region AAS

            #region Accounts Receivable
            SalesInvoice = new SalesInvoiceRepository(_db);
            ServiceInvoice = new ServiceInvoiceRepository(_db);
            CollectionReceipt = new CollectionReceiptRepository(_db);
            DebitMemo = new DebitMemoRepository(_db);
            CreditMemo = new CreditMemoRepository(_db);
            #endregion

            #region Accounts Payable
            CheckVoucher = new CheckVoucherRepository(_db);
            JournalVoucher = new JournalVoucherRepository(_db);
            PurchaseOrder = new PurchaseOrderRepository(_db);
            ReceivingReport = new ReceivingReportRepository(_db);
            #endregion

            #region Books and Report
            Inventory = new InventoryRepository(_db);
            Report = new ReportRepository(_db);
            #endregion

            #region Master File

            BankAccount = new BankAccountRepository(_db);
            ServiceMaster = new ServiceMasterRepository(_db);

            #endregion

            #endregion

            #region --MMSI

            Billing = new BillingRepository(_db);
            Collection = new CollectionRepository(_db);
            DispatchTicket = new DispatchTicketRepository(_db);
            JobOrder = new JobOrderRepository(_db);
            MMSIReport = new MMSIReportRepository(_db);
            Msap = new MsapRepository(_db);
            Port = new PortRepository(_db);
            Principal = new PrincipalRepository(_db);
            Service = new MMSI.ServiceRepository(_db);
            ServiceRequest = new ServiceRequestRepository(_db);
            TariffTable = new TariffTableRepository(_db);
            Terminal = new TerminalRepository(_db);
            Tugboat = new TugboatRepository(_db);
            TugMaster = new TugMasterRepository(_db);
            TugboatOwner = new TugboatOwnerRepository(_db);
            UserAccess = new UserAccessRepository(_db);
            Vessel = new VesselRepository(_db);

            #endregion
        }

        public async Task SaveAsync(CancellationToken cancellationToken = default)
        {
            await _db.SaveChangesAsync(cancellationToken);
        }

        public void Dispose() => _db.Dispose();

        #region--Master Files

        // Make the function generic
        Expression<Func<T, bool>> GetCompanyFilter<T>(string companyName) where T : class
        {
            // Use reflection or property pattern matching to dynamically access properties
            var param = Expression.Parameter(typeof(T), "x");

            // Build the appropriate expression based on the company name
            Expression propertyAccess = companyName switch
            {
                SD.Company_Filpride => Expression.Property(param, "IsFilpride"),
                SD.Company_MMSI => Expression.OrElse(Expression.Property(param, "IsFilpride"), Expression.Property(param, "IsMMSI")),
                _ => Expression.Constant(false)
            };

            return Expression.Lambda<Func<T, bool>>(propertyAccess, param);
        }

        public async Task<List<SelectListItem>> GetCustomerListAsyncById(string company, CancellationToken cancellationToken = default)
        {

            return await _db.Customers
                .OrderBy(c => c.CustomerId)
                .Where(c => c.IsActive)
                .Where(GetCompanyFilter<Customer>(company))
                .Select(c => new SelectListItem
                {
                    Value = c.CustomerId.ToString(),
                    Text = c.CustomerName
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<List<SelectListItem>> GetSupplierListAsyncById(string company, CancellationToken cancellationToken = default)
        {
            return await _db.Suppliers
                .OrderBy(s => s.SupplierCode)
                .Where(s => s.IsActive)
                .Where(GetCompanyFilter<Supplier>(company))
                .Select(s => new SelectListItem
                {
                    Value = s.SupplierId.ToString(),
                    Text = s.SupplierCode + " " + s.SupplierName
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<List<SelectListItem>> GetTradeSupplierListAsyncById(string company, CancellationToken cancellationToken = default)
        {
            return await _db.Suppliers
                .OrderBy(s => s.SupplierCode)
                .Where(s => s.IsActive && s.Category == "Trade")
                .Where(GetCompanyFilter<Supplier>(company))
                .Select(s => new SelectListItem
                {
                    Value = s.SupplierId.ToString(),
                    Text = s.SupplierCode + " " + s.SupplierName
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<List<SelectListItem>> GetNonTradeSupplierListAsyncById(string company, CancellationToken cancellationToken = default)
        {
            return await _db.Suppliers
                .OrderBy(s => s.SupplierCode)
                .Where(s => s.IsActive && s.Category == "Non-Trade")
                .Where(GetCompanyFilter<Supplier>(company))
                .Select(s => new SelectListItem
                {
                    Value = s.SupplierId.ToString(),
                    Text = s.SupplierCode + " " + s.SupplierName
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<List<SelectListItem>> GetCommissioneeListAsyncById(string company, CancellationToken cancellationToken = default)
        {
            return await _db.Suppliers
                .OrderBy(s => s.SupplierCode)
                .Where(s => s.IsActive && s.Category == "Commissionee")
                .Where(GetCompanyFilter<Supplier>(company))
                .Select(s => new SelectListItem
                {
                    Value = s.SupplierId.ToString(),
                    Text = s.SupplierCode + " " + s.SupplierName
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<List<SelectListItem>> GetHaulerListAsyncById(string company, CancellationToken cancellationToken = default)
        {
            return await _db.Suppliers
                .OrderBy(s => s.SupplierCode)
                .Where(s => s.IsActive && s.Company == company && s.Category == "Hauler")
                .Where(GetCompanyFilter<Supplier>(company))
                .Select(s => new SelectListItem
                {
                    Value = s.SupplierId.ToString(),
                    Text = s.SupplierCode + " " + s.SupplierName
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<List<SelectListItem>> GetBankAccountListById(string company, CancellationToken cancellationToken = default)
        {
            return await _db.BankAccounts
                .Where(GetCompanyFilter<BankAccount>(company))
                .Select(ba => new SelectListItem
                {
                    Value = ba.BankAccountId.ToString(),
                    Text = ba.Bank + " " + ba.AccountNo + " " + ba.AccountName
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<List<SelectListItem>> GetEmployeeListById(CancellationToken cancellationToken = default)
        {
            return await _db.Employees
                .Where(e => e.IsActive)
                .Select(e => new SelectListItem
                {
                    Value = e.EmployeeId.ToString(),
                    Text = $"{e.EmployeeNumber} - {e.FirstName} {e.LastName}"
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<List<SelectListItem>> GetDistinctPickupPointListById(string companyClaims, CancellationToken cancellationToken = default)
        {
            return await _db.PickUpPoints
                .Where(GetCompanyFilter<PickUpPoint>(companyClaims))
                .GroupBy(p => p.Depot)
                .OrderBy(g => g.Key)
                .Select(g => new SelectListItem
                {
                    Value = g.First().PickUpPointId.ToString(),
                    Text = g.Key // g.Key is the Depot name
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<List<SelectListItem>> GetServiceListById(string companyClaims, CancellationToken cancellationToken = default)
        {
            return await _db.Services
                .OrderBy(s => s.ServiceId)
                .Where(GetCompanyFilter<ServiceMaster>(companyClaims))
                .Select(s => new SelectListItem
                {
                    Value = s.ServiceId.ToString(),
                    Text = s.Name
                })
                .ToListAsync(cancellationToken);
        }

        #endregion

        public async Task<List<SelectListItem>> GetProductListAsyncByCode(CancellationToken cancellationToken = default)
        {
            return await _db.Products
                .OrderBy(p => p.ProductId)
                .Where(p => p.IsActive)
                .Select(p => new SelectListItem
                {
                    Value = p.ProductCode,
                    Text = p.ProductCode + " " + p.ProductName
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<List<SelectListItem>> GetProductListAsyncById(CancellationToken cancellationToken = default)
        {
            return await _db.Products
                .OrderBy(p => p.ProductId)
                .Where(p => p.IsActive)
                .Select(p => new SelectListItem
                {
                    Value = p.ProductId.ToString(),
                    Text = p.ProductCode + " " + p.ProductName
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<List<SelectListItem>> GetCashierListAsyncByUsernameAsync(CancellationToken cancellationToken = default)
        {
            return await _db.ApplicationUsers
                .OrderBy(p => p.Id)
                .Where(p => p.Department == SD.Department_StationCashier)
                .Select(p => new SelectListItem
                {
                    Value = p.UserName!.ToString(),
                    Text = p.UserName.ToString()
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<List<SelectListItem>> GetCashierListAsyncByStationAsync(CancellationToken cancellationToken = default)
        {
            return await _db.ApplicationUsers
                .OrderBy(p => p.Id)
                .Where(p => p.Department == SD.Department_StationCashier)
                .Select(p => new SelectListItem
                {
                    Value = p.StationAccess!.ToString(),
                    Text = p.UserName!.ToString()
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<List<SelectListItem>> GetChartOfAccountListAsyncById(CancellationToken cancellationToken = default)
        {
            return await _db.ChartOfAccounts
                .Where(coa => !coa.HasChildren)
                .OrderBy(coa => coa.AccountNumber)
                .Select(s => new SelectListItem
                {
                    Value = s.AccountId.ToString(),
                    Text = s.AccountNumber + " " + s.AccountName
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<List<SelectListItem>> GetChartOfAccountListAsyncByNo(CancellationToken cancellationToken = default)
        {
            return await _db.ChartOfAccounts
                .Where(coa => !coa.HasChildren)
                .OrderBy(coa => coa.AccountNumber)
                .Select(s => new SelectListItem
                {
                    Value = s.AccountNumber,
                    Text = $"({s.AccountType}) {s.AccountNumber} {s.AccountName}"
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<List<SelectListItem>> GetChartOfAccountListAsyncByAccountTitle(CancellationToken cancellationToken = default)
        {
            return await _db.ChartOfAccounts
                .Where(coa => !coa.HasChildren)
                .OrderBy(coa => coa.AccountNumber)
                .Select(s => new SelectListItem
                {
                    Value = s.AccountNumber + " " + s.AccountName,
                    Text = $"({s.AccountType}) {s.AccountNumber} {s.AccountName}"
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<List<SelectListItem>> GetCompanyListAsyncByName(CancellationToken cancellationToken = default)
        {
            return await _db.Companies
                .OrderBy(c => c.CompanyCode)
                .Where(c => c.IsActive)
                .Select(c => new SelectListItem
                {
                    Value = c.CompanyName,
                    Text = c.CompanyCode + " " + c.CompanyName
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<List<SelectListItem>> GetCompanyListAsyncById(CancellationToken cancellationToken = default)
        {
            return await _db.Companies
                .OrderBy(c => c.CompanyCode)
                .Where(c => c.IsActive)
                .Select(c => new SelectListItem
                {
                    Value = c.CompanyId.ToString(),
                    Text = c.CompanyCode + " " + c.CompanyName
                })
                .ToListAsync(cancellationToken);
        }
    }
}
