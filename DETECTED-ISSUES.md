# Detected Code Problems

Entries dated 2026-09-19 and earlier are resolved or verified already fixed. Historical file/line references are retained.

## 2026-09-21
- [2026-09-21] [high] IBSWeb/Areas/User/Controllers/DispatchTicketController.cs:58 — After the requested Service Request controller/view removal, Create still redirects to the removed endpoint; POST still rejects direct creation. Replacement ticket-entry flow is pending the next module pass.
- [2026-09-21] [high] IBSWeb/Areas/User/Views/JobOrder/Details.cshtml:122 — Add, Accept and Edit Request actions still target the removed ServiceRequest controller (also lines 233, 243); IBSWeb/Views/Shared/_Navbar.cshtml:207 retains its registry link. Deferred with module cleanup per the staged removal request.
- [2026-09-21] [med] IBS.DataAccess/Repository/Msap/DispatchTicketRepository.cs:113 — Draft/Requested tickets remain excluded from the registry and IBS.Services/DispatchTicketService.cs:40 rejects editing them, while the removed controller owned acceptance. Existing request-state records need a workflow decision in the next pass; no data/status migration performed.
- [2026-09-21] [med] IBSWeb/Areas/User/Controllers/HomeController.cs:45 — Dashboard still counts Requested Service Requests after their controller/views were removed; reconcile this with the replacement workflow.

## 2026-09-19
- [2026-09-19] [med] AGENTS.md:7,29 — Build guidance incorrectly excluded Razor changes; CHANGELOG.md also claimed Razor never validates views at build. **Resolved:** require builds for .cshtml edits; a temporary invalid Razor view produced CS0117 during build, proving compilation is enabled. Probe removed; HTML/JavaScript still require separate checks.
- [2026-09-19] [high] IBSWeb/Areas/User/Controllers/PaymentTermsController.cs:149,217,271 — Create/Edit/Delete queued audit entries after their last save and committed without persisting them. **Resolved:** SaveAsync now runs after adding each audit entry and before commit.
- [2026-09-19] [med] IBSWeb/Areas/User/Controllers/ServiceRequestController.cs:496 — Deleted/date filters were ignored, and recordsTotal reflected filtered rows. **Resolved:** handle both filters and preserve the pre-filter count.
- [2026-09-19] [med] IBS.Services/SuperAdminService.cs:263 — Case-sensitive field searches were incompatible with the shared request model's lowercased query. **Resolved:** normalize both query and searchable fields for all four tables.
- [2026-09-19] [high] IBS.Services/DispatchTicketService.cs:49 — Direct creation bypassed Service Request posting and immediately assigned ForTariff. Fixed: removed direct creation service/form; legacy POST rejects requests and GET redirects to Service Request.
- [2026-09-19] [high] IBS.Services/DispatchTicketService.cs:60 — Ticket editing could promote an unposted Service Request to ForTariff when critical fields changed. Fixed: reject Draft, Requested and Service Request Deleted records before mutation.
- [2026-09-19] [med] IBS.DataAccess/Repository/Msap/DispatchTicketRepository.cs:113 — Registry excluded obsolete status names but included Draft/Requested service requests. Fixed: exclude current Service Request states from rows and totals.
- [2026-09-19] [med] IBSWeb/Areas/User/Controllers/ServiceRequestController.cs:83 — Create relied on the browser's required Job Order selector; submitted missing/closed/pending-billing Job Orders were not rejected. Fixed using the existing eligible Job Order list.
- [2026-09-19] [high] IBSWeb/Areas/User/Controllers/ServiceRequestController.cs:352 — Edit saves before adding its audit entry and then commits without another SaveAsync; the audit entry may never persist. **Fixed 2026-09-19: audit is queued before SaveAsync and transaction commit.**
- [2026-09-19] [high] IBSWeb/Areas/User/Controllers/ServiceRequestController.cs:561 — DeleteImage/DeleteVideo lack mutation-specific permission checks, workflow-state guards and audit entries; class access includes users with posting-only permission. **Fixed 2026-09-19: explicit edit permission, Draft/Requested and parent/period guards, audit entries; deleting the required image returns the request to Draft.**
- [2026-09-19] [med] IBSWeb/Areas/User/Controllers/ServiceRequestController.cs:640 — Post checks Requested status but does not revalidate the parent Job Order or closed accounting period. Create also lacks the closed-period guard used by the removed direct-creation service. **Fixed 2026-09-19: shared parent/billing/period validation on Create/Edit/Post and attachment deletion; posting also checks completeness.**
- [2026-09-19] [med] IBSWeb/Areas/User/Controllers/ServiceRequestController.cs:723 — Restore always assigns Requested, including records deleted while Draft; incomplete requests can then be posted. **Fixed 2026-09-19: Create/Edit/Restore/Post share the completeness rule; incomplete restores remain Draft.**
- [2026-09-19] [med] IBSWeb/Areas/User/Controllers/ServiceRequestController.cs:469 — Global search dereferences nullable COSNumber/TugMaster and lowercases the query without lowercasing several searched fields. **Fixed 2026-09-19: null-safe, ordinal case-insensitive matching.**
- [2026-09-19] [low] IBSWeb/Areas/User/Controllers/JobOrderController.cs:162 — Loads TicketViewModel select lists into ViewData although no view reads TicketViewModel. **Fixed 2026-09-19: removed the unused load and its now-unused population method.**
- [2026-09-19] [low] IBSWeb/Views/Shared/_Navbar.cshtml:52 — Comment still describes opt-in/localStorage activation although the modern navbar is always on. **Fixed 2026-09-19: corrected the always-on navbar comment.**

