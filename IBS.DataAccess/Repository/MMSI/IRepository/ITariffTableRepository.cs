using IBS.Models.Books;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models.MMSI;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IBS.DataAccess.Repository.MMSI.IRepository
{
    public interface ITariffTableRepository : IRepository<TariffRate>
    {
        Task SaveAsync(CancellationToken cancellationToken);
    }
}
