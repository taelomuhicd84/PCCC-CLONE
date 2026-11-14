// Thêm block của người dùng vào thư viện (lệnh PCCC_THEMBLOCK) và vẽ ảnh xem trước cho bảng block (TKPCCC.UI.dll, lệnh PCCC_BLOCK).
// Mọi xử lý bản vẽ nằm ở đây để thử được trong accoreconsole; DLL giao diện chỉ hiện danh sách và gửi lệnh.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using AcApp = Autodesk.AutoCAD.ApplicationServices.Core.Application;
using Gdi = System.Drawing; // không "using System.Drawing": trùng tên Image, Region, Font với AutoCAD

namespace TKPCCC
{
    public partial class Lenh
    {
        const int CO_ANH = 64; // cạnh ảnh xem trước (px)
        // Đổi cách vẽ ảnh thì đổi chuỗi này: ảnh cũ (vd nền trắng nét đen) tự vẽ lại một lần, người dùng không phải xoá tay.
        const string KIEU_ANH = "nen_den_mau_that_1";

        static string ThuMucAnh { get { return Path.Combine(ThuMuc, "anh_block"); } }

        class ChuAnh { public Point3d P; public double Cao; public string T; public int Ngang, Dung; public Gdi.Color Mau; }
        class NetAnh { public Point3d[] P; public Gdi.Color Mau; }

        /// Màu nét như trên màn hình CAD nền đen: ByLayer lấy màu layer (layer 0 trong block lồng lấy layer của block cha),
        /// ByBlock lấy màu block cha (cấp ngoài cùng: trắng), ACI 7 vẽ trắng, true color giữ nguyên.
        static Gdi.Color MauAnh(Transaction tr, Entity e, Gdi.Color mauCha, Gdi.Color? lopCha)
        {
            Autodesk.AutoCAD.Colors.Color c = e.Color;
            if (c.IsByBlock) return mauCha;
            if (c.IsByLayer) return MauLop((LayerTableRecord)tr.GetObject(e.LayerId, OpenMode.ForRead), lopCha);
            return MauRgb(c);
        }

        static Gdi.Color MauRgb(Autodesk.AutoCAD.Colors.Color c)
        {
            if (c.IsByAci && c.ColorIndex == 7) return Gdi.Color.White;
            if (c.IsByAci || c.IsByColor) return Gdi.Color.FromArgb(255, c.ColorValue);
            return Gdi.Color.White; // ByPen, None...: vẽ trắng như màu mặc định của CAD
        }

        // ---------- Ảnh xem trước ----------

        /// Gom nét của block (toạ độ block): mọi đường cong lấy mẫu theo tham số, block lồng tới 2 cấp, chữ giữ điểm đặt + chiều cao, kèm màu.
        static void GomNet(Transaction tr, BlockTableRecord b, Matrix3d m, int sau, Gdi.Color mauCha, Gdi.Color? lopCha, List<NetAnh> net, List<ChuAnh> chu)
        {
            foreach (ObjectId id in b)
            {
                Entity e = tr.GetObject(id, OpenMode.ForRead) as Entity;
                if (e == null || !e.Visible || e is AttributeDefinition) continue;
                Gdi.Color mau = MauAnh(tr, e, mauCha, lopCha);
                Curve c = e as Curve;
                BlockReference br = e as BlockReference;
                MText mt = e as MText;
                DBText dt = e as DBText;
                try
                {
                    if (c != null)
                    {
                        Polyline pl = c as Polyline;
                        int n = c is Line ? 1 : (pl != null ? 8 * Math.Max(1, pl.NumberOfVertices - (pl.Closed ? 0 : 1)) : 32);
                        double t0 = c.StartParam, t1 = c.EndParam;
                        Point3d[] ps = new Point3d[n + 1];
                        for (int i = 0; i <= n; i++) ps[i] = c.GetPointAtParameter(t0 + (t1 - t0) * i / n).TransformBy(m);
                        net.Add(new NetAnh { P = ps, Mau = mau });
                    }
                    else if (br != null && sau < 2)
                    {
                        // Nét layer 0 trong block con lấy màu layer của block con đó (block con cũng ở layer 0 thì lấy tiếp của cha).
                        Gdi.Color lop = MauLop((LayerTableRecord)tr.GetObject(br.LayerId, OpenMode.ForRead), lopCha);
                        GomNet(tr, (BlockTableRecord)tr.GetObject(br.BlockTableRecord, OpenMode.ForRead), m * br.BlockTransform, sau + 1, mau, lop, net, chu);
                    }
                    else if (mt != null || dt != null)
                    {
                        double k = Vector3d.YAxis.TransformBy(m).Length;
                        int a = mt != null ? (int)mt.Attachment - 1 : 6; // MText: 0..8 = trên-trái .. dưới-phải; DBText coi là dưới-trái
                        chu.Add(new ChuAnh { P = (mt != null ? mt.Location : dt.Position).TransformBy(m), Cao = (mt != null ? mt.TextHeight : dt.Height) * k,
                            T = mt != null ? mt.Text : dt.TextString, Ngang = a % 3, Dung = a / 3, Mau = mau });
                    }
                }
                catch (System.Exception) { } // nét lạ (tham số hỏng...): bỏ qua, ảnh chỉ để nhận mặt ký hiệu
            }
        }

