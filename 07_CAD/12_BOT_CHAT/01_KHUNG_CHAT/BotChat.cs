// Plugin Bot chat (bundle riêng TKPCCC_BotChat.bundle, lệnh TKBOTCHAT; tách khỏi palette PCCC 17/11).
// - Claude: `claude -p` (đăng nhập Claude Code, không API key) chạy NỀN với MCP cad, vẽ / sửa được bản vẽ; giữ hội thoại bằng --resume.
// - Antigravity (Gemini...): `agy -p` chế độ plan = chỉ đọc + trả lời; chưa có MCP cad nên KHÔNG vẽ; giữ hội thoại bằng --conversation.
// Người dùng 18/11, theo ảnh khung chat mẫu (Claude / VS Code): bố cục thanh đầu (dự án ▾, mới, lịch sử, cài đặt) · bong bóng tin · dòng trạng thái
// "✻ … m:ss · effort" · khung soạn (chip Link / vùng chọn / thư mục / skill / ảnh, ô nhập, nút + và /, nút model "Opus 5.5 · High", nút gửi tròn).
// - Enter gửi (bot đang chạy: xếp hàng), Ctrl+X gửi gấp vào lượt đang chạy (19/11), Shift+Enter xuống dòng; Ctrl+V / kéo thả / "+ → Ảnh" đính kèm ảnh
//   (%LOCALAPPDATA%\TKPCCC\botchat\anh, AI xem bằng Read qua --add-dir).
// - Cài đặt "Nối phiên chung" (19/11): VS Code / Claude Code = phiên chọn ở Lịch sử ghi thẳng vào phiên gốc, mở hội thoại bên đó bằng link.
// - Vùng chọn: đối tượng đang chọn trên CAD (hoặc TKBOTCHON) gửi kèm handle, thành phần, hộp bao để AI đọc (doc gi:"dt") và sửa (sua / xoa theo "h").
// - Dự án = thư mục làm việc của CLI (CLAUDE.md, skill .claude\skills); khác 07_CAD\ thì vẫn --add-dir 07_CAD\. Chỗ chứa skill, link phiên: PhienAi.cs.
// Lưu ở cai_dat.json: chat_ai, model, chat_effort, model_gemini, chat_thu_muc, chat_du_an, chat_du_an_ds, chat_skill, agy_models, chat_noi, chat_tk (tu_dong | pro | fp), chat_ultracode.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using AcAp = Autodesk.AutoCAD.ApplicationServices;
using Db = Autodesk.AutoCAD.DatabaseServices;

namespace TKPCCC
{
    partial class BotChat : UserControl
    {
        public const string MAU_PDF = "Vẽ theo quy trình 6 bước từ PDF: ";
        public const string MAU_SO_DO = "Vẽ sơ đồ nguyên lý báo cháy từ mặt bằng đang mở";
        const string CAI_CLAUDE = "Không tìm thấy lệnh claude. Cài Claude Code (npm i -g @anthropic-ai/claude-code) rồi thử lại; đăng nhập: nút model → Tài khoản → Đăng nhập Claude Pro (hoặc chọn Cổng FP).";
        const string CAI_AGY = "Không tìm thấy agy (Antigravity CLI). Cài Antigravity, mở terminal gõ agy một lần để đăng nhập Google, rồi thử lại.";
        static readonly string[] TEN_AI = { "Claude", "Antigravity" };
        static readonly string THU_MUC_ANH = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TKPCCC", "botchat", "anh");
        const int TOI_DA_HANDLE = 200; // handle gửi kèm tối đa: vùng lớn hơn thì AI lọc thêm bằng hop + l

        // Giao diện
        readonly KhungCuon dsTin = new KhungCuon();               // danh sách bong bóng (tự xếp, cuộn)
        readonly ONhap nhap = new ONhap();
        readonly Label goiYNhap = new Label();            // chữ mờ "Thêm yêu cầu…" khi ô nhập trống
        readonly Label trangThai = new Label();
        readonly FlowLayoutPanel chip = new FlowLayoutPanel();
        readonly Panel khungSoan = new Panel();
        readonly Chip nutDuAn, nutModel;
        readonly NutIcon nutGui;
        readonly ToolTip goiY = new ToolTip();
        readonly Timer dongHo = new Timer();

