using IBS.DataAccess.Repository.IRepository;
using IBS.Models.MasterFile;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IBS.DataAccess.Repository.MasterFile.IRepository
{
    public interface ICustomerRepository : IRepository<Customer>
    {
        Task<bool> IsTinNoExistAsync(string tin, string company, CancellationToken cancellationToken = default);

        Task<string> GenerateCodeAsync(string customerType, CancellationToken cancellationToken = default);

        Task UpdateAsync(Customer model, CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetCustomerBranchesSelectListAsync(int customerId, CancellationToken cancellationToken = default);

        Task<List<Customer>> SearchCustomersAsync(string term, int limit, CancellationToken cancellationToken);
    }
}
