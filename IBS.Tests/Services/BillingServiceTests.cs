using IBS.DataAccess.Repository.IRepository;
using IBS.DataAccess.Repository.Msap.IRepository;
using IBS.DataAccess.Repository.MasterFile.IRepository;
using IBS.Models.MSAP;
using IBS.Services;
using IBS.Utility.Constants;
using IBS.Utility.Helpers;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using FluentAssertions;
using System.Linq.Expressions;
using IBS.DataAccess.Data;
using IBS.Models.MasterFile;
using IBS.Models.MSAP.MasterFile;
using Microsoft.EntityFrameworkCore;
using IBS.Models.Books;
using Microsoft.EntityFrameworkCore.Diagnostics;
using IBS.DTOs;

namespace IBS.Tests.Services
{
    public class BillingServiceTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly BillingService _service;
        private readonly Mock<IBillingRepository> _mockBillingRepo;
        private readonly Mock<IJobOrderRepository> _mockJobOrderRepo;
        private readonly Mock<ICustomerRepository> _mockCustomerRepo;
        private readonly Mock<IDispatchTicketRepository> _mockTicketRepo;
        private readonly Mock<IVesselRepository> _mockVesselRepo;
        private readonly Mock<INotificationService> _mockNotification;
        private readonly Mock<IJobOrderService> _mockJobOrderService;

