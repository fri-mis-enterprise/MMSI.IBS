using IBS.DataAccess.Repository.IRepository;
using IBS.DataAccess.Repository.MasterFile.IRepository;
using IBS.Models.MasterFile;
using IBS.Services;
using IBS.Utility.Helpers;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using FluentAssertions;
using System.Linq.Expressions;
using IBS.Models;

namespace IBS.Tests.Services
{
    public class ChartOfAccountServiceTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<ICacheService> _mockCacheService;
        private readonly Mock<ILogger<ChartOfAccountService>> _mockLogger;
        private readonly ChartOfAccountService _service;
        private readonly Mock<IChartOfAccountRepository> _mockCoaRepo;

        public ChartOfAccountServiceTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockCacheService = new Mock<ICacheService>();
            _mockLogger = new Mock<ILogger<ChartOfAccountService>>();
            _mockCoaRepo = new Mock<IChartOfAccountRepository>();

            _mockUnitOfWork.Setup(u => u.ChartOfAccount).Returns(_mockCoaRepo.Object);
            _mockUnitOfWork.Setup(u => u.AuditTrail).Returns(new Mock<IAuditTrailRepository>().Object);
            
            _mockUnitOfWork.Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<Task>>(), It.IsAny<CancellationToken>()))
                .Callback<Func<Task>, CancellationToken>(async (action, _) => await action())
                .Returns(Task.CompletedTask);

            _service = new ChartOfAccountService(_mockUnitOfWork.Object, _mockCacheService.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task CreateAsync_CalculatesCorrectAccountNumber_ForLevel4()
        {
            // Arrange
            var parentAccount = new ChartOfAccount { AccountId = 1, AccountNumber = "1000", Level = 3, AccountType = "Asset" };
            var lastAccount = new ChartOfAccount { AccountNumber = "1100", ParentAccountId = 1 };

            _mockCoaRepo.Setup(r => r.GetAsync(It.IsAny<Expression<Func<ChartOfAccount, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(parentAccount);
            
            _mockCoaRepo.Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<ChartOfAccount, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<ChartOfAccount> { lastAccount });

            // Act
            var result = await _service.CreateAsync(1, "New Account", "user", "CompanyA", CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            _mockCoaRepo.Verify(r => r.AddAsync(It.Is<ChartOfAccount>(a => a.AccountNumber == "1200"), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_CalculatesCorrectAccountNumber_ForLevel5()
        {
            // Arrange
            var parentAccount = new ChartOfAccount { AccountId = 1, AccountNumber = "1200", Level = 4, AccountType = "Asset" };
            var lastAccount = new ChartOfAccount { AccountNumber = "1201", ParentAccountId = 1 };

            _mockCoaRepo.Setup(r => r.GetAsync(It.IsAny<Expression<Func<ChartOfAccount, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(parentAccount);
            
            _mockCoaRepo.Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<ChartOfAccount, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<ChartOfAccount> { lastAccount });

            // Act
            var result = await _service.CreateAsync(1, "Sub Account", "user", "CompanyA", CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            _mockCoaRepo.Verify(r => r.AddAsync(It.Is<ChartOfAccount>(a => a.AccountNumber == "1202"), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_UpdatesNameAndLogsAudit()
        {
            // Arrange
            var existingAccount = new ChartOfAccount { AccountId = 1, AccountName = "Old Name", AccountNumber = "1000" };
            _mockCoaRepo.Setup(r => r.GetAsync(It.IsAny<Expression<Func<ChartOfAccount, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingAccount);

            // Act
            var result = await _service.UpdateAsync(1, "Updated Name", "user", "CompanyA", CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            existingAccount.AccountName.Should().Be("Updated Name");
            _mockUnitOfWork.Verify(u => u.SaveAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
