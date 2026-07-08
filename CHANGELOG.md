# Changelog

## [2026-07-08]
### Added
- Dark mode: full dark/light theme toggle with `prefers-color-scheme` auto-detection and `localStorage` persistence. Uses Bootstrap 5.3's built-in `[data-bs-theme=dark]` system plus custom `--*` CSS variable overrides for the app's design tokens. Toggle button in both classic and modern navbars. Print output always forced to light theme. (scope: `_Layout.cshtml`, `_ModernNavbar.cshtml`, `modern-ui.css`, `modern-dashboard.css`, `modern-navbar.css`, `site.css`, `modern-dashboard.js`, `MaritimeReport/Index.cshtml`)
- Modern MSAP Dashboard: A high-fidelity, interactive operational dashboard featuring a workload status breakdown, a 6-month financial billing/collection trend chart (via ApexCharts), a relative-time operations activity feed, and pending task alerts (scope: `Index.cshtml`, `modern-dashboard.css`, `modern-dashboard.js`, `HomeController.cs`)
- C# JSON Endpoint: Added a performant `GetDashboardData` AJAX action on `HomeController` utilizing optimized EF Core queries and a time-ago relative formatter (scope: `HomeController.cs`).

### Changed
- Renamed `PopulateServiceRequestViewModelAsync` to `PopulateDispatchTicketViewModelAsync` in `DispatchTicketService` and all callers (scope: `DispatchTicketService.cs`, `DispatchTicketController.cs`, `JobOrderController.cs`)
- Dispatch ticket creation now requires a `jobOrderId` — returns failure if missing (scope: `DispatchTicketService.cs`, `DispatchTicketController.cs`)

### Fixed
- Modern navbar search showing notification badge "0" as a result — added `data-search-ignore` attribute and filter to skip it. (scope: `_ModernNavbar.cshtml`, `modern-navbar.js`)

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

## [2026-07-02]
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
