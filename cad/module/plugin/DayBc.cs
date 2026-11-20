// PCCC_DAYBC: đi dây báo cháy tự động theo 2 trường phái (người dùng 16/11; bản vẽ mẫu file_cad_test\test báo cháy.dwg:
// nửa trái hệ địa chỉ, nửa phải hệ thường; cách đọc mẫu ở cad/skills/ve/bao_chay/day_zone.md).
// - Hệ thường: MỘT đường tín hiệu từ tủ qua mọi đầu báo + nút nhấn, chỉ đi vuông góc (zíc zắc theo hàng, không đi xéo), mạch hở không vòng về tủ;
//   thiết bị cuối đường là thiết bị XA TỦ NHẤT (tiết kiệm dây) và gắn điện trở cuối dây. Chuông: một đường nguồn riêng, mạch cụt.
// - Hệ địa chỉ: mạch vòng tủ -> đầu báo / nút nhấn địa chỉ + module (ZM, CM, R2ML, ngoại vi) -> về tủ (dây hồng). Đầu báo, nút nhấn THƯỜNG nối về
//   module ZM gần nhất (mạch hở, điện trở cuối dây ở thiết bị xa ZM nhất); chuông thường nối về module CM, chuông địa chỉ về module nguồn R2ML
//   (tủ -> module -> các chuông, mạch cụt).
// Layer dây theo loại thiết bị ở đầu đến của mỗi đoạn: trang Báo cháy chọn, bảng layer ở trang Cài đặt (CaiDat.cs).
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using AcApp = Autodesk.AutoCAD.ApplicationServices.Core.Application;

[assembly: CommandClass(typeof(TKPCCC.Lenh.LenhDayBc))]

namespace TKPCCC
{
    public partial class Lenh
    {
        public class LenhDayBc
        {
            [CommandMethod("PCCC_DAYBC")] public void LenhDay() { Chay(DayBcTay); }
        }

        class TbBc
        {
            public Point2d P;
            public string Loai;      // dau_bao, nut_nhan, chuong, zm, cm, r2ml, ngoai_vi, tu
            public bool DiaChi;
            public bool CoChuong;    // tổ hợp nút nhấn + chuông
        }

        /// Chữ nhận dạng block: mã, tên block, chữ thuộc tính, tên block lồng một tầng; bỏ dấu, viết hoa.
        static string ChuNhanBc(BlockReference br, Transaction tr, string ma)
        {
            List<string> s = new List<string> { ma ?? "", TenBlock(br, tr) };
            foreach (ObjectId id in br.AttributeCollection)
            {
                AttributeReference ar = tr.GetObject(id, OpenMode.ForRead) as AttributeReference;
                if (ar != null) s.Add(ar.TextString);
            }
            int dem = 0;
            foreach (ObjectId id in (BlockTableRecord)tr.GetObject(br.BlockTableRecord, OpenMode.ForRead))
            {
                if (++dem > 200) break;
                BlockReference con = tr.GetObject(id, OpenMode.ForRead) as BlockReference;
                if (con != null && !(con is Table)) s.Add(TenBlock(con, tr));
            }
            return BoDau(string.Join(" | ", s)).ToUpperInvariant();
        }

        /// Loại thiết bị báo cháy từ mã vật tư, rồi từ chữ của block (bản vẽ người khác vẽ: block "ZM", "RM" có thuộc tính R2ML, "CHUÔNG BÁO CHÁY",
        /// block ẩn danh chứa "NUT NHAN KHAN"). null = không phải thiết bị cần nối dây (điện trở cuối dây, nội thất...).
        static TbBc PhanLoaiBc(BlockReference br, BoiCanh bc)
        {
            string ma = (MaThietBi(br, bc) ?? "").ToUpperInvariant();
            string ten = TenBlock(br, bc.Tr).ToUpperInvariant();
            string s = ChuNhanBc(br, bc.Tr, ma);
            Func<string, bool> co = k => Regex.IsMatch(s, "(^|[^A-Z0-9])" + k + "([^A-Z0-9]|$)");
            if (ma == "BC_DTCD" || co("DTCD") || s.Contains("DIEN TRO")) return null;
            TbBc t = new TbBc();
            if (ma == "BC_TT" || co("TTBC") || co("FACP") || s.Contains("TRUNG TAM BAO CHAY")) t.Loai = "tu";
            else if (ma == "BC_R2ML" || co("R2ML") || ten == "RM") t.Loai = "r2ml";
            else if (ma == "BC_ZM" || co("ZM")) t.Loai = "zm";
            else if (ma == "BC_CM" || co("CM")) t.Loai = "cm";
            else if (ma == "BC_MODULE" || s.Contains("MODULE")) t.Loai = "ngoai_vi";
            else if (ma == "BC_TOHOP" || s.Contains("TO HOP")) { t.Loai = "nut_nhan"; t.CoChuong = true; }
            else if (ma.StartsWith("BC_NUTAN") || s.Contains("NUT NHAN") || s.Contains("NUT AN") || co("MCP")) t.Loai = "nut_nhan";
            else if (ma.StartsWith("BC_CHUONG") || ma == "BC_COI_DEN" || ma == "BC_DEN" || s.Contains("CHUONG") || co("BELL") || s.Contains("COI DEN")) t.Loai = "chuong";
            else if (ma.StartsWith("BC_KHOI") || ma.StartsWith("BC_NHIET") || ma.StartsWith("BC_LUA") || ma == "BC_BEAM" || s.Contains("DAU BAO")
                || co("SMOKE") || co("HEAT")) t.Loai = "dau_bao";
            else return null;
            t.DiaChi = ma.EndsWith("_DC") || s.Contains("DIA CHI") || co("ADDR") || co("ADDRESSABLE");
            // Điểm nối: điểm chèn; block vẽ xa gốc (gốc block ở chỗ khác, như ZM, CM của bản vẽ mẫu) thì lấy tâm hình.
            Point3d p = br.Position;
            try
            {
                Extents3d x = br.GeometricExtents;
                double r = Math.Max(x.MaxPoint.X - x.MinPoint.X, x.MaxPoint.Y - x.MinPoint.Y) * 0.25;
                if (p.X < x.MinPoint.X - r || p.X > x.MaxPoint.X + r || p.Y < x.MinPoint.Y - r || p.Y > x.MaxPoint.Y + r)
                    p = new Point3d((x.MinPoint.X + x.MaxPoint.X) / 2, (x.MinPoint.Y + x.MaxPoint.Y) / 2, 0);
            }
            catch (Autodesk.AutoCAD.Runtime.Exception) { } // block rỗng: giữ điểm chèn
            t.P = new Point2d(p.X, p.Y);
            return t;
        }

