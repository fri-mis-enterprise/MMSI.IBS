using IBS.Models.MSAP;
using IBS.Utility.Helpers;

namespace IBS.Services
{
    public interface IJobOrderService
    {
        Task<IEnumerable<JobOrder>> GetAllJobOrdersAsync(CancellationToken cancellationToken);
        Task<JobOrder?> GetJobOrderByIdAsync(int id, CancellationToken cancellationToken);
        Task<ServiceResult<int>> CreateJobOrderAsync(JobOrder jobOrder, string username, CancellationToken cancellationToken);
        Task<ServiceResult> UpdateJobOrderAsync(JobOrder model, string username, CancellationToken cancellationToken);
        Task<ServiceResult> CancelJobOrderAsync(int id, string username, CancellationToken cancellationToken);
        Task<ServiceResult> CloseJobOrderAsync(int id, string username, bool forceClose, CancellationToken cancellationToken);
    }
}


