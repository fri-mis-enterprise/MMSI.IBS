using IBS.DataAccess.Repository.IRepository;
using IBS.DataAccess.Repository.MMSI.IRepository;
using IBS.DataAccess.Repository.MasterFile.IRepository;
using IBS.Models.MMSI;
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
using IBS.Models.MMSI.MasterFile;
using ModuleEnum = IBS.Models.Enums.Module;
using Microsoft.EntityFrameworkCore;
using IBS.Models.Books;

namespace IBS.Tests.Services
{
    public class BillingServiceTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<ILogger<BillingService>> _mockLogger;
        private readonly BillingService _service;
        private readonly Mock<IBillingRepository> _mockBillingRepo;
        private readonly Mock<IJobOrderRepository> _mockJobOrderRepo;
        private readonly Mock<ICustomerRepository> _mockCustomerRepo;
        private readonly Mock<IDispatchTicketRepository> _mockTicketRepo;
        private readonly Mock<IVesselRepository> _mockVesselRepo;
        private readonly ApplicationDbContext _dbContext;

        public BillingServiceTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockLogger = new Mock<ILogger<BillingService>>();
            _mockBillingRepo = new Mock<IBillingRepository>();
            _mockJobOrderRepo = new Mock<IJobOrderRepository>();
            _mockCustomerRepo = new Mock<ICustomerRepository>();
            _mockTicketRepo = new Mock<IDispatchTicketRepository>();
            _mockVesselRepo = new Mock<IVesselRepository>();

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "BillingTestDb")
                .Options;
            _dbContext = new ApplicationDbContext(options);

            _mockUnitOfWork.Setup(u => u.Billing).Returns(_mockBillingRepo.Object);
            _mockUnitOfWork.Setup(u => u.JobOrder).Returns(_mockJobOrderRepo.Object);
            _mockUnitOfWork.Setup(u => u.Customer).Returns(_mockCustomerRepo.Object);
            _mockUnitOfWork.Setup(u => u.DispatchTicket).Returns(_mockTicketRepo.Object);
            _mockUnitOfWork.Setup(u => u.Vessel).Returns(_mockVesselRepo.Object);
            _mockUnitOfWork.Setup(u => u.AuditTrail).Returns(new Mock<IAuditTrailRepository>().Object);

            _service = new BillingService(_mockUnitOfWork.Object, _dbContext, _mockLogger.Object);
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
                MMSIBillingNumber = "BL-001",
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

            _mockUnitOfWork.Setup(u => u.IsPeriodPostedAsync(It.IsAny<ModuleEnum>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            _mockBillingRepo.Setup(u => u.ComputeDueDateAsync(It.IsAny<string>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new DateOnly(2026, 6, 22));

            // Act
            var result = await _service.CreateBillingAsync(billing, "user", "MMSI", CancellationToken.None);

            // Assert
            if (!result.IsSuccess)
            {
                throw new Xunit.Sdk.XunitException($"Test failed with message: {result.Message}");
            }

            result.IsSuccess.Should().BeTrue();
            billing.CustomerId.Should().Be(10);
            billing.VesselId.Should().Be(20);
            billing.VoyageNumber.Should().Be("VOY001");
            billing.Amount.Should().Be(1000m);
            _mockBillingRepo.Verify(u => u.AddAsync(billing, It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
