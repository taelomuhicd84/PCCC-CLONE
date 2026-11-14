// Chuỗi kích thước: lõi dùng chung cho thao tác spec "kt_chuoi" (KienTruc.cs) và lệnh gõ tay PCCC_ADIM, PCCC_CDIM (việc 14/11, gói A).
// Chiếu điểm lên trục đo, gom toạ độ trùng trong ε (một cột thiết bị chỉ ra một mốc), mỗi cặp mốc kề nhau một RotatedDimension
// có đường dim đi qua điểm "qua". Không sinh dim 0 hay dim chồng.
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

[assembly: CommandClass(typeof(TKPCCC.Lenh.LenhDim))]

namespace TKPCCC
{
    public partial class Lenh
    {
        const double EPS_DIM_M = 0.01; // gom toạ độ chiếu cách nhau ≤ 10 mm thành một mốc (thiết bị đặt tay lệch vài mm vẫn là một cột)

        /// Mốc đo: điểm xếp theo toạ độ chiếu (x khi đo ngang, y khi đo đứng), bỏ điểm cách mốc trước ≤ eps.
        static List<Point3d> GomMoc(IEnumerable<Point3d> ps, bool doX, double eps)
        {
            List<Point3d> kq = new List<Point3d>();
            foreach (Point3d q in ps.OrderBy(q => doX ? q.X : q.Y))
                if (kq.Count == 0 || (doX ? q.X - kq[kq.Count - 1].X : q.Y - kq[kq.Count - 1].Y) > eps) kq.Add(q);
            return kq;
        }

        /// Tự nhận phương: điểm đặt đường dim lệch khỏi hộp bao theo Y nhiều hơn theo X (ở trên / dưới) thì đo ngang.
        static bool TuNhanX(List<Point3d> ps, Point3d q)
        {
            double x1 = ps.Min(p => p.X), x2 = ps.Max(p => p.X), y1 = ps.Min(p => p.Y), y2 = ps.Max(p => p.Y);
            double dx = q.X < x1 ? x1 - q.X : (q.X > x2 ? q.X - x2 : 0), dy = q.Y < y1 ? y1 - q.Y : (q.Y > y2 ? q.Y - y2 : 0);
            return dy >= dx;
        }

        /// Chuỗi dim qua các mốc (chưa thêm vào bản vẽ). Ít hơn 2 mốc thì báo lỗi.
        static List<RotatedDimension> ChuoiKt(List<Point3d> moc, bool doX, Point3d qua, ObjectId kieu)
        {
            if (moc.Count < 2) throw new System.Exception("cần ít nhất 2 mốc khác nhau theo phương " + (doX ? "X" : "Y") + " (các điểm trùng hàng trong 10 mm)");
            List<RotatedDimension> kq = new List<RotatedDimension>();
            for (int i = 0; i + 1 < moc.Count; i++)
                kq.Add(new RotatedDimension(doX ? 0 : Math.PI / 2, moc[i], moc[i + 1], qua, "", kieu));
            return kq;
        }

        // ---------- PCCC_ADIM, PCCC_CDIM ----------
        // shortcut: CDIM nhớ chuỗi ADIM gần nhất cho cả phiên AutoCAD (không theo từng bản vẽ); cần thì lưu theo Document.
        static bool cdimCo, cdimX;
        static Point3d cdimQua, cdimCuoi;
        /// Trang ADIM của palette (TKPCCC.UI.dll) đặt phương đo trước khi gửi PCCC_ADIM ("X", "Y", "Tudong"): lệnh dùng luôn, không hỏi lại; dùng một lần rồi xoá.
        /// Cần vì chuỗi gửi qua SendStringToExecute không điền được câu hỏi nằm SAU bước quét chọn.
        public static string AdimPhuong;

        static string HoiTu(Editor ed, string hoi, string ds, string macDinh)
        {
            PromptKeywordOptions k = new PromptKeywordOptions(hoi);
            foreach (string t in ds.Split(' ')) k.Keywords.Add(t);
            k.Keywords.Default = macDinh;
            k.AllowNone = true;
            PromptResult r = ed.GetKeywords(k);
            if (r.Status == PromptStatus.Cancel) return null;
            return r.Status == PromptStatus.OK && !string.IsNullOrEmpty(r.StringResult) ? r.StringResult : macDinh;
        }

        static void Adim()
        {
            Document doc = AcApp.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;
            Matrix3d ucs = ed.CurrentUserCoordinateSystem;
            string phSan = AdimPhuong;
            AdimPhuong = null;
            string loc = HoiTu(ed, "\nLoại đối tượng cần dim", "Block Line Pline Mline Tatca", "Block");
            if (loc == null) return;
            string loai = loc == "Block" ? "INSERT" : loc == "Line" ? "LINE" : loc == "Pline" ? "LWPOLYLINE,POLYLINE" : loc == "Mline" ? "MLINE" : "INSERT,LINE,LWPOLYLINE,POLYLINE,MLINE";
            PromptSelectionOptions so = new PromptSelectionOptions();
            so.MessageForAdding = "\nQuét cửa sổ chọn cả lưới đối tượng: ";
            PromptSelectionResult sr = ed.GetSelection(so, new SelectionFilter(new TypedValue[] { new TypedValue((int)DxfCode.Start, loai) }));
            if (sr.Status != PromptStatus.OK) return;
            List<Point3d> ps = new List<Point3d>();
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                foreach (ObjectId id in sr.Value.GetObjectIds())
                {
                    Entity e = tr.GetObject(id, OpenMode.ForRead) as Entity;
                    if (e is Table) continue; // Table kế thừa BlockReference: ký hiệu trong bảng không phải thiết bị
                    BlockReference br = e as BlockReference;
                    Line ln = e as Line;
                    Polyline pl = e as Polyline;
                    Polyline2d p2 = e as Polyline2d;
                    Mline ml = e as Mline;
                    if (ml != null) for (int i = 0; i < ml.NumberOfVertices; i++) ps.Add(ml.VertexAt(i));
                    if (br != null) ps.Add(br.Position);
                    else if (ln != null) { ps.Add(ln.StartPoint); ps.Add(ln.EndPoint); }
                    else if (pl != null) for (int i = 0; i < pl.NumberOfVertices; i++) ps.Add(pl.GetPoint3dAt(i));
                    else if (p2 != null) foreach (ObjectId v in p2) ps.Add(((Vertex2d)tr.GetObject(v, OpenMode.ForRead)).Position);
                }
                tr.Commit();
            }
            if (ps.Count == 0) throw new System.Exception("không có đối tượng hợp loại " + loc);

