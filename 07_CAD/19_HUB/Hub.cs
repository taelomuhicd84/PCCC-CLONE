// TKPCCC Hub (07_CAD\bin\TKPCCC.Hub.exe): MỘT chỗ cài mọi thứ plugin cần, chia theo tính năng (người dùng 17/11, tham khảo "MCP Hub Pro"):
//   Nền tảng (AutoCAD, .NET 4.8, plugin tự nạp) · MCP cad (máy chủ + cấu hình cho từng AI) · Bot chat (lệnh claude, agy) · YouTube (WebView2; Brave tuỳ chọn).
// Chạy ngoài AutoCAD, không tham chiếu thư viện Autodesk. Phần mềm của hãng khác: Hub chỉ MỞ TRANG TẢI CHÍNH THỨC, không tự tải, không tự chạy bộ cài.
// Sửa file JSON thì sao lưu <file>.tkpccc_<giờ>.bak trước; Claude Code, Antigravity ghi qua CLI của chính nó (không sửa file của chúng bằng tay:
// ~/.claude.json đang được Claude Code ghi liên tục). Nhật ký: %APPDATA%\TKPCCC\hub.log.
// Dòng lệnh (bộ test 07_CAD\04_KIEM_THU\thu_hub.ps1): /goc <thư mục> = nhà giả (CLI chỉ ghi lệnh, không chạy, không mở trang); /kiem; /cai a,b; /go a,b; /ra <file kết quả>.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using Microsoft.Win32;

namespace TKPCCC
{
    class Muc
    {
        public string Ma, Ten, Nhom, HoiCai, HoiGo;
        public Func<KeyValuePair<int, string>> Kiem; // 1 = xong, 0 = chưa / lỗi, -1 = máy không có phần mềm đó
        public Func<string> Cai, Go;                  // null = không có việc đó
    }

    static class Hub
    {
        public const string NEN = "Nền tảng", MCP = "MCP cad", BOT = "Bot chat", YT = "YouTube";
        public static readonly string[] NHOM = { NEN, MCP, BOT, YT };
        public static readonly Dictionary<string, string> MO_TA_NHOM = new Dictionary<string, string>
        {
            { NEN, "bắt buộc: AutoCAD mở là có plugin" },
            { MCP, "AI ngoài CAD (Claude, Gemini) vẽ thẳng vào bản vẽ" },
            { BOT, "khung chat trong palette PCCC" },
            { YT, "phân hệ YouTube trong palette PCCC" },
        };
        static readonly string[] DLL_WV2 = { "Microsoft.Web.WebView2.Core.dll", "Microsoft.Web.WebView2.WinForms.dll", "WebView2Loader.dll" };

        static string nha = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        static string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        static bool gia;
        static readonly StringBuilder nhatKy = new StringBuilder();
        public static event Action<string> Ghi;

        static string Cad { get { return Path.GetDirectoryName(Path.GetDirectoryName(Application.ExecutablePath)); } }
        static string McpExe { get { return Path.Combine(Cad, "bin", "TKPCCC.Mcp.exe"); } }
        static string Bundle { get { return Path.Combine(Cad, "TKPCCC.bundle"); } }
        static string Contents { get { return Path.Combine(Bundle, "Contents"); } }
        static string LienKet { get { return LienKetCua("TKPCCC.bundle"); } }
        // Người dùng 17/11: tách 3 plugin (PCCC, YouTube, Bot chat), mỗi plugin một bundle, cài / gỡ riêng.
        static string LienKetCua(string bundle) { return Path.Combine(appData, "Autodesk", "ApplicationPlugins", bundle); }
        const string B_YT = "TKPCCC_YouTube.bundle", B_BOT = "TKPCCC_BotChat.bundle";
        static string ContentsYt { get { return Path.Combine(Cad, B_YT, "Contents"); } }
        static string FileClaudeDesktop { get { return Path.Combine(appData, "Claude", "claude_desktop_config.json"); } }
        static string FileGemini { get { return Path.Combine(nha, ".gemini", "settings.json"); } }
        static string FileClaudeCode
        {
            get
            {
                string d = Environment.GetEnvironmentVariable("CLAUDE_CONFIG_DIR");
                return Path.Combine(gia || string.IsNullOrEmpty(d) ? nha : d, ".claude.json");
            }
        }

