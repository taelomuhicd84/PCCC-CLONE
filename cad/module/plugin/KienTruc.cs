// Mặt bằng kiến trúc (việc 14/11, gói A): thao tác spec "tuong" (tường, vách theo đường tim, góc gấp nối miter), "cua" (khe cửa trên tường
// hai nét + má cửa + cánh + cung mở), "kt_chuoi" (chuỗi kích thước, lõi chung với PCCC_ADIM ở Dim.cs) và lệnh gõ tay PCCC_TRUC, PCCC_TUONG, PCCC_CUA.
// Layer, độ dày, rộng cửa mặc định: nhóm "kien_truc" của cad/data/ky_hieu/ky_hieu.json (chuẩn ở cad/layer/kien_truc.md).
// Tường vẽ ra để "vung_tu_dong" (NhanVung.cs) dựng được phòng: layer có chữ TUONG / VACH (LOP_TUONG), cửa trên layer có chữ CUA (LOP_BO, bị bỏ qua),
// khe cửa là khe thẳng hàng của mặt tường, nối được khi rộng ≤ HO_CUA_M.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using AcApp = Autodesk.AutoCAD.ApplicationServices.Core.Application;

[assembly: CommandClass(typeof(TKPCCC.Lenh.LenhKienTruc))]

namespace TKPCCC
{
    public partial class Lenh
    {
        const double DAY_TUONG_MAX_M = 1.0; // cửa: hai mặt tường quanh điểm phải cách nhau ≤ 1 m

        /// Layer kiến trúc theo loại ("tuong", "vach", "cua", "kt") từ ky_hieu.json nhóm "kien_truc".
        static ObjectId LopKienTruc(Database db, Transaction tr, string loai)
        {
            Dictionary<string, object> k = NhomKyHieu("kien_truc");
            string ten = loai == "tuong" ? "TUONG" : loai == "vach" ? "VACH" : loai == "cua" ? "CUA" : "DIM";
            short mau = (short)(loai == "tuong" ? 7 : loai == "vach" ? 8 : loai == "cua" ? 4 : 3);
            return DamBaoLayer(db, tr, LayChu(k, "layer_" + loai, ten), (short)LaySo(k, "mau_" + loai, mau));
        }

        /// Đường lệch h (dương: bên trái chiều vẽ) của đường tim, góc gấp nối miter.
        static List<Point2d> Lech(List<Point2d> q, bool kin, double h)
        {
            int n = q.Count;
            Func<int, Vector2d> phap = delegate (int i) { Vector2d v = (q[(i + 1) % n] - q[i]).GetNormal(); return new Vector2d(-v.Y, v.X); };
            List<Point2d> kq = new List<Point2d>();
            for (int i = 0; i < n; i++)
            {
                if (!kin && i == 0) { kq.Add(q[0] + phap(0) * h); continue; }
                if (!kin && i == n - 1) { kq.Add(q[i] + phap(n - 2) * h); continue; }
                Vector2d a = phap((i - 1 + n) % n), b = phap(i);
                double k = 1 + a.DotProduct(b);
                // shortcut: gấp gần 180° thì miter dài vô hạn, báo lỗi thay vì vát góc; vát (bevel) khi có bản vẽ cần
                if (k < 0.02) throw new System.Exception("tường gấp ngược gần 180° tại điểm " + i);
                kq.Add(q[i] + (a + b) * (h / k));
            }
            return kq;
        }

