using IBS.Models.Books;
using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models.MasterFile;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using IBS.DTOs;

namespace IBS.DataAccess.Repository
{
    public class Repository<T> : IRepository<T> where T : class
    {
        private readonly ApplicationDbContext _db;
        internal DbSet<T> dbSet;

        private const decimal VatRate = 0.12m;

        public Repository(ApplicationDbContext db)
        {
            _db = db;
            dbSet = _db.Set<T>();
        }

        public virtual async Task<IEnumerable<T>> GetAllAsync(Expression<Func<T, bool>>? filter, CancellationToken cancellationToken = default)
        {
            IQueryable<T> query = dbSet;
            if (filter != null)
            {
                query = query.Where(filter);
            }

            return await query.ToListAsync(cancellationToken);
        }

        public virtual async Task<T?> GetAsync(Expression<Func<T, bool>> filter, CancellationToken cancellationToken = default)
        {
            return await dbSet.Where(filter).FirstOrDefaultAsync(cancellationToken);
        }

        public virtual async Task AddAsync(T entity, CancellationToken cancellationToken = default)
        {
            dbSet.Add(entity);
            await _db.SaveChangesAsync(cancellationToken);
        }

        public bool IsJournalEntriesBalanced(IEnumerable<GeneralLedgerBook> journals)
        {
            try
            {
                var totalDebit = Math.Round(journals.Sum(j => j.Debit), 2, MidpointRounding.AwayFromZero);
                var totalCredit = Math.Round(journals.Sum(j => j.Credit), 2, MidpointRounding.AwayFromZero);

                return totalDebit == totalCredit;
            }
            catch (Exception ex)
            {
                throw new ArgumentException(ex.Message);
            }
        }

        public async Task RemoveAsync(T entity, CancellationToken cancellationToken = default)
        {
            dbSet.Remove(entity);
            await _db.SaveChangesAsync(cancellationToken);
        }

        public async Task RemoveRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
        {
            dbSet.RemoveRange(entities);
            await _db.SaveChangesAsync(cancellationToken);
        }

        public async Task<SupplierDto?> MapSupplierToDTO(string supplierCode, CancellationToken cancellationToken = default)
        {
            return await _db.Set<Supplier>()
                .Where(s => s.SupplierCode == supplierCode)
                .Select(s => new SupplierDto
                {
                    SupplierId = s.SupplierId,
                    SupplierCode = s.SupplierCode!,
                    SupplierName = s.SupplierName
                })
                .FirstOrDefaultAsync(cancellationToken);
        }

        public decimal ComputeNetOfVat(decimal grossAmount)
        {
            if (grossAmount == 0)
            {
                return grossAmount;
            }

            return grossAmount / (1 + VatRate);
        }

        public decimal ComputeVatAmount(decimal netOfVatAmount)
        {
            return netOfVatAmount * VatRate;
        }

        public async Task RemoveRecords<TEntity>(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
       where TEntity : class
        {
            var entitySet = _db.Set<TEntity>();
            var entitiesToRemove = await entitySet.Where(predicate).ToListAsync(cancellationToken);

            if (entitiesToRemove.Any())
            {
                foreach (var entity in entitiesToRemove)
                {
                    entitySet.Remove(entity);
                }

                await _db.SaveChangesAsync(cancellationToken);
            }
        }

        public decimal ComputeEwtAmount(decimal netOfVatAmount, decimal percent)
        {
            return netOfVatAmount * percent;
        }

        public decimal ComputeNetOfEwt(decimal grossAmount, decimal ewtAmount)
        {
            return grossAmount - ewtAmount;
        }

        public async Task<List<AccountTitleDto>> GetListOfAccountTitleDto(CancellationToken cancellationToken = default)
        {
            return await _db.ChartOfAccounts
               .Where(coa => coa.Level == 4 || coa.Level == 5)
               .Select(coa => new AccountTitleDto
               {
                   AccountId = coa.AccountId,
                   AccountNumber = coa.AccountNumber!,
                   AccountName = coa.AccountName
               })
               .ToListAsync(cancellationToken);
        }

        public async Task<DateOnly> ComputeDueDateAsync(string terms, DateOnly transactionDate, CancellationToken cancellationToken = default)
        {
            var getTerms = await _db.Terms
                .FirstOrDefaultAsync(x => x.TermsCode == terms, cancellationToken);

            if (getTerms == null)
            {
                throw new ArgumentException("No terms found.");
            }

            DateOnly dueDate = default;

            dueDate =  transactionDate.AddMonths(getTerms.NumberOfMonths).AddDays(getTerms.NumberOfDays);

            if (!terms.Contains('M'))
            {
                return dueDate;
            }

            dueDate =  dueDate.AddDays(-transactionDate.Day);

            return dueDate;
        }
    }
}
