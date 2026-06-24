using IBS.Models;
using IBS.Models.MSAP;
using IBS.Models.MSAP.ViewModels;
using IBS.Utility.Helpers;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IBS.Services
{
    public interface IJobOrderService
    {
        Task<IEnumerable<JobOrder>> GetAllJobOrdersAsync(CancellationToken cancellationToken);
        Task<JobOrder?> GetJobOrderByIdAsync(int id, CancellationToken cancellationToken);
        Task<JobOrderViewModel> PopulateJobOrderViewModelAsync(JobOrderViewModel? viewModel, CancellationToken cancellationToken);
        Task<ServiceResult<int>> CreateJobOrderAsync(JobOrder jobOrder, string username, CancellationToken cancellationToken);
        Task<ServiceResult> UpdateJobOrderAsync(JobOrder model, string username, CancellationToken cancellationToken);
        Task TryAutoCloseAsync(int jobOrderId, string username, CancellationToken cancellationToken);
        Task<List<object>> SearchCustomersAsync(string? term, CancellationToken cancellationToken);
        Task<(IEnumerable<JobOrder> Data, int RecordsFiltered, int TotalRecords)> GetPagedJobOrdersAsync(DataTablesParameters parameters, CancellationToken cancellationToken);
        Task<ServiceResult> AssignTugboatAsync(int jobOrderId, int tugboatId, string username, CancellationToken cancellationToken);
        Task<ServiceResult> UnassignTugboatAsync(int jobOrderId, int tugboatId, string username, CancellationToken cancellationToken);
        Task<List<SelectListItem>> GetJobOrderSelectListAsync(CancellationToken cancellationToken);
    }
}