        // Trạng thái
        readonly List<string> thuMuc, duAnDs, skill, anh = new List<string>();
        int ai;                 // 0 Claude, 1 Antigravity
        string modelClaude, effortClaude, modelAgy; // modelAgy = tên agy đầy đủ (gốc-mức hoặc model không mức)
        AcAp.Document vungDoc;
        Db.ObjectId[] vungId;   // vùng quét chọn trên CAD; null = không có
        bool vungGiu;           // chọn bằng TKBOTCHON: giữ cả khi người dùng bỏ chọn trên bản vẽ
        string linkTen;         // phiên đang nối từ nơi khác; null = hội thoại của Bot chat
        bool tach;              // lần gửi kế thêm --fork-session: chạy tiếp ngữ cảnh phiên Claude nơi khác mà không ghi vào phiên gốc
        List<KeyValuePair<bool, string>> tinCu; // tin cũ của phiên vừa link chưa hiện ("Hiện tin cũ hơn")
        Process p;
        string phien;           // session_id (claude) hoặc conversation_id (agy) để nói tiếp
        string banVeTruoc;      // bản vẽ của lời nhắn trước (Document.Name): đổi thì báo AI
        string phienCwd;        // thư mục làm việc của `phien` (Claude tìm phiên theo thư mục): mở phiên bên VS Code cần
        string noi;             // nối phiên chung (cai_dat chat_noi): "" = link là nhánh riêng, "vscode" | "claude" = ghi thẳng vào phiên gốc, mở được bên đó
        bool noiChung;          // phiên đang link ghi thẳng vào phiên gốc (không --fork-session)
        int aiPhien = -1;       // máy AI của `phien`: đổi máy thì mở hội thoại mới
        DateTime batDau;
        string viec = "";
        readonly StringBuilder loi = new StringBuilder();

