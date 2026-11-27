// Menu "/" như Claude Code (người dùng 27/11): gõ "/" đầu ô nhập hoặc bấm chip "/" -> bảng gợi ý nổi ngay trên khung soạn, lọc theo chữ gõ sau "/".
// Ba nhóm: Lệnh (chạy ngay trong bot, không gửi AI), Lệnh Claude Code (gửi nguyên văn cho claude -p), Skill (chèn "/tên " đầu ô nhập).
// Phím (qua ChuyenPhim -> nhap.KeyDown): lên/xuống chọn, Enter/Tab chọn, Esc đóng; bấm chuột chọn. Bảng không lấy focus của ô nhập.
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace TKPCCC
{
    /// Một dòng của bảng "/".
    class MucGach
    {
        public readonly string Ten, MoTa;
        public readonly int Loai;          // 0 lệnh bot, 1 lệnh Claude Code (gửi AI), 2 skill
        public readonly bool CanDoi, ChiClaude; // CanDoi: chọn xong chèn "/tên " để gõ đối số
        public string Nhom, Nguon;         // Nguon: chỗ chứa skill (cho /skills, /plugins)

        public MucGach(int loai, string ten, string moTa, bool canDoi, bool chiClaude)
        {
            Loai = loai; Ten = ten; MoTa = moTa; CanDoi = canDoi; ChiClaude = chiClaude;
            Nhom = loai == 0 ? "Lệnh" : loai == 1 ? "Claude Code" : "Skill";
        }
    }

    /// Bảng gợi ý vẽ tay; không Selectable nên bấm chuột không lấy focus của ô nhập.
    class BangGach : Control
    {
        public const int CAO = 26, TOI_DA = 9;
        public List<MucGach> Ds = new List<MucGach>();
        public int Chon, Dau; // dòng chọn, dòng đầu đang hiện
        public event Action<MucGach> ChonMuc;

        public BangGach()
        {
            SetStyle(ControlStyles.Selectable, false);
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            Font = GiaoDien.Chu;
            Visible = false;
        }

        public int SoDong { get { return Math.Max(1, (Height - 4) / CAO); } }
        public MucGach Dang { get { return Chon >= 0 && Chon < Ds.Count ? Ds[Chon] : null; } }

        public void Dat(List<MucGach> ds) { Ds = ds; Chon = 0; Dau = 0; Invalidate(); }

        public void Doi(int buoc)
        {
            if (Ds.Count == 0) return;
            Chon = (Chon + buoc + Ds.Count) % Ds.Count;
            if (Chon < Dau) Dau = Chon;
            if (Chon >= Dau + SoDong) Dau = Chon - SoDong + 1;
            Invalidate();
        }

        int DongTai(int y) { int i = Dau + (y - 2) / CAO; return y >= 2 && i < Ds.Count ? i : -1; }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            int i = DongTai(e.Y);
            if (i >= 0 && i != Chon) { Chon = i; Invalidate(); }
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);
            int i = e.Button == MouseButtons.Left ? DongTai(e.Y) : -1;
            if (i >= 0 && ChonMuc != null) ChonMuc(Ds[i]);
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            Dau = Math.Max(0, Math.Min(Ds.Count - SoDong, Dau - Math.Sign(e.Delta) * 3));
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.Clear(GiaoDien.The);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (Pen v = new Pen(Ve.Tron(GiaoDien.The, GiaoDien.ChuMo, 0.45f))) g.DrawRectangle(v, 0, 0, Width - 1, Height - 1);
            const TextFormatFlags TF = TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine;
            for (int i = Dau; i < Math.Min(Ds.Count, Dau + SoDong); i++)
            {
                MucGach m = Ds[i];
                Rectangle r = new Rectangle(3, 2 + (i - Dau) * CAO, Width - 6, CAO);
                if (i == Chon)
                    using (GraphicsPath gp = GiaoDien.BoGoc(r, 6))
                    using (SolidBrush b = new SolidBrush(Ve.Tron(GiaoDien.The, GiaoDien.Xanh, 0.25f))) g.FillPath(b, gp);
                string ten = "/" + m.Ten;
                int rNhom = TextRenderer.MeasureText(g, m.Nhom, GiaoDien.Nho).Width;
                int rTen = Math.Min(TextRenderer.MeasureText(g, ten, GiaoDien.Dam).Width, r.Width * 55 / 100);
                int x = r.X + 8;
                TextRenderer.DrawText(g, ten, GiaoDien.Dam, new Rectangle(x, r.Y, rTen, CAO), GiaoDien.ChuDam, TF);
                x += rTen + 8;
                TextRenderer.DrawText(g, m.MoTa, GiaoDien.Chu, new Rectangle(x, r.Y, Math.Max(0, r.Right - rNhom - 14 - x), CAO), GiaoDien.ChuMo, TF);
                TextRenderer.DrawText(g, m.Nhom, GiaoDien.Nho, new Rectangle(r.Right - rNhom - 6, r.Y, rNhom, CAO), GiaoDien.ChuMo, TF);
            }
            if (Ds.Count > SoDong) // thanh cuộn mảnh: còn dòng ẩn
            {
                int h = Math.Max(12, (Height - 4) * SoDong / Ds.Count), y = 2 + (Height - 4 - h) * Dau / Math.Max(1, Ds.Count - SoDong);
                using (SolidBrush b = new SolidBrush(Ve.Tron(GiaoDien.The, GiaoDien.ChuMo, 0.5f))) g.FillRectangle(b, Width - 4, y, 2, h);
            }
        }
    }

    partial class BotChat
    {
        static readonly MucGach[] LENH =
        {
            new MucGach(0, "help", "Danh sách lệnh và phím tắt", false, false),
            new MucGach(0, "clear", "Xoá khung chat, hội thoại mới", false, false),
            new MucGach(0, "new", "Hội thoại mới", false, false),
            new MucGach(0, "resume", "Lịch sử: nối lại phiên cũ (VS Code, Claude Code, Antigravity)", false, false),
            new MucGach(0, "model", "Chọn model, effort, tài khoản", false, false),
            new MucGach(0, "effort", "Mức suy nghĩ: low | medium | high | xhigh | max", true, false),
            new MucGach(0, "ultracode", "Bật / tắt Ultracode (Claude điều phối nhiều agent)", false, true),
            new MucGach(0, "usage", "Token, chi phí, giới hạn 5 giờ / tuần", false, false),
            new MucGach(0, "cost", "Chi phí hội thoại này", false, false),
            new MucGach(0, "status", "Tài khoản, model, effort, dự án, thư mục", false, false),
            new MucGach(0, "login", "Đăng nhập Claude Pro (mở trình duyệt)", false, true),
            new MucGach(0, "account", "Nguồn tài khoản: pro | fp | auto", true, true),
            new MucGach(0, "mcp", "Máy chủ MCP bot đang dùng", false, false),
            new MucGach(0, "skills", "Danh sách skill", false, false),
            new MucGach(0, "plugins", "Skill theo plugin / chỗ chứa skill", false, false),
            new MucGach(0, "stop", "Dừng lượt đang chạy", false, false),
            new MucGach(0, "copy", "Chép trả lời cuối của AI vào clipboard", false, false),
            new MucGach(1, "compact", "Tóm gọn hội thoại cho đỡ tốn ngữ cảnh", false, true),
            new MucGach(1, "init", "Tạo / cập nhật CLAUDE.md cho dự án", false, true),
            new MucGach(1, "review", "Review thay đổi đang có (hoặc PR)", false, true),
            new MucGach(1, "security-review", "Rà bảo mật các thay đổi đang có", false, true),
        };

        BangGach bang;
        List<MucGach> skillDem; // skill quét đĩa, lưu đệm 10 giây
        DateTime skillLuc;

        /// Chip "/": đưa "/" lên đầu ô nhập (chữ đang gõ lùi ra sau) rồi mở bảng.
        void MoGach()
        {
            if (!nhap.Text.StartsWith("/")) nhap.Text = "/" + (nhap.TextLength > 0 ? " " + nhap.Text.TrimStart() : "");
            nhap.Focus();
            nhap.SelectionStart = CuoiTu();
            nhap.SelectionLength = 0;
            LocGach();
        }

        /// Vị trí ngay sau chữ liền "/" đầu ô nhập.
        int CuoiTu()
        {
            string t = nhap.Text;
            int i = 1;
            while (i < t.Length && !char.IsWhiteSpace(t[i])) i++;
            return Math.Min(i, t.Length);
        }

        /// Mở / lọc / đóng bảng theo ô nhập: chỉ hiện khi chữ bắt đầu bằng "/" và con trỏ nằm trong chữ liền sau "/".
        /// (Gán Text bằng code thì con trỏ về 0 => tự đóng.)
        void LocGach()
        {
            string t = nhap.Text;
            int c = nhap.SelectionStart, cuoi = t.StartsWith("/") ? CuoiTu() : -1;
            if (cuoi < 0 || c < 1 || c > cuoi) { AnGach(); return; }
            if ((bang == null || !bang.Visible) && (skillDem == null || (DateTime.Now - skillLuc).TotalSeconds > 10))
            {
                skillDem = ThuSkill();
                skillLuc = DateTime.Now;
            }
            string loc = t.Substring(1, cuoi - 1).ToLowerInvariant();
            List<MucGach> tat = LENH.Where(m => ai == 0 || !m.ChiClaude).Concat(skillDem).ToList();
            List<MucGach> ds = tat.Where(m => m.Ten.ToLowerInvariant().StartsWith(loc))
                .Concat(tat.Where(m => !m.Ten.ToLowerInvariant().StartsWith(loc) && m.Ten.ToLowerInvariant().Contains(loc))).ToList();
            if (ds.Count == 0) { AnGach(); return; }
            if (bang == null)
            {
                bang = new BangGach();
                bang.ChonMuc += delegate(MucGach m) { GiaoDien.An(delegate { ChonGach(m); }); };
                Controls.Add(bang);
                Resize += delegate { if (bang.Visible) XepGach(); };
            }
            bang.Dat(ds);
            XepGach();
            bang.Visible = true;
            bang.BringToFront();
        }

        /// Đặt bảng sát trên khung soạn (đè dòng trạng thái), không che thanh đầu.
        void XepGach()
        {
            int cao = Math.Min(Math.Min(bang.Ds.Count, BangGach.TOI_DA) * BangGach.CAO + 4, Math.Max(BangGach.CAO + 4, khungSoan.Top - 40));
            bang.Bounds = new Rectangle(khungSoan.Left + 8, khungSoan.Top + 2 - cao, Math.Max(120, khungSoan.Width - 16), cao);
            bang.Doi(0); // giữ dòng chọn trong vùng hiện
        }

        void AnGach() { if (bang != null && bang.Visible) bang.Visible = false; }

        /// Phím ở ô nhập khi bảng đang mở. true = đã xử lý (Enter là chọn, không gửi).
        bool PhimGach(KeyEventArgs e)
        {
            if (bang == null || !bang.Visible) return false;
            if (e.KeyCode == Keys.Up) bang.Doi(-1);
            else if (e.KeyCode == Keys.Down) bang.Doi(1);
            else if ((e.KeyCode == Keys.Enter && !e.Shift) || e.KeyCode == Keys.Tab) { MucGach m = bang.Dang; if (m != null) GiaoDien.An(delegate { ChonGach(m); }); }
            else if (e.KeyCode == Keys.Escape) AnGach();
            else return false;
            e.SuppressKeyPress = true;
            e.Handled = true;
            return true;
        }

        void ChonGach(MucGach m)
        {
            string t = nhap.Text, sau = (t.StartsWith("/") ? t.Substring(CuoiTu()) : t).Trim();
            AnGach();
            if (m.Loai == 2 || m.CanDoi) { DatCau("/" + m.Ten + " " + sau); AnGach(); return; }
            if (m.Loai == 1) { nhap.Text = "/" + m.Ten + (sau != "" ? " " + sau : ""); GuiNhap(); return; }
            nhap.Text = sau; // chữ đang gõ dở sau "/" giữ lại
            nhap.SelectionStart = nhap.TextLength;
            ChayLenh("/" + m.Ten);
        }

        /// Lệnh bot "/tên đối số": chạy ngay, không gửi AI. false = không phải lệnh bot (skill, lệnh Claude Code, chữ thường) => gửi như thường.
        bool ChayLenh(string cau)
        {
            if (!cau.StartsWith("/")) return false;
            int k = cau.IndexOfAny(new[] { ' ', '\t', '\r', '\n' });
            string ten = (k < 0 ? cau.Substring(1) : cau.Substring(1, k - 1)).ToLowerInvariant(), doi = k < 0 ? "" : cau.Substring(k).Trim();
            MucGach m = LENH.FirstOrDefault(x => x.Loai == 0 && x.Ten == ten);
            if (m == null) return false;
            if (m.ChiClaude && ai != 0) { trangThai.Text = "/" + ten + ": chỉ dùng với Claude."; return true; }
            string dl = doi.ToLowerInvariant();
            switch (ten)
            {
                case "help": Ghi(null, TroGiup(), GiaoDien.ChuMo); break;
                case "clear":
                case "new":
                    if (p != null) trangThai.Text = "Đang chạy: đợi xong (hoặc /stop) rồi mới mở hội thoại mới.";
                    else HoiThoaiMoi();
                    break;
                case "resume": MoLichSu(); break;
                case "model": MenuModel(); break;
                case "effort": if (doi == "") MenuModel(); else DatEffort(dl); break;
                case "ultracode": DatUltracode(dl == "" ? !Ultracode : !(dl == "off" || dl == "tat" || dl == "tắt" || dl == "0")); break;
                case "usage":
                case "cost": Ghi(null, MoTaUsage(), GiaoDien.ChuMo); break;
                case "status": Ghi(null, MoTaTrangThai(), GiaoDien.ChuMo); break;
                case "login": DangNhapPro(); break;
                case "account":
                    if (doi == "") Ghi(null, TrangThaiTk(), GiaoDien.ChuMo);
                    else DatNguonTk(dl == "auto" ? "tu_dong" : dl);
                    break;
                case "mcp": Ghi(null, MoTaMcp(), GiaoDien.ChuMo); break;
                case "skills": Ghi(null, DsSkill(false), GiaoDien.ChuMo); break;
                case "plugins": Ghi(null, DsSkill(true), GiaoDien.ChuMo); break;
                case "stop": if (p != null) Dung(); else trangThai.Text = "Không có lượt nào đang chạy."; break;
                case "copy":
                    BongBong b = dsTin.Controls.OfType<BongBong>().LastOrDefault(x => x.Kieu == 1);
                    if (b == null) { trangThai.Text = "Chưa có trả lời nào của AI."; break; }
                    try { Clipboard.SetText(b.Text); trangThai.Text = "Đã chép trả lời cuối của AI."; }
                    catch (System.Exception ex) { trangThai.Text = "Không chép được: " + ex.Message; }
                    break;
            }
            return true;
        }

        string TroGiup()
        {
            StringBuilder s = new StringBuilder("Lệnh \"/\" (gõ / đầu ô nhập):\n");
            foreach (MucGach m in LENH.Where(x => ai == 0 || !x.ChiClaude))
                s.AppendLine("/" + m.Ten + (m.CanDoi ? " <…>" : "") + "  —  " + m.MoTa + (m.Loai == 1 ? "  (Claude Code)" : ""));
            s.AppendLine("/<skill> <yêu cầu>  —  gọi skill của dự án / chỗ chứa skill (/skills để xem)");
            s.AppendLine("\nPhím tắt:");
            s.AppendLine("Enter gửi (đang chạy: xếp hàng) · Shift+Enter xuống dòng · Ctrl+X gửi gấp vào lượt đang chạy");
            s.AppendLine("Ctrl+V dán ảnh · kéo thả ảnh / file vào ô nhập · Esc đóng bảng \"/\"");
            s.Append("Bấm một tin rồi Ctrl+A, Ctrl+C: chép cả tin đó (hoặc quét chọn một đoạn) · chuột phải tin: Sao chép");
            return s.ToString();
        }

        string MoTaTrangThai()
        {
            ModelAi m = ModelDang();
            string e = EffortDang();
            return "Máy AI: " + TEN_AI[ai] + " · model " + m.Ten + (e != null ? " · effort " + Hoa(e) : "") + (ai == 0 && Ultracode ? " · Ultracode" : "")
                + "\nDự án: " + (ThuMucLam() ?? "?")
                + "\nThư mục gắn kèm: " + (thuMuc.Count == 0 ? "(không)" : string.Join(", ", thuMuc))
                + "\nChỗ chứa skill: " + (skill.Count == 0 ? "(không)" : string.Join(", ", skill))
                + "\nPhiên: " + (phien ?? "(hội thoại mới)") + (linkTen != null ? " · nối " + linkTen : "")
                + (ai == 0 ? "\n" + TrangThaiTk() : "");
        }

        string MoTaMcp()
        {
            if (ai != 0) return "Antigravity: chưa có MCP cad (chỉ đọc + trả lời).";
            string cad = ThuMucCad();
            string exe = cad == null ? null : Path.Combine(cad, "18_MCP", "TKPCCC.Mcp.exe");
            return "Máy chủ MCP của Bot chat (--strict-mcp-config: chỉ máy này, không nạp MCP của người dùng):\n- cad: "
                + (exe == null ? "không thấy repo (chạy 07_CAD\\build.bat)" : exe + (File.Exists(exe) ? "" : "  (CHƯA build)"))
                + "\n  công cụ: cad_tt (trạng thái bản vẽ), cad_chay (chạy spec vẽ / đọc / sửa), cad_anh (ảnh bản vẽ)";
        }

        /// /skills: mọi skill theo nơi chứa; /plugins (chiPlugin): chỉ skill dạng plugin (chỗ chứa skill có tiền tố "plugin:").
        string DsSkill(bool chiPlugin)
        {
            skillDem = ThuSkill();
            skillLuc = DateTime.Now;
            StringBuilder s = new StringBuilder();
            foreach (IGrouping<string, MucGach> g in skillDem.Where(x => !chiPlugin || x.Nhom != "Skill").GroupBy(x => x.Nhom + "  ·  " + x.Nguon))
            {
                s.AppendLine(g.Key);
                foreach (MucGach m in g) s.AppendLine("  /" + m.Ten + (m.MoTa != "" ? "  —  " + m.MoTa : ""));
            }
            return s.Length > 0 ? s.ToString().TrimEnd()
                : chiPlugin ? "Chưa có plugin: + → Chỗ chứa skill (thư mục có skills\\ hoặc chứa thẳng các skill)." : "Chưa có skill: chọn dự án có .claude\\skills, hoặc + → Chỗ chứa skill.";
        }

        /// Skill của thư mục dự án, của <repo>\07_CAD (luôn gắn), thư mục gắn kèm, chỗ chứa skill (tên plugin:skill).
        List<MucGach> ThuSkill()
        {
            List<MucGach> ds = new List<MucGach>();
            Action<string, string, string> them = delegate(string ms, string tienTo, string nguon)
            {
                foreach (string ten in KhoPhien.TenSkill(ms, tienTo))
                {
                    if (ds.Any(x => x.Ten == ten)) continue;
                    MucGach m = new MucGach(2, ten, MoTaSkill(Path.Combine(ms, ten.Substring(ten.LastIndexOf(':') + 1), "SKILL.md")), false, false);
                    if (tienTo != null) m.Nhom = "Plugin " + tienTo;
                    m.Nguon = nguon;
                    ds.Add(m);
                }
            };
            foreach (string d in new[] { ThuMucLam(), ThuMucCad() }.Concat(thuMuc).Where(d => d != null && Directory.Exists(d)).Distinct(StringComparer.OrdinalIgnoreCase))
                them(Path.Combine(d, ".claude", "skills"), null, d);
            foreach (string d in skill.Where(Directory.Exists))
            {
                string tienTo, ms;
                try { KhoPhien.CoSkill(d, out tienTo, out ms); } catch (System.Exception) { continue; } // mklink hỏng: bỏ chỗ này
                them(ms, tienTo, d);
            }
            return ds.OrderBy(x => x.Ten).ToList();
        }

        /// Dòng description: trong frontmatter SKILL.md (cả dạng nhiều dòng "description: >"), cắt ~80 ký tự.
        static string MoTaSkill(string f)
        {
            try
            {
                string[] d = File.ReadLines(f).Take(40).ToArray();
                if (d.Length == 0 || d[0].Trim() != "---") return "";
                for (int i = 1; i < d.Length && d[i].Trim() != "---"; i++)
                {
                    if (!d[i].StartsWith("description:")) continue;
                    string v = d[i].Substring(12).Trim();
                    if (v == "" || v == ">" || v == "|" || v == ">-" || v == "|-")
                    {
                        v = "";
                        for (int j = i + 1; j < d.Length && (d[j].StartsWith(" ") || d[j].StartsWith("\t")); j++) v += " " + d[j].Trim();
                    }
                    v = v.Trim().Trim('"', '\'');
                    return v.Length > 80 ? v.Substring(0, 79) + "…" : v;
                }
            }
            catch (System.Exception) { }
            return "";
        }
    }
}
