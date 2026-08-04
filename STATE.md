### STATE.md — Cross-Session State

Read this FIRST at session start. Update it at the END of every session so the next session starts where this one left off. Keep entries short — bullet points, not essays. If it's in AGENTS.md, don't duplicate it here.

### Current Focus
- MCP `audit_conformance` tool added (batch-scans controllers/services vs ARCHITECTURE §4/AGENTS rules: C2 primary ctor, C3 access control, S1 IUnitOfWork-only, S2 no ApplicationDbContext, S3 try/catch on mutations, S4 AuditTrail on CUD). Also `trace_workflow` now truly recursive (depth-bounded, resolves member→Service/Repository file), `execute_sql` capped at 500 rows. Rebuild: `npm run build` in mcp-server. Restart opencode to load the new tool.
- Access denial is transport-aware: AJAX gets `{success:false,message}` JSON, full-page navigations redirect back to the same-origin referer with TempData["error"] (fallback Home/Index). Create entry buttons gated (JobOrder Details ADD TICKET, Billing/JobOrder/SR Index CREATE). Navbar hides MSAP workflow links without matching procedures (Job Orders/Dispatch Tickets/Billing/Collection/MMSI Reports).

### Recent Decisions
- Tour steps: mark containers/inputs with `data-tour-step="N"`, config in `@section Scripts` via `window.IBS_TOUR_STEPS`. Add `data-page-header` to the `<h1>` to auto-inject the (?) help button.
- Editable-only fields (COS, Voyage, Customer, Port, Terminal, Vessel when JO-locked) get data-tour-step only in their editable branches; tutorial.js auto-skips missing step elements (polls 1s then skips).
- Removed "(optional for admin)" text from DispatchTicket image label; keep tour text free of role-specific caveats.
- Fixed tutorial.js autoAdvance: container divs (e.g. date/time rows) previously advanced on raw `click`, blocking native date/time picker. Now advances on `change/input` of contained inputs (tutorial.js ~line 320).

### Key Files & Shortcuts
- IBSWeb/wwwroot/js/tutorial.js — the tour engine (spotlight, popover, interactive unlock, auto-advance).
- IBSWeb/Areas/User/Views/JobOrder/Create.cshtml — reference implementation of tour (steps 1-8).
- IBSWeb/Areas/User/Views/DispatchTicket/Create.cshtml — tour steps 1-15 implemented.
- IBSWeb/Areas/User/Views/Billing/Create.cshtml — tour steps 1-7; closest template for multi-select + undoc-toggle pages (Collection/Create mirrors it).

### Open Questions / Next Steps
- Gate MSAP References menu links per-module (`ManageMaritimeMasterFile` etc.) — deliberately deferred.
- Continue tutorial.js rollout on remaining core MSAP pages.
- Consider a shared partial for IBS_TOUR_STEPS if step patterns repeat.

### Gotchas (session-specific)
- Tutorial.js z-index: interactive element lifted via `.tour-interactive-active` (z-index 10002) — must stay above overlay/backdrop. Popover flips above for selects.

