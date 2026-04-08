using IBS.Models.Books;
using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.MMSI.IRepository;
using IBS.Models.MMSI;
using IBS.Models.MMSI.MasterFile;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace IBS.DataAccess.Repository.MMSI
{
    public class TugboatRepository : Repository<Tugboat>, ITugboatRepository
    {
        private readonly ApplicationDbContext _db;

        public TugboatRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public async Task SaveAsync(CancellationToken cancellationToken)
        {
            await _db.SaveChangesAsync(cancellationToken);
        }

        public async Task<List<SelectListItem>> GetMMSITugboatsById(CancellationToken cancellationToken = default)
        {
            var tugBoats = await _db.MMSITugboats.OrderBy(s => s.TugboatNumber).Select(s => new SelectListItem
            {
                Value = s.TugboatId.ToString(),
                Text = s.TugboatNumber + " " + s.TugboatName
            }).ToListAsync(cancellationToken);

            return tugBoats;
        }

        public async Task<List<SelectListItem>> GetMMSICompanyOwnerSelectListById(CancellationToken cancellationToken = default)
        {
            var companyOwnerList = await _db.MMSITugboatOwners
                .OrderBy(dt => dt.TugboatOwnerNumber).Select(s => new SelectListItem
                {
                    Value = s.TugboatOwnerId.ToString(),
                    Text = $"{s.TugboatOwnerNumber} {s.TugboatOwnerName}"
                }).ToListAsync(cancellationToken);

            return companyOwnerList;
        }
    }
}
