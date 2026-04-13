using IBS.DataAccess.Repository.IRepository;
using IBS.Models.MMSI.MasterFile;

namespace IBS.DataAccess.Repository.MMSI.IRepository
{
    public interface IUserAccessRepository : IRepository<UserAccess>
    {
        Task SaveAsync(CancellationToken cancellationToken);
    }
}