        public static List<Muc> DanhSach()
        {
            string xin = " Sao lưu file trước khi sửa.";
            const string URL_NET = "https://dotnet.microsoft.com/download/dotnet-framework/net48";
            const string URL_CLAUDE = "https://docs.claude.com/en/docs/claude-code/setup";
            const string URL_AGY = "https://antigravity.google/download";
            const string URL_WV2 = "https://developer.microsoft.com/microsoft-edge/webview2/";
            const string URL_BRAVE = "https://brave.com/download/";
            return new List<Muc>
            {
                // ---- Nền tảng ----
                new Muc { Ma = "acad", Nhom = NEN, Ten = "AutoCAD 2021–2024", Kiem = KiemAcad },
                new Muc { Ma = "net48", Nhom = NEN, Ten = ".NET Framework 4.8", Kiem = KiemNet48,
                    Cai = delegate { return MoTrang(URL_NET); }, HoiCai = "Mở trang tải .NET Framework 4.8 của Microsoft: " + URL_NET },
                new Muc { Ma = "plugin", Nhom = NEN, Ten = "Plugin PCCC (tự nạp)", Kiem = delegate { return KiemPlugin("TKPCCC.bundle", "TKPCCC.Cad.dll"); },
                    Cai = delegate { return CaiPlugin("TKPCCC.bundle", "TKPCCC.Cad.dll"); }, Go = delegate { return GoPlugin("TKPCCC.bundle"); },
                    HoiCai = "Tạo liên kết " + LienKet + " tới " + Bundle + " để AutoCAD tự nạp plugin PCCC mỗi lần mở.",
                    HoiGo = "Gỡ liên kết " + LienKet + " (không xoá file plugin)." },
                // ---- MCP cad ----
                new Muc { Ma = "mcp", Nhom = MCP, Ten = "Máy chủ MCP (TKPCCC.Mcp.exe)", Kiem = KiemMcp },
                new Muc { Ma = "claude_code", Nhom = MCP, Ten = "Claude Code ← MCP cad", Kiem = KiemClaudeCode,
                    Cai = delegate { return ChayCli("claude", "mcp add -s user cad \"" + McpExe + "\""); },
                    Go = delegate { return ChayCli("claude", "mcp remove -s user cad"); },
                    HoiCai = "Chạy: claude mcp add -s user cad \"" + McpExe + "\" (Claude Code ở mọi thư mục gọi được MCP cad).",
                    HoiGo = "Chạy: claude mcp remove -s user cad." },
                new Muc { Ma = "claude_desktop", Nhom = MCP, Ten = "Claude Desktop ← MCP cad", Kiem = delegate { return KiemJson(FileClaudeDesktop, Directory.Exists(Path.GetDirectoryName(FileClaudeDesktop))); },
                    Cai = delegate { return SuaJson(FileClaudeDesktop, true); }, Go = delegate { return SuaJson(FileClaudeDesktop, false); },
                    HoiCai = "Thêm máy chủ cad vào " + FileClaudeDesktop + "." + xin, HoiGo = "Bỏ máy chủ cad khỏi " + FileClaudeDesktop + "." + xin },
                new Muc { Ma = "gemini", Nhom = MCP, Ten = "Gemini CLI ← MCP cad", Kiem = delegate { return KiemJson(FileGemini, TimLenh("gemini") != null); },
                    Cai = delegate { return SuaJson(FileGemini, true); }, Go = delegate { return SuaJson(FileGemini, false); },
                    HoiCai = "Thêm máy chủ cad vào " + FileGemini + "." + xin, HoiGo = "Bỏ máy chủ cad khỏi " + FileGemini + "." + xin },
                new Muc { Ma = "agy", Nhom = MCP, Ten = "Antigravity ← MCP cad", Kiem = KiemAgy,
                    Cai = delegate { return ChayCli("agy", "mcp add cad \"" + McpExe + "\""); },
                    Go = delegate { return ChayCli("agy", "mcp remove cad"); },
                    HoiCai = "Chạy: agy mcp add cad \"" + McpExe + "\".", HoiGo = "Chạy: agy mcp remove cad." },
                // ---- Bot chat ----
                new Muc { Ma = "plugin_bot", Nhom = BOT, Ten = "Plugin Bot chat (lệnh TKBOTCHAT)", Kiem = delegate { return KiemPlugin(B_BOT, "TKPCCC.BotChat.dll"); },
                    Cai = delegate { return CaiPlugin(B_BOT, "TKPCCC.BotChat.dll"); }, Go = delegate { return GoPlugin(B_BOT); },
                    HoiCai = "Tạo liên kết " + LienKetCua(B_BOT) + " tới " + Path.Combine(Cad, B_BOT) + " để AutoCAD tự nạp plugin Bot chat.",
                    HoiGo = "Gỡ liên kết " + LienKetCua(B_BOT) + " (không xoá file plugin)." },
                new Muc { Ma = "cli_claude", Nhom = BOT, Ten = "Claude Code CLI (lệnh claude)", Kiem = delegate { return KiemCli("claude"); },
                    Cai = delegate { return MoTrang(URL_CLAUDE); },
                    HoiCai = "Mở trang hướng dẫn cài Claude Code: " + URL_CLAUDE + ". Cài xong mở terminal gõ claude rồi /login." },
                new Muc { Ma = "cli_agy", Nhom = BOT, Ten = "Antigravity CLI (lệnh agy, Gemini)", Kiem = delegate { return KiemCli("agy"); },
                    Cai = delegate { return MoTrang(URL_AGY); },
                    HoiCai = "Mở trang tải Antigravity: " + URL_AGY + ". Cài xong mở terminal gõ agy để đăng nhập Google." },
                // ---- YouTube ----
                new Muc { Ma = "plugin_yt", Nhom = YT, Ten = "Plugin YouTube (lệnh TKYOUTUBE)", Kiem = delegate { return KiemPlugin(B_YT, "TKPCCC.YouTube.dll"); },
                    Cai = delegate { return CaiPlugin(B_YT, "TKPCCC.YouTube.dll"); }, Go = delegate { return GoPlugin(B_YT); },
                    HoiCai = "Tạo liên kết " + LienKetCua(B_YT) + " tới " + Path.Combine(Cad, B_YT) + " để AutoCAD tự nạp plugin YouTube.",
                    HoiGo = "Gỡ liên kết " + LienKetCua(B_YT) + " (không xoá file plugin)." },
                new Muc { Ma = "wv2_runtime", Nhom = YT, Ten = "Microsoft Edge WebView2 Runtime", Kiem = KiemWebView2,
                    Cai = delegate { return MoTrang(URL_WV2); },
                    HoiCai = "Mở trang tải WebView2 Runtime của Microsoft (chọn Evergreen Bootstrapper): " + URL_WV2 },
                new Muc { Ma = "wv2_dll", Nhom = YT, Ten = "Thư viện WebView2 cạnh plugin (3 DLL)", Kiem = KiemDllWv2, Cai = CaiDllWv2,
                    HoiCai = "Chép 3 DLL WebView2 từ " + Path.Combine(Cad, "18_YOUTUBE", "webview2") + " vào " + ContentsYt + "." },
                new Muc { Ma = "brave", Nhom = YT, Ten = "Brave (tuỳ chọn, xem YouTube cửa sổ riêng)", Kiem = delegate
                    {
                        string b = HangDoi.TimBrave();
                        return b != null ? KQ(1, "có: " + b) : KQ(0, "chưa có (không bắt buộc: tab YouTube dùng WebView2)");
                    },
                    Cai = delegate { return MoTrang(URL_BRAVE); }, HoiCai = "Mở trang tải Brave chính thức: " + URL_BRAVE },
            };
        }

