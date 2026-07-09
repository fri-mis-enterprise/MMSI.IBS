# AGENTS.md — MMSI-IBS / MSAP

MSAP = **Job Order → Dispatch Ticket → Billing → Collection**. .NET 10 ASP.NET Core MVC, active dev — inconsistencies exist, fix minimal.

## Quick reference

```powershell
dotnet build                 # TreatWarningsAsErrors on 6/8 projects (not test projects)
dotnet test IBS.Tests        # unit tests (xUnit + Moq + FluentAssertions + EF Core InMemory)
dotnet test IBS.Tests.UI     # UI integration (Playwright)
docker compose up            # app :5001, DB :5002
```

DB: `localhost:5432`, `mmsi_ibs_dev`, user `postgres`/`mis123`.

## Stack extras (not obvious from csproj)

- `Npgsql.EnableLegacyTimestampBehavior = true` in Program.cs
- Snake-case naming via `EFCore.NamingConventions`
- Custom `DecimalJsonConverter` rounds to 2 places
- **Quartz** scheduling in `IBS.Services`
- **QuestPDF** for report generation in `IBSWeb`
- Auth: Cookie-based 30-min sliding. `[Authorize(Roles = "Admin")]` on Admin area controllers; `[RequireAnyAccess]` on MSAP controllers
- Migrations in `IBS.DataAccess/Migrations/`, auto-applied on startup
- Local file storage at `App_Data/LocalStorage` under `/local-storage`

## Architecture

```
IBS.Models → IBS.DataAccess → IBS.Services → IBSWeb
                                    ↑              ↓
                               IBS.Utility    IBS.DTOs
```

- **Areas**: `User` (app — 28 controllers), `Admin` (users/roles), `Identity` (login — **Razor Pages**, not controllers)
- DI: Primary constructors, `IUnitOfWork` + generic `Repository<T>` + typed repos
- Service layer: thin orchestration over UoW. Some MSAP entities have service classes; accounting master files use controllers + UoW directly.

## MCP tools (always available)

| Tool | Use |
|------|-----|
| `search_code_context(methodName)` | C# method body + related DTOs/Models |
| `trace_workflow(methodName, filePath)` | Recursive Controller → Service → Repo trace |
| `analyze_action(methodName, filePath)` | Controller action + all deps |
| `read_model(modelName)` | Model/DTO properties summary |
| `execute_sql(sql)` | Run SQL against PostgreSQL (prompts on write) |
| `check_build_status` | `dotnet build` with structured errors |
| `list_csv_files` | List CSVs in Exported/Imports dirs |
| `query_csv(filePath, filter?)` | Filter CSV data |

## Conventions an agent would miss

- **Patterns first** (read before writing): standard Index → `Areas/User/Views/JobOrder/Index.cshtml`; standard Create/Edit → `JobOrder/Create.cshtml`; advanced (conditional dropdowns, uploads) → `DispatchTicket/Index.cshtml`, `Create.cshtml`
- **Root-cause fixes**: grep all callers of the function you're touching, fix once at the shared path
- **No unrequested abstractions**: no interface with one implementation, no factory for one product
- **Audit trail** on every create/edit/delete
- **Workflow state guards**: only "ForTariff" → price, only "Billed" → collect, etc.
- **Time zone**: Philippine time via `DateTimeHelper.GetCurrentPhilippineTime()`
- **Check build only for `.cs` changes** — `cshtml`/`js`/`css` changes just need browser refresh
- **Modern UI**: `modern-ui.css`, `modern-table.js`, `modern-select.js` (Select2 wrapper), `modern-alert.js` (SweetAlert2 wrapper). Icons: Material Symbols Outlined (`<span class="material-symbols-outlined">icon_name</span>`). **Gotcha**: `ModernTable.ajax()` sends POST — if endpoint is `[HttpGet]`, inline `ajax: { url, type: "GET", data: d => d }`.

## Housekeeping

- **Changelog**: append to `CHANGELOG.md` (reverse chronological) for user-visible or architecture-level changes. Format: `## [date]` / `### Added|Changed|Fixed`. Skip trivial edits.
- **Version** at `IBSWeb/Views/Shared/_Layout.cshtml:19` (`ViewBag.AppVersion`). Format: `0.{DeploymentVersion}.{CommitCounts}`. Ask before incrementing.
- **Commit**: never without asking. Present a summary and ask "Commit?".
- **Docs**: update `Docs/` if workflow or architecture changes.
- **Ask when uncertain** — vague prompt or ambiguity → ask instead of guessing.
