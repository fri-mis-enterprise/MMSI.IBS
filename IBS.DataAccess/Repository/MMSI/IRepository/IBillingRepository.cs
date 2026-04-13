using IBS.DataAccess.Repository.IRepository;
using IBS.Models.MMSI;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IBS.DataAccess.Repository.MMSI.IRepository
{
    public interface IBillingRepository : IRepository<Billing>
    {
        Task SaveAsync(CancellationToken cancellationToken);

        Task PostAsync(Billing billing, CancellationToken cancellationToken = default);

        Task<List<string>?> GetToBillDispatchTicketListAsync(int billingId, CancellationToken cancellationToken = default);

        Task<List<string>?> GetUniqueTugboatsListAsync(int billingId, CancellationToken cancellationToken = default);

        Task<List<DispatchTicket>?> GetPaidDispatchTicketsAsync(int billingId, CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetMMSITerminalsByPortId(int portId, CancellationToken cancellationToken = default);

        Task<List<SelectListItem>?> GetMMSICustomersWithBillablesSelectList(int? currentCustomerId, string type, CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetMMSIUnbilledTicketsById(string type, CancellationToken cancellationToken = default);

        Task<List<SelectListItem>?> GetMMSIUnbilledTicketsByCustomer(int? customerId, CancellationToken cancellationToken);

        Task<List<SelectListItem>> GetMMSIBilledTicketsById(int id, CancellationToken cancellationToken = default);

        Task<string> GenerateBillingNumber(CancellationToken cancellationToken = default);

        Billing ProcessAddress(Billing model, CancellationToken cancellationToken = default);
    }
}
