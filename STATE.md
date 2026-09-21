### STATE.md — Cross-Session State

Read this FIRST at session start. Update it at the END of every session so the next starts here. Keep entries short — bullet points, not essays. If it's in AGENTS.md, don't duplicate it here. Full history lives in CHANGELOG.md (don't duplicate logs here).

### Entry Template — use for every new/updated entry
```
- **<Feature> (<YYYY-MM-DD>[, committed <hash>]):** what changed + why. **Open:** unfinished pieces / pending checks.
```
- Current Focus: one bullet per active work item; demote to Session Log when done.
- Session Log: one line per session, newest first. Format: `YYYY-MM-DD — <Feature> — <outcome>`. No lines copied from CHANGELOG.
- Recent Decisions / Gotchas / Open Questions: only what a future session can't find elsewhere.

### Current Focus
- **Service Request removal (2026-09-21, uncommitted):** direct Dispatch Ticket flow restored; runtime code, permissions, documentation, and assets removed. `RemoveServiceRequest` maps `Draft`/`Requested` to `For Tariff` and `Service Request Deleted` to `Deleted`, then drops obsolete user-access columns. Select-list loading lives directly in `DispatchTicketRepository`; no form-only repository remains. Final build and migration-script checks pass. **Open:** authenticated browser smoke check blocked because no local browser/runtime is available.
- **Detected-issues cleanup (2026-09-21, committed 2cbfccd):** prior bug fixes and Accept Request wording preserved in git before Service Request removal. Build passed with 0 warnings/errors. Legacy `Draft`/`Requested` tickets are excluded from the registry while Job Order Detail can promote them to `For Tariff`. **Open:** authenticated browser smoke check of retained fixes.
- **SuperAdmin modernization + paging (2026-08-06, committed):** modern UI (grid cards, real `status-*` badges + new `.status-error`, styled notices, no phantom `scrollX` header row); `GetDataAsync` pages/sorts/filters in SQL via new `IRepository<T>.GetPagedAsync` (was loading whole tables in memory).
- **Billing BAF (2026-08-05):** per-ticket BAF + switchable TYPE (Per Move/Per Hour), live rate edits on `input`, `BafRates[ticketId]` write-back with audit trail. Gotcha: mutate `data-*` via `.data()` not `.attr()` (jQuery caches on first read). **Open:** Edit.cshtml has no TYPE switch (Create only, intentional); Playwright/browser check pending.
- **View + conformance audits at zero (2026-08-04):** all remaining findings are documented deviations (Billing/UserAccess V2) or false positives (C3 global FallbackPolicy, S1 infra services, etc). Build green 0/0.
- **14 master-file Create/Edit screens on `XViewModel : XEntity` (2026-08-04):** (9 MSAP + 5 MasterFile). Controllers build VM in GET, bind in POST; [NotMapped] select-lists populated after ctor. Activate/Deactivate keep entity @model.

### Recent Decisions
- Closed-period guards return `ServiceResult` from services; MVC controllers turn failures into `TempData["error"]`, rendered by `_Notification.cshtml` via `ModernAlert.error`. Do not introduce UI dependencies into services.
- Tours: `data-tour-step="N"` + `window.IBS_TOUR_STEPS` in `@section Scripts`; `data-page-header` on `<h1>` auto-injects (?) help. Editable-only fields tag steps only in their editable branches (tutorial.js auto-skips missing).
- tutorial.js autoAdvance fires on `change/input` of contained inputs, not raw `click` (was blocking native date/time pickers).
- Transport-aware denial: AJAX gets JSON `{success:false,message}`; full-page navs redirect to same-origin referer with TempData["error"] (fallback Home/Index).

### Key Files & Shortcuts
- IBSWeb/wwwroot/js/tutorial.js — tour engine.
- Views refs: JobOrder/Create (steps 1-8), DispatchTicket/Create (ticket entry), Billing/Create (1-7; template for multi-select + undoc-toggle, mirrors Collection/Create).

### Open Questions / Next Steps
- Decide if the 9 audit findings must read zero → Billing + UserAccess V2 conversion only actionable items left.
- Gate MSAP References menu links per-module (`ManageMaritimeMasterFile` etc.) — deferred.
- Finish tutorial.js rollout on remaining core MSAP pages; shared partial if step patterns repeat.

### Gotchas (session-specific)
- Tutorial.js z-index: interactive element via `.tour-interactive-active` (z-index 10002) must stay above overlay/backdrop; popover flips above for selects.

### Session Log (recent only — full history in CHANGELOG.md)
- 2026-09-21 — Closed-period alerts — Job Order create/edit service failures now show the shared ModernAlert rather than the validation summary; `dotnet build --no-restore` was blocked only by the active IBSWeb/debugger DLL locks.
- 2026-09-21 — Detected-issues review — restored legacy Draft/Requested registry exclusions and marked the issue resolved; Job Order Detail retains the edit-and-promote path.
- 2026-09-21 — Removed Service Request module and restored direct Dispatch Ticket entry; final build and migration script passed. Browser smoke check blocked because no local browser/runtime is available.
- 2026-09-21 — User authorized committing the existing bug fixes before a separate removal; build passed with 0 warnings/errors. Removal scope clarification requested.
- 2026-09-19 — Renamed user-facing Post Request wording to Accept Request, including confirmation, permission labels, success/error messages and new request audit entries; internal routes/permission identifiers unchanged.
- 2026-09-19 — Detected-issues backlog resolved; Razor compilation verified. Temporary checks remain outside the repo at C:/Users/MIS2/AppData/Local/Temp/msap-issues-check-20260919 (run `dotnet run --project <folder>/Check.csproj --no-restore` and `node <folder>/check-view.cjs`); no test projects added to the solution.
- 2026-09-19 — Required Service Request entry flow implemented; build passed with 0 warnings/errors and git diff --check passed. No permission migration or version increment.
- 2026-08-06 — SuperAdmin modern UI + server-side paging (GetPagedAsync). DELETED filters → modern-btn-error. Build green.
- 2026-08-06 — ServiceRequest Create/Edit JO filter (excludes Open JOs w/ ForPosting billing). Committed d56e904.
- 2026-08-05 — Billing Create BAF switchable TYPE + live rates; tickets TOTAL live. Build 0/0. Broken pre-session: BillingService/Legacy/Dispatcher/TaxAnalysisTests dtor.
- 2026-08-05 — Dispatch hours floor 1h (Create+Edit+legacy SR). Test `CreateDispatchTicketAsync_MinimumHoursIsOne`; 5/5 pass. ComputeTotalHours in MsapImportController NOT updated (DETECTED-ISSUES).
- 2026-08-04 — Billing per-ticket BAF, `BafRates` write-back + audit, AP Other Tug removed. Build 0/0.
- 2026-08-04 — 14-VM conversion complete; view audit 38→9; ARCHITECTURE §5.2 V4 documents Billing/UserAccess deviation.
- 2026-08-04 — MCP tools: `audit_views` (V1-V7 baseline 57→9); `audit_conformance` (6/8/14); recursive `trace_workflow`; `execute_sql` cap 500.
- 2026-08-03 — Transport-aware denial + navbar gating + linear action visibility; AccessControlService cleanup; Billing/Create tour.
- **Sales Report simplification (2026-09-16):** aligned MaritimeReportController SalesSummary fixed columns to Sales Report.xlsx and strips formulas from exported snapshot. **Open:** replace remaining dynamic/lower-summary construction with direct value writes; fix unbilled dispatch inclusion.
- **Sales Report IOC classification (2026-09-16):** uses direct DispatchTicket.Port and exact `INSULAR` match, matching OldVFPLogic.txt. **Open:** verify generated workbook against live records.
- **Sales Report non-IOC cleanup (2026-09-16):** monthly query includes unbilled dispatches and lower summary writes values directly. **Open:** obtain and review official IOC terminal list; verify generated workbook.
