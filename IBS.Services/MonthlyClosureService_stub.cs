using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using Microsoft.Extensions.Logging;

namespace IBS.Services
{
    public interface IMonthlyClosureService
    {
        Task Execute(DateOnly monthDate, string company, string user, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Stub implementation - Monthly Closure was removed with A/P module
    /// </summary>
    public class MonthlyClosureService : IMonthlyClosureService
    {
        private readonly ILogger<MonthlyClosureService> _logger;

        public MonthlyClosureService(
            ILogger<MonthlyClosureService> logger)
        {
            _logger = logger;
        }

        public Task Execute(DateOnly monthDate, string company, string user, CancellationToken cancellationToken = default)
        {
            _logger.LogWarning("Monthly Closure executed but not implemented - A/P module was removed.");
            return Task.CompletedTask;
        }
    }
}
