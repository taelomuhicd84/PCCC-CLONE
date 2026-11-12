// Rải thiết bị theo lưới trong vùng (thao tác "rai", lệnh PCCC_RAI) và đi tuyến ống/dây qua các điểm (thao tác "tuyen").
// Không chứa trị số quy chuẩn: bước rải do người gọi đưa vào (cad/rule/LUAT.md chỉ nơi tra).
// Cách vẽ theo bản mẫu của người dùng: cad/knowledge/mau-ban-ve-pccc.md.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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

        /// Lưới thiết bị trong một vùng (đa giác). Lưới tính theo hộp bao (xoay gocDo nếu phòng nghiêng), bỏ điểm ngoài đa giác,
        /// rồi tự kiểm vùng hở: điểm nào trong vùng xa mọi thiết bị hơn nửa đường chéo ô lưới cho phép thì tính là hở.
        /// dt > 0 (đơn vị bản vẽ bình phương) = diện tích bảo vệ tối đa của một thiết bị. Rải theo bước tối đa thì lấy số cột × số hàng ÍT thiết bị nhất
        /// mà ô lưới không vượt dt (hoà thì lấy ô vuông hơn): 16 × 12 m, s 4 m, dt 12 m2 ra 4 × 4 (bước 4000 × 3000), không phải 4 × 3 (16 m2) hay 5 × 4.
        static LuoiRai LuoiVung(List<Point2d> vung, double sx, double sy, double bx, double by, double le, double chan, double dt, double gocDo)
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
            ChiaBuoc(W, sx, bx, le, chan, nx0, out k.Nx, out k.Bx, out k.Tx);
            ChiaBuoc(H, sy, by, le, chan, ny0, out k.Ny, out k.By, out k.Ty);
            if (dt > 0 && bx <= 0 && ORai(k) > dt * (1 + 1e-9))
            {
                // Làm tròn bước theo "chan" dồn phần dư ra tường làm ô sát tường vượt dt: giữ bước chia đều.
                ChiaBuoc(W, sx, bx, le, 0, nx0, out k.Nx, out k.Bx, out k.Tx);
                ChiaBuoc(H, sy, by, le, 0, ny0, out k.Ny, out k.By, out k.Ty);
            }
            k.O = ORai(k);
            // Chặn bước gõ nhầm đơn vị (vd 4 thay vì 4000 trong bản vẽ mm): không để AutoCAD treo vì chèn hàng triệu block.
            if ((long)k.Nx * k.Ny > 5000) throw new System.Exception("bước quá nhỏ so với vùng (" + k.Nx + " x " + k.Ny + " thiết bị): kiểm đơn vị của bước và của bản vẽ");
            List<Point2d> luoi = new List<Point2d>();
            for (int j = 0; j < k.Ny; j++)
                for (int i = 0; i < k.Nx; i++)
                {
                    Point2d p = new Point2d(x0 + k.Tx + i * k.Bx, y0 + k.Ty + j * k.By);
                    if (TrongDaGiac(q, p)) luoi.Add(p);
                }

            double rx = bx > 0 ? bx : sx, ry = by > 0 ? by : sy;
            double r = Math.Sqrt(rx * rx + ry * ry) / 2 * 1.001;
            double mau = Math.Max(Math.Min(k.Bx, k.By) / 4, Math.Sqrt((x1 - x0) * (y1 - y0) / 2500)); // tối đa khoảng 2500 điểm mẫu
            int tong = 0, ho = 0;
            for (double y = y0 + mau / 2; y < y1; y += mau)
                for (double x = x0 + mau / 2; x < x1; x += mau)
                {
                    Point2d m = new Point2d(x, y);
                    if (!TrongDaGiac(q, m)) continue;
                    tong++;
                    if (!luoi.Any(p => p.GetDistanceTo(m) <= r)) ho++;
                }
            k.Ho = tong == 0 ? 0 : Math.Round(100.0 * ho / tong, 1);
            foreach (Point2d p in luoi) k.Diem.Add(new Point3d(p.X * c - p.Y * s, p.X * s + p.Y * c, 0));
            return k;
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

        /// Chuỗi kích thước định vị như bản mẫu: tường, thiết bị, ..., tường theo hàng dưới cùng và cột trái cùng.
        static void KtRai(GoiSpec g, List<Point2d> vung, LuoiRai r)
        {
            double x0 = vung.Min(p => p.X), x1 = vung.Max(p => p.X), y0 = vung.Min(p => p.Y), y1 = vung.Max(p => p.Y);
            double cao = LaySo(NhomKyHieu("kt"), "cao", 250), y = r.Diem.Min(p => p.Y), x = r.Diem.Min(p => p.X);
            ObjectId l = DamBaoLayer(g.Db, g.Tr, LAYER_KT_RAI, 8);
            List<double> xs = new List<double> { x0 }, ys = new List<double> { y0 };
            xs.AddRange(r.Diem.Where(p => Math.Abs(p.Y - y) < 1e-6).Select(p => p.X).OrderBy(v => v));
            ys.AddRange(r.Diem.Where(p => Math.Abs(p.X - x) < 1e-6).Select(p => p.Y).OrderBy(v => v));
            xs.Add(x1); ys.Add(y1);
            for (int i = 0; i + 1 < xs.Count; i++)
                if (xs[i + 1] - xs[i] > 1e-6) VeKt(g, new Point3d(xs[i], y, 0), new Point3d(xs[i + 1], y, 0), 0, cao, l);
            for (int i = 0; i + 1 < ys.Count; i++)
                if (ys[i + 1] - ys[i] > 1e-6) VeKt(g, new Point3d(x, ys[i], 0), new Point3d(x, ys[i + 1], 0), 0, cao, l);
        }

        static void OpRai(GoiSpec g, Dictionary<string, object> o)
        {
            string ma = LayChu(o, "ma", "");
            VatTu v = TimMa(g.Dm, ma);
            if (v == null || v.loai != "block") throw new System.Exception("mã thiết bị không có trong danh mục: " + ma);
            double sx, sy, bx, by;
            HaiSo(o, "s", out sx, out sy);
            HaiSo(o, "buoc", out bx, out by);
            if (sx <= 0 && bx <= 0) throw new System.Exception("rai cần \"s\" (bước tối đa) hoặc \"buoc\" (bước đúng), lấy theo quy chuẩn áp dụng cho công trình");
            double m = HeSoMet(g.Db), k = 0.001 / m, goc = LaySo(o, "goc", 0), dt = LaySo(o, "dt", 0) / (m * m);
            bool kt = LayCo(o, "kt", false) && goc == 0;
            g.DiemRai = new List<List<Point3d>>();
            g.VungRai = new List<List<Point2d>>();
            // Tính lưới MỌI vùng trước, đạt hết mới chèn: vùng sau lỗi thì không để lại thiết bị của vùng trước (gửi lại thao tác không rải trùng).
            List<List<Point2d>> vung = VungOp(g, o);
            List<LuoiRai> luoi = new List<LuoiRai>();
            foreach (List<Point2d> d in vung)
            {
                LuoiRai r = LuoiVung(d, sx, sy, bx, by, LaySo(o, "le", -1), LaySo(o, "chan", 0.05 / m), dt, goc);
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
                        { "tuong", new double[] { Lt(r.Tx), Lt(r.Ty) } }, { "m2", Math.Round(r.O * m * m, 2) }, { "ho", r.Ho } });
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
        /// Chân ống góp (kể cả theo "qua") ra ngoài đoạn thì nối thêm một đoạn dọc trục ống chính về đầu mút gần nhất.
        static List<List<Point3d>> TuyenXuongCa(List<Point3d> ps, List<Point3d> truc, Point3d? tu, Point3d? qua, double saiSo)
        {
            Point3d tam = new Point3d(ps.Average(p => p.X), ps.Average(p => p.Y), 0);
            int i0 = DoanGan(truc, tam);
            Point3d A = truc[i0], B = truc[i0 + 1];
            double L = A.DistanceTo(B);
            Vector3d u = (B - A).GetNormal(), n = new Vector3d(-u.Y, u.X, 0);
            Func<Point3d, double> fa = p => (p - A).DotProduct(u), fd = p => (p - A).DotProduct(n);
            Func<double, double> ngoai = x => Math.Max(0, Math.Max(-x, x - L)); // chân ống góp cách đoạn ống chính bao nhiêu
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
            if (ngoai(af) > 1e-6) kq.Add(new List<Point3d> { A + u * af, af < 0 ? A : B });
            foreach (List<Point3d> h in hang)
            {
                double d = h.Average(fd), a0 = Math.Min(h.Min(fa), af), a1 = Math.Max(h.Max(fa), af);
                if (a1 - a0 > 1e-6) kq.Add(new List<Point3d> { A + u * a0 + n * d, A + u * a1 + n * d });
            }
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
            if ((kieu == "xuong_ca" || kieu == "nhanh") && (truc == null || truc.Count < 2))
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
            else if (kieu == "tia")
            {
                // Đèn báo phòng: phòng có bao nhiêu đầu báo thì bấy nhiêu nét thẳng từ đầu báo ra đèn (cad/knowledge/tu-duy-di-day-di-ong.md).
                if (!den.HasValue) throw new System.Exception("tuyen kiểu \"tia\" cần \"den\" [x,y] (vd vị trí đèn báo phòng)");
                foreach (List<Point3d> ps in nhom)
                    foreach (Point3d p in ps)
                        if (p.DistanceTo(den.Value) > 1e-6) doan.Add(new List<Point3d> { p, den.Value });
            }
            else throw new System.Exception("tuyen: \"kieu\" phải là noi_tiep, tia, xuong_ca hoặc nhanh");

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

        /// PCCC_RAI: hỏi mã thiết bị và bước tối đa S, rồi lặp: bấm một điểm trong phòng (tự dò đường bao) hoặc Doituong để chọn polyline kín.
        static void RaiTay()
        {
            Document doc = AcApp.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;
            FileDanhMuc dm = DocDanhMuc();
            VatTu v = HoiMa(ed, dm, "block", "Mã thiết bị cần rải");
            if (v == null) return;
            // Plugin không tra tiêu chuẩn: bước tối đa do người thiết kế nhập theo TCVN đang áp dụng.
            PromptDoubleOptions so = new PromptDoubleOptions("\nBước tối đa S giữa hai thiết bị (m) theo tiêu chuẩn áp dụng: ");
            so.AllowNegative = false; so.AllowZero = false;
            PromptDoubleResult sr = ed.GetDouble(so);
            if (sr.Status != PromptStatus.OK) return;
            double m = HeSoMet(db), s = sr.Value / m, k = 0.001 / m;
            PromptDoubleOptions dtHoi = new PromptDoubleOptions("\nDiện tích bảo vệ tối đa của một thiết bị (m2) theo tiêu chuẩn áp dụng, Enter = không xét: ");
            dtHoi.AllowNegative = false; dtHoi.AllowZero = false; dtHoi.AllowNone = true;
            PromptDoubleResult dr = ed.GetDouble(dtHoi);
            if (dr.Status != PromptStatus.OK && dr.Status != PromptStatus.None) return;
            double dt = dr.Status == PromptStatus.OK ? dr.Value / (m * m) : 0;

            bool coBlock;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                coBlock = ((BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead)).Has(v.ma);
                tr.Commit();
            }
            if (!coBlock) NapVaoBanVe(doc, dm);

            int tong = 0;
            while (true)
            {
                PromptPointOptions po = new PromptPointOptions("\nBấm một điểm trong phòng"); // AutoCAD tự nối " [Doituong]: "
                po.Keywords.Add("Doituong");
                po.AllowNone = true;
                PromptPointResult pr = ed.GetPoint(po);
                List<List<Point2d>> vung = new List<List<Point2d>>();
                if (pr.Status == PromptStatus.Keyword)
                {
                    PromptSelectionOptions tuyChon = new PromptSelectionOptions();
                    tuyChon.MessageForAdding = "\nChọn polyline kín (mỗi polyline là một phòng): ";
                    PromptSelectionResult chon = ed.GetSelection(tuyChon, new SelectionFilter(new TypedValue[] { new TypedValue((int)DxfCode.Start, "LWPOLYLINE") }));
                    if (chon.Status != PromptStatus.OK) break;
                    using (Transaction tr = db.TransactionManager.StartTransaction())
                    {
                        foreach (ObjectId id in chon.Value.GetObjectIds())
                        {
                            Polyline pl = tr.GetObject(id, OpenMode.ForRead) as Polyline;
                            if (pl != null && pl.Closed && pl.NumberOfVertices >= 3) vung.Add(DinhPl(pl));
                        }
                        tr.Commit();
                    }
                    if (vung.Count == 0) { ed.WriteMessage("\nKhông có polyline kín nào trong các đối tượng vừa chọn."); continue; }
                }
                else if (pr.Status == PromptStatus.OK)
                {
                    DBObjectCollection bao = null;
                    try { bao = ed.TraceBoundary(pr.Value, false); } catch (System.Exception) { }
                    if (bao != null)
                    {
                        Polyline lon = bao.Cast<DBObject>().OfType<Polyline>().OrderByDescending(x => x.Area).FirstOrDefault();
                        if (lon != null) vung.Add(DinhPl(lon));
                        foreach (DBObject x in bao) x.Dispose();
                    }
                    if (vung.Count == 0)
                    {
                        ed.WriteMessage("\nKhông dò được đường bao kín quanh điểm này (phòng hở, hoặc chưa zoom thấy hết phòng). Dùng Doituong để chọn polyline kín.");
                        continue;
                    }
                }
                else break;

                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    foreach (List<Point2d> d in vung)
                    {
                        LuoiRai r = LuoiVung(d, s, s, 0, 0, -1, 0.05 / m, dt, 0);
                        foreach (Point3d p in r.Diem) ChenThietBi(db, tr, v, p, k, 0);
                        tong += r.Diem.Count;
                        ed.WriteMessage("\n[TKPCCC] PCCC_RAI " + v.ma + " n=" + r.Diem.Count + " luoi=" + r.Nx + "x" + r.Ny
                            + " buoc=" + Lt(r.Bx).ToString(CultureInfo.InvariantCulture) + "x" + Lt(r.By).ToString(CultureInfo.InvariantCulture)
                            + " m2=" + Math.Round(r.O * m * m, 2).ToString(CultureInfo.InvariantCulture)
                            + (r.Ho > 0 ? " HO=" + r.Ho.ToString(CultureInfo.InvariantCulture) + "% (vùng chưa phủ hết, kiểm lại)" : ""));
                    }
                    tr.Commit();
                }
            }
            ed.WriteMessage("\nĐã rải tổng " + tong + " " + v.ten + ".");
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
