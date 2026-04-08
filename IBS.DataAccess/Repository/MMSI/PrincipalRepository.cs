using IBS.Models.Books;
using System.Linq.Expressions;
using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.MMSI.IRepository;
using IBS.Models.MMSI.MasterFile;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace IBS.DataAccess.Repository.MMSI
{
    public class PrincipalRepository : Repository<Principal>, IPrincipalRepository
    {
        private readonly ApplicationDbContext _dbContext;

        public PrincipalRepository(ApplicationDbContext dbContext) : base(dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task SaveAsync(CancellationToken cancellationToken)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        public override async Task<IEnumerable<Principal>> GetAllAsync(Expression<Func<Principal, bool>>? filter, CancellationToken cancellationToken = default)
        {
            IQueryable<Principal> query = dbSet.Include(p => p.Customer);

            if (filter != null)
            {
                query = query.Where(filter);
            }

            return await query.ToListAsync(cancellationToken);
        }

        public async Task<List<SelectListItem>> GetMMSIPortsSelectList(CancellationToken cancellationToken = default)
        {
            var ports = await _dbContext.MMSIPorts
                .OrderBy(s => s.PortName)
                .Select(s => new SelectListItem
                {
                    Value = s.PortId.ToString(),
                    Text = s.PortName
                }).ToListAsync(cancellationToken);

            return ports;
        }
    }
}