        static double Mh(Point2d a, Point2d b) { return Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y); }

        /// Thứ tự rắn bò qua lưới (theo hàng Y hoặc cột X, bắt đầu hàng đầu hay hàng cuối, chiều đầu tiên): cách ngắn nhất tính từ "dau" (Manhattan).
        static List<Point2d> RanBoBc(List<Point2d> ps, Point2d dau, double eps)
        {
            List<Point2d> tot = new List<Point2d>(ps);
            double dTot = double.MaxValue;
            if (ps.Count < 2) return tot;
            foreach (bool theoY in new[] { true, false })
            {
                Func<Point2d, double> khoa = q => theoY ? q.Y : q.X, doc = q => theoY ? q.X : q.Y;
                List<List<Point2d>> hang = new List<List<Point2d>>();
                double dauHang = double.MinValue;
                foreach (Point2d q in ps.OrderBy(khoa))
                {
                    if (hang.Count == 0 || khoa(q) - dauHang > eps) { hang.Add(new List<Point2d>()); dauHang = khoa(q); }
                    hang[hang.Count - 1].Add(q);
                }
                foreach (bool tuDau in new[] { true, false })
                    foreach (bool tang in new[] { true, false })
                    {
                        List<Point2d> ds = new List<Point2d>();
                        bool c = tang;
                        foreach (List<Point2d> h in tuDau ? hang : Enumerable.Reverse(hang))
                        {
                            ds.AddRange(c ? h.OrderBy(doc) : h.OrderByDescending(doc));
                            c = !c;
                        }
                        double d = Mh(dau, ds[0]);
                        for (int i = 0; i + 1 < ds.Count; i++) d += Mh(ds[i], ds[i + 1]);
                        if (d < dTot - 1e-9) { dTot = d; tot = ds; }
                    }
            }
            return tot;
        }

        /// Chèn q vào chuỗi n (giữ n[0]; giữ n[cuối] khi coDinhCuoi, không thì được nối thêm vào cuối) ở chỗ tăng chiều dài ít nhất.
        static void ChenRe(List<Point2d> n, Point2d q, bool coDinhCuoi)
        {
            int tot = n.Count;
            double dTot = coDinhCuoi ? double.MaxValue : Mh(n[n.Count - 1], q);
            for (int i = 1; i < n.Count; i++)
            {
                double d = Mh(n[i - 1], q) + Mh(q, n[i]) - Mh(n[i - 1], n[i]);
                if (d < dTot) { dTot = d; tot = i; }
            }
            n.Insert(tot, q);
        }

        /// 2-opt: đảo đoạn giữa khi làm chuỗi ngắn đi (Manhattan = chiều dài dây vuông góc). Giữ n[0]; giữ n[cuối] khi coDinhCuoi.
        static void HaiOpt(List<Point2d> n, bool coDinhCuoi)
        {
            for (int vong = 0; vong < 60; vong++)
            {
                bool doi = false;
                for (int i = 0; i < n.Count - 2; i++)
                    for (int k = i + 2; k < n.Count; k++)
                    {
                        bool cuoi = k == n.Count - 1;
                        if (cuoi && coDinhCuoi) continue;
                        double truoc = Mh(n[i], n[i + 1]) + (cuoi ? 0 : Mh(n[k], n[k + 1])), sau = Mh(n[i], n[k]) + (cuoi ? 0 : Mh(n[i + 1], n[k + 1]));
                        if (sau < truoc - 1e-6 * (truoc + 1)) { n.Reverse(i + 1, k - i); doi = true; }
                    }
                if (!doi) break;
            }
        }

