// TKPCCC Hub (cad\bin\TKPCCC.Hub.exe): cài MỘT lần để mở AutoCAD là có plugin, và các AI trên máy (Claude Code, Claude Desktop, Gemini CLI,
// Antigravity) gọi được MCP cad. Chạy ngoài AutoCAD, không tham chiếu thư viện Autodesk.
// Sửa file JSON thì sao lưu <file>.tkpccc_<giờ>.bak trước; Claude Code, Antigravity ghi qua CLI của chính nó (không sửa file của chúng bằng tay:
// ~/.claude.json đang được Claude Code ghi liên tục). Nhật ký: %APPDATA%\TKPCCC\hub.log.
// Dòng lệnh (bộ test cad\test\thu_hub.ps1): /goc <thư mục> = nhà giả (CLI chỉ ghi lệnh, không chạy); /kiem; /cai a,b; /go a,b; /ra <file kết quả>.
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
        public string Ma, Ten, HoiCai, HoiGo;
        public Func<KeyValuePair<int, string>> Kiem; // 1 = xong, 0 = chưa / lỗi, -1 = máy không có
        public Func<string> Cai, Go;                  // null = mục chỉ kiểm
    }

    static class Hub
    {
        static string nha = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        static string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        static bool gia;
        static readonly StringBuilder nhatKy = new StringBuilder();
        public static event Action<string> Ghi;

        static string Cad { get { return Path.GetDirectoryName(Path.GetDirectoryName(Application.ExecutablePath)); } }
        static string McpExe { get { return Path.Combine(Cad, "bin", "TKPCCC.Mcp.exe"); } }
        static string Bundle { get { return Path.Combine(Cad, "TKPCCC.bundle"); } }
        static string LienKet { get { return Path.Combine(appData, "Autodesk", "ApplicationPlugins", "TKPCCC.bundle"); } }
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
            return new List<Muc>
            {
                new Muc { Ma = "acad", Ten = "AutoCAD 2021–2024", Kiem = KiemAcad },
                new Muc { Ma = "plugin", Ten = "Plugin tự nạp", Kiem = KiemPlugin, Cai = CaiPlugin, Go = GoPlugin,
                    HoiCai = "Tạo liên kết\n" + LienKet + "\ntới\n" + Bundle + "\nđể AutoCAD tự nạp plugin mỗi lần mở?",
                    HoiGo = "Gỡ liên kết\n" + LienKet + "\n(không xoá file plugin trong repo)?" },
                new Muc { Ma = "mcp", Ten = "Máy chủ MCP cad", Kiem = KiemMcp },
                new Muc { Ma = "claude_code", Ten = "Claude Code", Kiem = KiemClaudeCode,
                    Cai = delegate { return ChayCli("claude", "mcp add -s user cad \"" + McpExe + "\""); },
                    Go = delegate { return ChayCli("claude", "mcp remove -s user cad"); },
                    HoiCai = "Chạy: claude mcp add -s user cad \"" + McpExe + "\"\n(Claude Code ở mọi thư mục gọi được MCP cad)?",
                    HoiGo = "Chạy: claude mcp remove -s user cad?" },
                new Muc { Ma = "claude_desktop", Ten = "Claude Desktop", Kiem = delegate { return KiemJson(FileClaudeDesktop, Directory.Exists(Path.GetDirectoryName(FileClaudeDesktop))); },
                    Cai = delegate { return SuaJson(FileClaudeDesktop, true); }, Go = delegate { return SuaJson(FileClaudeDesktop, false); },
                    HoiCai = "Thêm máy chủ cad vào\n" + FileClaudeDesktop + "?" + xin, HoiGo = "Bỏ máy chủ cad khỏi\n" + FileClaudeDesktop + "?" + xin },
                new Muc { Ma = "gemini", Ten = "Gemini CLI", Kiem = delegate { return KiemJson(FileGemini, TimLenh("gemini") != null); },
                    Cai = delegate { return SuaJson(FileGemini, true); }, Go = delegate { return SuaJson(FileGemini, false); },
                    HoiCai = "Thêm máy chủ cad vào\n" + FileGemini + "?" + xin, HoiGo = "Bỏ máy chủ cad khỏi\n" + FileGemini + "?" + xin },
                new Muc { Ma = "agy", Ten = "Antigravity (agy)", Kiem = KiemAgy,
                    Cai = delegate { return ChayCli("agy", "mcp add cad \"" + McpExe + "\""); },
                    Go = delegate { return ChayCli("agy", "mcp remove cad"); },
                    HoiCai = "Chạy: agy mcp add cad \"" + McpExe + "\"\n(Gemini trong Antigravity và bot chat gọi được MCP cad)?",
                    HoiGo = "Chạy: agy mcp remove cad?" },
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
            if (f == null) { Log(m.Ten + ": mục này chỉ kiểm"); return false; }
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

        // ---------- AutoCAD, plugin, MCP ----------

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

        static string DllBundle { get { return Path.Combine(Bundle, "Contents", "TKPCCC.Cad.dll"); } }

        static KeyValuePair<int, string> KiemPlugin()
        {
            if (!File.Exists(DllBundle)) return KQ(0, "chưa build: chạy cad\\build.bat");
            if (!Directory.Exists(LienKet)) return KQ(0, "chưa cài (AutoCAD chưa tự nạp)");
            bool lk = (File.GetAttributes(LienKet) & FileAttributes.ReparsePoint) != 0;
            if (!File.Exists(Path.Combine(LienKet, "Contents", "TKPCCC.Cad.dll"))) return KQ(0, "liên kết hỏng: Gỡ rồi Cài lại");
            return KQ(1, lk ? "đã liên kết, build xong là AutoCAD dùng bản mới" : "có bản chép (không tự theo bản build mới)");
        }

        static string CaiPlugin()
        {
            if (!File.Exists(DllBundle)) throw new Exception("chưa có " + DllBundle + ": chạy cad\\build.bat trước");
            if (Directory.Exists(LienKet)) return "đã có " + LienKet + ", giữ nguyên";
            Directory.CreateDirectory(Path.GetDirectoryName(LienKet));
            int ma;
            string ra = Chay("cmd.exe", "/d /c mklink /J \"" + LienKet + "\" \"" + Bundle + "\"", out ma);
            if (ma != 0 || !Directory.Exists(LienKet)) throw new Exception("mklink lỗi: " + ra);
            return "đã liên kết " + LienKet + " -> " + Bundle + ". Tắt hẳn AutoCAD rồi mở lại.";
        }

        static string GoPlugin()
        {
            if (!Directory.Exists(LienKet)) return "chưa có, không làm gì";
            if ((File.GetAttributes(LienKet) & FileAttributes.ReparsePoint) == 0)
                throw new Exception(LienKet + " là thư mục thật, không phải liên kết: không xoá, gỡ tay nếu muốn");
            Directory.Delete(LienKet, false); // chỉ gỡ liên kết, không đụng file trong bundle
            return "đã gỡ liên kết. Mở lại AutoCAD để hết tự nạp.";
        }

        static KeyValuePair<int, string> KiemMcp()
        {
            if (!File.Exists(McpExe)) return KQ(0, "chưa build: chạy cad\\build.bat");
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

        // ---------- AI ----------

        static KeyValuePair<int, string> KiemClaudeCode()
        {
            if (TimLenh("claude") == null) return KQ(-1, "chưa cài Claude Code");
            Dictionary<string, object> nhom = NhomMcp(DocJson(FileClaudeCode, true), false);
            return nhom != null && nhom.ContainsKey("cad") ? KQ(1, "đã có cad (phạm vi user)") : KQ(0, "chưa có cad ở phạm vi user (repo cad\\ vẫn có .mcp.json)");
        }

        static KeyValuePair<int, string> KiemAgy()
        {
            if (TimLenh("agy") == null) return KQ(-1, "chưa cài Antigravity CLI");
            if (gia) return KQ(0, "(thử) không chạy agy mcp list");
            int ma;
            string ra = Chay(TimLenh("agy"), "mcp list", out ma);
            return Regex.IsMatch(ra, @"(^|\W)cad(\W|$)", RegexOptions.Multiline) ? KQ(1, "đã có cad") : KQ(0, "chưa có cad");
        }

        static KeyValuePair<int, string> KiemJson(string f, bool coCai)
        {
            Dictionary<string, object> nhom = NhomMcp(DocJson(f, false), false);
            if (nhom != null && nhom.ContainsKey("cad")) return KQ(1, "đã có cad trong " + f);
            return coCai ? KQ(0, "chưa có cad trong " + f) : KQ(-1, "chưa cài");
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
            if (exe == null) throw new Exception("chưa cài " + ten);
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
            si.RedirectStandardOutput = si.RedirectStandardError = true;
            si.StandardOutputEncoding = si.StandardErrorEncoding = new UTF8Encoding(false);
            si.EnvironmentVariables["NO_COLOR"] = "1";
            using (Process p = Process.Start(si))
            {
                Task<string> loi = p.StandardError.ReadToEndAsync();
                string ra = p.StandardOutput.ReadToEnd();
                if (!p.WaitForExit(60000)) { try { p.Kill(); } catch (Exception) { } ma = -1; return "quá 60 s"; }
                ma = p.ExitCode;
                return (ra + loi.Result).Trim();
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

    /// Cửa sổ Hub: mỗi mục một hàng (đèn · tên · chi tiết · Cài · Gỡ), nhật ký bên dưới.
    class CuaSoHub : Form
    {
        static readonly Color XANH = Color.FromArgb(22, 163, 74), CAM = Color.FromArgb(234, 88, 12), XAM = Color.FromArgb(156, 163, 175);
        readonly List<Muc> ds = Hub.DanhSach();
        readonly List<Label> den = new List<Label>(), chiTiet = new List<Label>();
        readonly List<Button> nut = new List<Button>();
        readonly TextBox nk = new TextBox();
        readonly ToolTip goiY = new ToolTip();

        public CuaSoHub()
        {
            Text = "TKPCCC Hub — cài plugin + MCP cad";
            ClientSize = new Size(820, 560);
            MinimumSize = new Size(600, 420);
            StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Segoe UI", 9.5f);
            BackColor = Color.White;

            Label dau = new Label();
            dau.Dock = DockStyle.Top;
            dau.Height = 52;
            dau.Padding = new Padding(12, 8, 12, 0);
            dau.Text = "Cài một lần: AutoCAD tự nạp plugin TKPCCC, các AI trên máy gọi được MCP cad để vẽ.\n"
                + "Xanh = xong, cam = chưa / lỗi, xám = máy chưa cài phần mềm đó. Mỗi lần Cài / Gỡ đều hỏi lại trước khi sửa.";

            TableLayoutPanel b = new TableLayoutPanel();
            b.Dock = DockStyle.Top;
            b.AutoSize = true;
            b.ColumnCount = 5;
            b.Padding = new Padding(10, 4, 10, 4);
            b.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 26));
            b.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160));
            b.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            b.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 64));
            b.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 64));
            foreach (Muc m in ds)
            {
                Muc mm = m;
                int hang = b.RowCount++;
                b.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
                Label d = new Label { Text = "●", ForeColor = XAM, Font = new Font("Segoe UI", 13f), Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter };
                Label t = new Label { Text = m.Ten, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
                Label c = new Label { Text = "đang kiểm...", ForeColor = Color.DimGray, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, AutoEllipsis = true };
                den.Add(d);
                chiTiet.Add(c);
                b.Controls.Add(d, 0, hang);
                b.Controls.Add(t, 1, hang);
                b.Controls.Add(c, 2, hang);
                if (m.Cai != null)
                {
                    b.Controls.Add(Nut("Cài", delegate { Bam(mm, true); }), 3, hang);
                    b.Controls.Add(Nut("Gỡ", delegate { Bam(mm, false); }), 4, hang);
                }
            }

            FlowLayoutPanel duoi = new FlowLayoutPanel();
            duoi.Dock = DockStyle.Top;
            duoi.Height = 40;
            duoi.Padding = new Padding(12, 4, 12, 0);
            Button kl = Nut("Kiểm tra lại", delegate { KiemLai(); });
            kl.Width = 110;
            Button mo = Nut("Mở nhật ký", delegate
            {
                string f = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TKPCCC", "hub.log");
                if (File.Exists(f)) Process.Start(f);
            });
            mo.Width = 110;
            duoi.Controls.AddRange(new Control[] { kl, mo });

            nk.Multiline = true;
            nk.ReadOnly = true;
            nk.ScrollBars = ScrollBars.Vertical;
            nk.Dock = DockStyle.Fill;
            nk.Font = new Font("Consolas", 9f);
            nk.BackColor = Color.FromArgb(248, 250, 252);

            Controls.Add(nk);
            Controls.Add(duoi);
            Controls.Add(b);
            Controls.Add(dau);
            Hub.Ghi += delegate(string s) { try { BeginInvoke((Action)delegate { nk.AppendText(s + Environment.NewLine); }); } catch (Exception) { } };
            Shown += delegate { KiemLai(); };
        }

        Button Nut(string chu, Action viec)
        {
            Button n = new Button { Text = chu, Width = 58, Height = 28, FlatStyle = FlatStyle.System, Anchor = AnchorStyles.Left };
            n.Click += delegate { viec(); };
            nut.Add(n);
            return n;
        }

        void Khoa(bool khoa) { foreach (Button n in nut) n.Enabled = !khoa; }

        void KiemLai()
        {
            Khoa(true);
            for (int i = 0; i < ds.Count; i++) { den[i].ForeColor = XAM; chiTiet[i].Text = "đang kiểm..."; }
            ThreadPool.QueueUserWorkItem(delegate
            {
                for (int i = 0; i < ds.Count; i++)
                {
                    int ii = i;
                    KeyValuePair<int, string> k = Hub.KiemAnToan(ds[i]);
                    BeginInvoke((Action)delegate
                    {
                        den[ii].ForeColor = k.Key > 0 ? XANH : k.Key == 0 ? CAM : XAM;
                        chiTiet[ii].Text = k.Value;
                        goiY.SetToolTip(chiTiet[ii], k.Value);
                    });
                }
                BeginInvoke((Action)delegate { Khoa(false); });
            });
        }

        void Bam(Muc m, bool cai)
        {
            if (MessageBox.Show(this, cai ? m.HoiCai : m.HoiGo, "TKPCCC Hub", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            Khoa(true);
            ThreadPool.QueueUserWorkItem(delegate
            {
                Hub.Lam(m, cai);
                BeginInvoke((Action)delegate { KiemLai(); });
            });
        }
    }
}
