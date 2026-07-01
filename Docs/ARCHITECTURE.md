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

| # | Issue | Location | Standard | Actual | Fix |
|---|-------|----------|----------|--------|-----|
| C1 | **Direct IUnitOfWork usage** | `DispatchTicketController`, `BillingController`, `CollectionController` | Delegate all data access to services | Some actions access `unitOfWork.X` directly alongside service calls | Move direct UoW calls into the respective services |
| C2 | **No auth on action** | `MasterFileController.GenerateExcel` | `[RequireAccess]` or `[RequireAnyAccess]` on every action | No authorization attribute | Add `[RequireAccess(ProcedureEnum.ManageMaritimeMasterFile)]` |
| C3 | **No auth on status change** | `DispatchTicketController.ChangeStatus` | `[RequireAccess]` on all mutation endpoints | No authorization attribute | Add appropriate `[RequireAccess]` |
| C4 | **Username resolution inconsistency** | `BillingController` | `User.Identity?.Name` (JobOrder, DispatchTicket, Collection) | Uses `UserManager<ApplicationUser>` to fetch user + claims | Replace with `User.Identity?.Name` |
| C5 | **JSON vs TempData responses** | `BillingController.Create/Edit` | `TempData` + `RedirectToAction` (JobOrder, Collection) | Returns JSON via `Success()`/`Failure()` helpers | Standardize on TempData + redirect (or keep if AJAX submit is intentional — document why) |

### 5.2 Service Layer

| # | Issue | Location | Standard | Actual | Fix |
|---|-------|----------|----------|--------|-----|
| S1 | **Traditional constructor instead of primary** | `CollectionService` | Primary constructor (JobOrder, DispatchTicket, Billing services) | Field-backed traditional constructor with `_field` naming | Convert to primary constructor |
| S2 | **Interface-per-service (single impl)** | All 4 MSAP services | AGENTS.md: "No interface with one implementation" | `IJobOrderService`/`JobOrderService`, etc. — each has exactly one implementation | Either remove interfaces and use classes directly, or keep only if testing requires mocking |

### 5.3 Repository Layer

| # | Issue | Location | Standard | Actual | Fix |
|---|-------|----------|----------|--------|-----|
| R1 | **Immediate SaveChanges in base Repository** | `Repository<T>.AddAsync()`, `.RemoveAsync()` | UoW pattern: accumulate changes, call `SaveAsync()` once | Calls `SaveChangesAsync()` immediately on every add/remove | Remove `SaveChangesAsync()` from base `AddAsync`/`RemoveAsync` — let the caller's `unitOfWork.SaveAsync()` control persistence |
| R2 | **RemoveRangeAsync not on interface** | `Repository<T>` implementation | All public methods should be on `IRepository<T>` | `RemoveRangeAsync` exists only on implementation class | Add to `IRepository<T>` |
| R3 | **MapSupplierToDTO, RemoveRecords not on interface** | `Repository<T>` | Interface should define all public contracts | Implementation-only methods | Add to interface or make private |
| R4 | **Property declaration style mismatch** | `UnitOfWork` | Consistent `{ get; }` or `{ get; private set; }` | Mix of both styles | Pick one pattern and align all properties |

### 5.4 View Layer

| # | Issue | Location | Standard | Actual | Fix |
|---|-------|----------|----------|--------|-----|
| V1 | **Model type inconsistency** | `Billing Create.cshtml` | Typed ViewModel (`JobOrderViewModel`, `ServiceRequestViewModel`, `CreateCollectionViewModel`) | Uses domain entity `@model Billing` directly | Create a dedicated `BillingViewModel` or use existing pattern |
| V2 | **CSS class inconsistency** | DispatchTicket/Billing/Collection Create vs JobOrder Create | Consistent input class naming | JobOrder uses `form-control` + inline border/radius styles; others use `.modern-input` | Align all forms to use `.modern-input` (or `form-control`) consistently |
| V3 | **Direct DataTable init bypassing ModernTable** | `Collection Create.cshtml` (`#billingsTable`) | All Index pages use `ModernTable.config()` + `ModernTable.ajax()` | Direct `$('#billingsTable').DataTable({...})` | Wrap in ModernTable utility or document why inline is needed |
| V4 | **No @model on Index pages** | `JobOrder Index.cshtml`, `Collection Index.cshtml` | Optional (DataTable loads via AJAX) | No `@model` declaration | Consider adding `@model IEnumerable<T>` for consistency with Billing/DispatchTicket Index |
| V5 | **Missing partial views for repeated UI** | Customer search, Port-Terminal cascade, media preview | Avoid inline code duplication | Same customer search ~80 lines repeated in 3+ Create/Edit views | Extract to shared partials or JS modules |

