using System.Linq.Dynamic.Core;
using System.Linq.Expressions;
using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.MMSI.IRepository;
using IBS.Models;
using IBS.Models.MMSI;
using IBS.Utility.Helpers;
using Microsoft.EntityFrameworkCore;

namespace IBS.DataAccess.Repository.MMSI
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
                model.Customer = await _db.Customers
                    .FirstOrDefaultAsync(x => x.CustomerId == model.CustomerId, cancellationToken);
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
                model.Customer = await _db.Customers
                    .FirstOrDefaultAsync(x => x.CustomerId == model.CustomerId, cancellationToken);
            }

            return model;
        }

        public override async Task AddAsync(DispatchTicket entity, CancellationToken cancellationToken = default)
        {
            if (entity.JobOrderId.HasValue)
            {
                var jobOrder = await _db.MMSIJobOrders
                    .Include(j => j.Customer)
                    .Include(j => j.Vessel)
                    .Include(j => j.Port)
                    .Include(j => j.Terminal)
                    .FirstOrDefaultAsync(j => j.JobOrderId == entity.JobOrderId.Value, cancellationToken);

                if (jobOrder != null)
                {
                    entity.JobOrder = jobOrder;
                    entity.CustomerId = jobOrder.CustomerId;
                    entity.Customer = jobOrder.Customer!;
                    entity.VesselId = jobOrder.VesselId;
                    entity.Vessel = jobOrder.Vessel!;
                    entity.PortId = jobOrder.PortId;
                    entity.Port = jobOrder.Port!;
                    entity.TerminalId = jobOrder.TerminalId;
                    entity.Terminal = jobOrder.Terminal!;
                    entity.VoyageNumber = jobOrder.VoyageNumber;
                    entity.COSNumber = jobOrder.COSNumber;
                    entity.Date = jobOrder.Date;
                }
            }
            else if (entity.TerminalId != 0 && entity.Terminal == null)
            {
                entity.Terminal = (await _db.MMSITerminals
                    .Include(t => t.Port)
                    .FirstOrDefaultAsync(t => t.TerminalId == entity.TerminalId, cancellationToken))!;
                entity.PortId = entity.Terminal.PortId;
                entity.Port = entity.Terminal.Port!;
            }

            if (entity.CustomerId != 0 && entity.Customer == null)
            {
                entity.Customer = (await _db.Customers.FindAsync(new object[] { entity.CustomerId }, cancellationToken))!;
            }

            if (entity is { DateLeft: not null, DateArrived: not null, TimeLeft: not null, TimeArrived: not null })
            {
                var start = entity.DateLeft.Value.ToDateTime(entity.TimeLeft.Value);
                var end = entity.DateArrived.Value.ToDateTime(entity.TimeArrived.Value);

                if (end <= start)
                {
                    throw new InvalidOperationException("Arrival Date/Time must be strictly after Departure Date/Time.");
                }

                entity.Status = "For Tariff";
                var duration = (decimal)(end - start).TotalHours;
                entity.TotalHours = Math.Max(duration, 0.5m);
            }
            else
            {
                entity.Status = "Pending";
            }

            entity.CreatedBy = entity.CreatedBy;
            entity.CreatedDate = DateTimeHelper.GetCurrentPhilippineTime();

            await base.AddAsync(entity, cancellationToken);
        }

        public async Task<bool> IsJobOrderEditableAsync(int? jobOrderId, CancellationToken cancellationToken = default)
        {
            if (jobOrderId == null) return true;
            var jobOrder = await _db.MMSIJobOrders.FindAsync(new object[] { jobOrderId.Value }, cancellationToken);
            return jobOrder?.Status == "Open";
        }

        public async Task UpdateStatusAsync(int id, string status, string updatedBy, string activity, string docType, CancellationToken cancellationToken = default)
        {
            var model = await GetAsync(dt => dt.DispatchTicketId == id, cancellationToken)
                ?? throw new InvalidOperationException("Ticket not found.");

            model.Status = status;
            model.EditedBy = updatedBy;
            model.EditedDate = DateTimeHelper.GetCurrentPhilippineTime();

            await _db.AuditTrails.AddAsync(new AuditTrail(updatedBy, $"{activity} #{model.DispatchTicketId}", docType), cancellationToken);
            await SaveAsync(cancellationToken);
        }

        public async Task SaveTariffAsync(DispatchTicket model, string chargeType, string bafChargeType, string updatedBy, bool isEdit, CancellationToken cancellationToken = default)
        {
            var currentModel = await GetAsync(dt => dt.DispatchTicketId == model.DispatchTicketId, cancellationToken)
                ?? throw new InvalidOperationException("Ticket not found.");

            string auditMessage;
            if (isEdit)
            {
                var changes = new List<string>();
                if (currentModel.CustomerId != model.CustomerId) changes.Add($"CustomerId: {currentModel.CustomerId} -> {model.CustomerId}");
                if (currentModel.DispatchChargeType != chargeType) changes.Add($"DispatchChargeType: {currentModel.DispatchChargeType} -> {chargeType}");
                if (currentModel.BAFChargeType != bafChargeType) changes.Add($"BAFChargeType: {currentModel.BAFChargeType} -> {bafChargeType}");
                if (currentModel.DispatchRate != model.DispatchRate) changes.Add($"DispatchRate: {currentModel.DispatchRate} -> {model.DispatchRate}");
                if (currentModel.BAFRate != model.BAFRate) changes.Add($"BAFRate: {currentModel.BAFRate} -> {model.BAFRate}");
                if (currentModel.DispatchDiscount != model.DispatchDiscount) changes.Add($"DispatchDiscount: {currentModel.DispatchDiscount} -> {model.DispatchDiscount}");
                if (currentModel.BAFDiscount != model.BAFDiscount) changes.Add($"BAFDiscount: {currentModel.BAFDiscount} -> {model.BAFDiscount}");
                if (currentModel.DispatchBillingAmount != model.DispatchBillingAmount) changes.Add($"DispatchBillingAmount: {currentModel.DispatchBillingAmount} -> {model.DispatchBillingAmount}");
                if (currentModel.BAFBillingAmount != model.BAFBillingAmount) changes.Add($"BAFBillingAmount: {currentModel.BAFBillingAmount} -> {model.BAFBillingAmount}");
                if (currentModel.DispatchNetRevenue != model.DispatchNetRevenue) changes.Add($"DispatchNetRevenue: {currentModel.DispatchNetRevenue} -> {model.DispatchNetRevenue}");
                if (currentModel.BAFNetRevenue != model.BAFNetRevenue) changes.Add($"BAFNetRevenue: {currentModel.BAFNetRevenue} -> {model.BAFNetRevenue}");
                if (currentModel.ApOtherTugs != model.ApOtherTugs) changes.Add($"ApOtherTugs: {currentModel.ApOtherTugs} -> {model.ApOtherTugs}");
                if (currentModel.TotalBilling != model.TotalBilling) changes.Add($"TotalBilling: {currentModel.TotalBilling} -> {model.TotalBilling}");
                if (currentModel.TotalNetRevenue != model.TotalNetRevenue) changes.Add($"TotalNetRevenue: {currentModel.TotalNetRevenue} -> {model.TotalNetRevenue}");
                if (currentModel.ServiceId != model.ServiceId) changes.Add($"ServiceId: {currentModel.ServiceId} -> {model.ServiceId}");

                currentModel.TariffEditedBy = updatedBy;
                currentModel.TariffEditedDate = DateTimeHelper.GetCurrentPhilippineTime();
                auditMessage = changes.Any() ? $"Edit tariff #{currentModel.DispatchNumber} {string.Join(", ", changes)}" : $"No changes detected for tariff details #{currentModel.DispatchNumber}";
            }
            else
            {
                currentModel.TariffBy = updatedBy;
                currentModel.TariffDate = DateTimeHelper.GetCurrentPhilippineTime();
                auditMessage = $"Set Tariff #{currentModel.DispatchTicketId}";
            }

            currentModel.Status = "For Approval";
            currentModel.DispatchChargeType = chargeType;
            currentModel.BAFChargeType = bafChargeType;
            currentModel.DispatchRate = model.DispatchRate;
            currentModel.BAFRate = model.BAFRate;
            currentModel.DispatchDiscount = model.DispatchDiscount;
            currentModel.BAFDiscount = model.BAFDiscount;
            currentModel.DispatchBillingAmount = model.DispatchBillingAmount;
            currentModel.BAFBillingAmount = model.BAFBillingAmount;
            currentModel.DispatchNetRevenue = model.DispatchNetRevenue;
            currentModel.BAFNetRevenue = model.BAFNetRevenue;
            currentModel.ApOtherTugs = model.ApOtherTugs;
            currentModel.TotalBilling = model.TotalBilling;
            currentModel.TotalNetRevenue = model.TotalNetRevenue;

            await _db.AuditTrails.AddAsync(new AuditTrail(updatedBy, auditMessage, "Tariff"), cancellationToken);
            await SaveAsync(cancellationToken);
        }

        public async Task UpdateAsync(DispatchTicket entity, string updatedBy, CancellationToken cancellationToken = default)
        {
            var currentModel = await GetAsync(dt => dt.DispatchTicketId == entity.DispatchTicketId, cancellationToken)
                ?? throw new InvalidOperationException("Ticket not found.");

            // Capture original values for change tracking
            var originalTotalHours = currentModel.TotalHours;

            // Validate dates
            decimal? newTotalHours = null;
            if (entity.DateLeft != null && entity.DateArrived != null &&
                entity.TimeLeft != null && entity.TimeArrived != null)
            {
                var departure = entity.DateLeft.Value.ToDateTime(entity.TimeLeft.Value);
                var arrival = entity.DateArrived.Value.ToDateTime(entity.TimeArrived.Value);
                if (arrival <= departure)
                {
                    throw new InvalidOperationException("Date/Time Left cannot be later than Date/Time Arrived!");
                }

                newTotalHours = Math.Max((decimal)(arrival - departure).TotalHours, 0.5m);
                currentModel.TotalHours = newTotalHours.Value;
            }

            // Change tracking
            var changes = new List<string>();
            void AddChange(string name, object? oldVal, object? newVal)
            {
                if (!Equals(oldVal, newVal)) changes.Add($"{name}: '{oldVal}' -> '{newVal}'");
            }

            AddChange(nameof(entity.Date), currentModel.Date, entity.Date);
            AddChange(nameof(entity.DispatchNumber), currentModel.DispatchNumber, entity.DispatchNumber);
            AddChange(nameof(entity.COSNumber), currentModel.COSNumber, entity.COSNumber);
            AddChange(nameof(entity.VoyageNumber), currentModel.VoyageNumber, entity.VoyageNumber);
            AddChange(nameof(entity.CustomerId), currentModel.CustomerId, entity.CustomerId);
            AddChange(nameof(entity.DateLeft), currentModel.DateLeft, entity.DateLeft);
            AddChange(nameof(entity.TimeLeft), currentModel.TimeLeft, entity.TimeLeft);
            AddChange(nameof(entity.DateArrived), currentModel.DateArrived, entity.DateArrived);
            AddChange(nameof(entity.TimeArrived), currentModel.TimeArrived, entity.TimeArrived);
            AddChange("TotalHours", originalTotalHours, newTotalHours ?? originalTotalHours);
            AddChange(nameof(entity.TerminalId), currentModel.TerminalId, entity.TerminalId);
            AddChange(nameof(entity.PortId), currentModel.PortId, entity.PortId);
            AddChange(nameof(entity.ServiceId), currentModel.ServiceId, entity.ServiceId);
            AddChange(nameof(entity.TugBoatId), currentModel.TugBoatId, entity.TugBoatId);
            AddChange(nameof(entity.TugMasterId), currentModel.TugMasterId, entity.TugMasterId);
            AddChange(nameof(entity.VesselId), currentModel.VesselId, entity.VesselId);
            AddChange(nameof(entity.Remarks), currentModel.Remarks, entity.Remarks);

            // Business rule for company-owned tugs
            var tugboat = await _db.MMSITugboats.FindAsync(new object[] { entity.TugBoatId }, cancellationToken);
            if (currentModel.TugBoatId != entity.TugBoatId && tugboat?.IsCompanyOwned == true && currentModel.ApOtherTugs != 0)
            {
                changes.Add($"ApOtherTugs: '{currentModel.ApOtherTugs}' -> '0'");
                currentModel.ApOtherTugs = 0;
            }

            // Update fields
            currentModel.EditedBy       = updatedBy;
            currentModel.EditedDate     = DateTimeHelper.GetCurrentPhilippineTime();
            currentModel.Date           = entity.Date;
            currentModel.DispatchNumber = entity.DispatchNumber;
            currentModel.COSNumber      = entity.COSNumber;
            currentModel.VoyageNumber   = entity.VoyageNumber;
            currentModel.CustomerId     = entity.CustomerId;
            currentModel.DateLeft       = entity.DateLeft;
            currentModel.TimeLeft       = entity.TimeLeft;
            currentModel.DateArrived    = entity.DateArrived;
            currentModel.TimeArrived    = entity.TimeArrived;
            currentModel.TerminalId     = entity.TerminalId;
            currentModel.PortId         = entity.PortId;
            currentModel.ServiceId      = entity.ServiceId;
            currentModel.TugBoatId      = entity.TugBoatId;
            currentModel.TugMasterId    = entity.TugMasterId;
            currentModel.VesselId       = entity.VesselId;
            currentModel.Remarks        = entity.Remarks;
            currentModel.JobOrderId     = entity.JobOrderId;

            if (!string.IsNullOrEmpty(entity.ImageName))
            {
                AddChange(nameof(entity.ImageName), currentModel.ImageName, entity.ImageName);
                currentModel.ImageName = entity.ImageName;
                currentModel.ImageSavedUrl = entity.ImageSavedUrl;
            }
            if (!string.IsNullOrEmpty(entity.VideoName))
            {
                AddChange(nameof(entity.VideoName), currentModel.VideoName, entity.VideoName);
                currentModel.VideoName = entity.VideoName;
                currentModel.VideoSavedUrl = entity.VideoSavedUrl;
            }

            // Reset tariff state
            currentModel.Status                = "For Tariff";
            currentModel.DispatchRate          = 0;
            currentModel.DispatchBillingAmount = 0;
            currentModel.DispatchDiscount      = 0;
            currentModel.DispatchNetRevenue    = 0;
            currentModel.BAFRate               = 0;
            currentModel.BAFBillingAmount      = 0;
            currentModel.BAFDiscount           = 0;
            currentModel.BAFNetRevenue         = 0;
            currentModel.TotalBilling          = 0;
            currentModel.TotalNetRevenue       = 0;
            currentModel.ApOtherTugs           = 0;
            currentModel.TariffBy              = string.Empty;
            currentModel.TariffDate            = default;
            currentModel.TariffEditedBy        = string.Empty;
            currentModel.TariffEditedDate      = null;

            await _db.AuditTrails.AddAsync(
                new AuditTrail(
                    updatedBy,
                    changes.Any()
                        ? $"Edit dispatch ticket #{currentModel.DispatchNumber}, {string.Join(", ", changes)}"
                        : $"No changes detected for #{currentModel.DispatchNumber}",
                    "Dispatch Ticket"),
                cancellationToken);

            await SaveAsync(cancellationToken);
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
