import { Server } from "@modelcontextprotocol/sdk/server/index.js";
import { StdioServerTransport } from "@modelcontextprotocol/sdk/server/stdio.js";
import {
  CallToolRequestSchema,
  ListToolsRequestSchema,
} from "@modelcontextprotocol/sdk/types.js";
import { getDbPool } from "./utils/db-client.js";
import { runBuild, parseBuildErrors } from "./tools/build-guard.js";
import { findMethodInFile, extractReferencedTypes, findTypeDefinition, traceMethodCalls } from "./utils/dotnet-parser.js";
import path from "path";
import fs from "fs";

const PROJECT_ROOT = "/home/roi/RiderProjects/MMSI.IBS";

const server = new Server(
  {
    name: "mmsi-ibs-assistant",
    version: "1.0.0",
  },
  {
    capabilities: {
      tools: {},
    },
  }
);

server.setRequestHandler(ListToolsRequestSchema, async () => {
  return {
    tools: [
      {
        name: "execute_sql",
        description: "Run SQL queries against the MMSI-IBS database. Prompt on Write is enforced by the client.",
        inputSchema: {
          type: "object",
          properties: {
            sql: { type: "string", description: "The SQL query to execute." },
          },
          required: ["sql"],
        },
      },
      {
        name: "check_build_status",
        description: "Runs 'dotnet build' and returns structured errors/warnings.",
        inputSchema: {
          type: "object",
          properties: {},
        },
      },
      {
        name: "search_code_context",
        description: "Search for a C# method and get its body plus all related DTO/Model definitions.",
        inputSchema: {
          type: "object",
          properties: {
            methodName: { type: "string" },
            filePath: { type: "string", description: "Optional file path to narrow search." },
          },
          required: ["methodName"],
        },
      },
      {
        name: "trace_workflow",
        description: "Recursively trace service and repository calls for a given code block or method.",
        inputSchema: {
          type: "object",
          properties: {
            methodName: { type: "string" },
            filePath: { type: "string" },
          },
          required: ["methodName", "filePath"],
        },
      },
    ],
  };
});

server.setRequestHandler(CallToolRequestSchema, async (request) => {
  const { name, arguments: args } = request.params;

  try {
    if (name === "execute_sql") {
      const sql = args?.sql as string;
      const pool = await getDbPool(PROJECT_ROOT);
      const result = await pool.query(sql);
      return {
        content: [{ type: "text", text: JSON.stringify(result.rows, null, 2) }],
      };
    }

    if (name === "check_build_status") {
      const buildResult = await runBuild(PROJECT_ROOT);
      const parsed = parseBuildErrors(buildResult.output);
      return {
        content: [{ type: "text", text: JSON.stringify({ success: buildResult.success, ...parsed }, null, 2) }],
      };
    }

    if (name === "search_code_context") {
      const methodName = args?.methodName as string;
      const filePath = args?.filePath as string;
      
      let methodBody = null;
      let foundPath = filePath;

      if (filePath) {
        methodBody = await findMethodInFile(path.join(PROJECT_ROOT, filePath), methodName);
      } else {
        // Search in Controllers and Services by default
        const searchDirs = ['IBSWeb', 'IBS.Services'];
        for (const dir of searchDirs) {
          const { glob } = await import('glob');
          const files = await glob(path.join(PROJECT_ROOT, dir, '**', '*.cs').replace(/\\/g, '/'));
          for (const file of files) {
            methodBody = await findMethodInFile(file, methodName);
            if (methodBody) {
              foundPath = path.relative(PROJECT_ROOT, file);
              break;
            }
          }
          if (methodBody) break;
        }
      }

      if (!methodBody) return { content: [{ type: "text", text: "Method not found." }] };

      const types = extractReferencedTypes(methodBody);
      const definitions: Record<string, string> = {};
      for (const type of types) {
        const def = await findTypeDefinition(PROJECT_ROOT, type);
        if (def) definitions[type] = def;
      }

      return {
        content: [{ type: "text", text: JSON.stringify({ path: foundPath, method: methodBody, types: definitions }, null, 2) }],
      };
    }

    if (name === "trace_workflow") {
      const methodName = args?.methodName as string;
      const filePath = args?.filePath as string;
      const fullPath = path.join(PROJECT_ROOT, filePath);

      const methodBody = await findMethodInFile(fullPath, methodName);
      if (!methodBody) return { content: [{ type: "text", text: "Method not found." }] };

      const trace = await traceMethodCalls(PROJECT_ROOT, methodBody);
      return {
        content: [{ type: "text", text: JSON.stringify(trace, null, 2) }],
      };
    }

    throw new Error(`Tool not found: ${name}`);
  } catch (error: any) {
    return {
      content: [{ type: "text", text: `Error: ${error.message}` }],
      isError: true,
    };
  }
});

const transport = new StdioServerTransport();
await server.connect(transport);
