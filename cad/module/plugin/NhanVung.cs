// Tự nhận diện vùng phòng trên mặt bằng kiến trúc chưa có polyline kín (người dùng test 13/11, việc 2).
// Bỏ trục và nét không phải tường: kiểu nét có gạch (CENTER, DASHDOT, nét khuất), layer trục / kích thước / cửa / nội thất,
// chữ, kích thước. Lấy nét liền của tường, cột (kể cả nét trong block và biên hatch cột), nối khe THẲNG HÀNG ≤ "ho"
// (ô cửa giữa hai má tường, cột chỉ có hatch), rồi lấy mặt kín NHỎ NHẤT chứa điểm bấm.
// Dùng cho thao tác spec "vung_tu_dong" và PCCC_RAI quét chọn mặt bằng (Rai.cs). Chỉ đọc bản vẽ qua DatabaseServices:
// chạy được trong accoreconsole, không phụ thuộc vùng đang zoom như Editor.TraceBoundary.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;

namespace TKPCCC
{
    public partial class Lenh
    {
        const string LAYER_VUNG = "PCCC_VUNG";
        // shortcut: khe thẳng hàng ≤ 3 m coi là ô cửa và nối kín; cửa cuốn, cửa lớn rộng hơn thì truyền "ho" (spec)
        const double HO_CUA_M = 3.0;
        // Tên layer so sau khi bỏ dấu và bỏ ký tự không phải chữ, số (A-WALL -> AWALL, "Trục" -> TRUC). Xét LOP_BO trước LOP_TUONG.
        static readonly string[] LOP_BO = { "TRUC", "AXIS", "GRID", "CENTER", "DIM", "CUA", "DOOR", "WINDOW", "NOITHAT", "FURN", "DEFPOINTS" };
        static readonly string[] LOP_TUONG = { "TUONG", "WALL", "COT", "COLUMN", "VACH" };

        class NenVung
        {
            public Database Db;
            public Transaction Tr;
            public double Tol;                                                            // sai số nối điểm (2 mm)
            public bool ChiTuong;                                                         // chỉ lấy layer tường, cột (+ PCCC_VUNG)
            public List<KeyValuePair<ObjectId, Extents3d>> Ds = new List<KeyValuePair<ObjectId, Extents3d>>();
            public Extents3d? ToanBo;
            public Dictionary<ObjectId, int> Lop = new Dictionary<ObjectId, int>();      // 0 bỏ, 1 thường, 2 tường
            public Dictionary<ObjectId, bool> Lien = new Dictionary<ObjectId, bool>();   // kiểu nét không có gạch
            public Dictionary<ObjectId, List<Point2d[]>> NetCua = new Dictionary<ObjectId, List<Point2d[]>>(); // đoạn của từng đối tượng, theo chế độ
        }

