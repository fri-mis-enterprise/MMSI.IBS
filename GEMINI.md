# MMSI-IBS Gemini CLI Guide

This workspace is equipped with specialized Model Context Protocol (MCP) servers to enhance your development workflow.

## Available MCP Servers

### 1. Database Peeker (`mcp-database-peeker`)
Used for inspecting the PostgreSQL database directly. It automatically detects connection strings from `IBSWeb/appsettings.Development.json`.

- **Tools**: `list_tables`, `describe_table`, `get_table_sample`, `execute_query`.
- **Database Switching**: Use the optional `database` parameter to switch between databases.
  - **MMSI IBS**: `database: "mmsi_ibs_dev"` (Default)
  - **IBS (Main)**: `database: "ibs_dev"`
- **Example**: `list_tables(database: "ibs_dev")`

### 2. .NET Analyzer (`mcp-dotnet-analyzer`)
Provides project-wide diagnostics and build errors.

- **Tools**: `analyze_file`, `analyze_project`.
- **Usage**: Run `analyze_project` to see all current compilation errors and warnings in the solution.

### 3. Function Reader (`mcp-function-reader`)
Reads specific C# functions/methods with their dependencies, avoiding the need to read entire large files.

- **Tools**: `read_function`, `find_type`, `list_project_files`.
- **Configuration**: Ensure `PROJECT_ROOT` in your `.qwen/settings.json` points to `C:\Users\MIS2\Desktop\MMSI-IBS`.
- **Example**: `read_function(functionName: "Create", filePath: "IBSWeb/Areas/User/Controllers/JobOrderController.cs")`

### 4. CSV Reader (`mcp-csv-reader`)
Efficiently reads and analyzes legacy CSV data in the `Imports` directory.

- **Tools**: `list_csv_files`, `get_csv_headers`, `read_csv_rows`, `search_csv`, `analyze_csv_integrity`.
- **Strictness**: Automatically handles empty fields as `null` when `strict: true`.
- **Search**: Use `search_csv` to find specific records (supports regex).
- **FK Analysis**: Use `analyze_csv_integrity` to verify relationships between CSVs (e.g., check if all `CUSTNO` in `billing.csv` exist in `customer.csv`).
- **Example**: `search_csv(fileName: "customer.csv", searchTerm: "INC")`

## Configuration Guide

To ensure all tools work correctly in this workspace, verify your `.gemini/settings.json` (or global equivalent) includes these servers:

```json
{
  "mcpServers": {
    "database-peeker": {
      "command": "node",
      "args": ["C:\\MCP\\mcp-database-peeker\\index.js"],
      "cwd": "C:\\Users\\MIS2\\Desktop\\MMSI-IBS"
    },
    "dotnet-analyzer": {
      "command": "node",
      "args": ["C:\\MCP\\mcp-dotnet-analyzer\\index.js"],
      "cwd": "C:\\Users\\MIS2\\Desktop\\MMSI-IBS"
    },
    "function-reader": {
      "command": "node",
      "args": ["C:\\MCP\\mcp-function-reader\\index.js"],
      "cwd": "C:\\MCP\\mcp-function-reader",
      "env": {
        "PROJECT_ROOT": "C:\\Users\\MIS2\\Desktop\\MMSI-IBS"
      }
    },
    "csv-reader": {
      "command": "node",
      "args": ["C:\\MCP\\mcp-csv-reader\\index.js"],
      "cwd": "C:\\Users\\MIS2\\Desktop\\MMSI-IBS",
      "env": {
        "IMPORTS_DIR": "C:\\Users\\MIS2\\Desktop\\MMSI-IBS\\Imports"
      }
    }
  }
}
```

## Conventions
- **Database**: Always specify the `database` name when working on main IBS modules vs MMSI-specific modules.
- **Diagnostics**: Run `analyze_file` after modifying complex controllers to ensure no regressions.
