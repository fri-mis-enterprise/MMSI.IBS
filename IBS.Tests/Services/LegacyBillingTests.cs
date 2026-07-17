using IBS.DataAccess.Repository.IRepository;
using IBS.DataAccess.Repository.Msap.IRepository;
using IBS.DataAccess.Repository.MasterFile.IRepository;
using IBS.Models;
using IBS.Models.MSAP;
using IBS.Models.MasterFile;
using IBS.Models.MSAP.MasterFile;
using IBS.Models.Books;
using IBS.DTOs;
using IBS.Services;
using IBS.Utility.Constants;
using Moq;
using Xunit;
using FluentAssertions;
using System.Linq.Expressions;
using Microsoft.Extensions.Logging;

namespace IBS.Tests.Services
{
    public class LegacyBillingTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly BillingService _service;
        private readonly Mock<IBillingRepository> _mockBillingRepo;
        private readonly Mock<ICustomerRepository> _mockCustomerRepo;
        private readonly Mock<IDispatchTicketRepository> _mockTicketRepo;
        private readonly Mock<IJobOrderRepository> _mockJobOrderRepo;
        private readonly Mock<IVesselRepository> _mockVesselRepo;
        private readonly Mock<IPrincipalRepository> _mockPrincipalRepo;
        private readonly Mock<JobOrderService> _mockJobOrderService;
        private readonly Mock<ILogger<JobOrderService>> _mockJobOrderLogger;
        private readonly Mock<IAuditTrailRepository> _mockAuditTrail;

        public LegacyBillingTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockBillingRepo = new Mock<IBillingRepository>();
            _mockCustomerRepo = new Mock<ICustomerRepository>();
            _mockTicketRepo = new Mock<IDispatchTicketRepository>();
            _mockJobOrderRepo = new Mock<IJobOrderRepository>();
            _mockVesselRepo = new Mock<IVesselRepository>();
            _mockPrincipalRepo = new Mock<IPrincipalRepository>();
            _mockJobOrderLogger = new Mock<ILogger<JobOrderService>>();
            _mockJobOrderService = new Mock<JobOrderService>(_mockUnitOfWork.Object, _mockJobOrderLogger.Object);
            var mockLogger = new Mock<ILogger<BillingService>>();

            _mockUnitOfWork.Setup(u => u.Billing).Returns(_mockBillingRepo.Object);
            _mockUnitOfWork.Setup(u => u.Customer).Returns(_mockCustomerRepo.Object);
            _mockUnitOfWork.Setup(u => u.DispatchTicket).Returns(_mockTicketRepo.Object);
            _mockUnitOfWork.Setup(u => u.JobOrder).Returns(_mockJobOrderRepo.Object);
            _mockUnitOfWork.Setup(u => u.Vessel).Returns(_mockVesselRepo.Object);
            _mockUnitOfWork.Setup(u => u.Principal).Returns(_mockPrincipalRepo.Object);
            _mockAuditTrail = new Mock<IAuditTrailRepository>();
            _mockUnitOfWork.Setup(u => u.AuditTrail).Returns(_mockAuditTrail.Object);

