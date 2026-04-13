using IBS.DataAccess.Repository.IRepository;
using IBS.Models.Integrated;
using IBS.Models.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IBS.DataAccess.Repository.Integrated.IRepository
{
    public interface ICustomerOrderSlipRepository : IRepository<CustomerOrderSlip>
    {
        Task<string> GenerateCodeAsync(string companyClaims, CancellationToken cancellationToken = default);

        Task UpdateAsync(CustomerOrderSlipViewModel viewModel, bool thereIsNewFile, CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetCosListNotDeliveredAsync(string companyClaims, CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetCosListPerCustomerAsync(int customerId, CancellationToken cancellationToken = default);

        Task<decimal> GetCustomerCreditBalance(int customerId, CancellationToken cancellationToken = default);
    }
}
