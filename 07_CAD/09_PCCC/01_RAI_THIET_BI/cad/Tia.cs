// Đầu báo tia chiếu BC_BEAM: tia, dim bước tia. Tách từ Rai.cs (26/11/2026: mỗi thư mục số của 07_CAD là một hạng mục tính năng, xem 07_CAD/README.md).
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
        const string LAYER_TIA = "PCCC_BC_TIA";
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

        /// Chuỗi kích thước bước tia như bộ bản vẽ mẫu (07_CAD/01_TRI_THUC_VE/mau/hoc-ban-ve-mau-dwg.md mục 2): tường – tia – … – tia – tường, đo vuông góc tia,
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

    }
}
