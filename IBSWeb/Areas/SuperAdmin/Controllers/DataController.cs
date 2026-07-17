using System.Text.Json;
using IBS.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IBSWeb.Areas.SuperAdmin.Controllers
{
    [Area("SuperAdmin")]
    [Authorize(Roles = "SuperAdmin")]
    public class DataController(
        SuperAdminService superAdminService,
        ILogger<DataController> logger)
        : Controller
    {
        private static readonly JsonSerializerOptions JsonCamelCase = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public IActionResult Index(string table)
        {
            if (!superAdminService.SupportedTables.Contains(table))
                return NotFound();

            ViewBag.TableName = table;
            ViewBag.DisplayName = superAdminService.DisplayName(table);
            ViewBag.ColumnsJson = JsonSerializer.Serialize(superAdminService.GetColumns(table), JsonCamelCase);
            ViewBag.FieldsJson = JsonSerializer.Serialize(superAdminService.GetEditableFields(table), JsonCamelCase);
            ViewBag.IdColumn = superAdminService.IdColumn(table);
            ViewBag.ReferenceColumn = superAdminService.ReferenceColumn(table);

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> GetData(string table, [FromForm] DataTablesParameters parameters, CancellationToken cancellationToken)
        {
            try
            {
                // Extract sort column and direction from DataTables request
                var sortColumn = parameters.Order?.Count > 0 && parameters.Columns?.Count > 0
                    ? parameters.Columns[parameters.Order[0].Column].Data
                    : null;
                var sortDir = parameters.Order?.Count > 0
                    ? parameters.Order[0].Dir
                    : "asc";

                var (data, total) = await superAdminService.GetDataAsync(
                    table, parameters.Start, parameters.Length,
                    parameters.Search?.Value,
                    sortColumn, sortDir,
                    cancellationToken);

                return Json(new
                {
                    draw = parameters.Draw,
                    recordsTotal = total,
                    recordsFiltered = total,
                    data
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "GetData failed for {Table}", table);
                return Json(new { draw = parameters.Draw, recordsTotal = 0, recordsFiltered = 0, data = Array.Empty<object>() });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetRecord(string table, int id, CancellationToken cancellationToken)
        {
            var record = await superAdminService.GetRecordAsync(table, id, cancellationToken);
            if (record == null)
                return Json(new { success = false, message = "Record not found." });

            return Json(new { success = true, data = record });
        }

        [HttpPost]
        public async Task<IActionResult> Save(string table, int id,
            [FromForm] Dictionary<string, string> changes,
            [FromForm] string remarks,
            CancellationToken cancellationToken)
        {
            var username = User.Identity?.Name ?? "SuperAdmin";
            var result = await superAdminService.SaveAsync(table, id, changes, remarks, username, cancellationToken);

            return Json(new { success = result.IsSuccess, message = result.Message });
        }

        [HttpGet]
        public async Task<IActionResult> GetLookup(string key, CancellationToken cancellationToken)
        {
            var items = await superAdminService.GetLookupAsync(key, cancellationToken);
            return Json(items);
        }
    }

    // DataTables request model
    public class DataTablesParameters
    {
        public int Draw { get; set; }
        public int Start { get; set; }
        public int Length { get; set; }
        public DataTablesSearch? Search { get; set; }
        public List<DataTablesOrder>? Order { get; set; }
        public List<DataTablesColumn>? Columns { get; set; }
    }

    public class DataTablesSearch
    {
        public string? Value { get; set; }
        public bool Regex { get; set; }
    }

    public class DataTablesOrder
    {
        public int Column { get; set; }
        public string Dir { get; set; } = "asc";
    }

    public class DataTablesColumn
    {
        public string Data { get; set; } = "";
        public string Name { get; set; } = "";
        public bool Orderable { get; set; }
        public bool Searchable { get; set; }
        public DataTablesSearch? Search { get; set; }
    }
}
