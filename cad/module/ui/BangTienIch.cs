// Phân hệ "Tiện ích" của palette PCCC (ý lấy từ addin mẫu + spec người dùng 14/11, cad/tien_do/viec_video_addin.md): lưới thẻ 2 cột chia 6 nhóm,
// hàng nút lọc nhóm, ô tìm của palette lọc theo tên, mô tả, mã tắt (SZ, CKC...) hoặc lệnh. Mỗi thẻ gửi một lệnh PCCC_* có sẵn hoặc lệnh AutoCAD có sẵn
// (không viết trùng). Mã tắt trên huy hiệu là mã của spec để người dùng nhận ra, KHÔNG tự đăng ký thành lệnh (tránh đè lệnh AutoCAD; phím tắt riêng đặt ở PCCC_HELP).
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace TKPCCC
{
    class BangTienIch : UserControl
    {
        // { nhóm, mã tắt, tên, mô tả, lệnh gửi } — lệnh bắt đầu bằng "#" là việc của giao diện (mở trang, mở thư mục).
        static readonly string[][] THE = {
            new[] { "1", "SZ", "Chọn khu vực", "Vẽ polyline kín vùng rải (PCCC_VUNG)", "_.-LAYER\n_M\nPCCC_VUNG\n\n_.PLINE\n" },
            new[] { "1", "SP", "Nhận diện mặt bằng", "Bấm điểm trong phòng: dựng biên kín", "_.BOUNDARY\n" },
            new[] { "1", "RAI", "Rải theo phòng", "Quét mặt bằng, tự nhận phòng rồi rải", "PCCC_RAI\n" },
            new[] { "2", "SB", "Đổi Block", "Thay block, giữ điểm, góc, tỉ lệ", "PCCC_DOIBLOCK\n" },
            new[] { "2", "RN", "Đổi tên Block", "Đổi tên định nghĩa block", "_.RENAME\n" },
            new[] { "2", "SL", "Đổi Layer", "Lấy layer theo đối tượng mẫu", "_.LAYMCH\n" },
            new[] { "2", "AN", "Đánh số thiết bị", "Tiền tố + số tăng dần theo hàng/cột", "PCCC_DANHSO\n" },
            new[] { "2", "AP", "Sao chép / Dàn", "Dàn block đều theo tuyến (MEASURE)", "_.MEASURE\n" },
            new[] { "3", "MR", "Di chuyển / Xoay", "Xoay từng block quanh điểm chèn", "PCCC_XOAY\n" },
            new[] { "3", "AD", "Căn chỉnh Dim", "Dóng đường dim về một mốc", "PCCC_CANDIM\n" },
            new[] { "3", "CLT", "Căn chỉnh Text", "Căn lề, xếp đều chữ (TEXTALIGN)", "_.TEXTALIGN\n" },
            new[] { "3", "ADIM", "Dim đối tượng", "Chuỗi dim tự động cả lưới thiết bị", "PCCC_ADIM\n" },
            new[] { "3", "CDIM", "Dim tiếp tục", "Nối tiếp chuỗi dim vừa tạo", "PCCC_CDIM\n" },
            new[] { "3", "SCB", "Tỷ lệ", "Tỉ lệ từng block quanh điểm chèn", "PCCC_TYLE\n" },
            new[] { "3", "CLN", "Xóa / Làm sạch", "Xoá nét trùng (OVERKILL)", "_.OVERKILL\n" },
            new[] { "4", "LM", "Quản lý Layer", "Bảng layer", "_.LAYER\n" },
            new[] { "4", "PD", "Vẽ ống theo DN", "Chọn cỡ ống DN25–DN400, vẽ tuyến", "PCCC_ONG\n" },
            new[] { "4", "APL", "Áp dụng Layer", "Đưa thiết bị về layer PCCC_<hệ>", "PCCC_LOPCHUAN\n" },
            new[] { "4", "DN", "Cập nhật DN", "Đổi cỡ đoạn ống đã chọn", "PCCC_DOIDN\n" },
            new[] { "5", "CKC", "Kiểm tra trùng", "Thiết bị đặt trùng điểm", "PCCC_TRUNG\n" },
            new[] { "5", "CKL", "Kiểm tra Layer", "Thiết bị sai layer (khoanh đỏ)", "PCCC_LOPCHUAN\n" },
            new[] { "5", "CKO", "Kiểm tra vùng mù", "% mù từng phòng theo R, khoanh đỏ", "PCCC_KT\n" },
            new[] { "5", "R", "Vùng bảo vệ R", "Vòng R quanh thiết bị, báo đứng lẻ", "PCCC_VUNGBV\n" },
            new[] { "5", "REP", "Báo cáo nhanh", "Bóc khối lượng theo tầng, khu", "PCCC_BOC\n" },
            new[] { "6", "SET", "Cài đặt chung", "Giao diện, model bot chat, bản quyền", "#caidat" },
            new[] { "6", "DIR", "Thư mục thư viện", "Mở thư mục dữ liệu plugin (block, vật tư)", "#thumuc" },
            new[] { "6", "STD", "Cài đặt tiêu chuẩn", "Mở bảng trị số rải theo TCVN", "#tieuchuan" },
            new[] { "6", "BAK", "Sao lưu dữ liệu", "Mở chỗ lưu cài đặt cai_dat.json", "#saoluu" } };
        static readonly string[] NHOM = { "1. KHU VỰC THIẾT KẾ", "2. XỬ LÝ ĐỐI TƯỢNG", "3. HIỆU CHỈNH", "4. LAYER & ĐƯỜNG KÍNH ỐNG", "5. KIỂM TRA NHANH", "6. CÀI ĐẶT" };
        static readonly string[] NUT = { "Tất cả", "1. Khu vực", "2. Xử lý", "3. Hiệu chỉnh", "4. Layer & Ống", "5. Kiểm tra", "6. Cài đặt" };

        readonly FlowLayoutPanel luoi = new FlowLayoutPanel();
        readonly List<TheNho> the = new List<TheNho>();
        readonly List<Label> dau = new List<Label>();
        readonly List<Label> nut = new List<Label>();
        readonly ToolTip goiY = new ToolTip(); // thẻ hẹp cắt mô tả: rê chuột xem đủ
        readonly Action<string> moTrang;
        string nhomChon = "", chuTim = "";

        public BangTienIch(Action<string> moTrang)
        {
            this.moTrang = moTrang;
            BackColor = GiaoDien.Nen;
            luoi.Dock = DockStyle.Fill;
            luoi.AutoScroll = true;
            luoi.BackColor = GiaoDien.Nen;
            luoi.Padding = new Padding(6, 4, 6, 6);
            Controls.Add(luoi);

            FlowLayoutPanel loc = new FlowLayoutPanel();
            loc.Dock = DockStyle.Top;
            loc.AutoSize = true;
            loc.BackColor = GiaoDien.Nen;
            loc.Padding = new Padding(6, 2, 6, 2);
            for (int i = 0; i < NUT.Length; i++)
            {
                string ma = i == 0 ? "" : i.ToString();
                Label l = new Label();
                l.Text = NUT[i];
                l.Font = GiaoDien.Nho;
                l.UseMnemonic = false; // "Layer & Ống": không biến & thành phím tắt
                l.AutoSize = true;
                l.Padding = new Padding(6, 2, 6, 2);
                l.Margin = new Padding(0, 0, 4, 4);
                l.Cursor = Cursors.Hand;
                l.Tag = ma;
                l.Click += delegate { nhomChon = ma; Loc(); };
                nut.Add(l);
                loc.Controls.Add(l);
            }
            Controls.Add(loc);

            for (int g = 0; g < NHOM.Length; g++)
            {
                string so = (g + 1).ToString();
                Label d = new Label();
                d.Font = GiaoDien.Dam;
                d.UseMnemonic = false;
                d.ForeColor = GiaoDien.Xanh;
                d.AutoSize = false;
                d.Height = 24;
                d.TextAlign = ContentAlignment.BottomLeft;
                d.Margin = new Padding(0, 6, 0, 2);
                d.Tag = so;
                d.Text = NHOM[g] + "   · " + THE.Count(t => t[0] == so) + " tiện ích";
                dau.Add(d);
                luoi.Controls.Add(d);
                foreach (string[] t in THE.Where(x => x[0] == so))
                {
                    string lenh = t[4];
                    TheNho c = new TheNho(t[0], t[1], t[2], t[3], lenh);
                    c.Click += delegate { GiaoDien.An(delegate { Chay(lenh); }); };
                    goiY.SetToolTip(c, t[2] + " (" + t[1] + "): " + t[3] + (lenh.StartsWith("#") ? "" : "\nGửi: " + lenh.Replace("\n", " ").Trim()));
                    the.Add(c);
                    luoi.Controls.Add(c);
                }
            }
            luoi.Resize += delegate { XepCo(); };
            XepCo();
            Loc();
        }

        void XepCo()
        {
            int w = Math.Max(200, luoi.ClientSize.Width - luoi.Padding.Horizontal - 2);
            luoi.SuspendLayout();
            foreach (Label d in dau) d.Width = w;
            foreach (TheNho t in the) t.Width = (w - 6) / 2;
            luoi.ResumeLayout();
        }

        /// Ô tìm của palette gọi khi đang xem trang này.
        public void Tim(string chu)
        {
            chuTim = (chu ?? "").Trim().ToLowerInvariant();
            Loc();
        }

        void Loc()
        {
            luoi.SuspendLayout();
            int n = 0;
            foreach (TheNho t in the)
            {
                t.Visible = (nhomChon == "" || t.Nhom == nhomChon) && (chuTim == "" || t.ChuTim.Contains(chuTim) || Lenh.BoDau(t.ChuTim).Contains(Lenh.BoDau(chuTim)));
                if (t.Visible) n++;
            }
            foreach (Label d in dau) d.Visible = the.Any(t => t.Visible && t.Nhom == (string)d.Tag);
            foreach (Label l in nut)
            {
                bool chon = (string)l.Tag == nhomChon;
                l.BackColor = chon ? GiaoDien.Xanh : GiaoDien.The;
                l.ForeColor = chon ? Color.White : GiaoDien.ChuDam;
            }
            luoi.ResumeLayout();
            luoi.Text = "TienIch: " + n; // thu_gui.ps1 đọc số thẻ đang hiện
        }

        void Chay(string lenh)
        {
            string thuMuc = Path.GetDirectoryName(typeof(Lenh).Assembly.Location);
            if (lenh == "#caidat") moTrang(BangChinh.CAI_DAT);
            else if (lenh == "#thumuc") Process.Start("explorer.exe", "\"" + thuMuc + "\"");
            else if (lenh == "#tieuchuan") Process.Start("notepad.exe", "\"" + Path.Combine(thuMuc, "khoang_cach_rai.json") + "\"");
            else if (lenh == "#saoluu") Process.Start("explorer.exe", "/select,\"" + GiaoDien.FileCaiDat + "\"");
            else GiaoDien.GuiLenh(lenh, null, null);
        }
    }

    /// Thẻ nhỏ của lưới 2 cột: tên đậm, mô tả mờ một dòng, huy hiệu mã tắt góc phải trên (kèm phím tắt người dùng đặt nếu có).
    class TheNho : Control
    {
        public readonly string Nhom, Ma, Ten, MoTa, ChuTim;

        public TheNho(string nhom, string ma, string ten, string moTa, string lenh)
        {
            Nhom = nhom; Ma = ma; Ten = ten; MoTa = moTa;
            string l = lenh.StartsWith("#") ? "" : lenh.Split('\n')[0];
            string pt;
            if (PhimTat.TheoLenh.TryGetValue(l, out pt)) Ma = ma + " · " + pt;
            ChuTim = (ten + " " + moTa + " " + ma + " " + l + " " + (pt ?? "")).ToLowerInvariant();
            Height = 50;
            Margin = new Padding(0, 0, 6, 6);
            DoubleBuffered = true;
            Cursor = Cursors.Hand;
            AccessibleName = ten + " (" + ma + ")";
            AccessibleRole = AccessibleRole.PushButton;
            BackColor = GiaoDien.Nen;
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
            Size hs = TextRenderer.MeasureText(Ma, GiaoDien.Nho);
            Rectangle h = new Rectangle(Width - hs.Width - 10, 6, hs.Width + 4, hs.Height);
            using (SolidBrush b = new SolidBrush(GiaoDien.Nen)) g.FillRectangle(b, h);
            TextRenderer.DrawText(g, Ma, GiaoDien.Nho, h, GiaoDien.Xanh, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            TextFormatFlags dong = TextFormatFlags.Left | TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine;
            TextRenderer.DrawText(g, Ten, GiaoDien.Dam, new Rectangle(8, 6, h.Left - 10, 18), GiaoDien.ChuDam, dong);
            TextRenderer.DrawText(g, MoTa, GiaoDien.Nho, new Rectangle(8, 26, Width - 14, 16), GiaoDien.ChuMo, dong);
        }
    }
}
