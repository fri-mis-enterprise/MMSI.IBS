import fs from 'fs';
import path from 'path';
import { glob } from 'glob';
export async function findMethodInFile(filePath, methodName) {
    if (!fs.existsSync(filePath))
        return null;
    const content = fs.readFileSync(filePath, 'utf-8');
    // A naive regex for C# methods. 
    // Matches [public|private|...] [async] [Type] MethodName(...) { ... }
    // This is tricky with nested braces, so we'll use a brace-counting approach after finding the signature.
    const escapedMethodName = methodName.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
    const methodRegex = new RegExp(`(?:public|private|protected|internal|static|async|virtual|override|new|\\s)+\\s+[\\w\\<\\>\\[\\]\\?]+\\s+${escapedMethodName}\\s*\\(`, 'g');
    let match;
    while ((match = methodRegex.exec(content)) !== null) {
        const startIdx = match.index;
        // Find the opening brace
        const openingBraceIdx = content.indexOf('{', startIdx + match[0].length);
        if (openingBraceIdx === -1)
            continue;
        let braceCount = 1;
        let endIdx = openingBraceIdx + 1;
        while (braceCount > 0 && endIdx < content.length) {
            if (content[endIdx] === '{')
                braceCount++;
            else if (content[endIdx] === '}')
                braceCount--;
            endIdx++;
        }
        if (braceCount === 0) {
            return content.substring(startIdx, endIdx);
        }
    }
    return null;
}
export function extractReferencedTypes(code) {
    // Extract words that look like Types (Capitalized, not keywords)
    const words = code.match(/\b[A-Z][a-zA-Z0-9_]*\b/g) || [];
    const keywords = new Set(['Task', 'String', 'Int32', 'DateTime', 'Guid', 'Boolean', 'ActionResult', 'JsonResult', 'IActionResult', 'List', 'IEnumerable', 'IQueryable', 'Context', 'DbSet', 'Repository']);
    return [...new Set(words)].filter(word => !keywords.has(word));
}
export async function findTypeDefinition(projectRoot, typeName) {
    // Search in IBS.Models, IBS.DTOs, etc.
    const searchPatterns = [
        path.join(projectRoot, 'IBS.Models', '**', `${typeName}.cs`),
        path.join(projectRoot, 'IBS.DTOs', '**', `${typeName}.cs`),
        path.join(projectRoot, 'IBS.Models', '**', '*.cs'), // Fallback to search inside files
        path.join(projectRoot, 'IBS.DTOs', '**', '*.cs')
    ];
    for (const pattern of searchPatterns) {
        const files = await glob(pattern.replace(/\\/g, '/'));
        for (const file of files) {
            const content = fs.readFileSync(file, 'utf-8');
            // Look for "class TypeName", "record TypeName", "enum TypeName", "interface TypeName"
            const typeRegex = new RegExp(`(?:public|internal|private|protected|static|partial|\\s)+\\s+(?:class|record|enum|struct|interface)\\s+${typeName}\\b`);
            if (typeRegex.test(content)) {
                // Find the whole block (naive brace counting again)
                const match = content.match(typeRegex);
                if (match) {
                    const startIdx = match.index;
                    const openingBraceIdx = content.indexOf('{', startIdx + match[0].length);
                    if (openingBraceIdx === -1) {
                        // Maybe it's a simple enum or something without braces on same line?
                        // Or maybe it's a file-scoped namespace class? 
                        // For now, return the whole file if it's small, or a chunk.
                        return content;
                    }
                    let braceCount = 1;
                    let endIdx = openingBraceIdx + 1;
                    while (braceCount > 0 && endIdx < content.length) {
                        if (content[endIdx] === '{')
                            braceCount++;
                        else if (content[endIdx] === '}')
                            braceCount--;
                        endIdx++;
                    }
                    return content.substring(startIdx, endIdx);
                }
            }
        }
    }
    return null;
}
export async function traceMethodCalls(projectRoot, code, depth = 2) {
    if (depth === 0)
        return [];
    // Look for patterns like _someService.SomeMethod( or _unitOfWork.SomeRepo.SomeMethod(
    const callRegex = /_(\w+)\.(\w+)\s*\(/g;
    const matches = [...code.matchAll(callRegex)];
    const traces = [];
    for (const match of matches) {
        const memberName = match[1];
        const methodName = match[2];
        // Try to find where this member is defined (e.g., in the constructor or field)
        // This is hard without a full parser, so we'll search for the method in Services and DataAccess
        const searchPatterns = [
            path.join(projectRoot, 'IBS.Services', '**', '*.cs'),
            path.join(projectRoot, 'IBS.DataAccess', '**', '*.cs')
        ];
        for (const pattern of searchPatterns) {
            const files = await glob(pattern.replace(/\\/g, '/'));
            for (const file of files) {
                const methodBody = await findMethodInFile(file, methodName);
                if (methodBody) {
                    const subTraces = await traceMethodCalls(projectRoot, methodBody, depth - 1);
                    traces.push({
                        member: memberName,
                        method: methodName,
                        file: path.relative(projectRoot, file),
                        body: methodBody,
                        calls: subTraces
                    });
                    break; // Found it, move to next match
                }
            }
        }
    }
    return traces;
}
