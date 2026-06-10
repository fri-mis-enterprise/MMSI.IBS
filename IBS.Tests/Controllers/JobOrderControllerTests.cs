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
using Moq;
using Xunit;
using FluentAssertions;

namespace IBS.Tests.Controllers
{
    public class JobOrderControllerTests
    {
        private readonly Mock<IJobOrderService> _mockJobOrderService;
        private readonly JobOrderController _controller;
        private readonly Mock<ITempDataDictionary> _mockTempData;

        public JobOrderControllerTests(JobOrderController controller, Mock<ITempDataDictionary> mockTempData)
        {
            _mockJobOrderService = new Mock<IJobOrderService>();
            var mockHubContext = new Mock<IHubContext<TugboatHub>>();
            _mockTempData = mockTempData;

            // Mock SignalR Clients
            var mockClients = new Mock<IHubClients>();
            var mockClientProxy = new Mock<IClientProxy>();
            mockHubContext.Setup(h => h.Clients).Returns(mockClients.Object);
            mockClients.Setup(c => c.All).Returns(mockClientProxy.Object);

            _controller = controller;

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

        public JobOrderControllerTests(Mock<IJobOrderService> mockJobOrderService, JobOrderController controller, Mock<ITempDataDictionary> mockTempData)
        {
            _mockJobOrderService = mockJobOrderService;
            _controller = controller;
            _mockTempData = mockTempData;
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

        #region Close Tests

        [Fact]
        public async Task Close_Post_ReturnsSuccess_RedirectsToDetails()
        {
            // Arrange
            _mockJobOrderService.Setup(s => s.CloseJobOrderAsync(1, "testuser", false, It.IsAny<CancellationToken>()))
                .ReturnsAsync(ServiceResult.Success("Closed"));

            // Act
            var result = await _controller.Close(1, CancellationToken.None);

            // Assert
            var redirectResult = result.Should().BeOfType<RedirectToActionResult>().Subject;
            redirectResult.ActionName.Should().Be("Details");
            _mockTempData.VerifySet(t => t["success"] = "Closed");
        }

        [Fact]
        public async Task Close_Post_RequiresConfirmation_SetsTempDataAndRedirects()
        {
            // Arrange
            var resultWithConfirmation = new ServiceResult
            {
                IsSuccess = true,
                Message = "Warning: Tickets pending approval",
                Status = ServiceResultStatus.ConfirmationRequired
            };

            _mockJobOrderService.Setup(s => s.CloseJobOrderAsync(1, "testuser", false, It.IsAny<CancellationToken>()))
                .ReturnsAsync(resultWithConfirmation);

            // Act
            var result = await _controller.Close(1, CancellationToken.None);

            // Assert
            _ = result.Should().BeOfType<RedirectToActionResult>().Subject;
            _mockTempData.VerifySet(t => t["JobOrder_PendingCloseId"] = 1);
            _mockTempData.VerifySet(t => t["warning"] = "Warning: Tickets pending approval");
        }

        #endregion
    }
}


