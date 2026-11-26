// Dây báo cháy nối tiếp thiết bị (PCCC_DAY), nhãn zone, điện trở cuối dây. Tách từ Rai.cs (26/11/2026: mỗi thư mục số của 07_CAD là một hạng mục tính năng, xem 07_CAD/README.md).
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
