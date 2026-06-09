using IBS.DataAccess.Repository.IRepository;
using IBS.DataAccess.Repository.Msap.IRepository;
using IBS.DataAccess.Repository.MasterFile.IRepository;
using IBS.Models.MSAP;
using IBS.Models.MasterFile;
using IBS.Models.MSAP.MasterFile;
using IBS.Services;
using IBS.Utility.Constants;
using Moq;
using Xunit;
using FluentAssertions;
using System.Linq.Expressions;
using Microsoft.Extensions.Logging;
using IBS.DTOs;

namespace IBS.Tests.Services
{
    public class TaxAnalysisTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly BillingService _billingService;
        private readonly CollectionService _collectionService;
        private readonly Mock<IBillingRepository> _mockBillingRepo;
        private readonly Mock<ICustomerRepository> _mockCustomerRepo;
        private readonly Mock<IDispatchTicketRepository> _mockTicketRepo;
        private readonly Mock<IJobOrderRepository> _mockJobOrderRepo;
        private readonly Mock<IVesselRepository> _mockVesselRepo;
        private readonly Mock<IJobOrderService> _mockJobOrderService;

        public TaxAnalysisTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockBillingRepo = new Mock<IBillingRepository>();
            _mockCustomerRepo = new Mock<ICustomerRepository>();
            _mockTicketRepo = new Mock<IDispatchTicketRepository>();
            _mockJobOrderRepo = new Mock<IJobOrderRepository>();
            _mockVesselRepo = new Mock<IVesselRepository>();
            _mockJobOrderService = new Mock<IJobOrderService>();
            var mockBillingLogger = new Mock<ILogger<BillingService>>();
            var mockCollectionLogger = new Mock<ILogger<CollectionService>>();

            _mockUnitOfWork.Setup(u => u.Billing).Returns(_mockBillingRepo.Object);
            _mockUnitOfWork.Setup(u => u.Customer).Returns(_mockCustomerRepo.Object);
            _mockUnitOfWork.Setup(u => u.DispatchTicket).Returns(_mockTicketRepo.Object);
            _mockUnitOfWork.Setup(u => u.JobOrder).Returns(_mockJobOrderRepo.Object);
            _mockUnitOfWork.Setup(u => u.Vessel).Returns(_mockVesselRepo.Object);
            _mockUnitOfWork.Setup(u => u.AuditTrail).Returns(new Mock<IAuditTrailRepository>().Object);

