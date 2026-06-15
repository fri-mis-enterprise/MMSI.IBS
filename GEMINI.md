# MSAP — MMSI Sales and Accounting Program

This workspace is dedicated to **MSAP**, a specialized standalone program for the maritime service industry. All development must adhere to the core MSAP workflow and architectural standards.

## 🏗️ Core MSAP Workflow
Every feature or change must align with the four-stage operational lifecycle:
1. **Job Order**: Service request entry and vessel planning.
2. **Dispatch Ticket**: Service fulfillment and field operations.
3. **Billing**: Service invoicing and tariff application.
4. **Collection**: Financial settlement and payment recording.

## 🛠️ Specialized Tools

### 1. Code Oracle (`search_code_context`)
Deep-dives into MSAP C# methods. Fetches method body plus DTO, Model, and Enum definitions.
- **Example**: `search_code_context(methodName: "CreateJobOrderAsync")`

### 2. Logic Mapper (`trace_workflow`)
Recursively maps the execution path from Controller ⮕ Service ⮕ Repository for the MSAP workflow.
- **Example**: `trace_workflow(methodName: "BillDispatchTickets", filePath: "IBSWeb/Areas/User/Controllers/BillingController.cs")`

### 3. Action Analyst (`analyze_action`)
Deep-dives into a specific MSAP Controller Action, showing dependencies and related DTOs.
- **Example**: `analyze_action(methodName: "Index", filePath: "IBSWeb/Areas/User/Controllers/JobOrderController.cs")`

### 4. Model Inspector (`read_model`)
Provides a concise summary of MSAP Models or DTOs.
- **Example**: `read_model(modelName: "JobOrderViewModel")`

### 5. Data Guardian (`execute_sql`)
Direct access to the MSAP PostgreSQL database. **Prompt on Write** is enforced.
- **Example**: `execute_sql(sql: "SELECT * FROM public.msap_job_orders LIMIT 10")`

### 6. Build Guard (`check_build_status`)
Runs `dotnet build` to ensure MSAP integrity.

## 📂 Project Structure
- **Web UI**: `IBSWeb/` — Focused strictly on MSAP Areas (Job Order, Dispatch, Billing, Collection).
- **Services**: `IBS.Services/` — Business logic for the MSAP operational lifecycle.
- **Data Access**: `IBS.DataAccess/` — Repositories and Unit of Work for MSAP entities.
- **Models/DTOs**: `IBS.Models/` & `IBS.DTOs/` — MSAP-specific entity and data definitions.

## 📜 Development Standards

### 1. Specialized 4-Tier Architecture
Strictly maintain layer separation:
- **Controllers**: Thin wrappers for MSAP request routing and JSON responses.
- **Services**: All MSAP business rules, tariff calculations, and workflow transitions live here.
- **DataAccess**: `IUnitOfWork` based repository access for MSAP tables.
- **Models**: Use `BaseEntity` for all database-mapped models.

### 2. Modern UI Design System
All UI must follow the MSAP Modern UI pattern:
- **Layout**: Use `_Layout.cshtml` with the `modern-layout` class.
- **Tables**: Use `ModernTable` (server-side AJAX) for all MSAP lists.
- **Selects**: Use `ModernSelect` for searchable dropdowns (Vessels, Ports, Customers).
- **Alerts**: Use `ModernAlert` for workflow feedback.

### 3. Workflow Integrity & Audit
- **Mandatory Audit**: Every transition in the MSAP workflow (e.g., Job Order -> Dispatch) **MUST** be logged via `AuditTrail`.
- **State Validation**: Ensure records are in the correct state before transitioning (e.g., only "Pending" tickets can be updated; only "Billed" tickets can be "Collected").
- **SignalR Updates**: Use hubs (`NotificationHub`, `TugboatHub`, `PlanningHub`) for real-time status synchronization across the operational floor.

### 4. Traceability
- Use `trace_workflow` before refactoring to avoid breaking the delicate chain of dependencies from Job Order through to Collection.
