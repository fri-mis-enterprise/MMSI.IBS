using IBS.DataAccess.Repository.IRepository;
using IBS.Models.MMSI;

namespace IBS.DataAccess.Repository.MMSI.IRepository
{
    public interface ITariffTableRepository : IRepository<TariffRate>
    {
        Task SaveAsync(CancellationToken cancellationToken);
    }
}
