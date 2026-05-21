using IBS.Models;
using IBS.Models.MMSI;
using IBS.Utility.Helpers;
using Microsoft.AspNetCore.Mvc.Rendering;
using IBS.DTOs;

namespace IBS.Services
{
    public interface IBillingService
    {
        Task<Billing?> GetBillingByIdAsync(int id, CancellationToken cancellationToken);
        Task<ServiceResult<int>> CreateBillingAsync(Billing model, string username, string company, CancellationToken cancellationToken);
        Task<ServiceResult> UpdateBillingAsync(Billing model, string username, CancellationToken cancellationToken);
        Task<ServiceResult> DeleteBillingAsync(int id, CancellationToken cancellationToken);
        Task<(IEnumerable<Billing> Data, int RecordsFiltered, int TotalRecords)> GetPagedBillingsAsync(DataTablesParameters parameters, CancellationToken cancellationToken);
        Task<byte[]> GenerateExcelForPrintingAsync(int id, CancellationToken cancellationToken);
        Task<List<object>> SearchCustomersAsync(string? term, CancellationToken cancellationToken);
        Task<List<object>> SearchPrincipalsAsync(string? term, int customerId, CancellationToken cancellationToken);
        Task<List<object>> SearchJobOrdersAsync(string? term, int customerId, CancellationToken cancellationToken);
        Task<ServiceResult<JobOrderBillingDto>> GetDispatchTicketsByJobOrderAsync(int jobOrderId, CancellationToken cancellationToken);
        Task<List<SelectListItem>?> GetPrincipalsSelectListAsync(int customerId, CancellationToken cancellationToken);
        Task<List<SelectListItem>?> GetEditTicketsSelectListAsync(int? customerId, int billingId, CancellationToken cancellationToken);
        Task<Billing> PopulateBillingSelectListsAsync(Billing model, CancellationToken cancellationToken);
        Task<ServiceResult<object>> GetCustomerDetailsAsync(int customerId, CancellationToken cancellationToken);
    }
}
