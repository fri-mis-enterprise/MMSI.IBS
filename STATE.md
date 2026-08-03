### STATE.md — Cross-Session State

Read this FIRST at session start. Update it at the END of every session so the next session starts where this one left off. Keep entries short — bullet points, not essays. If it's in AGENTS.md, don't duplicate it here.

### Current Focus
- Guided tour (tutorial.js) rollout across MSAP pages. Done: JobOrder/Create, DispatchTicket/Create, DispatchTicket/SetTariff, Billing/Create, Collection/Create. Next: EditTicket, JobOrder/Details, etc.

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
- Continue tutorial.js rollout on remaining core MSAP pages.
- Consider a shared partial for IBS_TOUR_STEPS if step patterns repeat.

### Gotchas (session-specific)
- Tutorial.js z-index: interactive element lifted via `.tour-interactive-active` (z-index 10002) — must stay above overlay/backdrop. Popover flips above for selects.

### Session Log
- 2026-08-03 — tutorial.js on DispatchTicket/Create — added data-tour-step (1-15) + data-page-header + IBS_TOUR_STEPS config; fixed autoAdvance click bug for date/time rows; removed admin caveat text. AGENTS.md noted cshtml/js/css needs no build (refresh only).
- 2026-08-03 — DispatchTicket/Create — trimmed tour to 8 steps (moved step 2 onward), removed `sticky top-24` from Timeline card (trapped tour z-index under stacking context). tutorial.js: 250ms debounce guard for double-advance (radio fires input+change).
- 2026-08-03 — DispatchTicket/SetTariff — tour steps 1-8; step 7 (AP Other Tugs) auto-skips when `Model.IsTugboatCompanyOwned` (inside @if); removed error styling on Outsourced header + ApOtherTugs input.
- 2026-08-03 — Access-control AJAX fix — RequireAccessAttribute now returns 403 JSON for `X-Requested-With: XMLHttpRequest` (not just Content-Type json); added XHR header to form-encoded fetches in JobOrder/Details (Approve/Disapprove/Delete Tariff), DispatchTicket/Index, ServiceRequest/Index. Pending: formalize one consistent success/error response shape for all AJAX actions (log to CHANGELOG).
- 2026-08-03 — Billing/Create — tour steps 1-7 (Customer, JO/Legacy, Tickets autoAdvance:false, Billing # + DOCUMENTED badge required:false, VAT/WHT, Port/Terminal/Vessel, Submit). Added "Edit Tariff" action to JobOrder/Details "For Approval" dropdown.
- 2026-08-03 — Collection/Create — tour steps 1-6 (Customer, Collection # + UNDOCUMENTED badge required:false, Billing Settlement autoAdvance:false, Payment Summary, Deposit & Check, Save).
