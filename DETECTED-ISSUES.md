# Detected Code Problems

Running log of issues spotted during file reads/sessions. Reverse-chronological.
Severity: `high` = likely bug, `med` = smells/tech debt, `low` = cosmetic/inconsistency.
Format: `[date] [severity] file:line — description (session context)`. Fix when a task touches the file; otherwise leave for a dedicated pass.

## 2026-08-05
- `med` IBS.Tests/Services/{BillingServiceTests,LegacyBillingTests,TaxAnalysisTests}.cs — all tests fail in the class constructor at `new Mock<JobOrderService>(...)` (verified failing on a clean checkout, unrelated to the BAF charge-type work). JobOrderService likely gained members the direct `Mock<JobOrderService>` can't proxy. Fix when a task touches these tests.
- `low` IBSWeb/Areas/User/Controllers/MsapImportController.cs:1828 — `ComputeTotalHours` has no 1-hour minimum (rounds up only when fractional >= 0.75). (Session: 1h-min hours change)

## (resolved)
- 2026-08-05 — IBSWeb/Areas/User/Controllers/ServiceRequestController.cs:120,269 — applied 1-hour minimum (`Math.Max(hours, 1m)`) to legacy SR create/edit TotalHours.

## 2026-08-04
- `low` IBSWeb/Areas/User/Views/Billing/Edit.cshtml:475 — `fillDataOnStartup` comment claims jQuery `:checked` excludes disabled inputs (so it iterates all checkboxes manually). jQuery `:checked` actually matches disabled+checked inputs, so `updateTotals()`'s `:checked` usage is fine; the comment is misleading. New `rebuildBafTable()` deliberately uses `.prop('checked')` to be safe either way. (Session: BAF per-ticket work)
- `med` IBSWeb/Areas/User/Controllers/PaymentTermsController.cs:17 — `ILogger<SupplierController>` injected into `PaymentTermsController` (copy-paste; should be `ILogger<PaymentTermsController>`). Harmless (ILogger<T> is contravariant) but misleading. Spotted during V2 ViewModel conversion.
- `low` IBSWeb/Areas/User/Controllers/PaymentTermsController.cs:126 — self-assignment `model.NumberOfDays = model.NumberOfDays;` (dead code). Spotted during V2 ViewModel conversion.
- `med` IBSWeb/Areas/User/Controllers/BankAccountController.cs:36-51,66-92 — Create/Edit mutate DB with no `AuditTrail.AddAsync` (AGENTS.md requires audit trails on all CUD). Only `[Authorize(Roles="Admin")]` gates it. Spotted during V2 ViewModel conversion.

## (resolved)
- 2026-08-04 — IBSWeb/Areas/User/Views/MsapImport/Index.cshtml `<main>` tag imbalance — FIXED (V5 audit).