        /// Màu của layer l như vẽ trên nền đen (layer 0 lồng thì theo cha).
        static Gdi.Color MauLop(LayerTableRecord l, Gdi.Color? lopCha)
        {
            if (l.Name == "0" && lopCha.HasValue) return lopCha.Value;
            return MauRgb(l.Color);
        }

        /// Ảnh xem trước của một block như trên màn hình CAD: nền đen, nét đúng màu, vừa khít ô CO_ANH.
        /// shortcut: không vẽ hatch, không xoay chữ, block động vẽ theo trạng thái đang lưu trong định nghĩa; cần đẹp hơn thì dùng ảnh của AutoCAD.
        static void VeAnhBlock(Transaction tr, BlockTableRecord b, string file)
        {
            List<NetAnh> net = new List<NetAnh>();
            List<ChuAnh> chu = new List<ChuAnh>();
            GomNet(tr, b, Matrix3d.Identity, 0, Gdi.Color.White, null, net, chu);
            double x0 = double.MaxValue, y0 = double.MaxValue, x1 = double.MinValue, y1 = double.MinValue;
            foreach (Point3d p in net.SelectMany(n => n.P).Concat(chu.SelectMany(c => new Point3d[] {
                new Point3d(c.P.X - c.Cao / 2, c.P.Y - c.Cao / 2, 0), new Point3d(c.P.X + c.Cao / 2, c.P.Y + c.Cao / 2, 0) })))
            {
                x0 = Math.Min(x0, p.X); y0 = Math.Min(y0, p.Y); x1 = Math.Max(x1, p.X); y1 = Math.Max(y1, p.Y);
            }
            using (Gdi.Bitmap bmp = new Gdi.Bitmap(CO_ANH, CO_ANH))
            using (Gdi.Graphics g = Gdi.Graphics.FromImage(bmp))
            {
                g.Clear(Gdi.Color.Black);
                g.SmoothingMode = Gdi.Drawing2D.SmoothingMode.AntiAlias;
                double rong = Math.Max(x1 - x0, y1 - y0);
                if (rong > 1e-9)
                {
                    double k = (CO_ANH - 8) / rong, ox = (CO_ANH - (x1 - x0) * k) / 2, oy = (CO_ANH - (y1 - y0) * k) / 2;
                    Func<Point3d, Gdi.PointF> doi = p => new Gdi.PointF((float)(ox + (p.X - x0) * k), (float)(CO_ANH - oy - (p.Y - y0) * k));
                    foreach (NetAnh n in net)
                        using (Gdi.Pen but = new Gdi.Pen(n.Mau, 1.2f))
                            g.DrawLines(but, n.P.Select(doi).ToArray());
                    foreach (ChuAnh c in chu)
                        using (Gdi.Font f = new Gdi.Font("Arial", (float)Math.Max(6, Math.Min(40, c.Cao * k)), Gdi.GraphicsUnit.Pixel))
                        using (Gdi.StringFormat sf = new Gdi.StringFormat())
                        {
                            sf.Alignment = (Gdi.StringAlignment)c.Ngang;   // 0 trái, 1 giữa, 2 phải
                            sf.LineAlignment = (Gdi.StringAlignment)c.Dung; // 0 trên, 1 giữa, 2 dưới
                            using (Gdi.SolidBrush bc = new Gdi.SolidBrush(c.Mau))
                                g.DrawString(c.T, f, bc, doi(c.P), sf);
                        }
                }
                bmp.Save(file, Gdi.Imaging.ImageFormat.Png);
            }
        }