        static KeyValuePair<int, string> KQ(int t, string s) { return new KeyValuePair<int, string>(t, s); }

        public static KeyValuePair<int, string> KiemAnToan(Muc m)
        {
            try { return m.Kiem(); } catch (Exception e) { return KQ(0, "lỗi: " + e.Message); }
        }

        public static bool Lam(Muc m, bool cai)
        {
            Func<string> f = cai ? m.Cai : m.Go;
            if (f == null) { Log(m.Ten + ": không có việc " + (cai ? "cài" : "gỡ")); return false; }
            try { Log((cai ? "CÀI " : "GỠ ") + m.Ten + ": " + f()); return true; }
            catch (Exception e) { Log("LỖI " + m.Ten + ": " + e.Message); return false; }
        }

        static void Log(string s)
        {
            string d = DateTime.Now.ToString("HH:mm:ss") + "  " + s;
            lock (nhatKy) nhatKy.AppendLine(d);
            try
            {
                string f = Path.Combine(appData, "TKPCCC", "hub.log");
                Directory.CreateDirectory(Path.GetDirectoryName(f));
                File.AppendAllText(f, DateTime.Now.ToString("yyyy-MM-dd ") + d + Environment.NewLine, Encoding.UTF8);
            }
            catch (Exception) { } // nhật ký file hỏng không chặn việc cài
            Action<string> g = Ghi;
            if (g != null) g(d);
        }

        static string MoTrang(string url)
        {
            if (gia) return "(thử) mở " + url;
            Process.Start(url);
            return "đã mở " + url + ". Cài xong bấm Quét lại máy.";
        }

        // ---------- Nền tảng ----------

