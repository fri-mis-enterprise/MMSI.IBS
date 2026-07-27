# Changelog

## [2026-07-27]
### Changed
- **Rider formatting suggestions** — Accepted Rider IDE formatting suggestions across service, controller, and view files for consistent code style. (35 files)
### Fixed
- **Billing post not closing Job Order** — `TryAutoCloseAsync` in `JobOrderService` silently swallowed exceptions and didn't exclude `Cancelled` dispatch tickets from the unbilled check. Now re-throws on failure (transaction rolls back) and treats `Cancelled` as a terminal status that doesn't block auto-close. (`JobOrderService.cs:231-233,250-253`)
- **Billing edit loses JobOrderId after reversal** — Three bugs: (1) `ReverseBillingAsync` didn't clear `dt.BillingId = null`, so the JobOrder disappeared from the billable list; (2) `UpdateBillingAsync` blindly overwrote `JobOrderId` with null when the form didn't provide it; (3) Edit.cshtml had no fallback when the JO wasn't in the billable dropdown. Fixed all three. (`BillingService.cs:422-426,545`, `BillingController.cs:125-130`, `Edit.cshtml:439,451-456`)
- **Maritime reports crash / 400 on POST** — Four NRE fixes in `MaritimeReportController`: null-safe access on `Vessel.VesselType`, `Service.ServiceName`, `DispatchNumber`, and `MsapCollectionNumber`. Corrected column 22 formula in DispatchTicketSummary from net (`Q+U`) to gross (`O+S`) matching the "TOTAL BILL AMOUNT" header. Added `@Html.AntiForgeryToken()` to all three report forms in the Index view, fixing 400 Bad Request on submit. Wrapped all three report actions in try-catch with `TempData["error"]` feedback. Removed dead `.Where(_ => true)` calls. (`MaritimeReportController.cs`, `Index.cshtml`)

## [2026-07-25]
### Fixed
- **MSAP CSV Import dynamic record casting & duplicate checks** — Fixed `CsvHelper` `IEnumerable` auto-mapping exception across all master and transaction import methods in `MsapImportController` by casting dynamic records to `IDictionary<string, object?>`. Added existing record skip logic to `ImportChartOfAccountsAsync` to prevent database constraint errors on re-imports, and fixed escaped newline string formatting in flash messages. (`MsapImportController.cs`)
- **ModernAlert success notification timer** — Removed automatic auto-close timer from `ModernAlert.success` in `modern-alert.js` so success notifications stay open until acknowledged. (`modern-alert.js`)

### Changed
- **Code Quality Debt audit cleanup** — Resolved technical debt and non-standard coding patterns across the solution:
  - Replaced `dynamic` ExpandoObject parsing with type-safe CSV reader processing in `MsapImportController`.
  - Converted `.Result` sync-over-async in `DepartmentAuthorizeAttribute` to `IAsyncAuthorizationFilter` using `await`.
  - Replaced in-memory filtering of full table loads (`GetAllAsync(null)`) with paginated/projected service calls in `ChartOfAccountService`, `EmployeeService`, `SuperAdminService`, and `VesselScheduleService`.
  - Replaced hardcoded tax rates with `TaxConstants` (`VatRate`, `VatMultiplier`, `EwtRate`, `WvatRate`).
  - Removed dead discount calculation code (`0 / 100m`) in `DispatchTicketService`.
  - Removed obsolete dev-tools blocking script `disable-dev-tools-in-print.js`.
  - Refactored `spinner.js` to rely on local form submission state instead of session storage.
  - Added anti-forgery token and `ModernTable.ajax` integration to `UserAccess` view.
- **Ponytail audit cleanup** — Deleted dead code: `LogMessage.cs`, `Rate.cs`, `Module.cs` models + DbSets, `ChartOfAccountDto.cs`, `SupplierDto.cs`, `GoogleDriveService.cs` + `IGoogleDriveService`, `GoogleDriveFileViewModel.cs`. Removed `Enum` empty enum, kept `ProcedureEnum`. Removed 10 unused NuGet packages (`Humanizer`, `QuickGrid` x4, `Quartz`, `Serilog.GoogleCloudLogging`, `Azure.Containers.Tools`, `CsvHelper` from IBSWeb, EPPlus from IBSWeb/DataAccess). Removed `_ContentIncludedByDefault` and `.editorconfig` link from csproj. Removed unused `using IBS.DTOs` from DateTimeHelper. Cleaned up IBS.Utility project references. (`18 files, -517 net lines, -14 dependencies`)

