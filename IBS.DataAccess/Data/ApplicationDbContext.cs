using IBS.Models;
using IBS.Models.Books;
using IBS.Models.MasterFile;
using IBS.Models.MSAP;
using IBS.Models.MSAP.MasterFile;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace IBS.DataAccess.Data
{
    public class ApplicationDbContext(DbContextOptions options): IdentityDbContext<ApplicationUser>(options)
    {
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

        public DbSet<AuditTrail> AuditTrails { get; set; }

        #region--Master File

        public DbSet<Customer> Customers { get; set; }

        public DbSet<Supplier> Suppliers { get; set; }

        public DbSet<Employee> Employees { get; set; }

        public DbSet<Terms> Terms { get; set; }

        #endregion

        #region --MSAP
        public DbSet<Billing> MsapBillings { get; set; }
        public DbSet<Collection> MsapCollections { get; set; }
        public DbSet<DispatchTicket> MsapDispatchTickets { get; set; }
        public DbSet<JobOrder> MsapJobOrders { get; set; }
        public DbSet<TariffRate> MsapTariffRates { get; set; }
        public DbSet<BillDispatch> MsapBillDispatches { get; set; }
        public DbSet<BillAdjust> MsapBillAdjustments { get; set; }
        public DbSet<CollectionBill> MsapCollectionBills { get; set; }

        #endregion

        #region --Master File Entity

        public DbSet<Service> MsapServices { get; set; }
        public DbSet<TugboatOwner> MsapTugboatOwners { get; set; }
        public DbSet<Port> MsapPorts { get; set; }
        public DbSet<Principal> MsapPrincipals { get; set; }
        public DbSet<Terminal> MsapTerminals { get; set; }
        public DbSet<Tugboat> MsapTugboats { get; set; }
        public DbSet<TugMaster> MsapTugMasters { get; set; }
        public DbSet<UserAccess> MsapUserAccesses { get; set; }
        public DbSet<Vessel> MsapVessels { get; set; }
        public DbSet<Rate> MsapRates { get; set; }
        public DbSet<Module> MsapModules { get; set; }

        #endregion --Master File Entities

        public DbSet<Company> Companies { get; set; }
        public DbSet<ChartOfAccount> ChartOfAccounts { get; set; }

        public DbSet<BankAccount> BankAccounts { get; set; }

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

            #endregion

            #region--Chart Of Account
            builder.Entity<ChartOfAccount>(coa =>
            {
                coa.HasIndex(coa => coa.AccountNumber).IsUnique();
                coa.HasIndex(coa => coa.AccountName);
            });
            #endregion

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

            #region --MSAP

            builder.Entity<Billing>(b =>
            {
                b.HasIndex(x => new { x.MsapBillingNumber, x.Company }).IsUnique();
                b.HasIndex(x => x.Date);
            });

            builder.Entity<Collection>(c =>
            {
                c.HasIndex(x => new { x.MsapCollectionNumber, x.Company }).IsUnique();
                c.HasIndex(x => x.Date);

                c.HasMany(x => x.PaidBills)
                    .WithOne(x => x.Collection)
                    .HasForeignKey(x => x.CollectionId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<JobOrder>(jo =>
            {
                jo.HasOne(j => j.PreferredTugboat)
                    .WithMany()
                    .HasForeignKey(j => j.PreferredTugboatId)
                    .OnDelete(DeleteBehavior.SetNull);

                jo.Property(j => j.PlannedStartTime).HasColumnType("timestamp without time zone");
                jo.Property(j => j.PlannedEndTime).HasColumnType("timestamp without time zone");
            });

            builder.Entity<Tugboat>(t =>
            {
                t.HasOne(x => x.Port)
                    .WithMany()
                    .HasForeignKey(x => x.PortId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            #endregion
        }
    }
}