        /// Chuỗi điểm dừng từ "dau" qua mọi thiết bị: rắn bò theo lưới các thiết bị "luoi", thiết bị khác (nút nhấn, module) chèn vào chỗ tốn
        /// ít dây nhất, rồi 2-opt. cuoi: điểm cuối cố định (= dau: mạch vòng; thiết bị xa tủ nhất: hệ thường); null = cuối tự do. Trả cả dau, cuoi.
        static List<Point2d> ChuoiDung(Point2d dau, Point2d? cuoi, List<Point2d> luoi, List<Point2d> khac, double eps)
        {
            List<Point2d> n = new List<Point2d> { dau };
            n.AddRange(RanBoBc(luoi, dau, eps));
            if (cuoi.HasValue) n.Add(cuoi.Value);
            foreach (Point2d q in khac.OrderBy(q => Mh(dau, q))) ChenRe(n, q, cuoi.HasValue);
            HaiOpt(n, cuoi.HasValue);
            return n;
        }

        /// Dây chuông kiểu xương cá (người dùng 16/11: "nối 1 đường mạch cụt, xương cá là được"): từ gốc, mỗi lần nối chuông gần cây nhất vào
        /// điểm gần nhất của cây bằng nhánh vuông góc (rẽ vuông góc với đoạn cây chỗ rẽ). Trả các nhánh (mỗi nhánh một polyline).
        static List<List<Point2d>> CayXuongCa(Point2d goc, List<Point2d> diem, double eps)
        {
            List<Point2d[]> cay = new List<Point2d[]>();
            List<List<Point2d>> kq = new List<List<Point2d>>();
            List<Point2d> con = new List<Point2d>(diem);
            while (con.Count > 0)
            {
                Point2d tot = con[0], q = goc;
                Point2d[] tren = null;
                double dTot = double.MaxValue;
                foreach (Point2d b in con)
                {
                    if (Mh(goc, b) < dTot) { dTot = Mh(goc, b); tot = b; q = goc; tren = null; }
                    foreach (Point2d[] s in cay)
                    {
                        Point2d c = new Point2d(Math.Min(Math.Max(b.X, Math.Min(s[0].X, s[1].X)), Math.Max(s[0].X, s[1].X)),
                            Math.Min(Math.Max(b.Y, Math.Min(s[0].Y, s[1].Y)), Math.Max(s[0].Y, s[1].Y)));
                        if (Mh(c, b) < dTot - 1e-9) { dTot = Mh(c, b); tot = b; q = c; tren = s; }
                    }
                }
                con.Remove(tot);
                List<Point2d> nhanh = new List<Point2d> { q };
                if (Math.Abs(q.X - tot.X) > eps && Math.Abs(q.Y - tot.Y) > eps)
                {
                    bool ngang = tren != null && Math.Abs(tren[0].Y - tren[1].Y) <= eps; // rẽ khỏi đoạn ngang thì đi đứng trước
                    nhanh.Add(tren == null || ngang ? new Point2d(q.X, tot.Y) : new Point2d(tot.X, q.Y));
                }
                nhanh.Add(tot);
                for (int i = 0; i + 1 < nhanh.Count; i++) cay.Add(new[] { nhanh[i], nhanh[i + 1] });
                kq.Add(nhanh);
            }
            return kq;
        }

        /// Đoạn vuông góc giữa các điểm dừng (người dùng 16/11: chỉ đi zíc zắc, không đi xéo): hai điểm lệch cả X lẫn Y thì gãy một góc;
        /// chọn góc để không quay ngược đoạn trước và để đoạn vào điểm sau thẳng hàng với đoạn ra kế tiếp. Trả mỗi đoạn một danh sách điểm.
        static List<List<Point2d>> DoanVuong(List<Point2d> n, double eps)
        {
            List<List<Point2d>> kq = new List<List<Point2d>>();
            Vector2d truoc = new Vector2d(0, 0);
            for (int i = 0; i + 1 < n.Count; i++)
            {
                Point2d a = n[i], b = n[i + 1];
                List<Point2d> d = new List<Point2d> { a };
                if (Math.Abs(a.X - b.X) > eps && Math.Abs(a.Y - b.Y) > eps)
                {
                    Point2d? c = i + 2 < n.Count ? n[i + 2] : (Point2d?)null;
                    Func<Point2d, double> phat = g =>
                    {
                        Vector2d v1 = g - a, v2 = b - g;
                        double p = 0;
                        double dt = truoc.Length > 0 && v1.Length > 0 ? truoc.GetNormal().DotProduct(v1.GetNormal()) : 0;
                        if (dt < -0.5) p += 1; else if (dt > 0.5) p -= 0.25; // quay ngược / đi thẳng tiếp
                        if (c.HasValue)
                        {
                            Vector2d v3 = c.Value - b;
                            bool thang = Math.Abs(v3.X) <= eps || Math.Abs(v3.Y) <= eps;
                            if (thang && v3.Length > eps && v2.Length > 0)
                            {
                                double cs = v2.GetNormal().DotProduct(v3.GetNormal());
                                if (cs < -0.5) p += 1; else if (cs > 0.5) p -= 0.5;
                            }
                        }
                        return p;
                    };
                    Point2d g1 = new Point2d(a.X, b.Y), g2 = new Point2d(b.X, a.Y);
                    d.Add(phat(g1) <= phat(g2) ? g1 : g2);
                }
                d.Add(b);
                Vector2d cuoi = d[d.Count - 1] - d[d.Count - 2];
                if (cuoi.Length > 0) truoc = cuoi;
                kq.Add(d);
            }
            return kq;
        }

