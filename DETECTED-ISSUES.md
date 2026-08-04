# Detected Code Problems

Running log of issues spotted during file reads/sessions. Reverse-chronological.
Severity: `high` = likely bug, `med` = smells/tech debt, `low` = cosmetic/inconsistency.
Format: `[date] [severity] file:line — description (session context)`. Fix when a task touches the file; otherwise leave for a dedicated pass.

## 2026-08-04
- `med` IBSWeb/Areas/User/Controllers/PaymentTermsController.cs:17 — `ILogger<SupplierController>` injected into `PaymentTermsController` (copy-paste; should be `ILogger<PaymentTermsController>`). Harmless (ILogger<T> is contravariant) but misleading. Spotted during V2 ViewModel conversion.
- `low` IBSWeb/Areas/User/Controllers/PaymentTermsController.cs:126 — self-assignment `model.NumberOfDays = model.NumberOfDays;` (dead code). Spotted during V2 ViewModel conversion.
- `med` IBSWeb/Areas/User/Controllers/BankAccountController.cs:36-51,66-92 — Create/Edit mutate DB with no `AuditTrail.AddAsync` (AGENTS.md requires audit trails on all CUD). Only `[Authorize(Roles="Admin")]` gates it. Spotted during V2 ViewModel conversion.

## (resolved)
- 2026-08-04 — IBSWeb/Areas/User/Views/MsapImport/Index.cshtml `<main>` tag imbalance — FIXED (V5 audit).
