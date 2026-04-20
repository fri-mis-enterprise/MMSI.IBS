using IBS.DataAccess.Repository.Integrated.IRepository;
using IBS.DataAccess.Repository.MasterFile.IRepository;
using IBS.DataAccess.Repository.MMSI.IRepository;
using IBS.Models.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IBS.DataAccess.Repository.IRepository
{
    public interface IUnitOfWork : IDisposable
    {
        IProductRepository Product { get; }

        ICompanyRepository Company { get; }

        Task SaveAsync(CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetProductListAsyncByCode(CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetProductListAsyncById(CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetChartOfAccountListAsyncByNo(CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetChartOfAccountListAsyncById(CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetChartOfAccountListAsyncByAccountTitle(CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetCompanyListAsyncByName(CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetCompanyListAsyncById(CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetCashierListAsyncByUsernameAsync(CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetCashierListAsyncByStationAsync(CancellationToken cancellationToken = default);

        Task ExecuteInTransactionAsync(Func<Task> action, CancellationToken cancellationToken = default);

        #region--Master Files

        IChartOfAccountRepository ChartOfAccount { get; }
        ICustomerOrderSlipRepository CustomerOrderSlip { get; }
        ISupplierRepository Supplier { get; }
        ICustomerRepository Customer { get; }
        IAuditTrailRepository AuditTrail { get; }
        IEmployeeRepository Employee { get; }
        ICustomerBranchRepository CustomerBranch { get; }
        ITermsRepository Terms { get; }

        Task<List<SelectListItem>> GetCustomerListAsyncById(CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetSupplierListAsyncById(string company, CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetTradeSupplierListAsyncById(string company, CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetNonTradeSupplierListAsyncById(string company, CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetCommissioneeListAsyncById(string company, CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetHaulerListAsyncById(string company, CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetBankAccountListById(string company, CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetEmployeeListById(CancellationToken cancellationToken = default);

        #endregion

        #region --Master File

        IBankAccountRepository BankAccount { get; }
        IServiceMasterRepository ServiceMaster { get; }
        IPickUpPointRepository PickUpPoint { get; }

        #endregion

        #region --MMSI

        IMsapRepository Msap { get; }
        IServiceRequestRepository ServiceRequest { get; }
        IJobOrderRepository JobOrder { get; }
        IDispatchTicketRepository DispatchTicket { get; }
        IBillingRepository Billing { get; }
        ICollectionRepository Collection { get; }
        IReportRepository Report { get; }
        MMSI.IRepository.IServiceRepository Service { get; }
        ITariffTableRepository TariffTable { get; }
        IPortRepository Port { get; }
        IPrincipalRepository Principal { get; }
        ITerminalRepository Terminal { get; }
        ITugboatRepository Tugboat { get; }
        ITugMasterRepository TugMaster { get; }
        ITugboatOwnerRepository TugboatOwner { get; }
        IUserAccessRepository UserAccess { get; }
        IVesselRepository Vessel { get; }

        #endregion

        INotificationRepository Notifications { get; }

        Task<bool> IsPeriodPostedAsync(DateOnly date, CancellationToken cancellationToken = default);

        Task<DateTime> GetMinimumPeriodBasedOnThePostedPeriods(Module module, CancellationToken cancellationToken = default);

        Task<bool> IsPeriodPostedAsync(Module module, DateOnly date, CancellationToken cancellationToken = default);
    }
}