        /// Đường bao tường dày d theo đường tim: tường hở = 1 polyline kín (mặt trái + mặt phải + 2 đầu), tường kín = 2 polyline kín (trong, ngoài).
        static List<Polyline> DuongTuong(List<Point3d> tim, bool kin, double d)
        {
            List<Point2d> q = new List<Point2d>();
            foreach (Point3d p in tim)
                if (q.Count == 0 || q[q.Count - 1].GetDistanceTo(new Point2d(p.X, p.Y)) > 1e-6) q.Add(new Point2d(p.X, p.Y));
            if (kin && q.Count > 1 && q[0].GetDistanceTo(q[q.Count - 1]) <= 1e-6) q.RemoveAt(q.Count - 1);
            if (q.Count < 2 || (kin && q.Count < 3)) throw new System.Exception("đường tim tường cần ít nhất " + (kin ? 3 : 2) + " điểm khác nhau");
            List<Point2d> trai = Lech(q, kin, d / 2), phai = Lech(q, kin, -d / 2);
            Func<IEnumerable<Point2d>, Polyline> pl = delegate (IEnumerable<Point2d> ds)
            {
                Polyline p = new Polyline();
                foreach (Point2d x in ds) p.AddVertexAt(p.NumberOfVertices, x, 0, 0, 0);
                p.Closed = true;
                return p;
            };
            if (kin) return new List<Polyline> { pl(trai), pl(phai) };
            phai.Reverse();
            return new List<Polyline> { pl(trai.Concat(phai)) };
        }

        static bool LaLopTuong(string layer)
        {
            string t = ChuanTen(layer);
            return !LOP_BO.Any(x => t.Contains(x)) && LOP_TUONG.Any(x => t.Contains(x));
        }

        class MatTuong { public ObjectId Id; public double O, S1, S2; }

