// Thao tác spec "so_do": sơ đồ nguyên lý theo tầng, hệ sprinkler ("spr") và báo cháy hệ thường ("bc").
// Thiết kế, nguồn từng chi tiết (lấy từ bản mẫu hay tự đặt): cad/knowledge/so-do-nguyen-ly.md. Từ vựng: cad/skills/TU_VUNG.md.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

namespace TKPCCC
{
    public partial class Lenh
    {
        // Mọi thứ của sơ đồ (ống, dây, chữ, CẢ block thiết bị) nằm trên layer bắt đầu bằng PCCC_SODO: Duyet (dem, PCCC_BOC) bỏ qua cả layer,
        // nên thiết bị vẽ lại trên sơ đồ không cộng trùng với thiết bị đặt trên mặt bằng.
        const string LAYER_SO_DO = "PCCC_SODO";
        const string LAYER_SO_DO_BC = "PCCC_SODO_BC";
        const string LAYER_SO_DO_TANG = "PCCC_SODO_TANG";

        class TangSoDo { public string Ten; public double Cot; public Dictionary<string, object> T; public List<string> Dem = new List<string>(); }

        /// 0 -> "±0.000", 4200 -> "+4.200", -3000 -> "-3.000" (cao độ mm ghi theo m, như kỹ sư ghi).
        static string ChuCaoDo(double mm)
        {
            double m = Math.Round(mm / 1000, 3);
            if (m == 0) return "±0.000";
            return (m > 0 ? "+" : "-") + Math.Abs(m).ToString("0.000", CultureInfo.InvariantCulture);
        }

