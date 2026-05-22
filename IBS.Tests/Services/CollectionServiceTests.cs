using IBS.DataAccess.Repository.IRepository;
using IBS.DataAccess.Repository.MMSI.IRepository;
using IBS.DataAccess.Repository.MasterFile.IRepository;
using IBS.Models.MMSI;
using IBS.Models.MMSI.ViewModels;
using IBS.Services;
using IBS.Utility.Constants;
using IBS.Utility.Helpers;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using FluentAssertions;
using IBS.Models.Enums;
using IBS.Models.MasterFile;

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

        public CollectionServiceTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockLogger = new Mock<ILogger<CollectionService>>();
            _mockCollectionRepo = new Mock<ICollectionRepository>();
            _mockBillingRepo = new Mock<IBillingRepository>();
            _mockCustomerRepo = new Mock<ICustomerRepository>();

            _mockUnitOfWork.Setup(u => u.Collection).Returns(_mockCollectionRepo.Object);
            _mockUnitOfWork.Setup(u => u.Billing).Returns(_mockBillingRepo.Object);
            _mockUnitOfWork.Setup(u => u.Customer).Returns(_mockCustomerRepo.Object);
            _mockUnitOfWork.Setup(u => u.AuditTrail).Returns(new Mock<IAuditTrailRepository>().Object);

            _service = new CollectionService(_mockUnitOfWork.Object, null!, _mockLogger.Object);
        }

        [Fact]
        public async Task CreateCollectionAsync_UpdatesBillingStatusToCollected()
        {
            // Arrange
            var viewModel = new CreateCollectionViewModel
            {
                ToCollectBillings = new List<string> { "100" },
                MMSICollectionNumber = "COL-001",
                Date = new DateOnly(2026, 5, 22),
                CustomerId = 1
            };

            var customer = new Customer { CustomerId = 1, CustomerName = "Test Customer" };
            _mockCustomerRepo.Setup(u => u.GetAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<Customer, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(customer);

            var billing = new Billing { MMSIBillingId = 100, Status = SD.BillingStatus.ForCollection };
            
            _mockBillingRepo.Setup(u => u.GetAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<Billing, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(billing);

            _mockUnitOfWork.Setup(u => u.IsPeriodPostedAsync(It.IsAny<Module>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            // Act
            var result = await _service.CreateCollectionAsync(viewModel, "user", CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            billing.Status.Should().Be(SD.BillingStatus.Collected);
            _mockCollectionRepo.Verify(u => u.UpdateBillingPayment(100, It.IsAny<decimal>(), It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
