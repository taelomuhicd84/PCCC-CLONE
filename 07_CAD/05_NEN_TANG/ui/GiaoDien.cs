// Kiểu giao diện chung của các bảng TKPCCC (palette PCCC, PCCC_BLOCK, PCCC_HELP): tiêu đề nền nhấn, ô tìm có chữ gợi ý, bảng dòng cao kẻ ngang,
// khung "Hành động" bên phải với nút bo góc và ô tổng số. Chỉ vẽ giao diện, không đụng bản vẽ.
// 3 kiểu màu (toi mặc định, sang, claude) lưu ở %APPDATA%\TKPCCC\cai_dat.json; đổi kiểu thì control đang mở đổi màu ngay (DatChuDe).
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Drawing.Drawing2D;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Autodesk.AutoCAD.ApplicationServices;
using AcApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace TKPCCC
{
    static class GiaoDien
    {
        // Tên giữ từ kiểu "sang" cũ (mã khác dùng), giá trị theo kiểu đang chọn: Xanh = màu nhấn (tiêu đề), XanhDam = nút, XanhNhat = dòng chọn,
        // Nen = nền, The = nền thẻ/bảng, Vien = viền, LuoiKe = kẻ bảng. Không readonly vì đổi được lúc chạy.
        public static Color Xanh, XanhDam, XanhNhat, Nen, The, Vien, LuoiKe, ChuDam, ChuMo, Do, XanhLa, Cam;
        public static string ChuDe = "toi";
        /// Các control gốc đang mở: DatChuDe đổi màu cả cây con của chúng.
        public static readonly List<Control> DangMo = new List<Control>();
        /// Gọi sau khi đổi kiểu (control tự giữ màu riêng thì cập nhật ở đây).
        public static event Action DaDoiChuDe;
        public static readonly Font Chu = new Font("Segoe UI", 9f);
        public static readonly Font Dam = new Font("Segoe UI", 9f, FontStyle.Bold);
        public static readonly Font Nho = new Font("Segoe UI", 8f);
        public static readonly Font To = new Font("Segoe UI", 18f, FontStyle.Bold);
        public static readonly Font NhoDam = new Font("Segoe UI", 7.5f, FontStyle.Bold);
        /// Biểu tượng: emoji Unicode vẽ bằng GDI (TextRenderer) ra hình đơn sắc theo màu chữ; máy không có Segoe UI Emoji thì Windows tự thay phông.
        public static readonly Font Icon = new Font("Segoe UI Emoji", 13f), IconTo = new Font("Segoe UI Emoji", 16f);

        /// Màu nút biểu tượng (ý từ video plugin 16/11: mỗi nhóm một màu tươi, giữ nguyên ở cả 3 kiểu nền).
        public static readonly Color MauLa = Color.FromArgb(20, 184, 166), MauCam = Color.FromArgb(249, 115, 22), MauDuong = Color.FromArgb(59, 130, 246),
            MauTim = Color.FromArgb(139, 92, 246), MauDo = Color.FromArgb(239, 68, 68), MauVang = Color.FromArgb(234, 179, 8), MauHong = Color.FromArgb(236, 72, 153),
            MauXam = Color.FromArgb(100, 116, 139), MauLuc = Color.FromArgb(34, 197, 94);
        public static readonly Color[] MauNhom = { MauLa, MauCam, MauDuong, MauTim, MauDo, MauVang, MauHong, MauLuc, MauXam };

        static GiaoDien() { BangMau(DocCaiDat("chu_de", "toi")); }

        /// Hình tròn tô chuyển sắc (sáng trên, đậm dưới) + viền mảnh; chữ / biểu tượng trắng ở giữa. Dùng cho nút biểu tượng, số bước, thẻ tiện ích.
        public static void VeTron(Graphics g, Rectangle r, Color mau, string chu, Font font, bool sang)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Color tren = ControlPaint.Light(mau, sang ? 0.6f : 0.25f), duoi = sang ? mau : ControlPaint.Dark(mau, 0.05f);
            using (LinearGradientBrush b = new LinearGradientBrush(new Rectangle(r.X, r.Y - 1, r.Width, r.Height + 2), tren, duoi, LinearGradientMode.Vertical))
                g.FillEllipse(b, r);
            using (Pen p = new Pen(Color.FromArgb(sang ? 200 : 90, Color.White), sang ? 2f : 1f))
                g.DrawEllipse(p, r.X + 1, r.Y + 1, r.Width - 2, r.Height - 2);
            if (!string.IsNullOrEmpty(chu))
                TextRenderer.DrawText(g, chu, font, new Rectangle(r.X - 4, r.Y + 1, r.Width + 8, r.Height), Color.White,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding | TextFormatFlags.SingleLine);
        }

        /// Ô vuông bo góc tô chuyển sắc (biểu tượng thanh dọc chọn phân hệ).
        public static void VeVuong(Graphics g, Rectangle r, Color mau, string chu, Font font, bool sang)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (GraphicsPath p = BoGoc(r, 9))
            {
                using (LinearGradientBrush b = new LinearGradientBrush(new Rectangle(r.X, r.Y - 1, r.Width, r.Height + 2),
                    ControlPaint.Light(mau, sang ? 0.5f : 0.2f), mau, LinearGradientMode.Vertical))
                    g.FillPath(b, p);
                if (sang) using (Pen v = new Pen(Color.FromArgb(220, Color.White), 2f)) g.DrawPath(v, p);
            }
            TextRenderer.DrawText(g, chu, font, new Rectangle(r.X - 4, r.Y + 1, r.Width + 8, r.Height), Color.White,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding | TextFormatFlags.SingleLine);
        }

        /// Lưới nút biểu tượng (ba nút một hàng, co theo bề rộng palette).
        public static FlowLayoutPanel LuoiNut()
        {
            FlowLayoutPanel p = new FlowLayoutPanel();
            p.AutoSize = true;
            p.WrapContents = true;
            p.BackColor = Nen;
            p.Margin = new Padding(0, 2, 0, 4);
            return p;
        }

        /// Thêm nút biểu tượng vào lưới; bề rộng do XepLuoi đặt.
        public static NutIcon NutLuoi(FlowLayoutPanel p, string icon, string chu, Color mau, Action viec)
        {
            NutIcon n = new NutIcon(icon, chu, mau);
            n.Click += delegate { An(viec); };
            p.Controls.Add(n);
            return n;
        }

        /// Chia đều nút trong lưới theo bề rộng w (3 nút một hàng; hẹp dưới 240 px thì 2).
        /// Bề rộng dùng được của khung cuộn: trừ sẵn thanh cuộn dọc (thanh cuộn hiện ra không đổi Size nên không có Resize để xếp lại).
        public static int RongDung(ScrollableControl t)
        {
            return Math.Max(200, t.ClientSize.Width - t.Padding.Horizontal - (t.VerticalScroll.Visible ? 0 : SystemInformation.VerticalScrollBarWidth) - 2);
        }

        public static void XepLuoi(FlowLayoutPanel p, int w)
        {
            int cot = w < 240 ? 2 : 3, rong = (w - 6 * cot) / cot;
            p.MaximumSize = new Size(w + 2, 0);
            foreach (Control c in p.Controls) if (c is NutIcon) { c.Width = rong; c.Margin = new Padding(0, 0, 6, 6); }
        }

        static Color H(int rgb) { return Color.FromArgb(255, Color.FromArgb(rgb)); }

        static void BangMau(string ten)
        {
            if (ten == "sang")
            {
                Xanh = Color.FromArgb(37, 117, 204); XanhDam = Color.FromArgb(0, 82, 163); XanhNhat = Color.FromArgb(222, 236, 252);
                Nen = Color.FromArgb(242, 246, 251); The = Color.FromArgb(255, 255, 255); Vien = Color.FromArgb(196, 212, 232); LuoiKe = Color.FromArgb(228, 233, 240);
                ChuDam = Color.FromArgb(24, 38, 74); ChuMo = Color.FromArgb(120, 130, 145);
                Do = Color.FromArgb(200, 30, 40); XanhLa = Color.FromArgb(46, 160, 67); Cam = Color.FromArgb(232, 130, 20);
            }
            else if (ten == "claude")
            {
                Xanh = H(0xD97757); XanhDam = H(0xC6613F); XanhNhat = H(0xF5E1D6);
                Nen = H(0xFAF9F5); The = H(0xF0EEE6); Vien = H(0xE8E6DC); LuoiKe = H(0xE3E1D7);
                ChuDam = H(0x141413); ChuMo = H(0x73726C);
                Do = H(0xB53333); XanhLa = H(0x5A8A3C); Cam = H(0xC88A2E);
            }
            else
            {
                ten = "toi";
                Xanh = H(0x10B981); XanhDam = H(0x059669); XanhNhat = H(0x134E4A);
                Nen = H(0x0F172A); The = H(0x1E293B); Vien = H(0x334155); LuoiKe = H(0x2B3648);
                ChuDam = H(0xE5E7EB); ChuMo = H(0x94A3B8);
                Do = H(0xF87171); XanhLa = H(0x34D399); Cam = H(0xFBBF24);
            }
            ChuDe = ten;
            Lenh.NenSang = ten != "toi"; // ảnh block (Block.cs) vẽ màu hợp nền ô; đổi kiểu thì ảnh tự vẽ lại lần nạp sau
        }

        // Thứ tự = vai trò: màu cũ ở vị trí i đổi thành màu mới ở vị trí i. Màu chữ trắng trên nền nhấn không nằm trong bộ chữ nên giữ nguyên.
        static Color[] BoNen() { return new[] { Xanh, XanhDam, XanhNhat, Nen, The, Vien, LuoiKe, ChuDam, ChuMo, Do, XanhLa, Cam }; }
        static Color[] BoChu() { return new[] { ChuDam, ChuMo, Do, XanhLa, Cam, Xanh }; }

        /// Đổi kiểu màu, lưu cài đặt, đổi màu ngay mọi control trong DangMo.
        public static void DatChuDe(string ten)
        {
            Color[] nen0 = BoNen(), chu0 = BoChu();
            BangMau(ten);
            LuuCaiDat("chu_de", ChuDe);
            Dictionary<int, Color> nen = AnhXa(nen0, BoNen()), chu = AnhXa(chu0, BoChu());
            foreach (Control c in DangMo) DoiMau(c, nen, chu);
            if (DaDoiChuDe != null) DaDoiChuDe();
        }

        static Dictionary<int, Color> AnhXa(Color[] cu, Color[] moi)
        {
            Dictionary<int, Color> m = new Dictionary<int, Color>();
            for (int i = 0; i < cu.Length; i++) if (!m.ContainsKey(cu[i].ToArgb())) m[cu[i].ToArgb()] = moi[i];
            return m;
        }

        static Color Doi(Dictionary<int, Color> m, Color c)
        {
            Color d;
            return c.A == 255 && m.TryGetValue(c.ToArgb(), out d) ? d : c;
        }

        static void DoiKieu(DataGridViewCellStyle s, Dictionary<int, Color> nen, Dictionary<int, Color> chu)
        {
            if (!s.BackColor.IsEmpty) s.BackColor = Doi(nen, s.BackColor);
            if (!s.SelectionBackColor.IsEmpty) s.SelectionBackColor = Doi(nen, s.SelectionBackColor);
            if (!s.ForeColor.IsEmpty) s.ForeColor = Doi(chu, s.ForeColor);
            if (!s.SelectionForeColor.IsEmpty) s.SelectionForeColor = Doi(chu, s.SelectionForeColor);
        }

        static void DoiMau(Control c, Dictionary<int, Color> nen, Dictionary<int, Color> chu)
        {
            c.BackColor = Doi(nen, c.BackColor);
            c.ForeColor = Doi(chu, c.ForeColor);
            NutBo nut = c as NutBo;
            if (nut != null) nut.Mau = Doi(nen, nut.Mau);
            DataGridView g = c as DataGridView;
            if (g != null)
            {
                g.BackgroundColor = Doi(nen, g.BackgroundColor);
                g.GridColor = Doi(nen, g.GridColor);
                DoiKieu(g.DefaultCellStyle, nen, chu);
                DoiKieu(g.ColumnHeadersDefaultCellStyle, nen, chu);
                foreach (DataGridViewColumn cot in g.Columns) DoiKieu(cot.DefaultCellStyle, nen, chu);
                foreach (DataGridViewRow d in g.Rows)
                    foreach (DataGridViewCell o in d.Cells)
                        if (o.HasStyle) DoiKieu(o.Style, nen, chu);
            }
            foreach (Control con in c.Controls) DoiMau(con, nen, chu);
            c.Invalidate();
        }

        // ---------- Cài đặt %APPDATA%\TKPCCC\cai_dat.json: JSON phẳng, giá trị là chuỗi ----------

        // Đọc, ghi ở TKPCCC.CaiDat (TKPCCC.Cad.dll): plugin cũng đọc bảng layer dây, dây theo thiết bị, kiểu dim.
        public static string FileCaiDat { get { return CaiDat.FileCaiDat; } }
        public static string DocCaiDat(string khoa, string macDinh) { return CaiDat.Doc(khoa, macDinh); }
        public static void LuuCaiDat(string khoa, string giaTri) { CaiDat.Luu(khoa, giaTri); }

        public static Label TieuDe(string t)
        {
            Label l = new Label();
            l.Text = t;
            l.Dock = DockStyle.Top;
            l.Height = 26;
            l.BackColor = Xanh;
            l.ForeColor = Color.White;
            l.Font = Dam;
            l.TextAlign = ContentAlignment.MiddleLeft;
            l.Padding = new Padding(6, 0, 0, 0);
            return l;
        }

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        static extern IntPtr SendMessage(IntPtr h, int msg, IntPtr w, string l);

        /// Ô tìm trong dải nền nhạt, có chữ mờ gợi ý (EM_SETCUEBANNER).
        public static Panel OTim(TextBox o, string goiY)
        {
            Panel p = new Panel();
            p.Dock = DockStyle.Top;
            p.Height = 36;
            p.BackColor = Nen;
            p.Padding = new Padding(6, 6, 6, 6);
            o.Dock = DockStyle.Fill;
            o.BackColor = The;
            o.ForeColor = ChuDam;
            o.Font = new Font("Segoe UI", 10f);
            o.BorderStyle = BorderStyle.FixedSingle;
            o.HandleCreated += delegate { SendMessage(o.Handle, 0x1501, (IntPtr)1, goiY); };
            p.Controls.Add(o);
            return p;
        }

        public static DataGridView Bang()
        {
            DataGridView g = new DataGridView();
            g.Dock = DockStyle.Fill;
            g.BackgroundColor = The;
            g.BorderStyle = BorderStyle.None;
            g.AllowUserToAddRows = false;
            g.AllowUserToDeleteRows = false;
            g.AllowUserToResizeRows = false;
            g.AllowUserToOrderColumns = false;
            g.RowHeadersVisible = false;
            g.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            g.MultiSelect = false;
            g.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            g.GridColor = LuoiKe;
            g.EnableHeadersVisualStyles = false;
            g.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
            g.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            g.ColumnHeadersHeight = 28;
            g.ColumnHeadersDefaultCellStyle.BackColor = Nen;
            g.ColumnHeadersDefaultCellStyle.ForeColor = ChuDam;
            g.ColumnHeadersDefaultCellStyle.SelectionBackColor = Nen;
            g.ColumnHeadersDefaultCellStyle.Font = Dam;
            g.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            g.DefaultCellStyle.Font = Chu;
            g.DefaultCellStyle.BackColor = The;
            g.DefaultCellStyle.ForeColor = ChuDam;
            g.DefaultCellStyle.SelectionBackColor = XanhNhat;
            g.DefaultCellStyle.SelectionForeColor = ChuDam;
            g.DefaultCellStyle.Padding = new Padding(4, 0, 4, 0);
            // Không nháy khi cuộn: DoubleBuffered là thuộc tính protected.
            typeof(DataGridView).GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(g, true, null);
            return g;
        }

        public static DataGridViewTextBoxColumn Cot(string ten, int rong, bool giua)
        {
            DataGridViewTextBoxColumn c = new DataGridViewTextBoxColumn();
            c.HeaderText = ten;
            c.Width = rong;
            c.SortMode = DataGridViewColumnSortMode.NotSortable;
            c.ReadOnly = true;
            if (giua) c.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            else c.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            return c;
        }

        /// Nút tròn màu (đầu dòng bảng). Trả hình tròn để bắt chuột.
        public static Rectangle Tron(Graphics g, int x, int giuaY, Color mau, string ky)
        {
            Rectangle r = new Rectangle(x, giuaY - 11, 22, 22);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (SolidBrush b = new SolidBrush(mau)) g.FillEllipse(b, r);
            using (Pen p = new Pen(Color.FromArgb(70, Color.Black))) g.DrawEllipse(p, r);
            using (StringFormat sf = new StringFormat())
            {
                sf.Alignment = StringAlignment.Center;
                sf.LineAlignment = StringAlignment.Center;
                g.DrawString(ky, Dam, Brushes.White, new RectangleF(r.X, r.Y + 1, r.Width, r.Height), sf);
            }
            return r;
        }

        public static GraphicsPath BoGoc(Rectangle r, int bk)
        {
            GraphicsPath p = new GraphicsPath();
            int d = bk * 2;
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }

        /// Khung "Hành động" bên phải: tiêu đề xanh + cột điều khiển xếp từ trên xuống (rộng theo khung).
        public static FlowLayoutPanel KhungHanhDong(Control cha, int rong)
        {
            Panel k = new Panel();
            k.Dock = DockStyle.Right;
            k.Width = rong;
            k.BackColor = Nen;
            k.Padding = new Padding(6, 0, 0, 0);
            FlowLayoutPanel f = new FlowLayoutPanel();
            f.Dock = DockStyle.Fill;
            f.FlowDirection = FlowDirection.TopDown;
            f.WrapContents = false;
            f.AutoScroll = true;
            f.BackColor = The;
            f.Padding = new Padding(8, 8, 8, 8);
            k.Controls.Add(f);
            k.Controls.Add(TieuDe("Hành động"));
            cha.Controls.Add(k);
            // Trong palette hẹp (≈ 400 px) khung bên phải chèn bảng còn vài chục px: dời xuống đáy, nút xếp ngang.
            EventHandler doi = delegate
            {
                bool hep = cha.Width < 640;
                if (hep == (k.Dock == DockStyle.Bottom)) return;
                k.Dock = hep ? DockStyle.Bottom : DockStyle.Right;
                if (hep) k.Height = 150; else k.Width = rong;
                f.FlowDirection = hep ? FlowDirection.LeftToRight : FlowDirection.TopDown;
                f.WrapContents = hep;
            };
            cha.Resize += doi;
            doi(null, null);
            return f;
        }

        /// Thêm điều khiển vào khung Hành động, rộng hết khung.
        public static T Them<T>(FlowLayoutPanel f, T c, int cao) where T : Control
        {
            c.Width = f.Parent.Width - f.Parent.Padding.Horizontal - f.Padding.Horizontal - 4;
            if (cao > 0) c.Height = cao;
            c.Margin = new Padding(0, 0, 0, 6);
            f.Controls.Add(c);
            return c;
        }

        /// Chạy việc của giao diện, lỗi thì báo hộp thoại (không để AutoCAD hiện "Unhandled exception").
        public static void An(Action viec)
        {
            try { viec(); }
            catch (System.Exception ex) { MessageBox.Show(ex.Message, "TKPCCC"); }
        }

        /// Mở plugin Bot chat (bundle riêng, lệnh TKBOTCHAT) kèm câu mẫu ("so_do" = sơ đồ nguyên lý); chưa cài thì chỉ đường cài.
        public static void MoBotChat(string mau)
        {
            if (!AppDomain.CurrentDomain.GetAssemblies().Any(a => a.GetName().Name == "TKPCCC.BotChat"))
            {
                MessageBox.Show("Chưa nạp plugin Bot chat. Mở 07_CAD\\bin\\TKPCCC.Hub.exe, nhóm Bot chat, cài \"Plugin Bot chat\" rồi mở lại AutoCAD.", "TKPCCC");
                return;
            }
            LuuCaiDat("bot_mau_cho", mau);
            GuiLenh("TKBOTCHAT\n", null, null);
        }

        /// Gửi lệnh cho bản vẽ đang mở. choLenh = tên lệnh mà khi nó kết thúc (xong, huỷ, lỗi) thì chạy sau() lúc AutoCAD rảnh.
        public static void GuiLenh(string lenh, string choLenh, Action sau)
        {
            Document doc = AcApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            if (choLenh != null && sau != null)
            {
                CommandEventHandler xong = null;
                xong = delegate(object s, CommandEventArgs e)
                {
                    if (e.GlobalCommandName != choLenh) return;
                    doc.CommandEnded -= xong;
                    doc.CommandCancelled -= xong;
                    doc.CommandFailed -= xong;
                    // Đọc bản vẽ khi AutoCAD rảnh: trong sự kiện lệnh, bản vẽ còn đang bận.
                    EventHandler ranh = null;
                    ranh = delegate { AcApp.Idle -= ranh; An(sau); };
                    AcApp.Idle += ranh;
                };
                doc.CommandEnded += xong;
                doc.CommandCancelled += xong;
                doc.CommandFailed += xong;
            }
            // \x03\x03 = Esc hai lần: huỷ lệnh đang dở trước khi chạy lệnh mới.
            doc.SendStringToExecute("\x03\x03" + lenh, true, false, true);
            try { Autodesk.AutoCAD.Internal.Utils.SetFocusToDwgView(); } // trả bàn phím về vùng vẽ để bấm điểm, gõ tiếp
            catch (System.Exception) { }
        }
    }

    /// Nút xanh đậm bo góc, chữ trắng đậm; sáng lên khi rê chuột.
    class NutBo : Button
    {
        bool tren;
        public Color Mau; // GiaoDien.DatChuDe đổi khi đổi kiểu màu

        public NutBo(string chu, Color mau)
        {
            Mau = mau;
            Text = chu;
            Font = GiaoDien.Dam;
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            Cursor = Cursors.Hand;
            MouseEnter += delegate { tren = true; Invalidate(); };
            MouseLeave += delegate { tren = false; Invalidate(); };
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.Clear(Parent != null ? Parent.BackColor : Color.White);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Color c = !Enabled ? Color.FromArgb(160, 170, 185) : tren ? ControlPaint.Light(Mau, 0.25f) : Mau;
            using (GraphicsPath p = GiaoDien.BoGoc(new Rectangle(0, 0, Width - 1, Height - 1), 6))
            using (SolidBrush b = new SolidBrush(c))
                g.FillPath(b, p);
            TextRenderer.DrawText(g, Text, Font, ClientRectangle, Color.White,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        }
    }

    /// Ô tổng số nổi bật: nhãn đỏ đậm, số to xanh lá, dòng phụ nhỏ.
    class OTong : Control
    {
        public string Nhan, So, Phu;

        public OTong(string nhan)
        {
            Nhan = nhan;
            So = "0";
            Phu = "";
            DoubleBuffered = true;
            BackColor = GiaoDien.The;
        }

        public void Dat(string so, string phu)
        {
            So = so;
            Phu = phu;
            AccessibleName = Nhan + ": " + so;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (GraphicsPath p = GiaoDien.BoGoc(new Rectangle(0, 0, Width - 1, Height - 1), 8))
            {
                using (SolidBrush b = new SolidBrush(GiaoDien.Nen)) g.FillPath(b, p);
                using (Pen v = new Pen(GiaoDien.Cam)) g.DrawPath(v, p);
            }
            TextFormatFlags giua = TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter;
            TextRenderer.DrawText(g, Nhan, GiaoDien.Dam, new Rectangle(0, 4, Width, 20), GiaoDien.Do, giua);
            TextRenderer.DrawText(g, So, GiaoDien.To, new Rectangle(0, 22, Width, 36), GiaoDien.XanhLa, giua);
            TextRenderer.DrawText(g, Phu, GiaoDien.Nho, new Rectangle(0, 58, Width, 18), GiaoDien.ChuMo, giua);
        }
    }

    /// Nút biểu tượng (ý từ video plugin 16/11): hình tròn màu chuyển sắc có biểu tượng, nhãn hai dòng bên dưới; rê chuột thì sáng viền.
    class NutIcon : Control
    {
        bool tren;
        public readonly string Icon;
        public Color Mau;

        public NutIcon(string icon, string chu, Color mau)
        {
            Icon = icon; Text = chu; Mau = mau;
            Size = new Size(88, 78);
            DoubleBuffered = true;
            Cursor = Cursors.Hand;
            AccessibleName = chu;
            AccessibleRole = AccessibleRole.PushButton;
            MouseEnter += delegate { tren = true; Invalidate(); };
            MouseLeave += delegate { tren = false; Invalidate(); };
            EnabledChanged += delegate { Invalidate(); };
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.Clear(Parent != null ? Parent.BackColor : GiaoDien.Nen);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            if (tren && Enabled)
                using (GraphicsPath p = GiaoDien.BoGoc(new Rectangle(0, 0, Width - 1, Height - 1), 8))
                using (SolidBrush b = new SolidBrush(GiaoDien.The)) g.FillPath(b, p);
            int d = 40;
            GiaoDien.VeTron(g, new Rectangle((Width - d) / 2, 4, d, d), Enabled ? Mau : Color.FromArgb(120, 130, 145), Icon, GiaoDien.Icon, tren && Enabled);
            TextRenderer.DrawText(g, Text.ToUpperInvariant(), GiaoDien.NhoDam, new Rectangle(2, d + 8, Width - 4, Height - d - 8),
                Enabled ? GiaoDien.ChuDam : GiaoDien.ChuMo, TextFormatFlags.HorizontalCenter | TextFormatFlags.Top | TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix);
        }
    }

    /// Hàng tab dạng viên thuốc (ý từ video plugin 16/11): tab đang chọn tô màu nhấn; hẹp thì xếp nhiều hàng đều nhau.
    class ThanhTab : Control
    {
        public readonly string[] Ten;
        public int Chon;
        public event Action<int> DaChon;
        readonly List<Rectangle> o = new List<Rectangle>();
        int tren = -1;

        public ThanhTab(string[] ten)
        {
            Ten = ten;
            DoubleBuffered = true;
            Cursor = Cursors.Hand;
            Dock = DockStyle.Top;
            Height = 36;
            AccessibleRole = AccessibleRole.PageTabList;
            Resize += delegate { Xep(); };
            MouseMove += delegate(object s, MouseEventArgs e) { int i = o.FindIndex(r => r.Contains(e.Location)); if (i != tren) { tren = i; Invalidate(); } };
            MouseLeave += delegate { tren = -1; Invalidate(); };
            MouseClick += delegate(object s, MouseEventArgs e) { int i = o.FindIndex(r => r.Contains(e.Location)); if (i >= 0) ChonTab(i); };
        }

        public void ChonTab(int i)
        {
            Chon = i;
            Invalidate();
            if (DaChon != null) GiaoDien.An(delegate { DaChon(i); });
        }

        void Xep()
        {
            o.Clear();
            int w = Math.Max(120, Width - 12), cot = Ten.Length;
            // Mỗi viên ít nhất 84 px: không đủ thì giảm số cột (4 tab trên palette 320 px -> 2 x 2 như mẫu).
            while (cot > 1 && w / cot < 84) cot--;
            cot = (Ten.Length + (Ten.Length + cot - 1) / cot - 1) / ((Ten.Length + cot - 1) / cot); // chia hàng đều: 4 tab 3 + 1 -> 2 x 2
            int hang = (Ten.Length + cot - 1) / cot, rong = (w - 6 * (cot - 1)) / cot;
            for (int i = 0; i < Ten.Length; i++) o.Add(new Rectangle(6 + (i % cot) * (rong + 6), 6 + (i / cot) * 30, rong, 26));
            int cao = 10 + hang * 30;
            if (Height != cao) Height = cao;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.Clear(GiaoDien.Nen);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            if (o.Count != Ten.Length) Xep();
            for (int i = 0; i < o.Count; i++)
            {
                bool c = i == Chon;
                using (GraphicsPath p = GiaoDien.BoGoc(o[i], 12))
                {
                    if (c) using (LinearGradientBrush b = new LinearGradientBrush(o[i], ControlPaint.Light(GiaoDien.MauTim, 0.3f), GiaoDien.MauTim, LinearGradientMode.Vertical)) g.FillPath(b, p);
                    else using (SolidBrush b = new SolidBrush(i == tren ? GiaoDien.Vien : GiaoDien.The)) g.FillPath(b, p);
                }
                TextRenderer.DrawText(g, Ten[i], GiaoDien.NhoDam, o[i], c ? Color.White : GiaoDien.ChuDam,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
            }
        }
    }

    /// Tiêu đề nhóm nhỏ: chữ hoa mờ + vạch màu nhấn bên dưới (ý từ video plugin 16/11).
    class TieuDeNhom : Control
    {
        readonly Color mau;

        public TieuDeNhom(string chu, Color mau)
        {
            Text = chu;
            this.mau = mau;
            Height = 22;
            Margin = new Padding(0, 10, 0, 4);
            DoubleBuffered = true;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.Clear(Parent != null ? Parent.BackColor : GiaoDien.Nen);
            TextRenderer.DrawText(g, Text.ToUpperInvariant(), GiaoDien.NhoDam, new Rectangle(0, 0, Width, 16), GiaoDien.ChuMo,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
            using (SolidBrush b = new SolidBrush(mau)) g.FillRectangle(b, 0, 18, Math.Min(Width, 48), 2);
            using (SolidBrush b = new SolidBrush(GiaoDien.Vien)) g.FillRectangle(b, 48, 19, Math.Max(0, Width - 48), 1);
        }
    }

    /// Khung TRẠNG THÁI (ý từ video plugin 16/11): mỗi dòng một dấu ✔ xanh (đủ), ⚠ vàng (cần làm), • mờ (thông tin).
    class KhungTrangThai : Control
    {
        public const int DAT = 1, CANH_BAO = 0, TIN = -1;
        readonly List<KeyValuePair<int, string>> dong = new List<KeyValuePair<int, string>>();

        public KhungTrangThai()
        {
            DoubleBuffered = true;
            Margin = new Padding(0, 10, 0, 6);
            Height = 40;
            Resize += delegate { DoCao(); };
        }

        public void Dat(params KeyValuePair<int, string>[] ds)
        {
            dong.Clear();
            dong.AddRange(ds.Where(x => !string.IsNullOrEmpty(x.Value)));
            AccessibleName = "Trạng thái: " + string.Join("; ", dong.Select(x => x.Value));
            DoCao();
            Invalidate();
        }

        public static KeyValuePair<int, string> D(int loai, string chu) { return new KeyValuePair<int, string>(loai, chu); }

        void DoCao()
        {
            int h = 30;
            foreach (KeyValuePair<int, string> x in dong)
                h += TextRenderer.MeasureText(x.Value, GiaoDien.Nho, new Size(Math.Max(60, Width - 34), 0), TextFormatFlags.WordBreak).Height + 3;
            if (Height != h + 6) Height = h + 6;
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
            using (SolidBrush b = new SolidBrush(GiaoDien.MauDuong)) g.FillRectangle(b, 10, 10, 3, 12);
            TextRenderer.DrawText(g, "TRẠNG THÁI", GiaoDien.NhoDam, new Point(18, 9), GiaoDien.ChuDam);
            int y = 30;
            foreach (KeyValuePair<int, string> x in dong)
            {
                Color m = x.Key == DAT ? GiaoDien.MauLuc : x.Key == CANH_BAO ? (GiaoDien.ChuDe == "toi" ? GiaoDien.MauVang : GiaoDien.Cam) : GiaoDien.ChuMo;
                TextRenderer.DrawText(g, x.Key == DAT ? "✔" : x.Key == CANH_BAO ? "⚠" : "•", GiaoDien.Dam, new Point(10, y - 1), m);
                Size s = TextRenderer.MeasureText(x.Value, GiaoDien.Nho, new Size(Math.Max(60, Width - 34), 0), TextFormatFlags.WordBreak);
                TextRenderer.DrawText(g, x.Value, GiaoDien.Nho, new Rectangle(28, y, Width - 34, s.Height), x.Key == TIN ? GiaoDien.ChuMo : m,
                    TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix);
                y += s.Height + 3;
            }
        }
    }
}