        static KeyValuePair<int, string> KiemAcad()
        {
            string[] ten = { "2021", "2022", "2023", "2024" };
            List<string> co = new List<string>();
            using (RegistryKey k = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Autodesk\AutoCAD"))
                for (int i = 0; k != null && i < ten.Length; i++)
                    using (RegistryKey r = k.OpenSubKey("R24." + i))
                        if (r != null)
                            foreach (string s in r.GetSubKeyNames())
                                using (RegistryKey p = r.OpenSubKey(s))
                                {
                                    string vt = p == null ? null : p.GetValue("AcadLocation") as string;
                                    if (!string.IsNullOrEmpty(vt) && File.Exists(Path.Combine(vt, "acad.exe"))) { co.Add("AutoCAD " + ten[i]); break; }
                                }
            if (co.Count == 0) return KQ(-1, "không thấy AutoCAD 2021–2024 (plugin build cho .NET Framework 4.8)");
            string mo = Process.GetProcessesByName("acad").Length == 0 ? "chưa mở" : PluginDangNoi() ? "đang mở, plugin đang nối" : "đang mở, plugin chưa nối";
            return KQ(1, string.Join(", ", co) + "; " + mo);
        }

        static KeyValuePair<int, string> KiemNet48()
        {
            using (RegistryKey k = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full"))
            {
                object r = k == null ? null : k.GetValue("Release");
                int so = r is int ? (int)r : 0;
                if (so >= 528040) return KQ(1, "có (Release " + so + ")");
                return KQ(0, so == 0 ? "chưa có .NET Framework 4.x" : "bản cũ (Release " + so + "), cần 4.8");
            }
        }

        /// song.json (cầu nối Cau.cs ghi đều) mới hơn 6 s, như Mcp.cs ConSong.
        static bool PluginDangNoi()
        {
            try
            {
                Dictionary<string, object> s = new JavaScriptSerializer().DeserializeObject(File.ReadAllText(HangDoi.Song, Encoding.UTF8)) as Dictionary<string, object>;
                DateTime luc = DateTime.Parse((string)s["luc"], null, System.Globalization.DateTimeStyles.RoundtripKind).ToUniversalTime();
                return (DateTime.UtcNow - luc).TotalSeconds < 6;
            }
            catch (Exception) { return false; }
        }

        /// bundle: tên thư mục bundle trong 07_CAD\ (TKPCCC.bundle, TKPCCC_YouTube.bundle, TKPCCC_BotChat.bundle); dll: DLL chính trong Contents.
        static KeyValuePair<int, string> KiemPlugin(string bundle, string dll)
        {
            string lienKet = LienKetCua(bundle);
            if (!File.Exists(Path.Combine(Cad, bundle, "Contents", dll))) return KQ(0, "chưa build: chạy 07_CAD\\build.bat");
            if (!Directory.Exists(lienKet)) return KQ(0, "chưa cài (AutoCAD chưa tự nạp)");
            bool lk = (File.GetAttributes(lienKet) & FileAttributes.ReparsePoint) != 0;
            if (!File.Exists(Path.Combine(lienKet, "Contents", dll))) return KQ(0, "liên kết hỏng (thư mục dự án đã dời / đổi tên?): bấm Cài để trỏ về đây");
            // Người dùng 26/11: liên kết còn trỏ thư mục dự án cũ (D:\...) nên AutoCAD nạp plugin cũ mà Hub vẫn báo xong.
            if (lk && !CungFile(Path.Combine(Cad, bundle, "Contents", dll), Path.Combine(lienKet, "Contents", dll)))
                return KQ(0, "liên kết trỏ sang bản khác (thư mục dự án cũ?): bấm Cài để trỏ về " + Path.Combine(Cad, bundle));
            return KQ(1, lk ? "đã liên kết, build xong là AutoCAD dùng bản mới" : "có bản chép (không tự theo bản build mới)");
        }

        /// Cùng một file (qua hai đường dẫn): cùng cỡ + cùng giờ ghi.
        static bool CungFile(string a, string b)
        {
            FileInfo x = new FileInfo(a), y = new FileInfo(b);
            return x.Exists && y.Exists && x.Length == y.Length && x.LastWriteTimeUtc == y.LastWriteTimeUtc;
        }

        static string CaiPlugin(string bundle, string dll)
        {
            string goc = Path.Combine(Cad, bundle), lienKet = LienKetCua(bundle), f = Path.Combine(goc, "Contents", dll);
            if (!File.Exists(f)) throw new Exception("chưa có " + f + ": chạy 07_CAD\\build.bat trước");
            if (Directory.Exists(lienKet))
            {
                if ((File.GetAttributes(lienKet) & FileAttributes.ReparsePoint) == 0)
                    return lienKet + " là thư mục thật (bản chép), giữ nguyên: xoá tay rồi Cài lại nếu muốn dùng bản ở đây";
                Directory.Delete(lienKet, false); // liên kết cũ (có thể trỏ thư mục dự án cũ): gỡ rồi tạo lại về đây
            }
            Directory.CreateDirectory(Path.GetDirectoryName(lienKet));
            int ma;
            string ra = Chay("cmd.exe", "/d /c mklink /J \"" + lienKet + "\" \"" + goc + "\"", out ma);
            if (ma != 0 || !Directory.Exists(lienKet)) throw new Exception("mklink lỗi: " + ra);
            // Plugin + Bot chat tìm repo theo file này (build.bat, cai_dat.bat ghi như vậy): ghi lại theo chỗ mới.
            string nguon = Path.Combine(goc, "Contents", "nguon_vat_tu.txt");
            if (File.Exists(nguon)) File.WriteAllText(nguon, Path.Combine(Cad, "..", "03_DU_LIEU", "vat_tu.json") + "\r\n");
            return "đã liên kết " + lienKet + " -> " + goc + ". Tắt hẳn AutoCAD rồi mở lại.";
        }

        static string GoPlugin(string bundle)
        {
            string LienKet = LienKetCua(bundle); // che tên thuộc tính chung: gỡ đúng bundle được chọn
            if (!Directory.Exists(LienKet)) return "chưa có, không làm gì";
            if ((File.GetAttributes(LienKet) & FileAttributes.ReparsePoint) == 0)
                throw new Exception(LienKet + " là thư mục thật, không phải liên kết: không xoá, gỡ tay nếu muốn");
            Directory.Delete(LienKet, false); // chỉ gỡ liên kết, không đụng file trong bundle
            return "đã gỡ liên kết. Mở lại AutoCAD để hết tự nạp.";
        }

        // ---------- MCP cad ----------

        static KeyValuePair<int, string> KiemMcp()
        {
            if (!File.Exists(McpExe)) return KQ(0, "chưa build: chạy 07_CAD\\build.bat");
            return BatTay() ? KQ(1, "bắt tay được (initialize trả tkpccc-cad)") : KQ(0, "không trả lời initialize trong 10 s");
        }

        /// Gửi initialize như một AI, đợi dòng trả lời có tên máy chủ.
        static bool BatTay()
        {
            ProcessStartInfo si = new ProcessStartInfo(McpExe);
            si.UseShellExecute = false;
            si.CreateNoWindow = true;
            si.RedirectStandardInput = si.RedirectStandardOutput = true;
            si.StandardOutputEncoding = new UTF8Encoding(false);
            using (Process p = Process.Start(si))
            {
                try
                {
                    p.StandardInput.WriteLine("{\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"initialize\",\"params\":{\"protocolVersion\":\"2025-06-18\",\"capabilities\":{},\"clientInfo\":{\"name\":\"tkpccc-hub\",\"version\":\"0\"}}}");
                    p.StandardInput.Flush();
                    Task<string> t = p.StandardOutput.ReadLineAsync();
                    return t.Wait(10000) && t.Result != null && t.Result.Contains("\"tkpccc-cad\"");
                }
                finally { try { p.Kill(); } catch (Exception) { } }
            }
        }

        static KeyValuePair<int, string> KiemClaudeCode()
        {
            if (TimLenh("claude") == null) return KQ(-1, "chưa cài Claude Code CLI (nhóm Bot chat)");
            Dictionary<string, object> nhom = NhomMcp(DocJson(FileClaudeCode, true), false);
            return nhom != null && nhom.ContainsKey("cad") ? KQ(1, "đã có cad (phạm vi user)") : KQ(0, "chưa có cad ở phạm vi user (repo 07_CAD\\ vẫn có .mcp.json)");
        }

        static KeyValuePair<int, string> KiemAgy()
        {
            if (TimLenh("agy") == null) return KQ(-1, "chưa cài Antigravity CLI (nhóm Bot chat)");
            if (gia) return KQ(0, "(thử) không chạy agy mcp list");
            int ma;
            string ra = Chay(TimLenh("agy"), "mcp list", out ma);
            return Regex.IsMatch(ra, @"(^|\W)cad(\W|$)", RegexOptions.Multiline) ? KQ(1, "đã có cad") : KQ(0, "chưa có cad");
        }

        static KeyValuePair<int, string> KiemJson(string f, bool coCai)
        {
            Dictionary<string, object> nhom = NhomMcp(DocJson(f, false), false);
            if (nhom != null && nhom.ContainsKey("cad")) return KQ(1, "đã có cad trong " + f);
            return coCai ? KQ(0, "chưa có cad trong " + f) : KQ(-1, "chưa cài phần mềm này");
        }

        static Dictionary<string, object> DocJson(string f, bool chiaSe)
        {
            if (!File.Exists(f)) return new Dictionary<string, object>();
            string s;
            using (FileStream fs = new FileStream(f, FileMode.Open, FileAccess.Read, chiaSe ? FileShare.ReadWrite : FileShare.Read))
            using (StreamReader r = new StreamReader(fs, Encoding.UTF8)) s = r.ReadToEnd();
            if (s.Trim() == "") return new Dictionary<string, object>();
            JavaScriptSerializer js = new JavaScriptSerializer();
            js.MaxJsonLength = int.MaxValue;
            Dictionary<string, object> j;
            try { j = js.DeserializeObject(s) as Dictionary<string, object>; }
            catch (Exception e) { throw new Exception("không đọc được JSON " + f + " (có chú thích?): " + e.Message); }
            if (j == null) throw new Exception(f + " không phải đối tượng JSON");
            return j;
        }

        static Dictionary<string, object> NhomMcp(Dictionary<string, object> j, bool tao)
        {
            object v;
            Dictionary<string, object> n = j.TryGetValue("mcpServers", out v) ? v as Dictionary<string, object> : null;
            if (n == null && tao) j["mcpServers"] = n = new Dictionary<string, object>();
            return n;
        }

        /// Thêm / bỏ mcpServers.cad, giữ nguyên mọi khoá khác; sao lưu trước khi ghi.
        static string SuaJson(string f, bool them)
        {
            Dictionary<string, object> j = DocJson(f, false);
            Dictionary<string, object> n = NhomMcp(j, them);
            if (them) n["cad"] = new Dictionary<string, object> { { "command", McpExe }, { "args", new object[0] } };
            else if (n == null || !n.Remove("cad")) return "không có cad trong " + f + ", giữ nguyên";
            string bak = null;
            if (File.Exists(f)) File.Copy(f, bak = f + ".tkpccc_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".bak", true);
            Directory.CreateDirectory(Path.GetDirectoryName(f));
            JavaScriptSerializer js = new JavaScriptSerializer();
            js.MaxJsonLength = int.MaxValue;
            File.WriteAllText(f, DepJson(js.Serialize(j)) + "\n", new UTF8Encoding(false));
            return (them ? "đã thêm cad vào " : "đã bỏ cad khỏi ") + f + (bak != null ? " (sao lưu " + Path.GetFileName(bak) + ")" : "") + ". Mở lại ứng dụng AI.";
        }

        /// Thụt lề JSON gọn (JavaScriptSerializer ghi một dòng) để người dùng còn đọc, sửa tay được.
        static string DepJson(string s)
        {
            StringBuilder b = new StringBuilder();
            int thut = 0;
            bool chuoi = false;
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                if (chuoi)
                {
                    b.Append(c);
                    if (c == '\\') b.Append(s[++i]);
                    else if (c == '"') chuoi = false;
                    continue;
                }
                if (c == '"') { chuoi = true; b.Append(c); }
                else if ((c == '{' || c == '[') && i + 1 < s.Length && (s[i + 1] == '}' || s[i + 1] == ']')) { b.Append(c).Append(s[++i]); }
                else if (c == '{' || c == '[') b.Append(c).Append('\n').Append(' ', 2 * ++thut);
                else if (c == '}' || c == ']') b.Append('\n').Append(' ', 2 * --thut).Append(c);
                else if (c == ',') b.Append(c).Append('\n').Append(' ', 2 * thut);
                else if (c == ':') b.Append(": ");
                else b.Append(c);
            }
            return b.ToString();
        }

        // ---------- Bot chat ----------

        static KeyValuePair<int, string> KiemCli(string ten)
        {
            string f = TimLenh(ten);
            if (f == null) return KQ(0, "chưa cài: bấm Cài để mở trang tải chính thức");
            if (gia) return KQ(1, f);
            int ma;
            string ra = Chay(f, "--version", out ma);
            string ban = ma == 0 && ra != "" ? ra.Split('\n')[0].Trim() + " · " : "";
            return KQ(1, ban + f + (ten == "claude" ? " (cần đăng nhập: claude rồi /login)" : ""));
        }

        // ---------- YouTube ----------

        static KeyValuePair<int, string> KiemWebView2()
        {
            const string ID = "{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}"; // mã sản phẩm WebView2 Runtime (tài liệu Microsoft)
            foreach (RegistryKey goc in new[] { Registry.LocalMachine, Registry.CurrentUser })
                foreach (string k in new[] { @"SOFTWARE\WOW6432Node\Microsoft\EdgeUpdate\Clients\" + ID, @"SOFTWARE\Microsoft\EdgeUpdate\Clients\" + ID })
                    using (RegistryKey r = goc.OpenSubKey(k))
                    {
                        string pv = r == null ? null : r.GetValue("pv") as string;
                        if (!string.IsNullOrEmpty(pv) && pv != "0.0.0.0") return KQ(1, "có (bản " + pv + ")");
                    }
            return KQ(0, "chưa có: phân hệ YouTube không chạy");
        }

        static KeyValuePair<int, string> KiemDllWv2()
        {
            string[] thieu = DLL_WV2.Where(f => !File.Exists(Path.Combine(ContentsYt, f))).ToArray();
            return thieu.Length == 0 ? KQ(1, "đủ 3 file cạnh TKPCCC.YouTube.dll") : KQ(0, "thiếu " + string.Join(", ", thieu));
        }

        static string CaiDllWv2()
        {
            string nguon = Path.Combine(Cad, "18_YOUTUBE", "webview2");
            Directory.CreateDirectory(ContentsYt);
            int n = 0;
            foreach (string f in DLL_WV2)
            {
                string dich = Path.Combine(ContentsYt, f);
                if (File.Exists(dich)) continue;
                if (!File.Exists(Path.Combine(nguon, f))) throw new Exception("không thấy " + Path.Combine(nguon, f));
                File.Copy(Path.Combine(nguon, f), dich);
                n++;
            }
            return n == 0 ? "đã đủ, giữ nguyên" : "đã chép " + n + " file vào " + ContentsYt + ". Mở lại AutoCAD.";
        }

        // ---------- Chạy lệnh ----------

        /// Tìm lệnh trên PATH và nơi cài mặc định (claude: ~/.local/bin, agy: %LOCALAPPDATA%\agy\bin). null = chưa cài.
        static string TimLenh(string ten)
        {
            List<string> thu = (Environment.GetEnvironmentVariable("PATH") ?? "").Split(';').Where(d => d.Trim() != "").ToList();
            thu.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "bin"));
            thu.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "agy", "bin"));
            foreach (string duoi in new[] { ".exe", ".cmd" })
                foreach (string d in thu)
                {
                    try
                    {
                        string f = Path.Combine(d.Trim().Trim('"'), ten + duoi);
                        if (File.Exists(f)) return f;
                    }
                    catch (ArgumentException) { } // mục PATH có ký tự lạ
                }
            return null;
        }

