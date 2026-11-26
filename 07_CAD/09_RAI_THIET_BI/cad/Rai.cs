// Rải thiết bị theo lưới trong vùng (thao tác "rai", lệnh PCCC_RAI) và đi tuyến ống/dây qua các điểm (thao tác "tuyen").
// Không chứa trị số quy chuẩn: bước rải do người gọi đưa vào, hoặc đọc theo mã thiết bị từ 03_DU_LIEU/khoang_cach_rai.json (07_CAD/02_LUAT_VE/LUAT.md chỉ nơi tra).
// Cách vẽ theo bản mẫu của người dùng: 07_CAD/01_TRI_THUC_VE/mau/mau-ban-ve-pccc.md.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.Script.Serialization;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using AcApp = Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace TKPCCC
{
    public partial class Lenh
    {
        const string LAYER_KT_RAI = "PCCC_KT"; // chuỗi kích thước định vị thiết bị
        const string LAYER_NHAN = "PCCC_NHAN"; // chữ DN dọc ống: layer riêng để PCCC_BOC không đụng tới

        class LuoiRai
        {
            public List<Point3d> Diem = new List<Point3d>();
            public int Nx, Ny;
            public double Bx, By, Tx, Ty, Ho; // bước, cách tường, % vùng hở
            public double O;                  // diện tích bảo vệ của một thiết bị (đơn vị bản vẽ bình phương), xem ORai
            public List<List<Point2d>> Manh;  // vùng chữ L / T rải theo từng mảnh chữ nhật (TachChuNhat): dim theo mảnh; null = một lưới
        }

        /// Người dùng 25/11 (Karaoke 123 tầng 1 "dim nhảy tùm lum"): vùng chữ L (hành lang 19,8 x 1,8 m + cánh 2,7 x 5,4 m) rải MỘT lưới theo hộp bao
        /// 19,8 x 7,2 m thì điểm lưới rơi ra ngoài, bị đẩy lệch hàng, thêm điểm bù, chuỗi dim cắt ngang tường. Vùng đặc < 70 % hộp bao thì cắt thẳng (song
        /// song trục) qua đỉnh lõm thành các mảnh gần chữ nhật, mỗi mảnh rải + dim như một phòng; vùng PCCC_VUNG vẫn là một. Đường cắt chọn làm các mảnh đặc
        /// nhất (theo diện tích), hoà thì mảnh dài nhất dài hơn (hành lang liền một dải). Đỉnh lõm có cạnh kề < 0,5 m (bậc mặt tường) không là chỗ cắt;
        /// mảnh < 1 m² hoặc không đặc hơn ít nhất 5 % thì không cắt; mảnh ≥ 90 % hộp hoặc < 8 m² thì thôi cắt tiếp.
        /// shortcut: chỉ cắt theo phương X / Y (lưới xoay "goc" không cắt); vùng chữ L xiên vẫn rải một lưới.
        const double DAC_TACH = 0.7;

        static double DoDac(List<Point2d> f)
        {
            double w = f.Max(p => p.X) - f.Min(p => p.X), h = f.Max(p => p.Y) - f.Min(p => p.Y);
            return w * h > 0 ? Math.Abs(DienTichDaGiac(f)) / (w * h) : 1;
        }

        static List<List<Point2d>> TachChuNhat(List<Point2d> d, double m)
        {
            List<List<Point2d>> kq = new List<List<Point2d>>();
            if (d.Count < 5 || DoDac(d) >= DAC_TACH) { kq.Add(d); return kq; }
            double tol = 0.002 / m, e = 1 / m;
            Func<List<Point2d>, double> dt = f => Math.Abs(DienTichDaGiac(f));
            Func<List<Point2d>, double> dai = f => Math.Max(f.Max(p => p.X) - f.Min(p => p.X), f.Max(p => p.Y) - f.Min(p => p.Y));
            Func<double, double, double, double, List<Point2d>> hcn = (a0, b0, a1, b1) => new List<Point2d> { new Point2d(a0, b0), new Point2d(a1, b0), new Point2d(a1, b1), new Point2d(a0, b1) };
            Queue<List<Point2d>> cho = new Queue<List<Point2d>>();
            cho.Enqueue(d);
            int vong = 0;
            while (cho.Count > 0)
            {
                List<Point2d> f = cho.Dequeue();
                if (DoDac(f) >= 0.9 || dt(f) * m * m < 8 || ++vong > 20) { kq.Add(f); continue; }
                List<Point2d> g = new List<Point2d>(f);
                if (DienTichDaGiac(g) < 0) g.Reverse();
                double x0 = g.Min(p => p.X) - e, y0 = g.Min(p => p.Y) - e, x1 = g.Max(p => p.X) + e, y1 = g.Max(p => p.Y) + e, a = dt(g);
                List<double>[] daThu = { new List<double>(), new List<double>() }; // đường cắt đã thử: [0] x, [1] y
                List<List<Point2d>> tot = null;
                double diem = DoDac(g), dTot = 0;
                for (int i = 0; i < g.Count; i++)
                {
                    Point2d q0 = g[(i - 1 + g.Count) % g.Count], p = g[i], q1 = g[(i + 1) % g.Count];
                    Vector2d e0 = p - q0, e1 = q1 - p;
                    if (e0.X * e1.Y - e0.Y * e1.X >= 0 || e0.Length * m < 0.5 || e1.Length * m < 0.5) continue; // lồi, hoặc bậc nhỏ
                    foreach (bool doc in new[] { true, false })
                    {
                        // chỉ đỉnh có cạnh kề song song đường cắt (góc vuông theo trục)
                        if (!(Math.Abs(doc ? e0.X : e0.Y) <= tol || Math.Abs(doc ? e1.X : e1.Y) <= tol)) continue;
                        double c = doc ? p.X : p.Y;
                        if (daThu[doc ? 0 : 1].Any(t => Math.Abs(t - c) <= tol)) continue;
                        daThu[doc ? 0 : 1].Add(c);
                        HashSet<List<Point2d>> lo = new HashSet<List<Point2d>>();
                        List<List<Point2d>> pa = BoolVung(g, doc ? hcn(x0, y0, c, y1) : hcn(x0, y0, x1, c), BooleanOperationType.BoolIntersect, tol, lo);
                        List<List<Point2d>> pb = BoolVung(g, doc ? hcn(c, y0, x1, y1) : hcn(x0, c, x1, y1), BooleanOperationType.BoolIntersect, tol, lo);
                        if (pa == null || pb == null || lo.Count > 0) continue;
                        List<List<Point2d>> mh = pa.Concat(pb).Where(x => dt(x) * m * m >= 0.01).ToList();
                        if (mh.Count < 2 || mh.Any(x => dt(x) * m * m < 1)) continue;
                        double s = mh.Sum(x => dt(x) * DoDac(x)) / a, l = mh.Max(dai);
                        if (s > diem + 0.005 || (tot != null && s > diem - 0.005 && l > dTot)) { tot = mh; diem = s; dTot = l; }
                    }
                }
                if (tot == null || diem < DoDac(g) + 0.05) { kq.Add(f); continue; }
                foreach (List<Point2d> x in tot) cho.Enqueue(x);
            }
            return kq;
        }

        /// Lưới của vùng d: vùng chữ L / T (TachChuNhat ra ≥ 2 mảnh) thì mỗi mảnh một lưới, gộp điểm, nhớ mảnh để dim theo mảnh.
        static LuoiRai LuoiTach(List<Point2d> d, double m, Func<List<Point2d>, LuoiRai> luoi)
        {
            List<List<Point2d>> manh = TachChuNhat(d, m);
            if (manh.Count < 2) return luoi(d);
            List<LuoiRai> ds = manh.Select(luoi).ToList();
            LuoiRai r = new LuoiRai { Nx = ds.Sum(x => x.Nx), Ny = ds.Max(x => x.Ny), Bx = ds.Max(x => x.Bx), By = ds.Max(x => x.By), Tx = ds.Max(x => x.Tx),
                Ty = ds.Max(x => x.Ty), Ho = ds.Max(x => x.Ho), O = ds.Max(x => x.O), Manh = manh };
            foreach (LuoiRai x in ds) r.Diem.AddRange(x.Diem);
            return r;
        }

        /// Vùng để dim: một vùng d với mọi điểm, hoặc từng mảnh (LuoiRai.Manh) với điểm nằm trong / gần mảnh đó nhất.
        static List<KeyValuePair<List<Point2d>, List<Point3d>>> VungDim(List<Point2d> d, LuoiRai r, List<Point3d> diem)
        {
            List<KeyValuePair<List<Point2d>, List<Point3d>>> kq = new List<KeyValuePair<List<Point2d>, List<Point3d>>>();
            if (r.Manh == null) { kq.Add(new KeyValuePair<List<Point2d>, List<Point3d>>(d, diem)); return kq; }
            foreach (List<Point2d> p in r.Manh) kq.Add(new KeyValuePair<List<Point2d>, List<Point3d>>(p, new List<Point3d>()));
            foreach (Point3d q in diem)
            {
                Point2d c = new Point2d(q.X, q.Y);
                int i = r.Manh.FindIndex(p => TrongDaGiac(p, c));
                if (i < 0) i = Enumerable.Range(0, r.Manh.Count).OrderBy(j => CachVien(r.Manh[j], c)).First();
                kq[i].Value.Add(q);
            }
            return kq;
        }

        // ---------- Hình học ----------

        static bool TrongDaGiac(List<Point2d> d, Point2d p)
        {
            bool trong = false;
            for (int i = 0, j = d.Count - 1; i < d.Count; j = i++)
                if ((d[i].Y > p.Y) != (d[j].Y > p.Y) && p.X < (d[j].X - d[i].X) * (p.Y - d[i].Y) / (d[j].Y - d[i].Y) + d[i].X) trong = !trong;
            return trong;
        }

        /// Các khoảng [x đầu, x cuối] của đường ngang y nằm trong đa giác (cùng quy ước cắt cạnh với TrongDaGiac).
        static List<double[]> KhoangNgang(List<Point2d> d, double y)
        {
            List<double> xs = new List<double>();
            for (int i = 0, j = d.Count - 1; i < d.Count; j = i++)
                if ((d[i].Y > y) != (d[j].Y > y)) xs.Add(d[i].X + (y - d[i].Y) * (d[j].X - d[i].X) / (d[j].Y - d[i].Y));
            xs.Sort();
            List<double[]> kq = new List<double[]>();
            for (int i = 0; i + 1 < xs.Count; i += 2)
                if (xs[i + 1] - xs[i] > 1e-6) kq.Add(new[] { xs[i], xs[i + 1] });
            return kq;
        }

        /// Khoảng cách từ p tới viền đa giác q.
        static double CachVien(List<Point2d> q, Point2d p)
        {
            double kq = double.MaxValue;
            for (int i = 0; i < q.Count; i++) kq = Math.Min(kq, CachDoan(new[] { q[i], q[(i + 1) % q.Count] }, p));
            return kq;
        }

        /// L6 (review 20/11, CHẶN 2): dải hẹp là hiên khi mép trong (cạnh sát phòng thật ≤ 0,6 m; phong không gồm dải hẹp khác) là tường thật — có nét
        /// song song cách 0,08–0,45 m phủ ≥ 50 % chiều dài — còn mép ngoài không phải tường liền (nét song song phủ ≤ 80 %; Quyết Thắng: viền mái 2 nét
        /// phủ 37–76 %). Bản vẽ tường 1 nét (hành lang có cột trong tường ngoài), mép ngoài là tường liền: không phải hiên.
        /// shortcut: phân biệt tường / viền mái chỉ theo nét đôi; tường ngoài nhiều ô cửa sổ để trống (phủ < 80 %) vẫn có thể bị coi là hiên.
        static bool MepHien(List<Point2d> f, List<List<Point2d>> phong, List<Point2d[]> doan, double m)
        {
            double[] dai = new double[2], doi = new double[2]; // [0] mép trong, [1] mép ngoài
            for (int i = 0; i < f.Count; i++)
            {
                Point2d a = f[i], b = f[(i + 1) % f.Count];
                double L = a.GetDistanceTo(b);
                if (L * m < 0.5) continue;
                Vector2d u = (b - a) / L;
                Point2d g = a + u * (L / 2);
                int k = phong.Any(v => v != f && CachVien(v, g) <= 0.6 / m) ? 0 : 1;
                double phu = 0;
                foreach (Point2d[] s in doan)
                {
                    Vector2d w = s[1] - s[0];
                    if (Math.Abs(u.X * w.Y - u.Y * w.X) > 0.02 * w.Length) continue;
                    double lech = Math.Abs(u.X * (s[0].Y - a.Y) - u.Y * (s[0].X - a.X)) * m;
                    if (lech < 0.08 || lech > 0.45) continue;
                    double p0 = u.DotProduct(s[0] - a), p1 = u.DotProduct(s[1] - a);
                    double ov = Math.Max(0, Math.Min(L, Math.Max(p0, p1)) - Math.Max(0, Math.Min(p0, p1)));
                    phu += ov;
                }
                dai[k] += L;
                doi[k] += Math.Min(L, phu);
            }
            return dai[0] > 0 && doi[0] >= 0.5 * dai[0] && doi[1] <= 0.8 * dai[1];
        }

        /// Một dải ứng viên hành lang bên: phương dọc, đường mép ngoài, khoảng dọc, phủ lan can mép ngoài, số cửa.
        /// Dài / rộng của đa giác theo phương cạnh dài nhất.
        static double TiLeDai(List<Point2d> f)
        {
            int iDai = 0; double lDai = 0;
            for (int i = 0; i < f.Count; i++) { double L = f[i].GetDistanceTo(f[(i + 1) % f.Count]); if (L > lDai) { lDai = L; iDai = i; } }
            if (lDai <= 0) return 1;
            Vector2d u = (f[(iDai + 1) % f.Count] - f[iDai]).GetNormal(), n = u.GetPerpendicularVector();
            double du = f.Max(p => u.DotProduct(p.GetAsVector())) - f.Min(p => u.DotProduct(p.GetAsVector()));
            double dn = f.Max(p => n.DotProduct(p.GetAsVector())) - f.Min(p => n.DotProduct(p.GetAsVector()));
            return dn <= 0 ? double.MaxValue : du / dn;
        }
        class DaiBen { public List<Point2d> F; public Vector2d U; public double MepNgoai, U0, U1, LanCan; public int Cua; public bool Nhan; }

        /// Người dùng 23/11 (trường học khung 6: "hành lang bên làm gì rải đầu báo"; QCVN 10:2025 1.5.11 miễn báo cháy cho hành lang bên), mọi loại trần.
        /// Ứng viên: dải rộng 0,8–3 m (theo phương cạnh dài nhất), MỘT mép dọc không có phòng nào cách ≤ 0,6 m (mở ra sân trong / ngoài nhà) trên ≥ 60 %
        /// chiều dài, mép dọc kia giáp phòng ≥ 40 %, không có chữ tên phòng. Nhận khi mép ngoài có lan can (LanCan phủ ≥ 50 %), hoặc thuộc chuỗi dải
        /// cùng đường mép ngoài (lệch ≤ 0,15 m, hở ≤ 6 m) có dải đã nhận, hoặc chuỗi ≥ 3 dải dài ≥ 12 m mà ≥ 2/3 dải không có cửa (dải 1,65 m ngoài
        /// cửa sổ dãy văn phòng, mép ngoài lan can mảnh không thành LanCan). Dải ứng viên lẻ không lan can (kho hẹp sát tường ngoài) vẫn rải.
        /// shortcut: "không có phòng bên kia" tính theo các mặt đã dựng; sân trong không có dấu X (thành một mặt kín) thì không nhận.
        static List<List<Point2d>> HanhLangBen(List<List<Point2d>> vung, List<List<Point2d>> phong, List<Point2d[]> lanCan, List<Point2d[]> cua,
            List<Point2d> tenPhong, double m)
        {
            List<DaiBen> ds = new List<DaiBen>();
            double ke = 0.6 / m;
            foreach (List<Point2d> f in vung)
            {
                if (Math.Abs(DienTichDaGiac(f)) * m * m > 250 || tenPhong.Any(p => TrongDaGiac(f, p))) continue;
                int iDai = -1; double lDai = 0;
                for (int i = 0; i < f.Count; i++) { double L = f[i].GetDistanceTo(f[(i + 1) % f.Count]); if (L > lDai) { lDai = L; iDai = i; } }
                if (iDai < 0) continue;
                Vector2d u = (f[(iDai + 1) % f.Count] - f[iDai]).GetNormal();
                if (Math.Abs(u.X) >= Math.Abs(u.Y) ? u.X < 0 : u.Y < 0) u = -u; // chiều chuẩn (cạnh gần đứng lệch 1e-7 vẫn cùng chiều): cùng đường mép thì so được
                Vector2d n = u.GetPerpendicularVector();
                double u0 = f.Min(p => u.DotProduct(p.GetAsVector())), u1 = f.Max(p => u.DotProduct(p.GetAsVector()));
                double n0 = f.Min(p => n.DotProduct(p.GetAsVector())), n1 = f.Max(p => n.DotProduct(p.GetAsVector()));
                double W = (n1 - n0) * m, Lu = u1 - u0;
                if (W < 0.8 || W > 3 || Lu * m < W) continue;
                double[] ngoai = new double[2], trong = new double[2], lc = new double[2];
                for (int i = 0; i < f.Count; i++)
                {
                    Point2d a = f[i], b = f[(i + 1) % f.Count];
                    double L = a.GetDistanceTo(b);
                    if (L * m < 0.3) continue;
                    Vector2d w = (b - a) / L;
                    if (Math.Abs(w.X * u.Y - w.Y * u.X) > 0.05) continue;
                    double t = n.DotProduct((a + (b - a) * 0.5).GetAsVector());
                    int s = (t - n0) * m <= 0.5 ? 0 : (n1 - t) * m <= 0.5 ? 1 : -1;
                    if (s < 0) continue;
                    // Điểm thử dời ra phía ngoài mép 0,4 m: phòng bên kia (sau tường ≤ 0,65 m) chứa hoặc cách ≤ 0,25 m. Dời theo pháp tuyến để dải kề
                    // nối đầu (ô dải 1,65 m chia theo dầm) không bị tính là phòng bên kia.
                    Vector2d ra = (s == 0 ? -n : n) * (0.4 / m);
                    int k = Math.Max(1, (int)Math.Round(L * m)), co = 0;
                    for (int j = 0; j < k; j++)
                    {
                        Point2d g = a + (b - a) * ((j + 0.5) / k) + ra;
                        if (phong.Any(v => v != f && (CachVien(v, g) <= 0.25 / m || TrongDaGiac(v, g)))) co++;
                    }
                    trong[s] += L * co / k;
                    ngoai[s] += L * (k - co) / k;
                    foreach (Point2d[] q in lanCan)
                    {
                        Vector2d v = q[1] - q[0];
                        if (v.Length < 1e-9 || Math.Abs(w.X * v.Y - w.Y * v.X) > 0.02 * v.Length) continue;
                        if (Math.Abs(w.X * (q[0].Y - a.Y) - w.Y * (q[0].X - a.X)) * m > 0.6) continue;
                        double p0 = w.DotProduct(q[0] - a), p1 = w.DotProduct(q[1] - a);
                        lc[s] += Math.Max(0, Math.Min(L, Math.Max(p0, p1)) - Math.Max(0, Math.Min(p0, p1)));
                    }
                }
                for (int s = 0; s < 2; s++)
                {
                    if (ngoai[s] < 0.6 * Lu || trong[1 - s] < 0.4 * Lu) continue;
                    ds.Add(new DaiBen { F = f, U = u, MepNgoai = s == 0 ? n0 : n1, U0 = u0, U1 = u1, LanCan = Math.Min(1, lc[s] / Lu),
                        Cua = cua.Count(h => { Point2d c = new Point2d((h[0].X + h[1].X) / 2, (h[0].Y + h[1].Y) / 2); return TrongDaGiac(f, c) || CachVien(f, c) <= 0.3 / m; }),
                        Nhan = lc[s] >= 0.5 * Lu });
                    break;
                }
            }
            // Chuỗi: cùng phương, cùng đường mép ngoài (lệch ≤ 0,15 m), nối nhau theo phương dọc khi hở ≤ 6 m.
            List<DaiBen> conLai = new List<DaiBen>(ds);
            while (conLai.Count > 0)
            {
                List<DaiBen> chuoi = new List<DaiBen> { conLai[0] };
                conLai.RemoveAt(0);
                for (bool them = true; them; )
                {
                    them = false;
                    foreach (DaiBen d in conLai.ToList())
                        if (chuoi.Any(c => Math.Abs(c.U.X * d.U.Y - c.U.Y * d.U.X) <= 0.02 && Math.Abs(c.MepNgoai - d.MepNgoai) * m <= 0.15
                            && Math.Max(c.U0, d.U0) - Math.Min(c.U1, d.U1) <= 6 / m))
                        { chuoi.Add(d); conLai.Remove(d); them = true; }
                }
                double dai = chuoi.Sum(c => c.U1 - c.U0) * m;
                bool nhan = chuoi.Any(c => c.Nhan) || (chuoi.Count >= 3 && dai >= 12 && chuoi.Count(c => c.Cua == 0) * 3 >= chuoi.Count * 2);
                if (nhan) foreach (DaiBen c in chuoi) c.Nhan = true;
            }
            return ds.Where(d => d.Nhan).Select(d => d.F).Distinct().ToList();
        }

        static List<Point2d> DinhPl(Polyline pl)
        {
            List<Point2d> d = new List<Point2d>();
            for (int i = 0; i < pl.NumberOfVertices; i++) d.Add(pl.GetPoint2dAt(i)); // shortcut: cung (bulge) coi là đoạn thẳng, phòng tường cong thì thêm chia cung
            if (d.Count > 3 && d[0].GetDistanceTo(d[d.Count - 1]) <= SaiSoKin(pl)) d.RemoveAt(d.Count - 1); // đỉnh cuối trùng đỉnh đầu (KinPl)
            return d;
        }

        static double SaiSoKin(Polyline pl) { return Math.Max(1e-6, 1e-7 * pl.Length); }

        /// Polyline vùng kín: cờ Closed, hoặc điểm cuối trùng điểm đầu (người dùng 16/11: vùng xanh lá vẽ tay về đúng điểm đầu nhưng không gõ C,
        /// Closed = 0, plugin bỏ qua vùng đó rồi rải sang chỗ khác).
        static bool KinPl(Polyline pl)
        {
            if (pl == null || pl.NumberOfVertices < 3) return false;
            return pl.Closed || (pl.NumberOfVertices >= 4 && pl.GetPoint2dAt(0).GetDistanceTo(pl.GetPoint2dAt(pl.NumberOfVertices - 1)) <= SaiSoKin(pl));
        }

        static Point3d ChanVuongGoc(Point3d p, Point3d a, Point3d b)
        {
            Vector3d ab = b - a;
            double t = ab.LengthSqrd < 1e-12 ? 0 : Math.Max(0, Math.Min(1, (p - a).DotProduct(ab) / ab.LengthSqrd));
            return a + ab * t;
        }

        /// Đoạn của ống chính gần điểm p nhất (chỉ số đỉnh đầu đoạn).
        static int DoanGan(List<Point3d> truc, Point3d p)
        {
            int tot = 0;
            double dTot = double.MaxValue;
            for (int i = 0; i + 1 < truc.Count; i++)
            {
                double d = p.DistanceTo(ChanVuongGoc(p, truc[i], truc[i + 1]));
                if (d < dTot) { dTot = d; tot = i; }
            }
            return tot;
        }

        /// Điểm gần p nhất trên CẢ polyline ống chính (không chỉ một đoạn).
        static Point3d ChanTruc(List<Point3d> truc, Point3d p)
        {
            int i = DoanGan(truc, p);
            return ChanVuongGoc(p, truc[i], truc[i + 1]);
        }

        /// Gom điểm thành nhóm theo một toạ độ (chênh không quá sai số thì cùng nhóm); nhóm xếp tăng dần.
        static List<List<Point3d>> Gom(IEnumerable<Point3d> ps, Func<Point3d, double> f, double saiSo)
        {
            List<List<Point3d>> kq = new List<List<Point3d>>();
            double truoc = 0;
            foreach (Point3d p in ps.OrderBy(f))
            {
                double x = f(p);
                if (kq.Count == 0 || x - truoc > saiSo) kq.Add(new List<Point3d>());
                kq[kq.Count - 1].Add(p);
                truoc = x;
            }
            return kq;
        }

        // ---------- Rải ----------

        /// Chia một chiều dài W thành n điểm bước b, cách tường t (lưới luôn đặt giữa).
        /// dung > 0: bước ĐÚNG, n = số điểm nhiều nhất mà cách tường còn >= le (mặc định bước / 4).
        /// ngược lại toiDa là bước TỐI ĐA: n = ceil(W / toiDa) (không ít hơn nMin), bước chia đều rồi làm tròn xuống theo chan nếu cách tường không vượt toiDa / 2.
        /// Có le (>= 0) cùng bước tối đa: giữ n, dàn bước = min(toiDa, (W - 2 le) / (n - 1)) để cách tường gần le thay vì nửa bước;
        /// le lớn hơn toiDa / 2 thì lấy toiDa / 2 (cách tường vẫn không vượt toiDa / 2).
        static void ChiaBuoc(double W, double toiDa, double dung, double le, double chan, int nMin, out int n, out double b, out double t)
        {
            if (dung > 0)
            {
                n = Math.Max(1, (int)Math.Floor((W - 2 * (le >= 0 ? le : dung / 4)) / dung + 1e-9) + 1);
                b = dung;
            }
            else
            {
                n = Math.Max(Math.Max(1, nMin), (int)Math.Ceiling(W / toiDa - 1e-9));
                b = W / n;
                double bl = n > 1 && le >= 0 ? (W - 2 * Math.Min(le, toiDa / 2)) / (n - 1) : 0;
                if (bl > 0) b = Math.Min(toiDa, bl); // bl <= 0: vùng hẹp hơn 2 le, giữ chia đều
                if (chan > 0 && n > 1)
                {
                    double tron = Math.Floor(b / chan + 1e-9) * chan;
                    if (tron > 0 && (W - (n - 1) * tron) / 2 <= toiDa / 2 + 1e-9) b = tron;
                }
            }
            t = (W - (n - 1) * b) / 2;
        }

        /// Diện tích bảo vệ của một thiết bị = Sx × Sy; mỗi phương S là số lớn hơn giữa bước và 2 lần cách tường (chỉ một hàng thì S = cả bề rộng vùng).
        static double ORai(LuoiRai k)
        {
            return (k.Nx > 1 ? Math.Max(k.Bx, 2 * k.Tx) : 2 * k.Tx) * (k.Ny > 1 ? Math.Max(k.By, 2 * k.Ty) : 2 * k.Ty);
        }

        /// Khoảng cách từ p tới đường bao đa giác.
        static double KhoangBien(List<Point2d> d, Point2d p)
        {
            double kq = double.MaxValue;
            for (int i = 0, j = d.Count - 1; i < d.Count; j = i++)
            {
                Vector2d ab = d[i] - d[j];
                double t = ab.LengthSqrd < 1e-12 ? 0 : Math.Max(0, Math.Min(1, (p - d[j]).DotProduct(ab) / ab.LengthSqrd));
                kq = Math.Min(kq, p.GetDistanceTo(d[j] + ab * t));
            }
            return kq;
        }

        /// Cách tường tối thiểu khi mã thiết bị chưa có "tuong_min" trong 03_DU_LIEU/khoang_cach_rai.json (vd đầu phun): không phải trị số quy chuẩn,
        /// chỉ để thiết bị không nằm trên / sát nét vùng (người dùng 19/11). Có trị số quy chuẩn thì thêm "tuong_min" vào json.
        const double CACH_TUONG_MIN_M = 0.1;

        /// Cách tường tối thiểu (đơn vị bản vẽ) của dòng tiêu chuẩn tc: "tuong_min", không có thì CACH_TUONG_MIN_M.
        static double CachMinRai(Dictionary<string, object> tc, double m)
        {
            double c = tc == null ? 0 : LaySo(tc, "tuong_min", 0);
            return (c > 0 ? c : CACH_TUONG_MIN_M) / m;
        }

        /// p trong vùng q và cách mọi cạnh ít nhất c.
        static bool DatCach(List<Point2d> q, Point2d p, double c)
        {
            return TrongDaGiac(q, p) && KhoangBien(q, p) >= c * (1 - 1e-6);
        }

        /// Kẹp p vào dây cung ngang (doc = false) hoặc dọc (doc = true) của q đi qua p: cách hai cạnh ở hai đầu dây cung ít nhất c,
        /// cạnh xiên lùi c / sin góc giữa cạnh và dây cung. Dây cung hẹp hơn tổng hai khoảng lùi thì đặt giữa dây cung.
        static Point2d KepDay(List<Point2d> q, Point2d p, double c, bool doc)
        {
            Func<Point2d, double> u = v => doc ? v.Y : v.X, w = v => doc ? v.X : v.Y; // u dọc dây cung, w toạ độ của dây cung
            double wp = w(p), up = u(p);
            List<double[]> cat = new List<double[]>(); // [u giao, khoảng lùi]
            for (int i = 0, j = q.Count - 1; i < q.Count; j = i++)
                if ((w(q[i]) > wp) != (w(q[j]) > wp))
                {
                    double du = u(q[i]) - u(q[j]), dw = w(q[i]) - w(q[j]);
                    cat.Add(new[] { u(q[j]) + (wp - w(q[j])) * du / dw, c * (1 + 1e-6) * Math.Sqrt(du * du + dw * dw) / Math.Abs(dw) });
                }
            cat.Sort((x, y) => x[0].CompareTo(y[0]));
            int tot = -1;
            double lech = double.MaxValue;
            for (int i = 0; i + 1 < cat.Count; i += 2)
            {
                double l = up < cat[i][0] ? cat[i][0] - up : up > cat[i + 1][0] ? up - cat[i + 1][0] : 0;
                if (l < lech) { lech = l; tot = i; }
            }
            if (tot < 0) return p;
            double a = cat[tot][0] + cat[tot][1], b = cat[tot + 1][0] - cat[tot + 1][1];
            double kq = a > b ? (cat[tot][0] + cat[tot + 1][0]) / 2 : Math.Max(a, Math.Min(b, up));
            return doc ? new Point2d(wp, kq) : new Point2d(kq, wp);
        }

        /// Dời p vào trong vùng q, cách mọi cạnh ít nhất c (người dùng 19/11: đầu báo rải theo hàng cột đều lệch ra ngoài / nằm trên nét vùng
        /// ở cạnh xiên và hành lang hẹp; được lệch hàng, cột). Mỗi bước kẹp theo hàng hoặc theo cột, lấy bước dời ít hơn (hoà thì theo hàng,
        /// giữ thẳng hàng cho dim gom hàng); góc lõm (kẹp hai phương không đủ) thì đẩy ra xa điểm gần nhất của đường bao.
        /// Khe hẹp hơn 2c thì không có chỗ đạt: trả vị trí đã thử xa đường bao nhất (giữa khe).
        static Point2d DoiVao(List<Point2d> q, Point2d p, double c)
        {
            if (DatCach(q, p, c)) return p;
            Point2d r = p, tot = p;
            double cachTot = TrongDaGiac(q, p) ? KhoangBien(q, p) : -1, e = 1e-9 * c;
            Func<Point2d, bool> thu = v =>
            {
                if (!TrongDaGiac(q, v)) return false;
                double k = KhoangBien(q, v);
                if (k > cachTot) { cachTot = k; tot = v; }
                return k >= c * (1 - 1e-6);
            };
            for (int lan = 0; lan < 4; lan++)
            {
                Point2d h = KepDay(q, r, c, false), v = KepDay(q, r, c, true);
                double dh = h.GetDistanceTo(r), dv = v.GetDistanceTo(r);
                if (dh <= e && dv <= e) break;
                r = dh > e && (dv <= e || dh <= dv + e) ? h : v;
                if (thu(r)) return r;
            }
            for (int lan = 0; lan < 4; lan++)
            {
                // Điểm gần nhất trên đường bao, đẩy r ra xa nó đúng c (r nằm trên nét thì theo pháp tuyến cạnh, phía trong vùng).
                Point2d b = r;
                double gan = double.MaxValue;
                Vector2d phap = new Vector2d(0, 0);
                for (int i = 0, j = q.Count - 1; i < q.Count; j = i++)
                {
                    Vector2d ab = q[i] - q[j];
                    double t = ab.LengthSqrd < 1e-12 ? 0 : Math.Max(0, Math.Min(1, (r - q[j]).DotProduct(ab) / ab.LengthSqrd));
                    Point2d x = q[j] + ab * t;
                    if (r.GetDistanceTo(x) < gan) { gan = r.GetDistanceTo(x); b = x; phap = ab.Length < 1e-12 ? phap : new Vector2d(-ab.Y, ab.X) / ab.Length; }
                }
                Vector2d huong = gan > e ? (r - b) / gan : TrongDaGiac(q, b + phap * c) ? phap : -phap;
                Point2d moi = b + huong * (c * (1 + 1e-6));
                if (moi.GetDistanceTo(r) <= e) break;
                r = moi;
                if (thu(r)) return r;
            }
            return tot;
        }

        /// Dời mọi điểm của luoi vào trong vùng (DoiVao); trả số điểm đã dời.
        static int DoiVaoVung(List<Point2d> q, List<Point2d> luoi, double c)
        {
            int n = 0;
            for (int i = 0; i < luoi.Count; i++)
            {
                Point2d moi = DoiVao(q, luoi[i], c);
                if (moi.GetDistanceTo(luoi[i]) > 1e-9 * c) { luoi[i] = moi; n++; }
            }
            return n;
        }

        /// Phủ kín vùng q: điểm mẫu (lưới trong vùng + dọc từng cạnh, kể cả đỉnh) phải có một thiết bị cách nó không quá hx theo X và hy theo Y
        /// (đầu báo cách tường xiên 6771 mm theo phương đứng khi tối đa 5250 là lỗi người dùng gặp 13/11). Còn điểm hở thì thêm thiết bị:
        /// lấy điểm hở thấp nhất (rồi trái nhất), thử các vị trí quanh nó (cột / hàng lưới đang có trước, rồi lưới 9 x 9 trong ô phủ),
        /// chọn vị trí trong vùng, cách tường >= cachMin, phủ được nhiều điểm hở nhất. Điểm mẫu có thể lọt khe hẹp hơn bước mẫu nên sau mỗi lượt
        /// kiểm chính xác bằng DiemHo (đỉnh của mọi khe hở) và rải tiếp tới khi hết. Trả % điểm mẫu trong vùng còn hở (0 khi phủ kín).
        static double PhuKin(List<Point2d> q, List<Point2d> luoi, double hx, double hy, double cachMin)
        {
            double x0 = q.Min(p => p.X), x1 = q.Max(p => p.X), y0 = q.Min(p => p.Y), y1 = q.Max(p => p.Y);
            double buoc = Math.Max(Math.Min(hx, hy) / 25, Math.Sqrt((x1 - x0) * (y1 - y0) / 40000)), e = 1e-6 * (hx + hy);
            List<Point2d> mau = new List<Point2d>();
            for (double y = y0 + buoc / 2; y < y1; y += buoc)
                for (double x = x0 + buoc / 2; x < x1; x += buoc)
                    if (TrongDaGiac(q, new Point2d(x, y))) mau.Add(new Point2d(x, y));
            int nTrong = mau.Count;
            for (int i = 0, j = q.Count - 1; i < q.Count; j = i++)
            {
                int n = Math.Max(1, (int)Math.Ceiling(q[j].GetDistanceTo(q[i]) / buoc));
                for (int t = 0; t < n; t++) mau.Add(q[j] + (q[i] - q[j]) * ((double)t / n));
            }
            Func<Point2d, Point2d, bool> phu = (p, c) => Math.Abs(p.X - c.X) <= hx + e && Math.Abs(p.Y - c.Y) <= hy + e;
            // Review 15/11 (vẽ mượt): tra bằng lưới băm ô (hx + 2e) x (hy + 2e) (mọi điểm thiết bị phủ nằm trong 3 x 3 ô quanh nó) và mảng đếm
            // số thiết bị phủ mỗi mẫu (dem); kết quả và thứ tự xét giữ y như cách quét hết danh sách trước đây.
            double wx = hx + 2 * e, wy = hy + 2 * e;
            Dictionary<long, List<int>> oMau = new Dictionary<long, List<int>>();
            List<int> dem = new List<int>();
            Action<int> themMau = s =>
            {
                long k = KhoaO(mau[s], wx, wy);
                List<int> l;
                if (!oMau.TryGetValue(k, out l)) { l = new List<int>(); oMau[k] = l; }
                l.Add(s);
                dem.Add(0);
            };
            // Gọi f cho mọi mẫu c phủ được (mỗi mẫu đúng một lần: 9 ô quanh c có khoá khác nhau).
            Action<Point2d, Action<int>> moiMauPhu = (c, f) =>
            {
                long ix = (long)Math.Floor(c.X / wx), iy = (long)Math.Floor(c.Y / wy);
                for (long a = ix - 1; a <= ix + 1; a++)
                    for (long b = iy - 1; b <= iy + 1; b++)
                    {
                        List<int> l;
                        if (oMau.TryGetValue(a * 2654435761L + b, out l))
                            foreach (int s in l) if (phu(mau[s], c)) f(s);
                    }
            };
            for (int s = 0; s < mau.Count; s++) themMau(s);
            foreach (Point2d c in luoi) moiMauPhu(c, s => dem[s]++);
            // ho luôn là đúng các mẫu dem = 0 (dem chỉ tăng trong lúc thêm thiết bị).
            List<int> ho = Enumerable.Range(0, mau.Count).Where(i => dem[i] == 0)
                .OrderBy(i => Math.Round(mau[i].Y / e)).ThenBy(i => mau[i].X).ToList();
            int them = 0;
            for (int luot = 0; luot < 20; luot++)
            {
                if (ho.Count == 0)
                {
                    List<Point2d> sot = DiemHo(q, luoi, hx, hy, phu);
                    if (sot.Count == 0) break;
                    // Khe hẹp hơn bước mẫu: mẫu dày (bước / 4) quanh từng đỉnh khe để chấm điểm vị trí theo đúng cỡ khe.
                    ChiMucLuoi cl = new ChiMucLuoi(luoi, hx, hy);
                    foreach (Point2d s in sot.ToList())
                        for (int i = -4; i <= 4; i++)
                            for (int j = -4; j <= 4; j++)
                            {
                                Point2d m = new Point2d(s.X + buoc * i / 4, s.Y + buoc * j / 4);
                                if (TrongDaGiac(q, m) && !cl.CoPhu(m, phu)) sot.Add(m);
                            }
                    ho = Enumerable.Range(mau.Count, sot.Count).ToList();
                    mau.AddRange(sot);
                    for (int s = mau.Count - sot.Count; s < mau.Count; s++) themMau(s);
                }
                for (; ho.Count > 0 && them < 5000; them++)
                {
                    if (them % 20 == 0) KiemEsc();
                    Point2d P = mau[ho[0]];
                    List<Point2d> thu = new List<Point2d>();
                    foreach (double cy in luoi.Select(c => c.Y).Distinct().Where(v => Math.Abs(v - P.Y) <= hy))
                        foreach (double cx in luoi.Select(c => c.X).Distinct().Where(v => Math.Abs(v - P.X) <= hx)) thu.Add(new Point2d(cx, cy));
                    List<Point2d> luoiThu = new List<Point2d>();
                    for (int i = 0; i <= 8; i++)
                        for (int j = 0; j <= 8; j++) luoiThu.Add(new Point2d(P.X - hx + hx * i / 4, P.Y - hy + hy * j / 4));
                    thu.AddRange(luoiThu.OrderBy(c => c.GetDistanceTo(P))); // hoà điểm thì gần điểm hở hơn (ô phủ dư ra hai phía)
                    // Không vị trí nào trong vùng: đặt ở điểm hở dời vào trong (DoiVao), dời rồi không còn phủ điểm hở thì đặt ngay điểm hở (luôn thuộc vùng).
                    Point2d tot = cachMin > 0 ? DoiVao(q, P, cachMin) : P;
                    if (!phu(P, tot)) tot = P;
                    int diemTot = 0;
                    for (int lan = 0; lan < 2 && diemTot == 0; lan++) // lần 2 nhận cả vị trí dời rồi vẫn sát tường (khe hẹp hơn 2 lần cách tường tối thiểu)
                        foreach (Point2d c0 in thu)
                        {
                            if (!TrongDaGiac(q, c0)) continue;
                            // Người dùng 19/11: vị trí thử sát tường thì dời vào (DoiVao) chứ không bỏ, không thì khe hẹp chỉ còn vị trí trên nét vùng.
                            Point2d c = cachMin > 0 && KhoangBien(q, c0) < cachMin ? DoiVao(q, c0, cachMin) : c0;
                            if (lan == 0 && cachMin > 0 && !DatCach(q, c, cachMin)) continue;
                            int diem = 0; // vị trí lưới đứng trước nên hoà thì giữ thẳng hàng
                            moiMauPhu(c, s => { if (dem[s] == 0) diem++; });
                            if (diem > diemTot) { diemTot = diem; tot = c; }
                        }
                    luoi.Add(tot);
                    moiMauPhu(tot, s => dem[s]++);
                    ho.RemoveAll(i => phu(mau[i], tot));
                }
            }
            if (ho.Count == 0)
            {
                if (DiemHo(q, luoi, hx, hy, phu).Count > 0) return 0.1; // còn khe sau 20 lượt (không gặp khi thử): báo hở để người vẽ xem
                // Tham lam có thể thêm thừa (thiết bị thêm sau phủ hết phần của thiết bị thêm trước): bỏ thử từng cái, từ cái thêm sau cùng.
                // Bỏ c làm hở một mẫu khi và chỉ khi có mẫu c phủ mà dem = 1 (mọi mẫu đang được phủ).
                for (int i = luoi.Count - 1, dau = luoi.Count - them; i >= dau; i--)
                {
                    Point2d c = luoi[i];
                    luoi.RemoveAt(i);
                    bool hoMau = false;
                    moiMauPhu(c, s => { if (dem[s] == 1) hoMau = true; });
                    if (hoMau || DiemHo(q, luoi, hx, hy, phu).Count > 0) luoi.Insert(i, c);
                    else moiMauPhu(c, s => dem[s]--);
                }
            }
            return nTrong == 0 ? 0 : Math.Round(100.0 * ho.Count(i => i < nTrong) / nTrong, 1);
        }

        /// Khoá ô lưới băm: hai ô trong cùng vùng 5 x 5 ô không bao giờ trùng khoá.
        static long KhoaO(Point2d p, double wx, double wy)
        {
            return (long)Math.Floor(p.X / wx) * 2654435761L + (long)Math.Floor(p.Y / wy);
        }

        /// Chỉ mục thiết bị theo ô 2hx x 2hy (review 15/11, vẽ mượt): thiết bị phủ p (lệch ≤ hx + e) hay cách thiết bị khác ≤ 2hx, 2hy
        /// đều nằm trong 3 x 3 ô quanh nó. Chỉ số thiết bị trong mỗi ô tăng dần.
        class ChiMucLuoi
        {
            readonly List<Point2d> luoi;
            readonly double wx, wy;
            readonly Dictionary<long, List<int>> o = new Dictionary<long, List<int>>();
            public ChiMucLuoi(List<Point2d> luoi, double hx, double hy)
            {
                this.luoi = luoi; wx = 2 * hx; wy = 2 * hy;
                for (int i = 0; i < luoi.Count; i++)
                {
                    long k = KhoaO(luoi[i], wx, wy);
                    List<int> l;
                    if (!o.TryGetValue(k, out l)) { l = new List<int>(); o[k] = l; }
                    l.Add(i);
                }
            }
            /// Chỉ số các thiết bị trong 3 x 3 ô quanh p, tăng dần.
            public List<int> Gan(Point2d p)
            {
                List<int> kq = new List<int>();
                long ix = (long)Math.Floor(p.X / wx), iy = (long)Math.Floor(p.Y / wy);
                for (long a = ix - 1; a <= ix + 1; a++)
                    for (long b = iy - 1; b <= iy + 1; b++)
                    {
                        List<int> l;
                        if (o.TryGetValue(a * 2654435761L + b, out l)) kq.AddRange(l);
                    }
                kq.Sort();
                return kq;
            }
            public bool CoPhu(Point2d p, Func<Point2d, Point2d, bool> phu)
            {
                foreach (int i in Gan(p)) if (phu(p, luoi[i])) return true;
                return false;
            }
        }

        /// Điểm trong vùng chưa được ô phủ nào chứa, tìm CHÍNH XÁC: mỗi khe hở (vùng trừ hợp các ô chữ nhật) có một đỉnh là đỉnh vùng,
        /// giao cạnh vùng với cạnh ô, hoặc giao cạnh hai ô; thử 4 điểm lệch chéo rất nhỏ quanh từng đỉnh đó.
        static List<Point2d> DiemHo(List<Point2d> q, List<Point2d> luoi, double hx, double hy, Func<Point2d, Point2d, bool> phu)
        {
            double eps = 1e-4 * (hx + hy);
            ChiMucLuoi cl = new ChiMucLuoi(luoi, hx, hy);
            List<Point2d> dinh = new List<Point2d>(q);
            for (int i = 0, j = q.Count - 1; i < q.Count; j = i++)
            {
                Point2d a = q[j];
                Vector2d ab = q[i] - a;
                foreach (Point2d c in luoi)
                {
                    foreach (double x in new[] { c.X - hx, c.X + hx })
                        if (Math.Abs(ab.X) > 1e-12)
                        {
                            double t = (x - a.X) / ab.X, y = a.Y + t * ab.Y;
                            if (t >= 0 && t <= 1 && Math.Abs(y - c.Y) <= hy) dinh.Add(new Point2d(x, y));
                        }
                    foreach (double y in new[] { c.Y - hy, c.Y + hy })
                        if (Math.Abs(ab.Y) > 1e-12)
                        {
                            double t = (y - a.Y) / ab.Y, x = a.X + t * ab.X;
                            if (t >= 0 && t <= 1 && Math.Abs(x - c.X) <= hx) dinh.Add(new Point2d(x, y));
                        }
                }
            }
            foreach (Point2d c in luoi)
                foreach (int k in cl.Gan(c)) // cùng thứ tự với quét hết danh sách: chỉ số tăng dần
                {
                    Point2d d = luoi[k];
                    if (Math.Abs(c.X - d.X) <= 2 * hx && Math.Abs(c.Y - d.Y) <= 2 * hy)
                        foreach (double x in new[] { c.X - hx, c.X + hx })
                            foreach (double y in new[] { d.Y - hy, d.Y + hy })
                                if (Math.Abs(x - d.X) <= hx && Math.Abs(y - c.Y) <= hy) dinh.Add(new Point2d(x, y));
                }
            List<Point2d> kq = new List<Point2d>();
            foreach (Point2d p in dinh)
                foreach (Vector2d v in new[] { new Vector2d(eps, eps), new Vector2d(-eps, eps), new Vector2d(eps, -eps), new Vector2d(-eps, -eps) })
                {
                    Point2d m = p + v;
                    if (TrongDaGiac(q, m) && !cl.CoPhu(m, phu)) kq.Add(m);
                }
            return kq;
        }

        /// Góc lồi của vùng và thiết bị gần nó nhất: kích thước chéo góc như bản mẫu (TCVN 7568-14 Hình 2: 7,2 m), không vẽ vòng tròn.
        /// shortcut: không xét đường chéo có cắt tường không (vùng lõm sâu), cần thì lọc thiết bị nhìn thấy góc.
        static List<Point2d[]> CheoGoc(List<Point2d> vung, List<Point3d> diem)
        {
            List<Point2d[]> kq = new List<Point2d[]>();
            if (diem.Count == 0) return kq;
            double s = 0;
            for (int i = 0, j = vung.Count - 1; i < vung.Count; j = i++) s += vung[j].X * vung[i].Y - vung[i].X * vung[j].Y;
            for (int i = 0; i < vung.Count; i++)
            {
                Point2d a = vung[(i + vung.Count - 1) % vung.Count], b = vung[i], c = vung[(i + 1) % vung.Count];
                Vector2d u = b - a, v = c - b;
                double x = u.X * v.Y - u.Y * v.X;
                if (x * s <= 1e-9 * u.Length * v.Length * Math.Abs(s)) continue; // góc lõm hoặc thẳng hàng
                Point3d gan = diem.OrderBy(p => (p.X - b.X) * (p.X - b.X) + (p.Y - b.Y) * (p.Y - b.Y)).First();
                kq.Add(new[] { b, new Point2d(gan.X, gan.Y) });
            }
            return kq;
        }

        /// Lưới thiết bị trong một vùng (đa giác). Lưới tính theo hộp bao (xoay gocDo nếu phòng nghiêng), bỏ điểm ngoài đa giác,
        /// rồi tự kiểm vùng hở: điểm nào trong vùng xa mọi thiết bị hơn nửa đường chéo ô lưới cho phép thì tính là hở.
        /// dt > 0 (đơn vị bản vẽ bình phương) = diện tích bảo vệ tối đa của một thiết bị. Rải theo bước tối đa thì lấy số cột × số hàng ÍT thiết bị nhất
        /// mà ô lưới không vượt dt (hoà thì lấy ô vuông hơn): 16 × 12 m, s 4 m, dt 12 m2 ra 4 × 4 (bước 4000 × 3000), không phải 4 × 3 (16 m2) hay 5 × 4.
        /// lex, ley: cách tường (xem ChiaBuoc), số âm = mặc định. theoHang: mỗi hàng chia lại theo bề rộng trống thật của hàng đó
        /// (giao đường ngang của hàng với đa giác, phòng chữ L có WC): số thiết bị như lưới chung (hàng rộng bằng vùng ra đúng lưới chung),
        /// thêm thiết bị khi ô của hàng vượt dt; với "buoc" ô của hàng có thể vượt dt: k.O = ô lớn nhất của các hàng để nơi gọi báo lỗi.
        /// Sau lưới, PhuKin thêm thiết bị cho tới khi MỌI điểm của vùng (góc nhọn, cạnh xiên, phần lõm) nằm trong ô phủ của một thiết bị.
        /// tuongMax > 0: cách tường tối đa theo tiêu chuẩn; cachMin > 0: mọi thiết bị trong vùng, cách mọi cạnh ít nhất bấy nhiêu (đơn vị bản vẽ;
        /// DoiVao, trừ khe hẹp hơn 2 cachMin: đặt giữa khe).
        static LuoiRai LuoiVung(List<Point2d> vung, double sx, double sy, double bx, double by, double lex, double ley, double chan, double dt, double gocDo, bool theoHang,
            double tuongMax, double cachMin)
        {
            double a = gocDo * Math.PI / 180, c = Math.Cos(a), s = Math.Sin(a);
            List<Point2d> q = vung.Select(p => new Point2d(p.X * c + p.Y * s, -p.X * s + p.Y * c)).ToList();
            double x0 = q.Min(p => p.X), x1 = q.Max(p => p.X), y0 = q.Min(p => p.Y), y1 = q.Max(p => p.Y);
            if (x1 - x0 < 1e-6 || y1 - y0 < 1e-6) throw new System.Exception("vùng suy biến (rộng hoặc cao bằng 0)");
            double W = x1 - x0, H = y1 - y0;
            int nx0 = 0, ny0 = 0;
            if (dt > 0 && bx <= 0)
            {
                int ax = Math.Max(1, (int)Math.Ceiling(W / sx - 1e-9)), ay = Math.Max(1, (int)Math.Ceiling(H / sy - 1e-9));
                for (int nx = ax; nx <= ax + 5000; nx++) // quá 5000 thiết bị thì phép chặn bên dưới báo lỗi
                {
                    int ny = Math.Max(ay, (int)Math.Ceiling(W / nx * H / dt - 1e-9));
                    if (nx0 == 0 || (long)nx * ny < (long)nx0 * ny0
                        || ((long)nx * ny == (long)nx0 * ny0 && Math.Abs(W / nx - H / ny) < Math.Abs(W / nx0 - H / ny0) - 1e-9)) { nx0 = nx; ny0 = ny; }
                    if (ny == ay) break; // thêm cột nữa chỉ thêm thiết bị
                }
            }
            LuoiRai k = new LuoiRai();
            double ch = chan, lex0 = lex;
            // Vượt dt (chỉ khi rải theo bước tối đa): lần 2 bỏ làm tròn theo "chan" (dư dồn ra tường làm ô sát tường lớn),
            // lần 3 bỏ cả "le" (chia đều, luôn đạt dt nhờ số cột × số hàng đã chọn ở trên).
            for (int lan = 0; lan < 3; lan++)
            {
                if (lan > 0) ch = 0;
                if (lan == 2) { lex = -1; ley = -1; }
                ChiaBuoc(W, sx, bx, lex, ch, nx0, out k.Nx, out k.Bx, out k.Tx);
                ChiaBuoc(H, sy, by, ley, ch, ny0, out k.Ny, out k.By, out k.Ty);
                if (!(dt > 0 && bx <= 0 && ORai(k) > dt * (1 + 1e-9))) break;
            }
            k.O = ORai(k);
            double oHang = 0, sy0 = k.Ny > 1 ? Math.Max(k.By, 2 * k.Ty) : 2 * k.Ty; // Sy của mọi hàng
            Func<int, double, double, double> oCuaHang = (n, b, t) => (n > 1 ? Math.Max(b, 2 * t) : 2 * t) * sy0;
            // Chặn bước gõ nhầm đơn vị (vd 4 thay vì 4000 trong bản vẽ mm): không để AutoCAD treo vì chèn hàng triệu block.
            if ((long)k.Nx * k.Ny > 5000) throw new System.Exception("bước quá nhỏ so với vùng (" + k.Nx + " x " + k.Ny + " thiết bị): kiểm đơn vị của bước và của bản vẽ");
            List<Point2d> luoi = new List<Point2d>();
            for (int j = 0; j < k.Ny; j++)
            {
                double y = y0 + k.Ty + j * k.By;
                if (theoHang)
                    foreach (double[] kh in KhoangNgang(q, y))
                    {
                        int n; double b, t, w = kh[1] - kh[0];
                        if (bx > 0) ChiaBuoc(w, 0, bx, lex, 0, 0, out n, out b, out t);
                        else if (w >= W - 1e-6) { n = k.Nx; b = k.Bx; t = k.Tx; } // hàng rộng bằng vùng: đúng lưới chung (cả vị trí)
                        else
                            // Hàng hẹp: từ 1 thiết bị (ChiaBuoc tự đủ bước ≤ s), ba lần thử như vòng "lan" ở trên, vượt dt thì thêm một thiết bị.
                            for (int nMin = 1; ; nMin = n + 1)
                            {
                                ChiaBuoc(w, sx, 0, lex0, chan, nMin, out n, out b, out t);
                                if (dt <= 0 || oCuaHang(n, b, t) <= dt * (1 + 1e-9)) break;
                                ChiaBuoc(w, sx, 0, lex0, 0, nMin, out n, out b, out t);
                                if (oCuaHang(n, b, t) <= dt * (1 + 1e-9)) break;
                                ChiaBuoc(w, sx, 0, -1, 0, nMin, out n, out b, out t);
                                if (oCuaHang(n, b, t) <= dt * (1 + 1e-9) || n > 5000) break;
                            }
                        oHang = Math.Max(oHang, oCuaHang(n, b, t));
                        for (int i = 0; i < n; i++) luoi.Add(new Point2d(kh[0] + t + i * b, y));
                    }
                else
                    for (int i = 0; i < k.Nx; i++)
                    {
                        Point2d p = new Point2d(x0 + k.Tx + i * k.Bx, y);
                        if (TrongDaGiac(q, p)) luoi.Add(p);
                    }
            }

            // Ô phủ của một thiết bị: ±hx, ±hy quanh nó (cách tường tối đa theo từng phương). Theo "s": s / 2; theo "buoc": như lưới chữ nhật
            // (nửa bước hoặc cách tường của lưới, lấy số lớn); có "tuongMax" (trị số tiêu chuẩn) thì không vượt số đó.
            double hx = sx > 0 && bx <= 0 ? sx / 2 : Math.Max(k.Bx / 2, k.Tx), hy = sy > 0 && by <= 0 ? sy / 2 : Math.Max(k.By / 2, k.Ty);
            if (tuongMax > 0) { hx = Math.Min(hx, tuongMax); hy = Math.Min(hy, tuongMax); }
            if (oHang > 0) k.O = oHang;
            // Người dùng 19/11: lưới chia theo hộp bao (hoặc theo hàng) có thể nằm sát / ngoài cạnh xiên, khe hẹp: dời vào trong vùng
            // cách mọi cạnh >= cachMin (lệch hàng, cột cũng được) rồi mới phủ kín, chỗ hở do dời thì PhuKin thêm thiết bị.
            if (cachMin > 0) DoiVaoVung(q, luoi, cachMin);
            k.Ho = PhuKin(q, luoi, hx, hy, cachMin);
            foreach (Point2d p in luoi) k.Diem.Add(new Point3d(p.X * c - p.Y * s, p.X * s + p.Y * c, 0));
            return k;
        }

        /// Đoạn từ điểm bắt buộc ra tường mút dài Lw: thêm k điểm ít nhất để bước ≤ s và cách tường ≤ s / 2. Cách tường bằng nửa bước;
        /// có le (>= 0) thì dàn bước tới s cho cách tường gần le. Trả khoảng cách từng điểm thêm tới điểm bắt buộc.
        static List<double> DauMut(double Lw, double s, double le, out double b, out double t)
        {
            int k = Math.Max(0, (int)Math.Ceiling(Lw / s - 0.5 - 1e-9));
            b = k == 0 ? 0 : le >= 0 ? Math.Min(s, (Lw - Math.Min(le, s / 2)) / k) : Lw / (k + 0.5);
            t = Lw - k * b;
            List<double> kq = new List<double>();
            for (int i = 1; i <= k; i++) kq.Add(i * b);
            return kq;
        }

        /// Rải dọc đường tim hành lang. Đỉnh giữa của tim là điểm đặt bắt buộc (góc, chỗ giao hành lang; đỉnh thẳng hàng cũng được);
        /// tim khép kín (đỉnh cuối trùng đỉnh đầu) thì mọi đỉnh bắt buộc. Giữa hai điểm bắt buộc chia đều theo chiều dài dọc tim, bước ≤ s.
        /// Hai đầu tim hở là tường mút (DauMut). Không có điểm bắt buộc thì chia như rai theo s. tuong: cách hai tường mút (tim kín thì rỗng).
        static List<Point3d> RaiTim(List<Point3d> tim, double s, double le, out double bMax, out double[] tuong)
        {
            List<double> mot = new List<double> { 0 }; // chiều dài dọc tim tới từng đỉnh
            for (int i = 0; i + 1 < tim.Count; i++) mot.Add(mot[i] + tim[i].DistanceTo(tim[i + 1]));
            double L = mot[mot.Count - 1];
            if (L < 1e-6) throw new System.Exception("\"tim\" dài bằng 0");
            bool kin = tim.Count > 3 && tim[0].DistanceTo(tim[tim.Count - 1]) < 1e-6;
            List<double> bb = mot.Skip(kin ? 0 : 1).Take(mot.Count - (kin ? 1 : 2)).ToList();
            List<double> vt = new List<double>();
            bMax = 0;
            tuong = new double[0];
            if (bb.Count == 0)
            {
                int n; double b, t;
                ChiaBuoc(L, s, 0, le, 0, 0, out n, out b, out t);
                for (int i = 0; i < n; i++) vt.Add(t + i * b);
                bMax = n > 1 ? b : 0;
                tuong = new[] { t, t };
            }
            else
            {
                if (kin) bb.Add(bb[0] + L); // đi hết vòng về điểm đầu
                for (int i = 0; i + 1 < bb.Count; i++)
                {
                    double d = bb[i + 1] - bb[i];
                    if (d < 1e-6) continue; // đỉnh trùng nhau
                    int n = (int)Math.Ceiling(d / s - 1e-9);
                    for (int j = 0; j < n; j++) vt.Add(bb[i] + j * d / n);
                    bMax = Math.Max(bMax, d / n);
                }
                if (!kin)
                {
                    double dau = bb[0], cuoi = bb[bb.Count - 1], b0, b1, t0, t1;
                    vt.Add(cuoi);
                    vt.AddRange(DauMut(dau, s, le, out b0, out t0).Select(x => dau - x));
                    vt.AddRange(DauMut(L - cuoi, s, le, out b1, out t1).Select(x => cuoi + x));
                    bMax = Math.Max(bMax, Math.Max(b0, b1));
                    tuong = new[] { t0, t1 };
                }
            }
            List<Point3d> kq = new List<Point3d>();
            foreach (double x0 in vt.OrderBy(x => x))
            {
                double x = x0 >= L ? x0 - L : x0;
                int i = 0;
                while (i + 2 < mot.Count && x > mot[i + 1]) i++;
                double d = mot[i + 1] - mot[i];
                kq.Add(d < 1e-9 ? tim[i] : tim[i] + (tim[i + 1] - tim[i]) * ((x - mot[i]) / d));
            }
            return kq;
        }

        /// Đọc "s" hoặc "buoc": một số (dùng cho cả hai phương) hoặc [x, y]. Không có thì trả 0.
        static void HaiSo(Dictionary<string, object> o, string k, out double x, out double y)
        {
            object v = Lay(o, k);
            IList l = v as IList;
            x = 0; y = 0;
            if (v == null) return;
            if (l != null)
            {
                if (l.Count < 2) throw new System.Exception("\"" + k + "\" phải là một số hoặc [x,y]");
                x = D(l[0]); y = D(l[1]);
            }
            else { x = D(v); y = x; }
            if (x <= 0 || y <= 0) throw new System.Exception("\"" + k + "\" phải > 0");
        }

        /// Các vùng của thao tác: "vung" (một đa giác), "hop" (chữ nhật), hoặc mọi polyline kín trên layer "l"
        /// (mặc định PCCC_VUNG; có "hop" thì chỉ lấy polyline nằm trọn trong hộp).
        static List<List<Point2d>> VungOp(GoiSpec g, Dictionary<string, object> o)
        {
            List<List<Point2d>> kq = new List<List<Point2d>>();
            if (Lay(o, "vung") != null)
            {
                List<Point2d> d = DsP(g, Lay(o, "vung"), "vung").Select(p => new Point2d(p.X, p.Y)).ToList();
                if (d.Count < 3) throw new System.Exception("\"vung\" cần ít nhất 3 đỉnh");
                kq.Add(d);
                return kq;
            }
            double[] h = HopOp(g, o);
            if (h != null && Lay(o, "l") == null)
            {
                kq.Add(new List<Point2d> { new Point2d(h[0], h[1]), new Point2d(h[2], h[1]), new Point2d(h[2], h[3]), new Point2d(h[0], h[3]) });
                return kq;
            }
            foreach (Entity e in Loc(g, LayChu(o, "l", "PCCC_VUNG"), h))
            {
                Polyline pl = e as Polyline;
                if (KinPl(pl)) kq.Add(DinhPl(pl));
            }
            if (kq.Count == 0)
                throw new System.Exception("không có vùng để rải: đưa \"vung\" [[x,y],...] hoặc \"hop\" [x1,y1,x2,y2], hoặc vẽ polyline kín trên layer \"l\" (mặc định PCCC_VUNG)");
            return kq;
        }

        /// Trị số rải theo tiêu chuẩn của mã thiết bị (03_DU_LIEU/khoang_cach_rai.json ở gốc repo, build.bat chép cạnh DLL; đơn vị m):
        /// s, tuong, tuong_min, r, h_max, can_cu. Không có dòng cho mã thì trả null. h > 0 (chiều cao trần, m) vượt h_max của mọi dòng thì báo lỗi.
        static Dictionary<string, object> TieuChuanRai(string ma, double h)
        {
            string f = Path.Combine(ThuMuc, "khoang_cach_rai.json");
            if (!File.Exists(f)) return null;
            Dictionary<string, object> d;
            try { d = new JavaScriptSerializer().DeserializeObject(File.ReadAllText(f, Encoding.UTF8)) as Dictionary<string, object>; }
            catch (System.Exception ex) { throw new System.Exception("khoang_cach_rai.json sai cú pháp: " + ex.Message); }
            IList ds = d == null ? null : Lay(d, "thiet_bi") as IList;
            if (ds == null) return null;
            List<Dictionary<string, object>> cuaMa = ds.OfType<Dictionary<string, object>>()
                .Where(x => Lay(x, "ma") is IList && ((IList)Lay(x, "ma")).Cast<object>().Any(y => string.Equals(y as string, ma, StringComparison.OrdinalIgnoreCase))).ToList();
            if (cuaMa.Count == 0) return null;
            Dictionary<string, object> kq = cuaMa.FirstOrDefault(x => h <= 0 || Lay(x, "h_max") == null || h <= D(Lay(x, "h_max")) + 1e-9);
            if (kq == null)
                throw new System.Exception(string.Format(CultureInfo.InvariantCulture, "trần cao {0} m vượt chiều cao tối đa của {1} theo {2}: chọn loại thiết bị khác",
                    h, ma, LayChu(cuaMa[0], "can_cu", "khoang_cach_rai.json")));
            return kq;
        }

        static void OpRai(GoiSpec g, Dictionary<string, object> o)
        {
            string ma = LayChu(o, "ma", "");
            VatTu v = TimMa(g.Dm, ma);
            if (v == null || v.loai != "block") throw new System.Exception("mã thiết bị không có trong danh mục: " + ma);
            double sx, sy, bx, by;
            HaiSo(o, "s", out sx, out sy);
            HaiSo(o, "buoc", out bx, out by);
            double m = HeSoMet(g.Db), k = 0.001 / m, goc = LaySo(o, "goc", 0), dt = LaySo(o, "dt", 0) / (m * m);
            // Không có "s", "buoc": lấy bước tối đa theo tiêu chuẩn của mã thiết bị (và chiều cao trần "h"); "s" người gọi đưa vào thì dùng số đó.
            Dictionary<string, object> tc = TieuChuanRai(ma, LaySo(o, "h", 0));
            double tuongMax = 0, cachMin = CachMinRai(tc, m);
            // shortcut: thao tác spec chỉ rải điểm; tia chiếu (vẽ tia nối hai tường) mới có ở lệnh PCCC_RAI, khi AI cần thì chuyển TiaVung sang đây.
            if (sx <= 0 && bx <= 0 && tc != null && LayChu(tc, "kieu", "") == "tia")
                throw new System.Exception("mã " + ma + " là đầu báo tia chiếu: rải bằng lệnh PCCC_RAI (vẽ tia nối hai tường); thao tác rai chỉ rải điểm");
            if (sx <= 0 && bx <= 0 && tc != null) { sx = sy = D(Lay(tc, "s")) / m; tuongMax = LaySo(tc, "tuong", 0) / m; }
            if (sx <= 0 && bx <= 0)
                throw new System.Exception("rai cần \"s\" (bước tối đa) hoặc \"buoc\" (bước đúng) lấy theo quy chuẩn áp dụng: mã " + ma + " chưa có trị số trong 03_DU_LIEU/khoang_cach_rai.json");
            bool ktCheo = "cheo".Equals(Lay(o, "kt") as string); // kt:"cheo" = chuỗi + dim chéo góc lồi; kt:true = theo trang Cài đặt
            bool kt = (ktCheo || (!(Lay(o, "kt") is string) && LayCo(o, "kt", false))) && goc == 0;
            // "le": một số hoặc [x, y], cách tường theo từng phương; không có = mặc định (-1).
            double lex = -1, ley = -1;
            if (Lay(o, "le") != null)
            {
                IList ll = Lay(o, "le") as IList;
                lex = D(ll != null ? ll[0] : Lay(o, "le"));
                ley = ll != null && ll.Count > 1 ? D(ll[1]) : lex;
                if (lex < 0 || ley < 0) throw new System.Exception("\"le\" phải >= 0");
            }
            g.DiemRai = new List<List<Point3d>>();
            g.VungRai = new List<List<Point2d>>();
            if (Lay(o, "tim") != null)
            {
                // Hành lang: rải dọc đường tim, đỉnh giữa là điểm bắt buộc (RaiTim). Không xét dt (plugin không biết bề rộng hành lang).
                if (sx <= 0) throw new System.Exception("rai theo \"tim\" cần \"s\" (bước tối đa)");
                List<Point3d> tim = DsP(g, Lay(o, "tim"), "tim");
                if (tim.Count < 2) throw new System.Exception("\"tim\" cần ít nhất 2 điểm");
                double bMax;
                double[] tuong;
                List<Point3d> ps = RaiTim(tim, sx, lex, out bMax, out tuong);
                if (ps.Count > 5000) throw new System.Exception("bước quá nhỏ so với chiều dài tim (" + ps.Count + " thiết bị): kiểm đơn vị");
                foreach (Point3d p in ps)
                {
                    ChenThietBi(g.Db, g.Tr, v, p, k, 0);
                    g.Ve++;
                    MoHop(g, new Extents3d(p, p));
                }
                g.DiemRai.Add(ps);
                g.VungRai.Add(null); // không có đa giác: "qua" của tuyen sau nhận mọi điểm
                if (g.KqRai.Count < DOC_TOI_DA)
                    g.KqRai.Add(new Dictionary<string, object> { { "n", ps.Count }, { "bx", Lt(bMax) }, { "tuong", tuong.Select(x => Lt(x)).ToArray() } });
                return;
            }
            // Tính lưới MỌI vùng trước, đạt hết mới chèn: vùng sau lỗi thì không để lại thiết bị của vùng trước (gửi lại thao tác không rải trùng).
            List<List<Point2d>> vung = VungOp(g, o);
            List<LuoiRai> luoi = new List<LuoiRai>();
            foreach (List<Point2d> d in vung)
            {
                Func<List<Point2d>, LuoiRai> luoiMot = x => LuoiVung(x, sx, sy, bx, by, lex, ley, LaySo(o, "chan", 0.05 / m), dt, goc, LayCo(o, "theo_hang", false), tuongMax, cachMin);
                LuoiRai r = goc == 0 ? LuoiTach(d, m, luoiMot) : luoiMot(d); // vùng chữ L / T: từng mảnh chữ nhật (người dùng 25/11)
                // Chỉ xảy ra khi rải theo "buoc": bước đúng là của người gọi, plugin không tự sửa.
                if (dt > 0 && r.O > dt * (1 + 1e-9))
                    throw new System.Exception(string.Format(CultureInfo.InvariantCulture,
                        "vùng {4}: \"buoc\" {0} x {1} cho diện tích bảo vệ một thiết bị {2} m2, vượt \"dt\" {3} m2: giảm \"buoc\", hoặc dùng \"s\" để plugin tự chia; chưa rải vùng nào",
                        Lt(r.Bx), Lt(r.By), Math.Round(r.O * m * m, 2), LaySo(o, "dt", 0), luoi.Count + 1));
                luoi.Add(r);
            }
            for (int j = 0; j < vung.Count; j++)
            {
                List<Point2d> d = vung[j];
                LuoiRai r = luoi[j];
                foreach (Point3d p in r.Diem)
                {
                    ChenThietBi(g.Db, g.Tr, v, p, k, 0);
                    g.Ve++;
                    MoHop(g, new Extents3d(p, p));
                }
                g.DiemRai.Add(r.Diem);
                g.VungRai.Add(d);
                if (kt && r.Diem.Count > 0) KtRai(g, d, r, ktCheo);
                if (g.KqRai.Count < DOC_TOI_DA)
                    g.KqRai.Add(new Dictionary<string, object> { { "n", r.Diem.Count }, { "nx", r.Nx }, { "ny", r.Ny }, { "bx", Lt(r.Bx) }, { "by", Lt(r.By) },
                        { "tuong", new double[] { Lt(r.Tx), Lt(r.Ty) } }, { "m2", Math.Round(r.O * m * m, 2) }, { "ho", r.Ho },
                        { "cheo", Lt(CheoGoc(d, r.Diem).Select(c => c[0].GetDistanceTo(c[1])).DefaultIfEmpty(0).Max()) } });
                if (r.Manh != null && g.KqRai.Count > 0 && g.KqRai.Count <= DOC_TOI_DA) ((Dictionary<string, object>)g.KqRai[g.KqRai.Count - 1])["manh"] = r.Manh.Count;
            }
        }

        // ---------- Đi tuyến ----------

        /// PCCC_RAI: hỏi mã thiết bị (và trị số khi mã chưa có tiêu chuẩn), quét chọn mặt bằng, Enter = mọi phòng hoặc bấm điểm từng phòng; vẽ vùng rồi rải.
        /// Tóm tắt lượt PCCC_RAI gõ tay gần nhất (phân hệ Bố trí BC của palette hiện sau khi lệnh xong); "" = lượt đó không rải.
        public static string KetQuaRaiCuoi = "";

        static void RaiTay()
        {
            KetQuaRaiCuoi = "";
            Document doc = AcApp.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;
            KiemDonVi(doc); // trước mọi phép đổi m -> đơn vị bản vẽ
            FileDanhMuc dm = DocDanhMuc();
            VatTu v = HoiMa(ed, dm, "block", "Mã thiết bị cần rải");
            if (v == null) return;
            double m = HeSoMet(db), s, k = 0.001 / m, dt = 0, le = -1, tuongMax = 0, cachMin = CachMinRai(null, m);
            bool xem = false, tia = false, khoangDam = false;
            // Người dùng chốt tối 16/11: độ sâu dầm d = h dầm TRỪ chiều dày sàn (phần nhô dưới sàn). Từ khoá DaySan ở dấu nhắc trần / bước S.
            double daySan = 0;
            Func<bool> hoiSan = () =>
            {
                PromptDoubleOptions o = new PromptDoubleOptions("\nChiều dày sàn (mm), độ sâu dầm d = h dầm - dày sàn: ");
                o.AllowNegative = false; o.AllowZero = true;
                PromptDoubleResult rs = ed.GetDouble(o);
                if (rs.Status != PromptStatus.OK) return false;
                daySan = rs.Value / 1000;
                return true;
            };
            double hTran = 0;
            int loaiTran = 0; // LOAI_TRAN: 0 có trần (trần phẳng), 1 không trần (mặt bằng dầm, chia khoang), 2 vừa có vừa không (vùng + dầm vẽ tay), 3 mái kèo (khoang theo hàng cột)
            Dictionary<string, object> tc = TieuChuanRai(v.ma, 0);
            if (tc != null)
            {
                // Mã có trị số tiêu chuẩn (03_DU_LIEU/khoang_cach_rai.json): không hỏi khoảng cách hay m2, chỉ hỏi chiều cao trần để chọn dòng.
                // Ý lấy từ addin mẫu (14/11), từ khoá ở cùng dấu nhắc: HeSo = mức tối ưu k 50–100 % (bước và cách tường tối đa nhân k, k < 100 % là rải dày hơn);
                // Dam = trần có dầm sâu > 0,3 m (dòng có "dam" thì bước nhân he_so); XemTruoc = chỉ đếm, không vẽ.
                int heSo = 100;
                PromptDoubleResult hcr;
                while (true)
                {
                    string them = (heSo < 100 ? ", k = " + heSo + " %" : "") + (loaiTran == 1 ? ", không trần" : loaiTran == 2 ? ", vừa có vừa không trần" : loaiTran == 3 ? ", mái kèo" : "")
                        + (daySan > 0 ? ", sàn " + Math.Round(daySan * 1000) + " mm" : "") + (xem ? ", xem trước" : "");
                    PromptDoubleOptions hc = new PromptDoubleOptions("\nChiều cao trần (m), Enter = không xét" + (them != "" ? " (" + them.Substring(2) + ")" : ""));
                    hc.Keywords.Add("HeSo"); // AutoCAD tự nối " [HeSo/LoaiTran/DaySan/XemTruoc]: "
                    hc.Keywords.Add("LoaiTran");
                    hc.Keywords.Add("DaySan");
                    hc.Keywords.Add("XemTruoc");
                    hc.Keywords.Add("Dam", "Dam", "Dam", false, true); // từ khoá cũ (palette, kịch bản cũ) = LoaiTran KhongTran, không hiện
                    hc.AllowNegative = false; hc.AllowZero = false; hc.AllowNone = true;
                    hcr = ed.GetDouble(hc);
                    if (hcr.Status != PromptStatus.Keyword) break;
                    if (hcr.StringResult == "Dam") { loaiTran = 1; continue; }
                    if (hcr.StringResult == "LoaiTran")
                    {
                        // Người dùng 17/11: chọn 1 trong 3 trường hợp trần trước khi rải.
                        PromptKeywordOptions lt = new PromptKeywordOptions("\nLoại trần: CoTran = rải như trần phẳng; KhongTran = đọc mặt bằng dầm, chia khoang (5.9.1.1.6);"
                            + " HonHop = vừa có vừa không trần (vùng PCCC_VUNG và line dầm vẽ tay); MaiKeo = xưởng mái kèo, khoang giữa hai hàng vì kèo (tim hàng cột)");
                        lt.Keywords.Add("CoTran"); lt.Keywords.Add("KhongTran"); lt.Keywords.Add("HonHop"); lt.Keywords.Add("MaiKeo");
                        lt.Keywords.Default = LOAI_TRAN[loaiTran];
                        lt.AllowNone = true;
                        PromptResult lr = ed.GetKeywords(lt);
                        if (lr.Status == PromptStatus.Cancel) return;
                        if (Array.IndexOf(LOAI_TRAN, lr.StringResult) >= 0) loaiTran = Array.IndexOf(LOAI_TRAN, lr.StringResult);
                        continue;
                    }
                    if (hcr.StringResult == "XemTruoc") { xem = true; continue; }
                    if (hcr.StringResult == "DaySan") { if (!hoiSan()) return; continue; }
                    PromptIntegerOptions ko = new PromptIntegerOptions("\nMức tối ưu k (%) so với khoảng cách tối đa của tiêu chuẩn, 50–100: ");
                    ko.LowerLimit = 50; ko.UpperLimit = 100; ko.DefaultValue = heSo; ko.UseDefaultValue = true;
                    PromptIntegerResult kr = ed.GetInteger(ko);
                    if (kr.Status != PromptStatus.OK) return;
                    heSo = kr.Value;
                }
                if (hcr.Status != PromptStatus.OK && hcr.Status != PromptStatus.None) return;
                if (hcr.Status == PromptStatus.OK) tc = TieuChuanRai(v.ma, hcr.Value);
                s = D(Lay(tc, "s")) * heSo / 100.0 / m;
                tuongMax = LaySo(tc, "tuong", 0) * heSo / 100.0 / m;
                cachMin = CachMinRai(tc, m);
                tia = LayChu(tc, "kieu", "") == "tia";
                ed.WriteMessage("\n[TKPCCC] " + v.ma + " theo " + LayChu(tc, "can_cu", "") + ": " + (tia ? "khoảng cách giữa các tia " : "bước tối đa ")
                    + Lt(s * m).ToString(CultureInfo.InvariantCulture) + " m, cách tường tối đa " + Lt(tuongMax * m).ToString(CultureInfo.InvariantCulture) + " m"
                    + (heSo < 100 ? " (k = " + heSo + " %)" : "") + ".");
                if (loaiTran > 0)
                {
                    Dictionary<string, object> dam = Lay(tc, "dam") as Dictionary<string, object>;
                    if (dam != null)
                    {
                        s *= LaySo(dam, "he_so", 1);
                        ed.WriteMessage("\n[TKPCCC] Trần có dầm sâu > 0,3 m, theo " + LayChu(dam, "can_cu", "") + ": bước tối đa còn "
                            + Lt(s * m).ToString(CultureInfo.InvariantCulture) + " m.");
                    }
                    else if (loaiTran == 3)
                    {
                        // L2 (người dùng duyệt 20/11, xưởng Quyết Thắng): mái kèo, xà gồ không chia khoang; khoang = giữa hai hàng vì kèo (DuongViKeo).
                        ed.WriteMessage("\n[TKPCCC] Mái kèo: khoang = giữa hai hàng vì kèo (tim hàng cột nhận theo hình), chạy hết bề rộng xưởng tới tường; không chia theo xà gồ."
                            + " Chỉ chia phòng ≥ " + VI_KEO_M2_MIN.ToString(CultureInfo.InvariantCulture) + " m²; phòng nhỏ, văn phòng giữ nguyên.");
                        khoangDam = true;
                        hTran = hcr.Status == PromptStatus.OK ? hcr.Value : 0;
                    }
                    else
                    {
                        ed.WriteMessage("\n[TKPCCC] " + v.ma + ": tiêu chuẩn không giảm bước khi có dầm. Đầu báo khói kiểu điểm xét từng ô giữa dầm theo TCVN 7568-14:2025 5.9.1.1.6"
                            + " (02_DIEU_KHOAN/07_BAO_CHAY/MOD_dau_bao_khoi_khoang_dam.md): bấm nét dầm ở bước nét DẦM mẫu; độ sâu dầm lấy từ ký hiệu b x h (hỏi nếu không có), dầm ≤ 0,3 m bỏ;"
                            + " khoang ≥ ngưỡng (9 m² trần ≥ 4 m, 4 m² trần 2–4 m) rải từng khoang, khoang nhỏ hơn rải chung như trần phẳng (lắp ở mặt dưới dầm).");
                        khoangDam = true;
                        hTran = hcr.Status == PromptStatus.OK ? hcr.Value : 0;
                    }
                }
            }
            else
            {
                // Mã chưa có trị số tiêu chuẩn (sprinkler...): người thiết kế nhập theo TCVN đang áp dụng.
                // Dam (KOCHAM 16/11: trần hở, dầm > 300 mm thì đầu phun bố trí trong từng khoang dầm): chia vùng theo dầm sâu như đầu báo, rải từng khoang.
                PromptDoubleOptions so = new PromptDoubleOptions("\nBước tối đa S giữa hai thiết bị (m) theo tiêu chuẩn áp dụng" + (khoangDam ? " (có dầm)" : ""));
                so.Keywords.Add("Dam"); // AutoCAD tự nối " [Dam/DaySan]: "
                so.Keywords.Add("DaySan");
                so.AllowNegative = false; so.AllowZero = false;
                PromptDoubleResult sr;
                while (true)
                {
                    sr = ed.GetDouble(so);
                    if (sr.Status != PromptStatus.Keyword) break;
                    if (sr.StringResult == "DaySan") { if (!hoiSan()) return; continue; }
                    khoangDam = true;
                    so.Message = "\nBước tối đa S giữa hai thiết bị (m) theo tiêu chuẩn áp dụng (có dầm)";
                }
                if (sr.Status != PromptStatus.OK) return;
                s = sr.Value / m;
                PromptDoubleOptions dtHoi = new PromptDoubleOptions("\nDiện tích bảo vệ tối đa của một thiết bị (m2) theo tiêu chuẩn áp dụng, Enter = không xét: ");
                dtHoi.AllowNegative = false; dtHoi.AllowZero = false; dtHoi.AllowNone = true;
                PromptDoubleResult dr = ed.GetDouble(dtHoi);
                if (dr.Status != PromptStatus.OK && dr.Status != PromptStatus.None) return;
                dt = dr.Status == PromptStatus.OK ? dr.Value / (m * m) : 0;
                // Cùng nghĩa "le", "theo_hang" của thao tác spec rai (xem ChiaBuoc, LuoiVung).
                PromptDoubleOptions leHoi = new PromptDoubleOptions("\nKhoảng cách từ thiết bị tới tường (m), Enter = mặc định (chia đều, tối đa S/2): ");
                leHoi.AllowNegative = false; leHoi.AllowNone = true;
                PromptDoubleResult lr = ed.GetDouble(leHoi);
                if (lr.Status != PromptStatus.OK && lr.Status != PromptStatus.None) return;
                le = lr.Status == PromptStatus.OK ? lr.Value / m : -1;
            }
            PromptKeywordOptions hHoi = new PromptKeywordOptions("\nMỗi hàng chia lại theo bề rộng trống thật của hàng (phòng không chữ nhật)?");
            hHoi.Keywords.Add("Co"); hHoi.Keywords.Add("Khong");
            hHoi.Keywords.Default = "Khong";
            hHoi.AllowNone = true;
            PromptResult hr = ed.GetKeywords(hHoi);
            if (hr.Status == PromptStatus.Cancel) return;
            bool theoHang = hr.StringResult == "Co";

            bool coBlock;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                coBlock = ((BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead)).Has(v.ma);
                tr.Commit();
            }
            if (!coBlock) NapVaoBanVe(doc, dm);

            // Quét chọn mặt bằng (người dùng dặn 14/11): bấm điểm từng dò cả bản vẽ rồi TraceBoundary làm treo AutoCAD;
            // nay chỉ dựng phòng từ các đối tượng đã chọn, một lần (NhanVung.cs MatChon), vẽ vùng lên PCCC_VUNG rồi rải.
            PromptSelectionOptions qc = new PromptSelectionOptions();
            qc.MessageForAdding = "\nQuét chọn mặt bằng cần rải (tường các phòng; polyline kín layer " + LAYER_VUNG + " có sẵn thì dùng luôn): ";
            PromptSelectionResult qr = ed.GetSelection(qc);
            if (qr.Status != PromptStatus.OK) return;
            // Nét phân vùng mẫu (người dùng 14/11: cả tờ là một block, vùng kẻ bằng nét xanh lá, đỏ): bấm được nét nằm trong block.
            HashSet<string> mau = new HashSet<string>();
            while (true)
            {
                PromptNestedEntityOptions no = new PromptNestedEntityOptions(mau.Count == 0
                    ? "\nBấm một nét phân vùng mẫu (mỗi màu một lần), Enter = tự nhận từ tường: " : "\nBấm nét phân vùng mẫu màu khác, Enter = xong: ");
                no.AllowNone = true;
                PromptNestedEntityResult nr = ed.GetNestedEntity(no);
                if (nr.Status == PromptStatus.None) break;
                if (nr.Status != PromptStatus.OK) return;
                string kh = KhoaNetMau(db, nr);
                if (mau.Add(kh)) ed.WriteMessage("\n[TKPCCC] Nét phân vùng mẫu: layer " + kh.Replace("|", ", màu ") + ".");
            }
            // Dầm (người dùng bổ sung 15/11: tiêu chuẩn xét độ sâu dầm d cùng chiều cao trần h, 5.9.1.1.6): d lấy từ ký hiệu "b x h" trên mặt bằng dầm
            // (vd B1 (400x600)), không có thì hỏi một cỡ cho cả mặt bằng hoặc TungDam (chọn line dọc dầm, nhập cỡ từng nhóm). d ≤ 0,3 m: bỏ nét dầm (trần phẳng, khoản a).
            HashSet<string> damMau = null;
            Func<Point2d[], bool> giuDam = null;
            List<Point2d[]> damThem = null;
            HashSet<ObjectId> idDam = new HashSet<ObjectId>();
            List<KeyValuePair<Point2d[], double>> suaDam = new List<KeyValuePair<Point2d[], double>>(); // đoạn dầm -> d (m) người dùng nhập, thắng nhãn
            List<Point2d[]> timTay = new List<Point2d[]>(); // tim dầm vẽ tay (TungDam) không dời được ra mép (thiếu b)
            ObjectId khoiDam = ObjectId.Null; // block ngoài cùng chứa nét dầm mẫu đầu tiên (mặt bằng dầm chép đè)
            if (khoangDam)
            {
                damMau = new HashSet<string>();
                // HonHop (người dùng 17/11): vùng có trần / không trần và dầm đều vẽ tay; không đọc mặt bằng dầm, chọn thẳng line dầm (TungDam).
                // MaiKeo (20/11): không hỏi dầm, khoang theo hàng vì kèo.
                while (loaiTran < 2)
                {
                    PromptNestedEntityOptions no = new PromptNestedEntityOptions(damMau.Count == 0
                        ? "\nBấm một nét DẦM mẫu (layer + màu của dầm), Enter = không có: " : "\nBấm nét dầm màu khác, Enter = xong; nhãn dầm ghi sai cỡ thì SuaDam");
                    no.AllowNone = true;
                    if (damMau.Count > 0) no.Keywords.Add("SuaDam"); // AutoCAD tự nối " [SuaDam]: "
                    PromptNestedEntityResult nr = ed.GetNestedEntity(no);
                    if (nr.Status == PromptStatus.None) break;
                    if (nr.Status == PromptStatus.Keyword)
                    {
                        // Người dùng 17/11: KOCHAM ghi "DS 1400X250" mà dầm thật rộng 1400 cao 700. Bấm từng nét dầm, nhập cỡ thật b x h; cỡ này thắng nhãn.
                        while (true)
                        {
                            PromptNestedEntityOptions so = new PromptNestedEntityOptions("\nBấm nét dầm cần sửa cỡ, Enter = xong: ");
                            so.AllowNone = true;
                            PromptNestedEntityResult sr = ed.GetNestedEntity(so);
                            if (sr.Status == PromptStatus.None) break;
                            if (sr.Status != PromptStatus.OK) return;
                            List<Point2d[]> doan = DoanNet(sr.ObjectId.Database, sr.ObjectId);
                            if (doan.Count == 0) { ed.WriteMessage("\nKhông phải line / polyline: bấm lại nét dầm."); continue; }
                            PromptResult cr = ed.GetString("\nKích thước THẬT b x h (rộng x cao, mm) của dầm này, vd 1400x700: ");
                            if (cr.Status != PromptStatus.OK) return;
                            double d = DocBxh(cr.StringResult, true);
                            if (d <= 0) { ed.WriteMessage("\nKhông đọc được b x h \"" + cr.StringResult + "\": bấm lại."); continue; }
                            foreach (Point2d[] dn in doan)
                            {
                                Point3d a = new Point3d(dn[0].X, dn[0].Y, 0).TransformBy(sr.Transform), b = new Point3d(dn[1].X, dn[1].Y, 0).TransformBy(sr.Transform);
                                suaDam.Add(new KeyValuePair<Point2d[], double>(new[] { new Point2d(a.X, a.Y), new Point2d(b.X, b.Y) }, d));
                            }
                            ed.WriteMessage("\n[TKPCCC] sua_dam d=" + d.ToString(CultureInfo.InvariantCulture) + " m doan=" + doan.Count
                                + (d - daySan > 0.3 + 1e-9 ? " (sâu)" : " (≤ 0,3 m: trần phẳng)"));
                        }
                        continue;
                    }
                    if (nr.Status != PromptStatus.OK) return;
                    string kh = KhoaNetMau(db, nr);
                    ObjectId[] chua = nr.GetContainers();
                    if (khoiDam.IsNull && chua.Length > 0) khoiDam = chua[chua.Length - 1];
                    mau.Remove(kh); // nét dầm không dựng làm tường
                    if (damMau.Add(kh)) ed.WriteMessage("\n[TKPCCC] Nét dầm mẫu: layer " + kh.Replace("|", ", màu ") + ".");
                }
                List<KeyValuePair<Point2d, Vector2d>> nhan = loaiTran >= 2 ? new List<KeyValuePair<Point2d, Vector2d>>() : NhanDam(qr.Value.GetObjectIds());
                double sauChung = -1;
                if (damMau.Count == 0 || nhan.Count == 0)
                {
                    string tl = loaiTran == 3 ? "" : "TungDam";
                    if (loaiTran < 2)
                    {
                        PromptStringOptions so = new PromptStringOptions(damMau.Count > 0
                            ? "\nKhông thấy ký hiệu dầm b x h (vd B1 (400x600)) trên mặt bằng. Nhập b x h (mm) cho cả mặt bằng, hoặc TungDam = chọn line dọc dầm rồi nhập từng cỡ: "
                            : "\nChưa có nét dầm mẫu. TungDam = chọn line dọc dầm bạn vẽ rồi nhập b x h; Enter = bỏ qua dầm (rải như trần phẳng): ");
                        so.AllowSpaces = false;
                        PromptResult sr = ed.GetString(so);
                        if (sr.Status != PromptStatus.OK) return;
                        tl = sr.StringResult.Trim();
                    }
                    if (tl.Equals("TungDam", StringComparison.OrdinalIgnoreCase) || tl.Equals("T", StringComparison.OrdinalIgnoreCase))
                    {
                        damThem = new List<Point2d[]>();
                        while (true)
                        {
                            PromptSelectionOptions lo = new PromptSelectionOptions();
                            lo.MessageForAdding = "\nChọn line / polyline dọc dầm cùng một cỡ (tim dầm, hoặc hai mép), Enter = xong: ";
                            PromptSelectionResult lr = ed.GetSelection(lo, new SelectionFilter(new[] { new TypedValue((int)DxfCode.Start, "LINE,LWPOLYLINE") }));
                            if (lr.Status != PromptStatus.OK) break;
                            PromptResult cr = ed.GetString("\nKích thước dầm b x h (mm) của các line này, vd 400x600: ");
                            if (cr.Status != PromptStatus.OK) return;
                            double d = DocBxh(cr.StringResult, true), rong = DocRong(cr.StringResult);
                            if (d <= 0) { ed.WriteMessage("\nKhông đọc được b x h \"" + cr.StringResult + "\": chọn lại."); continue; }
                            int truoc = damThem.Count;
                            List<Point2d[]> nhom = new List<Point2d[]>();
                            foreach (ObjectId id in lr.Value.GetObjectIds())
                            {
                                idDam.Add(id);
                                nhom.AddRange(DoanNet(db, id));
                            }
                            // Người dùng 17/11: mép dầm là tường của khoang. Line vẽ ở tim dầm thì dời ra hai mép (± b/2); hai line mép cách nhau ≈ b thì giữ.
                            if (d - daySan > 0.3 + 1e-9) damThem.AddRange(MepDam(nhom, rong / m, timTay));
                            ed.WriteMessage("\n[TKPCCC] tung_dam d=" + d.ToString(CultureInfo.InvariantCulture) + " m: " + lr.Value.Count + " nét"
                                + (d - daySan > 0.3 + 1e-9 ? " (sâu, chia khoang " + (damThem.Count - truoc) + " đoạn)" : " (≤ 0,3 m: bỏ, trần phẳng)"));
                        }
                    }
                    else if (tl != "")
                    {
                        sauChung = DocBxh(tl, true);
                        if (sauChung <= 0) { ed.WriteMessage("\nKhông đọc được b x h \"" + tl + "\"."); return; }
                    }
                }
                giuDam = dn =>
                {
                    double gan = double.MaxValue, d = -1;
                    // KOCHAM 16/11: nhãn gần nhất hay là nhãn dầm cắt ngang (dầm sâu bị coi nông, 2 cặp khoang gộp): ưu tiên nhãn chữ song song nét dầm.
                    Vector2d u = (dn[1] - dn[0]).GetNormal();
                    Point2d g = dn[0] + (dn[1] - dn[0]) * 0.5;
                    foreach (KeyValuePair<Point2d[], double> kv in suaDam) // cỡ nhập tay (SuaDam): đoạn song song, trung điểm nằm trên nét đã bấm
                    {
                        Vector2d w = kv.Key[1] - kv.Key[0];
                        if (w.Length > 1e-9 && Math.Abs(u.X * w.Y - u.Y * w.X) <= 0.02 * w.Length && CachDoan(kv.Key, g) <= 0.05 / m)
                            return kv.Value - daySan > 0.3 + 1e-9;
                    }
                    foreach (bool songSong in new[] { true, false })
                    {
                        foreach (KeyValuePair<Point2d, Vector2d> kv in nhan)
                        {
                            if (songSong && Math.Abs(u.X * kv.Value.Y - u.Y * kv.Value.X) > 0.26 * kv.Value.Length) continue;
                            double c = CachDoan(dn, kv.Key);
                            if (c < gan) { gan = c; d = kv.Value.Length; }
                        }
                        if (gan <= 10 / m) break;
                    }
                    if (gan <= 10 / m) return d - daySan > 0.3 + 1e-9;               // ký hiệu gần nhất trong 10 m
                    return sauChung > 0 ? sauChung - daySan > 0.3 + 1e-9 : true;     // không ký hiệu gần, không cỡ chung: coi là dầm sâu (chia khoang, rải dày hơn)
                };
                ed.WriteMessage("\n[TKPCCC] dam ky_hieu=" + nhan.Count + (daySan > 0 ? " day_san=" + Math.Round(daySan * 1000) : "") + (sauChung > 0 ? " d_chung=" + sauChung.ToString(CultureInfo.InvariantCulture) : "")
                    + (damThem != null ? " tung_dam=" + damThem.Count : "") + (suaDam.Count > 0 ? " sua_dam=" + suaDam.Count : ""));
            }
            List<List<Point2d>> vung, mat, oX, gach;
            ObjectId[] conLai;
            List<List<Point2d>> loaiTru = TachLoaiTru(db, qr.Value.GetObjectIds(), out conLai);
            if (loaiTru.Count > 0) ed.WriteMessage("\n[TKPCCC] " + loaiTru.Count + " vùng loại trừ (polyline kín layer " + LAYER_LOAI_TRU + "): không rải trong đó.");
            List<List<Point2d>> ongGio = TachOngGio(db, conLai, out conLai);
            if (ongGio.Count > 0) ed.WriteMessage("\n[TKPCCC] " + ongGio.Count + " ống gió (polyline kín layer có ONG_GIO / DUCT): đầu rơi vào ống thì dịch ra ngoài biên.");
            if (idDam.Count > 0) conLai = conLai.Where(id => !idDam.Contains(id)).ToArray(); // line dầm chọn tay không làm tường
            int[] demDam;
            System.Diagnostics.Stopwatch dongHo = System.Diagnostics.Stopwatch.StartNew(); // người dùng 16/11: rải lâu, đứng máy: in thời gian từng pha
            Dictionary<List<Point2d>, List<Point2d>> khoangChia; // khoang -> vùng vẽ sẵn chứa nó
            List<Point2d> khuKhong;
            List<Point2d[]> damSau;
            // Khung quét chọn (W, C, WP, CP): chỉ dựng phòng trong khung. Có đối tượng chọn kiểu khác (bấm, L, !ss) thì không giới hạn.
            double[] hopChon = null;
            foreach (SelectedObject so in qr.Value)
            {
                CrossingOrWindowSelectedObject cw = so as CrossingOrWindowSelectedObject;
                if (cw == null) { hopChon = null; break; }
                foreach (PickPointDescriptor pd in cw.GetPickPoints())
                {
                    Point3d p = pd.PointOnLine;
                    if (hopChon == null) hopChon = new[] { p.X, p.Y, p.X, p.Y };
                    else hopChon = new[] { Math.Min(hopChon[0], p.X), Math.Min(hopChon[1], p.Y), Math.Max(hopChon[2], p.X), Math.Max(hopChon[3], p.Y) };
                }
            }
            if (hopChon != null && (hopChon[2] - hopChon[0] < 1e-9 || hopChon[3] - hopChon[1] < 1e-9)) hopChon = null;
            double[] khungTruc;
            int boTruc;
            NhanThem nhanThem; // cột, chữ khu không rải, chữ tên phòng (người dùng 20/11: phân vùng như AI)
            MatChon(db, conLai, mau.Count > 0 ? mau : null, damMau, giuDam, damThem, hopChon, khoiDam, out vung, out mat, out oX, out gach, out demDam, out khoangChia, out khuKhong, out damSau, out khungTruc, out boTruc, out nhanThem);
            if (nhanThem.Cot.Count > 0) ed.WriteMessage("\n[TKPCCC] cot=" + nhanThem.Cot.Count + " (polyline kín nhỏ cùng cỡ lặp thẳng hàng = cột: không khép khe tường từ nét cột).");
            if (khungTruc != null) ed.WriteMessage("\n[TKPCCC] khung_truc x=" + string.Join("..", new[] { khungTruc[0], khungTruc[2] }.Select(t => double.IsNaN(t) ? "?" : Math.Round(t).ToString(CultureInfo.InvariantCulture)))
                + " y=" + string.Join("..", new[] { khungTruc[1], khungTruc[3] }.Select(t => double.IsNaN(t) ? "?" : Math.Round(t).ToString(CultureInfo.InvariantCulture))));
            if (boTruc > 0) ed.WriteMessage("\n[TKPCCC] bo_net_truc=" + boTruc + " (nét liền trùng đường trục: không coi là tường).");
            List<double> soTang = nhanThem.TenTang.Select(t0 => t0.Value).Distinct().OrderBy(t0 => t0).ToList();
            if (soTang.Count >= 2) ed.WriteMessage("\n[TKPCCC] mat_bang_tang=" + string.Join(",", soTang.Select(t0 => t0.ToString(CultureInfo.InvariantCulture)))
                + " (quét nhiều tầng: tầng nhận theo tên mặt bằng; khung trục chỉ áp cho mặt bằng có trục; tầng ngay dưới ô X thông tầng lấy vùng như tầng trên).");
            if (loaiTran == 2 && vung.Count == 0)
            {
                ed.WriteMessage("\nVừa có vừa không trần: vẽ polyline kín layer " + LAYER_VUNG + " quanh từng vùng (vùng có trần không vẽ dầm) rồi chọn cả vùng lẫn line dầm.");
                return;
            }
            // Tim dầm (người dùng 17/11: đầu báo lắp ở mặt dưới dầm thì đặt đúng giữa bề rộng dầm): ghép từng cặp mép dầm, thêm tim vẽ tay.
            List<Point2d[]> timDam = khoangDam ? TimDam(damSau, m) : new List<Point2d[]>();
            timDam.AddRange(timTay);
            if (timDam.Count > 0) ed.WriteMessage("\n[TKPCCC] tim_dam=" + timDam.Count);
            if (khoangChia.Count > 0) ed.WriteMessage("\n[TKPCCC] chia_khoang=" + khoangChia.Count + " (vùng " + LAYER_VUNG + " vẽ sẵn chia theo dầm sâu; nét khoang vẽ trên layer " + LAYER_KHOANG + ")");
            ed.WriteMessage("\n[TKPCCC] thoi_gian dung_phong=" + dongHo.ElapsedMilliseconds + "ms doi_tuong=" + conLai.Length + " mat=" + mat.Count);
            if (khoangDam && demDam[0] + demDam[1] > 0)
                ed.WriteMessage("\n[TKPCCC] dam_sau=" + demDam[0] + " dam_nong=" + demDam[1] + " (dầm nông d ≤ 0,3 m bỏ, rải như trần phẳng: 5.9.1.1.6 a)");
            if (oX.Count > 0)
            {
                ed.WriteMessage("\n[TKPCCC] o_x=" + oX.Count + " (ô có dấu X cỡ giếng thang máy hoặc dải khe dài hẹp, block thang máy: không rải).");
                loaiTru.AddRange(oX);
            }
            if (nhanThem.ThongTang.Count > 0)
                ed.WriteMessage("\n[TKPCCC] thong_tang=" + nhanThem.ThongTang.Count + " m2=" + string.Join(",", nhanThem.ThongTang.Select(o =>
                    Math.Round(Math.Abs(DienTichDaGiac(o)) * m * m, 1).ToString(CultureInfo.InvariantCulture)
                    + "(" + Math.Round(o[0].GetDistanceTo(o[1]) * m, 1).ToString(CultureInfo.InvariantCulture) + "x" + Math.Round(o[1].GetDistanceTo(o[2]) * m, 1).ToString(CultureInfo.InvariantCulture) + ")@" + Math.Round(o.Average(q => q.X)).ToString(CultureInfo.InvariantCulture)
                    + "," + Math.Round(o.Average(q => q.Y)).ToString(CultureInfo.InvariantCulture))) + " (ô dấu X lớn = sảnh / lỗ thông tầng: VÙNG RẢI RIÊNG, đầu báo, đầu phun"
                    + " của vùng này lắp ở ĐỈNH sảnh thông tầng; hành lang, ô giáp sảnh là vùng riêng, lắp ở trần tầng đó).");
            if (gach.Count > 0) ed.WriteMessage("\n[TKPCCC] gach_cheo=" + gach.Count + " (hatch gạch chéo = công trình hiện hữu: Enter mọi phòng thì bỏ qua).");
            bool veVung = vung.Count == 0;
            HashSet<List<Point2d>> daCo = new HashSet<List<Point2d>>(); // vùng PCCC_VUNG vẽ sẵn dùng thay phòng tự dựng: không vẽ lại
            HashSet<List<Point2d>> daVe = new HashSet<List<Point2d>>(); // phòng đã vẽ (nhiều khoang dầm chung một phòng)
            HashSet<List<Point2d>> coLo = new HashSet<List<Point2d>>(); // vùng có lỗ nối ra viền bằng khe (ChieuThongTang): không làm gọn
            bool tatCa = false; // Enter = mọi phòng (không bấm điểm từng phòng)
            if (veVung)
            {
                if (mat.Count == 0)
                {
                    ed.WriteMessage("\nKhông dựng được phòng kín nào từ các " + (mau.Count > 0 ? "nét cùng layer, màu với nét mẫu" : "đối tượng đã chọn") + " (chưa chọn đủ, hoặc vùng hở khe rộng hơn "
                        + HO_CUA_M.ToString(CultureInfo.InvariantCulture) + " m: vẽ polyline kín layer " + LAYER_VUNG + " rồi chọn lại).");
                    return;
                }
                while (true)
                {
                    PromptPointOptions po = new PromptPointOptions(vung.Count == 0 ? "\nBấm điểm trong phòng cần rải, Enter = mọi phòng" : "\nBấm điểm trong phòng tiếp theo, Enter = xong");
                    if (vung.Count == 0) po.Keywords.Add("TatCa"); // AutoCAD tự nối " [TatCa]: "
                    po.AllowNone = true;
                    PromptPointResult pr = ed.GetPoint(po);
                    if (pr.Status == PromptStatus.Cancel) return;
                    if (pr.Status == PromptStatus.Keyword || (pr.Status == PromptStatus.None && vung.Count == 0))
                    {
                        List<List<Point2d>> pt = PhongThat(mat, m);
                        int gopSanh, gopNho, oCua;
                        pt = GopMatNho(mat, pt, nhanThem.Doan, khuKhong, nhanThem.VatDung, m, out gopSanh, out gopNho, out oCua, nhanThem.Cua, nhanThem.LanCan);
                        if (oCua > 0) ed.WriteMessage("\n[TKPCCC] o_nho_qua_cua=" + oCua + " (ô nhỏ chỉ thông với phòng qua ô cửa có cánh: vùng riêng, không gộp).");
                        if (gopSanh + gopNho > 0) ed.WriteMessage("\n[TKPCCC] gop_sanh=" + gopSanh + " gop_o_nho=" + gopNho
                            + " (sảnh nhỏ trước WC, ô giao, đoạn hành lang chỉ ngăn bằng khe hở: gộp vào phòng / hành lang kề).");
                        vung = pt.Where(f0 => !MatKhongRai(f0, oX, gach)).ToList();
                        if (pt.Count > vung.Count) ed.WriteMessage("\n[TKPCCC] bo_phong=" + (pt.Count - vung.Count) + " (ô X, gạch chéo).");
                        // Người dùng 17/11: ngoài đường trục ngoài cùng thì không phân vùng khi chưa được cho phép (bấm điểm trong phòng = cho phép).
                        if (khungTruc != null)
                        {
                            double nt = 0.3 / m;
                            Func<Point2d, bool> trongTruc = c => (double.IsNaN(khungTruc[0]) || (c.X >= khungTruc[0] - nt && c.X <= khungTruc[2] + nt))
                                && (double.IsNaN(khungTruc[1]) || (c.Y >= khungTruc[1] - nt && c.Y <= khungTruc[3] + nt));
                            int truoc = vung.Count;
                            // Người dùng 25/11 (Karaoke 123 tầng 1 + 2 quét một lần: khung trục chỉ nhận ở tầng 1, 16 phòng tầng 2 bị bỏ như ngoài trục): có ≥ 2 tên
                            // mặt bằng khác số tầng thì cụm phòng (hộp bao cách ≤ 2 m) không có phòng nào trong khung trục là mặt bằng tầng khác: không xét ngoài trục
                            // (cụm nhỏ = hình chi tiết vẫn qua CumChinh bên dưới).
                            HashSet<int> cumTruc = null;
                            int[] cumP = null;
                            if (nhanThem.TenTang.Select(t0 => t0.Value).Distinct().Count() >= 2)
                            {
                                cumP = CumPhong(vung, 2 / m);
                                cumTruc = new HashSet<int>(Enumerable.Range(0, vung.Count).Where(i => trongTruc(DiemTrongMat(vung[i]))).Select(i => cumP[i]));
                            }
                            // L8 (người dùng duyệt 20/11): phòng có chữ tên phòng ("VĂN PHÒNG"...) nằm ngoài trục vẫn giữ (2 văn phòng ở góc ngoài khối xưởng bị bỏ sót).
                            vung = vung.Where((f0, i) => trongTruc(DiemTrongMat(f0)) || nhanThem.TenPhong.Any(p => TrongDaGiac(f0, p)) || (cumTruc != null && !cumTruc.Contains(cumP[i]))).ToList();
                            if (vung.Count < truoc) ed.WriteMessage("\n[TKPCCC] bo_ngoai_truc=" + (truoc - vung.Count) + " (phòng ngoài đường trục ngoài cùng: không phân vùng; cần rải thì bấm điểm trong phòng đó).");
                        }
                        // Mọi phòng thật (kể cả WC, thang sắp bỏ) để xét "bên kia mép có phòng không" (hành lang bên); mặt ngoài trục (lam, mái đua) không tính.
                        List<List<Point2d>> phongDu = vung.ToList();
                        // Người dùng dặn 16/11: đầu báo không đặt trong lõi thang máy, buồng thang, WC (phòng ≤ 60 m² chứa dấu hiệu GomKhu).
                        // shortcut: nhận theo tên block, layer, chữ phòng; bản vẽ không có các dấu hiệu đó thì vẫn rải: bấm điểm từng phòng.
                        if (v.he == "BC")
                        {
                            // Người dùng 23/11 (trường học khung 6, WC học sinh 79–80 m²): phòng có CHỮ WC / VỆ SINH / TOILET bỏ tới 150 m² (chữ thắng ngưỡng 60 m²).
                            List<List<Point2d>> boKhu = vung.Where(f0 => (DienTichDaGiac(f0) * m * m <= 60 && khuKhong.Any(p => TrongDaGiac(f0, p)))
                                || (DienTichDaGiac(f0) * m * m <= 150 && nhanThem.KhuChu.Any(p => TrongDaGiac(f0, p)))).ToList();
                            List<List<Point2d>> wc = boKhu.Where(f0 => nhanThem.KhuWc.Any(p => TrongDaGiac(f0, p))).ToList();
                            vung = vung.Where(f0 => !boKhu.Contains(f0)).ToList();
                            if (boKhu.Count > 0) ed.WriteMessage("\n[TKPCCC] bo_khu_khong_bao=" + boKhu.Count + " m2="
                                + string.Join(",", boKhu.Select(f0 => Math.Round(DienTichDaGiac(f0) * m * m, 1).ToString(CultureInfo.InvariantCulture)))
                                + " (lõi thang máy, buồng thang, WC: không đặt đầu báo; cần rải thì bấm điểm trong phòng đó).");
                            // Người dùng 26/11 (TESTBCHAY_20_11: "vùng wc nhỏ trong phòng này, không được bao phòng này; các phòng có wc nhỏ cũng tương tự"):
                            // phòng nhỏ ≤ 5 m² (26/11 chiều hạ từ 8), không chữ tên, có ký hiệu cửa, mọi cửa của nó chỉ mở sang PHÒNG (không sang dải hành lang) = WC riêng trong phòng:
                            // không vùng, không đầu báo (vùng phòng chủ đã chừa nó ra vì nó có tường / ô cửa có cánh, GopMatNho không gộp).
                            // shortcut: kho nhỏ ≤ 5 m² không ghi chữ, cửa mở vào phòng cũng bị coi là WC: cần rải thì bấm điểm trong phòng đó.
                            Func<List<Point2d>, bool> laHanhLang = g0 =>
                            {
                                double a0 = Math.Abs(DienTichDaGiac(g0)) * m * m, p0 = 0;
                                for (int i = 0; i < g0.Count; i++) p0 += g0[i].GetDistanceTo(g0[(i + 1) % g0.Count]) * m;
                                double r0 = p0 > 0 ? 2 * a0 / p0 : 0;
                                // dải: rộng TB ≤ 2,5 m và dài ≥ 3 lần rộng (chữ nhật w x l: A / r0² = (w + l)² / (w l) ≥ 5,33); phòng 6 x 3,9 m (r0 2,36) không phải dải
                                return r0 > 0 && r0 <= 2.5 && a0 / (r0 * r0) >= 5.3;
                            };
                            // tâm hộp cửa cách mặt ≤ nửa cạnh hộp (bán kính cánh / 2) + 0,3 m (bề dày tường): cửa thuộc mặt đó
                            Func<List<Point2d>, Point2d[], bool> ganCua = (g0, h) => { Point2d c = new Point2d((h[0].X + h[1].X) / 2, (h[0].Y + h[1].Y) / 2);
                                return TrongDaGiac(g0, c) || CachVien(g0, c) <= Math.Max(h[1].X - h[0].X, h[1].Y - h[0].Y) / 2 + 0.3 / m; };
                            List<List<Point2d>> wcTrong = vung.Where(f0 =>
                            {
                                // Người dùng 26/11 chiều (ô 2,5 x 2,75 m = 6,9 m² trong phòng 8–9 TESTBCHAY: "phòng đó cũng khá to, có thể là kho nên phải bao vùng"):
                                // WC riêng chỉ tới 5 m² (WC thật ở đó 2,6–3,2 m²); ô lớn hơn coi là kho, vẫn có vùng và đầu báo.
                                if (Math.Abs(DienTichDaGiac(f0)) * m * m > 5 || nhanThem.TenChu.Any(p => TrongDaGiac(f0, p)) || nhanThem.TenPhong.Any(p => TrongDaGiac(f0, p))) return false;
                                List<Point2d[]> cuaF = nhanThem.Cua.Where(h => ganCua(f0, h)).ToList();
                                if (cuaF.Count == 0) return false;
                                // bên kia cửa = mặt khác GẦN tâm hộp cửa nhất (cửa sát góc: hành lang sau tường vuông góc cách xa hơn phòng chủ)
                                bool coPhong = false;
                                foreach (Point2d[] h in cuaF)
                                {
                                    List<Point2d> benKia = null;
                                    double ganNhat = double.MaxValue;
                                    foreach (List<Point2d> g0 in phongDu)
                                    {
                                        if (g0 == f0 || !ganCua(g0, h)) continue;
                                        Point2d c = new Point2d((h[0].X + h[1].X) / 2, (h[0].Y + h[1].Y) / 2);
                                        double kc = TrongDaGiac(g0, c) ? 0 : CachVien(g0, c);
                                        if (kc < ganNhat) { ganNhat = kc; benKia = g0; }
                                    }
                                    if (benKia == null) continue;
                                    if (laHanhLang(benKia)) return false;
                                    coPhong = true;
                                }
                                return coPhong;
                            }).ToList();
                            if (wcTrong.Count > 0)
                            {
                                vung = vung.Where(f0 => !wcTrong.Contains(f0)).ToList();
                                ed.WriteMessage("\n[TKPCCC] bo_wc_trong_phong=" + wcTrong.Count + " m2=" + string.Join(",", wcTrong.Select(f0 => Math.Round(Math.Abs(DienTichDaGiac(f0)) * m * m, 1).ToString(CultureInfo.InvariantCulture)))
                                    + " (phòng nhỏ không chữ, cửa chỉ mở vào phòng khác = WC riêng trong phòng: không đặt đầu báo; cần rải thì bấm điểm trong phòng đó).");
                            }
                            // Người dùng 26/11 chiều ("phòng rất nhỏ, hoặc ghi chữ WC, hoặc có thiết bị WC trong đó thì coi là WC"): chữ / block thiết bị đã bỏ ở
                            // bo_khu_khong_bao, rất nhỏ ở bo_wc_trong_phong; còn thiết bị WC vẽ nét nổ rời (bồn cầu, lavabo, tiểu: hình kín 0,25–0,9 m, GomVatDung)
                            // trong phòng ≤ 8 m², không chữ tên phòng: bỏ. shortcut: phòng nhỏ có đồ vật kín cỡ đó (ghế, tủ nhỏ) cũng bị bỏ: bấm điểm để rải.
                            List<List<Point2d>> wcTb = vung.Where(f0 => Math.Abs(DienTichDaGiac(f0)) * m * m <= 8 && !nhanThem.TenPhong.Any(p => TrongDaGiac(f0, p))
                                && nhanThem.VatDung.Any(p => TrongDaGiac(f0, p))).ToList();
                            if (wcTb.Count > 0)
                            {
                                vung = vung.Where(f0 => !wcTb.Contains(f0)).ToList();
                                ed.WriteMessage("\n[TKPCCC] bo_wc_thiet_bi=" + wcTb.Count + " m2=" + string.Join(",", wcTb.Select(f0 => Math.Round(Math.Abs(DienTichDaGiac(f0)) * m * m, 1).ToString(CultureInfo.InvariantCulture)))
                                    + " (phòng nhỏ có thiết bị vệ sinh vẽ nét rời = WC: không đặt đầu báo; cần rải thì bấm điểm trong phòng đó).");
                            }
                            // Người dùng 16/11 + 17/11 (vẽ lại vùng tầng 8 KOCHAM): sảnh thang máy không đặt đầu báo. Bản vẽ không ghi chữ "SẢNH": nhận là phòng
                            // ≤ 30 m² sát giếng thang (ô có dấu X, cách ≤ 0,6 m = bề dày vách). shortcut: ô X cũng là lỗ thông tầng, phòng nhỏ sát lỗ thông tầng bị bỏ theo.
                            Func<List<Point2d>, List<Point2d>, bool> keNhau = (a, b) => a.Any(p => CachVien(b, p) <= 0.6 / m) || b.Any(p => CachVien(a, p) <= 0.6 / m);
                            // Người dùng 21/11 (căn hộ 441: mẫu CÓ đầu báo ở sảnh trước thang máy): chỉ bỏ sảnh thang trong BUỒNG ĐỆM (≤ 2 ký hiệu cửa có tâm
                            // trong phòng hoặc cách biên ≤ 1 m) hoặc trong BUỒNG THANG (phòng chứa tâm hộp thang bộ). Sảnh tiếp tân, hành lang nhiều cửa phòng: rải.
                            Func<List<Point2d>, int> soCua = f0 => nhanThem.Cua.Count(h => { Point2d c = new Point2d((h[0].X + h[1].X) / 2, (h[0].Y + h[1].Y) / 2);
                                return TrongDaGiac(f0, c) || CachVien(f0, c) <= 1 / m; });
                            // Người dùng 21/11 (nhắc lại): phòng có chữ SẢNH (không kèm THANG), TIẾP TÂN, LỄ TÂN là sảnh chung: luôn rải.
                            List<List<Point2d>> sanh = vung.Where(f0 => DienTichDaGiac(f0) * m * m <= 30 && oX.Any(x => keNhau(f0, x))
                                && !nhanThem.Sanh.Any(p => TrongDaGiac(f0, p))
                                && (soCua(f0) <= 2 || nhanThem.ThangBo.Any(h => TrongDaGiac(f0, new Point2d((h[0].X + h[1].X) / 2, (h[0].Y + h[1].Y) / 2))))).ToList();
                            vung = vung.Where(f0 => !sanh.Contains(f0)).ToList();
                            if (sanh.Count > 0) ed.WriteMessage("\n[TKPCCC] bo_sanh_thang=" + sanh.Count + " m2="
                                + string.Join(",", sanh.Select(f0 => Math.Round(DienTichDaGiac(f0) * m * m, 1).ToString(CultureInfo.InvariantCulture)))
                                + " (phòng nhỏ sát giếng thang máy = sảnh thang: không đặt đầu báo; cần rải thì bấm điểm trong phòng đó).");
                            // L5 (người dùng duyệt 19/11, xưởng Quyết Thắng): khu có chữ MÁI CHE, KHÔNG CÓ CÔNG NĂNG, SÂN, HIỆN HỮU, PHÒNG BƠM không đặt đầu báo.
                            // shortcut: chỉ bỏ phòng ≤ 500 m² chứa tâm chữ; phòng lớn hơn (chữ rơi vào xưởng vì mái che không có tường ngăn) chỉ báo, người dùng vẽ PCCC_VUNG tách ra.
                            List<List<Point2d>> boKhuTen = new List<List<Point2d>>();
                            List<string> ghiKhuTen = new List<string>();
                            foreach (List<Point2d> fk in vung)
                            {
                                string[] loaiKhu = nhanThem.KhuTen.Where(kv => TrongDaGiac(fk, kv.Key)).Select(kv => kv.Value).Distinct().ToArray();
                                if (loaiKhu.Length == 0) continue;
                                double dtKhu = DienTichDaGiac(fk) * m * m, cvKhu = 0;
                                // Review 21/11: luật L5 chỉ nói phòng bơm PCCC ĐỨNG RIÊNG. Phòng bơm thuộc cụm mặt bằng chính (CumChinh, nằm trong nhà, tầng hầm) xét theo
                                // hạng mục: vẫn rải, chỉ báo. Nhà bơm tách rời (Quyết Thắng: nhà bơm 11,4 m² sát bể nước, cụm nhỏ) vẫn bỏ. Cụm tính không kể phòng có chữ khu
                                // khác: mái che 187 m² chưa bỏ nối nhà bơm Quyết Thắng vào xưởng.
                                if (loaiKhu.All(l => l.EndsWith("bơm")))
                                {
                                    int b0, b1;
                                    List<List<Point2d>> xetBom = vung.Where(g0 => g0 == fk || !nhanThem.KhuTen.Any(kv => TrongDaGiac(g0, kv.Key))).ToList();
                                    if (CumChinh(xetBom, 2 / m, out b0, out b1).Contains(fk))
                                    {
                                        ed.WriteMessage("\n[TKPCCC] phong_bom_trong_nha m2=" + Math.Round(dtKhu, 1).ToString(CultureInfo.InvariantCulture)
                                            + " (thuộc khối nhà chính: vẫn rải; phòng bơm đứng riêng không thuộc hạng mục thì xoá đầu báo trong phòng đó).");
                                        continue;
                                    }
                                }
                                for (int i = 0; i < fk.Count; i++) cvKhu += fk[i].GetDistanceTo(fk[(i + 1) % fk.Count]) * m;
                                // Review 20/11 (CHẶN 3): phòng 100–500 m² chỉ bỏ khi là dải (rộng TB 2A/P ≤ 6 m); xưởng nhỏ 15 x 30 m có góc mái che ghi chữ thì vẫn rải.
                                if (dtKhu <= 100 || (dtKhu <= 500 && cvKhu > 0 && 2 * dtKhu / cvKhu <= 6))
                                {
                                    boKhuTen.Add(fk);
                                    ghiKhuTen.Add(string.Join("+", loaiKhu) + " " + Math.Round(dtKhu, 1).ToString(CultureInfo.InvariantCulture) + " m2");
                                }
                                else ed.WriteMessage("\n[TKPCCC] chu_khu_trong_phong_lon m2=" + Math.Round(dtKhu, 1).ToString(CultureInfo.InvariantCulture) + " (" + string.Join(", ", loaiKhu)
                                    + "): vẫn rải phòng này; khu đó không rải thì vẽ polyline kín " + LAYER_VUNG + " tách ra rồi chọn lại.");
                            }
                            vung = vung.Where(f0 => !boKhuTen.Contains(f0)).ToList();
                            if (boKhuTen.Count > 0) ed.WriteMessage("\n[TKPCCC] bo_khu_ten=" + boKhuTen.Count + " (" + string.Join("; ", ghiKhuTen)
                                + ": mái che, sân, hiện hữu, phòng bơm không đặt đầu báo; cần rải thì bấm điểm trong phòng đó).");
                            // L6 (người dùng duyệt 19/11, xưởng Quyết Thắng): hiên = dải hẹp (rộng TB 2A/P ≤ 3 m, ≤ 200 m²) có ≥ 2 cột nằm trong hoặc cách viền ≤ 0,6 m
                            // mà cột đó không thuộc phòng khác (cột trong tường xưởng sát dải xiên có vách thì không tính) -> không đặt đầu báo.
                            // shortcut: chỉ nhận hiên có vẽ cột; hiên chỉ có nét viền mái thì người dùng vẽ PCCC_VUNG tách ra. Chỉ chạy với LoaiTran MaiKeo:
                            // "phòng khác" chỉ là phòng còn lại sau các lượt bỏ, nhà máy nhiều cột (test báo cháy.dwg) có phòng hẹp sát cột mà phòng kề đã bị bỏ.
                            double ganCot = 0.6 / m;
                            Func<List<Point2d>, bool> hep = f0 =>
                            {
                                double a0 = Math.Abs(DienTichDaGiac(f0)) * m * m, p0 = 0;
                                for (int i = 0; i < f0.Count; i++) p0 += f0[i].GetDistanceTo(f0[(i + 1) % f0.Count]) * m;
                                return a0 <= 200 && p0 > 0 && 2 * a0 / p0 <= 3;
                            };
                            List<List<Point2d>> hepDs = loaiTran == 3 ? vung.Where(hep).ToList() : new List<List<Point2d>>();
                            List<List<Point2d>> phongKhac = vung.Where(f0 => !hepDs.Contains(f0)).ToList();
                            Func<List<Point2d>, Point2d, bool> sat = (f0, c0) => TrongDaGiac(f0, c0) || CachVien(f0, c0) <= ganCot;
                            List<List<Point2d>> hien = hepDs.Where(f0 => nhanThem.Cot.Count(c0 => sat(f0, c0) && !phongKhac.Any(g0 => sat(g0, c0))) >= 2).ToList();
                            // Review 20/11 (CHẶN 2): luật L6 "mép ngoài chỉ có cột, không tường": mép ngoài cũng là tường thì không bỏ, chỉ báo.
                            List<List<Point2d>> hienNghi = hien.Where(f0 => !MepHien(f0, phongKhac, nhanThem.Doan, m)).ToList();
                            if (hienNghi.Count > 0)
                            {
                                hien = hien.Where(f0 => !hienNghi.Contains(f0)).ToList();
                                ed.WriteMessage("\n[TKPCCC] hien_nghi=" + hienNghi.Count + " m2=" + string.Join(",", hienNghi.Select(f0 => Math.Round(Math.Abs(DienTichDaGiac(f0)) * m * m, 1).ToString(CultureInfo.InvariantCulture)))
                                    + " (dải hẹp có cột nhưng mép ngoài cũng là tường / tường 1 nét: vẫn rải; đúng là hiên thì xoá đầu báo trong dải đó).");
                            }
                            if (hien.Count > 0)
                            {
                                vung = vung.Where(f0 => !hien.Contains(f0)).ToList();
                                ed.WriteMessage("\n[TKPCCC] bo_hien=" + hien.Count + " m2=" + string.Join(",", hien.Select(f0 => Math.Round(Math.Abs(DienTichDaGiac(f0)) * m * m, 1).ToString(CultureInfo.InvariantCulture)))
                                    + " (dải hẹp mép ngoài chỉ có cột = hiên: không đặt đầu báo; cần rải thì bấm điểm trong dải đó).");
                            }
                            List<List<Point2d>> hlBen = HanhLangBen(vung, phongDu, nhanThem.LanCan, nhanThem.Cua, nhanThem.TenPhong, m);
                            if (hlBen.Count > 0)
                            {
                                vung = vung.Where(f0 => !hlBen.Contains(f0)).ToList();
                                ed.WriteMessage("\n[TKPCCC] bo_hanh_lang_ben=" + hlBen.Count + " m2=" + string.Join(",", hlBen.Select(f0 => Math.Round(Math.Abs(DienTichDaGiac(f0)) * m * m, 1).ToString(CultureInfo.InvariantCulture)))
                                    + " (dải hẹp một mép mở ra sân / ngoài nhà, có lan can hoặc cùng chuỗi = hành lang bên, QCVN 10 1.5.11: không đặt đầu báo; cần rải thì bấm điểm trong dải đó).");
                            }
                            // Người dùng 23/11 (trường học khung 6): mặt con của khối WC (khu rửa tay, khu tiểu, sảnh 7–17 m² tách bởi vách buồng xí) không có chữ,
                            // kề phòng WC vừa bỏ: bỏ theo. Chỉ mặt ≤ 20 m², không chữ tên nào (PHÒNG DỤNG CỤ cạnh WC giữ), không phải dải dài (dài ≤ 3 lần rộng:
                            // hành lang trong nhà sát WC vẫn rải), kề WC (≥ 2 đỉnh cách viền ≤ 0,3 m). Xét sau hành lang bên (dải ngoài cạnh WC đã bỏ ở đó).
                            Func<List<Point2d>, List<Point2d>, bool> keWc = (a0, b0) => a0.Count(p => CachVien(b0, p) <= 0.3 / m) + b0.Count(p => CachVien(a0, p) <= 0.3 / m) >= 2;
                            List<List<Point2d>> conWc = vung.Where(f0 => DienTichDaGiac(f0) * m * m <= 20 && TiLeDai(f0) <= 3
                                && !nhanThem.TenChu.Any(p => TrongDaGiac(f0, p)) && !nhanThem.TenPhong.Any(p => TrongDaGiac(f0, p)) && wc.Any(w0 => keWc(f0, w0))).ToList();
                            if (conWc.Count > 0)
                            {
                                vung = vung.Where(f0 => !conWc.Contains(f0)).ToList();
                                ed.WriteMessage("\n[TKPCCC] bo_mat_con_wc=" + conWc.Count + " m2=" + string.Join(",", conWc.Select(f0 => Math.Round(DienTichDaGiac(f0) * m * m, 1).ToString(CultureInfo.InvariantCulture)))
                                    + " (mặt nhỏ không chữ kề phòng WC = khu rửa tay, khu tiểu của khối WC: không đặt đầu báo; cần rải thì bấm điểm trong phòng đó).");
                            }
                        }
                        int boPhong, boCum;
                        // Trường học khung 6 (21/11): 2 tầng trong một lần chọn, nhà thi đấu tầng 1 cách dãy học một sân > 2 m thành cụm < 20 % cả tầng 2,
                        // bị bỏ như hình chi tiết (mẫu có 15 đầu báo). Có lưới trục: phòng có tâm trong khung trục là mặt bằng thật, không xét cụm
                        // (hình chi tiết ngoài trục đã lọc ở bo_ngoai_truc).
                        List<List<Point2d>> trongKhung = khungTruc == null ? new List<List<Point2d>>() : vung.Where(f0 =>
                        {
                            Point2d c = DiemTrongMat(f0);
                            return (double.IsNaN(khungTruc[0]) || (c.X >= khungTruc[0] && c.X <= khungTruc[2])) && (double.IsNaN(khungTruc[1]) || (c.Y >= khungTruc[1] && c.Y <= khungTruc[3]));
                        }).ToList();
                        vung = CumChinh(vung, 2 / m, out boPhong, out boCum, f0 => nhanThem.TenPhong.Any(p => TrongDaGiac(f0, p)), TEN_PHONG_GAN_M / m);
                        List<List<Point2d>> lai = trongKhung.Where(f0 => !vung.Contains(f0)).ToList();
                        if (lai.Count > 0) { vung.AddRange(lai); boPhong -= lai.Count; boCum = Math.Min(boCum, boPhong); }
                        if (boPhong > 0) ed.WriteMessage("\n[TKPCCC] bo_cum=" + boCum + " phong=" + boPhong + " (cụm phòng nhỏ tách xa mặt bằng chính: hình chi tiết, mặt cắt; cần rải thì bấm điểm trong phòng).");
                        tatCa = true;
                        break;
                    }
                    if (pr.Status != PromptStatus.OK) break;
                    Point3d q = pr.Value.TransformBy(ed.CurrentUserCoordinateSystem);
                    List<Point2d> f = mat.Where(x => TrongDaGiac(x, new Point2d(q.X, q.Y))).OrderBy(x => DienTichDaGiac(x)).FirstOrDefault();
                    if (f == null) ed.WriteMessage("\nĐiểm không nằm trong phòng kín nào của vùng đã chọn.");
                    else if (MatKhongRai(f, oX, new List<List<Point2d>>())) ed.WriteMessage("\nÔ có dấu X (giếng thang máy, khe giữa hai khối): không rải.");
                    else
                    {
                        // Người dùng 16/11: phòng quanh điểm bấm hở thì mặt nhỏ nhất chứa điểm là mặt bao nhiều phòng (763 079 m², rải treo máy): hỏi trước.
                        double sf = DienTichDaGiac(f);
                        int bao = mat.Count(x => x != f && DienTichDaGiac(x) * m * m >= 4 && DienTichDaGiac(x) < sf && TrongDaGiac(f, DiemTrongMat(x)));
                        if (bao >= 3)
                        {
                            PromptKeywordOptions bo = new PromptKeywordOptions("\nĐiểm bấm rơi vào mặt " + Math.Round(sf * m * m).ToString(CultureInfo.InvariantCulture)
                                + " m² bao " + bao + " phòng khác: phòng quanh điểm này chưa kín (khe tường > 3 m không có ký hiệu cửa?). Vẫn rải cả mặt này?");
                            bo.Keywords.Add("Co"); bo.Keywords.Add("Khong");
                            bo.Keywords.Default = "Khong";
                            bo.AllowNone = true;
                            PromptResult br = ed.GetKeywords(bo);
                            if (br.Status == PromptStatus.Cancel) return;
                            if (br.StringResult != "Co") { ed.WriteMessage("\n[TKPCCC] bo_mat_bao m2=" + Math.Round(sf * m * m).ToString(CultureInfo.InvariantCulture) + " phong_trong=" + bao + ": vẽ polyline kín layer " + LAYER_VUNG + " quanh phòng rồi chọn lại."); continue; }
                        }
                        if (!vung.Contains(f)) vung.Add(f);
                    }
                }
                // Người dùng 16/11: vùng vẽ ra là đa giác gọn (bỏ khấc cột, cung cửa, gờ < 1 m), không bám từng nét tường.
                vung = vung.Select(d => LamGon(d, LAM_GON_M / m, 0.002 / m)).ToList();
                // Người dùng 25/11: quét nhiều tầng (Enter mọi phòng), tầng trên có ô X thông tầng mà tầng ngay dưới không có: vùng tầng dưới ở đó lấy như vùng
                // ô X tầng trên. Sau LamGon: phòng chưa làm gọn có thể tự chạm (khe cửa 100 mm của kho gầm thang Karaoke 123 tầng 1), Region không dựng được.
                if (tatCa)
                {
                    List<string> chieu = new List<string>();
                    vung = ChieuThongTang(vung, nhanThem, oX, m, coLo, chieu);
                    if (chieu.Count > 0) ed.WriteMessage("\n[TKPCCC] chieu_thong_tang=" + chieu.Count + " " + string.Join("; ", chieu)
                        + " (tầng dưới sảnh thông tầng không có dấu X: vùng ở đó lấy đúng như vùng ô X tầng trên, đầu báo / đầu phun lắp ở ĐỈNH sảnh;"
                        + " phần còn lại của phòng là vùng riêng, lắp ở trần tầng đó; vế thang cạnh sảnh bỏ).");
                }
                // L7 (người dùng duyệt 19/11, nhà văn phòng 2 Quyết Thắng): thang bộ trong phòng khoét ra như lỗ thông tầng, cạnh vuông theo hộp ký hiệu thang,
                // cạnh hộp cách tường ≤ 0,3 m nắn ra sát tường. Thang chiếm ≥ 50 % phòng = buồng thang (người dùng 16/11: không đặt đầu báo): bỏ cả phòng.
                if (v.he == "BC" && nhanThem.ThangBo.Count > 0)
                {
                    List<string> khoet = new List<string>(), buong = new List<string>();
                    List<List<Point2d>> conT = new List<List<Point2d>>();
                    foreach (List<Point2d> z in vung)
                    {
                        List<Point2d> kq = z;
                        bool bo = false;
                        foreach (Point2d[] h in nhanThem.ThangBo)
                        {
                            if (!TrongDaGiac(kq, new Point2d((h[0].X + h[1].X) / 2, (h[0].Y + h[1].Y) / 2))) continue;
                            double dtZ = Math.Abs(DienTichDaGiac(kq)) * m * m, dtH = (h[1].X - h[0].X) * (h[1].Y - h[0].Y) * m * m;
                            if (dtH >= 0.5 * dtZ) { bo = true; buong.Add(Math.Round(dtZ, 1).ToString(CultureInfo.InvariantCulture)); break; }
                            List<Point2d> t = KhoetHop(kq, NanHop(kq, h, 0.3 / m, 0.01 / m), 0.002 / m);
                            if (t == null) continue;
                            khoet.Add(Math.Round(dtZ - Math.Abs(DienTichDaGiac(t)) * m * m, 1).ToString(CultureInfo.InvariantCulture));
                            kq = t;
                        }
                        if (!bo) conT.Add(kq);
                    }
                    vung = conT;
                    if (khoet.Count > 0) ed.WriteMessage("\n[TKPCCC] khoet_thang=" + khoet.Count + " m2=" + string.Join(",", khoet) + " (thang bộ trong phòng khoét ra như lỗ thông tầng: không đặt đầu báo trên thang).");
                    if (buong.Count > 0) ed.WriteMessage("\n[TKPCCC] bo_buong_thang=" + buong.Count + " m2=" + string.Join(",", buong) + " (thang bộ chiếm ≥ 50 % phòng = buồng thang: không đặt đầu báo; cần rải thì vẽ polyline kín " + LAYER_VUNG + " quanh phòng rồi chọn lại).");
                }
                List<List<Point2d>> coSan = VungCoSan(db);
                if (coSan.Count > 0)
                {
                    vung = TheoVungCoSan(vung, coSan, daCo);
                    if (daCo.Count > 0) ed.WriteMessage("\n[TKPCCC] theo_vung_co_san=" + daCo.Count + " (vùng " + LAYER_VUNG + " vẽ sẵn trong phòng: rải theo vùng đó, không vẽ chồng).");
                }
                // Vùng = phòng (vẽ lên PCCC_VUNG); dầm sâu chia khoang bên trong (nét khoang layer PCCC_KHOANG_M2), như vùng vẽ sẵn.
                if (damSau.Count > 0)
                {
                    List<List<Point2d>> moi = new List<List<Point2d>>();
                    foreach (List<Point2d> z in vung)
                    {
                        List<List<Point2d>> kc = ChiaKhoang(z, damSau, 0.002 / m, m);
                        moi.AddRange(kc);
                        if (kc.Count > 1) foreach (List<Point2d> x in kc) khoangChia[x] = z;
                    }
                    vung = moi;
                    if (khoangChia.Count > 0) ed.WriteMessage("\n[TKPCCC] chia_khoang=" + khoangChia.Count + " (phòng tự dựng chia theo dầm sâu; nét khoang vẽ trên layer " + LAYER_KHOANG + ")");
                }
            }
            // L2 (người dùng duyệt 20/11, xưởng mái kèo): phòng lớn chia khoang giữa hai hàng vì kèo (tim hàng cột), như vùng AI đã vẽ; cả vùng tự dựng lẫn vùng vẽ sẵn.
            if (loaiTran == 3)
            {
                int soPhongMk = 0, soKhoangMk = 0;
                List<List<Point2d>> moiMk = new List<List<Point2d>>();
                List<List<Point2d>> maiCheMk = new List<List<Point2d>>(); // khoang bỏ vì chữ mái che / không có công năng: L5b lan sang phòng nhỏ sát nó
                foreach (List<Point2d> zMk in vung)
                {
                    // Review 21/11: phòng có tên phòng (văn phòng, phòng họp) giữ nguyên, không chia theo hàng cột mượn của xưởng bên (L3).
                    List<Point2d[]> vk = nhanThem.TenPhong.Any(p => TrongDaGiac(zMk, p)) ? new List<Point2d[]>() : DuongViKeo(zMk, nhanThem.Cot, m);
                    List<List<Point2d>> kcMk = vk.Count == 0 ? new List<List<Point2d>> { zMk } : ChiaKhoang(zMk, vk, 0.002 / m, m);
                    if (kcMk.Count >= 2)
                    {
                        // Thử 20/11: dải "KHU VỰC MÁI CHE" không có tường ngăn nằm trong phòng xưởng; sau khi chia, khoang ≤ 500 m² chứa tâm chữ khu (L5) thì bỏ.
                        // Review 20/11 (CHẶN 3): chỉ bỏ khoang ĐẦU / CUỐI theo phương chia, nhỏ hơn 0,9 x diện tích khoang trung vị (dải mái che đầu hồi như Quyết Thắng:
                        // 75,9 m² hình chữ L dài 10,3 m theo phương chia, khoang thường 120 m²);
                        // mái che dọc cạnh dài không tường ngăn thì chữ rơi vào khoang giữa chạy hết bề rộng xưởng: giữ, chỉ báo.
                        Vector2d nMk = (vk[0][1] - vk[0][0]).GetNormal().GetPerpendicularVector();
                        Func<List<Point2d>, double[]> chieu = x => new[] { x.Min(p => nMk.DotProduct(p.GetAsVector())), x.Max(p => nMk.DotProduct(p.GetAsVector())) };
                        double[] cPh = chieu(zMk);
                        List<double> dtMk = kcMk.Select(x => Math.Abs(DienTichDaGiac(x))).OrderBy(bMk => bMk).ToList();
                        double trungVi = dtMk[dtMk.Count / 2], biMk = 0.3 / m;
                        Func<List<Point2d>, bool> dauHep = x =>
                        {
                            double[] cx = chieu(x);
                            return (cx[0] <= cPh[0] + biMk || cx[1] >= cPh[1] - biMk) && Math.Abs(DienTichDaGiac(x)) < 0.9 * trungVi;
                        };
                        List<List<Point2d>> chuMk = kcMk.Where(x => DienTichDaGiac(x) * m * m <= 500 && nhanThem.KhuTen.Any(kv => TrongDaGiac(x, kv.Key))).ToList();
                        List<List<Point2d>> boMk = chuMk.Where(dauHep).ToList();
                        if (chuMk.Count > boMk.Count)
                            ed.WriteMessage("\n[TKPCCC] mai_keo chu_khu_khoang_giua=" + (chuMk.Count - boMk.Count) + " (chữ mái che / không công năng nằm ở khoang giữa xưởng: vẫn rải;"
                                + " khu đó không rải thì vẽ polyline kín " + LAYER_VUNG + " tách ra rồi chọn lại).");
                        if (boMk.Count > 0)
                        {
                            kcMk = kcMk.Where(x => !boMk.Contains(x)).ToList();
                            maiCheMk.AddRange(boMk.Where(x => nhanThem.KhuTen.Any(kv => TrongDaGiac(x, kv.Key) && (kv.Value == "mái che" || kv.Value == "không có công năng"))));
                            ed.WriteMessage("\n[TKPCCC] mai_keo bo_khoang_khu_ten=" + boMk.Count + " m2="
                                + string.Join(",", boMk.Select(x => Math.Round(DienTichDaGiac(x) * m * m, 1).ToString(CultureInfo.InvariantCulture))));
                        }
                    }
                    moiMk.AddRange(kcMk);
                    if (kcMk.Count < 2) continue;
                    List<Point2d> chaMk;
                    if (khoangChia.TryGetValue(zMk, out chaMk)) khoangChia.Remove(zMk); else chaMk = zMk; // khoang dầm chia tiếp: không vẽ nét khoang cũ
                    foreach (List<Point2d> xMk in kcMk) khoangChia[xMk] = chaMk;
                    soPhongMk++;
                    soKhoangMk += kcMk.Count;
                }
                // L5b (người dùng 20/11 "Không rải", xưởng Quyết Thắng): "KHU VỰC MÁI CHE" phía nam bị nét chia thành nhiều mặt, chữ chỉ nằm trong một khoang.
                // Phòng ≤ 100 m² không có chữ tên nào (review 21/11: trước chỉ chữ "PHÒNG" giữ được, KHO 1 / KHO 2 bị bỏ), không phải khoang xưởng,
                // cách khoang mái che đã bỏ (hoặc phòng vừa bỏ theo luật này) ≤ 0,3 m thì bỏ theo.
                // shortcut: lan theo khoảng cách đỉnh – cạnh; kho nhỏ có tường dưới mái che mà không ghi chữ nào cũng bị bỏ (dòng lệnh báo m², bấm điểm để rải lại).
                double keMc = 0.3 / m;
                Func<List<Point2d>, List<Point2d>, bool> keNhau = (a0, b0) => a0.Any(p0 => CachVien(b0, p0) <= keMc) || b0.Any(p0 => CachVien(a0, p0) <= keMc);
                List<List<Point2d>> boKe = new List<List<Point2d>>();
                for (List<List<Point2d>> nguon = maiCheMk; nguon.Count > 0; )
                {
                    nguon = moiMk.Where(x => !boKe.Contains(x) && !khoangChia.ContainsKey(x) && Math.Abs(DienTichDaGiac(x)) * m * m <= 100
                        && !nhanThem.TenChu.Any(t0 => TrongDaGiac(x, t0)) && nguon.Any(y0 => keNhau(x, y0))).ToList();
                    boKe.AddRange(nguon);
                }
                if (boKe.Count > 0)
                {
                    moiMk = moiMk.Where(x => !boKe.Contains(x)).ToList();
                    ed.WriteMessage("\n[TKPCCC] mai_keo bo_ke_mai_che=" + boKe.Count + " m2=" + string.Join(",", boKe.Select(x => Math.Round(Math.Abs(DienTichDaGiac(x)) * m * m, 1).ToString(CultureInfo.InvariantCulture)))
                        + " (phòng nhỏ sát khu mái che không công năng: không đặt đầu báo; cần rải thì bấm điểm trong phòng đó).");
                }
                vung = moiMk;
                ed.WriteMessage("\n[TKPCCC] mai_keo cot=" + nhanThem.Cot.Count + " phong_chia=" + soPhongMk + " khoang=" + soKhoangMk
                    + (nhanThem.Cot.Count < 2 ? " (không nhận được cột: chọn cả cột khi quét, hoặc vẽ polyline kín " + LAYER_VUNG + " từng khoang)" : " (nét khoang vẽ trên layer " + LAYER_KHOANG + ")"));
            }

            // Khoang dầm (TCVN 7568-14:2025 5.9.1.1.6 c–f; bộ bản vẽ mẫu ghi "khoang < 9 m² => không bố trí theo khoang dầm"): khoang nhỏ hơn ngưỡng
            // gom chung, rải MỘT lưới như trần phẳng trên hộp bao các khoang nhỏ, giữ điểm nằm trong khoang nhỏ hoặc trên dầm sát khoang (≤ 0,6 m).
            List<List<Point2d>> nho = new List<List<Point2d>>();
            // Hộp bao của từng cụm khoang nhỏ -> các khoang của cụm (review 15/11: trước gom khoang nhỏ của MỌI phòng vào một hộp, khoang có thể không có đầu báo).
            Dictionary<List<Point2d>, List<List<Point2d>>> nhoCuaHop = new Dictionary<List<Point2d>, List<List<Point2d>>>();
            List<List<Point2d>> vungRai = vung;
            if (khoangDam && !tia)
            {
                // Không nhập trần: ngưỡng 4 m² của trần 2–4 m (an toàn hơn 9 m²: khoang 4–9 m² vẫn có đầu báo riêng, 5.9.1.1.6 d).
                double nguong = hTran > 0 && hTran < 2 ? double.MaxValue : hTran >= 4 ? 9 : 4;
                // Review 20/11 (CHẶN 4): mái kèo không có dầm, chỉ khoang vì kèo (khoangChia) mới gom; phòng nhỏ sát nhau rải riêng như CoTran.
                nho = vung.Where(d => DienTichDaGiac(d) * m * m < nguong && (loaiTran != 3 || khoangChia.ContainsKey(d))).ToList();
                vungRai = vung.Where(d => !nho.Contains(d)).ToList();
                // Cụm = khoang nhỏ kề nhau (hộp bao cách nhau ≤ 0,6 m, tức qua một dầm); mỗi cụm một lưới như trần phẳng.
                double ke = 0.6 / m;
                double[][] hb = nho.Select(d => new[] { d.Min(p => p.X), d.Min(p => p.Y), d.Max(p => p.X), d.Max(p => p.Y) }).ToArray();
                int[] cha = Enumerable.Range(0, nho.Count).ToArray();
                Func<int, int> goc = null;
                goc = i => cha[i] == i ? i : (cha[i] = goc(cha[i]));
                for (int i = 0; i < nho.Count; i++)
                    for (int j = i + 1; j < nho.Count; j++)
                        if (hb[i][0] <= hb[j][2] + ke && hb[j][0] <= hb[i][2] + ke && hb[i][1] <= hb[j][3] + ke && hb[j][1] <= hb[i][3] + ke) cha[goc(i)] = goc(j);
                foreach (IGrouping<int, int> cum in Enumerable.Range(0, nho.Count).GroupBy(i => goc(i)))
                {
                    double x0 = cum.Min(i => hb[i][0]), y0 = cum.Min(i => hb[i][1]), x1 = cum.Max(i => hb[i][2]), y1 = cum.Max(i => hb[i][3]);
                    List<Point2d> hop = new List<Point2d> { new Point2d(x0, y0), new Point2d(x1, y0), new Point2d(x1, y1), new Point2d(x0, y1) };
                    nhoCuaHop[hop] = cum.Select(i => nho[i]).ToList();
                    vungRai.Add(hop);
                }
                ed.WriteMessage("\n[TKPCCC] khoang_dam nguong=" + (nguong == double.MaxValue ? "phang" : nguong.ToString(CultureInfo.InvariantCulture))
                    + " lon=" + (vung.Count - nho.Count) + " nho=" + nho.Count + " cum=" + nhoCuaHop.Count
                    + (hTran <= 0 ? " (không nhập trần: lấy ngưỡng 4 m² của trần 2–4 m, an toàn)" : ""));
            }

            int tong = 0, soHo = 0, soBo = 0;
            int[] ghi = { 0, 0, 0 };
            dongHo.Restart();
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTableRecord ms = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);
                ObjectId lop = veVung && !xem ? DamBaoLayer(db, tr, LAYER_VUNG, MAU_VUNG) : ObjectId.Null;
                ObjectId lopTia = tia && !xem ? DamBaoLayer(db, tr, LAYER_TIA, MauHe(v.he)) : ObjectId.Null;
                string duoi = xem ? " (xem trước, chưa vẽ)" : "";
                // Xem trước (spec addin mẫu): vẽ tạm điểm + vòng R hiệu dụng trên layer không in; lượt rải thật hay xem lại thì xoá bản tạm cũ.
                int xoaTam = XoaXemTruoc(db, tr, ms);
                if (xoaTam > 0 && !xem) ed.WriteMessage("\n[TKPCCC] xoa_xem_truoc=" + xoaTam);
                ObjectId lopTam = xem ? LopXemTruoc(db, tr) : ObjectId.Null;
                double rTam = tc == null ? 0 : LaySo(tc, "r", 0) * s / D(Lay(tc, "s")); // R tiêu chuẩn (m) nhân cùng tỉ lệ với bước (k, dầm, đổi đơn vị)
                int soTam = 0;
                Dictionary<List<Point2d>, List<Point3d>> diemCha = new Dictionary<List<Point2d>, List<Point3d>>();
                if (!xem && khoangChia.Count > 0)
                {
                    ObjectId lopK = DamBaoLayer(db, tr, LAYER_KHOANG, 6);
                    foreach (List<Point2d> kc in khoangChia.Keys) { Polyline pk = PlVung(kc, lopK); ms.AppendEntity(pk); tr.AddNewlyCreatedDBObject(pk, true); }
                }
                // Thanh tiến độ + Esc mỗi phòng (review 15/11: lệnh dài không dừng được, AutoCAD "Not Responding"). Esc: ném lỗi, transaction không commit.
                Autodesk.AutoCAD.Runtime.ProgressMeter tienDo = new Autodesk.AutoCAD.Runtime.ProgressMeter();
                tienDo.SetLimit(vungRai.Count);
                tienDo.Start("TKPCCC: rải " + vungRai.Count + " vùng (Esc = dừng)");
                try {
                foreach (List<Point2d> d in vungRai)
                {
                    tienDo.MeterProgress();
                    KiemEsc();
                    if (tia)
                    {
                        double bt, tt;
                        List<Point2d[]> ts = TiaVung(d, s, 0.2 / m, out bt, out tt);
                        int soKt = 0;
                        if (!xem)
                        {
                            foreach (Point2d[] a in ts) VeTia(db, tr, ms, v, a, k, lopTia, 0.3 / m);
                            soKt = KtTia(db, tr, ms, d, ts, DamBaoLayer(db, tr, LAYER_TIA_KT, MauHe(v.he)));
                        }
                        else foreach (Point2d[] a in ts) soTam += VeTam(tr, ms, new Line(new Point3d(a[0].X, a[0].Y, 0), new Point3d(a[1].X, a[1].Y, 0)), lopTam);
                        tong += ts.Count;
                        ed.WriteMessage("\n[TKPCCC] PCCC_RAI " + v.ma + " tia=" + ts.Count + " buoc=" + Lt(bt).ToString(CultureInfo.InvariantCulture)
                            + " tuong=" + Lt(tt).ToString(CultureInfo.InvariantCulture) + (xem ? "" : " kt=" + soKt) + duoi);
                        continue;
                    }
                    List<List<Point2d>> nhoD;
                    bool laHopNho = nhoCuaHop.TryGetValue(d, out nhoD);
                    LuoiRai r;
                    // Người dùng 16/11: vùng nhận sai (mặt bao cả mặt bằng, 763 079 m²) cho lưới hàng nghìn thiết bị: bỏ riêng vùng đó, không vẽ vùng, rải tiếp vùng khác.
                    Func<List<Point2d>, LuoiRai> luoiMot = x => LuoiVung(x, s, s, 0, 0, le, le, 0.05 / m, dt, 0, theoHang, tuongMax, cachMin);
                    // vùng chữ L / T (người dùng 25/11): từng mảnh chữ nhật; khoang dầm, hộp khoang nhỏ giữ một lưới
                    try { r = laHopNho || khoangChia.ContainsKey(d) ? luoiMot(d) : LuoiTach(d, m, luoiMot); }
                    catch (System.Exception ex)
                    {
                        soBo++;
                        ed.WriteMessage("\n[TKPCCC] bo_vung m2=" + Math.Round(DienTichDaGiac(d) * m * m, 2).ToString(CultureInfo.InvariantCulture) + ": " + ex.Message);
                        continue;
                    }
                    if (veVung && !xem)
                        foreach (List<Point2d> kv in laHopNho ? nhoD : new List<List<Point2d>> { d })
                        {
                            List<Point2d> dv;
                            if (!khoangChia.TryGetValue(kv, out dv)) dv = kv; // khoang dầm: vẽ phòng chứa nó, một lần
                            if (daCo.Contains(dv) || !daVe.Add(dv)) continue;
                            Polyline pl = PlVung(dv, lop);
                            ms.AppendEntity(pl);
                            tr.AddNewlyCreatedDBObject(pl, true);
                            VungLenTren(tr, ms, pl);
                            ed.WriteMessage("\n[TKPCCC] Vùng tự dựng m2=" + Math.Round(DienTichDaGiac(dv) * m * m, 2).ToString(CultureInfo.InvariantCulture)
                                + " (" + dv.Count + " đỉnh) trên layer " + LAYER_VUNG + ".");
                        }
                    if (laHopNho)
                    {
                        int ngoai = r.Diem.RemoveAll(p => !nhoD.Any(q => TrongDaGiac(q, new Point2d(p.X, p.Y)) || CachVien(q, new Point2d(p.X, p.Y)) <= 0.6 / m));
                        // Lưới tính trên hộp bao, đã lọc: kiểm phủ lại trên chính các khoang, chỗ hở thì thêm đầu báo.
                        double h0 = tuongMax > 0 ? Math.Min(s / 2, tuongMax) : s / 2;
                        int bu, len = 0;
                        // 5.9.1.1.6 c, e: khoang nhỏ hơn ngưỡng thì đầu báo lắp ở mặt dưới dầm; người dùng 17/11: đặt đúng tim bề rộng dầm.
                        // Trần < 2 m (khoản b) là trần phẳng, đặt đâu cũng được. Đầu phun không đặt lên dầm.
                        if (v.he == "BC" && !(hTran > 0 && hTran < 2) && timDam.Count > 0)
                        {
                            len = DatLenDam(nhoD, r.Diem, timDam, KE_DAM_M / m);
                            bu = PhuBuDam(nhoD, r.Diem, h0, h0, timDam, KE_DAM_M / m);
                        }
                        else bu = PhuBu(nhoD, r.Diem, h0, h0);
                        r.Ho = 0;
                        ed.WriteMessage("\n[TKPCCC] khoang_nho_chung n=" + r.Diem.Count + (ngoai > 0 ? " bo_ngoai=" + ngoai : "") + (bu > 0 ? " bu=" + bu : "")
                            + (len > 0 ? " len_dam=" + len + " (mặt dưới dầm, tim bề rộng dầm: 5.9.1.1.6 c, e)" : ""));
                    }
                    // shortcut: điểm trong vùng loại trừ chỉ bị bỏ, không dời ra mép lỗ; mép lỗ xa thiết bị thì PCCC_KT báo vùng mù, rải bù bằng PCCC_DAT.
                    int bo = r.Diem.RemoveAll(p => loaiTru.Any(q => TrongDaGiac(q, new Point2d(p.X, p.Y))));
                    if (bo > 0) ed.WriteMessage("\n[TKPCCC] bo_loai_tru=" + bo);
                    int dich = 0, ket = 0;
                    for (int i = 0; i < r.Diem.Count; i++)
                    {
                        List<Point2d> og = ongGio.FirstOrDefault(q => TrongDaGiac(q, new Point2d(r.Diem[i].X, r.Diem[i].Y)));
                        if (og == null) continue;
                        Point2d? ra = DichRaNgoai(og, new Point2d(r.Diem[i].X, r.Diem[i].Y), 0.1 / m);
                        if (ra.HasValue && TrongDaGiac(d, ra.Value) && !ongGio.Any(q => TrongDaGiac(q, ra.Value))) { r.Diem[i] = new Point3d(ra.Value.X, ra.Value.Y, 0); dich++; }
                        else ket++;
                    }
                    if (dich + ket > 0) ed.WriteMessage("\n[TKPCCC] dich_ong_gio=" + dich + (ket > 0 ? " ket=" + ket + " (không dịch ra được: kiểm lại tay)" : ""));
                    int soKtRai = 0;
                    if (!xem)
                    {
                        foreach (Point3d p in r.Diem) ChenThietBi(db, tr, v, p, k, 0);
                        // Người dùng 16/11: tự dim khoảng cách giữa các đầu (một chuỗi X, một chuỗi Y), kiểu dim theo trang Cài đặt.
                        // Lỗi 6 KOCHAM 16/11 (20 khoang, mỗi khoang 4 dim): khoang chia từ một vùng vẽ sẵn dim chung trên vùng đó, sau vòng rải.
                        List<Point2d> cha;
                        if (khoangChia.TryGetValue(d, out cha))
                        {
                            List<Point3d> l;
                            if (!diemCha.TryGetValue(cha, out l)) { l = new List<Point3d>(); diemCha[cha] = l; }
                            l.AddRange(r.Diem);
                        }
                        else foreach (KeyValuePair<List<Point2d>, List<Point3d>> kv in VungDim(d, r, r.Diem)) soKtRai += KtSauRai(db, tr, ms, kv.Key, kv.Value, null, nhanThem.Cot);
                    }
                    else foreach (Point3d p in r.Diem)
                    {
                        soTam += VeTam(tr, ms, new Circle(p, Vector3d.ZAxis, 0.15 / m), lopTam);
                        if (rTam > 0) soTam += VeTam(tr, ms, new Circle(p, Vector3d.ZAxis, rTam), lopTam);
                    }
                    tong += r.Diem.Count;
                    if (r.Ho > 0) soHo++;
                    ed.WriteMessage("\n[TKPCCC] PCCC_RAI " + v.ma + " n=" + r.Diem.Count + (r.Manh != null ? " manh=" + r.Manh.Count : "") + " luoi=" + r.Nx + "x" + r.Ny
                        + " buoc=" + Lt(r.Bx).ToString(CultureInfo.InvariantCulture) + "x" + Lt(r.By).ToString(CultureInfo.InvariantCulture)
                        + " m2=" + Math.Round(r.O * m * m, 2).ToString(CultureInfo.InvariantCulture)
                        + (r.Ho > 0 ? " HO=" + r.Ho.ToString(CultureInfo.InvariantCulture) + "% (vùng chưa phủ hết, kiểm lại)" : "")
                        + (soKtRai > 0 ? " kt=" + soKtRai : "") + duoi);
                }
                } finally { tienDo.Stop(); tienDo.Dispose(); }
                // Người dùng 17/11: đầu báo trong khoang (≥ 4 / 9 m²) thì mép dầm là tường, dim từ mép dầm ra (chuỗi chung vẫn một chuỗi X, một chuỗi Y).
                foreach (KeyValuePair<List<Point2d>, List<Point3d>> kv in diemCha)
                    ed.WriteMessage("\n[TKPCCC] kt_gom n=" + kv.Value.Count + " kt=" + KtSauRai(db, tr, ms, kv.Key, kv.Value, v.he == "BC" ? damSau : null)
                        + " (dim chung các khoang của một vùng" + (v.he == "BC" ? ", mốc mép dầm" : "") + ")");
                ed.WriteMessage("\n[TKPCCC] thoi_gian rai=" + dongHo.ElapsedMilliseconds + "ms vung=" + vungRai.Count);
                if (xem) ed.WriteMessage("\n[TKPCCC] xem_truoc=" + soTam + " (vẽ tạm trên layer " + LAYER_XEM_TRUOC + ", không in, tự xoá khi rải thật)");
                // Ý từ video VPTTOOL 16/11: ghi m² từng khoang cho người thẩm duyệt kiểm; tắt ở tab Khoang dầm (cai_dat.json "kd_ghi_m2").
                if (khoangDam && !tia && !xem && CaiDat.Doc("kd_ghi_m2", "Co") != "Khong")
                {
                    ghi = GhiKhoang(db, tr, ms, vung, hTran > 0 && hTran < 2 ? new List<List<Point2d>>() : nho, m); // trần < 2 m (khoản b) là trần phẳng: không đánh X
                    ed.WriteMessage("\n[TKPCCC] ghi_khoang=" + ghi[0] + " dau_x=" + ghi[1] + " bo_nho_1m2=" + ghi[2]
                        + " (chữ m² giữa khoang, dấu X = khoang dưới ngưỡng: đầu báo lắp ở mặt dưới dầm; layer " + LAYER_KHOANG + ")");
                }
                tr.Commit();
            }
            ed.WriteMessage("\n" + (xem ? "Xem trước: sẽ rải " : "Đã rải tổng ") + tong + " " + v.ten + (tia ? " (tia)" : "") + " trong " + vung.Count + " phòng.");
            KetQuaRaiCuoi = (xem ? "Xem trước (chưa vẽ): " : "Lượt cuối: ") + tong + " " + v.ma + (tia ? " (tia)" : "") + " trong " + vung.Count + (khoangDam && !tia ? " khoang" : " phòng")
                + (ghi[0] > 0 ? " (" + ghi[1] + " khoang nhỏ đánh X" + (ghi[2] > 0 ? ", " + ghi[2] + " khoang < 1 m² không ghi" : "") + ")" : "")
                + (soBo > 0 ? " · BỎ " + soBo + " vùng lưới quá lớn (vùng nhận sai, xem dòng lệnh)" : "")
                + (soHo > 0 ? " · " + soHo + " phòng chưa phủ hết (HO, xem dòng lệnh)" : tia ? "" : soBo > 0 ? "" : " · phủ kín");
        }

        static readonly string[] LOAI_TRAN = { "CoTran", "KhongTran", "HonHop", "MaiKeo" };
    }
}
