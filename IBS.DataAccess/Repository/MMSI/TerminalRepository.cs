using System.Linq.Expressions;
using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.MMSI.IRepository;
using IBS.Models.MMSI;
using IBS.Models.MMSI.MasterFile;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace IBS.DataAccess.Repository.MMSI
{
    public class TerminalRepository(ApplicationDbContext db): Repository<Terminal>(db), ITerminalRepository
    {
        private readonly ApplicationDbContext _db = db;

        public async Task SaveAsync(CancellationToken cancellationToken)
        {
            await _db.SaveChangesAsync(cancellationToken);
        }

        public override async Task<Terminal?> GetAsync(Expression<Func<Terminal, bool>> filter, CancellationToken cancellationToken = default)
        {
            return await dbSet
                .Include(t => t.Port)
                .Where(filter)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public override async Task<IEnumerable<Terminal>> GetAllAsync(Expression<Func<Terminal, bool>>? filter, CancellationToken cancellationToken = default)
        {
            IQueryable<Terminal> query = dbSet
                .Include(a => a.Port)
                .OrderBy(t => t.TerminalName);

            if (filter != null)
            {
                query = query.Where(filter);
            }

            return await query.ToListAsync(cancellationToken);
        }

        public async Task<List<SelectListItem>> GetMMSITerminalsById(DispatchTicket model, CancellationToken cancellationToken = default)
        {
            List<SelectListItem> terminals;

            if (model.Terminal?.Port?.PortId != null)
            {
                terminals = await _db.MMSITerminals
                .Where(t => t.PortId == model.Terminal.Port.PortId)
                .OrderBy(s => s.TerminalNumber)
                .Select(s => new SelectListItem
                {
                    Value = s.TerminalId.ToString(),
                    Text = s.TerminalNumber + " " + s.TerminalName
                }).ToListAsync(cancellationToken);
            }
            else
            {
                terminals = await _db.MMSITerminals
                .OrderBy(s => s.TerminalNumber)
                .Select(s => new SelectListItem
                {
                    Value = s.TerminalId.ToString(),
                    Text = s.TerminalNumber + " " + s.TerminalName
                }).ToListAsync(cancellationToken);
            }

            return terminals;
        }

        public async Task<List<SelectListItem>> GetMMSIAllTerminalsById(CancellationToken cancellationToken = default)

        {
            var terminals = await _db.MMSITerminals
                .OrderBy(s => s.TerminalNumber)
                .Select(s => new SelectListItem
                {
                    Value = s.TerminalId.ToString(),
                    Text = s.TerminalNumber + " " + s.TerminalName,
                }).ToListAsync(cancellationToken);

            return terminals;
        }

        public async Task<List<SelectListItem>?> GetMMSITerminalsSelectList(int? portId, CancellationToken cancellationToken = default)
        {
            IQueryable<Terminal> query = dbSet;

            if (portId != 0)
            {
                query = query.Where(t => t.PortId == portId);
            }

            return await query
                .OrderBy(s => s.TerminalNumber)
                .Select(s => new SelectListItem
                {
                    Value = s.TerminalId.ToString(),
                    Text = s.TerminalName,
                }).ToListAsync(cancellationToken);
        }
    }
}
