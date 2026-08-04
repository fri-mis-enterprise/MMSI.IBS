import fs from 'fs';
import path from 'path';
import { glob } from 'glob';

interface MethodInfo {
  name: string;
  returnType: string;
  attributes: string;
  body: string;
  startIndex: number;
}

interface Finding {
  file: string;
  item: string;
  rule: string;
  detail: string;
}

/** Attribute lines directly above a match index (handles multi-line attrs). */
function attributesAbove(content: string, matchIndex: number): string {
  const lines = content.substring(0, matchIndex).split('\n');
  const attrs: string[] = [];
  let inMultiLine = false;
  for (let i = lines.length - 1; i >= 0; i--) {
    const t = lines[i].trim();
    if (inMultiLine) {
      attrs.unshift(lines[i]);
      if (t.startsWith('[')) inMultiLine = false;
      continue;
    }
    if (t.startsWith('[')) {
      attrs.unshift(lines[i]);
      if (!t.includes(']')) inMultiLine = true;
      continue;
    }
    // closing tail of a multi-line attribute (opening `[` is further up)
    if (t.endsWith(']') && !t.startsWith('[')) {
      attrs.unshift(lines[i]);
      inMultiLine = true;
      continue;
    }
    if (t === '' || t.startsWith('//')) continue;
    break;
  }
  return attrs.join('\n');
}

/** All public methods with brace-counted bodies. */
function publicMethods(content: string): MethodInfo[] {
  const regex = /public\s+(?:async\s+)?([\w<>\[\]?,\.\s]+)\s+(\w+)\s*\(/g;
  const methods: MethodInfo[] = [];
  let m;
  while ((m = regex.exec(content)) !== null) {
    if (/\b(?:class|interface|record|enum|struct)\b/.test(m[1])) continue;
    const open = content.indexOf('{', m.index + m[0].length);
    if (open === -1) continue;
    let depth = 1;
    let end = open + 1;
    while (depth > 0 && end < content.length) {
      if (content[end] === '{') depth++;
      else if (content[end] === '}') depth--;
      end++;
    }
    if (depth !== 0) continue;
    methods.push({
      name: m[2],
      returnType: m[1].trim(),
      attributes: attributesAbove(content, m.index),
      body: content.substring(open, end),
      startIndex: m.index,
    });
  }
  return methods;
}

const hasAccessAttr = (attrs: string) => /\[(RequireAccess|RequireAnyAccess|Authorize|AllowAnonymous)\b/.test(attrs);
const isActionResult = (rt: string) => /ActionResult|JsonResult|FileResult|IActionResult/.test(rt);

export async function auditControllers(projectRoot: string): Promise<Finding[]> {
  const findings: Finding[] = [];
  const files = await glob(path.join(projectRoot, 'IBSWeb', 'Areas', '**', 'Controllers', '*.cs').replace(/\\/g, '/'));
  for (const file of files) {
    const content = fs.readFileSync(file, 'utf-8');
    const rel = path.relative(projectRoot, file).replace(/\\/g, '/');

    const classMatch = content.match(/class\s+(\w+)\s*\(/);
    if (!classMatch) {
      findings.push({ file: rel, item: '(class)', rule: 'C2', detail: 'No primary constructor found.' });
      continue;
    }
    const lineStart = content.lastIndexOf('\n', classMatch.index! - 1) + 1;
    const classAttrs = attributesAbove(content, lineStart);
    const classCovered = hasAccessAttr(classAttrs);

    for (const m of publicMethods(content)) {
      if (!isActionResult(m.returnType)) continue;
      if (classCovered || hasAccessAttr(m.attributes)) continue;
      findings.push({
        file: rel, item: m.name, rule: 'C3',
        detail: 'Action has no [RequireAccess]/[RequireAnyAccess]/[Authorize] and class has none either.',
      });
    }
  }
  return findings;
}

export async function auditServices(projectRoot: string): Promise<Finding[]> {
  const findings: Finding[] = [];
  const files = await glob(path.join(projectRoot, 'IBS.Services', '*.cs').replace(/\\/g, '/'));
  for (const file of files) {
    const content = fs.readFileSync(file, 'utf-8');
    const rel = path.relative(projectRoot, file).replace(/\\/g, '/');
    if (/^I[A-Z]/.test(path.basename(file)) || rel.endsWith('Middleware.cs')) continue;

    const classMatch = content.match(/class\s+(\w+)\s*\(([^)]*)\)/);
    if (!classMatch) {
      findings.push({ file: rel, item: '(class)', rule: 'S1', detail: 'No primary constructor found.' });
    } else if (!/IUnitOfWork/.test(classMatch[2])) {
      findings.push({ file: rel, item: '(class)', rule: 'S1', detail: 'Primary constructor does not inject IUnitOfWork.' });
    }

    if (/\bApplicationDbContext\b/.test(content)) {
      findings.push({ file: rel, item: '(class)', rule: 'S2', detail: 'References ApplicationDbContext directly; use IUnitOfWork only.' });
    }

    for (const m of publicMethods(content)) {
      const mutates = /unitOfWork\.\w+\.(AddAsync|RemoveAsync)/.test(m.body) || /unitOfWork\.SaveAsync\s*\(/.test(m.body);
      if (!mutates) continue;
      if (!/catch\s*\(/.test(m.body)) {
        findings.push({ file: rel, item: m.name, rule: 'S3', detail: 'Mutation without try/catch returning ServiceResult.' });
      }
      if (!/RecordAuditAsync|AuditTrail\.AddAsync|\.Audit\b/.test(m.body)) {
        findings.push({ file: rel, item: m.name, rule: 'S4', detail: 'Mutation without AuditTrail.AddAsync (AGENTS: audit trails on all CUD).' });
      }
    }
  }
  return findings;
}