        /// Layer dây theo bảng ở trang Cài đặt (màu, kiểu nét); tên chưa có trong bảng thì màu 7.
        static ObjectId DamBaoLopDay(Database db, Transaction tr, string ten)
        {
            LayerTable lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
            if (lt.Has(ten)) return lt[ten];
            string[] r = CaiDat.LopDay().FirstOrDefault(x => string.Equals(x[0], ten, StringComparison.OrdinalIgnoreCase));
            short mau;
            if (r == null || !short.TryParse(r[1], out mau) || mau < 1 || mau > 255) mau = 7;
            ObjectId id = DamBaoLayer(db, tr, ten, mau);
            if (r != null && r[2] != "" && !r[2].Equals("Continuous", StringComparison.OrdinalIgnoreCase))
            {
                LinetypeTable t = (LinetypeTable)tr.GetObject(db.LinetypeTableId, OpenMode.ForRead);
                if (!t.Has(r[2]))
                    foreach (string f in new[] { "acadiso.lin", "acad.lin" })
                    {
                        try { db.LoadLineTypeFile(r[2], f); break; }
                        catch (System.Exception) { }
                    }
                if (t.Has(r[2])) ((LayerTableRecord)tr.GetObject(id, OpenMode.ForWrite)).LinetypeObjectId = t[r[2]];
            }
            return id;
        }

        /// Trang Cài đặt (TKPCCC.UI): layer của bản vẽ đang mở { tên, màu ACI, kiểu nét } để thêm vào bảng layer dây.
        public static List<string[]> DsLopBanVe()
        {
            List<string[]> kq = new List<string[]>();
            Document doc = AcApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return kq;
            using (doc.LockDocument())
            using (Transaction tr = doc.Database.TransactionManager.StartTransaction())
            {
                foreach (ObjectId id in (LayerTable)tr.GetObject(doc.Database.LayerTableId, OpenMode.ForRead))
                {
                    LayerTableRecord l = (LayerTableRecord)tr.GetObject(id, OpenMode.ForRead);
                    if (l.IsDependent) continue; // layer của XREF
                    string net = ((LinetypeTableRecord)tr.GetObject(l.LinetypeObjectId, OpenMode.ForRead)).Name;
                    kq.Add(new[] { l.Name, l.Color.IsByAci ? l.Color.ColorIndex.ToString(CultureInfo.InvariantCulture) : "7", net });
                }
                tr.Commit();
            }
            return kq.OrderBy(x => x[0]).ToList();
        }

        /// Trang Cài đặt: tên các kiểu kích thước (DimStyle) của bản vẽ đang mở.
        /// Khung TRẠNG THÁI của palette: số polyline kín trong model space trên PCCC_VUNG và PCCC_LOAITRU { vùng, loại trừ }. Không có bản vẽ: { 0, 0 }.
        public static int[] DemVung()
        {
            int[] kq = new int[2];
            Document doc = AcApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return kq;
            using (doc.LockDocument())
            using (Transaction tr = doc.Database.TransactionManager.StartTransaction())
            {
                BlockTableRecord ms = (BlockTableRecord)tr.GetObject(SymbolUtilityServices.GetBlockModelSpaceId(doc.Database), OpenMode.ForRead);
                foreach (ObjectId id in ms)
                {
                    if (id.ObjectClass.DxfName != "LWPOLYLINE") continue;
                    Polyline pl = (Polyline)tr.GetObject(id, OpenMode.ForRead);
                    if (!KinPl(pl)) continue;
                    if (string.Equals(pl.Layer, LAYER_VUNG, StringComparison.OrdinalIgnoreCase)) kq[0]++;
                    else if (string.Equals(pl.Layer, LAYER_LOAI_TRU, StringComparison.OrdinalIgnoreCase)) kq[1]++;
                }
                tr.Commit();
            }
            return kq;
        }

        public static List<string> DsKieuDim()
        {
            List<string> kq = new List<string>();
            Document doc = AcApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return kq;
            using (doc.LockDocument())
            using (Transaction tr = doc.Database.TransactionManager.StartTransaction())
            {
                foreach (ObjectId id in (DimStyleTable)tr.GetObject(doc.Database.DimStyleTableId, OpenMode.ForRead))
                {
                    DimStyleTableRecord s = (DimStyleTableRecord)tr.GetObject(id, OpenMode.ForRead);
                    if (!s.IsDependent) kq.Add(s.Name);
                }
                tr.Commit();
            }
            return kq.OrderBy(x => x).ToList();
        }

        /// Vai trò dây của thiết bị (khoá CaiDat.VAI_TRO).
        static string VaiTro(TbBc t, bool heDiaChi)
        {
            if (t.Loai == "dau_bao") return heDiaChi && t.DiaChi ? "dau_bao_dc" : "dau_bao_thuong";
            if (t.Loai == "nut_nhan") return heDiaChi && t.DiaChi ? "nut_nhan_dc" : "nut_nhan_thuong";
            if (t.Loai == "chuong") return "chuong";
            return t.Loai; // zm, cm, r2ml, ngoai_vi
        }

