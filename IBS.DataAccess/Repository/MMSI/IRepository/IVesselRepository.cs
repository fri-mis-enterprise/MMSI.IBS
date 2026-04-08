using IBS.Models.Books;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models.MMSI;
using IBS.Models.MMSI.MasterFile;
using IBS.Models.MMSI.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IBS.DataAccess.Repository.MMSI.IRepository
{
    public interface IVesselRepository : IRepository<Vessel>
    {
        Task SaveAsync(CancellationToken cancellationToken);

        Task<List<SelectListItem>> GetMMSIVesselsSelectList(CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetMMSIVesselsById(CancellationToken cancellationToken = default);
    }
}
