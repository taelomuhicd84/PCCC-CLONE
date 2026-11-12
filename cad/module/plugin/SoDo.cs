// Thao tác spec "so_do": sơ đồ nguyên lý theo tầng (lượt này chỉ hệ sprinkler).
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
        // Ống, chữ của sơ đồ nằm trên layer riêng, KHÔNG phải layer PCCC_<mã ống>: PCCC_BOC không cộng chiều dài sơ đồ vào khối lượng.
        // Van là block thư viện (có MA_VT) nên vẫn được đếm: số van trên sơ đồ là số van thật.
        const string LAYER_SO_DO = "PCCC_SODO";
        const string LAYER_SO_DO_TANG = "PCCC_SODO_TANG";

        class TangSoDo { public string Ten; public double Cot; public List<string> Dem = new List<string>(); }

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
            if (he != "spr") throw new System.Exception("\"he\" hiện chỉ có \"spr\" (sơ đồ báo cháy chưa làm)");

            // --- Đọc và kiểm hết đầu vào TRƯỚC khi vẽ: thao tác lỗi không để lại hình dở trong gói ---
            List<TangSoDo> tang = new List<TangSoDo>();
            foreach (object x in DS(Lay(o, "tang"), "tang"))
            {
                Dictionary<string, object> t = x as Dictionary<string, object>;
                if (t == null) throw new System.Exception("mỗi phần tử \"tang\" phải là {\"ten\",\"cot\",\"dem\"}");
                if (Lay(t, "cot") == null) throw new System.Exception("tầng thứ " + (tang.Count + 1) + " thiếu \"cot\" (cao độ sàn, mm)");
                TangSoDo s = new TangSoDo { Ten = LayChu(t, "ten", "TẦNG " + (tang.Count + 1)), Cot = LaySo(t, "cot", 0) };
                Dictionary<string, object> dem = Lay(t, "dem") as Dictionary<string, object>;
                if (dem != null)
                    foreach (KeyValuePair<string, object> kv in dem)
                    {
                        VatTu v = TimMa(g.Dm, kv.Key);
                        s.Dem.Add((v != null ? v.ten : kv.Key) + ": " + So(D(kv.Value)) + (v != null ? " " + v.dvt : ""));
                    }
                tang.Add(s);
            }
            if (tang.Count == 0) throw new System.Exception("\"tang\" rỗng");
            tang = tang.OrderBy(t => t.Cot).ToList();
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

            double k = 0.001 / HeSoMet(g.Db); // 1 mm theo đơn vị bản vẽ (cùng tỉ lệ chèn block)
            double h = LaySo(o, "h", 3000 * k), dai = LaySo(o, "dai", 8000 * k), cao = LaySo(o, "cao", LaySo(NhomKyHieu("chu"), "cao", 250));
            if (h <= 0 || dai <= 0 || cao <= 0) throw new System.Exception("\"h\", \"dai\", \"cao\" phải > 0");
            Point3d p0 = Lay(o, "p") == null ? new Point3d(g.Gx, g.Gy, 0) : P(g, Lay(o, "p"), "p");

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

            for (int i = 0; i < tang.Count; i++)
            {
                double y = y0 + i * h;
                // Vạch sàn + ký hiệu cao độ (tam giác ngược đặt trên vạch) + tên tầng dưới vạch.
                Them(g, TaoPline(new List<Point3d> { new Point3d(xL, y, 0), new Point3d(xEnd, y, 0) }, false, lTang));
                double tx = xL + cao;
                Them(g, TaoPline(new List<Point3d> { new Point3d(tx, y, 0), new Point3d(tx - 0.6 * cao, y + cao, 0), new Point3d(tx + 0.6 * cao, y + cao, 0) }, true, lTang));
                Them(g, TaoChu(g, new Point3d(tx + cao, y + 0.3 * cao, 0), ChuCaoDo(tang[i].Cot), cao, 0, false, lTang));
                Them(g, TaoChu(g, new Point3d(xL, y - 1.5 * cao, 0), tang[i].Ten, cao, 0, false, lTang));
                if (tang[i].Dem.Count == 0) continue;

                // Mỗi trục cấp một nhánh: van OS&Y + công tắc dòng chảy, mũi tên "đi tới sprinkler tầng". Nhánh vượt trục khác bằng nét cầu.
                for (int j = 0; j < n; j++)
                {
                    double yb = yNhanh(i, j), xj = x0 + j * dx;
                    Them(g, OngCau(new Point3d(xj, yb, 0), new Point3d(xEnd - 400 * k, yb, 0), Enumerable.Range(j + 1, n - j - 1).Select(m => x0 + m * dx), r, lOng));
                    Polyline mui = TaoPline(new List<Point3d> { new Point3d(xEnd - 400 * k, yb, 0), new Point3d(xEnd, yb, 0) }, false, lOng);
                    mui.SetStartWidthAt(0, 150 * k); mui.SetEndWidthAt(0, 0);
                    Them(g, mui);
                    DatSoDo(g, van[j], new Point3d(xv, yb, 0), k);
                    DatSoDo(g, fs, new Point3d(xfs, yb, 0), k);
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
                DatSoDo(g, aav, new Point3d(xj, yTop, 0), k);
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
            List<string> kq = new List<string> { "SPR_CTDC", "SPR_VAN_XK" };
            IList truc = Lay(o, "truc") as IList;
            if (truc == null) kq.Add("SPR_VAN_OSY_DN100");
            else foreach (object m in truc) kq.Add("SPR_VAN_OSY_" + Convert.ToString(m, CultureInfo.InvariantCulture).ToUpperInvariant().Replace("ONG_", ""));
            return kq;
        }

        static void DatSoDo(GoiSpec g, VatTu v, Point3d p, double k)
        {
            ChenThietBi(g.Db, g.Tr, v, p, k, 0);
            g.Ve++;
            MoHop(g, new Extents3d(p, p));
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
