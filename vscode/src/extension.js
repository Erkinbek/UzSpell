// UzSpell — VS Code kengaytmasi. 100% oflayn oʻzbekcha imlo/grammatika + transliteratsiya.
const vscode = require('vscode');
const fs = require('node:fs');
const path = require('node:path');

let engine = null;
let diagnostics = null;
let customWords = new Set();      // doimiy shaxsiy lugʻat (normalizatsiya qilingan)
let sessionIgnored = new Set();   // shu sessiyada eʼtiborsiz qoldirilganlar
let customWordsPath = null;
const debounceTimers = new Map(); // uri -> timeout
const docErrors = new Map();      // uri -> errors[] (code action uchun)

// node-engine.js ESM — dinamik import bilan yuklaymiz
let engineMod = null;
async function getEngineMod() {
  if (!engineMod) engineMod = await import('./node-engine.js');
  return engineMod;
}

function config() {
  return vscode.workspace.getConfiguration('uzspell');
}

function checkOpts() {
  const c = config();
  return {
    script: c.get('script', 'auto'),
    grammar: c.get('grammar', true),
    suggestions: true,
    checkAllCaps: c.get('checkAllCaps', false),
    maxSuggestions: c.get('maxSuggestions', 6),
    ignored: new Set([...customWords, ...sessionIgnored]),
  };
}

function langEnabled(doc) {
  if (!config().get('enable', true)) return false;
  const langs = config().get('languages', ['plaintext', 'markdown']);
  return langs.includes(doc.languageId);
}

// ---------------- Faollashtirish ----------------

async function activate(context) {
  diagnostics = vscode.languages.createDiagnosticCollection('uzspell');
  context.subscriptions.push(diagnostics);

  // Shaxsiy lugʻat globalStorage'da saqlanadi
  customWordsPath = path.join(context.globalStorageUri.fsPath, 'custom_words.txt');
  loadCustomWords();

  const mod = await getEngineMod();
  engine = mod.createNodeEngine(context.extensionPath);

  // Hodisalar
  context.subscriptions.push(
    vscode.workspace.onDidOpenTextDocument((d) => scheduleCheck(d, 0)),
    vscode.workspace.onDidChangeTextDocument((e) => scheduleCheck(e.document, 450)),
    vscode.workspace.onDidCloseTextDocument((d) => {
      diagnostics.delete(d.uri);
      docErrors.delete(d.uri.toString());
    }),
    vscode.workspace.onDidChangeConfiguration((e) => {
      if (e.affectsConfiguration('uzspell')) recheckAll();
    }),
  );

  // Buyruqlar
  context.subscriptions.push(
    vscode.commands.registerCommand('uzspell.checkDocument', () => {
      const ed = vscode.window.activeTextEditor;
      if (ed) scheduleCheck(ed.document, 0);
    }),
    vscode.commands.registerCommand('uzspell.toCyrillic', () => transliterate('cyrillic')),
    vscode.commands.registerCommand('uzspell.toLatin', () => transliterate('latin')),
    vscode.commands.registerCommand('uzspell.toggle', async () => {
      const cur = config().get('enable', true);
      await config().update('enable', !cur, vscode.ConfigurationTarget.Global);
      vscode.window.showInformationMessage(`UzSpell tekshiruvi ${!cur ? 'yoqildi' : 'oʻchirildi'}.`);
    }),
    vscode.commands.registerCommand('uzspell._fix', applyFix),
    vscode.commands.registerCommand('uzspell._ignore', ignoreWord),
    vscode.commands.registerCommand('uzspell._addToDict', addToDictionary),
  );

  // Tuzatish takliflari (code actions)
  context.subscriptions.push(
    vscode.languages.registerCodeActionsProvider('*', new UzSpellActions(), {
      providedCodeActionKinds: [vscode.CodeActionKind.QuickFix],
    }),
  );

  // Ochilgan hujjatlarni tekshiramiz
  recheckAll();
}

function deactivate() {
  for (const t of debounceTimers.values()) clearTimeout(t);
}

// ---------------- Tekshiruv ----------------

function recheckAll() {
  diagnostics.clear();
  for (const doc of vscode.workspace.textDocuments) scheduleCheck(doc, 0);
}

function scheduleCheck(doc, delay) {
  if (!langEnabled(doc)) {
    diagnostics.delete(doc.uri);
    return;
  }
  const key = doc.uri.toString();
  if (debounceTimers.has(key)) clearTimeout(debounceTimers.get(key));
  debounceTimers.set(key, setTimeout(() => {
    debounceTimers.delete(key);
    runCheck(doc);
  }, delay));
}