        /// Vẽ lại ảnh cho block nào chưa có ảnh hoặc ảnh cũ hơn PCCC_Blocks.dwg. Trả số ảnh vừa vẽ.
        static int CapNhatAnhBlock(FileDanhMuc dm)
        {
            if (!File.Exists(FileThuVien)) return 0;
            Directory.CreateDirectory(ThuMucAnh);
            DateTime luc = File.GetLastWriteTimeUtc(FileThuVien);
            string fKieu = Path.Combine(ThuMucAnh, "kieu_anh.txt");
            if (!File.Exists(fKieu) || File.ReadAllText(fKieu).Trim() != KIEU_ANH) luc = DateTime.MaxValue; // ảnh kiểu cũ: vẽ lại hết
            List<VatTu> can = dm.vat_tu.Where(v => v.loai == "block" && File.GetLastWriteTimeUtc(Path.Combine(ThuMucAnh, v.ma + ".png")) < luc).ToList(); // file chưa có thì giờ ghi = năm 1601
            if (can.Count == 0) { File.WriteAllText(fKieu, KIEU_ANH); return 0; }
            int n = 0;
            using (Database db = new Database(false, true))
            {
                db.ReadDwgFile(FileThuVien, FileOpenMode.OpenForReadAndAllShare, true, "");
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                    foreach (VatTu v in can)
                    {
                        if (!bt.Has(v.ma)) continue;
                        VeAnhBlock(tr, (BlockTableRecord)tr.GetObject(bt[v.ma], OpenMode.ForRead), Path.Combine(ThuMucAnh, v.ma + ".png"));
                        n++;
                    }
                    tr.Commit();
                }
            }
            File.WriteAllText(fKieu, KIEU_ANH);
            return n;
        }

        /// Cho TKPCCC.UI.dll (bảng PCCC_BLOCK): danh mục block + tên hệ + thư mục ảnh xem trước đã cập nhật.
        public static List<VatTu> DanhMucBlock(out Dictionary<string, string> he, out string thuMucAnh)
        {
            FileDanhMuc dm = DocDanhMuc();
            DamBaoThuVien(dm);
            CapNhatAnhBlock(dm);
            he = dm.he;
            thuMucAnh = ThuMucAnh;
            return dm.vat_tu.Where(v => v.loai == "block").ToList();
        }

