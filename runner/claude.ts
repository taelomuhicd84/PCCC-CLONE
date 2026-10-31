// Gọi Claude qua `claude -p` (Claude Code CLI) bằng phiên đăng nhập trên máy — KHÔNG dùng API key.
// Chỉ cho công cụ đọc. Mẫu tham khảo: PCCC_he_thong/runner/src/engines/claude.ts (bỏ phần token dán tay).
import { spawn, spawnSync } from "node:child_process";
import { existsSync } from "node:fs";
import os from "node:os";
import path from "node:path";

/** Tìm claude: PATH trước, rồi chỗ bộ cài mặc định (%USERPROFILE%\.local\bin). Windows: .exe gọi thẳng, .cmd (npm) qua shell. */
function lenh(): { cmd: string; shell: boolean } | null {
  if (process.platform !== "win32") return { cmd: "claude", shell: false };
  const w = (spawnSync("where", ["claude"], { encoding: "utf8" }).stdout ?? "").split(/\r?\n/).map((x) => x.trim()).filter(Boolean);
  const exe = w.find((x) => x.toLowerCase().endsWith(".exe")) ?? [path.join(os.homedir(), ".local", "bin", "claude.exe")].find(existsSync);
  if (exe) return { cmd: exe, shell: false };
  return w[0] ? { cmd: w[0], shell: true } : null;
}
const quote = (a: string) => (/[\s"]/.test(a) ? `"${a.replace(/"/g, '\\"')}"` : a);

export const HUONG_DAN_CAI = "Chưa có Claude Code CLI. Cài: npm i -g @anthropic-ai/claude-code, rồi mở terminal gõ `claude` một lần để đăng nhập tài khoản Claude.";
const HUONG_DAN_DANG_NHAP = "Phiên đăng nhập Claude trên máy chạy runner đã hết hạn: mở terminal, gõ `claude`, rồi /login (hoặc `claude auth login`), xong thử lại.";

export function hoiClaude(cauHoi: string, thuMuc: string, timeoutMs = 5 * 60_000): Promise<string> {
  const l = lenh();
  if (!l) return Promise.reject(new Error(HUONG_DAN_CAI));
  // --tools: chỉ có công cụ đọc. --setting-sources project,local: không nạp hook/plugin trong cài đặt cá nhân (~/.claude/settings.json)
  // để câu trả lời đúng định dạng web cần. --strict-mcp-config: bỏ MCP server cho khởi động nhanh. (Claude Code ≥ 2.1)
  const args = [
    "-p", "--output-format", "json",
    "--tools", "Read", "Grep", "Glob", "--allowedTools", "Read", "Grep", "Glob",
    "--setting-sources", "project,local", "--strict-mcp-config", "--disable-slash-commands", "--no-session-persistence",
  ];
  // Bỏ API key để CLI dùng phiên đăng nhập (gói Claude), không tính tiền API. Bỏ CLAUDECODE khi runner được bật từ trong Claude Code.
  const env = { ...process.env };
  delete env.ANTHROPIC_API_KEY;
  delete env.CLAUDECODE;
  return new Promise((resolve, reject) => {
    const p = spawn(l.shell ? quote(l.cmd) : l.cmd, l.shell ? args.map(quote) : args, { cwd: thuMuc, shell: l.shell, windowsHide: true, env });
    let out = "", err = "";
    const t = setTimeout(() => { p.kill(); reject(new Error("Claude quá thời gian")); }, timeoutMs);
    p.stdout.on("data", (d) => (out += d));
    p.stderr.on("data", (d) => (err += d));
    p.stdin.on("error", () => {}); // claude thoát sớm (lỗi đăng nhập...) thì ghi stdin báo EPIPE: bỏ qua, lỗi thật đọc ở "close"
    p.on("error", () => { clearTimeout(t); reject(new Error(HUONG_DAN_CAI)); });
    p.on("close", (code) => {
      clearTimeout(t);
      const loi = (m: string) => new Error(/authenticat|oauth|log ?in|401|credential/i.test(m) ? HUONG_DAN_DANG_NHAP + " (" + m.slice(0, 200) + ")" : m);
      try {
        const j = JSON.parse(out);
        if (j.is_error) return reject(loi("Claude báo lỗi: " + String(j.result ?? "").slice(0, 500)));
        resolve(String(j.result ?? ""));
      } catch {
        reject(loi(`claude thoát mã ${code}: ${(err || out).trim().slice(0, 500) || HUONG_DAN_CAI}`));
      }
    });
    p.stdin.end(cauHoi, "utf8");
  });
}
