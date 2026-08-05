### AGENTS.md — MMSI-IBS / MSAP

.NET 10 ASP.NET Core MVC & PostgreSQL active dev. MSAP Workflow: Job Order → Dispatch Ticket → Billing → Collection. 

### Quick Reference & Environment

* Build: dotnet build (TreatWarningsAsErrors on 6/8 projects). Only run when touching .cs/.csproj — .cshtml/js/css changes need no build; browser refresh is enough.
* Tests: dotnet test IBS.Tests (xUnit/Moq) | dotnet test IBS.Tests.UI (Playwright)
* Environment: docker compose up (App :5001, DB :5002)
* DB: localhost:5432, mmsi_ibs_dev, user postgres/mis123

### Stack Configuration & Gotchas

* Timestamps: Npgsql.EnableLegacyTimestampBehavior = true | Snake-case via EFCore.NamingConventions
* Serialization: DecimalJsonConverter rounds to 2 decimal places.
* Core Packages: Quartz scheduling (IBS.Services), QuestPDF (IBSWeb).
* Auth: Cookie-based 30-min sliding. [Authorize(Roles = "Admin")] or MSAP [RequireAnyAccess].
* Storage & DB: Auto-migrations via IBS.DataAccess. Local storage at App_Data/LocalStorage -> /local-storage.
* Architecture Hierarchy: IBS.Models → IBS.DataAccess → IBS.Services → IBSWeb. Areas: User (28 controllers), Admin, Identity (Razor Pages). Uses Primary Constructors & UoW/Generic Repository.
* Modern UI: Sole navbar is `_Navbar.cshtml` (partial in `_Layout.cshtml:71`). Powered by `modern-navbar.js` (always-on, no toggle). `modern-dashboard.css` shows modern dashboard unconditionally. Classic Bootstrap dashboard removed.

### Conventions & Agent Constraints

* Ponytail Optimization: Strict compliance with YAGNI/KISS rules. No unrequested abstractions (no single-implementation interfaces, no single-product factories). Code bloat will be heavily penalized.
* Minimal Fixes: Inconsistencies exist in legacy blocks; do minimal, localized fixes. Write the absolute minimum custom code required to pass tests.
* Patterns First: Follow existing implementations in Areas/User/Views/ before writing new code.
* Root-Cause Fixes: Grep all callers of a function before modifying shared logic.
* Constraints: Invariant Philippine time via DateTimeHelper.GetCurrentPhilippineTime(). Workflow state guards mandatory (e.g., "ForTariff" before pricing). Audit trails required on all CUD operations.
* Frontend: Refreshes only for .cshtml/.js/.css (no build needed). Icons via <span class="material-symbols-outlined">. Gotcha: ModernTable.ajax() is POST by default; pass explicit inline type: "GET" if targeting [HttpGet].

### Housekeeping & Workflow Tools

* Persistent Memory: opencode-mem provides cross-session memory. Use the `memory` tool: `memory({ mode: "search", query: "..." })` at session start for relevant context; `memory({ mode: "add", content: "..." })` for durable decisions/facts (bug patterns, "we tried X and it failed", preferences). Scope: project by default, `"scope": "all-projects"` to search across projects. Web UI at http://127.0.0.1:4747. Manual `add`/`search` only — auto-capture is disabled (free-tier provider, no structured output).
* Session State: STATE.md holds cross-session context (current focus, decisions, key file shortcuts, next steps). READ it at session start; UPDATE it before ending a session. Keep it current or it rots.
* Detected Issues: Whenever you READ a file and spot a possible problem (bug, smell, dead code, missing audit trail, copy-paste error, etc.), log it to DETECTED-ISSUES.md (reverse-chronological, `[date] [severity] file:line — description`). Do this even if you fix it later in the same session. Don't fix issues unrelated to the current task — just log them for a future pass.
* Tools: Use search_code_context, trace_workflow, analyze_action, and read_model to map code. Use execute_sql for diagnostics.
* Changelog: Append reverse-chronological logs to CHANGELOG.md. Skip trivial edits.
* Versioning: Found at IBSWeb/Views/Shared/_Layout.cshtml:15. Format: 0.{DeploymentVersion}.{CommitCounts}. Ask before incrementing.
* Git: Summarize changes and ask "Commit?" explicitly before committing.
* Ambiguity: Ask for clarification if prompts or requirements are vague.
