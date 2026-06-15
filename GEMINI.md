# MMSI-IBS Gemini Workspace

This workspace is powered by a custom MCP server designed to handle the complexity of the MMSI-IBS N-Tier architecture and MSAP modules.

## 🛠️ Specialized Tools

### 1. Code Oracle (`search_code_context`)
Deep-dives into C# methods. It fetches the method body plus definitions of every DTO, Model, and Enum referenced.
- **Example**: `search_code_context(methodName: "Create")`

### 2. Logic Mapper (`trace_workflow`)
Recursively maps the execution path from Controller ⮕ Service ⮕ Repository.
- **Example**: `trace_workflow(methodName: "PostCheckVoucher", filePath: "IBSWeb/Areas/User/Controllers/CheckVoucherController.cs")`

### 3. Action Analyst (`analyze_action`)
Deep-dives into a specific Controller Action, showing its dependencies and related DTOs in one view.
- **Example**: `analyze_action(methodName: "Index", filePath: "IBSWeb/Areas/User/Controllers/JobOrderController.cs")`

### 4. Model Inspector (`read_model`)
Provides a concise summary of a Model or DTO's properties and types.
- **Example**: `read_model(modelName: "JobOrderDto")`

### 5. Data Guardian (`execute_sql`)
Direct access to the PostgreSQL database. **Prompt on Write** is enforced.
- **Example**: `execute_sql(sql: "SELECT * FROM public.customer LIMIT 10")`

### 6. Build Guard (`check_build_status`)
Runs `dotnet build` and returns structured errors/warnings.

### 7. CSV Explorer (`list_csv_files` & `query_csv`)
Lists and queries legacy data or export logs stored in CSV format within the `Imports/` or `Exported/` directories.

## 📂 Project Structure
- **Web UI**: `IBSWeb/` (Areas: Admin, User, Identity)
- **Services**: `IBS.Services/` (Business Logic & Audit)
- **Data Access**: `IBS.DataAccess/` (Repositories & DbContext)
- **Models/DTOs**: `IBS.Models/` & `IBS.DTOs/`
- **Legacy/Static Data**: `Imports/` & `Exported/`

## 📜 Development Guidelines
- **Architecture**: Strictly follow the 4-Tier pattern defined in `MSAP_ARCHITECTURAL_GUIDE.md`.
- **UI Standard**: Adhere to the Modern UI design system using `modern-layout`, `ModernTable`, and `ModernSelect` components.
- **Validation**: Always verify changes with `check_build_status`.
- **Audit**: Every state-changing operation must be logged via the Audit Trail system.
- **Traceability**: Use `trace_workflow` before refactoring to avoid breaking downstream dependencies.
