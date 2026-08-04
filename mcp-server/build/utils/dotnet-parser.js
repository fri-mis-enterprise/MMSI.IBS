import fs from 'fs';
import path from 'path';
import { glob } from 'glob';
/** Walk backwards from match index to collect preceding attribute lines */
function collectAttributes(content, matchIndex) {
    const lines = content.substring(0, matchIndex).split('\n');
    const attrLines = [];
    let inMultiLine = false;
    for (let i = lines.length - 1; i >= 0; i--) {
        const trimmed = lines[i].trim();
        if (trimmed.startsWith('[')) {
            attrLines.unshift(lines[i]);
            inMultiLine = !trimmed.endsWith(']');
        }
        else if (inMultiLine) {
            attrLines.unshift(lines[i]);
            if (trimmed.includes(']'))
                inMultiLine = false;
        }
        else if (trimmed === '' || trimmed.startsWith('//')) {
            continue; // skip blanks and comments — attributes may be above them
        }
        else {
            break; // non-attribute code → stop
        }
    }
    return attrLines.join('\n');
}
export async function findMethodInFile(filePath, methodName) {
    if (!fs.existsSync(filePath))
        return null;
    const content = fs.readFileSync(filePath, 'utf-8');
    const escapedMethodName = methodName.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
    const methodRegex = new RegExp(`(?:public|private|protected|internal|static|async|virtual|override|new|\\s)+\\s+[\\w\\<\\>\\[\\]\\?\\,]+\\s+${escapedMethodName}\\s*\\(`, 'g');
    const bodies = [];
    let match;
    while ((match = methodRegex.exec(content)) !== null) {
        const startIdx = match.index;
        const attributes = collectAttributes(content, startIdx);
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
            const preamble = attributes ? attributes + '\n' : '';
            bodies.push(preamble + content.substring(startIdx, endIdx));
        }
    }
    return bodies.length > 0 ? bodies.join('\n\n') : null;
}
export function extractReferencedTypes(code) {
    // Extract words that look like Types (Capitalized, not keywords)
    const words = code.match(/\b[A-Z][a-zA-Z0-9_]*\b/g) || [];
    const keywords = new Set([
        'Task', 'String', 'Int32', 'DateTime', 'DateOnly', 'Guid', 'Boolean', 'Decimal',
        'ActionResult', 'JsonResult', 'IActionResult', 'List', 'IEnumerable', 'IQueryable',
        'Context', 'DbSet', 'Repository', 'UnitOfWork', 'IUnitOfWork', 'Controller',
        'SelectListItem', 'CancellationToken', 'ClaimsPrincipal', 'UserManager', 'ILogger',
        'ApplicationUser', 'ApplicationDbContext', 'View', 'ViewBag', 'TempData', 'ModelState',
        'User', 'Ok', 'BadRequest', 'NotFound', 'RedirectToAction', 'Json', 'nameof'
    ]);
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
/** Lightweight delegation scan: regex-parse method body for calls, no recursive file search */
export function traceMethodCalls(code) {
    const callRegex = /\b([a-z_]\w*)\s*\.\s*(\w+)\s*\(/g;
    const matches = [...code.matchAll(callRegex)];
    const skipMembers = new Set(['logger', 'console', 'await', 'task', 'string', 'result', 'ex', 'e', 'viewModel', 'model', 'query', 'key', 'value', 'pair', 'item', 'arg', 'args', 'options', 'config', 'configuration', 'httpClient', 'httpContext', 'url', 'path', 'dir', 'file', 'stream', 'reader', 'writer']);
    const seen = new Set();
    const calls = [];
    for (const match of matches) {
        const member = match[1];
        const method = match[2];
        const key = `${member}.${method}`;
        if (seen.has(key))
            continue;
        seen.add(key);
        const root = member.toLowerCase().split('.')[0];
        if (skipMembers.has(root))
            continue;
        calls.push({ member, method });
    }
    return calls;
}
/** Resolve a member reference (variable or property chain) to a candidate class file. */
async function resolveMemberFile(projectRoot, member) {
    const segment = member.split('.').pop();
    const pascal = segment.charAt(0).toUpperCase() + segment.slice(1).replace(/^I/, '');
    const singular = pascal.replace(/ies$/, 'y').replace(/s$/, '');
    const baseNames = new Set([
        pascal, pascal + 'Service', pascal + 'Repository',
        singular, singular + 'Service', singular + 'Repository',
        'I' + pascal + 'Service', 'I' + pascal + 'Repository',
    ]);
    const patterns = [
        path.join(projectRoot, 'IBS.Services', '**', '*.cs'),
        path.join(projectRoot, 'IBS.DataAccess', '**', '*.cs'),
    ];
    for (const pattern of patterns) {
        const files = await glob(pattern.replace(/\\/g, '/'));
        for (const file of files) {
            const base = path.basename(file, '.cs');
            if (baseNames.has(base))
                return file;
        }
    }
    return null;
}
/**
 * Recursively trace delegation calls: resolves member.method() to a definition file
 * (IBS.Services / IBS.DataAccess) and follows it, deduping cycles and capping depth.
 */
export async function traceWorkflowDeep(projectRoot, filePath, methodName, maxDepth = 3) {
    const fullPath = path.join(projectRoot, filePath);
    const body = await findMethodInFile(fullPath, methodName);
    if (!body)
        return [];
    const results = [];
    const visited = new Set();
    const queue = [{ file: filePath, method: methodName, depth: 0 }];
    while (queue.length > 0) {
        const { file, method, depth } = queue.shift();
        const key = `${file}#${method}`;
        if (visited.has(key) || depth > maxDepth)
            continue;
        visited.add(key);
        const full = path.join(projectRoot, file);
        const methodBody = await findMethodInFile(full, method);
        if (!methodBody)
            continue;
        const calls = traceMethodCalls(methodBody);
        results.push({ file, method, calls });
        for (const call of calls) {
            const memberFile = await resolveMemberFile(projectRoot, call.member);
            if (!memberFile)
                continue;
            queue.push({
                file: path.relative(projectRoot, memberFile).replace(/\\/g, '/'),
                method: call.method,
                depth: depth + 1,
            });
        }
    }
    return results;
}
export function extractModelInfo(code) {
    const propertyRegex = /\[([^\]]+)\]\s*public\s+([\w\<\>\[\]\?]+)\s+(\w+)\s*\{\s*get;\s*set;\s*\}/g;
    const simplePropertyRegex = /public\s+([\w\<\>\[\]\?]+)\s+(\w+)\s*\{\s*get;\s*set;\s*\}/g;
    const properties = [];
    let match;
    // First pass: properties with attributes
    while ((match = propertyRegex.exec(code)) !== null) {
        properties.push({
            attributes: match[1].split(',').map(a => a.trim()),
            type: match[2],
            name: match[3]
        });
    }
    // Second pass: properties without attributes (avoiding duplicates)
    while ((match = simplePropertyRegex.exec(code)) !== null) {
        if (!properties.find(p => p.name === match[2])) {
            properties.push({
                attributes: [],
                type: match[1],
                name: match[2]
            });
        }
    }
    return properties;
}
export async function analyzeActionRelations(projectRoot, filePath, methodName) {
    const fullPath = path.join(projectRoot, filePath);
    if (!fs.existsSync(fullPath))
        throw new Error(`File not found: ${filePath}`);
    const content = fs.readFileSync(fullPath, 'utf-8');
    // Find injected services in constructor
    const constructorRegex = /public\s+\w+\s*\(([^)]*)\)/;
    const constructorMatch = content.match(constructorRegex);
    const injectedServices = [];
    if (constructorMatch) {
        const params = constructorMatch[1].split(',');
        params.forEach(p => {
            const parts = p.trim().split(/\s+/);
            if (parts.length >= 2) {
                injectedServices.push(parts[0]); // Take the Type
            }
        });
    }
    const methodBody = await findMethodInFile(fullPath, methodName);
    if (!methodBody)
        throw new Error(`Action ${methodName} not found in ${filePath}`);
    const referencedModels = extractReferencedTypes(methodBody);
    const calls = traceMethodCalls(methodBody);
    return {
        methodName,
        filePath,
        injectedServices: [...new Set(injectedServices)],
        referencedModels,
        calls
    };
}
