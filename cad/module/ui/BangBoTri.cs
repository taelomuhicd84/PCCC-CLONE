// Phân hệ "Bố trí BC" của palette PCCC (lệnh PCCC_BOTRI), ý lấy từ addin mẫu 14/11: chọn thiết bị kèm trị số tiêu chuẩn (data/khoang_cach_rai.json),
// chiều cao trần, thanh "mức tối ưu k" 50–100 % hiện ngay bước, cách tường, R dùng thật, rồi BỐ TRÍ = gửi PCCC_RAI đã điền sẵn câu trả lời
// (mã, HeSo k, trần, theo hàng); người dùng chỉ còn quét chọn mặt bằng. Chỉ giao diện: mọi xử lý bản vẽ ở Rai.cs.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace TKPCCC
{
    class BangBoTri : UserControl
    {
        class ThietBi { public string Ma, Ten, CanCu; public double S, Tuong, TuongMin, R, HMax; }

        readonly FlowLayoutPanel f = new FlowLayoutPanel();
        readonly List<ThietBi> ds = new List<ThietBi>();
        readonly List<RadioButton> chon = new List<RadioButton>();
        readonly TextBox tran = new TextBox();
        readonly Label canhBao = new Label(), mucK = new Label(), uoc = new Label(), ketQua = new Label();
        readonly TrackBar k = new TrackBar();
        readonly OTong oBuoc = new OTong("Bước ≤"), oTuong = new OTong("Cách tường ≤"), oR = new OTong("R ≤");
        readonly CheckBox theoHang = new CheckBox();

        public BangBoTri()
        {
            BackColor = GiaoDien.Nen;
            f.Dock = DockStyle.Fill;
            f.FlowDirection = FlowDirection.TopDown;
            f.WrapContents = false;
            f.AutoScroll = true;
            f.BackColor = GiaoDien.Nen;
            f.Padding = new Padding(8, 6, 8, 8);
            Controls.Add(f);
            DocTieuChuan();
            f.Text = "BoTri: " + ds.Count; // thu_gui.ps1 đọc số thiết bị (chữ của cửa sổ)

            Muc("1", "THIẾT BỊ CẦN BỐ TRÍ", "theo tiêu chuẩn");
            string maCu = GiaoDien.DocCaiDat("bt_ma", ds.Count > 0 ? ds[0].Ma : "");
            foreach (ThietBi t in ds)
            {
                RadioButton r = new RadioButton();
                r.Text = t.Ten;
                r.Font = GiaoDien.Dam;
                r.ForeColor = GiaoDien.ChuDam;
                r.AutoSize = true;
                r.Margin = new Padding(0, 4, 0, 0);
                r.Checked = t.Ma == maCu;
                r.CheckedChanged += delegate { if (r.Checked) TinhLai(); };
                chon.Add(r);
                f.Controls.Add(r);
                f.Controls.Add(Chu(t.Ma + " · S ≤ " + So(t.S) + " m · tường " + So(t.TuongMin) + "–" + So(t.Tuong) + " m · R ≤ " + So(t.R) + " m"
                    + (t.HMax > 0 ? " · trần ≤ " + So(t.HMax) + " m" : "") + "\n" + t.CanCu, 18));
            }
            if (ds.Count == 0) f.Controls.Add(Chu("Thiếu khoang_cach_rai.json cạnh TKPCCC.Cad.dll: chạy lại cad\\build.bat.", 0));
            else if (!chon.Any(r => r.Checked)) chon[0].Checked = true;
            f.Controls.Add(Chu("Nút ấn, chuông, đèn: đặt từng cái bằng PCCC_DAT (nút dưới cùng).", 0));

            Muc("2", "CHIỀU CAO TRẦN (m)", "để trống = không xét");
            tran.Width = 120;
            tran.BackColor = GiaoDien.The;
            tran.ForeColor = GiaoDien.ChuDam;
            tran.BorderStyle = BorderStyle.FixedSingle;
            tran.Text = GiaoDien.DocCaiDat("bt_h", "");
            tran.TextChanged += delegate { TinhLai(); };
            f.Controls.Add(tran);
            canhBao.ForeColor = GiaoDien.Do;
            canhBao.Font = GiaoDien.Nho;
            canhBao.AutoSize = true;
            f.Controls.Add(canhBao);

            Muc("3", "MỨC TỐI ƯU", "k nhân bước, cách tường tối đa");
            k.Minimum = 50; k.Maximum = 100; k.SmallChange = 5; k.LargeChange = 10; k.TickFrequency = 10;
            int kCu;
            k.Value = int.TryParse(GiaoDien.DocCaiDat("bt_k", "100"), out kCu) && kCu >= 50 && kCu <= 100 ? kCu : 100;
            k.BackColor = GiaoDien.Nen;
            k.AccessibleName = "Mức tối ưu k";
            k.ValueChanged += delegate { TinhLai(); };
            mucK.Font = GiaoDien.Dam;
            mucK.ForeColor = GiaoDien.XanhLa;
            mucK.AutoSize = true;
            f.Controls.Add(mucK);
            f.Controls.Add(k);
            FlowLayoutPanel o = new FlowLayoutPanel();
            o.AutoSize = true;
            o.WrapContents = false;
            o.BackColor = GiaoDien.Nen;
            o.Margin = new Padding(0);
            foreach (OTong t in new[] { oBuoc, oTuong, oR }) { t.Size = new Size(96, 80); t.Margin = new Padding(0, 0, 6, 0); o.Controls.Add(t); }
            f.Controls.Add(o);
            uoc.Font = GiaoDien.Nho;
            uoc.ForeColor = GiaoDien.ChuMo;
            uoc.AutoSize = true;
            uoc.Margin = new Padding(0, 4, 0, 0);
            f.Controls.Add(uoc);

            Muc("4", "CÁCH CHIA", "");
            theoHang.Text = "Phòng không chữ nhật: chia theo từng hàng";
            theoHang.ForeColor = GiaoDien.ChuDam;
            theoHang.AutoSize = true;
            theoHang.Checked = GiaoDien.DocCaiDat("bt_hang", "Khong") == "Co";
            f.Controls.Add(theoHang);

            NutBo boTri = new NutBo("▶  BỐ TRÍ", GiaoDien.XanhDam);
            boTri.Size = new Size(300, 38);
            k.Width = 300;
            // Nút BỐ TRÍ, thanh k theo bề rộng palette (kéo hẹp tới MinimumSize vẫn không tràn ngang).
            f.Resize += delegate { int w = Math.Max(200, f.ClientSize.Width - f.Padding.Horizontal - 4); boTri.Width = w; k.Width = w; };
            boTri.Margin = new Padding(0, 12, 0, 4);
            boTri.Click += delegate { GiaoDien.An(BoTri); };
            f.Controls.Add(boTri);
            f.Controls.Add(Chu("Gửi PCCC_RAI đã điền sẵn: còn quét chọn mặt bằng, bấm nét phân vùng mẫu (Enter = tự nhận), Enter = mọi phòng.", 0));
            ketQua.Font = GiaoDien.Dam;
            ketQua.AutoSize = true;
            ketQua.MaximumSize = new Size(300, 0);
            ketQua.Margin = new Padding(0, 4, 0, 0);
            f.Controls.Add(ketQua);
            FlowLayoutPanel nut = new FlowLayoutPanel();
            nut.AutoSize = true;
            nut.MaximumSize = new Size(310, 0);
            nut.BackColor = GiaoDien.Nen;
            nut.Margin = new Padding(0, 6, 0, 0);
            NutNho(nut, "↶ Hoàn tác lượt", delegate { GiaoDien.GuiLenh("_.U\n", null, null); });
            NutNho(nut, "Kiểm tra phủ R", delegate { ThietBi t = Dang(); if (t != null) GiaoDien.GuiLenh("PCCC_VUNGBV\n" + t.Ma + "\n" + Ct(t.R) + "\n", null, null); });
            NutNho(nut, "Dim tự động", delegate { GiaoDien.GuiLenh("PCCC_ADIM\n", null, null); });
            NutNho(nut, "Đi dây, ống", delegate { GiaoDien.GuiLenh("PCCC_TUYEN\n", null, null); });
            NutNho(nut, "Đặt thiết bị khác", delegate { GiaoDien.GuiLenh("PCCC_DAT\n", null, null); });
            NutNho(nut, "Thống kê", delegate { GiaoDien.GuiLenh("PCCC_BOC\n", null, null); });
            f.Controls.Add(nut);
            TinhLai();
        }

        void DocTieuChuan()
        {
            string file = Path.Combine(Path.GetDirectoryName(typeof(Lenh).Assembly.Location), "khoang_cach_rai.json");
            if (!File.Exists(file)) return;
            Dictionary<string, object> d = new JavaScriptSerializer().DeserializeObject(File.ReadAllText(file, Encoding.UTF8)) as Dictionary<string, object>;
            object tb;
            if (d == null || !d.TryGetValue("thiet_bi", out tb) || !(tb is IList)) return;
            foreach (Dictionary<string, object> x in ((IList)tb).OfType<Dictionary<string, object>>())
            {
                IList ma = Lay(x, "ma") as IList;
                if (ma == null || ma.Count == 0) continue;
                ThietBi t = new ThietBi();
                t.Ma = Convert.ToString(ma[0]);
                t.Ten = Convert.ToString(Lay(x, "ten") ?? t.Ma);
                t.CanCu = Convert.ToString(Lay(x, "can_cu") ?? "");
                t.S = SoCua(x, "s"); t.Tuong = SoCua(x, "tuong"); t.TuongMin = SoCua(x, "tuong_min"); t.R = SoCua(x, "r"); t.HMax = SoCua(x, "h_max");
                if (t.S > 0) ds.Add(t);
            }
        }

        static object Lay(Dictionary<string, object> d, string k) { object v; return d.TryGetValue(k, out v) ? v : null; }
        static double SoCua(Dictionary<string, object> d, string k) { object v = Lay(d, k); return v == null ? 0 : Convert.ToDouble(v, CultureInfo.InvariantCulture); }
        static string So(double x) { return x.ToString("0.##", CultureInfo.InvariantCulture).Replace('.', ','); } // hiện kiểu Việt Nam
        static string Ct(double x) { return x.ToString("0.###", CultureInfo.InvariantCulture); } // gõ vào dòng lệnh

        ThietBi Dang()
        {
            for (int i = 0; i < chon.Count; i++) if (chon[i].Checked) return ds[i];
            return null;
        }

        /// Chiều cao trần người dùng gõ ("3,5" = 3.5); trống = 0 (không xét); gõ sai = -1.
        double Tran()
        {
            string s = tran.Text.Trim().Replace(',', '.');
            if (s == "") return 0;
            double h;
            return double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out h) && h > 0 ? h : -1;
        }

        void TinhLai()
        {
            ThietBi t = Dang();
            double kk = k.Value / 100.0, h = Tran();
            mucK.Text = "k = " + k.Value + " %" + (k.Value == 100 ? "  (đúng trị số tối đa, ít thiết bị nhất)" : "  (rải dày hơn mức tối đa)");
            canhBao.Text = h < 0 ? "Chiều cao trần phải là số dương." : t != null && t.HMax > 0 && h > t.HMax ? "Trần " + So(h) + " m vượt " + So(t.HMax) + " m của " + t.Ma + ": chọn loại khác." : "";
            if (t == null) return;
            oBuoc.Dat(So(t.S * kk), "m");
            oTuong.Dat(So(t.Tuong * kk), "m");
            oR.Dat(So(t.R * kk), "m");
            // Lưới vuông bước k·S: mỗi thiết bị phủ (k·S)² m².
            uoc.Text = "≈ " + So(Math.Round(100 / Math.Pow(t.S * kk, 2), 1)) + " thiết bị / 100 m² (lưới vuông, chưa tính chia lẻ theo phòng)";
        }

        void BoTri()
        {
            ThietBi t = Dang();
            if (t == null) throw new System.Exception("Chưa có thiết bị nào có trị số tiêu chuẩn (khoang_cach_rai.json).");
            double h = Tran();
            if (h < 0 || canhBao.Text != "") throw new System.Exception(canhBao.Text);
            GiaoDien.LuuCaiDat("bt_ma", t.Ma);
            GiaoDien.LuuCaiDat("bt_h", tran.Text.Trim());
            GiaoDien.LuuCaiDat("bt_k", k.Value.ToString(CultureInfo.InvariantCulture));
            GiaoDien.LuuCaiDat("bt_hang", theoHang.Checked ? "Co" : "Khong");
            // Đúng thứ tự dấu nhắc của PCCC_RAI (Rai.cs RaiTay): mã → [HeSo → k] → trần (trống = Enter) → theo hàng.
            GiaoDien.GuiLenh("PCCC_RAI\n" + t.Ma + "\n" + (k.Value < 100 ? "HeSo\n" + k.Value + "\n" : "") + (h > 0 ? Ct(h) : "") + "\n"
                + (theoHang.Checked ? "Co" : "Khong") + "\n", "PCCC_RAI", delegate
                {
                    ketQua.Text = Lenh.KetQuaRaiCuoi;
                    ketQua.ForeColor = Lenh.KetQuaRaiCuoi.Contains("HO") ? GiaoDien.Do : GiaoDien.XanhLa;
                });
        }

        void Muc(string so, string ten, string phu)
        {
            Label l = new Label();
            l.Text = so + "   " + ten + (phu != "" ? "   · " + phu : "");
            l.Font = GiaoDien.Dam;
            l.ForeColor = GiaoDien.Xanh;
            l.AutoSize = true;
            l.Margin = new Padding(0, 14, 0, 4);
            f.Controls.Add(l);
        }

        static Label Chu(string t, int lui)
        {
            Label l = new Label();
            l.Text = t;
            l.Font = GiaoDien.Nho;
            l.ForeColor = GiaoDien.ChuMo;
            l.AutoSize = true;
            l.MaximumSize = new Size(300, 0);
            l.Margin = new Padding(lui, 0, 0, 2);
            return l;
        }

        static void NutNho(FlowLayoutPanel p, string chu, Action viec)
        {
            NutBo n = new NutBo(chu, GiaoDien.Xanh);
            n.Font = GiaoDien.Nho;
            n.Size = new Size(98, 30);
            n.Margin = new Padding(0, 0, 4, 4);
            n.Click += delegate { GiaoDien.An(viec); };
            p.Controls.Add(n);
        }
    }
}
