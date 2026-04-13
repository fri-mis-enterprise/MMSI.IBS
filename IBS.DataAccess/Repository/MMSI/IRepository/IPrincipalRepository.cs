using IBS.DataAccess.Repository.IRepository;
using IBS.Models.MMSI.MasterFile;

namespace IBS.DataAccess.Repository.MMSI.IRepository
{
    public interface IPrincipalRepository : IRepository<Principal>
    {
        Task SaveAsync(CancellationToken cancellationToken);

    }
}