async function runCheck(doc) {
  if (!engine || !langEnabled(doc)) return;
  const text = doc.getText();
  let errors;
  try {
    errors = await engine.check(text, checkOpts());
  } catch (err) {
    console.error('UzSpell check error:', err);
    return;
  }
  docErrors.set(doc.uri.toString(), errors);

  const diags = errors.map((e) => {
    const range = new vscode.Range(doc.positionAt(e.start), doc.positionAt(e.start + e.length));
    const isGram = e.type === 'gram';
    const d = new vscode.Diagnostic(
      range,
      isGram ? e.message : `Imlo xatosi: «${e.word}»`,
      isGram ? vscode.DiagnosticSeverity.Information : vscode.DiagnosticSeverity.Warning,
    );
    d.source = 'UzSpell';
    d.code = isGram ? e.ruleId : 'imlo';
    return d;
  });
  diagnostics.set(doc.uri, diags);
}

// ---------------- Tuzatish amallari ----------------

class UzSpellActions {
  provideCodeActions(doc, range) {
    const errors = docErrors.get(doc.uri.toString());
    if (!errors) return [];
    const actions = [];
    for (const e of errors) {
      const eRange = new vscode.Range(doc.positionAt(e.start), doc.positionAt(e.start + e.length));
      if (!eRange.contains(range.start) && !range.intersection(eRange)) continue;

      const max = config().get('maxSuggestions', 6);
      for (const s of (e.suggestions || []).slice(0, max)) {
        const a = new vscode.CodeAction(`→ ${s}`, vscode.CodeActionKind.QuickFix);
        a.command = { command: 'uzspell._fix', title: 'Tuzatish', arguments: [doc.uri, eRange, s] };
        actions.push(a);
      }
      if (e.type === 'spell') {
        const ign = new vscode.CodeAction(`«${e.word}» — eʼtiborsiz qoldirish (sessiya)`, vscode.CodeActionKind.QuickFix);
        ign.command = { command: 'uzspell._ignore', title: 'Eʼtiborsiz', arguments: [doc.uri, e.normalized] };
        actions.push(ign);

        const add = new vscode.CodeAction(`«${e.word}» — lugʻatga qoʻshish`, vscode.CodeActionKind.QuickFix);
        add.command = { command: 'uzspell._addToDict', title: 'Lugʻatga', arguments: [doc.uri, e.normalized] };
        actions.push(add);
      }
    }
    return actions;
  }
}

async function applyFix(uri, range, replacement) {
  const edit = new vscode.WorkspaceEdit();
  edit.replace(uri, range, replacement);
  await vscode.workspace.applyEdit(edit);
}

function ignoreWord(uri, normalized) {
  sessionIgnored.add(normalized);
  const doc = vscode.workspace.textDocuments.find((d) => d.uri.toString() === uri.toString());
  if (doc) runCheck(doc);
}

function addToDictionary(uri, normalized) {
  customWords.add(normalized);
  saveCustomWords();
  recheckAll();
}

// ---------------- Shaxsiy lugʻat ----------------

function loadCustomWords() {
  try {
    if (fs.existsSync(customWordsPath)) {
      for (const line of fs.readFileSync(customWordsPath, 'utf8').split('\n')) {
        const w = line.trim();
        if (w) customWords.add(w);
      }
    }
  } catch { /* lugʻatni oʻqib boʻlmasa ham ishlayveramiz */ }
}

function saveCustomWords() {
  try {
    fs.mkdirSync(path.dirname(customWordsPath), { recursive: true });
    fs.writeFileSync(customWordsPath, [...customWords].join('\n') + '\n', 'utf8');
  } catch (err) {
    vscode.window.showWarningMessage('UzSpell: shaxsiy lugʻatga yozib boʻlmadi.');
  }
}

// ---------------- Transliteratsiya ----------------

async function transliterate(target) {
  const ed = vscode.window.activeTextEditor;
  if (!ed) return;
  const mod = await getEngineMod();
  const convert = target === 'cyrillic' ? mod.toCyrillic : mod.toLatin;

  await ed.edit((builder) => {
    const sels = ed.selections.filter((s) => !s.isEmpty);
    if (sels.length > 0) {
      for (const sel of sels) builder.replace(sel, convert(ed.document.getText(sel)));
    } else {
      const full = new vscode.Range(
        ed.document.positionAt(0),
        ed.document.positionAt(ed.document.getText().length),
      );
      builder.replace(full, convert(ed.document.getText()));
    }
  });
}

module.exports = { activate, deactivate };
