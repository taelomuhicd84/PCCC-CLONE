// Gán mã block, bóc khối lượng, bảng thống kê (PCCC_GAN, PCCC_BOC, PCCC_BANG). Tách từ Lenh.cs (26/11/2026: mỗi thư mục số của 07_CAD là một hạng mục tính năng, xem 07_CAD/README.md).
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

namespace TKPCCC
{
    public partial class Lenh
    {
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

        /// Chủng loại của block động = trạng thái Visibility hiện tại (BÌNH CHỮA CHÁY: "BỘT ABC 8KG", ĐÈN EXIT: "2 HƯỚNG"); không có Visibility
        /// thì tham số chữ có danh sách giá trị (Lookup). Độ dài, góc, lật, toạ độ không làm chủng loại. null = block thường hoặc không có tham số đó.
        static string ChungLoai(BlockReference br)
        {
            if (!br.IsDynamicBlock) return null;
            string kq = null;
            foreach (DynamicBlockReferenceProperty p in br.DynamicBlockReferencePropertyCollection)
            {
                string v = p.Value as string;
                if (string.IsNullOrWhiteSpace(v) || p.GetAllowedValues().Length == 0) continue;
                // .NET không có cờ "đây là Visibility": nhận theo tên mặc định Visibility, Visibility1... (tên đổi tay thì rơi vào nhánh Lookup, vẫn ra chủng loại)
                if (p.PropertyName.StartsWith("Visibility", StringComparison.OrdinalIgnoreCase)) return v.Trim();
                if (kq == null) kq = v.Trim();
            }
            return kq;
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
                string ten, loai = null;
                using (Transaction tr = doc.Database.TransactionManager.StartTransaction())
                {
                    DBObject o2 = tr.GetObject(r.ObjectId, OpenMode.ForRead);
                    ten = o2 is Table ? null : TenBlock((BlockReference)o2, tr);
                    if (ten != null) loai = ChungLoai((BlockReference)o2);
                    tr.Commit();
                }
                if (ten == null) { ed.WriteMessage("\nĐó là bảng, không phải block thiết bị."); continue; }
                // Block động nhiều trạng thái (bình 4 kg / 8 kg...): gán riêng trạng thái đang chọn thì khoá "tên|trạng thái" (MaThietBi xét trước khoá tên).
                if (loai != null)
                {
                    PromptKeywordOptions ko = new PromptKeywordOptions("\nBlock động đang ở trạng thái \"" + loai + "\". Gán mã Rieng trạng thái này hay Chung mọi trạng thái?");
                    ko.Keywords.Add("Rieng"); ko.Keywords.Add("Chung");
                    ko.Keywords.Default = "Rieng";
                    ko.AllowNone = true;
                    PromptResult kr = ed.GetKeywords(ko);
                    if (kr.Status == PromptStatus.Cancel) continue;
                    if (kr.StringResult == "Chung") loai = null;
                }
                string khoa = loai == null ? ten : ten + "|" + loai, cu;
                if (alias.TryGetValue(khoa, out cu)) ed.WriteMessage("\nBlock \"" + khoa + "\" đang gán " + cu + ".");
                VatTu v = HoiMa(ed, dm, "block", "Mã vật tư cho block \"" + khoa + "\"");
                if (v == null) continue;
                alias[khoa] = v.ma;
                File.WriteAllText(FileAlias, new JavaScriptSerializer().Serialize(alias), Encoding.UTF8);
                ed.WriteMessage("\nĐã gán \"" + khoa + "\" = " + v.ma + " (" + v.ten + "). Lưu ở " + FileAlias);
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
            /// Dòng tách chủng loại block động: khoá DongKhoa(mã, chủng loại). Tong, TheoKhu giữ theo mã cho web, sơ đồ, bảng PCCC_BLOCK.
            public Dictionary<string, double> Dong = new Dictionary<string, double>();
            public Dictionary<string, Dictionary<string, double>> DongKhu = new Dictionary<string, Dictionary<string, double>>();
        }

        const char TACH_LOAI = '\t'; // mã \t chủng loại ("" = block thường, ống)
        static string DongKhoa(string ma, string loai) { return ma + TACH_LOAI + (loai ?? ""); }
        static string MaCuaDong(string dong) { return dong.Substring(0, dong.IndexOf(TACH_LOAI)); }
        static string LoaiCuaDong(string dong) { return dong.Substring(dong.IndexOf(TACH_LOAI) + 1); }

        class BoiCanh
        {
            public Transaction Tr;
            public FileDanhMuc Dm;
            public Dictionary<string, string> Alias;
            public List<Khu> Khu;
            public double HeSo;
            public KetQua Kq;
            /// Định nghĩa block -> có thể chứa thiết bị / nét PCCC_ không (review 15/11: không duyệt lại nội thất cho từng bản chèn).
            public Dictionary<ObjectId, bool> CoTb = new Dictionary<ObjectId, bool>();
            HashSet<string> tenAliasLoai;
            /// Tên block có alias riêng theo chủng loại ("tên|trạng thái"): chỉ những block này mới phải đọc tham số động trong MaThietBi.
            public bool CoAliasLoai(string ten)
            {
                if (tenAliasLoai == null)
                    tenAliasLoai = new HashSet<string>(Alias.Keys.Where(k => k.IndexOf('|') > 0).Select(k => k.Substring(0, k.IndexOf('|'))), StringComparer.OrdinalIgnoreCase);
                return tenAliasLoai.Contains(ten);
            }
        }