        /// Cửa đi rộng w, tâm p trên tường hai nét (layer tường) gần p: cắt khe ở hai mặt tường, vẽ 2 má cửa (layer của tường),
        /// cánh + cung mở 90° (layer lopCua) về phía điểm "ve" (null = bên trái chiều đoạn tường gần nhất). banLeTrai: bản lề bên trái
        /// người đứng phía cánh mở nhìn vào tường. Trả đối tượng mới (chưa thêm vào bản vẽ); xoa = số đối tượng tường cũ đã thay.
        static List<Entity> CatCua(Database db, Transaction tr, BlockTableRecord ms, Point3d p, double w, Point3d? ve, bool banLeTrai, ObjectId lopCua, out int xoa)
        {
            double mm = 0.001 / HeSoMet(db), tol = mm, rMax = DAY_TUONG_MAX_M / HeSoMet(db);
            if (w <= 0) throw new System.Exception("rộng cửa \"w\" phải > 0");
            Point2d p2 = new Point2d(p.X, p.Y);
            List<KeyValuePair<ObjectId, LineSegment2d>> doan = new List<KeyValuePair<ObjectId, LineSegment2d>>();
            Dictionary<ObjectId, bool> lop = new Dictionary<ObjectId, bool>();
            foreach (ObjectId id in ms)
            {
                Curve c = tr.GetObject(id, OpenMode.ForRead) as Curve;
                if (c == null || !(c is Line || c is Polyline)) continue;
                bool la;
                if (!lop.TryGetValue(c.LayerId, out la)) { la = LaLopTuong(c.Layer); lop[c.LayerId] = la; }
                if (!la) continue;
                Extents3d x;
                try { x = c.GeometricExtents; } catch (System.Exception) { continue; }
                if (x.MinPoint.X > p.X + rMax + w || x.MaxPoint.X < p.X - rMax - w || x.MinPoint.Y > p.Y + rMax + w || x.MaxPoint.Y < p.Y - rMax - w) continue;
                Line ln = c as Line;
                Polyline pl = c as Polyline;
                if (ln != null) doan.Add(new KeyValuePair<ObjectId, LineSegment2d>(id, new LineSegment2d(new Point2d(ln.StartPoint.X, ln.StartPoint.Y), new Point2d(ln.EndPoint.X, ln.EndPoint.Y))));
                else
                    for (int i = 0; i < (pl.Closed ? pl.NumberOfVertices : pl.NumberOfVertices - 1); i++)
                        if (pl.GetSegmentType(i) == SegmentType.Line) doan.Add(new KeyValuePair<ObjectId, LineSegment2d>(id, pl.GetLineSegment2dAt(i)));
            }
            KeyValuePair<ObjectId, LineSegment2d> gan = doan.Where(kv => kv.Value.Length > tol).OrderBy(kv => kv.Value.GetDistanceTo(p2)).FirstOrDefault();
            if (gan.Value == null || gan.Value.GetDistanceTo(p2) > rMax)
                throw new System.Exception("không thấy nét tường (layer có chữ TUONG, WALL, VACH...) trong 1 m quanh điểm cửa");
            Vector2d u = gan.Value.Direction.GetNormal(), n = new Vector2d(-u.Y, u.X);
            double s0 = u.DotProduct(p2.GetAsVector()), a = s0 - w / 2, b = s0 + w / 2;

            // Mặt tường: đoạn song song phương tường, phủ trọn khe [a, b]. Lấy cặp mặt kề nhau bao điểm p có khoảng cách nhỏ nhất (bề dày tường).
            List<MatTuong> mat = new List<MatTuong>();
            foreach (KeyValuePair<ObjectId, LineSegment2d> kv in doan)
            {
                Vector2d v = kv.Value.EndPoint - kv.Value.StartPoint;
                if (v.Length <= tol || Math.Abs(u.X * v.Y - u.Y * v.X) > 1e-3 * v.Length) continue;
                double s1 = u.DotProduct(kv.Value.StartPoint.GetAsVector()), s2 = u.DotProduct(kv.Value.EndPoint.GetAsVector());
                double o = n.DotProduct(kv.Value.StartPoint - p2);
                if (Math.Abs(o) <= rMax && Math.Min(s1, s2) <= a + tol && Math.Max(s1, s2) >= b - tol)
                    mat.Add(new MatTuong { Id = kv.Key, O = o, S1 = Math.Min(s1, s2), S2 = Math.Max(s1, s2) });
            }
            mat = mat.OrderBy(m => m.O).ToList();
            MatTuong duoi = null, tren = null;
            for (int i = 0; i + 1 < mat.Count; i++)
            {
                MatTuong m1 = mat[i], m2 = mat[i + 1];
                if (m1.O > tol || m2.O < -tol || m2.O - m1.O <= 2 * tol) continue;
                if (duoi == null || m2.O - m1.O < tren.O - duoi.O) { duoi = m1; tren = m2; }
            }
            if (duoi == null)
                throw new System.Exception("không thấy hai mặt tường song song quanh điểm cửa phủ trọn khe rộng " + Lt(w).ToString(CultureInfo.InvariantCulture)
                    + " (cửa vượt đoạn tường thẳng, hoặc tường một nét)");

            // Cắt khe: mỗi đối tượng tường tách tại các điểm khe, bỏ mảnh nằm trong khe.
            Func<double, double, Point3d> diem = delegate (double o, double s) { Point2d q = p2 + n * o + u * (s - s0); return new Point3d(q.X, q.Y, p.Z); };
            List<Entity> moi = new List<Entity>();
            xoa = 0;
            foreach (IGrouping<ObjectId, MatTuong> nhom in new[] { duoi, tren }.GroupBy(m => m.Id))
            {
                Curve c = (Curve)tr.GetObject(nhom.Key, OpenMode.ForWrite);
                List<double> th = new List<double>();
                foreach (MatTuong m in nhom)
                    foreach (double s in new[] { a, b }) th.Add(c.GetParameterAtPoint(c.GetClosestPointTo(diem(m.O, s), false)));
                th = th.Distinct().OrderBy(t => t).ToList();
                foreach (DBObject x in c.GetSplitCurves(new DoubleCollection(th.ToArray())))
                {
                    Curve k = (Curve)x;
                    Point3d g = k.GetPointAtDist(k.GetDistanceAtParameter(k.EndParam) / 2);
                    Point2d g2 = new Point2d(g.X, g.Y);
                    bool trongKhe = Math.Abs(u.DotProduct(g2 - p2)) < w / 2 - tol && nhom.Any(m => Math.Abs(n.DotProduct(g2 - p2) - m.O) <= tol);
                    if (trongKhe) k.Dispose(); else moi.Add(k);
                }
                c.Erase();
                xoa++;
            }
            ObjectId lopTuong = ((Entity)tr.GetObject(tren.Id, OpenMode.ForRead)).LayerId;
            foreach (double s in new[] { a, b })
            {
                Line ma = new Line(diem(duoi.O, s), diem(tren.O, s));
                ma.LayerId = lopTuong;
                moi.Add(ma);
            }

            // Cánh + cung mở về phía "ve".
            bool len = !ve.HasValue || n.DotProduct(new Point2d(ve.Value.X, ve.Value.Y) - p2) >= 0;
            Vector2d f = len ? n : -n, trai = new Vector2d(f.Y, -f.X); // trái của người đứng phía cánh mở, mặt hướng -f
            double oMo = len ? tren.O : duoi.O, sBl = (trai.DotProduct(u) > 0) == banLeTrai ? b : a;
            Point3d H = diem(oMo, sBl), J = diem(oMo, sBl == a ? b : a), T = H + new Vector3d(f.X, f.Y, 0) * w;
            Line canh = new Line(H, T);
            canh.LayerId = lopCua;
            moi.Add(canh);
            double g1 = Math.Atan2(J.Y - H.Y, J.X - H.X), g2a = Math.Atan2(T.Y - H.Y, T.X - H.X);
            bool nguoc = (J.X - H.X) * (T.Y - H.Y) - (J.Y - H.Y) * (T.X - H.X) > 0;
            Arc cung = nguoc ? new Arc(H, w, g1, g2a) : new Arc(H, w, g2a, g1);
            cung.LayerId = lopCua;
            moi.Add(cung);
            return moi;
        }