        static string ChayCli(string ten, string doiSo)
        {
            string exe = TimLenh(ten);
            if (exe == null) throw new Exception("chưa cài " + ten + " (cài ở nhóm Bot chat trước)");
            if (gia) return "(thử) " + ten + " " + doiSo;
            int ma;
            string ra = Chay(exe, doiSo, out ma);
            if (ma != 0) throw new Exception(ten + " " + doiSo + " thoát mã " + ma + ": " + ra);
            return ten + " " + doiSo + ": " + (ra == "" ? "xong" : ra) + ". Mở lại ứng dụng AI.";
        }

        static string Chay(string exe, string doiSo, out int ma)
        {
            ProcessStartInfo si = exe.EndsWith(".cmd", StringComparison.OrdinalIgnoreCase)
                ? new ProcessStartInfo("cmd.exe", "/d /s /c \"\"" + exe + "\" " + doiSo + "\"") : new ProcessStartInfo(exe, doiSo);
            si.UseShellExecute = false;
            si.CreateNoWindow = true;
            si.RedirectStandardInput = true; // CLI không được ngồi chờ bàn phím
            si.RedirectStandardOutput = si.RedirectStandardError = true;
            si.StandardOutputEncoding = si.StandardErrorEncoding = new UTF8Encoding(false);
            si.EnvironmentVariables["NO_COLOR"] = "1";
            using (Process p = Process.Start(si))
            {
                p.StandardInput.Close();
                Task<string> loi = p.StandardError.ReadToEndAsync();
                Task<string> ra = p.StandardOutput.ReadToEndAsync();
                if (!p.WaitForExit(60000)) { try { p.Kill(); } catch (Exception) { } ma = -1; return "quá 60 s"; }
                ma = p.ExitCode;
                return (ra.Result + loi.Result).Trim();
            }
        }

