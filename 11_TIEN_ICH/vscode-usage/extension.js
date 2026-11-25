const vscode = require('vscode');
const fs = require('fs');
const os = require('os');
const path = require('path');

const ROOT = path.join(os.homedir(), '.claude', 'projects');
const cache = new Map(); // file -> parsed result, reused while mtime+size unchanged

function walk(dir, depth, out) {
  let ents;
  try { ents = fs.readdirSync(dir, { withFileTypes: true }); } catch (e) { return; }
  for (const e of ents) {
    const p = path.join(dir, e.name);
    if (e.isDirectory()) { if (depth < 3) walk(p, depth + 1, out); }
    else if (e.name.endsWith('.jsonl')) out.push(p);
  }
}

function parse(file) {
  const st = fs.statSync(file);
  const sig = st.mtimeMs + ':' + st.size;
  const c = cache.get(file);
  if (c && c.sig === sig) return c;
  const byId = new Map(); // streaming repeats one message id: keep the last
  let title = '', first = '', cwd = '', last = 0;
  for (const line of fs.readFileSync(file, 'utf8').split('\n')) {
    if (!line) continue;
    const hasUsage = line.indexOf('"usage"') >= 0;
    if (!hasUsage && line.indexOf('"custom-title"') < 0 && !(first === '' && line.indexOf('"type":"user"') >= 0)) continue;
    let o; try { o = JSON.parse(line); } catch (e) { continue; }
    if (o.type === 'custom-title' && o.customTitle) title = o.customTitle;
    else if (o.type === 'user' && !first && o.message) {
      const m = o.message.content;
      const t = typeof m === 'string' ? m : (Array.isArray(m) ? (m.find(x => x.type === 'text') || {}).text : '');
      if (t && t[0] !== '<') first = t.replace(/\s+/g, ' ').slice(0, 80);
    }
    if (o.cwd) cwd = o.cwd;
    const u = o.message && o.message.usage;
    if (u && o.timestamp) {
      const ts = Date.parse(o.timestamp);
      if (ts > last) last = ts;
      byId.set(o.message.id || o.uuid, {
        ts,
        n: (u.input_tokens || 0) + (u.output_tokens || 0) + (u.cache_creation_input_tokens || 0),
        r: u.cache_read_input_tokens || 0
      });
    }
  }
  const rec = { sig, recs: [...byId.values()], title, first, cwd, last: last || st.mtimeMs };
  cache.set(file, rec);
  return rec;
}

function scan() {
  const files = [];
  walk(ROOT, 0, files);
  const all = [], sessions = [];
  for (const f of files) {
    let d; try { d = parse(f); } catch (e) { continue; }
    all.push(...d.recs);
    if (path.dirname(path.dirname(f)) === ROOT) { // top-level session file, not subagent
      let n = 0; for (const r of d.recs) n += r.n;
      sessions.push({ id: path.basename(f, '.jsonl'), title: d.title || d.first || '(không tên)',
        cwd: d.cwd, last: d.last, tokens: n });
    }
  }
  return { all, sessions };
}

