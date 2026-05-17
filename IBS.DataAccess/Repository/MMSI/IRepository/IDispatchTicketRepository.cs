using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using IBS.Models.MMSI;

namespace IBS.DataAccess.Repository.MMSI.IRepository
{
    public interface IDispatchTicketRepository : IRepository<DispatchTicket>
    {
        Task SaveAsync(CancellationToken cancellationToken);

        Task<DispatchTicket?> GetDispatchTicketWithDetailsAsync(int id, CancellationToken cancellationToken = default);

        Task<IEnumerable<DispatchTicket>> GetAllDispatchTicketsWithDetailsAsync(CancellationToken cancellationToken = default);

        Task<IEnumerable<DispatchTicket>> GetDispatchTicketsWithDetailsAsync(DateTime start, DateTime end, CancellationToken cancellationToken = default);

        Task<bool> IsJobOrderEditableAsync(int? jobOrderId, CancellationToken cancellationToken = default);

        Task UpdateStatusAsync(int id, string status, string updatedBy, string activity, string docType, CancellationToken cancellationToken = default);

        Task SaveTariffAsync(DispatchTicket model, string chargeType, string bafChargeType, string updatedBy, bool isEdit, CancellationToken cancellationToken = default);

        Task UpdateAsync(DispatchTicket entity, string updatedBy, CancellationToken cancellationToken = default);

        Task<(IEnumerable<DispatchTicket> Data, int RecordsFiltered, int TotalRecords)> GetPagedDispatchTicketsAsync(DataTablesParameters parameters, string filterType, CancellationToken cancellationToken = default);       }
}
