// Rải theo khoang dầm: đọc dầm b x h, phủ bù khoang, ghi m² khoang (GhiKhoang). Tách từ Rai.cs (26/11/2026: mỗi thư mục số của 07_CAD là một hạng mục tính năng, xem 07_CAD/README.md).
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
        /// Độ sâu dầm (m) từ ký hiệu "b x h" (mm), vd "B1 (400x600)", "300X700", "250×500". -1 = không phải ký hiệu dầm.
        /// Nhãn trên bản vẽ: số lớn hơn là h (người dùng dặn 16/11): có bộ kết cấu ghi ngược h x b ("DS 400X250" = rộng 250, cao 400).
        /// dungThuTu = cỡ người dùng gõ tay: đọc đúng b x h (rộng x cao), dầm bẹt 1400x700 ra h = 0,7 m.
        static double DocBxh(string s, bool dungThuTu = false)
        {
            System.Text.RegularExpressions.Match k = System.Text.RegularExpressions.Regex.Match(s ?? "", @"(\d{2,4})\s*[xX×\*]\s*(\d{2,4})");
            if (!k.Success) return -1;
            double m1 = double.Parse(k.Groups[1].Value, CultureInfo.InvariantCulture), m2 = double.Parse(k.Groups[2].Value, CultureInfo.InvariantCulture);
            // Nhãn dầm bẹt (KOCHAM "DS 1400X250" thật rộng 1400 cao 700) bị đọc thành dầm cao: người dùng sửa bằng từ khoá SuaDam ở dấu nhắc nét dầm mẫu.
            double b = dungThuTu ? m1 : Math.Min(m1, m2), h = dungThuTu ? m2 : Math.Max(m1, m2);
            return b >= 50 && b <= 2000 && h >= 100 && h <= 3000 ? h / 1000 : -1;
        }

        /// Bề rộng b (m) của "b x h" gõ tay (đúng thứ tự); -1 = không đọc được.
        static double DocRong(string s)
        {
            System.Text.RegularExpressions.Match k = System.Text.RegularExpressions.Regex.Match(s ?? "", @"(\d{2,4})\s*[xX×\*]\s*(\d{2,4})");
            double b;
            return k.Success && double.TryParse(k.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out b) && b >= 50 && b <= 2000 ? b / 1000 : -1;
        }

        /// Ký hiệu dầm "b x h" trong các đối tượng (cả chữ trong block lồng nhau): tâm hộp bao chữ -> độ sâu (m).
        static List<KeyValuePair<Point2d, Vector2d>> NhanDam(ObjectId[] ids)
        {
            List<KeyValuePair<Point2d, Vector2d>> kq = new List<KeyValuePair<Point2d, Vector2d>>();
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

        static void ChuDam(Entity e, int sau, List<KeyValuePair<Point2d, Vector2d>> kq)
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
            Vector2d huong = dt != null ? new Vector2d(Math.Cos(dt.Rotation), Math.Sin(dt.Rotation)) : new Vector2d(mt.Direction.X, mt.Direction.Y).GetNormal();
            kq.Add(new KeyValuePair<Point2d, Vector2d>(new Point2d(p.X, p.Y), huong * d)); // hướng chữ, dài = độ sâu dầm (m)
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

        const double KE_DAM_M = 0.8; // điểm trên tim dầm cách khoang nhỏ ≤ 0,8 m (nửa dầm bẹt 1400) mới coi là dầm của cụm khoang đó

        /// Tim dầm từ nét mép (người dùng 17/11: đầu báo lắp ở mặt dưới dầm phải nằm giữa bề rộng dầm): mỗi nét ghép với nét song song gần nhất
        /// cách 0,1–1,6 m, phần chồng ≥ 0,3 m; tim = đường giữa phần chồng. Hai nét ghép nhau hai chiều chỉ lấy một tim.
        static List<Point2d[]> TimDam(List<Point2d[]> canh, double m, List<double> nua = null) // nua: nửa bề rộng từng tim (lòng dầm)
        {
            List<Point2d[]> kq = new List<Point2d[]>();
            double dMin = 0.1 / m, dMax = 1.6 / m, chong = 0.3 / m, trung = 0.05 / m;
            for (int i = 0; i < canh.Count; i++)
            {
                Point2d a = canh[i][0];
                double L = a.GetDistanceTo(canh[i][1]);
                if (L < chong) continue;
                Vector2d u = (canh[i][1] - a) / L, n = new Vector2d(-u.Y, u.X);
                int tot = -1;
                double gan = double.MaxValue, t0 = 0, t1 = 0, lech = 0;
                for (int j = 0; j < canh.Count; j++)
                {
                    if (j == i) continue;
                    Point2d c = canh[j][0], e = canh[j][1];
                    if (Math.Abs(n.DotProduct(e - c)) > 0.02 * c.GetDistanceTo(e)) continue; // không song song
                    double kc = n.DotProduct(c - a);
                    if (Math.Abs(kc) < dMin || Math.Abs(kc) > dMax || Math.Abs(kc) >= gan) continue;
                    double s0 = u.DotProduct(c - a), s1 = u.DotProduct(e - a), lo = Math.Max(0, Math.Min(s0, s1)), hi = Math.Min(L, Math.Max(s0, s1));
                    if (hi - lo < chong) continue;
                    gan = Math.Abs(kc); tot = j; t0 = lo; t1 = hi; lech = kc / 2;
                }
                if (tot < 0) continue;
                Point2d p = a + u * t0 + n * lech, q = a + u * t1 + n * lech;
                if (!kq.Any(x => (x[0].GetDistanceTo(p) <= trung && x[1].GetDistanceTo(q) <= trung) || (x[0].GetDistanceTo(q) <= trung && x[1].GetDistanceTo(p) <= trung)))
                {
                    kq.Add(new[] { p, q });
                    if (nua != null) nua.Add(Math.Abs(lech));
                }
            }
            return kq;
        }

        /// Line dầm vẽ tay cùng một cỡ (TungDam): hai line song song cách nhau ≈ b là hai mép (giữ, tim do TimDam ghép); line lẻ là tim dầm:
        /// có b thì trả hai mép ± b/2, không có b thì giữ line làm nét chia khoang và ghi vào tim.
        static List<Point2d[]> MepDam(List<Point2d[]> nhom, double b, List<Point2d[]> tim)
        {
            List<Point2d[]> kq = new List<Point2d[]>();
            foreach (Point2d[] s in nhom)
            {
                double L = s[0].GetDistanceTo(s[1]);
                if (L < 1e-9) continue;
                Vector2d u = (s[1] - s[0]) / L, n = new Vector2d(-u.Y, u.X);
                bool doi = b > 0 && nhom.Any(o => o != s && Math.Abs(n.DotProduct(o[1] - o[0])) <= 0.02 * o[0].GetDistanceTo(o[1])
                    && Math.Abs(Math.Abs(n.DotProduct(o[0] - s[0])) - b) <= 0.3 * b
                    && Math.Min(L, Math.Max(u.DotProduct(o[0] - s[0]), u.DotProduct(o[1] - s[0]))) - Math.Max(0, Math.Min(u.DotProduct(o[0] - s[0]), u.DotProduct(o[1] - s[0]))) > 0);
                if (doi || b <= 0) { kq.Add(s); if (!doi) tim.Add(s); continue; }
                kq.Add(new[] { s[0] + n * (b / 2), s[1] + n * (b / 2) });
                kq.Add(new[] { s[0] - n * (b / 2), s[1] - n * (b / 2) });
            }
            return kq;
        }

        /// Điểm trên tim dầm gần p nhất, chỉ xét dầm sát cụm khoang (cách một khoang ≤ ke); null = không có.
        static Point2d? GanTimDam(List<List<Point2d>> khoang, List<Point2d[]> tim, Point2d p, double ke)
        {
            Point2d? kq = null;
            double gan = double.MaxValue;
            foreach (Point2d[] t in tim)
            {
                Vector2d u = t[1] - t[0];
                double L2 = u.DotProduct(u);
                if (L2 < 1e-12) continue;
                Point2d q = t[0] + u * Math.Max(0, Math.Min(1, (p - t[0]).DotProduct(u) / L2));
                double c = q.GetDistanceTo(p);
                if (c < gan && khoang.Any(k => TrongDaGiac(k, q) || CachVien(k, q) <= ke)) { gan = c; kq = q; }
            }
            return kq;
        }

        /// Dời từng đầu báo của cụm khoang nhỏ về điểm gần nhất trên tim dầm (mặt dưới dầm, giữa bề rộng); hai đầu dời trùng (≤ 0,3 m) thì bỏ một.
        /// Trả số đầu nằm trên dầm.
        static int DatLenDam(List<List<Point2d>> khoang, List<Point3d> diem, List<Point2d[]> tim, double ke)
        {
            List<Point3d> moi = new List<Point3d>();
            double trung = 0.3 * ke / KE_DAM_M;
            foreach (Point3d p in diem)
            {
                Point2d? q = GanTimDam(khoang, tim, new Point2d(p.X, p.Y), ke);
                Point3d r = q.HasValue ? new Point3d(q.Value.X, q.Value.Y, 0) : p;
                if (!moi.Any(x => x.DistanceTo(r) <= trung)) moi.Add(r);
            }
            diem.Clear();
            diem.AddRange(moi);
            return moi.Count(x => GanTimDam(khoang, tim, new Point2d(x.X, x.Y), ke).HasValue);
        }

        /// Như PhuBu cho cụm khoang nhỏ nhưng đầu bù đặt trên tim dầm gần chỗ hở nhất. Dầm xa chỗ hở quá nửa bước thì chịu hở chỗ đó (PCCC_KT báo).
        static int PhuBuDam(List<List<Point2d>> khoang, List<Point3d> diem, double hx, double hy, List<Point2d[]> tim, double ke)
        {
            int them = 0;
            double e = 1e-6 * (hx + hy), b = Math.Min(hx, hy) / 4;
            List<Point2d> mau = new List<Point2d>();
            foreach (List<Point2d> q in khoang)
            {
                mau.AddRange(q);
                double x0 = q.Min(p => p.X), x1 = q.Max(p => p.X), y0 = q.Min(p => p.Y), y1 = q.Max(p => p.Y);
                for (double y = y0 + b / 2; y < y1; y += b)
                    for (double x = x0 + b / 2; x < x1; x += b)
                        if (TrongDaGiac(q, new Point2d(x, y))) mau.Add(new Point2d(x, y));
            }
            Func<Point2d, bool> phu = p => diem.Any(c => Math.Abs(c.X - p.X) <= hx + e && Math.Abs(c.Y - p.Y) <= hy + e);
            for (int lan = 0; lan < 200; lan++)
            {
                int i = mau.FindIndex(p => !phu(p));
                if (i < 0) break;
                Point2d t = GanTimDam(khoang, tim, mau[i], ke) ?? mau[i];
                diem.Add(new Point3d(t.X, t.Y, 0));
                them++;
                if (!phu(mau[i])) mau.RemoveAt(i);
            }
            return them;
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

    }
}
