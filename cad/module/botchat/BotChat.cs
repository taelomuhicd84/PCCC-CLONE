// Plugin Bot chat (bundle riêng TKPCCC_BotChat.bundle, lệnh TKBOTCHAT; tách khỏi palette PCCC 17/11): chọn máy AI, model và effort ngay trên khung,
// gắn thẻ thư mục dự án để AI đọc hồ sơ.
// - Claude: `claude -p` (đăng nhập Claude Code, không API key) chạy NỀN ở <repo>\cad với MCP cad, vẽ được vào bản vẽ; giữ hội thoại bằng --resume.
// - Gemini: `agy -p` (Antigravity CLI, đăng nhập Google) chế độ plan = chỉ đọc + trả lời; chưa có MCP cad nên KHÔNG vẽ; giữ hội thoại bằng --conversation.
// Thẻ thư mục: truyền `--add-dir` cho cả hai CLI và ghi danh sách vào đầu lời nhắn. Lưu ở cai_dat.json (chat_ai, model, model_gemini, chat_effort, chat_thu_muc).
// Effort (người dùng 17/11): Claude `--effort low|medium|high|xhigh|max`; Gemini (agy) mức nằm trong tên model (gemini-4-argon-high): ghép gốc + "-" + mức.
// Chạy nền nên luồng AutoCAD không bị khoá, cầu nối Cau.cs vẫn chạy hàng đợi trong lúc chờ.
// Người dùng 18/11 (như VS Code / Claude Code): Enter gửi, Shift+Enter xuống dòng; dán (Ctrl+V) / kéo thả / "+ Ảnh" đính kèm ảnh (lưu
// %LOCALAPPDATA%\TKPCCC\botchat\anh, AI xem bằng Read qua --add-dir); thẻ "vùng chọn" = đối tượng đang quét chọn trên CAD (handle, thành phần,
// hộp bao) gửi kèm để AI đọc (doc gi:"dt") và sửa (sua / xoa theo "h") đúng phần đó.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using AcAp = Autodesk.AutoCAD.ApplicationServices;
using Db = Autodesk.AutoCAD.DatabaseServices;

