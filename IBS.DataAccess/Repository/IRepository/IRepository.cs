using IBS.Models.Books;
using System.Linq.Expressions;
using IBS.DTOs;

namespace IBS.DataAccess.Repository.IRepository
{
    public interface IRepository<T> where T : class
    {
        Task<IEnumerable<T>> GetAllAsync(Expression<Func<T, bool>>? filter = null, CancellationToken cancellationToken = default);

        Task<T?> GetAsync(Expression<Func<T, bool>> filter, CancellationToken cancellationToken = default);

        Task AddAsync(T entity, CancellationToken cancellationToken = default);

        Task RemoveAsync(T entity, CancellationToken cancellationToken = default);

        bool IsJournalEntriesBalanced(IEnumerable<GeneralLedgerBook> journals);

        decimal ComputeNetOfVat(decimal grossAmount);

        decimal ComputeVatAmount(decimal netOfVatAmount);

        Task<List<AccountTitleDto>> GetListOfAccountTitleDto(CancellationToken cancellationToken = default);

        Task<DateOnly> ComputeDueDateAsync(string terms, DateOnly transactionDate, CancellationToken cancellationToken = default);
    }
}
