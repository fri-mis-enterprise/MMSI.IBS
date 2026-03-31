using System.Linq.Expressions;
using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.MasterFile.IRepository;
using IBS.Models.MasterFile;
using IBS.Utility.Helpers;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace IBS.DataAccess.Repository.MasterFile
{
    public class TermsRepository : Repository<Terms>, ITermsRepository
    {
        private readonly ApplicationDbContext _db;

        public TermsRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public override async Task<IEnumerable<Terms>> GetAllAsync(Expression<Func<Terms, bool>>? filter, CancellationToken cancellationToken = default)
        {
            IQueryable<Terms> query = dbSet;

            if (filter != null)
            {
                query = query.Where(filter);
            }

            return await query.ToListAsync(cancellationToken);
        }

        public async Task UpdateAsync(Terms model, CancellationToken cancellationToken = default)
        {
            var existingTerms = await _db.Terms
                .FirstOrDefaultAsync(x => x.TermsCode == model.TermsCode, cancellationToken)
                                   ?? throw new InvalidOperationException($"Terms with code '{model.TermsCode}' not found.");

            existingTerms.TermsCode = model.TermsCode;
            existingTerms.NumberOfDays = model.NumberOfDays;
            existingTerms.NumberOfMonths = model.NumberOfMonths;

            if (_db.ChangeTracker.HasChanges())
            {
                existingTerms.EditedBy = model.EditedBy;
                existingTerms.EditedDate = DateTimeHelper.GetCurrentPhilippineTime();
                await _db.SaveChangesAsync(cancellationToken);
            }
            else
            {
                throw new InvalidOperationException("No data changes!");
            }
        }

        public async Task<List<SelectListItem>> GetTermsListAsyncByCode(CancellationToken cancellationToken = default)
        {
            return await _db.Terms
                .OrderBy(x => x.TermsCode)
                .Select(x => new SelectListItem
                {
                    Value = x.TermsCode,
                    Text = x.TermsCode
                })
                .ToListAsync(cancellationToken);
        }
    }
}
