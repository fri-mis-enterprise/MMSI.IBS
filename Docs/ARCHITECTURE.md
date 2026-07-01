# MMSI-IBS / MSAP Architecture

> **MSAP** = Maritime Service Workflow: **Job Order → Dispatch Ticket → Billing → Collection**

---

## 1. Data Flow (View → Controller → Service → Repository → Database)

```
.cshtml View
    ↓  (form POST / AJAX)
Controller  ←  DI: Services, IUnitOfWork, SignalR hubs, etc.
    ↓
Service  ←  DI: IUnitOfWork, ILogger, INotificationService, etc.
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
- All business logic delegated to **typed services** — never access `IUnitOfWork` directly
- `[RequireAccess]` / `[RequireAnyAccess]` on every action
- `User.Identity?.Name` for current user
- `TempData["success"]` / `TempData["error"]` for feedback
- `RedirectToAction` for post-redirect-get

### 4.2 Service Pattern (reference: `JobOrderService`)

```csharp
public class JobOrderService(
    IUnitOfWork unitOfWork,
    ILogger<JobOrderService> logger,
    INotificationService notificationService) : IJobOrderService
```

- **Primary constructor** for DI
- Only `IUnitOfWork` for data access — no direct `ApplicationDbContext`
- Mutations wrapped in try-catch → return `ServiceResult`
- Read-only methods have no try-catch
- Audit trail on every mutation via `unitOfWork.AuditTrail.AddAsync()`
- Notifications on workflow transitions via `INotificationService`

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

## 5. Inconsistencies & Deviations

### 5.1 Controller Architecture

| # | Issue | Location | Notes |
|---|-------|----------|-------|
| C1 | **Direct IUnitOfWork usage** | `DispatchTicketController`, `BillingController`, `CollectionController` | Some actions access `unitOfWork.X` directly alongside service calls instead of delegating all data access to services |
| C2 | **No auth on action** | `MasterFileController.GenerateExcel` | Export endpoint lacks authorization attribute |
| C3 | **No auth on status change** | `DispatchTicketController.ChangeStatus` | Status mutation endpoint lacks authorization attribute |
| C4 | **Username resolution inconsistency** | `BillingController` | Uses `UserManager<ApplicationUser>` instead of `User.Identity?.Name` (as used in JobOrder, DispatchTicket, Collection) |
| C5 | **JSON vs TempData responses** | `BillingController.Create/Edit` | Returns JSON via `Success()`/`Failure()` helpers instead of `TempData` + `RedirectToAction` |

### 5.2 Service Layer

| # | Issue | Location | Notes |
|---|-------|----------|-------|
| S1 | **Interface-per-service (single impl)** | All 4 MSAP services | Each has exactly one implementation, violating AGENTS.md "No interface with one implementation" rule — kept for test mocking |
| S2 | **Customer search duplicated across services** | `JobOrderService`, `DispatchTicketService`, `BillingService`, `CollectionService` | Each service has its own `SearchCustomersAsync` with different return shapes instead of a shared helper or `CustomerRepository` method |

### 5.3 Repository Layer

| # | Issue | Location | Notes |
|---|-------|----------|-------|
| R1 | **RemoveRangeAsync not on interface** | `Repository<T>` implementation | Public method exists only on implementation class, not on `IRepository<T>` |
| R2 | **MapSupplierToDTO, RemoveRecords not on interface** | `Repository<T>` | Public methods exist only on implementation, not on interface |
| R3 | **Property declaration style mismatch** | `UnitOfWork` | Mix of `{ get; }` and `{ get; private set; }` styles across properties |

### 5.4 View Layer

| # | Issue | Location | Notes |
|---|-------|----------|-------|
| V1 | **Model type inconsistency** | `Billing Create.cshtml` | Uses domain entity `@model Billing` with `[Bind]` workaround instead of a dedicated ViewModel |
| V2 | **Direct DataTable init bypassing ModernTable** | `Collection Create.cshtml` (`#billingsTable`) | Direct `$('#billingsTable').DataTable({...})` instead of `ModernTable.config()` + `ModernTable.ajax()` |
| V3 | **No @model on Index pages** | `JobOrder Index.cshtml`, `Collection Index.cshtml` | Inconsistent with Billing/DispatchTicket Index which declare `@model` |
| V4 | **Missing partial views for repeated UI** | Customer search, Port-Terminal cascade, media preview | Same customer search ~80 lines repeated in 3+ Create/Edit views |

### 5.5 Authorization & Access Control

| # | Issue | Location | Notes |
|---|-------|----------|-------|
| A1 | **Missing access control** | `MasterFileController` (all actions) | No authorization on any endpoint |
| A2 | **Missing access control** | `DispatchTicketController.ChangeStatus` | No authorization attribute on status mutation |

## 6. Migration Architecture Notes

- **Legacy column attributes** (`[Column("RECID")]`, `[Column("CUSTNO")]`) on `DispatchTicket`, `Billing`, `Collection` indicate this was migrated from a legacy system
- Some models extend `BaseEntity` (JobOrder, Billing, Collection) while `DispatchTicket` has its own `CreatedBy`/`EditedBy` fields (legacy carryover)
- `DispatchTicket` has a `DispatchTicketId` PK but also legacy `RECID` column mapping

