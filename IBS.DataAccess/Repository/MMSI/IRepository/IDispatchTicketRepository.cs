using IBS.DataAccess.Repository.IRepository;
using IBS.Models.MMSI;

namespace IBS.DataAccess.Repository.MMSI.IRepository
{
    public interface IDispatchTicketRepository : IRepository<DispatchTicket>
    {
        Task SaveAsync(CancellationToken cancellationToken);

        Task<DispatchTicket> GetDispatchTicketLists(DispatchTicket model, CancellationToken cancellationToken = default);
    }
}
