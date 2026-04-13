using System.Linq.Expressions;
using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.MasterFile.IRepository;
using IBS.Models.MasterFile;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace IBS.DataAccess.Repository.MasterFile
{
    public class PickUpPointRepository(ApplicationDbContext db): Repository<PickUpPoint>(db), IPickUpPointRepository
    {
        private readonly ApplicationDbContext _db = db;

        public override async Task<IEnumerable<PickUpPoint>> GetAllAsync(Expression<Func<PickUpPoint, bool>>? filter, CancellationToken cancellationToken = default)
        {
            IQueryable<PickUpPoint> query = dbSet;
            if (filter != null)
            {
                query = query.Where(filter);
            }

            return await query
                .Include(p => p.Supplier)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<SelectListItem>> GetPickUpPointListBasedOnSupplier(string companyClaims, int supplierId, CancellationToken cancellationToken = default)
        {
            return await _db.PickUpPoints
                .OrderBy(p => p.Depot)
                .Where(p => p.SupplierId == supplierId)
                .Select(po => new SelectListItem
                {
                    Value = po.PickUpPointId.ToString(),
                    Text = po.Depot
                })
                .ToListAsync(cancellationToken);
        }
    }
}
