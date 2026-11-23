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
    class BongBong : Control
    {
        public readonly int Kieu; // 0 người dùng, 1 AI, 2 hệ thống / công cụ
        const int DEM = 10, NHO_CAO = 60, NHO_CACH = 6;
        // Người dùng 23/11: ảnh đã gửi hiện thu nhỏ trong bong bóng, bấm mở ảnh gốc (trình xem ảnh của Windows).
        readonly List<string> anh = new List<string>();
        readonly List<Image> nho = new List<Image>();
        readonly List<Rectangle> oNho = new List<Rectangle>();
        int caoChu;
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
            Size c = TextRenderer.MeasureText(Text, Font, new Size(Math.Max(40, toiDa - 2 * DEM), int.MaxValue), TextFormatFlags.WordBreak | TextFormatFlags.TextBoxControl);
            Size = Kieu == 2 ? new Size(toiDa, c.Height + 4) : new Size(Math.Min(toiDa, c.Width + 2 * DEM + 2), c.Height + 2 * DEM - 4);
            caoChu = c.Height;
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
            Rectangle chu = new Rectangle(DEM, DEM - 2, Width - 2 * DEM + 2, oNho.Count == 0 ? Height - 2 * DEM + 4 : caoChu + 4);
            if (Kieu != 2)
            {
                using (GraphicsPath p = GiaoDien.BoGoc(new Rectangle(0, 0, Width - 1, Height - 1), 12))
                using (SolidBrush b = new SolidBrush(Kieu == 0 ? GiaoDien.XanhDam : GiaoDien.The))
                    g.FillPath(b, p);
            }
            else chu = new Rectangle(2, 2, Width - 4, Height - 2);
            TextRenderer.DrawText(g, Text, Font, chu, ForeColor, TextFormatFlags.WordBreak | TextFormatFlags.TextBoxControl);
            for (int i = 0; i < oNho.Count; i++)
            {
                g.DrawImage(nho[i], oNho[i]);
                g.DrawRectangle(Pens.White, oNho[i].X, oNho[i].Y, oNho[i].Width - 1, oNho[i].Height - 1);
            }
        }
    }

    class ModelAi
    {
        public int Ai;          // 0 Claude (claude -p), 1 Antigravity (agy -p)
        public string Id, Ten;  // Antigravity có mức effort: Id = gốc, tên agy thật = Id + "-" + mức
        public string[] Effort; // null = model không chọn mức (Antigravity: Orchestrator, Claude qua agy, GPT-OSS)
    }

    static class DanhMucModel
    {
        static readonly string[] E5 = { "low", "medium", "high", "xhigh", "max" }, E4 = { "low", "medium", "high", "max" };

        // Bảng model của Anthropic (skill claude-api, 03/11/2026): 4.6 chưa có xhigh. Phần tử đầu là mặc định.
        public static readonly List<ModelAi> Claude = new List<ModelAi>
        {
            new ModelAi { Ai = 0, Id = "claude-opus-5-5", Ten = "Opus 5.5", Effort = E5 },
            new ModelAi { Ai = 0, Id = "claude-opus-5", Ten = "Opus 5", Effort = E5 },
            new ModelAi { Ai = 0, Id = "claude-opus-4-8", Ten = "Opus 4.8", Effort = E5 },
            new ModelAi { Ai = 0, Id = "claude-opus-4-7", Ten = "Opus 4.7", Effort = E5 },
            new ModelAi { Ai = 0, Id = "claude-opus-4-6", Ten = "Opus 4.6", Effort = E4 },
            new ModelAi { Ai = 0, Id = "claude-sonnet-5-5", Ten = "Sonnet 5.5", Effort = E5 },
            new ModelAi { Ai = 0, Id = "claude-sonnet-5", Ten = "Sonnet 5", Effort = E5 },
            new ModelAi { Ai = 0, Id = "claude-sonnet-4-6", Ten = "Sonnet 4.6", Effort = E4 },
        };

        // `agy models` ngày 18/11/2026 (dùng khi chưa đọc được từ máy).
        const string AGY_MAC_DINH = "gemini-4-hydrogen-high=Gemini 4 Hydrogen (High)|gemini-4-hydrogen-xhigh=Gemini 4 Hydrogen (XHigh)|gemini-4-hydrogen-medium=Gemini 4 Hydrogen (Medium)|"
            + "gemini-4-hydrogen-low=Gemini 4 Hydrogen (Low)|gemini-4-argon-medium=Gemini 4 Argon (Medium)|gemini-4-argon-high=Gemini 4 Argon (High)|gemini-4-argon-low=Gemini 4 Argon (Low)|"
            + "gemini-4-carbon-medium=Gemini 4 Carbon (Medium)|gemini-4-carbon-high=Gemini 4 Carbon (High)|gemini-4-carbon-low=Gemini 4 Carbon (Low)|"
            + "gemini-3.8-flash-high=Gemini 3.8 Flash (High)|gemini-3.8-flash-medium=Gemini 3.8 Flash (Medium)|gemini-3.8-flash-low=Gemini 3.8 Flash (Low)|"
            + "gemini-pro-orchestrator=Orchestrator (Carbon)|gemini-pro-orchestrator-argon=Orchestrator (Argon)|claude-sonnet-4-6=Claude Sonnet 4.6 (Thinking)|"
            + "claude-opus-4-6-thinking=Claude Opus 4.6 (Thinking)|gpt-oss-120b-medium=GPT-OSS 120B (Medium)";

        static readonly string[] MUC = { "low", "medium", "high", "xhigh", "max" };

        /// Model Antigravity: gom "gốc-mức" thành một model nhiều mức (theo thứ tự low → max).
        public static List<ModelAi> Antigravity()
        {
            List<ModelAi> kq = new List<ModelAi>();
            foreach (string cap in GiaoDien.DocCaiDat("agy_models", AGY_MAC_DINH).Split('|'))
            {
                int i = cap.IndexOf('=');
                if (i <= 0) continue;
                string id = cap.Substring(0, i), ten = cap.Substring(i + 1);
                Match m = Regex.Match(id, "^(.+)-(low|medium|high|xhigh|max)$");
                Match mt = Regex.Match(ten, "^(.+?) \\((Low|Medium|High|XHigh|Max)\\)$");
                if (m.Success && mt.Success && !id.StartsWith("gpt-"))
                {
                    ModelAi g = kq.FirstOrDefault(x => x.Id == m.Groups[1].Value);
                    if (g == null) kq.Add(g = new ModelAi { Ai = 1, Id = m.Groups[1].Value, Ten = mt.Groups[1].Value, Effort = new string[0] });
                    g.Effort = g.Effort.Concat(new[] { m.Groups[2].Value }).OrderBy(x => Array.IndexOf(MUC, x)).ToArray();
                }
                else kq.Add(new ModelAi { Ai = 1, Id = id, Ten = ten });
            }
            return kq;
        }

        /// Chạy `agy models` nền, lưu danh sách mới vào "agy_models" (lần mở menu sau dùng). Lỗi / không có agy: giữ danh sách cũ.
        public static void LamMoiAgy(string agy)
        {
            if (agy == null) return;
            ThreadPool.QueueUserWorkItem(delegate
            {
                try
                {
                    ProcessStartInfo si = new ProcessStartInfo(agy, "models");
                    si.UseShellExecute = false;
                    si.CreateNoWindow = true;
                    si.RedirectStandardOutput = true;
                    si.StandardOutputEncoding = new UTF8Encoding(false);
                    using (Process p = Process.Start(si))
                    {
                        string ra = p.StandardOutput.ReadToEnd();
                        p.WaitForExit(30000);
                        List<string> ds = ra.Split('\n').Select(d => d.Trim()).Where(d => Regex.IsMatch(d, "^[a-z0-9.\\-]+\t.+$"))
                            .Select(d => d.Replace('\t', '=').Replace("|", "/")).ToList();
                        if (ds.Count > 0) GiaoDien.LuuCaiDat("agy_models", string.Join("|", ds));
                    }
                }
                catch (Exception) { } // agy lỗi / chưa đăng nhập: giữ danh sách cũ
            });
        }
    }
}
