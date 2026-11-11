// PCCC_SPEC: chạy một gói thao tác (spec JSON) trong MỘT lệnh = một bước Undo, trả kết quả gọn (đếm + lỗi).
// Từ vựng (nguồn duy nhất): cad/skills/TU_VUNG.md. Thêm thao tác: viết hàm Op..., thêm 1 dòng vào MotOp, ghi vào TU_VUNG.md.
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
        const int DOC_TOI_DA = 40;        // trần số dòng một thao tác "doc" trả về: kết quả phải gọn để đỡ token
        const string KIEU_KT = "PCCC_KT"; // kiểu kích thước riêng: số nguyên, chữ Arial, cỡ theo "cao" của từng kích thước

        class GoiSpec
        {
            public Document Doc;
            public Database Db;
            public Transaction Tr;
            public BlockTableRecord Ms;
            public FileDanhMuc Dm;
            public double Gx, Gy;
            public int Ve, Xoa;
            public Extents3d? Hop;
            public List<object> Loi = new List<object>();
            public List<object> KqDoc = new List<object>();
            public Dictionary<string, object> Dem;
        }

        // ---------- Đọc JSON ----------

        static double D(object o) { return Convert.ToDouble(o, CultureInfo.InvariantCulture); }
        static double Lt(double x) { return Math.Round(x, 1); }
        static object Lay(Dictionary<string, object> d, string k) { object v; return d.TryGetValue(k, out v) ? v : null; }
        static double LaySo(Dictionary<string, object> d, string k, double macDinh) { object v = Lay(d, k); return v == null ? macDinh : D(v); }
        static string LayChu(Dictionary<string, object> d, string k, string macDinh) { object v = Lay(d, k); return v == null ? macDinh : Convert.ToString(v, CultureInfo.InvariantCulture); }
        static bool LayCo(Dictionary<string, object> d, string k, bool macDinh) { object v = Lay(d, k); return v == null ? macDinh : (v is bool ? (bool)v : D(v) != 0); }

        static IList DS(object o, string ten)
        {
            IList l = o as IList;
            if (l == null) throw new System.Exception("thiếu \"" + ten + "\" (phải là mảng)");
            return l;
        }

        static Point3d P(GoiSpec g, object o, string ten)
        {
            IList l = DS(o, ten);
            if (l.Count < 2) throw new System.Exception("\"" + ten + "\" phải là [x,y]");
            return new Point3d(D(l[0]) + g.Gx, D(l[1]) + g.Gy, 0);
        }

        /// Nhận [[x,y],...] hoặc một điểm [x,y].
        static List<Point3d> DsP(GoiSpec g, object o, string ten)
        {
            IList l = DS(o, ten);
            List<Point3d> kq = new List<Point3d>();
            if (l.Count > 0 && !(l[0] is IList)) { kq.Add(P(g, l, ten)); return kq; }
            foreach (object x in l) kq.Add(P(g, x, ten));
            return kq;
        }

        // ---------- Mặc định ký hiệu (trục, kích thước, chữ) ----------
        // Đọc ky_hieu.json cạnh DLL (nguồn: cad/data/ky_hieu/ky_hieu.json, build.bat chép sang). Thiếu file hoặc thiếu khoá thì dùng số trong mã.

        static Dictionary<string, object> kyHieu;

        static Dictionary<string, object> NhomKyHieu(string nhom)
        {
            if (kyHieu == null)
            {
                kyHieu = new Dictionary<string, object>();
                string f = Path.Combine(ThuMuc, "ky_hieu.json");
                if (File.Exists(f))
                {
                    Dictionary<string, object> d;
                    try { d = new JavaScriptSerializer().DeserializeObject(File.ReadAllText(f, Encoding.UTF8)) as Dictionary<string, object>; }
                    catch (System.Exception ex) { kyHieu = null; throw new System.Exception("ky_hieu.json sai cú pháp: " + ex.Message); }
                    if (d != null) kyHieu = d;
                }
            }
            return Lay(kyHieu, nhom) as Dictionary<string, object> ?? new Dictionary<string, object>();
        }

        // ---------- Lệnh ----------

        static void ChaySpec()
        {
            Document doc = AcApp.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;
            PromptStringOptions po = new PromptStringOptions("\nFile spec JSON: ");
            po.AllowSpaces = true;
            PromptResult pr = ed.GetString(po);
            if (pr.Status != PromptStatus.OK) return;
            string f = Path.GetFullPath(pr.StringResult.Trim().Trim('"'));
            // Việc từ hàng đợi (máy chủ MCP) trả kết quả vào thư mục "ra"; file thường thì ghi <tên>.kq.json cạnh nó.
            bool tuHangDoi = string.Equals(Path.GetDirectoryName(f), HangDoi.Dang, StringComparison.OrdinalIgnoreCase);
            string fKq = tuHangDoi ? Path.Combine(HangDoi.Ra, Path.GetFileName(f)) : Path.ChangeExtension(f, ".kq.json");
            Dictionary<string, object> kq;
            bool zoom = false;
            try { kq = ThucHienSpec(doc, File.ReadAllText(f, Encoding.UTF8), out zoom); }
            catch (System.Exception ex)
            {
                kq = new Dictionary<string, object> { { "ok", false }, { "ve", 0 }, { "xoa", 0 },
                    { "loi", new object[] { new Dictionary<string, object> { { "i", -1 }, { "op", "spec" }, { "ly_do", ex.Message } } } } };
            }
            JavaScriptSerializer js = new JavaScriptSerializer();
            js.MaxJsonLength = int.MaxValue;
            HangDoi.GhiAnToan(fKq, js.Serialize(kq));
            if (tuHangDoi) { try { File.Delete(f); } catch (System.Exception) { } }
            IList loi = Lay(kq, "loi") as IList;
            ed.WriteMessage("\n[TKPCCC] SPEC ve=" + kq["ve"] + " xoa=" + kq["xoa"] + " loi=" + (loi == null ? 0 : loi.Count) + " -> " + fKq);
            // Chỉ khi spec xin "zoom": thêm một bước Undo cho lệnh ZOOM, nên mặc định không làm.
            if (zoom && tuHangDoi) doc.SendStringToExecute("_.ZOOM _E ", true, false, false);
        }

        static Dictionary<string, object> ThucHienSpec(Document doc, string json, out bool zoom)
        {
            JavaScriptSerializer js = new JavaScriptSerializer();
            js.MaxJsonLength = int.MaxValue;
            Dictionary<string, object> goi = js.DeserializeObject(json) as Dictionary<string, object>;
            if (goi == null || !(Lay(goi, "ops") is IList)) throw new System.Exception("Spec phải có dạng {\"ops\":[...]}");
            IList ops = (IList)goi["ops"];
            zoom = LayCo(goi, "zoom", false);
            kyHieu = null; // đọc lại ky_hieu.json mỗi lần chạy: sửa file xong không cần mở lại AutoCAD

            GoiSpec g = new GoiSpec();
            g.Doc = doc;
            g.Db = doc.Database;
            g.Dm = DocDanhMuc();
            IList goc = Lay(goi, "goc") as IList;
            if (goc != null && goc.Count >= 2) { g.Gx = D(goc[0]); g.Gy = D(goc[1]); }

            // Block thiết bị phải có trong bản vẽ trước khi mở transaction vẽ (NapVaoBanVe tự mở transaction riêng).
            List<string> maDat = new List<string>();
            foreach (object x in ops)
            {
                Dictionary<string, object> d = x as Dictionary<string, object>;
                if (d != null && LayChu(d, "op", "") == "dat") maDat.Add(LayChu(d, "ma", ""));
            }
            if (maDat.Count > 0)
            {
                bool thieu;
                using (Transaction tr = g.Db.TransactionManager.StartTransaction())
                {
                    BlockTable bt = (BlockTable)tr.GetObject(g.Db.BlockTableId, OpenMode.ForRead);
                    thieu = maDat.Any(m => TimMa(g.Dm, m) != null && !bt.Has(m));
                    tr.Commit();
                }
                if (thieu) NapVaoBanVe(doc, g.Dm);
            }

            using (Transaction tr = g.Db.TransactionManager.StartTransaction())
            {
                g.Tr = tr;
                g.Ms = (BlockTableRecord)tr.GetObject(g.Db.CurrentSpaceId, OpenMode.ForWrite);
                for (int i = 0; i < ops.Count; i++)
                {
                    Dictionary<string, object> o = ops[i] as Dictionary<string, object>;
                    string ten = o == null ? "?" : LayChu(o, "op", "?");
                    try
                    {
                        if (o == null) throw new System.Exception("thao tác phải là object {\"op\":...}");
                        MotOp(g, ten, o);
                    }
                    catch (System.Exception ex)
                    {
                        g.Loi.Add(new Dictionary<string, object> { { "i", i }, { "op", ten }, { "ly_do", ex.Message } });
                    }
                }
                tr.Commit();
            }

            Dictionary<string, object> kq = new Dictionary<string, object>();
            kq["ok"] = g.Loi.Count == 0;
            kq["ve"] = g.Ve;
            kq["xoa"] = g.Xoa;
            if (g.Hop.HasValue)
            {
                Extents3d h = g.Hop.Value;
                kq["hop"] = new double[] { Lt(h.MinPoint.X), Lt(h.MinPoint.Y), Lt(h.MaxPoint.X), Lt(h.MaxPoint.Y) };
            }
            if (g.Loi.Count > 0) kq["loi"] = g.Loi;
            if (g.Dem != null) kq["dem"] = g.Dem;
            if (g.KqDoc.Count > 0) kq["doc"] = g.KqDoc;
            kq["ban_ve"] = Path.GetFileName(doc.Name);
            return kq;
        }

        static void MotOp(GoiSpec g, string ten, Dictionary<string, object> o)
        {
            switch (ten)
            {
                case "dat": OpDat(g, o); break;
                case "ong": OpOng(g, o); break;
                case "net": OpNet(g, o); break;
                case "tron": OpTron(g, o); break;
                case "chu": OpChu(g, o); break;
                case "kt": OpKt(g, o); break;
                case "luoi_truc": OpLuoiTruc(g, o); break;
                case "xoa": OpXoa(g, o); break;
                case "doc": OpDoc(g, o); break;
                case "dem": OpDem(g); break;
                default: throw new System.Exception("không có thao tác \"" + ten + "\" (xem cad/skills/TU_VUNG.md)");
            }
        }

        // ---------- Tiện ích vẽ ----------

        static void MoHop(GoiSpec g, Extents3d x)
        {
            if (!g.Hop.HasValue) { g.Hop = x; return; }
            Extents3d h = g.Hop.Value;
            h.AddExtents(x);
            g.Hop = h;
        }

        static void Them(GoiSpec g, Entity e)
        {
            g.Ms.AppendEntity(e);
            g.Tr.AddNewlyCreatedDBObject(e, true);
            g.Ve++;
            try { MoHop(g, e.GeometricExtents); } catch (System.Exception) { }
        }

        /// Layer theo trường "l" của thao tác (mặc định layer 0); layer chưa có thì tạo với màu "mau" (mặc định 7).
        static ObjectId LayerOp(GoiSpec g, Dictionary<string, object> o)
        {
            string l = LayChu(o, "l", "0");
            return l == "0" ? g.Db.LayerZero : DamBaoLayer(g.Db, g.Tr, l, (short)LaySo(o, "mau", 7));
        }

        static Polyline TaoPline(List<Point3d> ps, bool kin, ObjectId layer)
        {
            if (ps.Count < 2) throw new System.Exception("\"p\" cần ít nhất 2 điểm");
            Polyline pl = new Polyline();
            for (int i = 0; i < ps.Count; i++) pl.AddVertexAt(i, new Point2d(ps[i].X, ps[i].Y), 0, 0, 0);
            pl.Closed = kin;
            pl.LayerId = layer;
            return pl;
        }

        static MText TaoChu(GoiSpec g, Point3d p, string t, double cao, double gocDo, bool giua, ObjectId layer)
        {
            MText m = new MText();
            m.Contents = t.Replace("\\", "\\\\").Replace("{", "\\{").Replace("}", "\\}").Replace("\n", "\\P");
            m.Location = p;
            m.TextHeight = cao;
            m.Rotation = gocDo * Math.PI / 180;
            m.Attachment = giua ? AttachmentPoint.MiddleCenter : AttachmentPoint.BottomLeft;
            m.TextStyleId = DamBaoKieuChu(g.Db, g.Tr);
            m.LayerId = layer;
            return m;
        }

        static ObjectId DamBaoKieuKt(GoiSpec g)
        {
            DimStyleTable t = (DimStyleTable)g.Tr.GetObject(g.Db.DimStyleTableId, OpenMode.ForRead);
            if (t.Has(KIEU_KT)) return t[KIEU_KT];
            t.UpgradeOpen();
            DimStyleTableRecord s = new DimStyleTableRecord();
            s.Name = KIEU_KT;
            s.Dimtxt = 2.5; s.Dimasz = 1.5; s.Dimexe = 1.25; s.Dimexo = 1.0; s.Dimgap = 0.8;
            s.Dimdec = 0; s.Dimtad = 1; s.Dimtih = false; s.Dimtoh = false;
            s.Dimtxsty = DamBaoKieuChu(g.Db, g.Tr);
            ObjectId id = t.Add(s);
            g.Tr.AddNewlyCreatedDBObject(s, true);
            return id;
        }

        /// Kích thước song song p1-p2; cach > 0 đặt bên trái hướng p1 -> p2, cach < 0 bên phải. cao = cỡ chữ theo đơn vị bản vẽ.
        static void VeKt(GoiSpec g, Point3d a, Point3d b, double cach, double cao, ObjectId layer)
        {
            Vector3d v = b - a;
            if (v.Length < 1e-9) throw new System.Exception("p1 trùng p2");
            Vector3d n = new Vector3d(-v.Y, v.X, 0).GetNormal();
            Point3d giua = new Point3d((a.X + b.X) / 2, (a.Y + b.Y) / 2, 0) + n * cach;
            AlignedDimension d = new AlignedDimension(a, b, giua, "", DamBaoKieuKt(g));
            d.Dimscale = cao / 2.5;
            d.LayerId = layer;
            Them(g, d);
        }

        static ObjectId DamBaoNet(GoiSpec g, string ten)
        {
            LinetypeTable t = (LinetypeTable)g.Tr.GetObject(g.Db.LinetypeTableId, OpenMode.ForRead);
            if (!t.Has(ten))
                foreach (string f in new string[] { "acadiso.lin", "acad.lin" })
                {
                    try { g.Db.LoadLineTypeFile(ten, f); break; }
                    catch (System.Exception) { }
                }
            return t.Has(ten) ? t[ten] : g.Db.ContinuousLinetype;
        }

        /// Chèn 1 thiết bị (block thư viện + thuộc tính MA_VT) tại điểm WCS. Dùng chung cho PCCC_DAT và thao tác "dat".
        static ObjectId ChenThietBi(Database db, Transaction tr, VatTu v, Point3d p, double k, double goc)
        {
            BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
            if (!bt.Has(v.ma)) throw new System.Exception("Thư viện không có block " + v.ma);
            ObjectId bid = bt[v.ma];
            BlockTableRecord ms = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);
            BlockReference br = new BlockReference(p, bid);
            br.ScaleFactors = new Scale3d(k);
            br.Rotation = goc;
            br.LayerId = DamBaoLayer(db, tr, TIEN_TO_LAYER + v.he, MauHe(v.he));
            ObjectId rid = ms.AppendEntity(br);
            tr.AddNewlyCreatedDBObject(br, true);
            BlockTableRecord def = (BlockTableRecord)tr.GetObject(bid, OpenMode.ForRead);
            foreach (ObjectId id in def)
            {
                AttributeDefinition ad = tr.GetObject(id, OpenMode.ForRead) as AttributeDefinition;
                if (ad == null || ad.Constant) continue;
                AttributeReference ar = new AttributeReference();
                ar.SetAttributeFromBlock(ad, br.BlockTransform);
                if (ad.Tag.ToUpperInvariant() == TAG_MA) ar.TextString = v.ma;
                br.AttributeCollection.AppendAttribute(ar);
                tr.AddNewlyCreatedDBObject(ar, true);
            }
            return rid;
        }

        // ---------- Thao tác ----------

        static void OpDat(GoiSpec g, Dictionary<string, object> o)
        {
            string ma = LayChu(o, "ma", "");
            VatTu v = TimMa(g.Dm, ma);
            if (v == null || v.loai != "block") throw new System.Exception("mã thiết bị không có trong danh mục: " + ma);
            double k = 0.001 / HeSoMet(g.Db), goc = LaySo(o, "goc", 0) * Math.PI / 180;
            foreach (Point3d p in DsP(g, Lay(o, "p"), "p"))
            {
                ChenThietBi(g.Db, g.Tr, v, p, k, goc);
                g.Ve++;
                MoHop(g, new Extents3d(p, p));
            }
        }

        static void OpOng(GoiSpec g, Dictionary<string, object> o)
        {
            string ma = LayChu(o, "ma", "");
            VatTu v = TimMa(g.Dm, ma);
            if (v == null || v.loai != "tuyen") throw new System.Exception("mã ống/dây không có trong danh mục: " + ma);
            Them(g, TaoPline(DsP(g, Lay(o, "p"), "p"), false, DamBaoLayer(g.Db, g.Tr, TIEN_TO_LAYER + v.ma, MauHe(v.he))));
        }

        static void OpNet(GoiSpec g, Dictionary<string, object> o)
        {
            Them(g, TaoPline(DsP(g, Lay(o, "p"), "p"), LayCo(o, "kin", false), LayerOp(g, o)));
        }

        static void OpTron(GoiSpec g, Dictionary<string, object> o)
        {
            double r = LaySo(o, "r", 0);
            if (r <= 0) throw new System.Exception("\"r\" phải > 0");
            Circle c = new Circle(P(g, Lay(o, "c"), "c"), Vector3d.ZAxis, r);
            c.LayerId = LayerOp(g, o);
            Them(g, c);
        }

        static void OpChu(GoiSpec g, Dictionary<string, object> o)
        {
            Them(g, TaoChu(g, P(g, Lay(o, "p"), "p"), LayChu(o, "t", ""), LaySo(o, "cao", LaySo(NhomKyHieu("chu"), "cao", 250)), LaySo(o, "goc", 0),
                LayChu(o, "can", "trai") == "giua", LayerOp(g, o)));
        }

        static void OpKt(GoiSpec g, Dictionary<string, object> o)
        {
            Dictionary<string, object> k = NhomKyHieu("kt");
            VeKt(g, P(g, Lay(o, "p1"), "p1"), P(g, Lay(o, "p2"), "p2"), LaySo(o, "cach", LaySo(k, "cach", 500)), LaySo(o, "cao", LaySo(k, "cao", 250)), LayerOp(g, o));
        }

        static List<double> CongDon(object o, string ten)
        {
            List<double> kq = new List<double> { 0 };
            foreach (object x in DS(o, ten)) kq.Add(kq[kq.Count - 1] + D(x));
            if (kq.Count < 2) throw new System.Exception("\"" + ten + "\" cần ít nhất 1 nhịp");
            return kq;
        }

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

        static bool KhopTen(string ten, string mau)
        {
            if (mau.EndsWith("*")) return ten.StartsWith(mau.Substring(0, mau.Length - 1), StringComparison.OrdinalIgnoreCase);
            return string.Equals(ten, mau, StringComparison.OrdinalIgnoreCase);
        }

        /// Hộp [x1,y1,x2,y2] của thao tác (đã cộng gốc), null nếu không có.
        static double[] HopOp(GoiSpec g, Dictionary<string, object> o)
        {
            IList h = Lay(o, "hop") as IList;
            if (h == null) return null;
            if (h.Count < 4) throw new System.Exception("\"hop\" phải là [x1,y1,x2,y2]");
            double x1 = D(h[0]) + g.Gx, y1 = D(h[1]) + g.Gy, x2 = D(h[2]) + g.Gx, y2 = D(h[3]) + g.Gy;
            return new double[] { Math.Min(x1, x2), Math.Min(y1, y2), Math.Max(x1, x2), Math.Max(y1, y2) };
        }

        static bool TrongHop(Entity e, double[] h)
        {
            if (h == null) return true;
            Extents3d x;
            try { x = e.GeometricExtents; } catch (System.Exception) { return false; }
            return x.MinPoint.X >= h[0] && x.MinPoint.Y >= h[1] && x.MaxPoint.X <= h[2] && x.MaxPoint.Y <= h[3];
        }

        static List<Entity> Loc(GoiSpec g, string l, double[] hop)
        {
            List<Entity> kq = new List<Entity>();
            foreach (ObjectId id in g.Ms)
            {
                Entity e = g.Tr.GetObject(id, OpenMode.ForRead) as Entity;
                if (e == null || (l != null && !KhopTen(e.Layer, l)) || !TrongHop(e, hop)) continue;
                kq.Add(e);
            }
            return kq;
        }

        static void OpXoa(GoiSpec g, Dictionary<string, object> o)
        {
            string l = LayChu(o, "l", null);
            double[] hop = HopOp(g, o);
            if (l == null && hop == null && !LayCo(o, "tat_ca", false))
                throw new System.Exception("xoa cần \"l\" (layer, cho phép đuôi *) hoặc \"hop\" [x1,y1,x2,y2]; xoá hết thì thêm \"tat_ca\":true");
            foreach (Entity e in Loc(g, l, hop))
            {
                e.UpgradeOpen();
                e.Erase();
                g.Xoa++;
            }
        }

        static void OpDoc(GoiSpec g, Dictionary<string, object> o)
        {
            string gi = LayChu(o, "gi", "layer");
            string l = LayChu(o, "l", null);
            double[] hop = HopOp(g, o);
            List<object> ds = new List<object>();
            int n = 0;
            if (gi == "layer")
            {
                Dictionary<string, int> dem = new Dictionary<string, int>();
                foreach (Entity e in Loc(g, l, hop)) { int c; dem.TryGetValue(e.Layer, out c); dem[e.Layer] = c + 1; }
                n = dem.Count;
                foreach (KeyValuePair<string, int> p in dem.OrderByDescending(x => x.Value).Take(DOC_TOI_DA))
                    ds.Add(new Dictionary<string, object> { { "l", p.Key }, { "n", p.Value } });
            }
            else if (gi == "vung")
            {
                // Vùng = polyline kín, mặc định trên layer PCCC_VUNG: diện tích m2, hộp bao, đỉnh (tối đa 12).
                double m = HeSoMet(g.Db);
                foreach (Entity e in Loc(g, l ?? "PCCC_VUNG", hop))
                {
                    Polyline pl = e as Polyline;
                    if (pl == null || !pl.Closed) continue;
                    if (++n > DOC_TOI_DA) continue;
                    Extents3d x = pl.GeometricExtents;
                    List<object> dinh = new List<object>();
                    for (int i = 0; i < pl.NumberOfVertices && i < 12; i++) { Point2d q = pl.GetPoint2dAt(i); dinh.Add(new double[] { Lt(q.X), Lt(q.Y) }); }
                    ds.Add(new Dictionary<string, object> { { "h", pl.Handle.ToString() }, { "m2", Math.Round(pl.Area * m * m, 2) },
                        { "hop", new double[] { Lt(x.MinPoint.X), Lt(x.MinPoint.Y), Lt(x.MaxPoint.X), Lt(x.MaxPoint.Y) } }, { "p", dinh } });
                }
            }
            else if (gi == "chu")
            {
                foreach (Entity e in Loc(g, l, hop))
                {
                    DBText t = e as DBText;
                    MText mt = e as MText;
                    if (t == null && mt == null) continue;
                    if (++n > DOC_TOI_DA) continue;
                    Point3d q = t != null ? t.Position : mt.Location;
                    ds.Add(new Dictionary<string, object> { { "t", t != null ? t.TextString : mt.Text }, { "p", new double[] { Lt(q.X), Lt(q.Y) } }, { "l", e.Layer } });
                }
            }
            else throw new System.Exception("doc: \"gi\" phải là layer, vung hoặc chu");
            Dictionary<string, object> kq = new Dictionary<string, object> { { "gi", gi }, { "n", n }, { "ds", ds } };
            if (n > ds.Count) kq["con"] = n - ds.Count;
            g.KqDoc.Add(kq);
        }

        /// Đếm thiết bị (cái) và ống/dây (m) theo mã vật tư, cùng cách đếm với PCCC_BOC.
        static void OpDem(GoiSpec g)
        {
            BoiCanh bc = new BoiCanh();
            bc.Tr = g.Tr;
            bc.Dm = g.Dm;
            bc.Alias = DocAlias();
            bc.Khu = new List<Khu>();
            bc.HeSo = HeSoMet(g.Db);
            bc.Kq = new KetQua();
            foreach (ObjectId id in g.Ms)
            {
                Entity e = g.Tr.GetObject(id, OpenMode.ForRead) as Entity;
                if (e != null) Duyet(e, Matrix3d.Identity, 0, 1, bc);
            }
            g.Dem = new Dictionary<string, object>();
            foreach (KeyValuePair<string, double> p in bc.Kq.Tong.OrderBy(x => x.Key)) g.Dem[p.Key] = Math.Round(p.Value, 2);
        }
    }
}
