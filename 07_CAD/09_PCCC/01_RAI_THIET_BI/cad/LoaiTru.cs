// Vùng loại trừ PCCC_LOAITRU, ống gió, vẽ tạm xem trước PCCC_XEM_TRUOC. Tách từ Rai.cs (26/11/2026: mỗi thư mục số của 07_CAD là một hạng mục tính năng, xem 07_CAD/README.md).
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

    }
}
