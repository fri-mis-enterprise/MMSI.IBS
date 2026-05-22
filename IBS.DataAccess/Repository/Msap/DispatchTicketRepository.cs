using System.Linq.Dynamic.Core;
using System.Linq.Expressions;
using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.Msap.IRepository;
using IBS.Models;
using IBS.Models.MSAP;
using Microsoft.EntityFrameworkCore;

namespace IBS.DataAccess.Repository.Msap
{
    public class DispatchTicketRepository(ApplicationDbContext db)
        : Repository<DispatchTicket>(db), IDispatchTicketRepository
    {
        private readonly ApplicationDbContext _db = db;

        public async Task SaveAsync(CancellationToken cancellationToken)
        {
            await _db.SaveChangesAsync(cancellationToken);
        }

        public override async Task<IEnumerable<DispatchTicket>> GetAllAsync(Expression<Func<DispatchTicket, bool>>? filter, CancellationToken cancellationToken = default)
        {
            IQueryable<DispatchTicket> query = dbSet
                .Include(a => a.Service)
                .Include(a => a.Terminal).ThenInclude(t => t.Port)
                .Include(a => a.Tugboat)
                .Include(a => a.TugMaster)
                .Include(a => a.Vessel);

            if (filter != null)
            {
                query = query.Where(filter);
            }

            return await query.ToListAsync(cancellationToken);
        }

        public override async Task<DispatchTicket?> GetAsync(Expression<Func<DispatchTicket, bool>> filter, CancellationToken cancellationToken = default)
        {
            var model =  await dbSet.Where(filter)
                .Include(a => a.Service)
                .Include(a => a.Terminal).ThenInclude(t => t.Port)
                .Include(a => a.Tugboat).ThenInclude(t => t.TugboatOwner)
                .Include(a => a.TugMaster)
                .Include(a => a.Vessel)
                .FirstOrDefaultAsync(cancellationToken);

            if (model != null && model.CustomerId != 0)
            {
                model.Customer = (await _db.Customers
                    .FirstOrDefaultAsync(x => x.CustomerId == model.CustomerId, cancellationToken))!;
            }

            return model;
        }

        public async Task<DispatchTicket?> GetDispatchTicketWithDetailsAsync(int id, CancellationToken cancellationToken = default)
        {
            var model = await dbSet.Where(dt => dt.DispatchTicketId == id)
                .Include(a => a.Service)
                .Include(a => a.Terminal).ThenInclude(t => t.Port)
                .Include(a => a.Tugboat).ThenInclude(t => t.TugboatOwner)
                .Include(a => a.TugMaster)
                .Include(a => a.Vessel)
                .FirstOrDefaultAsync(cancellationToken);

            if (model != null && model.CustomerId != 0)
            {
                model.Customer = (await _db.Customers
                    .FirstOrDefaultAsync(x => x.CustomerId == model.CustomerId, cancellationToken))!;
            }

            return model;
        }

        public async Task<IEnumerable<DispatchTicket>> GetDispatchTicketsWithDetailsAsync(DateTime start, DateTime end, CancellationToken cancellationToken = default)
        {
            var startDate = DateOnly.FromDateTime(start);
            var endDate = DateOnly.FromDateTime(end);

            var tickets = await dbSet
                .Include(a => a.Service)
                .Include(a => a.Terminal).ThenInclude(t => t.Port)
                .Include(a => a.Tugboat).ThenInclude(t => t.TugboatOwner)
                .Include(a => a.TugMaster)
                .Include(a => a.Vessel)
                .Where(dt => dt.DateLeft <= endDate && dt.DateArrived >= startDate)
                .ToListAsync(cancellationToken);

            foreach (var ticket in tickets.Where(t => t.CustomerId != 0))
            {
                ticket.Customer = (await _db.Customers
                    .FirstOrDefaultAsync(x => x.CustomerId == ticket.CustomerId, cancellationToken))!;
            }

            return tickets;
        }

        public async Task<IEnumerable<DispatchTicket>> GetAllDispatchTicketsWithDetailsAsync(CancellationToken cancellationToken = default)
        {
            var tickets = await dbSet
                .Include(a => a.Service)
                .Include(a => a.Terminal).ThenInclude(t => t.Port)
                .Include(a => a.Tugboat).ThenInclude(t => t.TugboatOwner)
                .Include(a => a.TugMaster)
                .Include(a => a.Vessel)
                .ToListAsync(cancellationToken);

            foreach (var ticket in tickets.Where(t => t.CustomerId != 0))
            {
                ticket.Customer = (await _db.Customers
                    .FirstOrDefaultAsync(x => x.CustomerId == ticket.CustomerId, cancellationToken))!;
            }

            return tickets;
        }

        public async Task<bool> IsJobOrderEditableAsync(int? jobOrderId, CancellationToken cancellationToken = default)
        {
            if (jobOrderId == null)
            {
                return true;
            }

            var jobOrder = await _db.MsapJobOrders.FindAsync(new object[] { jobOrderId.Value }, cancellationToken);
            return jobOrder?.Status == IBS.Utility.Constants.SD.JobOrderStatus.Open;
        }

        public async Task<(IEnumerable<DispatchTicket> Data, int RecordsFiltered, int TotalRecords)> GetPagedDispatchTicketsAsync(DataTablesParameters parameters, string filterType, CancellationToken cancellationToken = default)
        {
            var query = dbSet
                .Include(dt => dt.Service)
                .Include(dt => dt.Terminal).ThenInclude(dt => dt.Port)
                .Include(dt => dt.Tugboat)
                .Include(dt => dt.TugMaster)
                .Include(dt => dt.Vessel)
                .Include(dt => dt.Customer)
                .Where(dt => dt.Status != "For Posting" && dt.Status != "Cancelled" && dt.Status != "Incomplete");

            if (!string.IsNullOrEmpty(filterType))
            {
                query = filterType.ToLower() switch
                {
                    "for tariff" => query.Where(dt => dt.Status == "For Tariff"),
                    "for approval" => query.Where(dt => dt.Status == "For Approval"),
                    "disapproved" => query.Where(dt => dt.Status == "Disapproved"),
                    "for billing" => query.Where(dt => dt.Status == "For Billing"),
                    "billed" => query.Where(dt => dt.Status == "Billed"),
                    _ => query
                };
            }

            if (!string.IsNullOrEmpty(parameters.Search.Value))
            {
                var s = parameters.Search.Value.ToLower();
                query = query.Where(dt =>
                    (dt.COSNumber != null && dt.COSNumber.ToLower().Contains(s)) ||
                    dt.DispatchNumber.ToLower().Contains(s) ||
                    (dt.Service != null && dt.Service.ServiceName.ToLower().Contains(s)) ||
                    (dt.Tugboat != null && dt.Tugboat.TugboatName.ToLower().Contains(s)) ||
                    (dt.Customer != null && dt.Customer.CustomerName.ToLower().Contains(s)) ||
                    (dt.Vessel != null && dt.Vessel.VesselName.ToLower().Contains(s)) ||
                    dt.Status.ToLower().Contains(s));
            }

            var totalRecords = await dbSet.CountAsync(dt => dt.Status != "For Posting" && dt.Status != "Cancelled" && dt.Status != "Incomplete", cancellationToken);
            var recordsFiltered = await query.CountAsync(cancellationToken);

            if (parameters.Order?.Count > 0)
            {
                var col = parameters.Columns[parameters.Order[0].Column].Data;
                var dir = parameters.Order[0].Dir.ToLower() == "asc" ? "ascending" : "descending";
                query = query.OrderBy($"{col} {dir}");
            }

            var data = await query
                .Skip(parameters.Start)
                .Take(parameters.Length)
                .ToListAsync(cancellationToken);

            return (data, recordsFiltered, totalRecords);
        }
    }
}



