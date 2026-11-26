// Đặt thiết bị, vẽ ống / dây theo mã (PCCC_DAT, PCCC_ONG). Tách từ Lenh.cs (26/11/2026: mỗi thư mục số của 07_CAD là một hạng mục tính năng, xem 07_CAD/README.md).
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Web.Script.Serialization;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using AcApp = Autodesk.AutoCAD.ApplicationServices.Core.Application; // Core: chạy được cả trong accoreconsole (không giao diện)

namespace TKPCCC
{
    public partial class Lenh
    {
        static void DatThietBi()
        {
            Document doc = AcApp.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;
            FileDanhMuc dm = DocDanhMuc();
            VatTu v = HoiMa(ed, dm, "block", "Mã thiết bị");
            if (v == null) return;

            bool co;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                co = ((BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead)).Has(v.ma);
                tr.Commit();
            }
            if (!co) NapVaoBanVe(doc, dm);
            double k = 0.001 / HeSoMet(db); // block thư viện vẽ theo mm

            while (true)
            {
                PromptPointOptions o = new PromptPointOptions("\nĐiểm đặt " + v.ten + " <Enter = xong>: ");
                o.AllowNone = true;
                PromptPointResult r = ed.GetPoint(o);
                if (r.Status != PromptStatus.OK) return;
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    ChenThietBi(db, tr, v, r.Value.TransformBy(ed.CurrentUserCoordinateSystem), k, 0); // 07_CAD/06_VE_TU_DONG_AI/cad/Spec.cs
                    tr.Commit();
                }
            }
        }

        static void VeOng()
        {
            Document doc = AcApp.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            FileDanhMuc dm = DocDanhMuc();
            Editor ed = doc.Editor;
            VatTu v = HoiMa(ed, dm, "tuyen", "Mã ống/dây (vd ONG_DN50, DAY_BC)");
            if (v == null) return;
            ObjectId lop;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                lop = DamBaoLayer(db, tr, TIEN_TO_LAYER + v.ma, MauHe(v.he));
                db.Clayer = lop;
                tr.Commit();
            }
            ed.WriteMessage("\nLayer hiện hành: " + TIEN_TO_LAYER + v.ma + ". Vẽ tuyến " + v.ten + ".");
            // Tự hỏi điểm (16/11: SendStringToExecute("_.PLINE") chạy sau khi lệnh xong nên trong file .scr toạ độ bị đọc thành lệnh).
            List<Point3d> ps = new List<Point3d>();
            ObjectId id = ObjectId.Null;
            while (true)
            {
                PromptPointOptions o = new PromptPointOptions(ps.Count == 0 ? "\nĐiểm đầu tuyến: " : "\nĐiểm tiếp theo <Enter = xong>: ");
                o.AllowNone = ps.Count > 0;
                if (ps.Count > 0) { o.UseBasePoint = true; o.BasePoint = ps[ps.Count - 1].TransformBy(ed.CurrentUserCoordinateSystem.Inverse()); }
                PromptPointResult r = ed.GetPoint(o);
                if (r.Status != PromptStatus.OK) break;
                Point3d p = r.Value.TransformBy(ed.CurrentUserCoordinateSystem);
                if (ps.Count > 0 && p.DistanceTo(ps[ps.Count - 1]) < 1e-6) continue;
                ps.Add(p);
                if (ps.Count < 2) continue;
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    if (id.IsNull)
                    {
                        BlockTableRecord ms = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);
                        Polyline pl = TaoPline(ps, false, lop);
                        id = ms.AppendEntity(pl);
                        tr.AddNewlyCreatedDBObject(pl, true);
                    }
                    else
                    {
                        Polyline pl = (Polyline)tr.GetObject(id, OpenMode.ForWrite);
                        pl.AddVertexAt(pl.NumberOfVertices, new Point2d(p.X, p.Y), 0, 0, 0);
                    }
                    tr.Commit();
                }
            }
            if (id.IsNull) { ed.WriteMessage("\n[TKPCCC] PCCC_ONG: cần ít nhất 2 điểm, chưa vẽ."); return; }
            double dai = 0;
            for (int i = 1; i < ps.Count; i++) dai += ps[i].DistanceTo(ps[i - 1]);
            ed.WriteMessage("\n[TKPCCC] PCCC_ONG " + v.ma + " doan=" + (ps.Count - 1) + " m=" + Math.Round(dai * HeSoMet(db), 2).ToString(CultureInfo.InvariantCulture));
        }

        // ---------- Bóc khối lượng ----------
        // Ý tưởng tham khảo LHB Block Scheduler (chỉ tính năng, không dùng mã): không đếm trùng vị trí, đếm block lồng
        // và MINSERT, bỏ XREF, tách theo tầng/khu vực, gán tên block có sẵn -> mã vật tư, xuất bảng Table, vùng bảo vệ.

    }
}