        /// Cho bảng PCCC_BLOCK: số lượng theo mã vật tư trong Model của bản vẽ (thiết bị: cái, ống/dây: m), cùng cách đếm với PCCC_BOC.
        public static Dictionary<string, double> DemBanVe(Database db)
        {
            BoiCanh bc = new BoiCanh();
            bc.Dm = DocDanhMuc();
            bc.Alias = DocAlias();
            bc.Khu = new List<Khu>();
            bc.HeSo = HeSoMet(db);
            bc.Kq = new KetQua();
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                bc.Tr = tr;
                BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                foreach (ObjectId id in (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead))
                {
                    Entity e = tr.GetObject(id, OpenMode.ForRead) as Entity;
                    if (e != null) Duyet(e, Matrix3d.Identity, 0, 1, bc);
                }
                tr.Commit();
            }
            return bc.Kq.Tong;
        }

        // ---------- Thêm block của người dùng ----------

        /// Hỏi một chuỗi. Trả null khi người dùng huỷ (Esc), chuỗi rỗng khi chỉ Enter.
        static string HoiChu(Editor ed, string loiNhac, bool coCach)
        {
            PromptStringOptions o = new PromptStringOptions("\n" + loiNhac + ": ");
            o.AllowSpaces = coCach;
            PromptResult r = ed.GetString(o);
            return r.Status == PromptStatus.OK ? r.StringResult.Trim() : null;
        }

        /// Nội dung mới của file danh mục khi thêm một dòng vào mảng "vat_tu", giữ nguyên cách trình bày (mỗi vật tư một dòng) để diff git chỉ có một dòng.
        /// Chưa ghi gì: mã đã có trong file, file chỉ đọc hoặc đang bị khoá thì ném lỗi để PCCC_THEMBLOCK dừng TRƯỚC khi lưu thư viện.
        static string DanhMucThem(string file, VatTu v)
        {
            JavaScriptSerializer js = new JavaScriptSerializer();
            string t = File.ReadAllText(file, Encoding.UTF8);
            if (TimMa(js.Deserialize<FileDanhMuc>(t), v.ma) != null) throw new System.Exception("mã " + v.ma + " đã có trong " + file + ": chưa ghi gì");
            int dong = t.LastIndexOf(']'), cuoi = dong < 0 ? -1 : t.LastIndexOf('}', dong);
            if (cuoi < 0) throw new System.Exception("không đọc được cấu trúc " + file);
            string moi = t.Substring(0, cuoi + 1) + "," + (t.Contains("\r\n") ? "\r\n" : "\n") + "    { \"ma\": " + js.Serialize(v.ma) + ", \"ten\": " + js.Serialize(v.ten)
                + ", \"dvt\": " + js.Serialize(v.dvt) + ", \"he\": " + js.Serialize(v.he) + ", \"loai\": \"block\" }" + t.Substring(cuoi + 1);
            // Tự kiểm trước khi ghi đè: file mới phải đọc lại được và có mã vừa thêm.
            if (TimMa(js.Deserialize<FileDanhMuc>(moi), v.ma) == null) throw new System.Exception("ghi danh mục hỏng, chưa lưu: " + file);
            try { using (new FileStream(file, FileMode.Open, FileAccess.ReadWrite, FileShare.None)) { } }
            catch (System.Exception ex) { throw new System.Exception("không ghi được " + file + " (chỉ đọc hoặc đang mở ở chương trình khác): " + ex.Message + "; chưa ghi gì"); }
            return moi;
        }

        /// PCCC_THEMBLOCK: đưa một block của người dùng (kể cả block động) vào PCCC_Blocks.dwg dưới một mã vật tư MỚI và thêm dòng vào vat_tu.json.
        /// shortcut: chỉ nhận mã mới; đổi hình cho mã có sẵn thì sửa trong PCCC_Blocks.dwg hoặc dùng PCCC_GAN. Block phải vẽ theo mm như thư viện.
        static void ThemBlock()
        {
            Document doc = AcApp.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;
            FileDanhMuc dm = DocDanhMuc();
            PromptSelectionOptions chon = new PromptSelectionOptions();
            chon.MessageForAdding = "\nChọn một block trong bản vẽ (kể cả block động): ";
            chon.SingleOnly = true;
            PromptSelectionResult r = ed.GetSelection(chon, new SelectionFilter(new TypedValue[] { new TypedValue((int)DxfCode.Start, "INSERT") }));
            if (r.Status != PromptStatus.OK || r.Value.Count == 0) return;

            VatTu v = new VatTu();
            v.loai = "block";
            v.ma = (HoiChu(ed, "Mã vật tư mới (chữ hoa không dấu, số, gạch dưới; vd VAN_BUOM_DN100)", false) ?? "").ToUpperInvariant();
            if (v.ma == "") return;
            if (!Regex.IsMatch(v.ma, "^[A-Z][A-Z0-9_]{1,30}$")) throw new System.Exception("mã chỉ gồm chữ không dấu, số, gạch dưới, bắt đầu bằng chữ, dài 2 đến 31 ký tự");
            if (TimMa(dm, v.ma) != null)
                throw new System.Exception("mã " + v.ma + " đã có trong danh mục. Dùng block của bạn cho mã có sẵn: PCCC_GAN (để bóc khối lượng) hoặc sửa hình trong PCCC_Blocks.dwg");
            v.ten = HoiChu(ed, "Tên thiết bị", true);
            if (string.IsNullOrEmpty(v.ten)) return;
            string he = HoiChu(ed, "Hệ (" + string.Join("/", dm.he.Keys) + ") <KHAC>", false);
            if (he == null) return;
            v.he = he == "" ? "KHAC" : he.ToUpperInvariant();
            if (!dm.he.ContainsKey(v.he)) throw new System.Exception("không có hệ \"" + v.he + "\"");
            v.dvt = HoiChu(ed, "Đơn vị tính <cái>", true);
            if (v.dvt == null) return;
            if (v.dvt == "") v.dvt = "cái";

            // Danh mục: bản cạnh DLL (plugin đọc) và file gốc của repo (nguồn duy nhất; build.bat ghi đường dẫn vào nguon_vat_tu.txt).
            // Dựng và tự kiểm nội dung mới của cả hai file TRƯỚC, rồi lưu thư viện, rồi mới ghi danh mục: lỗi danh mục không để lại block mồ côi.
            List<string> ghi = new List<string> { Path.Combine(ThuMuc, "vat_tu.json") };
            string fNguon = Path.Combine(ThuMuc, "nguon_vat_tu.txt");
            if (File.Exists(fNguon))
            {
                string goc = Path.GetFullPath(File.ReadAllText(fNguon).Trim());
                if (File.Exists(goc) && !string.Equals(goc, ghi[0], StringComparison.OrdinalIgnoreCase)) ghi.Add(goc);
            }
            List<string> noiDung = ghi.Select(f => DanhMucThem(f, v)).ToList();

            ObjectId nguon;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockReference br = (BlockReference)tr.GetObject(r.Value.GetObjectIds()[0], OpenMode.ForRead);
                nguon = br.IsDynamicBlock ? br.DynamicBlockTableRecord : br.BlockTableRecord; // block động đã chỉnh mang tên vô danh *U: lấy định nghĩa gốc
                BlockTableRecord b = (BlockTableRecord)tr.GetObject(nguon, OpenMode.ForRead);
                if (b.IsFromExternalReference || b.IsLayout || b.Name.StartsWith("*")) throw new System.Exception("không thêm được xref, layout hay block vô danh");
                tr.Commit();
            }

            DamBaoThuVien(dm); // tạo PCCC_Blocks.dwg nếu chưa có
            using (Database tv = new Database(false, true))
            {
                tv.ReadDwgFile(FileThuVien, FileOpenMode.OpenForReadAndAllShare, true, "");
                tv.CloseInput(true); // đọc hết vào bộ nhớ, nhả file để ghi đè được
                IdMapping map = new IdMapping();
                ObjectIdCollection ids = new ObjectIdCollection();
                ids.Add(nguon);
                db.WblockCloneObjects(ids, tv.BlockTableId, map, DuplicateRecordCloning.Ignore, false);
                IdPair cap = map[nguon];
                if (!cap.IsCloned) throw new System.Exception("thư viện đã có block trùng tên với block bạn chọn: đổi tên block trong bản vẽ (RENAME) rồi thêm lại");
                Database dangLam = HostApplicationServices.WorkingDatabase;
                HostApplicationServices.WorkingDatabase = tv; // đối tượng mới lấy mặc định từ WorkingDatabase, không đổi thì eWrongDatabase
                try
                {
                    using (Transaction tr = tv.TransactionManager.StartTransaction())
                    {
                        BlockTable bt = (BlockTable)tr.GetObject(tv.BlockTableId, OpenMode.ForRead);
                        // Block vừa chép còn mang tên gốc: tên gốc trùng mã (block VAN_KHACH, mã van_khach) là chính nó, không phải trùng.
                        if (bt.Has(v.ma) && bt[v.ma] != cap.Value) throw new System.Exception("thư viện đã có block tên " + v.ma);
                        BlockTableRecord b = (BlockTableRecord)tr.GetObject(cap.Value, OpenMode.ForWrite);
                        b.Name = v.ma;
                        // MA_VT để PCCC_BOC đếm: block động chèn ra mang tên vô danh, chỉ còn thuộc tính này để nhận ra.
                        bool coMa = false;
                        foreach (ObjectId id in b)
                        {
                            AttributeDefinition a = tr.GetObject(id, OpenMode.ForRead) as AttributeDefinition;
                            if (a == null || a.Tag.ToUpperInvariant() != TAG_MA) continue;
                            a.UpgradeOpen();
                            a.TextString = v.ma;
                            coMa = true;
                        }
                        if (!coMa) ThemAttDef(b, tr, DamBaoKieuChu(tv, tr), TAG_MA, v.ma, Point3d.Origin);
                        BlockTableRecord ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);
                        int i = ms.Cast<ObjectId>().Count(); // bày mẫu nối tiếp các mẫu đã có, như DamBaoThuVien
                        BlockReference mau = new BlockReference(new Point3d((i % 8) * 1000, -(i / 8) * 1000, 0), cap.Value);
                        ms.AppendEntity(mau);
                        tr.AddNewlyCreatedDBObject(mau, true);
                        tr.Commit();
                    }
                }
                finally { HostApplicationServices.WorkingDatabase = dangLam; }
                tv.SaveAs(FileThuVien, DwgVersion.Current);
            }

            for (int i = 0; i < ghi.Count; i++) HangDoi.GhiAnToan(ghi[i], noiDung[i]);
            CapNhatAnhBlock(DocDanhMuc());
            ed.WriteMessage("\n[TKPCCC] PCCC_THEMBLOCK " + v.ma + " -> " + FileThuVien + "; danh mục: " + string.Join(", ", ghi));
            if (ghi.Count == 1) ed.WriteMessage("\nLưu ý: không thấy file danh mục gốc của repo (nguon_vat_tu.txt), mã mới chỉ nằm ở bản cạnh DLL và sẽ mất khi chạy lại build.bat.");
        }
    }
}
