// Plugin Bot chat (bundle riêng TKPCCC_BotChat.bundle, lệnh TKBOTCHAT; tách khỏi palette PCCC 17/11).
// - Claude: `claude -p` (đăng nhập Claude Code, không API key) chạy NỀN với MCP cad, vẽ / sửa được bản vẽ; giữ hội thoại bằng --resume.
// - Antigravity (Gemini...): `agy -p` chế độ plan = chỉ đọc + trả lời; chưa có MCP cad nên KHÔNG vẽ; giữ hội thoại bằng --conversation.
// Người dùng 18/11, theo ảnh khung chat mẫu (Claude / VS Code): bố cục thanh đầu (dự án ▾, mới, lịch sử, cài đặt) · bong bóng tin · dòng trạng thái
// "✻ … m:ss · effort" · khung soạn (chip Link / vùng chọn / thư mục / skill / ảnh, ô nhập, nút + và /, nút model "Opus 5.5 · High", nút gửi tròn).
// - Enter gửi (bot đang chạy: xếp hàng), Ctrl+X gửi gấp vào lượt đang chạy (19/11), Shift+Enter xuống dòng; Ctrl+V / kéo thả / "+ → Ảnh" đính kèm ảnh
//   (%LOCALAPPDATA%\TKPCCC\botchat\anh, AI xem bằng Read qua --add-dir).
// - Cài đặt "Nối phiên chung" (19/11): VS Code / Claude Code = phiên chọn ở Lịch sử ghi thẳng vào phiên gốc, mở hội thoại bên đó bằng link.
// - Vùng chọn: đối tượng đang chọn trên CAD (hoặc TKBOTCHON) gửi kèm handle, thành phần, hộp bao để AI đọc (doc gi:"dt") và sửa (sua / xoa theo "h").
// - Dự án = thư mục làm việc của CLI (CLAUDE.md, skill .claude\skills); khác cad\ thì vẫn --add-dir cad\. Chỗ chứa skill, link phiên: PhienAi.cs.
// Lưu ở cai_dat.json: chat_ai, model, chat_effort, model_gemini, chat_thu_muc, chat_du_an, chat_du_an_ds, chat_skill, agy_models, chat_noi.
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
    class BotChat : UserControl
    {
        public const string MAU_PDF = "Vẽ theo quy trình 6 bước từ PDF: ";
        public const string MAU_SO_DO = "Vẽ sơ đồ nguyên lý báo cháy từ mặt bằng đang mở";
        const string CAI_CLAUDE = "Không tìm thấy lệnh claude. Cài Claude Code (npm i -g @anthropic-ai/claude-code), mở terminal gõ claude một lần để đăng nhập, rồi thử lại.";
        const string CAI_AGY = "Không tìm thấy agy (Antigravity CLI). Cài Antigravity, mở terminal gõ agy một lần để đăng nhập Google, rồi thử lại.";
        static readonly string[] TEN_AI = { "Claude", "Antigravity" };
        static readonly string THU_MUC_ANH = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TKPCCC", "botchat", "anh");
        const int TOI_DA_HANDLE = 200; // handle gửi kèm tối đa: vùng lớn hơn thì AI lọc thêm bằng hop + l

        // Giao diện
        readonly Panel dsTin = new Panel();               // danh sách bong bóng (tự xếp, cuộn)
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
                if (e.KeyCode == Keys.Enter && !e.Shift) { e.SuppressKeyPress = true; GiaoDien.An(GuiNhap); }
                // Ctrl+X không bôi đen chữ = gửi gấp (người dùng 19/11); có bôi đen thì vẫn là Cắt như thường.
                else if (e.KeyCode == Keys.X && e.Control && !e.Shift && !e.Alt && nhap.SelectionLength == 0) { e.SuppressKeyPress = true; GiaoDien.An(GuiGap); }
            };
            // Palette mất focus khi chuột rời khỏi nó (AutoCAD trả phím về dòng lệnh): giữ focus khi đang gõ ở ô nhập (LenhBotChat gán GiuFocus).
            nhap.GotFocus += delegate { if (GiuFocus != null) GiuFocus(true); };
            nhap.LostFocus += delegate { if (GiuFocus != null) GiuFocus(false); };
            dang = this;
            if (!daMoc) { daMoc = true; AcAp.Application.PreTranslateMessage += ChuyenPhim; }
            nhap.TextChanged += delegate { goiYNhap.Visible = nhap.TextLength == 0; };
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
            goiY.SetToolTip(nhap, "Enter gửi (bot đang chạy: xếp hàng), Ctrl+X gửi gấp vào lượt đang chạy, Shift+Enter xuống dòng, Ctrl+V dán ảnh, kéo thả ảnh / file vào đây");
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
            goiY.SetToolTip(gach, "Skill của dự án / chỗ chứa skill");
            gach.BamChip += MenuSkill;
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
            if (b == null || b.IsDisposed || !b.nhap.IsHandleCreated || m.hwnd != b.nhap.Handle) return;
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
            if (cau == "") return;
            if (!vaoMo)
            {
                GuiNhap();
                trangThai.Text = ai == 0 ? "Lượt này đang kết thúc: lời nhắn xếp hàng." : "Antigravity chưa nhận lời nhắn giữa lượt: lời nhắn xếp hàng.";
                return;
            }
            AcAp.Document dv = AcAp.Core.Application.DocumentManager.MdiActiveDocument;
            string kem, gui = Soan(cau, null, out kem);
            if (dv != null && banVeTruoc != null && !string.Equals(dv.Name, banVeTruoc, StringComparison.OrdinalIgnoreCase))
                gui = "(Lời nhắn gửi gấp lúc tôi đang xem \"" + Path.GetFileName(dv.Name) + "\"; công cụ cad của lượt đang chạy vẫn làm trên \""
                    + Path.GetFileName(banVeTruoc) + "\", việc cho bản vẽ kia để lượt sau.)\n\n" + gui;
            if (!GhiVao(p, gui)) { vaoMo = false; GuiNhap(); return; }
            nhap.Clear();
            Ghi("Bạn", "[gửi gấp] " + cau + kem, GiaoDien.Xanh);
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
            if (p != null)
            {
                hangCho.Enqueue(cau);
                Ghi(null, "Xếp hàng (gửi khi xong lượt này): " + cau, GiaoDien.ChuMo);
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
        void Ghi(string ten, string chu, Color mau)
        {
            dsTin.Controls.Add(new BongBong(ten == "Bạn" ? 0 : ten == null ? 2 : 1, chu, mau));
            XepTin(true);
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
            if (cuoi && dsTin.Controls.Count > 0) dsTin.ScrollControlIntoView(dsTin.Controls[dsTin.Controls.Count - 1]);
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
                t.Size = new Size(40, 30);
                t.Margin = new Padding(0, 2, 6, 2);
                t.BackColor = GiaoDien.Nen;
                t.BackgroundImage = ThuNho(ff);
                t.BackgroundImageLayout = ImageLayout.Zoom;
                t.Text = t.BackgroundImage == null ? "ảnh ✕" : "✕";
                t.TextAlign = ContentAlignment.TopRight;
                t.ForeColor = t.BackgroundImage == null ? GiaoDien.ChuDam : Color.White;
                t.Font = GiaoDien.Nho;
                t.Cursor = Cursors.Hand;
                t.Click += delegate { anh.Remove(ff); VeChip(); };
                goiY.SetToolTip(t, ff + "\nBấm để bỏ ảnh");
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

        string TenNoi() { return noi == "vscode" ? "VS Code" : "Claude Code"; }

        /// Phiên thuộc bên đang nối chung (VS Code: entrypoint claude-vscode; Claude Code: cli, claude-desktop).
        bool KhopNoi(PhienAi x) { return x.Ai == 0 && (noi == "vscode" ? x.Nguon == "VS Code" : noi == "claude" && (x.Nguon == "Claude Code" || x.Nguon == "Desktop")); }

        /// Mở hội thoại Claude đang có ở bên nối chung. VS Code: vscode://anthropic.claude-code/open?session= (extension 2.1.327 tìm phiên theo thư mục
        /// đang mở, nên mở thư mục của phiên trước, đợi cửa sổ lên rồi mới gửi link). Claude Code: claude://resume?session= (app nhập phiên CLI rồi mở).
        void MoBenKia()
        {
            if (phien == null || aiPhien != 0) { trangThai.Text = "Chưa có hội thoại Claude: gửi một lời nhắn hoặc nối một phiên trước."; return; }
            if (p != null) { trangThai.Text = "Đang chạy: đợi xong rồi mở bên kia (hai nơi cùng ghi một phiên sẽ rẽ nhánh)."; return; }
            try
            {
                if (noi == "claude") { Process.Start("claude://resume?session=" + phien); trangThai.Text = "Đã gửi phiên sang app Claude Code."; return; }
                string link = "vscode://anthropic.claude-code/open?session=" + phien;
                bool quaCmd;
                string code = Tim(new[] { "code.cmd", "code.exe" }, new string[0], out quaCmd);
                if (code == null || phienCwd == null || !Directory.Exists(phienCwd)) { Process.Start(link); trangThai.Text = "Đã gửi phiên sang VS Code."; return; }
                ProcessStartInfo si = quaCmd ? new ProcessStartInfo("cmd.exe", "/d /s /c \"\"" + code + "\" " + Ngoac(phienCwd) + "\"") : new ProcessStartInfo(code, Ngoac(phienCwd));
                si.UseShellExecute = false;
                si.CreateNoWindow = true;
                Process.Start(si).Dispose();
                Timer t = new Timer();
                t.Interval = 3000;
                t.Tick += delegate { t.Stop(); t.Dispose(); try { Process.Start(link); } catch (System.Exception) { } };
                t.Start();
                trangThai.Text = "Đang mở trong VS Code: " + phienCwd;
            }
            catch (System.Exception ex) { trangThai.Text = "Không mở được: " + ex.Message; }
        }

        void ThemThuMuc()
        {
            string d = ChonThuMuc.Hoi(this, "Thư mục gắn kèm để AI đọc làm ngữ cảnh", ThuMucLam());
            if (d == null || thuMuc.Contains(d, StringComparer.OrdinalIgnoreCase)) return;
            thuMuc.Add(d);
            LuuDs();
        }

        void ThemSkill()
        {
            string d = ChonThuMuc.Hoi(this, "Chỗ chứa skill (skills, plugin có skills, hoặc dự án có .claude\\skills)",
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".claude"));
            if (d == null || skill.Contains(d, StringComparer.OrdinalIgnoreCase)) return;
            skill.Add(d);
            LuuDs();
        }

        // ---------- Model ----------

        ModelAi ModelDang()
        {
            if (ai == 0) return DanhMucModel.Claude.FirstOrDefault(m => m.Id == modelClaude) ?? DanhMucModel.Claude[0];
            List<ModelAi> ds = DanhMucModel.Antigravity();
            return ds.FirstOrDefault(m => m.Effort == null ? m.Id == modelAgy : modelAgy.StartsWith(m.Id + "-") && m.Effort.Contains(modelAgy.Substring(m.Id.Length + 1)))
                ?? ds.FirstOrDefault(m => m.Id == "gemini-4-argon") ?? ds[0];
        }

        string EffortDang()
        {
            ModelAi m = ModelDang();
            if (m.Effort == null) return null;
            string e = ai == 0 ? effortClaude : (modelAgy.StartsWith(m.Id + "-") ? modelAgy.Substring(m.Id.Length + 1) : "high");
            return m.Effort.Contains(e) ? e : m.Effort.Contains("high") ? "high" : m.Effort.Last();
        }

        /// Tên model gửi cho CLI: Claude = id; agy = gốc-mức (hoặc id model không mức).
        string ModelCli()
        {
            ModelAi m = ModelDang();
            return ai == 0 || m.Effort == null ? m.Id : m.Id + "-" + EffortDang();
        }

        static string Hoa(string e) { return e == null ? "" : e == "xhigh" ? "XHigh" : char.ToUpper(e[0]) + e.Substring(1); }

        void VeModel()
        {
            ModelAi m = ModelDang();
            string e = EffortDang();
            nutModel.Text = m.Ten + (e != null ? "  " + Hoa(e) : "") + "  ▾";
            if (nutModel.Parent != null) nutModel.Location = new Point(nutModel.Location.X, 4);
        }

        void ChonModel(ModelAi m, string e)
        {
            if (p != null) { trangThai.Text = "Đang chạy: đợi xong (hoặc Dừng) rồi mới đổi model."; return; }
            ai = m.Ai;
            if (ai == 0)
            {
                modelClaude = m.Id;
                if (e != null) effortClaude = e;
                GiaoDien.LuuCaiDat("model", modelClaude);
                GiaoDien.LuuCaiDat("chat_effort", effortClaude);
            }
            else
            {
                string e2 = m.Effort == null ? null : (e ?? (m.Effort.Contains("high") ? "high" : m.Effort.Last()));
                modelAgy = e2 == null ? m.Id : m.Id + "-" + e2;
                GiaoDien.LuuCaiDat("model_gemini", modelAgy);
            }
            GiaoDien.LuuCaiDat("chat_ai", ai == 0 ? "claude" : "gemini");
            VeModel();
            trangThai.Text = ai == 0 ? "Claude: đọc, ghi file trong dự án, vẽ, sửa CAD." : "Antigravity: chỉ đọc + trả lời (chưa vẽ CAD).";
        }

        /// Menu model như khung chat mẫu: nhóm Claude (Opus, Sonnet các đời), nhóm Antigravity (Gemini 4, Gemini 3.8 Flash, ...), rồi các mức effort của model đang chọn.
        void MenuModel()
        {
            ModelAi dang = ModelDang();
            string eDang = EffortDang();
            ContextMenuStrip m = new ContextMenuStrip();
            Action<string, List<ModelAi>> nhom = delegate(string ten, List<ModelAi> ds)
            {
                m.Items.Add(Nhom(ten));
                foreach (ModelAi x in ds)
                {
                    ModelAi xx = x;
                    ToolStripMenuItem i = new ToolStripMenuItem(x.Ten, null, delegate { ChonModel(xx, null); });
                    i.Checked = x.Ai == dang.Ai && x.Id == dang.Id;
                    m.Items.Add(i);
                }
            };
            nhom("Claude  (claude: đọc, ghi file, vẽ, sửa CAD)", DanhMucModel.Claude);
            m.Items.Add(new ToolStripSeparator());
            nhom("Antigravity  (agy: chỉ đọc)", DanhMucModel.Antigravity());
            if (dang.Effort != null)
            {
                m.Items.Add(new ToolStripSeparator());
                m.Items.Add(Nhom("Effort  (" + dang.Ten + ")"));
                foreach (string e in dang.Effort)
                {
                    string ee = e;
                    ToolStripMenuItem i = new ToolStripMenuItem(Hoa(e), null, delegate { ChonModel(dang, ee); });
                    i.Checked = e == eDang;
                    m.Items.Add(i);
                }
            }
            HienMenu(m, nutModel);
        }

        // ---------- Dự án ----------

        /// Thư mục dự án đang chọn; null = mặc định (<repo>\cad).
        string DuAn()
        {
            string d = GiaoDien.DocCaiDat("chat_du_an", "");
            return d == "" ? null : d;
        }

        /// Thư mục làm việc của CLI: dự án đang chọn (còn tồn tại), không thì <repo>\cad.
        string ThuMucLam()
        {
            string d = DuAn();
            return d != null && Directory.Exists(d) ? d : ThuMucCad();
        }

        void VeDuAn()
        {
            string d = DuAn();
            nutDuAn.Text = (d == null ? "TKPCCC CAD" : Path.GetFileName(d.TrimEnd('\\'))) + "  ▾";
            goiY.SetToolTip(nutDuAn, "Thư mục dự án: " + (ThuMucLam() ?? "?") + "\nAI làm việc trong thư mục này: đọc CLAUDE.md, dùng skill .claude\\skills"
                + "\nĐổi nhanh: kéo thư mục từ Explorer thả vào đây, hoặc copy đường dẫn rồi bấm \"Chọn thư mục…\" (hộp mở sẵn tới đó)");
            if (nutDuAn.Parent != null) // xếp lại giữa thanh đầu
                nutDuAn.Location = new Point(Math.Max(6, Math.Min((nutDuAn.Parent.Width - nutDuAn.Width) / 2, nutDuAn.Parent.Width - 100 - nutDuAn.Width)), 7);
        }

        /// Đổi dự án (d null = mặc định cad\): nhớ 8 thư mục gần nhất, mở hội thoại mới.
        void DoiDuAn(string d)
        {
            if (p != null) { trangThai.Text = "Đang chạy: đợi xong (hoặc Dừng) rồi mới đổi dự án."; return; }
            DatDuAn(d);
            HoiThoaiMoi();
            trangThai.Text = "Thư mục dự án: " + (ThuMucLam() ?? "?");
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
            VeDuAn();
        }

        void MenuDuAn()
        {
            ContextMenuStrip m = new ContextMenuStrip();
            string dang = DuAn();
            m.Items.Add(Nhom("Thư mục dự án (AI đọc CLAUDE.md, skill .claude\\skills)"));
            ToolStripMenuItem mac = new ToolStripMenuItem("TKPCCC CAD (mặc định)", null, delegate { DoiDuAn(null); });
            mac.Checked = dang == null;
            m.Items.Add(mac);
            foreach (string d in duAnDs)
            {
                string dd = d;
                ToolStripMenuItem i = new ToolStripMenuItem(Path.GetFileName(d.TrimEnd('\\')), null, delegate { DoiDuAn(dd); });
                i.ToolTipText = d;
                i.Checked = string.Equals(d, dang, StringComparison.OrdinalIgnoreCase);
                m.Items.Add(i);
            }
            m.Items.Add(new ToolStripSeparator());
            m.Items.Add("Chọn thư mục…  (copy đường dẫn trước: mở thẳng tới đó)", null, delegate
            {
                GiaoDien.An(delegate
                {
                    string d = ChonThuMuc.Hoi(this, "Thư mục dự án (AI đọc CLAUDE.md, dùng skill .claude\\skills)", ThuMucLam());
                    if (d != null) DoiDuAn(d);
                });
            });
            string cb = ChonThuMuc.TuClipboard();
            if (cb != null) m.Items.Add("Dùng đường dẫn đã copy: " + cb, null, delegate { DoiDuAn(cb); });
            HienMenu(m, nutDuAn);
        }

        // ---------- Hội thoại, lịch sử, link phiên ----------

        void HoiThoaiMoi()
        {
            if (p != null) return;
            phien = null;
            phienCwd = null;
            aiPhien = -1;
            linkTen = null;
            tach = false;
            noiChung = false;
            XoaTin();
            VeChip();
            trangThai.Text = "Hội thoại mới.";
        }

        /// Menu phiên gần đây: Claude Code (VS Code, CLI, Desktop, Bot chat) và Antigravity. Chọn = nối tiếp phiên đó.
        void MoLichSu()
        {
            if (p != null) { trangThai.Text = "Đang chạy: đợi xong (hoặc Dừng) rồi mới đổi phiên."; return; }
            ContextMenuStrip m = new ContextMenuStrip();
            m.Font = GiaoDien.Chu;
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
            m.Items.Add(Nhom("Claude Code: VS Code · CLI · Desktop · Bot chat" + (noi != "" ? "  (nối chung: " + TenNoi() + " trước)" : "")));
            them(KhoPhien.Claude(15).OrderBy(x => KhopNoi(x) ? 0 : 1).ToList()); // OrderBy giữ thứ tự mới nhất trong mỗi nhóm
            m.Items.Add(new ToolStripSeparator());
            m.Items.Add(Nhom("Antigravity: IDE · agy"));
            them(KhoPhien.Antigravity(10));
            m.Show(Cursor.Position);
        }

        /// Nối phiên x: chọn đúng máy AI, đổi thư mục dự án theo phiên (Claude tìm phiên theo thư mục làm việc), hiện 6 tin cuối.
        void Link(PhienAi x)
        {
            if (p != null) return;
            if (x.Ai != ai)
            {
                ai = x.Ai;
                GiaoDien.LuuCaiDat("chat_ai", ai == 0 ? "claude" : "gemini");
                VeModel();
            }
            if (x.Cwd != null && Directory.Exists(x.Cwd) && !string.Equals(x.Cwd.TrimEnd('\\'), (ThuMucLam() ?? "").TrimEnd('\\'), StringComparison.OrdinalIgnoreCase))
                DatDuAn(x.Cwd);
            XoaTin();
            phien = x.Id;
            phienCwd = x.Cwd;
            aiPhien = x.Ai;
            // Nối chung = ghi thẳng vào phiên gốc. Phiên vừa ghi trong 2 phút qua có thể đang chạy ở nơi khác: hai nơi cùng ghi sẽ rẽ nhánh, nên lần này vẫn tách.
            bool dangGhi = x.Ai == 0 && File.Exists(x.File) && (DateTime.Now - File.GetLastWriteTime(x.File)).TotalMinutes < 2;
            noiChung = x.Ai == 0 && noi != "" && !dangGhi;
            tach = x.Ai == 0 && !noiChung;
            string ten = x.Ten.Replace("\n", " ");
            linkTen = x.Nguon + ": " + (ten.Length > 28 ? ten.Substring(0, 27) + "…" : ten);
            List<KeyValuePair<bool, string>> ds = KhoPhien.TinCuoi(x, 40);
            tinCu = ds.Take(Math.Max(0, ds.Count - 6)).ToList();
            if (tinCu.Count > 0)
            {
                Label cu = new Label();
                cu.AutoSize = true;
                cu.Text = "Hiện tin cũ hơn (" + tinCu.Count + ")";
                cu.ForeColor = GiaoDien.Xanh;
                cu.Font = GiaoDien.Nho;
                cu.Cursor = Cursors.Hand;
                cu.Click += delegate
                {
                    List<KeyValuePair<bool, string>> them = tinCu;
                    cu.Dispose();
                    List<Control> sau = dsTin.Controls.Cast<Control>().ToList();
                    dsTin.Controls.Clear();
                    foreach (KeyValuePair<bool, string> t in them) dsTin.Controls.Add(new BongBong(t.Key ? 0 : 1, Cat(t.Value), GiaoDien.ChuDam));
                    dsTin.Controls.AddRange(sau.ToArray());
                    tinCu = null;
                    XepTin(false);
                };
                dsTin.Controls.Add(cu);
            }
            foreach (KeyValuePair<bool, string> t in ds.Skip(tinCu.Count)) Ghi(t.Key ? "Bạn" : TEN_AI[x.Ai], Cat(t.Value), GiaoDien.ChuDam);
            Ghi(null, "— Đã nối " + x + (x.Ai != 0 ? ". Lời nhắn kế ghi tiếp vào hội thoại Antigravity này."
                : noiChung ? ". Nối chung: lời nhắn kế ghi thẳng vào phiên này, mở lại ở " + x.Nguon + " (hoặc bấm chip link) là thấy. Đừng gõ cùng lúc ở hai nơi."
                : (dangGhi && noi != "" ? ". Phiên này vừa ghi trong 2 phút qua (có thể đang chạy ở nơi khác) nên lần này nối nhánh riêng." : ".")
                    + " Lời nhắn kế chạy tiếp ngữ cảnh phiên này ở một nhánh mới (phiên gốc giữ nguyên)."), GiaoDien.ChuMo);
            VeChip();
        }

        static string Cat(string s) { return s.Length > 900 ? s.Substring(0, 900) + "…" : s; }

        /// Menu "/": skill của thư mục dự án, của <repo>\cad (luôn gắn), thư mục gắn kèm, chỗ chứa skill. Chọn = chèn "/tên " đầu ô nhập.
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
            foreach (string t in ten.Distinct().OrderBy(t => t))
            {
                string tt = t;
                m.Items.Add("/" + t, null, delegate { DatCau("/" + tt + " " + nhap.Text.TrimStart()); });
            }
            if (m.Items.Count == 0) m.Items.Add("(chưa có skill: + → Chỗ chứa skill, hoặc chọn dự án có .claude\\skills)").Enabled = false;
            HienMenu(m, khungSoan);
        }

        void CapNhat()
        {
            if (p == null) return;
            int s = (int)(DateTime.Now - batDau).TotalSeconds;
            string e = EffortDang();
            trangThai.Text = "✻ " + viec + "…  " + s / 60 + ":" + (s % 60).ToString("00") + (e != null ? " · " + Hoa(e) : "");
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
            VeChip();
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

        /// Ảnh thu nhỏ cho chip (không giữ khoá file). null = không đọc được (vd webp).
        static Image ThuNho(string f)
        {
            try
            {
                using (Image i = Image.FromFile(f))
                {
                    double k = Math.Min(1, Math.Min(80.0 / i.Width, 60.0 / i.Height));
                    return new Bitmap(i, Math.Max(1, (int)(i.Width * k)), Math.Max(1, (int)(i.Height * k)));
                }
            }
            catch (System.Exception) { return null; }
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
            VeChip();
        }

        /// Đối tượng đang chọn trên bản vẽ đổi: có thì thành vùng chọn; bỏ chọn hết thì bỏ chip (trừ vùng TKBOTCHON).
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
                s.Append("Thành phần: " + string.Join("; ", dem.OrderByDescending(x => x.Value).Take(15).Select(x => x.Value + " " + x.Key)) + (dem.Count > 15 ? "; ..." : "") + ".\n");
            s.Append("Handle: " + string.Join(",", ids.Take(TOI_DA_HANDLE).Select(i => i.Handle.ToString()))
                + (ids.Count > TOI_DA_HANDLE ? " (còn " + (ids.Count - TOI_DA_HANDLE) + " đối tượng, lọc thêm bằng hop + l)" : "") + "\n");
            if (claude)
                s.Append("Đọc chi tiết: cad_chay {\"ops\":[{\"op\":\"doc\",\"gi\":\"dt\",\"h\":[...]}]}; xem hình: op anh với hop trên. "
                    + "Sửa: op sua (h, doi, xoay, tl, tam, l_moi, mau, t), xoa (h) hoặc vẽ mới. Chỉ đụng tới vùng này trừ khi tôi bảo khác.\n");
            return s.Append("\n").ToString();
        }

        public void DatCau(string cau)
        {
            nhap.Text = cau;
            nhap.Focus();
            nhap.SelectionStart = nhap.TextLength;
        }

        // ---------- Chạy CLI ----------

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

        static string TimAgy()
        {
            bool quaCmd;
            return Tim(new[] { "agy.exe" }, new[] { Path.Combine(Environment.GetEnvironmentVariable("LOCALAPPDATA") ?? "", "agy", "bin") }, out quaCmd);
        }

        internal static string Ngoac(string s) { return KhoPhien.Ngoac(s); }

        /// Lời nhắn gửi AI: dau (vd bản vẽ của lời nhắn) + thư mục gắn kèm + vùng chọn CAD + ảnh + câu. kem = dòng đính kèm hiện trong bong bóng.
        string Soan(string cau, string dau, out string kem)
        {
            List<string> co = thuMuc.Where(Directory.Exists).ToList();
            List<string> anhCo = anh.Where(File.Exists).ToList();
            string vung = MoTaVung(ai == 0);
            kem = (vung != "" ? "\n[vùng chọn " + vungId.Length + " đối tượng]" : "") + (anhCo.Count > 0 ? "\n[" + anhCo.Count + " ảnh]" : "");
            return (dau ?? "") + (co.Count == 0 ? "" : "Thư mục dự án gắn kèm (đọc khi cần làm ngữ cảnh):\n" + string.Join("\n", co.Select(d => "- " + d)) + "\n\n")
                + vung
                + (anhCo.Count == 0 ? "" : "Ảnh đính kèm (mở xem bằng công cụ Read):\n" + string.Join("\n", anhCo.Select(f => "- " + f)) + "\n\n")
                + cau;
        }

        /// Đính kèm đi theo lời nhắn vừa gửi; hội thoại tiếp vẫn nhớ (--resume).
        void BoDinhKem()
        {
            anh.Clear();
            vungId = null;
            vungDoc = null;
            VeChip();
        }

        /// Chạy CLI với lời nhắn cau. false = chưa chạy được (thiếu repo / CLI).
        bool Gui(string cau)
        {
            string cad = ThuMucCad();
            if (cad == null) { trangThai.Text = "Không thấy thư mục repo (cad\\.mcp.json): chạy cad\\build.bat từ repo TKPCCC."; return false; }
            string model = ModelCli(), effort = EffortDang() ?? "high";
            if (aiPhien != ai) { phien = null; linkTen = null; tach = false; noiChung = false; }
            string lam = ThuMucLam() ?? cad;
            bool laCad = string.Equals(lam.TrimEnd('\\'), cad.TrimEnd('\\'), StringComparison.OrdinalIgnoreCase);
            List<string> co = thuMuc.Where(Directory.Exists).ToList();
            List<string> anhCo = anh.Where(File.Exists).ToList();
            // Dự án khác cad\: vẫn gắn cad\ (skill vẽ CAD tkpccc-ve-cad, luật) bằng --add-dir.
            // Claude luôn gắn thư mục ảnh: ảnh đính kèm lời nhắn gửi gấp giữa lượt cũng đọc được.
            if (ai == 0) Directory.CreateDirectory(THU_MUC_ANH);
            string themDir = string.Concat(co.Select(d => " --add-dir " + Ngoac(d))) + (anhCo.Count > 0 || ai == 0 ? " --add-dir " + Ngoac(THU_MUC_ANH) : "")
                + (laCad ? "" : " --add-dir " + Ngoac(cad));
            string coSkill = "";
            foreach (string d in skill.Where(Directory.Exists))
            {
                string tienTo, ms;
                coSkill += ai == 0 ? KhoPhien.CoSkill(d, out tienTo, out ms) : " --add-dir " + Ngoac(d);
            }
            // Người dùng 19/11: lời nhắn gắn bản vẽ đang hiện lúc gửi (máy chủ MCP nhận qua env, xem KhoPhien.FileMcp): việc của lượt này chạy trên bản vẽ đó
            // dù người dùng sang tab khác. Báo AI khi hội thoại mới / đổi bản vẽ để nó không nhận nhầm tên file cũ trong hội thoại.
            AcAp.Document dv = AcAp.Core.Application.DocumentManager.MdiActiveDocument;
            string banVe = dv == null || ai != 0 ? null : dv.Name;
            string bvMoi = banVe != null && (phien == null || !string.Equals(banVe, banVeTruoc, StringComparison.OrdinalIgnoreCase))
                ? "Bản vẽ của lời nhắn này: \"" + Path.GetFileName(banVe) + "\" (" + banVe + "). Công cụ cad tự chạy trên bản vẽ này kể cả khi tôi sang tab khác, không cần ghi ban_ve.\n\n" : "";
            string kem, gui1 = Soan(cau, bvMoi, out kem);

            bool quaCmd;
            string exe, doiSo;
            if (ai == 0)
            {
                exe = Tim(new[] { "claude.exe", "claude.cmd" }, new[] { Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "bin") }, out quaCmd);
                if (exe == null) { Ghi(null, CAI_CLAUDE, GiaoDien.Do); return false; }
                // Như runner/src/engines/claude.ts: không API key, không nạp cài đặt cá nhân (chỉ project, local của thư mục dự án); thêm MCP cad (đường dẫn
                // tuyệt đối: thư mục làm việc có thể khác cad\). Người dùng 19/11 ("như VS Code, bot phải ghi được file"): đọc + Edit/Write + Skill + 3 công cụ
                // cad; --permission-mode acceptEdits tự duyệt sửa file trong thư mục dự án + thư mục --add-dir (ngoài đó Claude Code vẫn từ chối).
                // Không mở Bash: chạy lệnh máy không có bước người dùng duyệt. Nối phiên nơi khác: --resume <id> --fork-session.
                // --add-dir / --plugin-dir trước --tools: các cờ này nhận nhiều giá trị, cờ kế tiếp mới cắt.
                // --input-format stream-json: stdin mở suốt lượt để Ctrl+X ghi thêm lời nhắn gấp (GuiGap), đóng khi có dòng result (DongVao).
                doiSo = "-p --input-format stream-json --output-format stream-json --verbose --model " + model + " --effort " + effort
                    + " --mcp-config " + Ngoac(KhoPhien.FileMcp(cad, banVe)) + " --strict-mcp-config --setting-sources project,local --permission-mode acceptEdits"
                    + (phien != null ? " --resume " + phien + (tach ? " --fork-session" : "") : "") + themDir + coSkill
                    + " --tools Read Grep Glob Skill Edit Write --allowedTools Read Grep Glob Skill Edit Write mcp__cad__cad_tt mcp__cad__cad_chay mcp__cad__cad_anh";
            }
            else
            {
                exe = Tim(new[] { "agy.exe" }, new[] { Path.Combine(Environment.GetEnvironmentVariable("LOCALAPPDATA") ?? "", "agy", "bin") }, out quaCmd);
                if (exe == null) { Ghi(null, CAI_AGY, GiaoDien.Do); return false; }
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
            BoDinhKem();
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
            catch (System.Exception ex) { Ghi(null, (ai == 0 ? CAI_CLAUDE : CAI_AGY) + " (" + ex.Message + ")", GiaoDien.Do); return true; } // lời nhắn đã hiện trong khung
            p = q;
            banVeTruoc = banVe;
            phienCwd = lam;
            nutGui.Glyph = Ve.DUNG;
            nutGui.Nen = GiaoDien.Do;
            goiY.SetToolTip(nutGui, "Dừng (Enter lúc này = xếp hàng, Ctrl+X = gửi gấp vào lượt đang chạy)");
            nutGui.Invalidate();
            goiYNhap.Text = ai == 0 ? "Enter xếp hàng · Ctrl+X gửi gấp…" : "Enter xếp hàng…";
            batDau = DateTime.Now;
            viec = ModelDang().Ten + " đang nghĩ";
            CapNhat();
            dongHo.Start();
            q.BeginOutputReadLine();
            q.BeginErrorReadLine();
            vaoMo = ai == 0 && GhiVao(q, gui1);
            if (!vaoMo) try { q.StandardInput.Close(); } catch (System.Exception) { } // agy nhận lời nhắn qua -p
            return true;
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
            if (loai == "result") DongVao();
            if (loai == "assistant" && j.TryGetValue("message", out v))
            {
                Dictionary<string, object> m = v as Dictionary<string, object>;
                object[] nd = m != null && m.TryGetValue("content", out v) ? v as object[] : null;
                foreach (Dictionary<string, object> k in (nd ?? new object[0]).OfType<Dictionary<string, object>>())
                {
                    object t;
                    string kieu = k.TryGetValue("type", out t) ? t as string : null;
                    if (kieu == "text" && k.TryGetValue("text", out t)) { Ghi("Claude", Convert.ToString(t).Trim(), GiaoDien.ChuDam); viec = "Đang viết kết quả"; }
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
                if (r.TryGetValue("response", out t) && Convert.ToString(t).Trim() != "") Ghi("Antigravity", Convert.ToString(t).Trim(), GiaoDien.ChuDam);
                if (r.TryGetValue("status", out t) && Convert.ToString(t) != "SUCCESS")
                    Ghi(null, "Lỗi: " + Convert.ToString(t) + (r.TryGetValue("error", out t) ? " " + Convert.ToString(t) : ""), GiaoDien.Do);
            }
        }

        void CongCu(string ten)
        {
            Ghi(null, "· " + ten, GiaoDien.ChuMo);
            viec = "Đang chạy " + ten;
            CapNhat();
        }

        static string LoiDangNhap(string m)
        {
            string t = (m ?? "").ToLowerInvariant();
            return t.Contains("login") || t.Contains("auth") || t.Contains("401") || t.Contains("credential")
                ? "\nPhiên đăng nhập hết hạn: mở terminal, gõ claude rồi /login (Antigravity: gõ agy)." : "";
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
            vaoMo = false;
            goiYNhap.Text = "Thêm yêu cầu…";
            nutGui.Glyph = Ve.GUI;
            nutGui.Nen = GiaoDien.XanhDam;
            goiY.SetToolTip(nutGui, "Gửi (Enter)");
            nutGui.Invalidate();
            if (hangCho.Count > 0) { string cau = hangCho.Dequeue(); BeginInvoke((Action)delegate { GiaoDien.An(delegate { Gui(cau); }); }); }
        }
    }
}
