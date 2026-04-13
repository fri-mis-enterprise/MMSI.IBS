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
    public class MonthlyClosureService(ILogger<MonthlyClosureService> logger): IMonthlyClosureService
    {
        public Task Execute(DateOnly monthDate, string company, string user, CancellationToken cancellationToken = default)
        {
            logger.LogWarning("Monthly Closure executed but not implemented - A/P module was removed.");
            return Task.CompletedTask;
        }
    }
}
