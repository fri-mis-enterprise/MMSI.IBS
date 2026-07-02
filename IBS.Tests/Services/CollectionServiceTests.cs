using IBS.DataAccess.Repository.IRepository;
using IBS.DataAccess.Repository.Msap.IRepository;
using IBS.DataAccess.Repository.MasterFile.IRepository;
using IBS.Models.MSAP;
using IBS.Models.MSAP.ViewModels;
using IBS.Services;
using IBS.Utility.Constants;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using FluentAssertions;
using IBS.Models.MasterFile;
using IBS.Models;
using IBS.Tests.TestHelpers;

namespace IBS.Tests.Services
{
    public class CollectionServiceTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<ILogger<CollectionService>> _mockLogger;
        private readonly CollectionService _service;
        private readonly Mock<ICollectionRepository> _mockCollectionRepo;
        private readonly Mock<IBillingRepository> _mockBillingRepo;
        private readonly Mock<ICustomerRepository> _mockCustomerRepo;
        private readonly Mock<INotificationService> _mockNotification;
        private readonly Mock<IAuditTrailRepository> _mockAuditTrail;

        public CollectionServiceTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockLogger = new Mock<ILogger<CollectionService>>();
            _mockNotification = new Mock<INotificationService>();
            _mockCollectionRepo = new Mock<ICollectionRepository>();
            _mockBillingRepo = new Mock<IBillingRepository>();
            _mockCustomerRepo = new Mock<ICustomerRepository>();

