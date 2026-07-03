# AGENTS.md — MMSI-IBS / MSAP

## Identity

MSAP = maritime service workflow: **Job Order → Dispatch Ticket → Billing → Collection**.
This is a .NET 10 ASP.NET Core MVC app under active development — inconsistencies exist. Fix minimal but standard.

## Stack

| What | How |
|------|-----|
| Runtime | .NET 10, ASP.NET Core MVC + Razor Pages |
| ORM | EF Core 10 + Npgsql (PostgreSQL 18) |
| DB | `localhost:5432`, `mmsi_ibs_dev`, user `postgres`/`mis123` |
| Build | `TreatWarningsAsErrors=true`, `Nullable=enable` |
| EF conventions | Snake-case naming, `Npgsql.EnableLegacyTimestampBehavior = true` |
| JSON | Custom `DecimalJsonConverter` rounds decimals to 2 places |
| Tests | xUnit + Moq + FluentAssertions + EF Core InMemory (unit); Playwright (UI) |
| Auth | Cookie-based, 30-min sliding, `[Authorize(Roles = "Admin")]` on master files, `RequireAnyAccess` on MSAP |
| Logging | Serilog (console / GCP) |
| UI stack | jQuery, Bootstrap 5, DataTables 2.x, Select2, SweetAlert2, SignalR, Toastr |

## Architecture

```
IBS.Models → IBS.DataAccess → IBS.Services → IBSWeb
                                     ↑              ↓
                                 IBS.Utility    IBS.DTOs
```

- **Areas**: `User` (app), `Admin` (users/roles), `Identity` (login)
- **DI**: Primary constructors, `IUnitOfWork` exposes all repos, typed Service classes
- **Repos**: Generic `Repository<T>` + typed repos. UnitOfWork wraps them.
- **Service layer**: Thin orchestration over `IUnitOfWork`. Some MSAP entities have service classes; Accounting master files use controllers + UoW directly.
- **Models**: `IBS.Models.MasterFile.*` (Terms, BankAccount, Customer, etc.), `IBS.Models.MSAP.MasterFile.*` (Vessel, Port, Principal, etc.)

## Custom tools

| Tool | What it does | Example |
|------|-------------|---------|
| `search_code_context` | Deep-dive into a C# method + related DTOs/Models | `search_code_context(methodName: "CreateJobOrderAsync")` |
| `trace_workflow` | Recursive trace of Controller → Service → Repository | `trace_workflow(methodName: "BillDispatchTickets", filePath: "IBSWeb/.../BillingController.cs")` |
| `analyze_action` | Deep-dive into a controller action + its deps | `analyze_action(methodName: "Index", filePath: "IBSWeb/.../JobOrderController.cs")` |
| `read_model` | Summarise a Model or DTO's properties | `read_model(modelName: "JobOrderViewModel")` |
| `execute_sql` | Run SQL against the PostgreSQL DB. **Prompt on Write**. | `execute_sql(sql: "SELECT * FROM msap_job_orders LIMIT 10")` |
| `check_build_status` | Run `dotnet build`, return structured errors | _(no params)_ |

## Build & run

```powershell
dotnet build          # warnings = errors
dotnet test IBS.Tests # unit tests
```

- Docker: `docker compose up` (app `:5001`, DB `:5002`)
- Migrations auto-apply on startup (`db.Database.MigrateAsync()`)
- Local file storage at `App_Data/LocalStorage`, served under `/local-storage`

## Coding guardrails

- **Architecture first** — `Docs/ARCHITECTURE.md` defines the data flow, state machine, and standard patterns (controller, service, repo, view). Read it before writing new code. The `code-review` agent checks against it.
- **Patterns first**. Read reference implementations before writing new code:
  - Standard Index: `Areas/User/Views/JobOrder/Index.cshtml`
  - Standard Create/Edit: `Areas/User/Views/JobOrder/Create.cshtml`
  - Advanced variant (conditional dropdowns, filter buttons, media uploads): `Areas/User/Views/DispatchTicket/Index.cshtml`, `Create.cshtml`
