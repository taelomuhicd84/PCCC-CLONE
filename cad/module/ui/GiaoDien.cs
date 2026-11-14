// Kiểu giao diện chung của các bảng TKPCCC (palette PCCC, PCCC_BLOCK, PCCC_HELP): tiêu đề nền nhấn, ô tìm có chữ gợi ý, bảng dòng cao kẻ ngang,
// khung "Hành động" bên phải với nút bo góc và ô tổng số. Chỉ vẽ giao diện, không đụng bản vẽ.
// 3 kiểu màu (toi mặc định, sang, claude) lưu ở %APPDATA%\TKPCCC\cai_dat.json; đổi kiểu thì control đang mở đổi màu ngay (DatChuDe).
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
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

        static GiaoDien() { BangMau(DocCaiDat("chu_de", "toi")); }

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

        public static string FileCaiDat
        {
            get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TKPCCC", "cai_dat.json"); }
        }

        static Dictionary<string, string> DocTatCa()
        {
            Dictionary<string, string> d = new Dictionary<string, string>();
            try
            {
                if (File.Exists(FileCaiDat))
                    foreach (Match m in Regex.Matches(File.ReadAllText(FileCaiDat, Encoding.UTF8), "\"([A-Za-z_]+)\"\\s*:\\s*\"([^\"\\\\]*)\""))
                        d[m.Groups[1].Value] = m.Groups[2].Value;
            }
            catch (System.Exception) { } // file hỏng: dùng mặc định
            return d;
        }

        public static string DocCaiDat(string khoa, string macDinh)
        {
            string v;
            return DocTatCa().TryGetValue(khoa, out v) && v != "" ? v : macDinh;
        }

        /// Ghi một khoá, giữ các khoá khác (giá trị bỏ dấu " và \ để JSON luôn hợp lệ).
        public static void LuuCaiDat(string khoa, string giaTri)
        {
            Dictionary<string, string> d = DocTatCa();
            d[khoa] = (giaTri ?? "").Replace("\"", "").Replace("\\", "");
            List<string> dong = new List<string>();
            foreach (KeyValuePair<string, string> p in d) dong.Add("  \"" + p.Key + "\": \"" + p.Value + "\"");
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(FileCaiDat));
                File.WriteAllText(FileCaiDat, "{\r\n" + string.Join(",\r\n", dong) + "\r\n}\r\n", new UTF8Encoding(false));
            }
            catch (System.Exception) { } // không ghi được: kiểu vẫn đổi trong phiên này
        }

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
}
