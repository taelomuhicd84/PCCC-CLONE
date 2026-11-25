// Người dùng 18/11 (ảnh khung chat mẫu có "Link VS Code session", chọn dự án): Bot chat nối tiếp phiên của Claude Code (VS Code, CLI, Desktop, chính Bot chat)
// và hội thoại Antigravity; chọn thư mục chứa skill. File này chỉ đọc kho phiên trên máy + dựng cờ CLI, không có giao diện (test/thu_bot.ps1 thử ngoài AutoCAD).
// - Claude: %USERPROFILE%\.claude\projects\<thư mục mã hoá>\<session_id>.jsonl (dòng có cwd, entrypoint; tên ở dòng custom-title / ai-title).
// - Antigravity: %USERPROFILE%\.gemini\antigravity\conversation_summaries.db (SQLite; .NET 4 không có SQLite nên đọc thô bản ghi bảng conversation_summaries).
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;

namespace TKPCCC
{
    class PhienAi
    {
        public int Ai;          // 0 = Claude, 1 = Antigravity (Gemini)
        public string Id, Cwd, Ten, Nguon, File;
        public DateTime Luc;

        public override string ToString()
        {
            string t = Ten.Replace("\r", " ").Replace("\n", " ").Trim();
            if (t.Length > 48) t = t.Substring(0, 47) + "…";
            string duAn = string.IsNullOrEmpty(Cwd) ? "" : " · " + Path.GetFileName(Cwd.TrimEnd('\\', '/'));
            return Nguon + " · " + t + " · " + Luc.ToString("dd/MM HH:mm") + duAn;
        }
    }

    static class KhoPhien
    {
        static readonly JavaScriptSerializer js = new JavaScriptSerializer { MaxJsonLength = int.MaxValue };

        static string Nha() { return Environment.GetFolderPath(Environment.SpecialFolder.UserProfile); }

        static string GocClaude()
        {
            string e = Environment.GetEnvironmentVariable("CLAUDE_CONFIG_DIR");
            return string.IsNullOrEmpty(e) ? Path.Combine(Nha(), ".claude") : e;
        }

