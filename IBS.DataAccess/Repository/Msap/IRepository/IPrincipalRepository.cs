using IBS.DataAccess.Repository.IRepository;
using IBS.Models.MSAP.MasterFile;

namespace IBS.DataAccess.Repository.Msap.IRepository
{
    public interface IPrincipalRepository : IRepository<Principal>
    {
        Task SaveAsync(CancellationToken cancellationToken);

    }
}