            _mockUnitOfWork.Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<Task>>(), It.IsAny<CancellationToken>()))
                .Callback<Func<Task>, CancellationToken>(async (action, _) => await action())
                .Returns(Task.CompletedTask);

            var mockPostedPeriod = new Mock<IPostedPeriodRepository>();
            mockPostedPeriod.Setup(p => p.IsMonthClosedAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
            _mockUnitOfWork.Setup(u => u.PostedPeriod).Returns(mockPostedPeriod.Object);

            _service = new BillingService(_mockUnitOfWork.Object, _mockJobOrderService.Object, mockLogger.Object);
        }

        [Fact]
        public async Task CreateBillingAsync_Replicate_Legacy_Dec2025_RECID3666()
        {
            // Arrange: Replicating RECID 3666 from database
            // Customer: INSULAR OIL CORPORATION (7) - Vatable
            // Tickets: 70,000 + 70,000 = 140,000
            // Date: 2025-12-02
            // Expected Amount in Legacy: 140,000.00

            var billing = new Billing
            {
                JobOrderId = null, // Legacy data (no JO)
                Company = "MMSI",
                Date = new DateOnly(2025, 12, 2),
                CustomerId = 7,
                MsapBillingNumber = "1224", // Replicating RECID 3666
                IsVatInclusive = false, // Matches DB value; new system applies VAT for Vatable + Exclusive
                ToBillDispatchTickets = new List<string> { "11889", "11890" }
            };

            var customer = new Customer
            {
                CustomerId = 7,
                CustomerName = "INSULAR OIL CORPORATION",
                VatType = SD.VatType_Vatable
            };

            var ticket1 = new DispatchTicket
            {
                DispatchTicketId = 11889,
                TotalNetRevenue = 70000m,
                DispatchNumber = "19968"
            };

            var ticket2 = new DispatchTicket
            {
                DispatchTicketId = 11890,
                TotalNetRevenue = 70000m,
                DispatchNumber = "22293"
            };

            _mockCustomerRepo.Setup(u => u.GetAsync(It.IsAny<Expression<Func<Customer, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(customer);

            _mockTicketRepo.Setup(u => u.GetAsync(It.IsAny<Expression<Func<DispatchTicket, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Expression<Func<DispatchTicket, bool>> expr, CancellationToken _) =>
                {
                    var compiled = expr.Compile();
                    if (compiled(ticket1)) return ticket1;
                    if (compiled(ticket2)) return ticket2;
                    return null;
                });

            _mockBillingRepo.Setup(u => u.ComputeDueDateAsync(It.IsAny<string>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new DateOnly(2025, 12, 2));

            // Act
            var result = await _service.CreateBillingAsync(billing, "testuser", "MMSI", CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue(result.Message);

            // With IsVatInclusive=false and Vatable customer, code applies total * 1.12
            billing.Amount.Should().Be(156800.00m, "Vatable + Exclusive: 140000 * 1.12 = 156800");
            _mockAuditTrail.Verify(r => r.AddAsync(It.IsAny<AuditTrail>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task PostBillingAsync_Replicate_Legacy_Dec2025_RECID3666()
        {
            // Arrange: Using RECID 3666 data (Amount: 140,000, Inclusive=True)
            var billing = new Billing
            {
                MsapBillingId = 3666,
                MsapBillingNumber = "1224",
                Status = SD.BillingStatus.ForPosting,
                CustomerId = 7,
                VesselId = 8,
                Amount = 140000.00m,
                IsVatable = true,
                IsVatInclusive = true,
                PrintWht = true,
                Company = "MMSI"
            };

            var customer = new Customer { CustomerId = 7, CustomerName = "INSULAR OIL CORPORATION", CustomerTin = "247-923-221-000" };
            var vessel = new Vessel { VesselId = 8, VesselName = "Tug Titan" };

            _mockBillingRepo.Setup(u => u.GetAsync(It.IsAny<Expression<Func<Billing, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(billing);
            _mockCustomerRepo.Setup(u => u.GetAsync(It.IsAny<Expression<Func<Customer, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(customer);
            _mockVesselRepo.Setup(u => u.GetAsync(It.IsAny<Expression<Func<Vessel, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(vessel);
            _mockPrincipalRepo.Setup(u => u.GetAsync(It.IsAny<Expression<Func<Principal, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync((Principal)null!);

            _mockBillingRepo.Setup(u => u.GetListOfAccountTitleDto(It.IsAny<CancellationToken>())).ReturnsAsync(new List<AccountTitleDto>
            {
                new() { AccountNumber = SD.MsapAccounts.ArTrade, AccountId = 1, AccountName = "AR Trade" },
                new() { AccountNumber = SD.MsapAccounts.MaritimeServiceRevenue, AccountId = 2, AccountName = "Service Revenue" },
                new() { AccountNumber = SD.MsapAccounts.OutputVat, AccountId = 3, AccountName = "Output VAT" }
            });

            _mockBillingRepo.Setup(u => u.ComputeNetOfVat(140000.00m)).Returns(125000.00m);
            _mockBillingRepo.Setup(u => u.ComputeVatAmount(125000.00m)).Returns(15000.00m);
            _mockBillingRepo.Setup(u => u.IsJournalEntriesBalanced(It.IsAny<List<GeneralLedgerBook>>())).Returns(true);

            // Act
            var result = await _service.PostBillingAsync(3666, "testuser", CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue(result.Message);

            _mockAuditTrail.Verify(r => r.AddAsync(It.IsAny<AuditTrail>(), It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
