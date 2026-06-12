using IBS.DTOs;

namespace IBS.Services
{
    public interface IVesselPlanningService
    {
        Task<VesselPlanningDashboardDto> GetVesselPlanningDashboardAsync(int? portId, CancellationToken cancellationToken = default);
    }
}
