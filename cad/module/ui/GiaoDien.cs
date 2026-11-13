// Kiểu giao diện chung của các bảng TKPCCC (PCCC_BLOCK, PCCC_HELP): tiêu đề nền xanh, ô tìm có chữ gợi ý, bảng dòng cao kẻ ngang,
// khung "Hành động" bên phải với nút xanh đậm bo góc và ô tổng số. Chỉ vẽ giao diện, không đụng bản vẽ.
using System;
using System.Drawing;
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
        public static readonly Color Xanh = Color.FromArgb(37, 117, 204);      // tiêu đề nhóm, tiêu đề khung
        public static readonly Color XanhDam = Color.FromArgb(0, 82, 163);     // nút
        public static readonly Color XanhNhat = Color.FromArgb(222, 236, 252); // dòng đang chọn
        public static readonly Color Nen = Color.FromArgb(242, 246, 251);
        public static readonly Color Vien = Color.FromArgb(196, 212, 232);
        public static readonly Color ChuDam = Color.FromArgb(24, 38, 74);
        public static readonly Color ChuMo = Color.FromArgb(120, 130, 145);
        public static readonly Color Do = Color.FromArgb(200, 30, 40);
        public static readonly Color XanhLa = Color.FromArgb(46, 160, 67);
        public static readonly Color Cam = Color.FromArgb(232, 130, 20);
        public static readonly Font Chu = new Font("Segoe UI", 9f);
        public static readonly Font Dam = new Font("Segoe UI", 9f, FontStyle.Bold);
        public static readonly Font Nho = new Font("Segoe UI", 8f);
        public static readonly Font To = new Font("Segoe UI", 18f, FontStyle.Bold);

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
            g.BackgroundColor = Color.White;
            g.BorderStyle = BorderStyle.None;
            g.AllowUserToAddRows = false;
            g.AllowUserToDeleteRows = false;
            g.AllowUserToResizeRows = false;
            g.AllowUserToOrderColumns = false;
            g.RowHeadersVisible = false;
            g.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            g.MultiSelect = false;
            g.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            g.GridColor = Color.FromArgb(228, 233, 240);
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
            f.BackColor = Color.White;
            f.Padding = new Padding(8, 8, 8, 8);
            k.Controls.Add(f);
            k.Controls.Add(TieuDe("Hành động"));
            cha.Controls.Add(k);
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
        readonly Color mau;

        public NutBo(string chu, Color mau)
        {
            this.mau = mau;
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
            Color c = !Enabled ? Color.FromArgb(160, 170, 185) : tren ? ControlPaint.Light(mau, 0.25f) : mau;
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
            BackColor = Color.White;
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
                using (SolidBrush b = new SolidBrush(Color.FromArgb(255, 248, 240))) g.FillPath(b, p);
                using (Pen v = new Pen(Color.FromArgb(240, 190, 140))) g.DrawPath(v, p);
            }
            TextFormatFlags giua = TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter;
            TextRenderer.DrawText(g, Nhan, GiaoDien.Dam, new Rectangle(0, 4, Width, 20), GiaoDien.Do, giua);
            TextRenderer.DrawText(g, So, GiaoDien.To, new Rectangle(0, 22, Width, 36), GiaoDien.XanhLa, giua);
            TextRenderer.DrawText(g, Phu, GiaoDien.Nho, new Rectangle(0, 58, Width, 18), GiaoDien.ChuMo, giua);
        }
    }
}
