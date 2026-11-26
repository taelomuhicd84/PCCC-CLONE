// Vòng bán kính bảo vệ (PCCC_VUNGBV). Tách từ Lenh.cs (26/11/2026: mỗi thư mục số của 07_CAD là một hạng mục tính năng, xem 07_CAD/README.md).
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
        static void VungBaoVe()
        {
            Document doc = AcApp.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;
            FileDanhMuc dm = DocDanhMuc();
            VatTu v = HoiMa(ed, dm, "block", "Mã thiết bị cần vẽ vùng bảo vệ (vd BC_KHOI, SPR_DAU_DOWN)");
            if (v == null) return;
            // Plugin không tra tiêu chuẩn: bán kính do người thiết kế nhập theo TCVN đang áp dụng.
            PromptDoubleOptions ro = new PromptDoubleOptions("\nBán kính bảo vệ R (m) theo tiêu chuẩn áp dụng: ");
            ro.AllowNegative = false; ro.AllowZero = false;
            PromptDoubleResult rr = ed.GetDouble(ro);
            if (rr.Status != PromptStatus.OK) return;
            double r = rr.Value / HeSoMet(db);
            string tenLop = "PCCC_VUNGBV_" + v.ma; // mỗi loại 1 layer: chạy lại chỉ thay vòng của loại đó

            BoiCanh bc = new BoiCanh();
            bc.Dm = dm; bc.Alias = DocAlias();
            List<Point3d> tam = new List<Point3d>();
            int xoa = 0;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                bc.Tr = tr;
                BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                BlockTableRecord ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);
                ObjectId lop = DamBaoLayer(db, tr, tenLop, 8);
                LayerTableRecord lr = (LayerTableRecord)tr.GetObject(lop, OpenMode.ForWrite);
                // Tủ họng vách tường: bộ bản vẽ mẫu IN vòng bán kính bảo vệ bằng nét đứt (hoc-ban-ve-mau-dwg.md mục 4); loại khác chỉ để kiểm, không in.
                bool inVong = v.he == "HVT";
                lr.IsPlottable = inVong;
                if (inVong)
                {
                    LinetypeTable lt = (LinetypeTable)tr.GetObject(db.LinetypeTableId, OpenMode.ForRead);
                    if (!lt.Has("DASHED")) foreach (string f in new[] { "acadiso.lin", "acad.lin" }) { try { db.LoadLineTypeFile("DASHED", f); break; } catch (System.Exception) { } }
                    if (lt.Has("DASHED")) lr.LinetypeObjectId = lt["DASHED"];
                }
                double tol = 0.001 / HeSoMet(db);
                HashSet<string> daCo = new HashSet<string>(); // bản copy đè cùng điểm chỉ vẽ 1 vòng
                foreach (ObjectId id in ms.Cast<ObjectId>().ToList())
                {
                    Entity e = tr.GetObject(id, OpenMode.ForRead) as Entity;
                    BlockReference br = e as BlockReference;
                    if (br != null && MaThietBi(br, bc) == v.ma)
                    {
                        if (daCo.Add(Math.Round(br.Position.X / tol) + "|" + Math.Round(br.Position.Y / tol))) tam.Add(br.Position);
                    }
                    else if (e is Circle && e.LayerId == lop) { e.UpgradeOpen(); e.Erase(); xoa++; }
                }
                foreach (Point3d p in tam)
                {
                    Circle c = new Circle(p, Vector3d.ZAxis, r);
                    c.LayerId = lop;
                    ms.AppendEntity(c); tr.AddNewlyCreatedDBObject(c, true);
                }
                tr.Commit();
            }
            // Khoảng hở: thiết bị không có thiết bị cùng loại nào trong phạm vi 2R (vùng bảo vệ không chạm nhau).
            int le = tam.Count(p => tam.Count(q => q != p && p.DistanceTo(q) <= 2 * r) == 0);
            ed.WriteMessage("\nĐã vẽ " + tam.Count + " vùng bảo vệ R = " + rr.Value + " m trên layer " + tenLop + (v.he == "HVT" ? " (nét đứt, in được)" : " (không in)")
                + (xoa > 0 ? ", thay " + xoa + " vòng cũ" : "") + "."
                + (le > 0 && tam.Count > 1 ? " Có " + le + " thiết bị đứng lẻ (cách thiết bị gần nhất > 2R) — kiểm tra khoảng hở." : ""));
        }
    }
}
