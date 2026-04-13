using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.MMSI.IRepository;
using IBS.Models.MMSI.MasterFile;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace IBS.DataAccess.Repository.MMSI
{
    public class TugMasterRepository(ApplicationDbContext db): Repository<TugMaster>(db), ITugMasterRepository
    {
        private readonly ApplicationDbContext _db = db;

        public async Task SaveAsync(CancellationToken cancellationToken)
        {
            await _db.SaveChangesAsync(cancellationToken);
        }

        public async Task<List<SelectListItem>> GetMMSITugMastersById(CancellationToken cancellationToken = default)
        {
            var tugMasters = await _db.MMSITugMasters.OrderBy(s => s.TugMasterNumber).Select(s => new SelectListItem
            {
                Value = s.TugMasterId.ToString(),
                Text = s.TugMasterNumber + " " + s.TugMasterName
            }).ToListAsync(cancellationToken);

            return tugMasters;
        }
    }
}