namespace TKPCCC
{
    /// Ô nhập lời nhắn: lệnh dán (Ctrl+V, menu chuột phải) có ảnh trong clipboard thì đính kèm ảnh thay vì dán chữ.
    class ONhap : TextBox
    {
        public Func<bool> DanAnh;

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == 0x0302 && DanAnh != null && DanAnh()) return; // WM_PASTE
            base.WndProc(ref m);
        }
    }

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
        readonly ONhap nhap = new ONhap();
        readonly NutBo gui = new NutBo("Gửi", GiaoDien.XanhDam);
        readonly Label trangThai = new Label();
        readonly ComboBox chonAi = new ComboBox(), chonModel = new ComboBox(), chonEffort = new ComboBox();
        readonly FlowLayoutPanel the = new FlowLayoutPanel(), dinhKem = new FlowLayoutPanel();
        readonly List<string> anh = new List<string>(); // ảnh đính kèm lời nhắn kế
        static readonly string THU_MUC_ANH = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TKPCCC", "botchat", "anh");
        const int TOI_DA_HANDLE = 200; // handle gửi kèm tối đa: vùng lớn hơn thì AI lọc thêm bằng hop + l
        AcAp.Document vungDoc;
        Db.ObjectId[] vungId; // vùng quét chọn trên CAD; null = không có
        bool vungGiu;         // chọn bằng TKBOTCHON: giữ cả khi người dùng bỏ chọn trên bản vẽ
        readonly ToolTip goiY = new ToolTip();
        readonly List<string> thuMuc;
        // Người dùng 18/11 (ảnh khung chat mẫu): chọn thư mục dự án = thư mục làm việc của AI (skill .claude\skills, CLAUDE.md của dự án đó),
        // thư mục chứa skill, nối phiên VS Code / Claude Code / Antigravity (PhienAi.cs).
        readonly ComboBox chonDuAn = new ComboBox();
        readonly List<string> duAnDs;  // thư mục dự án gần đây (chat_du_an_ds); ô chọn: 0 = TKPCCC CAD, 1.. = danh sách này, cuối = "Chọn thư mục…"
        readonly List<string> skill;   // thư mục chứa skill (chat_skill)
        bool napDuAn;                  // đang nạp lại ô dự án: bỏ qua sự kiện đổi
        string linkTen;                // phiên đang nối từ nơi khác; null = hội thoại của Bot chat
        bool tach;                     // lần gửi kế thêm --fork-session: chạy tiếp ngữ cảnh phiên Claude nơi khác mà không ghi vào phiên gốc
        readonly Timer dongHo = new Timer();
        DateTime batDau;
        string viec = "";
        Process p;
        string phien; // session_id (claude) hoặc conversation_id (agy) để nói tiếp
        int aiPhien = -1; // máy AI của `phien`: đổi máy thì mở hội thoại mới
        readonly StringBuilder loi = new StringBuilder();

        public BotChat()
        {
            Font = GiaoDien.Chu;
            BackColor = GiaoDien.Nen;
            thuMuc = GiaoDien.DocCaiDat("chat_thu_muc", "").Split('|').Where(d => d != "").ToList();
            duAnDs = GiaoDien.DocCaiDat("chat_du_an_ds", "").Split('|').Where(d => d != "").ToList();
            skill = GiaoDien.DocCaiDat("chat_skill", "").Split('|').Where(d => d != "").ToList();
            dongHo.Interval = 1000;
            dongHo.Tick += delegate { CapNhat(); };

            hoi.Dock = DockStyle.Fill;
            hoi.ReadOnly = true;
            hoi.BorderStyle = BorderStyle.None;
            hoi.BackColor = GiaoDien.The;
            hoi.ForeColor = GiaoDien.ChuDam;
            hoi.Font = GiaoDien.Chu;
            Panel khungHoi = new Panel(); // lề quanh khung hội thoại: bong bóng không dính mép
            khungHoi.Dock = DockStyle.Fill;
            khungHoi.Padding = new Padding(8, 8, 4, 6);
            khungHoi.BackColor = GiaoDien.The;
            khungHoi.Controls.Add(hoi);
            Controls.Add(khungHoi);

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
            KieuHop(chonDuAn, 170);
            NapDuAn();
            chonDuAn.SelectedIndexChanged += delegate { if (!napDuAn) BeginInvoke((Action)delegate { GiaoDien.An(DoiDuAn); }); };
            Label lichSu = Mau("Lịch sử / Link", MoLichSu);
            goiY.SetToolTip(lichSu, "Nối tiếp một phiên Claude Code (VS Code, CLI, Desktop, Bot chat) hoặc hội thoại Antigravity");
            Label moi = Mau("＋ Mới", HoiThoaiMoi);
            goiY.SetToolTip(moi, "Hội thoại mới");
            dau.Controls.AddRange(new Control[] { chonDuAn, lichSu, moi, chonAi, chonModel, chonEffort });
            Controls.Add(dau);

            Panel duoi = new Panel();
            duoi.Dock = DockStyle.Bottom;
            duoi.Height = 236;
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
            nhap.KeyDown += delegate(object s, KeyEventArgs e) { if (e.KeyCode == Keys.Enter && !e.Shift) { e.SuppressKeyPress = true; Gui(); } };
            nhap.DanAnh = DanAnh;
            nhap.AllowDrop = true;
            nhap.DragEnter += delegate(object s, DragEventArgs e) { e.Effect = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None; };
            nhap.DragDrop += delegate(object s, DragEventArgs e)
            {
                GiaoDien.An(delegate
                {
                    foreach (string f in (string[])e.Data.GetData(DataFormats.FileDrop) ?? new string[0])
                        if (LaAnh(f)) ThemAnh(ChepAnh(f));
                        else nhap.AppendText((nhap.TextLength > 0 ? " " : "") + f); // file khác: chèn đường dẫn để AI đọc
                });
            };
            dinhKem.Dock = DockStyle.Top;
            dinhKem.AutoSize = true;
            dinhKem.BackColor = GiaoDien.Nen;
            dinhKem.Padding = new Padding(0, 0, 0, 4);
            VeDinhKem();
            FlowLayoutPanel nut = new FlowLayoutPanel();
            nut.Dock = DockStyle.Bottom;
            nut.Height = 66;
            nut.BackColor = GiaoDien.Nen;
            nut.Padding = new Padding(0, 4, 0, 0);
            gui.Size = new Size(70, 28);
            gui.Click += delegate { Gui(); };
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
            nut.Controls.AddRange(new Control[] { gui, mau1, mau2 });
            trangThai.Dock = DockStyle.Bottom;
            trangThai.Height = 20;
            trangThai.Font = GiaoDien.Nho;
            trangThai.ForeColor = GiaoDien.ChuMo;
            trangThai.AutoEllipsis = true;
            duoi.Controls.Add(nhap);
            duoi.Controls.Add(dinhKem); // thêm trước "the" nên neo dưới hàng thẻ thư mục, sát ô nhập
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
            trangThai.Text = (i == 1 ? "Gemini: chỉ đọc." : "Claude: vẽ, sửa CAD.") + " Enter gửi · Shift+Enter xuống dòng · Ctrl+V dán ảnh";
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
            Label them = Mau("+ Skill", delegate
            {
                using (FolderBrowserDialog d = new FolderBrowserDialog())
                {
                    d.Description = "Chọn thư mục chứa skill: thư mục skills (vd C:\\Users\\<tên>\\.claude\\skills), plugin có thư mục skills, hoặc dự án có .claude\\skills";
                    if (d.ShowDialog() != DialogResult.OK || skill.Contains(d.SelectedPath, StringComparer.OrdinalIgnoreCase)) return;
                    skill.Add(d.SelectedPath);
                    LuuThe();
                }
            });
            goiY.SetToolTip(them, "Thêm chỗ chứa skill cho Claude (gõ / để chọn skill)");
            the.Controls.Add(them);
            foreach (string d in skill)
            {
                string dd = d;
                Label t = Mau("skill: " + Path.GetFileName(dd.TrimEnd('\\')) + "  ✕", delegate { skill.Remove(dd); LuuThe(); });
                t.ForeColor = GiaoDien.ChuDam;
                goiY.SetToolTip(t, "Skill: " + dd + "\nBấm để bỏ");
                the.Controls.Add(t);
            }
            the.ResumeLayout();
        }

        void LuuThe()
        {
            GiaoDien.LuuCaiDat("chat_thu_muc", string.Join("|", thuMuc));
            GiaoDien.LuuCaiDat("chat_skill", string.Join("|", skill));
            VeThe();
        }

        // ---------- Thư mục dự án, lịch sử / link phiên, skill ----------

        void NapDuAn()
        {
            napDuAn = true;
            chonDuAn.Items.Clear();
            chonDuAn.Items.Add("TKPCCC CAD (mặc định)");
            foreach (string d in duAnDs) chonDuAn.Items.Add("Dự án: " + Path.GetFileName(d.TrimEnd('\\')));
            chonDuAn.Items.Add("Chọn thư mục dự án…");
            string dang = GiaoDien.DocCaiDat("chat_du_an", "");
            chonDuAn.SelectedIndex = duAnDs.FindIndex(d => string.Equals(d, dang, StringComparison.OrdinalIgnoreCase)) + 1;
            goiY.SetToolTip(chonDuAn, "Thư mục dự án: " + (DuAn() ?? ThuMucCad() ?? "?") + "\nAI làm việc trong thư mục này: đọc CLAUDE.md, dùng skill trong .claude\\skills");
            napDuAn = false;
        }

        /// Thư mục dự án đang chọn; null = mặc định (<repo>\cad).
        string DuAn()
        {
            int i = chonDuAn.SelectedIndex - 1;
            return i >= 0 && i < duAnDs.Count ? duAnDs[i] : null;
        }

        /// Thư mục làm việc của CLI: dự án đang chọn (còn tồn tại), không thì <repo>\cad.
        string ThuMucLam()
        {
            string d = DuAn();
            return d != null && Directory.Exists(d) ? d : ThuMucCad();
        }

        void DatDuAn(string d)
        {
            if (d != null)
            {
                duAnDs.RemoveAll(x => string.Equals(x, d, StringComparison.OrdinalIgnoreCase));
                duAnDs.Insert(0, d);
                if (duAnDs.Count > 8) duAnDs.RemoveRange(8, duAnDs.Count - 8);
                GiaoDien.LuuCaiDat("chat_du_an_ds", string.Join("|", duAnDs));
            }
            GiaoDien.LuuCaiDat("chat_du_an", d ?? "");
            NapDuAn();
        }

        void DoiDuAn()
        {
            if (chonDuAn.SelectedIndex == chonDuAn.Items.Count - 1)
                using (FolderBrowserDialog d = new FolderBrowserDialog())
                {
                    d.Description = "Chọn thư mục dự án: AI làm việc trong thư mục này (CLAUDE.md, skill trong .claude\\skills)";
                    if (d.ShowDialog() == DialogResult.OK) DatDuAn(d.SelectedPath);
                    else { NapDuAn(); return; }
                }
            else DatDuAn(DuAn());
            HoiThoaiMoi();
            trangThai.Text = "Thư mục dự án: " + (ThuMucLam() ?? "?");
        }

        void HoiThoaiMoi()
        {
            if (p != null) return;
            phien = null;
            aiPhien = -1;
            linkTen = null;
            tach = false;
            hoi.Clear();
            VeDinhKem();
            trangThai.Text = "Hội thoại mới.";
        }

        /// Menu phiên gần đây: Claude Code (VS Code, CLI, Desktop, Bot chat) và Antigravity. Chọn = nối tiếp phiên đó.
        void MoLichSu()
        {
            if (p != null) { trangThai.Text = "Đang chạy: đợi xong (hoặc Dừng) rồi mới đổi phiên."; return; }
            ContextMenuStrip m = new ContextMenuStrip();
            m.Font = GiaoDien.Chu;
            Action<string> nhom = delegate(string ten) { ToolStripLabel l = new ToolStripLabel(ten); l.ForeColor = Color.Gray; m.Items.Add(l); };
            Action<List<PhienAi>> them = delegate(List<PhienAi> ds)
            {
                if (ds.Count == 0) { m.Items.Add("(không có)").Enabled = false; return; }
                foreach (PhienAi x in ds)
                {
                    PhienAi xx = x;
                    ToolStripItem i = m.Items.Add(x.ToString(), null, delegate { GiaoDien.An(delegate { Link(xx); }); });
                    i.ToolTipText = (x.Cwd ?? "") + "\n" + x.Id;
                }
            };
            nhom("Claude Code: VS Code · CLI · Desktop · Bot chat");
            them(KhoPhien.Claude(15));
            m.Items.Add(new ToolStripSeparator());
            nhom("Antigravity: IDE · agy");
            them(KhoPhien.Antigravity(10));
            m.Show(Cursor.Position);
        }

        /// Nối phiên x: chọn đúng máy AI, đổi thư mục dự án theo phiên (Claude tìm phiên theo thư mục làm việc), hiện vài tin cuối.
        void Link(PhienAi x)
        {
            if (p != null) return;
            chonAi.SelectedIndex = x.Ai;
            if (x.Cwd != null && Directory.Exists(x.Cwd) && !string.Equals(x.Cwd.TrimEnd('\\'), (ThuMucLam() ?? "").TrimEnd('\\'), StringComparison.OrdinalIgnoreCase))
                DatDuAn(x.Cwd);
            hoi.Clear();
            phien = x.Id;
            aiPhien = x.Ai;
            tach = x.Ai == 0;
            string ten = x.Ten.Replace("\n", " ");
            linkTen = x.Nguon + ": " + (ten.Length > 30 ? ten.Substring(0, 29) + "…" : ten);
            foreach (KeyValuePair<bool, string> t in KhoPhien.TinCuoi(x, 6))
                Ghi(t.Key ? "Bạn" : TEN_AI[x.Ai], t.Value.Length > 700 ? t.Value.Substring(0, 700) + "…" : t.Value, t.Key ? GiaoDien.Xanh : GiaoDien.ChuDam);
            Ghi(null, "— Đã nối " + x + (x.Ai == 0
                ? ". Lời nhắn kế chạy tiếp ngữ cảnh phiên này ở một nhánh mới (phiên gốc trong VS Code / Claude Code giữ nguyên)."
                : ". Lời nhắn kế ghi tiếp vào hội thoại Antigravity này."), GiaoDien.ChuMo);
            VeDinhKem();
        }

        /// Menu "/": skill của thư mục dự án, của <repo>\cad (luôn gắn), thẻ thư mục, chỗ chứa skill. Chọn = chèn "/tên " đầu ô nhập.
        void MenuSkill()
        {
            List<string> ten = new List<string>();
            string lam = ThuMucLam(), cad = ThuMucCad();
            foreach (string d in new[] { lam, cad }.Concat(thuMuc).Where(d => d != null && Directory.Exists(d)))
                ten.AddRange(KhoPhien.TenSkill(Path.Combine(d, ".claude", "skills"), null));
            foreach (string d in skill.Where(Directory.Exists))
            {
                string tienTo, ms;
                KhoPhien.CoSkill(d, out tienTo, out ms);
                ten.AddRange(KhoPhien.TenSkill(ms, tienTo));
            }
            ContextMenuStrip m = new ContextMenuStrip();
            m.Font = GiaoDien.Chu;
            foreach (string t in ten.Distinct().OrderBy(t => t))
            {
                string tt = t;
                m.Items.Add("/" + t, null, delegate { DatCau("/" + tt + " " + nhap.Text.TrimStart()); });
            }
            if (m.Items.Count == 0) m.Items.Add("(chưa có skill: bấm + Skill, hoặc chọn dự án có .claude\\skills)").Enabled = false;
            m.Show(Cursor.Position);
        }

        void CapNhat()
        {
            if (p == null) return;
            int s = (int)(DateTime.Now - batDau).TotalSeconds;
            trangThai.Text = "✻ " + TEN_AI[Math.Max(0, aiPhien)] + " " + viec + "…  " + s / 60 + ":" + (s % 60).ToString("00") + " · " + EffortChon();
        }

        // ---------- Đính kèm: ảnh + vùng chọn trên CAD ----------

        static bool LaAnh(string f)
        {
            return new[] { ".png", ".jpg", ".jpeg", ".gif", ".bmp", ".webp" }.Contains(Path.GetExtension(f).ToLowerInvariant());
        }

        static bool daDon;

        /// Tên file mới trong thư mục ảnh đính kèm; lần đầu mỗi phiên AutoCAD dọn ảnh quá 7 ngày.
        static string FileAnhMoi(string duoi)
        {
            Directory.CreateDirectory(THU_MUC_ANH);
            if (!daDon)
            {
                daDon = true;
                foreach (string cu in Directory.GetFiles(THU_MUC_ANH))
                    if (File.GetLastWriteTimeUtc(cu) < DateTime.UtcNow.AddDays(-7)) try { File.Delete(cu); } catch (System.Exception) { }
            }
            return Path.Combine(THU_MUC_ANH, DateTime.Now.ToString("yyyyMMdd_HHmmss_fff") + duoi);
        }

        /// Chép ảnh vào thư mục ảnh đính kèm: AI chỉ cần một --add-dir.
        static string ChepAnh(string f)
        {
            string moi = FileAnhMoi(Path.GetExtension(f).ToLowerInvariant());
            File.Copy(f, moi);
            return moi;
        }

        void ThemAnh(string f)
        {
            anh.Add(f);
            VeDinhKem();
        }

        /// Dán: ảnh trong clipboard (chụp màn hình) hoặc file ảnh đã copy. false = không có ảnh, dán chữ như thường.
        bool DanAnh()
        {
            try
            {
                if (Clipboard.ContainsImage())
                {
                    using (Image i = Clipboard.GetImage())
                    {
                        if (i == null) return false;
                        string f = FileAnhMoi(".png");
                        i.Save(f, ImageFormat.Png);
                        ThemAnh(f);
                    }
                    return true;
                }
                if (Clipboard.ContainsFileDropList())
                {
                    List<string> ds = Clipboard.GetFileDropList().Cast<string>().Where(LaAnh).ToList();
                    foreach (string f in ds) ThemAnh(ChepAnh(f));
                    return ds.Count > 0;
                }
            }
            catch (System.Exception ex) { trangThai.Text = "Không dán được ảnh: " + ex.Message; return true; }
            return false;
        }

        void ChonAnh()
        {
            using (OpenFileDialog d = new OpenFileDialog())
            {
                d.Filter = "Ảnh|*.png;*.jpg;*.jpeg;*.gif;*.bmp;*.webp";
                d.Multiselect = true;
                if (d.ShowDialog() == DialogResult.OK) foreach (string f in d.FileNames) ThemAnh(ChepAnh(f));
            }
        }

        /// Ảnh thu nhỏ cho thẻ (không giữ khoá file). null = không đọc được (vd webp).
        static Image ThuNho(string f)
        {
            try
            {
                using (Image i = Image.FromFile(f))
                {
                    double k = Math.Min(1, Math.Min(92.0 / i.Width, 80.0 / i.Height));
                    return new Bitmap(i, Math.Max(1, (int)(i.Width * k)), Math.Max(1, (int)(i.Height * k)));
                }
            }
            catch (System.Exception) { return null; }
        }

        /// Hàng đính kèm sát ô nhập: nút quét vùng, nút thêm ảnh, thẻ vùng chọn, thẻ từng ảnh (bấm thẻ để bỏ).
        void VeDinhKem()
        {
            dinhKem.SuspendLayout();
            List<Control> cu = dinhKem.Controls.Cast<Control>().ToList();
            dinhKem.Controls.Clear();
            foreach (Control c in cu) { if (c.BackgroundImage != null) c.BackgroundImage.Dispose(); c.Dispose(); }
            if (linkTen != null)
            {
                Label lk = Mau("Link · " + linkTen + "  ✕", HoiThoaiMoi);
                lk.ForeColor = GiaoDien.ChuDam;
                goiY.SetToolTip(lk, "Đang nối phiên này. Bấm để bỏ nối (hội thoại mới)");
                dinhKem.Controls.Add(lk);
            }
            Label sk = Mau("/ Skill", MenuSkill);
            goiY.SetToolTip(sk, "Chọn skill của dự án / chỗ chứa skill để chèn vào lời nhắn");
            dinhKem.Controls.Add(sk);
            Label q = Mau("⬚ Quét vùng CAD", QuetVung);
            goiY.SetToolTip(q, "Quét chọn đối tượng trên bản vẽ để AI đọc và sửa đúng phần đó (lệnh TKBOTCHON).\nChọn sẵn trên bản vẽ rồi gõ yêu cầu cũng được.");
            dinhKem.Controls.Add(q);
            Label a = Mau("+ Ảnh", ChonAnh);
            goiY.SetToolTip(a, "Đính kèm ảnh (hoặc Ctrl+V dán ảnh chụp màn hình, kéo thả file ảnh vào ô nhập)");
            dinhKem.Controls.Add(a);
            if (vungId != null)
            {
                Label v = Mau("⬚ " + vungId.Length + " đối tượng đang chọn  ✕", delegate { vungId = null; vungDoc = null; VeDinhKem(); });
                v.ForeColor = GiaoDien.ChuDam;
                goiY.SetToolTip(v, "Gửi kèm lời nhắn kế: handle, thành phần, hộp bao của vùng chọn.\nBấm để bỏ");
                dinhKem.Controls.Add(v);
            }
            foreach (string f in anh)
            {
                string ff = f;
                Label t = new Label();
                t.Size = new Size(46, 40);
                t.Margin = new Padding(0, 4, 6, 0);
                t.BackColor = GiaoDien.The;
                t.BackgroundImage = ThuNho(ff);
                t.BackgroundImageLayout = ImageLayout.Zoom;
                t.Text = t.BackgroundImage == null ? "ảnh ✕" : "✕";
                t.TextAlign = ContentAlignment.TopRight;
                t.ForeColor = t.BackgroundImage == null ? GiaoDien.ChuDam : Color.White;
                t.Font = GiaoDien.Nho;
                t.Cursor = Cursors.Hand;
                t.Click += delegate { anh.Remove(ff); VeDinhKem(); };
                goiY.SetToolTip(t, ff + "\nBấm để bỏ ảnh");
                dinhKem.Controls.Add(t);
            }
            dinhKem.ResumeLayout();
        }

        void QuetVung()
        {
            AcAp.Document d = AcAp.Core.Application.DocumentManager.MdiActiveDocument;
            if (d == null) { trangThai.Text = "Chưa mở bản vẽ nào."; return; }
            d.SendStringToExecute("_TKBOTCHON ", true, false, false);
        }

        /// Gắn vùng chọn (TKBOTCHON: giu = true).
        public void DatVung(AcAp.Document d, Db.ObjectId[] ids, bool giu)
        {
            vungDoc = ids.Length > 0 ? d : null;
            vungId = ids.Length > 0 ? ids : null;
            vungGiu = giu;
            VeDinhKem();
        }

        /// Đối tượng đang chọn trên bản vẽ đổi: có thì thành vùng chọn; bỏ chọn hết thì bỏ thẻ (trừ vùng TKBOTCHON).
        public void ChonDoi(AcAp.Document d, Db.ObjectId[] ids)
        {
            if (ids.Length > 0)
            {
                if (vungId != null && vungDoc == d && ids.SequenceEqual(vungId)) return;
                DatVung(d, ids, false);
            }
            else if (vungId != null && !vungGiu && vungDoc == d) DatVung(d, ids, false);
        }

        static string So(double x) { return Math.Round(x).ToString(CultureInfo.InvariantCulture); }

        /// Mô tả vùng chọn gửi kèm lời nhắn: tên bản vẽ, số đối tượng, hộp bao, thành phần (loại / block / layer), handle. "" = không có vùng.
        string MoTaVung(bool claude)
        {
            if (vungId == null) return "";
            AcAp.Document d = vungDoc;
            if (!AcAp.Core.Application.DocumentManager.Cast<AcAp.Document>().Contains(d)) return ""; // bản vẽ đã đóng
            List<Db.ObjectId> ids = vungId.Where(i => i.IsValid && !i.IsErased).ToList();
            if (ids.Count == 0) return "";
            Dictionary<string, int> dem = new Dictionary<string, int>();
            Db.Extents3d? hop = null;
            try
            {
                using (d.LockDocument())
                using (Db.Transaction tr = d.Database.TransactionManager.StartTransaction())
                {
                    foreach (Db.ObjectId id in ids)
                    {
                        Db.Entity e = tr.GetObject(id, Db.OpenMode.ForRead) as Db.Entity;
                        if (e == null) continue;
                        string k = id.ObjectClass.DxfName;
                        Db.BlockReference br = e as Db.BlockReference;
                        if (br != null)
                            k += " " + ((Db.BlockTableRecord)tr.GetObject(br.IsDynamicBlock ? br.DynamicBlockTableRecord : br.BlockTableRecord, Db.OpenMode.ForRead)).Name;
                        k += " (layer " + e.Layer + ")";
                        int c;
                        dem.TryGetValue(k, out c);
                        dem[k] = c + 1;
                        try
                        {
                            Db.Extents3d x = e.GeometricExtents;
                            if (hop.HasValue) { Db.Extents3d h = hop.Value; h.AddExtents(x); hop = h; } else hop = x;
                        }
                        catch (System.Exception) { }
                    }
                    tr.Commit();
                }
            }
            catch (System.Exception) { } // bản vẽ đang bận (lệnh đang chạy): vẫn gửi số đối tượng + handle
            StringBuilder s = new StringBuilder();
            s.Append("Vùng tôi đang quét chọn trên bản vẽ \"" + Path.GetFileName(d.Name) + "\": " + ids.Count + " đối tượng");
            if (hop.HasValue)
                s.Append(", hộp bao [" + So(hop.Value.MinPoint.X) + "," + So(hop.Value.MinPoint.Y) + "," + So(hop.Value.MaxPoint.X) + "," + So(hop.Value.MaxPoint.Y) + "]");
            s.Append(".\n");
            if (dem.Count > 0)
                s.Append("Thành phần: " + string.Join("; ", dem.OrderByDescending(p => p.Value).Take(15).Select(p => p.Value + " " + p.Key)) + (dem.Count > 15 ? "; ..." : "") + ".\n");
            s.Append("Handle: " + string.Join(",", ids.Take(TOI_DA_HANDLE).Select(i => i.Handle.ToString()))
                + (ids.Count > TOI_DA_HANDLE ? " (còn " + (ids.Count - TOI_DA_HANDLE) + " đối tượng, lọc thêm bằng hop + l)" : "") + "\n");
            if (claude)
                s.Append("Đọc chi tiết: cad_chay {\"ops\":[{\"op\":\"doc\",\"gi\":\"dt\",\"h\":[...]}]}; xem hình: op anh với hop trên. "
                    + "Sửa: op sua (h, doi, xoay, tl, tam, l_moi, mau, t), xoa (h) hoặc vẽ mới. Chỉ đụng tới vùng này trừ khi tôi bảo khác.\n");
            return s.Append("\n").ToString();
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

        /// Một tin trong khung hội thoại, kiểu bong bóng như khung chat mẫu: người dùng nền xanh lệch phải, AI nền nhạt lệch trái, dòng công cụ / hệ thống chữ mờ.
        void Ghi(string ai, string chu, Color mau)
        {
            bool ban = ai == "Bạn";
            if (hoi.TextLength > 0) { hoi.Select(hoi.TextLength, 0); hoi.SelectionBackColor = hoi.BackColor; hoi.AppendText(ai != null ? "\n\n" : "\n"); }
            int dau = hoi.TextLength;
            hoi.AppendText(ai != null ? " " + chu.Replace("\n", " \n ") + " " : chu);
            hoi.Select(dau, hoi.TextLength - dau);
            hoi.SelectionAlignment = ban ? HorizontalAlignment.Right : HorizontalAlignment.Left;
            hoi.SelectionIndent = ban ? 48 : 4;
            hoi.SelectionRightIndent = ban ? 4 : 32;
            hoi.SelectionBackColor = ban ? GiaoDien.XanhDam : ai != null ? GiaoDien.Nen : hoi.BackColor;
            hoi.SelectionColor = ban ? Color.White : mau;
            hoi.SelectionFont = ai != null ? GiaoDien.Chu : GiaoDien.Nho;
            hoi.Select(hoi.TextLength, 0);
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

        internal static string Ngoac(string s) { return KhoPhien.Ngoac(s); }

        void Gui()
        {
            if (p != null) { try { p.Kill(); } catch (System.Exception) { } return; } // đang chạy: nút là "Dừng"
            string cau = nhap.Text.Trim();
            if (cau == "") return;
            string cad = ThuMucCad();
            if (cad == null) { trangThai.Text = "Không thấy thư mục repo (cad\\.mcp.json): chạy cad\\build.bat từ repo TKPCCC."; return; }
            int ai = chonAi.SelectedIndex;
            string model = ai == 0 ? ModelChon() : ModelChon() + "-" + EffortChon(), effort = EffortChon();
            if (aiPhien != ai) { phien = null; linkTen = null; tach = false; }
            string lam = ThuMucLam() ?? cad;
            bool laCad = string.Equals(lam.TrimEnd('\\'), cad.TrimEnd('\\'), StringComparison.OrdinalIgnoreCase);
            List<string> co = thuMuc.Where(Directory.Exists).ToList();
            List<string> anhCo = anh.Where(File.Exists).ToList();
            // Dự án khác cad\: vẫn gắn cad\ (skill vẽ CAD tkpccc-ve-cad, luật) bằng --add-dir.
            string themDir = string.Concat(co.Select(d => " --add-dir " + Ngoac(d))) + (anhCo.Count > 0 ? " --add-dir " + Ngoac(THU_MUC_ANH) : "")
                + (laCad ? "" : " --add-dir " + Ngoac(cad));
            string coSkill = "";
            foreach (string d in skill.Where(Directory.Exists))
            {
                string tienTo, ms;
                coSkill += ai == 0 ? KhoPhien.CoSkill(d, out tienTo, out ms) : " --add-dir " + Ngoac(d);
            }
            string vung = MoTaVung(ai == 0);
            string gui1 = (co.Count == 0 ? "" : "Thư mục dự án gắn kèm (đọc khi cần làm ngữ cảnh):\n" + string.Join("\n", co.Select(d => "- " + d)) + "\n\n")
                + vung
                + (anhCo.Count == 0 ? "" : "Ảnh đính kèm (mở xem bằng công cụ Read):\n" + string.Join("\n", anhCo.Select(f => "- " + f)) + "\n\n")
                + cau;
            string kem = (vung != "" ? "  [vùng chọn " + vungId.Length + " đối tượng]" : "") + (anhCo.Count > 0 ? "  [" + anhCo.Count + " ảnh]" : "");

            bool quaCmd;
            string exe, doiSo;
            if (ai == 0)
            {
                exe = Tim(new[] { "claude.exe", "claude.cmd" }, new[] { Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "bin") }, out quaCmd);
                if (exe == null) { Ghi(null, CAI_CLAUDE, GiaoDien.Do); return; }
                // Như runner/src/engines/claude.ts: không API key, không nạp cài đặt cá nhân (chỉ project, local của thư mục dự án); thêm MCP cad (đường dẫn
                // tuyệt đối: thư mục làm việc có thể khác cad\); công cụ đọc + Skill + 3 công cụ cad. Nối phiên nơi khác: --resume <id> --fork-session.
                // --add-dir / --plugin-dir trước --tools: các cờ này nhận nhiều giá trị, cờ kế tiếp mới cắt.
                doiSo = "-p --output-format stream-json --verbose --model " + model + " --effort " + effort
                    + " --mcp-config " + Ngoac(KhoPhien.FileMcp(cad)) + " --strict-mcp-config --setting-sources project,local"
                    + (phien != null ? " --resume " + phien + (tach ? " --fork-session" : "") : "") + themDir + coSkill
                    + " --tools Read Grep Glob Skill --allowedTools Read Grep Glob Skill mcp__cad__cad_tt mcp__cad__cad_chay mcp__cad__cad_anh";
            }
            else
            {
                exe = Tim(new[] { "agy.exe" }, new[] { Path.Combine(Environment.GetEnvironmentVariable("LOCALAPPDATA") ?? "", "agy", "bin") }, out quaCmd);
                if (exe == null) { Ghi(null, CAI_AGY, GiaoDien.Do); return; }
                // agy nhận lời nhắn qua -p (exe Go đọc dòng lệnh UTF-16 nên chữ Việt giữ nguyên); --mode plan = chỉ đọc.
                doiSo = "-p " + Ngoac(gui1) + " --output-format stream-json --mode plan --model " + model
                    + (phien != null ? " --conversation " + phien : "") + themDir + coSkill;
            }
            ProcessStartInfo si = quaCmd ? new ProcessStartInfo("cmd.exe", "/d /s /c \"\"" + exe + "\" " + doiSo + "\"") : new ProcessStartInfo(exe, doiSo);
            si.UseShellExecute = false;
            si.CreateNoWindow = true;
            si.WorkingDirectory = lam;
            si.RedirectStandardInput = si.RedirectStandardOutput = si.RedirectStandardError = true;
            si.StandardOutputEncoding = si.StandardErrorEncoding = new UTF8Encoding(false);
            si.EnvironmentVariables.Remove("ANTHROPIC_API_KEY");
            si.EnvironmentVariables.Remove("CLAUDECODE");
            si.EnvironmentVariables["NO_COLOR"] = "1";

            Ghi("Bạn", cau + kem, GiaoDien.Xanh);
            nhap.Clear();
            anh.Clear(); // đính kèm đi theo lời nhắn này; hội thoại tiếp vẫn nhớ (--resume)
            vungId = null;
            vungDoc = null;
            VeDinhKem();
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
            batDau = DateTime.Now;
            viec = "đang nghĩ";
            CapNhat();
            dongHo.Start();
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
            if (j.TryGetValue("session_id", out v) && v is string)
            {
                if ((string)v != phien) tach = false; // đã tách nhánh: từ giờ nói tiếp nhánh mới
                phien = (string)v;
            }
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
                    if (kieu == "text" && k.TryGetValue("text", out t)) { Ghi("Claude", Convert.ToString(t).Trim(), GiaoDien.ChuDam); viec = "đang viết"; }
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
            viec = "đang chạy " + ten;
            CapNhat();
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
            dongHo.Stop();
            int s = (int)(DateTime.Now - batDau).TotalSeconds;
            trangThai.Text = (ma == 0 ? "Xong" : "Dừng (mã " + ma + ")") + " · " + s / 60 + ":" + (s % 60).ToString("00");
            q.Dispose();
            p = null;
            gui.Text = "Gửi";
            gui.Invalidate();
        }
    }
}
