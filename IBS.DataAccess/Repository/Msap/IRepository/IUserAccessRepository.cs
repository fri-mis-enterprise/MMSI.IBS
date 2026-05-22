using IBS.DataAccess.Repository.IRepository;
using IBS.Models.MSAP.MasterFile;

namespace IBS.DataAccess.Repository.Msap.IRepository
{
    public interface IUserAccessRepository : IRepository<UserAccess>
    {
        Task SaveAsync(CancellationToken cancellationToken);
    }
}