        public BillingServiceTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            var mockLogger = new Mock<ILogger<BillingService>>();
            _mockNotification = new Mock<INotificationService>();
            _mockBillingRepo = new Mock<IBillingRepository>();
            _mockJobOrderRepo = new Mock<IJobOrderRepository>();
            _mockCustomerRepo = new Mock<ICustomerRepository>();
            _mockTicketRepo = new Mock<IDispatchTicketRepository>();
            _mockVesselRepo = new Mock<IVesselRepository>();
            _mockJobOrderService = new Mock<IJobOrderService>();

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "BillingTestDb")
                .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;
            var dbContext = new ApplicationDbContext(options);

            _mockUnitOfWork.Setup(u => u.Billing).Returns(_mockBillingRepo.Object);
            _mockUnitOfWork.Setup(u => u.JobOrder).Returns(_mockJobOrderRepo.Object);
            _mockUnitOfWork.Setup(u => u.Customer).Returns(_mockCustomerRepo.Object);
            _mockUnitOfWork.Setup(u => u.DispatchTicket).Returns(_mockTicketRepo.Object);
            _mockUnitOfWork.Setup(u => u.Vessel).Returns(_mockVesselRepo.Object);
            _mockUnitOfWork.Setup(u => u.AuditTrail).Returns(new Mock<IAuditTrailRepository>().Object);
            _mockUnitOfWork.Setup(u => u.Principal).Returns(new Mock<IPrincipalRepository>().Object);
            _mockUnitOfWork.Setup(u => u.SaveAsync(It.IsAny<CancellationToken>()))
                .Returns((CancellationToken ct) => dbContext.SaveChangesAsync(ct));

            _mockUnitOfWork.Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<Task>>(), It.IsAny<CancellationToken>()))
                .Callback<Func<Task>, CancellationToken>(async (action, _) => await action())
                .Returns(Task.CompletedTask);

            _service = new BillingService(_mockUnitOfWork.Object, _mockJobOrderService.Object, mockLogger.Object, _mockNotification.Object);
        }

        [Fact]
        public async Task CreateBillingAsync_InheritsDataFromJobOrder()
        {
            // Arrange
            var billing = new Billing
            {
                JobOrderId = 1,
                Company = "MMSI",
                Date = new DateOnly(2026, 5, 22),
                CustomerId = 10,
                VesselId = 20,
                MsapBillingNumber = "BL-001",
                ToBillDispatchTickets = new List<string> { "500" }
            };

            var jobOrder = new JobOrder
            {
                JobOrderId = 1,
                CustomerId = 10,
                VesselId = 20,
                PortId = 30,
                TerminalId = 40,
                VoyageNumber = "VOY001"
            };

            var customer = new Customer { CustomerId = 10, CustomerName = "Test Customer", VatType = SD.VatType_Vatable };

            var ticket = new DispatchTicket
            {
                DispatchTicketId = 500,
                JobOrderId = 1,
                DispatchNumber = "DT-001",
                TotalNetRevenue = 1000m
            };

            var vessel = new Vessel { VesselId = 20, VesselName = "Tug Titan" };

            _mockJobOrderRepo.Setup(u => u.GetJobOrderWithDetailsAsync(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(jobOrder);

            _mockCustomerRepo.Setup(u => u.GetAsync(It.IsAny<Expression<Func<Customer, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(customer);

            _mockTicketRepo.Setup(u => u.GetAsync(It.IsAny<Expression<Func<DispatchTicket, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(ticket);

            _mockVesselRepo.Setup(u => u.GetAsync(It.IsAny<Expression<Func<Vessel, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(vessel);

            _mockBillingRepo.Setup(u => u.ComputeDueDateAsync(It.IsAny<string>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new DateOnly(2026, 6, 22));

            // Act
            var result = await _service.CreateBillingAsync(billing, "user", "MMSI", CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            billing.Status.Should().Be(SD.BillingStatus.ForPosting);
            billing.CustomerId.Should().Be(10);
            billing.VesselId.Should().Be(20);
            billing.VoyageNumber.Should().Be("VOY001");
            billing.Amount.Should().Be(1000m);

            _mockBillingRepo.Verify(u => u.AddAsync(billing, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task PostBillingAsync_CreatesSalesBookEntry()
        {
            // Arrange
            var billing = new Billing
            {
                MsapBillingId = 1,
                MsapBillingNumber = "BL-001",
                Status = SD.BillingStatus.ForPosting,
                CustomerId = 10,
                VesselId = 20,
                Amount = 1000m,
                Date = new DateOnly(2026, 5, 22),
                IsVatable = true,
                Company = "MMSI"
            };

            var customer = new Customer { CustomerId = 10, CustomerName = "Test Customer" };
            var vessel = new Vessel { VesselId = 20, VesselName = "Tug Titan" };

            _mockBillingRepo.Setup(u => u.GetAsync(It.IsAny<Expression<Func<Billing, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(billing);
            _mockCustomerRepo.Setup(u => u.GetAsync(It.IsAny<Expression<Func<Customer, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(customer);
            _mockVesselRepo.Setup(u => u.GetAsync(It.IsAny<Expression<Func<Vessel, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(vessel);

            _mockBillingRepo.Setup(u => u.GetListOfAccountTitleDto(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<AccountTitleDto>
                {
                    new() { AccountNumber = SD.MsapAccounts.ArTrade, AccountId = 1, AccountName = "AR Trade" },
                    new() { AccountNumber = SD.MsapAccounts.MaritimeServiceRevenue, AccountId = 2, AccountName = "Service Revenue" },
                    new() { AccountNumber = SD.MsapAccounts.OutputVat, AccountId = 3, AccountName = "Output VAT" }
                });

            _mockBillingRepo.Setup(u => u.ComputeNetOfVat(It.IsAny<decimal>())).Returns(892.86m);
            _mockBillingRepo.Setup(u => u.ComputeVatAmount(It.IsAny<decimal>())).Returns(107.14m);
            _mockBillingRepo.Setup(u => u.IsJournalEntriesBalanced(It.IsAny<List<GeneralLedgerBook>>())).Returns(true);

            // Act
            var result = await _service.PostBillingAsync(1, "user", CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue(result.Message);
            billing.Status.Should().Be(SD.BillingStatus.ForCollection);
        }

        [Fact]
        public async Task PostBillingAsync_AutomaticallyClosesJobOrder()
        {
            // Arrange
            var billing = new Billing
            {
                MsapBillingId = 1,
                MsapBillingNumber = "BL-001",
                Status = SD.BillingStatus.ForPosting,
                CustomerId = 10,
                VesselId = 20,
                Amount = 1000m,
                Date = new DateOnly(2026, 5, 22),
                IsVatable = true,
                Company = "MMSI",
                JobOrderId = 100
            };

            var customer = new Customer { CustomerId = 10, CustomerName = "Test Customer" };
            var vessel = new Vessel { VesselId = 20, VesselName = "Tug Titan" };

            _mockBillingRepo.Setup(u => u.GetAsync(It.IsAny<Expression<Func<Billing, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(billing);
            _mockCustomerRepo.Setup(u => u.GetAsync(It.IsAny<Expression<Func<Customer, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(customer);
            _mockVesselRepo.Setup(u => u.GetAsync(It.IsAny<Expression<Func<Vessel, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(vessel);

            _mockBillingRepo.Setup(u => u.GetListOfAccountTitleDto(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<AccountTitleDto>
                {
                    new() { AccountNumber = "101020100", AccountId = 1, AccountName = "AR Trade" },
                    new() { AccountNumber = "401020100", AccountId = 2, AccountName = "Service Revenue" },
                    new() { AccountNumber = SD.MsapAccounts.OutputVat, AccountId = 3, AccountName = "Output VAT" }
                });

            _mockBillingRepo.Setup(u => u.ComputeNetOfVat(It.IsAny<decimal>())).Returns(892.86m);
            _mockBillingRepo.Setup(u => u.ComputeVatAmount(It.IsAny<decimal>())).Returns(107.14m);
            _mockBillingRepo.Setup(u => u.IsJournalEntriesBalanced(It.IsAny<List<GeneralLedgerBook>>())).Returns(true);

            _mockJobOrderService.Setup(s => s.TryAutoCloseAsync(100, It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _service.PostBillingAsync(1, "user", CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            _mockJobOrderService.Verify(s => s.TryAutoCloseAsync(100, "user", It.IsAny<CancellationToken>()), Times.Once);
        }
        [Fact]
        public async Task CreateBillingAsync_CalculatesCorrectAmount_VatInclusive_Jan2026Data()
        {
            // Arrange: Using Billing #B000436 from Jan 2026 (Amount: 840,622.50)
            var billing = new Billing
            {
                JobOrderId = 1,
                Company = "MMSI",
                Date = new DateOnly(2026, 1, 3),
                CustomerId = 10,
                IsVatInclusive = true,
                MsapBillingNumber = "B000436",
                ToBillDispatchTickets = new List<string> { "500" }
            };

            var jobOrder = new JobOrder { JobOrderId = 1, CustomerId = 10 };
            var customer = new Customer { CustomerId = 10, VatType = SD.VatType_Vatable };

            // Assume the tickets total 840,622.50
            var ticket = new DispatchTicket { DispatchTicketId = 500, TotalNetRevenue = 840622.50m, JobOrderId = 1 };

            _mockJobOrderRepo.Setup(u => u.GetJobOrderWithDetailsAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(jobOrder);
            _mockCustomerRepo.Setup(u => u.GetAsync(It.IsAny<Expression<Func<Customer, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(customer);
            _mockTicketRepo.Setup(u => u.GetAsync(It.IsAny<Expression<Func<DispatchTicket, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(ticket);
            _mockBillingRepo.Setup(u => u.ComputeDueDateAsync(It.IsAny<string>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>())).ReturnsAsync(new DateOnly(2026, 2, 3));

            // Act
            await _service.CreateBillingAsync(billing, "user", "MMSI", CancellationToken.None);

            // Assert: Total should remain 840,622.50 because it's inclusive
            billing.Amount.Should().Be(840622.50m);
        }

        [Fact]
        public async Task CreateBillingAsync_CalculatesCorrectAmount_VatExclusive_LegacyData()
        {
            // Arrange: Using CR #352 (Jan 2025) data.
            // Gross: 239,600.00, Net: 213,928.57
            var billing = new Billing
            {
                JobOrderId = 1,
                Company = "MMSI",
                Date = new DateOnly(2025, 1, 7),
                CustomerId = 10,
                IsVatInclusive = false, // TEST EXCLUSIVE
                MsapBillingNumber = "BL-Legacy-Exc",
                ToBillDispatchTickets = new List<string> { "500" }
            };

            var jobOrder = new JobOrder { JobOrderId = 1, CustomerId = 10 };
            var customer = new Customer { CustomerId = 10, VatType = SD.VatType_Vatable };

            // Ticket has the Net amount
            var ticket = new DispatchTicket { DispatchTicketId = 500, TotalNetRevenue = 213928.57m, JobOrderId = 1 };

            _mockJobOrderRepo.Setup(u => u.GetJobOrderWithDetailsAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(jobOrder);
            _mockCustomerRepo.Setup(u => u.GetAsync(It.IsAny<Expression<Func<Customer, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(customer);
            _mockTicketRepo.Setup(u => u.GetAsync(It.IsAny<Expression<Func<DispatchTicket, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(ticket);
            _mockBillingRepo.Setup(u => u.ComputeDueDateAsync(It.IsAny<string>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>())).ReturnsAsync(new DateOnly(2025, 2, 7));

            // Act
            await _service.CreateBillingAsync(billing, "user", "MMSI", CancellationToken.None);

            // Assert: Total should be Net * 1.12
            // 213,928.57 * 1.12 = 239,599.9984
            Math.Round(billing.Amount, 2).Should().Be(239599.9984m);
        }

        [Fact]
        public async Task PostBillingAsync_PopulatesSalesBook_WithWht_LegacyData()
        {
            // Arrange: Using CR #352 data (Net: 213,928.57, WHT 2%: 4,278.57)
            var billing = new Billing
            {
                MsapBillingId = 1,
                MsapBillingNumber = "BL-Legacy",
                Status = SD.BillingStatus.ForPosting,
                CustomerId = 10,
                Amount = 239600.00m,
                IsVatable = true,
                IsVatInclusive = true,
                PrintWht = true
            };

            var customer = new Customer { CustomerId = 10, CustomerName = "Legacy Customer" };

            _mockBillingRepo.Setup(u => u.GetAsync(It.IsAny<Expression<Func<Billing, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(billing);
            _mockCustomerRepo.Setup(u => u.GetAsync(It.IsAny<Expression<Func<Customer, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(customer);
            _mockBillingRepo.Setup(u => u.GetListOfAccountTitleDto(It.IsAny<CancellationToken>())).ReturnsAsync(new List<AccountTitleDto>
            {
                new() { AccountNumber = SD.MsapAccounts.ArTrade, AccountId = 1 },
                new() { AccountNumber = SD.MsapAccounts.MaritimeServiceRevenue, AccountId = 2 },
                new() { AccountNumber = SD.MsapAccounts.OutputVat, AccountId = 3 }
            });

            _mockBillingRepo.Setup(u => u.ComputeNetOfVat(239600.00m)).Returns(213928.57m);
            _mockBillingRepo.Setup(u => u.ComputeVatAmount(213928.57m)).Returns(25671.43m);
            _mockBillingRepo.Setup(u => u.IsJournalEntriesBalanced(It.IsAny<List<GeneralLedgerBook>>())).Returns(true);

            // Act
            var result = await _service.PostBillingAsync(1, "user", CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
        }
    }
}



