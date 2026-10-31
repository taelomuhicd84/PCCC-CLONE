// Gọi Claude qua `claude -p` (Claude Code CLI) bằng phiên đăng nhập trên máy — KHÔNG dùng API key.
// Chỉ cho công cụ đọc. Mẫu tham khảo: PCCC_he_thong/runner/src/engines/claude.ts (bỏ phần token dán tay).
import { spawn, spawnSync } from "node:child_process";

/** Windows: claude.exe gọi thẳng; claude.cmd (cài qua npm) phải chạy qua shell. */
function lenh(): { cmd: string; shell: boolean } {
  if (process.platform !== "win32") return { cmd: "claude", shell: false };
  const w = (spawnSync("where", ["claude"], { encoding: "utf8" }).stdout ?? "").split(/\r?\n/).map((x) => x.trim()).filter(Boolean);
  const exe = w.find((x) => x.toLowerCase().endsWith(".exe"));
  return exe ? { cmd: exe, shell: false } : { cmd: w[0] ?? "claude", shell: true };
}
const quote = (a: string) => (/[\s"]/.test(a) ? `"${a.replace(/"/g, '\\"')}"` : a);

export const HUONG_DAN_CAI = "Chưa có Claude Code CLI hoặc chưa đăng nhập. Cài: npm i -g @anthropic-ai/claude-code, rồi chạy `claude` một lần để đăng nhập tài khoản Claude.";

export function hoiClaude(cauHoi: string, thuMuc: string, timeoutMs = 5 * 60_000): Promise<string> {
  const args = ["-p", "--output-format", "json", "--allowedTools", "Read", "Grep", "Glob"];
  // Bỏ API key nếu có trong môi trường để CLI dùng phiên đăng nhập (gói Claude), không tính tiền API.
  const env = { ...process.env };
  delete env.ANTHROPIC_API_KEY;
  return new Promise((resolve, reject) => {
    const l = lenh();
    const p = spawn(l.shell ? quote(l.cmd) : l.cmd, l.shell ? args.map(quote) : args, { cwd: thuMuc, shell: l.shell, windowsHide: true, env });
    let out = "", err = "";
    const t = setTimeout(() => { p.kill(); reject(new Error("Claude quá thời gian")); }, timeoutMs);
    p.stdout.on("data", (d) => (out += d));
    p.stderr.on("data", (d) => (err += d));
    p.on("error", () => { clearTimeout(t); reject(new Error(HUONG_DAN_CAI)); });
    p.on("close", (code) => {
      clearTimeout(t);
      try {
        const j = JSON.parse(out);
        if (j.is_error) return reject(new Error("Claude báo lỗi: " + String(j.result ?? "").slice(0, 500)));
        resolve(String(j.result ?? ""));
      } catch {
        reject(new Error(`claude thoát mã ${code}: ${(err || out).slice(0, 500) || HUONG_DAN_CAI}`));
      }
    });
    p.stdin.end(cauHoi, "utf8");
  });
}
