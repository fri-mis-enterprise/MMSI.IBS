# Code Quality Debt — Hacky Patterns & Technical Debt

> Generated 2026-07-25 from ponytail audit of the full tree.
> Covers non-standard coding, workarounds, brittle patterns, and performance traps.
> **Not** about architecture correctness — the code works, these are the sharp edges.

---

## Priority Matrix

| Priority | Pattern | Impact | Ease | Files Touched |
|----------|---------|--------|------|---------------|
| P0 | `dynamic` CSV import — 17 calls, 200+ lines of manual property lookup | data corruption, slow | medium | 1 |
| P0 | Empty catch blocks swallowing failures | silent data loss | easy | 4 |
| P1 | `.Result` sync-over-async — deadlock risk in auth filters | production crash | easy | 1 |
| P1 | Full table loads (`GetAllAsync(null)`) — 25+ calls in 12 services | perf, memory | medium | 12 |
| P2 | Inline event handlers (`onclick=`) in 35+ cshtml files | maintenance | medium | 50+ |
| P2 | Hardcoded tax rates (1.12, 0.02, 0.05) as magic literals | business risk | easy | 1 |
| P2 | CSS `!important` arms race — 237 declarations | maintenance | hard | 5 |
| P3 | Dead discount formula: `rate * (0 / 100m)` | confusion | easy | 1 |
| P3 | Brittle DOM mutation in tests — `catch { }` retry loops | flaky CI | medium | 2 |

---

## P0 — Critical

### 1. `dynamic` CSV Import (17 calls)

**File:** `IBSWeb/Areas/User/Controllers/MsapImportController.cs`

Every CSV section uses `csv.GetRecords<dynamic>().ToList()` (lines 305–1518, 17 occurrences) then pulls values out via manual `GetString(record, "COLUMN_NAME")` helpers (lines 1868–1915) that do case-insensitive `ExpandoObject` dictionary lookups:

```csharp
var records = csv.GetRecords<dynamic>().ToList();  // line 305
// ... 200 lines later ...
customerCode = GetString(record, "CUSTNO");
customerName = GetString(record, "CUSTNAME");
```

**Why it's hacky:** No compile-time type safety. Column renames in the CSV silently return `""`. Every import section duplicates the same pattern. The helpers at the bottom of the file (`GetString`, `ParseDecimal`, `ParseDate`, `GetValidPortNumber`) are all workarounds for the `dynamic` choice.

**Fix:** Define a typed CsvRow DTO per import section (`CustomerCsvRow`, `BillingCsvRow`, etc.) and use `csv.GetRecords<CustomerCsvRow>()`.

---

### 2. Empty Catch Blocks Swallowing Failures

| File | Line | Code |
|------|------|------|
| `IBS.Tests.UI/PlaywrightTestBase.cs` | 242 | `catch { }` — bare empty, SweetAlert dismiss |
| `IBS.Tests.UI/PlaywrightTestBase.cs` | 263 | `catch { }` — inside retry loop |
| `IBS.Tests.UI/PlaywrightTestBase.cs` | 269 | `catch { }` — dismiss-all-SweetAlerts |
| `IBSWeb/Areas/User/Controllers/MsapImportController.cs` | 321 | `catch { /* Ignore if fails */ }` |
| `IBSWeb/Areas/User/Controllers/JobOrderController.cs` | 149, 154 | `catch { logger.LogWarning("...") }` — no exception variable, loses context |

**Why it's hacky:** Tests pass despite genuine failures. Production import silently skips rows. The `catch { }` in line 321 masks parse/import errors — imported data may be silently incomplete.

**Fix:** Remove bare catches; let failures propagate. In tests, let xUnit report the failure. In import, log the real exception or throw.

---

## P1 — High

### 3. `.Result` Sync-over-Async — Deadlock Risk

**File:** `IBS.Services/Attributes/DepartmentAuthorizeAttribute.cs:19`

```csharp
var user = userManager.GetUserAsync(context.HttpContext.User).Result;
```

**Why it's hacky:** Synchronous `OnAuthorization` (implements `IAuthorizationFilter`, not `IAsyncAuthorizationFilter`) blocks on an async call. In ASP.NET with `SynchronizationContext`, this can deadlock. The sync interface was chosen — forcing `.Result` — when `IAsyncAuthorizationFilter` exists.