        // ---------- Thao tác spec ----------

        /// "tuong": p đường tim, d dày (mặc định theo ky_hieu.json), kin, vach:true = vách ngăn (layer VACH, mỏng hơn).
        static void OpTuong(GoiSpec g, Dictionary<string, object> o)
        {
            bool vach = LayCo(o, "vach", false);
            string loai = vach ? "vach" : "tuong";
            double d = LaySo(o, "d", LaySo(NhomKyHieu("kien_truc"), "d_" + loai, vach ? 100 : 200));
            if (d <= 0) throw new System.Exception("\"d\" (dày tường) phải > 0");
            ObjectId lop = LopKienTruc(g.Db, g.Tr, loai);
            foreach (Polyline pl in DuongTuong(DsP(g, Lay(o, "p"), "p"), LayCo(o, "kin", false), d))
            {
                pl.LayerId = lop;
                Them(g, pl);
            }
        }

        /// "cua": p tâm cửa trên tường đã vẽ, w rộng, ve điểm phía cánh mở vào, mo "trai" | "phai" (bản lề).
        static void OpCua(GoiSpec g, Dictionary<string, object> o)
        {
            string mo = LayChu(o, "mo", "trai");
            if (mo != "trai" && mo != "phai") throw new System.Exception("\"mo\" phải là \"trai\" hoặc \"phai\"");
            int xoa;
            foreach (Entity e in CatCua(g.Db, g.Tr, g.Ms, P(g, Lay(o, "p"), "p"), LaySo(o, "w", LaySo(NhomKyHieu("kien_truc"), "rong_cua", 900)),
                Lay(o, "ve") == null ? (Point3d?)null : P(g, Lay(o, "ve"), "ve"), mo == "trai", LopKienTruc(g.Db, g.Tr, "cua"), out xoa))
                Them(g, e);
            g.Xoa += xoa;
        }

        /// "kt_chuoi": chuỗi kích thước qua p, phuong "x" | "y" (bỏ = tự nhận theo "qua"), qua = điểm đường dim đi qua
        /// (bỏ = cách hộp bao "cach" về phía dưới hoặc trái). Điểm trùng hàng trong 10 mm gom một mốc.
        static void OpKtChuoi(GoiSpec g, Dictionary<string, object> o)
        {
            List<Point3d> ps = DsP(g, Lay(o, "p"), "p");
            Dictionary<string, object> k = NhomKyHieu("kt");
            string ph = LayChu(o, "phuong", "");
            if (ph != "" && ph != "x" && ph != "y") throw new System.Exception("\"phuong\" phải là \"x\" hoặc \"y\"");
            object qo = Lay(o, "qua");
            bool doX = ph == "x" || (ph == "" && (qo == null || TuNhanX(ps, P(g, qo, "qua"))));
            double cach = LaySo(o, "cach", LaySo(k, "cach", 500));
            Point3d qua = qo != null ? P(g, qo, "qua")
                : doX ? new Point3d(ps.Min(q => q.X), ps.Min(q => q.Y) - cach, 0) : new Point3d(ps.Min(q => q.X) - cach, ps.Min(q => q.Y), 0);
            ObjectId lop = Lay(o, "l") != null ? LayerOp(g, o) : LopKienTruc(g.Db, g.Tr, "kt");
            double tl = LaySo(o, "cao", LaySo(k, "cao", 250)) / 2.5;
            foreach (RotatedDimension d in ChuoiKt(GomMoc(ps, doX, EPS_DIM_M / HeSoMet(g.Db)), doX, qua, DamBaoKieuKt(g)))
            {
                d.Dimscale = tl;
                d.LayerId = lop;
                Them(g, d);
            }
        }