            _mockUnitOfWork.Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<Task>>(), It.IsAny<CancellationToken>()))
                .Callback<Func<Task>, CancellationToken>(async (action, _) => await action())
                .Returns(Task.CompletedTask);

            _billingService = new BillingService(_mockUnitOfWork.Object, _mockJobOrderService.Object, mockBillingLogger.Object);
            _collectionService = new CollectionService(_mockUnitOfWork.Object, mockCollectionLogger.Object);
        }

        [Theory]
        [InlineData(true, 1000, 1000)] // Inclusive: Amount stays 1000
        [InlineData(false, 1000, 1120)] // Exclusive: Amount becomes 1120
        public async Task Billing_VAT_Calculation_Analysis(bool isInclusive, decimal ticketTotal, decimal expectedBillingAmount)
        {
            // Arrange
            var customer = new Customer { CustomerId = 1, VatType = SD.VatType_Vatable };
            var billing = new Billing
            {
                CustomerId = 1,
                IsVatInclusive = isInclusive,
                ToBillDispatchTickets = new List<string> { "101" },
                MsapBillingNumber = "B-TEST"
            };
            var ticket = new DispatchTicket { DispatchTicketId = 101, TotalNetRevenue = ticketTotal };

            _mockCustomerRepo.Setup(c => c.GetAsync(It.IsAny<Expression<Func<Customer, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(customer);
            _mockTicketRepo.Setup(t => t.GetAsync(It.IsAny<Expression<Func<DispatchTicket, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(ticket);
            _mockBillingRepo.Setup(b => b.ComputeDueDateAsync(It.IsAny<string>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>())).ReturnsAsync(DateOnly.FromDateTime(DateTime.Now));

            // Act
            await _billingService.CreateBillingAsync(billing, "test", "MMSI", CancellationToken.None);

            // Assert
            billing.Amount.Should().Be(expectedBillingAmount);
        }

        [Fact]
        public async Task Collection_EWT_Calculation_Analysis_Vatable_Inclusive()
        {
            // Arrange: Billing Amount = 1120 (Inclusive, but let's say it's 1120 total)
            // If Inclusive, Gross = 1120, Net of VAT = 1000, EWT (2%) = 20
            var customer = new Customer { CustomerId = 1, WithHoldingTax = true, VatType = SD.VatType_Vatable };
            var billing = new Billing
            {
                MsapBillingId = 1,
                Amount = 1120m,
                IsVatable = true,
                IsVatInclusive = true,
                BilledTo = SD.BilledTo_Local
            };

            _mockCustomerRepo.Setup(c => c.GetAsync(It.IsAny<Expression<Func<Customer, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(customer);
            _mockUnitOfWork.Setup(u => u.Collection.GetMsapUncollectedBillingsByCustomerList(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Billing> { billing });

            // Act
            var result = await _collectionService.GetUncollectedBillingsForTableAsync(1, null, CancellationToken.None);

            // Assert
            var data = (IEnumerable<object>)result.Data!;
            var row = data.First();
            var ewt = (decimal)row.GetType().GetProperty("ewt")!.GetValue(row)!;
            var net = (decimal)row.GetType().GetProperty("net")!.GetValue(row)!;

            // Calculation: (1120 / 1.12) * 0.02 = 20
            ewt.Should().Be(20m);
            net.Should().Be(1100m);
        }

        [Fact]
        public async Task Collection_EWT_Calculation_Analysis_Vatable_Exclusive()
        {
            // Arrange: Billing Amount = 1120 (Exclusive: Ticket was 1000, VAT 120 added)
            // Gross = 1120, Net of VAT = 1000, EWT (2%) = 20
            var customer = new Customer { CustomerId = 1, WithHoldingTax = true, VatType = SD.VatType_Vatable };
            var billing = new Billing
            {
                MsapBillingId = 1,
                Amount = 1120m,
                IsVatable = true,
                IsVatInclusive = false,
                BilledTo = SD.BilledTo_Local
            };

            _mockCustomerRepo.Setup(c => c.GetAsync(It.IsAny<Expression<Func<Customer, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(customer);
            _mockUnitOfWork.Setup(u => u.Collection.GetMsapUncollectedBillingsByCustomerList(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Billing> { billing });

            // Act
            var result = await _collectionService.GetUncollectedBillingsForTableAsync(1, null, CancellationToken.None);

            // Assert
            var data = (IEnumerable<object>)result.Data!;
            var row = data.First();
            var ewt = (decimal)row.GetType().GetProperty("ewt")!.GetValue(row)!;
            
            // Even if Exclusive, the formula (Amount / 1.12) * 0.02 is applied if IsVatable is true
            ewt.Should().Be(20m);
        }

        [Fact]
        public async Task Collection_EWT_Calculation_Analysis_ZeroRated()
        {
            // Arrange: Zero Rated Customer. Amount = 1000. No VAT.
            // EWT (2%) = 1000 * 0.02 = 20
            var customer = new Customer { CustomerId = 1, WithHoldingTax = true, VatType = SD.VatType_ZeroRated };
            var billing = new Billing
            {
                MsapBillingId = 1,
                Amount = 1000m,
                IsVatable = false,
                BilledTo = SD.BilledTo_Local
            };

            _mockCustomerRepo.Setup(c => c.GetAsync(It.IsAny<Expression<Func<Customer, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(customer);
            _mockUnitOfWork.Setup(u => u.Collection.GetMsapUncollectedBillingsByCustomerList(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Billing> { billing });

            // Act
            var result = await _collectionService.GetUncollectedBillingsForTableAsync(1, null, CancellationToken.None);

            // Assert
            var data = (IEnumerable<object>)result.Data!;
            var row = data.First();
            var ewt = (decimal)row.GetType().GetProperty("ewt")!.GetValue(row)!;
            
            // For Non-Vatable: Amount * 0.02
            ewt.Should().Be(20m);
        }
    }
}