**Fix:** Change to `IAsyncAuthorizationFilter` and use `await`.

**Also:** `IBS.Services/BillingService.cs:711` — `.ContinueWith(t => t.Result.Any(), cancellationToken)` which wraps async in sync unnecessarily.

---

### 4. Full Table Loads (`GetAllAsync(null)`)

Found in 12 service files, 25+ total calls:

| File | Calls | What it loads |
|------|-------|---------------|
| `IBS.Services/SuperAdminService.cs` | 12 | JobOrders, DispatchTickets, Billings, Collections — entire tables |
| `IBS.Services/ChartOfAccountService.cs` | 3 | All chart-of-accounts, then filters/pages in C# |
| `IBS.Services/VesselScheduleService.cs` | 1 | All tugboats for every conflict check |
| `IBS.Services/EmployeeService.cs` | 1 | All employees then in-memory `.Where()` |
| `IBS.Services/{Principal,Terminal,Tugboat,TugMaster,...}Service.cs` | 1 each | Entire master file tables |

**Why it's hacky:** `GetAllAsync(null)` loads every row from the table into memory, then `.Where()`, `.Skip()`, `.Take()` happens in C# (not SQL). A 50k-row ChartOfAccounts table means 50k rows pulled over the network and allocated on the heap, just to show page 1 of 20. Defeats database indexing.

**Fix:** Add paginated queries (`GetPagedAsync(filter, skip, take)`) and projection queries (`GetSelectListAsync()`) to the repository layer instead of loading everything and filtering in memory.

---

## P2 — Medium

### 5. Inline Event Handlers (35+ cshtml files)

Widely scattered — worst offenders:

| File | Inline handlers |
|------|----------------|
| `Areas/User/Views/JobOrder/Details.cshtml` | 12 (`onclick`) |
| `Areas/Admin/Views/User/Index.cshtml` | 6 |
| `Areas/User/Views/ChartOfAccount/Index.cshtml` | 5 |
| `Areas/User/Views/DispatchTicket/Index.cshtml` | 5 |
| `Areas/User/Views/Billing/Index.cshtml` | 3 |
| `Areas/User/Views/Supplier/Create.cshtml` | 8 |
| `Areas/User/Views/VesselSchedule/Create.cshtml` | 2 |

**Why it's hacky:** Inline `onclick="confirmDelete(...)"` pollutes global scope, can't attach multiple handlers, mixes concerns. These should use `addEventListener` in `@section Scripts { }` or a `.js` file.

**Fix:** Move to `$('#element').on('click', function() { ... })` inside the `@section Scripts` block.

---

### 6. Hardcoded Tax Rates as Magic Literals

**File:** `IBS.Services/CollectionService.cs:272`

```csharp
ewt = b.IsVatable ? (b.Amount / 1.12m) * 0.02m : b.Amount * 0.02m;
wvat = b.IsVatable ? (b.Amount / 1.12m) * 0.05m : 0;
```

Where `1.12m` = 12% VAT, `0.02m` = 2% EWT, `0.05m` = 5% WVAT.

**Why it's hacky:** Tax rates change. When the Philippines adjusts VAT to 14% or EWT thresholds, someone must grep for `1.12m` and hope they found all occurrences. These are business-domain constants that belong in configuration or a `TaxConstants` class.

**Fix:** Extract to `IBS.Utility/Constants/TaxConstants.cs`:
```csharp
public static class TaxConstants
{
    public const decimal VatRate = 0.12m;
    public const decimal VatMultiplier = 1m + VatRate;  // 1.12
    public const decimal EwtRate = 0.02m;
    public const decimal WvatRate = 0.05m;
}
```

Also check `IBS.Services/BillingService.cs:240-241` for duplicate VAT computations.

---

### 7. CSS `!important` Arms Race — 237 Declarations

| File | `!important` count |
|------|-------------------|
| `wwwroot/css/site.css` | 60+ |
| `wwwroot/css/modern-ui.css` | 50+ |
| `wwwroot/css/modern-navbar.css` | 15+ |
| `wwwroot/css/form-style.css` | 10+ |
| `wwwroot/css/index-style.css` | 2 |