            _mockUnitOfWork.Setup(u => u.Collection).Returns(_mockCollectionRepo.Object);
            _mockUnitOfWork.Setup(u => u.Billing).Returns(_mockBillingRepo.Object);
            _mockUnitOfWork.Setup(u => u.Customer).Returns(_mockCustomerRepo.Object);
            _mockAuditTrail = new Mock<IAuditTrailRepository>();
            _mockUnitOfWork.Setup(u => u.AuditTrail).Returns(_mockAuditTrail.Object);
            _mockUnitOfWork.Setup(u => u.BankAccount).Returns(new Mock<IBankAccountRepository>().Object);
            _mockUnitOfWork.Setup(u => u.SaveAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            _mockCollectionRepo.Setup(u => u.AddAsync(It.IsAny<Collection>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            _mockCollectionRepo.Setup(u => u.PostAsync(It.IsAny<Collection>(), It.IsAny<List<Offsettings>>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            _mockUnitOfWork.Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<Task>>(), It.IsAny<CancellationToken>()))
                .Returns((Func<Task> action, CancellationToken ct) => action());

            _service = new CollectionService(_mockUnitOfWork.Object, _mockLogger.Object, _mockNotification.Object);
        }

        [Fact]
        public async Task GetUncollectedBillingsForTableAsync_CalculatesEwtCorrectly_ForVatableCustomer()
        {
            // Arrange
            int customerId = 1;
            var customer = new Customer
            {
                CustomerId = customerId,
                WithHoldingTax = true,
                VatType = SD.VatType_Vatable
            };

            var billings = new List<Billing>
            {
                new Billing
                {
                    MsapBillingId = 100,
                    Amount = 1120m,
                    BilledTo = SD.BilledTo_Local,
                    IsVatable = true
                }
            };

            _mockCustomerRepo.Setup(u => u.GetAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Customer, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(customer);

            _mockCollectionRepo.Setup(u => u.GetMsapUncollectedBillingsByCustomerList(customerId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(billings);

            // Act
            var result = await _service.GetUncollectedBillingsForTableAsync(customerId, null, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            var data = (IEnumerable<object>)result.Data!;
            var billing = data.First();

            var ewt = AnonymousTypeHelper.Get<decimal>(billing, "ewt");
            var net = AnonymousTypeHelper.Get<decimal>(billing, "net");

            // For Vatable: (1120 / 1.12) * 0.02 = 20
            ewt.Should().Be(20m);
            net.Should().Be(1100m);
        }

        [Fact]
        public async Task GetUncollectedBillingsForTableAsync_CalculatesEwtCorrectly_ForZeroRatedCustomer()
        {
            // Arrange
            int customerId = 1;
            var customer = new Customer
            {
                CustomerId = customerId,
                WithHoldingTax = true,
                VatType = "Zero-Rated"
            };

            var billings = new List<Billing>
            {
                new Billing
                {
                    MsapBillingId = 100,
                    Amount = 1000m,
                    BilledTo = SD.BilledTo_Local,
                    IsVatable = false
                }
            };

            _mockCustomerRepo.Setup(u => u.GetAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Customer, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(customer);

            _mockCollectionRepo.Setup(u => u.GetMsapUncollectedBillingsByCustomerList(customerId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(billings);

            // Act
            var result = await _service.GetUncollectedBillingsForTableAsync(customerId, null, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            var data = (IEnumerable<object>)result.Data!;
            var billing = data.First();

            var ewt = AnonymousTypeHelper.Get<decimal>(billing, "ewt");
            var net = AnonymousTypeHelper.Get<decimal>(billing, "net");

            // For Zero-Rated: 1000 * 0.02 = 20
            ewt.Should().Be(20m);
            net.Should().Be(980m);
        }

        [Fact]
        public async Task GetUncollectedBillingsForTableAsync_DoesNotCalculateEwt_WhenWithHoldingTaxIsFalse()
        {
            // Arrange
            int customerId = 1;
            var customer = new Customer
            {
                CustomerId = customerId,
                WithHoldingTax = false,
                VatType = SD.VatType_Vatable
            };

            var billings = new List<Billing>
            {
                new Billing
                {
                    MsapBillingId = 100,
                    Amount = 1120m,
                    BilledTo = SD.BilledTo_Local,
                    IsVatable = true
                }
            };

            _mockCustomerRepo.Setup(u => u.GetAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Customer, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(customer);

            _mockCollectionRepo.Setup(u => u.GetMsapUncollectedBillingsByCustomerList(customerId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(billings);

            // Act
            var result = await _service.GetUncollectedBillingsForTableAsync(customerId, null, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            var data = (IEnumerable<object>)result.Data!;
            var billing = data.First();

            var ewt = AnonymousTypeHelper.Get<decimal>(billing, "ewt");
            var net = AnonymousTypeHelper.Get<decimal>(billing, "net");

            ewt.Should().Be(0m);
            net.Should().Be(1120m);
        }

        [Fact]
        public async Task CreateCollectionAsync_UpdatesBillingStatusToCollected_WithPartialPayment()
        {
            // Arrange
            var viewModel = new CreateCollectionViewModel
            {
                BillingPayments = new List<BillingPaymentViewModel>
                {
                    new BillingPaymentViewModel { BillingId = 100, AmountToPay = 500m }
                },
                Amount = 500m,
                MsapCollectionNumber = "COL-001",
                Date = new DateOnly(2026, 5, 22),
                CustomerId = 1
            };

            var customer = new Customer { CustomerId = 1, CustomerName = "Test Customer" };
            _mockCustomerRepo.Setup(u => u.GetAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Customer, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(customer);

            var billing = new Billing { MsapBillingId = 100, Status = SD.BillingStatus.ForCollection, Amount = 1000m, Balance = 1000m };

            _mockBillingRepo.Setup(u => u.GetAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Billing, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(billing);

            // Act
            var result = await _service.CreateCollectionAsync(viewModel, "user", CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            billing.Status.Should().Be(SD.BillingStatus.Collected);
            _mockCollectionRepo.Verify(u => u.UpdateBillingPayment(100, 500m, It.IsAny<CancellationToken>()), Times.Once);
            _mockAuditTrail.Verify(r => r.AddAsync(It.IsAny<AuditTrail>(), It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}


