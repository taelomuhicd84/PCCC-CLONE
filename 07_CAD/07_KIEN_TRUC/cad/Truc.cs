// Lưới trục (thao tác spec luoi_truc). Tách từ Spec.cs (26/11/2026: mỗi thư mục số của 07_CAD là một hạng mục tính năng, xem 07_CAD/README.md).
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
using Autodesk.AutoCAD.PlottingServices;
using AcApp = Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace TKPCCC
{
    public partial class Lenh
    {
        /// "1" -> 1, 2, 3...; "A" -> A, B, C... (quá Z thì AA, AB...).
        static string TenTruc(string dau, int i)
        {
            int n;
            if (int.TryParse(dau, out n)) return (n + i).ToString(CultureInfo.InvariantCulture);
            int c = char.ToUpperInvariant(dau[0]) - 'A' + i;
            return (c >= 26 ? ((char)('A' + c / 26 - 1)).ToString() : "") + (char)('A' + c % 26);
        }

        static void Bong(GoiSpec g, Point3d tam, string ten, double r, double cao, ObjectId layer)
        {
            Circle c = new Circle(tam, Vector3d.ZAxis, r);
            c.LayerId = layer;
            Them(g, c);
            Them(g, TaoChu(g, tam, ten, cao, 0, true, layer));
        }

        /// Lưới trục: x, y là dãy NHỊP (khoảng cách giữa các trục). Trục đứng đánh số theo ten_x, trục ngang theo ten_y.
        static void OpLuoiTruc(GoiSpec g, Dictionary<string, object> o)
        {
            List<double> xs = CongDon(Lay(o, "x"), "x"), ys = CongDon(Lay(o, "y"), "y");
            Dictionary<string, object> k = NhomKyHieu("luoi_truc");
            double cao = LaySo(o, "cao", LaySo(k, "cao", 350)), r = LaySo(o, "r", cao * LaySo(k, "r", 1.15)), du = LaySo(o, "du", cao * LaySo(k, "du", 5));
            Point3d O = Lay(o, "p") == null ? new Point3d(g.Gx, g.Gy, 0) : P(g, Lay(o, "p"), "p");
            string tx = LayChu(o, "ten_x", "1"), ty = LayChu(o, "ten_y", "A");
            if (tx == "" || ty == "") throw new System.Exception("ten_x, ten_y không được rỗng");
            double W = xs[xs.Count - 1], H = ys[ys.Count - 1];
            ObjectId lTruc = DamBaoLayer(g.Db, g.Tr, LayChu(k, "layer_truc", "TRUC"), (short)LaySo(k, "mau_truc", 1));
            ObjectId lKh = DamBaoLayer(g.Db, g.Tr, LayChu(k, "layer_bong", "KH_TRUC"), (short)LaySo(k, "mau_bong", 3));
            ObjectId lKt = DamBaoLayer(g.Db, g.Tr, LayChu(k, "layer_kt", "DIM_TRUC"), (short)LaySo(k, "mau_kt", 5));
            ObjectId net = DamBaoNet(g, LayChu(k, "net", "CENTER"));
            double tlNet = cao / LaySo(k, "ti_le_net", 7);

            for (int i = 0; i < xs.Count; i++)
            {
                double x = O.X + xs[i];
                Line ln = new Line(new Point3d(x, O.Y - du, 0), new Point3d(x, O.Y + H + du, 0));
                ln.LayerId = lTruc; ln.LinetypeId = net; ln.LinetypeScale = tlNet;
                Them(g, ln);
                Bong(g, new Point3d(x, O.Y - du - r, 0), TenTruc(tx, i), r, cao, lKh);
                Bong(g, new Point3d(x, O.Y + H + du + r, 0), TenTruc(tx, i), r, cao, lKh);
            }
            for (int i = 0; i < ys.Count; i++)
            {
                double y = O.Y + ys[i];
                Line ln = new Line(new Point3d(O.X - du, y, 0), new Point3d(O.X + W + du, y, 0));
                ln.LayerId = lTruc; ln.LinetypeId = net; ln.LinetypeScale = tlNet;
                Them(g, ln);
                Bong(g, new Point3d(O.X - du - r, y, 0), TenTruc(ty, i), r, cao, lKh);
                Bong(g, new Point3d(O.X + W + du + r, y, 0), TenTruc(ty, i), r, cao, lKh);
            }
            if (!LayCo(o, "kt", true)) return;
            // Kích thước từng nhịp (gần lưới) và tổng (xa hơn), phía dưới và bên trái.
            double c1 = du * LaySo(k, "kt_gan", 0.4), c2 = du * LaySo(k, "kt_xa", 0.75), caoKt = cao * LaySo(k, "kt_cao", 0.7);
            for (int i = 0; i + 1 < xs.Count; i++)
                VeKt(g, new Point3d(O.X + xs[i], O.Y, 0), new Point3d(O.X + xs[i + 1], O.Y, 0), -c1, caoKt, lKt);
            if (xs.Count > 2) VeKt(g, new Point3d(O.X, O.Y, 0), new Point3d(O.X + W, O.Y, 0), -c2, caoKt, lKt);
            for (int i = 0; i + 1 < ys.Count; i++)
                VeKt(g, new Point3d(O.X, O.Y + ys[i], 0), new Point3d(O.X, O.Y + ys[i + 1], 0), c1, caoKt, lKt);
            if (ys.Count > 2) VeKt(g, new Point3d(O.X, O.Y, 0), new Point3d(O.X, O.Y + H, 0), c2, caoKt, lKt);
        }

    }
}
