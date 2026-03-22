using IBS.Models.Books;
using IBS.DataAccess.Repository.AccountsPayable.IRepository;
using IBS.DataAccess.Repository.AccountsReceivable.IRepository;
using IBS.DataAccess.Repository.Books.IRepository;
using IBS.DataAccess.Repository.Integrated.IRepository;
using IBS.DataAccess.Repository.IRepository;
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
        IDeliveryReceiptRepository DeliveryReceipt { get; }
        ISupplierRepository Supplier { get; }
        ICustomerRepository Customer { get; }
        IAuditTrailRepository AuditTrail { get; }
        IEmployeeRepository Employee { get; }
        ICustomerBranchRepository CustomerBranch { get; }
        ITermsRepository Terms { get; }

        Task<List<SelectListItem>> GetCustomerListAsyncById(string company, CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetSupplierListAsyncById(string company, CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetTradeSupplierListAsyncById(string company, CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetNonTradeSupplierListAsyncById(string company, CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetCommissioneeListAsyncById(string company, CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetHaulerListAsyncById(string company, CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetBankAccountListById(string company, CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetEmployeeListById(CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetDistinctPickupPointListById(string company, CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetServiceListById(string company, CancellationToken cancellationToken = default);

        #endregion

        #region --MMSI

        IMsapRepository Msap { get; }
        IServiceRequestRepository ServiceRequest { get; }
        IJobOrderRepository JobOrder { get; }
        IDispatchTicketRepository DispatchTicket { get; }
        IBillingRepository Billing { get; }
        ICollectionRepository Collection { get; }
        IMMSIReportRepository MMSIReport { get; }
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

        #region AAS

        #region Accounts Receivable
        ISalesInvoiceRepository SalesInvoice { get; }

        IServiceInvoiceRepository ServiceInvoice { get; }

        ICollectionReceiptRepository CollectionReceipt { get; }

        IDebitMemoRepository DebitMemo { get; }

        ICreditMemoRepository CreditMemo { get; }
        #endregion

        #region Accounts Payable

        ICheckVoucherRepository CheckVoucher { get; }

        IJournalVoucherRepository JournalVoucher { get; }

        IPurchaseOrderRepository PurchaseOrder { get; }

        IReceivingReportRepository ReceivingReport { get; }

        #endregion

        #region Books and Report
        IInventoryRepository Inventory { get; }

        IReportRepository Report { get; }
        #endregion

        #region Master File

        IBankAccountRepository BankAccount { get; }

        IServiceMasterRepository ServiceMaster { get; }

        IPickUpPointRepository PickUpPoint { get; }

        IFreightRepository Freight { get; }

        IAuthorityToLoadRepository AuthorityToLoad { get; }

        #endregion

        #endregion

        INotificationRepository Notifications { get; }

        Task<bool> IsPeriodPostedAsync(DateOnly date, CancellationToken cancellationToken = default);

        Task<DateTime> GetMinimumPeriodBasedOnThePostedPeriods(Module module, CancellationToken cancellationToken = default);

        Task<bool> IsPeriodPostedAsync(Module module, DateOnly date, CancellationToken cancellationToken = default);
    }
}
