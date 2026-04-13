using IBS.Models.Books;
using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.MMSI.IRepository;
using IBS.Models.MMSI;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace IBS.DataAccess.Repository.MMSI
{
    public class BillingRepository(ApplicationDbContext db): Repository<Billing>(db), IBillingRepository
    {
        private readonly ApplicationDbContext _db = db;

        public async Task SaveAsync(CancellationToken cancellationToken)
        {
            await _db.SaveChangesAsync(cancellationToken);
        }

        public async Task PostAsync(Billing billing, CancellationToken cancellationToken = default)
        {
            #region --Sales Book Recording

            var salesBook = new SalesBook
            {
                TransactionDate = billing.Date,
                SerialNo = billing.MMSIBillingNumber!,
                SoldTo = (billing.PrincipalId != null ? billing.Principal?.PrincipalName : billing.Customer?.CustomerName) ?? string.Empty,
                TinNo = (billing.PrincipalId != null ? billing.Principal?.TIN : billing.Customer?.CustomerTin) ?? string.Empty,
                Address = (billing.PrincipalId != null ? billing.Principal?.Address : billing.Customer?.CustomerAddress) ?? string.Empty,
                Description = billing.Vessel?.VesselName ?? "Maritime Services",
                Amount = billing.Amount - billing.Discount
            };

            if (billing.IsVatable)
            {
                salesBook.VatableSales = ComputeNetOfVat(salesBook.Amount);
                salesBook.VatAmount = ComputeVatAmount(salesBook.VatableSales);
                salesBook.NetSales = salesBook.VatableSales - salesBook.Discount;
            }
            else
            {
                salesBook.ZeroRated = salesBook.Amount;
                salesBook.NetSales = salesBook.ZeroRated - salesBook.Discount;
            }

            salesBook.Discount = billing.Discount;
            salesBook.CreatedBy = billing.CreatedBy;
            salesBook.CreatedDate = billing.CreatedDate;
            salesBook.DueDate = billing.DueDate;
            salesBook.DocumentId = billing.MMSIBillingId;
            salesBook.Company = billing.Company;

            await _db.SalesBooks.AddAsync(salesBook, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);

            #endregion --Sales Book Recording
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
                .ThenInclude(t => t!.Port)
                .Include(b => b.Vessel)
                .Include(b => b.Customer)
                .Include(b => b.Principal);

            if (filter != null)
            {
                query = query.Where(filter);
            }

            return await query.FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<List<string>?> GetToBillDispatchTicketListAsync(int billingId, CancellationToken cancellationToken = default)
        {
            return await _db.MMSIDispatchTickets.Where(t => t.BillingId == billingId)
                .Select(d => d.DispatchTicketId.ToString()).ToListAsync(cancellationToken);
        }

        public async Task<List<string>?> GetUniqueTugboatsListAsync(int billingId, CancellationToken cancellationToken = default)
        {
            return await _db.MMSIDispatchTickets
                .Where(dt => dt.BillingId == billingId)
                .Select(dt => dt.Tugboat!.TugboatName.ToString())
                .Distinct() // Ensures unique values
                .ToListAsync(cancellationToken);
        }

        public async Task<List<DispatchTicket>?> GetPaidDispatchTicketsAsync(int billingId, CancellationToken cancellationToken = default)
        {
            return await _db.MMSIDispatchTickets
                .Where(dt => dt.BillingId == billingId)
                .Include(a => a.Service)
                .Include(a => a.Terminal).ThenInclude(t => t!.Port)
                .Include(a => a.Tugboat)
                .Include(a => a.TugMaster)
                .Include(a => a.Vessel)
                .OrderBy(dt => dt.DateLeft).ThenBy(dt => dt.TimeLeft)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<SelectListItem>> GetMMSITerminalsByPortId(int portId, CancellationToken cancellationToken)
        {
            var terminals = await _db
                .MMSITerminals
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

        public async Task<List<SelectListItem>?> GetMMSICustomersWithBillablesSelectList(int? currentCustomerId, string type, CancellationToken cancellationToken = default)
        {
            var dispatchToBeBilled = await _db.MMSIDispatchTickets
                .Where(t => t.Status == "For Billing" || (currentCustomerId != 0 && t.CustomerId == currentCustomerId))
                .Include(t => t.Customer)
                .ToListAsync(cancellationToken);

            var listOfCustomerWithBillableTickets = dispatchToBeBilled
                .Select(t => t.Customer!.CustomerId)
                .Distinct()
                .ToList();

            return await _db.Customers
                .Where(c => c.IsMMSI == true && listOfCustomerWithBillableTickets.Contains(c.CustomerId) &&
                            (string.IsNullOrEmpty(type) || c.Type == type))
                .OrderBy(s => s.CustomerName)
                .Select(s => new SelectListItem
                {
                    Value = s.CustomerId.ToString(),
                    Text = s.CustomerName
                }).ToListAsync(cancellationToken);
        }

        public async Task<List<SelectListItem>> GetMMSIUnbilledTicketsById(string type, CancellationToken cancellationToken = default)
        {
            var dispatchTicketList = await _db.MMSIDispatchTickets
                .Where(dt => dt.Status == "For Billing")
                .OrderBy(dt => dt.DispatchNumber)
                .Select(s => new SelectListItem
                {
                    Value = s.DispatchTicketId.ToString(),
                    Text = s.DispatchNumber
                }).ToListAsync(cancellationToken);

            return dispatchTicketList;
        }

        public async Task<List<SelectListItem>?> GetMMSIUnbilledTicketsByCustomer(int? customerId, CancellationToken cancellationToken)
        {
            var tickets = await _db
                .MMSIDispatchTickets
                .Where(b => b.CustomerId == customerId && b.Status == "For Billing")
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

        public async Task<List<SelectListItem>> GetMMSIBilledTicketsById(int id, CancellationToken cancellationToken = default)
        {
            var dispatchTicketList = await _db.MMSIDispatchTickets
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
            var lastRecord = await _db.MMSIBillings
                .Where(b => !string.IsNullOrEmpty(b.MMSIBillingNumber) && b.MMSIBillingNumber.StartsWith("BL"))
                .OrderByDescending(b => b.MMSIBillingNumber)
                .FirstOrDefaultAsync(cancellationToken);

            if (lastRecord == null)
            {
                return "BL00000001";
            }

            var lastSeries = lastRecord.MMSIBillingNumber.Substring(2); // "BL" is 2 chars
            if (int.TryParse(lastSeries, out int lastNumber))
            {
                return "BL" + ((lastNumber + 1).ToString("D8"));
            }

            // Fallback if parsing fails
            return "BL" + (DateTime.Now.Ticks % 100000000).ToString("D8");
        }

        public Billing ProcessAddress(Billing model, CancellationToken cancellationToken = default)
        {
            // Splitting the address for the view
            var words = model.PrincipalId != null
                ? model.Principal?.Address.Split(' ')
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
            foreach (var word in words!)
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
    }
}