            // 2 mốc mép tường / trục (tuỳ chọn): Enter bỏ qua.
            List<Point3d> tuong = new List<Point3d>();
            foreach (string hoi in new[] { "\nMép tường / trục đầu <Enter = bỏ qua>: ", "\nMép tường / trục cuối <Enter = bỏ qua>: " })
            {
                PromptPointOptions po = new PromptPointOptions(hoi);
                po.AllowNone = true;
                PromptPointResult pr = ed.GetPoint(po);
                if (pr.Status == PromptStatus.Cancel) return;
                if (pr.Status != PromptStatus.OK) break;
                tuong.Add(pr.Value.TransformBy(ucs));
            }
            string ph = phSan ?? HoiTu(ed, "\nPhương đo", "X Y Tudong", "Tudong");
            if (ph == null) return;
            PromptPointResult qr = ed.GetPoint("\nĐiểm đặt đường kích thước: ");
            if (qr.Status != PromptStatus.OK) return;
            Point3d qua = qr.Value.TransformBy(ucs);
            bool doX = ph == "X" || (ph == "Tudong" && (tuong.Count == 2
                ? Math.Abs(tuong[1].X - tuong[0].X) >= Math.Abs(tuong[1].Y - tuong[0].Y) : TuNhanX(ps, qua)));

            List<Point3d> moc = GomMoc(ps.Concat(tuong), doX, EPS_DIM_M / HeSoMet(db));
            int n;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTableRecord ms = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);
                List<RotatedDimension> ds = ChuoiKt(moc, doX, qua, db.Dimstyle);
                foreach (RotatedDimension d in ds)
                {
                    d.SetDatabaseDefaults(db); // layer, màu theo hiện hành
                    d.DimensionStyle = db.Dimstyle;
                    ms.AppendEntity(d);
                    tr.AddNewlyCreatedDBObject(d, true);
                }
                n = ds.Count;
                tr.Commit();
            }
            cdimCo = true; cdimX = doX; cdimQua = qua; cdimCuoi = moc[moc.Count - 1];
            ed.WriteMessage("\n[TKPCCC] PCCC_ADIM n=" + n + " phuong=" + (doX ? "X" : "Y") + " moc=" + moc.Count + " (" + ps.Count + " diem, " + tuong.Count + " moc tuong)");
        }

        static void Cdim()
        {
            Document doc = AcApp.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;
            if (!cdimCo) throw new System.Exception("chưa có chuỗi kích thước để nối: chạy PCCC_ADIM trước");
            double eps = EPS_DIM_M / HeSoMet(db);
            int n = 0;
            while (true)
            {
                PromptPointOptions po = new PromptPointOptions("\nĐiểm tiếp theo <Enter = xong>: ");
                po.AllowNone = true;
                po.UseBasePoint = true;
                po.BasePoint = cdimCuoi.TransformBy(ed.CurrentUserCoordinateSystem.Inverse());
                PromptPointResult pr = ed.GetPoint(po);
                if (pr.Status != PromptStatus.OK) break;
                Point3d q = pr.Value.TransformBy(ed.CurrentUserCoordinateSystem);
                if (Math.Abs(cdimX ? q.X - cdimCuoi.X : q.Y - cdimCuoi.Y) <= eps) { ed.WriteMessage("\n[TKPCCC] Điểm trùng mốc cuối, bỏ qua."); continue; }
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    BlockTableRecord ms = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);
                    RotatedDimension d = ChuoiKt(new List<Point3d> { cdimCuoi, q }, cdimX, cdimQua, db.Dimstyle)[0];
                    d.SetDatabaseDefaults(db);
                    d.DimensionStyle = db.Dimstyle;
                    ms.AppendEntity(d);
                    tr.AddNewlyCreatedDBObject(d, true);
                    tr.Commit();
                }
                cdimCuoi = q;
                n++;
            }
            ed.WriteMessage("\n[TKPCCC] PCCC_CDIM n=" + n);
        }

        /// Lệnh gõ tay về kích thước (lớp riêng, gói B gọi từ palette theo tên lệnh). Danh sách PCCC_HELP: Lenh.DS_LENH.
        public class LenhDim
        {
            [CommandMethod("PCCC_ADIM")] public void LenhAdim() { Chay(Adim); }
            [CommandMethod("PCCC_CDIM")] public void LenhCdim() { Chay(Cdim); }
        }
    }
}