        static void OpSoDo(GoiSpec g, Dictionary<string, object> o)
        {
            string he = LayChu(o, "he", "spr").ToLowerInvariant();
            if (he != "spr" && he != "bc") throw new System.Exception("\"he\" chỉ có \"spr\" (sprinkler) hoặc \"bc\" (báo cháy hệ thường)");

            // --- Đọc và kiểm hết đầu vào TRƯỚC khi vẽ: thao tác lỗi không để lại hình dở trong gói ---
            List<TangSoDo> tang = new List<TangSoDo>();
            // bc không vẽ tầng: cho truyền thẳng "zone" (cot không bắt buộc), "tang" giữ để tương thích spec cũ.
            if (he == "bc" && Lay(o, "tang") == null && Lay(o, "zone") != null)
                tang.Add(new TangSoDo { Ten = "", T = new Dictionary<string, object> { { "zone", Lay(o, "zone") } } });
            else foreach (object x in DS(Lay(o, "tang"), "tang"))
            {
                Dictionary<string, object> t = x as Dictionary<string, object>;
                if (t == null) throw new System.Exception("mỗi phần tử \"tang\" phải là {\"ten\",\"cot\",\"dem\"}");
                if (he == "spr" && Lay(t, "cot") == null) throw new System.Exception("tầng thứ " + (tang.Count + 1) + " thiếu \"cot\" (cao độ sàn, mm)");
                tang.Add(new TangSoDo { Ten = LayChu(t, "ten", "TẦNG " + (tang.Count + 1)), Cot = LaySo(t, "cot", 0), T = t });
            }
            if (tang.Count == 0) throw new System.Exception("\"tang\" rỗng");
            tang = tang.OrderBy(t => t.Cot).ToList();

            double k = 0.001 / HeSoMet(g.Db); // 1 mm theo đơn vị bản vẽ (cùng tỉ lệ chèn block)
            double h = LaySo(o, "h", 3000 * k), dai = LaySo(o, "dai", 8000 * k), cao = LaySo(o, "cao", k * LaySo(NhomKyHieu("chu"), "cao", 250));
            if (h <= 0 || dai <= 0 || cao <= 0) throw new System.Exception("\"h\", \"dai\", \"cao\" phải > 0");
            Point3d p0 = Lay(o, "p") == null ? new Point3d(g.Gx, g.Gy, 0) : P(g, Lay(o, "p"), "p");
            if (he == "bc") { SoDoBaoChay(g, o, tang, k, h, cao, p0); return; }

            foreach (TangSoDo s in tang)
            {
                Dictionary<string, object> dem = Lay(s.T, "dem") as Dictionary<string, object>;
                if (dem != null)
                    foreach (KeyValuePair<string, object> kv in dem)
                    {
                        VatTu v = TimMa(g.Dm, kv.Key);
                        if (v == null) throw new System.Exception(s.Ten + ": mã không có trong danh mục: " + kv.Key);
                        if (v.he != "SPR") continue; // kết quả "dem" đếm cả bản vẽ: bỏ mã hệ khác, ống, dây
                        double sl = D(kv.Value);
                        if (sl <= 0) throw new System.Exception(s.Ten + ": " + kv.Key + " phải > 0");
                        s.Dem.Add(v.ten + ": " + So(sl) + " " + v.dvt);
                    }
                if ((s.Dem.Count + 1) * 1.67 * cao > h)
                    throw new System.Exception(s.Ten + ": " + (s.Dem.Count + 1) + " dòng chữ đếm cao hơn \"h\": tăng \"h\" hoặc giảm \"cao\"");
            }
            if (!tang.Any(t => t.Dem.Count > 0)) throw new System.Exception("không tầng nào có \"dem\": sơ đồ không có nhánh tầng nào");

            List<string> truc = new List<string>();
            object ot = Lay(o, "truc");
            if (ot == null) truc.Add("ONG_DN100");
            else foreach (object x in DS(ot, "truc")) truc.Add(Convert.ToString(x, CultureInfo.InvariantCulture).ToUpperInvariant());
            if (truc.Count == 0) throw new System.Exception("\"truc\" rỗng");
            List<VatTu> van = new List<VatTu>();
            foreach (string ma in truc)
            {
                VatTu ong = TimMa(g.Dm, ma);
                if (ong == null || ong.loai != "tuyen" || !ma.StartsWith("ONG_DN")) throw new System.Exception("\"truc\": " + ma + " không phải mã ống ONG_DN...");
                VatTu v = TimMa(g.Dm, "SPR_VAN_OSY_" + ma.Substring(4));
                if (v == null) throw new System.Exception("danh mục không có van OS&Y cho trục " + ma + " (có SPR_VAN_OSY_DN65..DN150)");
                van.Add(v);
            }
            string xa = LayChu(o, "xa", "ONG_DN50").ToUpperInvariant();
            if (xa != "" && TimMa(g.Dm, xa) == null) throw new System.Exception("\"xa\": mã ống không có trong danh mục: " + xa);
            VatTu fs = TimMa(g.Dm, "SPR_CTDC"), aav = TimMa(g.Dm, "SPR_VAN_XK");
            if (fs == null || aav == null) throw new System.Exception("danh mục thiếu SPR_CTDC hoặc SPR_VAN_XK");
            BlockTable bt = (BlockTable)g.Tr.GetObject(g.Db.BlockTableId, OpenMode.ForRead);
            foreach (VatTu v in van.Concat(new VatTu[] { fs, aav }))
                if (!bt.Has(v.ma)) throw new System.Exception("Thư viện không có block " + v.ma + " (chạy PCCC_LIB)");

            // --- Bố trí (chiều đứng KHÔNG theo cao độ thật: mỗi tầng cách nhau "h", chữ ghi cao độ thật) ---
            int n = truc.Count;
            double x0 = p0.X, y0 = p0.Y, dx = 600 * k, gap = 900 * k, r = 100 * k;
            double xd = x0 - dx;                                   // trục xả bên trái các trục cấp (như chi tiết trên tờ CC-07)
            double xv = x0 + (n - 1) * dx + 700 * k, xfs = xv + 700 * k, xtap = xfs + 700 * k;
            double xEnd = Math.Max(x0 + dai, xtap + 1500 * k), xL = xd - 3000 * k;
            // shortcut: nhiều trục (gap × số trục > 0,6 h) thì nhánh dưới cùng chạm vạch sàn; gặp thì tăng "h".
            Func<int, int, double> yNhanh = (i, j) => y0 + i * h + 0.6 * h - j * gap;

            ObjectId lOng = DamBaoLayer(g.Db, g.Tr, LAYER_SO_DO, MauHe("SPR")), lTang = DamBaoLayer(g.Db, g.Tr, LAYER_SO_DO_TANG, 8);
            double yBot = y0 - 0.5 * h, yTop = double.MinValue, yXaTop = double.MinValue;
            VeVachTang(g, tang, y0, h, xL, xEnd, cao, lTang);

            for (int i = 0; i < tang.Count; i++)
            {
                if (tang[i].Dem.Count == 0) continue;

                // Mỗi trục cấp một nhánh: van OS&Y + công tắc dòng chảy, mũi tên "đi tới sprinkler tầng". Nhánh vượt trục khác bằng nét cầu.
                for (int j = 0; j < n; j++)
                {
                    double yb = yNhanh(i, j), xj = x0 + j * dx;
                    Them(g, OngCau(new Point3d(xj, yb, 0), new Point3d(xEnd - 400 * k, yb, 0), Enumerable.Range(j + 1, n - j - 1).Select(m => x0 + m * dx), r, lOng));
                    Polyline mui = TaoPline(new List<Point3d> { new Point3d(xEnd - 400 * k, yb, 0), new Point3d(xEnd, yb, 0) }, false, lOng);
                    mui.SetStartWidthAt(0, 150 * k); mui.SetEndWidthAt(0, 0);
                    Them(g, mui);
                    DatSoDo(g, van[j], new Point3d(xv, yb, 0), k, lOng);
                    DatSoDo(g, fs, new Point3d(xfs, yb, 0), k, lOng);
                    Them(g, TaoChu(g, new Point3d((xtap + xEnd) / 2, yb + 0.8 * cao, 0), truc[j].Substring(4), 0.8 * cao, 0, true, lOng));
                    yTop = Math.Max(yTop, yb + 800 * k);
                }
                // Ống xả tầng: từ sau công tắc dòng chảy của nhánh dưới cùng, xuống rồi về trục xả, có van xả (vẽ nét, chưa có mã vật tư).
                if (xa != "")
                {
                    double yb = yNhanh(i, n - 1), yx = yb - 400 * k;
                    Polyline ox = OngCau(new Point3d(xtap, yx, 0), new Point3d(xd, yx, 0), Enumerable.Range(0, n).Select(m => x0 + m * dx).Reverse(), r, lOng);
                    ox.AddVertexAt(0, new Point2d(xtap, yb), 0, 0, 0);
                    Them(g, ox);
                    double a = 150 * k, b = 100 * k; // nơ van (hai tam giác đối đỉnh)
                    Them(g, TaoPline(new List<Point3d> { new Point3d(xv - a, yx + b, 0), new Point3d(xv - a, yx - b, 0), new Point3d(xv + a, yx + b, 0), new Point3d(xv + a, yx - b, 0) }, true, lOng));
                    yXaTop = Math.Max(yXaTop, yx);
                }
                // Chữ số đếm của tầng: sau mũi tên, giữa các nhánh.
                MText m2 = TaoChu(g, new Point3d(xEnd + cao, (yNhanh(i, 0) + yNhanh(i, n - 1)) / 2, 0),
                    "SPRINKLER " + tang[i].Ten + "\n" + string.Join("\n", tang[i].Dem), cao, 0, false, lOng);
                m2.Attachment = AttachmentPoint.MiddleLeft;
                Them(g, m2);
            }

            // Trục cấp: từ dưới (trạm bơm) lên tới nhánh tầng trên cùng, đầu trục có van xả khí. Trục xả bên trái.
            for (int j = 0; j < n; j++)
            {
                double xj = x0 + j * dx;
                Them(g, TaoPline(new List<Point3d> { new Point3d(xj, yBot, 0), new Point3d(xj, yTop, 0) }, false, lOng));
                DatSoDo(g, aav, new Point3d(xj, yTop, 0), k, lOng);
                Them(g, TaoChu(g, new Point3d(xj - 0.7 * cao, y0 - 0.25 * h, 0), truc[j].Substring(4), 0.8 * cao, 90, true, lOng));
            }
            Them(g, TaoChu(g, new Point3d(x0 + (n - 1) * dx / 2, yBot - cao, 0), "TỪ TRẠM BƠM", cao, 0, true, lOng));
            if (xa != "")
            {
                Them(g, TaoPline(new List<Point3d> { new Point3d(xd, yBot, 0), new Point3d(xd, yXaTop, 0) }, false, lOng));
                Them(g, TaoChu(g, new Point3d(xd - 0.7 * cao, y0 - 0.25 * h, 0), xa.StartsWith("ONG_") ? xa.Substring(4) : xa, 0.8 * cao, 90, true, lOng));
                Them(g, TaoChu(g, new Point3d(xd, yBot - 2.6 * cao, 0), "ỐNG XẢ", cao, 0, true, lOng));
            }
        }