        static string DocDoan(string f, long tu, int dai)
        {
            using (FileStream s = new FileStream(f, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
            {
                if (tu > 0) s.Seek(tu, SeekOrigin.Begin);
                byte[] b = new byte[Math.Min(dai, Math.Max(0, s.Length - tu))];
                int n = s.Read(b, 0, b.Length);
                return Encoding.UTF8.GetString(b, 0, n);
            }
        }

        static Dictionary<string, object> Json(string dong)
        {
            if (!dong.StartsWith("{")) return null;
            try { return js.DeserializeObject(dong) as Dictionary<string, object>; } catch (Exception) { return null; }
        }

        static string Chu(Dictionary<string, object> j, string k) { object v; return j.TryGetValue(k, out v) ? v as string : null; }
        static bool Co(Dictionary<string, object> j, string k) { object v; return j.TryGetValue(k, out v) && v is bool && (bool)v; }

        /// Chữ của một dòng user / assistant (chuỗi, hoặc các khối "text"). Bỏ dòng hệ thống (bắt đầu bằng "<": lệnh, nhắc hệ thống).
        static string ChuTin(Dictionary<string, object> j)
        {
            object v;
            Dictionary<string, object> m = j.TryGetValue("message", out v) ? v as Dictionary<string, object> : null;
            if (m == null || !m.TryGetValue("content", out v)) return null;
            string t = v as string;
            if (t == null && v is object[])
                t = string.Join("\n", ((object[])v).OfType<Dictionary<string, object>>().Where(k => Chu(k, "type") == "text").Select(k => Chu(k, "text")));
            t = (t ?? "").Trim();
            return t == "" || t.StartsWith("<") ? null : t;
        }

        static string TenNguon(string e)
        {
            if (e == null) return "Claude";
            if (e.Contains("vscode")) return "VS Code";
            if (e == "cli") return "Claude Code";
            if (e.Contains("desktop")) return "Desktop";
            if (e.StartsWith("sdk")) return "Bot chat/SDK";
            return e;
        }

        /// Phiên Claude Code gần nhất (mọi thư mục dự án), mới nhất trước.
        public static List<PhienAi> Claude(int toiDa)
        {
            List<PhienAi> kq = new List<PhienAi>();
            string goc = Path.Combine(GocClaude(), "projects");
            if (!Directory.Exists(goc)) return kq;
            IEnumerable<FileInfo> ds = Directory.GetDirectories(goc).SelectMany(d => new DirectoryInfo(d).GetFiles("*.jsonl")).OrderByDescending(f => f.LastWriteTimeUtc);
            foreach (FileInfo fi in ds.Take(toiDa * 3))
            {
                PhienAi p = null;
                try { p = DocPhien(fi); } catch (Exception) { } // file đang ghi dở / hỏng: bỏ
                if (p != null) kq.Add(p);
                if (kq.Count >= toiDa) break;
            }
            return kq;
        }

        static PhienAi DocPhien(FileInfo fi)
        {
            PhienAi p = new PhienAi { Ai = 0, Id = Path.GetFileNameWithoutExtension(fi.Name), File = fi.FullName, Luc = fi.LastWriteTime };
            string dauTien = null, nguon = null, tenTay = null, tenAi = null;
            foreach (string dong in DocDoan(fi.FullName, 0, 131072).Split('\n'))
            {
                Dictionary<string, object> j = Json(dong.Trim());
                if (j == null) continue;
                if (p.Cwd == null) p.Cwd = Chu(j, "cwd");
                if (nguon == null) nguon = Chu(j, "entrypoint");
                if (dauTien == null && Chu(j, "type") == "user" && !Co(j, "isMeta") && !Co(j, "isSidechain")) dauTien = ChuTin(j);
            }
            foreach (string dong in DocDoan(fi.FullName, Math.Max(0, fi.Length - 65536), 65536).Split('\n'))
            {
                Dictionary<string, object> j = Json(dong.Trim());
                if (j == null) continue;
                string loai = Chu(j, "type");
                if (loai == "custom-title") tenTay = Chu(j, "customTitle") ?? tenTay;
                else if (loai == "ai-title") tenAi = Chu(j, "aiTitle") ?? tenAi;
            }
            p.Ten = tenTay ?? tenAi ?? dauTien;
            p.Nguon = TenNguon(nguon);
            return p.Ten == null || p.Cwd == null ? null : p;
        }

        /// n tin cuối (true = người dùng) của một phiên Claude, để hiện lại khi link.
        public static List<KeyValuePair<bool, string>> TinCuoi(PhienAi p, int n)
        {
            List<KeyValuePair<bool, string>> kq = new List<KeyValuePair<bool, string>>();
            if (p.Ai != 0 || !File.Exists(p.File)) return kq;
            long dai = new FileInfo(p.File).Length;
            foreach (string dong in DocDoan(p.File, Math.Max(0, dai - 524288), 524288).Split('\n'))
            {
                Dictionary<string, object> j = Json(dong.Trim());
                if (j == null || Co(j, "isMeta") || Co(j, "isSidechain")) continue;
                string loai = Chu(j, "type");
                if (loai != "user" && loai != "assistant") continue;
                string t = ChuTin(j);
                if (t != null) kq.Add(new KeyValuePair<bool, string>(loai == "user", t));
            }
            return kq.Skip(Math.Max(0, kq.Count - n)).ToList();
        }

        // ---------- Antigravity: đọc thô SQLite ----------

        static long Varint(byte[] b, ref int i)
        {
            long v = 0;
            for (int k = 0; k < 9 && i < b.Length; k++)
            {
                byte c = b[i++];
                if (k == 8) return (v << 8) | c;
                v = (v << 7) | (uint)(c & 0x7f);
                if ((c & 0x80) == 0) return v;
            }
            return v;
        }

        static int CoGiaTri(long st)
        {
            if (st >= 12) return (int)((st - 12) / 2);
            return new[] { 0, 1, 2, 3, 4, 6, 8, 8, 0, 0, 0, 0 }[st];
        }

        /// Hội thoại Antigravity (IDE + agy CLI) gần nhất. Bản ghi conversation_summaries: conversation_id (36 chữ), title, preview, step_count,
        /// last_modified_time, workspace_uris, ... Tìm mọi chuỗi id trong file db + wal, nhận bản ghi khi header SQLite ngay trước nó khớp (cột 1 là chữ dài 36).
        /// shortcut: bản ghi tràn trang (preview rất dài) đọc hụt cột sau thì bỏ giờ / thư mục; cần đúng tuyệt đối thì gọi sqlite3.
        public static List<PhienAi> Antigravity(int toiDa)
        {
            Dictionary<string, PhienAi> theoId = new Dictionary<string, PhienAi>();
            string goc = Path.Combine(Nha(), ".gemini", "antigravity", "conversation_summaries.db");
            foreach (string f in new[] { goc, goc + "-wal" })
            {
                if (!File.Exists(f)) continue;
                byte[] b;
                try { using (FileStream s = new FileStream(f, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete)) { b = new byte[s.Length]; s.Read(b, 0, b.Length); } }
                catch (Exception) { continue; }
                string lat = Encoding.GetEncoding(28591).GetString(b); // 1 byte = 1 ký tự: vị trí khớp regex = vị trí byte
                foreach (Match m in Regex.Matches(lat, "[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}"))
                {
                    PhienAi p = DocBanGhi(b, m.Index);
                    PhienAi cu;
                    if (p != null && (!theoId.TryGetValue(p.Id, out cu) || p.Luc >= cu.Luc)) theoId[p.Id] = p;
                }
            }
            return theoId.Values.OrderByDescending(x => x.Luc).Take(toiDa).ToList();
        }

        static PhienAi DocBanGhi(byte[] b, int k)
        {
            for (int h = k - 2; h >= Math.Max(0, k - 40); h--)
            {
                if (b[h] != k - h || b[h + 1] != 85) continue; // cỡ header = khoảng tới id; cột 1: chữ 36 byte (serial 85)
                int i = h + 1;
                List<long> st = new List<long>();
                while (i < k) st.Add(Varint(b, ref i));
                if (i != k || st.Count < 6 || st[1] < 13 || st[1] % 2 == 0 || st[2] < 13 || st[2] % 2 == 0 || st[3] >= 12 || st[4] < 13 || st[5] < 13) continue;
                int[] dai = st.Select(CoGiaTri).ToArray();
                int vt = k;
                string[] chu = new string[6];
                for (int c = 0; c < 6; c++)
                {
                    if (vt + dai[c] > b.Length) return null;
                    if (st[c] >= 13) chu[c] = Encoding.UTF8.GetString(b, vt, dai[c]);
                    vt += dai[c];
                }
                DateTime luc;
                if (!DateTime.TryParse(chu[4].Length >= 19 ? chu[4].Substring(0, 19) : chu[4], CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out luc)) return null;
                string thuMuc = null;
                Match w = Regex.Match(chu[5], "file:///([^\"]+)");
                if (w.Success) thuMuc = Uri.UnescapeDataString(w.Groups[1].Value).Replace('/', '\\');
                string ten = chu[1].Trim() != "" ? chu[1] : chu[2].Trim() != "" ? chu[2] : "(chưa có tên) " + chu[0].Substring(0, 8);
                return new PhienAi { Ai = 1, Id = chu[0], Ten = ten, Cwd = thuMuc, Nguon = "Antigravity", Luc = luc };
            }
            return null;
        }

        /// Một dòng lời nhắn người dùng cho `claude -p --input-format stream-json` (Bot chat ghi vào stdin, kể cả lời nhắn gửi gấp Ctrl+X giữa lượt).
        public static string DongNguoiDung(string chu)
        {
            return js.Serialize(new Dictionary<string, object> { { "type", "user" }, { "message", new Dictionary<string, object> {
                { "role", "user" }, { "content", new object[] { new Dictionary<string, object> { { "type", "text" }, { "text", chu } } } } } } });
        }

        // ---------- Skill + MCP ----------

        /// Bọc một đối số dòng lệnh Windows (luật CommandLineToArgvW): \ đứng trước " hoặc cuối chuỗi nhân đôi, " thành \".
        internal static string Ngoac(string s)
        {
            return "\"" + Regex.Replace(s, "(\\\\*)(\"|\\z)", m => m.Groups[1].Value + m.Groups[1].Value + (m.Groups[2].Value == "\"" ? "\\\"" : "")) + "\"";
        }

        static string Gon(string s)
        {
            string t = Regex.Replace(s.Normalize(NormalizationForm.FormD), "[^A-Za-z0-9_-]+", "-").Trim('-');
            return t == "" ? "skill" : t;
        }

        /// Thư mục người dùng chọn làm "chỗ chứa skill" -> cờ CLI Claude, và tiền tố tên skill (null = không tiền tố).
        /// X/.claude/skills -> --add-dir X (Claude nạp skill của thư mục thêm); X/skills -> --plugin-dir X (plugin tên X: "X:skill");
        /// X chứa thẳng */SKILL.md (vd ~/.claude/skills) -> lớp bọc %LOCALAPPDATA%\TKPCCC\botchat\skill\<tên>\skills (junction tới X), --plugin-dir lớp bọc.
        /// (Đã dò 18/11 trên claude 2.1.326 bằng dòng init: --plugin-dir một thư mục skill trần thì KHÔNG nạp.)
        public static string CoSkill(string x, out string tienTo, out string thuMucSkill)
        {
            tienTo = null;
            thuMucSkill = Path.Combine(x, ".claude", "skills");
            if (Directory.Exists(thuMucSkill)) return " --add-dir " + Ngoac(x);
            thuMucSkill = Path.Combine(x, "skills");
            if (Directory.Exists(thuMucSkill)) { tienTo = Path.GetFileName(x.TrimEnd('\\')); return " --plugin-dir " + Ngoac(x); }
            thuMucSkill = x;
            string ten = Gon(Path.GetFileName(x.TrimEnd('\\'))) + "-" + ((uint)x.ToLowerInvariant().GetHashCode() % 10000).ToString("D4");
            string boc = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TKPCCC", "botchat", "skill", ten);
            if (!Directory.Exists(Path.Combine(boc, "skills")))
            {
                Directory.CreateDirectory(boc);
                ProcessStartInfo si = new ProcessStartInfo("cmd.exe", "/d /c mklink /J \"" + Path.Combine(boc, "skills") + "\" \"" + x.TrimEnd('\\') + "\"");
                si.CreateNoWindow = true;
                si.UseShellExecute = false;
                using (Process p = Process.Start(si)) p.WaitForExit(5000);
            }
            tienTo = ten;
            return " --plugin-dir " + Ngoac(boc);
        }

        /// Tên skill (thư mục con có SKILL.md) trong một thư mục skills, kèm tiền tố plugin nếu có, cho menu "/".
        public static IEnumerable<string> TenSkill(string thuMucSkill, string tienTo)
        {
            if (!Directory.Exists(thuMucSkill)) return new string[0];
            return Directory.GetDirectories(thuMucSkill).Where(d => File.Exists(Path.Combine(d, "SKILL.md")))
                .Select(d => (tienTo == null ? "" : tienTo + ":") + Path.GetFileName(d));
        }

        /// Cấu hình MCP với đường dẫn tuyệt đối (.mcp.json của repo ghi đường tương đối, chỉ đúng khi thư mục làm việc là 07_CAD\).
        /// banVe = bản vẽ của lời nhắn (Document.Name lúc gửi): vào env TKPCCC_CAD_BAN_VE của máy chủ MCP (Claude Code gộp env này với env hệ thống, dò 19/11).
        public static string FileMcp(string cad, string banVe)
        {
            string f = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TKPCCC", "botchat", "mcp.json");
            Directory.CreateDirectory(Path.GetDirectoryName(f));
            Dictionary<string, object> may = new Dictionary<string, object> { { "command", Path.Combine(cad, "bin", "TKPCCC.Mcp.exe") }, { "args", new object[0] } };
            if (banVe != null) may["env"] = new Dictionary<string, object> { { "TKPCCC_CAD_BAN_VE", banVe } };
            System.IO.File.WriteAllText(f, js.Serialize(new Dictionary<string, object> { { "mcpServers", new Dictionary<string, object> { { "cad", may } } } }), new UTF8Encoding(false));
            return f;
        }
    }
}
