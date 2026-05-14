using IBS.Models.MasterFile;
using CsvHelper;
using CsvHelper.Configuration;
using IBS.DataAccess.Data;
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
    public class MsapImportController(ApplicationDbContext dbContext) : Controller
    {
        private readonly CsvConfiguration _csvConfig = new(CultureInfo.InvariantCulture)
        {
            PrepareHeaderForMatch = args => args.Header.Trim().ToLower(),
            HeaderValidated = null,
            MissingFieldFound = null,
        };

        private readonly List<string> _importErrors = new();

        private string GetUserFullName() =>
            User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.GivenName)?.Value
            ?? User.Identity?.Name
            ?? "Unknown";

        // -------------------------------------------------------------------------
        // Actions
        // -------------------------------------------------------------------------

        [RequireAnyAccess("You do not have permission to import Msap data.", ProcedureEnum.ManageMsapImport)]
        [HttpGet]
        public IActionResult Index() => View();

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
                // Resolve individual vs bulk files
                customerFile      = ResolveFile(customerFile, bulkFiles, "customer", "customer.csv");
                portFile          = ResolveFile(portFile, bulkFiles, "port", "port.csv");
                terminalFile      = ResolveFile(terminalFile, bulkFiles, "terminal", "terminal.csv");
                principalFile     = ResolveFile(principalFile, bulkFiles, "principal", "principal.csv");
                serviceFile       = ResolveFile(serviceFile, bulkFiles, "service", "service.csv");
                tugboatOwnerFile  = ResolveFile(tugboatOwnerFile, bulkFiles, "tugboatowner", "tugboatowner.csv");
                tugboatFile       = tugboatFile ?? bulkFiles?.FirstOrDefault(f =>
                    (f.FileName.Equals("tugboat.csv", StringComparison.OrdinalIgnoreCase)
                     || f.FileName.Contains("tugboat", StringComparison.OrdinalIgnoreCase))
                    && !f.FileName.Contains("owner", StringComparison.OrdinalIgnoreCase));
                tugMasterFile     = ResolveFile(tugMasterFile, bulkFiles, "tugmaster", "tugmaster.csv")
                                    ?? ResolveFile(null, bulkFiles, "master", "master.csv");
                vesselFile        = ResolveFile(vesselFile, bulkFiles, "vessel", "vessel.csv");
                tariffFile        = ResolveFile(tariffFile, bulkFiles, "tariff", "tariff.csv");
                dispatchTicketFile = ResolveFile(dispatchTicketFile, bulkFiles, "dispatch", "dispatch.csv");
                billingFile       = ResolveFile(billingFile, bulkFiles, "billing", "billing.csv");
                collectionFile    = ResolveFile(collectionFile, bulkFiles, "collection", "collection.csv");

                // Preload maps from existing DB records
                var maps = new ImportMaps();
                await LoadExistingMapsAsync(maps, CancellationToken.None);

                // Level 1 — Independent master files
                if (customerFile != null) sb.AppendLine(await ImportCustomersAsync(customerFile, maps));
                if (portFile != null) sb.AppendLine(await ImportPortsAsync(portFile, maps));
                if (serviceFile != null) sb.AppendLine(await ImportServicesAsync(serviceFile, maps));
                if (tugboatOwnerFile != null) sb.AppendLine(await ImportTugboatOwnersAsync(tugboatOwnerFile, maps));
                if (tugMasterFile != null) sb.AppendLine(await ImportTugMastersAsync(tugMasterFile, maps));
                if (vesselFile != null) sb.AppendLine(await ImportVesselsAsync(vesselFile, maps));

                // Level 2 — Single dependency
                if (terminalFile != null) sb.AppendLine(await ImportTerminalsAsync(terminalFile, maps));
                if (tugboatFile != null) sb.AppendLine(await ImportTugboatsAsync(tugboatFile, maps));
                if (principalFile != null) sb.AppendLine(await ImportPrincipalsAsync(principalFile, maps));

                // Level 3 — Mixed dependencies
                if (tariffFile != null) sb.AppendLine(await ImportTariffRatesAsync(tariffFile, maps));
                if (collectionFile != null) sb.AppendLine(await ImportCollectionsAsync(collectionFile, maps));

                // Level 4 — Transactional
                if (billingFile != null) sb.AppendLine(await ImportBillingsAsync(billingFile, maps));

                // Level 5 — Final
                if (dispatchTicketFile != null) sb.AppendLine(await ImportDispatchTicketsAsync(dispatchTicketFile, maps));

                if (sb.Length == 0 && _importErrors.Count == 0)
                {
                    TempData["error"] = "Please upload at least one CSV file.";
                    return RedirectToAction(nameof(Index));
                }

                await dbContext.SaveChangesAsync();
                await transaction.CommitAsync();

                if (_importErrors.Count > 0)
                {
                    sb.AppendLine("\nERRORS/SKIPPED RECORDS:");
                    foreach (var error in _importErrors.Take(100)) sb.AppendLine(error);
                    if (_importErrors.Count > 100) sb.AppendLine("... (more errors truncated)");
                }

                TempData["success"] = sb.ToString().Replace(Environment.NewLine, "\\n");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                TempData["error"] = ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        // -------------------------------------------------------------------------
        // Import Maps
        // -------------------------------------------------------------------------

        private sealed class ImportMaps
        {
            public Dictionary<string, int> Customer { get; } = new();
            public Dictionary<string, int> Port { get; } = new();
            public Dictionary<string, int> Service { get; } = new();
            public Dictionary<string, int> Owner { get; } = new();
            public Dictionary<string, int> Tugboat { get; } = new();
            public Dictionary<string, int> TugMaster { get; } = new();
            public Dictionary<string, int> Vessel { get; } = new();
            public Dictionary<string, int> Terminal { get; } = new();
            public Dictionary<string, int> Principal { get; } = new();
            public Dictionary<string, int> Collection { get; } = new();
            public Dictionary<string, int> Billing { get; } = new();
            public HashSet<string> TariffRate { get; } = new();
            public HashSet<string> DispatchTicket { get; } = new();
        }

        private async Task LoadExistingMapsAsync(ImportMaps maps, CancellationToken ct)
        {
            maps.Port.Clear();
            foreach (var p in await dbContext.MMSIPorts.AsNoTracking().ToListAsync(ct))
                maps.Port[p.PortNumber!] = p.PortId;

            maps.Service.Clear();
            foreach (var s in await dbContext.MMSIServices.AsNoTracking().ToListAsync(ct))
                maps.Service[s.ServiceNumber] = s.ServiceId;

            maps.Owner.Clear();
            foreach (var o in await dbContext.MMSITugboatOwners.AsNoTracking().ToListAsync(ct))
                maps.Owner[o.TugboatOwnerNumber] = o.TugboatOwnerId;

            maps.TugMaster.Clear();
            foreach (var m in await dbContext.MMSITugMasters.AsNoTracking().ToListAsync(ct))
                maps.TugMaster[m.TugMasterNumber] = m.TugMasterId;

            maps.Vessel.Clear();
            foreach (var v in await dbContext.MMSIVessels.AsNoTracking().ToListAsync(ct))
                maps.Vessel[v.VesselNumber] = v.VesselId;

            maps.Tugboat.Clear();
            foreach (var t in await dbContext.MMSITugboats.AsNoTracking().ToListAsync(ct))
                maps.Tugboat[t.TugboatNumber] = t.TugboatId;

            maps.Terminal.Clear();
            foreach (var t in await dbContext.MMSITerminals.Include(x => x.Port).AsNoTracking().ToListAsync(ct))
            {
                if (t.Port != null && t.Port.PortNumber != null)
                {
                    maps.Terminal[$"{t.Port.PortNumber}{t.TerminalNumber}"] = t.TerminalId;
                }
            }

            maps.Billing.Clear();
            foreach (var b in await dbContext.Billings.AsNoTracking().ToListAsync(ct))
                maps.Billing[b.MMSIBillingNumber] = b.MMSIBillingId;

            maps.Collection.Clear();
            foreach (var c in await dbContext.MMSICollections.AsNoTracking().ToListAsync(ct))
                maps.Collection[c.MMSICollectionNumber] = c.MMSICollectionId;

            maps.Customer.Clear();
            foreach (var c in await dbContext.Customers.AsNoTracking().Where(x => x.Company == "MMSI").ToListAsync(ct))
                if (c.CustomerCode != null)
                    maps.Customer[c.CustomerCode] = c.CustomerId;

            maps.Principal.Clear();
            foreach (var p in await dbContext.MMSIPrincipals.AsNoTracking().ToListAsync(ct))
                maps.Principal[p.PrincipalNumber] = p.PrincipalId;

            maps.TariffRate.Clear();
            foreach (var tr in await dbContext.MMSITariffRates.AsNoTracking().ToListAsync(ct))
                maps.TariffRate.Add($"{tr.AsOfDate:yyyy-MM-dd}|{tr.CustomerId}|{tr.TerminalId}|{tr.ServiceId}");

            maps.DispatchTicket.Clear();
            foreach (var dt in await dbContext.MMSIDispatchTickets.AsNoTracking().ToListAsync(ct))
                maps.DispatchTicket.Add(dt.DispatchNumber);
        }

        // -------------------------------------------------------------------------
        // Master file imports (Level 1 & 2)
        // -------------------------------------------------------------------------

        private async Task<string> ImportCustomersAsync(IFormFile file, ImportMaps maps)
        {
            using var reader = new StreamReader(file.OpenReadStream());
            using var csv = new CsvReader(reader, _csvConfig);
            var records = csv.GetRecords<dynamic>();
            int count = 0, skipped = 0;
            var newRecords = new List<Customer>();

            foreach (var record in records)
            {
                string msapNumber = PadNumber(GetString(record, "number"), 4);
                if (maps.Customer.ContainsKey(msapNumber)) { skipped++; continue; }

                string name = GetString(record, "name");
                if (name == "-") { _importErrors.Add($"Customer {msapNumber}: Name is missing. Skipping."); continue; }

                var entity = new Customer
                {
                    CustomerCode = msapNumber,
                    CustomerName = name,
                    Address1 = GetString(record, "address1"),
                    Address2 = GetString(record, "address2"),
                    Address3 = GetString(record, "address3"),
                    CustomerAddress = JoinAddress(GetString(record, "address1"), GetString(record, "address2"), GetString(record, "address3")),
                    CustomerTin = NormalizeTin(GetString(record, "tin"), "000-000-000-00000"),
                    BusinessStyle = GetString(record, "business"),
                    CustomerTerms = MapTerms(GetString(record, "terms")),
                    CustomerType = "Industrial",
                    WithHoldingVat = ParseBool(record, "vatable"),
                    VatType = ParseBool(record, "vatable") ? "Vatable" : "Zero-Rated",
                    IsActive = ParseBool(record, "active"),
                    ZipCode = "0000",
                    Type = "Documented",
                    Company = "MMSI",
                    CreatedBy = $"Import: {GetUserFullName()}",
                    CreatedDate = DateTimeHelper.GetCurrentPhilippineTime(),
                };

                newRecords.Add(entity);
                count++;

                if (newRecords.Count >= 500)
                {
                    await dbContext.Customers.AddRangeAsync(newRecords);
                    await dbContext.SaveChangesAsync();
                    foreach (var r in newRecords) maps.Customer[r.CustomerCode!] = r.CustomerId;
                    newRecords.Clear();
                }
            }

            if (newRecords.Count > 0)
            {
                await dbContext.Customers.AddRangeAsync(newRecords);
                await dbContext.SaveChangesAsync();
                foreach (var r in newRecords) maps.Customer[r.CustomerCode!] = r.CustomerId;
            }

            return $"Customers: {count} imported, {skipped} already existed.";
        }

        private async Task<string> ImportPortsAsync(IFormFile file, ImportMaps maps)
        {
            using var reader = new StreamReader(file.OpenReadStream());
            using var csv = new CsvReader(reader, _csvConfig);
            var records = csv.GetRecords<dynamic>();
            int count = 0, skipped = 0;
            var newRecords = new List<Port>();

            foreach (var record in records)
            {
                string msapNumber = PadNumber(GetString(record, "number"), 3);
                if (maps.Port.ContainsKey(msapNumber)) { skipped++; continue; }

                var entity = new Port
                {
                    PortNumber = msapNumber,
                    PortName = GetString(record, "name")
                };

                newRecords.Add(entity);
                count++;
            }

            if (newRecords.Count > 0)
            {
                await dbContext.MMSIPorts.AddRangeAsync(newRecords);
                await dbContext.SaveChangesAsync();
                foreach (var r in newRecords) maps.Port[r.PortNumber!] = r.PortId;
            }

            return $"Ports: {count} imported, {skipped} already existed.";
        }

        private async Task<string> ImportServicesAsync(IFormFile file, ImportMaps maps)
        {
            using var reader = new StreamReader(file.OpenReadStream());
            using var csv = new CsvReader(reader, _csvConfig);
            var records = csv.GetRecords<dynamic>();
            int count = 0, skipped = 0;
            var newRecords = new List<Service>();

            foreach (var record in records)
            {
                string msapNumber = PadNumber(GetString(record, "number"), 3);
                if (maps.Service.ContainsKey(msapNumber)) { skipped++; continue; }

                var entity = new Service
                {
                    ServiceNumber = msapNumber,
                    ServiceName = GetString(record, "desc"),
                    ServiceType = GetString(record, "type")
                };

                newRecords.Add(entity);
                count++;
            }

            if (newRecords.Count > 0)
            {
                await dbContext.MMSIServices.AddRangeAsync(newRecords);
                await dbContext.SaveChangesAsync();
                foreach (var r in newRecords) maps.Service[r.ServiceNumber] = r.ServiceId;
            }

            return $"Services: {count} imported, {skipped} already existed.";
        }

        private async Task<string> ImportTugboatOwnersAsync(IFormFile file, ImportMaps maps)
        {
            using var reader = new StreamReader(file.OpenReadStream());
            using var csv = new CsvReader(reader, _csvConfig);
            var records = csv.GetRecords<dynamic>();
            int count = 0, skipped = 0;
            var newRecords = new List<TugboatOwner>();

            foreach (var record in records)
            {
                string msapNumber = PadNumber(GetString(record, "number"), 3);
                if (maps.Owner.ContainsKey(msapNumber)) { skipped++; continue; }

                var entity = new TugboatOwner
                {
                    TugboatOwnerNumber = msapNumber,
                    TugboatOwnerName = GetString(record, "name")
                };

                newRecords.Add(entity);
                count++;
            }

            if (newRecords.Count > 0)
            {
                await dbContext.MMSITugboatOwners.AddRangeAsync(newRecords);
                await dbContext.SaveChangesAsync();
                foreach (var r in newRecords) maps.Owner[r.TugboatOwnerNumber] = r.TugboatOwnerId;
            }

            return $"Tugboat Owners: {count} imported, {skipped} already existed.";
        }

        private async Task<string> ImportTugMastersAsync(IFormFile file, ImportMaps maps)
        {
            using var reader = new StreamReader(file.OpenReadStream());
            using var csv = new CsvReader(reader, _csvConfig);
            var records = csv.GetRecords<dynamic>();
            int count = 0, skipped = 0;
            var newRecords = new List<TugMaster>();

            foreach (var record in records)
            {
                string empNo = GetString(record, "empno");
                if (maps.TugMaster.ContainsKey(empNo)) { skipped++; continue; }

                var entity = new TugMaster
                {
                    TugMasterNumber = empNo,
                    TugMasterName = GetString(record, "name"),
                    IsActive = ParseBool(record, "active")
                };

                newRecords.Add(entity);
                count++;
            }

            if (newRecords.Count > 0)
            {
                await dbContext.MMSITugMasters.AddRangeAsync(newRecords);
                await dbContext.SaveChangesAsync();
                foreach (var r in newRecords) maps.TugMaster[r.TugMasterNumber] = r.TugMasterId;
            }

            return $"Tug Masters: {count} imported, {skipped} already existed.";
        }

        private async Task<string> ImportVesselsAsync(IFormFile file, ImportMaps maps)
        {
            using var reader = new StreamReader(file.OpenReadStream());
            using var csv = new CsvReader(reader, _csvConfig);
            var records = csv.GetRecords<dynamic>();
            int count = 0, skipped = 0;
            var newRecords = new List<Vessel>();

            foreach (var record in records)
            {
                string msapNumber = PadNumber(GetString(record, "number"), 4);
                if (maps.Vessel.ContainsKey(msapNumber)) { skipped++; continue; }

                var entity = new Vessel
                {
                    VesselNumber = msapNumber,
                    VesselName = GetString(record, "name"),
                    VesselType = GetString(record, "type") == "L" ? "LOCAL" : "FOREIGN"
                };

                newRecords.Add(entity);
                count++;
            }

            if (newRecords.Count > 0)
            {
                await dbContext.MMSIVessels.AddRangeAsync(newRecords);
                await dbContext.SaveChangesAsync();
                foreach (var r in newRecords) maps.Vessel[r.VesselNumber] = r.VesselId;
            }

            return $"Vessels: {count} imported, {skipped} already existed.";
        }

        private async Task<string> ImportTerminalsAsync(IFormFile file, ImportMaps maps)
        {
            using var reader = new StreamReader(file.OpenReadStream());
            using var csv = new CsvReader(reader, _csvConfig);
            var records = csv.GetRecords<dynamic>();
            int count = 0, skipped = 0;
            var newRecords = new List<Terminal>();

            foreach (var record in records)
            {
                string msapNumber = GetString(record, "number");
                string? mapKey = NormalizeTerminalCode(msapNumber);
                if (mapKey == null) { _importErrors.Add($"Terminal {msapNumber}: Invalid code format. Skipping."); continue; }

                if (maps.Terminal.ContainsKey(mapKey)) { skipped++; continue; }

                string portPart = mapKey.Substring(0, 3);
                string terminalPart = mapKey.Substring(3, 3);

                if (!maps.Port.TryGetValue(portPart, out int portId))
                {
                    _importErrors.Add($"Terminal {msapNumber}: Port {portPart} not found. Skipping.");
                    continue;
                }

                var entity = new Terminal
                {
                    PortId = portId,
                    TerminalNumber = terminalPart,
                    TerminalName = GetString(record, "name")
                };

                newRecords.Add(entity);
                count++;
            }

            if (newRecords.Count > 0)
            {
                await dbContext.MMSITerminals.AddRangeAsync(newRecords);
                await dbContext.SaveChangesAsync();
                foreach (var r in newRecords) maps.Terminal[$"{maps.Port.First(x => x.Value == r.PortId).Key}{r.TerminalNumber}"] = r.TerminalId;
            }

            return $"Terminals: {count} imported, {skipped} already existed.";
        }

        private async Task<string> ImportTugboatsAsync(IFormFile file, ImportMaps maps)
        {
            using var reader = new StreamReader(file.OpenReadStream());
            using var csv = new CsvReader(reader, _csvConfig);
            var records = csv.GetRecords<dynamic>();
            int count = 0, skipped = 0;
            var newRecords = new List<Tugboat>();

            foreach (var record in records)
            {
                string msapNumber = PadNumber(GetString(record, "number"), 3);
                if (maps.Tugboat.ContainsKey(msapNumber)) { skipped++; continue; }

                string ownerNo = PadNumber(GetString(record, "owner"), 3);
                int? ownerId = null;
                if (ownerNo != "-" && maps.Owner.TryGetValue(ownerNo, out int id))
                {
                    ownerId = id;
                }
                else if (ownerNo != "-")
                {
                    _importErrors.Add($"Tugboat {msapNumber}: Owner {ownerNo} not found. Setting as null.");
                }

                var entity = new Tugboat
                {
                    TugboatNumber = msapNumber,
                    TugboatName = GetString(record, "name"),
                    IsCompanyOwned = ParseBool(record, "companyown"),
                    TugboatOwnerId = ownerId
                };

                newRecords.Add(entity);
                count++;
            }

            if (newRecords.Count > 0)
            {
                await dbContext.MMSITugboats.AddRangeAsync(newRecords);
                await dbContext.SaveChangesAsync();
                foreach (var r in newRecords) maps.Tugboat[r.TugboatNumber] = r.TugboatId;
            }

            return $"Tugboats: {count} imported, {skipped} already existed.";
        }

        private async Task<string> ImportPrincipalsAsync(IFormFile file, ImportMaps maps)
        {
            using var reader = new StreamReader(file.OpenReadStream());
            using var csv = new CsvReader(reader, _csvConfig);
            var records = csv.GetRecords<dynamic>();
            int count = 0, skipped = 0;
            var newRecords = new List<Principal>();

            foreach (var record in records)
            {
                string msapNumber = PadNumber(GetString(record, "number"), 4);
                if (maps.Principal.ContainsKey(msapNumber)) { skipped++; continue; }

                string agentNo = PadNumber(GetString(record, "agent"), 4);
                if (!maps.Customer.TryGetValue(agentNo, out int customerId))
                {
                    _importErrors.Add($"Principal {msapNumber}: Agent/Customer {agentNo} not found. Skipping.");
                    continue;
                }

                var entity = new Principal
                {
                    PrincipalNumber = msapNumber,
                    PrincipalName = GetString(record, "name"),
                    Agent = GetString(record, "agent"),
                    Address1 = GetString(record, "address1"),
                    Address2 = GetString(record, "address2"),
                    Address3 = GetString(record, "address3"),
                    TIN = NormalizeTin(GetString(record, "tin"), "000-000-000000"),
                    BusinessType = GetString(record, "business"),
                    Terms = MapTerms(GetString(record, "terms")),
                    Landline1 = GetString(record, "landline1"),
                    Landline2 = GetString(record, "landline2"),
                    Mobile1 = GetString(record, "mobile1"),
                    Mobile2 = GetString(record, "mobile2"),
                    IsVatable = ParseBool(record, "vatable"),
                    IsActive = ParseBool(record, "active"),
                    CustomerId = customerId
                };

                newRecords.Add(entity);
                count++;
            }

            if (newRecords.Count > 0)
            {
                await dbContext.MMSIPrincipals.AddRangeAsync(newRecords);
                await dbContext.SaveChangesAsync();
                foreach (var r in newRecords) maps.Principal[r.PrincipalNumber] = r.PrincipalId;
            }

            return $"Principals: {count} imported, {skipped} already existed.";
        }

        private async Task<string> ImportTariffRatesAsync(IFormFile file, ImportMaps maps)
        {
            using var reader = new StreamReader(file.OpenReadStream());
            using var csv = new CsvReader(reader, _csvConfig);
            var records = csv.GetRecords<dynamic>();
            int count = 0, skipped = 0;
            var newRecords = new List<TariffRate>();

            var terminalToPortMap = await dbContext.MMSITerminals.AsNoTracking().ToDictionaryAsync(t => t.TerminalId, t => t.PortId);

            foreach (var record in records)
            {
                string custNo = PadNumber(GetString(record, "custno"), 4);
                string terminalRaw = GetString(record, "terminal");
                string serviceNum = PadNumber(GetString(record, "service"), 3);

                if (!maps.Customer.TryGetValue(custNo, out int customerId)) { _importErrors.Add($"Tariff: Customer {custNo} not found. Skipping."); continue; }

                string? terminalKey = NormalizeTerminalCode(terminalRaw);
                if (terminalKey == null || !maps.Terminal.TryGetValue(terminalKey, out int terminalId))
                {
                    _importErrors.Add($"Tariff: Terminal {terminalRaw} not found. Skipping.");
                    continue;
                }

                if (!maps.Service.TryGetValue(serviceNum, out int serviceId)) { _importErrors.Add($"Tariff: Service {serviceNum} not found. Skipping."); continue; }

                var asOfDate = ParseDateOnly(record, "date") ?? DateOnly.FromDateTime(DateTime.Today);
                string key = $"{asOfDate:yyyy-MM-dd}|{customerId}|{terminalId}|{serviceId}";
                if (maps.TariffRate.Contains(key)) { skipped++; continue; }

                var entity = new TariffRate
                {
                    AsOfDate = asOfDate,
                    CustomerId = customerId,
                    TerminalId = terminalId,
                    ServiceId = serviceId,
                    PortId = terminalToPortMap.GetValueOrDefault(terminalId),
                    Dispatch = ParseDecimal(record, "dispatch"),
                    BAF = ParseDecimal(record, "baf"),
                    CreatedBy = GetString(record, "createdby"),
                    CreatedDate = ParseDateTime(record, "createddat") ?? DateTimeHelper.GetCurrentPhilippineTime()
                };

                if (entity.PortId == 0) { _importErrors.Add($"Tariff: Port not found for terminal {terminalRaw}. Skipping."); continue; }

                newRecords.Add(entity);
                count++;
                maps.TariffRate.Add(key);
            }

            if (newRecords.Count > 0)
            {
                await dbContext.MMSITariffRates.AddRangeAsync(newRecords);
            }

            return $"Tariff Rates: {count} imported, {skipped} already existed.";
        }

        private async Task<string> ImportCollectionsAsync(IFormFile file, ImportMaps maps)
        {
            using var reader = new StreamReader(file.OpenReadStream());
            using var csv = new CsvReader(reader, _csvConfig);
            var records = csv.GetRecords<dynamic>();
            int count = 0, skipped = 0;
            var newRecords = new List<Collection>();

            foreach (var record in records)
            {
                string crNum = GetString(record, "crnum");
                if (maps.Collection.ContainsKey(crNum)) { skipped++; continue; }

                string custNo = PadNumber(GetString(record, "custno"), 4);
                if (!maps.Customer.TryGetValue(custNo, out int customerId))
                {
                    _importErrors.Add($"Collection {crNum}: Customer {custNo} not found. Skipping.");
                    continue;
                }

                string checkDate = GetString(record, "checkdate");

                var entity = new Collection
                {
                    MMSICollectionNumber = crNum,
                    CustomerId = customerId,
                    CheckNumber = GetString(record, "checkno"),
                    Status = "Create",
                    Company = "MMSI",
                    CashAmount = 0,
                    WVAT = 0,
                    Date = ParseDateOnly(record, "crdate") ?? DateOnly.FromDateTime(DateTime.Today),
                    DepositDate = ParseDateOnly(record, "datedeposi"),
                    Amount = ParseDecimal(record, "amount"),
                    EWT = ParseDecimal(record, "n2307"),
                    IsUndocumented = ParseBool(record, "undocument"),
                    CreatedDate = ParseDateTime(record, "createddat") ?? DateTimeHelper.GetCurrentPhilippineTime(),
                    CreatedBy = GetString(record, "createdby"),
                    CheckDate = (checkDate != "/  /") ? ParseDateOnly(record, "checkdate") : null,
                };

                entity.CheckAmount = entity.Amount;
                entity.Total = entity.Amount + entity.EWT;

                newRecords.Add(entity);
                count++;
                maps.Collection[crNum] = -1; // Temp mark to avoid duplicates in same file

                if (newRecords.Count >= 500)
                {
                    await dbContext.MMSICollections.AddRangeAsync(newRecords);
                    await dbContext.SaveChangesAsync();
                    foreach (var r in newRecords) maps.Collection[r.MMSICollectionNumber] = r.MMSICollectionId;
                    newRecords.Clear();
                }
            }

            if (newRecords.Count > 0)
            {
                await dbContext.MMSICollections.AddRangeAsync(newRecords);
                await dbContext.SaveChangesAsync();
                foreach (var r in newRecords) maps.Collection[r.MMSICollectionNumber] = r.MMSICollectionId;
            }

            return $"Collections: {count} imported, {skipped} already existed.";
        }

        private async Task<string> ImportBillingsAsync(IFormFile file, ImportMaps maps)
        {
            using var reader = new StreamReader(file.OpenReadStream());
            using var csv = new CsvReader(reader, _csvConfig);
            var records = csv.GetRecords<dynamic>();
            int count = 0, skipped = 0;
            var newRecords = new List<Billing>();

            foreach (var record in records)
            {
                string billingNumber = GetString(record, "number");
                if (maps.Billing.ContainsKey(billingNumber)) { skipped++; continue; }

                string custNo = PadNumber(GetString(record, "custno"), 4);
                string vesselNum = PadNumber(GetString(record, "vesselnum"), 4);
                string terminalRaw = GetString(record, "terminal");
                string portNumRaw = PadNumber(GetString(record, "portnum"), 3);
                string principalNum = PadNumber(GetString(record, "billto"), 4);
                string crNum = GetString(record, "crnum");

                if (!maps.Customer.TryGetValue(custNo, out int customerId)) { _importErrors.Add($"Billing {billingNumber}: Customer {custNo} not found. Skipping."); continue; }
                if (!maps.Vessel.TryGetValue(vesselNum, out int vesselId)) { _importErrors.Add($"Billing {billingNumber}: Vessel {vesselNum} not found. Skipping."); continue; }

                var (pId, tId) = ResolvePortTerminal(terminalRaw, portNumRaw, maps);
                if (pId == null) { _importErrors.Add($"Billing {billingNumber}: Port reference not found. Skipping."); continue; }
                if (tId == null) { _importErrors.Add($"Billing {billingNumber}: Terminal reference not found. Skipping."); continue; }

                int? principalId = maps.Principal.TryGetValue(principalNum, out int pid) ? pid : null;
                int? collectionId = maps.Collection.TryGetValue(crNum, out int cid) ? cid : null;

                DateOnly billingDate = ParseDateOnly(record, "date") ?? DateOnly.FromDateTime(DateTime.Today);

                var entity = new Billing
                {
                    MMSIBillingNumber = billingNumber,
                    CustomerId = customerId,
                    VesselId = vesselId,
                    PortId = pId.Value,
                    TerminalId = tId.Value,
                    PrincipalId = principalId,
                    IsPrincipal = principalId.HasValue,
                    BilledTo = principalId.HasValue ? "PRINCIPAL" : "LOCAL",
                    CollectionId = collectionId,
                    CollectionNumber = crNum != "-" ? crNum : null,
                    Status = collectionId.HasValue ? "Collected" : "For Collection",
                    Date = billingDate,
                    DueDate = billingDate,
                    Amount = ParseDecimal(record, "amount"),
                    Balance = ParseDecimal(record, "amount"),
                    AmountPaid = 0,
                    IsPaid = false,
                    Company = "MMSI",
                    IsUndocumented = ParseBool(record, "undocument"),
                    ApOtherTug = ParseDecimal(record, "apothertug"),
                    DispatchAmount = ParseDecimal(record, "dispatcham"),
                    BAFAmount = ParseDecimal(record, "bafamount"),
                    Discount = 0,
                    IsVatable = ParseBool(record, "vat"),
                    IsPrinted = ParseBool(record, "printed"),
                    VoyageNumber = NullIfDash(GetString(record, "voyage")),
                    CreatedDate = ParseDateTime(record, "entrydate") ?? DateTimeHelper.GetCurrentPhilippineTime(),
                    CreatedBy = GetString(record, "entryby"),
                };

                newRecords.Add(entity);
                count++;
                maps.Billing[billingNumber] = -1;

                if (newRecords.Count >= 500)
                {
                    await dbContext.Billings.AddRangeAsync(newRecords);
                    await dbContext.SaveChangesAsync();
                    foreach (var r in newRecords) maps.Billing[r.MMSIBillingNumber] = r.MMSIBillingId;
                    newRecords.Clear();
                }
            }

            if (newRecords.Count > 0)
            {
                await dbContext.Billings.AddRangeAsync(newRecords);
                await dbContext.SaveChangesAsync();
                foreach (var r in newRecords) maps.Billing[r.MMSIBillingNumber] = r.MMSIBillingId;
            }

            return $"Billings: {count} imported, {skipped} already existed.";
        }

        private async Task<string> ImportDispatchTicketsAsync(IFormFile file, ImportMaps maps)
        {
            using var reader = new StreamReader(file.OpenReadStream());
            using var csv = new CsvReader(reader, _csvConfig);
            var records = csv.GetRecords<dynamic>();
            int count = 0, skipped = 0;
            var newRecords = new List<DispatchTicket>();

            foreach (var record in records)
            {
                string dispatchNo = GetString(record, "number");
                if (maps.DispatchTicket.Contains(dispatchNo)) { skipped++; continue; }

                string custNoRaw = GetString(record, "custno");
                string custNo = (custNoRaw == "-" || string.IsNullOrWhiteSpace(custNoRaw)) ? "0000" : PadNumber(custNoRaw, 4);
                string tugboatNum = PadNumber(GetString(record, "tugnum"), 3);
                string vesselNum = PadNumber(GetString(record, "vesselnum"), 4);
                string serviceNum = PadNumber(GetString(record, "srvctype"), 3);
                string terminalRaw = GetString(record, "terminal");
                string billNumStr = GetString(record, "billnum");

                if (!maps.Customer.TryGetValue(custNo, out int customerId))
                {
                    if (custNo == "0000")
                    {
                        var unknown = await dbContext.Customers.FirstOrDefaultAsync(c => c.CustomerCode == "0000" && c.Company == "MMSI");
                        if (unknown == null)
                        {
                            unknown = new Customer
                            {
                                CustomerCode = "0000",
                                CustomerName = "-",
                                CustomerAddress = "-",
                                CustomerTin = "000-000-000-00000",
                                CustomerTerms = "COD",
                                CustomerType = "Industrial",
                                VatType = "Vatable",
                                ZipCode = "0000",
                                Company = "MMSI",
                                CreatedBy = "System Import Fallback",
                                CreatedDate = DateTimeHelper.GetCurrentPhilippineTime()
                            };
                            dbContext.Customers.Add(unknown);
                            await dbContext.SaveChangesAsync();
                        }
                        maps.Customer["0000"] = unknown.CustomerId;
                        customerId = unknown.CustomerId;
                    }
                    else
                    {
                        _importErrors.Add($"Dispatch {dispatchNo}: Customer {custNo} not found. Skipping.");
                        continue;
                    }
                }

                if (!maps.Tugboat.TryGetValue(tugboatNum, out int tugboatId)) { _importErrors.Add($"Dispatch {dispatchNo}: Tugboat {tugboatNum} not found. Skipping."); continue; }
                if (!maps.Vessel.TryGetValue(vesselNum, out int vesselId)) { _importErrors.Add($"Dispatch {dispatchNo}: Vessel {vesselNum} not found. Skipping."); continue; }
                if (!maps.Service.TryGetValue(serviceNum, out int serviceId)) { _importErrors.Add($"Dispatch {dispatchNo}: Service {serviceNum} not found. Skipping."); continue; }

                var (pId, tId) = ResolvePortTerminal(terminalRaw, "-", maps);
                if (pId == null || tId == null) { _importErrors.Add($"Dispatch {dispatchNo}: Port/Terminal not found. Skipping."); continue; }

                int? billingId = maps.Billing.TryGetValue(billNumStr, out int bid) ? bid : null;

                var entity = new DispatchTicket
                {
                    DispatchNumber = dispatchNo,
                    CustomerId = customerId,
                    TugBoatId = tugboatId,
                    VesselId = vesselId,
                    ServiceId = serviceId,
                    PortId = pId.Value,
                    TerminalId = tId.Value,
                    BillingId = billingId,
                    BillingNumber = billNumStr != "-" ? billNumStr : null,
                    ApOtherTugs = ParseDecimal(record, "apothertug"),
                    DispatchChargeType = ParseBool(record, "perhour") ? "Per hour" : "Per move",
                    BAFChargeType = "Per move",
                    COSNumber = NullIfDash(GetString(record, "cosno")),
                    VoyageNumber = NullIfDash(GetString(record, "voyage")),
                    Remarks = NullIfDash(GetString(record, "remarks")),
                    DateLeft = ParseDateOnly(record, "dateleft"),
                    TimeLeft = ParseTimeOnly(record, "timeleft"),
                    DateArrived = ParseDateOnly(record, "datearrive"),
                    TimeArrived = ParseTimeOnly(record, "timearrive"),
                    Date = ParseDateOnly(record, "date"),
                    CreatedBy = GetString(record, "createdby"),
                    CreatedDate = ParseDateTime(record, "date") ?? DateTimeHelper.GetCurrentPhilippineTime(),
                    Status = ParseBool(record, "approved")
                        ? (billNumStr != "-" ? "Billed" : "For Billing")
                        : "For Tariff",
                };

                entity.TotalHours = ComputeTotalHours(entity, entity.CustomerId);

                newRecords.Add(entity);
                count++;
                maps.DispatchTicket.Add(dispatchNo);

                if (newRecords.Count >= 500)
                {
                    await dbContext.MMSIDispatchTickets.AddRangeAsync(newRecords);
                    await dbContext.SaveChangesAsync();
                    newRecords.Clear();
                }
            }

            if (newRecords.Count > 0)
            {
                await dbContext.MMSIDispatchTickets.AddRangeAsync(newRecords);
                await dbContext.SaveChangesAsync();
            }

            return $"Dispatch Tickets: {count} imported, {skipped} already existed.";
        }

        // -------------------------------------------------------------------------
        // Shared helpers
        // -------------------------------------------------------------------------

        private static (int? PortId, int? TerminalId) ResolvePortTerminal(
            string terminalRaw, string portNumRaw, ImportMaps maps)
        {
            string? terminalKey = NormalizeTerminalCode(terminalRaw);
            if (terminalKey != null)
            {
                string portPart = terminalKey.Substring(0, 3);
                int? portId = maps.Port.TryGetValue(portPart, out int pid) ? pid : null;
                int? terminalId = maps.Terminal.TryGetValue(terminalKey, out int tid) ? tid : null;
                return (portId, terminalId);
            }

            if (portNumRaw != "-" && maps.Port.TryGetValue(portNumRaw, out int pId))
            {
                return (pId, null);
            }

            return (null, null);
        }

        private static decimal ComputeTotalHours(DispatchTicket dt, int customerId)
        {
            if (dt.DateLeft == null || dt.DateArrived == null
                || dt.TimeLeft == null || dt.TimeArrived == null)
                return 0;

            var dtl       = dt.DateLeft.Value.ToDateTime(dt.TimeLeft.Value);
            var dta       = dt.DateArrived.Value.ToDateTime(dt.TimeArrived.Value);
            var totalHours = Math.Round((decimal)(dta - dtl).TotalHours, 2);

            // Customer 179 uses quarter-hour rounding rules
            if (customerId == 179)
            {
                var whole      = Math.Truncate(totalHours);
                var fractional = totalHours - whole;
                totalHours = fractional >= 0.75m ? whole + 1.0m
                           : fractional >= 0.25m ? whole + 0.5m
                           : whole;
            }

            return totalHours;
        }

        private static IFormFile? ResolveFile(
            IFormFile? individual, List<IFormFile>? bulk, string keyword, string? exactName = null)
        {
            if (individual != null) return individual;
            if (bulk == null || bulk.Count == 0) return null;

            if (exactName != null)
            {
                var exact = bulk.FirstOrDefault(f =>
                    f.FileName.Equals(exactName, StringComparison.OrdinalIgnoreCase));
                if (exact != null) return exact;
            }

            var byCsv = bulk.FirstOrDefault(f =>
                f.FileName.Equals($"{keyword}.csv", StringComparison.OrdinalIgnoreCase));
            if (byCsv != null) return byCsv;

            return bulk.FirstOrDefault(f =>
                f.FileName.Contains(keyword, StringComparison.OrdinalIgnoreCase));
        }

        private static string GetString(dynamic record, string propertyName)
        {
            var dict = (IDictionary<string, object>)record;
            var key  = propertyName.Trim().ToLower();
            if (dict.TryGetValue(key, out var value))
            {
                var str = value.ToString()?.Trim();
                return string.IsNullOrWhiteSpace(str) ? "-" : str;
            }
            return "-";
        }

        private static bool ParseBool(dynamic record, string propertyName)
        {
            var val = GetString(record, propertyName).ToLower();
            return val == "t" || val == "true";
        }

        private static decimal ParseDecimal(dynamic record, string propertyName)
        {
            var val = GetString(record, propertyName);
            return decimal.TryParse(val, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal result)
                ? result : 0;
        }

        private static DateOnly? ParseDateOnly(dynamic record, string propertyName)
        {
            var val = GetString(record, propertyName);
            return DateOnly.TryParse(val, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly result)
                ? result : null;
        }

        private static DateTime? ParseDateTime(dynamic record, string propertyName)
        {
            var val = GetString(record, propertyName);
            return DateTime.TryParse(val, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime result)
                ? result : null;
        }

        private static TimeOnly? ParseTimeOnly(dynamic record, string propertyName)
        {
            var val = GetString(record, propertyName).Trim();

            // Handle legacy compact format: "330" → 03:30, "1915" → 19:15
            if (int.TryParse(val, out int _) && val.Length <= 4)
            {
                int hours = 0, minutes;
                if      (val.Length == 4) { hours = int.Parse(val.Substring(0, 2)); minutes = int.Parse(val.Substring(2, 2)); }
                else if (val.Length == 3) { hours = int.Parse(val.Substring(0, 1)); minutes = int.Parse(val.Substring(1, 2)); }
                else                      { minutes = int.Parse(val); }

                if (hours is >= 0 and < 24 && minutes is >= 0 and < 60)
                    return new TimeOnly(hours, minutes);
            }

            return TimeOnly.TryParse(val, CultureInfo.InvariantCulture, DateTimeStyles.None, out TimeOnly result)
                ? result : null;
        }

        private static string PadNumber(string? value, int width)
        {
            if (string.IsNullOrEmpty(value) || value == "-") return value ?? string.Empty;
            return int.TryParse(value, out var n)
                ? n.ToString($"D{width}")
                : value.PadLeft(width, '0');
        }

        private static string? NormalizeTerminalCode(string? value)
        {
            if (string.IsNullOrWhiteSpace(value) || value == "-") return null;
            if (value.Length < 6) return null;

            string portPart = PadNumber(value.Substring(0, 3), 3);
            string terminalPart = PadNumber(value.Substring(value.Length - 3, 3), 3);
            return $"{portPart}{terminalPart}";
        }

        private static string MapTerms(string raw) => raw switch
        {
            "7"  => "7D",
            "15" => "15D",
            "30" => "30D",
            "60" => "60D",
            _    => "COD"
        };

        /// <summary>
        /// Returns the fallback value when the field is the "-" sentinel,
        /// used only for fields that require a specific format (TIN).
        /// </summary>
        private static string NormalizeTin(string value, string fallback) =>
            value == "-" ? fallback : value;

        /// <summary>Returns null for "-" sentinel on optional string fields.</summary>
        private static string? NullIfDash(string value) =>
            value == "-" ? null : value;

        private static string JoinAddress(string a1, string a2, string a3)
        {
            var joined = $"{a1} {a2} {a3}".Replace("-", "").Trim();
            return string.IsNullOrWhiteSpace(joined) ? "-" : joined;
        }
    }
}