        /// Mã block thao tác "so_do" sẽ chèn: ThucHienSpec nạp thư viện vào bản vẽ trước khi mở transaction vẽ.
        static List<string> MaSoDo(Dictionary<string, object> o)
        {
            if (LayChu(o, "he", "spr").ToLowerInvariant() == "bc")
            {
                List<string> bc = new List<string> { LayChu(o, "tu", "BC_TT").ToUpperInvariant(), "BC_DTCD" };
                IList ts = Lay(o, "tang") as IList ?? new List<object> { new Dictionary<string, object> { { "zone", Lay(o, "zone") } } };
                if (ts != null)
                    foreach (Dictionary<string, object> t in ts.OfType<Dictionary<string, object>>())
                    {
                        IList zs = Lay(t, "zone") as IList ?? new List<object> { t };
                        foreach (Dictionary<string, object> z in zs.OfType<Dictionary<string, object>>())
                        {
                            Dictionary<string, object> dem = Lay(z, "dem") as Dictionary<string, object>;
                            if (dem != null) bc.AddRange(dem.Keys.Select(m => m.ToUpperInvariant()));
                        }
                    }
                return bc;
            }
            List<string> kq = new List<string> { "SPR_CTDC", "SPR_VAN_XK" };
            IList truc = Lay(o, "truc") as IList;
            if (truc == null) kq.Add("SPR_VAN_OSY_DN100");
            else foreach (object m in truc) kq.Add("SPR_VAN_OSY_" + Convert.ToString(m, CultureInfo.InvariantCulture).ToUpperInvariant().Replace("ONG_", ""));
            return kq;
        }