        [STAThread]
        static int Main(string[] a)
        {
            string file = null;
            List<string> cai = null, go = null;
            bool kiem = false;
            for (int i = 0; i < a.Length; i++)
            {
                string k = a[i].ToLowerInvariant(), v = i + 1 < a.Length ? a[i + 1] : "";
                if (k == "/goc") { nha = Path.GetFullPath(v); appData = Path.Combine(nha, "AppData", "Roaming"); gia = true; i++; }
                else if (k == "/ra") { file = v; i++; }
                else if (k == "/cai") { cai = v.Split(',').ToList(); i++; }
                else if (k == "/go") { go = v.Split(',').ToList(); i++; }
                else if (k == "/kiem") kiem = true;
            }
            if (cai == null && go == null && !kiem)
            {
                Application.EnableVisualStyles();
                Application.Run(new CuaSoHub());
                return 0;
            }
            bool ok = true;
            List<Muc> ds = DanhSach();
            foreach (Muc m in ds)
            {
                if (cai != null && cai.Contains(m.Ma)) ok &= Lam(m, true);
                if (go != null && go.Contains(m.Ma)) ok &= Lam(m, false);
            }
            if (kiem)
                foreach (Muc m in ds)
                {
                    KeyValuePair<int, string> k = KiemAnToan(m);
                    Log("KIEM " + m.Ma + " " + k.Key + " " + k.Value);
                }
            if (file != null) File.WriteAllText(file, nhatKy.ToString(), new UTF8Encoding(false));
            return ok ? 0 : 1;
        }
    }

