// Tiện ích chỉnh sửa, kiểm tra nhanh (phân hệ Tiện ích của palette, ý lấy từ addin mẫu 14/11; danh sách việc cad/tien_do/viec_video_addin.md):
// PCCC_TRUNG (CKC) · PCCC_LOPCHUAN (CKL, APL) · PCCC_DANHSO (AN) · PCCC_XOAY (MR) · PCCC_TYLE (SCB) · PCCC_DOIBLOCK (SB) · PCCC_CANDIM (AD) · PCCC_DOIDN (DN).
// Thiết bị = block có mã vật tư (MaThietBi), như bóc khối lượng. Mỗi lệnh in một dòng "[TKPCCC] PCCC_X ..." để test/chay_thu.ps1 đọc.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using AcApp = Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace TKPCCC
{
    public partial class Lenh
    {
        [CommandMethod("PCCC_TRUNG")] public void LenhTrung() { Chay(KiemTrung); }
        [CommandMethod("PCCC_LOPCHUAN")] public void LenhLopChuan() { Chay(LopChuan); }
        [CommandMethod("PCCC_DANHSO")] public void LenhDanhSo() { Chay(DanhSo); }
        [CommandMethod("PCCC_XOAY")] public void LenhXoay() { Chay(delegate { DoiTungBlock(true); }); }
        [CommandMethod("PCCC_TYLE")] public void LenhTyLe() { Chay(delegate { DoiTungBlock(false); }); }
        [CommandMethod("PCCC_DOIBLOCK")] public void LenhDoiBlock() { Chay(DoiBlock); }
        [CommandMethod("PCCC_CANDIM")] public void LenhCanDim() { Chay(CanDim); }
        [CommandMethod("PCCC_DOIDN")] public void LenhDoiDn() { Chay(DoiDn); }

        const string LAYER_KIEM = "PCCC_KIEM"; // vòng đánh dấu lỗi kiểm tra, không in

        /// Chọn block (lọc INSERT). caBanVe: Enter = mọi block trong không gian hiện hành. Trả id các block là thiết bị kèm mã.
        static List<KeyValuePair<ObjectId, string>> ChonThietBi(Editor ed, Transaction tr, BoiCanh bc, string loiNhac, bool caBanVe)
        {
            SelectionFilter loc = new SelectionFilter(new[] { new TypedValue((int)DxfCode.Start, "INSERT") });
            PromptSelectionOptions o = new PromptSelectionOptions();
            o.MessageForAdding = "\n" + loiNhac + (caBanVe ? " (Enter = cả bản vẽ)" : "") + ": ";
            PromptSelectionResult r = ed.GetSelection(o, loc);
            if (r.Status != PromptStatus.OK && caBanVe) r = ed.SelectAll(loc);
            List<KeyValuePair<ObjectId, string>> kq = new List<KeyValuePair<ObjectId, string>>();
            if (r.Status != PromptStatus.OK) return kq;
            foreach (ObjectId id in r.Value.GetObjectIds())
            {
                BlockReference br = tr.GetObject(id, OpenMode.ForRead) as BlockReference;
                if (br == null || br is Table) continue;
                string ma = MaThietBi(br, bc);
                if (ma != null) kq.Add(new KeyValuePair<ObjectId, string>(id, ma));
            }
            return kq;
        }

        static void VongKiem(Database db, Transaction tr, Point3d p, double r)
        {
            ObjectId lop = DamBaoLayer(db, tr, LAYER_KIEM, 1);
            ((LayerTableRecord)tr.GetObject(lop, OpenMode.ForWrite)).IsPlottable = false;
            BlockTableRecord ms = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);
            Circle c = new Circle(new Point3d(p.X, p.Y, 0), Vector3d.ZAxis, r);
            c.LayerId = lop;
            ms.AppendEntity(c);
            tr.AddNewlyCreatedDBObject(c, true);
        }

        /// CKC: hai thiết bị cùng mã đặt trùng điểm (cách nhau ≤ 10 mm): khoanh đỏ trên PCCC_KIEM, đếm số bản thừa.
        static void KiemTrung()
        {
            Document doc = AcApp.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            double m = HeSoMet(db), tol = 0.01 / m;
            int thua = 0, cum = 0;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BoiCanh bc = new BoiCanh { Tr = tr, Dm = DocDanhMuc(), Alias = DocAlias() };
                List<KeyValuePair<ObjectId, string>> ds = ChonThietBi(doc.Editor, tr, bc, "Chọn vùng kiểm thiết bị trùng", true);
                foreach (IGrouping<string, KeyValuePair<ObjectId, string>> g in ds.GroupBy(x => x.Value))
                {
                    List<Point3d> ps = g.Select(x => ((BlockReference)tr.GetObject(x.Key, OpenMode.ForRead)).Position).ToList();
                    bool[] da = new bool[ps.Count];
                    for (int i = 0; i < ps.Count; i++)
                    {
                        if (da[i]) continue;
                        int n = 0;
                        for (int j = i + 1; j < ps.Count; j++)
                            if (!da[j] && ps[i].DistanceTo(ps[j]) <= tol) { da[j] = true; n++; }
                        if (n == 0) continue;
                        thua += n; cum++;
                        VongKiem(db, tr, ps[i], 0.5 / m);
                    }
                }
                tr.Commit();
            }
            doc.Editor.WriteMessage("\n[TKPCCC] PCCC_TRUNG trung=" + cum + " thua=" + thua
                + (cum > 0 ? " (khoanh đỏ trên layer " + LAYER_KIEM + ", không in; xoá bản thừa rồi xoá vòng)" : " (không có thiết bị trùng)"));
        }

        /// CKL + APL: thiết bị không nằm trên layer chuẩn PCCC_<hệ> (như PCCC_DAT, PCCC_RAI đặt). Co = chuyển về layer chuẩn.
        static void LopChuan()
        {
            Document doc = AcApp.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;
            int sai = 0, sua = 0;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                FileDanhMuc dm = DocDanhMuc();
                BoiCanh bc = new BoiCanh { Tr = tr, Dm = dm, Alias = DocAlias() };
                List<KeyValuePair<ObjectId, string>> ds = ChonThietBi(ed, tr, bc, "Chọn vùng kiểm layer thiết bị", true);
                List<KeyValuePair<ObjectId, string>> loi = new List<KeyValuePair<ObjectId, string>>();
                foreach (KeyValuePair<ObjectId, string> x in ds)
                {
                    VatTu v = TimMa(dm, x.Value);
                    if (v != null && !string.Equals(((BlockReference)tr.GetObject(x.Key, OpenMode.ForRead)).Layer, TIEN_TO_LAYER + v.he, StringComparison.OrdinalIgnoreCase))
                        loi.Add(new KeyValuePair<ObjectId, string>(x.Key, TIEN_TO_LAYER + v.he));
                }
                sai = loi.Count;
                if (sai > 0)
                {
                    PromptKeywordOptions k = new PromptKeywordOptions("\n" + sai + " thiết bị sai layer. Chuyển về layer chuẩn PCCC_<hệ>?");
                    k.Keywords.Add("Co"); k.Keywords.Add("Khong");
                    k.Keywords.Default = "Khong";
                    k.AllowNone = true;
                    PromptResult kr = ed.GetKeywords(k);
                    bool doi = kr.Status == PromptStatus.OK && kr.StringResult == "Co";
                    foreach (KeyValuePair<ObjectId, string> x in loi)
                    {
                        BlockReference br = (BlockReference)tr.GetObject(x.Key, OpenMode.ForRead);
                        if (doi)
                        {
                            ObjectId lop = DamBaoLayer(db, tr, x.Value, MauHe(TimMa(dm, MaThietBi(br, bc)).he));
                            br.UpgradeOpen();
                            br.LayerId = lop;
                            sua++;
                        }
                        else VongKiem(db, tr, br.Position, 0.5 / HeSoMet(db));
                    }
                }
                tr.Commit();
            }
            ed.WriteMessage("\n[TKPCCC] PCCC_LOPCHUAN sai=" + sai + " sua=" + sua + (sai > sua ? " (chưa sửa: khoanh đỏ trên " + LAYER_KIEM + ")" : ""));
        }

        /// AN: đánh số thiết bị đã chọn: tiền tố + số tăng dần, thứ tự theo hàng (XY: hàng trên xuống, trong hàng trái sang phải) hoặc theo cột (YX).
        /// Chữ đặt lệch lên phải điểm chèn, layer PCCC_SO.
        static void DanhSo()
        {
            Document doc = AcApp.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;
            double m = HeSoMet(db), cao = LaySo(NhomKyHieu("chu"), "cao", 250) * 0.001 / m;
            int n = 0;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BoiCanh bc = new BoiCanh { Tr = tr, Dm = DocDanhMuc(), Alias = DocAlias() };
                List<KeyValuePair<ObjectId, string>> ds = ChonThietBi(ed, tr, bc, "Chọn thiết bị cần đánh số", false);
                if (ds.Count == 0) { tr.Commit(); return; }
                PromptStringOptions to = new PromptStringOptions("\nTiền tố (vd S1-, Enter = không có): ");
                to.AllowSpaces = false;
                PromptResult tr1 = ed.GetString(to);
                if (tr1.Status != PromptStatus.OK) return;
                PromptIntegerOptions so = new PromptIntegerOptions("\nSố bắt đầu: ");
                so.DefaultValue = 1; so.UseDefaultValue = true; so.AllowNegative = false;
                PromptIntegerResult sr = ed.GetInteger(so);
                if (sr.Status != PromptStatus.OK) return;
                PromptKeywordOptions th = new PromptKeywordOptions("\nThứ tự đánh số");
                th.Keywords.Add("XY"); th.Keywords.Add("YX");
                th.Keywords.Default = "XY";
                th.AllowNone = true;
                PromptResult thr = ed.GetKeywords(th);
                if (thr.Status == PromptStatus.Cancel) return;
                bool theoCot = thr.StringResult == "YX";

                List<Point3d> ps = ds.Select(x => ((BlockReference)tr.GetObject(x.Key, OpenMode.ForRead)).Position).ToList();
                double sai = 0.3 / m; // cùng hàng (cột) khi lệch ≤ 0,3 m
                List<Point3d> thuTu = new List<Point3d>();
                if (theoCot) foreach (List<Point3d> c in Gom(ps, p => p.X, sai)) thuTu.AddRange(c.OrderByDescending(p => p.Y));
                else foreach (List<Point3d> h in Gom(ps, p => -p.Y, sai)) thuTu.AddRange(h.OrderBy(p => p.X));
                ObjectId lop = DamBaoLayer(db, tr, TIEN_TO_LAYER + "SO", 7);
                BlockTableRecord ms = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);
                foreach (Point3d p in thuTu)
                {
                    DBText t = new DBText();
                    t.TextString = tr1.StringResult + (sr.Value + n);
                    t.Height = cao;
                    t.Position = new Point3d(p.X + cao, p.Y + cao, 0);
                    t.LayerId = lop;
                    ms.AppendEntity(t);
                    tr.AddNewlyCreatedDBObject(t, true);
                    n++;
                }
                tr.Commit();
            }
            ed.WriteMessage("\n[TKPCCC] PCCC_DANHSO n=" + n);
        }

        /// MR / SCB: xoay thêm một góc (độ) hoặc nhân tỉ lệ cho TỪNG block quanh điểm chèn của chính nó (không văng vị trí).
        static void DoiTungBlock(bool xoay)
        {
            Document doc = AcApp.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;
            PromptSelectionOptions o = new PromptSelectionOptions();
            o.MessageForAdding = "\nChọn các block: ";
            PromptSelectionResult r = ed.GetSelection(o, new SelectionFilter(new[] { new TypedValue((int)DxfCode.Start, "INSERT") }));
            if (r.Status != PromptStatus.OK) return;
            PromptDoubleOptions so = new PromptDoubleOptions(xoay ? "\nGóc xoay thêm (độ, ngược chiều kim đồng hồ): " : "\nHệ số tỉ lệ (vd 2 = gấp đôi, 0,5 = một nửa): ");
            so.AllowZero = xoay; so.AllowNegative = xoay;
            PromptDoubleResult sr = ed.GetDouble(so);
            if (sr.Status != PromptStatus.OK) return;
            int n = 0;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                foreach (ObjectId id in r.Value.GetObjectIds())
                {
                    BlockReference br = tr.GetObject(id, OpenMode.ForWrite) as BlockReference;
                    if (br == null || br is Table) continue;
                    Matrix3d bd = xoay ? Matrix3d.Rotation(sr.Value * Math.PI / 180, Vector3d.ZAxis, br.Position) : Matrix3d.Scaling(sr.Value, br.Position);
                    br.TransformBy(bd); // biến đổi cả attribute đi kèm
                    n++;
                }
                tr.Commit();
            }
            ed.WriteMessage("\n[TKPCCC] " + (xoay ? "PCCC_XOAY n=" + n + " goc=" : "PCCC_TYLE n=" + n + " he_so=") + sr.Value.ToString(CultureInfo.InvariantCulture));
        }

        /// SB: thay block đã chọn bằng block của mã vật tư khác, giữ điểm chèn, góc, tỉ lệ, layer; attribute MA_VT (nếu có) ghi mã mới.
        static void DoiBlock()
        {
            Document doc = AcApp.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;
            PromptSelectionOptions o = new PromptSelectionOptions();
            o.MessageForAdding = "\nChọn các block cần thay: ";
            PromptSelectionResult r = ed.GetSelection(o, new SelectionFilter(new[] { new TypedValue((int)DxfCode.Start, "INSERT") }));
            if (r.Status != PromptStatus.OK) return;
            FileDanhMuc dm = DocDanhMuc();
            VatTu v = HoiMa(ed, dm, "block", "Mã thiết bị thay vào");
            if (v == null) return;
            bool co;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                co = ((BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead)).Has(v.ma);
                tr.Commit();
            }
            if (!co) NapVaoBanVe(doc, dm);
            int n = 0;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                ObjectId moi = ((BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead))[v.ma];
                foreach (ObjectId id in r.Value.GetObjectIds())
                {
                    BlockReference br = tr.GetObject(id, OpenMode.ForWrite) as BlockReference;
                    if (br == null || br is Table || br.BlockTableRecord == moi) continue;
                    br.BlockTableRecord = moi;
                    foreach (ObjectId aid in br.AttributeCollection)
                    {
                        AttributeReference ar = tr.GetObject(aid, OpenMode.ForWrite) as AttributeReference;
                        if (ar != null && ar.Tag.ToUpperInvariant() == TAG_MA) ar.TextString = v.ma;
                    }
                    n++;
                }
                tr.Commit();
            }
            ed.WriteMessage("\n[TKPCCC] PCCC_DOIBLOCK n=" + n + " " + v.ma);
        }

        /// AD: dóng đường kích thước của các dim đã chọn về cùng một mốc (bấm điểm đường dim đi qua).
        static void CanDim()
        {
            Document doc = AcApp.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;
            PromptSelectionOptions o = new PromptSelectionOptions();
            o.MessageForAdding = "\nChọn các kích thước cần dóng hàng: ";
            PromptSelectionResult r = ed.GetSelection(o, new SelectionFilter(new[] { new TypedValue((int)DxfCode.Start, "DIMENSION") }));
            if (r.Status != PromptStatus.OK) return;
            PromptPointResult pr = ed.GetPoint("\nBấm điểm đường kích thước đi qua: ");
            if (pr.Status != PromptStatus.OK) return;
            Point3d q = pr.Value.TransformBy(ed.CurrentUserCoordinateSystem);
            int n = 0;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                foreach (ObjectId id in r.Value.GetObjectIds())
                {
                    Dimension d = tr.GetObject(id, OpenMode.ForWrite) as Dimension;
                    RotatedDimension rd = d as RotatedDimension;
                    AlignedDimension ad = d as AlignedDimension;
                    if (rd != null) rd.DimLinePoint = q;
                    else if (ad != null) ad.DimLinePoint = q;
                    else continue;
                    n++;
                }
                tr.Commit();
            }
            ed.WriteMessage("\n[TKPCCC] PCCC_CANDIM n=" + n);
        }

        /// DN: chuyển đoạn ống (line, polyline) đã chọn sang layer PCCC_<mã ống mới>, như PCCC_ONG vẽ.
        /// shortcut: chưa sửa chữ nhãn DN cạnh ống (nhãn của tuyen "nhan"); khi cần thì tìm chữ trên PCCC_NHAN gần đoạn ống.
        static void DoiDn()
        {
            Document doc = AcApp.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;
            PromptSelectionOptions o = new PromptSelectionOptions();
            o.MessageForAdding = "\nChọn các đoạn ống cần đổi cỡ: ";
            PromptSelectionResult r = ed.GetSelection(o, new SelectionFilter(new[] { new TypedValue((int)DxfCode.Start, "LINE,LWPOLYLINE,POLYLINE,ARC") }));
            if (r.Status != PromptStatus.OK) return;
            VatTu v = HoiMa(ed, DocDanhMuc(), "tuyen", "Mã ống mới (vd ONG_DN65)");
            if (v == null) return;
            int n = 0;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                ObjectId lop = DamBaoLayer(db, tr, TIEN_TO_LAYER + v.ma, MauHe(v.he));
                foreach (ObjectId id in r.Value.GetObjectIds())
                {
                    Entity e = (Entity)tr.GetObject(id, OpenMode.ForWrite);
                    e.LayerId = lop;
                    n++;
                }
                tr.Commit();
            }
            ed.WriteMessage("\n[TKPCCC] PCCC_DOIDN n=" + n + " " + v.ma);
        }
    }
}
