// Plugin AutoCAD 2021 (.NET Framework 4.8) — thư viện block PCCC, vẽ thiết bị/ống, bóc khối lượng gửi runner,
// bảng thống kê, vùng bảo vệ.
// Biên dịch bằng csc.exe có sẵn trong Windows (C# 5): không dùng $"", ?., nameof, out var.
using System;
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
            Document doc = AcApp.DocumentManager.MdiActiveDocument;
            if (doc != null) doc.Editor.WriteMessage("\n[TKPCCC] Đã nạp plugin PCCC. Gõ PCCC_HELP để xem lệnh.");
            // Cầu nối MCP chỉ bật trong AutoCAD có giao diện: accoreconsole không có vòng lặp thông điệp cho timer.
            try { if (System.Diagnostics.Process.GetCurrentProcess().ProcessName.ToLowerInvariant() == "acad") Cau.BatDau(); }
            catch (System.Exception) { }
        }

        public void Terminate() { }
    }

    public partial class Lenh // các file khác trong cad/src (Spec.cs...) là phần nối của lớp này
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
        [CommandMethod("PCCC_SPEC")] public void LenhSpec() { Chay(ChaySpec); } // cad/module/plugin/Spec.cs

        static void Chay(Action viec)
        {
            try { viec(); }
            catch (System.Exception ex)
            {
                Document doc = AcApp.DocumentManager.MdiActiveDocument;
                if (doc != null) doc.Editor.WriteMessage("\n[TKPCCC] Lỗi: " + ex.Message);
            }
        }

        static void TroGiup()
        {
            Editor ed = AcApp.DocumentManager.MdiActiveDocument.Editor;
            string[] ds = {
                "PCCC_LIB     Nạp/cập nhật block PCCC từ PCCC_Blocks.dwg (Taomoi = cất bản cũ, tạo lại thư viện mặc định)",
                "PCCC_DAT     Đặt thiết bị theo mã vật tư (? = danh sách, gõ 1 phần tên/mã để tìm)",
                "PCCC_ONG     Vẽ ống/dây trên layer PCCC_<mã> (vd PCCC_ONG_DN50)",
                "PCCC_GAN     Gán block có sẵn trong bản vẽ -> mã vật tư (bóc được bản vẽ không vẽ bằng plugin)",
                "PCCC_BOC     Bóc khối lượng (tách theo tầng/khu nếu muốn), gửi lên web báo giá",
                "PCCC_BANG    Chèn bảng thống kê (kết quả PCCC_BOC gần nhất) vào bản vẽ",
                "PCCC_VUNGBV  Vẽ vùng bảo vệ bán kính R quanh thiết bị, báo thiết bị đứng lẻ" };
            ed.WriteMessage("\n[TKPCCC] Thư mục dữ liệu: " + ThuMuc);
            foreach (string s in ds) ed.WriteMessage("\n  " + s);
        }

        // ---------- Danh mục, tiện ích ----------

        static FileDanhMuc DocDanhMuc()
        {
            string f = Path.Combine(ThuMuc, "vat_tu.json");
            if (!File.Exists(f)) throw new FileNotFoundException("Thiếu " + f + " — chạy lại cad\\build.bat để chép danh mục vật tư.");
            return new JavaScriptSerializer().Deserialize<FileDanhMuc>(File.ReadAllText(f, Encoding.UTF8));
        }

        static VatTu TimMa(FileDanhMuc dm, string ma)
        {
            return dm.vat_tu.FirstOrDefault(v => string.Equals(v.ma, ma, StringComparison.OrdinalIgnoreCase));
        }

        /// Chữ thường không dấu để tìm "dau bao khoi" ra "Đầu báo khói".
        static string BoDau(string s)
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
        static double HeSoMet(Database db)
        {
            switch (db.Insunits)
            {
                case UnitsValue.Meters: return 1;
                case UnitsValue.Centimeters: return 0.01;
                default: return 0.001; // shortcut: coi mọi đơn vị khác là mm (bản vẽ VN thường mm); thêm đơn vị khi gặp
            }
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
                if (s == "?") { InDanhSach(ed, cung); continue; }
                VatTu v = cung.FirstOrDefault(x => string.Equals(x.ma, s, StringComparison.OrdinalIgnoreCase));
                if (v != null) return v;
                string k = BoDau(s);
                List<VatTu> tim = cung.Where(x => BoDau(x.ma).Contains(k) || BoDau(x.ten).Contains(k)).ToList();
                if (tim.Count == 1) { ed.WriteMessage("\n  -> " + tim[0].ma + "  " + tim[0].ten); return tim[0]; }
                if (tim.Count == 0) ed.WriteMessage("\nKhông có mã/tên nào chứa \"" + s + "\". Gõ ? để xem danh sách.");
                else { ed.WriteMessage("\nCó " + tim.Count + " mã khớp, gõ đúng mã:"); InDanhSach(ed, tim); }
            }
        }

        // ---------- Thư viện block ----------

        /// Block mặc định: vòng tròn R200 mm + ký hiệu + attribute MA_VT, TEN (ẩn). Người dùng sửa hình trong PCCC_Blocks.dwg.
        static ObjectId TaoBlockMacDinh(Transaction tr, BlockTable bt, ObjectId kieu, VatTu v)
        {
            BlockTableRecord b = new BlockTableRecord();
            b.Name = v.ma;
            b.Origin = Point3d.Origin;
            b.Units = UnitsValue.Millimeters; // INSERT tay vào bản vẽ m/cm thì AutoCAD tự đổi tỉ lệ
            if (!bt.IsWriteEnabled) bt.UpgradeOpen();
            ObjectId bid = bt.Add(b);
            tr.AddNewlyCreatedDBObject(b, true);

            Circle c = new Circle(Point3d.Origin, Vector3d.ZAxis, 200);
            b.AppendEntity(c); tr.AddNewlyCreatedDBObject(c, true);

            // MText căn giữa theo Location: không cần AdjustAlignment như DBText trong database phụ.
            string kh = v.ky_hieu ?? v.ma;
            MText t = new MText();
            t.Contents = kh;
            t.TextStyleId = kieu;
            t.TextHeight = kh.Length > 2 ? 90 : 140;
            t.Attachment = AttachmentPoint.MiddleCenter;
            t.Location = Point3d.Origin;
            b.AppendEntity(t); tr.AddNewlyCreatedDBObject(t, true);

            ThemAttDef(b, tr, kieu, TAG_MA, v.ma, new Point3d(0, -300, 0));
            ThemAttDef(b, tr, kieu, "TEN", v.ten, new Point3d(0, -450, 0));
            return bid;
        }

        static void ThemAttDef(BlockTableRecord b, Transaction tr, ObjectId kieu, string tag, string giaTri, Point3d p)
        {
            AttributeDefinition a = new AttributeDefinition(p, giaTri, tag, tag, kieu);
            a.Height = 60;
            a.Invisible = true;
            a.Preset = true;
            b.AppendEntity(a); tr.AddNewlyCreatedDBObject(a, true);
        }

        /// Tạo PCCC_Blocks.dwg nếu chưa có; có rồi thì chỉ thêm block mặc định cho mã mới trong vat_tu.json
        /// (block người dùng đã sửa giữ nguyên). Trả về số block thêm.
        static int DamBaoThuVien(FileDanhMuc dm)
        {
            bool moi = !File.Exists(FileThuVien);
            int them = 0;
            using (Database db = new Database(moi, true))
            {
                if (moi) db.Insunits = UnitsValue.Millimeters;
                else
                {
                    db.ReadDwgFile(FileThuVien, FileOpenMode.OpenForReadAndAllShare, true, "");
                    db.CloseInput(true); // đọc hết vào bộ nhớ, nhả file để ghi đè được
                }
                // Đối tượng mới lấy mặc định (kiểu chữ, layer) từ WorkingDatabase: phải trỏ sang database thư viện, không thì eWrongDatabase.
                Database dangLam = HostApplicationServices.WorkingDatabase;
                HostApplicationServices.WorkingDatabase = db;
                try
                {
                    using (Transaction tr = db.TransactionManager.StartTransaction())
                    {
                        BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                        List<VatTu> thieu = dm.vat_tu.Where(x => x.loai == "block" && !bt.Has(x.ma)).ToList();
                        if (thieu.Count > 0)
                        {
                            ObjectId kieu = DamBaoKieuChu(db, tr);
                            BlockTableRecord ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);
                            int i = ms.Cast<ObjectId>().Count(); // bày mẫu nối tiếp các mẫu đã có
                            foreach (VatTu v in thieu)
                            {
                                ObjectId bid = TaoBlockMacDinh(tr, bt, kieu, v);
                                // Bày mẫu trong model space để người dùng thấy và sửa hình.
                                BlockReference br = new BlockReference(new Point3d((i % 8) * 1000, -(i / 8) * 1000, 0), bid);
                                ms.AppendEntity(br); tr.AddNewlyCreatedDBObject(br, true);
                                i++; them++;
                            }
                        }
                        tr.Commit();
                    }
                }
                finally { HostApplicationServices.WorkingDatabase = dangLam; }
                if (them > 0) db.SaveAs(FileThuVien, DwgVersion.Current);
            }
            return them;
        }

        /// Chép định nghĩa block từ thư viện vào bản vẽ (Replace = cập nhật block đã có).
        static int NapBlock(Database dich, FileDanhMuc dm)
        {
            using (Database src = new Database(false, true))
            {
                src.ReadDwgFile(FileThuVien, FileOpenMode.OpenForReadAndAllShare, true, "");
                ObjectIdCollection ids = new ObjectIdCollection();
                using (Transaction tr = src.TransactionManager.StartTransaction())
                {
                    BlockTable bt = (BlockTable)tr.GetObject(src.BlockTableId, OpenMode.ForRead);
                    foreach (VatTu v in dm.vat_tu.Where(x => x.loai == "block"))
                        if (bt.Has(v.ma)) ids.Add(bt[v.ma]);
                    tr.Commit();
                }
                if (ids.Count > 0)
                    src.WblockCloneObjects(ids, dich.BlockTableId, new IdMapping(), DuplicateRecordCloning.Replace, false);
                return ids.Count;
            }
        }

        /// Đảm bảo thư viện đủ block rồi nạp vào bản vẽ hiện hành.
        static void NapVaoBanVe(Document doc, FileDanhMuc dm)
        {
            Editor ed = doc.Editor;
            bool moi = !File.Exists(FileThuVien);
            int them = DamBaoThuVien(dm);
            if (moi) ed.WriteMessage("\nĐã tạo thư viện block mặc định: " + FileThuVien + "\nMở file này sửa hình block (BEDIT), lưu lại rồi chạy PCCC_LIB để cập nhật.");
            else if (them > 0) ed.WriteMessage("\nĐã thêm " + them + " block mặc định cho mã mới vào " + FileThuVien);
            int n = NapBlock(doc.Database, dm);
            ed.WriteMessage("\nĐã nạp/cập nhật " + n + " block PCCC vào bản vẽ.\n");
            ed.Regen();
        }

        static void NapThuVien()
        {
            Document doc = AcApp.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;
            FileDanhMuc dm = DocDanhMuc();
            PromptKeywordOptions ko = new PromptKeywordOptions("\nThư viện block"); // AutoCAD tự nối " [Nap/Taomoi] <Nap>: "
            ko.Keywords.Add("Nap"); ko.Keywords.Add("Taomoi");
            ko.Keywords.Default = "Nap";
            ko.AllowNone = true;
            PromptResult kr = ed.GetKeywords(ko);
            if (kr.Status == PromptStatus.Cancel) return;
            if (kr.StringResult == "Taomoi" && File.Exists(FileThuVien))
            {
                // Cất bản cũ (có thể đã sửa hình) thay vì xoá.
                string cu = Path.Combine(ThuMuc, "PCCC_Blocks_cu_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".dwg");
                File.Move(FileThuVien, cu);
                ed.WriteMessage("\nĐã cất thư viện cũ: " + cu);
            }
            NapVaoBanVe(doc, dm);
        }

        // ---------- Vẽ ----------

        static void DatThietBi()
        {
            Document doc = AcApp.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;
            FileDanhMuc dm = DocDanhMuc();
            VatTu v = HoiMa(ed, dm, "block", "Mã thiết bị");
            if (v == null) return;

            bool co;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                co = ((BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead)).Has(v.ma);
                tr.Commit();
            }
            if (!co) NapVaoBanVe(doc, dm);
            double k = 0.001 / HeSoMet(db); // block thư viện vẽ theo mm

            while (true)
            {
                PromptPointOptions o = new PromptPointOptions("\nĐiểm đặt " + v.ten + " <Enter = xong>: ");
                o.AllowNone = true;
                PromptPointResult r = ed.GetPoint(o);
                if (r.Status != PromptStatus.OK) return;
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    ChenThietBi(db, tr, v, r.Value.TransformBy(ed.CurrentUserCoordinateSystem), k, 0); // cad/module/plugin/Spec.cs
                    tr.Commit();
                }
            }
        }

        static void VeOng()
        {
            Document doc = AcApp.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            FileDanhMuc dm = DocDanhMuc();
            VatTu v = HoiMa(doc.Editor, dm, "tuyen", "Mã ống/dây (vd ONG_DN50, DAY_BC)");
            if (v == null) return;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                db.Clayer = DamBaoLayer(db, tr, TIEN_TO_LAYER + v.ma, MauHe(v.he));
                tr.Commit();
            }
            doc.Editor.WriteMessage("\nLayer hiện hành: " + TIEN_TO_LAYER + v.ma + ". Vẽ polyline tuyến " + v.ten + ".");
            doc.SendStringToExecute("_.PLINE ", true, false, false);
        }

        // ---------- Bóc khối lượng ----------
        // Ý tưởng tham khảo LHB Block Scheduler (chỉ tính năng, không dùng mã): không đếm trùng vị trí, đếm block lồng
        // và MINSERT, bỏ XREF, tách theo tầng/khu vực, gán tên block có sẵn -> mã vật tư, xuất bảng Table, vùng bảo vệ.

        const int SAU_TOI_DA = 4; // độ sâu block lồng được quét
        const string KHU_NGOAI = "Ngoài khu vực";

        // ----- Gán tên block có sẵn (bản vẽ không vẽ bằng plugin) -> MA_VT, lưu block_alias.json cạnh DLL -----

        static string FileAlias { get { return Path.Combine(ThuMuc, "block_alias.json"); } }

        static Dictionary<string, string> DocAlias()
        {
            Dictionary<string, string> a = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (!File.Exists(FileAlias)) return a;
            foreach (KeyValuePair<string, string> p in new JavaScriptSerializer().Deserialize<Dictionary<string, string>>(File.ReadAllText(FileAlias, Encoding.UTF8)))
                a[p.Key] = p.Value;
            return a;
        }

        static string TenBlock(BlockReference br, Transaction tr)
        {
            ObjectId bid = br.IsDynamicBlock ? br.DynamicBlockTableRecord : br.BlockTableRecord;
            return ((BlockTableRecord)tr.GetObject(bid, OpenMode.ForRead)).Name;
        }

        static void GanBlock()
        {
            Document doc = AcApp.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;
            FileDanhMuc dm = DocDanhMuc();
            Dictionary<string, string> alias = DocAlias();
            while (true)
            {
                PromptEntityOptions o = new PromptEntityOptions("\nChọn block thiết bị có sẵn <Enter = xong>: ");
                o.SetRejectMessage("\nPhải chọn block.");
                o.AddAllowedClass(typeof(BlockReference), false);
                o.AllowNone = true;
                PromptEntityResult r = ed.GetEntity(o);
                if (r.Status != PromptStatus.OK) break;
                string ten;
                using (Transaction tr = doc.Database.TransactionManager.StartTransaction())
                {
                    DBObject o2 = tr.GetObject(r.ObjectId, OpenMode.ForRead);
                    ten = o2 is Table ? null : TenBlock((BlockReference)o2, tr);
                    tr.Commit();
                }
                if (ten == null) { ed.WriteMessage("\nĐó là bảng, không phải block thiết bị."); continue; }
                string cu;
                if (alias.TryGetValue(ten, out cu)) ed.WriteMessage("\nBlock \"" + ten + "\" đang gán " + cu + ".");
                VatTu v = HoiMa(ed, dm, "block", "Mã vật tư cho block \"" + ten + "\"");
                if (v == null) continue;
                alias[ten] = v.ma;
                File.WriteAllText(FileAlias, new JavaScriptSerializer().Serialize(alias), Encoding.UTF8);
                ed.WriteMessage("\nĐã gán \"" + ten + "\" = " + v.ma + " (" + v.ten + "). Lưu ở " + FileAlias);
            }
        }

        // ----- Khu vực (tầng) -----

        class Khu
        {
            public string Ten;
            public List<Point2d> Dinh; // polyline kín
            public Point2d Tam;         // hoặc hình tròn
            public double R;

            public bool Chua(Point3d p)
            {
                if (Dinh == null) return Tam.GetDistanceTo(new Point2d(p.X, p.Y)) <= R;
                bool trong = false; // ray casting; shortcut: bỏ qua cung (bulge) của polyline
                for (int i = 0, j = Dinh.Count - 1; i < Dinh.Count; j = i++)
                {
                    Point2d a = Dinh[i], b = Dinh[j];
                    if ((a.Y > p.Y) != (b.Y > p.Y) && p.X < (b.X - a.X) * (p.Y - a.Y) / (b.Y - a.Y) + a.X) trong = !trong;
                }
                return trong;
            }
        }

        /// Chọn đường bao kín quanh từng tầng/khu; tên = chữ cao nhất nằm trong đường bao.
        static List<Khu> ChonKhu(Editor ed, Database db)
        {
            PromptSelectionOptions o = new PromptSelectionOptions();
            o.MessageForAdding = "\nChọn các đường bao kín (polyline/circle) quanh từng tầng/khu vực: ";
            SelectionFilter f = new SelectionFilter(new TypedValue[] {
                new TypedValue((int)DxfCode.Operator, "<OR"),
                new TypedValue((int)DxfCode.Start, "LWPOLYLINE"), new TypedValue((int)DxfCode.Start, "CIRCLE"),
                new TypedValue((int)DxfCode.Operator, "OR>") });
            PromptSelectionResult r = ed.GetSelection(o, f);
            List<Khu> ds = new List<Khu>();
            if (r.Status != PromptStatus.OK) return ds;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                foreach (ObjectId id in r.Value.GetObjectIds())
                {
                    Entity e = (Entity)tr.GetObject(id, OpenMode.ForRead);
                    Polyline pl = e as Polyline;
                    Circle c = e as Circle;
                    Khu k = new Khu();
                    if (pl != null)
                    {
                        if (pl.NumberOfVertices < 3) continue;
                        if (!pl.Closed && pl.GetPoint2dAt(0).GetDistanceTo(pl.GetPoint2dAt(pl.NumberOfVertices - 1)) > 1e-6) continue;
                        k.Dinh = new List<Point2d>();
                        for (int i = 0; i < pl.NumberOfVertices; i++) k.Dinh.Add(pl.GetPoint2dAt(i));
                    }
                    else { k.Tam = new Point2d(c.Center.X, c.Center.Y); k.R = c.Radius; }
                    ds.Add(k);
                }
                // Đặt tên theo chữ cao nhất bên trong.
                double[] cao = new double[ds.Count];
                BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                foreach (ObjectId id in (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead))
                {
                    Entity e = tr.GetObject(id, OpenMode.ForRead) as Entity;
                    string chu = null; double h = 0; Point3d p = Point3d.Origin;
                    DBText t = e as DBText; MText m = e as MText;
                    if (t != null) { chu = t.TextString; h = t.Height; p = t.Position; }
                    else if (m != null) { chu = m.Text; h = m.TextHeight; p = m.Location; }
                    if (string.IsNullOrWhiteSpace(chu)) continue;
                    for (int i = 0; i < ds.Count; i++)
                        if (h > cao[i] && ds[i].Chua(p)) { cao[i] = h; ds[i].Ten = chu.Trim(); }
                }
                tr.Commit();
            }
            if (r.Value.Count > ds.Count) ed.WriteMessage("\nBỏ " + (r.Value.Count - ds.Count) + " polyline không kín.");
            for (int i = 0; i < ds.Count; i++) if (ds[i].Ten == null) ds[i].Ten = "Khu " + (i + 1);
            // Trùng tên thì thêm số thứ tự để không gộp nhầm.
            HashSet<string> da = new HashSet<string>();
            foreach (Khu k in ds) { string t = k.Ten; int n = 2; while (!da.Add(k.Ten)) k.Ten = t + " (" + n++ + ")"; }
            return ds;
        }

        // ----- Quét -----

        class KetQua
        {
            public Dictionary<string, double> Tong = new Dictionary<string, double>();
            public Dictionary<string, Dictionary<string, double>> TheoKhu = new Dictionary<string, Dictionary<string, double>>();
            public List<string> TenKhu = new List<string>();
            public HashSet<string> ViTri = new HashSet<string>();
            public int Trung, NgoaiDanhMuc, QuaSau;
        }

        class BoiCanh
        {
            public Transaction Tr;
            public FileDanhMuc Dm;
            public Dictionary<string, string> Alias;
            public List<Khu> Khu;
            public double HeSo;
            public KetQua Kq;
        }

        static void Cong(Dictionary<string, double> kl, string ma, double sl)
        {
            double cu;
            kl.TryGetValue(ma, out cu);
            kl[ma] = cu + sl;
        }

        static void Ghi(BoiCanh bc, string ma, double sl, Point3d p)
        {
            Cong(bc.Kq.Tong, ma, sl);
            if (bc.Khu.Count == 0) return;
            Khu k = bc.Khu.FirstOrDefault(x => x.Chua(p));
            string ten = k != null ? k.Ten : KHU_NGOAI;
            if (!bc.Kq.TheoKhu.ContainsKey(ten)) bc.Kq.TheoKhu[ten] = new Dictionary<string, double>();
            Cong(bc.Kq.TheoKhu[ten], ma, sl);
        }

        /// Mã thiết bị của block: attribute MA_VT > tên block là mã trong danh mục > tên block đã gán (PCCC_GAN). null = không phải thiết bị.
        static string MaThietBi(BlockReference br, BoiCanh bc)
        {
            foreach (ObjectId id in br.AttributeCollection)
            {
                AttributeReference ar = bc.Tr.GetObject(id, OpenMode.ForRead) as AttributeReference;
                if (ar != null && ar.Tag.ToUpperInvariant() == TAG_MA && ar.TextString.Trim() != "") return ar.TextString.Trim();
            }
            string ten = TenBlock(br, bc.Tr);
            VatTu v = TimMa(bc.Dm, ten);
            if (v != null && v.loai == "block") return v.ma;
            string ma;
            return bc.Alias.TryGetValue(ten, out ma) ? ma : null;
        }

        /// nhan = số lần lặp của các MINSERT cha (block nằm trong MINSERT 3x4 được đếm 12 lần).
        static void Duyet(Entity e, Matrix3d tf, int sau, int nhan, BoiCanh bc)
        {
            if (!e.Visible) return; // ẩn theo visibility của dynamic block cha
            if (e is Table) return; // Table kế thừa BlockReference: ký hiệu trong bảng thống kê không phải thiết bị
            BlockReference br = e as BlockReference;
            if (br != null)
            {
                BlockTableRecord btr = (BlockTableRecord)bc.Tr.GetObject(br.BlockTableRecord, OpenMode.ForRead);
                if (btr.IsFromExternalReference || btr.IsDependent) return; // bỏ XREF (cửa, nội thất của kiến trúc)
                MInsertBlock mi = br as MInsertBlock;
                int lap = nhan * (mi != null ? mi.Rows * mi.Columns : 1);
                string ma = MaThietBi(br, bc);
                Point3d p = br.Position.TransformBy(tf);
                if (ma != null)
                {
                    if (lap == 1)
                    {
                        // Không đếm trùng: cùng mã, cùng điểm chèn (sai số 1 mm) = bản copy đè.
                        double tol = 0.001 / bc.HeSo;
                        string key = ma + "|" + Math.Round(p.X / tol) + "|" + Math.Round(p.Y / tol) + "|" + Math.Round(p.Z / tol);
                        if (!bc.Kq.ViTri.Add(key)) { bc.Kq.Trung++; return; }
                    }
                    if (TimMa(bc.Dm, ma) == null) bc.Kq.NgoaiDanhMuc += lap;
                    Ghi(bc, ma, lap, p);
                    return;
                }
                if (sau >= SAU_TOI_DA) { bc.Kq.QuaSau++; return; }
                Matrix3d tf2 = tf * br.BlockTransform;
                foreach (ObjectId id in btr)
                {
                    Entity con = bc.Tr.GetObject(id, OpenMode.ForRead) as Entity;
                    if (con != null) Duyet(con, tf2, sau + 1, lap, bc);
                }
                return;
            }
            Curve c = e as Curve;
            if (c == null || !e.Layer.StartsWith(TIEN_TO_LAYER, StringComparison.OrdinalIgnoreCase)) return;
            VatTu v = TimMa(bc.Dm, e.Layer.Substring(TIEN_TO_LAYER.Length));
            if (v == null || v.loai != "tuyen") return;
            double dai;
            try { dai = c.GetDistanceAtParameter(c.EndParam) - c.GetDistanceAtParameter(c.StartParam); }
            catch (Autodesk.AutoCAD.Runtime.Exception) { return; } // đường suy biến (độ dài 0), xline/ray
            // Đoạn tính vào khu chứa trung điểm.
            Ghi(bc, v.ma, nhan * dai * bc.HeSo * Math.Abs(tf.GetScale()), c.GetPointAtDist(dai / 2).TransformBy(tf));
        }

        static void BocKhoiLuong()
        {
            Document doc = AcApp.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            PromptKeywordOptions ko = new PromptKeywordOptions("\nTách khối lượng theo tầng/khu vực?");
            ko.Keywords.Add("Co"); ko.Keywords.Add("Khong");
            ko.Keywords.Default = "Khong";
            ko.AllowNone = true;
            PromptResult kr = ed.GetKeywords(ko);
            if (kr.Status == PromptStatus.Cancel) return;
            List<Khu> khu = kr.StringResult == "Co" ? ChonKhu(ed, db) : new List<Khu>();

            PromptSelectionOptions so = new PromptSelectionOptions();
            so.MessageForAdding = "\nChọn vùng bóc khối lượng <Enter = cả model space>: ";
            PromptSelectionResult sr = ed.GetSelection(so);
            if (sr.Status == PromptStatus.Cancel) return;

            BoiCanh bc = new BoiCanh();
            bc.Dm = DocDanhMuc();
            bc.Alias = DocAlias();
            bc.Khu = khu;
            bc.HeSo = HeSoMet(db);
            bc.Kq = new KetQua();
            bc.Kq.TenKhu = khu.Select(k => k.Ten).ToList();
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                bc.Tr = tr;
                IEnumerable<ObjectId> ids;
                if (sr.Status == PromptStatus.OK) ids = sr.Value.GetObjectIds();
                else
                {
                    BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                    ids = ((BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead)).Cast<ObjectId>().ToList();
                }
                foreach (ObjectId id in ids)
                {
                    Entity e = tr.GetObject(id, OpenMode.ForRead) as Entity;
                    if (e != null) Duyet(e, Matrix3d.Identity, 0, 1, bc);
                }
                tr.Commit();
            }
            KetQua kq = bc.Kq;
            if (kq.TheoKhu.ContainsKey(KHU_NGOAI)) kq.TenKhu.Add(KHU_NGOAI);

            if (kq.Tong.Count == 0) { ed.WriteMessage("\nKhông tìm thấy thiết bị/ống PCCC nào. Vẽ bằng PCCC_DAT / PCCC_ONG, hoặc gán block có sẵn bằng PCCC_GAN."); return; }
            if (db.Insunits != UnitsValue.Millimeters && db.Insunits != UnitsValue.Meters && db.Insunits != UnitsValue.Centimeters)
                ed.WriteMessage("\nChú ý: INSUNITS không phải mm/cm/m — chiều dài ống tính theo giả định bản vẽ đơn vị mm.");

            ed.WriteMessage("\n--- KHỐI LƯỢNG ---");
            foreach (KeyValuePair<string, double> p in kq.Tong.OrderBy(x => x.Key))
            {
                VatTu v = TimMa(bc.Dm, p.Key);
                ed.WriteMessage("\n  " + p.Key.PadRight(16) + So(p.Value).PadLeft(10) + " " + (v != null ? v.dvt + "  " + v.ten : "(ngoài danh mục)"));
            }
            foreach (string t in kq.TenKhu)
                ed.WriteMessage("\n  [" + t + "] " + string.Join(", ", kq.TheoKhu.ContainsKey(t) ? kq.TheoKhu[t].OrderBy(x => x.Key).Select(x => x.Key + "=" + So(x.Value)).ToArray() : new string[0]));
            if (kq.Trung > 0) ed.WriteMessage("\nĐã bỏ " + kq.Trung + " block trùng vị trí (copy đè).");
            if (kq.NgoaiDanhMuc > 0) ed.WriteMessage("\nChú ý: " + kq.NgoaiDanhMuc + " block có MA_VT ngoài danh mục vat_tu.json.");
            if (kq.QuaSau > 0) ed.WriteMessage("\nChú ý: " + kq.QuaSau + " block lồng sâu hơn " + SAU_TOI_DA + " tầng chưa được quét.");
            doc.UserData["TKPCCC_KQ"] = kq; // cho PCCC_BANG

            string banVe = Path.GetFileNameWithoutExtension(doc.Name);
            PromptStringOptions po = new PromptStringOptions("\nTên dự án gửi lên web"); // AutoCAD tự nối " <mặc định>: "
            po.AllowSpaces = true;
            po.DefaultValue = banVe;
            po.UseDefaultValue = true;
            PromptResult pr = ed.GetString(po);
            if (pr.Status != PromptStatus.OK) return;
            string duAn = pr.StringResult.Trim() == "" ? banVe : pr.StringResult.Trim();

            Dictionary<string, object> goi = new Dictionary<string, object>();
            goi["du_an"] = duAn;
            goi["ban_ve"] = Path.GetFileName(doc.Name);
            goi["luc"] = DateTime.Now.ToString("o");
            goi["dong"] = DongJson(kq.Tong);
            if (kq.TenKhu.Count > 0)
                goi["khu_vuc"] = kq.TenKhu.Select(t => new Dictionary<string, object> { { "ten", t }, { "dong", DongJson(kq.TheoKhu.ContainsKey(t) ? kq.TheoKhu[t] : new Dictionary<string, double>()) } }).ToList();
            string json = new JavaScriptSerializer().Serialize(goi);

            string url = (Environment.GetEnvironmentVariable("TKPCCC_URL") ?? "http://127.0.0.1:8787") + "/api/khoi-luong";
            try
            {
                using (WebClient wc = new WebClient())
                {
                    wc.Encoding = Encoding.UTF8;
                    wc.Headers[HttpRequestHeader.ContentType] = "application/json";
                    wc.UploadString(url, "POST", json);
                }
                ed.WriteMessage("\nĐã gửi khối lượng lên " + url + " — mở web để xem báo giá. PCCC_BANG để chèn bảng vào bản vẽ.");
            }
            catch (WebException ex)
            {
                // Runner không chạy: ghi file cạnh bản vẽ để không mất kết quả.
                string thuMuc = Path.IsPathRooted(doc.Name) ? Path.GetDirectoryName(doc.Name) : Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                string f = Path.Combine(thuMuc, banVe + "_KL.json");
                File.WriteAllText(f, json, Encoding.UTF8);
                ed.WriteMessage("\nKhông gửi được tới runner (" + ex.Message + "). Đã lưu " + f + " — bật runner (npm start) rồi chạy lại PCCC_BOC.");
            }
        }

        static List<Dictionary<string, object>> DongJson(Dictionary<string, double> kl)
        {
            return kl.OrderBy(x => x.Key).Select(x => new Dictionary<string, object> { { "ma", x.Key }, { "sl", Math.Round(x.Value, 3) } }).ToList();
        }

        static string So(double x)
        {
            return Math.Round(x, 2).ToString("#,##0.##", CultureInfo.GetCultureInfo("vi-VN"));
        }

        // ----- Bảng thống kê trên bản vẽ -----

        static void ChenBang()
        {
            Document doc = AcApp.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;
            KetQua kq = doc.UserData["TKPCCC_KQ"] as KetQua;
            if (kq == null) { ed.WriteMessage("\nChưa có kết quả: chạy PCCC_BOC trước."); return; }
            FileDanhMuc dm = DocDanhMuc();

            PromptPointResult pp = ed.GetPoint("\nĐiểm chèn bảng (góc trên trái): ");
            if (pp.Status != PromptStatus.OK) return;
            PromptDoubleOptions ho = new PromptDoubleOptions("\nChiều cao chữ (đơn vị bản vẽ): ");
            ho.DefaultValue = 0.25 / HeSoMet(db); // chữ 2,5 mm khi in tỉ lệ 1/100
            ho.UseDefaultValue = true;
            ho.AllowNegative = false; ho.AllowZero = false;
            PromptDoubleResult hr = ed.GetDouble(ho);
            if (hr.Status != PromptStatus.OK) return;
            double h = hr.Value;

            // Thứ tự dòng theo danh mục (nhóm theo hệ), mã ngoài danh mục ở cuối.
            List<string> ma = dm.vat_tu.Select(v => v.ma).Where(m => kq.Tong.ContainsKey(m)).ToList();
            ma.AddRange(kq.Tong.Keys.Where(m => !ma.Contains(m)).OrderBy(m => m));
            bool coKhu = kq.TenKhu.Count > 0;
            int nCot = 4 + (coKhu ? kq.TenKhu.Count : 0) + 1;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                ObjectId kieu = DamBaoKieuChu(db, tr);
                Table t = new Table();
                t.TableStyle = db.Tablestyle;
                t.Position = pp.Value.TransformBy(ed.CurrentUserCoordinateSystem);
                t.SetSize(ma.Count + 2, nCot);
                try { t.MergeCells(CellRange.Create(t, 0, 0, 0, nCot - 1)); }
                catch (Autodesk.AutoCAD.Runtime.Exception) { } // kiểu bảng đã tự gộp dòng tiêu đề
                t.Cells[0, 0].TextString = "BẢNG THỐNG KÊ THIẾT BỊ, VẬT TƯ PCCC";
                string[] dau = new string[] { "STT", "Ký hiệu", "Tên vật tư", "ĐVT" };
                for (int c = 0; c < 4; c++) t.Cells[1, c].TextString = dau[c];
                for (int k = 0; coKhu && k < kq.TenKhu.Count; k++) t.Cells[1, 4 + k].TextString = kq.TenKhu[k];
                t.Cells[1, nCot - 1].TextString = coKhu ? "Tổng" : "Số lượng";
                for (int i = 0; i < ma.Count; i++)
                {
                    int r = i + 2;
                    VatTu v = TimMa(dm, ma[i]);
                    t.Cells[r, 0].TextString = (i + 1).ToString();
                    if (bt.Has(ma[i]))
                    {
                        t.Cells[r, 1].BlockTableRecordId = bt[ma[i]];
                        t.Cells[r, 1].Contents[0].IsAutoScale = true; // ký hiệu co theo ô
                    }
                    else t.Cells[r, 1].TextString = v != null && v.loai == "tuyen" ? "—" : ma[i];
                    t.Cells[r, 2].TextString = " " + (v != null ? v.ten : ma[i]); // cách mép trái ô (lề mặc định của kiểu bảng rất nhỏ)
                    t.Cells[r, 3].TextString = v != null ? v.dvt : "";
                    for (int k = 0; coKhu && k < kq.TenKhu.Count; k++)
                    {
                        double sl;
                        Dictionary<string, double> d;
                        if (kq.TheoKhu.TryGetValue(kq.TenKhu[k], out d) && d.TryGetValue(ma[i], out sl)) t.Cells[r, 4 + k].TextString = So(sl);
                    }
                    t.Cells[r, nCot - 1].TextString = So(kq.Tong[ma[i]]);
                }
                for (int r = 0; r < ma.Count + 2; r++)
                {
                    t.Rows[r].Height = h * 2.2;
                    for (int c = 0; c < nCot; c++)
                    {
                        t.Cells[r, c].TextStyleId = kieu;
                        t.Cells[r, c].TextHeight = h;
                        t.Cells[r, c].Alignment = c == 2 && r > 1 ? CellAlignment.MiddleLeft : CellAlignment.MiddleCenter;
                    }
                }
                double[] rong = new double[] { 4, 6, 26, 5 };
                for (int c = 0; c < nCot; c++) t.Columns[c].Width = h * (c < 4 ? rong[c] : 7);
                t.GenerateLayout();
                BlockTableRecord space = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);
                space.AppendEntity(t); tr.AddNewlyCreatedDBObject(t, true);
                tr.Commit();
            }
        }

        // ----- Vùng bảo vệ -----

        static void VungBaoVe()
        {
            Document doc = AcApp.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;
            FileDanhMuc dm = DocDanhMuc();
            VatTu v = HoiMa(ed, dm, "block", "Mã thiết bị cần vẽ vùng bảo vệ (vd BC_KHOI, SPR_DAU_DOWN)");
            if (v == null) return;
            // Plugin không tra tiêu chuẩn: bán kính do người thiết kế nhập theo TCVN đang áp dụng.
            PromptDoubleOptions ro = new PromptDoubleOptions("\nBán kính bảo vệ R (m) theo tiêu chuẩn áp dụng: ");
            ro.AllowNegative = false; ro.AllowZero = false;
            PromptDoubleResult rr = ed.GetDouble(ro);
            if (rr.Status != PromptStatus.OK) return;
            double r = rr.Value / HeSoMet(db);
            string tenLop = "PCCC_VUNGBV_" + v.ma; // mỗi loại 1 layer: chạy lại chỉ thay vòng của loại đó

            BoiCanh bc = new BoiCanh();
            bc.Dm = dm; bc.Alias = DocAlias();
            List<Point3d> tam = new List<Point3d>();
            int xoa = 0;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                bc.Tr = tr;
                BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                BlockTableRecord ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);
                ObjectId lop = DamBaoLayer(db, tr, tenLop, 8);
                ((LayerTableRecord)tr.GetObject(lop, OpenMode.ForWrite)).IsPlottable = false;
                double tol = 0.001 / HeSoMet(db);
                HashSet<string> daCo = new HashSet<string>(); // bản copy đè cùng điểm chỉ vẽ 1 vòng
                foreach (ObjectId id in ms.Cast<ObjectId>().ToList())
                {
                    Entity e = tr.GetObject(id, OpenMode.ForRead) as Entity;
                    BlockReference br = e as BlockReference;
                    if (br != null && MaThietBi(br, bc) == v.ma)
                    {
                        if (daCo.Add(Math.Round(br.Position.X / tol) + "|" + Math.Round(br.Position.Y / tol))) tam.Add(br.Position);
                    }
                    else if (e is Circle && e.LayerId == lop) { e.UpgradeOpen(); e.Erase(); xoa++; }
                }
                foreach (Point3d p in tam)
                {
                    Circle c = new Circle(p, Vector3d.ZAxis, r);
                    c.LayerId = lop;
                    ms.AppendEntity(c); tr.AddNewlyCreatedDBObject(c, true);
                }
                tr.Commit();
            }
            // Khoảng hở: thiết bị không có thiết bị cùng loại nào trong phạm vi 2R (vùng bảo vệ không chạm nhau).
            int le = tam.Count(p => tam.Count(q => q != p && p.DistanceTo(q) <= 2 * r) == 0);
            ed.WriteMessage("\nĐã vẽ " + tam.Count + " vùng bảo vệ R = " + rr.Value + " m trên layer " + tenLop + " (không in)"
                + (xoa > 0 ? ", thay " + xoa + " vòng cũ" : "") + "."
                + (le > 0 && tam.Count > 1 ? " Có " + le + " thiết bị đứng lẻ (cách thiết bị gần nhất > 2R) — kiểm tra khoảng hở." : ""));
        }
    }
}