        /// Lưới ô vuông trên hợp các vùng PCCC_VUNG (lỗi 5 KOCHAM 16/11: dây từ sảnh lên dải văn phòng cắt ngang lõi thang máy).
        /// Ô đi được: trong một vùng, hoặc cách hai vùng ≤ Noi (qua tường giữa hai vùng kề nhau), hoặc cách một điểm dừng ≤ 1 m (tủ trong hộp kỹ thuật).
        class LuoiDay
        {
            public double X0, Y0, O, Noi;
            public int Nx, Ny;
            public bool[] Di, Sat; // Sat: ô cách mép vùng ≤ 0,4 m (dây đi vòng tránh bám nét tường)
            public List<List<Point2d>> Vung;
            public List<Point2d> Dung;
            public int Ne; // số đoạn đã đi vòng
        }

        // shortcut: lưới ô 0,25 m tối đa 600 nghìn ô (mặt bằng ~190 x 190 m); lớn hơn thì không né, vẽ gãy góc như cũ.
        static LuoiDay TaoLuoiDay(List<List<Point2d>> vung, List<Point2d> dung, double m)
        {
            // Chỉ vùng gần các điểm dừng (≤ 10 m): bản vẽ nhiều tầng không dựng lưới cả bản vẽ.
            double cach = 10 / m, ax = dung.Min(p => p.X) - cach, ay = dung.Min(p => p.Y) - cach, bx = dung.Max(p => p.X) + cach, by = dung.Max(p => p.Y) + cach;
            vung = vung.Where(z => z.Max(p => p.X) >= ax && z.Min(p => p.X) <= bx && z.Max(p => p.Y) >= ay && z.Min(p => p.Y) <= by).ToList();
            if (vung.Count == 0) return null;
            LuoiDay g = new LuoiDay { O = 0.25 / m, Noi = 0.5 / m, Vung = vung, Dung = dung };
            IEnumerable<Point2d> tat = vung.SelectMany(z => z).Concat(dung);
            g.X0 = tat.Min(p => p.X) - 1 / m; g.Y0 = tat.Min(p => p.Y) - 1 / m;
            g.Nx = (int)((tat.Max(p => p.X) + 1 / m - g.X0) / g.O) + 1; g.Ny = (int)((tat.Max(p => p.Y) + 1 / m - g.Y0) / g.O) + 1;
            if ((long)g.Nx * g.Ny > 600000) return null;
            g.Di = new bool[g.Nx * g.Ny];
            g.Sat = new bool[g.Nx * g.Ny];
            for (int j = 0; j < g.Ny; j++)
                for (int i = 0; i < g.Nx; i++)
                {
                    Point2d p = new Point2d(g.X0 + i * g.O, g.Y0 + j * g.O);
                    g.Di[j * g.Nx + i] = DiDuoc(g, p);
                    g.Sat[j * g.Nx + i] = g.Di[j * g.Nx + i] && g.Vung.Any(z => CachVien(z, p) <= 0.4 / m);
                }
            return g;
        }

        static bool DiDuoc(LuoiDay g, Point2d p)
        {
            // Qua tường chỉ ở khe giữa HAI vùng (sát mép một vùng thôi là trong tường lõi thang, không đi).
            int gan = 0;
            foreach (List<Point2d> z in g.Vung)
            {
                if (TrongDaGiac(z, p)) return true;
                if (CachVien(z, p) <= g.Noi && ++gan >= 2) return true;
            }
            foreach (Point2d q in g.Dung) if (q.GetDistanceTo(p) <= 4 * g.O) return true;
            return false;
        }

        static bool DoanDiDuoc(LuoiDay g, List<Point2d> d)
        {
            for (int k = 0; k + 1 < d.Count; k++)
            {
                int so = (int)Math.Ceiling(d[k].GetDistanceTo(d[k + 1]) / (g.O / 2));
                for (int i = 0; i <= so; i++) if (!DiDuoc(g, d[k] + (d[k + 1] - d[k]) * ((double)i / Math.Max(so, 1)))) return false;
            }
            return true;
        }