    /// Cửa sổ Hub: hàng nút lọc theo nhóm, bảng (chọn · thành phần · dùng cho · trên máy · chi tiết), nút Cài / Gỡ mục đã chọn, nhật ký.
    class CuaSoHub : Form
    {
        static readonly Color XANH = Color.FromArgb(22, 163, 74), CAM = Color.FromArgb(234, 88, 12), XAM = Color.FromArgb(120, 128, 140), NHAN = Color.FromArgb(37, 99, 235);
        readonly List<Muc> ds = Hub.DanhSach();
        readonly int[] trangThai;
        readonly DataGridView bang = new DataGridView();
        readonly FlowLayoutPanel loc = new FlowLayoutPanel();
        readonly TextBox nk = new TextBox();
        readonly Label tong = new Label();
        readonly List<Button> nut = new List<Button>();
        string dangLoc;

        public CuaSoHub()
        {
            Text = "TKPCCC Hub — cài đặt theo tính năng";
            ClientSize = new Size(980, 640);
            MinimumSize = new Size(760, 480);
            StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Segoe UI", 9.5f);
            BackColor = Color.White;
            trangThai = new int[ds.Count];

            Label dau = new Label();
            dau.Dock = DockStyle.Top;
            dau.Height = 54;
            dau.Padding = new Padding(12, 8, 12, 0);
            dau.Font = new Font("Segoe UI", 9.5f);
            dau.Text = "TKPCCC HUB — mọi thứ plugin PCCC cần, chia theo tính năng. Tích các mục rồi bấm Cài.\n"
                + "Phần mềm của hãng khác (Microsoft, Anthropic, Google): Hub chỉ mở trang tải chính thức. Mọi lần sửa cấu hình đều hỏi lại và có sao lưu.";

            loc.Dock = DockStyle.Top;
            loc.Height = 38;
            loc.Padding = new Padding(10, 4, 10, 0);

            bang.Dock = DockStyle.Fill;
            bang.AllowUserToAddRows = bang.AllowUserToDeleteRows = bang.AllowUserToResizeRows = false;
            bang.RowHeadersVisible = false;
            bang.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            bang.BackgroundColor = Color.White;
            bang.BorderStyle = BorderStyle.FixedSingle;
            bang.GridColor = Color.FromArgb(226, 232, 240);
            bang.EnableHeadersVisualStyles = false;
            bang.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(241, 245, 249);
            bang.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            bang.ColumnHeadersHeight = 30;
            bang.RowTemplate.Height = 30;
            bang.DefaultCellStyle.SelectionBackColor = Color.FromArgb(219, 234, 254);
            bang.DefaultCellStyle.SelectionForeColor = Color.Black;
            bang.Columns.Add(new DataGridViewCheckBoxColumn { HeaderText = "Chọn", Width = 50 });
            bang.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Thành phần", Width = 250, ReadOnly = true });
            bang.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Dùng cho", Width = 110, ReadOnly = true });
            bang.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Trên máy", Width = 120, ReadOnly = true });
            bang.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Chi tiết", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, ReadOnly = true });
            foreach (Muc m in ds) bang.Rows.Add(false, m.Ten, m.Nhom, "đang quét...", "");
            bang.CurrentCellDirtyStateChanged += delegate { if (bang.IsCurrentCellDirty) bang.CommitEdit(DataGridViewDataErrorContexts.Commit); DemChon(); };

            FlowLayoutPanel duoi = new FlowLayoutPanel();
            duoi.Dock = DockStyle.Bottom;
            duoi.Height = 46;
            duoi.Padding = new Padding(10, 6, 10, 0);
            Button cai = Nut("Cài mục đã chọn", 150, delegate { LamChon(true); });
            cai.BackColor = NHAN;
            cai.ForeColor = Color.White;
            cai.FlatStyle = FlatStyle.Flat;
            duoi.Controls.AddRange(new Control[]
            {
                cai,
                Nut("Gỡ mục đã chọn", 130, delegate { LamChon(false); }),
                Nut("Chọn mục còn thiếu", 140, ChonThieu),
                Nut("Quét lại máy", 110, delegate { Quet(); }),
                Nut("Mở nhật ký", 100, delegate
                {
                    string f = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TKPCCC", "hub.log");
                    if (File.Exists(f)) Process.Start(f);
                }),
            });
            tong.AutoSize = true;
            tong.Margin = new Padding(12, 8, 0, 0);
            tong.ForeColor = XAM;
            duoi.Controls.Add(tong);

            nk.Multiline = true;
            nk.ReadOnly = true;
            nk.ScrollBars = ScrollBars.Vertical;
            nk.Dock = DockStyle.Bottom;
            nk.Height = 120;
            nk.Font = new Font("Consolas", 9f);
            nk.BackColor = Color.FromArgb(248, 250, 252);

            Controls.Add(bang);
            Controls.Add(duoi);
            Controls.Add(nk);
            Controls.Add(loc);
            Controls.Add(dau);
            TaoLoc();
            Hub.Ghi += delegate(string s) { try { BeginInvoke((Action)delegate { nk.AppendText(s + Environment.NewLine); }); } catch (Exception) { } };
            Shown += delegate { Quet(); };
        }

