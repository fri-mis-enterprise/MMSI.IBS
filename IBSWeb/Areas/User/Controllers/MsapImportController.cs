using IBS.Models.Books;
using IBS.Models.Integrated;
using IBS.Models.MasterFile;
using IBS.Utility.Constants;
using CsvHelper;
using CsvHelper.Configuration;
using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using IBS.Models.MMSI;
using IBS.Models.MMSI.MasterFile;
using IBS.Services;
using IBS.Services.AccessControl;
using IBS.Utility.Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Security.Claims;
using System.Text;

namespace IBSWeb.Areas.User.Controllers
{
    [Area("User")]
    public class MsapImportController : MmsiBaseController
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<MsapImportController> _logger;

        private readonly CsvConfiguration _csvConfig = new(CultureInfo.InvariantCulture)
        {
            PrepareHeaderForMatch = args => args.Header.ToLower(),
        };

        public MsapImportController(
            IUnitOfWork unitOfWork,
            ApplicationDbContext dbContext,
            IAccessControlService accessControl,
            UserManager<ApplicationUser> userManager,
            ILogger<MsapImportController> logger)
            : base(accessControl, userManager)
        {
            _dbContext = dbContext;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        private string GetUserFullName()
        {
            return User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.GivenName)?.Value
                    ?? User.Identity?.Name
                    ?? "Unknown";
        }

