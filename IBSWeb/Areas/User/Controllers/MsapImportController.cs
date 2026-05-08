using IBS.Models.MasterFile;
using CsvHelper;
using CsvHelper.Configuration;
using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models.MMSI;
using IBS.Models.MMSI.MasterFile;
using IBS.Utility.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Security.Claims;
using System.Text;
using IBS.Models.Enums;
using IBS.Services.Attributes;

namespace IBSWeb.Areas.User.Controllers
{
    [Area("User")]
    public class MsapImportController(
        IUnitOfWork unitOfWork,
        ApplicationDbContext dbContext)
        : Controller
    {
        private readonly CsvConfiguration _csvConfig = new(CultureInfo.InvariantCulture)
        {
            PrepareHeaderForMatch = args => args.Header.ToLower(),
        };

        private string GetUserFullName()
        {
            return User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.GivenName)?.Value
                    ?? User.Identity?.Name
                    ?? "Unknown";
        }

        [RequireAnyAccess("You do not have permission to import Msap data.", ProcedureEnum.ManageMsapImport)]
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [RequireAnyAccess("You do not have permission to import Msap data.", ProcedureEnum.ManageMsapImport)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(
            IFormFile? customerFile,
            IFormFile? portFile,
            IFormFile? terminalFile,
            IFormFile? principalFile,
            IFormFile? serviceFile,
            IFormFile? tugboatOwnerFile,
            IFormFile? tugboatFile,
            IFormFile? tugMasterFile,
            IFormFile? vesselFile,
            IFormFile? tariffFile,
            IFormFile? dispatchTicketFile,
            IFormFile? billingFile,
            IFormFile? collectionFile,
            List<IFormFile>? bulkFiles)
        {
            var sb = new StringBuilder();
            await using var transaction = await dbContext.Database.BeginTransactionAsync();

            try
            {
                // Helper to resolve files from bulk list if individual parameter is null
                IFormFile? GetFile(IFormFile? individual, string keyword)
                {
                    if (individual != null) return individual;
                    if (bulkFiles == null || bulkFiles.Count == 0) return null;

                    return bulkFiles.FirstOrDefault(f =>
                        f.FileName.Equals($"{keyword}.csv", StringComparison.OrdinalIgnoreCase) ||
                        f.FileName.Contains(keyword, StringComparison.OrdinalIgnoreCase));
                }

                // Resolve all files
                customerFile = GetFile(customerFile, "customer");
                portFile = GetFile(portFile, "port");
                terminalFile = GetFile(terminalFile, "terminal");
                principalFile = GetFile(principalFile, "principal");
                serviceFile = GetFile(serviceFile, "service");
                tugboatOwnerFile = GetFile(tugboatOwnerFile, "tugboatowner");
                // Special case for tugboat to not match tugboatowner
                tugboatFile = tugboatFile ?? bulkFiles?.FirstOrDefault(f =>
                    (f.FileName.Equals("tugboat.csv", StringComparison.OrdinalIgnoreCase) || f.FileName.Contains("tugboat", StringComparison.OrdinalIgnoreCase)) &&
                    !f.FileName.Contains("owner", StringComparison.OrdinalIgnoreCase));

                tugMasterFile = GetFile(tugMasterFile, "tugmaster") ?? GetFile(tugMasterFile, "master");
                vesselFile = GetFile(vesselFile, "vessel");
                tariffFile = GetFile(tariffFile, "tariff");
                dispatchTicketFile = GetFile(dispatchTicketFile, "dispatch");
                billingFile = GetFile(billingFile, "billing");
                collectionFile = GetFile(collectionFile, "collection");

                // Maps for dependency resolution
                var customerMap = new Dictionary<string, int>(); // MsapNumber -> IbsCustomerId
                var portMap = new Dictionary<string, int>();     // MsapNumber -> IbsPortId
                var serviceMap = new Dictionary<string, int>();  // MsapNumber -> IbsServiceId
                var ownerMap = new Dictionary<string, int>();    // MsapNumber -> IbsOwnerId
                var tugboatMap = new Dictionary<string, int>();  // MsapNumber -> IbsTugboatId
                var tugMasterMap = new Dictionary<string, int>();// MsapNumber -> IbsTugMasterId
                var vesselMap = new Dictionary<string, int>();   // MsapNumber -> IbsVesselId
                var terminalMap = new Dictionary<string, int>(); // PortNum|TermNum -> IbsTerminalId
                var principalMap = new Dictionary<string, int>();// MsapNumber -> IbsPrincipalId
                var collectionMap = new Dictionary<string, int>();// MsapNumber -> IbsCollectionId
                var billingMap = new Dictionary<string, int>();  // MsapNumber -> IbsBillingId

                // Pre-load maps from existing database records to support partial uploads
                await LoadMsapMapsAsync(customerMap, portMap, serviceMap, ownerMap, tugboatMap, tugMasterMap, vesselMap, terminalMap, principalMap, collectionMap, billingMap, CancellationToken.None);

                var dummy = await GetOrCreateDummyIdsAsync(CancellationToken.None);

                // Level 1: Independent
                if (customerFile != null)
                {
                    var (res, map) = await ImportMsapCustomers(customerFile, CancellationToken.None);
                    sb.AppendLine(res);
                    foreach(var entry in map) customerMap[entry.Key] = entry.Value;
                }

                if (portFile != null)
                {
                    var (res, map) = await ImportMsapPorts(portFile, CancellationToken.None);
                    sb.AppendLine(res);
                    portMap = map;
                }

                if (serviceFile != null)
                {
                    var (res, map) = await ImportMsapServices(serviceFile, CancellationToken.None);
                    sb.AppendLine(res);
                    serviceMap = map;
                }

                if (tugboatOwnerFile != null)
                {
                    var (res, map) = await ImportMsapTugboatOwners(tugboatOwnerFile, CancellationToken.None);
                    sb.AppendLine(res);
                    ownerMap = map;
                }

                if (tugMasterFile != null)
                {
                    var (res, map) = await ImportMsapTugMasters(tugMasterFile, CancellationToken.None);
                    sb.AppendLine(res);
                    tugMasterMap = map;
                }

                if (vesselFile != null)
                {
                    var (res, map) = await ImportMsapVessels(vesselFile, CancellationToken.None);
                    sb.AppendLine(res);
                    vesselMap = map;
                }

                // Level 2: Single Dependency
                if (terminalFile != null)
                {
                    var (res, map) = await ImportMsapTerminals(terminalFile, portMap, dummy, CancellationToken.None);
                    sb.AppendLine(res);
                    terminalMap = map;
                }

                if (tugboatFile != null)
                {
                    var (res, map) = await ImportMsapTugboats(tugboatFile, ownerMap, CancellationToken.None);
                    sb.AppendLine(res);
                    tugboatMap = map;
                }

                if (principalFile != null)
                {
                    var (res, map) = await ImportMsapPrincipals(principalFile, customerMap, dummy, CancellationToken.None);
                    sb.AppendLine(res);
                    principalMap = map;
                }

                // Level 3: Mixed Dependencies
                if (tariffFile != null)
                {
                    sb.AppendLine(await ImportMsapTariffRates(tariffFile, customerMap, terminalMap, serviceMap, dummy, CancellationToken.None));
                }

                if (collectionFile != null)
                {
                    var (res, map) = await ImportMsapCollections(collectionFile, customerMap, dummy, CancellationToken.None);
                    sb.AppendLine(res);
                    collectionMap = map;
                }

                // Level 4: Transactional
                if (billingFile != null)
                {
                    var (res, map) = await ImportMsapBillings(billingFile, customerMap, vesselMap, portMap, terminalMap, principalMap, collectionMap, dummy, CancellationToken.None);
                    sb.AppendLine(res);
                    billingMap = map;
                }

                // Level 5: Final
                if (dispatchTicketFile != null)
                {
                    sb.AppendLine(await ProcessDispatchTickets(dispatchTicketFile, customerMap, vesselMap, tugboatMap, tugMasterMap, serviceMap, terminalMap, portMap, billingMap, dummy, CancellationToken.None));
                }

                if (sb.Length == 0)
                {
                    TempData["error"] = "Please upload at least one CSV file.";
                    return RedirectToAction(nameof(Index));
                }

                await transaction.CommitAsync();
                TempData["success"] = sb.ToString().Replace(Environment.NewLine, "\\n");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                TempData["error"] = ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        #region -- Helpers --

        public class DummyIds
        {
            public int CustomerId { get; set; }
            public int PortId { get; set; }
            public int ServiceId { get; set; }
            public int OwnerId { get; set; }
            public int TugboatId { get; set; }
            public int TugMasterId { get; set; }
            public int VesselId { get; set; }
            public int TerminalId { get; set; }
            public int PrincipalId { get; set; }
        }

        private async Task<DummyIds> GetOrCreateDummyIdsAsync(CancellationToken ct)
        {
            var dummy = new DummyIds();

            // 1. Customer
            var customer = await dbContext.Customers.FirstOrDefaultAsync(c => c.CustomerName == "[SYSTEM] N/A", ct);
            if (customer == null)
            {
                customer = new Customer
                {
                    CustomerCode = "NA00",
                    CustomerName = "[SYSTEM] N/A",
                    CustomerAddress = "-",
                    CustomerTin = "000-000-000-00000",
                    CustomerTerms = "COD",
                    CustomerType = "Industrial",
                    ZipCode = "0000",
                    Type = "Documented",
                    Company = "MMSI",
                    CreatedBy = "SYSTEM",
                    CreatedDate = DateTimeHelper.GetCurrentPhilippineTime(),
                    IsActive = true,
                    VatType = "Zero-Rated"
                };
                dbContext.Customers.Add(customer);
                await dbContext.SaveChangesAsync(ct);
            }
            dummy.CustomerId = customer.CustomerId;

            // 2. Port
            var port = await dbContext.MMSIPorts.FirstOrDefaultAsync(p => p.PortNumber == "000", ct);
            if (port == null)
            {
                port = new Port { PortNumber = "000", PortName = "[SYSTEM] N/A" };
                dbContext.MMSIPorts.Add(port);
                await dbContext.SaveChangesAsync(ct);
            }
            dummy.PortId = port.PortId;

            // 3. Service
            var service = await dbContext.MMSIServices.FirstOrDefaultAsync(s => s.ServiceNumber == "000", ct);
            if (service == null)
            {
                service = new Service { ServiceNumber = "000", ServiceName = "[SYSTEM] N/A" };
                dbContext.MMSIServices.Add(service);
                await dbContext.SaveChangesAsync(ct);
            }
            dummy.ServiceId = service.ServiceId;

            // 4. Owner
            var owner = await dbContext.MMSITugboatOwners.FirstOrDefaultAsync(o => o.TugboatOwnerNumber == "000", ct);
            if (owner == null)
            {
                owner = new TugboatOwner { TugboatOwnerNumber = "000", TugboatOwnerName = "[SYSTEM] N/A" };
                dbContext.MMSITugboatOwners.Add(owner);
                await dbContext.SaveChangesAsync(ct);
            }
            dummy.OwnerId = owner.TugboatOwnerId;

            // 5. Tugboat
            var tugboat = await dbContext.MMSITugboats.FirstOrDefaultAsync(t => t.TugboatNumber == "000", ct);
            if (tugboat == null)
            {
                tugboat = new Tugboat
                {
                    TugboatNumber = "000",
                    TugboatName = "[SYSTEM] N/A",
                    TugboatOwnerId = dummy.OwnerId,
                    IsCompanyOwned = true
                };
                dbContext.MMSITugboats.Add(tugboat);
                await dbContext.SaveChangesAsync(ct);
            }
            dummy.TugboatId = tugboat.TugboatId;

            // 6. TugMaster
            var master = await dbContext.MMSITugMasters.FirstOrDefaultAsync(m => m.TugMasterNumber == "000", ct);
            if (master == null)
            {
                master = new TugMaster { TugMasterNumber = "000", TugMasterName = "[SYSTEM] N/A", IsActive = true };
                dbContext.MMSITugMasters.Add(master);
                await dbContext.SaveChangesAsync(ct);
            }
            dummy.TugMasterId = master.TugMasterId;

            // 7. Vessel
            var vessel = await dbContext.MMSIVessels.FirstOrDefaultAsync(v => v.VesselNumber == "0000", ct);
            if (vessel == null)
            {
                vessel = new Vessel { VesselNumber = "0000", VesselName = "[SYSTEM] N/A", VesselType = "LOCAL" };
                dbContext.MMSIVessels.Add(vessel);
                await dbContext.SaveChangesAsync(ct);
            }
            dummy.VesselId = vessel.VesselId;

            // 8. Terminal
            var terminal = await dbContext.MMSITerminals.FirstOrDefaultAsync(t => t.PortId == dummy.PortId && t.TerminalNumber == "000", ct);
            if (terminal == null)
            {
                terminal = new Terminal { PortId = dummy.PortId, TerminalNumber = "000", TerminalName = "[SYSTEM] N/A" };
                dbContext.MMSITerminals.Add(terminal);
                await dbContext.SaveChangesAsync(ct);
            }
            dummy.TerminalId = terminal.TerminalId;

            // 9. Principal
            var principal = await dbContext.MMSIPrincipals.FirstOrDefaultAsync(p => p.CustomerId == dummy.CustomerId && p.PrincipalNumber == "0000", ct);
            if (principal == null)
            {
                principal = new Principal
                {
                    CustomerId = dummy.CustomerId,
                    PrincipalNumber = "0000",
                    PrincipalName = "[SYSTEM] N/A",
                    Address = "-",
                    TIN = "000-000-000000",
                    IsActive = true
                };
                dbContext.MMSIPrincipals.Add(principal);
                await dbContext.SaveChangesAsync(ct);
            }
            dummy.PrincipalId = principal.PrincipalId;

            return dummy;
        }

        private static string? GetString(dynamic record, string propertyName)
        {
            var dict = (IDictionary<string, object>)record;
            if (dict.TryGetValue(propertyName, out var value))
            {
                return value.ToString();
            }
            return null;
        }

        private static bool ParseBool(dynamic record, string propertyName)
        {
            string? val = GetString(record, propertyName);
            if (string.IsNullOrEmpty(val))
            {
                return false;
            }

            val = val.Trim().ToLower();
            return val == "t" || val == "true";
        }

        private static decimal ParseDecimal(dynamic record, string propertyName)
        {
            string? val = GetString(record, propertyName);
            if (string.IsNullOrEmpty(val))
            {
                return 0;
            }

            if (decimal.TryParse(val, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal result))
            {
                return result;
            }
            return 0;
        }

        private static DateOnly? ParseDateOnly(dynamic record, string propertyName)
        {
            string? val = GetString(record, propertyName);
            if (string.IsNullOrEmpty(val))
            {
                return null;
            }

            if (DateOnly.TryParse(val, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly result))
            {
                return result;
            }
            return null;
        }

        private static DateTime? ParseDateTime(dynamic record, string propertyName)
        {
            string? val = GetString(record, propertyName);
            if (string.IsNullOrEmpty(val))
            {
                return null;
            }

            if (DateTime.TryParse(val, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime result))
            {
                return result;
            }
            return null;
        }

        private static TimeOnly? ParseTimeOnly(dynamic record, string propertyName)
        {
            string? val = GetString(record, propertyName);
            if (string.IsNullOrEmpty(val))
            {
                return null;
            }

            val = val.Trim();

            // Handle legacy format like "330" (03:30) or "1915" (19:15)
            if (int.TryParse(val, out int timeInt) && val.Length <= 4)
            {
                int hours = 0;
                int minutes = 0;

                if (val.Length == 4)
                {
                    hours = int.Parse(val.Substring(0, 2));
                    minutes = int.Parse(val.Substring(2, 2));
                }
                else if (val.Length == 3)
                {
                    hours = int.Parse(val.Substring(0, 1));
                    minutes = int.Parse(val.Substring(1, 2));
                }
                else if (val.Length <= 2)
                {
                    hours = 0;
                    minutes = int.Parse(val);
                }

                if (hours >= 0 && hours < 24 && minutes >= 0 && minutes < 60)
                {
                    return new TimeOnly(hours, minutes);
                }
            }

            if (TimeOnly.TryParse(val, CultureInfo.InvariantCulture, DateTimeStyles.None, out TimeOnly result))
            {
                return result;
            }
            return null;
        }

        private static string PadNumber(string? value, int width)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            return int.TryParse(value, out var n)
                ? n.ToString($"D{width}")
                : value.PadLeft(width, '0');
        }

        private async Task LoadMsapMapsAsync(
            Dictionary<string, int> customerMap,
            Dictionary<string, int> portMap,
            Dictionary<string, int> serviceMap,
            Dictionary<string, int> ownerMap,
            Dictionary<string, int> tugboatMap,
            Dictionary<string, int> tugMasterMap,
            Dictionary<string, int> vesselMap,
            Dictionary<string, int> terminalMap,
            Dictionary<string, int> principalMap,
            Dictionary<string, int> collectionMap,
            Dictionary<string, int> billingMap,
            CancellationToken cancellationToken)
        {
            // Note: Since MSAP numbers are not stored in our DB for most entities (we only used them for mapping during import),
            // this pre-loading is only useful if we are running multiple imports in the same session.
            // For a "one-time" import, these will likely be empty unless we store the MSAP mapping somewhere.
            // However, some entities DO have their original numbers (like PortNumber, ServiceNumber, etc.)

            var ports = await dbContext.MMSIPorts.AsNoTracking().Where(x => x.PortNumber != null).ToListAsync(cancellationToken);
            foreach (var p in ports) portMap[p.PortNumber!] = p.PortId;

            var services = await dbContext.MMSIServices.AsNoTracking().ToListAsync(cancellationToken);
            foreach (var s in services) serviceMap[s.ServiceNumber] = s.ServiceId;

            var owners = await dbContext.MMSITugboatOwners.AsNoTracking().ToListAsync(cancellationToken);
            foreach (var o in owners) ownerMap[o.TugboatOwnerNumber] = o.TugboatOwnerId;

            var masters = await dbContext.MMSITugMasters.AsNoTracking().Where(x => x.TugMasterNumber != null).ToListAsync(cancellationToken);
            foreach (var m in masters) tugMasterMap[m.TugMasterNumber] = m.TugMasterId;

            var vessels = await dbContext.MMSIVessels.AsNoTracking().Where(x => x.VesselNumber != null).ToListAsync(cancellationToken);
            foreach (var v in vessels) vesselMap[v.VesselNumber] = v.VesselId;

            var tugboats = await dbContext.MMSITugboats.AsNoTracking().Where(x => x.TugboatNumber != null).ToListAsync(cancellationToken);
            foreach (var t in tugboats) tugboatMap[t.TugboatNumber] = t.TugboatId;

            var terminals = await dbContext.MMSITerminals.Include(t => t.Port).AsNoTracking().ToListAsync(cancellationToken);
            foreach (var t in terminals) terminalMap[$"{t.Port!.PortNumber}{t.TerminalNumber}"] = t.TerminalId;

            var billings = await dbContext.Billings.AsNoTracking().Where(x => x.MMSIBillingNumber != null).ToListAsync(cancellationToken);
            foreach (var b in billings) billingMap[b.MMSIBillingNumber] = b.MMSIBillingId;

            var collections = await dbContext.MMSICollections.AsNoTracking().Where(x => x.MMSICollectionNumber != null).ToListAsync(cancellationToken);
            foreach (var c in collections) collectionMap[c.MMSICollectionNumber] = c.MMSICollectionId;

            // Customers are tricky because we don't store MSAP Number in Customer table.
            // But we can match by name if needed, or assume customerMap is only for the current session.
        }

        #endregion

        #region -- Masterfiles --

        public async Task<(string Result, Dictionary<string, int> Map)> ImportMsapCustomers(IFormFile file, CancellationToken cancellationToken)
        {
            var existingCustomers = (await unitOfWork.Customer.GetAllAsync(c => c.Company == "MMSI", cancellationToken))
                .Where(c => !string.IsNullOrWhiteSpace(c.CustomerName))
                .ToDictionary(c => c.CustomerName.Trim(), c => c.CustomerId, StringComparer.OrdinalIgnoreCase);

            var customerMap = new Dictionary<string, int>();
            var customerList = new List<Customer>();

            using var reader = new StreamReader(file.OpenReadStream());
            using var csv = new CsvReader(reader, _csvConfig);
            var records = csv.GetRecords<dynamic>();

            foreach (var record in records)
            {
                string customerName = (GetString(record, "name") ?? string.Empty).Trim();
                string? rawNumber = GetString(record, "number");
                if (string.IsNullOrWhiteSpace(rawNumber))
                {
                    throw new Exception("Customer number is missing in CSV.");
                }
                string msapNumber = PadNumber(rawNumber, 4);

                if (string.IsNullOrWhiteSpace(customerName))
                {
                    throw new Exception($"Customer name is missing for record #{msapNumber}.");
                }

                if (existingCustomers.TryGetValue(customerName, out int existingId))
                {
                    customerMap[msapNumber] = existingId;
                    continue;
                }

                Customer newCustomer = new Customer();

                switch (GetString(record, "terms"))
                {
                    case "7": newCustomer.CustomerTerms = "7D"; break;
                    case "0": newCustomer.CustomerTerms = "COD"; break;
                    case "15": newCustomer.CustomerTerms = "15D"; break;
                    case "30": newCustomer.CustomerTerms = "30D"; break;
                    case "60": newCustomer.CustomerTerms = "60D"; break;
                    default: newCustomer.CustomerTerms = "COD"; break;
                }

                newCustomer.CustomerCode = await unitOfWork.Customer.GenerateCodeAsync("Industrial", cancellationToken);
                newCustomer.CustomerName = customerName;
                string? addr1 = GetString(record, "address1");
                string? addr2 = GetString(record, "address2");
                string? addr3 = GetString(record, "address3");
                var addressConcatenated = $"{addr1} {addr2} {addr3}".Trim();
                newCustomer.CustomerAddress = string.IsNullOrEmpty(addressConcatenated) ? "-" : addressConcatenated;
                newCustomer.CustomerTin = GetString(record, "tin") ?? "000-000-000-00000";
                newCustomer.BusinessStyle = GetString(record, "business");
                newCustomer.CustomerType = "Industrial";
                newCustomer.WithHoldingVat = ParseBool(record, "vatable");
                newCustomer.WithHoldingTax = false;
                newCustomer.CreatedBy = $"Import: {GetUserFullName()}";
                newCustomer.CreatedDate = DateTimeHelper.GetCurrentPhilippineTime();
                newCustomer.VatType = newCustomer.WithHoldingVat ? "Vatable" : "Zero-Rated";
                newCustomer.IsActive = ParseBool(record, "active");
                newCustomer.ZipCode = "0000";
                newCustomer.Type = "Documented";
                newCustomer.Company = "MMSI";

                customerList.Add(newCustomer);
                // Temporarily store in existingCustomers to prevent duplicates in the same batch
                existingCustomers[customerName] = 0;

                // We'll update the map with the real ID after saving
                customerMap[msapNumber] = -1;
            }

            if (customerList.Count > 0)
            {
                await dbContext.Customers.AddRangeAsync(customerList, cancellationToken);
                await dbContext.SaveChangesAsync(cancellationToken);

                // Update the map for the newly added customers
                // Re-fetch to be safe or use the local list if IDs are populated
                foreach (var customer in customerList)
                {
                    // Find the MSAP number that corresponds to this name
                    var msapNo = customerMap.FirstOrDefault(x => x.Value == -1 &&
                        customerList.Any(cl => cl.CustomerName == customer.CustomerName)).Key;

                    if (msapNo != null)
                    {
                        customerMap[msapNo] = customer.CustomerId;
                    }
                }
            }

            // Cleanup any temporary -1 values if any name was a duplicate in CSV but not in DB
            // (Shouldn't happen with the current logic but good to be safe)

            return ($"Customers imported successfully, {customerList.Count} new records", customerMap);
        }

        public async Task<(string Result, Dictionary<string, int> Map)> ImportMsapPorts(IFormFile file, CancellationToken cancellationToken)
        {
            var existingPorts = await dbContext.MMSIPorts
                .Where(p => p.PortNumber != null)
                .ToDictionaryAsync(p => p.PortNumber!, p => p.PortId, cancellationToken);
            var portMap = new Dictionary<string, int>();
            var newRecords = new List<Port>();

            using var reader = new StreamReader(file.OpenReadStream());
            using var csv = new CsvReader(reader, _csvConfig);
            var records = csv.GetRecords<dynamic>();

            foreach (var record in records)
            {
                string msapNumber = GetString(record, "number") ?? string.Empty;
                string padded = PadNumber(msapNumber, 3);

                if (string.IsNullOrEmpty(padded))
                {
                    continue;
                }

                if (existingPorts.TryGetValue(padded, out int existingId))
                {
                    portMap[msapNumber] = existingId;
                    continue;
                }

                Port newRecord = new Port { PortNumber = padded, PortName = GetString(record, "name") };

                newRecords.Add(newRecord);
                existingPorts[padded] = 0; // Prevent duplicates in batch
                portMap[msapNumber] = -1;
            }

            if (newRecords.Count > 0)
            {
                await dbContext.MMSIPorts.AddRangeAsync(newRecords, cancellationToken);
                await dbContext.SaveChangesAsync(cancellationToken);
                foreach (var record in newRecords)
                {
                    var key = portMap.FirstOrDefault(x => x.Value == -1 && PadNumber(x.Key, 3) == record.PortNumber).Key;
                    if (key != null)
                    {
                        portMap[key] = record.PortId;
                    }
                }
            }

            return ($"Ports imported successfully, {newRecords.Count} new records", portMap);
        }

        public async Task<(string Result, Dictionary<string, int> Map)> ImportMsapServices(IFormFile file, CancellationToken cancellationToken)
        {
            var existingServices = await dbContext.MMSIServices
                .Where(s => true)
                .ToDictionaryAsync(s => s.ServiceNumber, s => s.ServiceId, cancellationToken);
            var serviceMap = new Dictionary<string, int>();
            var newRecords = new List<Service>();

            using var reader = new StreamReader(file.OpenReadStream());
            using var csv = new CsvReader(reader, _csvConfig);
            var records = csv.GetRecords<dynamic>();

            foreach (var record in records)
            {
                string msapNumber = GetString(record, "number") ?? string.Empty;
                string padded = PadNumber(msapNumber, 3);

                if (string.IsNullOrEmpty(padded))
                {
                    continue;
                }

                if (existingServices.TryGetValue(padded, out int existingId))
                {
                    serviceMap[msapNumber] = existingId;
                    continue;
                }

                Service newRecord = new Service { ServiceNumber = padded, ServiceName = GetString(record, "desc") ?? "UNKNOWN" };

                newRecords.Add(newRecord);
                existingServices[padded] = 0;
                serviceMap[msapNumber] = -1;
            }

            if (newRecords.Count > 0)
            {
                await dbContext.MMSIServices.AddRangeAsync(newRecords, cancellationToken);
                await dbContext.SaveChangesAsync(cancellationToken);
                foreach (var record in newRecords)
                {
                    var key = serviceMap.FirstOrDefault(x => x.Value == -1 && PadNumber(x.Key, 3) == record.ServiceNumber).Key;
                    if (key != null)
                    {
                        serviceMap[key] = record.ServiceId;
                    }
                }
            }

            return ($"Services imported successfully, {newRecords.Count} new records", serviceMap);
        }

        public async Task<(string Result, Dictionary<string, int> Map)> ImportMsapTugboatOwners(IFormFile file, CancellationToken cancellationToken)
        {
            var existingOwners = await dbContext.MMSITugboatOwners
                .Where(o => true)
                .ToDictionaryAsync(o => o.TugboatOwnerNumber, o => o.TugboatOwnerId, cancellationToken);
            var ownerMap = new Dictionary<string, int>();
            var newRecords = new List<TugboatOwner>();

            using var reader = new StreamReader(file.OpenReadStream());
            using var csv = new CsvReader(reader, _csvConfig);
            var records = csv.GetRecords<dynamic>();

            foreach (var record in records)
            {
                string msapNumber = GetString(record, "number") ?? string.Empty;
                string padded = PadNumber(msapNumber, 3);

                if (string.IsNullOrEmpty(padded))
                {
                    continue;
                }

                if (existingOwners.TryGetValue(padded, out int existingId))
                {
                    ownerMap[msapNumber] = existingId;
                    continue;
                }

                TugboatOwner newRecord = new TugboatOwner { TugboatOwnerNumber = padded, TugboatOwnerName = GetString(record, "name") ?? "UNKNOWN" };

                newRecords.Add(newRecord);
                existingOwners[padded] = 0;
                ownerMap[msapNumber] = -1;
            }

            if (newRecords.Count > 0)
            {
                await dbContext.MMSITugboatOwners.AddRangeAsync(newRecords, cancellationToken);
                await dbContext.SaveChangesAsync(cancellationToken);
                foreach (var record in newRecords)
                {
                    var key = ownerMap.FirstOrDefault(x => x.Value == -1 && PadNumber(x.Key, 3) == record.TugboatOwnerNumber).Key;
                    if (key != null)
                    {
                        ownerMap[key] = record.TugboatOwnerId;
                    }
                }
            }

            return ($"Tugboat Owners imported successfully, {newRecords.Count} new records", ownerMap);
        }

        public async Task<(string Result, Dictionary<string, int> Map)> ImportMsapTugMasters(IFormFile file, CancellationToken cancellationToken)
        {
            var existingMasters = await dbContext.MMSITugMasters
                .Where(m => true)
                .ToDictionaryAsync(m => m.TugMasterNumber, m => m.TugMasterId, cancellationToken);
            var tugMasterMap = new Dictionary<string, int>();
            var newRecords = new List<TugMaster>();

            using var reader = new StreamReader(file.OpenReadStream());
            using var csv = new CsvReader(reader, _csvConfig);
            var records = csv.GetRecords<dynamic>();

            foreach (var record in records)
            {
                string empNo = GetString(record, "empno") ?? string.Empty;

                if (string.IsNullOrEmpty(empNo))
                {
                    continue;
                }

                if (existingMasters.TryGetValue(empNo, out int existingId))
                {
                    tugMasterMap[empNo] = existingId;
                    continue;
                }

                TugMaster newRecord = new TugMaster
                {
                    TugMasterNumber = empNo, TugMasterName = GetString(record, "name") ?? "UNKNOWN", IsActive = ParseBool(record, "active")
                };

                newRecords.Add(newRecord);
                existingMasters[empNo] = 0;
                tugMasterMap[empNo] = -1;
            }

            if (newRecords.Count > 0)
            {
                await dbContext.MMSITugMasters.AddRangeAsync(newRecords, cancellationToken);
                await dbContext.SaveChangesAsync(cancellationToken);
                foreach (var record in newRecords)
                {
                    if (tugMasterMap.ContainsKey(record.TugMasterNumber))
                    {
                        tugMasterMap[record.TugMasterNumber] = record.TugMasterId;
                    }
                }
            }

            return ($"Tug Masters imported successfully, {newRecords.Count} new records", tugMasterMap);
        }

        public async Task<(string Result, Dictionary<string, int> Map)> ImportMsapVessels(IFormFile file, CancellationToken cancellationToken)
        {
            var existingVessels = await dbContext.MMSIVessels
                .Where(v => true)
                .ToDictionaryAsync(v => v.VesselNumber, v => v.VesselId, cancellationToken);
            var vesselMap = new Dictionary<string, int>();
            var newRecords = new List<Vessel>();

            using var reader = new StreamReader(file.OpenReadStream());
            using var csv = new CsvReader(reader, _csvConfig);
            var records = csv.GetRecords<dynamic>();

            foreach (var record in records)
            {
                string msapNumber = GetString(record, "number") ?? string.Empty;
                string padded = PadNumber(msapNumber, 4);

                if (string.IsNullOrEmpty(padded))
                {
                    continue;
                }

                if (existingVessels.TryGetValue(padded, out int existingId))
                {
                    vesselMap[msapNumber] = existingId;
                    continue;
                }

                Vessel newRecord = new Vessel
                {
                    VesselNumber = padded, VesselName = GetString(record, "name") ?? "UNKNOWN", VesselType = GetString(record, "type") == "L" ? "LOCAL" : "FOREIGN"
                };

                newRecords.Add(newRecord);
                existingVessels[padded] = 0;
                vesselMap[msapNumber] = -1;
            }

            if (newRecords.Count > 0)
            {
                await dbContext.MMSIVessels.AddRangeAsync(newRecords, cancellationToken);
                await dbContext.SaveChangesAsync(cancellationToken);
                foreach (var record in newRecords)
                {
                    var key = vesselMap.FirstOrDefault(x => x.Value == -1 && PadNumber(x.Key, 4) == record.VesselNumber).Key;
                    if (key != null)
                    {
                        vesselMap[key] = record.VesselId;
                    }
                }
            }

            return ($"Vessels imported successfully, {newRecords.Count} new records", vesselMap);
        }

        public async Task<(string Result, Dictionary<string, int> Map)> ImportMsapTerminals(IFormFile file, Dictionary<string, int> portMap, DummyIds dummy, CancellationToken cancellationToken)
        {
            var existingTerminals = await dbContext.MMSITerminals
                .Include(t => t.Port)
                .ToDictionaryAsync(t => $"{t.Port!.PortNumber}|{t.TerminalNumber}", t => t.TerminalId, cancellationToken);

            var terminalMap = new Dictionary<string, int>();
            var newRecords = new List<Terminal>();

            using var reader = new StreamReader(file.OpenReadStream());
            using var csv = new CsvReader(reader, _csvConfig);
            var records = csv.GetRecords<dynamic>();

            foreach (var record in records)
            {
                string msapNumber = GetString(record, "number") ?? string.Empty;
                if (msapNumber.Length < 6)
                {
                    throw new Exception($"Terminal number '{msapNumber}' is invalid. It must be at least 6 characters.");
                }

                var portPart = msapNumber.Substring(0, 3);
                var terminalPart = msapNumber.Substring(msapNumber.Length - 3, 3);

                string paddedPortNumber = PadNumber(portPart, 3);
                string paddedTerminalNumber = PadNumber(terminalPart, 3);

                string lookupKey = $"{paddedPortNumber}|{paddedTerminalNumber}";

                if (existingTerminals.TryGetValue(lookupKey, out int existingId))
                {
                    terminalMap[msapNumber] = existingId;
                    continue;
                }

                if (!portMap.TryGetValue(portPart, out int portId))
                {
                    portId = dummy.PortId;
                }

                Terminal newRecord = new Terminal
                {
                    PortId = portId, TerminalName = GetString(record, "name"), TerminalNumber = paddedTerminalNumber
                };

                newRecords.Add(newRecord);
                existingTerminals[lookupKey] = 0;
                terminalMap[msapNumber] = -1;
            }

            if (newRecords.Count > 0)
            {
                await dbContext.MMSITerminals.AddRangeAsync(newRecords, cancellationToken);
                await dbContext.SaveChangesAsync(cancellationToken);
                foreach (var record in newRecords)
                {
                    var portNum = portMap.FirstOrDefault(x => x.Value == record.PortId).Key;
                    var key = terminalMap.FirstOrDefault(x => x.Value == -1 && x.Key.StartsWith(portNum!) && x.Key.EndsWith(record.TerminalNumber!)).Key;
                    if (key != null)
                    {
                        terminalMap[key] = record.TerminalId;
                    }
                }
            }

            return ($"Terminals imported successfully, {newRecords.Count} new records", terminalMap);
        }

        public async Task<(string Result, Dictionary<string, int> Map)> ImportMsapTugboats(IFormFile file, Dictionary<string, int> ownerMap, CancellationToken cancellationToken)
        {
            var existingTugboats = await dbContext.MMSITugboats
                .Where(t => true)
                .ToDictionaryAsync(t => t.TugboatNumber, t => t.TugboatId, cancellationToken);
            var tugboatMap = new Dictionary<string, int>();
            var newRecords = new List<Tugboat>();

            using var reader = new StreamReader(file.OpenReadStream());
            using var csv = new CsvReader(reader, _csvConfig);
            var records = csv.GetRecords<dynamic>();

            foreach (var record in records)
            {
                string msapNumber = GetString(record, "number") ?? string.Empty;
                string padded = PadNumber(msapNumber, 3);

                if (string.IsNullOrEmpty(padded))
                {
                    continue;
                }

                if (existingTugboats.TryGetValue(padded, out int existingId))
                {
                    tugboatMap[msapNumber] = existingId;
                    continue;
                }

                Tugboat newRecord = new Tugboat();
                string ownerNo = GetString(record, "owner") ?? string.Empty;
                if (ownerMap.TryGetValue(ownerNo, out int ownerId))
                {
                    newRecord.TugboatOwnerId = ownerId;
                }

                newRecord.TugboatNumber = padded;
                newRecord.TugboatName = GetString(record, "name") ?? "UNKNOWN";
                newRecord.IsCompanyOwned = ParseBool(record, "companyown");

                newRecords.Add(newRecord);
                existingTugboats[padded] = 0;
                tugboatMap[msapNumber] = -1;
            }

            if (newRecords.Count > 0)
            {
                await dbContext.MMSITugboats.AddRangeAsync(newRecords, cancellationToken);
                await dbContext.SaveChangesAsync(cancellationToken);
                foreach (var record in newRecords)
                {
                    var key = tugboatMap.FirstOrDefault(x => x.Value == -1 && PadNumber(x.Key, 3) == record.TugboatNumber).Key;
                    if (key != null)
                    {
                        tugboatMap[key] = record.TugboatId;
                    }
                }
            }

            return ($"Tugboats imported successfully, {newRecords.Count} new records", tugboatMap);
        }

        public async Task<(string Result, Dictionary<string, int> Map)> ImportMsapPrincipals(IFormFile file, Dictionary<string, int> customerMap, DummyIds dummy, CancellationToken cancellationToken)
        {
            var existingPrincipals = await dbContext.MMSIPrincipals
                .Select(p => new { p.PrincipalNumber, p.PrincipalName, p.CustomerId, p.PrincipalId })
                .ToListAsync(cancellationToken);

            var principalMap = new Dictionary<string, int>();
            var newRecords = new List<Principal>();

            using var reader = new StreamReader(file.OpenReadStream());
            using var csv = new CsvReader(reader, _csvConfig);
            var records = csv.GetRecords<dynamic>();

            foreach (var record in records)
            {
                string msapNumber = GetString(record, "number") ?? string.Empty;
                string padded = PadNumber(msapNumber, 4);
                string agentNo = GetString(record, "agent") ?? string.Empty;

                if (!customerMap.TryGetValue(agentNo, out int customerId))
                {
                    throw new Exception($"Agent/Customer '{agentNo}' not found for principal '{msapNumber}'. Please import Customers first.");
                }

                string principalName = GetString(record, "name") ?? string.Empty;

                var existing = existingPrincipals.FirstOrDefault(p => p.PrincipalNumber == padded && p.PrincipalName == principalName && p.CustomerId == customerId);
                if (existing != null)
                {
                    principalMap[msapNumber] = existing.PrincipalId;
                    continue;
                }

                Principal newRecord = new Principal();

                switch (GetString(record, "terms"))
                {
                    case "7": newRecord.Terms = "7D"; break;
                    case "0": newRecord.Terms = "COD"; break;
                    case "15": newRecord.Terms = "15D"; break;
                    case "30": newRecord.Terms = "30D"; break;
                    case "60": newRecord.Terms = "60D"; break;
                }

                newRecord.CustomerId = customerId;
                newRecord.PrincipalNumber = padded;
                newRecord.PrincipalName = principalName;
                string? addr1 = GetString(record, "address1");
                string? addr2 = GetString(record, "address2");
                string? addr3 = GetString(record, "address3");
                var addressConcatenated = $"{addr1} {addr2} {addr3}".Trim();
                newRecord.Address = string.IsNullOrEmpty(addressConcatenated) ? "-" : addressConcatenated;
                newRecord.TIN = GetString(record, "tin") ?? "000-000-000000";
                newRecord.BusinessType = GetString(record, "business");
                newRecord.Landline1 = GetString(record, "landline1");
                newRecord.Landline2 = GetString(record, "landline2");
                newRecord.Mobile1 = GetString(record, "mobile1");
                newRecord.Mobile2 = GetString(record, "mobile2");
                newRecord.IsVatable = ParseBool(record, "vatable");
                newRecord.IsActive = ParseBool(record, "active");

                newRecords.Add(newRecord);
                principalMap[msapNumber] = -1;
            }

            if (newRecords.Count > 0)
            {
                await dbContext.MMSIPrincipals.AddRangeAsync(newRecords, cancellationToken);
                await dbContext.SaveChangesAsync(cancellationToken);
                foreach (var record in newRecords)
                {
                    var key = principalMap.FirstOrDefault(x => x.Value == -1 && PadNumber(x.Key, 4) == record.PrincipalNumber).Key;
                    if (key != null)
                    {
                        principalMap[key] = record.PrincipalId;
                    }
                }
            }

            return ($"Principals imported successfully, {newRecords.Count} new records", principalMap);
        }

        public async Task<string> ImportMsapTariffRates(IFormFile file, Dictionary<string, int> customerMap, Dictionary<string, int> terminalMap, Dictionary<string, int> serviceMap, DummyIds dummy, CancellationToken cancellationToken)
        {
            var existingIdentifier = await dbContext.MMSITariffRates
                .AsNoTracking()
                .Select(t => new { t.CustomerId, t.TerminalId, t.ServiceId, t.AsOfDate })
                .ToHashSetAsync(cancellationToken);

            var newRecords = new List<TariffRate>();
            var currentBatch = new HashSet<object>();

            using var reader = new StreamReader(file.OpenReadStream());
            using var csv = new CsvReader(reader, _csvConfig);
            var records = csv.GetRecords<dynamic>();

            foreach (var record in records)
            {
                DateOnly asOfDate = ParseDateOnly(record, "date") ?? DateOnly.FromDateTime(DateTime.Today);

                string? custNo = GetString(record, "custno");
                if (!customerMap.TryGetValue(PadNumber(custNo ?? "", 4), out int customerId))
                {
                    customerId = dummy.CustomerId;
                }

                string? terminalRaw = GetString(record, "terminal");
                if (!terminalMap.TryGetValue(terminalRaw ?? "", out int terminalId))
                {
                    terminalId = dummy.TerminalId;
                }

                string? serviceNum = GetString(record, "service");
                if (!serviceMap.TryGetValue(PadNumber(serviceNum ?? "", 3), out int serviceId))
                {
                    serviceId = dummy.ServiceId;
                }

                var identity = new { CustomerId = customerId, TerminalId = terminalId, ServiceId = serviceId, AsOfDate = asOfDate };

                if (existingIdentifier.Contains(identity) || currentBatch.Contains(identity))
                {
                    continue;
                }

                TariffRate newRecord = new TariffRate
                {
                    CustomerId = customerId, TerminalId = terminalId, ServiceId = serviceId, AsOfDate = asOfDate,
                    Dispatch = ParseDecimal(record, "dispatch"),
                    BAF = ParseDecimal(record, "baf"),
                    CreatedBy = GetString(record, "createdby"),
                    CreatedDate = ParseDateTime(record, "createddat") ?? DateTimeHelper.GetCurrentPhilippineTime()
                };

                newRecords.Add(newRecord);
                currentBatch.Add(identity);
            }

            await dbContext.MMSITariffRates.AddRangeAsync(newRecords, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
            return $"Tariff Rates imported successfully, {newRecords.Count} new records";
        }

        #endregion -- Masterfiles --

        private async Task<string> ProcessDispatchTickets(
            IFormFile file,
            Dictionary<string, int> customerMap,
            Dictionary<string, int> vesselMap,
            Dictionary<string, int> tugboatMap,
            Dictionary<string, int> tugMasterMap,
            Dictionary<string, int> serviceMap,
            Dictionary<string, int> terminalMap,
            Dictionary<string, int> portMap,
            Dictionary<string, int> billingMap,
            DummyIds dummy,
            CancellationToken cancellationToken)
        {
            var existingIdentifier = await dbContext.MMSIDispatchTickets
                .AsNoTracking()
                .Select(dt => new { dt.DispatchNumber, dt.CreatedDate })
                .ToHashSetAsync(cancellationToken);

            var newRecords = new List<DispatchTicket>();
            var currentBatch = new HashSet<object>();

            using var reader = new StreamReader(file.OpenReadStream());
            using var csv = new CsvReader(reader, _csvConfig);
            var records = csv.GetRecords<dynamic>();

            foreach (var record in records)
            {
                DateTime entryDate = ParseDateTime(record, "entrydate") ?? DateTimeHelper.GetCurrentPhilippineTime();
                string dispatchNumber = GetString(record, "number")?.Trim() ?? "";

                var identity = new { DispatchNumber = dispatchNumber, CreatedDate = entryDate };

                if (existingIdentifier.Contains(identity) || currentBatch.Contains(identity))
                {
                    continue;
                }

                string? custNo = GetString(record, "custno");
                if (!customerMap.TryGetValue(PadNumber(custNo ?? "", 4), out int customerId))
                {
                    customerId = dummy.CustomerId;
                }

                string? tugboatNum = GetString(record, "tugnum");
                if (!tugboatMap.TryGetValue(tugboatNum ?? "", out int tbId))
                {
                    tbId = dummy.TugboatId;
                }

                string? vesselNum = GetString(record, "vesselnum");
                if (!vesselMap.TryGetValue(vesselNum ?? "", out int vId))
                {
                    vId = dummy.VesselId;
                }

                string? serviceNum = GetString(record, "srvctype");
                if (!serviceMap.TryGetValue(serviceNum ?? "", out int sId))
                {
                    sId = dummy.ServiceId;
                }

                string? terminalRaw = GetString(record, "terminal");
                int portId;
                int termId;

                if (!string.IsNullOrEmpty(terminalRaw) && terminalRaw.Length >= 6)
                {
                    var portPart = terminalRaw.Substring(0, 3);
                    if (!portMap.TryGetValue(portPart, out portId)) portId = dummy.PortId;
                    if (!terminalMap.TryGetValue(terminalRaw, out termId)) termId = dummy.TerminalId;
                }
                else
                {
                    portId = dummy.PortId;
                    termId = dummy.TerminalId;
                }

                DispatchTicket newRecord = new DispatchTicket
                {
                    CustomerId = customerId,
                    TugBoatId = tbId,
                    VesselId = vId,
                    ServiceId = sId,
                    PortId = portId,
                    TerminalId = termId
                };

                string? billNumberStr = GetString(record, "billnum");
                if (!string.IsNullOrEmpty(billNumberStr) && billingMap.TryGetValue(billNumberStr, out int billingId))
                {
                    newRecord.BillingId = billingId;
                }

                newRecord.BillingNumber = string.IsNullOrEmpty(billNumberStr) ? null : billNumberStr;
                newRecord.DispatchNumber = dispatchNumber;
                newRecord.ApOtherTugs = ParseDecimal(record, "apothertug");
                newRecord.DispatchChargeType = ParseBool(record, "perhour") ? "Per hour" : "Per move";
                newRecord.BAFChargeType = "Per move";
                newRecord.CreatedBy = GetString(record, "createdby") ?? "IMPORT";
                newRecord.CreatedDate = ParseDateTime(record, "date") ?? DateTimeHelper.GetCurrentPhilippineTime();

                newRecord.DateLeft = ParseDateOnly(record, "dateleft");
                newRecord.TimeLeft = ParseTimeOnly(record, "timeleft");
                newRecord.DateArrived = ParseDateOnly(record, "datearrive");
                newRecord.TimeArrived = ParseTimeOnly(record, "timearrive");
                newRecord.Date = ParseDateOnly(record, "date");
                newRecord.COSNumber = GetString(record, "cosno");
                newRecord.VoyageNumber = GetString(record, "voyage");
                newRecord.Remarks = GetString(record, "remarks");

                if (ParseBool(record, "approved"))
                {
                    newRecord.Status = string.IsNullOrEmpty(billNumberStr) ? "For Billing" : "Billed";
                }
                else
                {
                    newRecord.Status = "For Tariff";
                }

                if (newRecord is { DateLeft: not null, DateArrived: not null, TimeLeft: not null, TimeArrived: not null })
                {
                    DateTime dtl = newRecord.DateLeft.Value.ToDateTime(newRecord.TimeLeft.Value);
                    DateTime dta = newRecord.DateArrived.Value.ToDateTime(newRecord.TimeArrived.Value);
                    TimeSpan ts = dta - dtl;
                    var totalHours = Math.Round((decimal)ts.TotalHours, 2);

                    if (newRecord.CustomerId == 179)
                    {
                        var wholeHours = Math.Truncate(totalHours);
                        var fractionalPart = totalHours - wholeHours;
                        if (fractionalPart >= 0.75m)
                        {
                            totalHours = wholeHours + 1.0m;
                        }
                        else if (fractionalPart >= 0.25m)
                        {
                            totalHours = wholeHours + 0.5m;
                        }
                        else
                        {
                            totalHours = wholeHours;
                        }
                    }
                    newRecord.TotalHours = totalHours;
                }

                newRecords.Add(newRecord);
                currentBatch.Add(identity);
            }

            await dbContext.MMSIDispatchTickets.AddRangeAsync(newRecords, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
            return $"Dispatch Tickets imported successfully, {newRecords.Count} new records";
        }

        public async Task<(string Result, Dictionary<string, int> Map)> ImportMsapBillings(
            IFormFile file,
            Dictionary<string, int> customerMap,
            Dictionary<string, int> vesselMap,
            Dictionary<string, int> portMap,
            Dictionary<string, int> terminalMap,
            Dictionary<string, int> principalMap,
            Dictionary<string, int> collectionMap,
            DummyIds dummy,
            CancellationToken cancellationToken)
        {
            var existingIdentifier = await dbContext.Billings
                .AsNoTracking()
                .Where(b => true)
                .ToDictionaryAsync(b => b.MMSIBillingNumber, b => b.MMSIBillingId, cancellationToken);

            var billingMap = new Dictionary<string, int>();
            var newRecords = new List<Billing>();

            using var reader = new StreamReader(file.OpenReadStream());
            using var csv = new CsvReader(reader, _csvConfig);
            var records = csv.GetRecords<dynamic>();

            foreach (var record in records)
            {
                string? billingNumber = GetString(record, "number");
                if (string.IsNullOrEmpty(billingNumber))
                {
                    continue;
                }

                if (existingIdentifier.TryGetValue(billingNumber, out int existingId))
                {
                    billingMap[billingNumber] = existingId;
                    continue;
                }

                string? vesselNum = GetString(record, "vesselnum");
                if (!vesselMap.TryGetValue(vesselNum ?? "", out int vesselId))
                {
                    vesselId = dummy.VesselId;
                }

                string? custNo = GetString(record, "custno");
                if (!customerMap.TryGetValue(PadNumber(custNo ?? "", 4), out int customerId))
                {
                    customerId = dummy.CustomerId;
                }

                string? terminalRaw = GetString(record, "terminal");
                int portId;
                int terminalId;

                if (!string.IsNullOrEmpty(terminalRaw) && terminalRaw.Length >= 6)
                {
                    var portPart = terminalRaw.Substring(0, 3);
                    if (!portMap.TryGetValue(portPart, out portId)) portId = dummy.PortId;
                    if (!terminalMap.TryGetValue(terminalRaw, out terminalId)) terminalId = dummy.TerminalId;
                }
                else
                {
                    portId = dummy.PortId;
                    terminalId = dummy.TerminalId;
                }

                Billing newRecord = new Billing
                {
                    VesselId = vesselId,
                    CustomerId = customerId,
                    PortId = portId,
                    TerminalId = terminalId
                };

                newRecord.MMSIBillingNumber = billingNumber;
                DateOnly? billingDateNullable = ParseDateOnly(record, "date");
                if (billingDateNullable.HasValue)
                {
                    newRecord.Date = billingDateNullable.Value;
                    newRecord.DueDate = billingDateNullable.Value;
                }
                else
                {
                    newRecord.Date = DateOnly.FromDateTime(DateTime.Today);
                    newRecord.DueDate = DateOnly.FromDateTime(DateTime.Today);
                }

                newRecord.Amount = ParseDecimal(record, "amount");
                newRecord.Balance = newRecord.Amount;
                newRecord.AmountPaid = 0;
                newRecord.IsPaid = false;
                newRecord.Company = "MMSI";
                newRecord.IsUndocumented = ParseBool(record, "undocument");
                newRecord.ApOtherTug = ParseDecimal(record, "apothertug");
                newRecord.CreatedDate = ParseDateTime(record, "entrydate") ?? DateTimeHelper.GetCurrentPhilippineTime();
                newRecord.CreatedBy = GetString(record, "entryby") ?? "IMPORT";
                newRecord.VoyageNumber = GetString(record, "voyage") == string.Empty ? null : GetString(record, "voyage");
                newRecord.DispatchAmount = ParseDecimal(record, "dispatcham");
                newRecord.BAFAmount = ParseDecimal(record, "bafamount");
                newRecord.Discount = 0;
                newRecord.BilledTo = "LOCAL";

                string? crNum = GetString(record, "crnum");
                if (!string.IsNullOrEmpty(crNum) && collectionMap.TryGetValue(crNum, out int collId))
                {
                    newRecord.CollectionId = collId;
                    newRecord.Status = "Collected";
                }
                else
                {
                    newRecord.Status = "For Collection";
                }

                newRecord.CollectionNumber = string.IsNullOrEmpty(crNum) ? null : crNum;
                newRecord.IsVatable = ParseBool(record, "vat");

                string principalNum = GetString(record, "billto") ?? string.Empty;
                if (!string.IsNullOrEmpty(principalNum) && principalMap.TryGetValue(principalNum, out int principalId))
                {
                    newRecord.PrincipalId = principalId;
                    newRecord.BilledTo = "PRINCIPAL";
                    newRecord.IsPrincipal = true;
                }
                else
                {
                    newRecord.PrincipalId = dummy.PrincipalId;
                    newRecord.BilledTo = "LOCAL";
                    newRecord.IsPrincipal = false;
                }

                newRecord.IsPrinted = ParseBool(record, "printed");
                newRecords.Add(newRecord);
                existingIdentifier[billingNumber] = 0;
                billingMap[billingNumber] = -1;
            }

            if (newRecords.Count > 0)
            {
                await dbContext.Billings.AddRangeAsync(newRecords, cancellationToken);
                await dbContext.SaveChangesAsync(cancellationToken);
                foreach (var record in newRecords)
                {
                    if (billingMap.ContainsKey(record.MMSIBillingNumber))
                    {
                        billingMap[record.MMSIBillingNumber] = record.MMSIBillingId;
                    }
                }
            }

            return ($"Billings imported successfully, {newRecords.Count} new records", billingMap);
        }

        public async Task<(string Result, Dictionary<string, int> Map)> ImportMsapCollections(IFormFile file, Dictionary<string, int> customerMap, DummyIds dummy, CancellationToken cancellationToken)
        {
            var existingIdentifier = await dbContext.MMSICollections
                .AsNoTracking()
                .Where(b => true)
                .ToDictionaryAsync(b => b.MMSICollectionNumber, b => b.MMSICollectionId, cancellationToken);

            var collectionMap = new Dictionary<string, int>();
            var newRecords = new List<Collection>();

            using var reader = new StreamReader(file.OpenReadStream());
            using var csv = new CsvReader(reader, _csvConfig);
            var records = csv.GetRecords<dynamic>();

            foreach (var record in records)
            {
                string? crNum = GetString(record, "crnum");
                if (string.IsNullOrEmpty(crNum))
                {
                    continue;
                }

                if (existingIdentifier.TryGetValue(crNum, out int existingId))
                {
                    collectionMap[crNum] = existingId;
                    continue;
                }

                string? custNo = GetString(record, "custno");
                if (!customerMap.TryGetValue(custNo ?? "", out int customerId))
                {
                    customerId = dummy.CustomerId;
                }

                Collection newRecord = new Collection
                {
                    CustomerId = customerId, MMSICollectionNumber = crNum, CheckNumber = GetString(record, "checkno"),
                    Status = "Create",
                    Company = "MMSI",
                    CashAmount = 0,
                    WVAT = 0,
                    Date = ParseDateOnly(record, "crdate") ?? DateOnly.FromDateTime(DateTime.Today),
                    DepositDate = ParseDateOnly(record, "datedeposi"),
                    Amount = ParseDecimal(record, "amount")
                };

                newRecord.CheckAmount = newRecord.Amount;
                newRecord.EWT = ParseDecimal(record, "n2307");

                string? checkDateStr = GetString(record, "checkdate");
                if (checkDateStr != "/  /" && !string.IsNullOrEmpty(checkDateStr))
                {
                    newRecord.CheckDate = ParseDateOnly(record, "checkdate");
                }

                newRecord.Total = newRecord.Amount + newRecord.EWT;
                newRecord.IsUndocumented = ParseBool(record, "undocument");
                newRecord.CreatedDate = ParseDateTime(record, "createddat") ?? DateTimeHelper.GetCurrentPhilippineTime();
                newRecord.CreatedBy = GetString(record, "createdby") ?? "IMPORT";

                newRecords.Add(newRecord);
                existingIdentifier[crNum] = 0;
                collectionMap[crNum] = -1;
            }

            if (newRecords.Count > 0)
            {
                await dbContext.MMSICollections.AddRangeAsync(newRecords, cancellationToken);
                await dbContext.SaveChangesAsync(cancellationToken);
                foreach (var record in newRecords)
                {
                    if (collectionMap.ContainsKey(record.MMSICollectionNumber))
                    {
                        collectionMap[record.MMSICollectionNumber] = record.MMSICollectionId;
                    }
                }
            }

            return ($"Collection import successful, {newRecords.Count} new records", collectionMap);
        }
    }
}
