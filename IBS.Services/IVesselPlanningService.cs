using IBS.DTOs;

namespace IBS.Services
{
    public interface IVesselPlanningService
    {
        Task<VesselPlanningDataDto> GetVesselPlanningDataAsync(int? portId, DateTime start, DateTime end, CancellationToken cancellationToken = default);
    }
}
