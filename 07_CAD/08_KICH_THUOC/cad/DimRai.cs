// Dim định vị thiết bị sau rải (KtRai, KtLuoi, KtSauRai). Tách từ Rai.cs (26/11/2026: mỗi thư mục số của 07_CAD là một hạng mục tính năng, xem 07_CAD/README.md).
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
        /// Kích thước như bản mẫu (TCVN 7568-14 Hình 2): chuỗi tường, thiết bị, ..., tường của hàng trên cùng (đặt trên vùng) và cột trái cùng
        /// (đặt bên trái vùng), tường lấy theo giao của hàng / cột với đa giác. Người dùng 21/11 (đầu phun dùng ké cách dim của báo cháy, một lần gọi):
        /// cách dim (có chéo góc hay không), kiểu dim, layer lấy ở trang Cài đặt như PCCC_RAI (KtSauRai); cheo = kt:"cheo" ép thêm dim chéo góc lồi.
        static void KtRai(GoiSpec g, List<Point2d> vung, LuoiRai r, bool cheo)
        {
            DimStyleTable dst = (DimStyleTable)g.Tr.GetObject(g.Db.DimStyleTableId, OpenMode.ForRead);
            string ten = CaiDat.DimKieu();
            bool coKieu = ten != "" && dst.Has(ten);
            ObjectId kieu = coKieu ? dst[ten] : DamBaoKieuKt(g);
            double cao = LaySo(NhomKyHieu("kt"), "cao", 250);
            ObjectId l = DamBaoLayer(g.Db, g.Tr, CaiDat.DimLop(), 8);
            foreach (KeyValuePair<List<Point2d>, List<Point3d>> kv in VungDim(vung, r, r.Diem))
                foreach (Dimension d in KtLuoi(kv.Key, kv.Value, cheo || CaiDat.DimDang() == CaiDat.DIM_CHUOI_CHEO, kieu, cao, EPS_DIM_M / HeSoMet(g.Db)))
                {
                    // như KtSauRai (người dùng 25/11: dim spec hiện 4 số lẻ): mặc định bản vẽ rồi gán kiểu, kiểu mới áp đủ định dạng số
                    d.SetDatabaseDefaults(g.Db);
                    d.DimensionStyle = kieu;
                    if (!coKieu) d.Dimscale = cao / 2.5; // chưa chọn kiểu: cỡ chữ theo ky_hieu.json
                    d.LayerId = l;
                    Them(g, d);
                }
        }

        /// Chuỗi kích thước thiết bị theo bản mẫu người dùng gửi 16/11 (hàng trùng toạ độ chỉ dim một lần, khác lưới mới dim thêm, dim sát thiết bị):
        /// lưới = cột (cùng X trong eps) có ≥ 2 thiết bị hoặc có thiết bị ở hàng chính (hàng đông nhất, trên cùng), hàng tương tự với cột chính (cột đông nhất, gần giữa vùng).
        /// MỘT chuỗi ngang tường – các cột lưới – tường dọc hàng chính, đường dim cách hàng 2 x cao; MỘT chuỗi đứng tường – các hàng lưới – tường dọc cột chính.
        /// Thiết bị lệch lưới: chuỗi riêng tường gần – thiết bị – cột (hàng) lưới gần nhất phía kia, sát thiết bị. cheo: thêm dim chéo tới góc lồi.
        /// Trả các dim chưa thêm vào bản vẽ (nơi gọi đặt layer, cỡ).
        static List<Dimension> KtLuoi(List<Point2d> vung, List<Point3d> diem, bool cheo, ObjectId kieu, double cao, double eps, List<Point2d[]> canh = null,
            List<Point2d> vatCan = null)
        {
            List<Dimension> kq = new List<Dimension>();
            if (diem.Count == 0) return kq;
            double ra = 2 * cao;
            List<Point2d> chuyen = vung.Select(p => new Point2d(p.Y, p.X)).ToList();
            // canh: mép dầm (người dùng 17/11: đầu báo trong khoang thì mép dầm là tường, dim từ mép dầm); đổi X/Y cho chuỗi đứng như vùng
            List<Point2d[]> canhT = canh == null ? null : canh.Select(c => new[] { new Point2d(c[0].Y, c[0].X), new Point2d(c[1].Y, c[1].X) }).ToList();
            List<List<Point3d>> hang = NhomToaDo(diem, false, eps), cot = NhomToaDo(diem, true, eps);
            List<Point3d> hangChinh = hang.OrderByDescending(h => h.Count).ThenByDescending(h => h[0].Y).First();
            double giua = (vung.Min(p => p.X) + vung.Max(p => p.X)) / 2;
            List<Point3d> cotChinh = cot.OrderByDescending(c => c.Count).ThenBy(c => Math.Abs(c[0].X - giua)).First();
            List<double> xs = cot.Where(c => c.Count >= 2 || hangChinh.Any(q => Math.Abs(q.X - c[0].X) <= eps)).Select(c => c[0].X).ToList();
            List<double> ys = hang.Where(h => h.Count >= 2 || cotChinh.Any(q => Math.Abs(q.Y - h[0].Y) <= eps)).Select(h => h[0].Y).ToList();
            // Người dùng 23/11 (Karaoke 123 tầng 2): hành lang hẹp một hàng -> chuỗi dọc hành lang lệch khỏi hàng đầu báo 1/4 bề rộng (giữa hàng và tường,
            // tối đa 2 cỡ chữ). Chiều 23/11: "không nằm ngang bằng đầu báo / đầu phun, trên hoặc dưới một khoảng vừa phải" (trước đó đi ngay trên hàng);
            // sáng 23/11: không sát tường, không đè số chuỗi ngang. Chuỗi ngang hành lang một lần, đặt ở đầu báo mà đường dim không cắt qua cột (vatCan = tâm cột). eps = 10 mm.
            double met = eps / 0.01;
            List<Point2d> vcT = vatCan == null ? null : vatCan.Select(p => new Point2d(p.Y, p.X)).ToList();
            Func<List<Point2d>, double, double, double> beRong = (d, duong, t) =>
            {
                double[] k = KhoangNgang(d, duong).FirstOrDefault(x => x[0] <= t + 1e-6 && x[1] >= t - 1e-6);
                return k == null ? double.MaxValue : k[1] - k[0];
            };
            // đường dim y = duong (đã đổi X/Y với chuỗi đứng) qua t cắt cột: tâm cột cách đường < 0,5 m, trong khoảng tường – tường nới 0,3 m
            Func<List<Point2d>, List<Point2d>, double, double, bool> catCot = (d, vc, duong, t) =>
            {
                double[] k = KhoangNgang(d, duong).FirstOrDefault(x => x[0] <= t + 1e-6 && x[1] >= t - 1e-6) ?? new[] { t, t };
                return vc != null && vc.Any(c => Math.Abs(c.Y - duong) < 0.5 * met && c.X > k[0] - 0.3 * met && c.X < k[1] + 0.3 * met);
            };
            double bx = beRong(chuyen, hangChinh[0].X, hangChinh[0].Y), by = beRong(vung, cotChinh[0].Y, cotChinh[0].X);
            double raX = hang.Count == 1 && hangChinh.Count >= 2 && bx <= 3 * met ? Math.Min(ra, bx / 4) : ra;
            double raY = cot.Count == 1 && cotChinh.Count >= 2 && by <= 3 * met ? Math.Min(ra, by / 4) : ra;
            if (raX > 0 && catCot(vung, vatCan, hangChinh[0].Y + raX, hangChinh[0].X) && !catCot(vung, vatCan, hangChinh[0].Y - raX, hangChinh[0].X)) raX = -raX;
            if (raY > 0)
            {
                // cột thiết bị cùng tập hàng với cột chính đo ra cùng một chuỗi: chọn cột + phía (phải / trái) không cắt cột, gần giữa vùng trước
                foreach (List<Point3d> c in cot.Where(c => c.Count == cotChinh.Count && c.All(p => cotChinh.Any(q => Math.Abs(q.Y - p.Y) <= eps)))
                    .OrderBy(c => c == cotChinh ? 0 : 1).ThenBy(c => Math.Abs(c[0].X - giua)))
                {
                    double s = !catCot(chuyen, vcT, c[0].X + raY, c[0].Y) ? raY : (!catCot(chuyen, vcT, c[0].X - raY, c[0].Y) ? -raY : 0);
                    if (s != 0) { cotChinh = c; raY = s; break; }
                }
            }
            kq.AddRange(ChuoiTuong(vung, xs, hangChinh[0].Y, hangChinh[0].X, true, raX, kieu, false, canh));
            kq.AddRange(ChuoiTuong(chuyen, ys, cotChinh[0].X, cotChinh[0].Y, false, raY, kieu, false, canhT));
            // Hàng (cột) có thiết bị lệch lưới: ≥ 2 thiết bị (rải so le) thì một chuỗi cho cả hàng; một thiết bị thì chuỗi riêng tường – thiết bị – lưới.
            foreach (List<Point3d> h in hang)
            {
                if (h == hangChinh || h.All(p => xs.Any(x => Math.Abs(x - p.X) <= eps))) continue;
                if (h.Count >= 2) kq.AddRange(ChuoiTuong(vung, h.Select(p => p.X).ToList(), h[0].Y, h[0].X, true, ra, kieu, false, canh));
                else kq.AddRange(ChuoiTuong(vung, xs, h[0].Y, h[0].X, true, ra, kieu, true, canh));
            }
            foreach (List<Point3d> c in cot)
            {
                if (c == cotChinh || c.All(p => ys.Any(y => Math.Abs(y - p.Y) <= eps))) continue;
                if (c.Count >= 2) kq.AddRange(ChuoiTuong(chuyen, c.Select(p => p.Y).ToList(), c[0].X, c[0].Y, false, ra, kieu, false, canhT));
                else kq.AddRange(ChuoiTuong(chuyen, ys, c[0].X, c[0].Y, false, ra, kieu, true, canhT));
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
        static List<RotatedDimension> ChuoiTuong(List<Point2d> d, List<double> luoi, double duong, double t, bool doX, double ra, ObjectId kieu, bool le,
            List<Point2d[]> canh = null)
        {
            double[] k = KhoangNgang(d, duong).FirstOrDefault(x => x[0] <= t + 1e-6 && x[1] >= t - 1e-6);
            // Mép dầm cắt đường dim (canh đã đổi X/Y như d): mốc như tường.
            List<double> mep = new List<double>();
            if (canh != null && k != null)
                foreach (Point2d[] c in canh)
                {
                    double dy = c[1].Y - c[0].Y;
                    if (Math.Abs(dy) < 1e-9 || (c[0].Y - duong) * (c[1].Y - duong) > 0) continue;
                    double x = c[0].X + (c[1].X - c[0].X) * (duong - c[0].Y) / dy;
                    if (x > k[0] + 1e-6 && x < k[1] - 1e-6) mep.Add(x);
                }
            List<double> v = new List<double>();
            if (!le)
            {
                v.AddRange(k == null ? luoi : luoi.Where(x => x >= k[0] - 1e-6 && x <= k[1] + 1e-6));
                if (k != null) { v.Add(k[0]); v.Add(k[1]); }
                v.AddRange(mep);
            }
            else
            {
                v.Add(t);
                if (k != null)
                {
                    // tường hoặc mép dầm gần nhất hai phía
                    double tr0 = mep.Where(x => x < t).Concat(new[] { k[0] }).Max(), ph1 = mep.Where(x => x > t).Concat(new[] { k[1] }).Min();
                    bool trai = t - tr0 <= ph1 - t; // tường gần ở phía nhỏ
                    v.Add(trai ? tr0 : ph1);
                    IEnumerable<double> kia = trai ? luoi.Where(x => x > t && x <= ph1) : luoi.Where(x => x < t && x >= tr0);
                    if (kia.Any()) v.Add(trai ? kia.Min() : kia.Max());
                }
            }
            List<Point3d> moc = GomMoc(v.Select(x => doX ? new Point3d(x, duong, 0) : new Point3d(duong, x, 0)), doX, 1e-6);
            if (moc.Count < 2) return new List<RotatedDimension>();
            return ChuoiKt(moc, doX, doX ? new Point3d(moc[0].X, duong + ra, 0) : new Point3d(duong + ra, moc[0].Y, 0), kieu);
        }

        /// Dim tự động sau PCCC_RAI theo trang Cài đặt (kiểu dim, cách dim, layer). Trả số dim đã vẽ.
        static int KtSauRai(Database db, Transaction tr, BlockTableRecord ms, List<Point2d> vung, List<Point3d> diem, List<Point2d[]> canh = null, List<Point2d> vatCan = null)
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
            foreach (Dimension d in KtLuoi(vung, diem, dang == CaiDat.DIM_CHUOI_CHEO, kieu, cao, EPS_DIM_M / HeSoMet(db), canh, vatCan))
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

    }
}
