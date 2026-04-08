using IBS.Models.Books;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models.MMSI;
using IBS.Models.MMSI.MasterFile;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IBS.DataAccess.Repository.MMSI.IRepository
{
    public interface ITerminalRepository : IRepository<Terminal>
    {
        Task SaveAsync(CancellationToken cancellationToken);

        Task<List<SelectListItem>?> GetMMSITerminalsSelectList(int? portId, CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetMMSITerminalsById(DispatchTicket model, CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetMMSIAllTerminalsById(CancellationToken cancellationToken = default);
    }
}
