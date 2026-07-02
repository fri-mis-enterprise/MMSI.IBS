export function formatSqlResult(rows) {
    if (!rows || rows.length === 0)
        return "No results found.";
    const keys = Object.keys(rows[0]);
    const header = `| ${keys.join(" | ")} |`;
    const separator = `| ${keys.map(() => "---").join(" | ")} |`;
    const body = rows
        .map((row) => `| ${keys.map((key) => row[key]).join(" | ")} |`)
        .join("\n");
    return `### SQL Query Results\n\n${header}\n${separator}\n${body}`;
}
export function formatBuildStatus(success, errors, warnings) {
    let output = `### Build Status: ${success ? "✅ Success" : "❌ Failed"}\n\n`;
    if (errors.length > 0) {
        output += `#### 🛑 Errors (${errors.length})\n` + errors.map((e) => `- ${e}`).join("\n") + "\n\n";
    }
    if (warnings.length > 0) {
        output += `#### ⚠️ Warnings (${warnings.length})\n` + warnings.map((w) => `- ${w}`).join("\n") + "\n\n";
    }
    return output;
}
export function formatModel(modelName, properties) {
    let output = `### Model: ${modelName}\n\n`;
    output += `| Property | Type | Attributes |\n| --- | --- | --- |\n`;
    output += properties
        .map((p) => `| ${p.name} | ${p.type} | ${p.attributes.join(", ")} |`)
        .join("\n");
    return output;
}
export function formatActionAnalysis(analysis) {
    let output = `### Action Analysis: ${analysis.methodName}\n\n`;
    output += `**File Path:** \`${analysis.filePath}\`\n\n`;
    if (analysis.injectedServices.length > 0) {
        output += `#### 💉 Injected Services\n` + analysis.injectedServices.map((s) => `- ${s}`).join("\n") + "\n\n";
    }
    if (analysis.referencedModels.length > 0) {
        output += `#### 📦 Referenced Models\n` + analysis.referencedModels.map((m) => `- ${m}`).join("\n") + "\n\n";
    }
    if (analysis.calls.length > 0) {
        output += `#### 📞 Delegation Calls\n` + analysis.calls.map((c) => `- \`${c.member}.${c.method}()\``).join("\n") + "\n\n";
    }
    return output;
}
export function formatCodeContext(data) {
    let output = `### Code Context: ${data.path}\n\n`;
    output += "#### Method Implementation\n\n```csharp\n" + data.method + "\n```\n\n";
    if (Object.keys(data.types).length > 0) {
        output += "#### Referenced Type Definitions\n\n";
        for (const [type, def] of Object.entries(data.types)) {
            output += `<details>\n<summary>${type}</summary>\n\n\`\`\`csharp\n${def}\n\`\`\`\n\n</details>\n`;
        }
    }
    return output;
}
export function formatWorkflowTrace(calls) {
    if (!calls || calls.length === 0)
        return "No delegation calls detected.";
    const lines = calls.map(c => `- \`${c.member}.${c.method}()\``);
    return "### Delegation Calls\n\n" + lines.join("\n");
}
export function formatCsvList(files) {
    let output = "### CSV Files\n\n| Path | Size (bytes) |\n| --- | --- |\n";
    output += files.map(f => `| ${f.path} | ${f.size} |`).join("\n");
    return output;
}
export function formatCsvQuery(data) {
    if (data.length === 0)
        return "No CSV records found.";
    return formatSqlResult(data); // Re-use table formatting
}