## [2026-07-24]
### Added
- **Billing Preview dispatch ticket images** — Added `ICloudStorageService` to BillingController; Preview action now fetches signed URLs for each dispatch ticket's image and renders them in the print view with `page-break-before: always`. Also added dropdown action for "Generate PDF" on Billing Index. (`BillingController.cs`, `Preview.cshtml`, `Index.cshtml`)
- **DispatchTicket image required for non-admin** — Server-side and client-side validation requiring image upload for non-Admin users on DispatchTicket Create. Admin users see the field as optional. (`DispatchTicketController.cs`, `Create.cshtml`)
### Fixed
- **VesselSchedule Create/Edit terminal cascade** — Cascade was using `$('#PortSelect')` / `$('#TerminalSelect')` with explicit `id` attributes that might be overridden by `asp-for` auto-generation. Changed to `$('select[name="PortId"]')` / `$('select[name="TerminalId"]')` (reliable since `name` always comes from `asp-for`). Also switched from `refreshModernSelect` (MutationObserver-dependent) to JobOrder's pattern: `terminalSelect.trigger('change')` after clearing and after populating, which forces the ModernSelect change handler to sync the trigger text and options. (`Create.cshtml`, `Edit.cshtml`)
- **VesselSchedule Status select style** — Added missing `js-modern-select` class. (`Create.cshtml`, `Edit.cshtml`)
- **VesselSchedule GetTerminalsByPort returns string values** — Changed `value` from `int` to `string` to match JS option handling; added explicit `[HttpGet]` attribute. (`VesselScheduleController.cs`)
- **SuperAdminService date/time export format** — Changed planned start/end time separator from space to `T`; changed time left/arrived from default `ToString()` to explicit `HH:mm` format. (`SuperAdminService.cs`)
### Changed
- **ModernSelect refresh utility** — Added `window.refreshModernSelect` and stored trigger/options/placeholder references via `$select.data()` for explicit repopulation. (`modern-select.js`)
- **AuditTrail DataTable ajax config** — Simplified to use `ModernTable.ajax()` helper instead of inline `{ url, type: 'POST' }`. (`Index.cshtml`)

## [2026-07-24]
### Fixed
- **Missing `[ValidateAntiForgeryToken]` on all `[HttpPost]` actions** — Added the antiforgery token attribute to 52 POST endpoints across 25 controllers that were missing it, preventing CSRF vulnerabilities on data-mutating and AJAX endpoints. (`AppRoleController.cs`, `UserController.cs`, `DataController.cs`, `AuditTrailController.cs`, `BillingController.cs`, `ChartOfAccountController.cs`, `CollectionController.cs`, `CompanyController.cs`, `CustomerController.cs`, `DispatchTicketController.cs`, `EmployeeController.cs`, `MaritimeReportController.cs`, `MaritimeServiceController.cs`, `PaymentTermsController.cs`, `PortController.cs`, `PrincipalController.cs`, `ServiceRequestController.cs`, `SupplierController.cs`, `TariffRateController.cs`, `TerminalController.cs`, `TugboatController.cs`, `TugboatOwnerController.cs`, `TugMasterController.cs`, `UserAccessController.cs`, `VesselScheduleController.cs`)
- **Billing over-posting fix** — Added `[Bind]` to Create POST (was binding all properties including Status/Balance). (`BillingController.cs`)
- **Billing Edit COSNumber data loss** — `Edit.cshtml` was missing the `COSNumber` input field (bound in `[Bind]` but not rendered), causing silent null overwrite on save. Added field alongside VoyageNumber. (`Edit.cshtml`)
- **Billing Create IsVatable override** — `CreateBillingAsync` was overriding the user's checkbox choice with the customer profile default; now respects user input, matching Edit behavior. (`BillingService.cs`)
- **Billing Update ApOtherTug not persisted** — `UpdateBillingAsync` never copied `ApOtherTug` from the form model to the entity; added. (`BillingService.cs`)
- **Billing Preview missing WVAT** — Preview totals section only showed WHT, not 5% WVAT (Print controller had it). Added WVAT row. (`Preview.cshtml`)
### Fixed
- **ModernSelect search auto-focus on desktop** — Search input now auto-focuses when dropdown opens on non-touch devices. Touch/mobile still skips focus to avoid on-screen keyboard. (`modern-select.js`)
- **Billing Preview print top gap** — `body.mnav-enabled` 64px padding from modern navbar leaked into print, creating blank space above the statement. Added `body.mnav-enabled { padding-top: 0 !important; }` to print CSS. (`Preview.cshtml`)

