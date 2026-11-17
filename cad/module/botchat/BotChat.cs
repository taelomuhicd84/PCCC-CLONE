// Plugin Bot chat (bundle riêng TKPCCC_BotChat.bundle, lệnh TKBOTCHAT; tách khỏi palette PCCC 17/11): chọn máy AI, model và effort ngay trên khung,
// gắn thẻ thư mục dự án để AI đọc hồ sơ.
// - Claude: `claude -p` (đăng nhập Claude Code, không API key) chạy NỀN ở <repo>\cad với MCP cad, vẽ được vào bản vẽ; giữ hội thoại bằng --resume.
// - Gemini: `agy -p` (Antigravity CLI, đăng nhập Google) chế độ plan = chỉ đọc + trả lời; chưa có MCP cad nên KHÔNG vẽ; giữ hội thoại bằng --conversation.
// Thẻ thư mục: truyền `--add-dir` cho cả hai CLI và ghi danh sách vào đầu lời nhắn. Lưu ở cai_dat.json (chat_ai, model, model_gemini, chat_effort, chat_thu_muc).
// Effort (người dùng 17/11): Claude `--effort low|medium|high|xhigh|max`; Gemini (agy) mức nằm trong tên model (gemini-4-argon-high): ghép gốc + "-" + mức.
// Chạy nền nên luồng AutoCAD không bị khoá, cầu nối Cau.cs vẫn chạy hàng đợi trong lúc chờ.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace TKPCCC
{
    class BotChat : UserControl
    {
        public const string MAU_PDF = "Vẽ theo quy trình 6 bước từ PDF: ";
        public const string MAU_SO_DO = "Vẽ sơ đồ nguyên lý báo cháy từ mặt bằng đang mở";
        const string CAI_CLAUDE = "Không tìm thấy lệnh claude. Cài Claude Code (npm i -g @anthropic-ai/claude-code), mở terminal gõ claude một lần để đăng nhập, rồi thử lại.";
        const string CAI_AGY = "Không tìm thấy agy (Antigravity CLI). Cài Antigravity, mở terminal gõ agy một lần để đăng nhập Google, rồi thử lại.";

        // Người dùng 14/11: không Haiku, không Gemini Flash. Phần tử đầu là mặc định.
        static readonly string[] AI = { "claude", "gemini" };
        static readonly string[] TEN_AI = { "Claude", "Gemini" };
        static readonly string[][] MODEL = {
            new[] { "sonnet", "opus" },
            new[] { "gemini-4-argon", "gemini-4-carbon", "gemini-4-hydrogen" } }; // Gemini: gốc model, mức effort ghép sau (agy models)
        static readonly string[] KHOA_MODEL = { "model", "model_gemini" };
        static readonly string[] EFFORT_CLAUDE = { "low", "medium", "high", "xhigh", "max" };

        /// Mức effort của model đang chọn: Claude theo cờ --effort; Gemini theo tên model agy có (hydrogen có thêm xhigh).
        static string[] Effort(int ai, string model)
        {
            if (ai == 0) return EFFORT_CLAUDE;
            return model.EndsWith("hydrogen") ? new[] { "low", "medium", "high", "xhigh" } : new[] { "low", "medium", "high" };
        }

        readonly RichTextBox hoi = new RichTextBox();
        readonly TextBox nhap = new TextBox();
        readonly NutBo gui = new NutBo("Gửi", GiaoDien.XanhDam);
        readonly Label trangThai = new Label();
        readonly ComboBox chonAi = new ComboBox(), chonModel = new ComboBox(), chonEffort = new ComboBox();
        readonly FlowLayoutPanel the = new FlowLayoutPanel();
        readonly ToolTip goiY = new ToolTip();
        readonly List<string> thuMuc;
        Process p;
        string phien; // session_id (claude) hoặc conversation_id (agy) để nói tiếp
        int aiPhien = -1; // máy AI của `phien`: đổi máy thì mở hội thoại mới
        readonly StringBuilder loi = new StringBuilder();

        public BotChat()
        {
            Font = GiaoDien.Chu;
            BackColor = GiaoDien.Nen;
            thuMuc = GiaoDien.DocCaiDat("chat_thu_muc", "").Split('|').Where(d => d != "").ToList();

            hoi.Dock = DockStyle.Fill;
            hoi.ReadOnly = true;
            hoi.BorderStyle = BorderStyle.None;
            hoi.BackColor = GiaoDien.The;
            hoi.ForeColor = GiaoDien.ChuDam;
            hoi.Font = GiaoDien.Chu;
            Controls.Add(hoi);

            // Đầu khung: chọn máy AI + model.
            FlowLayoutPanel dau = new FlowLayoutPanel();
            dau.Dock = DockStyle.Top;
            dau.AutoSize = true; // khung hẹp thì ô effort xuống dòng, không bị che
            dau.MinimumSize = new Size(0, 34);
            dau.BackColor = GiaoDien.Nen;
            dau.Padding = new Padding(6, 5, 6, 0);
            KieuHop(chonAi, 80);
            KieuHop(chonModel, 150);
            KieuHop(chonEffort, 80);
            goiY.SetToolTip(chonEffort, "Effort: mức suy nghĩ của AI (cao hơn = kỹ hơn, chậm và tốn hạn mức hơn)");
            chonAi.Items.AddRange(TEN_AI);
            chonAi.SelectedIndex = Math.Max(0, Array.IndexOf(AI, GiaoDien.DocCaiDat("chat_ai", AI[0])));
            NapModel();
            chonAi.SelectedIndexChanged += delegate
            {
                GiaoDien.LuuCaiDat("chat_ai", AI[chonAi.SelectedIndex]);
                NapModel();
            };
            chonModel.SelectedIndexChanged += delegate
            {
                if (chonModel.SelectedIndex < 0) return; // Items.Clear() lúc đổi máy AI
                NapEffort();
                LuuModel();
            };
            chonEffort.SelectedIndexChanged += delegate { if (chonEffort.SelectedIndex >= 0) LuuModel(); };
            dau.Controls.AddRange(new Control[] { chonAi, chonModel, chonEffort });
            Controls.Add(dau);

            Panel duoi = new Panel();
            duoi.Dock = DockStyle.Bottom;
            duoi.Height = 186;
            duoi.BackColor = GiaoDien.Nen;
            duoi.Padding = new Padding(6, 6, 6, 6);
            the.Dock = DockStyle.Top;
            the.AutoSize = true;
            the.BackColor = GiaoDien.Nen;
            the.Padding = new Padding(0, 0, 0, 4);
            VeThe();
            nhap.Multiline = true;
            nhap.ScrollBars = ScrollBars.Vertical;
            nhap.Dock = DockStyle.Fill;
            nhap.BackColor = GiaoDien.The;
            nhap.ForeColor = GiaoDien.ChuDam;
            nhap.KeyDown += delegate(object s, KeyEventArgs e) { if (e.KeyCode == Keys.Enter && e.Control) { e.SuppressKeyPress = true; Gui(); } };
            FlowLayoutPanel nut = new FlowLayoutPanel();
            nut.Dock = DockStyle.Bottom;
            nut.Height = 66;
            nut.BackColor = GiaoDien.Nen;
            nut.Padding = new Padding(0, 4, 0, 0);
            gui.Size = new Size(70, 28);
            gui.Click += delegate { Gui(); };
            NutBo moi = new NutBo("Hội thoại mới", GiaoDien.XanhDam);
            moi.Size = new Size(110, 28);
            moi.Click += delegate
            {
                if (p != null) return;
                phien = null;
                hoi.Clear();
                trangThai.Text = "Hội thoại mới.";
            };
            Label mau1 = Mau("Mẫu: 6 bước từ PDF...", delegate
            {
                using (OpenFileDialog d = new OpenFileDialog())
                {
                    d.Filter = "PDF|*.pdf";
                    if (d.ShowDialog() == DialogResult.OK) DatCau(MAU_PDF + d.FileName);
                    else DatCau(MAU_PDF + "<đường dẫn>");
                }
            });
            Label mau2 = Mau("Mẫu: sơ đồ nguyên lý BC", delegate { DatCau(MAU_SO_DO); });
            nut.Controls.AddRange(new Control[] { gui, moi, mau1, mau2 });
            trangThai.Dock = DockStyle.Bottom;
            trangThai.Height = 20;
            trangThai.Font = GiaoDien.Nho;
            trangThai.ForeColor = GiaoDien.ChuMo;
            duoi.Controls.Add(nhap);
            duoi.Controls.Add(the);
            duoi.Controls.Add(nut);
            duoi.Controls.Add(trangThai);
            Controls.Add(duoi);
        }

        static void KieuHop(ComboBox h, int rong)
        {
            h.DropDownStyle = ComboBoxStyle.DropDownList;
            h.FlatStyle = FlatStyle.Flat;
            h.BackColor = GiaoDien.The;
            h.ForeColor = GiaoDien.ChuDam;
            h.Font = GiaoDien.Chu;
            h.Width = rong;
            h.Margin = new Padding(0, 0, 6, 0);
        }

        void NapModel()
        {
            int i = chonAi.SelectedIndex;
            chonModel.Items.Clear();
            chonModel.Items.AddRange(MODEL[i]);
            string luu = GiaoDien.DocCaiDat(KHOA_MODEL[i], MODEL[i][0]);
            if (i == 1) luu = MODEL[1].FirstOrDefault(g => luu.StartsWith(g + "-")) ?? MODEL[1][0]; // "gemini-4-argon-high" -> gốc
            chonModel.SelectedIndex = Math.Max(0, Array.IndexOf(MODEL[i], luu));
            NapEffort();
            trangThai.Text = (i == 1 ? "Gemini: chỉ đọc + trả lời, chưa vẽ CAD." : "Claude: đọc + vẽ CAD qua MCP.") + " Ctrl+Enter để gửi.";
        }

        string ModelChon() { return MODEL[chonAi.SelectedIndex][Math.Max(0, chonModel.SelectedIndex)]; }
        string EffortChon() { return chonEffort.SelectedIndex < 0 ? "high" : (string)chonEffort.Items[chonEffort.SelectedIndex]; }

        /// Nạp mức effort hợp model đang chọn, giữ mức đã lưu (Claude: chat_effort; Gemini: đuôi của model_gemini).
        void NapEffort()
        {
            int i = chonAi.SelectedIndex;
            string[] ds = Effort(i, ModelChon());
            string luu = i == 0 ? GiaoDien.DocCaiDat("chat_effort", "high") : GiaoDien.DocCaiDat(KHOA_MODEL[1], "");
            if (i == 1) luu = luu.StartsWith(ModelChon() + "-") ? luu.Substring(ModelChon().Length + 1) : "high";
            chonEffort.Items.Clear();
            chonEffort.Items.AddRange(ds);
            int k = Array.IndexOf(ds, luu);
            chonEffort.SelectedIndex = k >= 0 ? k : Array.IndexOf(ds, "high");
        }

        void LuuModel()
        {
            if (chonAi.SelectedIndex == 0) { GiaoDien.LuuCaiDat(KHOA_MODEL[0], ModelChon()); GiaoDien.LuuCaiDat("chat_effort", EffortChon()); }
            else GiaoDien.LuuCaiDat(KHOA_MODEL[1], ModelChon() + "-" + EffortChon());
        }

        /// Hàng thẻ: "+ Thư mục dự án" rồi mỗi thư mục một thẻ, bấm thẻ để bỏ.
        void VeThe()
        {
            the.SuspendLayout();
            the.Controls.Clear();
            the.Controls.Add(Mau("+ Thư mục dự án", delegate
            {
                using (FolderBrowserDialog d = new FolderBrowserDialog())
                {
                    d.Description = "Chọn thư mục dự án để AI đọc làm ngữ cảnh";
                    if (d.ShowDialog() != DialogResult.OK || thuMuc.Contains(d.SelectedPath, StringComparer.OrdinalIgnoreCase)) return;
                    thuMuc.Add(d.SelectedPath);
                    LuuThe();
                }
            }));
            foreach (string d in thuMuc)
            {
                string dd = d;
                Label t = Mau(Path.GetFileName(dd.TrimEnd('\\')) + "  ✕", delegate { thuMuc.Remove(dd); LuuThe(); });
                t.ForeColor = GiaoDien.ChuDam;
                goiY.SetToolTip(t, dd + "\nBấm để bỏ thẻ");
                the.Controls.Add(t);
            }
            the.ResumeLayout();
        }

        void LuuThe()
        {
            GiaoDien.LuuCaiDat("chat_thu_muc", string.Join("|", thuMuc));
            VeThe();
        }

        static Label Mau(string chu, Action viec)
        {
            Label h = new Label();
            h.Text = chu;
            h.Font = GiaoDien.Nho;
            h.AutoSize = true;
            h.Padding = new Padding(4, 3, 4, 3);
            h.Margin = new Padding(0, 4, 6, 0);
            h.BackColor = GiaoDien.The;
            h.ForeColor = GiaoDien.Xanh;
            h.Cursor = Cursors.Hand;
            h.Click += delegate { GiaoDien.An(viec); };
            return h;
        }

        public void DatCau(string cau)
        {
            nhap.Text = cau;
            nhap.Focus();
            nhap.SelectionStart = nhap.TextLength;
        }

        void Ghi(string ai, string chu, Color mau)
        {
            hoi.SelectionStart = hoi.TextLength;
            hoi.SelectionColor = mau;
            hoi.AppendText((hoi.TextLength > 0 ? "\n" : "") + (ai != null ? ai + ": " : "") + chu);
            hoi.ScrollToCaret();
        }

        /// <repo>\cad suy từ nguon_vat_tu.txt (build.bat ghi <repo>\cad\..\data\vat_tu.json). null = không thấy.
        static string ThuMucCad()
        {
            string f = Path.Combine(Path.GetDirectoryName(typeof(BotChat).Assembly.Location), "nguon_vat_tu.txt"); // build.bat ghi cạnh TKPCCC.BotChat.dll
            if (!File.Exists(f)) return null;
            string vt = Path.GetFullPath(File.ReadAllText(f).Trim());
            string cad = Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(vt)), "cad");
            return File.Exists(Path.Combine(cad, ".mcp.json")) ? cad : null;
        }

        /// Tìm file chạy trên PATH và các thư mục thêm; quaCmd = file .cmd (npm) phải chạy qua cmd. null = không thấy.
        static string Tim(string[] ten, string[] them, out bool quaCmd)
        {
            quaCmd = false;
            List<string> thu = (Environment.GetEnvironmentVariable("PATH") ?? "").Split(';').Where(d => d.Trim() != "").ToList();
            thu.AddRange(them);
            foreach (string t in ten)
                foreach (string d in thu)
                {
                    string f;
                    try { f = Path.Combine(d.Trim().Trim('"'), t); } catch (System.Exception) { continue; }
                    if (!File.Exists(f)) continue;
                    quaCmd = t.EndsWith(".cmd");
                    return f;
                }
            return null;
        }

        /// Bọc một đối số dòng lệnh Windows (luật CommandLineToArgvW): \ đứng trước " hoặc cuối chuỗi nhân đôi, " thành \".
        internal static string Ngoac(string s)
        {
            return "\"" + Regex.Replace(s, "(\\\\*)(\"|\\z)", m => m.Groups[1].Value + m.Groups[1].Value + (m.Groups[2].Value == "\"" ? "\\\"" : "")) + "\"";
        }

        void Gui()
        {
            if (p != null) { try { p.Kill(); } catch (System.Exception) { } return; } // đang chạy: nút là "Dừng"
            string cau = nhap.Text.Trim();
            if (cau == "") return;
            string cad = ThuMucCad();
            if (cad == null) { trangThai.Text = "Không thấy thư mục repo (cad\\.mcp.json): chạy cad\\build.bat từ repo TKPCCC."; return; }
            int ai = chonAi.SelectedIndex;
            string model = ai == 0 ? ModelChon() : ModelChon() + "-" + EffortChon(), effort = EffortChon();
            if (aiPhien != ai) phien = null;
            List<string> co = thuMuc.Where(Directory.Exists).ToList();
            string themDir = string.Concat(co.Select(d => " --add-dir " + Ngoac(d)));
            string gui1 = co.Count == 0 ? cau
                : "Thư mục dự án gắn kèm (đọc khi cần làm ngữ cảnh):\n" + string.Join("\n", co.Select(d => "- " + d)) + "\n\n" + cau;

            bool quaCmd;
            string exe, doiSo;
            if (ai == 0)
            {
                exe = Tim(new[] { "claude.exe", "claude.cmd" }, new[] { Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "bin") }, out quaCmd);
                if (exe == null) { Ghi(null, CAI_CLAUDE, GiaoDien.Do); return; }
                // Như runner/src/engines/claude.ts: không API key, không nạp cài đặt cá nhân; thêm MCP cad và chỉ cho công cụ đọc + 3 công cụ cad.
                // --add-dir trước --tools: các cờ này nhận nhiều giá trị, cờ kế tiếp mới cắt.
                doiSo = "-p --output-format stream-json --verbose --model " + model + " --effort " + effort
                    + " --mcp-config .mcp.json --strict-mcp-config --setting-sources project,local --disable-slash-commands"
                    + (phien != null ? " --resume " + phien : "") + themDir
                    + " --tools Read Grep Glob --allowedTools Read Grep Glob mcp__cad__cad_tt mcp__cad__cad_chay mcp__cad__cad_anh";
            }
            else
            {
                exe = Tim(new[] { "agy.exe" }, new[] { Path.Combine(Environment.GetEnvironmentVariable("LOCALAPPDATA") ?? "", "agy", "bin") }, out quaCmd);
                if (exe == null) { Ghi(null, CAI_AGY, GiaoDien.Do); return; }
                // agy nhận lời nhắn qua -p (exe Go đọc dòng lệnh UTF-16 nên chữ Việt giữ nguyên); --mode plan = chỉ đọc.
                doiSo = "-p " + Ngoac(gui1) + " --output-format stream-json --mode plan --model " + model
                    + (phien != null ? " --conversation " + phien : "") + themDir;
            }
            ProcessStartInfo si = quaCmd ? new ProcessStartInfo("cmd.exe", "/d /s /c \"\"" + exe + "\" " + doiSo + "\"") : new ProcessStartInfo(exe, doiSo);
            si.UseShellExecute = false;
            si.CreateNoWindow = true;
            si.WorkingDirectory = cad;
            si.RedirectStandardInput = si.RedirectStandardOutput = si.RedirectStandardError = true;
            si.StandardOutputEncoding = si.StandardErrorEncoding = new UTF8Encoding(false);
            si.EnvironmentVariables.Remove("ANTHROPIC_API_KEY");
            si.EnvironmentVariables.Remove("CLAUDECODE");
            si.EnvironmentVariables["NO_COLOR"] = "1";

            Ghi("Bạn", cau, GiaoDien.Xanh);
            nhap.Clear();
            loi.Length = 0;
            aiPhien = ai;
            Process q = new Process();
            q.StartInfo = si;
            q.OutputDataReceived += delegate(object s, DataReceivedEventArgs e)
            {
                string d = e.Data;
                try { BeginInvoke((Action)delegate { if (d == null) Xong(q); else DocDong(d); }); } catch (System.Exception) { } // palette đã đóng
            };
            q.ErrorDataReceived += delegate(object s, DataReceivedEventArgs e) { if (e.Data != null) lock (loi) loi.AppendLine(e.Data); };
            try { q.Start(); }
            catch (System.Exception ex) { Ghi(null, (ai == 0 ? CAI_CLAUDE : CAI_AGY) + " (" + ex.Message + ")", GiaoDien.Do); return; }
            p = q;
            gui.Text = "Dừng";
            gui.Invalidate();
            trangThai.Text = TEN_AI[ai] + " đang chạy (" + model + ")...";
            q.BeginOutputReadLine();
            q.BeginErrorReadLine();
            try
            {
                if (ai == 0)
                {
                    byte[] b = new UTF8Encoding(false).GetBytes(gui1); // claude.cmd qua cmd làm hỏng chữ Việt trên dòng lệnh: gửi qua stdin UTF-8
                    q.StandardInput.BaseStream.Write(b, 0, b.Length);
                }
                q.StandardInput.Close();
            }
            catch (System.Exception) { } // CLI thoát sớm: lỗi thật đọc ở stderr khi Xong
        }

        /// Một dòng stream-json.
        /// claude: system/init (session_id), assistant (chữ, công cụ), result (lỗi). agy: init (conversation_id), step_update (bước tool), result (response, status).
        void DocDong(string dong)
        {
            Dictionary<string, object> j;
            try
            {
                JavaScriptSerializer js = new JavaScriptSerializer();
                js.MaxJsonLength = int.MaxValue; // kết quả công cụ có thể dài
                j = js.DeserializeObject(dong) as Dictionary<string, object>;
            }
            catch (System.Exception) { j = null; }
            if (j == null) { if (!dong.TrimStart().StartsWith("{")) Ghi(null, dong, GiaoDien.ChuMo); return; }
            object v;
            if (j.TryGetValue("session_id", out v) && v is string) phien = (string)v;
            if (j.TryGetValue("conversation_id", out v) && v is string) phien = (string)v;
            string loai = j.TryGetValue("type", out v) ? v as string : null;
            string suKien = j.TryGetValue("event", out v) ? v as string : null;
            if (loai == "assistant" && j.TryGetValue("message", out v))
            {
                Dictionary<string, object> m = v as Dictionary<string, object>;
                object[] nd = m != null && m.TryGetValue("content", out v) ? v as object[] : null;
                foreach (Dictionary<string, object> k in (nd ?? new object[0]).OfType<Dictionary<string, object>>())
                {
                    object t;
                    string kieu = k.TryGetValue("type", out t) ? t as string : null;
                    if (kieu == "text" && k.TryGetValue("text", out t)) Ghi("Claude", Convert.ToString(t).Trim(), GiaoDien.ChuDam);
                    else if (kieu == "tool_use" && k.TryGetValue("name", out t)) CongCu(Convert.ToString(t).Replace("mcp__cad__", ""));
                }
            }
            else if (loai == "result" && j.TryGetValue("is_error", out v) && v is bool && (bool)v)
                Ghi(null, "Lỗi: " + (j.TryGetValue("result", out v) ? Convert.ToString(v) : "") + LoiDangNhap(Convert.ToString(v)), GiaoDien.Do);
            else if (suKien == "step_update" && j.TryGetValue("step_update", out v))
            {
                Dictionary<string, object> b = v as Dictionary<string, object>;
                object t;
                if (b != null && b.TryGetValue("state", out t) && Convert.ToString(t) == "ACTIVE" && b.TryGetValue("tool_name", out t)) CongCu(Convert.ToString(t));
            }
            else if (suKien == "result" && j.TryGetValue("result", out v))
            {
                Dictionary<string, object> r = v as Dictionary<string, object>;
                object t;
                if (r == null) return;
                if (r.TryGetValue("response", out t) && Convert.ToString(t).Trim() != "") Ghi("Gemini", Convert.ToString(t).Trim(), GiaoDien.ChuDam);
                if (r.TryGetValue("status", out t) && Convert.ToString(t) != "SUCCESS")
                    Ghi(null, "Lỗi: " + Convert.ToString(t) + (r.TryGetValue("error", out t) ? " " + Convert.ToString(t) : ""), GiaoDien.Do);
            }
        }

        void CongCu(string ten)
        {
            Ghi(null, "  · " + ten, GiaoDien.ChuMo);
            trangThai.Text = TEN_AI[Math.Max(0, aiPhien)] + " đang chạy: " + ten + "...";
        }

        static string LoiDangNhap(string m)
        {
            string t = (m ?? "").ToLowerInvariant();
            return t.Contains("login") || t.Contains("auth") || t.Contains("401") || t.Contains("credential")
                ? "\nPhiên đăng nhập hết hạn: mở terminal, gõ claude rồi /login (Gemini: gõ agy)." : "";
        }

        void Xong(Process q)
        {
            int ma = -1;
            try { q.WaitForExit(3000); if (q.HasExited) ma = q.ExitCode; } catch (System.Exception) { }
            string e;
            lock (loi) e = loi.ToString().Trim();
            if (ma != 0 && e != "") Ghi(null, TEN_AI[Math.Max(0, aiPhien)] + " thoát mã " + ma + ": " + (e.Length > 600 ? e.Substring(0, 600) : e) + LoiDangNhap(e), GiaoDien.Do);
            trangThai.Text = ma == 0 ? "Xong." : "Dừng (mã " + ma + ").";
            q.Dispose();
            p = null;
            gui.Text = "Gửi";
            gui.Invalidate();
        }
    }
}
