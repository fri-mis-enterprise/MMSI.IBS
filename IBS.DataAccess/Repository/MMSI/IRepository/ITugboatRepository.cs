using IBS.Models.Books;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models.MMSI;
using IBS.Models.MMSI.MasterFile;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IBS.DataAccess.Repository.MMSI.IRepository
{
    public interface ITugboatRepository : IRepository<Tugboat>
    {
        Task SaveAsync(CancellationToken cancellationToken);

        Task<List<SelectListItem>> GetMMSITugboatsById(CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetMMSICompanyOwnerSelectListById(CancellationToken cancellationToken = default);
    }
}