### 5.5 Authorization & Access Control

| # | Issue | Location | Standard | Actual | Fix |
|---|-------|----------|----------|--------|-----|
| A1 | **Missing access control** | `MasterFileController` (all actions) | `[RequireAccess]` / `[RequireAnyAccess]` | No authorization anywhere on this controller | Add per-action or class-level `[Authorize]` |
| A2 | **Missing access control** | `DispatchTicketController.ChangeStatus` | Same as above | No auth attribute | Add `[RequireAccess(ProcedureEnum.EditDispatchTicket)]` |

### 5.6 Code Organization & Duplication

| # | Issue | Location | Standard | Actual | Fix |
|---|-------|----------|----------|--------|-----|
| D1 | **Customer search duplicated across services** | Every service has its own `SearchCustomersAsync` | Single shared query method | 4 separate implementations (`JobOrderService`, `DispatchTicketService`, `BillingService`, `CollectionService`) | Consolidate into a shared helper or add to `CustomerRepository` |
| D2 | **`DispatchTicketController.ChangeStatus` bypasses service** | `DispatchTicketController.ChangeStatus` | All mutations through typed service | Direct UoW access in controller | Move status change logic into `IDispatchTicketService` |

---

## 6. Migration Architecture Notes

- **Legacy column attributes** (`[Column("RECID")]`, `[Column("CUSTNO")]`) on `DispatchTicket`, `Billing`, `Collection` indicate this was migrated from a legacy system
- Some models extend `BaseEntity` (JobOrder, Billing, Collection) while `DispatchTicket` has its own `CreatedBy`/`EditedBy` fields (legacy carryover)
- `DispatchTicket` has a `DispatchTicketId` PK but also legacy `RECID` column mapping

---

## 7. AGENTS.md Rule Compliance

| Rule | Status | Notes |
|------|--------|-------|
| Primary constructors | ❌ Partial | `CollectionService` uses traditional constructor |
| No interface with one impl | ❌ Violated | All 4 MSAP services have single-impl interfaces |
| `TreatWarningsAsErrors` | ✅ | Enforced in `.csproj` |
| Audit trail on every mutation | ✅ | Present in all service methods |
| Workflow state validation | ✅ | Guard clauses at top of mutation methods |
| `DateTimeHelper.GetCurrentPhilippineTime()` | ✅ | Used consistently |
| `IUnitOfWork` exposes all repos | ✅ | Via `UnitOfWork` properties |

---

## 8. Fix Priority Ranking

| Priority | Issue | Effort | Risk | Why |
|----------|-------|--------|------|-----|
| **P0** | R1: Immediate SaveChanges in base Repository | Medium | High | Data integrity risk — partial saves bypass UoW transaction |
| **P0** | A1/A2: Missing auth on MasterFile/ChangeStatus | Low | High | Security hole — unauthenticated access to data |
| **P1** | C1: Direct IUnitOfWork in controllers | Medium | Medium | Bypasses service layer, inconsistent transaction handling |
| **P1** | S1: CollectionService traditional constructor | Low | Low | Code style inconsistency |
| **P1** | V1: Billing Create uses domain entity as model | Medium | Medium | Over-posting risk, [Bind] attribute needed as workaround |
| **P1** | V2: CSS class naming inconsistency | Low | Low | Visual inconsistency |
| **P2** | S2: Interface-per-service | Low | Low | Violates project convention but functionally harmless |
| **P2** | C4: Username resolution inconsistency | Low | Low | Inconsistent but both work |
| **P2** | D1: Duplicated customer search | Medium | Low | Code smell, maintenance burden |
| **P2** | R2/R3: Interface gaps | Low | Low | Missing interface members |
| **P3** | V5: Repeated UI patterns | Medium | Low | Maintainability |
| **P3** | R4: Property style in UnitOfWork | Low | Low | Cosmetic |
