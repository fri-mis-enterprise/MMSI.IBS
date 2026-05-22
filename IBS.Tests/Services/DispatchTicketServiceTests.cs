using IBS.DataAccess.Repository.IRepository;
using IBS.DataAccess.Repository.MMSI.IRepository;
using IBS.Models.MMSI;
using IBS.Models.MMSI.ViewModels;
using IBS.Services;
using IBS.Utility.Constants;
using IBS.Utility.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using FluentAssertions;

namespace IBS.Tests.Services
{
    public class DispatchTicketServiceTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<ILogger<DispatchTicketService>> _mockLogger;
        private readonly Mock<ICloudStorageService> _mockCloudStorage;
        private readonly DispatchTicketService _service;
        private readonly Mock<IDispatchTicketRepository> _mockTicketRepo;
        private readonly Mock<IJobOrderRepository> _mockJobOrderRepo;

        public DispatchTicketServiceTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockLogger = new Mock<ILogger<DispatchTicketService>>();
            _mockCloudStorage = new Mock<ICloudStorageService>();
            _mockTicketRepo = new Mock<IDispatchTicketRepository>();
            _mockJobOrderRepo = new Mock<IJobOrderRepository>();

            _mockUnitOfWork.Setup(u => u.DispatchTicket).Returns(_mockTicketRepo.Object);
            _mockUnitOfWork.Setup(u => u.JobOrder).Returns(_mockJobOrderRepo.Object);
            _mockUnitOfWork.Setup(u => u.AuditTrail).Returns(new Mock<IAuditTrailRepository>().Object);

            _service = new DispatchTicketService(
                _mockUnitOfWork.Object,
                _mockCloudStorage.Object,
                _mockLogger.Object);
        }

        [Fact]
        public async Task CreateDispatchTicketAsync_CalculatesHoursAndSetsStatus()
        {
            // Arrange
            var viewModel = new ServiceRequestViewModel
            {
                JobOrderId = 1,
                DateLeft = new DateOnly(2026, 5, 22),
                TimeLeft = new TimeOnly(8, 0),
                DateArrived = new DateOnly(2026, 5, 22),
                TimeArrived = new TimeOnly(10, 30), // 2.5 hours
                DispatchNumber = "DT-001"
            };

            _mockTicketRepo.Setup(u => u.IsJobOrderEditableAsync(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var jobOrder = new JobOrder { JobOrderId = 1, Status = SD.JobOrderStatus.Open };
            _mockJobOrderRepo.Setup(u => u.GetJobOrderWithDetailsAsync(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(jobOrder);

            // Act
            var result = await _service.CreateDispatchTicketAsync(viewModel, null, null, "user", CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            _mockTicketRepo.Verify(u => u.AddAsync(It.Is<DispatchTicket>(dt => 
                dt.TotalHours == 2.5m && 
                dt.Status == SD.DispatchTicketStatus.ForTariff), 
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task CreateDispatchTicketAsync_Fails_IfArrivalBeforeDeparture()
        {
            // Arrange
            var viewModel = new ServiceRequestViewModel
            {
                JobOrderId = 1,
                DateLeft = new DateOnly(2026, 5, 22),
                TimeLeft = new TimeOnly(10, 0),
                DateArrived = new DateOnly(2026, 5, 22),
                TimeArrived = new TimeOnly(8, 0),
                DispatchNumber = "DT-001"
            };

            _mockTicketRepo.Setup(u => u.IsJobOrderEditableAsync(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var jobOrder = new JobOrder { JobOrderId = 1, Status = SD.JobOrderStatus.Open };
            _mockJobOrderRepo.Setup(u => u.GetJobOrderWithDetailsAsync(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(jobOrder);

            // Act
            var result = await _service.CreateDispatchTicketAsync(viewModel, null, null, "user", CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Contain("must be strictly after");
        }
    }
}
