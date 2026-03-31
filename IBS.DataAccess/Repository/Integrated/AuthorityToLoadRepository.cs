using IBS.Models.Books;
using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.Integrated.IRepository;
using IBS.Models.Integrated;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using IBS.Utility.Helpers;

namespace IBS.DataAccess.Repository.Integrated
{
    public class AuthorityToLoadRepository : Repository<AuthorityToLoad>, IAuthorityToLoadRepository
    {
        private readonly ApplicationDbContext _db;

        public AuthorityToLoadRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public async Task<string> GenerateAtlNo(string company, CancellationToken cancellationToken)
        {
            var lastAtl = await _db
                .AuthorityToLoads
                .AsNoTracking()
                .OrderByDescending(x => x.AuthorityToLoadNo)
                .FirstOrDefaultAsync(x => x.Company == company, cancellationToken);

            var yearToday = DateTimeHelper.GetCurrentPhilippineTime().Year;

            if (lastAtl != null)
            {
                var lastAtlNo = lastAtl.AuthorityToLoadNo;
                var lastAtlParts = lastAtlNo.Split('-');
                if (int.TryParse(lastAtlParts.Last(), out var lastIncrement))
                {
                    var newIncrement = lastIncrement + 1;
                    return $"FRI-{yearToday}-{newIncrement}";
                }
            }

            return $"FRI-{yearToday}-1";
        }

        public override async Task<IEnumerable<AuthorityToLoad>> GetAllAsync(Expression<Func<AuthorityToLoad, bool>>? filter, CancellationToken cancellationToken = default)
        {
            IQueryable<AuthorityToLoad> query = dbSet
                .Include(atl => atl.Supplier)
                .Include(atl => atl.CustomerOrderSlip).ThenInclude(po => po!.Product)
                .Include(atl => atl.CustomerOrderSlip).ThenInclude(dr => dr!.Hauler)
                .Include(atl => atl.CustomerOrderSlip).ThenInclude(dr => dr!.Customer)
                .Include(atl => atl.CustomerOrderSlip).ThenInclude(cos => cos!.PickUpPoint);

            if (filter != null)
            {
                query = query.Where(filter);
            }

            return await query.ToListAsync(cancellationToken);
        }

        public override async Task<AuthorityToLoad?> GetAsync(Expression<Func<AuthorityToLoad, bool>> filter, CancellationToken cancellationToken = default)
        {
            return await dbSet.Where(filter)
                .Include(cos => cos.Supplier)
                .Include(atl => atl.CustomerOrderSlip).ThenInclude(po => po!.Product)
                .Include(atl => atl.CustomerOrderSlip).ThenInclude(dr => dr!.Hauler)
                .Include(atl => atl.CustomerOrderSlip).ThenInclude(dr => dr!.Customer)
                .Include(atl => atl.CustomerOrderSlip).ThenInclude(cos => cos!.PickUpPoint)
                .Include(atl => atl.Details).ThenInclude(atl => atl.CustomerOrderSlip).ThenInclude(cos => cos!.AppointedSuppliers)
                .Include(atl => atl.Details).ThenInclude(atl => atl.CustomerOrderSlip).ThenInclude(cos => cos!.Customer)
                .Include(atl => atl.Details).ThenInclude(atl => atl.CustomerOrderSlip).ThenInclude(cos => cos!.Product)
                .FirstOrDefaultAsync(cancellationToken);
        }
    }
}
