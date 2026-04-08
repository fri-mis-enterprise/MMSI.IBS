using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.MMSI.IRepository;
using IBS.Models.MMSI;
using Microsoft.EntityFrameworkCore;

namespace IBS.DataAccess.Repository.MMSI
{
    public class JobOrderRepository : Repository<JobOrder>, IJobOrderRepository
    {
        private readonly ApplicationDbContext _db;

        public JobOrderRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public async Task<IEnumerable<JobOrder>> GetAllJobOrdersWithDetailsAsync(CancellationToken cancellationToken)
        {
            return await _db.MMSIJobOrders
                .Include(j => j.Customer)
                .Include(j => j.Vessel)
                .Include(j => j.Port)
                .Include(j => j.Terminal)
                .Include(j => j.DispatchTickets)
                .ToListAsync(cancellationToken);
        }

        public async Task<JobOrder?> GetJobOrderWithDetailsAsync(int id, CancellationToken cancellationToken)
        {
            return await _db.MMSIJobOrders
                .Include(j => j.Customer)
                .Include(j => j.Vessel)
                .Include(j => j.Port)
                .Include(j => j.Terminal)
                .Include(j => j.DispatchTickets)
                    .ThenInclude(dt => dt.Service)
                .Include(j => j.DispatchTickets)
                    .ThenInclude(dt => dt.Terminal)
                .Include(j => j.DispatchTickets)
                    .ThenInclude(dt => dt.Tugboat)
                .Include(j => j.DispatchTickets)
                    .ThenInclude(dt => dt.TugMaster)
                .FirstOrDefaultAsync(j => j.JobOrderId == id, cancellationToken);
        }

        public async Task<string> GenerateJobOrderNumber(CancellationToken cancellationToken)
        {
            var year = DateTime.Now.Year;
            var lastRecord = await _db.MMSIJobOrders
                .Where(j => j.JobOrderNumber.StartsWith($"JO-{year}"))
                .OrderByDescending(j => j.JobOrderNumber)
                .FirstOrDefaultAsync(cancellationToken);

            if (lastRecord == null)
            {
                return $"JO-{year}-0001";
            }

            var parts = lastRecord.JobOrderNumber.Split('-');
            if (parts.Length >= 3 && int.TryParse(parts[2], out int lastNumber))
            {
                return $"JO-{year}-{(lastNumber + 1):D4}";
            }

            return $"JO-{year}-0001";
        }
    }
}
