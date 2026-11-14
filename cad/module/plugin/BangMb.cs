// Bảng trên mặt bằng như bộ bản vẽ mẫu của người dùng (cad/knowledge/hoc-ban-ve-mau-dwg.md mục 2, học 14/11):
// bảng KÝ HIỆU | TÊN THIẾT BỊ (chỉ thiết bị, tuyến có trong vùng chọn, đếm như PCCC_BOC) và bảng QUY ĐỊNH LẮP ĐẶT ĐẦU BÁO CHÁY
// THEO TCVN 7568-14:2025 (trị số đọc data/khoang_cach_rai.json, không chép số vào mã). Lệnh PCCC_KYHIEU, PCCC_QUYDINH; thao tác spec "bang".
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.Script.Serialization;
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
        [CommandMethod("PCCC_KYHIEU")] public void LenhKyHieu() { Chay(delegate { BangTay(true); }); }
        [CommandMethod("PCCC_QUYDINH")] public void LenhQuyDinh() { Chay(delegate { BangTay(false); }); }

        const string LAYER_BANG = "PCCC_BANG";
        const string TIEU_DE_QUY_DINH = "QUY ĐỊNH LẮP ĐẶT ĐẦU BÁO CHÁY THEO TCVN 7568-14:2025";

        /// Mã thiết bị, tuyến có trong các đối tượng (cùng cách đếm của PCCC_BOC: bỏ bảng, ký hiệu sơ đồ, bản copy đè), theo thứ tự danh mục.
        static List<string> MaTrongVung(Database db, Transaction tr, IEnumerable<ObjectId> ids, FileDanhMuc dm)
        {
            BoiCanh bc = new BoiCanh();
            bc.Tr = tr; bc.Dm = dm; bc.Alias = DocAlias(); bc.Khu = new List<Khu>(); bc.HeSo = HeSoMet(db); bc.Kq = new KetQua();
            foreach (ObjectId id in ids)
            {
                Entity e = tr.GetObject(id, OpenMode.ForRead) as Entity;
                if (e != null) Duyet(e, Matrix3d.Identity, 0, 1, bc);
            }
            List<string> ma = dm.vat_tu.Select(v => v.ma).Where(m => bc.Kq.Tong.ContainsKey(m)).ToList();
            ma.AddRange(bc.Kq.Tong.Keys.Where(m => !ma.Contains(m)).OrderBy(m => m));
            return ma;
        }

        /// Dòng bảng quy định: [tên, R, S, cách tường] (m, dấu phẩy) cho mỗi loại đầu báo có trị số trong khoang_cach_rai.json.
        static List<string[]> DongQuyDinh()
        {
            string f = Path.Combine(ThuMuc, "khoang_cach_rai.json");
            if (!File.Exists(f)) throw new System.Exception("thiếu khoang_cach_rai.json cạnh DLL (chạy cad\\build.bat)");
            Dictionary<string, object> d = new JavaScriptSerializer().DeserializeObject(File.ReadAllText(f, Encoding.UTF8)) as Dictionary<string, object>;
            IList ds = d == null ? null : Lay(d, "thiet_bi") as IList;
            CultureInfo vi = CultureInfo.GetCultureInfo("vi-VN");
            Func<Dictionary<string, object>, string, string> so = (x, k) => Lay(x, k) == null ? "–" : D(Lay(x, k)).ToString("0.0##", vi);
            List<string[]> kq = new List<string[]>();
            foreach (Dictionary<string, object> x in (ds ?? new ArrayList()).OfType<Dictionary<string, object>>())
            {
                IList ma = Lay(x, "ma") as IList;
                if (ma == null || !ma.Cast<object>().Any(m => Convert.ToString(m).StartsWith("BC_"))) continue;
                kq.Add(new[] { LayChu(x, "ten", Convert.ToString(ma[0])), so(x, "r"), so(x, "s"), so(x, "tuong") });
            }
            if (kq.Count == 0) throw new System.Exception("khoang_cach_rai.json không có dòng đầu báo (mã BC_...)");
            return kq;
        }

        /// Dựng bảng (chưa thêm vào bản vẽ). kyHieu: bảng KÝ HIỆU (ma = mã cần liệt kê; > 7 dòng thì chia 2 cặp cột như bản mẫu),
        /// không thì bảng QUY ĐỊNH. p = góc trên trái, h = cao chữ. Viền xanh lá, chữ xanh dương như bản mẫu.
        static Table DungBang(Database db, Transaction tr, bool kyHieu, List<string> ma, FileDanhMuc dm, Point3d p, double h, ObjectId lop)
        {
            BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
            ObjectId kieu = DamBaoKieuChu(db, tr);
            Table t = new Table();
            t.TableStyle = db.Tablestyle;
            t.Position = p;
            t.LayerId = lop;
            t.Color = Color.FromColorIndex(ColorMethod.ByAci, 3);
            int nCot, nDong, dau; // dau = số dòng tiêu đề + đầu cột
            double[] rong;
            if (kyHieu)
            {
                int cap = ma.Count > 7 ? 2 : 1, n = (ma.Count + cap - 1) / cap;
                nCot = 2 * cap; nDong = n + 1; dau = 1;
                t.SetSize(nDong, nCot);
                try { t.UnmergeCells(CellRange.Create(t, 0, 0, 0, nCot - 1)); } catch (Autodesk.AutoCAD.Runtime.Exception) { } // kiểu bảng gộp sẵn dòng tiêu đề
                for (int c = 0; c < cap; c++) { t.Cells[0, 2 * c].TextString = "KÝ HIỆU"; t.Cells[0, 2 * c + 1].TextString = "TÊN THIẾT BỊ"; }
                for (int i = 0; i < ma.Count; i++)
                {
                    int r = i % n + 1, c = 2 * (i / n);
                    VatTu v = TimMa(dm, ma[i]);
                    if (bt.Has(ma[i]) && (v == null || v.loai != "tuyen"))
                    {
                        t.Cells[r, c].BlockTableRecordId = bt[ma[i]];
                        t.Cells[r, c].Contents[0].IsAutoScale = true;
                    }
                    else t.Cells[r, c].TextString = "———";
                    t.Cells[r, c + 1].TextString = " " + (v != null ? v.ten : ma[i]).ToUpper(CultureInfo.GetCultureInfo("vi-VN"));
                }
                rong = Enumerable.Range(0, nCot).Select(c => c % 2 == 0 ? 6.0 : 26.0).ToArray();
            }
            else
            {
                List<string[]> ds = DongQuyDinh();
                nCot = 4; nDong = ds.Count + 2; dau = 2;
                t.SetSize(nDong, nCot);
                try { t.MergeCells(CellRange.Create(t, 0, 0, 0, nCot - 1)); } catch (Autodesk.AutoCAD.Runtime.Exception) { }
                t.Cells[0, 0].TextString = TIEU_DE_QUY_DINH;
                string[] cot = { "Loại đầu báo cháy", "Bán kính bảo vệ tối đa (m)", "Khoảng cách tối đa giữa các đầu báo (m)", "Khoảng cách tối đa từ đầu báo đến tường (m)" };
                for (int c = 0; c < nCot; c++) t.Cells[1, c].TextString = cot[c];
                for (int i = 0; i < ds.Count; i++)
                    for (int c = 0; c < nCot; c++) t.Cells[i + 2, c].TextString = (c == 0 ? " " : "") + ds[i][c];
                rong = new double[] { 27, 11, 13, 13 };
            }
            Color xanh = Color.FromColorIndex(ColorMethod.ByAci, 5);
            for (int r = 0; r < nDong; r++)
            {
                t.Rows[r].Height = h * (kyHieu && r >= dau ? 2.6 : 2.2) * (!kyHieu && r == 1 ? 2 : 1);
                for (int c = 0; c < nCot; c++)
                {
                    t.Cells[r, c].TextStyleId = kieu;
                    t.Cells[r, c].TextHeight = h;
                    t.Cells[r, c].ContentColor = xanh;
                    bool trai = r >= dau && (kyHieu ? c % 2 == 1 : c == 0);
                    t.Cells[r, c].Alignment = trai ? CellAlignment.MiddleLeft : CellAlignment.MiddleCenter;
                }
            }
            for (int c = 0; c < nCot; c++) t.Columns[c].Width = h * rong[c];
            t.GenerateLayout();
            return t;
        }

        /// PCCC_KYHIEU (kyHieu) / PCCC_QUYDINH: chọn vùng mặt bằng (Enter = cả model space; chỉ bảng ký hiệu), bấm góc trên trái, cao chữ.
        static void BangTay(bool kyHieu)
        {
            Document doc = AcApp.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;
            FileDanhMuc dm = DocDanhMuc();
            PromptSelectionResult sr = null;
            if (kyHieu)
            {
                PromptSelectionOptions so = new PromptSelectionOptions();
                so.MessageForAdding = "\nChọn mặt bằng cần lập bảng ký hiệu <Enter = cả model space>: ";
                sr = ed.GetSelection(so);
                if (sr.Status == PromptStatus.Cancel) return;
            }
            PromptPointResult pp = ed.GetPoint("\nĐiểm chèn bảng (góc trên trái): ");
            if (pp.Status != PromptStatus.OK) return;
            PromptDoubleOptions ho = new PromptDoubleOptions("\nChiều cao chữ (đơn vị bản vẽ): ");
            ho.DefaultValue = 0.25 / HeSoMet(db); // chữ 2,5 mm khi in tỉ lệ 1/100, như PCCC_BANG
            ho.UseDefaultValue = true;
            ho.AllowNegative = false; ho.AllowZero = false;
            PromptDoubleResult hr = ed.GetDouble(ho);
            if (hr.Status != PromptStatus.OK) return;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                List<string> ma = null;
                if (kyHieu)
                {
                    IEnumerable<ObjectId> ids = sr.Status == PromptStatus.OK ? (IEnumerable<ObjectId>)sr.Value.GetObjectIds()
                        : ((BlockTableRecord)tr.GetObject(((BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead))[BlockTableRecord.ModelSpace], OpenMode.ForRead)).Cast<ObjectId>().ToList();
                    ma = MaTrongVung(db, tr, ids, dm);
                    if (ma.Count == 0) { ed.WriteMessage("\nVùng chọn không có thiết bị, tuyến PCCC nào (vẽ bằng PCCC_DAT / PCCC_RAI, hoặc gán block bằng PCCC_GAN)."); return; }
                }
                Table t = DungBang(db, tr, kyHieu, ma, dm, pp.Value.TransformBy(ed.CurrentUserCoordinateSystem), hr.Value, DamBaoLayer(db, tr, LAYER_BANG, 3));
                BlockTableRecord space = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);
                space.AppendEntity(t);
                tr.AddNewlyCreatedDBObject(t, true);
                tr.Commit();
                ed.WriteMessage("\n[TKPCCC] " + (kyHieu ? "PCCC_KYHIEU dong=" + ma.Count + " " + string.Join(",", ma) : "PCCC_QUYDINH dong=" + (t.Rows.Count - 2)));
            }
        }

        [CommandMethod("PCCC_THUYETMINH")] public void LenhThuyetMinh() { Chay(ThuyetMinhTay); }

        // Mục văn mẫu tổng quát theo hệ (tiêu đề "## ..." trong mau-thuyet-minh-pccc.md). Bỏ các mục có số liệu riêng của công trình mẫu
        // (lưu lượng, bể, bơm, họng nước kèm khối tích bể): những mục đó phải tính cho từng công trình.
        static readonly string[][] MUC_THEO_HE = {
            new[] { "BC", "Báo cháy — thuyết minh chung, trung tâm, nút ấn, đầu báo", "Báo cháy — thiết bị chỉ thị, âm thanh, cáp tín hiệu, ghi chú khác",
                "Báo cháy — tủ trung tâm (5.12.3–5.12.7)", "Báo cháy — cắt điện khi cháy" },
            new[] { "SPR", "Chữa cháy — lý do chọn sprinkler nước", "Chữa cháy — ghi chú chung đường ống, đai giữ" },
            new[] { "HVT", "Chữa cháy — vách tường, van giảm áp" },
            new[] { "NUOC", "Trạm bơm — thuyết minh phòng bơm", "Trạm bơm — nguyên lý hoạt động cụm bơm (auto, cưỡng bức)" },
            new[] { "DEN", "Đèn EXIT — EM — thuyết minh, tính độ rọi", "Đèn EXIT — EM — thông số đèn", "Đèn EXIT — EM — thông số đèn exit" },
            new[] { "BINH", "Bình chữa cháy xách tay" } };

        /// Đọc các mục "## tiêu đề" + khối ```text``` của file văn mẫu (build.bat chép cạnh DLL).
        static Dictionary<string, string> DocVanMau()
        {
            string f = Path.Combine(ThuMuc, "mau-thuyet-minh-pccc.md");
            if (!File.Exists(f)) throw new System.Exception("thiếu mau-thuyet-minh-pccc.md cạnh DLL (chạy cad\\build.bat)");
            Dictionary<string, string> kq = new Dictionary<string, string>();
            string muc = null;
            StringBuilder b = null;
            foreach (string l in File.ReadAllLines(f, Encoding.UTF8))
            {
                if (l.StartsWith("## ")) { muc = l.Substring(3).Trim(); continue; }
                if (muc == null) continue;
                if (b == null && l.StartsWith("```text")) { b = new StringBuilder(); continue; }
                if (b != null && l.StartsWith("```")) { kq[muc] = b.ToString().TrimEnd(); b = null; muc = null; continue; }
                if (b != null) b.AppendLine(l);
            }
            return kq;
        }

        /// PCCC_THUYETMINH: chèn thuyết minh theo các hệ có thiết bị trong model space (đếm như PCCC_BOC), mỗi hệ một cột MText trên layer PCCC_THUYET_MINH.
        static void ThuyetMinhTay()
        {
            Document doc = AcApp.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;
            FileDanhMuc dm = DocDanhMuc();
            Dictionary<string, string> mau = DocVanMau();
            PromptPointResult pp = ed.GetPoint("\nĐiểm chèn thuyết minh (góc trên trái): ");
            if (pp.Status != PromptStatus.OK) return;
            PromptDoubleOptions ho = new PromptDoubleOptions("\nChiều cao chữ (đơn vị bản vẽ): ");
            ho.DefaultValue = 0.25 / HeSoMet(db);
            ho.UseDefaultValue = true;
            ho.AllowNegative = false; ho.AllowZero = false;
            PromptDoubleResult hr = ed.GetDouble(ho);
            if (hr.Status != PromptStatus.OK) return;
            double h = hr.Value;
            List<string> he = new List<string>();
            int soMuc = 0;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTableRecord ms = (BlockTableRecord)tr.GetObject(((BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead))[BlockTableRecord.ModelSpace], OpenMode.ForRead);
                HashSet<string> coHe = new HashSet<string>(MaTrongVung(db, tr, ms.Cast<ObjectId>().ToList(), dm).Select(x => TimMa(dm, x)).Where(x => x != null).Select(x => x.he));
                BlockTableRecord space = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);
                ObjectId lop = DamBaoLayer(db, tr, "PCCC_THUYET_MINH", 7);
                Point3d p = pp.Value.TransformBy(ed.CurrentUserCoordinateSystem);
                double rong = 90 * h;
                foreach (string[] dong in MUC_THEO_HE)
                {
                    if (!coHe.Contains(dong[0])) continue;
                    List<string> doan = dong.Skip(1).Where(x => mau.ContainsKey(x)).Select(x => mau[x]).ToList();
                    if (doan.Count == 0) continue;
                    MText t = new MText();
                    t.Location = p + new Vector3d(he.Count * (rong + 5 * h), 0, 0);
                    t.Attachment = AttachmentPoint.TopLeft;
                    t.Width = rong;
                    t.TextHeight = h;
                    t.LayerId = lop;
                    t.Contents = E(string.Join("\n\n", doan).Replace("\r", ""));
                    space.AppendEntity(t);
                    tr.AddNewlyCreatedDBObject(t, true);
                    he.Add(dong[0]);
                    soMuc += doan.Count;
                }
                tr.Commit();
            }
            if (he.Count == 0) { ed.WriteMessage("\nBản vẽ chưa có thiết bị PCCC nào (đếm như PCCC_BOC): không có hệ để viết thuyết minh."); return; }
            ed.WriteMessage("\n[TKPCCC] PCCC_THUYETMINH he=" + string.Join(",", he) + " muc=" + soMuc
                + " (văn mẫu của bộ bản vẽ người dùng: đọc lại, sửa số liệu, điều khoản cho đúng công trình)");
        }

        /// Thao tác spec "bang": loai "ky_hieu" (thiết bị trong "hop" [x1,y1,x2,y2], bỏ = cả bản vẽ) | "quy_dinh"; p góc trên trái; cao (chữ, mặc định 2,5 mm × 100).
        static void OpBang(GoiSpec g, Dictionary<string, object> o)
        {
            string loai = LayChu(o, "loai", "");
            if (loai != "ky_hieu" && loai != "quy_dinh") throw new System.Exception("bang: \"loai\" là \"ky_hieu\" hoặc \"quy_dinh\"");
            bool kyHieu = loai == "ky_hieu";
            double h = LaySo(o, "cao", 0.25 / HeSoMet(g.Db));
            List<string> ma = null;
            if (kyHieu)
            {
                double[] hop = HopOp(g, o);
                List<ObjectId> ids = new List<ObjectId>();
                foreach (ObjectId id in g.Ms)
                {
                    Entity e = g.Tr.GetObject(id, OpenMode.ForRead) as Entity;
                    if (e == null) continue;
                    if (hop != null)
                    {
                        Extents3d x;
                        try { x = e.GeometricExtents; } catch (System.Exception) { continue; }
                        if (x.MaxPoint.X < hop[0] || x.MinPoint.X > hop[2] || x.MaxPoint.Y < hop[1] || x.MinPoint.Y > hop[3]) continue;
                    }
                    ids.Add(id);
                }
                ma = MaTrongVung(g.Db, g.Tr, ids, g.Dm);
                if (ma.Count == 0) throw new System.Exception("bang ky_hieu: vùng không có thiết bị, tuyến PCCC nào");
            }
            Table t = DungBang(g.Db, g.Tr, kyHieu, ma, g.Dm, P(g, Lay(o, "p"), "p"), h, DamBaoLayer(g.Db, g.Tr, LAYER_BANG, 3));
            Them(g, t);
        }
    }
}