const fmt = n => n >= 1e9 ? (n / 1e9).toFixed(2) + ' tỷ' : n >= 1e6 ? (n / 1e6).toFixed(1) + ' tr' : n >= 1e3 ? (n / 1e3).toFixed(0) + 'k' : String(n);
const dayKey = t => { const d = new Date(t); return d.getFullYear() + '-' + (d.getMonth() + 1) + '-' + d.getDate(); };
const ago = t => { const m = (Date.now() - t) / 6e4; return m < 60 ? Math.round(m) + ' phút' : m < 1440 ? Math.round(m / 60) + ' giờ' : Math.round(m / 1440) + ' ngày'; };
const esc = s => String(s).replace(/[&<>]/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;' }[c]));

function stats(all) {
  const now = Date.now(), tot = r => r.n + r.r;
  const sum = (from, f) => all.reduce((s, r) => s + (r.ts >= from ? f(r) : 0), 0);
  const days = [];
  for (let i = 6; i >= 0; i--) {
    const d = new Date(); d.setHours(0, 0, 0, 0); d.setDate(d.getDate() - i);
    const k = dayKey(d);
    days.push({ label: i === 0 ? 'nay' : ['CN', 'T2', 'T3', 'T4', 'T5', 'T6', 'T7'][d.getDay()],
      v: all.reduce((s, r) => s + (dayKey(r.ts) === k ? tot(r) : 0), 0) });
  }
  const t0 = new Date(); t0.setHours(0, 0, 0, 0);
  return {
    today: sum(t0.getTime(), tot), todayNew: sum(t0.getTime(), r => r.n),
    h5: sum(now - 5 * 36e5, tot), h5New: sum(now - 5 * 36e5, r => r.n),
    week: sum(now - 7 * 864e5, tot), weekNew: sum(now - 7 * 864e5, r => r.n),
    max: Math.max(...days.map(d => d.v)), days
  };
}

function bar(label, l) {
  if (!l || l.pct == null) return `<div class="m"><b>${label}</b> <small>chưa nhập</small></div>`;
  const c = l.pct >= 85 ? '#e5534b' : l.pct >= 60 ? '#d29922' : '#3fb950';
  return `<div class="m"><b>${label}</b><span class="r">${l.pct}%</span>
<div class="t"><i style="width:${Math.min(100, l.pct)}%;background:${c}"></i></div>
<small>reset: ${esc(l.reset || '?')} · nhập ${ago(l.saved)} trước</small></div>`;
}

function html(s, lim) {
  const bars = s.days.map(d => `<div class="c"><i style="height:${s.max ? Math.max(2, d.v / s.max * 60) : 2}px" title="${fmt(d.v)}"></i><small>${d.label}</small></div>`).join('');
  return `<!doctype html><html><head><meta charset="utf-8"><style>
body{font:12px var(--vscode-font-family);color:var(--vscode-foreground);padding:6px}
.m{margin:8px 0}.r{float:right}.t{height:6px;background:var(--vscode-input-background);border-radius:3px;margin:3px 0}
.t i{display:block;height:100%;border-radius:3px}small{opacity:.7}
.g{display:flex;align-items:flex-end;gap:6px;height:76px;margin-top:6px}.c{flex:1;text-align:center}
.c i{display:block;background:var(--vscode-charts-blue);border-radius:2px 2px 0 0}
table{width:100%}td:last-child{text-align:right}
a{color:var(--vscode-textLink-foreground);cursor:pointer}</style></head><body>
<b>Hạn mức (nhập tay)</b>${bar('5 giờ', lim.h5)}${bar('1 tuần', lim.wk)}
<a onclick="v.postMessage('set')">Nhập % hạn mức</a>
<hr><b>Token đã dùng</b>
<table><tr><td>Hôm nay</td><td>${fmt(s.today)} <small>(mới ${fmt(s.todayNew)})</small></td></tr>
<tr><td>5 giờ qua</td><td>${fmt(s.h5)} <small>(mới ${fmt(s.h5New)})</small></td></tr>
<tr><td>7 ngày qua</td><td>${fmt(s.week)} <small>(mới ${fmt(s.weekNew)})</small></td></tr></table>
<div class="g">${bars}</div>
<small>"mới" = input + output + ghi cache; tổng gồm cả đọc cache. Chỉ tính phiên trên máy này.</small>
<script>const v=acquireVsCodeApi();</script></body></html>`;
}

function activate(ctx) {
  const lim = () => ctx.globalState.get('limits', {});
  let data = { all: [], sessions: [] };
  const tree = new vscode.EventEmitter();
  let view;

  const refresh = () => {
    try { data = scan(); } catch (e) { console.error(e); }
    if (view) view.webview.html = html(stats(data.all), lim());
    tree.fire();
  };

  ctx.subscriptions.push(vscode.window.registerWebviewViewProvider('claudeUsage.account', {
    resolveWebviewView(v) {
      view = v; v.webview.options = { enableScripts: true };
      v.webview.onDidReceiveMessage(() => vscode.commands.executeCommand('claudeUsage.setLimits'));
      refresh();
    }
  }));

  ctx.subscriptions.push(vscode.window.registerTreeDataProvider('claudeUsage.sessions', {
    onDidChangeTreeData: tree.event,
    getTreeItem: e => e,
    getChildren: () => data.sessions.sort((a, b) => b.last - a.last).slice(0, 60).map(s => {
      const it = new vscode.TreeItem(s.title, vscode.TreeItemCollapsibleState.None);
      it.description = `${ago(s.last)} · ${fmt(s.tokens)}`;
      it.tooltip = `${s.cwd}\n${s.id}`;
      it.command = { command: 'claudeUsage.resume', title: 'Mở', arguments: [s] };
      return it;
    })
  }));

  ctx.subscriptions.push(
    vscode.commands.registerCommand('claudeUsage.refresh', refresh),
    vscode.commands.registerCommand('claudeUsage.resume', s => {
      if (!s || !s.id) return;
      const t = vscode.window.createTerminal({ name: 'claude ' + s.id.slice(0, 6), cwd: s.cwd || undefined });
      t.show(); t.sendText('claude --resume ' + s.id);
    }),
    vscode.commands.registerCommand('claudeUsage.setLimits', async () => {
      const cur = lim(), out = {};
      for (const [k, name] of [['h5', '5 giờ'], ['wk', 'tuần']]) {
        const p = await vscode.window.showInputBox({ prompt: `% đã dùng, cửa sổ ${name} (bỏ trống = xoá)`, value: cur[k] ? String(cur[k].pct) : '' });
        if (p === undefined) return;
        if (p === '') continue;
        const r = await vscode.window.showInputBox({ prompt: `Giờ reset cửa sổ ${name} (vd 18:20 hoặc 21/11 01:00)`, value: cur[k] ? cur[k].reset : '' });
        if (r === undefined) return;
        const n = Number(p.replace(',', '.'));
        if (isNaN(n)) { vscode.window.showWarningMessage('Không phải số: ' + p); return; }
        out[k] = { pct: Math.round(n), reset: r, saved: Date.now() };
      }
      await ctx.globalState.update('limits', out);
      refresh();
    })
  );

  const timer = setInterval(refresh, 60000);
  ctx.subscriptions.push({ dispose: () => clearInterval(timer) });
  setTimeout(refresh, 500);
}

module.exports = { activate, deactivate() {}, _scan: scan, _stats: stats };
