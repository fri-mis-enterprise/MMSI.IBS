# Detected Code Problems

Running log of issues spotted during file reads/sessions. Reverse-chronological.
Severity: `high` = likely bug, `med` = smells/tech debt, `low` = cosmetic/inconsistency.
Format: `[date] [severity] file:line — description (session context)`. Fix when a task touches the file; otherwise leave for a dedicated pass.

## 2026-08-06
- `low` IBSWeb/Areas/SuperAdmin/Views/Data/Index.cshtml (pre-fix) — `getStatusClass()` returned `badge-primary/badge-info/badge-error/...` classes that never existed anywhere (badges rendered colorless), `alert alert-error` was undefined, `modern-card-header` (Home) was undefined, and the DataTable's `scrollX: true` produced a phantom blank header `<tr>` (cloned scroll-head). All fixed this session.
- `low` IBSWeb/Areas/User/Views/DispatchTicket/Index.cshtml:147 — used `status-error` badge class for Disapproved before `.status-error` existed (undefined in modern-ui.css) → rendered colorless. Fixed by adding `.status-error` to modern-ui.css this session.
- `low` IBSWeb/Areas/SuperAdmin/Views/Data/Index.cshtml — server-side `TableColumnDef` only carries Data+Title (no render/type), so the `renderStatusBadge`/currency/boolean/date branches in `buildColumns()` are dead code; status columns render as plain text. Add render hints to the column defs if badges/₱-formatting are wanted in this screen.
- `low` IBSWeb/Areas/SuperAdmin/Controllers/DataController.cs:102 — duplicates `IBS.Models.DataTablesParameters` instead of reusing the shared model.
- `low` IBS.Services/SuperAdminService.cs (pre-fix) — GetDataAsync loaded the whole table (`GetAllAsync`→`ToListAsync`) and reflected over all rows in memory for filter/sort/paging. Replaced with SQL-side `GetPagedAsync` this session.

## 2026-08-05
- `low` IBS.Tests/Services/{BillingServiceTests,LegacyBillingTests,TaxAnalysisTests}.cs — `new Mock<JobOrderService>(...)` failed because commit dbda443 ("accept rider suggestion") sealed the class & dropped `virtual` from `CreateJobOrderAsync`/`UpdateJobOrderAsync`/`TryAutoCloseAsync`. Tests only mock repo/IUnitOfWork seams elsewhere; sealing a non-interfaced domain service breaks mockability (only sealed service here, `MemoryCacheService`, is sealed because it has an interface). Decision: keep domain services non-sealed + virtual on stubbed methods.
- `low` IBSWeb/Areas/User/Controllers/MsapImportController.cs:1828 — `ComputeTotalHours` has no 1-hour minimum (rounds up only when fractional >= 0.75). (Session: 1h-min hours change)

## (resolved)
- 2026-08-06 — IBS.Services/JobOrderService.cs + IBS.Tests/Services/{BillingServiceTests,LegacyBillingTests,TaxAnalysisTests}.cs + Controllers/JobOrderControllerTests.cs — reverted `sealed`, restored `virtual` (Create/Update/TryAutoClose) so `Mock<JobOrderService>` proxies again. Fixed `PostBillingAsync_PopulatesSalesBook_WithWht_LegacyData` (missing Vessel + empty ticket-list setups; NRE inside txn was swallowed by the mock `ExecuteInTransactionAsync` that returns a completed task). 34/34 tests pass.
- 2026-08-05 — IBSWeb/Areas/User/Controllers/ServiceRequestController.cs:120,269 — applied 1-hour minimum (`Math.Max(hours, 1m)`) to legacy SR create/edit TotalHours.

## 2026-08-04
- `low` IBSWeb/Areas/User/Views/Billing/Edit.cshtml:475 — `fillDataOnStartup` comment claims jQuery `:checked` excludes disabled inputs (so it iterates all checkboxes manually). jQuery `:checked` actually matches disabled+checked inputs, so `updateTotals()`'s `:checked` usage is fine; the comment is misleading. New `rebuildBafTable()` deliberately uses `.prop('checked')` to be safe either way. (Session: BAF per-ticket work)
- `med` IBSWeb/Areas/User/Controllers/PaymentTermsController.cs:17 — `ILogger<SupplierController>` injected into `PaymentTermsController` (copy-paste; should be `ILogger<PaymentTermsController>`). Harmless (ILogger<T> is contravariant) but misleading. Spotted during V2 ViewModel conversion.
- `low` IBSWeb/Areas/User/Controllers/PaymentTermsController.cs:126 — self-assignment `model.NumberOfDays = model.NumberOfDays;` (dead code). Spotted during V2 ViewModel conversion.
- `med` IBSWeb/Areas/User/Controllers/BankAccountController.cs:36-51,66-92 — Create/Edit mutate DB with no `AuditTrail.AddAsync` (AGENTS.md requires audit trails on all CUD). Only `[Authorize(Roles="Admin")]` gates it. Spotted during V2 ViewModel conversion.

## (resolved)
- 2026-08-04 — IBSWeb/Areas/User/Views/MsapImport/Index.cshtml `<main>` tag imbalance — FIXED (V5 audit).
