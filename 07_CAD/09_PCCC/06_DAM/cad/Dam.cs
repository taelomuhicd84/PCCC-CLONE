// Dầm có thông số b x h (người dùng duyệt 27/11): PCCC_DAM vẽ dầm theo tim, PCCC_GANDAM gán cỡ cho dầm vẽ sẵn hoặc nét dầm của bản kết cấu chép vào.
// Cỡ ghi vào chính đối tượng (XData app TKPCCC_DAM: "b x h", b mm, h mm, handle nhãn) nên lưu cùng bản vẽ; PCCC_RAI (LoaiTran KhongTran / HonHop)
// đọc dầm này thay cho bấm nét dầm mẫu + nhãn, spec "doc" trả trường "dam", thao tác spec "dam" vẽ / gán như hai lệnh.
// Không có dầm (không mặt bằng dầm): PCCC_RAI Enter mặc định = CoTran, rải như trần phẳng.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using AcApp = Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace TKPCCC
{
    public partial class Lenh
    {
        const string APP_DAM = "TKPCCC_DAM";
        const string LAYER_DAM = "PCCC_DAM";
        const string LAYER_NHAN_DAM = "PCCC_DAM_NHAN";
        const short MAU_DAM = 4; // xanh lơ
        static string bxhTruoc = ""; // cỡ gõ lần trước (mặc định lần sau)

        [CommandMethod("PCCC_DAM")] public void LenhDam() { Chay(VeDamTay); }
        [CommandMethod("PCCC_GANDAM")] public void LenhGanDam() { Chay(GanDamTay); }

        /// "b x h" gõ tay, đúng thứ tự rộng x cao (mm). false = không đọc được hoặc ngoài khoảng (b 50–2000, h 100–3000).
        static bool DocBxhTay(string s, out double b, out double h)
        {
            b = h = 0;
            System.Text.RegularExpressions.Match k = System.Text.RegularExpressions.Regex.Match(s ?? "", @"(\d{2,4})\s*[xX×\*]\s*(\d{2,4})");
            if (!k.Success) return false;
            b = double.Parse(k.Groups[1].Value, CultureInfo.InvariantCulture);
            h = double.Parse(k.Groups[2].Value, CultureInfo.InvariantCulture);
            return b >= 50 && b <= 2000 && h >= 100 && h <= 3000;
        }

        static string ChuBxh(double b, double h) { return Math.Round(b).ToString(CultureInfo.InvariantCulture) + "x" + Math.Round(h).ToString(CultureInfo.InvariantCulture); }

        /// Cỡ dầm đã gán của đối tượng; false = không phải dầm có thông số (nhãn dầm cũng trả false).
        static bool DocDam(Entity e, out double b, out double h)
        {
            b = h = 0;
            ResultBuffer rb = e.GetXDataForApplication(APP_DAM);
            if (rb == null) return false;
            TypedValue[] v = rb.AsArray();
            rb.Dispose();
            if (v.Length < 4 || Convert.ToString(v[1].Value) == "nhan") return false;
            b = Convert.ToDouble(v[2].Value, CultureInfo.InvariantCulture);
            h = Convert.ToDouble(v[3].Value, CultureInfo.InvariantCulture);
            return b > 0 && h > 0;
        }

        /// "400x600" của dầm có thông số, null = không phải (spec "doc" trả trường "dam").
        static string BxhCuaDt(Entity e)
        {
            double b, h;
            return DocDam(e, out b, out h) ? ChuBxh(b, h) : null;
        }

        static void DangKyAppDam(Database db, Transaction tr)
        {
            RegAppTable rat = (RegAppTable)tr.GetObject(db.RegAppTableId, OpenMode.ForRead);
            if (rat.Has(APP_DAM)) return;
            rat.UpgradeOpen();
            RegAppTableRecord r = new RegAppTableRecord();
            r.Name = APP_DAM;
            rat.Add(r);
            tr.AddNewlyCreatedDBObject(r, true);
        }

        /// Dầm bẹt (rộng > cao, vd 1400x250): hỏi lại, không tự đảo (người dùng 17/11 + 27/11). false = huỷ lệnh.
        static bool HoiDamBet(Editor ed, ref double b, ref double h)
        {
            if (b <= h) return true;
            PromptKeywordOptions o = new PromptKeywordOptions("\nDầm bẹt: rộng " + Math.Round(b) + " lớn hơn cao " + Math.Round(h) + " mm. Giữ đúng như vậy hay đảo (rộng " + Math.Round(h) + ", cao " + Math.Round(b) + ")?");
            o.Keywords.Add("Giu"); o.Keywords.Add("DaoNguoc");
            o.Keywords.Default = "Giu";
            o.AllowNone = true;
            PromptResult r = ed.GetKeywords(o);
            if (r.Status == PromptStatus.Cancel) return false;
            if (r.StringResult == "DaoNguoc") { double t = b; b = h; h = t; }
            return true;
        }

        /// Hỏi cỡ b x h (mặc định cỡ lần trước). false = huỷ.
        static bool HoiBxh(Editor ed, string loiNhac, out double b, out double h)
        {
            b = h = 0;
            while (true)
            {
                PromptStringOptions o = new PromptStringOptions(loiNhac);
                o.AllowSpaces = false;
                if (bxhTruoc != "") { o.DefaultValue = bxhTruoc; o.UseDefaultValue = true; }
                PromptResult r = ed.GetString(o);
                if (r.Status != PromptStatus.OK) return false;
                if (!DocBxhTay(r.StringResult, out b, out h)) { ed.WriteMessage("\nKhông đọc được b x h \"" + r.StringResult + "\" (vd 400x600: rộng 400, cao 600 mm)."); continue; }
                if (!HoiDamBet(ed, ref b, ref h)) return false;
                bxhTruoc = ChuBxh(b, h);
                return true;
            }
        }

        /// Ghi cỡ vào dầm và đặt / sửa nhãn "D bxh" cạnh dầm (dọc theo đoạn dài nhất). Trả nhãn (mới tạo thì đã thêm vào ms).
        static MText GhiDam(Database db, Transaction tr, BlockTableRecord ms, Entity e, double b, double h, Action<Entity> them)
        {
            DangKyAppDam(db, tr);
            double m = HeSoMet(db), cao = 0.25 / m;
            MText nhan = null;
            ResultBuffer cu = e.GetXDataForApplication(APP_DAM);
            if (cu != null)
            {
                foreach (TypedValue t in cu.AsArray())
                    if (t.TypeCode == (int)DxfCode.ExtendedDataHandle)
                    {
                        try
                        {
                            ObjectId id = db.GetObjectId(false, new Handle(Convert.ToInt64(Convert.ToString(t.Value), 16)), 0);
                            if (!id.IsNull && !id.IsErased) nhan = tr.GetObject(id, OpenMode.ForWrite) as MText;
                        }
                        catch (System.Exception) { }
                    }
                cu.Dispose();
            }
            string chu = "D " + ChuBxh(b, h);
            if (nhan == null)
            {
                // Vị trí nhãn: giữa đoạn dài nhất, lệch ra ngoài mép dầm.
                Point2d a = Point2d.Origin, c = Point2d.Origin;
                double dai = -1;
                Line ln = e as Line;
                Polyline pl = e as Polyline;
                if (ln != null) { a = new Point2d(ln.StartPoint.X, ln.StartPoint.Y); c = new Point2d(ln.EndPoint.X, ln.EndPoint.Y); }
                else if (pl != null)
                    for (int i = 0; i < (pl.Closed ? pl.NumberOfVertices : pl.NumberOfVertices - 1); i++)
                    {
                        Point2d p = pl.GetPoint2dAt(i), q = pl.GetPoint2dAt((i + 1) % pl.NumberOfVertices);
                        if (p.GetDistanceTo(q) > dai) { dai = p.GetDistanceTo(q); a = p; c = q; }
                    }
                Vector2d u = c - a;
                if (u.Length < 1e-9) u = new Vector2d(1, 0);
                u = u.GetNormal();
                if (u.X < -1e-9 || (Math.Abs(u.X) <= 1e-9 && u.Y < 0)) u = -u; // chữ đọc từ trái / từ dưới
                Vector2d n = new Vector2d(-u.Y, u.X);
                Point2d g = a + (c - a) * 0.5;
                bool duoi = false; // dầm kín: nhãn ra phía ngoài dầm (mép dài nằm phía dưới tâm thì chữ treo dưới mép)
                if (pl != null && pl.Closed && pl.NumberOfVertices > 2)
                {
                    double sx = 0, sy = 0;
                    for (int i = 0; i < pl.NumberOfVertices; i++) { sx += pl.GetPoint2dAt(i).X; sy += pl.GetPoint2dAt(i).Y; }
                    duoi = (g - new Point2d(sx / pl.NumberOfVertices, sy / pl.NumberOfVertices)).DotProduct(n) < 0;
                }
                g = g + n * (duoi ? -cao * 0.3 : cao * 0.3);
                nhan = new MText();
                nhan.SetDatabaseDefaults(db);
                nhan.LayerId = DamBaoLayer(db, tr, LAYER_NHAN_DAM, MAU_DAM);
                nhan.TextStyleId = DamBaoKieuChu(db, tr);
                nhan.TextHeight = cao;
                nhan.Attachment = duoi ? AttachmentPoint.TopCenter : AttachmentPoint.BottomCenter;
                nhan.Location = new Point3d(g.X, g.Y, 0);
                nhan.Rotation = Math.Atan2(u.Y, u.X);
                nhan.Contents = chu;
                them(nhan);
            }
            else nhan.Contents = chu;
            nhan.XData = new ResultBuffer(new TypedValue((int)DxfCode.ExtendedDataRegAppName, APP_DAM), new TypedValue((int)DxfCode.ExtendedDataAsciiString, "nhan"),
                new TypedValue((int)DxfCode.ExtendedDataHandle, e.Handle.ToString()));
            e.XData = new ResultBuffer(new TypedValue((int)DxfCode.ExtendedDataRegAppName, APP_DAM), new TypedValue((int)DxfCode.ExtendedDataAsciiString, ChuBxh(b, h)),
                new TypedValue((int)DxfCode.ExtendedDataReal, b), new TypedValue((int)DxfCode.ExtendedDataReal, h),
                new TypedValue((int)DxfCode.ExtendedDataHandle, nhan.Handle.ToString()));
            return nhan;
        }

        /// Dầm b x h theo tim a–c: polyline kín rộng b (hai mép là ranh khoang) trên layer PCCC_DAM, nét đứt, kèm nhãn.
        static Polyline TaoDam(Database db, Transaction tr, BlockTableRecord ms, Point2d a, Point2d c, double b, double h, Action<Entity> them)
        {
            double m = HeSoMet(db), L = a.GetDistanceTo(c);
            if (L < 0.1 / m) throw new System.Exception("dầm ngắn hơn 0,1 m");
            Vector2d u = (c - a) / L, n = new Vector2d(-u.Y, u.X) * (b / 1000 / m / 2);
            Polyline pl = new Polyline();
            pl.SetDatabaseDefaults(db);
            pl.AddVertexAt(0, a + n, 0, 0, 0);
            pl.AddVertexAt(1, c + n, 0, 0, 0);
            pl.AddVertexAt(2, c - n, 0, 0, 0);
            pl.AddVertexAt(3, a - n, 0, 0, 0);
            pl.Closed = true;
            pl.LayerId = DamBaoLayer(db, tr, LAYER_DAM, MAU_DAM);
            try
            {
                LinetypeTable ltt = (LinetypeTable)tr.GetObject(db.LinetypeTableId, OpenMode.ForRead);
                if (!ltt.Has("DASHED")) db.LoadLineTypeFile("DASHED", "acad.lin");
                if (ltt.Has("DASHED")) { pl.Linetype = "DASHED"; pl.LinetypeScale = 0.6 / m / 0.75; } // mẫu DASHED 0,75 đơn vị -> 0,6 m
            }
            catch (System.Exception) { } // không có acad.lin: nét liền
            them(pl);
            GhiDam(db, tr, ms, pl, b, h, them);
            return pl;
        }

        static void VeDamTay()
        {
            Document doc = AcApp.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;
            KiemDonVi(doc);
            double b, h;
            if (!HoiBxh(ed, "\nKích thước dầm b x h (rộng x cao, mm), vd 400x600: ", out b, out h)) return;
            int n = 0;
            while (true)
            {
                PromptPointOptions o1 = new PromptPointOptions("\nĐiểm đầu tim dầm " + ChuBxh(b, h) + ", Enter = xong");
                o1.Keywords.Add("KichThuoc"); // AutoCAD tự nối " [KichThuoc]: "
                o1.AllowNone = true;
                PromptPointResult r1 = ed.GetPoint(o1);
                if (r1.Status == PromptStatus.Keyword) { if (!HoiBxh(ed, "\nKích thước dầm b x h (rộng x cao, mm): ", out b, out h)) break; continue; }
                if (r1.Status != PromptStatus.OK) break;
                PromptPointOptions o2 = new PromptPointOptions("\nĐiểm cuối tim dầm: ");
                o2.UseBasePoint = true; o2.BasePoint = r1.Value;
                PromptPointResult r2 = ed.GetPoint(o2);
                if (r2.Status != PromptStatus.OK) break;
                Point3d p1 = r1.Value.TransformBy(ed.CurrentUserCoordinateSystem), p2 = r2.Value.TransformBy(ed.CurrentUserCoordinateSystem);
                using (doc.LockDocument())
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    BlockTableRecord ms = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);
                    TaoDam(db, tr, ms, new Point2d(p1.X, p1.Y), new Point2d(p2.X, p2.Y), b, h, e => { ms.AppendEntity(e); tr.AddNewlyCreatedDBObject(e, true); });
                    tr.Commit();
                }
                n++;
            }
            ed.WriteMessage("\n[TKPCCC] PCCC_DAM n=" + n + " bxh=" + ChuBxh(b, h) + " (layer " + LAYER_DAM + "; PCCC_RAI LoaiTran KhongTran đọc cỡ này, không cần bấm nét dầm mẫu).");
        }

        static void GanDamTay()
        {
            Document doc = AcApp.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;
            double b, h;
            if (!HoiBxh(ed, "\nKích thước dầm b x h (rộng x cao, mm) cần gán, vd 400x600: ", out b, out h)) return;
            PromptSelectionOptions o = new PromptSelectionOptions();
            o.MessageForAdding = "\nQuét chọn dầm (line / polyline: dầm PCCC_DAM, nét dầm của bản kết cấu; tim hoặc hai mép): ";
            PromptSelectionResult r = ed.GetSelection(o, new SelectionFilter(new[] { new TypedValue((int)DxfCode.Start, "LINE,LWPOLYLINE") }));
            if (r.Status != PromptStatus.OK) return;
            int n = 0;
            using (doc.LockDocument())
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTableRecord ms = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);
                foreach (ObjectId id in r.Value.GetObjectIds())
                {
                    Entity e = tr.GetObject(id, OpenMode.ForWrite) as Entity;
                    if (e == null || e.Layer == LAYER_NHAN_DAM) continue;
                    GhiDam(db, tr, ms, e, b, h, x => { ms.AppendEntity(x); tr.AddNewlyCreatedDBObject(x, true); });
                    n++;
                }
                tr.Commit();
            }
            ed.WriteMessage("\n[TKPCCC] PCCC_GANDAM n=" + n + " bxh=" + ChuBxh(b, h) + ".");
        }

        /// Dầm có thông số trong các đối tượng chọn (PCCC_RAI). idDam nhận mọi dầm (không dựng làm tường, kể cả khi rải trần phẳng).
        /// Trả nét chia khoang của dầm sâu (d = h - dày sàn > 0,3 m, 5.9.1.1.6): dầm PCCC_DAM lấy hai mép dài; nét gán tay như TungDam (tim -> ± b/2,
        /// hai mép giữ). dem = { số dầm, sâu, nông }. lech: cỡ đã gán khác nhãn b x h của bản vẽ cạnh dầm (≤ 1,5 m, song song) để báo người dùng chốt.
        static List<Point2d[]> DamThongSo(Database db, ObjectId[] ids, double daySan, List<Point2d[]> tim, HashSet<ObjectId> idDam, out int[] dem, List<string> lech)
        {
            List<Point2d[]> kq = new List<Point2d[]>();
            dem = new int[3];
            double m = HeSoMet(db);
            List<KeyValuePair<List<Point2d[]>, double[]>> ds = new List<KeyValuePair<List<Point2d[]>, double[]>>();
            List<ObjectId> khac = new List<ObjectId>();
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                foreach (ObjectId id in ids)
                {
                    Entity e = tr.GetObject(id, OpenMode.ForRead) as Entity;
                    if (e == null) continue;
                    double b, h;
                    if (!DocDam(e, out b, out h))
                    {
                        ResultBuffer rb = e.GetXDataForApplication(APP_DAM);
                        if (rb != null) rb.Dispose(); // nhãn dầm của plugin: không đối chiếu với chính nó
                        else khac.Add(id);
                        continue;
                    }
                    idDam.Add(id);
                    List<Point2d[]> doan = new List<Point2d[]>();
                    Line ln = e as Line;
                    Polyline pl = e as Polyline;
                    if (ln != null) doan.Add(new[] { new Point2d(ln.StartPoint.X, ln.StartPoint.Y), new Point2d(ln.EndPoint.X, ln.EndPoint.Y) });
                    else if (pl != null)
                        for (int i = 0; i < (pl.Closed ? pl.NumberOfVertices : pl.NumberOfVertices - 1); i++)
                            doan.Add(new[] { pl.GetPoint2dAt(i), pl.GetPoint2dAt((i + 1) % pl.NumberOfVertices) });
                    // Dầm PCCC_DAM (polyline kín 4 đỉnh): hai cạnh dài là hai mép; hai cạnh đầu (dài b) không phải nét dầm.
                    if (pl != null && pl.Closed && doan.Count == 4) doan = doan.OrderByDescending(s => s[0].GetDistanceTo(s[1])).Take(2).ToList();
                    ds.Add(new KeyValuePair<List<Point2d[]>, double[]>(doan, new[] { b, h }));
                }
                tr.Commit();
            }
            List<KeyValuePair<Point2d, Vector2d>> nhan = lech != null && ds.Count > 0 ? NhanDam(khac.ToArray()) : new List<KeyValuePair<Point2d, Vector2d>>();
            foreach (KeyValuePair<List<Point2d[]>, double[]> kv in ds)
            {
                dem[0]++;
                double d = kv.Value[1] / 1000;
                if (d - daySan > 0.3 + 1e-9) { dem[1]++; kq.AddRange(MepDam(kv.Key, kv.Value[0] / 1000 / m, tim)); }
                else dem[2]++;
                foreach (KeyValuePair<Point2d, Vector2d> nh in nhan)
                {
                    bool gan = kv.Key.Any(s =>
                    {
                        Vector2d u = s[1] - s[0];
                        return u.Length > 1e-9 && CachDoan(s, nh.Key) <= 1.5 / m && Math.Abs(u.GetNormal().X * nh.Value.GetNormal().Y - u.GetNormal().Y * nh.Value.GetNormal().X) <= 0.26;
                    });
                    if (gan && Math.Abs(nh.Value.Length - Math.Max(kv.Value[0], kv.Value[1]) / 1000) > 0.001)
                        lech.Add("dầm " + ChuBxh(kv.Value[0], kv.Value[1]) + " tại " + Math.Round(kv.Key[0][0].X) + "," + Math.Round(kv.Key[0][0].Y)
                            + ": nhãn bản vẽ ghi cao " + Math.Round(nh.Value.Length * 1000) + " mm");
                }
            }
            return kq;
        }

        /// Thao tác spec "dam": p [[x,y],...] = tim dầm (mỗi cặp điểm liền nhau một dầm) và / hoặc h [handle] = gán cỡ cho line / polyline có sẵn; bxh "400x600".
        static void OpDam(GoiSpec g, Dictionary<string, object> o)
        {
            double b, h;
            string s = LayChu(o, "bxh", "");
            if (!DocBxhTay(s, out b, out h)) throw new System.Exception("dam: \"bxh\" phải là rộng x cao mm, vd \"400x600\" (b 50–2000, h 100–3000)");
            Action<Entity> them = e => Them(g, e);
            if (Lay(o, "p") != null)
            {
                List<Point3d> ps = DsP(g, Lay(o, "p"), "p");
                if (ps.Count < 2) throw new System.Exception("dam: \"p\" cần ít nhất 2 điểm tim dầm");
                for (int i = 0; i + 1 < ps.Count; i++) TaoDam(g.Db, g.Tr, g.Ms, new Point2d(ps[i].X, ps[i].Y), new Point2d(ps[i + 1].X, ps[i + 1].Y), b, h, them);
            }
            HashSet<string> hs = HandleOp(o);
            if (hs != null)
                foreach (string hd in hs)
                {
                    ObjectId id;
                    try { id = g.Db.GetObjectId(false, new Handle(Convert.ToInt64(hd, 16)), 0); }
                    catch (System.Exception) { throw new System.Exception("dam: không có đối tượng handle " + hd); }
                    Entity e = g.Tr.GetObject(id, OpenMode.ForWrite) as Entity;
                    if (!(e is Line) && !(e is Polyline)) throw new System.Exception("dam: handle " + hd + " không phải line / polyline");
                    GhiDam(g.Db, g.Tr, g.Ms, e, b, h, them);
                }
            if (Lay(o, "p") == null && hs == null) throw new System.Exception("dam: cần \"p\" (vẽ dầm theo tim) hoặc \"h\" (gán cỡ cho nét có sẵn)");
        }
    }
}
