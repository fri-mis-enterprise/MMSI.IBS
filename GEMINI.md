# MMSI-IBS Gemini Workspace

This workspace is powered by a custom MCP server designed to handle the complexity of the MMSI-IBS N-Tier architecture.

## 🛠️ Specialized Tools

### 1. Code Oracle (`search_code_context`)
Deep-dives into C# methods. It doesn't just show the code; it fetches the definitions of every DTO, Model, and Enum referenced in that method.
- **Use for**: Understanding how a specific operation works without jumping between files.
- **Example**: `search_code_context(methodName: "Create")`

### 2. Logic Mapper (`trace_workflow`)
Maps the execution path across layers. It follows the chain from Controller -> Service -> Repository.
- **Use for**: Tracing complex business logic that spans multiple projects and files.
- **Example**: `trace_workflow(methodName: "PostCheckVoucher", filePath: "IBSWeb/Areas/User/Controllers/CheckVoucherController.cs")`

### 3. Data Guardian (`execute_sql`)
Direct access to the PostgreSQL database (`mmsi_ibs_dev`). 
- **Safety**: Automatically detects connection strings. **Prompt on Write** is enforced for any data modification.
- **Use for**: Verifying data state, checking constraints, or performing safe data fixes.
- **Example**: `execute_sql(sql: "SELECT * FROM public.customer LIMIT 10")`

### 4. Build Guard (`check_build_status`)
Integrates the .NET build system.
- **Use for**: Checking for compilation errors and warnings after making changes.
- **Example**: `check_build_status()`

## 📂 Project Structure
- **Web UI**: `IBSWeb/`
- **Services**: `IBS.Services/`
- **Data Access**: `IBS.DataAccess/`
- **Models/DTOs**: `IBS.Models/` & `IBS.DTOs/`
- **MCP Server**: `mcp-server/`

## 📜 Development Guidelines
- Always verify your changes with `check_build_status`.
- Use `trace_workflow` before refactoring core services to understand the impact on other layers.
- Database connection settings are managed in `IBSWeb/appsettings.Development.json`.
