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
using Autodesk.AutoCAD.Colors;
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
        // Người dùng chốt 14/11 "cứ có cửa mình khép lại 1 vùng": khe 3–12 m vẫn khép khi có ký hiệu cửa hoặc là ô cửa trên tường hai nét (CacMat).
        // shortcut: trần 12 m cho cửa cuốn xưởng; cửa rộng hơn thì vẽ polyline PCCC_VUNG.
        const double HO_CUA_MAX_M = 12.0;
        // Tên layer so sau khi bỏ dấu và bỏ ký tự không phải chữ, số (A-WALL -> AWALL, "Trục" -> TRUC). Xét LOP_BO trước LOP_TUONG.
        static readonly string[] LOP_BO = { "TRUC", "AXIS", "GRID", "CENTER", "DIM", "CUA", "DOOR", "WINDOW", "NOITHAT", "FURN", "DEFPOINTS" };
        static readonly string[] LOP_TUONG = { "TUONG", "WALL", "COT", "COLUMN", "VACH" };

        /// Băm khoá long (cặp đỉnh a<<32|b): băm mặc định của long là hi ^ lo, cặp đỉnh liền chỉ số trùng mã băm hàng loạt (HashSet thành bậc hai).
        class KhoaLong : IEqualityComparer<long>
        {
            public bool Equals(long a, long b) { return a == b; }
            public int GetHashCode(long x) { unchecked { return (int)(((ulong)x * 0x9E3779B97F4A7C15UL) >> 32); } }
        }
        static readonly KhoaLong KHOA_LONG = new KhoaLong();

        /// Lưới ô vuông chia không gian cho đoạn: tìm đoạn có hộp bao chạm một hộp mà không quét cả danh sách.
        class LuoiDoan
        {
            readonly double c;
            readonly Dictionary<long, List<int>> o = new Dictionary<long, List<int>>(KHOA_LONG);
            readonly int[] dau;
            int lan;
            public LuoiDoan(List<Point2d[]> ds, double canhO)
            {
                c = canhO;
                dau = new int[ds.Count];
                for (int i = 0; i < ds.Count; i++)
                {
                    Point2d a = ds[i][0], b = ds[i][1];
                    long x0 = (long)Math.Floor(Math.Min(a.X, b.X) / c), x1 = (long)Math.Floor(Math.Max(a.X, b.X) / c);
                    long y0 = (long)Math.Floor(Math.Min(a.Y, b.Y) / c), y1 = (long)Math.Floor(Math.Max(a.Y, b.Y) / c);
                    for (long x = x0; x <= x1; x++)
                        for (long y = y0; y <= y1; y++)
                        {
                            List<int> l;
                            long k = (x << 32) ^ (y & 0xffffffffL);
                            if (!o.TryGetValue(k, out l)) { l = new List<int>(); o[k] = l; }
                            l.Add(i);
                        }
                }
            }
            /// Đoạn có ô chạm hộp [x0,x1] x [y0,y1] (mỗi đoạn một lần).
            public List<int> Tim(double x0, double y0, double x1, double y1)
            {
                List<int> kq = new List<int>();
                lan++;
                for (long x = (long)Math.Floor(x0 / c); x <= (long)Math.Floor(x1 / c); x++)
                    for (long y = (long)Math.Floor(y0 / c); y <= (long)Math.Floor(y1 / c); y++)
                    {
                        List<int> l;
                        if (!o.TryGetValue((x << 32) ^ (y & 0xffffffffL), out l)) continue;
                        foreach (int i in l) if (dau[i] != lan) { dau[i] = lan; kq.Add(i); }
                    }
                return kq;
            }
        }

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
            public HashSet<string> Mau;                                                   // nét phân vùng mẫu "LAYER|màu" (null = tự nhận từ tường)
            public List<Point2d[]> Cua = new List<Point2d[]>();                           // hộp bao [min, max] ký hiệu cửa (cung cánh, block cửa)
            public List<List<Point2d>> GachCheo = new List<List<Point2d>>();             // biên hatch gạch chéo = công trình hiện hữu (người dùng 14/11)
            public bool ChiCua;                                                           // đang đi trong block layer bị bỏ: chỉ gom ký hiệu cửa
            public HashSet<string> LopKhoiDam;                                            // layer dầm khi đang đọc block mặt bằng dầm chép đè: khớp mọi màu
            public bool DocDam;                                                           // lượt đọc nét dầm (Mau = nét dầm mẫu)
            public HashSet<string> BoMau;                                                 // khoá "LAYER|màu" bỏ ra (nét dầm khi dựng tường)
            public List<Point2d> KhuKhong = new List<Point2d>();                          // dấu hiệu lõi thang, buồng thang, WC (GomKhu)
            public List<List<Point2d>> GiengThang = new List<List<Point2d>>();           // hộp bao block thang máy (GomKhu): như ô X, phòng sát nó ≤ 30 m² = sảnh thang
            public double[] Hop;                                                          // [x0, y0, x1, y1] chỉ đọc đối tượng chạm hộp này (khung chọn nới 5 m); null = đọc hết
            public List<Point2d> MocTruc = new List<Point2d>();                          // tâm ký hiệu trục (GomTruc)
            public List<KeyValuePair<Point2d, double>> Tron = new List<KeyValuePair<Point2d, double>>(); // vòng 0,2–1 m (tâm, bán kính)
            public List<Point2d> ChuNgan = new List<Point2d>();                          // tâm chữ ≤ 5 ký tự
            public bool GomCot;                                                           // lượt dựng tường của MatChon: ghi ứng viên cột (L1)
            public List<CotUng> CotUng = new List<CotUng>();                             // polyline kín nhỏ (GhiCotUng), TachCot chọn cột thật
            public List<KeyValuePair<Point2d, string>> KhuTen = new List<KeyValuePair<Point2d, string>>(); // L5: tâm chữ khu không rải -> loại
            public List<Point2d> TenPhong = new List<Point2d>();                         // L8: tâm chữ có "PHÒNG" (tên phòng)
            public List<Point2d> TenChu = new List<Point2d>();                           // L5b: tâm chữ tên bất kỳ (≥ 3 chữ cái: KHO, BẢO VỆ...), không phải chữ khu
            public List<Point2d[]> ThangBo = new List<Point2d[]>();                      // L7: hộp bao [min, max] ký hiệu thang bộ (GomKhu)
            public Dictionary<string, bool> Bac = new Dictionary<string, bool>();         // L7: định nghĩa block + tỉ lệ + góc -> có hình bậc (HinhBac)
        }

        /// Ứng viên cột (L1): tâm, hai cạnh hộp bao (m), các đoạn của nó trong danh sách đoạn (cùng mảng, so theo tham chiếu).
        class CotUng
        {
            public Point2d Tam;
            public double R, C;
            public List<Point2d[]> Doan;
        }

        /// Thêm từ lượt dựng phòng (người dùng 20/11: dạy plugin phân vùng như AI, xưởng Quyết Thắng): tâm cột (L1, L2), chữ khu không rải (L5), chữ tên phòng (L8).
        class NhanThem
        {
            public List<Point2d> Cot = new List<Point2d>();
            public List<KeyValuePair<Point2d, string>> KhuTen = new List<KeyValuePair<Point2d, string>>();
            public List<Point2d> TenPhong = new List<Point2d>();
            public List<Point2d> TenChu = new List<Point2d>();      // L5b (review 21/11): phòng có chữ tên bất kỳ không bỏ theo mái che
            public List<Point2d[]> ThangBo = new List<Point2d[]>(); // L7: hộp bao ký hiệu thang bộ, khoét khỏi vùng
            public List<Point2d[]> Doan = new List<Point2d[]>();    // L6 (review 20/11): nét đã dựng mặt, xét mép hiên có tường (nét đôi) hay không
        }

        /// Màu thật của nét: ByLayer theo layer thật, ByBlock theo màu block chứa nó (chaMau; ngoài block coi như 7). "A3" = ACI 3, "R0,255,0" = màu thật.
        static string MauKhoa(NenVung n, Entity e, ObjectId lop, string chaMau)
        {
            Color c = e.Color;
            if (c.IsByBlock) return chaMau ?? "A7";
            if (c.IsByLayer)
            {
                LayerTableRecord l = n.Tr.GetObject(lop, OpenMode.ForRead) as LayerTableRecord;
                if (l != null) c = l.Color;
            }
            return c.ColorMethod == ColorMethod.ByAci || c.IsByLayer ? "A" + c.ColorIndex : "R" + c.Red + "," + c.Green + "," + c.Blue;
        }

        static string KhoaNet(NenVung n, ObjectId lop, string mau)
        {
            LayerTableRecord l = n.Tr.GetObject(lop, OpenMode.ForRead) as LayerTableRecord;
            return (l == null ? "" : l.Name.ToUpperInvariant()) + "|" + mau;
        }

        /// Khoá "LAYER|màu" của nét người dùng bấm làm mẫu (có thể nằm sâu trong block, như bản vẽ cả tờ là một block).
        static string KhoaNetMau(Database db, PromptNestedEntityResult r)
        {
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                NenVung n = new NenVung();
                n.Db = db; n.Tr = tr;
                ObjectId[] cha = r.GetContainers(); // block chứa trực tiếp đứng đầu: đi từ ngoài vào trong
                ObjectId lop = ObjectId.Null;
                string mau = null;
                for (int i = cha.Length - 1; i >= -1; i--)
                {
                    Entity e = (Entity)tr.GetObject(i >= 0 ? cha[i] : r.ObjectId, OpenMode.ForRead);
                    lop = !lop.IsNull && e.LayerId == db.LayerZero ? lop : e.LayerId;
                    mau = MauKhoa(n, e, lop, mau);
                }
                string k = KhoaNet(n, lop, mau);
                tr.Commit();
                return k;
            }
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
            else if (l.Name.StartsWith("PCCC_", StringComparison.OrdinalIgnoreCase)) k = -1;       // thiết bị, ống, kích thước của plugin (không xem cả ký hiệu cửa)
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

        /// Đoạn thẳng của một đối tượng (cung chia nhỏ). chaLop, chaNet, chaMau: layer, kiểu nét, màu thật của block chứa nó (Null ở model space).
        static void ThuNet(NenVung n, Entity e, ObjectId chaLop, ObjectId chaNet, string chaMau, int sau, List<Point2d[]> ra)
        {
            if (!e.Visible) return;
            // Người dùng 17/11: cả bộ hồ sơ là một block, chọn một tầng thì plugin vẫn đọc mọi tờ (vùng rơi ra ngoài khung, vùng lặp, 9,5 s): bỏ đối tượng ngoài khung.
            if (n.Hop != null)
                try
                {
                    Extents3d x = e.GeometricExtents;
                    if (x.MaxPoint.X < n.Hop[0] || x.MinPoint.X > n.Hop[2] || x.MaxPoint.Y < n.Hop[1] || x.MinPoint.Y > n.Hop[3]) return;
                }
                catch (System.Exception) { } // không có hộp bao: vẫn đọc
            ObjectId lop = !chaLop.IsNull && e.LayerId == n.Db.LayerZero ? chaLop : e.LayerId;
            int loai = LoaiLop(n, lop);
            if (loai < 0 && n.Mau == null) return;
            // Layer bỏ (cửa, nội thất...) không có nét mẫu: không lấy nét tường nhưng vẫn gom ký hiệu cửa. Có nét mẫu thì chỉ lọc theo mẫu (nét vùng có thể nằm trên layer bất kỳ).
            // Nét trong block layer bỏ mà layer RIÊNG là tường vẫn lấy (KOCHAM 16/11: block lõi thang "CORE" đặt trên layer "HANA FURNITURE",
            // tường AAP-TUONG bên trong bị bỏ nên PCCC_RAI gộp lõi thang + sảnh vào văn phòng).
            bool chiCua = (n.ChiCua && loai != 2) || (loai == 0 && n.Mau == null);
            GomCua(n, e, lop);
            GomKhu(n, e, lop);
            GomTruc(n, e, lop);
            ObjectId net = KieuNet(n, e, lop, chaNet);
            string mau = n.Mau == null && n.BoMau == null ? null : MauKhoa(n, e, lop, chaMau);
            BlockReference br = e as BlockReference;
            if (br != null)
            {
                if (br is Table || sau > 6) return;
                DBObjectCollection con = new DBObjectCollection();
                try { br.Explode(con); } catch (System.Exception) { }
                bool cu = n.ChiCua;
                n.ChiCua = chiCua;
                foreach (DBObject x in con)
                {
                    Entity c = x as Entity;
                    if (c != null) ThuNet(n, c, lop, net, mau, sau + 1, ra);
                    x.Dispose();
                }
                n.ChiCua = cu;
                return;
            }
            if (chiCua) return;
            Hatch hg = e as Hatch;
            if (hg != null && loai >= 0 && LaGachCheo(hg)) n.GachCheo.AddRange(VongHatch(hg)); // hatch của plugin (PCCC_THOAT_NAN...) không phải công trình hiện hữu
            string kh = n.BoMau != null || n.Mau != null ? KhoaNet(n, lop, mau) : null;
            bool lopDam = kh != null && n.LopKhoiDam != null && n.LopKhoiDam.Contains(kh.Substring(0, kh.IndexOf('|')));
            if (n.BoMau != null && !(e is Hatch) && (n.BoMau.Contains(kh) || lopDam)) return;
            if (n.Mau != null && (e is Hatch || !(n.Mau.Contains(kh) || (n.DocDam && lopDam)))) return;
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
                    int dau = ra.Count;
                    int so = pl.Closed ? pl.NumberOfVertices : pl.NumberOfVertices - 1;
                    for (int i = 0; i < so; i++)
                    {
                        SegmentType st = pl.GetSegmentType(i);
                        if (st == SegmentType.Line) { LineSegment2d s = pl.GetLineSegment2dAt(i); ThemDoan(n, ra, s.StartPoint, s.EndPoint); }
                        else if (st == SegmentType.Arc) ThemCurve2d(n, ra, pl.GetArcSegment2dAt(i));
                    }
                    if (n.GomCot) GhiCotUng(n, pl, ra, dau);
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

        /// Ký hiệu cửa (người dùng chốt 14/11 "cứ có cửa mình khép lại 1 vùng"): cung cánh cửa (ARC hoặc elip tròn) 60–120°, bán kính 0,4–3 m (hộp bao tâm + 2 đầu cung);
        /// block có tên (kể cả tên block động) hoặc layer chứa CUA / DOOR (hộp bao block). Khe tường 3–12 m cắt hộp này thì khép (CacMat).
        static void GomCua(NenVung n, Entity e, ObjectId lop)
        {
            try
            {
                Arc a = e as Arc;
                Ellipse el = e as Ellipse; // Karaoke ABC 21/11: cánh cửa vẽ bằng cung elip gần tròn (block A$Cf159a6b7, layer "net mo": tỉ lệ 0,9, 95°)
                if (a != null || (el != null && el.RadiusRatio >= 0.8))
                {
                    double goc = a != null ? a.TotalAngle * 180 / Math.PI : (el.EndAngle - el.StartAngle) * 180 / Math.PI;
                    if (goc < 0) goc += 360;
                    double r = (a != null ? a.Radius : el.MajorRadius) * 0.002 / n.Tol; // n.Tol = 2 mm theo đơn vị bản vẽ
                    if (goc < 60 || goc > 120 || r < 0.4 || r > 3) return;
                    Point3d[] q = a != null ? new[] { a.Center, a.StartPoint, a.EndPoint } : new[] { el.Center, el.StartPoint, el.EndPoint };
                    n.Cua.Add(new[] { new Point2d(q.Min(p => p.X), q.Min(p => p.Y)), new Point2d(q.Max(p => p.X), q.Max(p => p.Y)) });
                    return;
                }
                BlockReference br = e as BlockReference;
                if (br == null || br is Table) return;
                string ten = br.Name;
                if (br.IsDynamicBlock) ten += " " + ((BlockTableRecord)n.Tr.GetObject(br.DynamicBlockTableRecord, OpenMode.ForRead)).Name;
                LayerTableRecord l = n.Tr.GetObject(lop, OpenMode.ForRead) as LayerTableRecord;
                string t = ChuanTen(ten + " " + (l == null ? "" : l.Name));
                if (!t.Contains("CUA") && !t.Contains("DOOR")) return;
                Extents3d x = br.GeometricExtents;
                n.Cua.Add(new[] { new Point2d(x.MinPoint.X, x.MinPoint.Y), new Point2d(x.MaxPoint.X, x.MaxPoint.Y) });
            }
            catch (System.Exception) { } // block hỏng, không có hộp bao: bỏ
        }

        static readonly string[] KHU_KHONG_BAO = { "THANGMAY", "LIFT", "ELEVATOR", "THANGBO", "STAIR", "TOILET", "VESINH", "LAVABO", "BONCAU", "XIBET", "URINAL", "TIEUNAM" };

        /// Người dùng dặn 16/11: lõi thang máy, buồng thang, WC không đặt đầu báo. Dấu hiệu: block (tên, layer) thang máy, thang bộ, thiết bị vệ sinh
        /// (tâm hộp bao); chữ tên phòng WC, VỆ SINH, THANG MÁY, THANG BỘ (điểm chèn). Sảnh thang máy cũng không đặt đầu báo (người dùng dặn tối 16/11).
        static void GomKhu(NenVung n, Entity e, ObjectId lop)
        {
            try
            {
                BlockReference br = e as BlockReference;
                DBText dt = e as DBText;
                MText mt = e as MText;
                if (br != null && !(br is Table))
                {
                    string ten = br.Name;
                    if (br.IsDynamicBlock) ten += " " + ((BlockTableRecord)n.Tr.GetObject(br.DynamicBlockTableRecord, OpenMode.ForRead)).Name;
                    LayerTableRecord l = n.Tr.GetObject(lop, OpenMode.ForRead) as LayerTableRecord;
                    string t = ChuanTen(ten + " " + (l == null ? "" : l.Name));
                    bool khu = KHU_KHONG_BAO.Any(k => t.Contains(k)), thang;
                    // L7: thang bộ (tên THANG BỘ / STAIR, hoặc hình bậc khi tên vô nghĩa như "fsdff" ở Quyết Thắng) -> hộp bao khoét khỏi vùng (Rai.cs).
                    // Hình bậc nhớ theo định nghĩa block + tỉ lệ + góc: bản vẽ nhà máy 506 cột cùng một block không nổ lại từng cái.
                    if (khu) thang = t.Contains("THANGBO") || t.Contains("STAIR");
                    else
                    {
                        Scale3d sc = br.ScaleFactors;
                        string kb = br.BlockTableRecord.Handle + "|" + Math.Round(sc.X, 3) + "|" + Math.Round(sc.Y, 3) + "|" + Math.Round(br.Rotation, 2);
                        if (!n.Bac.TryGetValue(kb, out thang)) n.Bac[kb] = thang = HinhBac(n, br, br.GeometricExtents);
                    }
                    if (!khu && !thang) return;
                    Extents3d x = br.GeometricExtents;
                    if (thang)
                    {
                        Point2d a = new Point2d(x.MinPoint.X, x.MinPoint.Y), b = new Point2d(x.MaxPoint.X, x.MaxPoint.Y);
                        if (!n.ThangBo.Any(h => h[0].GetDistanceTo(a) <= n.Tol * 50 && h[1].GetDistanceTo(b) <= n.Tol * 50)) n.ThangBo.Add(new[] { a, b });
                    }
                    if (!khu) return;
                    n.KhuKhong.Add(new Point2d((x.MinPoint.X + x.MaxPoint.X) / 2, (x.MinPoint.Y + x.MaxPoint.Y) / 2));
                    // KOCHAM 17/11: giếng thang là block "BUONG THANG MAY" trong block lõi, không có nét X trên layer tường. Hộp ≤ 20 m² (một giếng, không phải cả lõi).
                    double rong = x.MaxPoint.X - x.MinPoint.X, cao = x.MaxPoint.Y - x.MinPoint.Y, m2 = rong * cao * Math.Pow(0.002 / n.Tol, 2);
                    if ((t.Contains("THANGMAY") || t.Contains("LIFT") || t.Contains("ELEVATOR")) && m2 > 0 && m2 <= 20)
                        n.GiengThang.Add(new List<Point2d> { new Point2d(x.MinPoint.X, x.MinPoint.Y), new Point2d(x.MaxPoint.X, x.MinPoint.Y),
                            new Point2d(x.MaxPoint.X, x.MaxPoint.Y), new Point2d(x.MinPoint.X, x.MaxPoint.Y) });
                }
                else if (dt != null || mt != null)
                {
                    string t = ChuanTen(dt != null ? dt.TextString : mt.Text);
                    GomChuKhu(n, e, t);
                    if (t.Length > 24 || !(t.StartsWith("WC") || KHU_KHONG_BAO.Any(k => t.Contains(k)))) return;
                    Point3d p = dt != null ? dt.Position : mt.Location;
                    n.KhuKhong.Add(new Point2d(p.X, p.Y));
                }
            }
            catch (System.Exception) { } // block hỏng, không có hộp bao: bỏ
        }

        /// L7: block hình bậc thang = ≥ 6 đoạn song song dài bằng nhau 0,6–2,5 m, giữa thẳng hàng, cách đều 0,2–0,4 m (bậc), hộp 0,8–8 m, ≤ 50 m².
        /// shortcut: chỉ xét nét con trực tiếp của block (line, polyline); bậc nằm sâu trong block lồng nữa thì không nhận, gặp thì đệ quy một tầng.
        static bool HinhBac(NenVung n, BlockReference br, Extents3d x)
        {
            double k = 0.002 / n.Tol, r = (x.MaxPoint.X - x.MinPoint.X) * k, c = (x.MaxPoint.Y - x.MinPoint.Y) * k;
            if (r < 0.8 || c < 0.8 || r > 8 || c > 8 || r * c > 50) return false;
            List<Point2d[]> s = new List<Point2d[]>();
            DBObjectCollection con = new DBObjectCollection();
            try { br.Explode(con); } catch (System.Exception) { }
            foreach (DBObject o in con)
            {
                Line ln = o as Line;
                Polyline pl = o as Polyline;
                if (ln != null) s.Add(new[] { new Point2d(ln.StartPoint.X, ln.StartPoint.Y), new Point2d(ln.EndPoint.X, ln.EndPoint.Y) });
                else if (pl != null)
                    for (int i = 0; i < (pl.Closed ? pl.NumberOfVertices : pl.NumberOfVertices - 1); i++)
                        if (pl.GetSegmentType(i) == SegmentType.Line) { LineSegment2d g = pl.GetLineSegment2dAt(i); s.Add(new[] { g.StartPoint, g.EndPoint }); }
                o.Dispose();
            }
            s = s.Where(q => { double L = q[0].GetDistanceTo(q[1]) * k; return L >= 0.6 && L <= 2.5; }).ToList();
            if (s.Count < 6 || s.Count > 2000) return false;
            foreach (Point2d[] a in s)
            {
                double La = a[0].GetDistanceTo(a[1]);
                Vector2d u = (a[1] - a[0]) / La;
                Point2d ma = a[0] + (a[1] - a[0]) * 0.5;
                List<double> lech = new List<double>(); // khoảng cách vuông góc (m) từ bậc a tới các bậc cùng hàng
                foreach (Point2d[] b in s)
                {
                    Vector2d v = b[1] - b[0];
                    if (Math.Abs(u.X * v.Y - u.Y * v.X) > 0.02 * v.Length || Math.Abs(v.Length - La) * k > 0.05) continue;
                    Vector2d w = b[0] + v * 0.5 - ma;
                    if (Math.Abs(u.DotProduct(w)) * k > 0.1) continue;
                    lech.Add((u.X * w.Y - u.Y * w.X) * k);
                }
                if (lech.Count < 6) continue;
                lech.Sort();
                int chuoi = 1;
                double buoc = 0;
                for (int i = 1; i < lech.Count; i++)
                {
                    double g = lech[i] - lech[i - 1];
                    if (g < 0.01) continue; // nét trùng
                    bool deu = g >= 0.2 && g <= 0.4;
                    if (deu && chuoi > 1 && Math.Abs(g - buoc) <= 0.03) chuoi++;
                    else { chuoi = deu ? 2 : 1; buoc = g; }
                    if (chuoi >= 6) return true;
                }
            }
            return false;
        }

        // L5 (người dùng duyệt 19/11, xưởng Quyết Thắng): khu có chữ này không rải đầu báo (mái che không công năng, công trình hiện hữu không gạch chéo,
        // phòng bơm PCCC đứng riêng). Chữ đã bỏ dấu, bỏ ký tự không phải chữ số (ChuanTen).
        static readonly string[][] KHU_TEN = { new[] { "MAICHE", "mái che" }, new[] { "KHONGCOCONGNANG", "không có công năng" }, new[] { "HIENHUU", "hiện hữu" },
            new[] { "PHONGBOM", "phòng bơm" }, new[] { "TRAMBOM", "trạm bơm" }, new[] { "NHABOM", "nhà bơm" } };

        /// Loại khu không rải theo chữ (KHU_TEN), null = không phải. SÂN chỉ khớp nguyên chữ: SÀN, SẢNH bỏ dấu cũng ra SAN...
        static string LoaiKhuTen(string t)
        {
            if (t == "SAN" || t == "SANTRONG" || t == "SANVUON") return "sân";
            foreach (string[] k in KHU_TEN) if (t.Contains(k[0])) return k[1];
            return null;
        }

        /// Chữ khu không rải (L5), chữ tên phòng có "PHÒNG" (L8: phòng có tên thì không bỏ vì nằm ngoài trục / cụm nhỏ) và chữ tên bất kỳ ≥ 3 chữ cái
        /// (L5b: KHO 1, BẢO VỆ không bỏ theo mái che; mã cửa D1, cốt +0.000 không tính). Lưu tâm hộp bao chữ.
        static void GomChuKhu(NenVung n, Entity e, string t)
        {
            try
            {
                string loai = LoaiKhuTen(t);
                if (t.Length > 30 && loai == null) return;
                bool phong = loai == null && t.Contains("PHONG") && !t.Contains("PHONGCHAY"); // review 21/11: "PHÒNG CHÁY CHỮA CHÁY" của khung tên không phải tên phòng
                if (loai == null && !phong)
                {
                    // shortcut: L5b lấy điểm chèn chữ, không tính hộp bao (bản vẽ thật nghìn chữ: GeometricExtents đẩy dựng phòng quá 10 s); chữ căn trái dài vượt tường thì vẫn đúng phòng.
                    if (t.Count(char.IsLetter) < 3) return;
                    DBText d = e as DBText;
                    Point3d q = d == null ? ((MText)e).Location : (d.Justify == AttachmentPoint.BaseLeft ? d.Position : d.AlignmentPoint);
                    n.TenChu.Add(new Point2d(q.X, q.Y));
                    return;
                }
                Extents3d x = e.GeometricExtents;
                Point2d c = new Point2d((x.MinPoint.X + x.MaxPoint.X) / 2, (x.MinPoint.Y + x.MaxPoint.Y) / 2);
                if (loai != null) { n.KhuTen.Add(new KeyValuePair<Point2d, string>(c, loai)); return; }
                n.TenChu.Add(c);
                n.TenPhong.Add(c);
            }
            catch (System.Exception) { } // chữ không có hộp bao: bỏ
        }

        // L1 (người dùng duyệt 20/11, xưởng Quyết Thắng): cột chữ I đặt trên layer tên ống ($.2.9DN25). Bản cánh, bụng chữ I là hai nét song song cách 96–131 mm,
        // CacMat coi là tường hai nét có lỗ mở rồi khép khe tới cột kế bên (SongDoi, ≤ 12 m) thành tường ảo, ra ô giả 4,7 x 5,2 m giữa hai hàng cột.
        // Cột nhận theo HÌNH: polyline kín nhỏ (hai cạnh hộp bao 0,1–1,2 m) cùng cỡ, lặp thẳng hàng ≥ 3 cái. Nét cột vẫn dựng mặt nhưng không khép khe (CacMat khongNoi).
        // shortcut: chỉ polyline kín (cả polyline trong block); cột vẽ bằng 4 line rời hoặc chỉ có hatch chưa nhận, gặp thì thêm.
        const double COT_MIN_M = 0.1, COT_MAX_M = 1.2;

        /// Ghi polyline kín nhỏ vừa thêm vào ra (từ chỉ số dau) làm ứng viên cột.
        static void GhiCotUng(NenVung n, Polyline pl, List<Point2d[]> ra, int dau)
        {
            int so = pl.NumberOfVertices;
            if (ra.Count - dau < 4 || so < 4) return;
            if (!pl.Closed && pl.GetPoint2dAt(0).GetDistanceTo(pl.GetPoint2dAt(so - 1)) > n.Tol) return;
            double x0 = double.MaxValue, y0 = double.MaxValue, x1 = double.MinValue, y1 = double.MinValue;
            for (int i = dau; i < ra.Count; i++)
                foreach (Point2d p in ra[i]) { x0 = Math.Min(x0, p.X); y0 = Math.Min(y0, p.Y); x1 = Math.Max(x1, p.X); y1 = Math.Max(y1, p.Y); }
            double k = 0.002 / n.Tol, r = (x1 - x0) * k, c = (y1 - y0) * k; // n.Tol = 2 mm theo đơn vị bản vẽ
            if (Math.Max(r, c) > COT_MAX_M || Math.Min(r, c) < COT_MIN_M) return;
            if (Math.Max(r, c) > 2.5 * Math.Min(r, c)) return; // review 20/11: trụ tường 1000 x 220 vẽ polyline kín để hatch không phải cột
            n.CotUng.Add(new CotUng { Tam = new Point2d((x0 + x1) / 2, (y0 + y1) / 2), R = r, C = c, Doan = ra.GetRange(dau, ra.Count - dau) });
        }

        /// Cột thật trong các ứng viên: cùng cỡ (lệch ≤ 50 mm, kể cả xoay 90°) và ≥ 3 cái cùng hàng hoặc cùng cột (tâm lệch ≤ 0,15 m).
        /// Thêm tâm cột vào tam; trả các đoạn của cột (CacMat không khép khe từ các đoạn này).
        static HashSet<Point2d[]> TachCot(NenVung n, List<Point2d> tam)
        {
            HashSet<Point2d[]> kq = new HashSet<Point2d[]>();
            double sai = 0.15 * n.Tol / 0.002;
            foreach (CotUng a in n.CotUng)
            {
                int hang = 0, cot = 0;
                foreach (CotUng b in n.CotUng)
                {
                    bool coCo = (Math.Abs(a.R - b.R) <= 0.05 && Math.Abs(a.C - b.C) <= 0.05) || (Math.Abs(a.R - b.C) <= 0.05 && Math.Abs(a.C - b.R) <= 0.05);
                    if (!coCo) continue;
                    if (Math.Abs(a.Tam.Y - b.Tam.Y) <= sai) hang++; // tính cả chính nó
                    if (Math.Abs(a.Tam.X - b.Tam.X) <= sai) cot++;
                }
                if (hang < 3 && cot < 3) continue;
                tam.Add(a.Tam);
                foreach (Point2d[] s in a.Doan) kq.Add(s);
            }
            return kq;
        }

        // L2 (người dùng duyệt 20/11, xưởng mái kèo, PCCC_RAI LoaiTran MaiKeo): khoang = giữa hai hàng vì kèo (tim hàng cột), chạy hết bề rộng xưởng tới tường;
        // không chia theo xà gồ. shortcut: chỉ chia phòng ≥ 200 m², hai cạnh hộp bao ≥ 10 m (văn phòng, phòng nhỏ giữ nguyên, L3); xưởng không vẽ cột
        // mượn hàng cột cách hộp bao ≤ 6 m (xưởng kề bên); phương chia là phương có nhiều hàng cắt lòng phòng hơn.
        const double VI_KEO_M2_MIN = 200, VI_KEO_CANH_MIN_M = 10, VI_KEO_GAN_M = 6;

        /// Đường tim hàng vì kèo cắt phòng z (đoạn phủ hết hộp bao, nới 1 m mỗi đầu), dùng làm "dầm" cho ChiaKhoang. Hàng = toạ độ có ≥ 2 cột (cot: mọi cột của
        /// mặt bằng); chỉ lấy hàng của cột gần phòng, cách biên hộp bao > 1 m (hàng cột nằm ở tường biên không chia gì).
        static List<Point2d[]> DuongViKeo(List<Point2d> z, List<Point2d> cot, double m)
        {
            List<Point2d[]> kq = new List<Point2d[]>();
            double x0 = z.Min(p => p.X), y0 = z.Min(p => p.Y), x1 = z.Max(p => p.X), y1 = z.Max(p => p.Y);
            if (cot.Count < 2 || Math.Abs(DienTichDaGiac(z)) * m * m < VI_KEO_M2_MIN || (x1 - x0) * m < VI_KEO_CANH_MIN_M || (y1 - y0) * m < VI_KEO_CANH_MIN_M) return kq;
            double gan = VI_KEO_GAN_M / m, sai = 0.15 / m, le = 1 / m, vao = 0.6 / m;
            List<Point2d> c = cot.Where(p => p.X >= x0 - gan && p.X <= x1 + gan && p.Y >= y0 - gan && p.Y <= y1 + gan).ToList();
            // Thử 20/11 (Quyết Thắng): cột hiên nhà văn phòng cách xưởng < 6 m thành 2 hàng thừa (3 khoang mỏng). Hàng phải có ≥ 2 cột nằm trong bề ngang
            // của chính phòng (± 0,6 m); chỉ khi phòng không có hàng nào như vậy mới mượn hàng của cột gần (xưởng kề bên không vẽ cột).
            Func<Func<Point2d, double>, Func<Point2d, double>, double, double, double, double, double, List<double>> hang = (toaDo, khac, a, b, ka, kb, nam) =>
            {
                List<double> h = new List<double>();
                foreach (double v in c.Select(toaDo).OrderBy(t => t))
                    if ((h.Count == 0 || v - h[h.Count - 1] > sai) && v > a + le && v < b - le
                        && cot.Count(q => Math.Abs(toaDo(q) - v) <= sai && khac(q) >= ka - nam && khac(q) <= kb + nam) >= 2) h.Add(v);
                // Dải sát biên hẹp hơn nửa bước vì kèo (phần lồi tường đầu hồi, 2 m) không thành khoang riêng: bỏ hàng đó, dải gộp vào khoang kề.
                if (h.Count >= 2)
                {
                    List<double> buoc = h.Zip(h.Skip(1), (p, q) => q - p).OrderBy(t => t).ToList();
                    double nua = buoc[buoc.Count / 2] / 2;
                    h = h.Where(v => v - a >= nua && b - v >= nua).ToList();
                }
                return h;
            };
            List<double> ys = hang(p => p.Y, p => p.X, y0, y1, x0, x1, vao), xs = hang(p => p.X, p => p.Y, x0, x1, y0, y1, vao);
            // Review 21/11: lượt mượn chỉ đếm cột trong bề ngang phòng nới thêm một bề ngang (xưởng kề bên cùng cỡ), không đếm cột cùng toạ độ ở bất kỳ đâu trên bản vẽ.
            if (ys.Count == 0 && xs.Count == 0) { ys = hang(p => p.Y, p => p.X, y0, y1, x0, x1, x1 - x0); xs = hang(p => p.X, p => p.Y, x0, x1, y0, y1, y1 - y0); }
            if (ys.Count == 0 && xs.Count == 0) return kq;
            if (ys.Count >= xs.Count) foreach (double y in ys) kq.Add(new[] { new Point2d(x0 - le, y), new Point2d(x1 + le, y) });
            else foreach (double x in xs) kq.Add(new[] { new Point2d(x, y0 - le), new Point2d(x, y1 + le) });
            return kq;
        }

        /// Ký hiệu trục (người dùng 17/11: "ngoài đường trục thì không phân vùng khi không được cho phép"; file KOCHAM vẽ trục bằng polyline nét liền layer 0
        /// trong block, cắt văn phòng): block có tên / layer chứa TRUC, AXIS, GRID cỡ 0,3–4 m, hoặc vòng tròn 0,2–1 m có chữ ngắn bên trong. Lưu tâm hộp bao.
        static void GomTruc(NenVung n, Entity e, ObjectId lop)
        {
            try
            {
                BlockReference br = e as BlockReference;
                Circle ci = e as Circle;
                DBText dt = e as DBText;
                MText mt = e as MText;
                if (br != null && !(br is Table))
                {
                    LayerTableRecord l = n.Tr.GetObject(lop, OpenMode.ForRead) as LayerTableRecord;
                    string t = ChuanTen(br.Name + " " + (l == null ? "" : l.Name));
                    if (!t.Contains("TRUC") && !t.Contains("AXIS") && !t.Contains("GRID")) return;
                    Extents3d x = br.GeometricExtents;
                    double co = Math.Max(x.MaxPoint.X - x.MinPoint.X, x.MaxPoint.Y - x.MinPoint.Y) * 0.002 / n.Tol;
                    if (co >= 0.3 && co <= 4) n.MocTruc.Add(new Point2d((x.MinPoint.X + x.MaxPoint.X) / 2, (x.MinPoint.Y + x.MaxPoint.Y) / 2));
                }
                else if (ci != null)
                {
                    double r = ci.Radius * 0.002 / n.Tol;
                    if (r >= 0.2 && r <= 1) n.Tron.Add(new KeyValuePair<Point2d, double>(new Point2d(ci.Center.X, ci.Center.Y), ci.Radius));
                }
                else if (dt != null || mt != null)
                {
                    string t = (dt != null ? dt.TextString : mt.Text).Trim();
                    if (t.Length == 0 || t.Length > 5) return;
                    Extents3d x = e.GeometricExtents;
                    n.ChuNgan.Add(new Point2d((x.MinPoint.X + x.MaxPoint.X) / 2, (x.MinPoint.Y + x.MaxPoint.Y) / 2));
                }
            }
            catch (System.Exception) { } // không có hộp bao: bỏ
        }

        /// Toạ độ trục từ tâm ký hiệu trục: các ký hiệu cùng cột (x lệch ≤ 0,5 m, ≥ 2 cái) là đầu trục ngang -> y; cùng hàng -> x (trục đứng).
        /// truc = {xs, ys}; khung = [x0, y0, x1, y1] trục ngoài cùng (NaN = không biết phương đó). Không đủ ký hiệu: null.
        /// hop (khung chọn): chỉ giữ trục đi qua khung (trục ngang có y trong khung, trục đứng có x trong khung), bỏ trục của tờ bên cạnh.
        static double[] KhungTruc(List<Point2d> moc, double m, double[] hop, out List<double>[] truc)
        {
            truc = new[] { new List<double>(), new List<double>() };
            double gan = 0.5 / m;
            foreach (Point2d p in moc)
            {
                // ≥ 3 ký hiệu: cặp ký hiệu hai đầu một trục (trái + phải) không thành "hàng".
                if (moc.Count(q => Math.Abs(q.Y - p.Y) <= gan) >= 3 && (hop == null || (p.X >= hop[0] && p.X <= hop[2]))) truc[0].Add(p.X); // cùng hàng: trục đứng
                if (moc.Count(q => Math.Abs(q.X - p.X) <= gan) >= 3 && (hop == null || (p.Y >= hop[1] && p.Y <= hop[3]))) truc[1].Add(p.Y); // cùng cột: trục ngang
            }
            if (truc[0].Count < 2 && truc[1].Count < 2) return null;
            return new[] { truc[0].Count >= 2 ? truc[0].Min() : double.NaN, truc[1].Count >= 2 ? truc[1].Min() : double.NaN,
                           truc[0].Count >= 2 ? truc[0].Max() : double.NaN, truc[1].Count >= 2 ? truc[1].Max() : double.NaN };
        }

        /// Hatch gạch chéo (ANSI31–36, hoặc kiểu User nét nghiêng 45°): người dùng 14/11 "gạch chéo = công trình hiện hữu, không rải".
        static bool LaGachCheo(Hatch h)
        {
            if (h.PatternType == HatchPatternType.UserDefined) return Math.Abs(Math.Abs(h.PatternAngle * 180 / Math.PI) % 90 - 45) <= 10;
            return new[] { "ANSI31", "ANSI32", "ANSI33", "ANSI34", "ANSI35", "ANSI36" }.Contains((h.PatternName ?? "").ToUpperInvariant());
        }

        /// Các vòng biên của hatch (WCS, cung lấy 9 điểm). shortcut: vòng lỗ trong hatch cũng tính là vùng gạch chéo.
        static List<List<Point2d>> VongHatch(Hatch h)
        {
            List<List<Point2d>> kq = new List<List<Point2d>>();
            try
            {
                for (int i = 0; i < h.NumberOfLoops; i++)
                {
                    HatchLoop lp = h.GetLoopAt(i);
                    List<Point2d> d = new List<Point2d>();
                    if (lp.IsPolyline) foreach (BulgeVertex v in lp.Polyline) d.Add(v.Vertex);
                    else foreach (Curve2d c in lp.Curves) d.AddRange(c.GetSamplePoints(c is LineSegment2d ? 2 : 9));
                    if (d.Count >= 3) kq.Add(d);
                }
            }
            catch (System.Exception) { }
            return kq;
        }

        static Point2d DiemGan(Point2d[] s, Point2d q)
        {
            Vector2d r = s[1] - s[0];
            double t = r.LengthSqrd < 1e-18 ? 0 : Math.Max(0, Math.Min(1, (q - s[0]).DotProduct(r) / r.LengthSqrd));
            return s[0] + r * t;
        }

        static double CachDoan(Point2d[] s, Point2d q)
        {
            return DiemGan(s, q).GetDistanceTo(q);
        }

        /// Ô có dấu X (người dùng chốt 14/11: lỗ thông tầng, không bố trí): hai đoạn dài ≥ 1 m, dài gần bằng nhau, cắt nhau tại trung điểm của cả hai,
        /// 4 đầu mút nằm trên nét khác (4 góc ô). Bỏ 2 đường chéo khỏi doan (ô thành một mặt), trả 4 góc từng ô (ngược chiều kim đồng hồ).
        static List<List<Point2d>> TachOX(List<Point2d[]> doan, double tol)
        {
            List<List<Point2d>> kq = new List<List<Point2d>>();
            int[] ds = Enumerable.Range(0, doan.Count).Where(i => doan[i][0].GetDistanceTo(doan[i][1]) >= tol * 500)
                .OrderBy(i => doan[i][0].X + doan[i][1].X).ToArray();
            HashSet<int> bo = new HashSet<int>();
            for (int ii = 0; ii < ds.Length; ii++)
            {
                int i = ds[ii];
                if (bo.Contains(i)) continue;
                Point2d a = doan[i][0], b = doan[i][1];
                double Li = a.GetDistanceTo(b), mx = (a.X + b.X) / 2, my = (a.Y + b.Y) / 2;
                for (int jj = ii + 1; jj < ds.Length; jj++)
                {
                    int j = ds[jj];
                    Point2d c = doan[j][0], d = doan[j][1];
                    if ((c.X + d.X) / 2 - mx > 0.03 * Li) break;
                    double Lj = c.GetDistanceTo(d);
                    if (bo.Contains(j) || Math.Abs(Li - Lj) > 0.05 * Li || Math.Abs((c.X + d.X) / 2 - mx) > 0.02 * Li || Math.Abs((c.Y + d.Y) / 2 - my) > 0.02 * Li) continue;
                    Vector2d u = (b - a) / Li, v = (d - c) / Lj;
                    if (Math.Abs(u.X * v.Y - u.Y * v.X) < 0.2) continue; // gần song song: không phải dấu X
                    if (!new[] { a, b, c, d }.All(q => Enumerable.Range(0, doan.Count).Any(k => k != i && k != j && CachDoan(doan[k], q) <= tol * 5))) continue;
                    bo.Add(i); bo.Add(j);
                    List<Point2d> o = new List<Point2d> { a, c, b, d };
                    if (DienTichDaGiac(o) < 0) o.Reverse();
                    kq.Add(o);
                    break;
                }
            }
            foreach (int i in bo.OrderByDescending(x => x)) doan.RemoveAt(i);
            return kq;
        }

        /// Đoạn p–q cắt hộp h [min, max] nới thêm e (Liang–Barsky).
        static bool CatHop(Point2d p, Point2d q, Point2d[] h, double e)
        {
            double t0 = 0, t1 = 1;
            double[] pp = { p.X - q.X, q.X - p.X, p.Y - q.Y, q.Y - p.Y };
            double[] qq = { p.X - h[0].X + e, h[1].X + e - p.X, p.Y - h[0].Y + e, h[1].Y + e - p.Y };
            for (int i = 0; i < 4; i++)
            {
                if (Math.Abs(pp[i]) < 1e-12) { if (qq[i] < 0) return false; continue; }
                double t = qq[i] / pp[i];
                if (pp[i] < 0) t0 = Math.Max(t0, t); else t1 = Math.Min(t1, t);
                if (t0 > t1) return false;
            }
            return true;
        }

        /// Khe k và khe o là hai nét của cùng một ô cửa trên tường dày: song song, cách nhau 5 tol–0,6 m, hai đầu lệch nhau ≤ 0,6 m.
        static bool SongDoi(Point2d[] k, Point2d[] o, double tol)
        {
            double L = k[0].GetDistanceTo(k[1]), e = tol * 300;
            Vector2d u = (k[1] - k[0]) / L, v = o[1] - o[0];
            if (Math.Abs(u.X * v.Y - u.Y * v.X) > 0.01 * v.Length) return false;
            double lech = Math.Abs(u.X * (o[0].Y - k[0].Y) - u.Y * (o[0].X - k[0].X));
            if (lech < tol * 5 || lech > e) return false;
            double p0 = u.DotProduct(o[0] - k[0]), p1 = u.DotProduct(o[1] - k[0]);
            return (Math.Abs(p0) <= e && Math.Abs(p1 - L) <= e) || (Math.Abs(p1) <= e && Math.Abs(p0 - L) <= e);
        }

        /// Điểm chắc nằm trong mặt: trọng tâm nếu ở trong, không thì sát giữa cạnh đầu (mặt ngược chiều kim đồng hồ: lòng mặt bên trái cạnh).
        static Point2d DiemTrongMat(List<Point2d> f)
        {
            double a = 0, cx = 0, cy = 0;
            for (int i = 0, j = f.Count - 1; i < f.Count; j = i++)
            {
                double w = f[j].X * f[i].Y - f[i].X * f[j].Y;
                a += w; cx += (f[j].X + f[i].X) * w; cy += (f[j].Y + f[i].Y) * w;
            }
            if (Math.Abs(a) > 1e-12)
            {
                Point2d g = new Point2d(cx / (3 * a), cy / (3 * a));
                if (TrongDaGiac(f, g)) return g;
            }
            Vector2d c = f[1] - f[0];
            return f[0] + c * 0.5 + new Vector2d(-c.Y, c.X) * 0.01;
        }

        /// Mặt không rải: ô X (lỗ thông tầng) hoặc nằm trong vùng gạch chéo (công trình hiện hữu).
        static bool MatKhongRai(List<Point2d> f, List<List<Point2d>> oX, List<List<Point2d>> gach)
        {
            Point2d c = DiemTrongMat(f);
            return oX.Concat(gach).Any(q => TrongDaGiac(q, c));
        }

        /// L7: cạnh hộp thang h [min, max] cách cạnh dọc / ngang của vùng z ≤ d (cạnh đó chồng bề ngang hộp) thì nắn ra đúng cạnh vùng,
        /// không để dải mỏng giữa thang và tường. e: sai số coi cạnh vùng là dọc / ngang.
        static Point2d[] NanHop(List<Point2d> z, Point2d[] h, double d, double e)
        {
            double x0 = h[0].X, y0 = h[0].Y, x1 = h[1].X, y1 = h[1].Y;
            double[] goc = { x0, y0, x1, y1 }, moi = { x0, y0, x1, y1 }, tot = { d, d, d, d };
            for (int i = 0; i < z.Count; i++)
            {
                Point2d a = z[i], b = z[(i + 1) % z.Count];
                bool doc = Math.Abs(a.X - b.X) <= e && Math.Min(a.Y, b.Y) < y1 && Math.Max(a.Y, b.Y) > y0;
                bool ngang = Math.Abs(a.Y - b.Y) <= e && Math.Min(a.X, b.X) < x1 && Math.Max(a.X, b.X) > x0;
                if (!doc && !ngang) continue;
                double c = doc ? (a.X + b.X) / 2 : (a.Y + b.Y) / 2;
                for (int k = doc ? 0 : 1; k < 4; k += 2)
                    if (Math.Abs(c - goc[k]) < tot[k]) { tot[k] = Math.Abs(c - goc[k]); moi[k] = c; }
            }
            return new[] { new Point2d(moi[0], moi[1]), new Point2d(moi[2], moi[3]) };
        }

        /// L7: vùng z trừ hộp h (Region của AutoCAD), ngược chiều kim đồng hồ. null = sau khi trừ không còn đúng một vòng kín.
        /// shortcut: thang giữa phòng (thành lỗ) hoặc cắt đôi phòng thì không khoét; gặp thật thì nối lỗ ra tường gần nhất.
        static List<Point2d> KhoetHop(List<Point2d> z, Point2d[] h, double tol)
        {
            DBObjectCollection ca = null, cb = null, con = new DBObjectCollection();
            try
            {
                using (Polyline pa = new Polyline())
                using (Polyline pb = new Polyline())
                {
                    for (int i = 0; i < z.Count; i++) pa.AddVertexAt(i, z[i], 0, 0, 0);
                    pa.Closed = true;
                    Point2d[] g = { h[0], new Point2d(h[1].X, h[0].Y), h[1], new Point2d(h[0].X, h[1].Y) };
                    for (int i = 0; i < 4; i++) pb.AddVertexAt(i, g[i], 0, 0, 0);
                    pb.Closed = true;
                    ca = Region.CreateFromCurves(new DBObjectCollection { pa });
                    cb = Region.CreateFromCurves(new DBObjectCollection { pb });
                }
                Region ra = (Region)ca[0];
                ra.BooleanOperation(BooleanOperationType.BoolSubtract, (Region)cb[0]);
                ra.Explode(con);
                List<Point2d[]> s = new List<Point2d[]>();
                foreach (DBObject o in con)
                {
                    Line ln = o as Line;
                    if (ln == null) return null; // nhiều mảnh (Region con) hoặc cạnh cong
                    s.Add(new[] { new Point2d(ln.StartPoint.X, ln.StartPoint.Y), new Point2d(ln.EndPoint.X, ln.EndPoint.Y) });
                }
                if (s.Count < 3) return null;
                List<Point2d> kq = new List<Point2d> { s[0][0], s[0][1] };
                s.RemoveAt(0);
                while (s.Count > 0)
                {
                    Point2d cuoi = kq[kq.Count - 1];
                    int j = s.FindIndex(q => q[0].GetDistanceTo(cuoi) <= tol || q[1].GetDistanceTo(cuoi) <= tol);
                    if (j < 0) return null; // còn vòng thứ hai (lỗ)
                    kq.Add(s[j][0].GetDistanceTo(cuoi) <= tol ? s[j][1] : s[j][0]);
                    s.RemoveAt(j);
                }
                if (kq[0].GetDistanceTo(kq[kq.Count - 1]) > tol) return null;
                kq.RemoveAt(kq.Count - 1);
                if (DienTichDaGiac(kq) < 0) kq.Reverse();
                return BoThangHang(kq, tol);
            }
            catch (System.Exception) { return null; } // modeler không trừ được: giữ vùng cũ
            finally
            {
                foreach (DBObjectCollection dc in new[] { ca, cb, con })
                    if (dc != null) foreach (DBObject o in dc) o.Dispose();
            }
        }

        /// Đọc một lần mọi đối tượng có thể là tường trong không gian hiện hành, kèm hộp bao.
        static void DocDs(NenVung n)
        {
            BlockTableRecord ms = (BlockTableRecord)n.Tr.GetObject(n.Db.CurrentSpaceId, OpenMode.ForRead);
            foreach (ObjectId id in ms)
            {
                Entity e = n.Tr.GetObject(id, OpenMode.ForRead) as Entity;
                if (e == null || !e.Visible) continue;
                int lo = LoaiLop(n, e.LayerId);
                if (lo < 0 || (lo == 0 && !(e is BlockReference))) continue; // block trên layer bỏ vẫn đọc: nét tường bên trong (ThuNet)
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
        /// khongNoi: đoạn không dùng để khép khe ở bước 1, 1b (nét cột, TachCot); vẫn dựng mặt như nét thường.
        static List<List<Point2d>> CacMat(List<Point2d[]> doan0, double tol, double ho, List<Point2d[]> cua = null, double hoCua = 0, HashSet<Point2d[]> khongNoi = null)
        {
            List<Point2d[]> doan = new List<Point2d[]>(doan0);
            double tolTh = tol * 5; // lệch khỏi đường thẳng vẫn coi là thẳng hàng (10 mm)
            // Review 20/11 (CHẶN 1): nét cột thẳng hàng với nét tường thường (cột đứng trong tường; cửa cuốn để trống giữa hai cột) vẫn khép khe như
            // trước L1: nét tường thẳng hàng phải chạm nét cột (cách ≤ 50 mm). Nét cột không chạm tường thẳng hàng nào (cột giữa lòng xưởng, ô giả
            // Quyết Thắng; tường thẳng hàng ở xa không tính) mới không khép. shortcut: chỉ xét nét ngang / dọc; nét cột xiên vẫn không khép khe.
            if (khongNoi != null && khongNoi.Count > 0)
            {
                // toạ độ y (x) / tolTh của nét tường ngang (dọc) -> các khoảng [đầu, cuối] theo phương nét
                Dictionary<long, List<double[]>> ngang = new Dictionary<long, List<double[]>>(), doc = new Dictionary<long, List<double[]>>();
                Action<Dictionary<long, List<double[]>>, double, double, double> them = (h, v, a0, a1) =>
                {
                    long k = (long)Math.Round(v / tolTh);
                    List<double[]> ds;
                    if (!h.TryGetValue(k, out ds)) h[k] = ds = new List<double[]>();
                    ds.Add(new[] { Math.Min(a0, a1), Math.Max(a0, a1) });
                };
                Func<Dictionary<long, List<double[]>>, double, double, double, bool> cham = (h, v, a0, a1) =>
                {
                    long k = (long)Math.Round(v / tolTh);
                    double x0 = Math.Min(a0, a1) - tolTh * 5, x1 = Math.Max(a0, a1) + tolTh * 5;
                    for (long q = k - 1; q <= k + 1; q++)
                    {
                        List<double[]> ds;
                        if (h.TryGetValue(q, out ds) && ds.Any(r => r[0] <= x1 && r[1] >= x0)) return true;
                    }
                    return false;
                };
                foreach (Point2d[] s in doan)
                {
                    if (khongNoi.Contains(s)) continue;
                    if (Math.Abs(s[0].Y - s[1].Y) <= tol) them(ngang, (s[0].Y + s[1].Y) / 2, s[0].X, s[1].X);
                    else if (Math.Abs(s[0].X - s[1].X) <= tol) them(doc, (s[0].X + s[1].X) / 2, s[0].Y, s[1].Y);
                }
                khongNoi = new HashSet<Point2d[]>(khongNoi.Where(s => !(Math.Abs(s[0].Y - s[1].Y) <= tol && cham(ngang, (s[0].Y + s[1].Y) / 2, s[0].X, s[1].X))
                    && !(Math.Abs(s[0].X - s[1].X) <= tol && cham(doc, (s[0].X + s[1].X) / 2, s[0].Y, s[1].Y))));
            }

            // 1. Nối khe giữa hai đoạn thẳng hàng (ô cửa giữa hai má tường, cột chỉ có hatch): cầu nối là cạnh như tường. Khe ≤ ho: luôn nối.
            //    Khe ho..hoCua (người dùng chốt 14/11 "cứ có cửa mình khép lại 1 vùng"): chỉ khe GẦN NHẤT ở cả hai đầu đoạn, và khe có ký hiệu cửa (cua)
            //    hoặc là ô cửa trên tường hai nét (nét song song cũng hở đúng chỗ đó, như cửa cuốn xưởng vẽ để trống).
            double hoMax = Math.Max(ho, hoCua);
            int[] tt = SapX(doan);
            List<Point2d[]> cau = new List<Point2d[]>();
            Dictionary<int, double> ganNhat = new Dictionary<int, double>(); // đầu đoạn (2 x chỉ số, +1 = đầu cuối) -> khe thẳng hàng ngắn nhất từ đầu đó
            List<KeyValuePair<int[], Point2d[]>> xa = new List<KeyValuePair<int[], Point2d[]>>();
            Action<int, double> ghi = (k, g) => { double c0; if (!ganNhat.TryGetValue(k, out c0) || g < c0) ganNhat[k] = g; };
            // Review 20/11 (CHẶN 1): khe có đầu là nét cột (khongNoi) không khép, chỉ làm "khe song đôi" cho khe tường thường (cửa cuốn để trống:
            // khe mặt ngoài thẳng hàng tường + khe mặt trong cột).
            List<Point2d[]> capCot = new List<Point2d[]>();
            for (int ii = 0; ii < tt.Length; ii++)
            {
                if ((ii & 1023) == 0) KiemEsc();
                int i = tt[ii];
                bool cotI = khongNoi != null && khongNoi.Contains(doan[i]);
                Point2d a = doan[i][0], b = doan[i][1];
                double xMax = Math.Max(a.X, b.X), yMin = Math.Min(a.Y, b.Y), yMax = Math.Max(a.Y, b.Y);
                Vector2d u = b - a;
                double L = u.Length;
                u = u / L;
                for (int jj = ii + 1; jj < tt.Length; jj++)
                {
                    int j = tt[jj];
                    Point2d c = doan[j][0], d = doan[j][1];
                    if (Math.Min(c.X, d.X) > xMax + hoMax) break;
                    if (Math.Min(c.Y, d.Y) > yMax + hoMax || Math.Max(c.Y, d.Y) < yMin - hoMax) continue;
                    bool laCot = cotI || (khongNoi != null && khongNoi.Contains(doan[j]));
                    Vector2d v = d - c;
                    if (Math.Abs(u.X * v.Y - u.Y * v.X) > 0.01 * v.Length) continue;
                    if (Math.Abs(u.X * (c.Y - a.Y) - u.Y * (c.X - a.X)) > tolTh || Math.Abs(u.X * (d.Y - a.Y) - u.Y * (d.X - a.X)) > tolTh) continue;
                    double sc = u.DotProduct(c - a), sd = u.DotProduct(d - a);
                    double sMin = Math.Min(sc, sd), sMax = Math.Max(sc, sd);
                    int jMin = 2 * j + (sc < sd ? 0 : 1), jMax = 2 * j + (sc < sd ? 1 : 0); // đầu của j ở phía tham số nhỏ / lớn (theo hướng của i)
                    Point2d qMin = sc < sd ? c : d, qMax = sc < sd ? d : c;
                    if (laCot)
                    {
                        if (sMin - L > tol) capCot.Add(new[] { b, qMin });
                        else if (-sMax > tol) capCot.Add(new[] { qMax, a });
                        continue;
                    }
                    if (sMin > L - tol) // j nằm sau b
                    {
                        double g = sMin - L;
                        ghi(2 * i + 1, g); ghi(jMin, g);
                        if (g > tol) xa.Add(new KeyValuePair<int[], Point2d[]>(new[] { 2 * i + 1, jMin }, new[] { b, qMin }));
                    }
                    else if (sMax < tol) // j nằm trước a
                    {
                        double g = -sMax;
                        ghi(2 * i, g); ghi(jMax, g);
                        if (g > tol) xa.Add(new KeyValuePair<int[], Point2d[]>(new[] { 2 * i, jMax }, new[] { qMax, a }));
                    }
                    else
                    {
                        // Chồng nhau: đầu nằm trong đoạn kia không phải đầu hở.
                        if (sMax > L) ghi(2 * i + 1, 0);
                        if (sMin < 0) ghi(2 * i, 0);
                        if (sMax < L) ghi(jMax, 0);
                        if (sMin > 0) ghi(jMin, 0);
                    }
                }
            }
            // Người dùng 17/11 (file KOCHAM): khe 11 m từ góc lõi ra mặt dựng chạm cung nắp bồn cầu / cửa ở đầu khe bị khép, cắt văn phòng.
            // Hộp cửa phải phủ ≥ 25 % chiều dài khe (cửa nằm TRONG khe), không chỉ chạm một đầu.
            Func<Point2d, Point2d, Point2d[], bool> cuaTrongKhe = (a, b, h) =>
            {
                double L = a.GetDistanceTo(b);
                if (L < tol || !CatHop(a, b, h, tol * 150)) return false;
                Vector2d u = (b - a) / L;
                double[] s = { u.DotProduct(h[0] - a), u.DotProduct(new Point2d(h[1].X, h[0].Y) - a), u.DotProduct(h[1] - a), u.DotProduct(new Point2d(h[0].X, h[1].Y) - a) };
                return Math.Min(s.Max(), L) - Math.Max(s.Min(), 0) >= 0.25 * L;
            };
            // Karaoke ABC 21/11: tường ngăn hai bên hành lang thẳng hàng -> mặt tường hai bên cùng toạ độ, khe ngang hành lang 1,7 m bị khép như ô cửa,
            // hành lang vỡ 5 vùng. Ở CẢ HAI đầu khe có nét vuông góc chạy liền hai phía (mặt hành lang chạy tiếp qua đầu tường, chữ T) ngay tại đầu khe
            // hoặc cắt ngang khe trong 0,6 m (bề dày tường: khe đi từ mặt hành lang bên này qua tường tới góc phòng bên kia) = khe xuyên không gian liền,
            // không phải ô cửa: không khép, trừ khi có ký hiệu cửa trong khe. Miệng lối đi, sảnh (góc chữ L), má cửa (nắp đầu tường một phía) vẫn khép.
            // Lưới ô 0,5 m dựng khi cần lần đầu (bản vẽ thật: dựng phòng phải < 10 s; ô 3 m trả quá nhiều nét, chậm thêm 0,5 s).
            LuoiDoan luoiT = null;
            Func<LuoiDoan> luoiTT = () => luoiT ?? (luoiT = new LuoiDoan(doan, tol * 250));
            Func<Point2d, Vector2d, bool> coNet = (q, w) => luoiTT().Tim(q.X - tolTh, q.Y - tolTh, q.X + tolTh, q.Y + tolTh).Any(j =>
            {
                Vector2d v = doan[j][1] - doan[j][0];
                return Math.Abs(w.X * v.Y - w.Y * v.X) <= 0.01 * v.Length && CachDoan(doan[j], q) <= tolTh;
            });
            // Karaoke ABC: đầu khe là má cửa phòng (mặt hành lang dừng một phía, phía kia là ô cửa) vẫn tính chạy tiếp: điểm trong hộp ký hiệu cửa nới 0,3 m.
            double nCua = tol * 150;
            Func<Point2d, bool> trongCua = q => cua != null && cua.Any(h => q.X >= h[0].X - nCua && q.X <= h[1].X + nCua && q.Y >= h[0].Y - nCua && q.Y <= h[1].Y + nCua);
            Func<Point2d, Point2d, bool> dauXuyen = (p, q) =>
            {
                // Xét cả 0,6 m SAU đầu khe: đầu khe ở mặt cột lồi (Karaoke: trụ 200 x 100 nhô ra hành lang), mặt hành lang chạy liền ngay sau đó.
                double g = p.GetDistanceTo(q), tm = Math.Min(tol * 300, g - tolTh), ts = tol * 300;
                Vector2d u = (q - p) / g, w = u.GetPerpendicularVector();
                Point2d e = p + u * tm, s0 = p - u * ts;
                foreach (int j in luoiTT().Tim(Math.Min(s0.X, e.X) - tolTh, Math.Min(s0.Y, e.Y) - tolTh, Math.Max(s0.X, e.X) + tolTh, Math.Max(s0.Y, e.Y) + tolTh))
                {
                    Vector2d v = doan[j][1] - doan[j][0];
                    if (Math.Abs(u.DotProduct(v)) > 0.01 * v.Length) continue; // chỉ nét vuông góc với khe
                    double t = u.DotProduct(doan[j][0] - p);
                    if (t < -ts || t > tm) continue;
                    Point2d c = p + u * t;
                    if ((coNet(c + w * (tol * 40), w) || trongCua(c + w * (tol * 40))) && (coNet(c - w * (tol * 40), w) || trongCua(c - w * (tol * 40)))) return true;
                }
                return false;
            };
            // Chỉ nối khe GẦN NHẤT ở cả hai đầu (người dùng 16/11: nối mọi đoạn cùng hàng trong 3 m sinh cầu chồng nhau, 23 triệu điểm cắt, dựng phòng 78 s).
            List<Point2d[]> giu = new List<Point2d[]>();
            foreach (KeyValuePair<int[], Point2d[]> kv in xa)
            {
                double g = kv.Value[0].GetDistanceTo(kv.Value[1]);
                if (ganNhat[kv.Key[0]] < g - tol || ganNhat[kv.Key[1]] < g - tol) continue;
                // khe < 0,7 m không phải hành lang; khe > max(ho, 6 m) không cửa vốn không khép: khỏi xét (bản vẽ thật dựng phòng phải < 10 s)
                if (g >= tol * 350 && g <= Math.Max(ho, tol * 3000) && dauXuyen(kv.Value[0], kv.Value[1]) && dauXuyen(kv.Value[1], kv.Value[0]) && !(cua != null && cua.Any(h => cuaTrongKhe(kv.Value[0], kv.Value[1], h)))) continue;
                if (g <= ho) cau.Add(kv.Value); else giu.Add(kv.Value);
            }
            // Hai khe song song (tường 2 nét có lỗ mở) chỉ khép khi khe ≤ 6 m: KOCHAM mặt cột mặt dựng thẳng hàng mặt vách lõi, khe 11 m giữa văn phòng bị khép.
            double soDoiMax = tol * 3000;
            foreach (Point2d[] k in giu)
                if ((cua != null && cua.Any(h => cuaTrongKhe(k[0], k[1], h)))
                    || (k[0].GetDistanceTo(k[1]) <= soDoiMax && (giu.Any(k2 => k2 != k && SongDoi(k, k2, tol)) || capCot.Any(k2 => SongDoi(k, k2, tol))))) cau.Add(k);
            // 1b. Khe chữ T có cửa (cửa giữa đầu tường và tường vuông góc / xiên): đầu tường hở, cách hộp cửa ≤ 0,5 m, chiếu thẳng tới nét gần nhất
            //     trong hoCua; tia cắt hộp cửa thì khép.
            if (cua != null && cua.Count > 0 && hoCua > 0)
            {
                HashSet<Point2d> daNoi = new HashSet<Point2d>(cau.SelectMany(k => k));
                LuoiDoan luoi = new LuoiDoan(doan, hoCua / 4);
                for (int i = 0; i < doan.Count; i++)
                    for (int k = 0; k < 2; k++)
                    {
                        if (khongNoi != null && khongNoi.Contains(doan[i])) continue;
                        Point2d p = doan[i][k], q0 = doan[i][1 - k];
                        if (daNoi.Contains(p) || !cua.Any(h => CatHop(p, p, h, tol * 250))) continue;
                        double r0 = tol * 1.5;
                        if (luoi.Tim(p.X - r0, p.Y - r0, p.X + r0, p.Y + r0).Any(j => j != i && CachDoan(doan[j], p) <= r0)) continue; // đầu đã chạm nét khác
                        Vector2d u = (p - q0).GetNormal();
                        Point2d xa0 = p + u * hoCua;
                        double tot = double.MaxValue;
                        foreach (int js in luoi.Tim(Math.Min(p.X, xa0.X), Math.Min(p.Y, xa0.Y), Math.Max(p.X, xa0.X), Math.Max(p.Y, xa0.Y)))
                        {
                            Point2d[] s = doan[js];
                            Vector2d w = s[1] - s[0];
                            double mau0 = u.X * w.Y - u.Y * w.X;
                            if (Math.Abs(mau0) < 1e-9 * w.Length) continue;
                            Vector2d c0 = s[0] - p;
                            double t = (c0.X * w.Y - c0.Y * w.X) / mau0, r = (c0.X * u.Y - c0.Y * u.X) / mau0;
                            if (t > tol && t <= hoCua && r >= -1e-9 && r <= 1 + 1e-9 && t < tot) tot = t;
                        }
                        if (tot > hoCua) continue;
                        Point2d hit = p + u * tot;
                        if (cua.Any(h => cuaTrongKhe(p, hit, h))) cau.Add(new[] { p, hit });
                    }
            }
            doan.AddRange(cau);

            // 1c. Chữ T, góc L hụt tol..5 tol (2–10 mm, nét vẽ không chạm hẳn, hay gặp ở bản vẽ khách): nối đầu hở tới điểm gần nhất của đoạn kia.
            {
                int nd0 = doan.Count;
                int[] t2 = SapX(doan);
                double[] dMin = Enumerable.Repeat(double.MaxValue, 2 * nd0).ToArray();
                Point2d[] toi = new Point2d[2 * nd0];
                Action<int, int> xet = (e, j) =>
                {
                    Point2d g0 = DiemGan(doan[j], doan[e / 2][e % 2]);
                    double dd = g0.GetDistanceTo(doan[e / 2][e % 2]);
                    if (dd < dMin[e]) { dMin[e] = dd; toi[e] = g0; }
                };
                for (int ii = 0; ii < nd0; ii++)
                {
                    int i = t2[ii];
                    double xMax = Math.Max(doan[i][0].X, doan[i][1].X) + tolTh;
                    double yMin = Math.Min(doan[i][0].Y, doan[i][1].Y) - tolTh, yMax = Math.Max(doan[i][0].Y, doan[i][1].Y) + tolTh;
                    for (int jj = ii + 1; jj < nd0; jj++)
                    {
                        int j = t2[jj];
                        if (Math.Min(doan[j][0].X, doan[j][1].X) > xMax) break;
                        if (Math.Min(doan[j][0].Y, doan[j][1].Y) > yMax || Math.Max(doan[j][0].Y, doan[j][1].Y) < yMin) continue;
                        xet(2 * i, j); xet(2 * i + 1, j); xet(2 * j, i); xet(2 * j + 1, i);
                    }
                }
                for (int e = 0; e < dMin.Length; e++)
                    if (dMin[e] > tol && dMin[e] <= tolTh) doan.Add(new[] { doan[e / 2][e % 2], toi[e] });
            }

            // 2. Cắt mọi đoạn tại giao điểm, chỗ chạm (chữ T hụt ≤ tol) và chỗ chồng nhau thẳng hàng.
            int m = doan.Count;
            List<double>[] tham = new List<double>[m];
            for (int i = 0; i < m; i++) tham[i] = new List<double> { 0, 1 };
            tt = SapX(doan);
            for (int ii = 0; ii < m; ii++)
            {
                if ((ii & 1023) == 0) KiemEsc();
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
            Dictionary<long, List<int>> o = new Dictionary<long, List<int>>(KHOA_LONG);
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
            HashSet<long> canh = new HashSet<long>(KHOA_LONG);
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
            HashSet<long> daDi = new HashSet<long>(KHOA_LONG);
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

        // Người dùng 16/11 (test báo cháy.dwg): vùng tự dựng bám từng khấc cột, cung cửa, gờ tường (44–53 đỉnh); vùng người dùng sửa tay là đa giác
        // vuông góc 8–9 đỉnh: bỏ khấc nông, lấp hốc cột, giữ khấc lớn (lõi thang 2,6 x 7,2 m).
        // shortcut: ngưỡng cố định 1 m (khấc cột, hốc tường, gờ cửa nhỏ hơn); cần giữ chi tiết nhỏ hơn thì vẽ polyline PCCC_VUNG tay.
        const double LAM_GON_M = 1.0;

        class CanhChinh
        {
            public Point2d P, Q;                                  // đầu, cuối cạnh (đa giác ngược chiều kim đồng hồ)
            public List<Point2d> Sau = new List<Point2d>();       // đỉnh của chuỗi cạnh ngắn sau Q, trước cạnh chính kế
            public bool Giu;                                      // chuỗi ngắn dài quá (cung tường cong): giữ nguyên, không nối qua
            public Vector2d Huong { get { return (Q - P).GetNormal(); } }
        }

        static Point2d ChieuLen(Point2d p, Point2d goc, Vector2d d) { return goc + d * (p - goc).DotProduct(d); }

        /// Làm gọn đa giác vùng (người dùng 16/11): cạnh dài ≥ t là cạnh chính, chuỗi cạnh ngắn giữa hai cạnh chính (khấc cột, cung cửa, gờ)
        /// thay bằng giao điểm hai cạnh đó; hai cạnh chính song song cùng chiều lệch nhau < t (khấc nông) gộp về đường NGOÀI.
        /// Không làm gọn được (ít hơn 3 cạnh chính, ra đa giác tự cắt, diện tích lệch quá 25 %) thì trả đa giác cũ.
        static List<Point2d> LamGon(List<Point2d> v0, double t, double tol)
        {
            List<Point2d> v = BoThangHang(new List<Point2d>(v0), tol);
            if (v.Count < 5) return v;
            double a0 = DienTichDaGiac(v);
            if (a0 < 0) { v.Reverse(); a0 = -a0; }
            int n = v.Count, dau = -1;
            for (int i = 0; i < n && dau < 0; i++) if (v[i].GetDistanceTo(v[(i + 1) % n]) >= t) dau = i;
            if (dau < 0) return v;
            List<CanhChinh> c = new List<CanhChinh>();
            for (int k = 0; k < n; k++)
            {
                int i = (dau + k) % n;
                Point2d p = v[i], q = v[(i + 1) % n];
                if (p.GetDistanceTo(q) >= t) c.Add(new CanhChinh { P = p, Q = q });
                else c[c.Count - 1].Sau.Add(q);
            }
            if (c.Count < 3) return v;
            // Đỉnh cuối của chuỗi là đầu cạnh chính kế: bỏ khỏi Sau. Chuỗi dài hơn 3t (tường cong) thì giữ.
            for (int i = 0; i < c.Count; i++)
            {
                CanhChinh a = c[i];
                if (a.Sau.Count > 0) a.Sau.RemoveAt(a.Sau.Count - 1);
                double dai = 0;
                Point2d tr = a.Q;
                foreach (Point2d s in a.Sau.Concat(new[] { c[(i + 1) % c.Count].P })) { dai += tr.GetDistanceTo(s); tr = s; }
                a.Giu = dai > 3 * t;
            }
            const double SIN_SS = 0.035; // 2°
            for (int vong = 0; vong < 1000 && c.Count >= 3; vong++)
            {
                bool gop = false;
                for (int i = 0; i < c.Count && !gop; i++)
                {
                    int j = (i + 1) % c.Count;
                    CanhChinh A = c[i], B = c[j];
                    if (A.Giu) continue;
                    Vector2d da = A.Huong, db = B.Huong;
                    if (Math.Abs(da.X * db.Y - da.Y * db.X) > SIN_SS || da.DotProduct(db) <= 0) continue;
                    Vector2d ngoai = new Vector2d(da.Y, -da.X); // lòng đa giác ở bên trái cạnh
                    double lech = (B.P - A.P).DotProduct(ngoai);
                    if (Math.Abs(lech) >= t) continue;
                    Point2d goc = lech > 0 ? B.P : A.P;
                    A.P = ChieuLen(A.P, goc, da);
                    A.Q = ChieuLen(B.Q, goc, da);
                    A.Sau = B.Sau; A.Giu = B.Giu;
                    c.RemoveAt(j);
                    gop = true;
                }
                if (!gop) break;
            }
            if (c.Count < 3) return v;
            Point2d[] S = c.Select(x => x.P).ToArray(), E = c.Select(x => x.Q).ToArray();
            List<Point2d>[] noi = new List<Point2d>[c.Count];
            for (int i = 0; i < c.Count; i++)
            {
                int j = (i + 1) % c.Count;
                CanhChinh A = c[i], B = c[j];
                noi[i] = new List<Point2d>();
                if (A.Giu) { noi[i].AddRange(A.Sau); continue; }
                Vector2d da = A.Huong, db = B.Huong;
                double cheo = da.X * db.Y - da.Y * db.X;
                if (Math.Abs(cheo) > SIN_SS)
                {
                    Vector2d w = B.P - A.P;
                    Point2d x = A.P + da * ((w.X * db.Y - w.Y * db.X) / cheo);
                    if (x.GetDistanceTo(A.Q) <= 3 * t && x.GetDistanceTo(B.P) <= 3 * t) { E[i] = x; S[j] = x; }
                }
                else if (da.DotProduct(db) > 0) S[j] = ChieuLen(A.Q, B.P, db); // bậc lớn ≥ t: cạnh nối vuông góc tại cuối A
            }
            List<Point2d> kq = new List<Point2d>();
            for (int i = 0; i < c.Count; i++) { kq.Add(S[i]); kq.Add(E[i]); kq.AddRange(noi[i]); }
            List<Point2d> gon = new List<Point2d>();
            foreach (Point2d p in kq) if (gon.Count == 0 || gon[gon.Count - 1].GetDistanceTo(p) > tol) gon.Add(p);
            if (gon.Count > 1 && gon[0].GetDistanceTo(gon[gon.Count - 1]) <= tol) gon.RemoveAt(gon.Count - 1);
            gon = BoThangHang(gon, tol);
            double a1 = DienTichDaGiac(gon);
            if (gon.Count < 3 || a1 < 0.8 * a0 || a1 > 1.25 * a0 || TuCat(gon, tol)) return v;
            return gon;
        }

        /// Đa giác tự cắt: hai cạnh không kề nhau cắt nhau.
        static bool TuCat(List<Point2d> d, double tol)
        {
            int n = d.Count;
            for (int i = 0; i < n; i++)
                for (int j = i + 2; j < n; j++)
                {
                    if (i == 0 && j == n - 1) continue;
                    Point2d a = d[i], b = d[(i + 1) % n], p = d[j], q = d[(j + 1) % n];
                    Vector2d r = b - a, s = q - p;
                    double cr = r.X * s.Y - r.Y * s.X;
                    if (Math.Abs(cr) < 1e-12) continue;
                    Vector2d w = p - a;
                    double u = (w.X * s.Y - w.Y * s.X) / cr, z = (w.X * r.Y - w.Y * r.X) / cr;
                    double eu = tol / r.Length, ez = tol / s.Length;
                    if (u > eu && u < 1 - eu && z > ez && z < 1 - ez) return true;
                }
            return false;
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
                            ThuNet(n, (Entity)tr.GetObject(kv.Key, OpenMode.ForRead), ObjectId.Null, ObjectId.Null, null, 0, net);
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
        /// mat: mọi mặt kín dựng từ nét tường (WCS, đã bỏ đỉnh thẳng hàng). mau: khoá nét phân vùng mẫu (KhoaNetMau), có thì chỉ dựng mặt từ các nét
        /// cùng layer + màu thật với mẫu, kể cả nét trong block (người dùng 14/11: cả tờ là một block, vùng kẻ bằng nét xanh lá, đỏ).
        /// oX: ô có dấu X (lỗ thông tầng, TachOX); gach: biên hatch gạch chéo (công trình hiện hữu). Mặt trùng hai loại này không rải (MatKhongRai).
        /// Dầm (người dùng bổ sung 15/11: xét cả độ sâu dầm, 5.9.1.1.6): damMau = khoá nét dầm (không dựng làm tường), giuDam(đoạn) = dầm sâu > 0,3 m
        /// thì giữ làm biên khoang, dầm nông bỏ (trần phẳng, khoản a); damThem = đoạn dầm người dùng chọn tay (TungDam, đã lọc sâu). demDam = [giữ, bỏ].
        static void MatChon(Database db, ObjectId[] ids, HashSet<string> mau, out List<List<Point2d>> co, out List<List<Point2d>> mat,
            out List<List<Point2d>> oX, out List<List<Point2d>> gach)
        {
            int[] dem;
            Dictionary<List<Point2d>, List<Point2d>> khoang;
            List<Point2d> khu;
            List<Point2d[]> damSau;
            double[] khungTruc;
            int boTruc;
            NhanThem them;
            MatChon(db, ids, mau, null, null, null, null, ObjectId.Null, out co, out mat, out oX, out gach, out dem, out khoang, out khu, out damSau, out khungTruc, out boTruc, out them);
        }

        /// khoang: khoang dầm chia ra từ vùng PCCC_VUNG vẽ sẵn -> vùng cha (KOCHAM 16/11: có vùng vẽ sẵn thì trước đây không chia theo dầm), đã thay vào co.
        /// damSau: đoạn dầm sâu khi tự dựng phòng. Người dùng 17/11 (vẽ lại vùng tầng 8): vùng là PHÒNG dựng từ tường; dầm không dựng phòng
        /// (trước đây dầm cắt văn phòng thành 2 vùng, cắt WC ra mảnh không có dấu hiệu WC) — người gọi chia khoang trong phòng đã chọn (ChiaKhoang).
        /// hopChon: [x0, y0, x1, y1] khung quét chọn (null = chọn từng đối tượng, đọc hết): chỉ đọc nét trong khung nới 5 m, chỉ giữ phòng có điểm trong nằm trong khung.
        static void MatChon(Database db, ObjectId[] ids, HashSet<string> mau, HashSet<string> damMau, Func<Point2d[], bool> giuDam, List<Point2d[]> damThem, double[] hopChon, ObjectId khoiDam,
            out List<List<Point2d>> co, out List<List<Point2d>> mat, out List<List<Point2d>> oX, out List<List<Point2d>> gach, out int[] demDam,
            out Dictionary<List<Point2d>, List<Point2d>> khoang, out List<Point2d> khuKhong, out List<Point2d[]> damSau, out double[] khungTruc, out int boTruc,
            out NhanThem them)
        {
            them = new NhanThem();
            khungTruc = null;
            boTruc = 0;
            damSau = new List<Point2d[]>();
            khoang = new Dictionary<List<Point2d>, List<Point2d>>();
            khuKhong = new List<Point2d>();
            co = new List<List<Point2d>>();
            mat = new List<List<Point2d>>();
            oX = new List<List<Point2d>>();
            gach = new List<List<Point2d>>();
            demDam = new int[2];
            bool coDam = damMau != null && damMau.Count > 0;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                NenVung n = new NenVung();
                n.Db = db; n.Tr = tr; n.Tol = 2 * 0.001 / HeSoMet(db); n.Mau = mau;
                double noi = 5 / HeSoMet(db);
                if (hopChon != null) n.Hop = new[] { hopChon[0] - noi, hopChon[1] - noi, hopChon[2] + noi, hopChon[3] + noi };
                List<Entity> ds = new List<Entity>();
                foreach (ObjectId id in ids)
                {
                    Entity e = tr.GetObject(id, OpenMode.ForRead) as Entity;
                    Polyline pl = e as Polyline;
                    Circle ci = e as Circle;
                    if (KinPl(pl) && string.Equals(pl.Layer, LAYER_VUNG, StringComparison.OrdinalIgnoreCase)) co.Add(DinhPl(pl));
                    else if (ci != null && string.Equals(ci.Layer, LAYER_VUNG, StringComparison.OrdinalIgnoreCase)) co.Add(DaGiacTron(ci, 72)); // vùng tròn (Bố trí nhanh "Tròn")
                    else if (e != null && !(e is Table)) ds.Add(e);
                }
                bool coTuong = mau == null && ds.Any(e => LoaiLop(n, e.LayerId) == 2);
                // Người dùng 17/11: mặt bằng dầm block lại, chép đè lên mặt bằng tầng. Dầm trong block đó (layer nét dầm mẫu, MỌI màu) là dầm, không dựng tường.
                // Chỉ khi còn khối / tường khác được chọn (cả tờ là một block thì vẫn lọc theo layer + màu như trước).
                HashSet<string> lopDam = coDam && !khoiDam.IsNull && ds.Any(e => e.ObjectId == khoiDam)
                    && ds.Any(e => e.ObjectId != khoiDam && (e is BlockReference || LoaiLop(n, e.LayerId) == 2))
                    ? new HashSet<string>(damMau.Select(k => k.Substring(0, k.IndexOf('|')))) : null;
                // Có layer tường / cột thì dựng bằng các layer đó trước, không ra phòng nào mới dùng mọi nét liền (như VungTaiDiem).
                foreach (bool chiTuong in new[] { true, false })
                {
                    if (co.Count > 0 || mat.Count > 0 || (chiTuong && !coTuong)) continue;
                    n.ChiTuong = chiTuong;
                    n.Cua.Clear(); n.GachCheo.Clear(); n.KhuKhong.Clear(); n.GiengThang.Clear(); n.MocTruc.Clear(); n.Tron.Clear(); n.ChuNgan.Clear();
                    n.CotUng.Clear(); n.KhuTen.Clear(); n.TenPhong.Clear(); n.TenChu.Clear(); n.ThangBo.Clear();
                    List<Point2d[]> doan = new List<Point2d[]>();
                    n.BoMau = coDam ? damMau : null;
                    n.GomCot = true;
                    foreach (Entity e in ds)
                    {
                        if (doan.Count % 64 == 0) KiemEsc();
                        n.LopKhoiDam = lopDam != null && e.ObjectId == khoiDam ? lopDam : null;
                        ThuNet(n, e, ObjectId.Null, ObjectId.Null, null, 0, doan);
                    }
                    n.GomCot = false;
                    n.LopKhoiDam = null;
                    n.BoMau = null;
                    them.Cot = new List<Point2d>();
                    HashSet<Point2d[]> netCot = TachCot(n, them.Cot);
                    foreach (KeyValuePair<Point2d, double> c in n.Tron)
                        if (n.ChuNgan.Any(p => p.GetDistanceTo(c.Key) <= c.Value)) n.MocTruc.Add(c.Key);
                    List<double>[] truc;
                    khungTruc = KhungTruc(n.MocTruc, HeSoMet(db), hopChon, out truc);
                    // Dựng bằng mọi nét liền (không có layer tường): nét thẳng ≥ 3 m trùng đường trục (lệch ≤ 5 mm) là trục, không phải tường.
                    if (khungTruc != null && !n.ChiTuong)
                    {
                        double sai = 0.005 / HeSoMet(db), dai = 3 / HeSoMet(db);
                        int truoc = doan.Count;
                        doan.RemoveAll(s => s[0].GetDistanceTo(s[1]) >= dai
                            && ((Math.Abs(s[0].Y - s[1].Y) <= sai && truc[1].Any(y => Math.Abs(s[0].Y - y) <= sai))
                             || (Math.Abs(s[0].X - s[1].X) <= sai && truc[0].Any(x => Math.Abs(s[0].X - x) <= sai))));
                        boTruc = truoc - doan.Count;
                    }
                    if (coDam || damThem != null)
                    {
                        List<Point2d[]> dd = new List<Point2d[]>();
                        if (coDam)
                        {
                            HashSet<string> cu = n.Mau;
                            bool ct = n.ChiTuong;
                            n.Mau = damMau; n.ChiTuong = false; n.DocDam = true;
                            foreach (Entity e in ds) { n.LopKhoiDam = lopDam != null && e.ObjectId == khoiDam ? lopDam : null; ThuNet(n, e, ObjectId.Null, ObjectId.Null, null, 0, dd); }
                            n.Mau = cu; n.ChiTuong = ct; n.DocDam = false; n.LopKhoiDam = null;
                        }
                        demDam = new int[2];
                        damSau = new List<Point2d[]>();
                        foreach (Point2d[] s in dd)
                            if (giuDam == null || giuDam(s)) { damSau.Add(s); demDam[0]++; }
                            else demDam[1]++;
                        if (damThem != null) { damSau.AddRange(damThem); demDam[0] += damThem.Count; } // nét chọn tay: người gọi đã bỏ dầm nông
                    }
                    oX = TachOX(doan, n.Tol);
                    oX.AddRange(n.GiengThang); // giếng thang máy: không rải trong giếng, như ô X
                    them.ThangBo = new List<Point2d[]>(n.ThangBo);
                    them.Doan = doan;
                    gach = new List<List<Point2d>>(n.GachCheo);
                    if (doan.Count >= 3)
                        mat = CacMat(doan, n.Tol, HO_CUA_M / HeSoMet(db), n.Cua, HO_CUA_MAX_M / HeSoMet(db), netCot).Select(v => BoThangHang(v, n.Tol)).ToList();
                    if (hopChon != null)
                        mat = mat.Where(v => { Point2d c = DiemTrongMat(v); return c.X >= hopChon[0] && c.X <= hopChon[2] && c.Y >= hopChon[1] && c.Y <= hopChon[3]; }).ToList();
                    // Layer tên tường chỉ chứa block cửa (Karaoke ABC, file_cad_test 20/11): lượt chỉ-tường ra toàn mặt vụn, không phòng thật nào
                    // -> bỏ, lượt sau dựng bằng mọi nét liền (như VungTaiDiem). Trước đây "mọi phòng" ra 0 vùng.
                    if (n.ChiTuong && PhongThat(mat, HeSoMet(db)).Count == 0) mat = new List<List<Point2d>>();
                }
                // Vùng vẽ sẵn + dầm: chia từng vùng theo dầm sâu (5.9.1.1.6 d) thành khoang.
                if (co.Count > 0 && (coDam || damThem != null))
                {
                    List<Point2d[]> dd = new List<Point2d[]>();
                    if (coDam)
                    {
                        n.Mau = damMau; n.ChiTuong = false; n.DocDam = true;
                        foreach (Entity e in ds) { n.LopKhoiDam = lopDam != null && e.ObjectId == khoiDam ? lopDam : null; ThuNet(n, e, ObjectId.Null, ObjectId.Null, null, 0, dd); }
                        n.Mau = mau; n.DocDam = false; n.LopKhoiDam = null;
                    }
                    List<Point2d[]> sau = new List<Point2d[]>();
                    foreach (Point2d[] s in dd)
                        if (giuDam == null || giuDam(s)) { sau.Add(s); demDam[0]++; }
                        else demDam[1]++;
                    if (damThem != null) { sau.AddRange(damThem); demDam[0] += damThem.Count; }
                    damSau = sau; // người gọi ghép tim dầm (đầu báo trên dầm) và mốc dim mép dầm
                    List<List<Point2d>> moi = new List<List<Point2d>>();
                    foreach (List<Point2d> z in co)
                    {
                        List<List<Point2d>> k = ChiaKhoang(z, sau, n.Tol, HeSoMet(db));
                        moi.AddRange(k);
                        if (k.Count > 1) foreach (List<Point2d> kc in k) khoang[kc] = z;
                    }
                    co = moi;
                }
                // Vùng vẽ sẵn: lượt dựng tường không chạy, đọc riêng cột (L2 MaiKeo chia khoang theo hàng cột) và chữ khu, tên phòng.
                if (co.Count > 0)
                {
                    n.Mau = null; n.BoMau = null; n.ChiTuong = false; n.DocDam = false; n.LopKhoiDam = null;
                    n.CotUng.Clear(); n.KhuTen.Clear(); n.TenPhong.Clear(); n.TenChu.Clear();
                    n.GomCot = true;
                    List<Point2d[]> bo = new List<Point2d[]>();
                    foreach (Entity e in ds) ThuNet(n, e, ObjectId.Null, ObjectId.Null, null, 0, bo);
                    n.GomCot = false;
                    them.Cot = new List<Point2d>();
                    TachCot(n, them.Cot);
                }
                them.KhuTen = new List<KeyValuePair<Point2d, string>>(n.KhuTen);
                them.TenPhong = new List<Point2d>(n.TenPhong);
                them.TenChu = new List<Point2d>(n.TenChu);
                khuKhong = new List<Point2d>(n.KhuKhong);
                tr.Commit();
            }
        }

        /// Vùng z cắt bởi các đoạn dầm: khoang là mặt nằm trong z, bỏ lòng dầm và dải hẹp (PhongThat: rộng TB < 1 m). Không chia được thì trả z.
        static List<List<Point2d>> ChiaKhoang(List<Point2d> z, List<Point2d[]> dam, double tol, double m)
        {
            double x0 = z.Min(p => p.X), y0 = z.Min(p => p.Y), x1 = z.Max(p => p.X), y1 = z.Max(p => p.Y);
            List<Point2d[]> s = new List<Point2d[]>();
            for (int i = 0; i < z.Count; i++) s.Add(new[] { z[i], z[(i + 1) % z.Count] });
            // KOCHAM 16/11: nét dầm sơ đồ dừng ở mặt cột, cách dầm biên 200 mm, khoang không khép: kéo dài mỗi đầu 0,4 m (đoạn thò ra không chia khoang).
            double e = 0.4 / m;
            s.AddRange(dam.Where(d => Math.Max(d[0].X, d[1].X) >= x0 - e && Math.Min(d[0].X, d[1].X) <= x1 + e && Math.Max(d[0].Y, d[1].Y) >= y0 - e && Math.Min(d[0].Y, d[1].Y) <= y1 + e)
                .Select(d => { Vector2d u = (d[1] - d[0]).GetNormal() * e; return new[] { d[0] - u, d[1] + u }; }));
            if (s.Count == z.Count) return new List<List<Point2d>> { z };
            // Ô 1,8 x 1,8 m (rộng TB 0,9 m, 5.9.1.1.6 c) vẫn là khoang: ngưỡng 0,5 m² / rộng 0,4 m; lòng dầm (cả dầm bẹt 1400) bỏ theo tim dầm.
            List<double> nua = new List<double>();
            List<Point2d[]> tim = TimDam(s.Skip(z.Count).ToList(), m, nua);
            List<List<Point2d>> f = PhongThat(CacMat(s, tol, HO_CUA_M / m).Select(v => BoThangHang(v, tol)).ToList(), m, 0.5, 0.4)
                .Where(v => { Point2d c = DiemTrongMat(v); return TrongDaGiac(z, c) && !Enumerable.Range(0, tim.Count).Any(i => CachDoan(tim[i], c) < nua[i] - tol); }).ToList();
            return f.Count > 1 ? f : new List<List<Point2d>> { z };
        }

        /// Vùng PCCC_VUNG vẽ sẵn (polyline kín, tròn) trong không gian hiện hành.
        static List<List<Point2d>> VungCoSan(Database db)
        {
            List<List<Point2d>> kq = new List<List<Point2d>>();
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                foreach (ObjectId id in (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForRead))
                {
                    Entity e = tr.GetObject(id, OpenMode.ForRead) as Entity;
                    if (e == null || !string.Equals(e.Layer, LAYER_VUNG, StringComparison.OrdinalIgnoreCase)) continue;
                    Polyline pl = e as Polyline;
                    Circle ci = e as Circle;
                    if (KinPl(pl)) kq.Add(DinhPl(pl));
                    else if (ci != null) kq.Add(DaGiacTron(ci, 72));
                }
                tr.Commit();
            }
            return kq;
        }

        /// Người dùng 16/11 (chép nét PCCC_VUNG vào mặt bằng làm hướng dẫn, lượt sau plugin vẫn tự dựng phòng rồi vẽ chồng): vùng vẽ sẵn có điểm trong nằm trong
        /// một phòng tự dựng và không rộng quá 1,3 lần phòng đó (vẽ lại hoặc chia phòng, không phải khung bao cả tầng) thì thay phòng đó; daCo = vùng không vẽ lại.
        static List<List<Point2d>> TheoVungCoSan(List<List<Point2d>> phong, List<List<Point2d>> coSan, HashSet<List<Point2d>> daCo)
        {
            Dictionary<List<Point2d>, List<List<Point2d>>> thay = new Dictionary<List<Point2d>, List<List<Point2d>>>();
            foreach (List<Point2d> z in coSan)
            {
                Point2d c = DiemTrongMat(DienTichDaGiac(z) < 0 ? Enumerable.Reverse(z).ToList() : z);
                List<Point2d> r = phong.Where(f => TrongDaGiac(f, c)).OrderBy(f => Math.Abs(DienTichDaGiac(f))).FirstOrDefault();
                if (r == null || Math.Abs(DienTichDaGiac(z)) > 1.3 * Math.Abs(DienTichDaGiac(r))) continue;
                List<List<Point2d>> l;
                if (!thay.TryGetValue(r, out l)) { l = new List<List<Point2d>>(); thay[r] = l; }
                l.Add(z);
                daCo.Add(z);
            }
            return phong.SelectMany(f => thay.ContainsKey(f) ? thay[f] : new List<List<Point2d>> { f }).ToList();
        }

        // shortcut: ngưỡng cụm cố định (hộp bao cách ≤ cach, cụm < 20 % cụm lớn nhất); nhà nhỏ tách rời (nhà bảo vệ) bị bỏ thì bấm điểm trong phòng đó.
        /// Người dùng 16/11: quét cả block tờ rồi Enter mọi phòng thì nhận cả phòng trong hình chi tiết, mặt cắt ngoài mặt bằng. Gom phòng thành cụm
        /// (hộp bao cách nhau ≤ cach), giữ cụm có tổng diện tích ≥ 20 % cụm lớn nhất.
        /// giu (L8, người dùng duyệt 20/11): phòng giữ dù cụm nhỏ (phòng có tên "PHÒNG", vd văn phòng ở góc ngoài khối xưởng) khi hộp bao cách cụm lớn ≤ ganTen.
        /// Review 21/11: không giới hạn thì phòng có tên trong hình chi tiết cách mặt bằng 20 m cũng được rải (lỗi 16/11 quay lại).
        // shortcut: TEN_PHONG_GAN_M cố định 10 m (ca 14: nhà văn phòng cách xưởng 4 m); hình chi tiết đặt gần hơn thì bấm điểm / xoá đầu báo ở đó.
        const double TEN_PHONG_GAN_M = 10;
        static List<List<Point2d>> CumChinh(List<List<Point2d>> ds, double cach, out int boPhong, out int boCum, Func<List<Point2d>, bool> giu = null, double ganTen = 0)
        {
            int n = ds.Count;
            double[][] hb = ds.Select(d => new[] { d.Min(p => p.X), d.Min(p => p.Y), d.Max(p => p.X), d.Max(p => p.Y) }).ToArray();
            Func<int, int, double, bool> gan = (i, j, d) => hb[i][0] <= hb[j][2] + d && hb[j][0] <= hb[i][2] + d && hb[i][1] <= hb[j][3] + d && hb[j][1] <= hb[i][3] + d;
            int[] cha = Enumerable.Range(0, n).ToArray();
            Func<int, int> goc = null;
            goc = i => cha[i] == i ? i : (cha[i] = goc(cha[i]));
            for (int i = 0; i < n; i++)
                for (int j = i + 1; j < n; j++)
                    if (gan(i, j, cach)) cha[goc(i)] = goc(j);
            Dictionary<int, double> dt = new Dictionary<int, double>();
            for (int i = 0; i < n; i++) { int g = goc(i); double a; dt.TryGetValue(g, out a); dt[g] = a + Math.Abs(DienTichDaGiac(ds[i])); }
            double lon = dt.Count == 0 ? 0 : dt.Values.Max();
            bool[] cumLon = Enumerable.Range(0, n).Select(i => dt[goc(i)] >= 0.2 * lon).ToArray();
            bool[] con = Enumerable.Range(0, n).Select(i => cumLon[i] || (giu != null && giu(ds[i]) && Enumerable.Range(0, n).Any(j => cumLon[j] && gan(i, j, ganTen)))).ToArray();
            List<List<Point2d>> kq = Enumerable.Range(0, n).Where(i => con[i]).Select(i => ds[i]).ToList();
            boPhong = n - kq.Count;
            boCum = Enumerable.Range(0, n).Where(i => !con[i]).Select(i => goc(i)).Distinct().Count();
            return kq;
        }

        // shortcut: ngưỡng phòng cố định (rộng trung bình 2A/P ≥ 1 m, ≥ 1 m²); WC, hộp kỹ thuật hẹp hơn thì bấm điểm hoặc vẽ polyline PCCC_VUNG.
        /// Mặt là phòng thật: bỏ lòng tường, cột; bỏ mặt CHỨA phòng khác (khung giấy, vòng tường ngoài tách rời; sảnh có lõi thang ở giữa thì bấm điểm).
        static List<List<Point2d>> PhongThat(List<List<Point2d>> mat, double m, double aMin = 1, double rongMin = 1)
        {
            List<List<Point2d>> ung = mat.Where(v =>
            {
                double a = DienTichDaGiac(v) * m * m, p = 0;
                for (int i = 0; i < v.Count; i++) p += v[i].GetDistanceTo(v[(i + 1) % v.Count]) * m;
                return a >= aMin && 2 * a / p >= rongMin;
            }).ToList();
            Func<List<Point2d>, Point2d> diemTrong = g =>
            {
                Vector2d c = g[1] - g[0]; // mặt ngược chiều kim đồng hồ: lòng mặt ở bên trái cạnh
                return g[0] + c * 0.5 + new Vector2d(-c.Y, c.X) * 0.01;
            };
            return ung.Where(f => !ung.Any(g => g != f && TrongDaGiac(f, diemTrong(g)))).ToList();
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
                ds.Add(LamGon(d, LAM_GON_M / m, 0.002 / m));
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
