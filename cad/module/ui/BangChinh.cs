// Palette chính (lệnh PCCC; PCCC_BLOCK, PCCC_HELP mở đúng phân hệ): đầu trang [PCCC] + tên + phiên bản, ô tìm lọc thẻ, thanh dọc chọn phân hệ
// (Quy trình vẽ · Tiện ích · Báo cháy · Thư viện · Lệnh · Bot chat · Cài đặt), chân trang trạng thái cầu nối MCP + bản vẽ. Chỉ giao diện: lệnh vẽ gửi bằng tên qua GiaoDien.GuiLenh.
// Bot chat gọi `claude -p` (đăng nhập Claude Code, không API key) chạy NỀN ở <repo>\cad với MCP cad: luồng AutoCAD không bị khoá nên cầu nối Cau.cs
// vẫn chạy hàng đợi trong lúc chờ.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Windows;
using AcApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace TKPCCC
{
    public class BangChinh : UserControl
    {
        public const string QUY_TRINH = "Quy trình vẽ", TIEN_ICH = "Tiện ích", BO_TRI = "Báo cháy", THU_VIEN = "Thư viện", LENH = "Lệnh", BOT = "Bot chat", CAI_DAT = "Cài đặt";
        static PaletteSet ps;
        static BangChinh nd;

        /// Mở palette (trang null = giữ trang đang xem). Trả câu báo ngắn cho dòng lệnh.
        public static string Mo(string trang)
        {
            if (ps == null)
            {
                nd = new BangChinh();
                ps = new PaletteSet("TKPCCC - PCCC", new Guid("3E7A2C91-5B4D-4F18-8D6E-0C9A1B2F7E54"));
                ps.MinimumSize = new Size(340, 420);
                ps.Add("PCCC", nd);
                ps.DockEnabled = DockSides.Left | DockSides.Right;
                ps.Visible = true;
                ps.Size = new Size(400, 700);
                ps.Dock = DockSides.Left;
                AcApp.DocumentManager.DocumentActivated += delegate { if (ps.Visible && nd.block != null) GiaoDien.An(nd.block.DemLai); };
            }
            ps.Visible = true;
            nd.ChonTrang(trang ?? nd.dangXem ?? QUY_TRINH);
            if (trang == THU_VIEN) return nd.soBlock + " block trong bảng. Bấm đúp một dòng để đặt thiết bị.";
            return "palette TKPCCC, phân hệ " + nd.dangXem + ".";
        }

        readonly TextBox tim = new TextBox();
        readonly Panel noiDung = new Panel();
        readonly FlowLayoutPanel ben = new FlowLayoutPanel();
        readonly Label chan = new Label();
        readonly Dictionary<string, Control> trang = new Dictionary<string, Control>();
        readonly Dictionary<string, NutBen> nutBen = new Dictionary<string, NutBen>();
        readonly FlowLayoutPanel dsThe = new FlowLayoutPanel();
        readonly List<TheCongCu> the = new List<TheCongCu>();
        string dangXem;
        BangBlock block;
        BangLenh bangLenh;
        BangTienIch tienIch;
        BotChat bot;
        int soBlock;

        BangChinh()
        {
            Font = GiaoDien.Chu;
            BackColor = GiaoDien.Nen;
            GiaoDien.DangMo.Add(this);

            // Thứ tự Add quyết định cách xếp Dock: Fill trước, rồi cạnh trái, rồi trên/dưới (trên/dưới thêm sau nên phủ hết chiều ngang).
            noiDung.Dock = DockStyle.Fill;
            Controls.Add(noiDung);

            ben.Dock = DockStyle.Left;
            ben.Width = 84;
            ben.FlowDirection = FlowDirection.TopDown;
            ben.WrapContents = false;
            ben.BackColor = GiaoDien.Nen;
            ben.Padding = new Padding(0, 6, 0, 0);
            foreach (string t in new[] { QUY_TRINH, TIEN_ICH, BO_TRI, THU_VIEN, LENH, BOT, CAI_DAT })
            {
                string ten = t;
                NutBen n = new NutBen(ten);
                n.Click += delegate { GiaoDien.An(delegate { ChonTrang(ten); }); };
                nutBen[ten] = n;
                ben.Controls.Add(n);
            }
            Controls.Add(ben);

            // Đang xem Tiện ích thì ô tìm lọc thẻ tiện ích (tìm theo mã tắt SZ, CKC...); trang khác thì về Quy trình lọc thẻ lệnh.
            tim.TextChanged += delegate
            {
                if (dangXem == TIEN_ICH) { tienIch.Tim(tim.Text); return; }
                if (dangXem != QUY_TRINH && tim.Text != "") ChonTrang(QUY_TRINH);
                LocThe();
            };
            Controls.Add(GiaoDien.OTim(tim, "Tìm công cụ theo tên hoặc lệnh..."));
            Controls.Add(DauTrang());

            chan.Dock = DockStyle.Bottom;
            chan.Height = 22;
            chan.BackColor = GiaoDien.Nen;
            chan.ForeColor = GiaoDien.ChuMo;
            chan.Font = GiaoDien.Nho;
            chan.TextAlign = ContentAlignment.MiddleLeft;
            chan.Padding = new Padding(6, 0, 0, 0);
            Controls.Add(chan);
            System.Windows.Forms.Timer dongHo = new System.Windows.Forms.Timer();
            dongHo.Interval = 2000;
            dongHo.Tick += delegate { CapNhatChan(); };
            dongHo.Start();
            CapNhatChan();

            TaoQuyTrinh();
            GiaoDien.DaDoiChuDe += delegate { foreach (NutBen n in nutBen.Values) n.Invalidate(); };
        }

        Control DauTrang()
        {
            Panel p = new Panel();
            p.Dock = DockStyle.Top;
            p.Height = 46;
            p.BackColor = GiaoDien.Nen;
            Label huy = new Label();
            huy.Text = "PCCC";
            huy.Font = GiaoDien.Dam;
            huy.BackColor = GiaoDien.Xanh;
            huy.ForeColor = Color.White;
            huy.TextAlign = ContentAlignment.MiddleCenter;
            huy.SetBounds(8, 10, 48, 26);
            Label ten = new Label();
            ten.Text = "TKPCCC – Thiết kế PCCC";
            ten.Font = new Font("Segoe UI", 10.5f, FontStyle.Bold);
            ten.ForeColor = GiaoDien.ChuDam;
            ten.AutoSize = true;
            ten.Location = new Point(62, 5);
            Label pb = new Label();
            string dll = typeof(Lenh).Assembly.Location;
            pb.Text = "bản " + (File.Exists(dll) ? File.GetLastWriteTime(dll).ToString("yyyy.MM.dd") : "?");
            pb.Font = GiaoDien.Nho;
            pb.ForeColor = GiaoDien.ChuMo;
            pb.AutoSize = true;
            pb.Location = new Point(64, 26);
            p.Controls.AddRange(new Control[] { huy, ten, pb });
            return p;
        }

        void CapNhatChan()
        {
            string mcp;
            try
            {
                bool song = File.Exists(HangDoi.Song) && (DateTime.UtcNow - File.GetLastWriteTimeUtc(HangDoi.Song)).TotalSeconds < 6;
                bool dang = Directory.Exists(HangDoi.Dang) && Directory.GetFiles(HangDoi.Dang, "*.json").Length > 0;
                mcp = !song ? "chưa chạy" : dang ? "đang vẽ" : "sẵn sàng";
            }
            catch (System.Exception) { mcp = "?"; }
            Document doc = AcApp.DocumentManager.MdiActiveDocument;
            chan.Text = "Cầu nối MCP: " + mcp + "   ·   Bản vẽ: " + (doc == null ? "(không có)" : Path.GetFileName(doc.Name));
        }

        public void ChonTrang(string ten)
        {
            Control c;
            if (!trang.TryGetValue(ten, out c))
            {
                c = TaoTrang(ten);
                c.Dock = DockStyle.Fill;
                trang[ten] = c;
                noiDung.Controls.Add(c);
            }
            foreach (Control k in trang.Values) k.Visible = k == c;
            dangXem = ten;
            foreach (KeyValuePair<string, NutBen> n in nutBen) { n.Value.Chon = n.Key == ten; n.Value.Invalidate(); }
            if (ten == THU_VIEN) soBlock = block.Nap();
            if (ten == LENH) bangLenh.Nap();
        }

        Control TaoTrang(string ten)
        {
            if (ten == THU_VIEN) return block = new BangBlock();
            if (ten == LENH) return bangLenh = new BangLenh();
            if (ten == BOT) return bot = new BotChat();
            if (ten == CAI_DAT) return TrangCaiDat();
            if (ten == BO_TRI) return new BangBoTri();
            if (ten == TIEN_ICH) return tienIch = new BangTienIch(delegate(string t) { ChonTrang(t); });
            return dsThe;
        }

        // ---------- Quy trình vẽ: 6 thẻ bước + thẻ kích thước; khi tìm thì hiện thêm thẻ của mọi lệnh PCCC ----------

        void TaoQuyTrinh()
        {
            dsThe.FlowDirection = FlowDirection.TopDown;
            dsThe.WrapContents = false;
            dsThe.AutoScroll = true;
            dsThe.BackColor = GiaoDien.Nen;
            dsThe.Padding = new Padding(6, 6, 6, 6);
            dsThe.Resize += delegate { foreach (TheCongCu t in the) t.Width = RongThe(); };
            Them(1, "Trục", "Lưới trục theo nhịp X, Y; có PDF thì nhập làm nền trước", new[] { "PCCC_TRUC", "PDFIMPORT" }, null);
            Them(2, "Tường", "Tường theo tim, nhập chiều dày", new[] { "PCCC_TUONG" }, null);
            Them(3, "Mặt bằng chi tiết", "Vách ngăn (PCCC_TUONG, chọn Vách), cửa đi trên tường", new[] { "PCCC_TUONG", "PCCC_CUA" }, null);
            Them(4, "Thiết bị PCCC", "Rải đầu báo, đầu phun theo lưới; đặt từng thiết bị", new[] { BO_TRI, "PCCC_RAI", "PCCC_DAT" },
                new Action[] { delegate { ChonTrang(BO_TRI); }, null, null }); // Thư viện đã có ở thanh dọc: bỏ huy hiệu cho thẻ không tràn
            Them(5, "Dây, ống", "Dây báo cháy, ống nhánh theo thiết bị đã rải; vẽ ống, dây tay", new[] { "PCCC_TUYEN", "PCCC_ONG" }, null);
            Them(6, "Sơ đồ nguyên lý", "Nhờ bot chat vẽ từ mặt bằng đang mở", new[] { BOT },
                new Action[] { delegate { ChonTrang(BOT); bot.DatCau(BotChat.MAU_SO_DO); } });
            Them(0, "Kích thước tự động", "Quét cửa sổ, chuỗi kích thước theo thiết bị", new[] { "PCCC_ADIM" }, null);
            Them(0, "Kích thước tiếp tục", "Nối tiếp chuỗi kích thước vừa tạo", new[] { "PCCC_CDIM" }, null);

            foreach (string[] l in Lenh.DS_LENH) Them(-1, l[1], l[2], new[] { l[0] }, null); // chỉ hiện khi tìm
            LocThe();
        }

        int RongThe() { return Math.Max(200, dsThe.ClientSize.Width - dsThe.Padding.Horizontal - 2); }

        void Them(int so, string ten, string moTa, string[] nhan, Action[] viec)
        {
            Action[] v = new Action[nhan.Length];
            for (int i = 0; i < nhan.Length; i++)
            {
                string l = nhan[i];
                v[i] = viec != null && viec[i] != null ? viec[i] : delegate { GiaoDien.GuiLenh(l + "\n", null, null); };
            }
            TheCongCu t = new TheCongCu(so, ten, moTa, nhan, v);
            t.Width = RongThe();
            the.Add(t);
            dsThe.Controls.Add(t);
        }

        void LocThe()
        {
            string k = tim.Text.Trim().ToLowerInvariant();
            int n = 0;
            dsThe.SuspendLayout();
            foreach (TheCongCu t in the)
            {
                t.Visible = k == "" ? t.So >= 0 : t.ChuTim.Contains(k);
                if (t.Visible) n++;
            }
            dsThe.ResumeLayout();
            dsThe.Text = "The: " + n; // thu_gui.ps1 đọc số thẻ (chữ của cửa sổ)
        }

        // ---------- Cài đặt ----------

        Control TrangCaiDat()
        {
            FlowLayoutPanel f = new FlowLayoutPanel();
            f.FlowDirection = FlowDirection.TopDown;
            f.WrapContents = false;
            f.AutoScroll = true;
            f.BackColor = GiaoDien.Nen;
            f.Padding = new Padding(10, 8, 10, 8);
            Func<string, Label> nhan = delegate(string t)
            {
                Label l = new Label();
                l.Text = t;
                l.Font = GiaoDien.Dam;
                l.ForeColor = GiaoDien.ChuDam;
                l.AutoSize = true;
                l.Margin = new Padding(0, 10, 0, 4);
                return l;
            };
            f.Controls.Add(nhan("Giao diện"));
            f.Controls.Add(NhomChon(new[] { "toi", "sang", "claude" }, new[] { "Tối (mặc định)", "Sáng (xanh trắng)", "Claude (màu Claude Code)" },
                GiaoDien.ChuDe, delegate(string g) { GiaoDien.DatChuDe(g); }));
            f.Controls.Add(nhan("Model bot chat"));
            f.Controls.Add(NhomChon(new[] { "sonnet", "opus" }, new[] { "Sonnet (mặc định, nhanh, ít hạn mức)", "Opus (kỹ hơn, tốn hạn mức hơn)" },
                GiaoDien.DocCaiDat("model", "sonnet"), delegate(string m) { GiaoDien.LuuCaiDat("model", m); }));
            f.Controls.Add(nhan("Bản quyền"));
            NutBo bq = new NutBo("Bản quyền...", GiaoDien.XanhDam);
            bq.Size = new Size(160, 32);
            bq.Click += delegate { GiaoDien.GuiLenh("PCCC_BANQUYEN\n", null, null); };
            f.Controls.Add(bq);
            Label goi = new Label();
            goi.Text = "Cài đặt lưu ở " + GiaoDien.FileCaiDat;
            goi.Font = GiaoDien.Nho;
            goi.ForeColor = GiaoDien.ChuMo;
            goi.MaximumSize = new Size(280, 0);
            goi.AutoSize = true;
            goi.Margin = new Padding(0, 14, 0, 0);
            f.Controls.Add(goi);
            return f;
        }

        static Control NhomChon(string[] ma, string[] chu, string dangChon, Action<string> doi)
        {
            FlowLayoutPanel p = new FlowLayoutPanel();
            p.FlowDirection = FlowDirection.TopDown;
            p.AutoSize = true;
            p.BackColor = GiaoDien.Nen;
            for (int i = 0; i < ma.Length; i++)
            {
                string m = ma[i];
                RadioButton r = new RadioButton();
                r.Text = chu[i];
                r.AutoSize = true;
                r.ForeColor = GiaoDien.ChuDam;
                r.Checked = m == dangChon;
                r.CheckedChanged += delegate { if (r.Checked) GiaoDien.An(delegate { doi(m); }); };
                p.Controls.Add(r);
            }
            return p;
        }
    }

    /// Nút của thanh dọc bên trái: chữ hai dòng, đang chọn thì nền thẻ + vạch nhấn bên trái.
    class NutBen : Control
    {
        public bool Chon;

        public NutBen(string chu)
        {
            Text = chu;
            Size = new Size(84, 46);
            Margin = new Padding(0, 0, 0, 2);
            Cursor = Cursors.Hand;
            DoubleBuffered = true;
            AccessibleRole = AccessibleRole.PageTab;
            AccessibleName = chu;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.Clear(Chon ? GiaoDien.The : GiaoDien.Nen);
            if (Chon) using (SolidBrush b = new SolidBrush(GiaoDien.Xanh)) g.FillRectangle(b, 0, 6, 3, Height - 12);
            TextRenderer.DrawText(g, Text, Chon ? GiaoDien.Dam : GiaoDien.Chu, new Rectangle(4, 0, Width - 6, Height), Chon ? GiaoDien.Xanh : GiaoDien.ChuDam,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.WordBreak);
        }
    }

    /// Thẻ công cụ: số bước (vòng tròn nhấn), tên đậm, mô tả một dòng chữ mờ, huy hiệu tên lệnh góc phải (bấm = chạy). Bấm thân thẻ = huy hiệu đầu.
    class TheCongCu : Control
    {
        public readonly int So; // 1..6 bước, 0 thẻ thêm, -1 thẻ lệnh chỉ hiện khi tìm
        public readonly string Ten, MoTa, ChuTim;

        public TheCongCu(int so, string ten, string moTa, string[] nhan, Action[] viec)
        {
            So = so;
            Ten = ten;
            MoTa = moTa;
            // Huy hiệu kèm phím tắt người dùng (ý lấy từ addin mẫu 14/11: tìm được theo phím tắt). shortcut: sửa phím tắt thì mở lại palette thẻ mới đổi.
            string[] phim = nhan.Select(l => { string pt; return PhimTat.TheoLenh.TryGetValue(l, out pt) ? pt : ""; }).ToArray();
            ChuTim = (ten + " " + moTa + " " + string.Join(" ", nhan) + " " + string.Join(" ", phim)).ToLowerInvariant();
            Height = 64;
            Margin = new Padding(0, 0, 0, 6);
            DoubleBuffered = true;
            Cursor = Cursors.Hand;
            AccessibleName = ten;
            BackColor = GiaoDien.Nen;
            Click += delegate { GiaoDien.An(viec[0]); };
            for (int i = nhan.Length - 1; i >= 0; i--) // xếp từ phải sang trái
            {
                Label h = new Label();
                h.Text = nhan[i] + (phim[i] != "" ? " · " + phim[i] : "");
                h.Font = GiaoDien.Nho;
                h.AutoSize = true;
                h.Padding = new Padding(4, 1, 4, 1);
                h.BackColor = GiaoDien.Nen;
                h.ForeColor = GiaoDien.Xanh;
                h.Cursor = Cursors.Hand;
                h.Anchor = AnchorStyles.Top | AnchorStyles.Right;
                Action v = viec[i];
                h.Click += delegate { GiaoDien.An(v); };
                Controls.Add(h);
            }
            Resize += delegate { XepHuy(); };
        }

        void XepHuy()
        {
            int x = Width - 8;
            foreach (Control h in Controls)
            {
                x -= h.Width;
                h.Location = new Point(x, Height - h.Height - 7);
                x -= 4;
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.Clear(Parent != null ? Parent.BackColor : GiaoDien.Nen);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (GraphicsPath p = GiaoDien.BoGoc(new Rectangle(0, 0, Width - 1, Height - 1), 8))
            {
                using (SolidBrush b = new SolidBrush(GiaoDien.The)) g.FillPath(b, p);
                using (Pen v = new Pen(GiaoDien.Vien)) g.DrawPath(v, p);
            }
            int x = 10;
            if (So > 0)
            {
                Rectangle r = new Rectangle(10, 10, 24, 24);
                using (SolidBrush b = new SolidBrush(GiaoDien.Xanh)) g.FillEllipse(b, r);
                TextRenderer.DrawText(g, So.ToString(), GiaoDien.Dam, r, Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                x = 42;
            }
            TextFormatFlags dong = TextFormatFlags.Left | TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine;
            TextRenderer.DrawText(g, Ten, GiaoDien.Dam, new Rectangle(x, 8, Width - x - 8, 18), GiaoDien.ChuDam, dong);
            TextRenderer.DrawText(g, MoTa, GiaoDien.Nho, new Rectangle(x, 26, Width - x - 8, 16), GiaoDien.ChuMo, dong);
        }
    }

    /// Bot chat: gửi lời nhắn cho `claude -p` chạy nền ở <repo>\cad (MCP cad), giữ hội thoại bằng --resume.
    class BotChat : UserControl
    {
        public const string MAU_PDF = "Vẽ theo quy trình 6 bước từ PDF: ";
        public const string MAU_SO_DO = "Vẽ sơ đồ nguyên lý báo cháy từ mặt bằng đang mở";
        const string HUONG_DAN_CAI = "Không tìm thấy lệnh claude. Cài Claude Code (npm i -g @anthropic-ai/claude-code), mở terminal gõ claude một lần để đăng nhập, rồi thử lại.";

        readonly RichTextBox hoi = new RichTextBox();
        readonly TextBox nhap = new TextBox();
        readonly NutBo gui = new NutBo("Gửi", GiaoDien.XanhDam);
        readonly Label trangThai = new Label();
        Process p;
        string phien; // session_id của claude, để --resume
        readonly StringBuilder loi = new StringBuilder();

        public BotChat()
        {
            Font = GiaoDien.Chu;
            BackColor = GiaoDien.Nen;

            hoi.Dock = DockStyle.Fill;
            hoi.ReadOnly = true;
            hoi.BorderStyle = BorderStyle.None;
            hoi.BackColor = GiaoDien.The;
            hoi.ForeColor = GiaoDien.ChuDam;
            hoi.Font = GiaoDien.Chu;
            Controls.Add(hoi);

            Panel duoi = new Panel();
            duoi.Dock = DockStyle.Bottom;
            duoi.Height = 150;
            duoi.BackColor = GiaoDien.Nen;
            duoi.Padding = new Padding(6, 6, 6, 6);
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
                using (System.Windows.Forms.OpenFileDialog d = new System.Windows.Forms.OpenFileDialog())
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
            trangThai.Text = "Ctrl+Enter để gửi. Claude dùng đăng nhập Claude Code trên máy, không API key.";
            duoi.Controls.Add(nhap);
            duoi.Controls.Add(nut);
            duoi.Controls.Add(trangThai);
            Controls.Add(duoi);
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
            string f = Path.Combine(Path.GetDirectoryName(typeof(Lenh).Assembly.Location), "nguon_vat_tu.txt");
            if (!File.Exists(f)) return null;
            string vt = Path.GetFullPath(File.ReadAllText(f).Trim());
            string cad = Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(vt)), "cad");
            return File.Exists(Path.Combine(cad, ".mcp.json")) ? cad : null;
        }

        /// claude.exe trên PATH hoặc %USERPROFILE%\.local\bin; không có thì claude.cmd (npm) chạy qua cmd. null = không thấy.
        static string TimClaude(out bool quaCmd)
        {
            quaCmd = false;
            List<string> thu = (Environment.GetEnvironmentVariable("PATH") ?? "").Split(';').Where(d => d.Trim() != "").ToList();
            thu.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "bin"));
            foreach (string ten in new[] { "claude.exe", "claude.cmd" })
                foreach (string d in thu)
                {
                    string f;
                    try { f = Path.Combine(d.Trim().Trim('"'), ten); } catch (System.Exception) { continue; }
                    if (!File.Exists(f)) continue;
                    quaCmd = ten.EndsWith(".cmd");
                    return f;
                }
            return null;
        }

        void Gui()
        {
            if (p != null) { try { p.Kill(); } catch (System.Exception) { } return; } // đang chạy: nút là "Dừng"
            string cau = nhap.Text.Trim();
            if (cau == "") return;
            string cad = ThuMucCad();
            if (cad == null) { trangThai.Text = "Không thấy thư mục repo (cad\\.mcp.json): chạy cad\\build.bat từ repo TKPCCC."; return; }
            bool quaCmd;
            string exe = TimClaude(out quaCmd);
            if (exe == null) { Ghi(null, HUONG_DAN_CAI, GiaoDien.Do); return; }

            // Như runner/src/engines/claude.ts: không API key, không nạp cài đặt cá nhân; thêm MCP cad và chỉ cho công cụ đọc + 3 công cụ cad.
            string model = GiaoDien.DocCaiDat("model", "sonnet") == "opus" ? "opus" : "sonnet";
            string doiSo = "-p --output-format stream-json --verbose --model " + model
                + " --mcp-config .mcp.json --strict-mcp-config --setting-sources project,local --disable-slash-commands"
                + (phien != null ? " --resume " + phien : "")
                + " --tools Read Grep Glob --allowedTools Read Grep Glob mcp__cad__cad_tt mcp__cad__cad_chay mcp__cad__cad_anh";
            ProcessStartInfo si = quaCmd ? new ProcessStartInfo("cmd.exe", "/d /s /c \"\"" + exe + "\" " + doiSo + "\"") : new ProcessStartInfo(exe, doiSo);
            si.UseShellExecute = false;
            si.CreateNoWindow = true;
            si.WorkingDirectory = cad;
            si.RedirectStandardInput = si.RedirectStandardOutput = si.RedirectStandardError = true;
            si.StandardOutputEncoding = si.StandardErrorEncoding = new UTF8Encoding(false);
            si.EnvironmentVariables.Remove("ANTHROPIC_API_KEY");
            si.EnvironmentVariables.Remove("CLAUDECODE");

            Ghi("Bạn", cau, GiaoDien.Xanh);
            nhap.Clear();
            loi.Length = 0;
            Process q = new Process();
            q.StartInfo = si;
            q.OutputDataReceived += delegate(object s, DataReceivedEventArgs e)
            {
                string d = e.Data;
                try { BeginInvoke((Action)delegate { if (d == null) Xong(q); else DocDong(d); }); } catch (System.Exception) { } // palette đã đóng
            };
            q.ErrorDataReceived += delegate(object s, DataReceivedEventArgs e) { if (e.Data != null) lock (loi) loi.AppendLine(e.Data); };
            try { q.Start(); }
            catch (System.Exception ex) { Ghi(null, HUONG_DAN_CAI + " (" + ex.Message + ")", GiaoDien.Do); return; }
            p = q;
            gui.Text = "Dừng";
            gui.Invalidate();
            trangThai.Text = "Claude đang chạy (" + model + ")...";
            q.BeginOutputReadLine();
            q.BeginErrorReadLine();
            try
            {
                byte[] b = new UTF8Encoding(false).GetBytes(cau); // chữ Việt qua stdin UTF-8, không qua dòng lệnh
                q.StandardInput.BaseStream.Write(b, 0, b.Length);
                q.StandardInput.Close();
            }
            catch (System.Exception) { } // claude thoát sớm: lỗi thật đọc ở stderr khi Xong
        }

        /// Một dòng stream-json: system/init (session_id), assistant (chữ, công cụ đang gọi), result (lỗi, session_id).
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
            string loai = j.TryGetValue("type", out v) ? v as string : null;
            if (loai == "assistant" && j.TryGetValue("message", out v))
            {
                Dictionary<string, object> m = v as Dictionary<string, object>;
                object[] nd = m != null && m.TryGetValue("content", out v) ? v as object[] : null;
                foreach (Dictionary<string, object> k in (nd ?? new object[0]).OfType<Dictionary<string, object>>())
                {
                    object t;
                    string kieu = k.TryGetValue("type", out t) ? t as string : null;
                    if (kieu == "text" && k.TryGetValue("text", out t)) Ghi("Claude", Convert.ToString(t).Trim(), GiaoDien.ChuDam);
                    else if (kieu == "tool_use" && k.TryGetValue("name", out t))
                    {
                        string ten = Convert.ToString(t).Replace("mcp__cad__", "");
                        Ghi(null, "  · " + ten, GiaoDien.ChuMo);
                        trangThai.Text = "Claude đang chạy: " + ten + "...";
                    }
                }
            }
            else if (loai == "result" && j.TryGetValue("is_error", out v) && v is bool && (bool)v)
                Ghi(null, "Lỗi: " + (j.TryGetValue("result", out v) ? Convert.ToString(v) : "") + LoiDangNhap(Convert.ToString(v)), GiaoDien.Do);
        }

        static string LoiDangNhap(string m)
        {
            string t = (m ?? "").ToLowerInvariant();
            return t.Contains("login") || t.Contains("auth") || t.Contains("401") || t.Contains("credential")
                ? "\nPhiên đăng nhập Claude hết hạn: mở terminal, gõ claude rồi /login." : "";
        }

        void Xong(Process q)
        {
            int ma = -1;
            try { q.WaitForExit(3000); if (q.HasExited) ma = q.ExitCode; } catch (System.Exception) { }
            string e;
            lock (loi) e = loi.ToString().Trim();
            if (ma != 0 && e != "") Ghi(null, "claude thoát mã " + ma + ": " + (e.Length > 600 ? e.Substring(0, 600) : e) + LoiDangNhap(e), GiaoDien.Do);
            trangThai.Text = ma == 0 ? "Xong." : "Dừng (mã " + ma + ").";
            q.Dispose();
            p = null;
            gui.Text = "Gửi";
            gui.Invalidate();
        }
    }
}