        /// Đoạn gãy góc a -> b ra ngoài vùng: thử góc gãy kia, rồi đường vuông góc ngắn nhất trên lưới (Dijkstra, mỗi lần rẽ phạt 8 ô = 2 m;
        /// ô sát mép vùng ≤ 0,4 m phạt thêm 4 lần: người dùng 16/11, dây vòng qua lõi thang chạy trùng nét tường, nhìn không thấy).
        /// Không tìm được thì giữ đoạn cũ.
        static List<Point2d> NeLoi(LuoiDay g, List<Point2d> d)
        {
            if (g == null || DoanDiDuoc(g, d)) return d;
            Point2d a = d[0], b = d[d.Count - 1];
            foreach (Point2d c in new[] { new Point2d(a.X, b.Y), new Point2d(b.X, a.Y) })
            {
                List<Point2d> t = new List<Point2d> { a, c, b };
                if (DoanDiDuoc(g, t)) { g.Ne++; return t; }
            }
            Func<Point2d, int> o = p => Math.Min(g.Ny - 1, Math.Max(0, (int)Math.Round((p.Y - g.Y0) / g.O))) * g.Nx
                + Math.Min(g.Nx - 1, Math.Max(0, (int)Math.Round((p.X - g.X0) / g.O)));
            int n = g.Nx * g.Ny, s0 = o(a), s1 = o(b);
            int[] dx = { 1, 0, -1, 0 }, dy = { 0, 1, 0, -1 };
            int[] kc = Enumerable.Repeat(int.MaxValue, 4 * n).ToArray(), truoc = new int[4 * n];
            SortedSet<long> hang = new SortedSet<long>();
            for (int h = 0; h < 4; h++) { kc[4 * s0 + h] = 0; truoc[4 * s0 + h] = -1; hang.Add(4 * s0 + h); }
            int cuoi = -1;
            while (hang.Count > 0)
            {
                long k = hang.Min;
                hang.Remove(k);
                int st = (int)(k % (4 * n)), c = (int)(k / (4 * n));
                if (c > kc[st]) continue;
                int o0 = st / 4, h0 = st % 4;
                if (o0 == s1) { cuoi = st; break; }
                int x = o0 % g.Nx, y = o0 / g.Nx;
                for (int h = 0; h < 4; h++)
                {
                    if (h == (h0 + 2) % 4) continue;
                    int x2 = x + dx[h], y2 = y + dy[h];
                    if (x2 < 0 || y2 < 0 || x2 >= g.Nx || y2 >= g.Ny || !g.Di[y2 * g.Nx + x2]) continue;
                    int st2 = 4 * (y2 * g.Nx + x2) + h, c2 = c + 1 + (h == h0 || truoc[st] == -1 ? 0 : 8) + (g.Sat[y2 * g.Nx + x2] ? 4 : 0);
                    if (c2 >= kc[st2]) continue;
                    kc[st2] = c2; truoc[st2] = st;
                    hang.Add((long)c2 * 4 * n + st2);
                }
            }
            if (cuoi < 0) return d;
            List<Point2d> o2 = new List<Point2d>();
            for (int st = cuoi; st >= 0; st = truoc[st])
            {
                Point2d p = new Point2d(g.X0 + (st / 4) % g.Nx * g.O, g.Y0 + (st / 4) / g.Nx * g.O);
                if (o2.Count >= 2 && (Math.Abs(o2[o2.Count - 2].X - p.X) < 1e-9 && Math.Abs(o2[o2.Count - 1].X - p.X) < 1e-9
                    || Math.Abs(o2[o2.Count - 2].Y - p.Y) < 1e-9 && Math.Abs(o2[o2.Count - 1].Y - p.Y) < 1e-9)) o2[o2.Count - 1] = p;
                else o2.Add(p);
            }
            o2.Reverse();
            if (o2.Count < 3) return d; // cùng hàng / cột ô: đoạn gãy góc thường đã là tốt nhất
            // Ô đầu, ô cuối về đúng a, b: dời đoạn đầu (cuối) cho thẳng hàng với a (b).
            Func<Point2d, Point2d, Point2d, Point2d> doi = (moc, q, ke) => Math.Abs(q.Y - ke.Y) < 1e-9 ? new Point2d(ke.X, moc.Y) : new Point2d(moc.X, ke.Y);
            o2[1] = doi(a, o2[0], o2[1]);
            o2[o2.Count - 2] = doi(b, o2[o2.Count - 1], o2[o2.Count - 2]);
            o2[0] = a; o2[o2.Count - 1] = b;
            g.Ne++;
            return o2;
        }

        /// Vẽ một chuỗi dây qua các điểm dừng n (đã có cả đầu, cuối). lop(i) = layer của đoạn i (n[i] -> n[i+1]). Đoạn liền nhau cùng layer gộp một polyline.
        /// dtcd != null: kéo dây thêm 0,5 m ở mút cuối, đặt điện trở cuối dây. Trả chiều dài (đơn vị bản vẽ), đếm đoạn xéo vào xeo.
        /// luoi != null: đoạn ra ngoài các vùng PCCC_VUNG thì đi vòng trong vùng (NeLoi).
        static double VeChuoiDay(Database db, Transaction tr, BlockTableRecord ms, List<Point2d> n, Func<int, string> lop, VatTu dtcd, double eps, ref int xeo, LuoiDay luoi = null)
        {
            if (n.Count < 2) return 0;
            List<List<Point2d>> doan = DoanVuong(n, eps).Select(d => NeLoi(luoi, d)).ToList();
            double m = HeSoMet(db), dai = 0;
            if (dtcd != null)
            {
                List<Point2d> c = doan[doan.Count - 1];
                Vector2d h = c[c.Count - 1] - c[c.Count - 2];
                Point2d mut = c[c.Count - 1] + (h.Length > 1e-9 ? h.GetNormal() : Vector2d.XAxis) * (0.5 / m);
                c.Add(mut);
                ChenThietBi(db, tr, dtcd, new Point3d(mut.X, mut.Y, 0), 0.001 / m, 0);
            }
            int i0 = 0;
            while (i0 < doan.Count)
            {
                string l = lop(i0);
                List<Point3d> ps = new List<Point3d>();
                int i = i0;
                for (; i < doan.Count && lop(i) == l; i++)
                    foreach (Point2d q in doan[i])
                        if (ps.Count == 0 || new Point2d(ps[ps.Count - 1].X, ps[ps.Count - 1].Y).GetDistanceTo(q) > 1e-9) ps.Add(new Point3d(q.X, q.Y, 0));
                if (ps.Count >= 2)
                {
                    for (int k = 0; k + 1 < ps.Count; k++)
                        if (Math.Abs(ps[k].X - ps[k + 1].X) > eps && Math.Abs(ps[k].Y - ps[k + 1].Y) > eps) xeo++;
                    Polyline pl = TaoPline(ps, false, DamBaoLopDay(db, tr, l));
                    ms.AppendEntity(pl);
                    tr.AddNewlyCreatedDBObject(pl, true);
                    dai += pl.Length;
                }
                i0 = i;
            }
            return dai;
        }