        /// Block thiết bị của sơ đồ đặt lên layer sơ đồ: dem / PCCC_BOC bỏ qua (Duyet), không đếm trùng với mặt bằng.
        static void DatSoDo(GoiSpec g, VatTu v, Point3d p, double k, ObjectId layer)
        {
            ((Entity)g.Tr.GetObject(ChenThietBi(g.Db, g.Tr, v, p, k, 0), OpenMode.ForWrite)).LayerId = layer;
            g.Ve++;
            MoHop(g, new Extents3d(p, p));
        }

        /// Vạch sàn mỗi tầng (cách đều h) + ký hiệu cao độ (tam giác ngược trên vạch) + chữ cao độ + tên tầng dưới vạch (chỉ "spr").
        static void VeVachTang(GoiSpec g, List<TangSoDo> tang, double y0, double h, double xL, double xEnd, double cao, ObjectId lTang)
        {
            for (int i = 0; i < tang.Count; i++)
            {
                double y = y0 + i * h, tx = xL + cao;
                Them(g, TaoPline(new List<Point3d> { new Point3d(xL, y, 0), new Point3d(xEnd, y, 0) }, false, lTang));
                Them(g, TaoPline(new List<Point3d> { new Point3d(tx, y, 0), new Point3d(tx - 0.6 * cao, y + cao, 0), new Point3d(tx + 0.6 * cao, y + cao, 0) }, true, lTang));
                Them(g, TaoChu(g, new Point3d(tx + cao, y + 0.3 * cao, 0), ChuCaoDo(tang[i].Cot), cao, 0, false, lTang));
                Them(g, TaoChu(g, new Point3d(xL, y - 1.5 * cao, 0), tang[i].Ten, cao, 0, false, lTang));
            }
        }

        class ZoneSoDo { public string Ten, Kv; public double Dt; public List<KeyValuePair<VatTu, double>> Dem = new List<KeyValuePair<VatTu, double>>(); }

