using System.Drawing;
using System.Globalization;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models.MasterFile;
using IBS.Models.MSAP.MasterFile;
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
            const int totalCols = 22;

            WriteCompanyHeader(ws);
            ws.Cells["A2"].Value = "Dispatch Ticket For Billing";
            ws.Cells["A2"].Style.Font.Size = 14;
            ws.Cells["A2"].Style.Font.Bold = true;
            ws.Cells["A3"].Value = $"Date: {DateTime.Now:MMMM dd, yyyy}";

            var mainLabels = new[] { "COS #", "DISPATCH #", "DATE", "SERVICE", "VOYAGE #", "CUSTOMER", "VESSEL",
                "PORT", "TERMINAL", "DATE/TIME           LEFT", "DATE/TIME           ARRIVED", "# OF HOURS", "RATE INDICATOR" };
            for (int i = 0; i < mainLabels.Length; i++)
                ws.Cells[5, i + 1].Value = mainLabels[i];
            MergeRowRange(ws, 5, 14, 17, "D I S P A T C H");
            MergeRowRange(ws, 5, 18, 21, "B A F");
            ws.Cells[5, 22].Value = "TOTAL BILL AMOUNT";
            StyleHeader(ws, 5, totalCols);

            foreach (var (col, label) in new[] { (14, "RATE"), (15, "BILL AMOUNT"), (16, "DISCOUNT"), (17, "NET AMOUNT"),
                (18, "RATE"), (19, "BILL AMOUNT"), (20, "DISCOUNT"), (21, "NET AMOUNT") })
                ws.Cells[6, col].Value = label;
            StyleHeader(ws, 6, totalCols);
            for (int c = 1; c <= 13; c++)
                ws.Cells[5, c, 6, c].Merge = true;
            ws.Cells[5, 22, 6, 22].Merge = true;

            int dataStart = 7, row = dataStart;
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
                ws.Cells[row, 1, row, totalCols].Style.Font.Size = 11;
                for (int c = 12; c <= 22; c++)
                    ws.Cells[row, c].Style.Numberformat.Format = "#,##0.00";
                row++;
            }

            FinalizeColumns(ws, dataStart, row - 1, totalCols);
            return File(pkg.GetAsByteArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"Dispatch_For_Billing_{DateTime.Now:yyyyMMdd}.xlsx");
        }

        [HttpPost]
        public async Task<IActionResult> DispatchTicketSummary(DateOnly dateFrom, DateOnly dateTo, CancellationToken ct)
        {
            var data = await unitOfWork.Report.GetDispatchReportData(dateFrom, dateTo, ct);
            using var pkg = new ExcelPackage();
            var ws = pkg.Workbook.Worksheets.Add("Dispatch Summary");
            const int totalCols = 35;

            WriteCompanyHeader(ws);
            ws.Cells["A2"].Value = "Dispatch Ticket Summary";
            ws.Cells["A2"].Style.Font.Size = 14;
            ws.Cells["A2"].Style.Font.Bold = true;
            ws.Cells["A3"].Value = $"Period: {dateFrom:MMMM yyyy}";

            var mainLabels = new[] { "COS #", "DISPATCH #", "DATE", "SERVICE", "VOYAGE #", "CUSTOMER", "VESSEL",
                "PORT", "TERMINAL", "DATE/TIME           LEFT", "DATE/TIME           ARRIVED", "# OF HOURS", "RATE INDICATOR" };
            for (int i = 0; i < mainLabels.Length; i++)
                ws.Cells[5, i + 1].Value = mainLabels[i];
            MergeRowRange(ws, 5, 14, 17, "D I S P A T C H");
            MergeRowRange(ws, 5, 18, 21, "B A F");
            ws.Cells[5, 22].Value = "TOTAL BILL AMOUNT";
            MergeRowRange(ws, 5, 23, 28, "B I L L I N G");
            MergeRowRange(ws, 5, 29, 35, "C O L L E C T I O N");
            for (int c = 1; c <= 13; c++)
                ws.Cells[5, c, 7, c].Merge = true;
            ws.Cells[5, 22, 7, 22].Merge = true;

            var peach = Color.FromArgb(0xFF, 0xCC, 0x99);
            var cyan = Color.FromArgb(0xCC, 0xFF, 0xFF);
            var lavender = Color.FromArgb(0xCC, 0xCC, 0xFF);
            var paleGreen = Color.FromArgb(0xCC, 0xFF, 0xCC);
            var paleYellow = Color.FromArgb(0xFF, 0xFF, 0x99);
            foreach (int r in new[] { 5, 6, 7 })
            {
                StyleSection(ws, r, 1, 13, peach);
                StyleSection(ws, r, 14, 17, cyan);
                StyleSection(ws, r, 18, 21, lavender);
                StyleSection(ws, r, 22, 22, peach);
                StyleSection(ws, r, 23, 28, paleGreen);
                StyleSection(ws, r, 29, 35, paleYellow);
            }

            foreach (var (col, label) in new[] {
                (14, "RATE"), (15, "BILL AMOUNT"), (16, "DISCOUNT"), (17, "NET AMOUNT"),
                (18, "RATE"), (19, "BILL AMOUNT"), (20, "DISCOUNT"), (21, "NET AMOUNT"),
                (23, "BILL #"), (24, "DATE"),
                (29, "AP OTHER TUG"), (30, "CR NUMBER"), (31, "CHECK NUMBER"),
                (32, "CHECK DATE"), (33, "DATE DEPOSITED"), (34, "AMOUNT PER DISPATCH"), (35, "2307 PER DISPATCH") })
                ws.Cells[6, col].Value = label;
            MergeRowRange(ws, 6, 25, 26, "DISPATCH");
            MergeRowRange(ws, 6, 27, 28, "BAF");
            ws.Cells[7, 25].Value = "RATE";
            ws.Cells[7, 26].Value = "AMOUNT";
            ws.Cells[7, 27].Value = "RATE";
            ws.Cells[7, 28].Value = "AMOUNT";
            for (int c = 14; c <= 21; c++)
                ws.Cells[6, c, 7, c].Merge = true;
            ws.Cells[6, 23, 7, 23].Merge = true;
            ws.Cells[6, 24, 7, 24].Merge = true;
            ws.Cells[6, 29, 7, 29].Merge = true;
            for (int c = 30; c <= 35; c++)
                ws.Cells[6, c, 7, c].Merge = true;
            ws.View.FreezePanes(8, 1);

            int dataStart = 8, row = dataStart;
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
                ws.Cells[row, 22].Formula = $"Q{row}+U{row}";
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
                ws.Cells[row, 35].Value = NullIfZero(t.Billing?.Collection?.EWT ?? 0);
                ws.Cells[row, 1, row, totalCols].Style.Font.Size = 11;
                ws.Cells[row, 12].Style.Numberformat.Format = "#,##0.00";
                for (int c = 14; c <= 35; c++)
                    if (c < 30 || c > 33)
                        ws.Cells[row, c].Style.Numberformat.Format = "_(#,##0.00_);[Red](#,##0.00)";
                row++;
            }

            int lastDataRow = row - 1, totalRow = row;
            ws.Cells[totalRow, 1].Value = "TOTAL";
            ws.Cells[totalRow, 1, totalRow, totalCols].Style.Font.Size = 11;
            ws.Cells[totalRow, 1, totalRow, totalCols].Style.Font.Bold = true;
            foreach (int c in new[] { 15, 17, 19, 21, 22, 26, 28, 29, 34, 35 })
                ws.Cells[totalRow, c].FormulaR1C1 = $"SUM(R{dataStart}C:R{lastDataRow}C)";
            row++;

            FinalizeColumns(ws, dataStart, lastDataRow, totalCols);
            ws.Column(6).Width = 50.7;
            ws.Column(25).Width = 20.7;
            return File(pkg.GetAsByteArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"Dispatch_Ticket_Summary_{DateTime.Now:yyyyMMdd}.xlsx");
        }

        [HttpPost]
        public async Task<IActionResult> SalesSummary(int month, int year, CancellationToken ct)
        {
            var dateFrom = new DateOnly(year, month, 1);
            var dateTo = dateFrom.AddMonths(1).AddDays(-1);
            var data = await unitOfWork.Report.GetDispatchReportData(dateFrom, dateTo, ct);

            var tugboatsInData = data.Select(t => t.Tugboat?.TugboatName).Where(n => n != null).Distinct(StringComparer.OrdinalIgnoreCase).ToHashSet();
            var ownersInData = data.Select(t => t.Tugboat?.TugboatOwner?.TugboatOwnerName).Where(n => n != null).Distinct(StringComparer.OrdinalIgnoreCase).ToHashSet();
            var customersInData = data.Select(t => t.Customer?.CustomerName).Where(n => n != null).Distinct(StringComparer.OrdinalIgnoreCase).ToHashSet();
            var allTugboats = (await unitOfWork.Tugboat.GetAllAsync(cancellationToken: ct))
                .Where(t => tugboatsInData.Contains(t.TugboatName)).OrderBy(t => t.TugboatName).ToList();
            var tugboatOwners = (await unitOfWork.TugboatOwner.GetAllAsync(cancellationToken: ct))
                .Where(o => ownersInData.Contains(o.TugboatOwnerName)).OrderBy(o => o.TugboatOwnerName).ToList();
            var allCustomers = (await unitOfWork.Customer.GetAllAsync(cancellationToken: ct))
                .Where(c => c.IsActive && customersInData.Contains(c.CustomerName)).OrderBy(c => c.CustomerName).ToList();

            using var pkg = new ExcelPackage();
            var ws = pkg.Workbook.Worksheets.Add("AR Monitoring");

            WriteCompanyHeader(ws);
            ws.Cells["A2"].Value = "AR MONITORING AS OF";
            ws.Cells["E2"].Value = DateTime.Now.ToString("M/d/yyyy");

            var colInfo = BuildSalesSummaryColumns(allTugboats, tugboatOwners, allCustomers);
            var totalCols = colInfo.Count;

            var sectionColors = new Dictionary<int, Color>
            {
                [0] = Color.FromArgb(0xC0, 0xC0, 0xC0),
                [1] = Color.FromArgb(0xFF, 0xFF, 0x00),
                [2] = Color.FromArgb(0xFF, 0x99, 0x00),
                [3] = Color.FromArgb(0xFF, 0xCC, 0x66),
                [4] = Color.FromArgb(0x99, 0xCC, 0xFF),
                [5] = Color.FromArgb(0xFF, 0x99, 0xCC),
                [6] = Color.FromArgb(0x99, 0xFF, 0x99),
            };
            var sectionLabels = new Dictionary<int, string>
            {
                [0] = "DETAILS OF TRIPS OF TUGBOAT",
                [1] = "FOR PNL USE",
                [2] = "AP LEDGER",
                [3] = "A/R LEDGER",
                [4] = "Number of ASSISTS",
                [5] = "Number of TENDING",
                [6] = "Number of TENDING HOURS",
            };

            void PaintSectionHeader(int startCol0, int endCol0, int section)
            {
                var rng = ws.Cells[5, startCol0 + 1, 5, endCol0 + 1];
                rng.Merge = true;
                rng.Style.Fill.PatternType = ExcelFillStyle.Solid;
                rng.Style.Fill.BackgroundColor.SetColor(sectionColors[section]);
                rng.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                rng.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                rng.Style.WrapText = true;
                ws.Cells[5, startCol0 + 1].Value = sectionLabels[section];
                ws.Cells[5, startCol0 + 1].Style.Font.Bold = true;
                ws.Cells[5, startCol0 + 1].Style.Font.Size = 8;
            }

            var currentSection = -1;
            int? sectionStart = null;
            for (int c = 0; c < totalCols; c++)
            {
                var sec = colInfo[c].section;
                if (sec != currentSection)
                {
                    if (currentSection >= 0 && sectionStart.HasValue)
                        PaintSectionHeader(sectionStart.Value, c - 1, currentSection);
                    currentSection = sec;
                    sectionStart = c;
                }
            }
            if (currentSection >= 0 && sectionStart.HasValue)
                PaintSectionHeader(sectionStart.Value, totalCols - 1, currentSection);

            for (int i = 0; i < totalCols; i++)
                ws.Cells[6, i + 1].Value = colInfo[i].label;
            using (var rng = ws.Cells[6, 1, 6, totalCols])
            {
                rng.Style.Fill.PatternType = ExcelFillStyle.Solid;
                rng.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(0x99, 0xCC, 0xFF));
                rng.Style.Font.Size = 8;
                rng.Style.Font.Bold = false;
                rng.Style.WrapText = true;
                rng.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                rng.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                rng.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                rng.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                rng.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                rng.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            }

            // One lookup per tugboat instead of nine parallel dictionaries.
            var tugboatCols = new Dictionary<string, TugboatCols>(StringComparer.OrdinalIgnoreCase);
            TugboatCols ColsFor(string name) =>
                tugboatCols.TryGetValue(name, out var c) ? c : tugboatCols[name] = new TugboatCols();

            var ownerNameToCol = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var customerNameToCol = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            int? netSalesCol = null, docUndocCol = null, principalCol = null;

            for (int i = 0; i < totalCols; i++)
            {
                var sec = colInfo[i].section;
                var label = colInfo[i].label;

                if (sec == 1 && label.StartsWith("INCOME FROM ", StringComparison.OrdinalIgnoreCase))
                {
                    var name = label["INCOME FROM ".Length..];
                    if (name != "OTHER TUGS") ColsFor(name).Income = i;
                }
                else if (sec == 1 && TryStripSuffix(label, " # OF HOURS", out var name1))
                    ColsFor(name1).Hours = i;
                else if (sec == 2)
                    ownerNameToCol[label] = i;
                else if (sec == 3 && label != "TOTAL")
                    customerNameToCol[label] = i;
                else if (sec == 4 && TryStripSuffix(label, " LOCAL (IOC)", out var n2))
                    ColsFor(n2).AssistsLocalIoc = i;
                else if (sec == 4 && TryStripSuffix(label, " FOREIGN (IOC)", out var n3))
                    ColsFor(n3).AssistsForeignIoc = i;
                else if (sec == 4 && TryStripSuffix(label, " LOCAL (OUTSIDE)", out var n4))
                    ColsFor(n4).AssistsLocalOutside = i;
                else if (sec == 4 && TryStripSuffix(label, " FOREIGN (OUTSIDE)", out var n5))
                    ColsFor(n5).AssistsForeignOutside = i;
                else if (sec == 5 && label != "OTHER TUGS")
                    ColsFor(label).Tending = i;
                else if (sec == 6 && TryStripSuffix(label, " TENDING HOURS - LOCAL", out var n6))
                    ColsFor(n6).TendingHoursLocal = i;
                else if (sec == 6 && TryStripSuffix(label, " TENDING HOURS - FOREIGN", out var n7))
                    ColsFor(n7).TendingHoursForeign = i;

                if (label == "NET SALES") netSalesCol = i;
                if (label == "DOC/UNDOC") docUndocCol = i;
                if (label == "PRINCIPAL") principalCol = i;
            }

            int row = 7;
            foreach (var t in data)
            {
                ws.Cells[row, 1, row, totalCols].Style.Font.Size = 11;
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
                ws.Cells[row, 11].Value = FormatLegacyDateTime(t.DateLeft, t.TimeLeft);
                ws.Cells[row, 12].Value = FormatLegacyDateTime(t.DateArrived, t.TimeArrived);
                SetDecimal(ws, row, 13, t.TotalHours);
                SetDecimal(ws, row, 14, t.DispatchRate);
                SetDecimal(ws, row, 15, t.TotalBilling);
                ws.Cells[row, 16].Value = t.Billing?.Collection?.DepositDate?.ToString("MM/dd/yyyy");
                ws.Cells[row, 17].Value = t.Billing?.Collection?.Date.ToString("MM/dd/yyyy");
                ws.Cells[row, 18].Value = t.Billing?.Collection?.MsapCollectionNumber?.Trim();
                ws.Cells[row, 19].Value = t.Billing?.Collection?.CheckBank;
                SetDecimal(ws, row, 20, t.TotalBilling);
                SetDecimal(ws, row, 21, t.Billing?.Collection?.EWT ?? 0);
                SetDecimal(ws, row, 22, t.Billing?.Collection?.Amount ?? 0);
                SetDecimal(ws, row, 27, t.Billing?.Balance ?? 0);
                SetDecimal(ws, row, 28, t.ApOtherTugs);
                if (netSalesCol.HasValue) SetDecimal(ws, row, netSalesCol.Value + 1, t.TotalNetRevenue);

                var tugName = t.Tugboat?.TugboatName;
                var isForeign = t.Vessel?.VesselType?.Contains("FOREIGN", StringComparison.OrdinalIgnoreCase) == true;
                var isTending = t.Service?.ServiceName?.Contains("TENDING", StringComparison.OrdinalIgnoreCase) == true;

                if (tugName != null && tugboatCols.TryGetValue(tugName, out var tc))
                {
                    if (tc.Income.HasValue) SetDecimal(ws, row, tc.Income.Value + 1, t.TotalNetRevenue);
                    if (tc.Hours.HasValue) SetDecimal(ws, row, tc.Hours.Value + 1, t.TotalHours);

                    var isCompanyOwned = t.Tugboat?.IsCompanyOwned == true;
                    int? assistCol = (isCompanyOwned, isForeign) switch
                    {
                        (true, true) => tc.AssistsForeignIoc,
                        (true, false) => tc.AssistsLocalIoc,
                        (false, true) => tc.AssistsForeignOutside,
                        (false, false) => tc.AssistsLocalOutside,
                    };
                    if (assistCol.HasValue) ws.Cells[row, assistCol.Value + 1].Value = 1;

                    if (isTending)
                    {
                        if (tc.Tending.HasValue) ws.Cells[row, tc.Tending.Value + 1].Value = 1;
                        var hoursCol = isForeign ? tc.TendingHoursForeign : tc.TendingHoursLocal;
                        if (hoursCol.HasValue) SetDecimal(ws, row, hoursCol.Value + 1, t.TotalHours);
                    }
                }

                var ownerName = t.Tugboat?.TugboatOwner?.TugboatOwnerName;
                if (ownerName != null && ownerNameToCol.TryGetValue(ownerName, out var apCol))
                    SetDecimal(ws, row, apCol + 1, t.ApOtherTugs > 0 ? t.ApOtherTugs : t.TotalBilling);

                var custName = t.Customer?.CustomerName;
                if (custName != null && customerNameToCol.TryGetValue(custName, out var arCol))
                    SetDecimal(ws, row, arCol + 1, t.Billing?.Balance ?? t.TotalBilling);

                if (docUndocCol.HasValue)
                    ws.Cells[row, docUndocCol.Value + 1].Value = isForeign ? "UNDOC" : "DOC";
                if (principalCol.HasValue)
                    ws.Cells[row, principalCol.Value + 1].Value = t.Billing?.Principal?.PrincipalName;

                row++;
            }

            FinalizeColumns(ws, 7, row - 1, totalCols);
            return File(pkg.GetAsByteArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"Sales_Summary_{year}{month:D2}.xlsx");
        }

        private static bool TryStripSuffix(string label, string suffix, out string name)
        {
            if (label.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
            {
                name = label[..^suffix.Length];
                return true;
            }
            name = string.Empty;
            return false;
        }

        private sealed class TugboatCols
        {
            public int? Income, Hours;
            public int? AssistsLocalIoc, AssistsForeignIoc, AssistsLocalOutside, AssistsForeignOutside;
            public int? Tending, TendingHoursLocal, TendingHoursForeign;
        }

        private static List<(string label, int section)> BuildSalesSummaryColumns(
            List<Tugboat> tugboats, List<TugboatOwner> owners, List<Customer> customers)
        {
            var cols = new List<(string, int)>();

            var detailHeaders = new[] {
                "BILLING STATEMENT DATE/DISPATCH DATE", "DISPATCH TICKET NUMBER", "BILLING STATEMENT #",
                "CUSTOMER NAME", "NAME OF VESSEL", "TYPE OF VESSEL", "NAME OF TUGBOAT", "PORT", "TERMINAL",
                "NATURE OF SERVICE", "TIME STARTED", "TIME END", "NO. OF HRS", "RATE", "GROSS SALES",
                "DATE DEPOSITED", "RECEIPT DATE", "RECEIPT NUMBER", "BANK",
                "VATABLE AMOUNT", "EWT", "AMOUNT DEPOSITED", "SBMA SHARE", "OVERPAYMENT",
                "AGENCY INCENTIVE", "AGENT COMMISSION", "BALANCE", "AP OTHER TUGS" };
            foreach (var h in detailHeaders) cols.Add((h, 0));
            cols.Add(("NET SALES", 0));

            foreach (var t in tugboats)
                cols.Add(($"INCOME FROM {t.TugboatName}", 1));
            cols.Add(("INCOME FROM OTHER TUGS", 1));
            foreach (var t in tugboats)
                cols.Add(($"{t.TugboatName} # OF HOURS", 1));

            foreach (var o in owners)
                cols.Add((o.TugboatOwnerName, 2));

            foreach (var c in customers)
                cols.Add((c.CustomerName, 3));
            cols.Add(("TOTAL", 3));

            foreach (var t in tugboats)
                cols.Add(($"{t.TugboatName} LOCAL (IOC)", 4));
            foreach (var t in tugboats)
                cols.Add(($"{t.TugboatName} FOREIGN (IOC)", 4));
            foreach (var t in tugboats)
                cols.Add(($"{t.TugboatName} LOCAL (OUTSIDE)", 4));
            foreach (var t in tugboats)
                cols.Add(($"{t.TugboatName} FOREIGN (OUTSIDE)", 4));
            cols.Add(("OTHER TUGS LOCAL", 4));
            cols.Add(("OTHER TUGS FOREIGN", 4));

            foreach (var t in tugboats)
                cols.Add((t.TugboatName, 5));
            cols.Add(("OTHER TUGS", 5));

            foreach (var t in tugboats)
            {
                cols.Add(($"{t.TugboatName} TENDING HOURS - LOCAL", 6));
                cols.Add(($"{t.TugboatName} TENDING HOURS - FOREIGN", 6));
            }
            cols.Add(("OTHER TUGS LOCAL", 6));
            cols.Add(("OTHER TUGS FOREIGN", 6));

            cols.Add(("", 6));
            cols.Add(("DOC/UNDOC", 6));
            cols.Add(("PRINCIPAL", 6));

            return cols;
        }

        private static void SetDecimal(ExcelWorksheet ws, int row, int col, decimal val)
        {
            if (val == 0) return;
            ws.Cells[row, col].Value = val;
            ws.Cells[row, col].Style.Numberformat.Format = "#,##0.00";
        }

        private static string? FormatLegacyDateTime(DateOnly? date, TimeOnly? time)
        {
            if (!date.HasValue) return null;
            var d = date.Value.ToString("MM/dd/yyyy");
            return time.HasValue ? $"{d} {time.Value:HH:mm}" : d;
        }

        private static void WriteCompanyHeader(ExcelWorksheet ws)
        {
            ws.Cells["A1"].Value = "MALAYAN MARITIME SERVICES INC.";
            ws.Cells["A1"].Style.Font.Size = 16;
            ws.Cells["A1"].Style.Font.Bold = true;
        }

        // Replaces the "hide empty cols + autofit + min width" block that was copy-pasted in every method.
        private static void FinalizeColumns(ExcelWorksheet ws, int dataStart, int lastRow, int totalCols)
        {
            for (int c = 1; c <= totalCols; c++)
            {
                bool allEmpty = true;
                for (int r = dataStart; r <= lastRow; r++)
                    if (ws.Cells[r, c].Value != null) { allEmpty = false; break; }
                ws.Column(c).Hidden = allEmpty;
            }
            ws.Cells[dataStart, 1, lastRow, totalCols].AutoFitColumns();
            for (int c = 1; c <= totalCols; c++)
                if (!ws.Column(c).Hidden && ws.Column(c).Width < 14) ws.Column(c).Width = 14;
        }

        private static void MergeRowRange(ExcelWorksheet ws, int row, int startCol, int endCol, string label)
        {
            var rng = ws.Cells[row, startCol, row, endCol];
            rng.Merge = true;
            rng.Value = label;
            rng.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            rng.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
        }

        private static void StyleSection(ExcelWorksheet ws, int row, int startCol, int endCol, Color color)
        {
            using var rng = ws.Cells[row, startCol, row, endCol];
            rng.Style.Fill.PatternType = ExcelFillStyle.Solid;
            rng.Style.Fill.BackgroundColor.SetColor(color);
            rng.Style.Font.Size = 8;
            rng.Style.Font.Bold = true;
            rng.Style.WrapText = true;
            rng.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            rng.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            rng.Style.Border.Left.Style = ExcelBorderStyle.Thin;
            rng.Style.Border.Right.Style = ExcelBorderStyle.Thin;
            rng.Style.Border.Top.Style = ExcelBorderStyle.Thin;
            rng.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
        }

        private static void StyleHeader(ExcelWorksheet ws, int row, int colCount)
        {
            using var rng = ws.Cells[row, 1, row, colCount];
            rng.Style.Fill.PatternType = ExcelFillStyle.Solid;
            rng.Style.Fill.BackgroundColor.SetColor(Color.LightGray);
            rng.Style.Font.Size = 8;
            rng.Style.Font.Bold = true;
            rng.Style.WrapText = true;
            rng.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            rng.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            rng.Style.Border.Left.Style = ExcelBorderStyle.Thin;
            rng.Style.Border.Right.Style = ExcelBorderStyle.Thin;
            rng.Style.Border.Top.Style = ExcelBorderStyle.Thin;
            rng.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
        }

        private static string? FormatDateTime(DateOnly? date, TimeOnly? time)
        {
            return date.HasValue
                ? time.HasValue ? $"{date:MM/dd/yyyy} {time:HH:mm}" : date.Value.ToString("MM/dd/yyyy")
                : null;
        }

        private static decimal? NullIfZero(decimal val) => val == 0 ? null : val;
    }
}
