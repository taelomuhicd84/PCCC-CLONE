// Chỉ cho test/thu_bot.ps1 (người dùng 18/11): thử module/botchat/PhienAi.cs ngoài AutoCAD.
// 1. Phiên Claude: kho giả (CLAUDE_CONFIG_DIR) một phiên VS Code: tên lấy custom-title (thắng ai-title), bỏ dòng isMeta / "<...>", tin cuối đúng thứ tự.
// 2. Chỗ chứa skill: dự án có .claude\skills -> --add-dir; plugin có skills\ -> --plugin-dir; thư mục skill trần -> lớp bọc junction.
// 3. Antigravity: đọc được danh sách nếu máy có conversation_summaries.db (không có thì bỏ qua).
// 4. Dòng stream-json của Bot chat (KhoPhien.DongNguoiDung, 19/11) qua claude thật với model giả (không tốn hạn mức): claude nhận đúng chữ, có result
//    thì đóng stdin là thoát (như BotChat.DongVao). Lời nhắn gấp chen giữa lượt cần model thật: đã dò tay 19/11 (claude 2.1.326, Haiku: một lượt, một result).
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace TKPCCC
{
    static class ThuBot
    {
        static int loi;

        static void Kiem(bool dk, string ten)
        {
            Console.WriteLine((dk ? "DAT   " : "LOI   ") + ten);
            if (!dk) loi++;
        }

        static void Skill(string thuMuc, string ten)
        {
            string d = Path.Combine(thuMuc, ten);
            Directory.CreateDirectory(d);
            File.WriteAllText(Path.Combine(d, "SKILL.md"), "---\nname: " + ten + "\ndescription: thu\n---\n");
        }

        static int Main(string[] a)
        {
            string tam = a[0];
            Console.OutputEncoding = Encoding.UTF8;
            string cfgGoc = Environment.GetEnvironmentVariable("CLAUDE_CONFIG_DIR");

            // 1. Kho phiên Claude giả.
            string kho = Path.Combine(tam, "claude");
            string duAn = Path.Combine(kho, "projects", "D--du-an");
            Directory.CreateDirectory(duAn);
            string id = "11111111-2222-3333-4444-555555555555";
            File.WriteAllLines(Path.Combine(duAn, id + ".jsonl"), new[]
            {
                "{\"type\":\"user\",\"isMeta\":true,\"cwd\":\"D:\\\\du an\",\"entrypoint\":\"claude-vscode\",\"message\":{\"role\":\"user\",\"content\":\"<command-name>/clear</command-name>\"}}",
                "{\"type\":\"user\",\"cwd\":\"D:\\\\du an\",\"entrypoint\":\"claude-vscode\",\"message\":{\"role\":\"user\",\"content\":\"Vẽ lưới trục tầng 1\"}}",
                "{\"type\":\"assistant\",\"cwd\":\"D:\\\\du an\",\"message\":{\"role\":\"assistant\",\"content\":[{\"type\":\"thinking\",\"thinking\":\"x\"},{\"type\":\"text\",\"text\":\"Đã vẽ 5 trục.\"}]}}",
                "{\"type\":\"user\",\"cwd\":\"D:\\\\du an\",\"message\":{\"role\":\"user\",\"content\":[{\"type\":\"text\",\"text\":\"Thêm trục B\"}]}}",
                "{\"type\":\"ai-title\",\"aiTitle\":\"Lưới trục\",\"sessionId\":\"" + id + "\"}",
                "{\"type\":\"custom-title\",\"customTitle\":\"Tầng 1 KOCHAM\",\"sessionId\":\"" + id + "\"}",
            }, new UTF8Encoding(false));
            Environment.SetEnvironmentVariable("CLAUDE_CONFIG_DIR", kho);
            List<PhienAi> ds = KhoPhien.Claude(5);
            PhienAi p = ds.FirstOrDefault();
            Kiem(ds.Count == 1 && p.Id == id && p.Cwd == "D:\\du an" && p.Nguon == "VS Code" && p.Ten == "Tầng 1 KOCHAM",
                "KhoPhien.Claude: 1 phien VS Code, ten theo custom-title, cwd dung (" + (p == null ? "khong co" : p.Nguon + " | " + p.Ten + " | " + p.Cwd) + ")");
            List<KeyValuePair<bool, string>> tin = p == null ? new List<KeyValuePair<bool, string>>() : KhoPhien.TinCuoi(p, 6);
            Kiem(tin.Count == 3 && tin[0].Key && tin[0].Value == "Vẽ lưới trục tầng 1" && !tin[1].Key && tin[1].Value == "Đã vẽ 5 trục." && tin[2].Value == "Thêm trục B",
                "KhoPhien.TinCuoi: bo dong isMeta, lay chu khoi text, dung thu tu (" + string.Join(" / ", tin.Select(t => t.Value)) + ")");

            // 2. Chỗ chứa skill.
            string x1 = Path.Combine(tam, "du_an"), x2 = Path.Combine(tam, "plug"), x3 = Path.Combine(tam, "kho skill");
            Skill(Path.Combine(x1, ".claude", "skills"), "ska");
            Skill(Path.Combine(x2, "skills"), "skb");
            Skill(x3, "skc");
            string t1, t2, t3, m1, m2, m3;
            string c1 = KhoPhien.CoSkill(x1, out t1, out m1), c2 = KhoPhien.CoSkill(x2, out t2, out m2), c3 = KhoPhien.CoSkill(x3, out t3, out m3);
            Kiem(c1 == " --add-dir \"" + x1 + "\"" && KhoPhien.TenSkill(m1, t1).SequenceEqual(new[] { "ska" }), "CoSkill: du an co .claude\\skills -> --add-dir (" + c1 + ")");
            Kiem(c2 == " --plugin-dir \"" + x2 + "\"" && KhoPhien.TenSkill(m2, t2).SequenceEqual(new[] { "plug:skb" }), "CoSkill: plugin co skills\\ -> --plugin-dir, ten plug:skb (" + c2 + ")");
            string boc = c3.Replace(" --plugin-dir ", "").Trim('"');
            Kiem(c3.StartsWith(" --plugin-dir ") && File.Exists(Path.Combine(boc, "skills", "skc", "SKILL.md")) && KhoPhien.TenSkill(m3, t3).SequenceEqual(new[] { t3 + ":skc" }),
                "CoSkill: thu muc skill tran -> lop boc junction (" + boc + ", ten " + t3 + ":skc)");
            if (Directory.Exists(Path.Combine(boc, "skills"))) Directory.Delete(Path.Combine(boc, "skills"), false); // xoá junction, không đụng thư mục thật
            if (Directory.Exists(boc)) Directory.Delete(boc, true);
            Kiem(File.Exists(Path.Combine(x3, "skc", "SKILL.md")), "xoa lop boc khong dung thu muc skill that");

            // 3. Antigravity (máy thật).
            string db = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".gemini", "antigravity", "conversation_summaries.db");
            if (File.Exists(db))
            {
                List<PhienAi> ag = KhoPhien.Antigravity(10);
                Kiem(ag.Count > 0 && ag.All(g => g.Id.Length == 36 && g.Ten != null && g.Luc.Year > 2000), "KhoPhien.Antigravity: " + ag.Count + " hoi thoai doc tu SQLite");
                foreach (PhienAi g in ag.Take(3)) Console.WriteLine("      " + g + " | " + g.Id);
            }
            else Console.WriteLine("BO QUA Antigravity: may khong co conversation_summaries.db");

            // 4. claude -p --input-format stream-json (Bot chat giữ stdin mở cả lượt để gửi gấp Ctrl+X).
            Environment.SetEnvironmentVariable("CLAUDE_CONFIG_DIR", cfgGoc); // bỏ kho phiên giả của mục 1: claude cần đăng nhập thật
            string claude = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "bin", "claude.exe");
            if (!File.Exists(claude))
                claude = (Environment.GetEnvironmentVariable("PATH") ?? "").Split(';').Where(d => d.Trim() != "")
                    .Select(d => Path.Combine(d.Trim().Trim('"'), "claude.exe")).FirstOrDefault(File.Exists);
            if (claude == null) Console.WriteLine("BO QUA claude stream-json: may khong co claude.exe");
            else ThuStream(claude, tam);

            Console.WriteLine(loi == 0 ? "TAT CA DAT" : "CO " + loi + " LOI");
            return loi;
        }

        static void ThuStream(string claude, string tam)
        {
            string chu = "Bản vẽ \"D:\\du an\\Tầng 1.dwg\"\nvẽ trục A\\B, 3,5 m";
            ProcessStartInfo si = new ProcessStartInfo(claude, "-p --input-format stream-json --output-format stream-json --verbose --replay-user-messages"
                + " --model claude-khong-co-9-9 --setting-sources project,local --tools Read");
            si.UseShellExecute = false;
            si.CreateNoWindow = true;
            si.WorkingDirectory = tam;
            si.RedirectStandardInput = si.RedirectStandardOutput = si.RedirectStandardError = true;
            si.StandardOutputEncoding = new UTF8Encoding(false);
            si.EnvironmentVariables.Remove("ANTHROPIC_API_KEY");
            si.EnvironmentVariables.Remove("CLAUDECODE");
            JavaScriptSerializer js = new JavaScriptSerializer { MaxJsonLength = int.MaxValue };
            using (Process q = Process.Start(si))
            {
                q.ErrorDataReceived += delegate { };
                q.BeginErrorReadLine();
                byte[] b = new UTF8Encoding(false).GetBytes(KhoPhien.DongNguoiDung(chu) + "\n");
                q.StandardInput.BaseStream.Write(b, 0, b.Length);
                q.StandardInput.BaseStream.Flush();
                string nhan = null, khac = "";
                bool coKq = false;
                DateTime het = DateTime.Now.AddSeconds(90);
                while (DateTime.Now < het)
                {
                    Task<string> t = q.StandardOutput.ReadLineAsync();
                    if (!t.Wait(TimeSpan.FromSeconds(60)) || t.Result == null) break;
                    Dictionary<string, object> j = null;
                    try { j = js.DeserializeObject(t.Result) as Dictionary<string, object>; } catch (Exception) { }
                    if (j == null) { khac += t.Result + " | "; continue; }
                    object v;
                    string loai = j.TryGetValue("type", out v) ? v as string : null;
                    if (loai == "user" && j.TryGetValue("message", out v))
                    {
                        object[] nd = ((Dictionary<string, object>)v)["content"] as object[];
                        Dictionary<string, object> k = nd == null ? null : nd.OfType<Dictionary<string, object>>().FirstOrDefault();
                        if (k != null && k.TryGetValue("text", out v)) nhan = v as string;
                    }
                    if (loai == "result") { coKq = true; q.StandardInput.Close(); }
                }
                bool thoat = q.WaitForExit(20000);
                Kiem(nhan == chu, "stream-json: claude nhan dung chu (duong dan \\, ngoac kep, xuong dong, chu Viet)" + (nhan == chu ? "" : ": " + (nhan ?? khac)));
                Kiem(coKq && thoat, "stream-json: co dong result, dong stdin thi claude thoat (result " + coKq + ", thoat " + thoat + ")");
                if (!q.HasExited) q.Kill();
            }
        }
    }
}
