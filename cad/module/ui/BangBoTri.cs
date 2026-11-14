// Phân hệ "Bố trí BC" của palette PCCC (lệnh PCCC_BOTRI), ý lấy từ addin mẫu 14/11: chọn thiết bị kèm trị số tiêu chuẩn (data/khoang_cach_rai.json),
// chiều cao trần, thanh "mức tối ưu k" 50–100 % hiện ngay bước, cách tường, R dùng thật, trần có dầm; BỐ TRÍ / Xem trước = gửi PCCC_RAI đã điền sẵn câu trả lời
// (mã, HeSo k, Dam, XemTruoc, trần, theo hàng): người dùng chỉ còn quét chọn mặt bằng. Kiểm cự ly (nút ấn, đèn ≤ 45 m) = PCCC_VUNGBV với R = d / 2.
// Chỉ giao diện: mọi xử lý bản vẽ ở Rai.cs, Lenh.cs.
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
        class ThietBi { public string Ma, Ten, CanCu, CanCuDam; public double S, Tuong, TuongMin, R, HMax, HeSoDam; public bool Tia; }
        class CuLy { public string Ma, Ten, CanCu; public double D; }

        readonly FlowLayoutPanel f = new FlowLayoutPanel();
        readonly List<ThietBi> ds = new List<ThietBi>();
        readonly List<CuLy> cuLy = new List<CuLy>();
        readonly List<RadioButton> chon = new List<RadioButton>();
        readonly TextBox tran = new TextBox();
        readonly Label canhBao = new Label(), mucK = new Label(), uoc = new Label(), ketQua = new Label(), ghiDam = new Label();
        readonly TrackBar k = new TrackBar();
        readonly OTong oBuoc = new OTong("Bước ≤"), oTuong = new OTong("Cách tường ≤"), oR = new OTong("R ≤");
        readonly CheckBox theoHang = new CheckBox(), coDam = new CheckBox();
        readonly ComboBox preset = new ComboBox(), congNang = new ComboBox();
        readonly Label goiYCn = new Label();
        readonly List<string[]> bangCn = new List<string[]>(); // { khu vực, loại, căn cứ }
        NutBo phuR;

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

            // Preset (spec addin mẫu): bộ lựa chọn có tên, lưu một khoá "bt_presets" của cai_dat.json dạng "Tên=mã;trần;k;dầm;hàng|...".
            FlowLayoutPanel hp = HangNut();
            Label lp = new Label();
            lp.Text = "Preset";
            lp.Font = GiaoDien.Dam;
            lp.ForeColor = GiaoDien.ChuDam;
            lp.AutoSize = true;
            lp.Margin = new Padding(0, 7, 6, 0);
            hp.Controls.Add(lp);
            preset.DropDownStyle = ComboBoxStyle.DropDown;
            preset.Width = 130;
            preset.Margin = new Padding(0, 3, 4, 0);
            preset.AccessibleName = "Tên preset";
            preset.Items.AddRange(DocPreset().Keys.ToArray());
            hp.Controls.Add(preset);
            NutNho(hp, "Lưu", delegate { LuuPreset(preset.Text); }).Width = 46;
            NutNho(hp, "Nạp", delegate { NapPreset(preset.Text); }).Width = 46;
            f.Controls.Add(hp);

            // Bước "Tiêu chuẩn & công năng" (spec addin mẫu): chọn công năng → gợi ý loại đầu báo theo bảng "Loại theo khu vực" của
            // knowledge/chon-loai-dau-bao.md (nguồn duy nhất, build.bat chép cạnh DLL; không chép bảng vào mã).
            Muc("0", "CÔNG NĂNG", "gợi ý loại đầu báo");
            congNang.DropDownStyle = ComboBoxStyle.DropDownList;
            congNang.Width = 290;
            congNang.AccessibleName = "Công năng gian phòng";
            DocCongNang();
            foreach (string[] c in bangCn) congNang.Items.Add(c[0]);
            congNang.SelectedIndexChanged += delegate { GiaoDien.An(ChonCongNang); };
            f.Controls.Add(congNang);
            goiYCn.Font = GiaoDien.Nho;
            goiYCn.ForeColor = GiaoDien.Cam;
            goiYCn.AutoSize = true;
            goiYCn.MaximumSize = new Size(290, 0);
            goiYCn.Text = bangCn.Count == 0 ? "Thiếu chon-loai-dau-bao.md cạnh DLL: chạy lại cad\\build.bat." : "";
            f.Controls.Add(goiYCn);

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
                f.Controls.Add(Chu(t.Ma + " · " + (t.Tia ? "giữa các tia" : "S") + " ≤ " + So(t.S) + " m · tường " + (t.TuongMin > 0 ? So(t.TuongMin) + "–" : "≤ ") + So(t.Tuong) + " m"
                    + (t.R > 0 ? " · R ≤ " + So(t.R) + " m" : "") + (t.HMax > 0 ? " · trần ≤ " + So(t.HMax) + " m" : "") + "\n" + t.CanCu, 18));
            }
            if (ds.Count == 0) f.Controls.Add(Chu("Thiếu khoang_cach_rai.json cạnh TKPCCC.Cad.dll: chạy lại cad\\build.bat.", 0));
            else if (!chon.Any(r => r.Checked)) chon[0].Checked = true;
            f.Controls.Add(Chu("Nút ấn, chuông, đèn: đặt từng cái bằng nút \"Đặt thiết bị khác\", rồi kiểm cự ly ở mục 5.", 0));

            Muc("2", "CHIỀU CAO TRẦN (m)", "để trống = không xét");
            tran.Width = 120;
            tran.BackColor = GiaoDien.The;
            tran.ForeColor = GiaoDien.ChuDam;
            tran.BorderStyle = BorderStyle.FixedSingle;
            tran.AccessibleName = "Chiều cao trần (m)";
            tran.Text = GiaoDien.DocCaiDat("bt_h", "");
            tran.TextChanged += delegate { TinhLai(); };
            f.Controls.Add(tran);
            canhBao.ForeColor = GiaoDien.Do;
            canhBao.Font = GiaoDien.Nho;
            canhBao.AutoSize = true;
            canhBao.MaximumSize = new Size(290, 0);
            f.Controls.Add(canhBao);

            Muc("3", "MỨC TỐI ƯU", "k nhân bước, cách tường");
            k.Minimum = 50; k.Maximum = 100; k.SmallChange = 5; k.LargeChange = 10; k.TickFrequency = 10;
            int kCu;
            k.Value = int.TryParse(GiaoDien.DocCaiDat("bt_k", "100"), out kCu) && kCu >= 50 && kCu <= 100 ? kCu : 100;
            k.BackColor = GiaoDien.Nen;
            k.AccessibleName = "Mức tối ưu k";
            k.Width = 300;
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
            foreach (OTong t in new[] { oBuoc, oTuong, oR }) { t.Size = new Size(92, 80); t.Margin = new Padding(0, 0, 6, 0); o.Controls.Add(t); }
            f.Controls.Add(o);
            uoc.Font = GiaoDien.Nho;
            uoc.ForeColor = GiaoDien.ChuMo;
            uoc.AutoSize = true;
            uoc.MaximumSize = new Size(290, 0);
            uoc.Margin = new Padding(0, 4, 0, 0);
            f.Controls.Add(uoc);

            Muc("4", "TRẦN, CÁCH CHIA", "");
            coDam.Text = "Trần có dầm, xà, ống sâu > 0,3 m";
            coDam.ForeColor = GiaoDien.ChuDam;
            coDam.AutoSize = true;
            coDam.Checked = GiaoDien.DocCaiDat("bt_dam", "Khong") == "Co";
            coDam.CheckedChanged += delegate { TinhLai(); };
            f.Controls.Add(coDam);
            ghiDam.Font = GiaoDien.Nho;
            ghiDam.ForeColor = GiaoDien.Cam;
            ghiDam.AutoSize = true;
            ghiDam.MaximumSize = new Size(272, 0);
            ghiDam.Margin = new Padding(18, 0, 0, 4);
            f.Controls.Add(ghiDam);
            theoHang.Text = "Phòng không chữ nhật: chia theo từng hàng";
            theoHang.ForeColor = GiaoDien.ChuDam;
            theoHang.AutoSize = true;
            theoHang.Checked = GiaoDien.DocCaiDat("bt_hang", "Khong") == "Co";
            f.Controls.Add(theoHang);

            NutBo boTri = new NutBo("▶  BỐ TRÍ", GiaoDien.XanhDam);
            boTri.Size = new Size(300, 38);
            boTri.Margin = new Padding(0, 12, 0, 4);
            boTri.Click += delegate { GiaoDien.An(delegate { BoTri(false, null); }); };
            f.Controls.Add(boTri);
            NutBo xem = new NutBo("Xem trước số lượng (không vẽ)", GiaoDien.Xanh);
            xem.Size = new Size(300, 30);
            xem.Margin = new Padding(0, 0, 0, 4);
            xem.Click += delegate { GiaoDien.An(delegate { BoTri(true, null); }); };
            f.Controls.Add(xem);
            // Nút BỐ TRÍ, Xem trước, thanh k theo bề rộng palette (kéo hẹp tới MinimumSize vẫn không tràn ngang).
            f.Resize += delegate { int w = Math.Max(200, f.ClientSize.Width - f.Padding.Horizontal - 4); boTri.Width = w; xem.Width = w; k.Width = w; };
            f.Controls.Add(Chu("Gửi PCCC_RAI đã điền sẵn: còn quét chọn mặt bằng, bấm nét phân vùng mẫu (Enter = tự nhận), Enter = mọi phòng.", 0));
            ketQua.Font = GiaoDien.Dam;
            ketQua.AutoSize = true;
            ketQua.MaximumSize = new Size(290, 0);
            ketQua.Margin = new Padding(0, 4, 0, 0);
            f.Controls.Add(ketQua);
            FlowLayoutPanel nut = HangNut();
            NutNho(nut, "↶ Hoàn tác lượt", delegate { GiaoDien.GuiLenh("_.U\n", null, null); });
            phuR = NutNho(nut, "Kiểm tra phủ R", delegate { ThietBi t = Dang(); if (t != null && t.R > 0) GiaoDien.GuiLenh("PCCC_VUNGBV\n" + t.Ma + "\n" + Ct(t.R) + "\n", null, null); });
            NutNho(nut, "⚡ Bố trí nhanh", delegate { GiaoDien.An(delegate { BoTri(false, "RECTANG"); }); });
            NutNho(nut, "⬡ Đa giác đều", delegate { GiaoDien.An(delegate { BoTri(false, "POLYGON"); }); });
            NutNho(nut, "◯ Tròn", delegate { GiaoDien.An(delegate { BoTri(false, "CIRCLE"); }); });
            NutNho(nut, "Vẽ vùng tay", delegate { GiaoDien.GuiLenh("_.-LAYER\n_M\nPCCC_VUNG\n\n_.PLINE\n", null, null); });
            NutNho(nut, "Vùng loại trừ", delegate { GiaoDien.GuiLenh("_.-LAYER\n_M\nPCCC_LOAITRU\n\n_.PLINE\n", null, null); });
            f.Controls.Add(nut);
            f.Controls.Add(Chu("Bố trí nhanh: bấm 2 góc khung chữ nhật, rải ngay trong khung. Vẽ vùng tay, vùng loại trừ (lỗ thang, giếng trời): polyline KÍN (gõ C để khép), "
                + "rồi BỐ TRÍ quét chọn cả hai: không rải trong vùng loại trừ.", 0));

            Muc("5", "KIỂM TRA, ĐI DÂY, THỐNG KÊ", "");
            FlowLayoutPanel kiem = HangNut();
            NutNho(kiem, "✓ Kiểm vùng mù", delegate
            {
                GiaoDien.GuiLenh("PCCC_KT\n", "PCCC_KT", delegate
                {
                    ketQua.Text = Lenh.KetQuaKtCuoi; // chân trang: độ phủ, cảnh báo (spec addin mẫu)
                    ketQua.ForeColor = Lenh.KetQuaKtCuoi.Contains("phủ kín") ? GiaoDien.XanhLa : GiaoDien.Do;
                });
            });
            foreach (CuLy c in cuLy)
            {
                CuLy cc = c;
                NutBo n = NutNho(kiem, "Cự ly " + c.Ma + " ≤ " + So(c.D) + " m", delegate { GiaoDien.GuiLenh("PCCC_VUNGBV\n" + cc.Ma + "\n" + Ct(cc.D / 2) + "\n", null, null); });
                n.Width = 144;
                n.AccessibleDescription = c.Ten + " — " + c.CanCu;
            }
            NutNho(kiem, "Vẽ dây loop", delegate { GiaoDien.GuiLenh("PCCC_DAY\nDAY_BC\n", null, null); });
            NutNho(kiem, "Dim tự động", delegate { GiaoDien.GuiLenh("PCCC_ADIM\n", null, null); });
            NutNho(kiem, "Đặt thiết bị khác", delegate { GiaoDien.GuiLenh("PCCC_DAT\n", null, null); });
            NutNho(kiem, "Bóc khối lượng", delegate { GiaoDien.GuiLenh("PCCC_BOC\n", null, null); });
            NutNho(kiem, "Chèn bảng TK", delegate { GiaoDien.GuiLenh("PCCC_BANG\n", null, null); });
            f.Controls.Add(kiem);
            if (cuLy.Count > 0)
                f.Controls.Add(Chu("Cự ly: vẽ vòng R = d / 2 quanh từng thiết bị, báo thiết bị đứng lẻ (cách thiết bị cùng loại gần nhất > d, đo thẳng; quãng đường đi thật dài hơn). "
                    + string.Join("; ", cuLy.Select(c => c.Ten + " " + c.CanCu)), 0));
            TinhLai();
        }

        void DocTieuChuan()
        {
            string file = Path.Combine(Path.GetDirectoryName(typeof(Lenh).Assembly.Location), "khoang_cach_rai.json");
            if (!File.Exists(file)) return;
            Dictionary<string, object> d = new JavaScriptSerializer().DeserializeObject(File.ReadAllText(file, Encoding.UTF8)) as Dictionary<string, object>;
            if (d == null) return;
            foreach (Dictionary<string, object> x in DsCua(d, "thiet_bi"))
            {
                IList ma = Lay(x, "ma") as IList;
                if (ma == null || ma.Count == 0) continue;
                ThietBi t = new ThietBi();
                t.Ma = Convert.ToString(ma[0]);
                t.Ten = Convert.ToString(Lay(x, "ten") ?? t.Ma);
                t.CanCu = Convert.ToString(Lay(x, "can_cu") ?? "");
                t.S = SoCua(x, "s"); t.Tuong = SoCua(x, "tuong"); t.TuongMin = SoCua(x, "tuong_min"); t.R = SoCua(x, "r"); t.HMax = SoCua(x, "h_max");
                t.Tia = Convert.ToString(Lay(x, "kieu")) == "tia";
                Dictionary<string, object> dam = Lay(x, "dam") as Dictionary<string, object>;
                if (dam != null) { t.HeSoDam = SoCua(dam, "he_so"); t.CanCuDam = Convert.ToString(Lay(dam, "can_cu") ?? ""); }
                if (t.S > 0) ds.Add(t);
            }
            foreach (Dictionary<string, object> x in DsCua(d, "cu_ly"))
            {
                IList ma = Lay(x, "ma") as IList;
                if (ma == null || ma.Count == 0 || SoCua(x, "d") <= 0) continue;
                CuLy c = new CuLy();
                c.Ma = Convert.ToString(ma[0]);
                c.Ten = Convert.ToString(Lay(x, "ten") ?? c.Ma);
                c.CanCu = Convert.ToString(Lay(x, "can_cu") ?? "");
                c.D = SoCua(x, "d");
                cuLy.Add(c);
            }
        }

        static IEnumerable<Dictionary<string, object>> DsCua(Dictionary<string, object> d, string k)
        {
            IList l = Lay(d, k) as IList;
            return l == null ? Enumerable.Empty<Dictionary<string, object>>() : l.OfType<Dictionary<string, object>>();
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
            bool dam = coDam.Checked && !t.Tia;
            double s = t.S * kk * (dam && t.HeSoDam > 0 ? t.HeSoDam : 1);
            coDam.Enabled = !t.Tia;
            ghiDam.Text = !coDam.Checked ? "" : t.Tia ? "Tia chiếu: không áp dụng." : t.HeSoDam > 0 ? "Bước giảm còn " + So(t.HeSoDam * 100) + " % theo " + t.CanCuDam + "."
                : "Đầu báo khói: không giảm bước; xét từng ô giữa dầm theo TCVN 7568-14:2025 5.9.1.1.6 (ô S ≥ 4 m² khi 2 ≤ h ≤ 4 m, S ≥ 9 m² khi h ≥ 4 m: mỗi ô ít nhất một đầu báo). "
                + "Khi BỐ TRÍ, bấm nét dầm làm nét phân vùng mẫu để rải từng ô.";
            oBuoc.Nhan = t.Tia ? "Giữa tia ≤" : "Bước ≤";
            oBuoc.Dat(So(s), "m");
            oTuong.Dat(So(t.Tuong * kk), "m");
            oR.Dat(t.R > 0 ? So(t.R * kk) : "–", t.R > 0 ? "m" : "");
            phuR.Enabled = t.R > 0;
            // Lưới vuông bước s: mỗi thiết bị phủ s² m²; tia: mỗi tia phủ dải rộng s, tính theo m dài tia.
            uoc.Text = t.Tia ? "≈ 1 tia cho mỗi dải rộng " + So(s) + " m (tia song song cạnh dài phòng)"
                : "≈ " + So(Math.Round(100 / (s * s), 1)) + " thiết bị / 100 m² (lưới vuông, chưa tính chia lẻ theo phòng)";
        }

        /// Đọc bảng "## Loại theo khu vực" của chon-loai-dau-bao.md: mỗi dòng "| khu vực | loại | căn cứ |", bỏ dấu ** của markdown.
        void DocCongNang()
        {
            string file = Path.Combine(Path.GetDirectoryName(typeof(Lenh).Assembly.Location), "chon-loai-dau-bao.md");
            if (!File.Exists(file)) return;
            bool trong = false;
            foreach (string dong in File.ReadAllLines(file, Encoding.UTF8))
            {
                if (dong.StartsWith("## ")) { trong = dong.Contains("Loại theo khu vực"); continue; }
                if (!trong || !dong.StartsWith("|") || dong.StartsWith("|---") || dong.Contains("| Loại |")) continue;
                string[] o = dong.Trim('|').Split('|').Select(x => x.Replace("**", "").Trim()).ToArray();
                if (o.Length >= 3) bangCn.Add(new[] { o[0], o[1], o[2] });
            }
        }

        /// Chọn công năng: hiện loại + căn cứ; chỉ "Nhiệt" (không có khói) thì chọn đầu nhiệt, có "Khói" thì đầu khói;
        /// "Nhiệt hoặc khói" giữ lựa chọn đang có (người thiết kế quyết), đang chọn tia chiếu thì về đầu khói.
        void ChonCongNang()
        {
            if (congNang.SelectedIndex < 0) return;
            string[] c = bangCn[congNang.SelectedIndex];
            goiYCn.Text = "Loại: " + c[1] + "\nCăn cứ: " + c[2];
            string l = c[1].Split('(', ';')[0].ToLowerInvariant(); // chỉ phần loại, bỏ ghi chú trong ngoặc (vd karaoke: "Nhiệt (… khói thuốc gây báo giả)")
            bool khoi = l.Contains("khói"), nhiet = l.Contains("nhiệt");
            string ma = nhiet && !khoi ? "BC_NHIET" : khoi && !nhiet ? "BC_KHOI" : null;
            ThietBi t = Dang();
            if (ma == null && t != null && t.Tia) ma = "BC_KHOI";
            for (int i = 0; i < ds.Count && ma != null; i++) if (ds[i].Ma == ma) chon[i].Checked = true;
        }

        Dictionary<string, string> DocPreset()
        {
            Dictionary<string, string> d = new Dictionary<string, string>();
            foreach (string muc in GiaoDien.DocCaiDat("bt_presets", "").Split('|'))
            {
                int i = muc.IndexOf('=');
                if (i > 0) d[muc.Substring(0, i)] = muc.Substring(i + 1);
            }
            return d;
        }

        void LuuPreset(string ten)
        {
            ThietBi t = Dang();
            ten = new string((ten ?? "").Where(c => c != '|' && c != '=' && c != ';').ToArray()).Trim();
            if (ten == "" || t == null) throw new System.Exception("Gõ tên preset (vd Văn phòng - 100%) rồi bấm Lưu.");
            Dictionary<string, string> d = DocPreset();
            d[ten] = t.Ma + ";" + tran.Text.Trim().Replace(";", "") + ";" + k.Value + ";" + (coDam.Checked ? "Co" : "Khong") + ";" + (theoHang.Checked ? "Co" : "Khong");
            GiaoDien.LuuCaiDat("bt_presets", string.Join("|", d.Select(x => x.Key + "=" + x.Value)));
            if (!preset.Items.Contains(ten)) preset.Items.Add(ten);
            ketQua.ForeColor = GiaoDien.XanhLa;
            ketQua.Text = "Đã lưu preset \"" + ten + "\".";
        }

        void NapPreset(string ten)
        {
            string v;
            if (!DocPreset().TryGetValue((ten ?? "").Trim(), out v)) throw new System.Exception("Chưa có preset \"" + ten + "\".");
            string[] p = v.Split(';');
            for (int i = 0; i < ds.Count; i++) if (ds[i].Ma == p[0]) chon[i].Checked = true;
            if (p.Length > 1) tran.Text = p[1];
            int kk;
            if (p.Length > 2 && int.TryParse(p[2], out kk) && kk >= k.Minimum && kk <= k.Maximum) k.Value = kk;
            if (p.Length > 3) coDam.Checked = p[3] == "Co";
            if (p.Length > 4) theoHang.Checked = p[4] == "Co";
            TinhLai();
        }

        /// nhanh = lệnh AutoCAD vẽ khung kín ("RECTANG": bấm 2 góc; "POLYGON": đa giác đều) trên PCCC_VUNG; xong thì PCCC_RAI chọn khung vừa vẽ (L) và rải ngay.
        /// shortcut: layer hiện hành để lại PCCC_VUNG sau khi vẽ khung; muốn trả layer cũ thì đọc CLAYER trước khi gửi.
        void BoTri(bool xem, string nhanh)
        {
            ThietBi t = Dang();
            if (t == null) throw new System.Exception("Chưa có thiết bị nào có trị số tiêu chuẩn (khoang_cach_rai.json).");
            double h = Tran();
            if (h < 0 || canhBao.Text != "") throw new System.Exception(canhBao.Text);
            GiaoDien.LuuCaiDat("bt_ma", t.Ma);
            GiaoDien.LuuCaiDat("bt_h", tran.Text.Trim());
            GiaoDien.LuuCaiDat("bt_k", k.Value.ToString(CultureInfo.InvariantCulture));
            GiaoDien.LuuCaiDat("bt_dam", coDam.Checked ? "Co" : "Khong");
            GiaoDien.LuuCaiDat("bt_hang", theoHang.Checked ? "Co" : "Khong");
            // Đúng thứ tự dấu nhắc của PCCC_RAI (Rai.cs RaiTay): mã → [HeSo → k] [Dam] [XemTruoc] → trần (trống = Enter) → theo hàng.
            string cau = "PCCC_RAI\n" + t.Ma + "\n" + (k.Value < 100 ? "HeSo\n" + k.Value + "\n" : "") + (coDam.Checked && !t.Tia ? "Dam\n" : "")
                + (xem ? "XemTruoc\n" : "") + (h > 0 ? Ct(h) : "") + "\n" + (theoHang.Checked ? "Co" : "Khong") + "\n";
            Action xong = delegate
            {
                ketQua.Text = Lenh.KetQuaRaiCuoi;
                ketQua.ForeColor = Lenh.KetQuaRaiCuoi.Contains("HO") ? GiaoDien.Do : GiaoDien.XanhLa;
            };
            // Chọn L (khung vừa vẽ), Enter kết thúc chọn, Enter = không nét mẫu: khung là polyline kín PCCC_VUNG nên rải luôn, không hỏi điểm phòng.
            if (nhanh != null) GiaoDien.GuiLenh("_.-LAYER\n_M\nPCCC_VUNG\n\n_." + nhanh + "\n", nhanh, delegate { GiaoDien.GuiLenh(cau + "_L\n\n\n", "PCCC_RAI", xong); });
            else GiaoDien.GuiLenh(cau, "PCCC_RAI", xong);
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
            l.MaximumSize = new Size(290 - lui, 0);
            l.Margin = new Padding(lui, 0, 0, 2);
            return l;
        }

        static FlowLayoutPanel HangNut()
        {
            FlowLayoutPanel p = new FlowLayoutPanel();
            p.AutoSize = true;
            p.MaximumSize = new Size(296, 0);
            p.BackColor = GiaoDien.Nen;
            p.Margin = new Padding(0, 6, 0, 0);
            return p;
        }

        static NutBo NutNho(FlowLayoutPanel p, string chu, Action viec)
        {
            NutBo n = new NutBo(chu, GiaoDien.Xanh);
            n.Font = GiaoDien.Nho;
            n.Size = new Size(92, 30);
            n.Margin = new Padding(0, 0, 4, 4);
            n.Click += delegate { GiaoDien.An(viec); };
            p.Controls.Add(n);
            return n;
        }
    }
}