        // ---------- Lệnh gõ tay ----------

        static GoiSpec GoiTay(Document doc, Transaction tr)
        {
            kyHieu = null; // đọc lại ky_hieu.json như PCCC_SPEC
            GoiSpec g = new GoiSpec();
            g.Doc = doc; g.Db = doc.Database; g.Tr = tr;
            g.Ms = (BlockTableRecord)tr.GetObject(doc.Database.CurrentSpaceId, OpenMode.ForWrite);
            return g;
        }

        /// Dãy nhịp gõ "6000,6000,4500" (dấu phẩy, chấm phẩy hoặc khoảng trắng ngăn cách). null = huỷ.
        static ArrayList HoiNhip(Editor ed, string truc)
        {
            PromptStringOptions so = new PromptStringOptions("\nNhịp trục " + truc + " (vd 6000,6000,4500): ");
            PromptResult r = ed.GetString(so);
            if (r.Status != PromptStatus.OK) return null;
            ArrayList kq = new ArrayList();
            foreach (string t in r.StringResult.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries))
            {
                double v;
                if (!double.TryParse(t, NumberStyles.Float, CultureInfo.InvariantCulture, out v) || v <= 0) throw new System.Exception("nhịp \"" + t + "\" không phải số dương");
                kq.Add(v);
            }
            if (kq.Count == 0) throw new System.Exception("cần ít nhất một nhịp " + truc);
            return kq;
        }

        static void TrucTay()
        {
            Document doc = AcApp.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;
            ArrayList x = HoiNhip(ed, "X (trục đứng 1, 2, 3...)");
            if (x == null) return;
            ArrayList y = HoiNhip(ed, "Y (trục ngang A, B, C...)");
            if (y == null) return;
            PromptPointResult pr = ed.GetPoint("\nĐiểm gốc (giao trục 1 - A): ");
            if (pr.Status != PromptStatus.OK) return;
            Point3d O = pr.Value.TransformBy(ed.CurrentUserCoordinateSystem);
            using (Transaction tr = doc.Database.TransactionManager.StartTransaction())
            {
                GoiSpec g = GoiTay(doc, tr);
                OpLuoiTruc(g, new Dictionary<string, object> { { "x", x }, { "y", y }, { "p", new ArrayList { O.X, O.Y } } });
                tr.Commit();
                ed.WriteMessage("\n[TKPCCC] PCCC_TRUC " + (x.Count + 1) + "x" + (y.Count + 1) + " truc ve=" + g.Ve);
            }
        }

