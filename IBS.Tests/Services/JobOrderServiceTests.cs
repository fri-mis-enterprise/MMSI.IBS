using IBS.DataAccess.Repository.IRepository;
using IBS.DataAccess.Repository.Msap.IRepository;
using IBS.Models.MSAP;
using IBS.Services;
using IBS.Utility.Constants;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using FluentAssertions;
using System.Linq.Expressions;

namespace IBS.Tests.Services
{
    public class JobOrderServiceTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<ILogger<JobOrderService>> _mockLogger;
        private readonly Mock<INotificationService> _mockNotification;
        private readonly JobOrderService _service;
        private readonly Mock<IAuditTrailRepository> _mockAuditTrail;
        private readonly Mock<IJobOrderRepository> _mockJobOrderRepo;

        public JobOrderServiceTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockLogger = new Mock<ILogger<JobOrderService>>();
            _mockNotification = new Mock<INotificationService>();
            _mockAuditTrail = new Mock<IAuditTrailRepository>();
            _mockJobOrderRepo = new Mock<IJobOrderRepository>();

            _mockUnitOfWork.Setup(u => u.AuditTrail).Returns(_mockAuditTrail.Object);
            _mockUnitOfWork.Setup(u => u.JobOrder).Returns(_mockJobOrderRepo.Object);
            _mockUnitOfWork.Setup(u => u.Vessel).Returns(new Mock<IVesselRepository>().Object);

            _service = new JobOrderService(_mockUnitOfWork.Object, _mockLogger.Object, _mockNotification.Object);
        }

        #region Create Tests

        [Fact]
        public async Task CreateJobOrderAsync_Success_Flow()
        {
            // Arrange
            var jobOrder = new JobOrder { CustomerId = 1 };
            _mockJobOrderRepo.Setup(u => u.GenerateJobOrderNumber(It.IsAny<CancellationToken>())).ReturnsAsync("JO-001");

            // Act
            var result = await _service.CreateJobOrderAsync(jobOrder, "user", CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            jobOrder.Status.Should().Be(SD.JobOrderStatus.Open);
            _mockJobOrderRepo.Verify(u => u.AddAsync(jobOrder, It.IsAny<CancellationToken>()), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task CreateJobOrderAsync_Fails_IfEndTimeBeforeStartTime()
        {
            // Arrange
            var jobOrder = new JobOrder 
            { 
                CustomerId = 1,
                PlannedStartTime = new DateTime(2026, 5, 22, 10, 0, 0),
                PlannedEndTime = new DateTime(2026, 5, 22, 9, 0, 0) // Invalid
            };

            // Act
            var result = await _service.CreateJobOrderAsync(jobOrder, "user", CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Contain("must be strictly after");
        }

        #endregion

        #region Update Tests

        [Fact]
        public async Task UpdateJobOrderAsync_Success_Flow()
        {
            // Arrange
            var existingJob = new JobOrder { JobOrderId = 1, Status = SD.JobOrderStatus.Open, JobOrderNumber = "JO-001" };
            var updateModel = new JobOrder { JobOrderId = 1, Remarks = "Updated Remarks" };

            _mockJobOrderRepo.Setup(u => u.GetAsync(It.IsAny<Expression<Func<JobOrder, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingJob);
            _mockUnitOfWork.Setup(u => u.DispatchTicket.GetAllAsync(It.IsAny<Expression<Func<DispatchTicket, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<DispatchTicket>());
            _mockUnitOfWork.Setup(u => u.Billing.GetAllAsync(It.IsAny<Expression<Func<Billing, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Billing>());

            // Act
            var result = await _service.UpdateJobOrderAsync(updateModel, "user", CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            existingJob.Remarks.Should().Be("Updated Remarks");
            _mockUnitOfWork.Verify(u => u.SaveAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task UpdateJobOrderAsync_Fails_IfJobClosed()
        {
            // Arrange
            var existingJob = new JobOrder { JobOrderId = 1, Status = SD.JobOrderStatus.Closed, JobOrderNumber = "JO-001" };
            _mockJobOrderRepo.Setup(u => u.GetAsync(It.IsAny<Expression<Func<JobOrder, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingJob);

            // Act
            var result = await _service.UpdateJobOrderAsync(new JobOrder { JobOrderId = 1 }, "user", CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Contain("closed and cannot be edited");
        }

        [Fact]
        public async Task UpdateJobOrderAsync_CascadesToRelatedRecords()
        {
            // Arrange
            var existingJob = new JobOrder { JobOrderId = 1, Status = SD.JobOrderStatus.Open, JobOrderNumber = "JO-001", CustomerId = 1, VesselId = 1 };
            var updateModel = new JobOrder { JobOrderId = 1, CustomerId = 2, VesselId = 2, VoyageNumber = "V-NEW" };

            var tickets = new List<DispatchTicket> 
            { 
                new DispatchTicket { DispatchTicketId = 10, JobOrderId = 1, Status = SD.DispatchTicketStatus.ForTariff, CustomerId = 1 } 
            };
            var billings = new List<Billing> 
            { 
                new Billing { MsapBillingId = 100, JobOrderId = 1, Status = SD.BillingStatus.ForPosting, CustomerId = 1 } 
            };

            _mockJobOrderRepo.Setup(u => u.GetAsync(It.IsAny<Expression<Func<JobOrder, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingJob);
            _mockUnitOfWork.Setup(u => u.DispatchTicket.GetAllAsync(It.IsAny<Expression<Func<DispatchTicket, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(tickets);
            _mockUnitOfWork.Setup(u => u.Billing.GetAllAsync(It.IsAny<Expression<Func<Billing, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(billings);

            // Act
            var result = await _service.UpdateJobOrderAsync(updateModel, "user", CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            tickets[0].CustomerId.Should().Be(2);
            tickets[0].VoyageNumber.Should().Be("V-NEW");
            billings[0].CustomerId.Should().Be(2);
            billings[0].VoyageNumber.Should().Be("V-NEW");
        }

        #endregion

        #region Close Tests

        [Fact]
        public async Task CloseJobOrderAsync_Success_Flow()
        {
            // Arrange
            var jobOrder = new JobOrder 
            { 
                JobOrderId = 1, 
                Status = SD.JobOrderStatus.Open, 
                DispatchTickets = new List<DispatchTicket> { new DispatchTicket { Status = SD.DispatchTicketStatus.Billed } } 
            };
            _mockJobOrderRepo.Setup(u => u.GetJobOrderWithDetailsAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(jobOrder);

            // Act
            var result = await _service.CloseJobOrderAsync(1, "user", false, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            jobOrder.Status.Should().Be(SD.JobOrderStatus.Closed);
        }

        [Fact]
        public async Task CloseJobOrderAsync_Fails_IfTicketsInNonTerminalState()
        {
            // Arrange
            var jobOrder = new JobOrder 
            { 
                JobOrderId = 1, 
                Status = SD.JobOrderStatus.Open, 
                DispatchTickets = new List<DispatchTicket> { new DispatchTicket { Status = SD.DispatchTicketStatus.ForApproval } } 
            };
            _mockJobOrderRepo.Setup(u => u.GetJobOrderWithDetailsAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(jobOrder);

            // Act
            var result = await _service.CloseJobOrderAsync(1, "user", false, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Contain("non-terminal states");
        }

        #endregion
    }
}


