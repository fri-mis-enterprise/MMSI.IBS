using IBS.Models;
using IBS.Models.Integrated;
using IBS.Models.MasterFile;
using IBS.Models.MMSI;
using IBS.Models.MMSI.MasterFile;
using IBS.Models.Books;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace IBS.DataAccess.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions options) : base(options)
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSnakeCaseNamingConvention();
        }

        public DbSet<ApplicationUser> ApplicationUsers { get; set; }

        public DbSet<LogMessage> LogMessages { get; set; }

        public DbSet<AppSetting> AppSettings { get; set; }

        public DbSet<Notification> Notifications { get; set; }

        public DbSet<UserNotification> UserNotifications { get; set; }

        public DbSet<HubConnection> HubConnections { get; set; }

        public DbSet<PostedPeriod> PostedPeriods { get; set; }

        public DbSet<AuditTrail> AuditTrails { get; set; }

        #region--Integrated

        public DbSet<CustomerOrderSlip> CustomerOrderSlips { get; set; }

        public DbSet<DeliveryReceipt> DeliveryReceipts { get; set; }

        public DbSet<Freight> Freights { get; set; }

        public DbSet<AuthorityToLoad> AuthorityToLoads { get; set; }

        public DbSet<COSAppointedSupplier> COSAppointedSuppliers { get; set; }

        public DbSet<POActualPrice> POActualPrices { get; set; }

        public DbSet<CustomerBranch> CustomerBranches { get; set; }

        public DbSet<BookAtlDetail> BookAtlDetails { get; set; }

        public DbSet<MonthlyNibit> MonthlyNibits { get; set; }

        public DbSet<SalesLockedRecordsQueue> SalesLockedRecordsQueues { get; set; }

        public DbSet<PurchaseLockedRecordsQueue> PurchaseLockedRecordsQueues { get; set; }

        public DbSet<GLPeriodBalance> GlPeriodBalances { get; set; }

        public DbSet<GLSubAccountBalance> GlSubAccountBalances { get; set; }

        #region--Master File

        public DbSet<Customer> Customers { get; set; }

        public DbSet<Supplier> Suppliers { get; set; }

        public DbSet<PickUpPoint> PickUpPoints { get; set; }

        public DbSet<Employee> Employees { get; set; }

        public DbSet<Terms> Terms { get; set; }

        #endregion

        #region --MMSI
        public DbSet<Billing> MMSIBillings { get; set; }
        public DbSet<Collection> MMSICollections { get; set; }
        public DbSet<DispatchTicket> MMSIDispatchTickets { get; set; }
        public DbSet<JobOrder> MMSIJobOrders { get; set; }
        public DbSet<TariffRate> MMSITariffRates { get; set; }

        #endregion

        #region --Master File Entity

        public DbSet<Service> MMSIServices { get; set; }
        public DbSet<TugboatOwner> MMSITugboatOwners { get; set; }
        public DbSet<Port> MMSIPorts { get; set; }
        public DbSet<Principal> MMSIPrincipals { get; set; }
        public DbSet<Terminal> MMSITerminals { get; set; }
        public DbSet<Tugboat> MMSITugboats { get; set; }
        public DbSet<TugMaster> MMSITugMasters { get; set; }
        public DbSet<UserAccess> MMSIUserAccesses { get; set; }
        public DbSet<Vessel> MMSIVessels { get; set; }

        #endregion --Master File Entities

        #endregion --Integrated

        public DbSet<Company> Companies { get; set; }
        public DbSet<ChartOfAccount> ChartOfAccounts { get; set; }
        public DbSet<Product> Products { get; set; }

        public DbSet<BankAccount> BankAccounts { get; set; }
        public DbSet<ServiceMaster> Services { get; set; }

        public DbSet<CashReceiptBook> CashReceiptBooks { get; set; }
        public DbSet<DisbursementBook> DisbursementBooks { get; set; }
        public DbSet<GeneralLedgerBook> GeneralLedgerBooks { get; set; }
        public DbSet<JournalBook> JournalBooks { get; set; }
        public DbSet<PurchaseBook> PurchaseBooks { get; set; }
        public DbSet<SalesBook> SalesBooks { get; set; }
        public DbSet<Inventory> Inventories { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            #region-- Master File

            // Company
            builder.Entity<Company>(c =>
            {
                c.HasIndex(c => c.CompanyCode).IsUnique();
                c.HasIndex(c => c.CompanyName).IsUnique();
            });

            // Product
            builder.Entity<Product>(p =>
            {
                p.HasIndex(p => p.ProductCode).IsUnique();
                p.HasIndex(p => p.ProductName).IsUnique();
            });

            #endregion

            #region--Chart Of Account
            builder.Entity<ChartOfAccount>(coa =>
            {
                coa.HasIndex(coa => coa.AccountNumber).IsUnique();
                coa.HasIndex(coa => coa.AccountName);
            });
            #endregion

            #region--Integrated

            builder.Entity<CustomerOrderSlip>(cos =>
            {
                cos.HasIndex(cos => new
                {
                    cos.CustomerOrderSlipNo,
                    cos.Company
                })
                .IsUnique();

                cos.HasIndex(cos => cos.Date);

                cos.HasOne(cos => cos.Customer)
                    .WithMany()
                    .HasForeignKey(cos => cos.CustomerId)
                    .OnDelete(DeleteBehavior.Restrict);

                cos.HasOne(cos => cos.Commissionee)
                    .WithMany()
                    .HasForeignKey(cos => cos.CommissioneeId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<DeliveryReceipt>(dr =>
            {
                dr.HasIndex(dr => new
                {
                    dr.DeliveryReceiptNo,
                    dr.Company
                })
                .IsUnique();

                dr.HasIndex(dr => dr.Date);

                dr.HasOne(dr => dr.CustomerOrderSlip)
                    .WithMany(cos => cos.DeliveryReceipts)
                    .HasForeignKey(dr => dr.CustomerOrderSlipId)
                    .OnDelete(DeleteBehavior.Restrict);

                dr.HasOne(dr => dr.Commissionee)
                    .WithMany()
                    .HasForeignKey(dr => dr.CommissioneeId)
                    .OnDelete(DeleteBehavior.Restrict);

                dr.HasOne(dr => dr.Customer)
                    .WithMany()
                    .HasForeignKey(dr => dr.CustomerId)
                    .OnDelete(DeleteBehavior.Restrict);

                dr.HasOne(dr => dr.Hauler)
                    .WithMany()
                    .HasForeignKey(dr => dr.HaulerId)
                    .OnDelete(DeleteBehavior.Restrict);

                dr.HasOne(dr => dr.AuthorityToLoad)
                    .WithMany()
                    .HasForeignKey(dr => dr.AuthorityToLoadId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<COSAppointedSupplier>(a =>
            {
                a.HasOne(a => a.CustomerOrderSlip)
                    .WithMany(cos => cos.AppointedSuppliers)
                    .HasForeignKey(a => a.CustomerOrderSlipId)
                    .OnDelete(DeleteBehavior.Restrict);

                a.HasOne(a => a.Supplier)
                    .WithMany()
                    .HasForeignKey(a => a.SupplierId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<POActualPrice>(p =>
            {
                p.HasIndex(p => new
                {
                    p.PurchaseOrderId,
                    p.TriggeredDate
                });
            });

            builder.Entity<CustomerBranch>(b =>
            {
                b.HasOne(b => b.Customer)
                    .WithMany(c => c.Branches)
                    .HasForeignKey(b => b.CustomerId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<BookAtlDetail>(b =>
            {
                b.HasOne(b => b.Header)
                    .WithMany(b => b.Details)
                    .HasForeignKey(b => b.AuthorityToLoadId)
                    .OnDelete(DeleteBehavior.Restrict);

                b.HasOne(b => b.CustomerOrderSlip)
                    .WithMany()
                    .HasForeignKey(b => b.CustomerOrderSlipId)
                    .OnDelete(DeleteBehavior.Restrict);

                b.HasOne(b => b.AppointedSupplier)
                    .WithMany()
                    .HasForeignKey(b => b.AppointedId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<AuthorityToLoad>(b =>
            {
                b.HasOne(b => b.Supplier)
                    .WithMany()
                    .HasForeignKey(b => b.SupplierId)
                    .OnDelete(DeleteBehavior.Restrict);

                b.HasIndex(b => new
                {
                    b.AuthorityToLoadNo,
                    b.Company
                })
                .IsUnique();
            });

            builder.Entity<MonthlyNibit>(n =>
            {
                n.HasIndex(n => n.Company);
                n.HasIndex(n => n.Month);
                n.HasIndex(n => n.Year);
            });

            builder.Entity<SalesLockedRecordsQueue>(x =>
            {
                x.HasOne(s => s.DeliveryReceipt)
                    .WithMany()
                    .HasForeignKey(s => s.DeliveryReceiptId)
                    .OnDelete(DeleteBehavior.Restrict);
                x.HasIndex(s => s.LockedDate);
            });

            builder.Entity<PurchaseLockedRecordsQueue>(x =>
            {
                x.HasIndex(s => s.LockedDate);
            });

            #region-- Master File

            // Customer
            builder.Entity<Customer>(c =>
            {
                c.HasIndex(c => c.CustomerCode);
                c.HasIndex(c => c.CustomerName);
            });

            // Supplier
            builder.Entity<Supplier>(s =>
            {
                s.HasIndex(s => s.SupplierCode);
                s.HasIndex(s => s.SupplierName);
            });

            // Employee
            builder.Entity<Employee>(c =>
            {
                c.HasIndex(c => c.EmployeeNumber);
            });

            // PickUpPoint
            builder.Entity<PickUpPoint>(p =>
            {
                p.HasIndex(p => p.Company);

                p.HasOne(p => p.Supplier)
                    .WithMany()
                    .HasForeignKey(p => p.SupplierId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<GLPeriodBalance>(b =>
            {
                b.HasOne(a => a.Account)
                    .WithMany(c => c.Balances)
                    .HasForeignKey(a => a.AccountId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<GLSubAccountBalance>(b =>
            {
                b.HasOne(a => a.Account)
                    .WithMany(c => c.SubAccountBalances)
                    .HasForeignKey(a => a.AccountId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            #endregion

            #region-- Books --

            builder.Entity<GeneralLedgerBook>(gl =>
            {
                gl.HasOne(gl => gl.Account)
                    .WithMany()
                    .HasForeignKey(gl => gl.AccountId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            #endregion

            #region--AppSettings

            builder.Entity<AppSetting>(a =>
            {
                a.HasIndex(a => a.SettingKey).IsUnique();
            });

            #endregion

            #region --MMSI

            builder.Entity<Billing>(b =>
            {
                b.HasIndex(x => new { x.MMSIBillingNumber, x.Company }).IsUnique();
                b.HasIndex(x => x.Date);
            });

            builder.Entity<Collection>(c =>
            {
                c.HasIndex(x => new { x.MMSICollectionNumber, x.Company }).IsUnique();
                c.HasIndex(x => x.Date);

                c.HasMany(x => x.PaidBills)
                    .WithOne(x => x.Collection)
                    .HasForeignKey(x => x.CollectionId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            #endregion

            #endregion --MMSI
        }
    }
}
