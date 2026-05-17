using IBS.DTOs;

namespace IBS.Services
{
    public interface ITugboatMonitoringService
    {
        Task<List<TugboatTimelineDto>> GetTugboatTimelineDataAsync(DateTime start, DateTime end, CancellationToken cancellationToken);
    }
}