        /// Block bid (kể cả block lồng) có thể chứa thứ được đếm: bản chèn mang mã thiết bị hoặc Curve trên layer PCCC_. Nhớ theo định nghĩa.
        static bool CoThietBi(ObjectId bid, BoiCanh bc)
        {
            bool co;
            if (bc.CoTb.TryGetValue(bid, out co)) return co;
            bc.CoTb[bid] = true; // đang xét (block không tự lồng chính nó); lỗi giữa chừng thì vẫn duyệt như cũ
            co = false;
            foreach (ObjectId id in (BlockTableRecord)bc.Tr.GetObject(bid, OpenMode.ForRead))
            {
                Entity e = bc.Tr.GetObject(id, OpenMode.ForRead) as Entity;
                BlockReference br = e as BlockReference;
                if (br != null) { if (!(br is Table) && (MaThietBi(br, bc) != null || CoThietBi(br.BlockTableRecord, bc))) { co = true; break; } }
                else if (e is Curve && e.Layer.StartsWith(TIEN_TO_LAYER, StringComparison.OrdinalIgnoreCase)) { co = true; break; }
            }
            bc.CoTb[bid] = co;
            return co;
        }

        static void Cong(Dictionary<string, double> kl, string ma, double sl)
        {
            double cu;
            kl.TryGetValue(ma, out cu);
            kl[ma] = cu + sl;
        }

        static void Ghi(BoiCanh bc, string ma, double sl, Point3d p, string loai)
        {
            Cong(bc.Kq.Tong, ma, sl);
            Cong(bc.Kq.Dong, DongKhoa(ma, loai), sl);
            if (bc.Khu.Count == 0) return;
            Khu k = bc.Khu.FirstOrDefault(x => x.Chua(p));
            string ten = k != null ? k.Ten : KHU_NGOAI;
            if (!bc.Kq.TheoKhu.ContainsKey(ten)) { bc.Kq.TheoKhu[ten] = new Dictionary<string, double>(); bc.Kq.DongKhu[ten] = new Dictionary<string, double>(); }
            Cong(bc.Kq.TheoKhu[ten], ma, sl);
            Cong(bc.Kq.DongKhu[ten], DongKhoa(ma, loai), sl);
        }

        /// Mã thiết bị của block: attribute MA_VT > gán riêng trạng thái block động ("tên|chủng loại", PCCC_GAN Rieng) > tên block là mã
        /// trong danh mục > tên block đã gán (PCCC_GAN). null = không phải thiết bị.
        static string MaThietBi(BlockReference br, BoiCanh bc)
        {
            foreach (ObjectId id in br.AttributeCollection)
            {
                AttributeReference ar = bc.Tr.GetObject(id, OpenMode.ForRead) as AttributeReference;
                if (ar != null && ar.Tag.ToUpperInvariant() == TAG_MA && ar.TextString.Trim() != "") return ar.TextString.Trim();
            }
            string ten = TenBlock(br, bc.Tr), ma;
            if (br.IsDynamicBlock && bc.CoAliasLoai(ten))
            {
                string loai = ChungLoai(br);
                if (loai != null && bc.Alias.TryGetValue(ten + "|" + loai, out ma)) return ma;
            }
            VatTu v = TimMa(bc.Dm, ten);
            if (v != null && v.loai == "block") return v.ma;
            return bc.Alias.TryGetValue(ten, out ma) ? ma : null;
        }

        /// nhan = số lần lặp của các MINSERT cha (block nằm trong MINSERT 3x4 được đếm 12 lần).
        static void Duyet(Entity e, Matrix3d tf, int sau, int nhan, BoiCanh bc)
        {
            if (!e.Visible) return; // ẩn theo visibility của dynamic block cha
            if (e is Table) return; // Table kế thừa BlockReference: ký hiệu trong bảng thống kê không phải thiết bị
            if (e.Layer.StartsWith(LAYER_SO_DO, StringComparison.OrdinalIgnoreCase)) return; // ký hiệu sơ đồ nguyên lý (SoDo.cs): thiết bị thật đếm ở mặt bằng
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
                    Ghi(bc, ma, lap, p, ChungLoai(br));
                    return;
                }
                if (sau >= SAU_TOI_DA) { bc.Kq.QuaSau++; return; }
                if (!CoThietBi(br.BlockTableRecord, bc)) return;
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
            Ghi(bc, v.ma, nhan * dai * bc.HeSo * Math.Abs(tf.GetScale()), c.GetPointAtDist(dai / 2).TransformBy(tf), null);
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
                foreach (KeyValuePair<string, double> d in kq.Dong.Where(x => MaCuaDong(x.Key) == p.Key && LoaiCuaDong(x.Key) != "").OrderBy(x => x.Key))
                    ed.WriteMessage("\n      + chủng loại \"" + LoaiCuaDong(d.Key) + "\": " + So(d.Value));
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

