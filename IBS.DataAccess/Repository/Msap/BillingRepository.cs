using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.Msap.IRepository;
using IBS.Models;
using IBS.Models.MSAP;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using System.Linq.Dynamic.Core;

namespace IBS.DataAccess.Repository.Msap
{
    public class BillingRepository(ApplicationDbContext db): Repository<Billing>(db), IBillingRepository
    {
        private readonly ApplicationDbContext _db = db;

        public async Task SaveAsync(CancellationToken cancellationToken)
        {
            await _db.SaveChangesAsync(cancellationToken);
        }

        public override async Task<IEnumerable<Billing>> GetAllAsync(Expression<Func<Billing, bool>>? filter, CancellationToken cancellationToken = default)
        {
            IQueryable<Billing> query = dbSet
                .Include(a => a.Terminal)
                .Include(a => a.Vessel)
                .Include(a => a.Customer)
                .Include(a => a.Port);

            if (filter != null)
            {
                query = query.Where(filter);
            }

            return await query.ToListAsync(cancellationToken);
        }

        public override async Task<Billing?> GetAsync(Expression<Func<Billing, bool>>? filter, CancellationToken cancellationToken = default)
        {
            IQueryable<Billing> query = dbSet
                .Include(b => b.Terminal)
                .ThenInclude(t => t.Port)
                .Include(b => b.Vessel)
                .Include(b => b.Customer)
                .Include(b => b.Principal)
                .Include(b => b.JobOrder);

            if (filter != null)
            {
                query = query.Where(filter);
            }

            return await query.FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<List<string>?> GetToBillDispatchTicketListAsync(int billingId, CancellationToken cancellationToken = default)
        {
            return await _db.MsapDispatchTickets.Where(t => t.BillingId == billingId)
                .Select(d => d.DispatchTicketId.ToString()).ToListAsync(cancellationToken);
        }

        public async Task<List<string>?> GetUniqueTugboatsListAsync(int billingId, CancellationToken cancellationToken = default)
        {
            return await _db.MsapDispatchTickets
                .Where(dt => dt.BillingId == billingId)
                .Select(dt => dt.Tugboat.TugboatName.ToString())
                .Distinct() // Ensures unique values
                .ToListAsync(cancellationToken);
        }

        public async Task<List<DispatchTicket>?> GetPaidDispatchTicketsAsync(int billingId, CancellationToken cancellationToken = default)
        {
            return await _db.MsapDispatchTickets
                .Where(dt => dt.BillingId == billingId)
                .Include(a => a.Service)
                .Include(a => a.Terminal).ThenInclude(t => t.Port)
                .Include(a => a.Tugboat)
                .Include(a => a.TugMaster)
                .Include(a => a.Vessel)
                .OrderBy(dt => dt.DateLeft).ThenBy(dt => dt.TimeLeft)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<SelectListItem>> GetMsapTerminalsByPortId(int portId, CancellationToken cancellationToken)
        {
            var terminals = await _db
                .MsapTerminals
                .Where(t => t.PortId == portId)
                .OrderBy(t => t.TerminalName)
                .ToListAsync(cancellationToken);

            var terminalsList = terminals.Select(t => new SelectListItem
            {
                Value = t.TerminalId.ToString(),
                Text = t.TerminalName
            }).ToList();

            return terminalsList;
        }

        public async Task<List<SelectListItem>?> GetMsapCustomersWithBillablesSelectList(int? currentCustomerId, string type, CancellationToken cancellationToken = default)
        {
            var dispatchToBeBilled = await _db.MsapDispatchTickets
                .Where(t => t.Status == Utility.Constants.SD.DispatchTicketStatus.ForBilling || (currentCustomerId.GetValueOrDefault() != 0 && t.CustomerId == currentCustomerId))
                .Include(t => t.Customer)
                .ToListAsync(cancellationToken);

            var listOfCustomerWithBillableTickets = dispatchToBeBilled
                .Where(t => t.Customer != null)
                .Select(t => t.Customer.CustomerId)
                .Distinct()
                .ToList();

            return await _db.Customers
                .Where(c => listOfCustomerWithBillableTickets.Contains(c.CustomerId) &&
                            (string.IsNullOrEmpty(type) || c.Type == type))
                .OrderBy(s => s.CustomerName)
                .Select(s => new SelectListItem
                {
                    Value = s.CustomerId.ToString(),
                    Text = s.CustomerName
                }).ToListAsync(cancellationToken);
        }

        public async Task<List<SelectListItem>> GetMsapUnbilledTicketsById(string type, CancellationToken cancellationToken = default)
        {
            var dispatchTicketList = await _db.MsapDispatchTickets
                .Where(dt => dt.Status == Utility.Constants.SD.DispatchTicketStatus.ForBilling)
                .OrderBy(dt => dt.DispatchNumber)
                .Select(s => new SelectListItem
                {
                    Value = s.DispatchTicketId.ToString(),
                    Text = s.DispatchNumber
                }).ToListAsync(cancellationToken);

            return dispatchTicketList;
        }

        public async Task<List<SelectListItem>?> GetMsapUnbilledTicketsByCustomer(int? customerId, CancellationToken cancellationToken)
        {
            var tickets = await _db
                .MsapDispatchTickets
                .Where(b => b.CustomerId == customerId && b.Status == Utility.Constants.SD.DispatchTicketStatus.ForBilling)
                .Include(b => b.Customer)
                .OrderBy(b => b.DispatchNumber)
                .ToListAsync(cancellationToken);

            var ticketsList = tickets.Select(b => new SelectListItem
            {
                Value = b.DispatchTicketId.ToString(),
                Text = b.DispatchNumber
            }).ToList();

            return ticketsList;
        }

        public async Task<List<SelectListItem>> GetMsapBilledTicketsById(int id, CancellationToken cancellationToken = default)
        {
            var dispatchTicketList = await _db.MsapDispatchTickets
                .Where(dt => dt.BillingId == id)
                .OrderBy(dt => dt.DispatchNumber).Select(s => new SelectListItem
                {
                    Value = s.DispatchTicketId.ToString(),
                    Text = s.DispatchNumber
                }).ToListAsync(cancellationToken);

            return dispatchTicketList;
        }

        public async Task<string> GenerateBillingNumber(CancellationToken cancellationToken = default)
        {
            // Get the highest BL-prefixed billing number across all billings
            var lastRecord = await _db.MsapBillings
                .Where(b => !string.IsNullOrEmpty(b.MsapBillingNumber) && b.MsapBillingNumber.StartsWith("BL"))
                .OrderByDescending(b => b.MsapBillingNumber)
                .FirstOrDefaultAsync(cancellationToken);

            if (lastRecord == null)
            {
                return "BL00000001";
            }

            var lastSeries = lastRecord.MsapBillingNumber.Substring(2); // "BL" is 2 chars
            if (int.TryParse(lastSeries, out int lastNumber))
            {
                return "BL" + ((lastNumber + 1).ToString("D8"));
            }

            // Fallback if parsing fails
            return "BL" + (DateTime.Now.Ticks % 100000000).ToString("D8");
        }

        public Billing ProcessAddress(Billing model, CancellationToken cancellationToken = default)
        {
            // ... (rest of ProcessAddress implementation)
            var words = model.PrincipalId != null
                ? model.Principal?.Address1.Split(' ')
                : model.Customer?.CustomerAddress.Split(' ');
            var resultStrings = new List<string>();
            var currentString = "";

            if (words == null)
            {
                model.AddressLine1 = string.Empty;
                model.AddressLine2 = string.Empty;
                model.AddressLine3 = string.Empty;
                model.AddressLine4 = string.Empty;

                return model;
            }
            foreach (var word in words)
            {
                if (currentString.Length + word.Length + (currentString.Length > 0 ? 1 : 0) > 40)
                {
                    if (currentString.Length > 0)
                    {
                        resultStrings.Add(currentString.Trim());
                    }
                    currentString = word;
                }
                else
                {
                    currentString += (currentString.Length > 0 ? " " : "") + word;
                }
            }

            if (currentString.Length > 0)
            {
                resultStrings.Add(currentString.Trim());
            }
            var firstString = resultStrings.Count > 0 ? resultStrings[0] : "";
            var secondString = resultStrings.Count > 1 ? resultStrings[1] : "";
            var thirdString = resultStrings.Count > 2 ? resultStrings[2] : "";
            var fourthString = resultStrings.Count > 3 ? resultStrings[3] : "";

            model.AddressLine1 = firstString;
            model.AddressLine2 = secondString;
            model.AddressLine3 = thirdString;
            model.AddressLine4 = fourthString;

            return model;
        }

        public async Task<(IEnumerable<Billing> Data, int RecordsFiltered, int TotalRecords)> GetPagedBillingsAsync(DataTablesParameters parameters, CancellationToken cancellationToken)
        {
            IQueryable<Billing> query = dbSet
                .Include(b => b.Customer)
                .Include(b => b.Terminal).ThenInclude(b => b.Port)
                .Include(b => b.Vessel);

            if (!string.IsNullOrEmpty(parameters.Search.Value))
            {
                var s = parameters.Search.Value.ToLower();
                query = query.Where(dt =>
                    dt.MsapBillingNumber.ToLower().Contains(s) ||
                    dt.Customer.CustomerName.ToLower().Contains(s) ||
                    dt.Vessel.VesselName.ToLower().Contains(s) ||
                    dt.Status.ToLower().Contains(s)
                );
            }

            // Column-specific search
            if (parameters.Columns != null)
            {
                foreach (var column in parameters.Columns)
                {
                    if (column.Search?.Value is { Length: > 0 } searchValue)
                    {
                        if (column.Data == "status")
                        {
                            query = query.Where(b => b.Status.ToLower() == searchValue);
                        }
                    }
                }
            }

            var totalRecords = await dbSet.CountAsync(cancellationToken);
            var recordsFiltered = await query.CountAsync(cancellationToken);

            if (parameters.Order?.Count > 0 && parameters.Columns != null)
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

        public async Task RemoveSalesBookEntryAsync(int documentId, string serialNo, CancellationToken cancellationToken)
        {
            var salesBook = await _db.SalesBooks.FirstOrDefaultAsync(s => s.DocumentId == documentId && s.SerialNo == serialNo, cancellationToken);
            if (salesBook != null)
            {
                _db.SalesBooks.Remove(salesBook);
            }
        }

        public async Task<List<Billing>> GetBillingsByCollectionIdAsync(int collectionId, CancellationToken cancellationToken)
        {
            return await _db.MsapBillings
                .Where(b => b.CollectionId == collectionId)
                .ToListAsync(cancellationToken);
        }

        public async Task AddSalesBookAsync(Models.Books.SalesBook salesBook, CancellationToken cancellationToken)
        {
            await _db.SalesBooks.AddAsync(salesBook, cancellationToken);
        }

        public async Task AddGeneralLedgerEntriesAsync(List<Models.Books.GeneralLedgerBook> ledgers, CancellationToken cancellationToken)
        {
            await _db.GeneralLedgerBooks.AddRangeAsync(ledgers, cancellationToken);
        }
    }
}



