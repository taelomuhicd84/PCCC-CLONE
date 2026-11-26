// Plugin AutoCAD 2021 (.NET Framework 4.8) — thư viện block PCCC, vẽ thiết bị/ống, bóc khối lượng gửi runner,
// bảng thống kê, vùng bảo vệ.
// Biên dịch bằng csc.exe có sẵn trong Windows (C# 5): không dùng $"", ?., nameof, out var.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Web.Script.Serialization;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using AcApp = Autodesk.AutoCAD.ApplicationServices.Core.Application; // Core: chạy được cả trong accoreconsole (không giao diện)

[assembly: CommandClass(typeof(TKPCCC.Lenh))]
[assembly: ExtensionApplication(typeof(TKPCCC.KhoiDong))]

namespace TKPCCC
{
    public class VatTu
    {
        public string ma { get; set; }
        public string ten { get; set; }
        public string dvt { get; set; }
        public string he { get; set; }
        public string loai { get; set; }
        public string ky_hieu { get; set; }
        // Chỉ cho mã dây vẽ trên sơ đồ nguyên lý (SoDo.cs): tên layer, loại đường, màu ACI khi phải tạo layer mới.
        public string layer_so_do { get; set; }
        public string net { get; set; }
        public int mau { get; set; }
    }

    public class FileDanhMuc
    {
        public Dictionary<string, string> he { get; set; }
        public List<VatTu> vat_tu { get; set; }
    }

    /// Báo đã nạp khi NETLOAD hoặc tự nạp từ bundle.
    public class KhoiDong : IExtensionApplication
    {
        public void Initialize()
        {
            BanQuyen.Chan(); // chỉ bản build phat_hanh chặn lệnh PCCC* khi chưa có bản quyền
            Document doc = AcApp.DocumentManager.MdiActiveDocument;
            // In đường dẫn DLL (review 15/11): biết AutoCAD đang chạy bản bundle hay bản thử; bộ test kiểm đúng bản vừa build.
            if (doc != null) doc.Editor.WriteMessage("\n[TKPCCC] Đã nạp plugin PCCC (" + Assembly.GetExecutingAssembly().Location + "). Gõ PCCC_HELP để xem lệnh.");
            // Cầu nối MCP chỉ bật trong AutoCAD có giao diện: accoreconsole không có vòng lặp thông điệp cho timer.
            try
            {
                if (System.Diagnostics.Process.GetCurrentProcess().ProcessName.ToLowerInvariant() == "acad")
                {
                    Cau.BatDau();
                    // Bảng block (TKPCCC.UI.dll, lệnh PCCC_BLOCK) cần acmgd nên chỉ nạp ở đây: người dùng NETLOAD một file TKPCCC.Cad.dll là đủ.
                    string ui = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "TKPCCC.UI.dll");
                    if (File.Exists(ui)) ExtensionLoader.Load(ui);
                }
            }
            catch (System.Exception ex) { if (doc != null) doc.Editor.WriteMessage("\n[TKPCCC] Không nạp được palette (TKPCCC.UI.dll): " + ex.Message); }
        }

