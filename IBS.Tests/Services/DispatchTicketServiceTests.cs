using IBS.DataAccess.Repository.IRepository;
using IBS.DataAccess.Repository.Msap.IRepository;
using IBS.Models.MSAP;
using IBS.Models.MSAP.ViewModels;
using IBS.Services;
using IBS.Utility.Constants;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using FluentAssertions;
using IBS.Models;
using System.Linq.Expressions;

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
        private readonly Mock<IAuditTrailRepository> _mockAuditTrail;

        public DispatchTicketServiceTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockLogger = new Mock<ILogger<DispatchTicketService>>();
            _mockCloudStorage = new Mock<ICloudStorageService>();
            _mockTicketRepo = new Mock<IDispatchTicketRepository>();
            _mockJobOrderRepo = new Mock<IJobOrderRepository>();

            _mockUnitOfWork.Setup(u => u.DispatchTicket).Returns(_mockTicketRepo.Object);
            _mockUnitOfWork.Setup(u => u.JobOrder).Returns(_mockJobOrderRepo.Object);
            _mockAuditTrail = new Mock<IAuditTrailRepository>();
            _mockUnitOfWork.Setup(u => u.AuditTrail).Returns(_mockAuditTrail.Object);
            _mockUnitOfWork.Setup(u => u.Vessel).Returns(new Mock<IVesselRepository>().Object);

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
            _mockAuditTrail.Verify(r => r.AddAsync(It.IsAny<AuditTrail>(), It.IsAny<CancellationToken>()), Times.Once);
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
        [Fact]
        public async Task UpdateDispatchTicketAsync_PreservesTariffOnNonCriticalChange()
        {
            // Arrange
            var viewModel = new ServiceRequestViewModel
            {
                DispatchTicketId = 1,
                Remarks = "New Remark",
                DispatchNumber = "DT-001" // Same as existing
            };

            var existingTicket = new DispatchTicket
            {
                DispatchTicketId = 1,
                Status = SD.DispatchTicketStatus.ForBilling,
                DispatchNumber = "DT-001",
                DispatchRate = 1000m,
                JobOrderId = 1
            };

            _mockTicketRepo.Setup(u => u.GetAsync(
                It.Is<Expression<Func<DispatchTicket, bool>>>(e => e.Compile()(new DispatchTicket { DispatchTicketId = 1 })),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingTicket);
            _mockUnitOfWork.Setup(u => u.DispatchTicket.IsJobOrderEditableAsync(It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Act
            var result = await _service.UpdateDispatchTicketAsync(viewModel, null, null, "user", CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            existingTicket.Status.Should().Be(SD.DispatchTicketStatus.ForBilling);
            existingTicket.DispatchRate.Should().Be(1000m);
            existingTicket.Remarks.Should().Be("New Remark");
            _mockAuditTrail.Verify(r => r.AddAsync(It.IsAny<AuditTrail>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task UpdateDispatchTicketAsync_ResetsTariffOnCriticalChange()
        {
            // Arrange
            var viewModel = new ServiceRequestViewModel
            {
                DispatchTicketId = 1,
                ServiceId = 2, // Critical change
                DispatchNumber = "DT-001"
            };

            var existingTicket = new DispatchTicket
            {
                DispatchTicketId = 1,
                ServiceId = 1,
                Status = SD.DispatchTicketStatus.ForBilling,
                DispatchNumber = "DT-001",
                DispatchRate = 1000m,
                JobOrderId = 1
            };

            _mockTicketRepo.Setup(u => u.GetAsync(
                It.Is<Expression<Func<DispatchTicket, bool>>>(e => e.Compile()(new DispatchTicket { DispatchTicketId = 1 })),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingTicket);
            _mockUnitOfWork.Setup(u => u.DispatchTicket.IsJobOrderEditableAsync(It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Act
            var result = await _service.UpdateDispatchTicketAsync(viewModel, null, null, "user", CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            existingTicket.Status.Should().Be(SD.DispatchTicketStatus.ForTariff);
            existingTicket.DispatchRate.Should().Be(0);
            _mockAuditTrail.Verify(r => r.AddAsync(It.IsAny<AuditTrail>(), It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}


