using System.Drawing;
using System.Globalization;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models.Enums;
using IBS.Services.Attributes;
using Microsoft.AspNetCore.Mvc;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace IBSWeb.Areas.User.Controllers
{
    [Area("User")]
    [RequireAnyAccess("Access denied. You don't have permission to view reports.", ProcedureEnum.ViewMaritimeReport)]
    public class MaritimeReportController(IUnitOfWork unitOfWork) : Controller
    {
        public IActionResult Index() => View();

        [HttpPost]
        public async Task<IActionResult> DispatchForBilling(DateOnly dateFrom, DateOnly dateTo, CancellationToken ct)
        {
            var data = await unitOfWork.Report.GetDispatchReportData(dateFrom, dateTo, ct);
            using var pkg = new ExcelPackage();
            var ws = pkg.Workbook.Worksheets.Add("Dispatch For Billing");

            WriteCompanyHeader(ws);
            ws.Cells["A3"].Value = "Dispatch Ticket For Billing";
            ws.Cells["A3"].Style.Font.Size = 14;
            ws.Cells["A3"].Style.Font.Bold = true;
            ws.Cells["A4"].Value = $"Date: {DateTime.Now:MMMM dd, yyyy}";

            var headers = new[] { "COS #", "DISPATCH #", "DATE", "SERVICE", "VOYAGE #", "CUSTOMER", "VESSEL",
                "PORT", "TERMINAL", "DATE/TIME LEFT", "DATE/TIME ARRIVED", "# OF HOURS", "RATE INDICATOR",
                "DISPATCH RATE", "DISPATCH BILL AMOUNT", "DISPATCH DISCOUNT", "DISPATCH NET AMOUNT",
                "BAF RATE", "BAF BILL AMOUNT", "BAF DISCOUNT", "BAF NET AMOUNT", "TOTAL BILL AMOUNT" };

            var headerRow = 6;
            for (int i = 0; i < headers.Length; i++)
                ws.Cells[headerRow, i + 1].Value = headers[i];
            StyleHeader(ws, headerRow, headers.Length);

            int row = headerRow + 1;
            foreach (var t in data)
            {
                ws.Cells[row, 1].Value = t.COSNumber;
                ws.Cells[row, 2].Value = t.DispatchNumber;
                ws.Cells[row, 3].Value = t.Date.ToString("MM/dd/yyyy");
                ws.Cells[row, 4].Value = t.Service?.ServiceName;
                ws.Cells[row, 5].Value = t.VoyageNumber;
                ws.Cells[row, 6].Value = t.Customer?.CustomerName;
                ws.Cells[row, 7].Value = t.Vessel?.VesselName;
                ws.Cells[row, 8].Value = t.Terminal?.Port?.PortName;
                ws.Cells[row, 9].Value = t.Terminal?.TerminalName;
                ws.Cells[row, 10].Value = FormatDateTime(t.DateLeft, t.TimeLeft);
                ws.Cells[row, 11].Value = FormatDateTime(t.DateArrived, t.TimeArrived);
                ws.Cells[row, 12].Value = Math.Round(t.TotalHours, 2);
                ws.Cells[row, 13].Value = "Per Move";
                ws.Cells[row, 14].Value = t.DispatchRate > 0 ? t.DispatchRate : (decimal?)null;
                ws.Cells[row, 15].Value = t.DispatchBillingAmount > 0 ? t.DispatchBillingAmount : (decimal?)null;
                ws.Cells[row, 16].Value = t.DispatchDiscount > 0 ? t.DispatchDiscount : (decimal?)null;
                ws.Cells[row, 17].Value = t.DispatchNetRevenue > 0 ? t.DispatchNetRevenue : (decimal?)null;
                ws.Cells[row, 18].Value = t.BAFRate > 0 ? t.BAFRate : (decimal?)null;
                ws.Cells[row, 19].Value = t.BAFBillingAmount > 0 ? t.BAFBillingAmount : (decimal?)null;
                ws.Cells[row, 20].Value = t.BAFDiscount > 0 ? t.BAFDiscount : (decimal?)null;
                ws.Cells[row, 21].Value = t.BAFNetRevenue > 0 ? t.BAFNetRevenue : (decimal?)null;
                ws.Cells[row, 22].Value = t.TotalBilling > 0 ? t.TotalBilling : (decimal?)null;
                for (int c = 12; c <= 22; c++)
                    ws.Cells[row, c].Style.Numberformat.Format = "#,##0.00";
                row++;
            }

            ws.Cells[headerRow, 1, row - 1, headers.Length].AutoFitColumns();
            return File(pkg.GetAsByteArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"Dispatch_For_Billing_{DateTime.Now:yyyyMMdd}.xlsx");
        }

        [HttpPost]
        public async Task<IActionResult> DispatchTicketSummary(DateOnly dateFrom, DateOnly dateTo, CancellationToken ct)
        {
            var data = await unitOfWork.Report.GetDispatchReportData(dateFrom, dateTo, ct);
            using var pkg = new ExcelPackage();
            var ws = pkg.Workbook.Worksheets.Add("Dispatch Summary");

            WriteCompanyHeader(ws);
            ws.Cells["A3"].Value = "Dispatch Ticket Summary";
            ws.Cells["A3"].Style.Font.Size = 14;
            ws.Cells["A3"].Style.Font.Bold = true;
            ws.Cells["A4"].Value = $"Period: {dateFrom:MMMM yyyy}";

            var headers = new[] { "COS #", "DISPATCH #", "DATE", "SERVICE", "VOYAGE #", "CUSTOMER", "VESSEL",
                "PORT", "TERMINAL", "DATE/TIME LEFT", "DATE/TIME ARRIVED", "# OF HOURS", "RATE INDICATOR",
                "DISPATCH RATE", "DISPATCH BILL AMOUNT", "DISPATCH DISCOUNT", "DISPATCH NET AMOUNT",
                "BAF RATE", "BAF BILL AMOUNT", "BAF DISCOUNT", "BAF NET AMOUNT", "TOTAL BILL AMOUNT",
                "BILL #", "BILL DATE", "DISPATCH RATE (BILLING)", "DISPATCH AMOUNT (BILLING)",
                "BAF RATE (BILLING)", "BAF AMOUNT (BILLING)", "AP OTHER TUG",
                "CR NUMBER", "CHECK NUMBER", "CHECK DATE", "DATE DEPOSITED", "AMOUNT PER DISPATCH" };

            var headerRow = 6;
            for (int i = 0; i < headers.Length; i++)
                ws.Cells[headerRow, i + 1].Value = headers[i];
            StyleHeader(ws, headerRow, headers.Length);

            int row = headerRow + 1;
            foreach (var t in data)
            {
                ws.Cells[row, 1].Value = t.COSNumber;
                ws.Cells[row, 2].Value = t.DispatchNumber;
                ws.Cells[row, 3].Value = t.Date.ToString("MM/dd/yyyy");
                ws.Cells[row, 4].Value = t.Service?.ServiceName;
                ws.Cells[row, 5].Value = t.VoyageNumber;
                ws.Cells[row, 6].Value = t.Customer?.CustomerName;
                ws.Cells[row, 7].Value = t.Vessel?.VesselName;
                ws.Cells[row, 8].Value = t.Terminal?.Port?.PortName;
                ws.Cells[row, 9].Value = t.Terminal?.TerminalName;
                ws.Cells[row, 10].Value = FormatDateTime(t.DateLeft, t.TimeLeft);
                ws.Cells[row, 11].Value = FormatDateTime(t.DateArrived, t.TimeArrived);
                ws.Cells[row, 12].Value = Math.Round(t.TotalHours, 2);
                ws.Cells[row, 13].Value = "Per Move";
                ws.Cells[row, 14].Value = NullIfZero(t.DispatchRate);
                ws.Cells[row, 15].Value = NullIfZero(t.DispatchBillingAmount);
                ws.Cells[row, 16].Value = NullIfZero(t.DispatchDiscount);
                ws.Cells[row, 17].Value = NullIfZero(t.DispatchNetRevenue);
                ws.Cells[row, 18].Value = NullIfZero(t.BAFRate);
                ws.Cells[row, 19].Value = NullIfZero(t.BAFBillingAmount);
                ws.Cells[row, 20].Value = NullIfZero(t.BAFDiscount);
                ws.Cells[row, 21].Value = NullIfZero(t.BAFNetRevenue);
                ws.Cells[row, 22].Value = NullIfZero(t.TotalBilling);
                ws.Cells[row, 23].Value = t.Billing?.MsapBillingNumber;
                ws.Cells[row, 24].Value = t.Billing?.Date.ToString("MM/dd/yyyy");
                ws.Cells[row, 25].Value = NullIfZero(t.DispatchRate);
                ws.Cells[row, 26].Value = NullIfZero(t.DispatchBillingAmount);
                ws.Cells[row, 27].Value = NullIfZero(t.BAFRate);
                ws.Cells[row, 28].Value = NullIfZero(t.BAFBillingAmount);
                ws.Cells[row, 29].Value = NullIfZero(t.ApOtherTugs);
                ws.Cells[row, 30].Value = t.Billing?.Collection?.MsapCollectionNumber;
                ws.Cells[row, 31].Value = t.Billing?.Collection?.CheckNumber;
                ws.Cells[row, 32].Value = t.Billing?.Collection?.CheckDate?.ToString("MM/dd/yyyy");
                ws.Cells[row, 33].Value = t.Billing?.Collection?.DepositDate?.ToString("MM/dd/yyyy");
                ws.Cells[row, 34].Value = NullIfZero(t.Billing?.Collection?.Amount ?? 0);
                for (int c = 12; c <= 34; c++)
                    ws.Cells[row, c].Style.Numberformat.Format = "#,##0.00";
                row++;
            }

            ws.Cells[headerRow, 1, row - 1, headers.Length].AutoFitColumns();
            return File(pkg.GetAsByteArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"Dispatch_Ticket_Summary_{DateTime.Now:yyyyMMdd}.xlsx");
        }

        [HttpPost]
        public async Task<IActionResult> SalesSummary(int month, int year, CancellationToken ct)
        {
            var dateFrom = new DateOnly(year, month, 1);
            var dateTo = dateFrom.AddMonths(1).AddDays(-1);
            var data = await unitOfWork.Report.GetDispatchReportData(dateFrom, dateTo, ct);

            const int totalCols = 256;
            using var pkg = new ExcelPackage();
            var ws = pkg.Workbook.Worksheets.Add("AR Monitoring");

            ws.Cells["A1"].Value = "MALAYAN MARITIME SERVICES INC.";
            ws.Cells["A1"].Style.Font.Size = 16;
            ws.Cells["A1"].Style.Font.Bold = true;

            ws.Cells["A2"].Value = "AR MONITORING AS OF";
            ws.Cells["E2"].Value = DateTime.Now.ToString("M/d/yyyy");

            // Row 5: section group headers with colored fills
            WriteSectionBands(ws);

            // Row 6: column headers - light blue, size 8, not bold
            var colHeaders = BuildColumnHeaders();
            int headerRow = 6;
            for (int i = 0; i < colHeaders.Length; i++)
                ws.Cells[headerRow, i + 1].Value = colHeaders[i];

            using (var rng = ws.Cells[headerRow, 1, headerRow, totalCols])
            {
                rng.Style.Fill.PatternType = ExcelFillStyle.Solid;
                rng.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(0x99, 0xCC, 0xFF));
                rng.Style.Font.Size = 8;
                rng.Style.Font.Bold = false;
                rng.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                rng.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                rng.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                rng.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                rng.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            }

            // Data rows
            int row = 7;
            foreach (var t in data)
            {
                ws.Cells[row, 1].Value = t.Date.ToString("MM/dd/yyyy");
                ws.Cells[row, 2].Value = t.DispatchNumber?.Trim();
                ws.Cells[row, 3].Value = t.Billing?.MsapBillingNumber;
                ws.Cells[row, 4].Value = t.Customer?.CustomerName;
                ws.Cells[row, 5].Value = t.Vessel?.VesselName;
                ws.Cells[row, 6].Value = t.Vessel?.VesselType?.Trim();
                ws.Cells[row, 7].Value = t.Tugboat?.TugboatName;
                ws.Cells[row, 8].Value = t.Terminal?.Port?.PortName;
                ws.Cells[row, 9].Value = t.Terminal?.TerminalName;
                ws.Cells[row, 10].Value = t.Service?.ServiceName;
                ws.Cells[row, 11].Value = t.DateLeft?.ToString("MM/dd/yyyy") + (t.TimeLeft.HasValue ? " " + t.TimeLeft.Value.ToString("h:mm") : "");
                ws.Cells[row, 12].Value = t.DateArrived?.ToString("MM/dd/yyyy") + (t.TimeArrived.HasValue ? " " + t.TimeArrived.Value.ToString("h:mm") : "");
                ws.Cells[row, 13].Value = Math.Round(t.TotalHours, 2);
                ws.Cells[row, 13].Style.Numberformat.Format = "#,##0.00";
                ws.Cells[row, 14].Value = NullIfZero(t.DispatchRate);
                ws.Cells[row, 14].Style.Numberformat.Format = "#,##0.00";
                ws.Cells[row, 15].Value = NullIfZero(t.TotalBilling);
                ws.Cells[row, 15].Style.Numberformat.Format = "#,##0.00";
                ws.Cells[row, 16].Value = t.Billing?.Collection?.DepositDate?.ToString("MM/dd/yyyy");
                ws.Cells[row, 17].Value = t.Billing?.Collection?.Date.ToString("MM/dd/yyyy");
                ws.Cells[row, 18].Value = t.Billing?.Collection?.MsapCollectionNumber?.Trim();
                ws.Cells[row, 19].Value = t.Billing?.Collection?.CheckBank;
                ws.Cells[row, 20].Value = NullIfZero(t.TotalBilling);
                ws.Cells[row, 20].Style.Numberformat.Format = "#,##0.00";
                ws.Cells[row, 21].Value = NullIfZero(t.Billing?.Collection?.EWT ?? 0);
                ws.Cells[row, 21].Style.Numberformat.Format = "#,##0.00";
                ws.Cells[row, 22].Value = NullIfZero(t.Billing?.Collection?.Amount ?? 0);
                ws.Cells[row, 22].Style.Numberformat.Format = "#,##0.00";
                // Cols 23-26: SBMA, Overpayment, Agency Incentive, Agent Commission (no data)
                ws.Cells[row, 27].Value = NullIfZero(t.Billing?.Balance ?? 0);
                ws.Cells[row, 27].Style.Numberformat.Format = "#,##0.00";
                ws.Cells[row, 28].Value = NullIfZero(t.ApOtherTugs);
                ws.Cells[row, 28].Style.Numberformat.Format = "#,##0.00";
                ws.Cells[row, 29].Value = NullIfZero(t.TotalNetRevenue);
                ws.Cells[row, 29].Style.Numberformat.Format = "#,##0.00";
                // Cols 30-42: Income per tugboat, # hours per tugboat (left empty)
                // Cols 55-163: AP/A/R ledger columns (left empty)
                // Cols 164-253: Assists, tending counts (left empty)
                ws.Cells[row, 255].Value = t.Vessel?.VesselType?.Contains("FOREIGN", StringComparison.OrdinalIgnoreCase) == true ? "UNDOC" : "DOC";
                ws.Cells[row, 256].Value = t.Billing?.Principal?.PrincipalName;
                row++;
            }

            ws.Cells[6, 1, row - 1, totalCols].AutoFitColumns();
            return File(pkg.GetAsByteArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"Sales_Summary_{year}{month:D2}.xlsx");
        }

        private static void WriteSectionBands(ExcelWorksheet ws)
        {
            var gray = Color.FromArgb(0xC0, 0xC0, 0xC0);
            var yellow = Color.FromArgb(0xFF, 0xFF, 0x00);
            var orange = Color.FromArgb(0xFF, 0x99, 0x00);
            var lightOrange = Color.FromArgb(0xFF, 0xCC, 0x66);
            var teal = Color.FromArgb(0x99, 0xCC, 0xFF);
            var pink = Color.FromArgb(0xFF, 0x99, 0xCC);
            var green = Color.FromArgb(0x99, 0xFF, 0x99);

            // Section breaks from the legacy CSV row 5:
            // Col 1: DETAILS (1-28), Col 29: FOR PNL USE (29-54), Col 55: AP LEDGER (55-77),
            // Col 78: A/R LEDGER (78-163), Col 165: Number of ASSISTS (165-214),
            // Col 215: Number of TENDING (215-227), Col 228: Number of TENDING HOURS (228-253)
            // Col 254-256: tail

            var sections = new (int startCol, int endCol, string label, Color color)[]
            {
                (1, 28, "DETAILS OF TRIPS OF TUGBOAT", gray),
                (29, 54, "FOR PNL USE", yellow),
                (55, 77, "AP LEDGER", orange),
                (78, 163, "A/R LEDGER", lightOrange),
                (165, 214, "Number of ASSISTS", teal),
                (215, 227, "Number of TENDING", pink),
                (228, 253, "Number of TENDING HOURS", green),
            };

            // Fill the section band row (row 5)
            foreach (var (startCol, endCol, label, color) in sections)
            {
                ws.Cells[5, startCol].Value = label;
                ws.Cells[5, startCol].Style.Font.Bold = true;
                ws.Cells[5, startCol].Style.Font.Size = 10;
                var rng = ws.Cells[5, startCol, 5, endCol];
                rng.Merge = true;
                rng.Style.Fill.PatternType = ExcelFillStyle.Solid;
                rng.Style.Fill.BackgroundColor.SetColor(color);
                rng.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                rng.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            }
        }

        private static string[] BuildColumnHeaders()
        {
            var tugboats = new[] { "ALLIE BRAVO", "AMATA MARU", "BOHOL SEA", "CEBU STRAIT", "CHOKWANG",
                "HOKUTO", "LAKANDULA", "PALDO", "PANAY GULF", "SUJEONG", "SUN JIN NO.505", "TABANGAO" };

            var headers = new List<string>(256);
            // Cols 1-28: DETAILS section
            headers.AddRange(new[] {
                "BILLING STATEMENT DATE/DISPATCH DATE", "DISPATCH TICKET NUMBER", "BILLING STATEMENT #",
                "CUSTOMER NAME", "NAME OF VESSEL", "TYPE OF VESSEL", "NAME OF TUGBOAT", "PORT", "TERMINAL",
                "NATURE OF SERVICE", "TIME STARTED", "TIME END", "NO. OF HRS", "RATE", "GROSS SALES",
                "DATE DEPOSITED", "RECEIPT DATE", "RECEIPT NUMBER", "BANK",
                "VATABLE AMOUNT", "EWT", "AMOUNT DEPOSITED", "SBMA SHARE", "OVERPAYMENT",
                "AGENCY INCENTIVE", "AGENT COMMISSION", "BALANCE", "AP OTHER TUGS" });

            // Col 29: NET SALES (boundary between DETAILS and FOR PNL USE)
            headers.Add("NET SALES");

            // Cols 30-42: INCOME FROM columns (13, including OTHER TUGS)
            foreach (var t in tugboats)
                headers.Add($"INCOME FROM {t}");
            headers.Add("INCOME FROM OTHER TUGS");

            // Cols 43-54: # OF HOURS (12)
            foreach (var t in tugboats)
                headers.Add($"{t} # OF HOURS");

            // Cols 55-77: AP LEDGER - third-party service providers (empty labels)
            var apProviders = new[] {
                "AGUILAR MARITIME LINK SERVICES INC.", "ASSIST TOW MARINE, INC.", "CEBU MOORING GANG",
                "DABAW PILOTS", "DAVAO GULF", "FORTIS", "GENESIS", "HARBOR STAR SHIPPINIG SERVICES, INC.",
                "LEO MARINE SERVICES", "MELCON MARINE", "METRO CEBU HARBOR PILOTS CO. INC,", "MTSC",
                "NAVFULL MARINE SERVICES, OPC", "NORTH HARBOR TUGS", "OCEANIC", "PACIFIC ROSE", "PCL",
                "SEEN SAM SHIPPING INC.", "SMC SHIPPING AND LIGHTERAGE CORPORATION", "SURIGAO PILOT",
                "TACLOBAN HARBOR PILOTS PARTNERSHIP (THHP) CO.", "TRANSCOASTAL TUGS & SHIPPING SERVICES CO. INC.",
                "VENUS MARINE SERVICES INC." };
            headers.AddRange(apProviders);

            // Cols 78-163: A/R LEDGER - customer/shipping line names
            var arCustomers = new[] {
                "2GO GROUP INC", "ALL ORIENT SHIPPING CORPORATION", "ANIMO MARINE HAUELERS SHIP MANAGEMENT CORP.",
                "ARDMORE MR POOL LLC", "ASTROX  OIL CORPORATION", "BAISHIPPING INC.", "BALYENA TANKER CORPORATION",
                "BEN LINE AGENCIES PHILS INC", "CEBU SEA CHARTERER INC.", "CLIO SHIPPING AND LOGISTICS PHILS. INC",
                "CMA CGM PHILIPPINES INC.", "DABAW PILOTS", "DAVAO GULF MARINE SERVICES INC.", "EBC SHIPPING SERVICES",
                "ECOLOGY MARINE CORPORATION", "EQUINOR ASIA PACIFIC PTE LTD.", "ESGUERRA SHIPPING CORPORATION",
                "FANRONG SHIPPING CORP.", "FASTGUYS LOGISTIC CORPORATION", "FELCOR PETROLEUM DEPOT CORPORATION",
                "FLYING VESSEL", "FOUR DRAGONS SHIPPING SERVICES", "FULLSTEAM SHIPPING CORPORATION",
                "GAC PHILIPPINES INC", "GENSAN SHIPYARD AND MACHINE WORKS INC.", "GIANG PHONG SHIPPING CO., LTD",
                "GOTHONG SOUTHERN SHIPPING LINES INC.", "HAFNIA MIDDLE EAST DMCC, DA-DESK", "HARBOR EAGLE",
                "HERMA SHIPPING AND TRANSPORT CORPORATION", "HONOR MERIT", "INSULAR OIL CORPORATION",
                "ISLAS TANKERS SEATRANSPORT CORP.", "KUDOS TRUCKING CORPORATION", "LORENZO SHIPPING CORPORATION",
                "LYNUX SHIPPING LTD", "MALAYAN TOWAGE & SALVAGE CORP.", "MAR-SHIPS AGENCY, INC.",
                "METRO CEBU HARBOR PILOTS CO. INC.", "METRO CEBU HARBOR PILOTS COMPANY,INC.", "MICHAEL INC.",
                "MIZZEN SHIPPING ENTERPRISES INC.", "MOLAVE TANKER CORPORATION", "MTP MARINE SERVICES",
                "NARRA TANKERS CORPORATION", "ND SHIPPING AGENCY AND ALLIED SERVICES", "NORTEAM",
                "NOVALCO ENTERPRISES SINGAPORE PTE. LTD.", "OCEANIC CONTAINER LINES",
                "OMS SHIPPING SERVICES AND LOGISTICS INC.", "PACIFICROSE SHIPPING SERVICES INC.",
                "PETROMIN SHIPPING & MARINE SERVICES INC.", "PETROTRADE PHILIPPINES INC.",
                "PHIL-CEB MARINE SERVICES INC.", "PHIL. SPAN ASIA CARRIER CORP.", "PHILHUA SHIPPING INC",
                "PHILIPPINE TRANSWORLD SHIPPING CORPORATION", "PHOENIX PETROLEUM PHILS. INC.",
                "PNX - CHELSEA SHIPPING CORP.", "SAFEAIR CORPORATION", "SAN MIGUEL FOODS, INC.",
                "SEABOARD SHIPPING AGENCY AND SERVICES CORP", "SEADOVE MARITIME SERVICES INC",
                "SERVPORT SHIPPING SERVICES INC.", "SHARPORT FERRY SERVICES", "SHOGUN SHIPS CO. INC.",
                "SITE RESOURCES DEVELOPMENT CORPORATION", "SMC SHIPPING AND LIGHTERAGE CORPORATION",
                "SOUTHWEST SHIPS AGENCIES INC.", "SOUTHWEST SHIPS AGENCIES, INC.", "SUBSEA SERVICES INC",
                "SWORD FISH MARINE SERVICES CORP.", "TERBAN MARINE CORPORATION", "TRANS-ASIA SHIPPING LINES, INC.",
                "TRUSTME SHIPPING CORP", "UKC BUILDERS", "UKC BUILDERS, INC.", "UPCMC SHIPPING INC.",
                "VIA MARINE CORPORATION", "WALLEM PHILIPPINES SHIPPING INC.",
                "WILHELMSEN-SMITH BELL SHIPPING, INC.", "WILHELMSEN-SMITH BELL SUBIC, INC.", "WILLY ONG DIZON",
                "WINDBAY SHIPPING AND LOGISTICS INC.", "XINGYUN MARINE TRANSPORT INC.", "YANGTZE PACIFIC SHIPPING LINES"
            };
            headers.AddRange(arCustomers);

            // Col 164: TOTAL
            headers.Add("TOTAL");

            // Cols 165-176: IOC LOCAL
            foreach (var t in tugboats)
                headers.Add($"{t} LOCAL (IOC)");

            // Cols 177-188: IOC FOREIGN
            foreach (var t in tugboats)
                headers.Add($"{t} FOREIGN (IOC)");

            // Cols 189-200: OUTSIDE LOCAL
            foreach (var t in tugboats)
                headers.Add($"{t} LOCAL (OUTSIDE)");

            // Cols 201-212: OUTSIDE FOREIGN
            foreach (var t in tugboats)
                headers.Add($"{t} FOREIGN (OUTSIDE)");

            // Cols 213-214
            headers.Add("OTHER TUGS LOCAL");
            headers.Add("OTHER TUGS FOREIGN");

            // Cols 215-227: Number of TENDING section
            foreach (var t in tugboats)
                headers.Add(t);
            headers.Add("OTHER TUGS");

            // Cols 228-251: TENDING HOURS
            foreach (var t in tugboats)
            {
                headers.Add($"{t} TENDING HOURS - LOCAL");
                headers.Add($"{t} TENDING HOURS - FOREIGN");
            }

            // Cols 252-253
            headers.Add("OTHER TUGS LOCAL");
            headers.Add("OTHER TUGS FOREIGN");

            // Cols 254-256
            headers.Add("");
            headers.Add("DOC/UNDOC");
            headers.Add("PRINCIPAL");

            return [.. headers];
        }

        private static void WriteCompanyHeader(ExcelWorksheet ws)
        {
            ws.Cells["A1"].Value = "MALAYAN MARITIME SERVICES INC.";
            ws.Cells["A1"].Style.Font.Size = 16;
            ws.Cells["A1"].Style.Font.Bold = true;
        }

        private static void StyleHeader(ExcelWorksheet ws, int row, int colCount)
        {
            using var rng = ws.Cells[row, 1, row, colCount];
            rng.Style.Fill.PatternType = ExcelFillStyle.Solid;
            rng.Style.Fill.BackgroundColor.SetColor(Color.LightGray);
            rng.Style.Font.Bold = true;
            rng.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            rng.Style.Border.Left.Style = ExcelBorderStyle.Thin;
            rng.Style.Border.Right.Style = ExcelBorderStyle.Thin;
            rng.Style.Border.Top.Style = ExcelBorderStyle.Thin;
            rng.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
        }

        private static string? FormatDateTime(DateOnly? date, TimeOnly? time)
        {
            return date.HasValue
                ? time.HasValue ? $"{date:MM/dd/yyyy} {time:h:mm tt}" : date.Value.ToString("MM/dd/yyyy")
                : null;
        }

        private static decimal? NullIfZero(decimal val) => val == 0 ? null : val;
    }
}
