// Thống kê dự án cho tab "Dự án": hạng mục (module trong CLAUDE.md), quy chuẩn trích dẫn, skill, agent.
import { existsSync, readdirSync, readFileSync, statSync } from "node:fs";
import path from "node:path";

const BO_QUA = new Set(["node_modules", ".git", "bin", "obj", "__pycache__", ".pytest_cache", "worktrees", "ket_qua"]);
const BO_QUA_DUOI = /\.(dll|pdb|exe|cache)$/i;
const DOC_CHU = /\.(md|json|ts|py|ya?ml|txt|html|cs|ps1|bat)$/i;

export type Tep = { rel: string; size: number };

export function quetTep(goc: string): { tep: Tep[]; congTrinh: number } {
  const tep: Tep[] = [];
  const duyet = (d: string) => {
    for (const e of readdirSync(d, { withFileTypes: true })) {
      if (BO_QUA.has(e.name)) continue;
      const p = path.join(d, e.name);
      if (e.isDirectory()) duyet(p);
      else if (!BO_QUA_DUOI.test(e.name)) tep.push({ rel: path.relative(goc, p).replace(/\\/g, "/"), size: statSync(p).size });
    }
  };
  duyet(goc);
  const kq = path.join(goc, "ket_qua"); // hồ sơ khách: chỉ đếm thư mục công trình, không liệt kê tên
  const congTrinh = existsSync(kq) ? readdirSync(kq, { withFileTypes: true }).filter((e) => e.isDirectory()).length : 0;
  return { tep, congTrinh };
}

/** Gom mã quy chuẩn trong 1 đoạn văn bản (mỗi mã tính 1 lần). */
export function maQuyChuan(text: string): Set<string> {
  const kq = new Set<string>();
  for (const m of text.matchAll(/\b(TCVN|QCVN)\s?(\d+(?:-\d+)?)(?:\s?:\s?(\d{4}))?/g)) kq.add(`${m[1]} ${m[2]}${m[3] ? ":" + m[3] : ""}`);
  for (const m of text.matchAll(/(?:\bNĐ|\bND|Nghị định)\s?(\d+\/\d{4})/g)) kq.add(`NĐ ${m[1]}`);
  for (const m of text.matchAll(/(?:\bTT|Thông tư)\s?(\d+\/\d{4})/g)) kq.add(`TT ${m[1]}`);
  return kq;
}

function frontmatter(txt: string): Record<string, string> {
  const m = /^---\r?\n([\s\S]*?)\r?\n---/.exec(txt);
  const o: Record<string, string> = {};
  for (const l of (m?.[1] ?? "").split(/\r?\n/)) { const k = /^(\w+):\s*(.*)$/.exec(l); if (k) o[k[1]] = k[2].trim(); }
  return o;
}

const duoi = (rel: string, goc: string) => rel === goc || rel.startsWith(goc + "/");

/** Bảng module trong CLAUDE.md: [tên, skill]. */
export function docModule(claudeMd: string): { ten: string; skill: string }[] {
  const ds: { ten: string; skill: string }[] = [];
  const phan = claudeMd.split("## Mỗi module là một skill")[1]?.split(/\n## /)[0] ?? "";
  for (const l of phan.split(/\r?\n/)) {
    const m = /^\|\s*([^|]+?)\s*\|\s*`([\w-]+)`/.exec(l);
    if (m && !ds.some((d) => d.skill === m[2])) ds.push({ ten: m[1].split(/ [(:]/)[0], skill: m[2] });
  }
  return ds;
}

export function thongKe(goc: string) {
  const { tep, congTrinh } = quetTep(goc);
  const thuMucSkill = (ten: string) => [".claude/skills/" + ten, "cad/.claude/skills/" + ten].find((p) => tep.some((t) => duoi(t.rel, p)));
  const demTheo = (ds: string[]) => tep.filter((t) => ds.some((d) => duoi(t.rel, d)));

  const skill = tep.filter((t) => /(^|\/)\.claude\/skills\/[^/]+\/SKILL\.md$/.test(t.rel)).map((t) => {
    const dir = t.rel.replace(/\/SKILL\.md$/, "");
    const f = frontmatter(readFileSync(path.join(goc, t.rel), "utf8"));
    return { ten: f.name ?? path.basename(dir), mo_ta: (f.description ?? "").slice(0, 160), model: f.model ?? "", effort: f.effort ?? "", tep: demTheo([dir]).length };
  });
  const agent = tep.filter((t) => /(^|\/)\.claude\/agents\/[^/]+\.md$/.test(t.rel)).map((t) => {
    const f = frontmatter(readFileSync(path.join(goc, t.rel), "utf8"));
    return { ten: f.name ?? path.basename(t.rel, ".md"), model: f.model ?? "", effort: f.effort ?? "" };
  });

  const daThuoc = new Set<string>();
  const hangMuc = docModule(readFileSync(path.join(goc, "CLAUDE.md"), "utf8")).map(({ ten, skill: sk }) => {
    const dir = thuMucSkill(sk);
    const nhom = new Map<string, Set<string>>();
    const them = (k: string, rel: string) => { (nhom.get(k) ?? nhom.set(k, new Set()).get(k)!).add(rel); daThuoc.add(rel); };
    if (dir) {
      for (const t of demTheo([dir])) { const r = t.rel.slice(dir.length + 1); them(r.includes("/") ? r.split("/")[0] : "SKILL.md", t.rel); }
      const md = readFileSync(path.join(goc, dir, "SKILL.md"), "utf8");
      const lietKe = new Set<string>();
      for (const m of md.matchAll(/`((?:shared|runner|thamdinh|cad|web|data|knowledge|skills|bang_tinh|scripts)\/[^`\s*]*)`/g)) lietKe.add(m[1].replace(/\/$/, ""));
      for (const p of lietKe) for (const t of demTheo([p])) them(p.split("/")[0], t.rel);
    }
    const tong = new Set([...nhom.values()].flatMap((s) => [...s]));
    return { ten, skill: sk, tep: tong.size, nhom: [...nhom].map(([k, s]) => ({ ten: k, so: s.size })).sort((a, b) => b.so - a.so) };
  });
  const khac = new Map<string, number>();
  for (const t of tep) if (!daThuoc.has(t.rel)) { const k = t.rel.includes("/") ? t.rel.split("/")[0] : "(gốc)"; khac.set(k, (khac.get(k) ?? 0) + 1); }
  hangMuc.push({ ten: "Ngoài module", skill: "", tep: [...khac.values()].reduce((a, b) => a + b, 0), nhom: [...khac].map(([k, so]) => ({ ten: k, so })).sort((a, b) => b.so - a.so) });

  const dem = new Map<string, number>();
  for (const t of tep) {
    if (!DOC_CHU.test(t.rel) || t.size > 2_000_000) continue;
    for (const m of maQuyChuan(readFileSync(path.join(goc, t.rel), "utf8"))) dem.set(m, (dem.get(m) ?? 0) + 1);
  }
  const quyChuan = [...dem].map(([ma, so]) => ({ ma, so })).sort((a, b) => b.so - a.so || a.ma.localeCompare(b.ma));

  const byte = tep.reduce((a, t) => a + t.size, 0);
  const nguon = new Set(tep.filter((t) => t.rel.includes("/")).map((t) => t.rel.split("/")[0]));
  return {
    tong: { tep: tep.length, mb: +(byte / 1048576).toFixed(1), thu_muc_nguon: nguon.size, cong_trinh: congTrinh, skill: skill.length, agent: agent.length, quy_chuan: quyChuan.length },
    hang_muc: hangMuc, quy_chuan: quyChuan, skill, agent,
  };
}