        static string ChuanTen(string s)
        {
            StringBuilder b = new StringBuilder();
            foreach (char c in s.Normalize(NormalizationForm.FormD))
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark) continue;
                char d = c == 'đ' || c == 'Đ' ? 'D' : char.ToUpperInvariant(c);
                if (char.IsLetterOrDigit(d)) b.Append(d);
            }
            return b.ToString();
        }

        static int LoaiLop(NenVung n, ObjectId id)
        {
            int k;
            if (n.Lop.TryGetValue(id, out k)) return k;
            LayerTableRecord l = n.Tr.GetObject(id, OpenMode.ForRead) as LayerTableRecord;
            string t = l == null ? "" : ChuanTen(l.Name);
            if (l == null || l.IsFrozen || l.IsOff) k = 0;
            // Vùng đã dựng là nét thường (không bật chế độ chỉ-tường): vùng lớn cả tầng không được nuốt phòng bấm bên trong.
            else if (string.Equals(l.Name, LAYER_VUNG, StringComparison.OrdinalIgnoreCase)) k = 1;
            else if (l.Name.StartsWith("PCCC_", StringComparison.OrdinalIgnoreCase)) k = 0;        // thiết bị, ống, kích thước của plugin
            else if (LOP_BO.Any(x => t.Contains(x))) k = 0;
            else if (LOP_TUONG.Any(x => t.Contains(x))) k = 2;
            else k = 1;
            n.Lop[id] = k;
            return k;
        }

        static bool NetLien(NenVung n, ObjectId id)
        {
            bool k;
            if (n.Lien.TryGetValue(id, out k)) return k;
            LinetypeTableRecord r = id.IsNull ? null : n.Tr.GetObject(id, OpenMode.ForRead) as LinetypeTableRecord;
            k = r == null || r.NumDashes == 0;
            n.Lien[id] = k;
            return k;
        }

        /// Kiểu nét thật: ByLayer theo layer thật (lop, đã tính layer 0 trong block kế thừa block), ByBlock theo block chứa nó.
        static ObjectId KieuNet(NenVung n, Entity e, ObjectId lop, ObjectId cha)
        {
            ObjectId lt = e.LinetypeId;
            if (lt == n.Db.ByLayerLinetype)
            {
                LayerTableRecord l = n.Tr.GetObject(lop, OpenMode.ForRead) as LayerTableRecord;
                return l == null ? ObjectId.Null : l.LinetypeObjectId;
            }
            return lt == n.Db.ByBlockLinetype ? cha : lt;
        }

        static void ThemDoan(NenVung n, List<Point2d[]> ra, Point2d a, Point2d b)
        {
            if (a.GetDistanceTo(b) > n.Tol) ra.Add(new[] { a, b });
        }

        static void ThemDay(NenVung n, List<Point2d[]> ra, IList<Point2d> ps)
        {
            for (int i = 0; i + 1 < ps.Count; i++) ThemDoan(n, ra, ps[i], ps[i + 1]);
        }

        static void ThemCurve2d(NenVung n, List<Point2d[]> ra, Curve2d c)
        {
            LineSegment2d s = c as LineSegment2d;
            if (s != null) ThemDoan(n, ra, s.StartPoint, s.EndPoint);
            else ThemDay(n, ra, c.GetSamplePoints(13));
        }

        /// Đoạn thẳng của một đối tượng (cung chia nhỏ). chaLop, chaNet: layer, kiểu nét của block chứa nó (Null ở model space).
        static void ThuNet(NenVung n, Entity e, ObjectId chaLop, ObjectId chaNet, int sau, List<Point2d[]> ra)
        {
            if (!e.Visible) return;
            ObjectId lop = !chaLop.IsNull && e.LayerId == n.Db.LayerZero ? chaLop : e.LayerId;
            int loai = LoaiLop(n, lop);
            if (loai == 0) return;
            ObjectId net = KieuNet(n, e, lop, chaNet);
            BlockReference br = e as BlockReference;
            if (br != null)
            {
                if (br is Table || sau > 6) return;
                DBObjectCollection con = new DBObjectCollection();
                try { br.Explode(con); } catch (System.Exception) { }
                foreach (DBObject x in con)
                {
                    Entity c = x as Entity;
                    if (c != null) ThuNet(n, c, lop, net, sau + 1, ra);
                    x.Dispose();
                }
                return;
            }
            if (n.ChiTuong && loai != 2) return;
            if (!NetLien(n, net)) return; // trục (CENTER, DASHDOT), nét khuất
            try
            {
                Line ln = e as Line;
                Polyline pl = e as Polyline;
                Hatch h = e as Hatch;
                if (ln != null) ThemDoan(n, ra, new Point2d(ln.StartPoint.X, ln.StartPoint.Y), new Point2d(ln.EndPoint.X, ln.EndPoint.Y));
                else if (pl != null)
                {
                    int so = pl.Closed ? pl.NumberOfVertices : pl.NumberOfVertices - 1;
                    for (int i = 0; i < so; i++)
                    {
                        SegmentType st = pl.GetSegmentType(i);
                        if (st == SegmentType.Line) { LineSegment2d s = pl.GetLineSegment2dAt(i); ThemDoan(n, ra, s.StartPoint, s.EndPoint); }
                        else if (st == SegmentType.Arc) ThemCurve2d(n, ra, pl.GetArcSegment2dAt(i));
                    }
                }
                else if (h != null)
                {
                    // Biên hatch (cột tô đặc không có nét viền). shortcut: coi hatch nằm trên mặt XY (pháp tuyến Z), hatch nghiêng thì bỏ qua sai.
                    for (int i = 0; i < h.NumberOfLoops; i++)
                    {
                        HatchLoop lp = h.GetLoopAt(i);
                        if (lp.IsPolyline)
                        {
                            BulgeVertexCollection bv = lp.Polyline;
                            for (int j = 0; j < bv.Count; j++)
                            {
                                Point2d a = bv[j].Vertex, b = bv[(j + 1) % bv.Count].Vertex;
                                if (Math.Abs(bv[j].Bulge) < 1e-9) ThemDoan(n, ra, a, b);
                                else if (a.GetDistanceTo(b) > n.Tol) ThemCurve2d(n, ra, new CircularArc2d(a, b, bv[j].Bulge, false));
                            }
                        }
                        else foreach (Curve2d c in lp.Curves) ThemCurve2d(n, ra, c);
                    }
                }
                else
                {
                    Curve cv = e as Curve;
                    if (cv == null) return;
                    // Polyline2d/3d cũ: đỉnh ứng với tham số nguyên. Cung, tròn, elip, spline: chia 24 khúc.
                    bool theoDinh = cv is Polyline2d || cv is Polyline3d;
                    double t0 = cv.StartParam, t1 = cv.EndParam, buoc = theoDinh ? 1 : (t1 - t0) / 24;
                    List<Point2d> ps = new List<Point2d>();
                    for (double t = t0; t <= t1 + 1e-9 && buoc > 0; t += buoc)
                    {
                        Point3d q = cv.GetPointAtParameter(Math.Min(t, t1));
                        ps.Add(new Point2d(q.X, q.Y));
                    }
                    ThemDay(n, ra, ps);
                }
            }
            catch (System.Exception) { } // đối tượng hỏng hình học: bỏ, không làm hỏng cả lần dò
        }

        /// Đọc một lần mọi đối tượng có thể là tường trong không gian hiện hành, kèm hộp bao.
        static void DocDs(NenVung n)
        {
            BlockTableRecord ms = (BlockTableRecord)n.Tr.GetObject(n.Db.CurrentSpaceId, OpenMode.ForRead);
            foreach (ObjectId id in ms)
            {
                Entity e = n.Tr.GetObject(id, OpenMode.ForRead) as Entity;
                if (e == null || !e.Visible || LoaiLop(n, e.LayerId) == 0) continue;
                if (!(e is Curve || e is Hatch || e is BlockReference) || e is Table || e is Xline || e is Ray) continue;
                Extents3d x;
                try { x = e.GeometricExtents; } catch (System.Exception) { continue; }
                n.Ds.Add(new KeyValuePair<ObjectId, Extents3d>(id, x));
                if (n.ToanBo.HasValue) { Extents3d t = n.ToanBo.Value; t.AddExtents(x); n.ToanBo = t; } else n.ToanBo = x;
            }
        }

        static double DienTichDaGiac(List<Point2d> d)
        {
            double s = 0;
            for (int i = 0, j = d.Count - 1; i < d.Count; j = i++) s += (d[j].X - d[i].X) * (d[j].Y + d[i].Y);
            return s / 2; // dương khi đi ngược chiều kim đồng hồ
        }

        static int[] SapX(List<Point2d[]> ds)
        {
            return Enumerable.Range(0, ds.Count).OrderBy(i => Math.Min(ds[i][0].X, ds[i][1].X)).ToArray();
        }

        static void TrenDoan(Point2d a, Point2d b, Point2d q, double tol, List<double> tham)
        {
            Vector2d r = b - a;
            double t = r.DotProduct(q - a) / r.DotProduct(r);
            if (t > 0 && t < 1 && (a + r * t).GetDistanceTo(q) <= tol) tham.Add(t);
        }

        /// Mặt kín nhỏ nhất (ngược chiều kim đồng hồ) chứa p dựng từ các đoạn. null nếu p ở ngoài mọi mặt kín.
        static List<Point2d> MatChua(List<Point2d[]> doan0, Point2d p, double tol, double ho)
        {
            List<Point2d> tot = null;
            double dtTot = double.MaxValue;
            foreach (List<Point2d> v in CacMat(doan0, tol, ho))
            {
                double dt = DienTichDaGiac(v);
                if (dt < dtTot && TrongDaGiac(v, p)) { tot = v; dtTot = dt; }
            }
            return tot == null ? null : BoThangHang(tot, tol);
        }

        /// Mọi mặt kín (ngược chiều kim đồng hồ, diện tích dương, chưa bỏ đỉnh thẳng hàng) dựng từ các đoạn.
        static List<List<Point2d>> CacMat(List<Point2d[]> doan0, double tol, double ho)
        {
            List<Point2d[]> doan = new List<Point2d[]>(doan0);
            double tolTh = tol * 5; // lệch khỏi đường thẳng vẫn coi là thẳng hàng (10 mm)

            // 1. Nối khe giữa hai đoạn thẳng hàng (ô cửa giữa hai má tường, cột chỉ có hatch): cầu nối là cạnh như tường.
            int[] tt = SapX(doan);
            List<Point2d[]> cau = new List<Point2d[]>();
            for (int ii = 0; ii < tt.Length; ii++)
            {
                Point2d a = doan[tt[ii]][0], b = doan[tt[ii]][1];
                double xMax = Math.Max(a.X, b.X), yMin = Math.Min(a.Y, b.Y), yMax = Math.Max(a.Y, b.Y);
                Vector2d u = b - a;
                double L = u.Length;
                u = u / L;
                for (int jj = ii + 1; jj < tt.Length; jj++)
                {
                    Point2d c = doan[tt[jj]][0], d = doan[tt[jj]][1];
                    if (Math.Min(c.X, d.X) > xMax + ho) break;
                    if (Math.Min(c.Y, d.Y) > yMax + ho || Math.Max(c.Y, d.Y) < yMin - ho) continue;
                    Vector2d v = d - c;
                    if (Math.Abs(u.X * v.Y - u.Y * v.X) > 0.01 * v.Length) continue;
                    if (Math.Abs(u.X * (c.Y - a.Y) - u.Y * (c.X - a.X)) > tolTh || Math.Abs(u.X * (d.Y - a.Y) - u.Y * (d.X - a.X)) > tolTh) continue;
                    double sc = u.DotProduct(c - a), sd = u.DotProduct(d - a);
                    double sMin = Math.Min(sc, sd), sMax = Math.Max(sc, sd);
                    if (sMin > L + tol && sMin - L <= ho) cau.Add(new[] { b, sc < sd ? c : d });
                    else if (sMax < -tol && -sMax <= ho) cau.Add(new[] { sc < sd ? d : c, a });
                }
            }
            doan.AddRange(cau);

            // 2. Cắt mọi đoạn tại giao điểm, chỗ chạm (chữ T hụt ≤ tol) và chỗ chồng nhau thẳng hàng.
            int m = doan.Count;
            List<double>[] tham = new List<double>[m];
            for (int i = 0; i < m; i++) tham[i] = new List<double> { 0, 1 };
            tt = SapX(doan);
            for (int ii = 0; ii < m; ii++)
            {
                int i = tt[ii];
                Point2d a = doan[i][0], b = doan[i][1];
                Vector2d r = b - a;
                double Li = r.Length, xMax = Math.Max(a.X, b.X), yMin = Math.Min(a.Y, b.Y), yMax = Math.Max(a.Y, b.Y);
                for (int jj = ii + 1; jj < m; jj++)
                {
                    int j = tt[jj];
                    Point2d c = doan[j][0], d = doan[j][1];
                    if (Math.Min(c.X, d.X) > xMax + tol) break;
                    if (Math.Min(c.Y, d.Y) > yMax + tol || Math.Max(c.Y, d.Y) < yMin - tol) continue;
                    Vector2d s = d - c;
                    double Lj = s.Length, mau = r.X * s.Y - r.Y * s.X;
                    if (Math.Abs(mau) > 1e-6 * Li * Lj)
                    {
                        Vector2d w = c - a;
                        double t = (w.X * s.Y - w.Y * s.X) / mau, q = (w.X * r.Y - w.Y * r.X) / mau;
                        double ei = tol / Li, ej = tol / Lj;
                        if (t >= -ei && t <= 1 + ei && q >= -ej && q <= 1 + ej)
                        {
                            tham[i].Add(Math.Max(0, Math.Min(1, t)));
                            tham[j].Add(Math.Max(0, Math.Min(1, q)));
                        }
                    }
                    TrenDoan(a, b, c, tol, tham[i]); TrenDoan(a, b, d, tol, tham[i]);
                    TrenDoan(c, d, a, tol, tham[j]); TrenDoan(c, d, b, tol, tham[j]);
                }
            }

            // 3. Đỉnh (gộp điểm cách nhau ≤ 1,5 tol) và cạnh không trùng.
            List<Point2d> dinh = new List<Point2d>();
            Dictionary<long, List<int>> o = new Dictionary<long, List<int>>();
            double co = tol * 1.5;
            Func<Point2d, int> layDinh = delegate (Point2d q)
            {
                long ix = (long)Math.Floor(q.X / co), iy = (long)Math.Floor(q.Y / co);
                for (long x = ix - 1; x <= ix + 1; x++)
                    for (long y = iy - 1; y <= iy + 1; y++)
                    {
                        List<int> l;
                        if (o.TryGetValue(x * 73856093L ^ y * 19349663L, out l))
                            foreach (int k in l) if (dinh[k].GetDistanceTo(q) <= co) return k;
                    }
                long khoa = ix * 73856093L ^ iy * 19349663L;
                List<int> moi;
                if (!o.TryGetValue(khoa, out moi)) { moi = new List<int>(); o[khoa] = moi; }
                moi.Add(dinh.Count);
                dinh.Add(q);
                return dinh.Count - 1;
            };
            HashSet<long> canh = new HashSet<long>();
            for (int i = 0; i < m; i++)
            {
                Point2d a = doan[i][0];
                Vector2d r = doan[i][1] - a;
                int truoc = -1;
                foreach (double t in tham[i].Distinct().OrderBy(x => x))
                {
                    int k = layDinh(a + r * t);
                    if (truoc >= 0 && k != truoc) canh.Add(((long)Math.Min(k, truoc) << 32) | (uint)Math.Max(k, truoc));
                    truoc = k;
                }
            }
            int nd = dinh.Count;
            List<int>[] ke = new List<int>[nd];
            for (int i = 0; i < nd; i++) ke[i] = new List<int>();
            foreach (long c in canh) { int a = (int)(c >> 32), b = (int)(c & 0xffffffffL); ke[a].Add(b); ke[b].Add(a); }

            // 4. Bỏ nhánh cụt (đầu tường thừa, nét lẻ) rồi xếp cạnh quanh mỗi đỉnh theo góc.
            Queue<int> cut = new Queue<int>(Enumerable.Range(0, nd).Where(i => ke[i].Count == 1));
            while (cut.Count > 0)
            {
                int v = cut.Dequeue();
                if (ke[v].Count != 1) continue;
                int w = ke[v][0];
                ke[v].Clear();
                ke[w].Remove(v);
                if (ke[w].Count == 1) cut.Enqueue(w);
            }
            for (int i = 0; i < nd; i++)
            {
                Point2d g0 = dinh[i];
                ke[i].Sort((x, y) => Math.Atan2(dinh[x].Y - g0.Y, dinh[x].X - g0.X).CompareTo(Math.Atan2(dinh[y].Y - g0.Y, dinh[y].X - g0.X)));
            }

            // 5. Đi từng mặt (mặt bên trái nửa cạnh); mặt kín đi ngược chiều kim đồng hồ, diện tích dương.
            HashSet<long> daDi = new HashSet<long>();
            List<List<Point2d>> mat = new List<List<Point2d>>();
            int tran = canh.Count * 2 + 2;
            for (int u0 = 0; u0 < nd; u0++)
                foreach (int v0 in ke[u0])
                {
                    if (daDi.Contains(((long)u0 << 32) | (uint)v0)) continue;
                    List<Point2d> vong = new List<Point2d>();
                    int a = u0, b = v0, dem = 0;
                    while (true)
                    {
                        daDi.Add(((long)a << 32) | (uint)b);
                        vong.Add(dinh[a]);
                        List<int> kb = ke[b];
                        int w = kb[(kb.IndexOf(a) - 1 + kb.Count) % kb.Count];
                        a = b; b = w;
                        if ((a == u0 && b == v0) || ++dem > tran) break;
                    }
                    if (dem > tran) continue;
                    if (DienTichDaGiac(vong) > tol * tol) mat.Add(vong);
                }
            return mat;
        }

        /// Bỏ đỉnh thẳng hàng (chỗ cắt, chỗ cầu nối).
        static List<Point2d> BoThangHang(List<Point2d> tot, double tol)
        {
            bool doi = true;
            while (doi && tot.Count > 3)
            {
                doi = false;
                for (int i = 0; i < tot.Count && tot.Count > 3; i++)
                {
                    Point2d a = tot[(i - 1 + tot.Count) % tot.Count], b = tot[i], c = tot[(i + 1) % tot.Count];
                    Vector2d ac = c - a;
                    double L = ac.Length;
                    if (L < tol || Math.Abs(ac.X * (b.Y - a.Y) - ac.Y * (b.X - a.X)) / L <= tol) { tot.RemoveAt(i); doi = true; i--; }
                }
            }
            return tot;
        }

        /// Đa giác phòng chứa điểm p (WCS). ho: khe thẳng hàng tối đa được nối (đơn vị bản vẽ). null + lyDo khi không dò được.
        /// Dò trong ô vuông quanh p, nới gấp đôi tới khi mặt tìm được nằm trọn trong ô (nét ngoài ô không thể cắt mặt đó) hoặc ô phủ cả bản vẽ.
        /// Có layer tường / cột thì dò bằng các layer đó trước, không ra mới dò mọi nét liền.
        static List<Point2d> VungTaiDiem(Database db, Transaction tr, Point2d p, double ho, out string lyDo)
        {
            double mm = 0.001 / HeSoMet(db);
            NenVung n = new NenVung();
            n.Db = db; n.Tr = tr; n.Tol = 2 * mm;
            DocDs(n);
            lyDo = null;
            if (!n.ToanBo.HasValue)
            {
                lyDo = "bản vẽ không có nét nào dùng làm tường được (mọi nét là trục, chữ, kích thước hoặc layer đang tắt)";
                return null;
            }
            Extents3d tb = n.ToanBo.Value;
            double rMax = new[] { p.X - tb.MinPoint.X, tb.MaxPoint.X - p.X, p.Y - tb.MinPoint.Y, tb.MaxPoint.Y - p.Y }.Max();
            bool coTuong = n.Lop.Values.Any(k => k == 2);
            foreach (bool chiTuong in new[] { true, false })
            {
                if (chiTuong && !coTuong) continue;
                n.ChiTuong = chiTuong;
                n.NetCua.Clear();
                for (double R = 10000 * mm; ; R *= 2)
                {
                    bool het = R >= rMax;
                    List<Point2d[]> doan = new List<Point2d[]>();
                    foreach (KeyValuePair<ObjectId, Extents3d> kv in n.Ds)
                    {
                        Extents3d x = kv.Value;
                        if (x.MaxPoint.X < p.X - R || x.MinPoint.X > p.X + R || x.MaxPoint.Y < p.Y - R || x.MinPoint.Y > p.Y + R) continue;
                        List<Point2d[]> net;
                        if (!n.NetCua.TryGetValue(kv.Key, out net))
                        {
                            net = new List<Point2d[]>();
                            ThuNet(n, (Entity)tr.GetObject(kv.Key, OpenMode.ForRead), ObjectId.Null, ObjectId.Null, 0, net);
                            n.NetCua[kv.Key] = net;
                        }
                        doan.AddRange(net);
                    }
                    List<Point2d> d = doan.Count < 3 ? null : MatChua(doan, p, n.Tol, ho);
                    if (d != null && (het || d.All(q => Math.Abs(q.X - p.X) < R && Math.Abs(q.Y - p.Y) < R))) return d;
                    if (het) break;
                }
            }
            lyDo = "không có đường bao kín quanh điểm (phòng hở khe rộng hơn " + Math.Round(ho / mm / 1000, 2).ToString(CultureInfo.InvariantCulture)
                + " m, hoặc điểm ở ngoài công trình)";
            return null;
        }

        static Polyline PlVung(List<Point2d> d, ObjectId lop)
        {
            return TaoPline(d.Select(q => new Point3d(q.X, q.Y, 0)).ToList(), true, lop);
        }

        /// PCCC_RAI quét chọn mặt bằng (người dùng dặn 14/11: bấm điểm dò cả bản vẽ rồi TraceBoundary làm treo AutoCAD):
        /// chỉ dựng đoạn từ các đối tượng đã chọn, một lần. co: polyline kín layer PCCC_VUNG trong vùng chọn (dùng thẳng, khi đó không dựng mặt).
        /// mat: mọi mặt kín dựng từ nét tường (WCS, đã bỏ đỉnh thẳng hàng).
        static void MatChon(Database db, ObjectId[] ids, out List<List<Point2d>> co, out List<List<Point2d>> mat)
        {
            co = new List<List<Point2d>>();
            mat = new List<List<Point2d>>();
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                NenVung n = new NenVung();
                n.Db = db; n.Tr = tr; n.Tol = 2 * 0.001 / HeSoMet(db);
                List<Entity> ds = new List<Entity>();
                foreach (ObjectId id in ids)
                {
                    Entity e = tr.GetObject(id, OpenMode.ForRead) as Entity;
                    Polyline pl = e as Polyline;
                    if (pl != null && pl.Closed && pl.NumberOfVertices >= 3 && string.Equals(pl.Layer, LAYER_VUNG, StringComparison.OrdinalIgnoreCase)) co.Add(DinhPl(pl));
                    else if (e != null && !(e is Table)) ds.Add(e);
                }
                bool coTuong = ds.Any(e => LoaiLop(n, e.LayerId) == 2);
                // Có layer tường / cột thì dựng bằng các layer đó trước, không ra phòng nào mới dùng mọi nét liền (như VungTaiDiem).
                foreach (bool chiTuong in new[] { true, false })
                {
                    if (co.Count > 0 || mat.Count > 0 || (chiTuong && !coTuong)) continue;
                    n.ChiTuong = chiTuong;
                    List<Point2d[]> doan = new List<Point2d[]>();
                    foreach (Entity e in ds) ThuNet(n, e, ObjectId.Null, ObjectId.Null, 0, doan);
                    if (doan.Count >= 3) mat = CacMat(doan, n.Tol, HO_CUA_M / HeSoMet(db)).Select(v => BoThangHang(v, n.Tol)).ToList();
                }
                tr.Commit();
            }
        }

        // shortcut: ngưỡng phòng cố định (rộng trung bình 2A/P ≥ 0,5 m, ≥ 1 m²); hộp kỹ thuật nhỏ hơn thì bấm điểm hoặc vẽ polyline PCCC_VUNG.
        /// Mặt là phòng thật: bỏ lòng tường, cột; bỏ mặt bao ngoài có quá nửa diện tích là phòng khác (vòng tường ngoài tách rời các tường trong).
        static List<List<Point2d>> PhongThat(List<List<Point2d>> mat, double m)
        {
            List<List<Point2d>> ung = mat.Where(v =>
            {
                double a = DienTichDaGiac(v) * m * m, p = 0;
                for (int i = 0; i < v.Count; i++) p += v[i].GetDistanceTo(v[(i + 1) % v.Count]) * m;
                return a >= 1 && 2 * a / p >= 0.5;
            }).ToList();
            Func<List<Point2d>, Point2d> diemTrong = g =>
            {
                Vector2d c = g[1] - g[0]; // mặt ngược chiều kim đồng hồ: lòng mặt ở bên trái cạnh
                return g[0] + c * 0.5 + new Vector2d(-c.Y, c.X) * 0.01;
            };
            return ung.Where(f => ung.Where(g => g != f && TrongDaGiac(f, diemTrong(g))).Sum(g => DienTichDaGiac(g)) <= 0.5 * DienTichDaGiac(f)).ToList();
        }

        /// Thao tác spec "vung_tu_dong": p [[x,y],...] điểm trong từng phòng -> polyline kín trên layer "l" (mặc định PCCC_VUNG).
        /// Một điểm không dò được thì báo lỗi, không vẽ vùng nào (gửi lại không bị trùng).
        static void OpVungTuDong(GoiSpec g, Dictionary<string, object> o)
        {
            List<Point3d> ps = DsP(g, Lay(o, "p"), "p");
            double m = HeSoMet(g.Db), ho = LaySo(o, "ho", HO_CUA_M / m);
            if (ho < 0) throw new System.Exception("\"ho\" phải ≥ 0");
            List<List<Point2d>> ds = new List<List<Point2d>>();
            for (int i = 0; i < ps.Count; i++)
            {
                string lyDo;
                List<Point2d> d = VungTaiDiem(g.Db, g.Tr, new Point2d(ps[i].X, ps[i].Y), ho, out lyDo);
                if (d == null)
                    throw new System.Exception("điểm " + i + " [" + Lt(ps[i].X - g.Gx).ToString(CultureInfo.InvariantCulture) + ","
                        + Lt(ps[i].Y - g.Gy).ToString(CultureInfo.InvariantCulture) + "]: " + lyDo);
                ds.Add(d);
            }
            ObjectId lop = DamBaoLayer(g.Db, g.Tr, LayChu(o, "l", LAYER_VUNG), (short)LaySo(o, "mau", 6));
            foreach (List<Point2d> d in ds)
            {
                Them(g, PlVung(d, lop));
                // hop theo gốc của gói, làm tròn RA NGOÀI: dán thẳng vào "hop" của "rai" là lấy đúng polyline này.
                g.KqVung.Add(new Dictionary<string, object> {
                    { "m2", Math.Round(DienTichDaGiac(d) * m * m, 2) }, { "dinh", d.Count },
                    { "hop", new double[] { Math.Floor(d.Min(q => q.X) - g.Gx), Math.Floor(d.Min(q => q.Y) - g.Gy), Math.Ceiling(d.Max(q => q.X) - g.Gx), Math.Ceiling(d.Max(q => q.Y) - g.Gy) } } });
            }
        }
    }
}