**Why it's hacky:** Each `!important` is a specificity battle won by force. The next developer adds another `!important` to override it. The cascade is broken — a properly layered CSS architecture rarely needs any `!important`.

**Fix:** Audit the CSS cascade. Remove `!important` one layer at a time, starting from the lowest-specificity base styles. Use BEM or a utility-first approach to avoid specificity wars.

---

### 8. Dead Discount Formula

**File:** `IBS.Services/DispatchTicketService.cs:581,587`

```csharp
decimal dispatchDiscountAmount = dispatchRate * (0 / 100m);
decimal bafDiscountAmount = bafRate * (0 / 100m);
```

**Why it's hacky:** `0 / 100m` always equals `0`. This is either a planned feature that was never wired, or dead code that creates confusion. A new developer will wonder if discount logic exists.

**Fix:** Remove the variables, or if discount is needed later, implement it then. Comment with `// ponytail: discount not yet implemented` if deferring.

---

## P3 — Low

### 9. Brittle DOM Mutation in UI Tests

**File:** `IBS.Tests.UI/PlaywrightTestBase.cs:250-270`

```csharp
// 20-line method with retries, catch { }, and WaitForTimeout(300)
// called DismissAnySweetAlertAsync
```

Also `SelectModernOptionAsync` (lines 102-188, 90 lines) with:
- 3 retry attempts with jQuery escape hatch
- Regex-based option matching
- 300-500ms `WaitForTimeout` calls

**Why it's hacky:** Tests compensate for UI fragility with retries and timeouts instead of fixing the UI. The `catch { }` on line 263 means a SweetAlert that doesn't dismiss is silently ignored — the test may pass when it shouldn't, or fail 50 lines later with a confusing error.

**Fix:** Add `data-testid` attributes to the modern-select and SweetAlert2 confirm buttons, replace `SelectModernOptionAsync` with a deterministic `Page.Locator('[data-testid="..."]').ClickAsync()`.

---

### 10. Dev-Tools-Blocking JavaScript

**File:** `IBSWeb/wwwroot/js/disable-dev-tools-in-print.js` (57 lines)

Blocks F12, Ctrl+P, Ctrl+Shift+I, Ctrl+U, right-click. Detects DevTools by checking `window.outerWidth - window.innerWidth > 160` and destroys the page with `document.body.innerHTML = ''`.

**Why it's hacky:** All trivially bypassable. Ctrl+P blocking breaks the browser's native print. The 160px threshold is unreliable across monitors/zoom. `document.body.innerHTML = ''` is destructive and breaks screen reader accessibility.

**Fix:** Remove the file. If print protection is needed, add a server-side PDF-with-watermark option. Accept that client-side "security" is cosmetic.

---

### 11. Session-Storage Spinner State

**File:** `IBSWeb/wwwroot/js/spinner.js`

```javascript
if (sessionStorage.getItem('isSubmitting') === 'true') { ... }
```

**Why it's hacky:** Session storage is cleared on tab close — if the page is refreshed mid-submission, the spinner state is lost. If two tabs are open, they share session storage and can interfere.

**Fix:** Use a local variable or a form-level flag (`form.dataset.submitting = 'true'`). No global state needed.

---

## Summary

| Category | Count | Key Files |
|----------|-------|-----------|
| `dynamic` bypassing type safety | 17 calls | `MsapImportController.cs` |
| Empty catches hiding failures | 6 | `PlaywrightTestBase.cs`, `MsapImportController.cs`, `JobOrderController.cs` |
| `.Result` deadlock risk | 2 | `DepartmentAuthorizeAttribute.cs`, `BillingService.cs` |
| Full table loads (perf) | 25+ calls | 12 service files |
| Inline event handlers | 35+ | 50+ cshtml files |
| Magic business constants | 3 values | `CollectionService.cs` |
| CSS `!important` | 237 | 5 CSS files |
| Dead code (discount) | 2 lines | `DispatchTicketService.cs` |
| Brittle test DOM hacks | 2 methods | `PlaywrightTestBase.cs` |
| Fake client-side security | 1 file | `disable-dev-tools-in-print.js` |

Each finding is cross-referenced to a file+line. None is speculative.
