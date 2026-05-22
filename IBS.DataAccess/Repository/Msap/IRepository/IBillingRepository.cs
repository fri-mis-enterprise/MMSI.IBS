using IBS.DataAccess.Repository.IRepository;
using IBS.Models.MSAP;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IBS.DataAccess.Repository.Msap.IRepository
{
    public interface IBillingRepository : IRepository<Billing>
    {
        Task SaveAsync(CancellationToken cancellationToken);

        Task<List<string>?> GetToBillDispatchTicketListAsync(int billingId, CancellationToken cancellationToken = default);

        Task<List<string>?> GetUniqueTugboatsListAsync(int billingId, CancellationToken cancellationToken = default);

        Task<List<DispatchTicket>?> GetPaidDispatchTicketsAsync(int billingId, CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetMsapTerminalsByPortId(int portId, CancellationToken cancellationToken = default);

        Task<List<SelectListItem>?> GetMsapCustomersWithBillablesSelectList(int? currentCustomerId, string type, CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetMsapUnbilledTicketsById(string type, CancellationToken cancellationToken = default);

        Task<List<SelectListItem>?> GetMsapUnbilledTicketsByCustomer(int? customerId, CancellationToken cancellationToken);

        Task<List<SelectListItem>> GetMsapBilledTicketsById(int id, CancellationToken cancellationToken = default);

        Task<string> GenerateBillingNumber(CancellationToken cancellationToken = default);

        Billing ProcessAddress(Billing model, CancellationToken cancellationToken = default);
    }
}



