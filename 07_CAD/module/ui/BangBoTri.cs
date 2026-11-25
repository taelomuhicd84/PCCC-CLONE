// Phân hệ "Bố trí BC" của palette PCCC (lệnh PCCC_BOTRI), ý lấy từ addin mẫu 14/11: chọn thiết bị kèm trị số tiêu chuẩn (03_DU_LIEU/khoang_cach_rai.json),
// chiều cao trần, thanh "mức tối ưu k" 50–100 % hiện ngay bước, cách tường, R dùng thật, trần có dầm; BỐ TRÍ / Xem trước = gửi PCCC_RAI đã điền sẵn câu trả lời
// (mã, HeSo k, Dam, XemTruoc, trần, theo hàng): người dùng chỉ còn quét chọn mặt bằng. Kiểm cự ly (nút ấn, đèn ≤ 45 m) = PCCC_VUNGBV với R = d / 2.
// Giao diện 16/11 (ý từ video plugin VPTTOOL): 4 tab viên thuốc Rải đầu báo · Khoang dầm · Kiểm tra · Đi dây, nút biểu tượng tròn, khung TRẠNG THÁI cuối tab.
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

        FlowLayoutPanel f; // tab đang dựng: Muc, Chu, Luoi thêm vào đây
        readonly FlowLayoutPanel[] tab = new FlowLayoutPanel[4];
        readonly KhungTrangThai[] tt = new KhungTrangThai[4];
        readonly ThanhTab thanh = new ThanhTab(new[] { "RẢI ĐẦU BÁO", "KHOANG DẦM", "KIỂM TRA", "ĐI DÂY" });
        readonly TextBox tranKd = new TextBox();
        readonly ComboBox loaiTran = new ComboBox(); // tab Khoang dầm: 3 trường hợp trần (người dùng 17/11), thứ tự = Rai.cs LOAI_TRAN
        int[] vungDem = { -1, -1 }; // { PCCC_VUNG, PCCC_LOAITRU } đếm lần gần nhất (DemLai)
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
        NutIcon phuR;

        public BangBoTri()
        {
            BackColor = GiaoDien.Nen;
            for (int i = 0; i < tab.Length; i++)
            {
                FlowLayoutPanel t = new FlowLayoutPanel();
                t.Dock = DockStyle.Fill;
                t.FlowDirection = FlowDirection.TopDown;
                t.WrapContents = false;
                t.AutoScroll = true;
                t.BackColor = GiaoDien.Nen;
                t.Padding = new Padding(8, 4, 8, 8);
                t.Visible = i == 0;
                t.Resize += delegate { XepLai(t); };
                tab[i] = t;
                tt[i] = new KhungTrangThai();
                Controls.Add(t);
            }
            thanh.DaChon += delegate(int i)
            {
                for (int j = 0; j < tab.Length; j++) tab[j].Visible = j == i;
                XepLai(tab[i]);
                DemLai();
            };
            Controls.Add(thanh); // thêm sau các tab Fill: Dock Top nằm trên
            VisibleChanged += delegate { if (Visible) DemLai(); };
            f = tab[0];
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
            // 01_TRI_THUC/chon-loai-dau-bao.md (nguồn duy nhất, build.bat chép cạnh DLL; không chép bảng vào mã).
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
            goiYCn.Text = bangCn.Count == 0 ? "Thiếu chon-loai-dau-bao.md cạnh DLL: chạy lại 07_CAD\\build.bat." : "";
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
            if (ds.Count == 0) f.Controls.Add(Chu("Thiếu khoang_cach_rai.json cạnh TKPCCC.Cad.dll: chạy lại 07_CAD\\build.bat.", 0));
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
            f.Controls.Add(Chu("Ống gió: polyline kín (hoặc vòng tròn) trên layer tên có ONG_GIO, ONG-GIO hoặc DUCT, quét chọn cùng mặt bằng: đầu báo rơi vào ống tự dịch ra ngoài biên ống.", 0));

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
            boTri.Tag = xem.Tag = k.Tag = "rong"; // XepLai giãn theo bề rộng tab
            f.Controls.Add(Chu("Gửi PCCC_RAI đã điền sẵn: còn quét chọn mặt bằng, bấm nét phân vùng mẫu (Enter = tự nhận), Enter = mọi phòng.", 0));
            ketQua.Font = GiaoDien.Dam;
            ketQua.AutoSize = true;
            ketQua.MaximumSize = new Size(290, 0);
            ketQua.Margin = new Padding(0, 4, 0, 0);
            f.Controls.Add(ketQua);
            f.Controls.Add(new TieuDeNhom("Vùng rải & làm lại", GiaoDien.MauLa));
            FlowLayoutPanel nut = Luoi();
            GiaoDien.NutLuoi(nut, "\u25AD", "Bố trí nhanh", GiaoDien.MauLa, delegate { BoTri(false, "RECTANG"); });
            GiaoDien.NutLuoi(nut, "\u2B21", "Đa giác đều", GiaoDien.MauDuong, delegate { BoTri(false, "POLYGON"); });
            GiaoDien.NutLuoi(nut, "\u25EF", "Tròn", GiaoDien.MauTim, delegate { BoTri(false, "CIRCLE"); });
            GiaoDien.NutLuoi(nut, "\u270F", "Vẽ vùng tay", GiaoDien.MauCam, delegate { GiaoDien.GuiLenh("_.-LAYER\n_M\nPCCC_VUNG\n\n_.PLINE\n", "PLINE", DemLai); });
            GiaoDien.NutLuoi(nut, "\u26D4", "Vùng loại trừ", GiaoDien.MauDo, delegate { GiaoDien.GuiLenh("_.-LAYER\n_M\nPCCC_LOAITRU\n\n_.PLINE\n", "PLINE", DemLai); });
            GiaoDien.NutLuoi(nut, "\u21B6", "Hoàn tác lượt", GiaoDien.MauXam, delegate { GiaoDien.GuiLenh("_.U\n", "U", DemLai); });
            f.Controls.Add(Chu("Bố trí nhanh: bấm 2 góc khung chữ nhật, rải ngay trong khung. Vẽ vùng tay, vùng loại trừ (lỗ thang, giếng trời): polyline KÍN (gõ C để khép), "
                + "rồi BỐ TRÍ quét chọn cả hai: không rải trong vùng loại trừ.", 0));
            f.Controls.Add(tt[0]);

            TaoKhoangDam();

            // ---------- Tab KIỂM TRA ----------
            f = tab[2];
            f.Controls.Add(new TieuDeNhom("Kiểm tra bố trí", GiaoDien.MauLuc));
            FlowLayoutPanel kiem = Luoi();
            GiaoDien.NutLuoi(kiem, "\u2714", "Kiểm vùng mù", GiaoDien.MauLuc, delegate { GiaoDien.GuiLenh("PCCC_KT\n", "PCCC_KT", DemLai); });
            phuR = GiaoDien.NutLuoi(kiem, "\u25CE", "Kiểm phủ R", GiaoDien.MauDuong, delegate { ThietBi t = Dang(); if (t != null && t.R > 0) GiaoDien.GuiLenh("PCCC_VUNGBV\n" + t.Ma + "\n" + Ct(t.R) + "\n", null, null); });
            foreach (CuLy c in cuLy)
            {
                CuLy cc = c;
                NutIcon n = GiaoDien.NutLuoi(kiem, "\U0001F4CF", "Cự ly " + c.Ma + " ≤ " + So(c.D) + " m", GiaoDien.MauCam, delegate { GiaoDien.GuiLenh("PCCC_VUNGBV\n" + cc.Ma + "\n" + Ct(cc.D / 2) + "\n", null, null); });
                n.AccessibleDescription = c.Ten + " — " + c.CanCu;
            }
            GiaoDien.NutLuoi(kiem, "\u29C9", "Kiểm trùng", GiaoDien.MauTim, delegate { GiaoDien.GuiLenh("PCCC_TRUNG\n", null, null); });
            GiaoDien.NutLuoi(kiem, "\u2630", "Kiểm layer", GiaoDien.MauXam, delegate { GiaoDien.GuiLenh("PCCC_LOPCHUAN\n", null, null); });
            if (cuLy.Count > 0)
                f.Controls.Add(Chu("Cự ly: vẽ vòng R = d / 2 quanh từng thiết bị, báo thiết bị đứng lẻ (cách thiết bị cùng loại gần nhất > d, đo thẳng; quãng đường đi thật dài hơn). "
                    + string.Join("; ", cuLy.Select(c => c.Ten + " " + c.CanCu)), 0));
            f.Controls.Add(new TieuDeNhom("Hoàn thiện & thống kê", GiaoDien.MauTim));
            FlowLayoutPanel ht = Luoi();
            GiaoDien.NutLuoi(ht, "\u2194", "Dim tự động", GiaoDien.MauDuong, delegate { GiaoDien.GuiLenh("PCCC_ADIM\n", null, null); });
            GiaoDien.NutLuoi(ht, "\U0001F4CD", "Đặt thiết bị khác", GiaoDien.MauCam, delegate { GiaoDien.GuiLenh("PCCC_DAT\n", null, null); });
            GiaoDien.NutLuoi(ht, "#", "Đánh số", GiaoDien.MauLa, delegate { GiaoDien.GuiLenh("PCCC_DANHSO\n", null, null); });
            GiaoDien.NutLuoi(ht, "\U0001F4CB", "Bóc khối lượng", GiaoDien.MauTim, delegate { GiaoDien.GuiLenh("PCCC_BOC\n", null, null); });
            GiaoDien.NutLuoi(ht, "\u25A6", "Chèn bảng TK", GiaoDien.MauLuc, delegate { GiaoDien.GuiLenh("PCCC_BANG\n", null, null); });
            GiaoDien.NutLuoi(ht, "\u2139", "Bảng ký hiệu", GiaoDien.MauHong, delegate { GiaoDien.GuiLenh("PCCC_KYHIEU\n", null, null); });
            f.Controls.Add(tt[2]);
            TaoDiDay();
            TinhLai();
            DemLai();
        }

        // ---------- Tab KHOANG DẦM (ý từ video plugin 16/11): chuẩn bị ranh giới, tra cứu 5.9.1.1.6 tại chỗ, chiều cao trần, rải ----------

        void TaoKhoangDam()
        {
            f = tab[1];
            f.Controls.Add(Chu("Đầu báo khói, trần có dầm: plugin đọc ký hiệu dầm b x h trên mặt bằng, chia khoang, xét từng ô theo TCVN 7568-14:2025 5.9.1.1.6 "
                + "(ô đủ ngưỡng thì mỗi ô ít nhất một đầu báo, ô nhỏ lắp ở mặt dưới dầm).", 0));
            f.Controls.Add(new TieuDeNhom("Chuẩn bị", GiaoDien.MauLa));
            FlowLayoutPanel cb = Luoi();
            GiaoDien.NutLuoi(cb, "\u25AD", "Ranh giới chữ nhật", GiaoDien.MauLa, delegate { GiaoDien.GuiLenh("_.-LAYER\n_M\nPCCC_VUNG\n\n_.RECTANG\n", "RECTANG", DemLai); });
            GiaoDien.NutLuoi(cb, "\u270F", "Ranh giới đa giác", GiaoDien.MauCam, delegate { GiaoDien.GuiLenh("_.-LAYER\n_M\nPCCC_VUNG\n\n_.PLINE\n", "PLINE", DemLai); });
            GiaoDien.NutLuoi(cb, "\U0001F4D6", "Tra cứu TCVN", GiaoDien.MauDuong, delegate { BangTraCuu.Mo(); });

            f.Controls.Add(new TieuDeNhom("Thông số", GiaoDien.MauCam));
            Label nl = new Label();
            nl.Text = "LOẠI TRẦN";
            nl.Font = GiaoDien.NhoDam;
            nl.ForeColor = GiaoDien.ChuMo;
            nl.AutoSize = true;
            f.Controls.Add(nl);
            loaiTran.DropDownStyle = ComboBoxStyle.DropDownList;
            loaiTran.Width = 290;
            loaiTran.AccessibleName = "Loại trần";
            loaiTran.Items.Add("Có trần: rải như trần phẳng");
            loaiTran.Items.Add("Không trần: đọc mặt bằng dầm, chia khoang");
            loaiTran.Items.Add("Vừa có vừa không: vùng + dầm vẽ tay");
            loaiTran.Items.Add("Mái kèo: khoang giữa hai hàng vì kèo (hàng cột)");
            int lt;
            loaiTran.SelectedIndex = int.TryParse(GiaoDien.DocCaiDat("kd_loai_tran", "1"), out lt) && lt >= 0 && lt <= 3 ? lt : 1;
            loaiTran.SelectedIndexChanged += delegate { GiaoDien.LuuCaiDat("kd_loai_tran", loaiTran.SelectedIndex.ToString(CultureInfo.InvariantCulture)); };
            f.Controls.Add(loaiTran);
            f.Controls.Add(Chu("Không trần: khối mặt bằng dầm chép đè lên mặt bằng tầng; mép dầm là tường của khoang. Vừa có vừa không: vẽ polyline kín "
                + "PCCC_VUNG quanh từng vùng, vẽ line dọc dầm (tim hoặc hai mép) ở vùng không trần; khi rải chọn line dầm, nhập b x h.", 0));
            Label nh = new Label();
            nh.Text = "CHIỀU CAO TRẦN h (m)";
            nh.Font = GiaoDien.NhoDam;
            nh.ForeColor = GiaoDien.ChuMo;
            nh.AutoSize = true;
            f.Controls.Add(nh);
            tranKd.Width = 120;
            tranKd.Font = new Font("Segoe UI", 11f, FontStyle.Bold);
            tranKd.BackColor = GiaoDien.The;
            tranKd.ForeColor = GiaoDien.MauCam;
            tranKd.BorderStyle = BorderStyle.FixedSingle;
            tranKd.TextAlign = HorizontalAlignment.Center;
            tranKd.AccessibleName = "Chiều cao trần khoang dầm (m)";
            tranKd.Text = GiaoDien.DocCaiDat("kd_h", GiaoDien.DocCaiDat("bt_h", ""));
            tranKd.TextChanged += delegate { CapNhatTrangThai(); };
            f.Controls.Add(tranKd);
            f.Controls.Add(Chu("h < 2 m: như trần phẳng. 2 ≤ h ≤ 4 m: ô ≥ 4 m² đặt trong ô. h ≥ 4 m: ô ≥ 9 m². Độ sâu dầm d lấy từ ký hiệu b x h (vd B1 (300x600)); "
                + "bản vẽ không ghi thì plugin hỏi một cỡ chung hoặc TungDam. Dầm d ≤ 0,3 m bỏ (trần phẳng).", 0));
            CheckBox ghiM2 = new CheckBox();
            ghiM2.Text = "Ghi m² từng khoang, đánh X khoang nhỏ (layer PCCC_KHOANG_M2)";
            ghiM2.ForeColor = GiaoDien.ChuDam;
            ghiM2.AutoSize = false;
            ghiM2.Size = new Size(300, 36);
            ghiM2.CheckAlign = ContentAlignment.TopLeft;
            ghiM2.TextAlign = ContentAlignment.TopLeft;
            ghiM2.Margin = new Padding(0, 6, 0, 0);
            ghiM2.Checked = GiaoDien.DocCaiDat("kd_ghi_m2", "Co") != "Khong"; // Rai.cs GhiKhoang đọc cùng khoá
            ghiM2.CheckedChanged += delegate { GiaoDien.LuuCaiDat("kd_ghi_m2", ghiM2.Checked ? "Co" : "Khong"); };
            f.Controls.Add(ghiM2);

            f.Controls.Add(new TieuDeNhom("Rải & làm lại", GiaoDien.MauLuc));
            FlowLayoutPanel r = Luoi();
            GiaoDien.NutLuoi(r, "\U0001F514", "Rải đầu báo khoang", GiaoDien.MauLuc, RaiKhoang);
            GiaoDien.NutLuoi(r, "\u21B6", "Hoàn tác lượt", GiaoDien.MauXam, delegate { GiaoDien.GuiLenh("_.U\n", "U", DemLai); });
            GiaoDien.NutLuoi(r, "\u2714", "Kiểm vùng mù", GiaoDien.MauDuong, delegate { GiaoDien.GuiLenh("PCCC_KT\n", "PCCC_KT", DemLai); });
            f.Controls.Add(Chu("RẢI gửi PCCC_RAI BC_KHOI + loại trần + h: quét chọn mặt bằng (ranh giới + nét dầm), Enter = tự nhận phòng, bấm một nét DẦM mẫu (vừa có vừa không: chọn line dầm), Enter.", 0));
            f.Controls.Add(tt[1]);
        }

        /// Chiều cao trần của tab Khoang dầm ("3,5" = 3.5); trống = 0; gõ sai = -1.
        double TranKd()
        {
            string s = tranKd.Text.Trim().Replace(',', '.');
            if (s == "") return 0;
            double h;
            return double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out h) && h > 0 ? h : -1;
        }

        void RaiKhoang()
        {
            double h = TranKd();
            if (h <= 0 && loaiTran.SelectedIndex != 0) throw new System.Exception("Nhập chiều cao trần h (m): ngưỡng ô 4 m² hay 9 m² tính theo h (5.9.1.1.6).");
            if (!ds.Any(t => t.Ma == "BC_KHOI")) throw new System.Exception("Không có BC_KHOI trong khoang_cach_rai.json: chạy lại 07_CAD\\build.bat.");
            GiaoDien.LuuCaiDat("kd_h", tranKd.Text.Trim());
            // Thứ tự dấu nhắc PCCC_RAI (Rai.cs RaiTay): mã → Dam → trần → theo hàng.
            string[] loai = { "CoTran", "KhongTran", "HonHop", "MaiKeo" }; // mã -> LoaiTran -> loại trần -> trần -> theo hàng
            GiaoDien.GuiLenh("PCCC_RAI\nBC_KHOI\nLoaiTran\n" + loai[Math.Max(0, loaiTran.SelectedIndex)] + "\n" + (h > 0 ? Ct(h) : "") + "\nKhong\n", "PCCC_RAI", delegate
            {
                ketQua.Text = Lenh.KetQuaRaiCuoi;
                ketQua.ForeColor = Lenh.KetQuaRaiCuoi.Contains("HO") ? GiaoDien.Do : GiaoDien.XanhLa;
                DemLai();
            });
        }

        // ---------- Bố cục, trạng thái ----------

        /// Lưới nút biểu tượng trong tab đang dựng.
        FlowLayoutPanel Luoi()
        {
            FlowLayoutPanel p = GiaoDien.LuoiNut();
            f.Controls.Add(p);
            return p;
        }

        /// Co nút, tiêu đề nhóm, khung trạng thái theo bề rộng tab (gọi khi đổi cỡ và khi chuyển tab: tab ẩn không nhận Resize).
        static void XepLai(FlowLayoutPanel t)
        {
            int w = GiaoDien.RongDung(t);
            t.SuspendLayout();
            foreach (Control c in t.Controls)
            {
                FlowLayoutPanel p = c as FlowLayoutPanel;
                if (p != null && p.Controls.OfType<NutIcon>().Any()) GiaoDien.XepLuoi(p, w);
                else if (c is TieuDeNhom || c is KhungTrangThai || "rong".Equals(c.Tag)) c.Width = w; // Tag "rong": nút, thanh trượt giãn đủ ngang
            }
            t.ResumeLayout();
        }

        /// Đếm lại vùng trong bản vẽ (sau lệnh, khi mở trang, khi đổi tab) rồi vẽ lại khung trạng thái.
        void DemLai()
        {
            try { vungDem = Lenh.DemVung(); } catch (System.Exception) { vungDem = new[] { -1, -1 }; } // đang chạy lệnh khác: để lần sau
            CapNhatTrangThai();
        }

        void CapNhatTrangThai()
        {
            const int DAT = KhungTrangThai.DAT, CB = KhungTrangThai.CANH_BAO, TIN = KhungTrangThai.TIN;
            KeyValuePair<int, string> vung = vungDem[0] < 0 ? KhungTrangThai.D(TIN, "")
                : vungDem[0] > 0 ? KhungTrangThai.D(DAT, "Bản vẽ có " + vungDem[0] + " vùng PCCC_VUNG" + (vungDem[1] > 0 ? ", " + vungDem[1] + " vùng loại trừ" : ""))
                : KhungTrangThai.D(CB, "Chưa có vùng PCCC_VUNG: BỐ TRÍ tự nhận phòng từ tường, hoặc vẽ ranh giới trước");
            string rc = Lenh.KetQuaRaiCuoi;
            KeyValuePair<int, string> rai = KhungTrangThai.D(rc.Contains("HO") ? CB : DAT, rc == "" ? "" : "Lượt rải cuối: " + rc);
            ThietBi t = Dang();
            double h = Tran(), hk = TranKd();
            tt[0].Dat(KhungTrangThai.D(t != null ? DAT : CB, t != null ? "Thiết bị: " + t.Ten + " (" + t.Ma + ")" : "Chưa chọn thiết bị"),
                KhungTrangThai.D(h > 0 ? DAT : TIN, h > 0 ? "Trần h = " + So(h) + " m" : "Trần: chưa nhập (không xét giới hạn chiều cao)"), vung, rai);
            string nguong = hk <= 0 ? (hk < 0 ? "Chiều cao trần phải là số dương" : "Chưa nhập chiều cao trần h")
                : hk < 2 ? "h = " + So(hk) + " m < 2 m: rải như trần phẳng (5.9.1.1.6 b)"
                : hk > 15 ? "h = " + So(hk) + " m > 15 m: đầu báo khói điểm không áp dụng (5.9.1.1.1)"
                : "h = " + So(hk) + " m: ô ≥ " + (hk >= 4 ? "9" : "4") + " m² đặt trong ô, ô nhỏ hơn lắp ở mặt dưới dầm" + (hk == 4 ? " (h = 4 m đúng ranh giới: chọn yêu cầu chặt hơn)" : "");
            tt[1].Dat(KhungTrangThai.D(hk >= 2 && hk <= 15 ? DAT : CB, nguong), vung, rai);
            string kt = Lenh.KetQuaKtCuoi;
            tt[2].Dat(KhungTrangThai.D(kt == "" ? TIN : kt.Contains("phủ kín") ? DAT : CB, kt == "" ? "Chưa kiểm vùng mù lượt này" : kt), vung);
            int lop = 0;
            try { lop = CaiDat.LopDay().Count(r => r[3] == "BC"); } catch (System.Exception) { }
            tt[3].Dat(KhungTrangThai.D(DAT, "Hệ " + (heDiaChi.Checked ? "địa chỉ" : "thường")),
                KhungTrangThai.D(lop > 0 ? DAT : CB, lop > 0 ? lop + " layer dây báo cháy trong bảng Cài đặt" : "Chưa có layer dây báo cháy: thêm ở trang Cài đặt"));
            foreach (KhungTrangThai k in tt) k.Invalidate();
        }

        // ---------- Mục 6: đi dây báo cháy (người dùng 16/11): hệ thường / hệ địa chỉ, chọn layer dây cho từng loại thiết bị ----------
        readonly RadioButton heThuong = new RadioButton(), heDiaChi = new RadioButton();
        readonly List<KeyValuePair<string, ComboBox>> chonDay = new List<KeyValuePair<string, ComboBox>>();

        void TaoDiDay()
        {
            f = tab[3];
            Muc("1", "ĐI DÂY BÁO CHÁY", "vuông góc, không đi xéo");
            bool dc = GiaoDien.DocCaiDat("he_bc", "Thuong") == "DiaChi";
            heThuong.Text = "Hệ thường: 1 đường qua đầu báo + nút nhấn, điện trở cuối ở thiết bị xa tủ nhất; chuông 1 đường nguồn mạch cụt";
            heDiaChi.Text = "Hệ địa chỉ: mạch vòng tủ → module, thiết bị địa chỉ → về tủ; đầu báo, nút nhấn thường về ZM; chuông thường về CM, chuông địa chỉ về R2ML";
            foreach (RadioButton r in new[] { heThuong, heDiaChi })
            {
                r.ForeColor = GiaoDien.ChuDam;
                // RadioButton AutoSize không xuống dòng: cỡ cố định, chữ canh trên để đọc đủ câu dài.
                r.AutoSize = false;
                r.Size = new Size(300, 50);
                r.CheckAlign = ContentAlignment.TopLeft;
                r.TextAlign = ContentAlignment.TopLeft;
                r.Margin = new Padding(0, 4, 0, 0);
                f.Controls.Add(r);
            }
            heThuong.Checked = !dc; heDiaChi.Checked = dc;
            heThuong.CheckedChanged += delegate { GiaoDien.LuuCaiDat("he_bc", heDiaChi.Checked ? "DiaChi" : "Thuong"); CapNhatTrangThai(); };
            f.Controls.Add(Chu("Dây cho từng loại thiết bị (layer lấy từ bảng ở trang Cài đặt):", 0));
            TableLayoutPanel bang = new TableLayoutPanel();
            bang.ColumnCount = 2;
            bang.AutoSize = true;
            bang.BackColor = GiaoDien.Nen;
            bang.Margin = new Padding(0, 2, 0, 4);
            foreach (string[] v in CaiDat.VAI_TRO)
            {
                string khoa = v[0];
                Label l = new Label();
                l.Text = v[1];
                l.ForeColor = GiaoDien.ChuDam;
                l.AutoSize = true;
                l.Margin = new Padding(0, 6, 6, 0);
                ComboBox c = new ComboBox();
                c.DropDownStyle = ComboBoxStyle.DropDownList;
                c.Width = 170;
                c.AccessibleName = "Dây " + v[1];
                c.SelectedIndexChanged += delegate { if (c.SelectedItem != null) GiaoDien.LuuCaiDat("day_" + khoa, c.SelectedItem.ToString()); };
                bang.Controls.Add(l);
                bang.Controls.Add(c);
                chonDay.Add(new KeyValuePair<string, ComboBox>(khoa, c));
            }
            f.Controls.Add(bang);
            NapChonDay();
            // Bảng layer có thể đổi ở trang Cài đặt: mở lại trang này thì nạp lại danh sách.
            VisibleChanged += delegate { if (Visible) NapChonDay(); };
            NutBo di = new NutBo("▶  ĐI DÂY", GiaoDien.XanhDam);
            di.Size = new Size(300, 36);
            di.Margin = new Padding(0, 6, 0, 4);
            di.Click += delegate { GiaoDien.GuiLenh("PCCC_DAYBC\n" + (heDiaChi.Checked ? "DiaChi" : "Thuong") + "\n", null, null); };
            f.Controls.Add(di);
            di.Tag = "rong";
            f.Controls.Add(Chu("Gửi PCCC_DAYBC: quét chọn thiết bị một zone (kể cả module ZM, CM, R2ML và tủ nếu có), bấm vị trí tủ TTBC khi vùng chọn không có tủ. "
                + "Thiết bị địa chỉ: mã có đuôi _DC (BC_KHOI_DC, BC_NUTAN_DC, BC_CHUONG_DC) hoặc tên block có \"DIA CHI\".", 0));
            f.Controls.Add(new TieuDeNhom("Dây tay, sơ đồ", GiaoDien.MauDo));
            FlowLayoutPanel tay = Luoi();
            GiaoDien.NutLuoi(tay, "\u27F2", "Vẽ dây loop", GiaoDien.MauCam, delegate { GiaoDien.GuiLenh("PCCC_DAY\nDAY_BC\n", null, null); });
            GiaoDien.NutLuoi(tay, "\U0001F4DC", "Bảng quy định lắp đặt", GiaoDien.MauTim, delegate { GiaoDien.GuiLenh("PCCC_QUYDINH\n", null, null); });
            GiaoDien.NutLuoi(tay, "\u21B6", "Hoàn tác lượt", GiaoDien.MauXam, delegate { GiaoDien.GuiLenh("_.U\n", null, null); });
            f.Controls.Add(tt[3]);
        }

        void NapChonDay()
        {
            List<string> lop = CaiDat.LopDay().Where(r => r[3] == "BC").Select(r => r[0]).ToList();
            foreach (KeyValuePair<string, ComboBox> kv in chonDay)
            {
                string dang = CaiDat.DayVaiTro(kv.Key);
                ComboBox c = kv.Value;
                c.BeginUpdate();
                c.Items.Clear();
                foreach (string l in lop) c.Items.Add(l);
                if (!lop.Contains(dang)) c.Items.Add(dang); // layer đã xoá khỏi bảng: vẫn hiện để biết đang dùng gì
                c.SelectedItem = dang;
                c.EndUpdate();
            }
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
            if (phuR != null) phuR.Enabled = t.R > 0;
            // Lưới vuông bước s: mỗi thiết bị phủ s² m²; tia: mỗi tia phủ dải rộng s, tính theo m dài tia.
            if (tt[3] != null) CapNhatTrangThai();
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
                DemLai();
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
