using IBS.DataAccess.Repository.IRepository;
using IBS.Models.MMSI;
using IBS.Models.MMSI.ViewModels;

namespace IBS.DataAccess.Repository.MMSI.IRepository
{
    public interface IServiceRequestRepository : IRepository<DispatchTicket>
    {
        Task SaveAsync(CancellationToken cancellationToken);

        Task<ServiceRequestViewModel> GetDispatchTicketSelectLists(ServiceRequestViewModel model, CancellationToken cancellationToken = default);

    }
}