        /// Gán mỗi thiết bị cho module gần nhất (Manhattan).
        static Dictionary<TbBc, List<TbBc>> GanModule(List<TbBc> ds, List<TbBc> mod)
        {
            Dictionary<TbBc, List<TbBc>> kq = mod.ToDictionary(x => x, x => new List<TbBc>());
            foreach (TbBc t in ds) kq[mod.OrderBy(x => Mh(x.P, t.P)).First()].Add(t);
            return kq;
        }

        static void DayBcTay()
        {
            Document doc = AcApp.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;
            KiemDonVi(doc);
            FileDanhMuc dm = DocDanhMuc();
            PromptKeywordOptions ko = new PromptKeywordOptions("\nHệ báo cháy");
            ko.Keywords.Add("Thuong"); ko.Keywords.Add("DiaChi");
            ko.Keywords.Default = CaiDat.Doc("he_bc", "Thuong") == "DiaChi" ? "DiaChi" : "Thuong";
            ko.AllowNone = true;
            PromptResult kr = ed.GetKeywords(ko);
            if (kr.Status == PromptStatus.Cancel) return;
            bool diaChi = (kr.Status == PromptStatus.OK && kr.StringResult != "" ? kr.StringResult : ko.Keywords.Default) == "DiaChi";
            CaiDat.Luu("he_bc", diaChi ? "DiaChi" : "Thuong");
            PromptSelectionOptions so = new PromptSelectionOptions();
            so.MessageForAdding = "\nQuét chọn thiết bị báo cháy của một zone (đầu báo, nút nhấn, chuông" + (diaChi ? ", module ZM, CM, R2ML" : "") + "; có tủ TTBC thì chọn luôn): ";
            PromptSelectionResult sr = ed.GetSelection(so, new SelectionFilter(new[] { new TypedValue((int)DxfCode.Start, "INSERT") }));
            if (sr.Status != PromptStatus.OK) return;

            List<TbBc> ds = new List<TbBc>();
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BoiCanh bc = new BoiCanh { Tr = tr, Dm = dm, Alias = DocAlias() };
                foreach (ObjectId id in sr.Value.GetObjectIds())
                {
                    BlockReference br = tr.GetObject(id, OpenMode.ForRead) as BlockReference;
                    if (br == null || br is Table) continue;
                    TbBc t = PhanLoaiBc(br, bc);
                    if (t != null) ds.Add(t);
                }
                tr.Commit();
            }
            TbBc tu = ds.FirstOrDefault(t => t.Loai == "tu");
            ds.RemoveAll(t => t.Loai == "tu");
            if (ds.Count == 0) throw new System.Exception("vùng chọn không có thiết bị báo cháy nào (đầu báo, nút nhấn, chuông, module)");
            Point2d T;
            if (tu != null) T = tu.P;
            else
            {
                PromptPointResult pr = ed.GetPoint("\nBấm vị trí tủ TTBC (hoặc điểm dây đi về tủ): ");
                if (pr.Status != PromptStatus.OK) return;
                Point3d q = pr.Value.TransformBy(ed.CurrentUserCoordinateSystem);
                T = new Point2d(q.X, q.Y);
            }

