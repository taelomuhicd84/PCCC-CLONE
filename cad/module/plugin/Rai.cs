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

        static List<Point2d> DinhPl(Polyline pl)
        {
            List<Point2d> d = new List<Point2d>();
            for (int i = 0; i < pl.NumberOfVertices; i++) d.Add(pl.GetPoint2dAt(i)); // shortcut: cung (bulge) coi là đoạn thẳng, phòng tường cong thì thêm chia cung
            return d;
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
            List<int> ho = Enumerable.Range(0, mau.Count).Where(i => !luoi.Any(c => phu(mau[i], c)))
                .OrderBy(i => Math.Round(mau[i].Y / e)).ThenBy(i => mau[i].X).ToList();
            int them = 0;
            for (int luot = 0; luot < 20; luot++)
            {
                if (ho.Count == 0)
                {
                    List<Point2d> sot = DiemHo(q, luoi, hx, hy, phu);
                    if (sot.Count == 0) break;
                    // Khe hẹp hơn bước mẫu: mẫu dày (bước / 4) quanh từng đỉnh khe để chấm điểm vị trí theo đúng cỡ khe.
                    foreach (Point2d s in sot.ToList())
                        for (int i = -4; i <= 4; i++)
                            for (int j = -4; j <= 4; j++)
                            {
                                Point2d m = new Point2d(s.X + buoc * i / 4, s.Y + buoc * j / 4);
                                if (TrongDaGiac(q, m) && !luoi.Any(c => phu(m, c))) sot.Add(m);
                            }
                    ho = Enumerable.Range(mau.Count, sot.Count).ToList();
                    mau.AddRange(sot);
                }
            for (; ho.Count > 0 && them < 5000; them++)
            {
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
                        int diem = ho.Count(i => phu(mau[i], c)); // vị trí lưới đứng trước nên hoà thì giữ thẳng hàng
                        if (diem > diemTot) { diemTot = diem; tot = c; }
                    }
                luoi.Add(tot);
                ho.RemoveAll(i => phu(mau[i], tot));
            }
            }
            if (ho.Count == 0)
            {
                if (DiemHo(q, luoi, hx, hy, phu).Count > 0) return 0.1; // còn khe sau 20 lượt (không gặp khi thử): báo hở để người vẽ xem
                // Tham lam có thể thêm thừa (thiết bị thêm sau phủ hết phần của thiết bị thêm trước): bỏ thử từng cái, từ cái thêm sau cùng.
                for (int i = luoi.Count - 1, dau = luoi.Count - them; i >= dau; i--)
                {
                    Point2d c = luoi[i];
                    luoi.RemoveAt(i);
                    if (mau.Any(m => !luoi.Any(d => phu(m, d))) || DiemHo(q, luoi, hx, hy, phu).Count > 0) luoi.Insert(i, c);
                }
            }
            return nTrong == 0 ? 0 : Math.Round(100.0 * ho.Count(i => i < nTrong) / nTrong, 1);
        }

        /// Điểm trong vùng chưa được ô phủ nào chứa, tìm CHÍNH XÁC: mỗi khe hở (vùng trừ hợp các ô chữ nhật) có một đỉnh là đỉnh vùng,
        /// giao cạnh vùng với cạnh ô, hoặc giao cạnh hai ô; thử 4 điểm lệch chéo rất nhỏ quanh từng đỉnh đó.
        static List<Point2d> DiemHo(List<Point2d> q, List<Point2d> luoi, double hx, double hy, Func<Point2d, Point2d, bool> phu)
        {
            double eps = 1e-4 * (hx + hy);
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
                foreach (Point2d d in luoi)
                    if (Math.Abs(c.X - d.X) <= 2 * hx && Math.Abs(c.Y - d.Y) <= 2 * hy)
                        foreach (double x in new[] { c.X - hx, c.X + hx })
                            foreach (double y in new[] { d.Y - hy, d.Y + hy })
                                if (Math.Abs(x - d.X) <= hx && Math.Abs(y - c.Y) <= hy) dinh.Add(new Point2d(x, y));
            List<Point2d> kq = new List<Point2d>();
            foreach (Point2d p in dinh)
                foreach (Vector2d v in new[] { new Vector2d(eps, eps), new Vector2d(-eps, eps), new Vector2d(eps, -eps), new Vector2d(-eps, -eps) })
                {
                    Point2d m = p + v;
                    if (TrongDaGiac(q, m) && !luoi.Any(c => phu(m, c))) kq.Add(m);
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
                if (pl != null && pl.Closed && pl.NumberOfVertices >= 3) kq.Add(DinhPl(pl));
            }
            if (kq.Count == 0)
                throw new System.Exception("không có vùng để rải: đưa \"vung\" [[x,y],...] hoặc \"hop\" [x1,y1,x2,y2], hoặc vẽ polyline kín trên layer \"l\" (mặc định PCCC_VUNG)");
            return kq;
        }

        /// Kích thước như bản mẫu (TCVN 7568-14 Hình 2): chuỗi tường, thiết bị, ..., tường của hàng trên cùng (đặt trên vùng) và cột trái cùng
        /// (đặt bên trái vùng), tường lấy theo giao của hàng / cột với đa giác; mỗi góc lồi một kích thước chéo từ thiết bị gần nhất tới góc.
        static void KtRai(GoiSpec g, List<Point2d> vung, LuoiRai r)
        {
            double x0 = vung.Min(p => p.X), y1 = vung.Max(p => p.Y);
            double cao = LaySo(NhomKyHieu("kt"), "cao", 250), ra = 4 * cao, y = r.Diem.Max(p => p.Y), x = r.Diem.Min(p => p.X);
            ObjectId l = DamBaoLayer(g.Db, g.Tr, LAYER_KT_RAI, 8);
            List<double> xs = r.Diem.Where(p => Math.Abs(p.Y - y) < 1e-6).Select(p => p.X).OrderBy(v => v).ToList();
            List<double> ys = r.Diem.Where(p => Math.Abs(p.X - x) < 1e-6).Select(p => p.Y).OrderBy(v => v).ToList();
            ChuoiTuong(xs, KhoangNgang(vung, y));
            ChuoiTuong(ys, KhoangNgang(vung.Select(p => new Point2d(p.Y, p.X)).ToList(), x));
            for (int i = 0; i + 1 < xs.Count; i++)
                if (xs[i + 1] - xs[i] > 1e-6) VeKt(g, new Point3d(xs[i], y, 0), new Point3d(xs[i + 1], y, 0), y1 - y + ra, cao, l);
            for (int i = 0; i + 1 < ys.Count; i++)
                if (ys[i + 1] - ys[i] > 1e-6) VeKt(g, new Point3d(x, ys[i], 0), new Point3d(x, ys[i + 1], 0), x - x0 + ra, cao, l);
            foreach (Point2d[] c in CheoGoc(vung, r.Diem))
                if (c[0].GetDistanceTo(c[1]) > 1e-6) VeKt(g, new Point3d(c[1].X, c[1].Y, 0), new Point3d(c[0].X, c[0].Y, 0), 0, cao, l);
        }

        /// Thêm hai đầu tường vào chuỗi toạ độ thiết bị (đã xếp tăng): mép khoảng giao chứa thiết bị đầu và thiết bị cuối.
        static void ChuoiTuong(List<double> ds, List<double[]> khoang)
        {
            double dau = ds[0], cuoi = ds[ds.Count - 1];
            double[] k0 = khoang.Where(k => k[0] <= dau + 1e-6).OrderByDescending(k => k[0]).FirstOrDefault();
            double[] k1 = khoang.Where(k => k[1] >= cuoi - 1e-6).OrderBy(k => k[1]).FirstOrDefault();
            if (k0 != null) ds.Insert(0, k0[0]);
            if (k1 != null) ds.Add(k1[1]);
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
        static List<List<Point3d>> TuyenXuongCa(List<Point3d> ps, List<Point3d> truc, Point3d? tu, Point3d? qua, double saiSo)
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
        static void RaiTay()
        {
            Document doc = AcApp.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;
            FileDanhMuc dm = DocDanhMuc();
            VatTu v = HoiMa(ed, dm, "block", "Mã thiết bị cần rải");
            if (v == null) return;
            double m = HeSoMet(db), s, k = 0.001 / m, dt = 0, le = -1, tuongMax = 0, cachMin = 0;
            Dictionary<string, object> tc = TieuChuanRai(v.ma, 0);
            if (tc != null)
            {
                // Mã có trị số tiêu chuẩn (data/khoang_cach_rai.json): không hỏi khoảng cách hay m2, chỉ hỏi chiều cao trần để chọn dòng.
                PromptDoubleOptions hc = new PromptDoubleOptions("\nChiều cao trần (m), Enter = không xét: ");
                hc.AllowNegative = false; hc.AllowZero = false; hc.AllowNone = true;
                PromptDoubleResult hcr = ed.GetDouble(hc);
                if (hcr.Status != PromptStatus.OK && hcr.Status != PromptStatus.None) return;
                if (hcr.Status == PromptStatus.OK) tc = TieuChuanRai(v.ma, hcr.Value);
                s = D(Lay(tc, "s")) / m;
                tuongMax = LaySo(tc, "tuong", 0) / m;
                cachMin = LaySo(tc, "tuong_min", 0) / m;
                ed.WriteMessage("\n[TKPCCC] " + v.ma + " theo " + LayChu(tc, "can_cu", "") + ": bước tối đa " + Lt(s * m).ToString(CultureInfo.InvariantCulture)
                    + " m, cách tường tối đa " + Lt(tuongMax * m).ToString(CultureInfo.InvariantCulture) + " m.");
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
            List<List<Point2d>> vung, mat;
            MatChon(db, qr.Value.GetObjectIds(), mau.Count > 0 ? mau : null, out vung, out mat);
            bool veVung = vung.Count == 0;
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
                    if (pr.Status == PromptStatus.Keyword || (pr.Status == PromptStatus.None && vung.Count == 0)) { vung = PhongThat(mat, m); break; }
                    if (pr.Status != PromptStatus.OK) break;
                    Point3d q = pr.Value.TransformBy(ed.CurrentUserCoordinateSystem);
                    List<Point2d> f = mat.Where(x => TrongDaGiac(x, new Point2d(q.X, q.Y))).OrderBy(x => DienTichDaGiac(x)).FirstOrDefault();
                    if (f == null) ed.WriteMessage("\nĐiểm không nằm trong phòng kín nào của vùng đã chọn.");
                    else if (!vung.Contains(f)) vung.Add(f);
                }
            }

            int tong = 0;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTableRecord ms = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);
                ObjectId lop = veVung ? DamBaoLayer(db, tr, LAYER_VUNG, 6) : ObjectId.Null;
                foreach (List<Point2d> d in vung)
                {
                    if (veVung)
                    {
                        Polyline pl = PlVung(d, lop);
                        ms.AppendEntity(pl);
                        tr.AddNewlyCreatedDBObject(pl, true);
                        ed.WriteMessage("\n[TKPCCC] Vùng tự dựng m2=" + Math.Round(DienTichDaGiac(d) * m * m, 2).ToString(CultureInfo.InvariantCulture)
                            + " (" + d.Count + " đỉnh) trên layer " + LAYER_VUNG + ".");
                    }
                    LuoiRai r = LuoiVung(d, s, s, 0, 0, le, le, 0.05 / m, dt, 0, theoHang, tuongMax, cachMin);
                    foreach (Point3d p in r.Diem) ChenThietBi(db, tr, v, p, k, 0);
                    tong += r.Diem.Count;
                    ed.WriteMessage("\n[TKPCCC] PCCC_RAI " + v.ma + " n=" + r.Diem.Count + " luoi=" + r.Nx + "x" + r.Ny
                        + " buoc=" + Lt(r.Bx).ToString(CultureInfo.InvariantCulture) + "x" + Lt(r.By).ToString(CultureInfo.InvariantCulture)
                        + " m2=" + Math.Round(r.O * m * m, 2).ToString(CultureInfo.InvariantCulture)
                        + (r.Ho > 0 ? " HO=" + r.Ho.ToString(CultureInfo.InvariantCulture) + "% (vùng chưa phủ hết, kiểm lại)" : ""));
                }
                tr.Commit();
            }
            ed.WriteMessage("\nĐã rải tổng " + tong + " " + v.ten + " trong " + vung.Count + " phòng.");
        }

        /// PCCC_TUYEN: ống nhánh sprinkler kiểu xương cá cho một phòng. Chọn đầu phun, chọn ống chính, rồi BẤM chỗ đặt ống góp
        /// (người dùng dặn 2026-11-11: tự chọn vị trí ống góp, không để máy chọn cột đầu phun).
        static void TuyenTay()
        {
            Document doc = AcApp.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;
            VatTu v = HoiMa(ed, DocDanhMuc(), "tuyen", "Mã ống nhánh (vd ONG_DN25)");
            if (v == null) return;
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
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                List<Point3d> ps = new List<Point3d>(), truc = new List<Point3d>();
                BoiCanh bc = new BoiCanh { Tr = tr, Dm = DocDanhMuc(), Alias = DocAlias() };
                foreach (ObjectId id in r1.Value.GetObjectIds())
                {
                    BlockReference br = tr.GetObject(id, OpenMode.ForRead) as BlockReference;
                    // Chỉ block là thiết bị (có mã vật tư): quét cửa sổ quanh phòng không lấy nhầm đồ nội thất.
                    if (br != null && !(br is Table) && MaThietBi(br, bc) != null) ps.Add(new Point3d(br.Position.X, br.Position.Y, 0));
                }
                Entity e = (Entity)tr.GetObject(r2.Value.GetObjectIds()[0], OpenMode.ForRead);
                Polyline pl = e as Polyline;
                Line ln = e as Line;
                if (pl != null) for (int i = 0; i < pl.NumberOfVertices; i++) truc.Add(pl.GetPoint3dAt(i));
                else if (ln != null) { truc.Add(ln.StartPoint); truc.Add(ln.EndPoint); }
                if (ps.Count == 0 || truc.Count < 2) throw new System.Exception("cần ít nhất một đầu phun (block) và một ống chính có 2 điểm trở lên");
                ObjectId layer = DamBaoLayer(db, tr, TIEN_TO_LAYER + v.ma, MauHe(v.he));
                BlockTableRecord ms = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);
                foreach (List<Point3d> d in TuyenXuongCa(ps, truc, null, qua, 0.1 / m))
                {
                    Polyline ong = TaoPline(d, false, layer);
                    ms.AppendEntity(ong);
                    tr.AddNewlyCreatedDBObject(ong, true);
                    dai += ong.Length;
                    soDoan++;
                }
                tr.Commit();
            }
            ed.WriteMessage("\n[TKPCCC] PCCC_TUYEN " + v.ma + " doan=" + soDoan + " m=" + Math.Round(dai * m, 2).ToString(CultureInfo.InvariantCulture));
        }
    }
}