### Session Log
- 2026-08-04 — MCP audit tooling — new `audit_conformance` tool batch-scans controllers/services against ARCHITECTURE.md §4 / AGENTS.md rules (C2/C3/S1/S2/S3/S4). Fixed `trace_workflow` to be genuinely recursive (member→file resolution, depth cap 3, cycle dedupe) and capped `execute_sql` at 500 rows. Audit baseline (2026-08-04): 69 controller findings (68 C3, 1 C2), 14 service findings (7 S4, 4 S1, 2 S3, 1 S2). Notable: DispatchTicket has 8 access-control-gap actions (GetVesselVoyageType, ChangeTerminal, GetDispatchTicketList(s), CheckForTariffRate, DeleteImage/Video, SearchCustomers); VesselScheduleService + TariffRateService + SuperAdminService + BillingService(DeleteBillingAsync) + DispatchTicketService(DeleteImage/Video) mutate without audit trails; UserAccessService references ApplicationDbContext.
- 2026-08-03 — Contextual denial redirect + navbar gating — full-page denials now redirect back to the same-origin referer (fallback Home/Index). Navbar MSAP core links (JobOrders/DispatchTickets/Billing/Collection/MMSI Reports) hidden without matching procedures via `ModernHasAnyAccess` helper; MSAP References deliberately untouched. Build green.
- 2026-08-03 — Transport-aware denial + Create button gating — RequireAccess attributes now return JSON for AJAX and redirect full-page navs back to referer with TempData["error"] (base class `RequireAccessBaseAttribute` dedups the shared claim/deny logic). Gated ADD TICKET / CREATE BILLING / CREATE JOB ORDER / CREATE REQUEST entry buttons.
- 2026-08-03 — Action visibility gating — DispatchTicket Index dropdown + Preview buttons, JobOrder Index/Details (header edit, batch toolbar/checkboxes, ticket dropdown, SR post/edit), Billing Index (Post/Edit/Delete), ServiceRequest Index (Edit/Delete/Restore) now hidden without the matching procedure. `ViewBag.Can*` pattern in controllers; also added missing `confirmApprove()` JS in DispatchTicket Index. JobOrderControllerTests ctor updated for new IAccessControlService param.
- 2026-08-03 — AccessControlService cleanup — removed 12 dead members (`HasAllAccessAsync`, `GetAccessMapAsync`, 10 unused extensions); only HasAccessAsync/HasAnyAccessAsync + HasServiceRequestAccessAsync/HasMsapImportAccessAsync/HasMaritimeReportAccessAsync remain. Build green.
- 2026-08-03 — RequireAccess refactor — both attributes always return `Json(new { success = false, message })` at HTTP 200; dropped redirectController/Action/Area params + unused 5-arg RequireAnyAccess ctor; removed TempData["Denied"] from _Notification.cshtml; updated 15 DispatchTicketController call sites. Resolves the "formalize one consistent success/error response shape" pending item. Trade-off: non-AJAX nav to a denied action now renders raw JSON (global FallbackPolicy handles unauthenticated).
- 2026-08-03 — tutorial.js on DispatchTicket/Create — added data-tour-step (1-15) + data-page-header + IBS_TOUR_STEPS config; fixed autoAdvance click bug for date/time rows; removed admin caveat text. AGENTS.md noted cshtml/js/css needs no build (refresh only).
- 2026-08-03 — DispatchTicket/Create — trimmed tour to 8 steps (moved step 2 onward), removed `sticky top-24` from Timeline card (trapped tour z-index under stacking context). tutorial.js: 250ms debounce guard for double-advance (radio fires input+change).
- 2026-08-03 — DispatchTicket/SetTariff — tour steps 1-8; step 7 (AP Other Tugs) auto-skips when `Model.IsTugboatCompanyOwned` (inside @if); removed error styling on Outsourced header + ApOtherTugs input.
- 2026-08-03 — Access-control AJAX fix — RequireAccessAttribute now returns 403 JSON for `X-Requested-With: XMLHttpRequest` (not just Content-Type json); added XHR header to form-encoded fetches in JobOrder/Details (Approve/Disapprove/Delete Tariff), DispatchTicket/Index, ServiceRequest/Index. Pending: formalize one consistent success/error response shape for all AJAX actions (log to CHANGELOG).
- 2026-08-03 — Billing/Create — tour steps 1-7 (Customer, JO/Legacy, Tickets autoAdvance:false, Billing # + DOCUMENTED badge required:false, VAT/WHT, Port/Terminal/Vessel, Submit). Added "Edit Tariff" action to JobOrder/Details "For Approval" dropdown.
- 2026-08-03 — Collection/Create — tour steps 1-6 (Customer, Collection # + UNDOCUMENTED badge required:false, Billing Settlement autoAdvance:false, Payment Summary, Deposit & Check, Save).
