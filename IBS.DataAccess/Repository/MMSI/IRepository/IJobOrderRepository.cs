using IBS.DataAccess.Repository.IRepository;
using IBS.Models.MMSI;

namespace IBS.DataAccess.Repository.MMSI.IRepository
{
    public interface IJobOrderRepository : IRepository<JobOrder>
    {
        Task<IEnumerable<JobOrder>> GetAllJobOrdersWithDetailsAsync(CancellationToken cancellationToken);
        Task<JobOrder?> GetJobOrderWithDetailsAsync(int id, CancellationToken cancellationToken);
        Task<string> GenerateJobOrderNumber(CancellationToken cancellationToken);
    }
}