            // Thứ tự dòng theo danh mục (nhóm theo hệ), mã ngoài danh mục ở cuối; block động mỗi chủng loại (Visibility) một dòng.
            List<string> ma = dm.vat_tu.Select(v => v.ma).Where(m => kq.Tong.ContainsKey(m)).ToList();
            ma.AddRange(kq.Tong.Keys.Where(m => !ma.Contains(m)).OrderBy(m => m));
            List<string> dong = ma.SelectMany(m => kq.Dong.Keys.Where(d => MaCuaDong(d) == m).OrderBy(d => d)).ToList();
            bool coKhu = kq.TenKhu.Count > 0, coLoai = dong.Any(d => LoaiCuaDong(d) != "");
            int cLoai = coLoai ? 1 : 0; // cột "Chủng loại" chỉ có khi có block động: bảng block thường giữ như cũ
            int nCot = 4 + cLoai + (coKhu ? kq.TenKhu.Count : 0) + 1;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                ObjectId kieu = DamBaoKieuChu(db, tr);
                Table t = new Table();
                t.TableStyle = db.Tablestyle;
                t.Position = pp.Value.TransformBy(ed.CurrentUserCoordinateSystem);
                t.SetSize(dong.Count + 2, nCot);
                try { t.MergeCells(CellRange.Create(t, 0, 0, 0, nCot - 1)); }
                catch (Autodesk.AutoCAD.Runtime.Exception) { } // kiểu bảng đã tự gộp dòng tiêu đề
                t.Cells[0, 0].TextString = "BẢNG THỐNG KÊ THIẾT BỊ, VẬT TƯ PCCC";
                string[] dau = coLoai ? new string[] { "STT", "Ký hiệu", "Tên vật tư", "Chủng loại", "ĐVT" } : new string[] { "STT", "Ký hiệu", "Tên vật tư", "ĐVT" };
                for (int c = 0; c < dau.Length; c++) t.Cells[1, c].TextString = dau[c];
                for (int k = 0; coKhu && k < kq.TenKhu.Count; k++) t.Cells[1, 4 + cLoai + k].TextString = kq.TenKhu[k];
                t.Cells[1, nCot - 1].TextString = coKhu ? "Tổng" : "Số lượng";
                for (int i = 0; i < dong.Count; i++)
                {
                    int r = i + 2;
                    string m = MaCuaDong(dong[i]);
                    VatTu v = TimMa(dm, m);
                    t.Cells[r, 0].TextString = (i + 1).ToString();
                    if (bt.Has(m))
                    {
                        t.Cells[r, 1].BlockTableRecordId = bt[m];
                        t.Cells[r, 1].Contents[0].IsAutoScale = true; // ký hiệu co theo ô
                    }
                    else t.Cells[r, 1].TextString = v != null && v.loai == "tuyen" ? "—" : m;
                    t.Cells[r, 2].TextString = " " + (v != null ? v.ten : m); // cách mép trái ô (lề mặc định của kiểu bảng rất nhỏ)
                    if (coLoai) t.Cells[r, 3].TextString = LoaiCuaDong(dong[i]);
                    t.Cells[r, 3 + cLoai].TextString = v != null ? v.dvt : "";
                    for (int k = 0; coKhu && k < kq.TenKhu.Count; k++)
                    {
                        double sl;
                        Dictionary<string, double> d;
                        if (kq.DongKhu.TryGetValue(kq.TenKhu[k], out d) && d.TryGetValue(dong[i], out sl)) t.Cells[r, 4 + cLoai + k].TextString = So(sl);
                    }
                    t.Cells[r, nCot - 1].TextString = So(kq.Dong[dong[i]]);
                }
                for (int r = 0; r < dong.Count + 2; r++)
                {
                    t.Rows[r].Height = h * 2.2;
                    for (int c = 0; c < nCot; c++)
                    {
                        t.Cells[r, c].TextStyleId = kieu;
                        t.Cells[r, c].TextHeight = h;
                        t.Cells[r, c].Alignment = c == 2 && r > 1 ? CellAlignment.MiddleLeft : CellAlignment.MiddleCenter;
                    }
                }
                double[] rong = coLoai ? new double[] { 4, 6, 26, 14, 5 } : new double[] { 4, 6, 26, 5 };
                for (int c = 0; c < nCot; c++) t.Columns[c].Width = h * (c < rong.Length ? rong[c] : 7);
                t.GenerateLayout();
                BlockTableRecord space = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);
                space.AppendEntity(t); tr.AddNewlyCreatedDBObject(t, true);
                tr.Commit();
            }
            ed.WriteMessage("\n[TKPCCC] PCCC_BANG dong=" + dong.Count + (coLoai ? " chung_loai=" + string.Join(";", dong.Where(d => LoaiCuaDong(d) != "").Select(d => MaCuaDong(d) + ":" + LoaiCuaDong(d)).ToArray()) : ""));
        }

        // ----- Vùng bảo vệ -----

    }
}
