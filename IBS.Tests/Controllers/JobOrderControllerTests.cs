using System.Security.Claims;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models.MSAP;
using IBS.Models.MSAP.ViewModels;
using IBS.Services;
using IBS.Utility.Helpers;
using IBSWeb.Areas.User.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using FluentAssertions;

namespace IBS.Tests.Controllers
{
    public class JobOrderControllerTests
    {
        private readonly Mock<JobOrderService> _mockJobOrderService;
        private readonly Mock<DispatchTicketService> _mockDispatchTicketService;
        private readonly Mock<ITerminalService> _mockTerminalService;
        private readonly Mock<ILogger<JobOrderController>> _mockLogger;
        private readonly Mock<ICloudStorageService> _mockCloudStorageService;
        private readonly JobOrderController _controller;
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<ITempDataDictionary> _mockTempData;

        public JobOrderControllerTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockCloudStorageService = new Mock<ICloudStorageService>();
            _mockJobOrderService = new Mock<JobOrderService>(_mockUnitOfWork.Object, new Mock<ILogger<JobOrderService>>().Object, new Mock<INotificationService>().Object);
            _mockDispatchTicketService = new Mock<DispatchTicketService>(_mockUnitOfWork.Object, _mockCloudStorageService.Object, new Mock<ILogger<DispatchTicketService>>().Object, new Mock<INotificationService>().Object);
            _mockTerminalService = new Mock<ITerminalService>();
            _mockLogger = new Mock<ILogger<JobOrderController>>();
            _mockTempData = new Mock<ITempDataDictionary>();

            _controller = new JobOrderController(
                _mockUnitOfWork.Object,
                _mockJobOrderService.Object,
                _mockDispatchTicketService.Object,
                _mockTerminalService.Object,
                _mockCloudStorageService.Object,
                _mockLogger.Object
            );

            // Mock User Identity
            var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, "testuser"),
                new Claim(ClaimTypes.Role, "Admin")
            }, "mock"));

            _controller.ControllerContext = new ControllerContext()
            {
                HttpContext = new DefaultHttpContext() { User = user }
            };

            _controller.TempData = _mockTempData.Object;
        }

        #region Create Tests

        [Fact]
        public async Task Create_Post_ValidModel_RedirectsToDetails_OnSuccess()
        {
            // Arrange
            var viewModel = new JobOrderViewModel { CustomerId = 1, VesselId = 1 };
            _mockJobOrderService.Setup(s => s.CreateJobOrderAsync(It.IsAny<JobOrder>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(ServiceResult<int>.Success(123, "Success"));

            // Act
            var result = await _controller.Create(viewModel, CancellationToken.None);

            // Assert
            var redirectResult = result.Should().BeOfType<RedirectToActionResult>().Subject;
            redirectResult.ActionName.Should().Be("Details");
            redirectResult.RouteValues!["id"].Should().Be(123);
        }

        #endregion

        #region Edit Tests

        [Fact]
        public async Task Edit_Post_ValidModel_RedirectsToDetails_OnSuccess()
        {
            // Arrange
            var viewModel = new JobOrderViewModel { JobOrderId = 1, CustomerId = 1 };
            _mockJobOrderService.Setup(s => s.UpdateJobOrderAsync(It.IsAny<JobOrder>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(ServiceResult.Success("Updated"));

            // Act
            var result = await _controller.Edit(viewModel, CancellationToken.None);

            // Assert
            var redirectResult = result.Should().BeOfType<RedirectToActionResult>().Subject;
            redirectResult.ActionName.Should().Be("Details");
            _mockTempData.VerifySet(t => t["success"] = "Updated");
        }

        #endregion
    }
}