## 2026-09-16
- `med` IBSWeb/Areas/User/Controllers/MaritimeReportController.cs:271 — SalesSummary filters only billed dispatches by billing date, so qualifying unbilled dispatches are omitted from the monthly report; repository query needs the legacy unbilled branch. **Verified resolved 2026-09-19: ReportRepository.GetDispatchReportData already includes the unbilled dispatch-date branch; removed stale controller TODO.**

Running log of issues spotted during file reads/sessions. Reverse-chronological.
Severity: `high` = likely bug, `med` = smells/tech debt, `low` = cosmetic/inconsistency.
Format: `[date] [severity] file:line — description (session context)`. Fix when a task touches the file; otherwise leave for a dedicated pass.

## 2026-08-06
- `low` IBSWeb/Areas/SuperAdmin/Views/Data/Index.cshtml (pre-fix) — `getStatusClass()` returned `badge-primary/badge-info/badge-error/...` classes that never existed anywhere (badges rendered colorless), `alert alert-error` was undefined, `modern-card-header` (Home) was undefined, and the DataTable's `scrollX: true` produced a phantom blank header `<tr>` (cloned scroll-head). All fixed this session.
- `low` IBSWeb/Areas/User/Views/DispatchTicket/Index.cshtml:147 — used `status-error` badge class for Disapproved before `.status-error` existed (undefined in modern-ui.css) → rendered colorless. Fixed by adding `.status-error` to modern-ui.css this session.
- `low` IBSWeb/Areas/SuperAdmin/Views/Data/Index.cshtml — server-side `TableColumnDef` only carries Data+Title (no render/type), so the `renderStatusBadge`/currency/boolean/date branches in `buildColumns()` are dead code; status columns render as plain text. Add render hints to the column defs if badges/₱-formatting are wanted in this screen. **Fixed 2026-09-19: render status from its actual column name, use escaped text for other cells, remove unreachable renderer branches.**
- `low` IBSWeb/Areas/SuperAdmin/Controllers/DataController.cs:102 — duplicates `IBS.Models.DataTablesParameters` instead of reusing the shared model. **Fixed 2026-09-19: reuse IBS.Models request types; SuperAdmin searches normalize both query and fields.**
- `low` IBS.Services/SuperAdminService.cs (pre-fix) — GetDataAsync loaded the whole table (`GetAllAsync`→`ToListAsync`) and reflected over all rows in memory for filter/sort/paging. Replaced with SQL-side `GetPagedAsync` this session.

## 2026-08-05
- `low` IBSWeb/Areas/User/Controllers/MsapImportController.cs:1828 — `ComputeTotalHours` has no 1-hour minimum (rounds up only when fractional >= 0.75). (Session: 1h-min hours change) **Fixed 2026-09-19: apply Math.Max(totalHours, 1m) after customer-specific rounding; missing timestamps retain their existing zero result.**

## (resolved)
- 2026-08-05 — IBSWeb/Areas/User/Controllers/ServiceRequestController.cs:120,269 — applied 1-hour minimum (`Math.Max(hours, 1m)`) to legacy SR create/edit TotalHours.

## 2026-08-04
- `low` IBSWeb/Areas/User/Views/Billing/Edit.cshtml:475 — `fillDataOnStartup` comment claims jQuery `:checked` excludes disabled inputs (so it iterates all checkboxes manually). jQuery `:checked` actually matches disabled+checked inputs, so `updateTotals()`'s `:checked` usage is fine; the comment is misleading. New `rebuildBafTable()` deliberately uses `.prop('checked')` to be safe either way. (Session: BAF per-ticket work) **Fixed 2026-09-19: corrected both misleading comments; hidden inputs still submit disabled selected tickets.**
- `med` IBSWeb/Areas/User/Controllers/PaymentTermsController.cs:17 — `ILogger<SupplierController>` injected into `PaymentTermsController` (copy-paste; should be `ILogger<PaymentTermsController>`). Harmless (ILogger<T> is contravariant) but misleading. Spotted during V2 ViewModel conversion. **Verified already resolved 2026-09-19: constructor uses ILogger<PaymentTermsController>.**
- `low` IBSWeb/Areas/User/Controllers/PaymentTermsController.cs:126 — self-assignment `model.NumberOfDays = model.NumberOfDays;` (dead code). Spotted during V2 ViewModel conversion. **Fixed 2026-09-19: removed both NumberOfDays and NumberOfMonths self-assignments.**
- `med` IBSWeb/Areas/User/Controllers/BankAccountController.cs:36-51,66-92 — Create/Edit mutate DB with no `AuditTrail.AddAsync` (AGENTS.md requires audit trails on all CUD). Only `[Authorize(Roles="Admin")]` gates it. Spotted during V2 ViewModel conversion. **Fixed 2026-09-19: queue a bank-account audit entry with the mutation and persist both in the same SaveAsync.**

## (resolved)
- 2026-08-04 — IBSWeb/Areas/User/Views/MsapImport/Index.cshtml `<main>` tag imbalance — FIXED (V5 audit).
