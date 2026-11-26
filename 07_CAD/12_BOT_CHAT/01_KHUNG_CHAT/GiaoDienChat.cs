// Bot chat kiểu khung chat mẫu người dùng gửi 18/11 (Claude / VS Code): bong bóng bo góc, chip ("Link VS Code session", "Selection"), nút biểu tượng tròn,
// nút model "Opus 5.5 · High". Biểu tượng vẽ bằng font "Segoe MDL2 Assets" (có sẵn Windows 10/11; emoji không hiện được trong WinForms GDI).
// Danh mục model: Claude theo bảng model của Anthropic (không Haiku, không Fable: dặn 14/11 và memory); Gemini đọc từ `agy models` (lưu "agy_models").
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;

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

    static class Ve
    {
        public const string THEM = "", LICH_SU = "", CAI_DAT = "", GUI = "", DUNG = "", LINK = "",
            THU_MUC = "", CHON = "", SKILL = "", XUONG = "", ANH = "", DONG = "";
        static Font icon, iconNho;
        static readonly bool coMdl2 = FontFamily.Families.Any(f => f.Name == "Segoe MDL2 Assets");

        public static Font Icon { get { return icon ?? (icon = new Font(coMdl2 ? "Segoe MDL2 Assets" : "Segoe UI Symbol", 10f)); } }
        public static Font IconNho { get { return iconNho ?? (iconNho = new Font(coMdl2 ? "Segoe MDL2 Assets" : "Segoe UI Symbol", 8f)); } }

        /// Không có font MDL2 (Windows cũ): đổi biểu tượng sang ký tự thường.
        public static string G(string glyph)
        {
            if (coMdl2) return glyph;
            switch (glyph) { case THEM: return "+"; case GUI: return "➤"; case DUNG: return "■"; case XUONG: return "▾"; case DONG: return "✕"; default: return "•"; }
        }

        public static Color Tron(Color a, Color b, float k) { return Color.FromArgb((int)(a.R + (b.R - a.R) * k), (int)(a.G + (b.G - a.G) * k), (int)(a.B + (b.B - a.B) * k)); }
    }

    /// Nút biểu tượng: vuông trong suốt (sáng lên khi rê chuột) hoặc tròn tô màu (nút gửi).
    class NutIcon : Control
    {
        public string Glyph;
        public Color? Nen;   // có màu = nút tròn tô màu
        bool tren;

        public NutIcon(string glyph, string goiY, ToolTip tt)
        {
            Glyph = glyph;
            Size = new Size(28, 28);
            Cursor = Cursors.Hand;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            ForeColor = GiaoDien.ChuMo;
            if (tt != null) tt.SetToolTip(this, goiY);
            MouseEnter += delegate { tren = true; Invalidate(); };
            MouseLeave += delegate { tren = false; Invalidate(); };
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle r = new Rectangle(1, 1, Width - 3, Height - 3);
            Color chu = ForeColor;
            if (Nen.HasValue)
            {
                using (SolidBrush b = new SolidBrush(tren ? ControlPaint.Light(Nen.Value, 0.2f) : Nen.Value)) g.FillEllipse(b, r);
                chu = Color.White;
            }
            else if (tren)
                using (GraphicsPath p = GiaoDien.BoGoc(r, 6))
                using (SolidBrush b = new SolidBrush(Color.FromArgb(40, GiaoDien.ChuDam))) g.FillPath(b, p);
            TextRenderer.DrawText(g, Ve.G(Glyph), Ve.Icon, ClientRectangle, tren && !Nen.HasValue ? GiaoDien.ChuDam : chu,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
        }
    }

    /// Chip bo tròn: biểu tượng + chữ (+ dấu ✕ để bỏ). Bấm chip = BamChip, bấm ✕ = Bo.
    class Chip : Control
    {
        public string Glyph;
        public bool CoBo, Dam;       // Dam = chip dạng nút có nền (nút model, nút dự án)
        public event Action BamChip, Bo;
        bool tren;
        const int CAO = 24;

        public Chip(string glyph, string chu, bool coBo)
        {
            Glyph = glyph;
            Text = chu;
            CoBo = coBo;
            Font = GiaoDien.Nho;
            Cursor = Cursors.Hand;
            Margin = new Padding(0, 2, 6, 2);
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            ForeColor = GiaoDien.ChuDam;
            MouseEnter += delegate { tren = true; Invalidate(); };
            MouseLeave += delegate { tren = false; Invalidate(); };
            TextChanged += delegate { DoCo(); };
            DoCo();
        }

        int RongIcon { get { return Glyph == null ? 0 : 18; } }

        public void DoCo()
        {
            int w = TextRenderer.MeasureText(Text, Font, Size.Empty, TextFormatFlags.NoPadding).Width;
            Size = new Size(Math.Min(10 + RongIcon + w + (CoBo ? 18 : 0) + 8, 260), CAO);
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);
            if (e.Button != MouseButtons.Left) return;
            if (CoBo && e.X >= Width - 20) { if (Bo != null) GiaoDien.An(Bo); }
            else if (BamChip != null) GiaoDien.An(BamChip);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle r = new Rectangle(0, 0, Width - 1, Height - 1);
            using (GraphicsPath p = GiaoDien.BoGoc(r, 11))
            {
                Color nen = Dam ? GiaoDien.The : Color.Transparent;
                if (tren) nen = Ve.Tron(Dam ? GiaoDien.The : GiaoDien.Nen, GiaoDien.ChuDam, 0.12f);
                if (nen.A > 0) using (SolidBrush b = new SolidBrush(nen)) g.FillPath(b, p);
                using (Pen v = new Pen(Ve.Tron(GiaoDien.Nen, GiaoDien.ChuMo, 0.55f))) g.DrawPath(v, p);
            }
            int x = 9;
            if (Glyph != null)
            {
                TextRenderer.DrawText(g, Ve.G(Glyph), Ve.IconNho, new Rectangle(x, 0, 16, Height), GiaoDien.ChuMo, TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
                x += RongIcon;
            }
            TextRenderer.DrawText(g, Text, Font, new Rectangle(x, 0, Width - x - (CoBo ? 20 : 6), Height), ForeColor,
                TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine);
            if (CoBo) TextRenderer.DrawText(g, Ve.G(Ve.DONG), Ve.IconNho, new Rectangle(Width - 20, 0, 14, Height), GiaoDien.ChuMo, TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
        }
    }

    /// Một tin: người dùng (bong bóng màu nhấn, lệch phải), AI (bong bóng nền nhạt, lệch trái), dòng hệ thống / công cụ (chữ mờ, không bong bóng).
    /// Chuột phải: "Sao chép". Rộng tối đa 85 % khung; cao theo chữ.
    /// Khung cuộn tin: bấm vào ô chữ (lấy focus) không cuộn khung tới ô đó, giữ nguyên chỗ người dùng đang đọc.
    class KhungCuon : Panel
    {
        protected override Point ScrollToControl(Control c) { return DisplayRectangle.Location; }
    }

    class BongBong : Control
    {
        public readonly int Kieu; // 0 người dùng, 1 AI, 2 hệ thống / công cụ
        const int DEM = 10, NHO_CAO = 60, NHO_CACH = 6;
        // Người dùng 23/11: ảnh đã gửi hiện thu nhỏ trong bong bóng, bấm mở ảnh gốc (trình xem ảnh của Windows).
        readonly List<string> anh = new List<string>();
        readonly List<Image> nho = new List<Image>();
        readonly List<Rectangle> oNho = new List<Rectangle>();
        // Người dùng 26/11: quét chuột chọn một đoạn để copy -> chữ nằm trong ô chỉ đọc, không vẽ tay nữa.
        readonly TextBox o = new TextBox();
        static readonly Regex DONG_ANH = new Regex(@"^- (.+\.(png|jpe?g|bmp|gif))\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase);

        /// anh: ảnh đính kèm; null = tin người dùng nạp từ lịch sử thì lấy đường dẫn ảnh trong lời nhắn ("Ảnh đính kèm ...: - <file>").
        public BongBong(int kieu, string chu, Color mau, IEnumerable<string> anhKem = null)
        {
            Kieu = kieu;
            Text = chu;
            ForeColor = kieu == 0 ? Color.White : mau;
            Font = kieu == 2 ? GiaoDien.Nho : GiaoDien.Chu;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            ContextMenuStrip m = new ContextMenuStrip();
            m.Items.Add("Sao chép", null, delegate { try { Clipboard.SetText(Text); } catch (Exception) { } });
            ContextMenuStrip = m;
            o.Multiline = true; o.ReadOnly = true; o.WordWrap = true; o.TabStop = false;
            o.BorderStyle = BorderStyle.None; o.ScrollBars = ScrollBars.None;
            o.Font = Font; o.ForeColor = ForeColor; o.BackColor = kieu == 0 ? GiaoDien.XanhDam : kieu == 1 ? GiaoDien.The : GiaoDien.Nen;
            o.Text = chu.Replace("\r\n", "\n").Replace("\n", "\r\n");
            Controls.Add(o);
            if (kieu != 0) return;
            foreach (string f in anhKem ?? DONG_ANH.Matches(chu).Cast<Match>().Select(x => x.Groups[1].Value.Trim()))
            {
                if (!File.Exists(f)) continue;
                Image i = BotChat.ThuNho(f);
                if (i == null) continue;
                anh.Add(f);
                nho.Add(i);
            }
        }

        /// Đặt cỡ theo bề rộng khung; trả chiều cao.
        public int DoCo(int rongKhung)
        {
            int toiDa = Kieu == 2 ? rongKhung - 8 : (int)(rongKhung * 0.85);
            // đo hẹp hơn ô chữ 8 px (lề trong của TextBox) để ô không xuống dòng nhiều hơn số dòng đã đo
            Size c = TextRenderer.MeasureText(o.Text, Font, new Size(Math.Max(40, toiDa - 2 * DEM - 8), int.MaxValue), TextFormatFlags.WordBreak | TextFormatFlags.TextBoxControl);
            Size = Kieu == 2 ? new Size(toiDa, c.Height + 4) : new Size(Math.Min(toiDa, c.Width + 2 * DEM + 10), c.Height + 2 * DEM - 4);
            o.Bounds = Kieu == 2 ? new Rectangle(2, 2, Width - 4, c.Height + 2) : new Rectangle(DEM, DEM - 2, Width - 2 * DEM + 2, c.Height + 2);
            if (nho.Count == 0) return Height;
            // ảnh thu nhỏ xếp hàng dưới chữ, hết bề rộng thì xuống hàng
            oNho.Clear();
            int x = DEM, y = Height - DEM + 4, rongDung = 0;
            foreach (Image i in nho)
            {
                if (x > DEM && x + i.Width > toiDa - DEM) { x = DEM; y += NHO_CAO + NHO_CACH; }
                oNho.Add(new Rectangle(x, y + (NHO_CAO - i.Height) / 2, i.Width, i.Height));
                x += i.Width + NHO_CACH;
                rongDung = Math.Max(rongDung, x - NHO_CACH + DEM);
            }
            Size = new Size(Math.Min(toiDa, Math.Max(Width, rongDung)), y + NHO_CAO + DEM);
            return Height;
        }

        int NhoTai(Point p) { return oNho.FindIndex(r => r.Contains(p)); }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            Cursor = NhoTai(e.Location) >= 0 ? Cursors.Hand : Cursors.Default;
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);
            int k = e.Button == MouseButtons.Left ? NhoTai(e.Location) : -1;
            if (k >= 0) try { Process.Start(anh[k]); } catch (Exception) { }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) foreach (Image i in nho) i.Dispose();
            base.Dispose(disposing);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.Clear(Parent != null ? Parent.BackColor : GiaoDien.Nen);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            if (Kieu != 2)
            {
                using (GraphicsPath p = GiaoDien.BoGoc(new Rectangle(0, 0, Width - 1, Height - 1), 12))
                using (SolidBrush b = new SolidBrush(Kieu == 0 ? GiaoDien.XanhDam : GiaoDien.The))
                    g.FillPath(b, p);
            }
            for (int i = 0; i < oNho.Count; i++)
            {
                g.DrawImage(nho[i], oNho[i]);
                g.DrawRectangle(Pens.White, oNho[i].X, oNho[i].Y, oNho[i].Width - 1, oNho[i].Height - 1);
            }
        }
    }

}