        /// Sơ đồ báo cháy hệ thường theo hồ sơ thật (cad/knowledge/mau-so-do-that.md): KHÔNG vạch tầng, cao độ; tủ ở dưới, zone xếp hàng ngang
        /// từ dưới lên (zone 1 dưới cùng); bó dây đứng bên trái rẽ vào từng zone, thiết bị mỗi loại một cột, số lượng ghi DƯỚI ký hiệu,
        /// cuối hàng điện trở cuối dây + chữ khu vực / diện tích bảo vệ. Giới hạn đầu báo một zone "toi_da" truyền từ spec (cad/rule/LUAT.md BC-04).
        static void SoDoBaoChay(GoiSpec g, Dictionary<string, object> o, List<TangSoDo> tang, double k, double h, double cao, Point3d p0)
        {
            if (Lay(o, "toi_da") == null) throw new System.Exception("he \"bc\" cần \"toi_da\" (số đầu báo tối đa một zone, tra cad/rule/LUAT.md BC-04)");
            double toiDa = LaySo(o, "toi_da", 0);
            if (toiDa <= 0) throw new System.Exception("\"toi_da\" phải > 0");
            string maTu = LayChu(o, "tu", "BC_TT").ToUpperInvariant(), maDay = LayChu(o, "day", "DAY_BC").ToUpperInvariant();
            VatTu tu = TimMa(g.Dm, maTu), dtcd = TimMa(g.Dm, "BC_DTCD"), day = TimMa(g.Dm, maDay);
            if (tu == null || tu.loai != "block") throw new System.Exception("\"tu\": không phải mã thiết bị: " + maTu);
            if (day == null || day.loai != "tuyen") throw new System.Exception("\"day\": không phải mã dây: " + maDay);
            if (dtcd == null) throw new System.Exception("danh mục thiếu BC_DTCD (điện trở cuối dây)");

            List<ZoneSoDo> zone = new List<ZoneSoDo>();
            List<VatTu> cot = new List<VatTu>(); // mỗi loại thiết bị một cột, thứ tự như danh mục
            foreach (TangSoDo t in tang)
            {
                string tTen = t.Ten == "" ? "" : " (" + t.Ten + ")";
                object oz = Lay(t.T, "zone");
                IList ds = oz != null ? DS(oz, "zone") : (Lay(t.T, "dem") != null ? new List<object> { new Dictionary<string, object> { { "dem", Lay(t.T, "dem") } } } : new List<object>());
                foreach (object x in ds)
                {
                    Dictionary<string, object> zd = x as Dictionary<string, object>;
                    Dictionary<string, object> dem = zd == null ? null : Lay(zd, "dem") as Dictionary<string, object>;
                    if (dem == null || dem.Count == 0) throw new System.Exception(t.Ten + ": mỗi zone phải là {\"ten\",\"dem\":{mã:số}}");
                    // Khu vực = kv; zone trong tầng có tên mà kv chưa có tên tầng thì ghép "tầng + kv"; không kv thì lấy tên tầng.
                    string kvz = LayChu(zd, "kv", "");
                    if (kvz == "") kvz = t.Ten;
                    else if (t.Ten != "" && !kvz.ToUpperInvariant().Contains(t.Ten.ToUpperInvariant())) kvz = t.Ten + " " + kvz;
                    ZoneSoDo z = new ZoneSoDo { Ten = LayChu(zd, "ten", "ZONE " + (zone.Count + 1)), Kv = kvz, Dt = LaySo(zd, "dt", 0) };
                    double nDau = 0;
                    foreach (KeyValuePair<string, object> kv in dem)
                    {
                        VatTu v = TimMa(g.Dm, kv.Key);
                        if (v == null) throw new System.Exception(z.Ten + ": mã không có trong danh mục: " + kv.Key);
                        if (v.loai != "block") continue; // "dem" chép từ kết quả dem có cả dây/ống (DAY_BC): sơ đồ zone không vẽ
                        double sl = D(kv.Value);
                        if (sl <= 0) continue;
                        if (v.ten.StartsWith("Đầu báo")) nDau += sl;
                        z.Dem.Add(new KeyValuePair<VatTu, double>(v, sl));
                        if (!cot.Contains(v)) cot.Add(v);
                    }
                    if (z.Dem.Count == 0) throw new System.Exception(z.Ten + tTen + ": \"dem\" không có mã thiết bị nào");
                    if (nDau > toiDa) throw new System.Exception(z.Ten + tTen + ": " + So(nDau) + " đầu báo, quá toi_da " + So(toiDa) + ": chia thêm zone");
                    zone.Add(z);
                }
            }
            if (zone.Count == 0) throw new System.Exception("không có \"zone\" hoặc \"dem\": sơ đồ không có zone nào");
            cot = cot.OrderBy(v => g.Dm.vat_tu.IndexOf(v)).ToList();
            BlockTable bt = (BlockTable)g.Tr.GetObject(g.Db.BlockTableId, OpenMode.ForRead);
            foreach (VatTu v in cot.Concat(new VatTu[] { tu, dtcd }))
                if (!bt.Has(v.ma)) throw new System.Exception("Thư viện không có block " + v.ma + " (chạy PCCC_LIB)");
            string duPhong = LayChu(o, "du_phong", "");
            List<string> lienDong = new List<string>();
            if (Lay(o, "lien_dong") != null)
                foreach (object x in DS(Lay(o, "lien_dong"), "lien_dong")) lienDong.Add(Convert.ToString(x, CultureInfo.InvariantCulture));

            // --- Bố trí: tủ (khung) dưới cùng; dây zone z đứng ở x0 + z × dxz (bó dây bên trái) lên hàng z rồi rẽ phải ---
            // Hàng cách nhau "h" (mặc định 1500), tối thiểu đủ nửa ký hiệu trên + số lượng dưới ký hiệu.
            int nz = zone.Count;
            double dy = Math.Max(Lay(o, "h") == null ? 1500 * k : h, 700 * k + 2 * cao);
            double x0 = p0.X, y0 = p0.Y, dxz = 300 * k, dxc = 2000 * k, r = 100 * k;
            double xLast = x0 + (nz - 1) * dxz, xS = xLast + 2000 * k, xR = xS + cot.Count * dxc;
            double xTuL = x0 - 1200 * k, xTuR = xLast + 300 * k, yChan = y0 + 600 * k;
            Func<int, double> yHang = z => yChan + (z + 1) * dy;
            ObjectId lBc = DamBaoLayer(g.Db, g.Tr, LAYER_SO_DO_BC, MauHe("BC"));

            Them(g, TaoPline(new List<Point3d> { new Point3d(xTuL, y0, 0), new Point3d(xTuR, y0, 0), new Point3d(xTuR, yChan, 0), new Point3d(xTuL, yChan, 0) }, true, lBc));
            DatSoDo(g, tu, new Point3d(x0 - 800 * k, y0 + 300 * k, 0), k, lBc);
            MText mTu = TaoChu(g, new Point3d(xTuR + 300 * k, y0 + 300 * k, 0), tu.ten + "\nDây: " + day.ten, 0.7 * cao, 0, false, lBc);
            mTu.Attachment = AttachmentPoint.MiddleLeft;
            Them(g, mTu);

            for (int z = 0; z < nz; z++)
            {
                double xz = x0 + z * dxz, yz = yHang(z);
                Polyline pl = OngCau(new Point3d(xz, yz, 0), new Point3d(xR, yz, 0), Enumerable.Range(z + 1, nz - z - 1).Select(q => x0 + q * dxz), r, lBc);
                pl.AddVertexAt(0, new Point2d(xz, yChan), 0, 0, 0);
                Them(g, pl);
                Them(g, TaoChu(g, new Point3d(xLast + 200 * k, yz + 0.2 * cao, 0), zone[z].Ten, 0.8 * cao, 0, false, lBc));
                foreach (KeyValuePair<VatTu, double> kv in zone[z].Dem)
                {
                    double xc = xS + cot.IndexOf(kv.Key) * dxc;
                    DatSoDo(g, kv.Key, new Point3d(xc, yz, 0), k, lBc);
                    MText ms = TaoChu(g, new Point3d(xc, yz - 300 * k, 0), So(kv.Value), 0.8 * cao, 0, true, lBc);
                    ms.Attachment = AttachmentPoint.TopCenter;
                    Them(g, ms);
                }
                DatSoDo(g, dtcd, new Point3d(xR, yz, 0), k, lBc);
                // Chữ cuối zone 3 dòng (người dùng dặn 13/11): khu vực / DIỆN TÍCH BẢO VỆ: xx m² / tên zone; thiếu kv hoặc dt thì bỏ dòng đó.
                List<string> dong = new List<string>();
                if (zone[z].Kv != "") dong.Add(zone[z].Kv);
                if (zone[z].Dt > 0) dong.Add("DIỆN TÍCH BẢO VỆ: " + zone[z].Dt.ToString("0.##", CultureInfo.InvariantCulture) + " m²"); // dấu chấm như bản gốc trang 11
                dong.Add(zone[z].Ten);
                MText mg = TaoChu(g, new Point3d(xR + 400 * k, yz, 0), string.Join("\n", dong), 0.7 * cao, 0, false, lBc);
                mg.Attachment = AttachmentPoint.MiddleLeft;
                Them(g, mg);
            }
            if (duPhong != "") Them(g, TaoChu(g, new Point3d(xLast + 200 * k, yHang(nz) + 0.2 * cao, 0), duPhong, 0.8 * cao, 0, false, lBc));
            // Tên thiết bị trên đầu mỗi cột (thay bảng ký hiệu).
            double yDau = yHang(duPhong != "" ? nz : nz - 1) + 0.6 * dy;
            for (int c = 0; c <= cot.Count; c++)
            {
                MText mc = TaoChu(g, new Point3d(xS + c * dxc, yDau, 0), (c < cot.Count ? cot[c] : dtcd).ten, 0.7 * cao, 0, true, lBc);
                mc.Attachment = AttachmentPoint.BottomCenter; mc.Width = 0.9 * dxc;
                Them(g, mc);
            }
            // Liên động: dây xuống từ đáy tủ, mỗi mục một mũi tên sang phải + chữ.
            for (int i = 0; i < lienDong.Count; i++)
            {
                double yl = y0 - (i + 1) * 600 * k, xa = x0 - 600 * k, xb = xa + 1500 * k;
                Them(g, TaoPline(new List<Point3d> { new Point3d(xa, y0 - i * 600 * k, 0), new Point3d(xa, yl, 0), new Point3d(xb - 300 * k, yl, 0) }, false, lBc));
                Polyline mui = TaoPline(new List<Point3d> { new Point3d(xb - 300 * k, yl, 0), new Point3d(xb, yl, 0) }, false, lBc);
                mui.SetStartWidthAt(0, 150 * k); mui.SetEndWidthAt(0, 0);
                Them(g, mui);
                MText ml = TaoChu(g, new Point3d(xb + 200 * k, yl, 0), lienDong[i], 0.7 * cao, 0, false, lBc);
                ml.Attachment = AttachmentPoint.MiddleLeft;
                Them(g, ml);
            }
        }

        /// Ống ngang a -> b; tại mỗi x trong "qua" (ống đứng khác cắt ngang) vẽ nửa vòng cầu bán kính r phía trên.
        static Polyline OngCau(Point3d a, Point3d b, IEnumerable<double> qua, double r, ObjectId layer)
        {
            Polyline pl = new Polyline();
            pl.LayerId = layer;
            int i = 0;
            double huong = b.X >= a.X ? 1 : -1;
            pl.AddVertexAt(i++, new Point2d(a.X, a.Y), 0, 0, 0);
            foreach (double x in qua)
            {
                // Đi trái -> phải: bulge âm là cung phía trên; đi phải -> trái thì ngược dấu.
                pl.AddVertexAt(i++, new Point2d(x - huong * r, a.Y), -huong, 0, 0);
                pl.AddVertexAt(i++, new Point2d(x + huong * r, a.Y), 0, 0, 0);
            }
            pl.AddVertexAt(i, new Point2d(b.X, b.Y), 0, 0, 0);
            return pl;
        }
    }
}
