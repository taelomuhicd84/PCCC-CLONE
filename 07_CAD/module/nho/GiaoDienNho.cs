// Giao diện tối thiểu cho các plugin tách riêng (YouTube, Bot chat; người dùng 17/11: "tách riêng làm 3 plugin, đừng gom nhiều nó rối").
// Mỗi plugin biên dịch kèm file này nên chạy được khi KHÔNG cài plugin PCCC. Cùng kiểu màu ("chu_de") và cùng file cài đặt với palette PCCC
// (%APPDATA%\TKPCCC\cai_dat.json, định dạng như TKPCCC.CaiDat của TKPCCC.Cad.dll): đổi kiểu màu ở palette PCCC thì plugin này theo ở lần mở sau.
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace TKPCCC
{
    static class GiaoDien
    {
        public static Color Xanh, XanhDam, Nen, The, ChuDam, ChuMo, Do;
        public static readonly Font Chu = new Font("Segoe UI", 9f);
        public static readonly Font Dam = new Font("Segoe UI", 9f, FontStyle.Bold);
        public static readonly Font Nho = new Font("Segoe UI", 8f);

        static GiaoDien()
        {
            string ten = DocCaiDat("chu_de", "toi");
            if (ten == "sang")
            {
                Xanh = Color.FromArgb(37, 117, 204); XanhDam = Color.FromArgb(0, 82, 163);
                Nen = Color.FromArgb(242, 246, 251); The = Color.FromArgb(255, 255, 255);
                ChuDam = Color.FromArgb(24, 38, 74); ChuMo = Color.FromArgb(120, 130, 145); Do = Color.FromArgb(200, 30, 40);
            }
            else if (ten == "claude")
            {
                Xanh = H(0xD97757); XanhDam = H(0xC6613F); Nen = H(0xFAF9F5); The = H(0xF0EEE6);
                ChuDam = H(0x141413); ChuMo = H(0x73726C); Do = H(0xB53333);
            }
            else
            {
                Xanh = H(0x10B981); XanhDam = H(0x059669); Nen = H(0x0F172A); The = H(0x1E293B);
                ChuDam = H(0xE5E7EB); ChuMo = H(0x94A3B8); Do = H(0xF87171);
            }
        }

        static Color H(int rgb) { return Color.FromArgb((rgb >> 16) & 255, (rgb >> 8) & 255, rgb & 255); }

        static string FileCaiDat
        {
            get
            {
                string e = Environment.GetEnvironmentVariable("TKPCCC_CAI_DAT");
                if (!string.IsNullOrEmpty(e)) return e;
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TKPCCC", "cai_dat.json");
            }
        }

        static Dictionary<string, string> DocTatCa()
        {
            Dictionary<string, string> d = new Dictionary<string, string>();
            try
            {
                if (File.Exists(FileCaiDat))
                    foreach (Match m in Regex.Matches(File.ReadAllText(FileCaiDat, Encoding.UTF8), "\"([A-Za-z_0-9]+)\"\\s*:\\s*\"([^\"\\\\]*)\""))
                        d[m.Groups[1].Value] = m.Groups[2].Value;
            }
            catch (Exception) { } // file hỏng: dùng mặc định
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
            catch (Exception) { } // không ghi được (thư mục khoá): chỉ mất nhớ lựa chọn
        }

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        static extern IntPtr SendMessage(IntPtr h, int msg, IntPtr w, string l);

        /// Ô nhập một dòng có chữ gợi ý mờ, nằm trên cùng khung.
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

        /// Chạy việc của giao diện, lỗi thì báo hộp thoại (không để AutoCAD hiện "Unhandled exception").
        public static void An(Action viec)
        {
            try { viec(); }
            catch (Exception ex) { MessageBox.Show(ex.Message, "TKPCCC"); }
        }
    }

    /// Nút bo góc màu nhấn (như NutBo của palette PCCC).
    class NutBo : Button
    {
        bool tren;
        public Color Mau;

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
}
