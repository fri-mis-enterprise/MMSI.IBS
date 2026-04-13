using IBS.DataAccess.Repository.IRepository;
using IBS.Models.MMSI.MasterFile;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IBS.DataAccess.Repository.MMSI.IRepository
{
    public interface ITugMasterRepository : IRepository<TugMaster>
    {
        Task SaveAsync(CancellationToken cancellationToken);

        Task<List<SelectListItem>> GetMMSITugMastersById(CancellationToken cancellationToken = default);
    }
}