- **Root-cause fixes**. Grep all callers of the function you're touching. Fix once at the shared path, not in every caller.
- **No unrequested abstractions**. No interface with one implementation, no factory for one product.
- **`TreatWarningsAsErrors`** — any warning = build fail. Fix warnings, don't suppress them.
- **Audit trail** — every create/edit/delete records an `AuditTrail` entry.
- **Workflow state validation** — guard transitions. Only "ForTariff" tickets can be priced, only "Billed" tickets can be collected, etc.
- **Time zone** — Philippine time via `DateTimeHelper.GetCurrentPhilippineTime()`.
- **Fixing inconsistencies** — the codebase has them (input class names, grid layouts, wrapper divs). Fix the specific issue matching the dominant pattern. Don't refactor everything.
- **Ask when uncertain** — vague prompts, unclear intent, or ambiguity about approach → ask clarifying questions. A short diagnostic question saves a wrong implementation cycle.
- **Check build only for C# changes** — `cshtml`, `js`, `css` changes don't need `dotnet build`; browser refresh is enough. Run `check_build_status` only when `.cs` files change.

## Modern UI

- **CSS**: `modern-ui.css` (custom properties)
- **JS**: `modern-table.js`, `modern-select.js` (Select2 wrapper), `modern-alert.js` (SweetAlert2 wrapper)
- **Icons**: Material Symbols Outlined (`<span class="material-symbols-outlined">icon_name</span>`)
- **Gotcha**: `ModernTable.ajax()` sends **POST**. If the endpoint is `[HttpGet]`, inline `ajax: { url, type: "GET", data: d => d }`.

## Tests & migrations

```powershell
dotnet test IBS.Tests      # unit tests
dotnet test IBS.Tests.UI   # UI integration (Playwright)
```

- Migrations in `IBS.DataAccess/Migrations/`, auto-applied at startup.

## Changelog

Every significant change (new feature, refactor, bug fix, test) is auto-logged
to `CHANGELOG.md` in reverse chronological order. Entries follow this format:

```markdown
## [2026-07-02]
### Added
- New feature description (scope: file/module)

### Changed
- Refactor or enhancement description (scope: file/module)

### Fixed
- Bug fix description (scope: file/module)
```

**Trigger rule**: When a task results in a user-visible or architecture-level
change, append an entry before concluding. Skip trivial edits (whitespace,
comment-only, rename-only).

## Committing

Never commit without asking. After completing a task, present a summary of
what changed (files modified, diff highlights) and ask: *"Commit?"*. Only
proceed if the user says yes.

## Project layout

```
IBSWeb/
  Areas/User/Views/{Feature}/   — Index.cshtml, Create.cshtml, Edit.cshtml
  Areas/User/Controllers/        — {Feature}Controller.cs
IBS.Models/
  MasterFile/                     — Terms.cs, BankAccount.cs, etc.
  MSAP/MasterFile/                — Vessel.cs, Port.cs, Principal.cs, etc.
IBS.DataAccess/Repository/        — Repository.cs, UnitOfWork.cs
IBS.Services/                     — typed service classes
IBS.DTOs/                         — Data Transfer Objects
IBS.Utility/                      — helpers, constants
IBS.Tests/                        — xUnit unit tests
IBS.Tests.UI/                     — Playwright integration tests
```
## Docs

Update Docs and manual if there's any significant changes happen on the
workflow or architectural changes

## Versioning

Increase app version, ask me if the feature needs increment by 1.
Format: [0].[DeploymentVersion].[CommitCounts]
Reset the commit counts base on deployment version, if it increment, reset to 0
The version is at IBSWeb/Views/Shared/_Layout.cshtml Line 19: ViewBag.AppVersion
