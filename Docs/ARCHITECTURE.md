# MMSI-IBS / MSAP Architecture

> **MSAP** = Maritime Service Workflow: **Job Order → Dispatch Ticket → Billing → Collection**

---

## 1. Data Flow (View → Controller → Service → Repository → Database)

```
.cshtml View
    ↓  (form POST / AJAX)
Controller  ←  DI: Services, IUnitOfWork, SignalR hubs, etc.
    ↓
Service  ←  DI: IUnitOfWork, ILogger, etc.
    ↓
Repository (typed, extends Repository<T>)  ←  DI: ApplicationDbContext
    ↓
IUnitOfWork.SaveAsync()  →  ApplicationDbContext.SaveChangesAsync()
    ↓
PostgreSQL (via Npgsql, snake_case naming)
```

## 2. Project Layer Reference

| Layer | Path | Purpose |
|-------|------|---------|
| **Models** | `IBS.Models/` | Domain entities, BaseEntity, enums |
| **DTOs** | `IBS.DTOs/` | Data transfer objects (BillingDto, etc.) |
| **DataAccess** | `IBS.DataAccess/Repository/` | Generic `Repository<T>`, typed repos, `UnitOfWork` |
| **Services** | `IBS.Services/` | Typed service classes (orchestrate UoW + business logic) |
| **Web** | `IBSWeb/Areas/User/` | Controllers, Views, ViewModels |
| **Utility** | `IBS.Utility/` | Constants (`SD.cs`), helpers (`ServiceResult`, `ExceptionHelper`), `DateTimeHelper` |

---

## 3. MSAP Workflow State Machine

```
JobOrder:  Open  ──→  Closed  (auto-closes when all DTs billed)

DispatchTicket:
  Draft → Requested → Pending → For Tariff → For Approval → For Billing → Billed
                                          ↘ Disapproved ↗
             Cancelled ←──────────────────┘

Billing:  For Posting → For Collection → Collected / Paid
```

---

## 4. Standard Implementation Pattern

### 4.1 Controller Pattern (reference: `JobOrderController`)

```csharp
[Area("User")]
public class JobOrderController(
    IJobOrderService jobOrderService,
    ILogger<JobOrderController> logger) : Controller
```

- **Primary constructor** for DI
- Mutations delegated to **typed services**; pure read queries (e.g., customer search dropdowns) may access `IUnitOfWork` directly from the controller
- `[RequireAccess]` / `[RequireAnyAccess]` on every action
- `User.Identity?.Name` for current user
- `TempData["success"]` / `TempData["error"]` for feedback
- `RedirectToAction` for post-redirect-get

### 4.2 Service Pattern (reference: `JobOrderService`)

```csharp
public class JobOrderService(
    IUnitOfWork unitOfWork,
    ILogger<JobOrderService> logger,
    ) : IJobOrderService
```

- **Primary constructor** for DI
- Only `IUnitOfWork` for data access — no direct `ApplicationDbContext`
- Mutations wrapped in try-catch → return `ServiceResult`
- Read-only methods have no try-catch
- Audit trail on every mutation via `unitOfWork.AuditTrail.AddAsync()`


### 4.3 Repository Pattern (reference: `Repository<T>`)

```csharp
public class Repository<T> : IRepository<T> where T : class
```

- `GetAllAsync(filter)` / `GetAsync(filter)` / `AddAsync(entity)` / `RemoveAsync(entity)`
- Typed repos override `GetAllAsync`/`GetAsync` to add `.Include()` chains
- Repo-specific query methods (e.g., `GetPagedJobOrdersAsync`, `GenerateJobOrderNumber`)

### 4.4 View Pattern (reference: `JobOrder Index/Create`)

- **Index**: no `@model`, DataTable loaded server-side via `ModernTable.ajax()` (POST)
- **Create/Edit**: typed ViewModel (`@model JobOrderViewModel`)
- Consistent class naming: `.modern-layout > .modern-container > .modern-card > .modern-card-body`
- `@section Scripts { }` for inline JS
- `modern-btn-primary`, `modern-btn-secondary`, `modern-table`, `js-modern-select`

---

## 5. Notes & Deviations

### 5.1 Controller Architecture

| # | Topic | Location | Notes |
|---|-------|----------|-------|
| C1 | **JSON responses for AJAX forms** | `BillingController.Create/Edit` | Returns JSON via `Success()`/`Failure()` helpers instead of `TempData` + `RedirectToAction`. Intentional — both Create and Edit views use AJAX submit via `fetch()`, expecting `{ success, message, redirectUrl }`. |

### 5.2 View Layer

| # | Topic | Location | Notes |
|---|-------|----------|-------|
| V1 | **Model type inconsistency** | `Billing Create.cshtml`, `Billing Edit.cshtml` | Uses domain entity `@model Billing` with `[Bind]` attribute workaround instead of a ViewModel. Low risk — `[Bind]` restricts over-posting. |
| V2 | **Direct DataTable init** | `Collection Create.cshtml` (`#billingsTable`) | Uses direct `$('#billingsTable').DataTable({...})` instead of `ModernTable.config()` + `ModernTable.ajax()`. Intentional — this is a sub-table within a form, not a primary list, so ModernTable's server-side pattern doesn't apply. |
| V3 | **No @model on Index pages** | `JobOrder Index.cshtml`, `Collection Index.cshtml` | Inconsistent with Billing/DispatchTicket Index which declare `@model`. DataTable loads via AJAX server-side, so `@model` is functionally optional. |

## 6. Migration Architecture Notes

- **Legacy column attributes** (`[Column("RECID")]`, `[Column("CUSTNO")]`) on `DispatchTicket`, `Billing`, `Collection` indicate this was migrated from a legacy system
- Some models extend `BaseEntity` (JobOrder, Billing, Collection) while `DispatchTicket` has its own `CreatedBy`/`EditedBy` fields (legacy carryover)
- `DispatchTicket` has a `DispatchTicketId` PK but also legacy `RECID` column mapping