        private async Task<string?> GetCompanyClaimAsync()
        {
            var user = await UserManager.GetUserAsync(User);

            if (user == null)
            {
                return null;
            }

            var claims = await UserManager.GetClaimsAsync(user);
            return claims.FirstOrDefault(c => c.Type == "Company")?.Value;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            if (!await HasMsapImportAccessAsync())
            {
                TempData["error"] = "Access denied.";
                return RedirectToAction("Index", "Home", new { area = "User" });
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(List<string> fieldList)
        {
            if (!await HasMsapImportAccessAsync())
            {
                TempData["error"] = "Access denied.";
                return RedirectToAction("Index", "Home", new { area = "User" });
            }

            try
            {
                if (fieldList == null || fieldList.Count == 0)
                {
                    TempData["error"] = "Please select at least one import field.";
                    return RedirectToAction(nameof(Index));
                }
                var sb = new StringBuilder();

                foreach (string field in fieldList)
                {
                    string importResult = await ImportFromCSV(field);
                    sb.AppendLine(importResult);
                }

                TempData["success"] = sb.ToString().Replace(Environment.NewLine, "\\n");
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        public async Task<string> ImportFromCSV(string field, CancellationToken cancellationToken = default)
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var customerCSVPath = @"C:\Users\MIS2\Documents\CSV_OUTPUTS\customer.csv";
                var portCSVPath = @"C:\Users\MIS2\Documents\CSV_OUTPUTS\port.csv";
                var terminalCSVPath = @"C:\Users\MIS2\Documents\CSV_OUTPUTS\terminal.csv";
                var principalCSVPath = @"C:\Users\MIS2\Documents\CSV_OUTPUTS\principal.csv";
                var servicesCSVPath = @"C:\Users\MIS2\Documents\CSV_OUTPUTS\services.csv";
                var tugboatCSVPath = @"C:\Users\MIS2\Documents\CSV_OUTPUTS\tugboat.csv";
                var tugboatOwnerCSVPath = @"C:\Users\MIS2\Documents\CSV_OUTPUTS\tugboatowner.csv";
                var tugMasterCSVPath = @"C:\Users\MIS2\Documents\CSV_OUTPUTS\tugmaster.csv";
                var vesselCSVPath = @"C:\Users\MIS2\Documents\CSV_OUTPUTS\vessel.csv";
                var dispatchTicketCSVPath = @"C:\Users\MIS2\Documents\CSV_OUTPUTS\dispatch.csv";
                var billingCSVPath = @"C:\Users\MIS2\Documents\CSV_OUTPUTS\billing.csv";
                var collectionCSVPath = @"C:\Users\MIS2\Documents\CSV_OUTPUTS\collection.csv";
                var tariffCSVPath = @"C:\Users\MIS2\Documents\CSV_OUTPUTS\tariff.csv";

                string result;

                switch (field)
                {
                    #region -- Masterfiles --

                    case "Customer":
                        {
                            result = await ImportMsapCustomers(customerCSVPath, cancellationToken);
                            break;
                        }
                    case "Port":
                        {
                            result = await ImportMsapPorts(portCSVPath, cancellationToken);
                            break;
                        }
                    case "Terminal":
                        {
                            result = await ImportMsapTerminals(terminalCSVPath, cancellationToken);
                            break;
                        }
                    case "Principal":
                        {
                            result = await ImportMsapPrincipals(principalCSVPath, customerCSVPath, cancellationToken);
                            break;
                        }
                    case "Service":
                        {
                            result = await ImportMsapServices(servicesCSVPath, cancellationToken);
                            break;
                        }
                    case "Tugboat":
                        {
                            result = await ImportMsapTugboats(tugboatCSVPath, cancellationToken);
                            break;
                        }
                    case "TugboatOwner":
                        {
                            result = await ImportMsapTugboatOwners(tugboatOwnerCSVPath, cancellationToken);
                            break;
                        }
                    case "TugMaster":
                        {
                            result = await ImportMsapTugMasters(tugMasterCSVPath, cancellationToken);
                            break;
                        }
                    case "Vessel":
                        {
                            result = await ImportMsapVessels(vesselCSVPath, cancellationToken);
                            break;
                        }
                    case "Tariff":
                        {
                            result = await ImportMsapTariffRates(tariffCSVPath, customerCSVPath, cancellationToken);
                            break;
                        }

                    #endregion -- Masterfiles --

                    #region -- Data entries --

                    case "DispatchTicket":
                        {
                            result = await ImportMsapDispatchTickets(dispatchTicketCSVPath, customerCSVPath, cancellationToken);
                            break;
                        }
                    case "Billing":
                        {
                            result = await ImportMsapBillings(billingCSVPath, customerCSVPath, cancellationToken);
                            break;
                        }
                    case "Collection":
                        {
                            result = await ImportMsapCollections(collectionCSVPath, customerCSVPath, cancellationToken);
                            break;
                        }

                    #endregion -- Data entries --

                    default:
                        result = $"{field} field is invalid";
                        break;
                }

                await transaction.CommitAsync(cancellationToken);
                return result;
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                await transaction.RollbackAsync(cancellationToken);
                throw new InvalidOperationException(ex.Message);
            }
        }

        #region -- Helpers --

        private static string? GetString(dynamic record, string propertyName)
        {
            var dict = (IDictionary<string, object>)record;
            if (dict.TryGetValue(propertyName, out var value))
            {
                return value?.ToString();
            }
            return null;
        }

        private static bool ParseBool(dynamic record, string propertyName)
        {
            string? val = GetString(record, propertyName);
            if (string.IsNullOrEmpty(val)) return false;
            val = val.Trim().ToLower();
            return val == "t" || val == "true";
        }

        private static decimal ParseDecimal(dynamic record, string propertyName)
        {
            string? val = GetString(record, propertyName);
            if (string.IsNullOrEmpty(val)) return 0;
            if (decimal.TryParse(val, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal result))
            {
                return result;
            }
            return 0;
        }

        private static DateOnly? ParseDateOnly(dynamic record, string propertyName)
        {
            string? val = GetString(record, propertyName);
            if (string.IsNullOrEmpty(val)) return null;
            if (DateOnly.TryParse(val, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly result))
            {
                return result;
            }
            return null;
        }

        private static DateTime? ParseDateTime(dynamic record, string propertyName)
        {
            string? val = GetString(record, propertyName);
            if (string.IsNullOrEmpty(val)) return null;
            if (DateTime.TryParse(val, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime result))
            {
                return result;
            }
            return null;
        }

        private static string PadNumber(string? value, int width)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            return int.TryParse(value, out var n)
                ? n.ToString($"D{width}")
                : value.PadLeft(width, '0');
        }

        #endregion

        #region -- Masterfiles --

        public async Task<string> ImportMsapCustomers(string customerCSVPath, CancellationToken cancellationToken)
        {
            var existingNames = (await _unitOfWork.Customer.GetAllAsync(c => c.Company == "MMSI", cancellationToken))
                .Select(c => c.CustomerName).ToHashSet();

            var currentBatchNames = new HashSet<string>();
            var customerList = new List<Customer>();
            using var reader = new StreamReader(customerCSVPath);
            using var csv = new CsvReader(reader, _csvConfig);
            var records = csv.GetRecords<dynamic>().ToList();

            foreach (var record in records)
            {
                string customerName = GetString(record, "name") ?? string.Empty;

                if (string.IsNullOrEmpty(customerName) || existingNames.Contains(customerName) || currentBatchNames.Contains(customerName))
                {
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

                newCustomer.CustomerCode = await _unitOfWork.Customer.GenerateCodeAsync("Industrial", cancellationToken);
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
                newCustomer.Company = await GetCompanyClaimAsync() ?? "MMSI";
                newCustomer.ZipCode = "0000";
                newCustomer.IsMMSI = true;
                newCustomer.Type = "Documented";

                customerList.Add(newCustomer);
                currentBatchNames.Add(customerName);
            }

            await _dbContext.Customers.AddRangeAsync(customerList, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return $"Customers imported successfully, {customerList.Count} new records";
        }

        public async Task<string> ImportMsapPorts(string portCSVPath, CancellationToken cancellationToken)
        {
            var existingIdentifier = (await _dbContext.MMSIPorts.ToListAsync(cancellationToken))
                .Select(c => c.PortNumber).ToHashSet();

            var currentBatch = new HashSet<string>();
            var newRecords = new List<MMSIPort>();
            using var reader = new StreamReader(portCSVPath);
            using var csv = new CsvReader(reader, _csvConfig);
            var records = csv.GetRecords<dynamic>().ToList();

            foreach (var record in records)
            {
                string padded = PadNumber(GetString(record, "number"), 3);

                if (string.IsNullOrEmpty(padded) || existingIdentifier.Contains(padded) || currentBatch.Contains(padded))
                {
                    continue;
                }

                MMSIPort newRecord = new MMSIPort();
                newRecord.PortNumber = padded;
                newRecord.PortName = GetString(record, "name");

                newRecords.Add(newRecord);
                currentBatch.Add(padded);
            }

            await _dbContext.MMSIPorts.AddRangeAsync(newRecords, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return $"Ports imported successfully, {newRecords.Count} new records";
        }

        public async Task<string> ImportMsapTerminals(string terminalCSVPath, CancellationToken cancellationToken)
        {
            var existingIdentifier = (await _dbContext.MMSITerminals.Include(t => t.Port).ToListAsync(cancellationToken))
                .Select(c => new { PortNumber = (string?)c.Port!.PortNumber, TerminalNumber = (string?)c.TerminalNumber }).ToHashSet();

            var existingPorts = (await _dbContext.MMSIPorts.ToListAsync(cancellationToken))
                .Select(p => new { p.PortId, p.PortNumber }).ToList();

            var currentBatch = new HashSet<object>();
            var newRecords = new List<MMSITerminal>();
            using var reader = new StreamReader(terminalCSVPath);
            using var csv = new CsvReader(reader, _csvConfig);
            var records = csv.GetRecords<dynamic>().ToList();

            foreach (var record in records)
            {
                string terminalComposite = GetString(record, "number") ?? string.Empty;
                if (terminalComposite.Length < 6) { continue; }
                var portPart = terminalComposite.Substring(0, 3);
                var terminalPart = terminalComposite.Substring(terminalComposite.Length - 3, 3);
                
                string paddedPortNumber = PadNumber(portPart, 3);
                string paddedTerminalNumber = PadNumber(terminalPart, 3);

                var identity = new { PortNumber = (string?)paddedPortNumber, TerminalNumber = (string?)paddedTerminalNumber };

                if (existingIdentifier.Contains(identity) || currentBatch.Contains(identity))
                {
                    continue;
                }

                MMSITerminal newRecord = new MMSITerminal();
                var port = existingPorts.FirstOrDefault(p => p.PortNumber == paddedPortNumber);
                if (port == null) { continue; }
                newRecord.PortId = port.PortId;
                newRecord.TerminalName = GetString(record, "name");
                newRecord.TerminalNumber = paddedTerminalNumber;

                newRecords.Add(newRecord);
                currentBatch.Add(identity);
            }

            await _dbContext.MMSITerminals.AddRangeAsync(newRecords, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return $"Terminals imported successfully, {newRecords.Count} new records";
        }

        public async Task<string> ImportMsapPrincipals(string principalCSVPath, string customerCSVPath, CancellationToken cancellationToken)
        {
            var existingIdentifier = (await _dbContext.MMSIPrincipals.ToListAsync(cancellationToken))
                .Select(c => new { PrincipalNumber = c.PrincipalNumber, PrincipalName = c.PrincipalName, CustomerId = c.CustomerId }).ToHashSet();

            var mmsiCustomers = await _unitOfWork.Customer
                .GetAllAsync(c => c.Company == "MMSI", cancellationToken);

            var newRecords = new List<MMSIPrincipal>();
            using var reader = new StreamReader(principalCSVPath);
            using var csv = new CsvReader(reader, _csvConfig);
            var records = csv.GetRecords<dynamic>().ToList();

            using var reader2 = new StreamReader(customerCSVPath);
            using var csv2 = new CsvReader(reader2, _csvConfig);
            var customers = csv2.GetRecords<dynamic>().ToList();

            var customersList = customers
                .Select(c =>
                {
                    string? name = GetString(c, "name");
                    var matchedCustomer = mmsiCustomers.FirstOrDefault(cu => cu.CustomerName == name);
                    return matchedCustomer == null ? null : new
                    {
                        CustomerId = (int)matchedCustomer.CustomerId,
                        CustomerNumber = GetString(c, "number"),
                        CustomerName = name
                    };
                })
                .Where(c => c != null)
                .ToList();

            var currentBatch = new HashSet<object>();

            foreach (var record in records)
            {
                string padded = PadNumber(GetString(record, "number"), 4);
                string paddedCustomerNumber = PadNumber(GetString(record, "agent"), 4);
                
                var agent = customersList.FirstOrDefault(c => c?.CustomerNumber == paddedCustomerNumber);
                if (agent == null) continue;

                var identity = new
                {
                    PrincipalNumber = padded,
                    PrincipalName = (string)(GetString(record, "name") ?? string.Empty),
                    CustomerId = agent.CustomerId
                };

                if (existingIdentifier.Contains(identity) || currentBatch.Contains(identity))
                {
                    continue;
                }

                MMSIPrincipal newRecord = new MMSIPrincipal();

                switch (GetString(record, "terms"))
                {
                    case "7": newRecord.Terms = "7D"; break;
                    case "0": newRecord.Terms = "COD"; break;
                    case "15": newRecord.Terms = "15D"; break;
                    case "30": newRecord.Terms = "30D"; break;
                    case "60": newRecord.Terms = "60D"; break;
                }

                newRecord.CustomerId = agent.CustomerId;
                newRecord.PrincipalNumber = padded;
                newRecord.PrincipalName = GetString(record, "name") ?? "UNKNOWN";
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
                currentBatch.Add(identity);
            }

            await _dbContext.MMSIPrincipals.AddRangeAsync(newRecords, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return $"Principals imported successfully, {newRecords.Count} new records";
        }

        public async Task<string> ImportMsapServices(string serviceCSVPath, CancellationToken cancellationToken)
        {
            var existingIdentifier = (await _dbContext.MMSIServices.ToListAsync(cancellationToken))
                .Select(c => c.ServiceNumber).ToHashSet();

            var currentBatch = new HashSet<string>();
            var newRecords = new List<MMSIService>();
            using var reader = new StreamReader(serviceCSVPath);
            using var csv = new CsvReader(reader, _csvConfig);
            var records = csv.GetRecords<dynamic>().ToList();

            foreach (var record in records)
            {
                string padded = PadNumber(GetString(record, "number"), 3);

                if (string.IsNullOrEmpty(padded) || existingIdentifier.Contains(padded) || currentBatch.Contains(padded))
                {
                    continue;
                }

                MMSIService newRecord = new MMSIService();
                newRecord.ServiceNumber = padded;
                newRecord.ServiceName = GetString(record, "desc") ?? "UNKNOWN";

                newRecords.Add(newRecord);
                currentBatch.Add(padded);
            }

            await _dbContext.MMSIServices.AddRangeAsync(newRecords, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return $"Services imported successfully, {newRecords.Count} new records";
        }

        public async Task<string> ImportMsapTugboats(string tugboatCSVPath, CancellationToken cancellationToken)
        {
            var existingIdentifier = (await _dbContext.MMSITugboats.ToListAsync(cancellationToken))
                .Select(c => c.TugboatNumber).ToHashSet();
            var existingTugboatOwners = (await _dbContext.MMSITugboatOwners.ToListAsync(cancellationToken))
                .Select(c => new { c.TugboatOwnerId, c.TugboatOwnerNumber }).ToList();

            var currentBatch = new HashSet<string>();
            var newRecords = new List<MMSITugboat>();
            using var reader = new StreamReader(tugboatCSVPath);
            using var csv = new CsvReader(reader, _csvConfig);
            var records = csv.GetRecords<dynamic>().ToList();

            foreach (var record in records)
            {
                string padded = PadNumber(GetString(record, "number"), 3);
                string paddedOwnerNumber = PadNumber(GetString(record, "owner"), 3);
                
                var owner = existingTugboatOwners.FirstOrDefault(t => t.TugboatOwnerNumber == paddedOwnerNumber);

                if (string.IsNullOrEmpty(padded) || existingIdentifier.Contains(padded) || currentBatch.Contains(padded))
                {
                    continue;
                }

                MMSITugboat newRecord = new MMSITugboat();
                if (owner != null) newRecord.TugboatOwnerId = owner.TugboatOwnerId;
                newRecord.TugboatNumber = padded;
                newRecord.TugboatName = GetString(record, "name") ?? "UNKNOWN";
                newRecord.IsCompanyOwned = ParseBool(record, "companyown");

                newRecords.Add(newRecord);
                currentBatch.Add(padded);
            }

            await _dbContext.MMSITugboats.AddRangeAsync(newRecords, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return $"Tugboats imported successfully, {newRecords.Count} new records";
        }

        public async Task<string> ImportMsapTugboatOwners(string tugboatOwnerCSVPath, CancellationToken cancellationToken)
        {
            var existingIdentifier = (await _dbContext.MMSITugboatOwners.ToListAsync(cancellationToken))
                .Select(c => c.TugboatOwnerNumber).ToHashSet();

            var currentBatch = new HashSet<string>();
            var newRecords = new List<MMSITugboatOwner>();
            using var reader = new StreamReader(tugboatOwnerCSVPath);
            using var csv = new CsvReader(reader, _csvConfig);
            var records = csv.GetRecords<dynamic>().ToList();

            foreach (var record in records)
            {
                string padded = PadNumber(GetString(record, "number"), 3);

                if (string.IsNullOrEmpty(padded) || existingIdentifier.Contains(padded) || currentBatch.Contains(padded))
                {
                    continue;
                }

                MMSITugboatOwner newRecord = new MMSITugboatOwner();
                newRecord.TugboatOwnerNumber = padded;
                newRecord.TugboatOwnerName = GetString(record, "name") ?? "UNKNOWN";

                newRecords.Add(newRecord);
                currentBatch.Add(padded);
            }

            await _dbContext.MMSITugboatOwners.AddRangeAsync(newRecords, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return $"Tugboat Owners imported successfully, {newRecords.Count} new records";
        }

        public async Task<string> ImportMsapTugMasters(string tugMasterCSVPath, CancellationToken cancellationToken)
        {
            var existingIdentifier = (await _dbContext.MMSITugMasters.ToListAsync(cancellationToken))
                .Select(c => c.TugMasterNumber).ToHashSet();

            var currentBatch = new HashSet<string>();
            var newRecords = new List<MMSITugMaster>();
            using var reader = new StreamReader(tugMasterCSVPath);
            using var csv = new CsvReader(reader, _csvConfig);
            var records = csv.GetRecords<dynamic>().ToList();

            foreach (var record in records)
            {
                string? empNo = GetString(record, "empno");
                if (string.IsNullOrEmpty(empNo) || existingIdentifier.Contains(empNo) || currentBatch.Contains(empNo))
                {
                    continue;
                }

                MMSITugMaster newRecord = new MMSITugMaster();
                newRecord.TugMasterNumber = empNo;
                newRecord.TugMasterName = GetString(record, "name") ?? "UNKNOWN";
                newRecord.IsActive = ParseBool(record, "active");

                newRecords.Add(newRecord);
                currentBatch.Add(empNo);
            }

            await _dbContext.MMSITugMasters.AddRangeAsync(newRecords, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return $"Tug Masters imported successfully, {newRecords.Count} new records";
        }

        public async Task<string> ImportMsapVessels(string vesselCSVPath, CancellationToken cancellationToken)
        {
            var existingIdentifier = (await _dbContext.MMSIVessels.ToListAsync(cancellationToken))
                .Select(c => c.VesselNumber).ToHashSet();

            var currentBatch = new HashSet<string>();
            var newRecords = new List<MMSIVessel>();
            using var reader = new StreamReader(vesselCSVPath);
            using var csv = new CsvReader(reader, _csvConfig);
            var records = csv.GetRecords<dynamic>().ToList();

            foreach (var record in records)
            {
                string padded = PadNumber(GetString(record, "number"), 4);

                if (string.IsNullOrEmpty(padded) || existingIdentifier.Contains(padded) || currentBatch.Contains(padded))
                {
                    continue;
                }

                MMSIVessel newRecord = new MMSIVessel();
                newRecord.VesselNumber = padded;
                newRecord.VesselName = GetString(record, "name") ?? "UNKNOWN";
                newRecord.VesselType = GetString(record, "type") == "L" ? "LOCAL" : "FOREIGN";

                newRecords.Add(newRecord);
                currentBatch.Add(padded);
            }

            await _dbContext.MMSIVessels.AddRangeAsync(newRecords, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return $"Vessels imported successfully, {newRecords.Count} new records";
        }

        public async Task<string> ImportMsapTariffRates(string tariffCSVPath, string customerCSVPath, CancellationToken cancellationToken)
        {
            using var reader0 = new StreamReader(customerCSVPath);
            using var csv0 = new CsvReader(reader0, _csvConfig);

            var msapCustomerRecords = csv0.GetRecords<dynamic>().Select(c => new
            {
                number = (string?)GetString(c, "number"),
                name = (string?)GetString(c, "name")
            }).ToList();

            using var reader = new StreamReader(tariffCSVPath);
            using var csv = new CsvReader(reader, _csvConfig);
            var records = csv.GetRecords<dynamic>().ToList();

            var existingIdentifier = await _dbContext.MMSITariffRates
                .AsNoTracking()
                .Select(t => new { CustomerId = t.CustomerId, TerminalId = t.TerminalId, ServiceId = t.ServiceId, AsOfDate = t.AsOfDate })
                .ToHashSetAsync(cancellationToken);

            var ibsCustomerList = await _dbContext.Customers
                .Where(c => c.Company == "MMSI")
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            var existingTerminals = await _dbContext.MMSITerminals
                .AsNoTracking()
                .Include(t => t.Port)
                .Select(t => new { TerminalNumber = t.TerminalNumber, TerminalId = t.TerminalId, PortNumber = t.Port!.PortNumber })
                .ToListAsync(cancellationToken);

            var existingServices = await _dbContext.MMSIServices
                .AsNoTracking()
                .Select(s => new { ServiceNumber = s.ServiceNumber, ServiceId = s.ServiceId })
                .ToListAsync(cancellationToken);

            var currentBatch = new HashSet<object>();
            var newRecords = new List<MMSITariffRate>();

            foreach (var record in records)
            {
                DateOnly? asOfDateNullable = ParseDateOnly(record, "date");
                if (asOfDateNullable == null) continue;
                DateOnly asOfDate = asOfDateNullable.Value;

                string? custNo = GetString(record, "custno");
                var msapCustomer = msapCustomerRecords.FirstOrDefault(mc => mc.number == custNo);
                if (msapCustomer == null) continue;

                var customer = ibsCustomerList.FirstOrDefault(c => c.CustomerName.Trim() == msapCustomer.name?.Trim());
                if (customer == null) continue;

                string terminalRaw = GetString(record, "terminal") ?? string.Empty;
                if (terminalRaw.Length < 6) continue;
                var portPart = terminalRaw.Substring(0, 3);
                var terminalPart = terminalRaw.Substring(terminalRaw.Length - 3, 3);
                string paddedPortNum = PadNumber(portPart, 3);
                string paddedTerminalNum = PadNumber(terminalPart, 3);

                var terminal = existingTerminals.FirstOrDefault(t => t.TerminalNumber == paddedTerminalNum && t.PortNumber == paddedPortNum);
                if (terminal == null) continue;

                string paddedServiceNum = PadNumber(GetString(record, "service"), 3);
                var service = existingServices.FirstOrDefault(s => s.ServiceNumber == paddedServiceNum);
                if (service == null) continue;

                var identity = new
                {
                    CustomerId = customer.CustomerId,
                    TerminalId = terminal.TerminalId,
                    ServiceId = service.ServiceId,
                    AsOfDate = asOfDate
                };

                if (existingIdentifier.Contains(identity) || currentBatch.Contains(identity))
                {
                    continue;
                }

                MMSITariffRate newRecord = new MMSITariffRate();
                newRecord.CustomerId = customer.CustomerId;
                newRecord.TerminalId = terminal.TerminalId;
                newRecord.ServiceId = service.ServiceId;
                newRecord.AsOfDate = asOfDate;
                newRecord.Dispatch = ParseDecimal(record, "dispatch");
                newRecord.BAF = ParseDecimal(record, "baf");
                newRecord.CreatedBy = GetString(record, "createdby");
                newRecord.CreatedDate = ParseDateTime(record, "createddat") ?? DateTimeHelper.GetCurrentPhilippineTime();

                newRecords.Add(newRecord);
                currentBatch.Add(identity);
            }

            await _dbContext.MMSITariffRates.AddRangeAsync(newRecords, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return $"Tariff Rates imported successfully, {newRecords.Count} new records";
        }

        #endregion -- Masterfiles --

        public async Task<string> ImportMsapDispatchTickets(string dispatchTicketCSVPath, string customerCSVPath, CancellationToken cancellationToken)
        {
            using var reader0 = new StreamReader(customerCSVPath);
            using var csv0 = new CsvReader(reader0, _csvConfig);

            var msapCustomerRecords = csv0.GetRecords<dynamic>().Select(c => new
            {
                number = (string?)GetString(c, "number"),
                name = (string?)GetString(c, "name")
            }).ToList();

            var existingIdentifier = await _dbContext.MMSIDispatchTickets
                .AsNoTracking()
                .Select(dt => new { DispatchNumber = dt.DispatchNumber, CreatedDate = dt.CreatedDate })
                .ToHashSetAsync(cancellationToken);

            var existingVessels = await _dbContext.MMSIVessels
                .AsNoTracking()
                .Select(v => new { VesselNumber = v.VesselNumber, VesselId = v.VesselId })
                .ToListAsync(cancellationToken);

            var existingTerminals = await _dbContext.MMSITerminals
                .Include(t => t.Port)
                .Select(dt => new { TerminalNumber = dt.TerminalNumber, TerminalId = dt.TerminalId, PortNumber = dt.Port!.PortNumber, PortId = dt.Port.PortId })
                .ToListAsync(cancellationToken);

            var existingTugboats = await _dbContext.MMSITugboats
                .AsNoTracking()
                .Select(dt => new { TugboatNumber = dt.TugboatNumber, TugboatId = dt.TugboatId })
                .ToListAsync(cancellationToken);

            var existingTugMasters = await _dbContext.MMSITugMasters
                .AsNoTracking()
                .Select(dt => new { TugMasterNumber = dt.TugMasterNumber, TugMasterId = dt.TugMasterId })
                .ToListAsync(cancellationToken);

            var existingServices = await _dbContext.MMSIServices
                .AsNoTracking()
                .Select(dt => new { ServiceNumber = dt.ServiceNumber, ServiceId = dt.ServiceId })
                .ToListAsync(cancellationToken);

            var ibsCustomerList = await _dbContext.Customers
                .Where(c => c.Company == "MMSI")
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            var existingBilling = await _dbContext.MMSIBillings
                .AsNoTracking()
                .Select(b => new { MMSIBillingNumber = b.MMSIBillingNumber, MMSIBillingId = b.MMSIBillingId, CustomerId = (int?)b.CustomerId })
                .ToListAsync(cancellationToken);

            var currentBatch = new HashSet<object>();
            var newRecords = new List<MMSIDispatchTicket>();

            using var reader = new StreamReader(dispatchTicketCSVPath);
            using var csv = new CsvReader(reader, _csvConfig);
            var records = csv.GetRecords<dynamic>().ToList();

            foreach (var record in records)
            {
                DateTime? entryDateNullable = ParseDateTime(record, "entrydate");
                if (entryDateNullable == null) continue;
                DateTime entryDate = entryDateNullable.Value;

                string dispatchNumber = GetString(record, "number")?.Trim() ?? "";
                var identity = new
                {
                    DispatchNumber = dispatchNumber,
                    CreatedDate = entryDate
                };

                if (existingIdentifier.Contains(identity) || currentBatch.Contains(identity))
                {
                    continue;
                }

                MMSIDispatchTicket newRecord = new MMSIDispatchTicket();

                string portTerminalOriginal = GetString(record, "terminal") ?? string.Empty;
                string portNumber = string.Empty;
                string terminalNumber = string.Empty;

                if (!string.IsNullOrWhiteSpace(portTerminalOriginal))
                {
                    var cleanStr = new string(portTerminalOriginal.Where(c => !char.IsWhiteSpace(c)).ToArray());
                    if (cleanStr.Length >= 6)
                    {
                        portNumber = cleanStr.Substring(0, 3);
                        terminalNumber = cleanStr.Substring(cleanStr.Length - 3, 3);
                    }
                }

                string paddedVesselNum = PadNumber(GetString(record, "vesselnum"), 4);
                string paddedTugboatNum = PadNumber(GetString(record, "tugnum"), 3);
                string paddedServiceNum = PadNumber(GetString(record, "srvctype"), 3);

                string? custNo = GetString(record, "custno");
                var msapCustomer = msapCustomerRecords.FirstOrDefault(mc => mc.number == custNo);

                if (msapCustomer != null)
                {
                    var customer = ibsCustomerList.FirstOrDefault(c => c.CustomerName.Trim() == msapCustomer.name?.Trim());
                    if (customer != null) newRecord.CustomerId = customer.CustomerId;
                    else if (!string.IsNullOrEmpty(GetString(record, "billnum")))
                    {
                        string? billNum = GetString(record, "billnum");
                        newRecord.CustomerId = existingBilling.FirstOrDefault(b => b.MMSIBillingNumber == billNum)?.CustomerId;
                    }
                }

                string? billNumberStr = GetString(record, "billnum");
                newRecord.BillingId = existingBilling.FirstOrDefault(b => b.MMSIBillingNumber == billNumberStr)?.MMSIBillingId;
                newRecord.BillingNumber = string.IsNullOrEmpty(billNumberStr) ? null : billNumberStr;
                newRecord.DispatchNumber = dispatchNumber;
                newRecord.Date = ParseDateOnly(record, "date");
                newRecord.COSNumber = GetString(record, "cosno") == string.Empty ? null : GetString(record, "cosno");
                newRecord.DateLeft = ParseDateOnly(record, "dateleft");
                newRecord.DateArrived = ParseDateOnly(record, "datearrive");
                
                string? timeLeftStr = GetString(record, "timeleft");
                string? timeArriveStr = GetString(record, "timearrive");
                if (!string.IsNullOrEmpty(timeLeftStr) && !string.IsNullOrEmpty(timeArriveStr) && 
                    int.TryParse(timeLeftStr, out int timeLeftInt) && int.TryParse(timeArriveStr, out int timeArriveInt))
                {
                    newRecord.TimeLeft = TimeOnly.ParseExact(timeLeftInt.ToString("D4"), "HHmm", CultureInfo.InvariantCulture);
                    newRecord.TimeArrived = TimeOnly.ParseExact(timeArriveInt.ToString("D4"), "HHmm", CultureInfo.InvariantCulture);
                }
                
                newRecord.BaseOrStation = GetString(record, "@base") == string.Empty ? null : GetString(record, "@base");
                newRecord.VoyageNumber = GetString(record, "voyage") == string.Empty ? null : GetString(record, "voyage");
                newRecord.DispatchRate = ParseDecimal(record, "dispatchra");
                newRecord.DispatchBillingAmount = ParseDecimal(record, "dispatchbi");
                newRecord.DispatchNetRevenue = ParseDecimal(record, "dispatchne");
                newRecord.BAFRate = ParseDecimal(record, "bafrate");
                newRecord.BAFBillingAmount = ParseDecimal(record, "bafbillamt");
                newRecord.BAFNetRevenue = ParseDecimal(record, "bafnetamt");
                newRecord.TotalBilling = newRecord.DispatchBillingAmount + newRecord.BAFBillingAmount;
                newRecord.TotalNetRevenue = newRecord.DispatchNetRevenue + newRecord.BAFNetRevenue;
                
                newRecord.TugBoatId = existingTugboats.FirstOrDefault(tb => tb.TugboatNumber == paddedTugboatNum)?.TugboatId;
                newRecord.TugMasterId = existingTugMasters.FirstOrDefault(tm => tm.TugMasterNumber == GetString(record, "masterno"))?.TugMasterId;
                newRecord.VesselId = existingVessels.FirstOrDefault(v => v.VesselNumber == paddedVesselNum)?.VesselId;
                newRecord.ServiceId = existingServices.FirstOrDefault(s => s.ServiceNumber == paddedServiceNum)?.ServiceId;
                newRecord.TerminalId = string.IsNullOrEmpty(portTerminalOriginal) ? null : existingTerminals.FirstOrDefault(t => t.PortNumber == portNumber && t.TerminalNumber == terminalNumber)?.TerminalId;
                
                newRecord.CreatedBy = GetString(record, "entryby") ?? "IMPORT";
                newRecord.CreatedDate = entryDate;
                newRecord.ApOtherTugs = ParseDecimal(record, "apothertug");
                newRecord.DispatchChargeType = ParseBool(record, "perhour") ? "Per hour" : "Per move";
                newRecord.BAFChargeType = "Per move";

                if (ParseBool(record, "approved")) newRecord.Status = string.IsNullOrEmpty(billNumberStr) ? "For Billing" : "Billed";
                else newRecord.Status = "For Tariff";

                if (newRecord.DateLeft.HasValue && newRecord.DateArrived.HasValue && newRecord.TimeLeft.HasValue && newRecord.TimeArrived.HasValue)
                {
                    DateTime dtl = newRecord.DateLeft.Value.ToDateTime(newRecord.TimeLeft.Value);
                    DateTime dta = newRecord.DateArrived.Value.ToDateTime(newRecord.TimeArrived.Value);
                    TimeSpan ts = dta - dtl;
                    var totalHours = Math.Round((decimal)ts.TotalHours, 2);

                    if (newRecord.CustomerId == 179)
                    {
                        var wholeHours = Math.Truncate(totalHours);
                        var fractionalPart = totalHours - wholeHours;
                        if (fractionalPart >= 0.75m) totalHours = wholeHours + 1.0m;
                        else if (fractionalPart >= 0.25m) totalHours = wholeHours + 0.5m;
                        else totalHours = wholeHours;
                    }
                    newRecord.TotalHours = totalHours;
                }

                newRecords.Add(newRecord);
                currentBatch.Add(identity);
            }

            await _dbContext.MMSIDispatchTickets.AddRangeAsync(newRecords, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return $"Dispatch Tickets imported successfully, {newRecords.Count} new records";
        }

        public async Task<string> ImportMsapBillings(string billingCSVPath, string customerCSVPath, CancellationToken cancellationToken)
        {
            using var reader0 = new StreamReader(customerCSVPath);
            using var csv0 = new CsvReader(reader0, _csvConfig);

            var msapCustomerRecords = csv0.GetRecords<dynamic>().Select(c => new
            {
                number = (string?)GetString(c, "number"),
                name = (string?)GetString(c, "name")
            }).ToList();

            using var reader = new StreamReader(billingCSVPath);
            using var csv = new CsvReader(reader, _csvConfig);

            var records = csv.GetRecords<dynamic>().ToList();
            var newRecords = new List<MMSIBilling>();

            var existingIdentifier = await _dbContext.MMSIBillings
                .AsNoTracking()
                .Select(b => b.MMSIBillingNumber)
                .ToHashSetAsync(cancellationToken);

            var existingVessels = await _dbContext.MMSIVessels
                .AsNoTracking()
                .Select(v => new { VesselNumber = v.VesselNumber, VesselId = v.VesselId })
                .ToListAsync(cancellationToken);

            var existingPorts = await _dbContext.MMSIPorts
                .AsNoTracking()
                .Select(p => new { PortNumber = p.PortNumber, PortId = p.PortId })
                .ToListAsync(cancellationToken);

            var existingTerminals = await _dbContext.MMSITerminals
                .AsNoTracking()
                .Include(t => t.Port)
                .Select(p => new { TerminalNumber = p.TerminalNumber, TerminalId = p.TerminalId, PortNumber = p.Port!.PortNumber })
                .ToListAsync(cancellationToken);

            var existingPrincipals = await _dbContext.MMSIPrincipals
                .AsNoTracking()
                .Select(p => new { PrincipalId = p.PrincipalId, CustomerId = p.CustomerId, PrincipalNumber = p.PrincipalNumber })
                .ToListAsync(cancellationToken);

            var existingCollection = await _dbContext.MMSICollections
                .AsNoTracking()
                .Select(c => new { MMSICollectionId = c.MMSICollectionId, MMSICollectionNumber = c.MMSICollectionNumber })
                .ToListAsync(cancellationToken);

            var ibsCustomerList = await _dbContext.Customers
                .Where(c => c.Company == "MMSI")
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            var currentBatch = new HashSet<string>();

            foreach (var record in records)
            {
                string? billingNumber = GetString(record, "number");
                if (string.IsNullOrEmpty(billingNumber) || existingIdentifier.Contains(billingNumber) || currentBatch.Contains(billingNumber))
                {
                    continue;
                }

                string paddedVesselNum = PadNumber(GetString(record, "vesselnum"), 4);
                if (paddedVesselNum == string.Empty) continue;

                var vessel = existingVessels.FirstOrDefault(v => v.VesselNumber == paddedVesselNum);
                if (vessel == null) continue;

                MMSIBilling newRecord = new MMSIBilling();
                newRecord.VesselId = vessel.VesselId;

                string? custNo = GetString(record, "custno");
                var msapCustomer = msapCustomerRecords.FirstOrDefault(mc => mc.number == custNo);
                if (msapCustomer != null)
                {
                    var customer = ibsCustomerList.FirstOrDefault(c => c.CustomerName.Trim() == msapCustomer.name?.Trim());
                    if (customer != null) newRecord.CustomerId = customer.CustomerId;
                }

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
                
                string terminalRaw = GetString(record, "terminal") ?? string.Empty;
                if (terminalRaw.Length >= 6)
                {
                    var portPart = terminalRaw.Substring(0, 3);
                    var terminalPart = terminalRaw.Substring(terminalRaw.Length - 3, 3);
                    string paddedPortNum = PadNumber(portPart, 3);
                    string paddedTerminalNum = PadNumber(terminalPart, 3);
                    
                    newRecord.PortId = existingPorts.FirstOrDefault(p => p.PortNumber == paddedPortNum)?.PortId;
                    newRecord.TerminalId = existingTerminals.FirstOrDefault(t => t.TerminalNumber == paddedTerminalNum && t.PortNumber == paddedPortNum)?.TerminalId;
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
                if (!string.IsNullOrEmpty(crNum))
                {
                    var collection = existingCollection.FirstOrDefault(c => c.MMSICollectionNumber == crNum);
                    if (collection != null)
                    {
                        newRecord.CollectionId = collection.MMSICollectionId;
                        newRecord.Status = "Collected";
                    }
                    else newRecord.Status = "For Collection";
                }
                else newRecord.Status = "For Collection";

                newRecord.CollectionNumber = string.IsNullOrEmpty(crNum) ? null : crNum;
                newRecord.IsVatable = ParseBool(record, "vat");

                string paddedPrincipalNum = PadNumber(GetString(record, "billto"), 4);
                if (!string.IsNullOrEmpty(paddedPrincipalNum))
                {
                    var principal = existingPrincipals.FirstOrDefault(p => p.PrincipalNumber == paddedPrincipalNum && p.CustomerId == newRecord.CustomerId);
                    if (principal != null) newRecord.PrincipalId = principal.PrincipalId;
                    newRecord.BilledTo = "PRINCIPAL";
                    newRecord.IsPrincipal = true;
                }
                else
                {
                    newRecord.BilledTo = "LOCAL";
                    newRecord.IsPrincipal = false;
                }

                newRecord.IsPrinted = ParseBool(record, "printed");
                newRecords.Add(newRecord);
                currentBatch.Add(billingNumber);
            }

            await _dbContext.MMSIBillings.AddRangeAsync(newRecords, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return $"Billings imported successfully, {newRecords.Count} new records";
        }

        public async Task<string> ImportMsapCollections(string collectionCSVPath, string customerCSVPath, CancellationToken cancellationToken)
        {
            using var reader0 = new StreamReader(customerCSVPath);
            using var csv0 = new CsvReader(reader0, _csvConfig);

            var msapCustomerRecords = csv0.GetRecords<dynamic>().Select(c => new
            {
                number = (string?)GetString(c, "number"),
                name = (string?)GetString(c, "name")
            }).ToList();

            using var reader = new StreamReader(collectionCSVPath);
            using var csv = new CsvReader(reader, _csvConfig);

            var records = csv.GetRecords<dynamic>().ToList();
            var newRecords = new List<MMSICollection>();

            var existingIdentifier = await _dbContext.MMSICollections
                .AsNoTracking()
                .Select(b => b.MMSICollectionNumber)
                .ToHashSetAsync(cancellationToken);

            var ibsCustomerList = await _dbContext.Customers
                .Where(c => c.Company == "MMSI")
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            var currentBatch = new HashSet<string>();

            foreach (var record in records)
            {
                string? crNum = GetString(record, "crnum");
                if (string.IsNullOrEmpty(crNum) || existingIdentifier.Contains(crNum) || currentBatch.Contains(crNum))
                {
                    continue;
                }

                MMSICollection newRecord = new MMSICollection();
                string? custNo = GetString(record, "custno");
                var msapCustomer = msapCustomerRecords.FirstOrDefault(mc => mc.number == custNo);
                if (msapCustomer == null) continue;

                var customer = ibsCustomerList.FirstOrDefault(c => c.CustomerName.Trim() == msapCustomer.name?.Trim());
                if (customer == null) continue;
                
                newRecord.CustomerId = customer.CustomerId;
                newRecord.MMSICollectionNumber = crNum;
                newRecord.CheckNumber = GetString(record, "checkno");
                newRecord.Status = "Create";
                newRecord.Company = "MMSI";
                newRecord.CashAmount = 0;
                newRecord.WVAT = 0;

                newRecord.Date = ParseDateOnly(record, "crdate") ?? DateOnly.FromDateTime(DateTime.Today);
                newRecord.DepositDate = ParseDateOnly(record, "datedeposi");
                newRecord.Amount = ParseDecimal(record, "amount");
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
                currentBatch.Add(crNum);
            }

            await _dbContext.MMSICollections.AddRangeAsync(newRecords, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return $"Collection import successful, {newRecords.Count} new records";
        }
    }
}