## [2026-07-23]
### Changed
- **JobOrder ViewModel cleanup** — Removed deprecated `RequiredTugCount`, `PreferredTugboatId`, `Tugboats` select list from `JobOrderViewModel` and its population in `JobOrderService.PopulateJobOrderViewModelAsync`. These fields were never rendered in the Create/Edit views. (`JobOrderViewModel.cs`, `JobOrderService.cs`)
### Added
- **DispatchTicket DeleteVideo** — Added `DeleteVideo` service method and controller action (mirrors `DeleteImage`), fixing 404 when clicking delete video on EditTicket page. (`DispatchTicketService.cs`, `DispatchTicketController.cs`)
- **DispatchTicket BatchTariffRequest DTO** — Moved inline class to `IBS.DTOs`. (`BatchTariffRequest.cs`)
### Fixed
- **Edit POST data loss** — On validation or business rule failure, the Edit POST action now re-renders the form with ModelState instead of redirecting to Details (which lost all user input). Also correctly provides `ViewData["HasTickets"]` and `ViewData["JobOrderNumber"]` on re-render. (`JobOrderController.cs`)
- **MapToEntity includes JobOrderId** — `MapToEntity` now maps `JobOrderId` from the ViewModel, eliminating the redundant post-map assignment in the Edit POST action. (`JobOrderController.cs`)
- **Details Edit button hidden when Closed** — The "Edit Details" button on the Details page is now only shown when `Status == Open`, matching the server-side guard on the Edit GET action. (`Details.cshtml`)
- **DispatchTicket DeleteVideo 404** — `EditTicket.cshtml` called `DeleteVideo` action that didn't exist on `DispatchTicketController`; added action and service method. (`DispatchTicketController.cs`, `DispatchTicketService.cs`)
- **DispatchTicket DataTables error response** — `GetDispatchTicketLists` catch block returned a redirect (broken JSON expectation); now returns valid JSON error envelope. (`DispatchTicketController.cs`)
- **DispatchTicket missing `[ValidateAntiForgeryToken]`** — `EditTicket` POST action was missing the attribute; added. (`DispatchTicketController.cs`)
- **DispatchTicket missing `[HttpPost]` on `CheckForTariffRate`** — Action accepted both verbs; narrowed to POST to match caller. (`DispatchTicketController.cs`)
- **DispatchTicket `ChangeStatus` excluded `Cancelled`** — Added `Cancelled` to valid target statuses HashSet. (`DispatchTicketController.cs`)
- **DispatchTicket `CustomerId` zero guard** — SetTariff/EditTariff POST now rejects null/zero `CustomerId` before mapping (was silently passing 0 as FK). (`DispatchTicketController.cs`)
- **DispatchTicket `ModelState.IsValid` checks** — All 4 data POST actions now validate ModelState before calling service; Create/EditTicket return View with validation summary, SetTariff/EditTariff collect errors into TempData. (`DispatchTicketController.cs`)
- **DispatchTicket `Preview` CancellationToken default** — Added `= default` for consistency. (`DispatchTicketController.cs`)
- **DispatchTicket Index filter used bare string** — `Index.cshtml` used `'Deleted'` literal instead of `@SD.DispatchTicketStatus.Deleted` constant. (`Index.cshtml`)
- **DispatchTicket EditTariff missing default rates** — `fetchDefaultRates()` was not called on page load (SetTariff had it, EditTariff didn't). (`EditTariff.cshtml`)
- **DispatchTicket dead ViewData** — Removed unused `ViewData["JobOrderId"]` from EditTicket GET. (`DispatchTicketController.cs`)
- **DispatchTicket `PopulateDispatchTicketViewModelAsync` result unassigned** — Create POST error path wasn't using the returned ViewModel (select lists not populated on re-render). (`DispatchTicketController.cs`)

## [2026-07-17]
### Added
- **Posting Periods (Monthly Lock)** — new `MsapPostedPeriod` entity tracks monthly close/open state. Admin UI at `/Admin/PostedPeriod` to close/open months manually. All MSAP write operations (create, edit, delete, post, reverse) are guarded: if the transaction's month is closed, the service returns a failure message. Action buttons in index views are hidden when `isMonthClosed` is true, passed via `[NotMapped]` flag on all 4 core entities (JobOrder, DispatchTicket, Billing, Collection). (`MsapPostedPeriod.cs`, `PostedPeriodController.cs`, `PostedPeriodRepository.cs`, `ApplicationDbContext.cs`, `IUnitOfWork.cs`, `Enum.cs`, 4 service files, 4 controllers, 4 Index views)
### Fixed
- **PostedPeriod audit trail** — Close and Open actions now write to `AuditTrail` table. Added `using IBS.Models;` import. Extracted `username` variable to avoid repeating `User.Identity?.Name` inline. (`PostedPeriodController.cs`)

## [2026-07-17]
### Fixed
- **ModernSelect mobile issues** — replaced time-based focus/click guard with flag to prevent dropdown toggling closed on mobile tap; removed `resize` handler that killed dropdown when mobile keyboard opened; added `stopPropagation` on dropdown click so clicking search/scrollbar doesn't bubble to document close handler; removed auto-focus on search input on open (keyboard no longer pops). Increased auto-pick threshold from 2 to 5 characters. (`modern-select.js`, `modern-ui.css`)

## [2026-07-16]
### Added
- **SuperAdmin module** — new `Areas/SuperAdmin` with direct table editing for JobOrder, DispatchTicket, Billing, Collection. Seed-only role, field-level audit trail, remarks required, no hard deletes. (`SuperAdminService.cs`, `HomeController.cs`, `DataController.cs`, `Data/Index.cshtml`, `Home/Index.cshtml`, `Program.cs`, `DbSeeder.cs`, `UserAccessService.cs`, `_Layout.cshtml`)

### Changed
- Updated all NuGet packages to latest versions across 7 projects: Microsoft.AspNetCore.*/EntityFrameworkCore 10.0.9→10.0.10, EPPlus 8.6.0→8.6.1, Npgsql.EntityFrameworkCore.PostgreSQL 10.0.2→10.0.3, QuestPDF 2026.6.0→2026.7.1, System.Linq.Dynamic.Core 1.7.2→1.7.3, Google.Apis.Drive.v3 1.74.0.4135→1.75.0.4192, Quartz 3.18.1→3.18.2, Serilog.Settings.Configuration 10.0.0→10.0.1, Microsoft.NET.Test.Sdk 18.6.0→18.8.1, coverlet.collector 10.0.1→10.0.2, Microsoft.Playwright.Xunit 1.60.0→1.61.0. Also updated `dotnet-ef` global tool 10.0.3→10.0.10. (`.csproj` files across solution)

### Changed
- DispatchTicket Create/EditTicket synced with ServiceRequest Create — Port, Terminal, COS#, Voyage# are now read-only when derived from a Job Order; input order reordered to Port|Terminal before Vessel|Service to match ServiceRequest layout; added VoyageType auto-set badge for Vessel. (`DispatchTicketController.cs`, `Create.cshtml`, `EditTicket.cshtml`)

## [2026-07-15]
### Added
- Conflict detection on VesselSchedule Create/Edit — checks terminal and tugboat overlap via AJAX before submit, shows inline warnings, user can still save with conflicts. (`IVesselScheduleService.cs`, `VesselScheduleService.cs`, `VesselScheduleController.cs`, `Create.cshtml`, `Edit.cshtml`)
- Tugboat Availability DataTable view on Vessel Schedule Index — lists all tugboats with Available/Busy status, searchable and sortable. Replaces the Berth Occupancy tab. (`VesselScheduleController.cs`, `Index.cshtml`)
- Vessel Schedule Board — new scheduling module separate from existing job-order workflow. `VesselSchedule` entity, Frappe Gantt timeline view, conflict guard, status flow (Tentative → Confirmed → In Progress → Completed / Cancelled), and tugboat assignment via JSON array. No changes to JobOrder/DispatchTicket/Billing flow.
### Fixed
- Conflict alerts on Create/Edit now use inline styled elements (orange background, icon, dismiss button) instead of `.modern-alert` CSS class that had no definition — conflicts are now visible. Also shows a SweetAlert2 warning modal before the inline alert and confirm dialog, so conflicts can't be missed. (`Create.cshtml`, `Edit.cshtml`)
### Changed
- Removed `CustomerId` from VesselSchedule — vessel scheduling is about arrivals and tugboats, not billing. Dropped FK, index, and column via migration; removed Customer field from Create/Edit/Details views, `Customers` list from ViewModel, and `.Include(s => s.Customer)` from repository. (`VesselSchedule.cs`, `VesselScheduleViewModel.cs`, `VesselScheduleController.cs`, `VesselScheduleService.cs`, `VesselScheduleRepository.cs`, `ApplicationDbContext.cs`, `Create.cshtml`, `Edit.cshtml`, `Details.cshtml`, `Index.cshtml`)
- Schedule Entries DataTable now client-side (`serverSide: false`) — `GetScheduleList` returns all data, DataTable handles sorting/paging. (`Index.cshtml`)
- Date filters on Schedule Entries and Tugboat Availability now reactive — pick a date, table reloads without clicking refresh. (`Index.cshtml`, `VesselScheduleController.cs`)
### Fixed
- DataTable `sClass` error caused by `<th>CUSTOMER</th>` lingering in HTML after column was removed — 8 `<th>` vs 7 JS columns. (`Index.cshtml`)

## [2026-07-14]
### Changed
- Maritime Excel reports now use Calibri as the default font across all worksheets. (`MaritimeReportController.cs`)
### Fixed
- Column A (COS#) no longer gets hidden by `FinalizeColumns` when all data rows are empty — keeps the company header visible. (`MaritimeReportController.cs`)

### Added
- Declarative keyboard shortcuts via `modern-hotkeys.js`: add `data-hotkey="c"` to any element for a 'c' keybinding with auto-underlined hotkey letter; `Esc` globally navigates back via `history.back()` (skips when a modal/overlay is open). Applied to Create buttons on JobOrder, Billing, Collection index pages. (`modern-hotkeys.js`, `Index.cshtml` x3, `_Layout.cshtml`)

## [2026-07-13]
### Added
- ModernSelect search auto-select now auto-focuses the next `.js-modern-select` in DOM order, so cascading dropdowns flow naturally (e.g. Port → Terminal on Job Order Create). (`modern-select.js`)
- Job Order Create/Edit customer field converted from client-side AJAX search to SSR modern-select dropdown, matching Vessel/Port/Terminal UX. (`Create.cshtml`, `Edit.cshtml`, UI tests)
- Billing Create/Edit customer and principal fields converted to SSR/dynamic modern-select dropdowns. Customer uses SSR with AJAX detail fetch; principal uses dynamic modern-select populated via `GetPrincipalsByCustomer`. (`BillingController.cs`, `BillingService.cs`, `Create.cshtml`, `Edit.cshtml`, UI tests)
- Billing Create/Edit Job Order search converted from AJAX text-input search to dynamic modern-select populated via `GetBillableJobOrders`. Added `GetBillableJobOrdersAsync` to service; removed dead `SearchJobOrdersAsync`. (`BillingController.cs`, `BillingService.cs`, `Create.cshtml`, `Edit.cshtml`)
- Collection Create customer search converted from AJAX text-input search to SSR modern-select. Removed `SearchCustomers` action and now-unused `IUnitOfWork` dependency from `CollectionController`. (`CollectionController.cs`, `Create.cshtml`, UI tests)

## [2026-07-11]
### Fixed
- Sales Summary (AR Monitoring) report now filters by **billing date** instead of dispatch ticket date — billings posted in the target month are no longer excluded just because the dispatch happened earlier. Column 1 ("BILLING STATEMENT DATE/DISPATCH DATE") shows the **billing date** when available. Sort order also uses billing date when the billing-date filter is active. (`ReportRepository.cs`, `IReportRepository.cs`, `MaritimeReportController.cs`)
### Added
- Total row at the bottom of Sales Summary with SUM formulas for GROSS SALES, BALANCE, NET SALES, and all columns from FOR PNL USE onwards (except text-only DOC/UNDOC/PRINCIPAL). Light cyan background (`#CCFFFF`), dark bold text, thin borders. Freeze pane at row 7 so headers stay visible during scroll. (`MaritimeReportController.cs`)

### Added
- Billing reversal (unpost) feature: new `ReverseBilling` procedure enum + `CanReverseBilling` permission; `ReverseBillingAsync` in `BillingService` resets status → `ForPosting`, reverts dispatch tickets → `ForBilling`, records `UnpostedBy/Date/Remarks`; blocked if billing is linked to a collection; `Reverse` action in `BillingController` + "Reverse (Unpost)" dropdown on Index with remarks prompt. (scope: `Enum.cs`, `UserAccess.cs`, `Billing.cs`, `UserAccessService.cs`, `UserAccessRepository.cs`, `BillingService.cs`, `BillingController.cs`, `Index.cshtml`, `Edit.cshtml`)
### Fixed
- Billing reversal now reopens auto-closed Job Orders when tickets are reverted to `ForBilling`
- Billing reversal success message shows billing number instead of ID
- Edit POST action returns JSON (matching JS expectation) instead of 302 redirect, fixing "An error occurred" alert on every edit
- Edit form auto-fetches tickets when JobOrder is pre-filled but server rendered none (e.g. after reversal)
### Changed
- Restructured User Access pages (Create/Edit) to follow MSAP workflow order: Service Request → Job Order → Dispatch Ticket → Tariff → Billing → Collection; added `- For Port Coordinators` label on SR section; moved Reverse Billing into Billing column with red text + tooltip; removed Treasury/Disbursement stubs; relabeled Maritime Master File section as `References`; fixed `gap-6` to valid `gap-4` for spacing

## [2026-07-10]
### Added
- ServiceRequest delete/restore feature: new `ServiceRequestDeleted` status constant; `[HttpPost] Delete` action in `ServiceRequestController` with inline soft-delete (status → "Service Request Deleted" + audit); "Delete" dropdown item for Draft/Requested statuses with "DELETED" filter button on SR index; `[HttpPost] Restore` action (ServiceRequestDeleted → Requested) with "Restore" dropdown item. (scope: `SD.cs`, `ServiceRequestController.cs`, `Index.cshtml`)

### Changed
- `SearchBillableJobOrdersAsync` now excludes `Deleted`/`ServiceRequestDeleted` tickets from the `.All()` check so deleted tickets don't block Job Orders from appearing in billing dropdown. (scope: `JobOrderRepository.cs`)
- `JobOrderRepository.cs`: All `.Include(DispatchTickets.Where(...))` filters exclude `ServiceRequestDeleted` alongside `Deleted`. (scope: `JobOrderRepository.cs`)
- `JobOrderService.cs`: `SyncRelatedRecordsAsync` and `TryAutoCloseAsync` queries also exclude `ServiceRequestDeleted`. (scope: `JobOrderService.cs`)
- `DispatchTicketRepository.cs`: `GetPagedDispatchTicketsAsync` excludes `ServiceRequestDeleted` from DT listing and total count. (scope: `DispatchTicketRepository.cs`)

### Changed
- Form validation overhaul: replaced scattered `checkValidity()` + `reportValidity()` calls with shared `ModernFormValidator` utility; removed 51 redundant `_ValidationScriptsPartial` includes (scripts already in `_Layout.cshtml`); added `:user-invalid`/`:user-valid` CSS for instant visual feedback (red borders) on invalid fields after user interaction; fixed ModernSelect to sync validation state to the visible trigger (`is-invalid` class on `change`); replaced vague `customerError` span with native `setCustomValidity()` on customer search inputs across JobOrder, Billing, Collection. (scope: `modern-ui.css`, `modern-select.js`, `modern-form-validator.js` (new), `_Layout.cshtml`, 51+ `.cshtml` files)
- DispatchTicket tariff forms now use `ModernFormValidator.validate()` instead of jQuery `.valid()`. (scope: `SetTariff.cshtml`, `EditTariff.cshtml`)
- ServiceRequest image upload now has a clearer custom validation message via `oninvalid`. (scope: `Create.cshtml`)

## [2026-07-09]
### Changed
- Service Request form locking: when a Job Order is selected, JO-derived fields (Customer, Vessel, Port, Terminal, COS#, Voyage#) are locked to read-only via ModernSelect trigger manipulation (`pointer-events: none`, `tabindex` removal, `.disabled` class) instead of HTML `disabled` — avoids the disabled-field-not-submitted problem. Layout regrouped so locked fields (Customer, Port, Terminal) sit together at the top of Service Details. Terminal uses retry-based locking to wait for the port cascade AJAX. (scope: `Create.cshtml`, `Edit.cshtml`, `modern-ui.css`)
- Billing Create/Edit form locking: same approach applied. Create locks Voyage#, COS#, Port, Terminal, Vessel on JO selection; unlocks on legacy toggle or customer change. Edit locks these fields on page load if JO-linked. (scope: `Create.cshtml`, `Edit.cshtml`)

### Added
- Service Request create/edit form now shows a `VoyageType` badge (Local/Foreign) that auto-fills from the selected vessel's `VesselType` field via AJAX. No new DB columns or migration — reads the existing `Vessel.VesselType` ("LOCAL"/"FOREIGN" from legacy data). Removed the `(VesselType)` suffix from the vessel dropdown since the badge now handles that.

### Removed
- Dead `BillAdjust`/`BillDispatch` entity models, their DbSet/Fluent config in `ApplicationDbContext`, and the `msap_bill_adjustments`/`msap_bill_dispatches` tables (new migration). These link tables from the legacy schema were unused by any business code — the dispatch-to-billing linkage and per-ticket pricing are fully covered by `DispatchTicket.BillingId` and the tariff fields on `DispatchTicket`. Also removed the unused `bill_adjust.csv`/`bill_dispatch.csv` from `Imports/` and the truncation statements from `MsapImportController.Reset()`.

### Changed
- Customer and Supplier area pages (12 CSHTML files) converted from old Bootstrap styling to modern-ui.css: Index pages use `modern-layout`, `modern-table`, `ModernTable.*` helpers with status badges and action dropdowns; Create/Edit pages use `modern-card`, `modern-grid`, breadcrumb headers, and `js-modern-select` for selects; Activate/Deactivate pages use styled modern cards; ExportIndex pages use `ModernTable.config/ajax` with `ModernAlert` warnings. (scope: `Areas/User/Views/{Customer,Supplier}/*.cshtml`)
- `.modern-select-search input` gets `background-color: transparent` so it inherits the parent dropdown background instead of browser default — fixes gray bar in dark mode. (scope: `modern-ui.css`)
- `.bg-white` override in modern-ui.css now uses `!important` so it respects `--surface-container-lowest` in dark mode instead of Bootstrap's `#fff`. (scope: `modern-ui.css`)
- `.js-modern-select` hidden via CSS (`display: none`) to prevent native select flash before ModernSelect replaces it on page load. (scope: `modern-ui.css`)

## [2026-07-08]
### Changed
- Modern navbar right zone: consolidated docs link, dark mode toggle, modern UI toggle, and logout into a single user dropdown triggered by the avatar chip. Removed standalone icon buttons and toggle chip from the right zone. (scope: `_ModernNavbar.cshtml`, `modern-navbar.css`, `modern-navbar.js`)

### Removed
- Notification feature: removed all notification-related code including models (Notification, UserNotification, HubConnection), services (INotificationService, NotificationService), repositories (INotificationRepository, NotificationRepository, IHubConnectionRepository, HubConnectionRepository), SignalR hub (NotificationHub), controller (NotificationController), views (Notification/Index.cshtml), JS (notification.js), CSS styles, and all references in layouts, services, tests, and docs. Kept the TempData flash message partial (_Notification.cshtml) as it's unrelated. (scope: ~45 files across IBS.Models, IBS.DataAccess, IBS.Services, IBSWeb, IBS.Tests, Docs)

### Added
- Dark mode: full dark/light theme toggle with `prefers-color-scheme` auto-detection and `localStorage` persistence. Uses Bootstrap 5.3's built-in `[data-bs-theme=dark]` system plus custom `--*` CSS variable overrides for the app's design tokens. Toggle button in both classic and modern navbars. Print output always forced to light theme. (scope: `_Layout.cshtml`, `_ModernNavbar.cshtml`, `modern-ui.css`, `modern-dashboard.css`, `modern-navbar.css`, `site.css`, `modern-dashboard.js`, `MaritimeReport/Index.cshtml`)
- Modern MSAP Dashboard: A high-fidelity, interactive operational dashboard featuring a workload status breakdown, a 6-month financial billing/collection trend chart (via ApexCharts), a relative-time operations activity feed, and pending task alerts (scope: `Index.cshtml`, `modern-dashboard.css`, `modern-dashboard.js`, `HomeController.cs`)
- C# JSON Endpoint: Added a performant `GetDashboardData` AJAX action on `HomeController` utilizing optimized EF Core queries and a time-ago relative formatter (scope: `HomeController.cs`).

### Changed
- Renamed `PopulateServiceRequestViewModelAsync` to `PopulateDispatchTicketViewModelAsync` in `DispatchTicketService` and all callers (scope: `DispatchTicketService.cs`, `DispatchTicketController.cs`, `JobOrderController.cs`)
- Dispatch ticket creation now requires a `jobOrderId` — returns failure if missing (scope: `DispatchTicketService.cs`, `DispatchTicketController.cs`)

### Fixed
- Modern navbar search showing notification badge "0" as a result — added `data-search-ignore` attribute and filter to skip it. (scope: `_ModernNavbar.cshtml`, `modern-navbar.js`)

### Added
- Mobile drawer for modern navbar: hamburger button on mobile (<768px) opens a slide-in drawer with cloned nav links, supporting mega-menu expand/collapse and overlay close (scope: `_ModernNavbar.cshtml`, `modern-navbar.css`, `modern-navbar.js`)

### Changed
- Modern UI Toggle Integration: Rebranded the navigation toggle from "Modern Nav" to "Modern UI" to orchestrate both the mega-menu navigation layout and the new modern dashboard (scope: `_Layout.cshtml`, `_ModernNavbar.cshtml`, `modern-navbar.js`).

### Fixed
- Icon and font flash on page load (FOUT): changed Google Fonts `display=swap` to `display=block` to hide text while fonts load instead of showing fallback text. Affects Material Symbols Outlined icons and Inter/Hanken Grotesk body fonts. (scope: `IBSWeb/Views/Shared/_Layout.cshtml`)

## [2026-07-07]
### Fixed
- "Post Request" dropdown action in Job Order Details sent a GET to a `[HttpPost]` endpoint, silently failing. Replaced with a hidden form POST + confirmation dialog matching other dropdown actions. (scope: `IBSWeb/Areas/User/Views/JobOrder/Details.cshtml`)

### Changed
- View Transition API: removed `view-transition-name` from nav elements (classic-header, modern-header, overlay, QA panel) whose content changes between pages — root cross-fade is smoother than morphing mismatched DOM. Added explicit `::view-transition-old/new(root)` animations with `prefers-reduced-motion` guard. (scope: `site.css`)
- Navbar visibility switching: moved from JS-on-DOMContentLoaded to CSS driven by `body.mnav-enabled`, set inline before navbars render. Eliminates the flash from classic→modern swap during page load and view transitions. (scope: `modern-navbar.css`, `modern-navbar.js`)

## [2026-07-03]
### Added
- Native Cross-Document View Transition API: enabled smooth, native MPA page transitions across full-page loads, keeping layout elements like the headers, overlay, sidebar, and footer persistent and visually static (scope: `site.css`)
- Opt-in modern mega-menu navbar: users can switch to a new premium navigation bar via a "Try Modern Nav" toggle chip in the existing classic navbar; preference persists via localStorage with zero backend changes (scope: `_ModernNavbar.cshtml`, `modern-navbar.css`, `modern-navbar.js`, `_Layout.cshtml`)
- Spotlight search in modern navbar: includes arrow key navigation, search results grouped by category, match highlighting, and a "/" keyboard shortcut to focus search.
- Quick Access sidebar suppression: automatically disables and hides the Quick Access panel and its bolt icon trigger when the modern navbar is active, restoring them when deactivated.

### Changed
- Quick Access sidebar redesigned: aligned CSS with modern-ui.css custom properties, replaced Bootstrap Icons with Material Symbols, removed left-edge toggle strip (kept navbar lightning trigger), merged Most Used + Recent into single sorted list (scope: wwwroot/css/quick-access-sidebar.css, wwwroot/js/quick-access-sidebar.js)

### Removed
- Unused `SD.BillingStatus.Paid` constant ("Paid") — collection workflow now only uses "Collected" as the terminal billing status; "Paid" was never assigned anywhere (scope: IBS.Utility/Constants/SD.cs)

## [2026-07-02]
### Added
- User manual with per-module documentation in `Docs/manual/` (scope: Docs/manual/)
- Rendered docs viewer via `DocsController` — Markdig-based markdown rendering with sidebar navigation (scope: IBSWeb)
- "Manual" nav link in main layout sidebar (scope: IBSWeb)
- `Markdig` NuGet dependency for server-side markdown rendering (scope: IBSWeb)
- `billing_year` column to `msap_billings` to scope billing number uniqueness per year (scope: IBS.Models, IBS.DataAccess, IBS.Services)

### Fixed
- Docs not rendering in Docker/Coolify deployment — `Dockerfile` now copies `Docs/` into `/Docs` in the final image so `DocsController`'s path resolution (`ContentRootPath + "/../Docs/manual"`) resolves correctly (scope: IBSWeb/Dockerfile)

### Changed
- Unique index on `msap_billings` changed from `(NUMBER, company)` to `(billing_year, NUMBER, company)` to prevent year-crossing billing number conflicts
- `GenerateBillingNumber` scoped per year — sequence resets each year
- `BillingService.CreateBillingAsync` and import controller auto-set `Year` from `Date`

### Removed
- Experimental Vessel Planning module (Fleet Control Dashboard) — removed controller, service, DTOs, view, JS, CSS, SignalR hub, nav entry
- Experimental Tugboat Monitoring module (Timeline/Scheduling) — removed controller, service, view, JS, CSS, SignalR hub, nav entry
- Cleared TugboatHub and PlanningHub dependencies from JobOrderController, DispatchTicketController, Program.cs, and tests

### Added
- Test cases with audit trail verification (scope: IBS.Tests)
- Agent code-review to enforce standard controller patterns (scope: .opencode/)

### Changed
- Removed shadow job order ID from migrations (scope: IBS.DataAccess/Migrations)
- Removed redundant model fields for consistent data integrity (scope: IBS.Models)
- Removed unnecessary ModelState checks from controllers (scope: IBSWeb/Areas)
- Formalized Collection controller and architecture (scope: IBSWeb, IBS.Services)
- Formalized Billing controller and architecture (scope: IBSWeb, IBS.Services)
- Formalized Service Request functionalities (scope: IBSWeb, IBS.Services)
- Made MCP server more reliable for context analysis (scope: .opencode/)

### Fixed
- Job Order TempData on edit not firing proper info (scope: IBSWeb/Areas/User/Controllers)
- Job Order controller inconsistencies (scope: IBSWeb/Areas/User/Controllers)
- Billing controller not using services properly (scope: IBSWeb/Areas/User/Controllers)
- Various cross-codebase inconsistencies (scope: multiple)

## [2026-07-01]
### Changed
- Deduplicated customer search into a shared helper (scope: IBS.Utility)