        public void Terminate() { }
    }

    public partial class Lenh // các file khác trong 07_CAD/src (Spec.cs...) là phần nối của lớp này
    {
        const string TAG_MA = "MA_VT";
        const string TIEN_TO_LAYER = "PCCC_";
        const string KIEU_CHU = "PCCC_ARIAL"; // chữ TrueType để hiện được tiếng Việt (txt.shx không có dấu)

        // vat_tu.json (build.bat chép vào), PCCC_Blocks.dwg và block_alias.json (người dùng sửa) nằm cạnh DLL.
        static string ThuMuc { get { return Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location); } }
        static string FileThuVien { get { return Path.Combine(ThuMuc, "PCCC_Blocks.dwg"); } }

        // ---------- Lệnh: bọc bắt lỗi để không hiện hộp "Unhandled exception" của AutoCAD ----------

        [CommandMethod("PCCC_HELP")] public void LenhHelp() { Chay(TroGiup); }
        [CommandMethod("PCCC_LIB")] public void LenhLib() { Chay(NapThuVien); }
        [CommandMethod("PCCC_DAT")] public void LenhDat() { Chay(DatThietBi); }
        [CommandMethod("PCCC_ONG")] public void LenhOng() { Chay(VeOng); }
        [CommandMethod("PCCC_GAN")] public void LenhGan() { Chay(GanBlock); }
        [CommandMethod("PCCC_BOC")] public void LenhBoc() { Chay(BocKhoiLuong); }
        [CommandMethod("PCCC_BANG")] public void LenhBang() { Chay(ChenBang); }
        [CommandMethod("PCCC_VUNGBV")] public void LenhVungBV() { Chay(VungBaoVe); }
        [CommandMethod("PCCC_SPEC")] public void LenhSpec() { Chay(ChaySpec); } // 07_CAD/06_VE_TU_DONG_AI/cad/Spec.cs
        [CommandMethod("PCCC_RAI")] public void LenhRai() { Chay(RaiTay); }     // 07_CAD/09_RAI_THIET_BI/cad/Rai.cs
        [CommandMethod("PCCC_TUYEN")] public void LenhTuyen() { Chay(TuyenTay); } // 07_CAD/09_RAI_THIET_BI/cad/Rai.cs
        [CommandMethod("PCCC_DAY")] public void LenhDay() { Chay(DayTay); }       // 07_CAD/09_RAI_THIET_BI/cad/Rai.cs
        [CommandMethod("PCCC_THEMBLOCK")] public void LenhThemBlock() { Chay(ThemBlock); } // 07_CAD/14_THU_VIEN_BLOCK/cad/Block.cs
        /// Esc giữa lệnh dài (review 15/11): ném lỗi để transaction đang mở không commit, bản vẽ không đổi. Gọi thưa (mỗi phòng, mỗi vài trăm vòng).
        static void KiemEsc()
        {
            if (HostApplicationServices.Current.UserBreak()) throw new System.Exception("đã dừng bằng Esc, bản vẽ không đổi");
            // Người dùng 26/11: bot chat gọi vung_tu_dong 25 phòng, AutoCAD treo > 5 phút. Gói spec quá hạn thì dừng (MCP chỉ chờ 90 s).
            if (hanChot != 0 && Environment.TickCount - hanChot > 0)
                throw new System.Exception("gói spec chạy quá " + HAN_SPEC_S + " giây nên đã dừng, bản vẽ không đổi: chia gói nhỏ hơn"
                    + " (vd vung_tu_dong 3–5 điểm một lần), giảm \"ho\", hoặc thu hẹp vùng");
        }

        const int HAN_SPEC_S = 75;
        static int hanChot; // Environment.TickCount lúc hết hạn gói spec đang chạy; 0 = không giới hạn (lệnh gõ tay)
        internal static bool DangSpec { get { return hanChot != 0; } } // Cau.cs: không chạy lồng gói nền vào giữa gói đang chạy

        static void Chay(Action viec)
        {
            try { viec(); }
            catch (System.Exception ex)
            {
                Document doc = AcApp.DocumentManager.MdiActiveDocument;
                if (doc != null) doc.Editor.WriteMessage("\n[TKPCCC] Lỗi: " + ex.Message);
            }
        }

        /// Mọi lệnh gõ tay: { tên, việc làm, cách dùng }. Thêm lệnh mới thì thêm một dòng ở đây (bảng PCCC_HELP đọc mảng này).
        public static readonly string[][] DS_LENH = {
            new[] { "PCCC", "Palette chính: quy trình vẽ 6 bước, thư viện, lệnh, bot chat, cài đặt", "Gõ lệnh; chọn phân hệ ở thanh dọc trái, ô tìm lọc thẻ theo tên hoặc lệnh" },
            new[] { "PCCC_HELP", "Bảng mọi lệnh PCCC, tìm lệnh, sửa phím tắt", "Gõ chữ vào ô tìm; sửa ô Phím tắt rồi Enter là lưu cho lần sau" },
            new[] { "PCCC_TRUC", "B1: vẽ lưới trục", "Nhập nhịp X (vd 6000,6000,4500), nhịp Y, bấm điểm gốc" },
            new[] { "PCCC_TUONG", "B2, B3: vẽ tường theo tim, vách ngăn", "Nhập dày, bấm chuỗi điểm như PLINE; Vach = vách ngăn, Dong = khép kín" },
            new[] { "PCCC_CUA", "B3: cửa đi trên tường đã vẽ", "Bấm tâm cửa trên tường, nhập rộng, bấm phía cánh mở vào" },
            new[] { "PCCC_ADIM", "Dim tự động cả lưới thiết bị", "Lọc Block/Line/Pline, quét cửa sổ, 2 mốc tường (Enter bỏ qua), bấm điểm đặt đường dim" },
            new[] { "PCCC_CDIM", "Dim tiếp tục chuỗi ADIM vừa tạo", "Bấm các điểm tiếp theo" },
            new[] { "PCCC_ADIMMOC", "Bấm trước mép tường / trục cho ADIM (trang ADIM bước 1, 2)", "Nhập 1 (đầu) hoặc 2 (cuối), bấm điểm; lần PCCC_ADIM kế tiếp dùng luôn, không hỏi" },
            new[] { "PCCC_BANQUYEN", "Bản quyền: email, mã máy, key", "Nhập email, Đăng ký hoặc dán key rồi Kích hoạt; Lấy key tự động sau 5–10 phút" },
            new[] { "PCCC_TIENICH", "Tiện ích: 27 thẻ chia 6 nhóm (khu vực, xử lý, hiệu chỉnh, layer ống, kiểm tra, cài đặt)", "Mở phân hệ Tiện ích của palette; nút lọc nhóm, ô tìm theo tên hoặc mã tắt (SZ, CKC...)" },
            new[] { "PCCC_BOTRI", "Bố trí báo cháy tự động: chọn thiết bị theo TCVN, trần, mức tối ưu k", "Mở phân hệ Báo cháy của palette; BỐ TRÍ gửi PCCC_RAI điền sẵn, chỉ còn quét chọn mặt bằng" },
            new[] { "PCCC_BLOCK", "Bảng block PCCC: ký hiệu, tên, số lượng trong bản vẽ", "Bấm đúp hoặc nút xanh để đặt, nút cam để rải; chỉ có trong AutoCAD có giao diện" },
            new[] { "PCCC_LIB", "Nạp, cập nhật block PCCC từ PCCC_Blocks.dwg", "Gõ lệnh; Taomoi = cất bản cũ, tạo lại thư viện mặc định" },
            new[] { "PCCC_DAT", "Đặt thiết bị theo mã vật tư", "Nhập mã (? = danh sách, gõ một phần tên hoặc mã để tìm), bấm điểm đặt" },
            new[] { "PCCC_ONG", "Vẽ ống, dây trên layer PCCC_<mã>", "Nhập mã ống, dây (vd ONG_DN50), bấm các điểm" },
            new[] { "PCCC_RAI", "Rải thiết bị theo lưới trong phòng", "Nhập mã (đầu báo: chiều cao trần; HeSo = mức tối ưu k 50–100 %, Dam = trần có dầm > 0,3 m, XemTruoc = chỉ đếm; sprinkler: Dam ở dấu nhắc S), quét chọn mặt bằng, bấm nét phân vùng mẫu (mỗi màu một lần, Enter = tự nhận từ tường), Enter = mọi vùng hoặc bấm điểm từng vùng; tự vẽ vùng PCCC_VUNG rồi rải; BC_BEAM vẽ tia nối hai tường" },
            new[] { "PCCC_TUYEN", "Ống nhánh sprinkler kiểu xương cá", "Mã ống, hoặc TuDong = DN theo số đầu phun phía sau (bảng ong_nhanh_dn.json); chọn đầu phun, ống chính, bấm vị trí ống góp" },
            new[] { "PCCC_TRUNG", "Kiểm tra thiết bị đặt trùng (CKC)", "Quét vùng (Enter = cả bản vẽ): cùng mã cách nhau ≤ 10 mm thì khoanh đỏ trên PCCC_KIEM" },
            new[] { "PCCC_LOPCHUAN", "Kiểm tra, áp layer chuẩn thiết bị (CKL, APL)", "Quét vùng (Enter = cả bản vẽ); Co = chuyển thiết bị sai về layer PCCC_<hệ>, Khong = khoanh đỏ" },
            new[] { "PCCC_DANHSO", "Đánh số thiết bị (AN)", "Chọn thiết bị, nhập tiền tố, số bắt đầu, thứ tự XY (theo hàng) hoặc YX (theo cột)" },
            new[] { "PCCC_XOAY", "Xoay từng block quanh điểm chèn (MR)", "Chọn block, nhập góc xoay thêm (độ)" },
            new[] { "PCCC_TYLE", "Tỉ lệ từng block quanh điểm chèn (SCB)", "Chọn block, nhập hệ số" },
            new[] { "PCCC_DOIBLOCK", "Đổi block, giữ điểm, góc, tỉ lệ (SB)", "Chọn block cần thay, nhập mã thiết bị mới" },
            new[] { "PCCC_CANDIM", "Dóng hàng đường kích thước (AD)", "Chọn các dim, bấm điểm đường dim đi qua" },
            new[] { "PCCC_DOIDN", "Đổi cỡ ống DN (DN)", "Chọn đoạn ống, nhập mã ống mới (vd ONG_DN65)" },
            new[] { "PCCC_TACHVUNG", "Tách khu vực (TZ)", "Chọn vùng PCCC_VUNG, bấm 2 điểm đường tách: ra 2 vùng" },
            new[] { "PCCC_KHUVUC", "Quản lý khu vực (MZ): đặt tên vùng, tổng m²", "Bấm điểm trong vùng PCCC_VUNG, gõ tên (ghi chữ tên + m²), Enter = xong" },
            new[] { "PCCC_KT","Kiểm tra vùng mù đầu báo (PCKT, CKO)", "Quét phòng PCCC_VUNG + thiết bị (Enter = cả bản vẽ): % mù từng phòng theo R, khoanh đỏ điểm mù trên PCCC_KIEM_MU" },
            new[] { "PCCC_DAY", "Dây báo cháy nối tiếp thiết bị (dây loop)", "Nhập mã dây (DAY_BC), chọn thiết bị, bấm điểm đầu (tủ), điểm cuối: Enter = dừng ở thiết bị cuối, Vong = về điểm đầu" },
            new[] { "PCCC_DAYBC", "Đi dây báo cháy tự động: hệ thường hoặc hệ địa chỉ", "Chọn Thuong / DiaChi, quét chọn thiết bị một zone (kể cả module ZM, CM, R2ML), bấm tủ TTBC; dây vuông góc, layer theo trang Báo cháy" },
            new[] { "PCCC_THEMBLOCK", "Thêm block của bạn (kể cả block động) vào thư viện", "Chọn block, nhập mã vật tư mới, tên, hệ, đơn vị tính" },
            new[] { "PCCC_GAN", "Gán block có sẵn trong bản vẽ vào mã vật tư", "Chọn block rồi nhập mã: bóc được bản vẽ không vẽ bằng plugin" },
            new[] { "PCCC_BOC", "Bóc khối lượng, gửi lên web báo giá", "Chọn vùng (Enter = cả bản vẽ), tách theo tầng, khu nếu muốn" },
            new[] { "PCCC_BANG", "Chèn bảng thống kê vào bản vẽ", "Chạy PCCC_BOC trước (bảng lấy kết quả gần nhất), bấm điểm chèn" },
            new[] { "PCCC_KYHIEU", "Chèn bảng KÝ HIỆU cho mặt bằng (chỉ thiết bị có trên tờ)", "Chọn mặt bằng (Enter = cả bản vẽ), bấm góc trên trái, cao chữ" },
            new[] { "PCCC_QUYDINH", "Chèn bảng quy định lắp đặt đầu báo (TCVN 7568-14:2025)", "Bấm góc trên trái, cao chữ; trị số lấy từ khoang_cach_rai.json" },
            new[] { "PCCC_THOATNAN", "Hatch gạch chéo ĐƯỜNG THOÁT NẠN", "Chọn polyline kín các lối thoát nạn (layer PCCC_THOAT_NAN)" },
            new[] { "PCCC_THUYETMINH", "Chèn thuyết minh theo các hệ có trên bản vẽ (văn mẫu)", "Bấm góc trên trái, cao chữ; mỗi hệ một cột, đọc lại số liệu" },
            new[] { "PCCC_VUNGBV", "Vẽ vùng bảo vệ bán kính R quanh thiết bị", "Nhập mã, bán kính R; báo thiết bị đứng lẻ" },
            new[] { "PCCC_SPEC", "Chạy một file spec JSON (các thao tác như cad_chay)", "Nhập đường dẫn file .json; kết quả ghi <tên>.kq.json cạnh file" } };

        /// TKPCCC.UI.dll gán khi nạp trong AutoCAD có giao diện: PCCC_HELP mở bảng lệnh thay cho in chữ.
        public static Action MoBangLenh;
        /// TKPCCC.UI.dll gán: hỏi mã mà gõ "?" (hoặc chữ khớp nhiều mã) thì mở bảng chọn (tiêu đề, danh sách, chữ lọc sẵn); trả null = đóng không chọn.
        public static Func<string, List<VatTu>, string, VatTu> ChonVatTu;

        static void TroGiup()
        {
            if (MoBangLenh != null) { MoBangLenh(); return; }
            Editor ed = AcApp.DocumentManager.MdiActiveDocument.Editor;
            ed.WriteMessage("\n[TKPCCC] Thư mục dữ liệu: " + ThuMuc);
            foreach (string[] l in DS_LENH) ed.WriteMessage("\n  " + l[0].PadRight(15) + l[1] + ": " + l[2]);
        }

        // ---------- Danh mục, tiện ích ----------

        static FileDanhMuc DocDanhMuc()
        {
            string f = Path.Combine(ThuMuc, "vat_tu.json");
            if (!File.Exists(f)) throw new FileNotFoundException("Thiếu " + f + " — chạy lại 07_CAD\\build.bat để chép danh mục vật tư.");
            return new JavaScriptSerializer().Deserialize<FileDanhMuc>(File.ReadAllText(f, Encoding.UTF8));
        }

        static VatTu TimMa(FileDanhMuc dm, string ma)
        {
            return dm.vat_tu.FirstOrDefault(v => string.Equals(v.ma, ma, StringComparison.OrdinalIgnoreCase));
        }

        /// Chữ thường không dấu để tìm "dau bao khoi" ra "Đầu báo khói".
        public static string BoDau(string s)
        {
            StringBuilder sb = new StringBuilder();
            foreach (char ch in (s ?? "").Normalize(NormalizationForm.FormD))
                if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark) sb.Append(ch == 'đ' || ch == 'Đ' ? 'd' : ch);
            return sb.ToString().ToLowerInvariant();
        }

        // Màu layer theo hệ để phân biệt trên bản vẽ.
        static short MauHe(string he)
        {
            switch (he)
            {
                case "BC": return 1;    // đỏ
                case "SPR": return 5;   // xanh dương
                case "HVT": return 3;   // xanh lá
                case "NUOC": return 4;  // cyan
                case "BINH": return 6;  // tím
                case "DEN": return 2;   // vàng
                default: return 30;     // cam: ống, dây
            }
        }

        static ObjectId DamBaoLayer(Database db, Transaction tr, string ten, short mau)
        {
            LayerTable lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
            if (lt.Has(ten)) return lt[ten];
            lt.UpgradeOpen();
            LayerTableRecord l = new LayerTableRecord();
            l.Name = ten;
            l.Color = Color.FromColorIndex(ColorMethod.ByAci, mau);
            ObjectId id = lt.Add(l);
            tr.AddNewlyCreatedDBObject(l, true);
            return id;
        }

        static ObjectId DamBaoKieuChu(Database db, Transaction tr)
        {
            TextStyleTable tst = (TextStyleTable)tr.GetObject(db.TextStyleTableId, OpenMode.ForRead);
            if (tst.Has(KIEU_CHU)) return tst[KIEU_CHU];
            tst.UpgradeOpen();
            TextStyleTableRecord s = new TextStyleTableRecord();
            s.Name = KIEU_CHU;
            s.FileName = "arial.ttf";
            s.Font = new Autodesk.AutoCAD.GraphicsInterface.FontDescriptor("Arial", false, false, 0, 0);
            ObjectId id = tst.Add(s);
            tr.AddNewlyCreatedDBObject(s, true);
            return id;
        }

        /// Hệ số đổi đơn vị bản vẽ ra mét theo INSUNITS.
        /// INSUNITS không phải đơn vị dài đã biết (Unitless...): đoán theo khổ bản vẽ (review 15/11: bản vẽ theo mét để Unitless từng bị coi là mm,
        /// mỗi phòng 1 đầu báo mà báo "phủ kín"). Khổ (cạnh lớn của EXTMIN–EXTMAX) dưới 2 000 đơn vị thì là m, còn lại mm.
        static double HeSoMet(Database db)
        {
            switch (db.Insunits)
            {
                case UnitsValue.Meters: return 1;
                case UnitsValue.Decimeters: return 0.1;
                case UnitsValue.Centimeters: return 0.01;
                case UnitsValue.Millimeters: return 0.001;
                case UnitsValue.Inches: return 0.0254;
                case UnitsValue.Feet: return 0.3048;
            }
            Point3d a = db.Extmin, b = db.Extmax;
            double kho = b.X >= a.X && b.Y >= a.Y ? Math.Max(b.X - a.X, b.Y - a.Y) : 0;
            double k = kho > 0 && kho < 2000 ? 1 : 0.001;
            if (canhBaoDonVi != db.UnmanagedObject)
            {
                canhBaoDonVi = db.UnmanagedObject;
                try
                {
                    AcApp.DocumentManager.MdiActiveDocument.Editor.WriteMessage("\n[TKPCCC] Chú ý: INSUNITS = " + db.Insunits + ", coi bản vẽ theo "
                        + (k == 1 ? "m" : "mm") + " (đoán theo khổ bản vẽ). Đặt INSUNITS 4 (mm) hoặc 6 (m) cho chắc.");
                }
                catch (System.Exception) { }
            }
            return k;
        }
        static IntPtr canhBaoDonVi;

        /// Người dùng 16/11 (ảnh "rải linh tinh"): bản vẽ để INSUNITS 1 (inch) mà vẽ theo mm, PCCC_RAI coi 1 m = 39 đơn vị: khe cửa không khép,
        /// lòng tường thành phòng, bước rải 0,4 m (thử lại: 8 837 đầu báo trong 684 "phòng"; đặt mm thì 112 đầu, 57 phòng).
        /// Khổ bản vẽ (EXTMIN–EXTMAX) theo INSUNITS phi lý (trên 2 km, hoặc dưới 2 m) mà theo mm (hoặc m) ra 5 m – 2 km thì đổi INSUNITS, có báo.
        /// Chỉ xét INSUNITS là đơn vị dài đã biết (Unitless thì HeSoMet đã tự đoán). Gọi đầu lệnh gõ tay, trước mọi phép đổi m sang đơn vị bản vẽ.
        static void KiemDonVi(Document doc)
        {
            Database db = doc.Database;
            UnitsValue cu = db.Insunits;
            if (cu != UnitsValue.Inches && cu != UnitsValue.Feet && cu != UnitsValue.Millimeters && cu != UnitsValue.Centimeters
                && cu != UnitsValue.Decimeters && cu != UnitsValue.Meters) return;
            Point3d a = db.Extmin, b = db.Extmax;
            if (!(b.X > a.X && b.Y > a.Y)) return;
            double kho = Math.Max(b.X - a.X, b.Y - a.Y), met = kho * HeSoMet(db);
            UnitsValue moi;
            if (met > 2000 && kho * 0.001 >= 5 && kho * 0.001 <= 2000) moi = UnitsValue.Millimeters;
            else if (met < 2 && kho >= 5 && kho <= 2000) moi = UnitsValue.Meters;
            else return;
            if (moi == cu) return;
            db.Insunits = moi;
            doc.Editor.WriteMessage("\n[TKPCCC] Chú ý: INSUNITS = " + (int)cu + " (" + cu + ") thì bản vẽ rộng "
                + Math.Round(met, 1).ToString(CultureInfo.InvariantCulture) + " m, không hợp lý; khổ " + Math.Round(kho).ToString(CultureInfo.InvariantCulture)
                + " đơn vị hợp với " + (moi == UnitsValue.Millimeters ? "mm" : "m") + ": đã đặt INSUNITS = " + (int)moi
                + ". Sai thì gõ INSUNITS đặt lại. don_vi_doi=" + (int)cu + "->" + (int)moi);
        }

        /// Tên đơn vị bản vẽ cho kết quả spec ("don_vi"): đơn vị đoán thì thêm " (doan)".
        static string TenDonVi(Database db)
        {
            switch (db.Insunits)
            {
                case UnitsValue.Meters: return "m";
                case UnitsValue.Decimeters: return "dm";
                case UnitsValue.Centimeters: return "cm";
                case UnitsValue.Millimeters: return "mm";
                case UnitsValue.Inches: return "in";
                case UnitsValue.Feet: return "ft";
            }
            return (HeSoMet(db) == 1 ? "m" : "mm") + " (doan)";
        }

        static void InDanhSach(Editor ed, IEnumerable<VatTu> ds)
        {
            foreach (VatTu v in ds) ed.WriteMessage("\n  " + v.ma.PadRight(16) + v.ten + " (" + v.dvt + ")");
        }

        /// Hỏi mã vật tư: đúng mã thì nhận; gõ 1 phần tên/mã (không cần dấu) mà chỉ khớp 1 mã thì nhận luôn.
        static VatTu HoiMa(Editor ed, FileDanhMuc dm, string loai, string loiNhac)
        {
            List<VatTu> cung = dm.vat_tu.Where(x => x.loai == loai).ToList();
            while (true)
            {
                PromptStringOptions o = new PromptStringOptions("\n" + loiNhac + " (? = danh sách, gõ 1 phần tên/mã để tìm, Enter = thoát): ");
                o.AllowSpaces = true;
                PromptResult r = ed.GetString(o);
                if (r.Status != PromptStatus.OK) return null;
                string s = r.StringResult.Trim();
                if (s == "") return null;
                if (s == "?") { VatTu c = XemDanhSach(ed, dm, cung, cung, loiNhac, ""); if (c != null) return c; continue; }
                VatTu v = cung.FirstOrDefault(x => string.Equals(x.ma, s, StringComparison.OrdinalIgnoreCase));
                if (v != null) return v;
                string k = BoDau(s);
                List<VatTu> tim = cung.Where(x => BoDau(x.ma).Contains(k) || BoDau(x.ten).Contains(k)).ToList();
                // Gõ đúng cả tên ("dau bao nhiet") thì nhận mã đó, dù tên khác chứa nó ("Đầu báo nhiệt địa chỉ").
                List<VatTu> dungTen = tim.Where(x => BoDau(x.ten) == k).ToList();
                if (dungTen.Count == 1) tim = dungTen;
                if (tim.Count == 1) { ed.WriteMessage("\n  -> " + tim[0].ma + "  " + tim[0].ten); return tim[0]; }
                if (tim.Count == 0) ed.WriteMessage("\nKhông có mã/tên nào chứa \"" + s + "\". Gõ ? để xem danh sách.");
                else
                {
                    ed.WriteMessage("\nCó " + tim.Count + " mã khớp" + (ChonVatTu == null ? ", gõ đúng mã:" : "."));
                    VatTu c = XemDanhSach(ed, dm, cung, tim, loiNhac, s);
                    if (c != null) return c;
                }
            }
        }

        /// Có TKPCCC.UI.dll: mở bảng chọn cả danh sách, lọc sẵn theo chữ đã gõ; accoreconsole: in các mã khớp ra dòng lệnh. Trả null = hỏi lại.
        static VatTu XemDanhSach(Editor ed, FileDanhMuc dm, List<VatTu> tatCa, List<VatTu> khop, string loiNhac, string loc)
        {
            if (ChonVatTu == null) { InDanhSach(ed, khop); return null; }
            if (tatCa.Any(x => x.loai == "block"))
                try { CapNhatAnhBlock(dm); } catch (System.Exception) { } // ảnh xem trước lỗi vẫn cho chọn, chỉ thiếu ký hiệu
            VatTu v = ChonVatTu(loiNhac, tatCa, loc);
            if (v != null) ed.WriteMessage("\n  -> " + v.ma + "  " + v.ten);
            return v;
        }

        // ---------- Thư viện block ----------

    }
}
