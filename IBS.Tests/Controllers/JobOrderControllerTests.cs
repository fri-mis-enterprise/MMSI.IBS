using System.Security.Claims;
using IBS.Models.MSAP;
using IBS.Models.MSAP.ViewModels;
using IBS.Services;
using IBS.Utility.Helpers;
using IBSWeb.Areas.User.Controllers;
using IBSWeb.Hubs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using FluentAssertions;

namespace IBS.Tests.Controllers
{
    public class JobOrderControllerTests
    {
        private readonly Mock<IJobOrderService> _mockJobOrderService;
        private readonly Mock<IDispatchTicketService> _mockDispatchTicketService;
        private readonly Mock<ITerminalService> _mockTerminalService;
        private readonly Mock<ILogger<JobOrderController>> _mockLogger;
        private readonly Mock<IHubContext<TugboatHub>> _mockTugboatHubContext;
        private readonly Mock<IHubContext<PlanningHub>> _mockPlanningHubContext;
        private readonly JobOrderController _controller;
        private readonly Mock<ITempDataDictionary> _mockTempData;

        public JobOrderControllerTests()
        {
            _mockJobOrderService = new Mock<IJobOrderService>();
            _mockDispatchTicketService = new Mock<IDispatchTicketService>();
            _mockTerminalService = new Mock<ITerminalService>();
            _mockLogger = new Mock<ILogger<JobOrderController>>();
            _mockTugboatHubContext = new Mock<IHubContext<TugboatHub>>();
            _mockPlanningHubContext = new Mock<IHubContext<PlanningHub>>();
            _mockTempData = new Mock<ITempDataDictionary>();

            // Mock SignalR Clients for TugboatHub
            var mockTugboatClients = new Mock<IHubClients>();
            var mockTugboatClientProxy = new Mock<IClientProxy>();
            _mockTugboatHubContext.Setup(h => h.Clients).Returns(mockTugboatClients.Object);
            mockTugboatClients.Setup(c => c.All).Returns(mockTugboatClientProxy.Object);

            // Mock SignalR Clients for PlanningHub
            var mockPlanningClients = new Mock<IHubClients>();
            var mockPlanningClientProxy = new Mock<IClientProxy>();
            _mockPlanningHubContext.Setup(h => h.Clients).Returns(mockPlanningClients.Object);
            mockPlanningClients.Setup(c => c.All).Returns(mockPlanningClientProxy.Object);

            _controller = new JobOrderController(
                _mockJobOrderService.Object,
                _mockDispatchTicketService.Object,
                _mockTerminalService.Object,
                _mockLogger.Object,
                _mockTugboatHubContext.Object,
                _mockPlanningHubContext.Object
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
