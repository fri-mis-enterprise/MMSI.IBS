using IBS.Models.MSAP;
using IBS.Models.MSAP.ViewModels;
using IBS.Models;
using IBS.Utility.Helpers;
using Microsoft.AspNetCore.Http;

namespace IBS.Services
{
    public interface IDispatchTicketService
    {
        Task<DispatchTicket?> GetDispatchTicketByIdAsync(int id, CancellationToken cancellationToken);
        Task<ServiceRequestViewModel> PopulateServiceRequestViewModelAsync(ServiceRequestViewModel? viewModel, int? jobOrderId, CancellationToken cancellationToken);
        Task<ServiceResult<int>> CreateDispatchTicketAsync(ServiceRequestViewModel viewModel, IFormFile? imageFile, IFormFile? videoFile, string username, CancellationToken cancellationToken);
        Task<ServiceResult> UpdateDispatchTicketAsync(ServiceRequestViewModel viewModel, IFormFile? imageFile, IFormFile? videoFile, string username, CancellationToken cancellationToken);
        Task<ServiceResult> SaveTariffAsync(DispatchTicket model, string chargeType, string chargeType2, string username, bool isEdit, CancellationToken cancellationToken);
        Task<ServiceResult> ApproveTariffAsync(int id, string username, CancellationToken cancellationToken);
        Task<ServiceResult> DisapproveTariffAsync(int id, string reason, string username, CancellationToken cancellationToken);
        Task<ServiceResult> DeleteImageAsync(int id, CancellationToken cancellationToken);
        Task<(IEnumerable<DispatchTicket> Data, int RecordsFiltered, int TotalRecords)> GetPagedDispatchTicketsAsync(DataTablesParameters parameters, string filterType, CancellationToken cancellationToken);
        Task<ServiceResult<object>> CheckForTariffRateAsync(int customerId, int dispatchTicketId, CancellationToken cancellationToken);
        Task<bool> IsJobOrderEditableAsync(int? jobOrderId, CancellationToken cancellationToken);
        Task<bool> IsTicketJobOrderEditableAsync(int dispatchTicketId, CancellationToken cancellationToken);
        Task<List<object>> SearchCustomersAsync(string? term, CancellationToken cancellationToken);
    }
}