            double m = HeSoMet(db), eps = 0.3 / m;
            VatTu dtcd = TimMa(dm, "BC_DTCD");
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                bool co = ((BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead)).Has("BC_DTCD");
                tr.Commit();
                if (!co) NapVaoBanVe(doc, dm);
            }

            LuoiDay luoi = TaoLuoiDay(VungCoSan(db), ds.Select(t => t.P).Concat(new[] { T }).ToList(), m);
            List<TbBc> tinHieu = ds.Where(t => t.Loai == "dau_bao" || t.Loai == "nut_nhan").ToList();
            List<TbBc> chuong = ds.Where(t => t.Loai == "chuong" || t.CoChuong).ToList();
            List<string> bao = new List<string>();
            int xeo = 0, soDtcd = 0;
            double mTinHieu = 0, mVong = 0, mChuong = 0;
            // Thiết bị tại một điểm dừng (để chọn layer đoạn); điểm trùng thì lấy thiết bị đầu.
            Func<List<TbBc>, Point2d, TbBc> tai = (nhom, q) => nhom.FirstOrDefault(t => t.P == q);
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTableRecord ms = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);
                // Mạch hở tín hiệu từ "goc" qua nhóm, gắn điện trở cuối dây. xaNhat: thiết bị cuối là thiết bị xa "goc" nhất (hệ thường, người dùng 16/11:
                // điện trở cuối dây đặt ở điểm xa tủ nhất cho tiết kiệm dây); nhóm về ZM thì cuối tự do (đường ngắn nhất).
                Action<Point2d, List<TbBc>, bool> machHo = (goc, nhom, xaNhat) =>
                {
                    if (nhom.Count == 0) return;
                    TbBc xa = xaNhat ? nhom.OrderByDescending(t => Mh(goc, t.P)).First() : null;
                    List<Point2d> n = ChuoiDung(goc, xa != null ? xa.P : (Point2d?)null,
                        nhom.Where(t => t.Loai == "dau_bao" && t != xa).Select(t => t.P).ToList(),
                        nhom.Where(t => t.Loai != "dau_bao" && t != xa).Select(t => t.P).ToList(), eps);
                    mTinHieu += VeChuoiDay(db, tr, ms, n, i => CaiDat.DayVaiTro(VaiTro(tai(nhom, n[i + 1]), false)), dtcd, eps, ref xeo, luoi);
                    soDtcd++;
                };
                // Dây chuông mạch cụt kiểu xương cá: tủ -> (module) rồi cây nhánh vuông góc tới các chuông.
                Action<Point2d?, List<TbBc>> dayChuong = (mod, nhom) =>
                {
                    if (nhom.Count == 0) return;
                    string l = CaiDat.DayVaiTro("chuong");
                    if (mod.HasValue) mChuong += VeChuoiDay(db, tr, ms, new List<Point2d> { T, mod.Value }, i => l, null, eps, ref xeo, luoi);
                    foreach (List<Point2d> nhanh in CayXuongCa(mod.HasValue ? mod.Value : T, nhom.Select(t => t.P).ToList(), eps))
                        mChuong += VeChuoiDay(db, tr, ms, nhanh, i => l, null, eps, ref xeo, luoi);
                };

                if (!diaChi)
                {
                    List<TbBc> bo = ds.Where(t => t.Loai != "dau_bao" && t.Loai != "nut_nhan" && t.Loai != "chuong").ToList();
                    if (bo.Count > 0) bao.Add("bo_module=" + bo.Count + " (hệ thường không nối module)");
                    machHo(T, tinHieu, true);
                    dayChuong(null, chuong);
                }
                else
                {
                    List<TbBc> vong = ds.Where(t => (t.Loai == "dau_bao" || t.Loai == "nut_nhan") && t.DiaChi
                        || t.Loai == "zm" || t.Loai == "cm" || t.Loai == "r2ml" || t.Loai == "ngoai_vi").ToList();
                    List<TbBc> thuong = tinHieu.Where(t => !t.DiaChi).ToList();
                    List<TbBc> zm = ds.Where(t => t.Loai == "zm").ToList(), cm = ds.Where(t => t.Loai == "cm").ToList(), r2 = ds.Where(t => t.Loai == "r2ml").ToList();
                    List<TbBc> chThuong = chuong.Where(t => !t.DiaChi).ToList(), chDc = chuong.Where(t => t.DiaChi).ToList();
                    if (thuong.Count > 0 && zm.Count == 0)
                        throw new System.Exception(thuong.Count + " đầu báo / nút nhấn thường cần module ZM: đặt ZM (mã BC_ZM) rồi chọn lại, hoặc dùng thiết bị địa chỉ (BC_KHOI_DC, BC_NUTAN_DC)");
                    if (chThuong.Count > 0 && cm.Count == 0)
                        throw new System.Exception(chThuong.Count + " chuông thường cần module CM: đặt CM (mã BC_CM) rồi chọn lại");
                    if (chDc.Count > 0 && r2.Count == 0)
                        throw new System.Exception(chDc.Count + " chuông địa chỉ cần module nguồn R2ML: đặt R2ML (mã BC_R2ML) rồi chọn lại");
                    if (vong.Count == 0) throw new System.Exception("hệ địa chỉ không có thiết bị nào trên mạch vòng (đầu báo, nút nhấn địa chỉ, module)");
                    // Mạch vòng: tủ -> thiết bị địa chỉ + module -> về tủ; đoạn về tủ theo layer thiết bị cuối.
                    List<Point2d> n = ChuoiDung(T, T, vong.Where(t => t.Loai == "dau_bao").Select(t => t.P).ToList(),
                        vong.Where(t => t.Loai != "dau_bao").Select(t => t.P).ToList(), eps);
                    mVong = VeChuoiDay(db, tr, ms, n, i => CaiDat.DayVaiTro(VaiTro(tai(vong, i + 1 < n.Count - 1 ? n[i + 1] : n[i]) ?? vong[0], true)), null, eps, ref xeo, luoi);
                    foreach (KeyValuePair<TbBc, List<TbBc>> g in GanModule(thuong, zm)) machHo(g.Key.P, g.Value, false);
                    foreach (KeyValuePair<TbBc, List<TbBc>> g in GanModule(chThuong, cm)) dayChuong(g.Key.P, g.Value);
                    foreach (KeyValuePair<TbBc, List<TbBc>> g in GanModule(chDc, r2)) dayChuong(g.Key.P, g.Value);
                    bao.Add("vong=" + vong.Count + " m_vong=" + Math.Round(mVong * m, 2).ToString(CultureInfo.InvariantCulture));
                }
                tr.Commit();
            }
            ed.WriteMessage("\n[TKPCCC] PCCC_DAYBC he=" + (diaChi ? "dia_chi" : "thuong") + " tin_hieu=" + tinHieu.Count
                + " m=" + Math.Round(mTinHieu * m, 2).ToString(CultureInfo.InvariantCulture) + " dtcd=" + soDtcd
                + " chuong=" + chuong.Count + " m_chuong=" + Math.Round(mChuong * m, 2).ToString(CultureInfo.InvariantCulture)
                + (bao.Count > 0 ? " " + string.Join(" ", bao) : "") + " xeo=" + xeo + (luoi != null ? " ne_loi=" + luoi.Ne : "") + (tu != null ? " (tủ: block BC_TT trong vùng chọn)" : ""));
        }
    }
}