        Button Nut(string chu, int rong, Action viec)
        {
            Button n = new Button { Text = chu, Width = rong, Height = 30, Margin = new Padding(0, 0, 8, 0) };
            n.Click += delegate { viec(); };
            nut.Add(n);
            return n;
        }

        /// Nút lọc: "Tất cả (n)" + mỗi nhóm "Tên (n)"; di chuột hiện nhóm đó dùng cho việc gì.
        void TaoLoc()
        {
            ToolTip goiY = new ToolTip();
            foreach (string nh in new[] { (string)null }.Concat(Hub.NHOM))
            {
                string n = nh;
                RadioButton r = new RadioButton
                {
                    Appearance = Appearance.Button, AutoSize = true, Margin = new Padding(0, 0, 6, 0), FlatStyle = FlatStyle.Flat, Checked = n == null,
                    Text = (n ?? "Tất cả") + " (" + (n == null ? ds.Count : ds.Count(m => m.Nhom == n)) + ")",
                };
                r.FlatAppearance.CheckedBackColor = Color.FromArgb(219, 234, 254);
                r.CheckedChanged += delegate { if (r.Checked) { dangLoc = n; ApLoc(); } };
                if (n != null) goiY.SetToolTip(r, Hub.MO_TA_NHOM[n]);
                loc.Controls.Add(r);
            }
        }

        void ApLoc()
        {
            bang.CurrentCell = null; // dòng đang chọn bị ẩn thì DataGridView báo lỗi
            for (int i = 0; i < ds.Count; i++) bang.Rows[i].Visible = dangLoc == null || ds[i].Nhom == dangLoc;
        }

        void Khoa(bool khoa) { foreach (Button n in nut) n.Enabled = !khoa; }

        void DemChon()
        {
            int chon = Enumerable.Range(0, ds.Count).Count(i => (bool)(bang.Rows[i].Cells[0].Value ?? false));
            int xong = trangThai.Count(t => t > 0);
            tong.Text = "Đã chọn " + chon + " mục  ·  " + xong + "/" + ds.Count + " mục đã xong";
        }

        void Quet()
        {
            Khoa(true);
            for (int i = 0; i < ds.Count; i++) { bang.Rows[i].Cells[3].Value = "đang quét..."; bang.Rows[i].Cells[3].Style.ForeColor = XAM; }
            ThreadPool.QueueUserWorkItem(delegate
            {
                for (int i = 0; i < ds.Count; i++)
                {
                    int ii = i;
                    KeyValuePair<int, string> k = Hub.KiemAnToan(ds[i]);
                    BeginInvoke((Action)delegate
                    {
                        trangThai[ii] = k.Key;
                        DataGridViewCell c = bang.Rows[ii].Cells[3];
                        c.Value = k.Key > 0 ? "● Đã xong" : k.Key == 0 ? "○ Chưa / lỗi" : "– Máy chưa có";
                        c.Style.ForeColor = k.Key > 0 ? XANH : k.Key == 0 ? CAM : XAM;
                        bang.Rows[ii].Cells[4].Value = k.Value;
                        bang.Rows[ii].Cells[4].ToolTipText = k.Value;
                        DemChon();
                    });
                }
                BeginInvoke((Action)delegate { Khoa(false); bang.ClearSelection(); });
            });
        }

        void ChonThieu()
        {
            for (int i = 0; i < ds.Count; i++) bang.Rows[i].Cells[0].Value = trangThai[i] == 0 && ds[i].Cai != null && bang.Rows[i].Visible;
            DemChon();
        }

        void LamChon(bool cai)
        {
            List<Muc> chon = Enumerable.Range(0, ds.Count).Where(i => (bool)(bang.Rows[i].Cells[0].Value ?? false))
                .Select(i => ds[i]).Where(m => (cai ? m.Cai : m.Go) != null).ToList();
            if (chon.Count == 0) { MessageBox.Show(this, "Chưa tích mục nào " + (cai ? "cài" : "gỡ") + " được.", "TKPCCC Hub"); return; }
            string hoi = string.Join("\n\n", chon.Select(m => "• " + m.Ten + ": " + (cai ? m.HoiCai : m.HoiGo)));
            if (MessageBox.Show(this, (cai ? "Sẽ làm:\n\n" : "Sẽ gỡ:\n\n") + hoi + "\n\nĐồng ý?", "TKPCCC Hub", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            Khoa(true);
            ThreadPool.QueueUserWorkItem(delegate
            {
                foreach (Muc m in chon) Hub.Lam(m, cai);
                BeginInvoke((Action)delegate
                {
                    foreach (DataGridViewRow r in bang.Rows) r.Cells[0].Value = false;
                    Quet();
                });
            });
        }
    }
}