        static void TuongTay()
        {
            Document doc = AcApp.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;
            string loai = HoiTu(ed, "\nLoại", "Tuong Vach", "Tuong");
            if (loai == null) return;
            bool vach = loai == "Vach";
            kyHieu = null;
            double macDinh = LaySo(NhomKyHieu("kien_truc"), vach ? "d_vach" : "d_tuong", vach ? 100 : 200);
            PromptDoubleOptions dd = new PromptDoubleOptions("\nDày (đơn vị bản vẽ) <Enter = " + macDinh.ToString(CultureInfo.InvariantCulture) + ">: ");
            dd.AllowNegative = false; dd.AllowZero = false; dd.AllowNone = true;
            PromptDoubleResult dr = ed.GetDouble(dd);
            if (dr.Status != PromptStatus.OK && dr.Status != PromptStatus.None) return;
            double d = dr.Status == PromptStatus.OK ? dr.Value : macDinh;

            Matrix3d ucs = ed.CurrentUserCoordinateSystem;
            PromptPointResult p0 = ed.GetPoint("\nĐiểm đầu đường tim: ");
            if (p0.Status != PromptStatus.OK) return;
            List<Point3d> ps = new List<Point3d> { p0.Value };
            bool kin = false;
            while (true)
            {
                PromptPointOptions po = new PromptPointOptions("\nĐiểm tiếp theo <Enter = xong>");
                po.Keywords.Add("Dong");
                po.AllowNone = true;
                po.UseBasePoint = true;
                po.BasePoint = ps[ps.Count - 1];
                PromptPointResult pr = ed.GetPoint(po);
                if (pr.Status == PromptStatus.Keyword) { kin = true; break; }
                if (pr.Status != PromptStatus.OK) break;
                ps.Add(pr.Value);
            }
            using (Transaction tr = doc.Database.TransactionManager.StartTransaction())
            {
                GoiSpec g = GoiTay(doc, tr);
                ObjectId lop = LopKienTruc(g.Db, tr, vach ? "vach" : "tuong");
                foreach (Polyline pl in DuongTuong(ps.Select(q => q.TransformBy(ucs)).ToList(), kin, d))
                {
                    pl.LayerId = lop;
                    Them(g, pl);
                }
                tr.Commit();
                ed.WriteMessage("\n[TKPCCC] PCCC_TUONG " + (vach ? "vach" : "tuong") + " d=" + d.ToString(CultureInfo.InvariantCulture) + " ve=" + g.Ve);
            }
        }

        static void CuaTay()
        {
            Document doc = AcApp.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;
            Matrix3d ucs = ed.CurrentUserCoordinateSystem;
            PromptPointResult pr = ed.GetPoint("\nTâm cửa trên tường: ");
            if (pr.Status != PromptStatus.OK) return;
            kyHieu = null;
            double macDinh = LaySo(NhomKyHieu("kien_truc"), "rong_cua", 900);
            PromptDoubleOptions wo = new PromptDoubleOptions("\nRộng cửa <Enter = " + macDinh.ToString(CultureInfo.InvariantCulture) + ">: ");
            wo.AllowNegative = false; wo.AllowZero = false; wo.AllowNone = true;
            PromptDoubleResult wr = ed.GetDouble(wo);
            if (wr.Status != PromptStatus.OK && wr.Status != PromptStatus.None) return;
            PromptPointOptions vo = new PromptPointOptions("\nBấm một điểm phía cánh mở vào: ");
            vo.UseBasePoint = true; vo.BasePoint = pr.Value;
            PromptPointResult vr = ed.GetPoint(vo);
            if (vr.Status != PromptStatus.OK) return;
            string mo = HoiTu(ed, "\nBản lề (đứng phía cánh mở nhìn vào tường)", "Trai Phai", "Trai");
            if (mo == null) return;
            using (Transaction tr = doc.Database.TransactionManager.StartTransaction())
            {
                GoiSpec g = GoiTay(doc, tr);
                int xoa;
                foreach (Entity e in CatCua(g.Db, tr, g.Ms, pr.Value.TransformBy(ucs), wr.Status == PromptStatus.OK ? wr.Value : macDinh,
                    vr.Value.TransformBy(ucs), mo == "Trai", LopKienTruc(g.Db, tr, "cua"), out xoa))
                    Them(g, e);
                tr.Commit();
                ed.WriteMessage("\n[TKPCCC] PCCC_CUA ve=" + g.Ve + " thay=" + xoa);
            }
        }

        /// Lệnh gõ tay vẽ kiến trúc (lớp riêng; tên lệnh cố định vì palette gọi theo tên). Danh sách PCCC_HELP: Lenh.DS_LENH.
        public class LenhKienTruc
        {
            [CommandMethod("PCCC_TRUC")] public void LenhTruc() { Chay(TrucTay); }
            [CommandMethod("PCCC_TUONG")] public void LenhTuong() { Chay(TuongTay); }
            [CommandMethod("PCCC_CUA")] public void LenhCua() { Chay(CuaTay); }
        }
    }
}
