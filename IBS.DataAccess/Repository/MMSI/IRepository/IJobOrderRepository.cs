using IBS.DataAccess.Repository.IRepository;
using IBS.Models.MMSI;

namespace IBS.DataAccess.Repository.MMSI.IRepository
{
    public interface IJobOrderRepository : IRepository<MMSIJobOrder>
    {
        Task<IEnumerable<MMSIJobOrder>> GetAllJobOrdersWithDetailsAsync(CancellationToken cancellationToken);
        Task<MMSIJobOrder?> GetJobOrderWithDetailsAsync(int id, CancellationToken cancellationToken);
        Task<string> GenerateJobOrderNumber(CancellationToken cancellationToken);
    }
}