        public BotChat()
        {
            Font = GiaoDien.Chu;
            BackColor = GiaoDien.Nen;
            thuMuc = DocDs("chat_thu_muc");
            duAnDs = DocDs("chat_du_an_ds");
            skill = DocDs("chat_skill");
            ai = GiaoDien.DocCaiDat("chat_ai", "claude") == "gemini" ? 1 : 0;
            modelClaude = GiaoDien.DocCaiDat("model", DanhMucModel.Claude[0].Id);
            if (modelClaude == "opus" || modelClaude == "sonnet") modelClaude = "claude-" + modelClaude + "-5-5"; // khoá cũ (bí danh) trước 18/11
            effortClaude = GiaoDien.DocCaiDat("chat_effort", "high");
            modelAgy = GiaoDien.DocCaiDat("model_gemini", "gemini-4-argon-high");
            noi = GiaoDien.DocCaiDat("chat_noi", "");
            if (noi != "vscode" && noi != "claude") noi = "";
            dongHo.Interval = 1000;
            dongHo.Tick += delegate { CapNhat(); };

            // ---- Danh sách tin ----
            dsTin.Dock = DockStyle.Fill;
            dsTin.AutoScroll = true;
            dsTin.BackColor = GiaoDien.Nen;
            dsTin.Resize += delegate { XepTin(false); };
            Controls.Add(dsTin);

            // ---- Thanh đầu: dự án ▾ (giữa) · mới · lịch sử · cài đặt (phải) ----
            Panel dau = new Panel();
            dau.Dock = DockStyle.Top;
            dau.Height = 38;
            dau.BackColor = GiaoDien.Nen;
            nutDuAn = new Chip(Ve.THU_MUC, "", false);
            nutDuAn.Dam = true;
            nutDuAn.BamChip += MenuDuAn;
            nutDuAn.AllowDrop = true;
            nutDuAn.DragEnter += delegate(object s, DragEventArgs e) { e.Effect = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Link : DragDropEffects.None; };
            nutDuAn.DragDrop += delegate(object s, DragEventArgs e)
            {
                string d = ((string[])e.Data.GetData(DataFormats.FileDrop) ?? new string[0]).FirstOrDefault(Directory.Exists);
                if (d != null) GiaoDien.An(delegate { DoiDuAn(d); });
            };
            NutIcon moi = new NutIcon(Ve.THEM, "Hội thoại mới", goiY), lichSu = new NutIcon(Ve.LICH_SU, "Lịch sử / Link phiên VS Code, Claude Code, Antigravity", goiY),
                caiDat = new NutIcon(Ve.CAI_DAT, "Cài đặt: thư mục gắn kèm, chỗ chứa skill, model Antigravity", goiY);
            moi.Click += delegate { GiaoDien.An(HoiThoaiMoi); };
            lichSu.Click += delegate { GiaoDien.An(MoLichSu); };
            caiDat.Click += delegate { GiaoDien.An(MenuCaiDat); };
            dau.Controls.AddRange(new Control[] { nutDuAn, moi, lichSu, caiDat });
            dau.Resize += delegate
            {
                caiDat.Location = new Point(dau.Width - 34, 5);
                lichSu.Location = new Point(dau.Width - 64, 5);
                moi.Location = new Point(dau.Width - 94, 5);
                nutDuAn.Location = new Point(Math.Max(6, Math.Min((dau.Width - nutDuAn.Width) / 2, dau.Width - 100 - nutDuAn.Width)), 7);
            };
            Controls.Add(dau);
            VeDuAn();

            // ---- Khung soạn: chip · ô nhập · thanh dưới (+ / model … gửi) ----
            khungSoan.Dock = DockStyle.Bottom;
            khungSoan.BackColor = GiaoDien.Nen;
            khungSoan.Padding = new Padding(10, 6, 10, 8);
            khungSoan.Paint += delegate(object s, PaintEventArgs e)
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                Rectangle r = new Rectangle(6, 2, khungSoan.Width - 13, khungSoan.Height - 6);
                using (GraphicsPath gp = GiaoDien.BoGoc(r, 12))
                {
                    using (SolidBrush b = new SolidBrush(GiaoDien.The)) e.Graphics.FillPath(b, gp);
                    using (Pen v = new Pen(Ve.Tron(GiaoDien.The, GiaoDien.ChuMo, 0.45f))) e.Graphics.DrawPath(v, gp);
                }
            };
            chip.Dock = DockStyle.Top;
            chip.AutoSize = true;
            chip.BackColor = GiaoDien.The;
            chip.Padding = new Padding(2, 4, 2, 0);
            chip.SizeChanged += delegate { DoCaoSoan(); };
            nhap.Multiline = true;
            nhap.ScrollBars = ScrollBars.None;
            nhap.BorderStyle = BorderStyle.None;
            nhap.Dock = DockStyle.Fill;
            nhap.BackColor = GiaoDien.The;
            nhap.ForeColor = GiaoDien.ChuDam;
            nhap.Font = new Font("Segoe UI", 9.5f);
            nhap.KeyDown += delegate(object s, KeyEventArgs e)
            {
                if (PhimGach(e)) return; // bảng "/" đang mở: lên/xuống/Enter/Tab/Esc thuộc bảng (LenhGach.cs)
                if (e.KeyCode == Keys.Enter && !e.Shift) { e.SuppressKeyPress = true; GiaoDien.An(GuiNhap); }
                // Ctrl+X không bôi đen chữ = gửi gấp (người dùng 19/11); có bôi đen thì vẫn là Cắt như thường.
                else if (e.KeyCode == Keys.X && e.Control && !e.Shift && !e.Alt && nhap.SelectionLength == 0) { e.SuppressKeyPress = true; GiaoDien.An(GuiGap); }
            };
            // Palette mất focus khi chuột rời khỏi nó (AutoCAD trả phím về dòng lệnh): giữ focus khi đang gõ ở ô nhập (LenhBotChat gán GiuFocus).
            nhap.GotFocus += delegate { if (GiuFocus != null) GiuFocus(true); };
            nhap.LostFocus += delegate
            {
                if (GiuFocus != null) GiuFocus(false);
                // focus sang chỗ khác trong bot (bấm một tin...) thì đóng bảng "/"; chỉ palette mất focus thì giữ
                BeginInvoke((Action)delegate { if (ContainsFocus && !nhap.Focused) AnGach(); });
            };
            nhap.PreviewKeyDown += delegate(object s, PreviewKeyDownEventArgs e) { if (e.KeyCode == Keys.Tab && bang != null && bang.Visible) e.IsInputKey = true; };
            dang = this;
            if (!daMoc) { daMoc = true; AcAp.Application.PreTranslateMessage += ChuyenPhim; }
            nhap.TextChanged += delegate { goiYNhap.Visible = nhap.TextLength == 0; LocGach(); };
            nhap.DanAnh = DanAnh;
            nhap.AllowDrop = true;
            nhap.DragEnter += delegate(object s, DragEventArgs e) { e.Effect = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None; };
            nhap.DragDrop += delegate(object s, DragEventArgs e)
            {
                GiaoDien.An(delegate
                {
                    foreach (string f in (string[])e.Data.GetData(DataFormats.FileDrop) ?? new string[0])
                        if (LaAnh(f)) ThemAnh(ChepAnh(f));
                        else nhap.AppendText((nhap.TextLength > 0 ? " " : "") + f); // file / thư mục khác: chèn đường dẫn để AI đọc
                });
            };
            goiYNhap.Text = "Thêm yêu cầu…";
            goiY.SetToolTip(nhap, "Enter gửi (bot đang chạy: xếp hàng), Ctrl+X gửi gấp vào lượt đang chạy (ô trống: gửi ngay lời nhắn đang xếp hàng), Shift+Enter xuống dòng, Ctrl+V dán ảnh, kéo thả ảnh / file vào đây");
            goiYNhap.ForeColor = GiaoDien.ChuMo;
            goiYNhap.BackColor = GiaoDien.The;
            goiYNhap.Font = nhap.Font;
            goiYNhap.AutoSize = true;
            goiYNhap.Location = new Point(1, 1);
            goiYNhap.Click += delegate { nhap.Focus(); };
            nhap.Controls.Add(goiYNhap);
            Panel thanhDuoi = new Panel();
            thanhDuoi.Dock = DockStyle.Bottom;
            thanhDuoi.Height = 32;
            thanhDuoi.BackColor = GiaoDien.The;
            NutIcon cong = new NutIcon(Ve.THEM, "Đính kèm: ảnh, vùng CAD, thư mục, skill, câu mẫu", goiY);
            cong.Click += delegate { GiaoDien.An(MenuCong); };
            Chip gach = new Chip(null, "/", false);
            goiY.SetToolTip(gach, "Lệnh, skill (như Claude Code): gõ / đầu ô nhập, /help xem hết");
            gach.BamChip += MoGach;
            nutModel = new Chip(null, "", false);
            nutModel.BamChip += MenuModel;
            goiY.SetToolTip(nutModel, "Model và effort (mức suy nghĩ: cao hơn = kỹ hơn, chậm và tốn hạn mức hơn)");
            nutGui = new NutIcon(Ve.GUI, "Gửi (Enter)", goiY);
            nutGui.Nen = GiaoDien.XanhDam;
            nutGui.Click += delegate { GiaoDien.An(delegate { if (p != null) Dung(); else GuiNhap(); }); };
            thanhDuoi.Controls.AddRange(new Control[] { cong, gach, nutModel, nutGui });
            thanhDuoi.Resize += delegate
            {
                cong.Location = new Point(0, 3);
                gach.Location = new Point(32, 4);
                nutModel.Location = new Point(gach.Right + 6, 4);
                nutGui.Location = new Point(thanhDuoi.Width - 30, 2);
            };
            khungSoan.Controls.Add(nhap);
            khungSoan.Controls.Add(chip);
            khungSoan.Controls.Add(thanhDuoi);
            // Dòng trạng thái "✻ … m:ss · effort" nằm ngay trên khung soạn như mẫu: thêm trước khungSoan (neo đáy sau, nên nằm trên nó).
            trangThai.Dock = DockStyle.Bottom;
            trangThai.Height = 22;
            trangThai.Padding = new Padding(12, 4, 6, 0);
            trangThai.Font = GiaoDien.Nho;
            trangThai.ForeColor = GiaoDien.ChuMo;
            trangThai.AutoEllipsis = true;
            Controls.Add(trangThai);
            Controls.Add(khungSoan);

