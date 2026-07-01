using IBS.Models;
using IBS.Models.MSAP;
using IBS.Models.MSAP.ViewModels;
using IBS.Utility.Helpers;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IBS.Services
{
    public interface ICollectionService
    {
        Task<Collection?> GetCollectionByIdAsync(int id, CancellationToken cancellationToken);
        Task<ServiceResult<int>> CreateCollectionAsync(CreateCollectionViewModel viewModel, string username, CancellationToken cancellationToken);
        Task<ServiceResult> UpdateCollectionAsync(CreateCollectionViewModel viewModel, string username, CancellationToken cancellationToken);
        Task<(IEnumerable<Collection> Data, int RecordsFiltered, int TotalRecords)> GetPagedCollectionsAsync(DataTablesParameters parameters, CancellationToken cancellationToken);
        Task<CreateCollectionViewModel> PopulateCreateViewModelAsync(CancellationToken cancellationToken);
        Task<CreateCollectionViewModel?> PopulateEditViewModelAsync(int id, CancellationToken cancellationToken);
        Task<ServiceResult<object>> GetUncollectedBillingsForTableAsync(int customerId, int? collectionId, CancellationToken cancellationToken);
        Task<ServiceResult<IEnumerable<Billing>>> GetSelectedBillingsAsync(List<string> billingIds, CancellationToken cancellationToken);
        Task<bool> IsCustomerVatableAsync(int customerId, CancellationToken cancellationToken);
        Task<ServiceResult<object>> GetBankAccountDetailsAsync(int bankId, CancellationToken cancellationToken);
        Task<List<SelectListItem>?> GetUncollectedBillingsSelectListAsync(int? customerId, CancellationToken cancellationToken);
        Task<List<SelectListItem>> GetCustomerSelectListAsync(int? collectionId, int customerId, CancellationToken cancellationToken);
    }
}


