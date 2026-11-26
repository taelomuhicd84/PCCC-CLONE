// Thư viện block: tạo / nạp PCCC_Blocks.dwg, hình mặc định (PCCC_LIB). Tách từ Lenh.cs (26/11/2026: mỗi thư mục số của 07_CAD là một hạng mục tính năng, xem 07_CAD/README.md).
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
        /// Block mặc định: vòng tròn R200 mm + ký hiệu + attribute MA_VT, TEN (ẩn). Người dùng sửa hình trong PCCC_Blocks.dwg.
        static ObjectId TaoBlockMacDinh(Transaction tr, BlockTable bt, ObjectId kieu, VatTu v)
        {
            BlockTableRecord b = new BlockTableRecord();
            b.Name = v.ma;
            b.Origin = Point3d.Origin;
            b.Units = UnitsValue.Millimeters; // INSERT tay vào bản vẽ m/cm thì AutoCAD tự đổi tỉ lệ
            if (!bt.IsWriteEnabled) bt.UpgradeOpen();
            ObjectId bid = bt.Add(b);
            tr.AddNewlyCreatedDBObject(b, true);

            IList hinh = HinhMacDinh(v.ma);
            if (hinh != null) VeHinhMacDinh(tr, b, kieu, hinh);
            else
            {
                Circle c = new Circle(Point3d.Origin, Vector3d.ZAxis, 200);
                b.AppendEntity(c); tr.AddNewlyCreatedDBObject(c, true);

                // MText căn giữa theo Location: không cần AdjustAlignment như DBText trong database phụ.
                string kh = v.ky_hieu ?? v.ma;
                MText t = new MText();
                t.Contents = kh;
                t.TextStyleId = kieu;
                t.TextHeight = kh.Length > 2 ? 90 : 140;
                t.Attachment = AttachmentPoint.MiddleCenter;
                t.Location = Point3d.Origin;
                b.AppendEntity(t); tr.AddNewlyCreatedDBObject(t, true);
            }

            ThemAttDef(b, tr, kieu, TAG_MA, v.ma, new Point3d(0, -300, 0));
            ThemAttDef(b, tr, kieu, "TEN", v.ten, new Point3d(0, -450, 0));
            return bid;
        }

        static Dictionary<string, object> hinhBc;
        /// Hình block mặc định lấy từ nét vector bảng ký hiệu hồ sơ thật (hinh_bc.json cạnh DLL, sinh bằng scripts/hinh_tu_pdf.py của skill
        /// tkpccc-cad-console); mã không có hình thì null = vòng tròn + chữ.
        static IList HinhMacDinh(string ma)
        {
            if (hinhBc == null)
            {
                string f = Path.Combine(ThuMuc, "hinh_bc.json");
                Dictionary<string, object> d = File.Exists(f) ? new JavaScriptSerializer().DeserializeObject(File.ReadAllText(f, Encoding.UTF8)) as Dictionary<string, object> : null;
                hinhBc = d != null && d.ContainsKey("hinh") ? (Dictionary<string, object>)d["hinh"] : new Dictionary<string, object>();
            }
            string k = ma.ToUpperInvariant();
            if (k.StartsWith("SPR_VAN_OSY_")) k = "SPR_VAN_OSY"; // một hình cho mọi cỡ van OS&Y
            return hinhBc.ContainsKey(k) ? hinhBc[k] as IList : null;
        }

        static void VeHinhMacDinh(Transaction tr, BlockTableRecord b, ObjectId kieu, IList hinh)
        {
            Func<object, Point3d> P3 = q => { IList a = (IList)q; return new Point3d(Convert.ToDouble(a[0], CultureInfo.InvariantCulture), Convert.ToDouble(a[1], CultureInfo.InvariantCulture), 0); };
            foreach (Dictionary<string, object> d in hinh.OfType<Dictionary<string, object>>())
            {
                List<Entity> moi = new List<Entity>();
                if (d.ContainsKey("pl"))
                {
                    IList ps = (IList)d["pl"];
                    Polyline pl = new Polyline();
                    for (int i = 0; i < ps.Count; i++) { Point3d q = P3(ps[i]); pl.AddVertexAt(i, new Point2d(q.X, q.Y), 0, 0, 0); }
                    pl.Closed = d.ContainsKey("kin") && (bool)d["kin"];
                    if (ps.Count > 1) moi.Add(pl);
                }
                else if (d.ContainsKey("c"))
                {
                    IList c = (IList)d["c"];
                    moi.Add(new Circle(P3(c), Vector3d.ZAxis, Convert.ToDouble(c[2], CultureInfo.InvariantCulture)));
                }
                else if (d.ContainsKey("to"))
                {
                    // Miền tô của PDF (các dải lồi nhỏ): quạt tam giác thành Solid.
                    List<Point3d> ps = ((IList)d["to"]).Cast<object>().Select(P3).ToList();
                    for (int i = 1; i + 1 < ps.Count; i++) moi.Add(new Solid(ps[0], ps[i], ps[i + 1]));
                }
                else if (d.ContainsKey("t"))
                {
                    MText t = new MText();
                    t.Contents = Convert.ToString(d["t"], CultureInfo.InvariantCulture);
                    t.TextStyleId = kieu;
                    t.TextHeight = Convert.ToDouble(d["h"], CultureInfo.InvariantCulture);
                    t.Attachment = AttachmentPoint.MiddleCenter;
                    t.Location = P3(d["p"]);
                    moi.Add(t);
                }
                foreach (Entity e in moi) { b.AppendEntity(e); tr.AddNewlyCreatedDBObject(e, true); }
            }
        }

        static void ThemAttDef(BlockTableRecord b, Transaction tr, ObjectId kieu, string tag, string giaTri, Point3d p)
        {
            AttributeDefinition a = new AttributeDefinition(p, giaTri, tag, tag, kieu);
            a.Height = 60;
            a.Invisible = true;
            a.Preset = true;
            b.AppendEntity(a); tr.AddNewlyCreatedDBObject(a, true);
        }

        /// Tạo PCCC_Blocks.dwg nếu chưa có; có rồi thì chỉ thêm block mặc định cho mã mới trong vat_tu.json
        /// (block người dùng đã sửa giữ nguyên). Trả về số block thêm.
        static int DamBaoThuVien(FileDanhMuc dm)
        {
            bool moi = !File.Exists(FileThuVien);
            int them = 0;
            using (Database db = new Database(moi, true))
            {
                if (moi) db.Insunits = UnitsValue.Millimeters;
                else
                {
                    db.ReadDwgFile(FileThuVien, FileOpenMode.OpenForReadAndAllShare, true, "");
                    db.CloseInput(true); // đọc hết vào bộ nhớ, nhả file để ghi đè được
                }
                // Đối tượng mới lấy mặc định (kiểu chữ, layer) từ WorkingDatabase: phải trỏ sang database thư viện, không thì eWrongDatabase.
                Database dangLam = HostApplicationServices.WorkingDatabase;
                HostApplicationServices.WorkingDatabase = db;
                try
                {
                    using (Transaction tr = db.TransactionManager.StartTransaction())
                    {
                        BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                        List<VatTu> thieu = dm.vat_tu.Where(x => x.loai == "block" && !bt.Has(x.ma)).ToList();
                        // Block còn đúng hình mặc định cũ (vòng tròn R200 + chữ ký hiệu) mà nay có hình mới (hinh_bc.json): vẽ lại; block người dùng đã sửa giữ nguyên.
                        foreach (VatTu v in dm.vat_tu.Where(x => x.loai == "block" && bt.Has(x.ma) && HinhMacDinh(x.ma) != null))
                        {
                            BlockTableRecord b = (BlockTableRecord)tr.GetObject(bt[v.ma], OpenMode.ForRead);
                            List<Entity> ve = b.Cast<ObjectId>().Select(id => (Entity)tr.GetObject(id, OpenMode.ForRead)).Where(e => !(e is AttributeDefinition)).ToList();
                            Circle c0 = ve.Count == 2 ? ve.OfType<Circle>().FirstOrDefault() : null;
                            MText t0 = ve.OfType<MText>().FirstOrDefault();
                            if (c0 == null || t0 == null || Math.Abs(c0.Radius - 200) > 1e-6 || c0.Center.DistanceTo(Point3d.Origin) > 1e-6 || t0.Text != (v.ky_hieu ?? v.ma)
                                || Math.Abs(t0.TextHeight - ((v.ky_hieu ?? v.ma).Length > 2 ? 90 : 140)) > 1e-6) continue; // cỡ chữ của block mặc định cũ
                            b.UpgradeOpen();
                            foreach (Entity e in ve) { e.UpgradeOpen(); e.Erase(); }
                            VeHinhMacDinh(tr, b, DamBaoKieuChu(db, tr), HinhMacDinh(v.ma));
                            them++;
                        }
                        if (thieu.Count > 0)
                        {
                            ObjectId kieu = DamBaoKieuChu(db, tr);
                            BlockTableRecord ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);
                            int i = ms.Cast<ObjectId>().Count(); // bày mẫu nối tiếp các mẫu đã có
                            foreach (VatTu v in thieu)
                            {
                                ObjectId bid = TaoBlockMacDinh(tr, bt, kieu, v);
                                // Bày mẫu trong model space để người dùng thấy và sửa hình.
                                BlockReference br = new BlockReference(new Point3d((i % 8) * 1000, -(i / 8) * 1000, 0), bid);
                                ms.AppendEntity(br); tr.AddNewlyCreatedDBObject(br, true);
                                i++; them++;
                            }
                        }
                        tr.Commit();
                    }
                }
                finally { HostApplicationServices.WorkingDatabase = dangLam; }
                if (them > 0) db.SaveAs(FileThuVien, DwgVersion.Current);
            }
            return them;
        }

        /// Chép định nghĩa block từ thư viện vào bản vẽ. capNhat (chỉ lệnh PCCC_LIB) = thay cả block đã có; còn lại chỉ chép mã bản vẽ CHƯA có:
        /// block mang mã thiết bị đã có trong bản vẽ là ký hiệu của bản vẽ đó, không ghi đè (người dùng dặn 13/11).
        static int NapBlock(Database dich, FileDanhMuc dm, bool capNhat)
        {
            HashSet<string> co = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (!capNhat)
                using (Transaction tr = dich.TransactionManager.StartTransaction())
                {
                    foreach (ObjectId id in (BlockTable)tr.GetObject(dich.BlockTableId, OpenMode.ForRead))
                        co.Add(((BlockTableRecord)tr.GetObject(id, OpenMode.ForRead)).Name);
                    tr.Commit();
                }
            using (Database src = new Database(false, true))
            {
                src.ReadDwgFile(FileThuVien, FileOpenMode.OpenForReadAndAllShare, true, "");
                ObjectIdCollection ids = new ObjectIdCollection();
                using (Transaction tr = src.TransactionManager.StartTransaction())
                {
                    BlockTable bt = (BlockTable)tr.GetObject(src.BlockTableId, OpenMode.ForRead);
                    foreach (VatTu v in dm.vat_tu.Where(x => x.loai == "block"))
                        if (bt.Has(v.ma) && !co.Contains(v.ma)) ids.Add(bt[v.ma]);
                    tr.Commit();
                }
                if (ids.Count > 0)
                    src.WblockCloneObjects(ids, dich.BlockTableId, new IdMapping(), DuplicateRecordCloning.Replace, false);
                return ids.Count;
            }
        }

        /// Đảm bảo thư viện đủ block rồi nạp vào bản vẽ hiện hành.
        static void NapVaoBanVe(Document doc, FileDanhMuc dm, bool capNhat = false)
        {
            Editor ed = doc.Editor;
            bool moi = !File.Exists(FileThuVien);
            int them = DamBaoThuVien(dm);
            if (!DangHien(doc)) { NapBlock(doc.Database, dm, capNhat); return; } // bản vẽ nền (Bot chat, người dùng đã đổi tab): không dùng Editor của nó
            if (moi) ed.WriteMessage("\nĐã tạo thư viện block mặc định: " + FileThuVien + "\nMở file này sửa hình block (BEDIT), lưu lại rồi chạy PCCC_LIB để cập nhật.");
            else if (them > 0) ed.WriteMessage("\nĐã thêm " + them + " block mặc định cho mã mới vào " + FileThuVien);
            int n = NapBlock(doc.Database, dm, capNhat);
            ed.WriteMessage("\nĐã nạp/cập nhật " + n + " block PCCC vào bản vẽ.\n");
            ed.Regen();
        }

        static void NapThuVien()
        {
            Document doc = AcApp.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;
            FileDanhMuc dm = DocDanhMuc();
            PromptKeywordOptions ko = new PromptKeywordOptions("\nThư viện block"); // AutoCAD tự nối " [Nap/Taomoi] <Nap>: "
            ko.Keywords.Add("Nap"); ko.Keywords.Add("Taomoi");
            ko.Keywords.Default = "Nap";
            ko.AllowNone = true;
            PromptResult kr = ed.GetKeywords(ko);
            if (kr.Status == PromptStatus.Cancel) return;
            if (kr.StringResult == "Taomoi" && File.Exists(FileThuVien))
            {
                // Cất bản cũ (có thể đã sửa hình) thay vì xoá.
                string cu = Path.Combine(ThuMuc, "PCCC_Blocks_cu_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".dwg");
                File.Move(FileThuVien, cu);
                ed.WriteMessage("\nĐã cất thư viện cũ: " + cu);
            }
            NapVaoBanVe(doc, dm, true);
            int anh = CapNhatAnhBlock(dm); // 07_CAD/14_THU_VIEN_BLOCK/cad/Block.cs
            if (anh > 0) ed.WriteMessage("\nĐã vẽ " + anh + " ảnh xem trước block cho bảng PCCC_BLOCK.");
        }

        // ---------- Vẽ ----------

    }
}
