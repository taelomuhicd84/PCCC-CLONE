// Rải thiết bị theo lưới trong vùng (thao tác "rai", lệnh PCCC_RAI) và đi tuyến ống/dây qua các điểm (thao tác "tuyen").
// Không chứa trị số quy chuẩn: bước rải do người gọi đưa vào, hoặc đọc theo mã thiết bị từ data/khoang_cach_rai.json (cad/rule/LUAT.md chỉ nơi tra).
// Cách vẽ theo bản mẫu của người dùng: cad/knowledge/mau-ban-ve-pccc.md.
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

        /// Độ sâu dầm (m) từ ký hiệu "b x h" (mm), vd "B1 (400x600)", "300X700", "250×500". -1 = không phải ký hiệu dầm.
        /// Số lớn hơn là h (người dùng dặn 16/11): có bộ kết cấu ghi ngược h x b ("DS 400X250" = rộng 250, cao 400).
        static double DocBxh(string s)
        {
            System.Text.RegularExpressions.Match k = System.Text.RegularExpressions.Regex.Match(s ?? "", @"(\d{2,4})\s*[xX×\*]\s*(\d{2,4})");
            if (!k.Success) return -1;
            double m1 = double.Parse(k.Groups[1].Value, CultureInfo.InvariantCulture), m2 = double.Parse(k.Groups[2].Value, CultureInfo.InvariantCulture);
            // shortcut: dầm bẹt (rộng > cao) bị đọc thành dầm cao; gặp bản vẽ có dầm bẹt thì thêm lựa chọn "đọc đúng b x h".
            double b = Math.Min(m1, m2), h = Math.Max(m1, m2);
            return b >= 50 && b <= 2000 && h >= 100 && h <= 3000 ? h / 1000 : -1;
        }

        /// Ký hiệu dầm "b x h" trong các đối tượng (cả chữ trong block lồng nhau): tâm hộp bao chữ -> độ sâu (m).
        static List<KeyValuePair<Point2d, double>> NhanDam(ObjectId[] ids)
        {
            List<KeyValuePair<Point2d, double>> kq = new List<KeyValuePair<Point2d, double>>();
            Database db = ids.Length > 0 ? ids[0].Database : null;
            if (db == null) return kq;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                foreach (ObjectId id in ids)
                {
                    Entity e = tr.GetObject(id, OpenMode.ForRead) as Entity;
                    if (e != null) ChuDam(e, 0, kq);
                }
                tr.Commit();
            }
            return kq;
        }

        static void ChuDam(Entity e, int sau, List<KeyValuePair<Point2d, double>> kq)
        {
            if (!e.Visible) return;
            BlockReference br = e as BlockReference;
            if (br != null)
            {
                if (br is Table || sau > 6) return;
                DBObjectCollection con = new DBObjectCollection();
                try { br.Explode(con); } catch (System.Exception) { }
                foreach (DBObject x in con)
                {
                    Entity c = x as Entity;
                    if (c != null) ChuDam(c, sau + 1, kq);
                    x.Dispose();
                }
                return;
            }
            DBText dt = e as DBText;
            MText mt = e as MText;
            string t = dt != null ? dt.TextString : mt != null ? mt.Text : null;
            if (t == null) return;
            double d = DocBxh(t);
            if (d <= 0) return;
            Point3d p = dt != null ? dt.Position : mt.Location;
            try { Extents3d x = e.GeometricExtents; p = x.MinPoint + (x.MaxPoint - x.MinPoint) * 0.5; } catch (System.Exception) { }
            kq.Add(new KeyValuePair<Point2d, double>(new Point2d(p.X, p.Y), d));
        }

        /// Các đoạn thẳng của một line / polyline (cung coi là dây cung).
        static List<Point2d[]> DoanNet(Database db, ObjectId id)
        {
            List<Point2d[]> kq = new List<Point2d[]>();
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                Line ln = tr.GetObject(id, OpenMode.ForRead) as Line;
                Polyline pl = tr.GetObject(id, OpenMode.ForRead) as Polyline;
                if (ln != null) kq.Add(new[] { new Point2d(ln.StartPoint.X, ln.StartPoint.Y), new Point2d(ln.EndPoint.X, ln.EndPoint.Y) });
                else if (pl != null)
                    for (int i = 0; i < (pl.Closed ? pl.NumberOfVertices : pl.NumberOfVertices - 1); i++)
                        kq.Add(new[] { pl.GetPoint2dAt(i), pl.GetPoint2dAt((i + 1) % pl.NumberOfVertices) });
                tr.Commit();
            }
            return kq;
        }

        /// Thêm thiết bị cho tới khi mọi điểm mẫu (đỉnh + lưới bước min(hx, hy) / 4) của từng khoang có thiết bị lệch ≤ hx theo X, ≤ hy theo Y.
        /// Lần đầu thử tâm khoang, không phủ được điểm hở thì đặt ngay điểm hở. Trả số thiết bị thêm.
        static int PhuBu(List<List<Point2d>> khoang, List<Point3d> diem, double hx, double hy)
        {
            int them = 0;
            double e = 1e-6 * (hx + hy), b = Math.Min(hx, hy) / 4;
            foreach (List<Point2d> q in khoang)
            {
                List<Point2d> mau = new List<Point2d>(q);
                double x0 = q.Min(p => p.X), x1 = q.Max(p => p.X), y0 = q.Min(p => p.Y), y1 = q.Max(p => p.Y);
                for (double y = y0 + b / 2; y < y1; y += b)
                    for (double x = x0 + b / 2; x < x1; x += b)
                        if (TrongDaGiac(q, new Point2d(x, y))) mau.Add(new Point2d(x, y));
                for (int lan = 0; lan < 100; lan++)
                {
                    Point2d? ho = null;
                    foreach (Point2d p in mau)
                        if (!diem.Any(c => Math.Abs(c.X - p.X) <= hx + e && Math.Abs(c.Y - p.Y) <= hy + e)) { ho = p; break; }
                    if (!ho.HasValue) break;
                    Point2d t = DiemTrongMat(q);
                    if (lan > 0 || Math.Abs(t.X - ho.Value.X) > hx || Math.Abs(t.Y - ho.Value.Y) > hy) t = ho.Value;
                    diem.Add(new Point3d(t.X, t.Y, 0));
                    them++;
                }
            }
            return them;
        }

        /// Khoảng cách từ p tới viền đa giác q.
        static double CachVien(List<Point2d> q, Point2d p)
        {
            double kq = double.MaxValue;
            for (int i = 0; i < q.Count; i++) kq = Math.Min(kq, CachDoan(new[] { q[i], q[(i + 1) % q.Count] }, p));
            return kq;
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
                    Point2d tot = P; // không vị trí nào trong vùng: đặt ngay điểm hở (luôn thuộc vùng)
                    int diemTot = 0;
                    for (int lan = 0; lan < 2 && diemTot == 0; lan++) // lần 2 bỏ điều kiện cách tường (góc quá nhọn)
                        foreach (Point2d c in thu)
                        {
                            if (!TrongDaGiac(q, c) || (lan == 0 && cachMin > 0 && KhoangBien(q, c) < cachMin)) continue;
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
        /// tuongMax > 0: cách tường tối đa theo tiêu chuẩn; cachMin > 0: thiết bị thêm cách tường ít nhất bấy nhiêu (đơn vị bản vẽ).
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

        /// Kích thước như bản mẫu (TCVN 7568-14 Hình 2): chuỗi tường, thiết bị, ..., tường của hàng trên cùng (đặt trên vùng) và cột trái cùng
        /// (đặt bên trái vùng), tường lấy theo giao của hàng / cột với đa giác; mỗi góc lồi một kích thước chéo từ thiết bị gần nhất tới góc.
        static void KtRai(GoiSpec g, List<Point2d> vung, LuoiRai r)
        {
            double cao = LaySo(NhomKyHieu("kt"), "cao", 250);
            ObjectId l = DamBaoLayer(g.Db, g.Tr, LAYER_KT_RAI, 8);
            foreach (Dimension d in KtLuoi(vung, r.Diem, true, DamBaoKieuKt(g), cao, EPS_DIM_M / HeSoMet(g.Db)))
            {
                d.Dimscale = cao / 2.5;
                d.LayerId = l;
                Them(g, d);
            }
        }

        /// Chuỗi kích thước thiết bị theo bản mẫu người dùng gửi 16/11 (hàng trùng toạ độ chỉ dim một lần, khác lưới mới dim thêm, dim sát thiết bị):
        /// lưới = cột (cùng X trong eps) có ≥ 2 thiết bị hoặc có thiết bị ở hàng chính (hàng đông nhất, trên cùng), hàng tương tự với cột chính (cột đông nhất, gần giữa vùng).
        /// MỘT chuỗi ngang tường – các cột lưới – tường dọc hàng chính, đường dim cách hàng 2 x cao; MỘT chuỗi đứng tường – các hàng lưới – tường dọc cột chính.
        /// Thiết bị lệch lưới: chuỗi riêng tường gần – thiết bị – cột (hàng) lưới gần nhất phía kia, sát thiết bị. cheo: thêm dim chéo tới góc lồi.
        /// Trả các dim chưa thêm vào bản vẽ (nơi gọi đặt layer, cỡ).
        static List<Dimension> KtLuoi(List<Point2d> vung, List<Point3d> diem, bool cheo, ObjectId kieu, double cao, double eps)
        {
            List<Dimension> kq = new List<Dimension>();
            if (diem.Count == 0) return kq;
            double ra = 2 * cao;
            List<Point2d> chuyen = vung.Select(p => new Point2d(p.Y, p.X)).ToList();
            List<List<Point3d>> hang = NhomToaDo(diem, false, eps), cot = NhomToaDo(diem, true, eps);
            List<Point3d> hangChinh = hang.OrderByDescending(h => h.Count).ThenByDescending(h => h[0].Y).First();
            double giua = (vung.Min(p => p.X) + vung.Max(p => p.X)) / 2;
            List<Point3d> cotChinh = cot.OrderByDescending(c => c.Count).ThenBy(c => Math.Abs(c[0].X - giua)).First();
            List<double> xs = cot.Where(c => c.Count >= 2 || hangChinh.Any(q => Math.Abs(q.X - c[0].X) <= eps)).Select(c => c[0].X).ToList();
            List<double> ys = hang.Where(h => h.Count >= 2 || cotChinh.Any(q => Math.Abs(q.Y - h[0].Y) <= eps)).Select(h => h[0].Y).ToList();
            kq.AddRange(ChuoiTuong(vung, xs, hangChinh[0].Y, hangChinh[0].X, true, ra, kieu, false));
            kq.AddRange(ChuoiTuong(chuyen, ys, cotChinh[0].X, cotChinh[0].Y, false, ra, kieu, false));
            // Hàng (cột) có thiết bị lệch lưới: ≥ 2 thiết bị (rải so le) thì một chuỗi cho cả hàng; một thiết bị thì chuỗi riêng tường – thiết bị – lưới.
            foreach (List<Point3d> h in hang)
            {
                if (h == hangChinh || h.All(p => xs.Any(x => Math.Abs(x - p.X) <= eps))) continue;
                if (h.Count >= 2) kq.AddRange(ChuoiTuong(vung, h.Select(p => p.X).ToList(), h[0].Y, h[0].X, true, ra, kieu, false));
                else kq.AddRange(ChuoiTuong(vung, xs, h[0].Y, h[0].X, true, ra, kieu, true));
            }
            foreach (List<Point3d> c in cot)
            {
                if (c == cotChinh || c.All(p => ys.Any(y => Math.Abs(y - p.Y) <= eps))) continue;
                if (c.Count >= 2) kq.AddRange(ChuoiTuong(chuyen, c.Select(p => p.Y).ToList(), c[0].X, c[0].Y, false, ra, kieu, false));
                else kq.AddRange(ChuoiTuong(chuyen, ys, c[0].X, c[0].Y, false, ra, kieu, true));
            }
            if (cheo)
                foreach (Point2d[] c in CheoGoc(vung, diem))
                {
                    if (c[0].GetDistanceTo(c[1]) <= 1e-6) continue;
                    Point3d a = new Point3d(c[1].X, c[1].Y, 0), b = new Point3d(c[0].X, c[0].Y, 0);
                    kq.Add(new AlignedDimension(a, b, new Point3d((a.X + b.X) / 2, (a.Y + b.Y) / 2, 0), "", kieu));
                }
            return kq;
        }

        /// Thiết bị gom theo X (doX, thành cột) hoặc Y (thành hàng): toạ độ liền nhau cách ≤ eps là một nhóm, phần tử đầu là mốc.
        static List<List<Point3d>> NhomToaDo(List<Point3d> diem, bool doX, double eps)
        {
            List<List<Point3d>> kq = new List<List<Point3d>>();
            foreach (Point3d p in diem.OrderBy(q => doX ? q.X : q.Y))
            {
                List<Point3d> cuoi = kq.Count > 0 ? kq[kq.Count - 1] : null;
                if (cuoi != null && (doX ? p.X - cuoi[cuoi.Count - 1].X : p.Y - cuoi[cuoi.Count - 1].Y) <= eps) cuoi.Add(p);
                else kq.Add(new List<Point3d> { p });
            }
            return kq;
        }

        /// Một chuỗi dim dọc đường "duong" (doX: đường ngang y = duong, toạ độ đo là X; không thì d là đa giác đã đổi X/Y) qua điểm có toạ độ t.
        /// le = false: tường – mọi toạ độ lưới trong khoảng – tường. le = true (thiết bị lệch lưới tại t): tường gần – t – toạ độ lưới gần nhất phía kia.
        static List<RotatedDimension> ChuoiTuong(List<Point2d> d, List<double> luoi, double duong, double t, bool doX, double ra, ObjectId kieu, bool le)
        {
            double[] k = KhoangNgang(d, duong).FirstOrDefault(x => x[0] <= t + 1e-6 && x[1] >= t - 1e-6);
            List<double> v = new List<double>();
            if (!le)
            {
                v.AddRange(k == null ? luoi : luoi.Where(x => x >= k[0] - 1e-6 && x <= k[1] + 1e-6));
                if (k != null) { v.Add(k[0]); v.Add(k[1]); }
            }
            else
            {
                v.Add(t);
                if (k != null)
                {
                    bool trai = t - k[0] <= k[1] - t; // tường gần ở phía nhỏ
                    v.Add(trai ? k[0] : k[1]);
                    IEnumerable<double> kia = trai ? luoi.Where(x => x > t && x <= k[1]) : luoi.Where(x => x < t && x >= k[0]);
                    if (kia.Any()) v.Add(trai ? kia.Min() : kia.Max());
                }
            }
            List<Point3d> moc = GomMoc(v.Select(x => doX ? new Point3d(x, duong, 0) : new Point3d(duong, x, 0)), doX, 1e-6);
            if (moc.Count < 2) return new List<RotatedDimension>();
            return ChuoiKt(moc, doX, doX ? new Point3d(moc[0].X, duong + ra, 0) : new Point3d(duong + ra, moc[0].Y, 0), kieu);
        }

        /// Dim tự động sau PCCC_RAI theo trang Cài đặt (kiểu dim, cách dim, layer). Trả số dim đã vẽ.
        static int KtSauRai(Database db, Transaction tr, BlockTableRecord ms, List<Point2d> vung, List<Point3d> diem)
        {
            string dang = CaiDat.DimDang();
            if (dang == CaiDat.DIM_KHONG || diem.Count == 0) return 0;
            DimStyleTable dst = (DimStyleTable)tr.GetObject(db.DimStyleTableId, OpenMode.ForRead);
            string ten = CaiDat.DimKieu();
            bool coKieu = ten != "" && dst.Has(ten);
            ObjectId kieu = coKieu ? dst[ten] : DamBaoKieuKt(db, tr); // chưa chọn: kiểu PCCC_KT như thao tác spec kt
            double cao = LaySo(NhomKyHieu("kt"), "cao", 250);
            ObjectId lop = DamBaoLayer(db, tr, CaiDat.DimLop(), 8);
            int n = 0;
            foreach (Dimension d in KtLuoi(vung, diem, dang == CaiDat.DIM_CHUOI_CHEO, kieu, cao, EPS_DIM_M / HeSoMet(db)))
            {
                d.SetDatabaseDefaults(db);
                d.DimensionStyle = kieu;
                if (!coKieu) d.Dimscale = cao / 2.5; // chưa chọn kiểu: cỡ chữ theo ky_hieu.json như thao tác kt
                d.LayerId = lop;
                ms.AppendEntity(d);
                tr.AddNewlyCreatedDBObject(d, true);
                n++;
            }
            return n;
        }

        /// Trị số rải theo tiêu chuẩn của mã thiết bị (data/khoang_cach_rai.json ở gốc repo, build.bat chép cạnh DLL; đơn vị m):
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
            double tuongMax = 0, cachMin = tc == null ? 0 : LaySo(tc, "tuong_min", 0) / m;
            // shortcut: thao tác spec chỉ rải điểm; tia chiếu (vẽ tia nối hai tường) mới có ở lệnh PCCC_RAI, khi AI cần thì chuyển TiaVung sang đây.
            if (sx <= 0 && bx <= 0 && tc != null && LayChu(tc, "kieu", "") == "tia")
                throw new System.Exception("mã " + ma + " là đầu báo tia chiếu: rải bằng lệnh PCCC_RAI (vẽ tia nối hai tường); thao tác rai chỉ rải điểm");
            if (sx <= 0 && bx <= 0 && tc != null) { sx = sy = D(Lay(tc, "s")) / m; tuongMax = LaySo(tc, "tuong", 0) / m; }
            if (sx <= 0 && bx <= 0)
                throw new System.Exception("rai cần \"s\" (bước tối đa) hoặc \"buoc\" (bước đúng) lấy theo quy chuẩn áp dụng: mã " + ma + " chưa có trị số trong data/khoang_cach_rai.json");
            bool kt = LayCo(o, "kt", false) && goc == 0;
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
                LuoiRai r = LuoiVung(d, sx, sy, bx, by, lex, ley, LaySo(o, "chan", 0.05 / m), dt, goc, LayCo(o, "theo_hang", false), tuongMax, cachMin);
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
                if (kt && r.Diem.Count > 0) KtRai(g, d, r);
                if (g.KqRai.Count < DOC_TOI_DA)
                    g.KqRai.Add(new Dictionary<string, object> { { "n", r.Diem.Count }, { "nx", r.Nx }, { "ny", r.Ny }, { "bx", Lt(r.Bx) }, { "by", Lt(r.By) },
                        { "tuong", new double[] { Lt(r.Tx), Lt(r.Ty) } }, { "m2", Math.Round(r.O * m * m, 2) }, { "ho", r.Ho },
                        { "cheo", Lt(CheoGoc(d, r.Diem).Select(c => c[0].GetDistanceTo(c[1])).DefaultIfEmpty(0).Max()) } });
            }
        }

        // ---------- Đi tuyến ----------

        /// Thứ tự rắn bò qua các điểm: gom thành hàng theo Y, đổi chiều mỗi hàng. Trong 4 góc xuất phát, lấy góc gần điểm "gan" nhất
        /// (không có thì đi từ trên xuống, từ trái sang).
        static List<Point3d> RanBo(List<Point3d> ps, Point3d? gan, double saiSo)
        {
            List<List<Point3d>> hang = Gom(ps, p => p.Y, saiSo);
            foreach (List<Point3d> h in hang) h.Sort((p, q) => p.X.CompareTo(q.X));
            List<Point3d> tot = null;
            double dTot = double.MaxValue;
            for (int k = 0; k < 4; k++)
            {
                bool tuTren = (k & 1) == 0, trai = (k & 2) == 0;
                List<Point3d> thu = new List<Point3d>();
                foreach (List<Point3d> h in tuTren ? Enumerable.Reverse(hang) : (IEnumerable<List<Point3d>>)hang)
                {
                    thu.AddRange(trai ? (IEnumerable<Point3d>)h : Enumerable.Reverse(h));
                    trai = !trai;
                }
                double d = gan.HasValue ? gan.Value.DistanceTo(thu[0]) : k;
                if (d < dTot) { dTot = d; tot = thu; }
            }
            return tot;
        }

        /// Dây nối tiếp: MỘT đường qua lần lượt mọi điểm, hết nhóm (phòng) này sang nhóm kế, mỗi nhóm bắt đầu từ góc gần điểm vừa tới.
        static List<List<Point3d>> TuyenNoiTiep(List<List<Point3d>> nhom, Point3d? tu, Point3d? den, double saiSo)
        {
            List<Point3d> d = new List<Point3d>();
            if (tu.HasValue) d.Add(tu.Value);
            foreach (List<Point3d> ps in nhom)
                if (ps.Count > 0) d.AddRange(RanBo(ps, d.Count > 0 ? d[d.Count - 1] : (Point3d?)null, saiSo));
            if (den.HasValue) d.Add(den.Value);
            List<Point3d> gon = new List<Point3d>();
            foreach (Point3d p in d)
                if (gon.Count == 0 || gon[gon.Count - 1].DistanceTo(p) > 1e-6) gon.Add(p);
            List<List<Point3d>> kq = new List<List<Point3d>>();
            if (gon.Count >= 2) kq.Add(gon);
            return kq;
        }

        /// Xương cá: MỘT ống góp vuông góc ống chính tới hàng xa nhất + mỗi hàng một nhánh song song ống chính, từ thiết bị ngoài cùng tới ống góp.
        /// Ống góp đi trùng cột thiết bị gần "tu" nhất (không có "tu" thì cột gần giữa), hoặc qua điểm "qua".
        /// Chỉ chọn cột có chân ống góp nằm trong đoạn ống chính; không cột nào được thì lấy cột chân gần đoạn nhất.
        /// Hoà thì lấy cột có chân X nhỏ hơn, rồi Y nhỏ hơn (WCS): đảo chiều vẽ "truc" không đổi kết quả.
        /// Chân ống góp (kể cả theo "qua") ra ngoài CẢ polyline ống chính thì nối thêm một đoạn về điểm gần nhất của ống chính.
        /// dem != null (PCCC_TUYEN TuDong): nhánh cắt tại từng đầu phun, ống góp cắt tại từng hàng; dem nhận, theo thứ tự đoạn, số đầu phun phía sau đoạn đó.
        static List<List<Point3d>> TuyenXuongCa(List<Point3d> ps, List<Point3d> truc, Point3d? tu, Point3d? qua, double saiSo, List<int> dem = null)
        {
            Point3d tam = new Point3d(ps.Average(p => p.X), ps.Average(p => p.Y), 0);
            int i0 = DoanGan(truc, tam);
            Point3d A = truc[i0], B = truc[i0 + 1];
            Vector3d u = (B - A).GetNormal(), n = new Vector3d(-u.Y, u.X, 0);
            Func<Point3d, double> fa = p => (p - A).DotProduct(u), fd = p => (p - A).DotProduct(n);
            Func<double, double> ngoai = x => (A + u * x).DistanceTo(ChanTruc(truc, A + u * x)); // chân ống góp cách ống chính (cả polyline) bao nhiêu
            double af;
            if (qua.HasValue) af = fa(qua.Value);
            else
            {
                double dich = tu.HasValue ? fa(tu.Value) : ps.Average(fa);
                List<double> cot = Gom(ps, fa, saiSo).Select(c => c.Average(fa)).ToList();
                bool coTrong = cot.Any(x => ngoai(x) <= saiSo);
                if (coTrong) cot = cot.Where(x => ngoai(x) <= saiSo).ToList();
                Func<double, double> lech = coTrong ? (Func<double, double>)(x => Math.Abs(x - dich)) : ngoai;
                double tot = cot.Min(lech);
                af = cot.Where(x => lech(x) <= tot + saiSo).OrderBy(x => Math.Round((A + u * x).X / saiSo)).ThenBy(x => (A + u * x).Y).First();
            }
            List<List<Point3d>> hang = Gom(ps, fd, saiSo), kq = new List<List<Point3d>>();
            double thap = Math.Min(0, hang.Min(h => h.Average(fd))), cao = Math.Max(0, hang.Max(h => h.Average(fd)));
            if (dem != null)
            {
                // Nước đi: ống chính -> chân ống góp -> ống góp (ra hai phía ống chính) -> nhánh (ra hai phía ống góp). Mỗi đoạn mang số đầu phun ở phía ngoài nó.
                Action<Point3d, Point3d, int> them = (p, q, c) => { if (p.DistanceTo(q) > 1e-6) { kq.Add(new List<Point3d> { p, q }); dem.Add(c); } };
                if (ngoai(af) > 1e-6) them(ChanTruc(truc, A + u * af), A + u * af, ps.Count);
                foreach (int sg in new[] { 1, -1 })
                {
                    List<List<Point3d>> hs = hang.Where(h => h.Average(fd) * sg > saiSo).OrderBy(h => Math.Abs(h.Average(fd))).ToList();
                    int con = hs.Sum(h => h.Count);
                    double truoc = 0;
                    foreach (List<Point3d> h in hs) { double d = h.Average(fd); them(A + u * af + n * truoc, A + u * af + n * d, con); con -= h.Count; truoc = d; }
                }
                foreach (List<Point3d> h in hang)
                {
                    double d = h.Average(fd);
                    foreach (int sg in new[] { 1, -1 })
                    {
                        // Đầu phun đứng ngay trên ống góp (lệch ≤ saiSo) nối thẳng vào ống góp, không thuộc nhánh nào.
                        List<double> x = h.Select(fa).Where(a => (a - af) * sg > saiSo).OrderBy(a => Math.Abs(a - af)).ToList();
                        double truoc = af;
                        for (int i = 0; i < x.Count; i++) { them(A + u * truoc + n * d, A + u * x[i] + n * d, x.Count - i); truoc = x[i]; }
                    }
                }
                return kq;
            }
            if (cao - thap > 1e-6) kq.Add(new List<Point3d> { A + u * af + n * thap, A + u * af + n * cao });
            if (ngoai(af) > 1e-6) kq.Add(new List<Point3d> { A + u * af, ChanTruc(truc, A + u * af) });
            foreach (List<Point3d> h in hang)
            {
                double d = h.Average(fd), a0 = Math.Min(h.Min(fa), af), a1 = Math.Max(h.Max(fa), af);
                if (a1 - a0 > 1e-6) kq.Add(new List<Point3d> { A + u * a0 + n * d, A + u * a1 + n * d });
            }
            return kq;
        }

        /// Mỗi cột thiết bị (cột = cùng vị trí dọc ống chính) một nhánh vuông góc ống chính, từ ống chính tới thiết bị xa nhất của cột
        /// (ống chính chạy sát hoặc xuyên qua phòng thì nhánh đi cả hai phía). Chân nhánh ra ngoài ống chính thì nối thêm như xuong_ca:
        /// các chân cùng phía cùng điểm nối chỉ một đoạn nối, từ chân xa nhất (không vẽ chồng).
        static List<List<Point3d>> TuyenCot(List<Point3d> ps, List<Point3d> truc, double saiSo)
        {
            Point3d tam = new Point3d(ps.Average(p => p.X), ps.Average(p => p.Y), 0);
            int i0 = DoanGan(truc, tam);
            Point3d A = truc[i0], B = truc[i0 + 1];
            Vector3d u = (B - A).GetNormal(), n = new Vector3d(-u.Y, u.X, 0);
            Func<Point3d, double> fa = p => (p - A).DotProduct(u), fd = p => (p - A).DotProduct(n);
            List<List<Point3d>> kq = new List<List<Point3d>>(), noi = new List<List<Point3d>>();
            foreach (List<Point3d> c in Gom(ps, fa, saiSo))
            {
                double x = c.Average(fa), thap = Math.Min(0, c.Min(fd)), cao = Math.Max(0, c.Max(fd));
                if (cao - thap > 1e-6) kq.Add(new List<Point3d> { A + u * x + n * thap, A + u * x + n * cao });
                Point3d F = A + u * x, T = ChanTruc(truc, F);
                if (F.DistanceTo(T) <= 1e-6) continue;
                // Cùng điểm nối T, cùng phía và T nằm trên đường trục (F, F' và T thẳng hàng): giữ đoạn dài hơn, đoạn dài phủ đoạn ngắn.
                // T ngoài đường trục quá saiSo (đỉnh của ống chính có bậc; bậc vẽ tay vài phần mm vẫn coi là trên trục) thì mỗi chân một đoạn nối, không thì nhánh ngắn bị treo.
                int j = Math.Abs(fd(T)) > saiSo ? -1 : noi.FindIndex(d => d[1].DistanceTo(T) <= 1e-6 && (d[0] - T).DotProduct(F - T) > 0);
                if (j < 0) noi.Add(new List<Point3d> { F, T });
                else if (F.DistanceTo(T) > noi[j][0].DistanceTo(T)) noi[j] = new List<Point3d> { F, T };
            }
            kq.AddRange(noi);
            return kq;
        }

        /// Mỗi điểm một đoạn vuông góc vào đoạn ống chính gần nhất (đầu phun hành lang, tủ vách tường).
        static List<List<Point3d>> TuyenNhanh(List<Point3d> ps, List<Point3d> truc)
        {
            List<List<Point3d>> kq = new List<List<Point3d>>();
            foreach (Point3d p in ps)
            {
                int i = DoanGan(truc, p);
                Point3d c = ChanVuongGoc(p, truc[i], truc[i + 1]);
                if (p.DistanceTo(c) > 1e-6) kq.Add(new List<Point3d> { p, c });
            }
            return kq;
        }

        /// Ghi chữ (vd DN50) dọc từng đoạn thẳng đủ dài, lệch về một phía của ống; chữ luôn đọc từ trái sang hoặc từ dưới lên.
        /// Bỏ chữ khi trong bán kính (cỡ chữ × số ký tự) đã có chữ cùng nội dung (daCo: điểm đặt các chữ đó, chữ mới ghi vào đây).
        static void NhanDoan(GoiSpec g, List<Point3d> d, string chu, double cao, ObjectId layer, List<Point3d> daCo)
        {
            for (int i = 0; i + 1 < d.Count; i++)
            {
                Vector3d v = d[i + 1] - d[i];
                if (v.Length < cao * chu.Length * 1.5) continue;
                double a = Math.Atan2(v.Y, v.X);
                if (a > Math.PI / 2 + 1e-9) a -= Math.PI;
                else if (a <= -Math.PI / 2 + 1e-9) a += Math.PI;
                Point3d giua = d[i] + v * 0.5 + new Vector3d(-Math.Sin(a), Math.Cos(a), 0) * (cao * 0.9);
                if (daCo.Any(p => p.DistanceTo(giua) < cao * chu.Length)) continue;
                daCo.Add(giua);
                Them(g, TaoChu(g, giua, chu, cao, a * 180 / Math.PI, true, layer));
            }
        }

        /// Điểm đặt mọi chữ có nội dung "chu" (không phân biệt hoa thường) đang có trong không gian vẽ.
        static List<Point3d> ChuCungNoiDung(GoiSpec g, string chu)
        {
            List<Point3d> kq = new List<Point3d>();
            foreach (ObjectId id in g.Ms)
            {
                if (id.ObjectClass.DxfName != "MTEXT" && id.ObjectClass.DxfName != "TEXT") continue;
                Entity e = g.Tr.GetObject(id, OpenMode.ForRead) as Entity;
                MText mt = e as MText;
                DBText t = e as DBText;
                string s = mt != null ? mt.Text : t != null ? t.TextString : null;
                if (s != null && string.Equals(s.Trim(), chu, StringComparison.OrdinalIgnoreCase)) kq.Add(mt != null ? mt.Location : t.Position);
            }
            return kq;
        }

        static Point3d? DiemTuyChon(GoiSpec g, Dictionary<string, object> o, string k)
        {
            return Lay(o, k) == null ? (Point3d?)null : P(g, Lay(o, k), k);
        }

        static void OpTuyen(GoiSpec g, Dictionary<string, object> o)
        {
            string ma = LayChu(o, "ma", ""), kieu = LayChu(o, "kieu", "noi_tiep");
            VatTu v = TimMa(g.Dm, ma);
            if (v == null || v.loai != "tuyen") throw new System.Exception("mã ống/dây không có trong danh mục: " + ma);
            List<List<Point3d>> nhom = new List<List<Point3d>>();
            List<List<Point2d>> vungNhom = new List<List<Point2d>>(); // đa giác vùng của từng nhóm; null khi điểm lấy từ "p"
            if (Lay(o, "p") != null) { nhom.Add(DsP(g, Lay(o, "p"), "p")); vungNhom.Add(null); }
            else if (g.DiemRai != null)
                for (int j = 0; j < g.DiemRai.Count; j++)
                    if (g.DiemRai[j].Count > 0) { nhom.Add(g.DiemRai[j]); vungNhom.Add(g.VungRai[j]); }
            if (nhom.Count == 0) throw new System.Exception("tuyen cần \"p\" [[x,y],...] hoặc đứng ngay sau một thao tác rai có thiết bị");
            List<Point3d> truc = Lay(o, "truc") == null ? null : DsP(g, Lay(o, "truc"), "truc");
            if ((kieu == "xuong_ca" || kieu == "nhanh" || kieu == "cot") && (truc == null || truc.Count < 2))
                throw new System.Exception("tuyen kiểu \"" + kieu + "\" cần \"truc\" [[x,y],[x,y],...] là ống chính");
            Point3d? tu = DiemTuyChon(g, o, "tu"), den = DiemTuyChon(g, o, "den");
            double m = HeSoMet(g.Db), saiSo = LaySo(o, "sai_so", 0.1 / m);
            // "qua": một hoặc nhiều điểm; mỗi điểm dùng cho vùng chứa nó (điểm lấy từ "p" thì một nhóm, nhận mọi điểm).
            Point3d?[] qua = new Point3d?[nhom.Count];
            if (Lay(o, "qua") != null)
                foreach (Point3d q in DsP(g, Lay(o, "qua"), "qua"))
                {
                    int j = -1;
                    for (int k = 0; k < nhom.Count && j < 0; k++) if (vungNhom[k] == null || TrongDaGiac(vungNhom[k], new Point2d(q.X, q.Y))) j = k;
                    if (j < 0) throw new System.Exception(string.Format(CultureInfo.InvariantCulture, "điểm \"qua\" [{0},{1}] không nằm trong vùng nào của rai trước đó", Lt(q.X - g.Gx), Lt(q.Y - g.Gy)));
                    if (qua[j].HasValue) throw new System.Exception("hai điểm \"qua\" cùng nằm trong một vùng: mỗi vùng một điểm");
                    qua[j] = q;
                }

            List<List<Point3d>> doan = new List<List<Point3d>>();
            if (kieu == "noi_tiep") doan = TuyenNoiTiep(nhom, tu, den, saiSo);
            else if (kieu == "xuong_ca") for (int j = 0; j < nhom.Count; j++) doan.AddRange(TuyenXuongCa(nhom[j], truc, tu, qua[j], saiSo));
            else if (kieu == "nhanh") foreach (List<Point3d> ps in nhom) doan.AddRange(TuyenNhanh(ps, truc));
            else if (kieu == "cot") foreach (List<Point3d> ps in nhom) doan.AddRange(TuyenCot(ps, truc, saiSo));
            else if (kieu == "tia")
            {
                // Đèn báo phòng: phòng có bao nhiêu đầu báo thì bấy nhiêu nét thẳng từ đầu báo ra đèn (cad/knowledge/tu-duy-di-day-di-ong.md).
                if (!den.HasValue) throw new System.Exception("tuyen kiểu \"tia\" cần \"den\" [x,y] (vd vị trí đèn báo phòng)");
                foreach (List<Point3d> ps in nhom)
                    foreach (Point3d p in ps)
                        if (p.DistanceTo(den.Value) > 1e-6) doan.Add(new List<Point3d> { p, den.Value });
            }
            else throw new System.Exception("tuyen: \"kieu\" phải là noi_tiep, tia, xuong_ca, cot hoặc nhanh");

            ObjectId layer = DamBaoLayer(g.Db, g.Tr, TIEN_TO_LAYER + v.ma, MauHe(v.he));
            bool nhan = LayCo(o, "nhan", false);
            ObjectId lNhan = nhan ? DamBaoLayer(g.Db, g.Tr, LAYER_NHAN, 7) : ObjectId.Null;
            string chu = v.ma.StartsWith("ONG_", StringComparison.OrdinalIgnoreCase) ? v.ma.Substring(4) : v.ma;
            double caoChu = LaySo(o, "cao", LaySo(NhomKyHieu("chu"), "cao", 250)), dai = 0;
            List<Point3d> daCo = nhan ? ChuCungNoiDung(g, chu) : null;
            foreach (List<Point3d> d in doan)
            {
                Polyline pl = TaoPline(d, false, layer);
                Them(g, pl);
                dai += pl.Length;
                if (nhan) NhanDoan(g, d, chu, caoChu, lNhan, daCo);
            }
            g.KqTuyen.Add(new Dictionary<string, object> { { "kieu", kieu }, { "doan", doan.Count }, { "m", Math.Round(dai * m, 2) } });
        }

        // ---------- Lệnh gõ tay ----------

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
            double m = HeSoMet(db), s, k = 0.001 / m, dt = 0, le = -1, tuongMax = 0, cachMin = 0;
            bool xem = false, tia = false, khoangDam = false;
            double hTran = 0;
            Dictionary<string, object> tc = TieuChuanRai(v.ma, 0);
            if (tc != null)
            {
                // Mã có trị số tiêu chuẩn (data/khoang_cach_rai.json): không hỏi khoảng cách hay m2, chỉ hỏi chiều cao trần để chọn dòng.
                // Ý lấy từ addin mẫu (14/11), từ khoá ở cùng dấu nhắc: HeSo = mức tối ưu k 50–100 % (bước và cách tường tối đa nhân k, k < 100 % là rải dày hơn);
                // Dam = trần có dầm sâu > 0,3 m (dòng có "dam" thì bước nhân he_so); XemTruoc = chỉ đếm, không vẽ.
                int heSo = 100;
                bool coDam = false;
                PromptDoubleResult hcr;
                while (true)
                {
                    string them = (heSo < 100 ? ", k = " + heSo + " %" : "") + (coDam ? ", có dầm" : "") + (xem ? ", xem trước" : "");
                    PromptDoubleOptions hc = new PromptDoubleOptions("\nChiều cao trần (m), Enter = không xét" + (them != "" ? " (" + them.Substring(2) + ")" : ""));
                    hc.Keywords.Add("HeSo"); // AutoCAD tự nối " [HeSo/Dam/XemTruoc]: "
                    hc.Keywords.Add("Dam");
                    hc.Keywords.Add("XemTruoc");
                    hc.AllowNegative = false; hc.AllowZero = false; hc.AllowNone = true;
                    hcr = ed.GetDouble(hc);
                    if (hcr.Status != PromptStatus.Keyword) break;
                    if (hcr.StringResult == "Dam") { coDam = true; continue; }
                    if (hcr.StringResult == "XemTruoc") { xem = true; continue; }
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
                cachMin = LaySo(tc, "tuong_min", 0) / m;
                tia = LayChu(tc, "kieu", "") == "tia";
                ed.WriteMessage("\n[TKPCCC] " + v.ma + " theo " + LayChu(tc, "can_cu", "") + ": " + (tia ? "khoảng cách giữa các tia " : "bước tối đa ")
                    + Lt(s * m).ToString(CultureInfo.InvariantCulture) + " m, cách tường tối đa " + Lt(tuongMax * m).ToString(CultureInfo.InvariantCulture) + " m"
                    + (heSo < 100 ? " (k = " + heSo + " %)" : "") + ".");
                if (coDam)
                {
                    Dictionary<string, object> dam = Lay(tc, "dam") as Dictionary<string, object>;
                    if (dam != null)
                    {
                        s *= LaySo(dam, "he_so", 1);
                        ed.WriteMessage("\n[TKPCCC] Trần có dầm sâu > 0,3 m, theo " + LayChu(dam, "can_cu", "") + ": bước tối đa còn "
                            + Lt(s * m).ToString(CultureInfo.InvariantCulture) + " m.");
                    }
                    else
                    {
                        ed.WriteMessage("\n[TKPCCC] " + v.ma + ": tiêu chuẩn không giảm bước khi có dầm. Đầu báo khói kiểu điểm xét từng ô giữa dầm theo TCVN 7568-14:2025 5.9.1.1.6"
                            + " (skills/MOD_dau_bao_khoi_khoang_dam.md): bấm nét dầm ở bước nét DẦM mẫu; độ sâu dầm lấy từ ký hiệu b x h (hỏi nếu không có), dầm ≤ 0,3 m bỏ;"
                            + " khoang ≥ ngưỡng (9 m² trần ≥ 4 m, 4 m² trần 2–4 m) rải từng khoang, khoang nhỏ hơn rải chung như trần phẳng (lắp ở mặt dưới dầm).");
                        khoangDam = true;
                        hTran = hcr.Status == PromptStatus.OK ? hcr.Value : 0;
                    }
                }
            }
            else
            {
                // Mã chưa có trị số tiêu chuẩn (sprinkler...): người thiết kế nhập theo TCVN đang áp dụng.
                PromptDoubleOptions so = new PromptDoubleOptions("\nBước tối đa S giữa hai thiết bị (m) theo tiêu chuẩn áp dụng: ");
                so.AllowNegative = false; so.AllowZero = false;
                PromptDoubleResult sr = ed.GetDouble(so);
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
            if (khoangDam)
            {
                damMau = new HashSet<string>();
                while (true)
                {
                    PromptNestedEntityOptions no = new PromptNestedEntityOptions(damMau.Count == 0
                        ? "\nBấm một nét DẦM mẫu (layer + màu của dầm), Enter = không có: " : "\nBấm nét dầm màu khác, Enter = xong: ");
                    no.AllowNone = true;
                    PromptNestedEntityResult nr = ed.GetNestedEntity(no);
                    if (nr.Status == PromptStatus.None) break;
                    if (nr.Status != PromptStatus.OK) return;
                    string kh = KhoaNetMau(db, nr);
                    mau.Remove(kh); // nét dầm không dựng làm tường
                    if (damMau.Add(kh)) ed.WriteMessage("\n[TKPCCC] Nét dầm mẫu: layer " + kh.Replace("|", ", màu ") + ".");
                }
                List<KeyValuePair<Point2d, double>> nhan = NhanDam(qr.Value.GetObjectIds());
                double sauChung = -1;
                if (damMau.Count == 0 || nhan.Count == 0)
                {
                    PromptStringOptions so = new PromptStringOptions(damMau.Count > 0
                        ? "\nKhông thấy ký hiệu dầm b x h (vd B1 (400x600)) trên mặt bằng. Nhập b x h (mm) cho cả mặt bằng, hoặc TungDam = chọn line dọc dầm rồi nhập từng cỡ: "
                        : "\nChưa có nét dầm mẫu. TungDam = chọn line dọc dầm bạn vẽ rồi nhập b x h; Enter = bỏ qua dầm (rải như trần phẳng): ");
                    so.AllowSpaces = false;
                    PromptResult sr = ed.GetString(so);
                    if (sr.Status != PromptStatus.OK) return;
                    string tl = sr.StringResult.Trim();
                    if (tl.Equals("TungDam", StringComparison.OrdinalIgnoreCase) || tl.Equals("T", StringComparison.OrdinalIgnoreCase))
                    {
                        damThem = new List<Point2d[]>();
                        while (true)
                        {
                            PromptSelectionOptions lo = new PromptSelectionOptions();
                            lo.MessageForAdding = "\nChọn line / polyline dọc dầm cùng một cỡ, Enter = xong: ";
                            PromptSelectionResult lr = ed.GetSelection(lo, new SelectionFilter(new[] { new TypedValue((int)DxfCode.Start, "LINE,LWPOLYLINE") }));
                            if (lr.Status != PromptStatus.OK) break;
                            PromptResult cr = ed.GetString("\nKích thước dầm b x h (mm) của các line này, vd 400x600: ");
                            if (cr.Status != PromptStatus.OK) return;
                            double d = DocBxh(cr.StringResult);
                            if (d <= 0) { ed.WriteMessage("\nKhông đọc được b x h \"" + cr.StringResult + "\": chọn lại."); continue; }
                            int truoc = damThem.Count;
                            foreach (ObjectId id in lr.Value.GetObjectIds())
                            {
                                idDam.Add(id);
                                if (d > 0.3 + 1e-9) damThem.AddRange(DoanNet(db, id));
                            }
                            ed.WriteMessage("\n[TKPCCC] tung_dam d=" + d.ToString(CultureInfo.InvariantCulture) + " m: " + lr.Value.Count + " nét"
                                + (d > 0.3 + 1e-9 ? " (sâu, chia khoang " + (damThem.Count - truoc) + " đoạn)" : " (≤ 0,3 m: bỏ, trần phẳng)"));
                        }
                    }
                    else if (tl != "")
                    {
                        sauChung = DocBxh(tl);
                        if (sauChung <= 0) { ed.WriteMessage("\nKhông đọc được b x h \"" + tl + "\"."); return; }
                    }
                }
                giuDam = dn =>
                {
                    double gan = double.MaxValue, d = -1;
                    foreach (KeyValuePair<Point2d, double> kv in nhan) { double c = CachDoan(dn, kv.Key); if (c < gan) { gan = c; d = kv.Value; } }
                    if (gan <= 10 / m) return d > 0.3 + 1e-9;               // ký hiệu gần nhất trong 10 m
                    return sauChung > 0 ? sauChung > 0.3 + 1e-9 : true;     // không ký hiệu gần, không cỡ chung: coi là dầm sâu (chia khoang, rải dày hơn)
                };
                ed.WriteMessage("\n[TKPCCC] dam ky_hieu=" + nhan.Count + (sauChung > 0 ? " d_chung=" + sauChung.ToString(CultureInfo.InvariantCulture) : "")
                    + (damThem != null ? " tung_dam=" + damThem.Count : ""));
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
            MatChon(db, conLai, mau.Count > 0 ? mau : null, damMau, giuDam, damThem, out vung, out mat, out oX, out gach, out demDam);
            ed.WriteMessage("\n[TKPCCC] thoi_gian dung_phong=" + dongHo.ElapsedMilliseconds + "ms doi_tuong=" + conLai.Length + " mat=" + mat.Count);
            if (khoangDam && demDam[0] + demDam[1] > 0)
                ed.WriteMessage("\n[TKPCCC] dam_sau=" + demDam[0] + " dam_nong=" + demDam[1] + " (dầm nông d ≤ 0,3 m bỏ, rải như trần phẳng: 5.9.1.1.6 a)");
            if (oX.Count > 0)
            {
                ed.WriteMessage("\n[TKPCCC] o_x=" + oX.Count + " (ô có dấu X = lỗ thông tầng: không rải).");
                loaiTru.AddRange(oX);
            }
            if (gach.Count > 0) ed.WriteMessage("\n[TKPCCC] gach_cheo=" + gach.Count + " (hatch gạch chéo = công trình hiện hữu: Enter mọi phòng thì bỏ qua).");
            bool veVung = vung.Count == 0;
            HashSet<List<Point2d>> daCo = new HashSet<List<Point2d>>(); // vùng PCCC_VUNG vẽ sẵn dùng thay phòng tự dựng: không vẽ lại
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
                        vung = pt.Where(f0 => !MatKhongRai(f0, oX, gach)).ToList();
                        if (pt.Count > vung.Count) ed.WriteMessage("\n[TKPCCC] bo_phong=" + (pt.Count - vung.Count) + " (ô X, gạch chéo).");
                        int boPhong, boCum;
                        vung = CumChinh(vung, 2 / m, out boPhong, out boCum);
                        if (boPhong > 0) ed.WriteMessage("\n[TKPCCC] bo_cum=" + boCum + " phong=" + boPhong + " (cụm phòng nhỏ tách xa mặt bằng chính: hình chi tiết, mặt cắt; cần rải thì bấm điểm trong phòng).");
                        break;
                    }
                    if (pr.Status != PromptStatus.OK) break;
                    Point3d q = pr.Value.TransformBy(ed.CurrentUserCoordinateSystem);
                    List<Point2d> f = mat.Where(x => TrongDaGiac(x, new Point2d(q.X, q.Y))).OrderBy(x => DienTichDaGiac(x)).FirstOrDefault();
                    if (f == null) ed.WriteMessage("\nĐiểm không nằm trong phòng kín nào của vùng đã chọn.");
                    else if (MatKhongRai(f, oX, new List<List<Point2d>>())) ed.WriteMessage("\nÔ có dấu X (lỗ thông tầng): không rải.");
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
                List<List<Point2d>> coSan = VungCoSan(db);
                if (coSan.Count > 0)
                {
                    vung = TheoVungCoSan(vung, coSan, daCo);
                    if (daCo.Count > 0) ed.WriteMessage("\n[TKPCCC] theo_vung_co_san=" + daCo.Count + " (vùng " + LAYER_VUNG + " vẽ sẵn trong phòng: rải theo vùng đó, không vẽ chồng).");
                }
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
                nho = vung.Where(d => DienTichDaGiac(d) * m * m < nguong).ToList();
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
                ObjectId lop = veVung && !xem ? DamBaoLayer(db, tr, LAYER_VUNG, 6) : ObjectId.Null;
                ObjectId lopTia = tia && !xem ? DamBaoLayer(db, tr, LAYER_TIA, MauHe(v.he)) : ObjectId.Null;
                string duoi = xem ? " (xem trước, chưa vẽ)" : "";
                // Xem trước (spec addin mẫu): vẽ tạm điểm + vòng R hiệu dụng trên layer không in; lượt rải thật hay xem lại thì xoá bản tạm cũ.
                int xoaTam = XoaXemTruoc(db, tr, ms);
                if (xoaTam > 0 && !xem) ed.WriteMessage("\n[TKPCCC] xoa_xem_truoc=" + xoaTam);
                ObjectId lopTam = xem ? LopXemTruoc(db, tr) : ObjectId.Null;
                double rTam = tc == null ? 0 : LaySo(tc, "r", 0) * s / D(Lay(tc, "s")); // R tiêu chuẩn (m) nhân cùng tỉ lệ với bước (k, dầm, đổi đơn vị)
                int soTam = 0;
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
                    try { r = LuoiVung(d, s, s, 0, 0, le, le, 0.05 / m, dt, 0, theoHang, tuongMax, cachMin); }
                    catch (System.Exception ex)
                    {
                        soBo++;
                        ed.WriteMessage("\n[TKPCCC] bo_vung m2=" + Math.Round(DienTichDaGiac(d) * m * m, 2).ToString(CultureInfo.InvariantCulture) + ": " + ex.Message);
                        continue;
                    }
                    if (veVung && !xem)
                        foreach (List<Point2d> dv in laHopNho ? nhoD : new List<List<Point2d>> { d })
                        {
                            if (daCo.Contains(dv)) continue;
                            Polyline pl = PlVung(dv, lop);
                            ms.AppendEntity(pl);
                            tr.AddNewlyCreatedDBObject(pl, true);
                            ed.WriteMessage("\n[TKPCCC] Vùng tự dựng m2=" + Math.Round(DienTichDaGiac(dv) * m * m, 2).ToString(CultureInfo.InvariantCulture)
                                + " (" + dv.Count + " đỉnh) trên layer " + LAYER_VUNG + ".");
                        }
                    if (laHopNho)
                    {
                        int ngoai = r.Diem.RemoveAll(p => !nhoD.Any(q => TrongDaGiac(q, new Point2d(p.X, p.Y)) || CachVien(q, new Point2d(p.X, p.Y)) <= 0.6 / m));
                        // Lưới tính trên hộp bao, đã lọc: kiểm phủ lại trên chính các khoang, chỗ hở thì thêm đầu báo.
                        double h0 = tuongMax > 0 ? Math.Min(s / 2, tuongMax) : s / 2;
                        int bu = PhuBu(nhoD, r.Diem, h0, h0);
                        r.Ho = 0;
                        ed.WriteMessage("\n[TKPCCC] khoang_nho_chung n=" + r.Diem.Count + (ngoai > 0 ? " bo_ngoai=" + ngoai : "") + (bu > 0 ? " bu=" + bu : ""));
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
                        soKtRai = KtSauRai(db, tr, ms, d, r.Diem);
                    }
                    else foreach (Point3d p in r.Diem)
                    {
                        soTam += VeTam(tr, ms, new Circle(p, Vector3d.ZAxis, 0.15 / m), lopTam);
                        if (rTam > 0) soTam += VeTam(tr, ms, new Circle(p, Vector3d.ZAxis, rTam), lopTam);
                    }
                    tong += r.Diem.Count;
                    if (r.Ho > 0) soHo++;
                    ed.WriteMessage("\n[TKPCCC] PCCC_RAI " + v.ma + " n=" + r.Diem.Count + " luoi=" + r.Nx + "x" + r.Ny
                        + " buoc=" + Lt(r.Bx).ToString(CultureInfo.InvariantCulture) + "x" + Lt(r.By).ToString(CultureInfo.InvariantCulture)
                        + " m2=" + Math.Round(r.O * m * m, 2).ToString(CultureInfo.InvariantCulture)
                        + (r.Ho > 0 ? " HO=" + r.Ho.ToString(CultureInfo.InvariantCulture) + "% (vùng chưa phủ hết, kiểm lại)" : "")
                        + (soKtRai > 0 ? " kt=" + soKtRai : "") + duoi);
                }
                } finally { tienDo.Stop(); tienDo.Dispose(); }
                ed.WriteMessage("\n[TKPCCC] thoi_gian rai=" + dongHo.ElapsedMilliseconds + "ms vung=" + vungRai.Count);
                if (xem) ed.WriteMessage("\n[TKPCCC] xem_truoc=" + soTam + " (vẽ tạm trên layer " + LAYER_XEM_TRUOC + ", không in, tự xoá khi rải thật)");
                // Ý từ video VPTTOOL 16/11: ghi m² từng khoang cho người thẩm duyệt kiểm; tắt ở tab Khoang dầm (cai_dat.json "kd_ghi_m2").
                if (khoangDam && !tia && !xem && CaiDat.Doc("kd_ghi_m2", "Co") != "Khong")
                {
                    ghi = GhiKhoang(db, tr, ms, vung, nho, m);
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

        const string LAYER_KHOANG = "PCCC_KHOANG_M2"; // chữ m² từng khoang dầm + dấu X khoang nhỏ; in được, xoá cả layer nếu không cần

        /// Ghi diện tích giữa từng khoang (dưới tâm, không đè đầu báo ở tâm); khoang dưới ngưỡng 5.9.1.1.6 (c, e) thêm dấu X hồng ở tâm.
        /// Khoang < 1 m² (khe giữa dầm đôi, lõi cột) không ghi. Trả { số khoang ghi, số dấu X, số khoang bỏ }.
        static int[] GhiKhoang(Database db, Transaction tr, BlockTableRecord ms, List<List<Point2d>> vung, List<List<Point2d>> nho, double m)
        {
            int[] n = new int[3];
            double cao = LaySo(NhomKyHieu("chu"), "cao", 250) * 0.001 / m;
            ObjectId lop = DamBaoLayer(db, tr, LAYER_KHOANG, 6);
            foreach (List<Point2d> d in vung)
            {
                double s = DienTichDaGiac(d) * m * m;
                if (s < 1) { n[2]++; continue; }
                Point2d c = DiemTrongMat(d);
                if (nho.Contains(d))
                {
                    double r = Math.Min(Math.Min(d.Max(p => p.X) - d.Min(p => p.X), d.Max(p => p.Y) - d.Min(p => p.Y)) * 0.2, 0.4 / m);
                    foreach (int sg in new[] { 1, -1 })
                    {
                        Line l = new Line(new Point3d(c.X - r, c.Y - sg * r, 0), new Point3d(c.X + r, c.Y + sg * r, 0));
                        l.LayerId = lop;
                        ms.AppendEntity(l);
                        tr.AddNewlyCreatedDBObject(l, true);
                    }
                    n[1]++;
                }
                Point2d o = new Point2d(c.X, c.Y - 2 * cao);
                MText t = new MText();
                t.Contents = Math.Round(s, 1).ToString("0.0", CultureInfo.InvariantCulture).Replace('.', ',') + " m²";
                t.TextHeight = cao;
                t.Attachment = AttachmentPoint.MiddleCenter;
                t.Location = TrongDaGiac(d, o) ? new Point3d(o.X, o.Y, 0) : new Point3d(c.X, c.Y, 0); // khoang hẹp: về tâm
                t.LayerId = lop;
                ms.AppendEntity(t);
                tr.AddNewlyCreatedDBObject(t, true);
                n[0]++;
            }
            return n;
        }

        const string LAYER_TIA = "PCCC_BC_TIA";
        const string LAYER_LOAI_TRU = "PCCC_LOAITRU";   // polyline kín: lỗ thang, giếng trời... không rải trong đó
        const string LAYER_XEM_TRUOC = "PCCC_XEM_TRUOC"; // bản vẽ tạm của XemTruoc, không in

        /// Tách polyline kín layer PCCC_LOAITRU khỏi vùng chọn (không đưa vào dựng phòng); trả đa giác loại trừ, conLai = các id còn lại.
        static List<List<Point2d>> TachLoaiTru(Database db, ObjectId[] ids, out ObjectId[] conLai)
        {
            List<List<Point2d>> kq = new List<List<Point2d>>();
            List<ObjectId> con = new List<ObjectId>();
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                foreach (ObjectId id in ids)
                {
                    Entity e = tr.GetObject(id, OpenMode.ForRead) as Entity;
                    Polyline pl = e as Polyline;
                    Circle ci = e as Circle;
                    if (e != null && (pl != null || ci != null) && string.Equals(e.Layer, LAYER_LOAI_TRU, StringComparison.OrdinalIgnoreCase))
                    {
                        if (ci != null) kq.Add(DaGiacTron(ci, 72));
                        else if (KinPl(pl)) kq.Add(DinhPl(pl));
                    }
                    else con.Add(id);
                }
                tr.Commit();
            }
            conLai = con.ToArray();
            return kq;
        }

        /// Ống gió (spec addin mẫu "vật cản trần"): polyline kín, vòng tròn trên layer tên có ONG_GIO, ONG-GIO, ONGGIO hoặc DUCT. Tách khỏi vùng chọn
        /// (không đưa vào dựng phòng); trả đa giác ống. shortcut: nhận ống theo tên layer cố định; bản vẽ đặt tên khác thì thêm từ khoá vào DUOI_ONG_GIO.
        static readonly string[] DUOI_ONG_GIO = { "ONG_GIO", "ONG-GIO", "ONGGIO", "DUCT" };
        static List<List<Point2d>> TachOngGio(Database db, ObjectId[] ids, out ObjectId[] conLai)
        {
            List<List<Point2d>> kq = new List<List<Point2d>>();
            List<ObjectId> con = new List<ObjectId>();
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                foreach (ObjectId id in ids)
                {
                    Entity e = tr.GetObject(id, OpenMode.ForRead) as Entity;
                    Polyline pl = e as Polyline;
                    Circle ci = e as Circle;
                    string lop = e == null ? "" : BoDau(e.Layer).ToUpperInvariant();
                    if ((pl != null || ci != null) && DUOI_ONG_GIO.Any(k => lop.Contains(k)))
                    {
                        if (ci != null) kq.Add(DaGiacTron(ci, 72));
                        else if (KinPl(pl)) kq.Add(DinhPl(pl));
                    }
                    else con.Add(id);
                }
                tr.Commit();
            }
            conLai = con.ToArray();
            return kq;
        }

        /// Điểm p trong đa giác q: dịch theo hướng tới cạnh gần nhất, qua biên thêm "qua". null = đa giác suy biến.
        static Point2d? DichRaNgoai(List<Point2d> q, Point2d p, double qua)
        {
            Point2d? gan = null;
            double dMin = double.MaxValue;
            for (int i = 0; i < q.Count; i++)
            {
                Point2d a = q[i], b = q[(i + 1) % q.Count];
                Vector2d ab = b - a;
                double t = ab.LengthSqrd < 1e-12 ? 0 : Math.Max(0, Math.Min(1, (p - a).DotProduct(ab) / ab.LengthSqrd));
                Point2d c = a + ab * t;
                double dd = c.GetDistanceTo(p);
                if (dd < dMin) { dMin = dd; gan = c; }
            }
            if (!gan.HasValue || dMin < 1e-9) return null;
            Vector2d v = (gan.Value - p).GetNormal();
            return gan.Value + v * qua;
        }

        static ObjectId LopXemTruoc(Database db, Transaction tr)
        {
            ObjectId lop = DamBaoLayer(db, tr, LAYER_XEM_TRUOC, 3);
            ((LayerTableRecord)tr.GetObject(lop, OpenMode.ForWrite)).IsPlottable = false;
            return lop;
        }

        static int VeTam(Transaction tr, BlockTableRecord ms, Entity e, ObjectId lop)
        {
            e.LayerId = lop;
            ms.AppendEntity(e);
            tr.AddNewlyCreatedDBObject(e, true);
            return 1;
        }

        /// Xoá mọi đối tượng layer PCCC_XEM_TRUOC trong không gian đang vẽ; trả số đã xoá.
        static int XoaXemTruoc(Database db, Transaction tr, BlockTableRecord ms)
        {
            LayerTable lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
            if (!lt.Has(LAYER_XEM_TRUOC)) return 0;
            ObjectId lop = lt[LAYER_XEM_TRUOC];
            int n = 0;
            foreach (ObjectId id in ms)
            {
                Entity e = tr.GetObject(id, OpenMode.ForRead) as Entity;
                if (e != null && e.LayerId == lop) { e.UpgradeOpen(); e.Erase(); n++; }
            }
            return n;
        }

        /// Đầu báo tia chiếu (5.9.1.3.2): các tia song song cạnh dài của khung bao vùng (ít tia, mỗi tia dài), cách nhau ≤ s, tia ngoài cùng cách tường
        /// nửa bước (≤ s / 2; dòng BC_BEAM có tuong = s / 2 nên đủ điều kiện cách tường). Mỗi hàng cắt vùng thành các đoạn trong phòng; hai đầu lùi vào "vao".
        /// shortcut: hàng chia theo khung bao, phòng chữ L có thể có góc xa tia hơn s / 2 theo phương vuông góc; khi cần thì chia phòng bằng nét phân vùng.
        static List<Point2d[]> TiaVung(List<Point2d> d, double s, double vao, out double b, out double t)
        {
            double x0 = d.Min(p => p.X), x1 = d.Max(p => p.X), y0 = d.Min(p => p.Y), y1 = d.Max(p => p.Y);
            bool doc = y1 - y0 > x1 - x0;
            List<Point2d> q = doc ? d.Select(p => new Point2d(p.Y, p.X)).ToList() : d;
            int n;
            ChiaBuoc(doc ? x1 - x0 : y1 - y0, s, 0, -1, 0, 1, out n, out b, out t);
            List<Point2d[]> kq = new List<Point2d[]>();
            for (int i = 0; i < n; i++)
            {
                double y = (doc ? x0 : y0) + t + i * b;
                foreach (double[] kh in KhoangNgang(q, y))
                {
                    if (kh[1] - kh[0] <= 2 * vao) continue;
                    Point2d a = new Point2d(kh[0] + vao, y), c = new Point2d(kh[1] - vao, y);
                    kq.Add(doc ? new[] { new Point2d(a.Y, a.X), new Point2d(c.Y, c.X) } : new[] { a, c });
                }
            }
            return kq;
        }

        const string LAYER_TIA_KT = "PCCC_BC_TIA_KT";

        /// Chuỗi kích thước bước tia như bộ bản vẽ mẫu (cad/knowledge/hoc-ban-ve-mau-dwg.md mục 2): tường – tia – … – tia – tường, đo vuông góc tia,
        /// đặt ngang giữa tia dài nhất, trong khoảng của phòng chứa nhiều tia nhất. Trả số dim.
        static int KtTia(Database db, Transaction tr, BlockTableRecord ms, List<Point2d> d, List<Point2d[]> ts, ObjectId lop)
        {
            if (ts.Count == 0) return 0;
            bool doc = Math.Abs(ts[0][1].X - ts[0][0].X) < Math.Abs(ts[0][1].Y - ts[0][0].Y); // tia dọc: đo theo X
            Func<Point2d, double> doc0 = p => doc ? p.X : p.Y, ngang = p => doc ? p.Y : p.X; // toạ độ theo phương đo / theo phương tia
            Point2d[] dai = ts.OrderByDescending(a => a[0].GetDistanceTo(a[1])).First();
            double c = (ngang(dai[0]) + ngang(dai[1])) / 2;
            List<double> v = ts.Where(a => Math.Min(ngang(a[0]), ngang(a[1])) <= c && Math.Max(ngang(a[0]), ngang(a[1])) >= c).Select(a => doc0(a[0])).ToList();
            List<Point2d> q = doc ? d : d.Select(p => new Point2d(p.Y, p.X)).ToList();
            double[] kh = KhoangNgang(q, c).OrderByDescending(k => v.Count(x => x > k[0] && x < k[1])).FirstOrDefault();
            if (kh == null) return 0;
            List<double> moc = new List<double> { kh[0] };
            moc.AddRange(v.Where(x => x > kh[0] && x < kh[1]).OrderBy(x => x));
            moc.Add(kh[1]);
            if (moc.Count < 3) return 0;
            List<Point3d> ps = moc.Select(x => doc ? new Point3d(x, c, 0) : new Point3d(c, x, 0)).ToList();
            int n = 0;
            foreach (RotatedDimension dm in ChuoiKt(ps, doc, ps[0], db.Dimstyle))
            {
                dm.LayerId = lop;
                ms.AppendEntity(dm);
                tr.AddNewlyCreatedDBObject(dm, true);
                n++;
            }
            return n;
        }

        /// Một tia: block đầu phát ở đầu a (quay về phía đầu thu, một block = một bộ khi bóc khối lượng), nét tia a–c và vạch ngang ở đầu thu c.
        static void VeTia(Database db, Transaction tr, BlockTableRecord ms, VatTu v, Point2d[] a, double k, ObjectId lop, double vach)
        {
            Vector2d h = a[1] - a[0];
            ChenThietBi(db, tr, v, new Point3d(a[0].X, a[0].Y, 0), k, Math.Atan2(h.Y, h.X));
            Vector2d n = new Vector2d(-h.Y, h.X).GetNormal() * vach;
            foreach (Line l in new[] { new Line(new Point3d(a[0].X, a[0].Y, 0), new Point3d(a[1].X, a[1].Y, 0)),
                new Line(new Point3d(a[1].X + n.X, a[1].Y + n.Y, 0), new Point3d(a[1].X - n.X, a[1].Y - n.Y, 0)) })
            {
                l.LayerId = lop;
                ms.AppendEntity(l);
                tr.AddNewlyCreatedDBObject(l, true);
            }
        }

        /// PCCC_TUYEN: ống nhánh sprinkler kiểu xương cá cho một phòng. Chọn đầu phun, chọn ống chính, rồi BẤM chỗ đặt ống góp
        /// (người dùng dặn 2026-11-11: tự chọn vị trí ống góp, không để máy chọn cột đầu phun).
        static void TuyenTay()
        {
            Document doc = AcApp.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;
            FileDanhMuc dm = DocDanhMuc();
            // Người dùng 16/11 (ý từ video VPTTOOL): TuDong = mỗi đoạn lấy DN theo số đầu phun phía sau, bảng data/ong_nhanh_dn.json, chọn mức áp suất đầu phun.
            PromptStringOptions o0 = new PromptStringOptions("\nMã ống nhánh (vd ONG_DN25), TuDong = DN theo số đầu phun phía sau, ? = danh sách: ");
            o0.AllowSpaces = false;
            PromptResult r0 = ed.GetString(o0);
            if (r0.Status != PromptStatus.OK || r0.StringResult.Trim() == "") return;
            string s0 = r0.StringResult.Trim();
            bool tuDong = s0.Equals("TuDong", StringComparison.OrdinalIgnoreCase) || s0.Equals("T", StringComparison.OrdinalIgnoreCase);
            VatTu v = null;
            string muc = "";
            List<KeyValuePair<string, int>> bang = null;
            if (tuDong)
            {
                PromptKeywordOptions ko = new PromptKeywordOptions("\nÁp suất đầu phun: Den05 = đến 0,5 MPa (thường dùng), Tren05 = 0,5 MPa trở lên");
                ko.Keywords.Add("Den05"); ko.Keywords.Add("Tren05");
                ko.Keywords.Default = "Den05";
                ko.AllowNone = true;
                PromptResult kr = ed.GetKeywords(ko);
                if (kr.Status == PromptStatus.Cancel) return;
                muc = kr.StringResult == "Tren05" ? "Tren05" : "Den05";
                string tenMuc;
                bang = BangDn(muc, out tenMuc);
                ed.WriteMessage("\n[TKPCCC] Cỡ ống theo bảng ong_nhanh_dn.json, mức \"" + tenMuc + "\": "
                    + string.Join(", ", bang.Where(x => x.Value != 0).Select(x => x.Key.Substring(4) + (x.Value < 0 ? " trên" : " ≤ " + x.Value))) + " đầu phun.");
            }
            else
            {
                v = dm.vat_tu.FirstOrDefault(x => x.loai == "tuyen" && string.Equals(x.ma, s0, StringComparison.OrdinalIgnoreCase));
                if (v == null) v = HoiMa(ed, dm, "tuyen", "Mã ống nhánh (vd ONG_DN25)");
                if (v == null) return;
            }
            PromptSelectionOptions c1 = new PromptSelectionOptions();
            c1.MessageForAdding = "\nChọn các đầu phun của một phòng: ";
            PromptSelectionResult r1 = ed.GetSelection(c1, new SelectionFilter(new TypedValue[] { new TypedValue((int)DxfCode.Start, "INSERT") }));
            if (r1.Status != PromptStatus.OK) return;
            // Chọn bằng GetSelection (không GetEntity) để kịch bản accoreconsole trả lời được bằng "L".
            PromptSelectionOptions c2 = new PromptSelectionOptions();
            c2.MessageForAdding = "\nChọn ống chính (polyline hoặc line): ";
            c2.SingleOnly = true;
            PromptSelectionResult r2 = ed.GetSelection(c2, new SelectionFilter(new TypedValue[] { new TypedValue((int)DxfCode.Start, "LWPOLYLINE,LINE") }));
            if (r2.Status != PromptStatus.OK || r2.Value.Count == 0) return;
            PromptPointOptions po = new PromptPointOptions("\nBấm vị trí ống góp (Enter = cột đầu phun gần giữa phòng): ");
            po.AllowNone = true;
            PromptPointResult pr = ed.GetPoint(po);
            if (pr.Status != PromptStatus.OK && pr.Status != PromptStatus.None) return;
            Point3d? qua = pr.Status == PromptStatus.OK ? pr.Value.TransformBy(ed.CurrentUserCoordinateSystem) : (Point3d?)null;

            double m = HeSoMet(db), dai = 0;
            int soDoan = 0;
            HashSet<string> thay = new HashSet<string>();
            Dictionary<string, int> theoMa = new Dictionary<string, int>(); // TuDong: số đoạn theo mã ống
            List<List<Point2d>> vungCo = VungCoSan(db);
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                List<Point3d> ps, truc = new List<Point3d>();
                List<KeyValuePair<Point3d, string>> dau = new List<KeyValuePair<Point3d, string>>();
                BoiCanh bc = new BoiCanh { Tr = tr, Dm = DocDanhMuc(), Alias = DocAlias() };
                foreach (ObjectId id in r1.Value.GetObjectIds())
                {
                    BlockReference br = tr.GetObject(id, OpenMode.ForRead) as BlockReference;
                    // Chỉ block là thiết bị (có mã vật tư): quét cửa sổ quanh phòng không lấy nhầm đồ nội thất.
                    string ma = br == null || br is Table ? null : MaThietBi(br, bc);
                    if (ma != null) dau.Add(new KeyValuePair<Point3d, string>(new Point3d(br.Position.X, br.Position.Y, 0), ma));
                }
                int boDau, soNhom;
                ps = LocDauPhun(dau, vungCo, 0.6 / m, out boDau, out soNhom);
                if (boDau > 0)
                    ed.WriteMessage("\n[TKPCCC] tuyen_loc giu=" + ps.Count + " bo=" + boDau + " nhom=" + soNhom
                        + " (đầu phun khác loại hoặc ở vùng " + LAYER_VUNG + " tách rời: chỉ nối nhóm đông nhất; cần nối nhóm khác thì chọn riêng).");
                Entity e = (Entity)tr.GetObject(r2.Value.GetObjectIds()[0], OpenMode.ForRead);
                Polyline pl = e as Polyline;
                Line ln = e as Line;
                if (pl != null) for (int i = 0; i < pl.NumberOfVertices; i++) truc.Add(pl.GetPoint3dAt(i));
                else if (ln != null) { truc.Add(ln.StartPoint); truc.Add(ln.EndPoint); }
                if (ps.Count == 0 || truc.Count < 2) throw new System.Exception("cần ít nhất một đầu phun (block) và một ống chính có 2 điểm trở lên");
                BlockTableRecord ms = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);
                List<int> dem = tuDong ? new List<int>() : null;
                List<List<Point3d>> ds = TuyenXuongCa(ps, truc, null, qua, 0.1 / m, dem);
                ObjectId lNhan = tuDong ? DamBaoLayer(db, tr, LAYER_NHAN, 7) : ObjectId.Null;
                double cao = LaySo(NhomKyHieu("chu"), "cao", 250) * 0.001 / m;
                string maTruoc = null;
                Point3d? cuoiTruoc = null;
                for (int i = 0; i < ds.Count; i++)
                {
                    List<Point3d> d = ds[i];
                    VatTu vd = tuDong ? MaDn(dm, bang, dem[i], thay) : v;
                    Polyline ong = TaoPline(d, false, DamBaoLayer(db, tr, TIEN_TO_LAYER + vd.ma, MauHe(vd.he)));
                    ms.AppendEntity(ong);
                    tr.AddNewlyCreatedDBObject(ong, true);
                    dai += ong.Length;
                    soDoan++;
                    if (!tuDong) continue;
                    theoMa[vd.ma] = (theoMa.ContainsKey(vd.ma) ? theoMa[vd.ma] : 0) + 1;
                    // Chữ DN ở đoạn đầu mỗi tuyến và chỗ đổi cỡ (đoạn nối tiếp cùng cỡ không ghi lại).
                    if (!cuoiTruoc.HasValue || cuoiTruoc.Value.DistanceTo(d[0]) > 1e-6 || vd.ma != maTruoc)
                        ChuDn(tr, ms, d[0], d[d.Count - 1], vd.ma.StartsWith("ONG_", StringComparison.OrdinalIgnoreCase) ? vd.ma.Substring(4) : vd.ma, cao, lNhan);
                    maTruoc = vd.ma;
                    cuoiTruoc = d[d.Count - 1];
                }
                tr.Commit();
            }
            foreach (string t in thay) ed.WriteMessage("\n[TKPCCC] " + t);
            ed.WriteMessage("\n[TKPCCC] PCCC_TUYEN " + (tuDong ? "TuDong " + muc : v.ma) + " doan=" + soDoan + " m=" + Math.Round(dai * m, 2).ToString(CultureInfo.InvariantCulture)
                + (tuDong ? " " + string.Join(" ", bang.Where(x => theoMa.ContainsKey(x.Key)).Select(x => x.Key.Substring(4) + "=" + theoMa[x.Key])) : ""));
        }

        /// PCCC_TUYEN (KOCHAM 16/11: khung chọn phòng chữ L dính đầu phun WC, hành lang): nhóm đầu phun theo (cụm vùng PCCC_VUNG có hộp bao cách nhau ≤ ke,
        /// mã thiết bị), giữ nhóm đông nhất; đầu phun ngoài mọi vùng cùng mã thì giữ. Khoang dầm kề nhau (qua dầm ≤ ke) là một cụm.
        static List<Point3d> LocDauPhun(List<KeyValuePair<Point3d, string>> dau, List<List<Point2d>> vung, double ke, out int bo, out int soNhom)
        {
            int n = vung.Count;
            double[][] hb = vung.Select(d => new[] { d.Min(p => p.X), d.Min(p => p.Y), d.Max(p => p.X), d.Max(p => p.Y) }).ToArray();
            int[] cha = Enumerable.Range(0, n).ToArray();
            Func<int, int> goc = null;
            goc = i => cha[i] == i ? i : (cha[i] = goc(cha[i]));
            for (int i = 0; i < n; i++)
                for (int j = i + 1; j < n; j++)
                    if (hb[i][0] <= hb[j][2] + ke && hb[j][0] <= hb[i][2] + ke && hb[i][1] <= hb[j][3] + ke && hb[j][1] <= hb[i][3] + ke) cha[goc(i)] = goc(j);
            int[] nhom = dau.Select(kv =>
            {
                Point2d q = new Point2d(kv.Key.X, kv.Key.Y);
                for (int i = 0; i < n; i++) if (TrongDaGiac(vung[i], q)) return goc(i);
                return -1;
            }).ToArray();
            Dictionary<string, int> dem = new Dictionary<string, int>();
            for (int i = 0; i < dau.Count; i++) { string k = nhom[i] + "|" + dau[i].Value; int c; dem.TryGetValue(k, out c); dem[k] = c + 1; }
            soNhom = dem.Count;
            if (dem.Count == 0) { bo = 0; return new List<Point3d>(); }
            string tot = dem.OrderByDescending(kv => kv.Key.StartsWith("-1|") ? 0 : kv.Value).ThenByDescending(kv => kv.Value).First().Key;
            int gTot = int.Parse(tot.Substring(0, tot.IndexOf('|')), CultureInfo.InvariantCulture);
            string maTot = tot.Substring(tot.IndexOf('|') + 1);
            List<Point3d> kq = Enumerable.Range(0, dau.Count).Where(i => dau[i].Value == maTot && (nhom[i] == gTot || nhom[i] == -1)).Select(i => dau[i].Key).ToList();
            bo = dau.Count - kq.Count;
            return kq;
        }

        /// Bảng DN theo số đầu phun (data/ong_nhanh_dn.json cạnh DLL), mức "Den05" / "Tren05": { mã ống theo DN tăng dần, số đầu tối đa } (0 = cỡ không dùng, -1 = không giới hạn).
        static List<KeyValuePair<string, int>> BangDn(string muc, out string ten)
        {
            string f = Path.Combine(ThuMuc, "ong_nhanh_dn.json");
            if (!File.Exists(f)) throw new System.Exception("Thiếu " + f + ": chạy lại cad\\build.bat.");
            Dictionary<string, object> d;
            try { d = new JavaScriptSerializer().DeserializeObject(File.ReadAllText(f, Encoding.UTF8)) as Dictionary<string, object>; }
            catch (System.Exception ex) { throw new System.Exception("ong_nhanh_dn.json sai cú pháp: " + ex.Message); }
            Dictionary<string, object> cacMuc = d == null ? null : Lay(d, "muc") as Dictionary<string, object>;
            Dictionary<string, object> mm = cacMuc == null ? null : Lay(cacMuc, muc) as Dictionary<string, object>;
            IList ma = d == null ? null : Lay(d, "ma") as IList, td = mm == null ? null : Lay(mm, "toi_da") as IList;
            if (ma == null || td == null || ma.Count != td.Count) throw new System.Exception("ong_nhanh_dn.json: thiếu mức \"" + muc + "\", hoặc \"ma\" và \"toi_da\" lệch số cột.");
            ten = LayChu(mm, "ten", muc);
            List<KeyValuePair<string, int>> kq = new List<KeyValuePair<string, int>>();
            for (int i = 0; i < ma.Count; i++) kq.Add(new KeyValuePair<string, int>(Convert.ToString(ma[i]), td[i] == null ? -1 : Convert.ToInt32(td[i], CultureInfo.InvariantCulture)));
            return kq;
        }

        /// Cỡ nhỏ nhất chở được soDau đầu phun; mã chưa có trong danh mục (vd ONG_DN20) thì lấy cỡ lớn kế tiếp có trong danh mục, ghi vào thay.
        static VatTu MaDn(FileDanhMuc dm, List<KeyValuePair<string, int>> bang, int soDau, HashSet<string> thay)
        {
            int i = bang.FindIndex(x => x.Value != 0 && (x.Value < 0 || x.Value >= soDau));
            if (i < 0) i = bang.Count - 1;
            for (int j = i; j < bang.Count; j++)
            {
                VatTu v = dm.vat_tu.FirstOrDefault(x => x.loai == "tuyen" && string.Equals(x.ma, bang[j].Key, StringComparison.OrdinalIgnoreCase));
                if (v == null) continue;
                if (j > i) thay.Add(bang[i].Key + " chưa có trong vat_tu.json: dùng " + v.ma + " (thêm mã vào data/vat_tu.json nếu cần đúng cỡ).");
                return v;
            }
            throw new System.Exception("Không mã ống nào từ " + bang[i].Key + " trở lên có trong vat_tu.json.");
        }

        /// Chữ DN song song đoạn ống, ở giữa đoạn, phía trên (góc đọc được: -90° < góc ≤ 90°), layer PCCC_NHAN (PCCC_BOC không đếm).
        static void ChuDn(Transaction tr, BlockTableRecord ms, Point3d a, Point3d b, string chu, double cao, ObjectId lop)
        {
            Vector3d t = b - a;
            if (t.Length < 1e-9) return;
            double g = Math.Atan2(t.Y, t.X);
            if (g > Math.PI / 2 + 1e-9) g -= Math.PI; else if (g <= -Math.PI / 2 + 1e-9) g += Math.PI;
            MText c = new MText();
            c.Contents = chu;
            c.TextHeight = cao;
            c.Attachment = AttachmentPoint.BottomCenter;
            c.Rotation = g;
            c.Location = a + t * 0.5 + new Vector3d(-Math.Sin(g), Math.Cos(g), 0) * cao * 0.4;
            c.LayerId = lop;
            ms.AppendEntity(c);
            tr.AddNewlyCreatedDBObject(c, true);
        }

        /// PCCC_DAY (ý "Vẽ dây Loop" của addin mẫu, 14/11): dây nối tiếp các thiết bị đã chọn, đi theo hàng kiểu rắn bò (RanBo, như tuyen noi_tiep),
        /// từ điểm đầu (tủ, hộp nối; Enter = từ thiết bị đầu). Điểm cuối: Enter = dừng ở thiết bị cuối, Vong = quay về điểm đầu (mạch vòng).
        static void DayTay()
        {
            Document doc = AcApp.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;
            FileDanhMuc dm = DocDanhMuc();
            VatTu v = HoiMa(ed, dm, "tuyen", "Mã dây (vd DAY_BC)");
            if (v == null) return;
            PromptSelectionOptions c1 = new PromptSelectionOptions();
            c1.MessageForAdding = "\nChọn các thiết bị cần nối dây: ";
            PromptSelectionResult r1 = ed.GetSelection(c1, new SelectionFilter(new TypedValue[] { new TypedValue((int)DxfCode.Start, "INSERT") }));
            if (r1.Status != PromptStatus.OK) return;
            PromptPointOptions po = new PromptPointOptions("\nBấm điểm đầu dây (tủ, hộp nối), Enter = bắt đầu từ thiết bị đầu: ");
            po.AllowNone = true;
            PromptPointResult pr = ed.GetPoint(po);
            if (pr.Status != PromptStatus.OK && pr.Status != PromptStatus.None) return;
            Point3d? tu = pr.Status == PromptStatus.OK ? pr.Value.TransformBy(ed.CurrentUserCoordinateSystem) : (Point3d?)null;
            PromptPointOptions pc = new PromptPointOptions("\nBấm điểm cuối dây, Enter = dừng ở thiết bị cuối");
            pc.Keywords.Add("Vong"); // AutoCAD tự nối " [Vong]: "
            pc.AllowNone = true;
            PromptPointResult pr2 = ed.GetPoint(pc);
            if (pr2.Status == PromptStatus.Cancel) return;
            bool vong = pr2.Status == PromptStatus.Keyword;
            Point3d? den = pr2.Status == PromptStatus.OK ? pr2.Value.TransformBy(ed.CurrentUserCoordinateSystem) : vong ? tu : (Point3d?)null;
            // Nhãn zone như bộ bản vẽ mẫu (hoc-ban-ve-mau-dwg.md mục 3): "VỀ TỦ TTBC – ZONE n" + "DIỆN TÍCH BẢO VỆ: … m²" ở đầu dây.
            PromptStringOptions zo = new PromptStringOptions("\nTên zone ghi ở đầu dây (vd ZONE 1), Enter = không ghi: ");
            zo.AllowSpaces = true;
            PromptResult zr = ed.GetString(zo);
            if (zr.Status != PromptStatus.OK) return;
            string zone = zr.StringResult.Trim();
            // Điện trở cuối dây ở cuối mạch hở của dây báo cháy (mạch vòng không cần).
            VatTu dtcd = !vong && (v.he == "BC" || v.ma.StartsWith("DAY_BC", StringComparison.OrdinalIgnoreCase)) ? TimMa(dm, "BC_DTCD") : null; // DAY_BC xếp hệ ONG trong vat_tu.json
            if (dtcd != null)
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    bool co = ((BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead)).Has(dtcd.ma);
                    tr.Commit();
                    if (!co) NapVaoBanVe(doc, dm);
                }

            double m = HeSoMet(db), dai = 0, m2 = 0;
            int n = 0, soDtcd = 0;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                List<Point3d> ps = new List<Point3d>();
                BoiCanh bc = new BoiCanh { Tr = tr, Dm = dm, Alias = DocAlias() };
                foreach (ObjectId id in r1.Value.GetObjectIds())
                {
                    BlockReference br = tr.GetObject(id, OpenMode.ForRead) as BlockReference;
                    if (br != null && !(br is Table) && MaThietBi(br, bc) != null) ps.Add(new Point3d(br.Position.X, br.Position.Y, 0));
                }
                if (ps.Count == 0) throw new System.Exception("vùng chọn không có thiết bị nào (block có mã vật tư)");
                n = ps.Count;
                ObjectId layer = DamBaoLayer(db, tr, TIEN_TO_LAYER + v.ma, MauHe(v.he));
                BlockTableRecord ms = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);
                List<List<Point3d>> tuyen = TuyenNoiTiep(new List<List<Point3d>> { ps }, tu, den, 0.1 / m);
                foreach (List<Point3d> d in tuyen)
                {
                    if (vong && !tu.HasValue) d.Add(d[0]); // không có điểm đầu: vòng về thiết bị đầu
                    if (dtcd != null && d == tuyen[tuyen.Count - 1] && d.Count >= 2)
                    {
                        // Kéo dây thêm 0,5 m theo hướng đoạn cuối, đặt điện trở ở mút.
                        Vector3d h = (d[d.Count - 1] - d[d.Count - 2]);
                        Point3d mut = d[d.Count - 1] + (h.Length > 1e-9 ? h.GetNormal() : Vector3d.XAxis) * (0.5 / m);
                        d.Add(mut);
                        ChenThietBi(db, tr, dtcd, mut, 0.001 / m, 0);
                        soDtcd++;
                    }
                    Polyline day = TaoPline(d, false, layer);
                    ms.AppendEntity(day);
                    tr.AddNewlyCreatedDBObject(day, true);
                    dai += day.Length;
                }
                if (zone != "")
                {
                    // Diện tích bảo vệ = tổng vùng PCCC_VUNG chứa ít nhất một thiết bị của dây.
                    foreach (ObjectId id in ms)
                    {
                        Polyline pl = tr.GetObject(id, OpenMode.ForRead) as Polyline;
                        if (!KinPl(pl) || !string.Equals(pl.Layer, LAYER_VUNG, StringComparison.OrdinalIgnoreCase)) continue;
                        List<Point2d> vg = DinhPl(pl);
                        if (ps.Any(p => TrongDaGiac(vg, new Point2d(p.X, p.Y)))) m2 += Math.Abs(pl.Area) * m * m;
                    }
                    Point3d dau = tuyen.Count > 0 && tuyen[0].Count > 0 ? tuyen[0][0] : ps[0];
                    MText nh = new MText();
                    nh.Location = dau + new Vector3d(0.3 / m, 0.6 / m, 0);
                    nh.Attachment = AttachmentPoint.BottomLeft;
                    nh.TextHeight = 0.35 / m;
                    nh.Contents = "VỀ TỦ TTBC - " + zone.ToUpperInvariant() + (m2 > 0 ? "\\PDIỆN TÍCH BẢO VỆ: " + Math.Round(m2, 1).ToString(CultureInfo.GetCultureInfo("vi-VN")) + " m²" : "");
                    nh.LayerId = DamBaoLayer(db, tr, "PCCC_ZONE", 1);
                    ms.AppendEntity(nh);
                    tr.AddNewlyCreatedDBObject(nh, true);
                }
                tr.Commit();
            }
            ed.WriteMessage("\n[TKPCCC] PCCC_DAY " + v.ma + " thiet_bi=" + n + (vong ? " vong" : "") + " m=" + Math.Round(dai * m, 2).ToString(CultureInfo.InvariantCulture)
                + (soDtcd > 0 ? " dtcd=" + soDtcd : "") + (zone != "" ? " zone=" + zone + " m2=" + Math.Round(m2, 1).ToString(CultureInfo.InvariantCulture) : ""));
        }
    }
}
