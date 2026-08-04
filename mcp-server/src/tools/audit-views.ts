import fs from 'fs';
import path from 'path';
import { glob } from 'glob';

interface Finding {
  file: string;
  item: string;
  rule: string;
  detail: string;
}

/** Strip script/style blocks and HTML comments so tag counting ignores JS/CSS strings. */
function stripNonMarkup(content: string): string {
  return content
    .replace(/<script[\s\S]*?<\/script>/gi, '')
    .replace(/<style[\s\S]*?<\/style>/gi, '')
    .replace(/<!--[\s\S]*?-->/g, '');
}

const STRUCTURAL_TAGS = ['div', 'form', 'table', 'thead', 'tbody', 'tr', 'select', 'main', 'section'];

export function auditViews(projectRoot: string, folder?: string): Finding[] {
  const findings: Finding[] = [];
  const viewsDir = path.join(projectRoot, 'IBSWeb', 'Areas', '**', 'Views', folder ? `${folder}/**` : '**', '*.cshtml');
  const files = glob.sync(viewsDir.replace(/\\/g, '/'));

  for (const file of files) {
    const content = fs.readFileSync(file, 'utf-8');
    const rel = path.relative(projectRoot, file).replace(/\\/g, '/');
    const base = path.basename(file).toLowerCase();
    const isPartial = base.startsWith('_');

    // V3: full pages must use modern layout (skip partials and tiny files)
    if (!isPartial && content.trim().length > 1000 && !/modern-layout/.test(content)) {
      findings.push({ file: rel, item: '(layout)', rule: 'V3', detail: 'No `.modern-layout` wrapper; does not follow §4.4 class naming.' });
    }

    // V1: Index pages should have no @model (§4.4)
    const modelMatch = content.match(/^\s*@model\s+(\S+)/m);
    if (base === 'index.cshtml' && modelMatch) {
      findings.push({ file: rel, item: '@model', rule: 'V1', detail: `Index page declares @model ${modelMatch[1]}; §4.4 says no @model on Index (DataTable loads via AJAX).` });
    }

    // V2: Create/Edit should use a typed ViewModel, not a domain entity
    if ((base === 'create.cshtml' || base === 'edit.cshtml') && modelMatch && !/ViewModel$/.test(modelMatch[1])) {
      findings.push({ file: rel, item: '@model', rule: 'V2', detail: `Uses domain entity @model ${modelMatch[1]} instead of a ViewModel (§4.4).` });
    }

    // V7: @model declared but never referenced (neither Model.* nor asp-for)
    if (modelMatch) {
      const used = /[@.]Model\./.test(content) || /asp-for\s*=/.test(content);
      if (!used) {
        findings.push({ file: rel, item: '@model', rule: 'V7', detail: `@model declared but never referenced via Model.* or asp-for.` });
      }
    }

    // V4: script blocks containing fetch() must handle success/error
    const scriptBlocks = content.match(/<script[\s\S]*?<\/script>/gi) || [];
    for (let i = 0; i < scriptBlocks.length; i++) {
      const script = scriptBlocks[i];
      if (!/fetch\s*\(/.test(script)) continue;
      if (!/\.success\b|\.catch\s*\(|\.then\s*\(/.test(script)) {
        findings.push({ file: rel, item: `(script ${i + 1})`, rule: 'V4', detail: 'Uses fetch() but never checks `.success` / has no `.catch`/`.then` error path.' });
      }

      // V6: `.success`/`.error`/`.message` read on a variable that is neither declared
      // nor a known global nor a function param -> ReferenceError.
      const GLOBALS = new Set(['console', 'ModernAlert', 'window', 'document', 'location', 'history', 'URL', 'URLSearchParams', 'FormData', 'JSON', 'encodeURIComponent', 'fetch', 'Promise', 'Math', 'Date', 'String', 'Number', 'Boolean', 'Object', 'Array', 'globalThis', 'top', 'self', 'parent', 'setTimeout', 'setInterval', 'clearTimeout', 'clearInterval', 'requestAnimationFrame', 'parseInt', 'parseFloat', 'isNaN', 'undefined', 'null', 'true', 'false']);
      const declared = new Set();
      const declRe = /\b(?:const|let|var)\s+([A-Za-z_$][\w$]*)/g;
      let d;
      while ((d = declRe.exec(script)) !== null) declared.add(d[1]);
      const paramRe = /(?:\(([^)]*)\)\s*=>|=>\s*\(([^)]*)\)|function\s*\w*\s*\(([^)]*)\)|\(([^)]*)\)\s*=>|([A-Za-z_$][\w$]*)\s*=>)/g;
      let pm;
      while ((pm = paramRe.exec(script)) !== null) {
        const list = pm[1] || pm[2] || pm[3] || pm[4] || pm[5] || '';
        list.split(',').forEach(p => { const n = p.trim().replace(/\s*=.*$/, ''); if (/^[A-Za-z_$][\w$]*$/.test(n)) declared.add(n); });
      }
      const readRe = /\b([A-Za-z_$][\w$]*)\.(success|error|message|redirectUrl)\b/g;
      let r;
      while ((r = readRe.exec(script)) !== null) {
        const prev = script[r.index - 1] || '';
        if (prev === '.' || prev === '$') continue; // property access on another object (PageConfig.messages.error)
        if (!declared.has(r[1]) && !GLOBALS.has(r[1])) {
          findings.push({ file: rel, item: `(script ${i + 1})`, rule: 'V6', detail: `Reads \`${r[1]}.${r[2]}\` but \`${r[1]}\` is never declared -> runtime ReferenceError.` });
          break;
        }
      }
    }

    // V5: balanced structural tags (after stripping scripts/styles/comments)
    const html = stripNonMarkup(content);
    for (const tag of STRUCTURAL_TAGS) {
      const open = (html.match(new RegExp(`<${tag}(?:\\s[^>]*)?>`, 'gi')) || []).length;
      const selfClose = (html.match(new RegExp(`<${tag}(?:\\s[^>]*)?\\/>`, 'gi')) || []).length;
      const close = (html.match(new RegExp(`</${tag}>`, 'gi')) || []).length;
      if (open - selfClose !== close) {
        findings.push({ file: rel, item: `<${tag}>`, rule: 'V5', detail: `Tag imbalance: ${open - selfClose} open vs ${close} close.` });
      }
    }
  }

  return findings;
}
