using System.Linq.Expressions;
using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.DataAccess.Repository.MasterFile;
using IBS.DataAccess.Repository.MasterFile.IRepository;
using IBS.DataAccess.Repository.Msap;
using IBS.DataAccess.Repository.Msap.IRepository;
using IBS.Models.MasterFile;
using IBS.Utility.Constants;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace IBS.DataAccess.Repository
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _db;

        public ICompanyRepository Company { get; }

        public INotificationRepository Notifications { get; }

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

        public IChartOfAccountRepository ChartOfAccount { get; }
        public ISupplierRepository Supplier { get; }
        public ICustomerRepository Customer { get; }
        public IAuditTrailRepository AuditTrail { get; }
        public IEmployeeRepository Employee { get; }
        public ITermsRepository Terms { get; }

        #endregion

        #region --Master File

        public IBankAccountRepository BankAccount { get; }

        #endregion

        #region --MSAP

        public IMsapRepository Msap { get; }
        public IServiceRequestRepository ServiceRequest { get; }
        public IJobOrderRepository JobOrder { get; }
        public IDispatchTicketRepository DispatchTicket { get; }
        public IBillingRepository Billing { get; }
        public ICollectionRepository Collection { get; }
        public IReportRepository Report { get; }
        public ITariffTableRepository TariffTable { get; }
        public IPortRepository Port { get; }
        public IPrincipalRepository Principal { get; }
        public IServiceRepository Service { get; }
        public ITerminalRepository Terminal { get; }
        public ITugboatRepository Tugboat { get; }
        public ITugMasterRepository TugMaster { get; }
        public ITugboatOwnerRepository TugboatOwner { get; }
        public IUserAccessRepository UserAccess { get; }
        public IVesselRepository Vessel { get; }

        #endregion

        public UnitOfWork(ApplicationDbContext db)
        {
            _db = db;

            Company = new CompanyRepository(_db);
            Notifications = new NotificationRepository(_db);

            #region--Master Files

            ChartOfAccount = new ChartOfAccountRepository(_db);
            Customer = new CustomerRepository(_db);
            Supplier = new SupplierRepository(_db);
            AuditTrail = new AuditTrailRepository(_db);
            Employee = new EmployeeRepository(_db);
            Terms = new TermsRepository(_db);

            #endregion

            #region --Master File

            BankAccount = new BankAccountRepository(_db);

            #endregion

            #region --MSAP

            Billing = new BillingRepository(_db);
            Collection = new CollectionRepository(_db);
            DispatchTicket = new DispatchTicketRepository(_db);
            JobOrder = new JobOrderRepository(_db);
            Report = new ReportRepository(_db);
            Msap = new MsapRepository(_db);
            Port = new PortRepository(_db);
            Principal = new PrincipalRepository(_db);
            Service = new ServiceRepository(_db);
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

        // Make the function generic - always returns true (no company filtering)
        Expression<Func<T, bool>> GetCompanyFilter<T>(string companyName) where T : class
        {
            return x => true;
        }

        public async Task<List<SelectListItem>> GetCustomerListAsyncById(CancellationToken cancellationToken = default)
        {

            return await _db.Customers
                .OrderBy(c => c.CustomerId)
                .Where(c => c.IsActive)
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

        public async Task<List<SelectListItem>> GetBankAccountListById(CancellationToken cancellationToken = default)
        {
            return await _db.BankAccounts
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

        #endregion

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
