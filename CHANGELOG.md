# Changelog

## [2026-07-02]
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