            VeModel();
            VeChip();
            trangThai.Text = ai == 0 ? "Claude: đọc, ghi file trong dự án, vẽ, sửa CAD." : "Antigravity: chỉ đọc + trả lời (chưa vẽ CAD).";
            DanhMucModel.LamMoiAgy(TimAgy());
        }

        static List<string> DocDs(string khoa) { return GiaoDien.DocCaiDat(khoa, "").Split('|').Where(d => d != "").ToList(); }

        // ---------- Phím trong palette (người dùng 19/11: "như VS Code: vừa chat, vừa xuống hàng, vừa gửi ảnh") ----------
        // AutoCAD dịch phím tắt trước khi tới palette (Ctrl+V = PASTECLIP, Enter vào dòng lệnh): phím gõ ở ô nhập thì tự chuyển thẳng cho ô nhập.
        public Action<bool> GiuFocus;
        static BotChat dang;
        static bool daMoc;

        [System.Runtime.InteropServices.DllImport("user32.dll")] static extern bool TranslateMessage(ref System.Windows.Interop.MSG m);
        // Bắt buộc bản W: mặc định DllImport là Ansi => DispatchMessageA đổi WM_CHAR Unicode sang mã ANSI, chữ Việt (Unikey) thành "¿"/"Ấ" (lỗi 19/11).
        [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)] static extern IntPtr DispatchMessage(ref System.Windows.Interop.MSG m);

        static void ChuyenPhim(object s, AcAp.PreTranslateMessageEventArgs e)
        {
            System.Windows.Interop.MSG m = e.Message;
            if (m.message < 0x100 || m.message > 0x102) return; // WM_KEYDOWN, WM_KEYUP, WM_CHAR
            BotChat b = dang;
            if (b == null || b.IsDisposed || !b.nhap.IsHandleCreated) return;
            if (m.hwnd != b.nhap.Handle)
            {
                // ô chữ một tin của bot đang focus (Ctrl+A / Ctrl+C chép tin, người dùng 27/11): cũng chuyển thẳng, không để AutoCAD bắt
                Control c = Control.FromHandle(m.hwnd);
                if (!(c is TextBox) || !(c.Parent is BongBong) || !b.dsTin.Contains(c)) return;
            }
            TranslateMessage(ref m);
            DispatchMessage(ref m);
            e.Handled = true;
        }

        // ---------- Gửi, xếp hàng, gửi gấp, dừng ----------
        readonly Queue<string> hangCho = new Queue<string>();
        bool vaoMo; // stdin của lượt Claude đang chạy còn mở (--input-format stream-json): ghi thêm lời nhắn gấp được

        /// Ctrl+X (người dùng 19/11, như VS Code / Claude Code): bot đang chạy thì gửi ngay vào lượt đó, không xếp hàng. Claude nhận lời nhắn ở chỗ nghỉ
        /// kế tiếp giữa hai bước công cụ và làm tiếp gộp với yêu cầu trước (dò 19/11 trên claude 2.1.326: một lượt, một result). Không chạy thì gửi như Enter.
        void GuiGap()
        {
            if (p == null) { GuiNhap(); return; }
            string cau = nhap.Text.Trim();
            // Người dùng 26/11: lỡ Enter (lời nhắn đã vào hàng chờ) thì Ctrl+X ở ô trống gửi ngay các lời nhắn đang xếp hàng.
            bool tuHang = cau == "" && hangCho.Count > 0;
            if (tuHang) cau = string.Join("\n\n", hangCho);
            if (cau == "") return;
            if (!vaoMo)
            {
                if (tuHang) { trangThai.Text = ai == 0 ? "Lượt này đang kết thúc: lời nhắn đang xếp hàng sẽ gửi ngay sau." : "Antigravity chưa nhận lời nhắn giữa lượt: lời nhắn vẫn xếp hàng."; return; }
                GuiNhap();
                trangThai.Text = ai == 0 ? "Lượt này đang kết thúc: lời nhắn xếp hàng." : "Antigravity chưa nhận lời nhắn giữa lượt: lời nhắn xếp hàng.";
                return;
            }
            AcAp.Document dv = AcAp.Core.Application.DocumentManager.MdiActiveDocument;
            string kem, gui = Soan(cau, null, out kem);
            if (dv != null && banVeTruoc != null && !string.Equals(dv.Name, banVeTruoc, StringComparison.OrdinalIgnoreCase))
                gui = "(Lời nhắn gửi gấp lúc tôi đang xem \"" + Path.GetFileName(dv.Name) + "\"; công cụ cad của lượt đang chạy vẫn làm trên \""
                    + Path.GetFileName(banVeTruoc) + "\", việc cho bản vẽ kia để lượt sau.)\n\n" + gui;
            if (!GhiVao(p, gui)) { vaoMo = false; if (!tuHang) GuiNhap(); return; }
            if (tuHang) hangCho.Clear(); else nhap.Clear();
            Ghi("Bạn", "[gửi gấp] " + cau + kem, GiaoDien.Xanh, anh.Where(File.Exists).ToList());
            BoDinhKem();
        }

        /// Một lời nhắn người dùng dạng stream-json vào stdin của claude.
        static bool GhiVao(Process q, string chu)
        {
            try
            {
                byte[] b = new UTF8Encoding(false).GetBytes(KhoPhien.DongNguoiDung(chu) + "\n"); // UTF-8 qua stdin: claude.cmd qua cmd làm hỏng chữ Việt trên dòng lệnh
                q.StandardInput.BaseStream.Write(b, 0, b.Length);
                q.StandardInput.BaseStream.Flush();
                return true;
            }
            catch (System.Exception) { return false; } // CLI đã thoát: lỗi thật đọc ở stderr khi Xong
        }

        /// Hết lượt (dòng result): đóng stdin để claude thoát. Lời nhắn gấp tới sát lúc này claude đã đọc thì nó chạy thêm một lượt rồi mới thoát.
        void DongVao()
        {
            if (!vaoMo || p == null) return;
            vaoMo = false;
            try { p.StandardInput.Close(); } catch (System.Exception) { }
        }

        /// Enter / nút gửi: đang chạy thì xếp hàng (gửi khi xong lượt này, như VS Code), không thì gửi ngay.
        void GuiNhap()
        {
            string cau = nhap.Text.Trim();
            if (cau == "") return;
            nhap.Clear();
            if (ChayLenh(cau)) return; // lệnh bot "/usage", "/effort high"...: chạy ngay, cả khi đang chạy
            if (p != null)
            {
                hangCho.Enqueue(cau);
                Ghi(null, "Xếp hàng (gửi khi xong lượt này; Ctrl+X để gửi ngay): " + cau, GiaoDien.ChuMo);
                return;
            }
            if (!Gui(cau)) nhap.Text = cau; // chưa gửi được (thiếu CLI, repo): trả chữ lại ô nhập
        }

        void Dung()
        {
            int n = hangCho.Count;
            hangCho.Clear();
            try { p.Kill(); } catch (System.Exception) { }
            if (n > 0) Ghi(null, "Đã dừng, bỏ " + n + " lời nhắn đang xếp hàng.", GiaoDien.ChuMo);
        }

        void DoCaoSoan() { khungSoan.Height = chip.Height + 30 + 64 + 14; }

        // ---------- Danh sách tin ----------

        /// Một tin: "Bạn" = người dùng; null = dòng hệ thống / công cụ; tên khác = AI.
        void Ghi(string ten, string chu, Color mau, IEnumerable<string> anhKem = null)
        {
            // Người dùng 26/11: đang kéo lên đọc tin cũ thì tin mới không giật xuống đáy; chỉ cuộn khi đang ở đáy hoặc mình vừa gửi.
            bool oDay = -dsTin.AutoScrollPosition.Y + dsTin.ClientSize.Height >= dsTin.DisplayRectangle.Height - 30;
            dsTin.Controls.Add(new BongBong(ten == "Bạn" ? 0 : ten == null ? 2 : 1, chu, mau, anhKem));
            XepTin(oDay || ten == "Bạn");
        }

        void XoaTin()
        {
            foreach (Control c in dsTin.Controls.Cast<Control>().ToList()) c.Dispose();
            dsTin.Controls.Clear();
            tinCu = null;
        }

        /// Xếp lại bong bóng theo bề rộng khung: người dùng lệch phải, AI lệch trái. cuoi = cuộn xuống tin cuối.
        void XepTin(bool cuoi)
        {
            // Người dùng 26/11: hạ AutoCAD xuống là đơ. Thu nhỏ thì khung rộng ~0: xếp ở bề rộng âm làm thanh cuộn bật/tắt -> Resize -> xếp lại mãi.
            // Khung quá hẹp thì để nguyên, mở lại cửa sổ sẽ có Resize xếp lại; đang xếp thì không xếp lồng.
            if (dangXep || dsTin.ClientSize.Width < 120 || dsTin.ClientSize.Height < 40) { choCuoi |= cuoi; return; }
            dangXep = true;
            try { XepTin0(cuoi || choCuoi); choCuoi = false; } finally { dangXep = false; }
        }

        bool dangXep, choCuoi; // choCuoi: tin mới tới lúc đang thu nhỏ, mở lại thì cuộn xuống

        void XepTin0(bool cuoi)
        {
            int rong = dsTin.ClientSize.Width - 20, y = dsTin.AutoScrollPosition.Y + 10;
            dsTin.SuspendLayout();
            foreach (Control c in dsTin.Controls)
            {
                BongBong b = c as BongBong;
                if (b == null) { c.Location = new Point(12, y); y += c.Height + 8; continue; } // nhãn "Hiện tin cũ hơn"
                int cao = b.DoCo(rong);
                int truocSau = b.Kieu == 2 ? 2 : 10;
                b.Location = new Point(b.Kieu == 0 ? 10 + rong - b.Width : 12, y);
                y += cao + truocSau;
            }
            dsTin.ResumeLayout();
            if (cuoi) dsTin.AutoScrollPosition = new Point(0, dsTin.DisplayRectangle.Height);
        }

        // ---------- Khung soạn: chip ----------

        static Chip TaoChip(string glyph, string chu, Action bo, Action bam)
        {
            Chip c = new Chip(glyph, chu, bo != null);
            if (bo != null) c.Bo += bo;
            if (bam != null) c.BamChip += bam;
            return c;
        }

        /// Hàng chip trên ô nhập: link phiên, vùng chọn CAD (hoặc nút quét vùng), thư mục gắn kèm, chỗ chứa skill, ảnh.
        void VeChip()
        {
            chip.SuspendLayout();
            List<Control> cu = chip.Controls.Cast<Control>().ToList();
            chip.Controls.Clear();
            foreach (Control c in cu) { if (c.BackgroundImage != null) c.BackgroundImage.Dispose(); c.Dispose(); }
            if (linkTen != null)
            {
                Chip lk = TaoChip(Ve.LINK, linkTen, HoiThoaiMoi, noi != "" ? (Action)MoBenKia : null);
                goiY.SetToolTip(lk, (noiChung ? "Nối chung: lời nhắn ghi thẳng vào phiên gốc." : "Đang nối phiên này (nhánh riêng, phiên gốc giữ nguyên).")
                    + (noi != "" ? " Bấm để mở trong " + TenNoi() + "." : "") + " Bấm ✕ để bỏ (hội thoại mới)");
                chip.Controls.Add(lk);
            }
            Chip v = vungId != null
                ? TaoChip(Ve.CHON, vungId.Length + " đối tượng đang chọn", delegate { vungId = null; vungDoc = null; VeChip(); }, null)
                : TaoChip(Ve.CHON, "Vùng CAD", null, QuetVung);
            goiY.SetToolTip(v, vungId != null ? "Gửi kèm lời nhắn kế: handle, thành phần, hộp bao của vùng chọn" : "Quét chọn đối tượng trên bản vẽ để AI đọc và sửa đúng phần đó (TKBOTCHON)");
            chip.Controls.Add(v);
            foreach (string d in thuMuc)
            {
                string dd = d;
                Chip c = TaoChip(Ve.THU_MUC, Path.GetFileName(dd.TrimEnd('\\')), delegate { thuMuc.Remove(dd); LuuDs(); }, null);
                goiY.SetToolTip(c, "Thư mục gắn kèm: " + dd);
                chip.Controls.Add(c);
            }
            foreach (string d in skill)
            {
                string dd = d;
                Chip c = TaoChip(Ve.SKILL, Path.GetFileName(dd.TrimEnd('\\')), delegate { skill.Remove(dd); LuuDs(); }, null);
                goiY.SetToolTip(c, "Chỗ chứa skill: " + dd);
                chip.Controls.Add(c);
            }
            foreach (string f in anh)
            {
                string ff = f;
                Label t = new Label();
                t.Size = new Size(56, 42);
                t.Margin = new Padding(0, 2, 6, 2);
                t.BackColor = GiaoDien.Nen;
                t.BackgroundImage = ThuNho(ff);
                t.BackgroundImageLayout = ImageLayout.Zoom;
                t.Text = t.BackgroundImage == null ? "ảnh ✕" : "✕";
                t.TextAlign = ContentAlignment.TopRight;
                t.ForeColor = t.BackgroundImage == null ? GiaoDien.ChuDam : Color.White;
                t.Font = GiaoDien.Nho;
                t.Cursor = Cursors.Hand;
                // Người dùng 26/11: bấm ảnh = xem ảnh; chỉ bấm ✕ góc trên phải mới bỏ.
                t.MouseClick += delegate(object s, MouseEventArgs e)
                {
                    if (e.Button != MouseButtons.Left) return;
                    if (e.X >= t.Width - 16 && e.Y <= 16) { anh.Remove(ff); VeChip(); }
                    else try { Process.Start(ff); } catch (System.Exception) { }
                };
                goiY.SetToolTip(t, ff + "\nBấm ảnh để xem, bấm ✕ để bỏ");
                chip.Controls.Add(t);
            }
            chip.ResumeLayout();
            DoCaoSoan();
        }

        void LuuDs()
        {
            GiaoDien.LuuCaiDat("chat_thu_muc", string.Join("|", thuMuc));
            GiaoDien.LuuCaiDat("chat_skill", string.Join("|", skill));
            VeChip();
        }

        /// Menu bật dưới nút ở thanh đầu, bật lên trên ở khung soạn (cuối palette).
        static void HienMenu(ContextMenuStrip m, Control nut)
        {
            m.Font = GiaoDien.Chu;
            if (nut.PointToScreen(Point.Empty).Y > Screen.FromControl(nut).WorkingArea.Height / 2) m.Show(nut, Point.Empty, ToolStripDropDownDirection.AboveRight);
            else m.Show(nut, new Point(0, nut.Height));
        }

        static ToolStripLabel Nhom(string ten)
        {
            ToolStripLabel l = new ToolStripLabel(ten);
            l.ForeColor = Color.Gray;
            l.Font = GiaoDien.Nho;
            return l;
        }

        /// Nút "+": ảnh, dán ảnh, vùng CAD, thư mục gắn kèm, chỗ chứa skill, câu mẫu.
        void MenuCong()
        {
            ContextMenuStrip m = new ContextMenuStrip();
            m.Items.Add("Ảnh…", null, delegate { GiaoDien.An(ChonAnh); });
            m.Items.Add("Dán ảnh từ clipboard  (Ctrl+V)", null, delegate { GiaoDien.An(delegate { if (!DanAnh()) trangThai.Text = "Clipboard không có ảnh."; }); });
            m.Items.Add("Quét vùng CAD  (TKBOTCHON)", null, delegate { GiaoDien.An(QuetVung); });
            m.Items.Add(new ToolStripSeparator());
            m.Items.Add("Thư mục gắn kèm…", null, delegate { GiaoDien.An(ThemThuMuc); });
            m.Items.Add("Chỗ chứa skill…", null, delegate { GiaoDien.An(ThemSkill); });
            m.Items.Add(new ToolStripSeparator());
            m.Items.Add("Mẫu: vẽ 6 bước từ PDF…", null, delegate
            {
                GiaoDien.An(delegate
                {
                    using (OpenFileDialog d = new OpenFileDialog())
                    {
                        d.Filter = "PDF|*.pdf";
                        DatCau(MAU_PDF + (d.ShowDialog() == DialogResult.OK ? d.FileName : "<đường dẫn>"));
                    }
                });
            });
            m.Items.Add("Mẫu: sơ đồ nguyên lý báo cháy", null, delegate { DatCau(MAU_SO_DO); });
            HienMenu(m, khungSoan);
        }

        /// Nút cài đặt: thư mục gắn kèm, chỗ chứa skill, mở thư mục dự án, làm mới model Antigravity.
        void MenuCaiDat()
        {
            ContextMenuStrip m = new ContextMenuStrip();
            m.Items.Add("Thư mục gắn kèm…", null, delegate { GiaoDien.An(ThemThuMuc); });
            m.Items.Add("Chỗ chứa skill…", null, delegate { GiaoDien.An(ThemSkill); });
            m.Items.Add("Mở thư mục dự án trong Explorer", null, delegate { string d = ThuMucLam(); if (d != null) Process.Start("explorer.exe", "\"" + d + "\""); });
            m.Items.Add("Làm mới danh sách model Antigravity (agy models)", null, delegate { DanhMucModel.LamMoiAgy(TimAgy()); trangThai.Text = "Đang đọc agy models…"; });
            // Người dùng 19/11: "cửa sổ link với VS Code hay Claude Code" = nối phiên chung, hội thoại mở tiếp được ở cả hai nơi.
            m.Items.Add(new ToolStripSeparator());
            m.Items.Add(Nhom("Nối phiên chung (Lịch sử → chọn phiên = ghi thẳng vào phiên đó)"));
            string[] ma = { "", "vscode", "claude" };
            string[] ten = { "Không: link = nhánh riêng, phiên gốc giữ nguyên", "VS Code (extension Claude Code)", "Claude Code (app)" };
            for (int i = 0; i < ma.Length; i++)
            {
                string mm = ma[i];
                ToolStripMenuItem it = new ToolStripMenuItem(ten[i], null, delegate
                {
                    noi = mm;
                    GiaoDien.LuuCaiDat("chat_noi", mm);
                    trangThai.Text = mm == "" ? "Link phiên = nhánh riêng." : "Nối phiên chung với " + TenNoi() + ": phiên chọn ở Lịch sử ghi thẳng vào phiên gốc.";
                    VeChip();
                });
                it.Checked = noi == mm;
                m.Items.Add(it);
            }
            if (noi != "")
                m.Items.Add("Mở hội thoại này trong " + TenNoi(), null, delegate { GiaoDien.An(MoBenKia); }).Enabled = phien != null && aiPhien == 0 && p == null;
            m.Show(Cursor.Position);
        }

        void CapNhat()
        {
            if (p == null) return;
            int s = (int)(DateTime.Now - batDau).TotalSeconds;
            string e = EffortDang();
            trangThai.Text = "✻ " + viec + "…  " + s / 60 + ":" + (s % 60).ToString("00") + (e != null ? " · " + Hoa(e) : "");
        }

        // ---------- Đính kèm: ảnh + vùng chọn trên CAD ----------

        public void DatCau(string cau)
        {
            nhap.Text = cau;
            nhap.Focus();
            nhap.SelectionStart = nhap.TextLength;
        }

        // ---------- Chạy CLI ----------

    }
}
