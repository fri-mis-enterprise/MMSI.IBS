using IBS.DataAccess.Repository.IRepository;
using IBS.DataAccess.Repository.MasterFile.IRepository;
using IBS.DataAccess.Repository.MMSI.IRepository;
using IBS.Models.MMSI;
using IBS.Models;
using IBS.Services;
using IBS.Utility.Constants;
using IBS.Utility.Helpers;
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
        private readonly JobOrderService _service;
        private readonly Mock<IAuditTrailRepository> _mockAuditTrail;
        private readonly Mock<IJobOrderRepository> _mockJobOrderRepo;

        public JobOrderServiceTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockLogger = new Mock<ILogger<JobOrderService>>();
            _mockAuditTrail = new Mock<IAuditTrailRepository>();
            _mockJobOrderRepo = new Mock<IJobOrderRepository>();

            _mockUnitOfWork.Setup(u => u.AuditTrail).Returns(_mockAuditTrail.Object);
            _mockUnitOfWork.Setup(u => u.JobOrder).Returns(_mockJobOrderRepo.Object);

            _service = new JobOrderService(_mockUnitOfWork.Object, _mockLogger.Object);
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

        #endregion

        #region Cancel Tests

        [Fact]
        public async Task CancelJobOrderAsync_Success_Flow()
        {
            // Arrange
            var jobOrder = new JobOrder { JobOrderId = 1, Status = SD.JobOrderStatus.Open, DispatchTickets = new List<DispatchTicket>() };
            _mockJobOrderRepo.Setup(u => u.GetJobOrderWithDetailsAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(jobOrder);

            // Act
            var result = await _service.CancelJobOrderAsync(1, "user", CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            jobOrder.Status.Should().Be(SD.JobOrderStatus.Cancelled);
        }

        [Fact]
        public async Task CancelJobOrderAsync_Fails_IfTicketsBilled()
        {
            // Arrange
            var jobOrder = new JobOrder 
            { 
                JobOrderId = 1, 
                Status = SD.JobOrderStatus.Open, 
                DispatchTickets = new List<DispatchTicket> { new DispatchTicket { Status = "Billed" } } 
            };
            _mockJobOrderRepo.Setup(u => u.GetJobOrderWithDetailsAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(jobOrder);

            // Act
            var result = await _service.CancelJobOrderAsync(1, "user", CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Contain("already in the billing process");
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
                DispatchTickets = new List<DispatchTicket> { new DispatchTicket { Status = "For Billing" } } 
            };
            _mockJobOrderRepo.Setup(u => u.GetJobOrderWithDetailsAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(jobOrder);

            // Act
            var result = await _service.CloseJobOrderAsync(1, "user", false, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            jobOrder.Status.Should().Be(SD.JobOrderStatus.Closed);
        }

        [Fact]
        public async Task CloseJobOrderAsync_Fails_IfTicketsPendingTariff()
        {
            // Arrange
            var jobOrder = new JobOrder 
            { 
                JobOrderId = 1, 
                Status = SD.JobOrderStatus.Open, 
                DispatchTickets = new List<DispatchTicket> { new DispatchTicket { Status = "Pending" } } 
            };
            _mockJobOrderRepo.Setup(u => u.GetJobOrderWithDetailsAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(jobOrder);

            // Act
            var result = await _service.CloseJobOrderAsync(1, "user", false, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Contain("have no tariff set");
        }

        #endregion
    }
}
