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
                List<string> bc = new List<string> { LayChu(o, "tu", "BC_TT").ToUpperInvariant(), "BC_DTCD", "BC_DIEN_THOAI" };
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
        static Extents3d DatSoDo(GoiSpec g, VatTu v, Point3d p, double k, ObjectId layer)
        {
            BlockReference br = (BlockReference)g.Tr.GetObject(ChenThietBi(g.Db, g.Tr, v, p, k, 0), OpenMode.ForWrite);
            br.LayerId = layer;
            g.Ve++;
            Extents3d x = HopBlock(g, v, br.BlockTransform); // hộp nét ký hiệu (bỏ attribute ẩn): để ngắt dây, đặt số lượng dưới ký hiệu
            MoHop(g, x);
            return x;
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

        class ZoneSoDo { public string Ten, Kv, Ghi; public double Dt; public VatTu Day; public List<KeyValuePair<VatTu, double>> Dem = new List<KeyValuePair<VatTu, double>>(); }
        class TbNguon { public VatTu V; public string Ten; public double Sl, Iq, Ia; }

        // Thứ tự dòng bảng ký hiệu như trang 11 (mã khác xếp sau, theo danh mục).
        static readonly string[] THU_TU_KY_HIEU = { "BC_NHIET", "BC_KHOI", "BC_CHI_THI", "BC_COI_DEN", "SPR_VAN_OSY", "SPR_CTDC", "SPR_VAN_BD", "BC_NUTAN", "BC_DTCD", "BC_CHI_THI_HL" };

        /// Ký hiệu trên sơ đồ to hơn trong bảng ký hiệu bao nhiêu lần (đo trên trang 11: đầu báo 1,75; Ω nhỏ hơn bảng).
        static double TlKyHieu(string ma)
        {
            switch (ma)
            {
                case "BC_DTCD": return 0.87;
                case "BC_CHI_THI": case "BC_CHI_THI_HL": return 1.47; // trang 11: 17,9 x 14,2 pt
                case "BC_NUTAN": case "BC_TOHOP": return 1.95;
                case "BC_COI_DEN": return 1.83;
                case "SPR_CTDC": return 1.65;
                case "BC_DIEN_THOAI": return 1;
            }
            return ma.StartsWith("SPR_VAN_OSY_") ? 1.43 : 1.75;
        }

        /// Layer một loại dây trên sơ đồ: bản vẽ đã có layer (tên theo layer_so_do của mã) thì dùng nguyên nét, màu của người dùng;
        /// chưa có thì tạo với loại đường, màu ghi trong vat_tu.json (mặc định như trang 11). Tên bắt đầu PCCC_SODO: dem / PCCC_BOC bỏ qua.
        static ObjectId LayerDay(GoiSpec g, VatTu v)
        {
            string ten = string.IsNullOrEmpty(v.layer_so_do) ? LAYER_SO_DO + "_" + v.ma : v.layer_so_do;
            LayerTable lt = (LayerTable)g.Tr.GetObject(g.Db.LayerTableId, OpenMode.ForRead);
            if (lt.Has(ten)) return lt[ten];
            ObjectId id = DamBaoLayer(g.Db, g.Tr, ten, (short)(v.mau > 0 ? v.mau : MauHe("BC")));
            string net = v.net ?? "";
            if (net != "" && !net.Equals("CONTINUOUS", StringComparison.OrdinalIgnoreCase))
                ((LayerTableRecord)g.Tr.GetObject(id, OpenMode.ForWrite)).LinetypeObjectId = net == "PCCC_FR" ? NetChu(g, net, "FR") : DamBaoNet(g, net);
            return id;
        }

        /// Loại đường có chữ (dây chống cháy: "--- FR --- FR ---"), đơn vị mm như acadiso.lin.
        static ObjectId NetChu(GoiSpec g, string ten, string chu)
        {
            LinetypeTable t = (LinetypeTable)g.Tr.GetObject(g.Db.LinetypeTableId, OpenMode.ForRead);
            if (t.Has(ten)) return t[ten];
            t.UpgradeOpen();
            LinetypeTableRecord r = new LinetypeTableRecord();
            r.Name = ten;
            r.AsciiDescription = "---- " + chu + " ---- " + chu + " ----";
            r.PatternLength = 16;
            r.NumDashes = 3;
            r.SetDashLengthAt(0, 10);
            r.SetDashLengthAt(1, -0.8);
            r.SetShapeStyleAt(1, DamBaoKieuChu(g.Db, g.Tr));
            r.SetShapeNumberAt(1, 0);
            r.SetShapeScaleAt(1, 3);
            r.SetShapeOffsetAt(1, new Vector2d(0, -1.5));
            r.SetShapeRotationAt(1, 0);
            r.SetTextAt(1, chu);
            r.SetDashLengthAt(2, -5.2);
            ObjectId id = t.Add(r);
            g.Tr.AddNewlyCreatedDBObject(r, true);
            return id;
        }

        /// Hộp bao nét của block (bỏ attribute ẩn) sau phép biến đổi tf.
        static Extents3d HopBlock(GoiSpec g, VatTu v, Matrix3d tf)
        {
            BlockTable bt = (BlockTable)g.Tr.GetObject(g.Db.BlockTableId, OpenMode.ForRead);
            BlockTableRecord def = (BlockTableRecord)g.Tr.GetObject(bt[v.ma], OpenMode.ForRead);
            Point3d o = Point3d.Origin.TransformBy(tf);
            Extents3d x = new Extents3d(o, o);
            foreach (ObjectId id in def)
            {
                Entity e = (Entity)g.Tr.GetObject(id, OpenMode.ForRead);
                if (e is AttributeDefinition) continue;
                try { Extents3d a = e.GeometricExtents; a.TransformBy(tf); x.AddExtents(a); } catch (System.Exception) { }
            }
            return x;
        }

        static string E(string s) { return s.Replace("\\", "\\\\").Replace("{", "\\{").Replace("}", "\\}").Replace("\n", "\\P"); }
        static string Duoi(string s) { return "{\\H0.7x;\\S^" + s + ";}"; } // chữ số dưới (I_Q, C_20) của MText
        static string Dam(string s) { return "{\\fArial|b1|i0|c0|p34;" + s + "}"; }
        static string Sb(double x) { return SoVN(x, "0.###"); }
        static string SoVN(double x, string f) { return x.ToString(f, CultureInfo.InvariantCulture); } // dấu chấm thập phân như trang 11

        /// Sơ đồ nguyên lý báo cháy hệ thường, bố cục chép theo trang 11 hồ sơ thật (cad/knowledge/mau-so-do-that.md, so-do-nguyen-ly.md):
        /// bảng ký hiệu + ghi chú + bảng tính nguồn (phụ lục C TCVN 7568-14:2025, hệ số từ spec) bên trái; tủ trung tâm dưới giữa;
        /// bó dây đứng (elip + chú thích) rẽ vào từng zone (zone 1 dưới cùng); cột còi đèn bên phải; liên động đi ngang từ cạnh tủ; tiêu đề dưới phải.
        /// Toạ độ viết theo pt của trang 11 (A3), đổi ra bản vẽ bằng S = 33 mm/pt (bảng ký hiệu: vòng H 12,12 pt = 400 mm như block thư viện).
        static void SoDoBaoChay(GoiSpec g, Dictionary<string, object> o, List<TangSoDo> tang, double k, double h, double cao, Point3d p0)
        {
            if (Lay(o, "toi_da") == null) throw new System.Exception("he \"bc\" cần \"toi_da\" (số đầu báo tối đa một zone, tra cad/rule/LUAT.md BC-04)");
            double toiDa = LaySo(o, "toi_da", 0);
            if (toiDa <= 0) throw new System.Exception("\"toi_da\" phải > 0");
            string maTu = LayChu(o, "tu", "BC_TT").ToUpperInvariant();
            VatTu tu = TimMa(g.Dm, maTu), dtcd = TimMa(g.Dm, "BC_DTCD"), dthoai = TimMa(g.Dm, "BC_DIEN_THOAI");
            if (tu == null || tu.loai != "block") throw new System.Exception("\"tu\": không phải mã thiết bị: " + maTu);
            if (dtcd == null || dthoai == null) throw new System.Exception("danh mục thiếu BC_DTCD hoặc BC_DIEN_THOAI");
            Func<string, string, VatTu> Day = (khoa, macDinh) =>
            {
                string m = LayChu(o, khoa, macDinh).ToUpperInvariant();
                VatTu v = TimMa(g.Dm, m);
                if (v == null || v.loai != "tuyen") throw new System.Exception("\"" + khoa + "\": không phải mã dây: " + m);
                return v;
            };
            VatTu day = Day("day", "DAY_BC_10");
            VatTu dayTruc = Lay(o, "day_truc") == null ? day : Day("day_truc", ""), dayCoi = Day("day_coi", "DAY_NGUON_15"), dayNguon = Day("day_nguon", "DAY_NGUON_15_FR");
            Dictionary<string, object> tenTb = Lay(o, "ten_tb") as Dictionary<string, object>;
            Func<VatTu, string> Ten = v =>
            {
                if (tenTb != null) foreach (KeyValuePair<string, object> kv in tenTb) if (string.Equals(kv.Key, v.ma, StringComparison.OrdinalIgnoreCase)) return Convert.ToString(kv.Value, CultureInfo.InvariantCulture);
                return v.ten.ToUpperInvariant();
            };
            Func<VatTu, bool> LaDau = v => v.ten.StartsWith("Đầu báo"), LaNut = v => v.ma == "BC_NUTAN" || v.ma == "BC_TOHOP",
                LaChiThi = v => v.ma.StartsWith("BC_CHI_THI"), LaCoi = v => v.ma == "BC_COI_DEN";

            // --- Đọc zone (giữ cách đọc "tang" cũ) ---
            List<ZoneSoDo> zone = new List<ZoneSoDo>();
            List<VatTu> cot = new List<VatTu>();
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
                    ZoneSoDo z = new ZoneSoDo { Ten = LayChu(zd, "ten", "ZONE " + (zone.Count + 1)), Kv = kvz, Dt = LaySo(zd, "dt", 0), Ghi = LayChu(zd, "ghi", ""),
                        Day = Lay(zd, "day") == null ? day : TimMa(g.Dm, LayChu(zd, "day", "")) };
                    if (z.Day == null || z.Day.loai != "tuyen") throw new System.Exception(z.Ten + ": \"day\" không phải mã dây: " + LayChu(zd, "day", ""));
                    double nDau = 0;
                    foreach (KeyValuePair<string, object> kv in dem)
                    {
                        VatTu v = TimMa(g.Dm, kv.Key);
                        if (v == null) throw new System.Exception(z.Ten + ": mã không có trong danh mục: " + kv.Key);
                        if (v.loai != "block") continue; // "dem" chép từ kết quả dem có cả dây/ống (DAY_BC): sơ đồ zone không vẽ
                        double sl = D(kv.Value);
                        if (sl <= 0) continue;
                        if (LaDau(v)) nDau += sl;
                        z.Dem.Add(new KeyValuePair<VatTu, double>(v, sl));
                        if (!cot.Contains(v)) cot.Add(v);
                    }
                    if (z.Dem.Count == 0) throw new System.Exception(z.Ten + tTen + ": \"dem\" không có mã thiết bị nào");
                    if (nDau > toiDa) throw new System.Exception(z.Ten + tTen + ": " + So(nDau) + " đầu báo, quá toi_da " + So(toiDa) + ": chia thêm zone");
                    z.Dem = z.Dem.OrderBy(kv => g.Dm.vat_tu.IndexOf(kv.Key)).ToList();
                    zone.Add(z);
                }
            }
            if (zone.Count == 0) throw new System.Exception("không có \"zone\" hoặc \"dem\": sơ đồ không có zone nào");
            string dienThoai = LayChu(o, "dien_thoai", "ĐIỆN THOẠI\nKẾT NỐI\nPCCC");
            BlockTable bt = (BlockTable)g.Tr.GetObject(g.Db.BlockTableId, OpenMode.ForRead);
            foreach (VatTu v in cot.Concat(new VatTu[] { dtcd, dthoai }))
                if (!bt.Has(v.ma)) throw new System.Exception("Thư viện không có block " + v.ma + " (chạy PCCC_LIB)");

            // --- Bảng tính nguồn (phụ lục C TCVN 7568-14:2025): mọi hệ số truyền từ spec, số lượng cộng từ zone (hoặc "sl" ghi đè) ---
            Dictionary<string, object> ng = Lay(o, "nguon") as Dictionary<string, object>;
            if (Lay(o, "nguon") != null && ng == null) throw new System.Exception("\"nguon\" phải là object {tq,ta,fc,hs,tq_nap,t_nap,i_tu,tb:[...]}");
            List<TbNguon> tbs = new List<TbNguon>();
            if (ng != null)
            {
                foreach (string kh in new[] { "tq", "ta", "fc", "hs", "tq_nap", "t_nap", "i_tu" })
                {
                    if (Lay(ng, kh) == null) throw new System.Exception("\"nguon\" thiếu \"" + kh + "\" (công thức C.1, C.2 phụ lục C TCVN 7568-14:2025, knowledge/tcvn7568-14-2025.md)");
                    double hsv = LaySo(ng, kh, 0);
                    if (kh == "i_tu" ? hsv < 0 : hsv <= 0) throw new System.Exception("\"nguon." + kh + "\" phải " + (kh == "i_tu" ? ">= 0" : "> 0"));
                }
                foreach (object x in DS(Lay(ng, "tb"), "nguon.tb"))
                {
                    Dictionary<string, object> d = x as Dictionary<string, object>;
                    VatTu v = d == null ? null : TimMa(g.Dm, LayChu(d, "ma", ""));
                    if (v == null) throw new System.Exception("\"nguon.tb\": mỗi phần tử {ma, ia (mA/cái), iq (µA/cái)}, ma có trong danh mục");
                    if (Lay(d, "ia") == null) throw new System.Exception("\"nguon.tb\" " + v.ma + " thiếu \"ia\" (dòng báo cháy mA/cái, theo catalog thiết bị)");
                    tbs.Add(new TbNguon { V = v, Ten = LayChu(d, "ten", Ten(v)), Iq = LaySo(d, "iq", 0), Ia = LaySo(d, "ia", 0),
                        Sl = Lay(d, "sl") != null ? LaySo(d, "sl", 0) : zone.Sum(z => z.Dem.Where(kv => kv.Key == v).Sum(kv => kv.Value)) });
                    TbNguon tm = tbs[tbs.Count - 1];
                    if (tm.Iq < 0 || tm.Ia < 0 || tm.Sl < 0) throw new System.Exception("\"nguon.tb\" " + v.ma + ": \"iq\", \"ia\", \"sl\" phải >= 0");
                }
                if (tbs.Count == 0) throw new System.Exception("\"nguon.tb\" rỗng");
            }

            // --- Bố trí ---
            double S = 33 * k;                                        // 1 pt trang 11 -> đơn vị bản vẽ
            Func<double, double, Point3d> M = (x, y) => new Point3d(p0.X + (x - 548.2) * S, p0.Y - (y - 735.6) * S, 0); // gốc "p" = góc dưới trái mặt tủ
            Func<Point3d, double> PX = q => (q.X - p0.X) / S + 548.2;
            Func<Point3d, double> PY = q => 735.6 - (q.Y - p0.Y) / S;
            int nz = zone.Count;
            if (Lay(o, "zone_tu") != null && LaySo(o, "zone_tu", 0) < nz) throw new System.Exception("\"zone_tu\" (" + LaySo(o, "zone_tu", 0) + ") nhỏ hơn số zone trên sơ đồ (" + nz + ")");
            double dy = Math.Max((Lay(o, "h") == null ? 1500 * k : h) / S, 38); // khoảng hàng zone (pt), tối thiểu đủ ký hiệu chỉ thị nâng lên + số lượng
            Func<int, double> Yz = i => 547.0 - i * dy;
            double bs = nz > 1 ? Math.Min(8.3, 91.0 / (nz - 1)) : 8.3; // bó dây đứng: zone 1 bên phải, zone sau lùi trái (không dây nào cắt nhau)
            Func<int, double> Xv = i => 661.2 - i * bs;
            double lts = 26 * k / (g.Db.Ltscale > 0 ? g.Db.Ltscale : 1);
            ObjectId lChu = DamBaoLayer(g.Db, g.Tr, LAYER_SO_DO_BC, MauHe("BC"));

            Func<ObjectId, bool, Point3d[], Polyline> Net = (l, kin, ps) => { Polyline pl = TaoPline(ps.ToList(), kin, l); pl.LinetypeScale = lts; Them(g, pl); return pl; };
            Func<double, double, double, double, Polyline> Ke = (x1, y1, x2, y2) => Net(lChu, false, new[] { M(x1, y1), M(x2, y2) });
            Action<double, double, double, double> KhungN = (x1, y1, x2, y2) => Net(lChu, true, new[] { M(x1, y1), M(x2, y1), M(x2, y2), M(x1, y2) });
            Func<Point3d, string, double, AttachmentPoint, double, MText> ChuM = (q, t, c, att, rong) =>
            {
                MText m = TaoChu(g, q, "", c * S, 0, false, lChu);
                m.Contents = t; m.Attachment = att; m.Location = q;
                if (rong > 0) m.Width = rong * S;
                Them(g, m);
                return m;
            };
            Func<double, double, string, double, AttachmentPoint, MText> Chu = (x, y, t, c, att) => ChuM(M(x, y), E(t), c, att, 0);
            Action<double, double, int> MuiNua = (xt, y, huong) => Net(lChu, true, new[] { M(xt, y), M(xt - 10 * huong, y), M(xt - 10 * huong, y - 3.2) }); // nửa mũi tên trên đường
            Func<VatTu, Point3d, double, Extents3d> Dat = (v, q, f) => DatSoDo(g, v, q, k * f, lChu);
            Func<double, string> SoLg = x => x == Math.Floor(x) && x < 100 ? ((int)x).ToString("00", CultureInfo.InvariantCulture) : So(x);
            Action<Extents3d, double> SoDuoi = (e, sl) => ChuM(new Point3d((e.MinPoint.X + e.MaxPoint.X) / 2, e.MinPoint.Y - 3 * S, 0), SoLg(sl), 6.0, AttachmentPoint.TopCenter, 0);
            // Đường ngang y từ x1 tới x2 (pt) trên layer dây, ngắt qua ký hiệu (trang 11 ký hiệu che dây).
            Action<ObjectId, double, double, double, List<Extents3d>> NgangNgat = (l, y, x1, x2, che) =>
            {
                double x = x1;
                foreach (Extents3d e in che.OrderBy(e => e.MinPoint.X))
                {
                    double a = PX(e.MinPoint), b = PX(e.MaxPoint);
                    if (a > x + 0.5) Net(l, false, new[] { M(x, y), M(Math.Min(a, x2), y) });
                    x = Math.Max(x, b);
                    if (x >= x2) return;
                }
                if (x2 > x + 0.5) Net(l, false, new[] { M(x, y), M(x2, y) });
            };

            // Cột thiết bị trên hàng zone: nút ấn 697, đầu báo + thiết bị khác từ 763 mỗi loại 34 pt; điện trở cuối dây sau thiết bị cuối cùng.
            Func<VatTu, int> Hang = v => { int i = Array.FindIndex(THU_TU_KY_HIEU, m => v.ma == m || (m == "SPR_VAN_OSY" && v.ma.StartsWith("SPR_VAN_OSY_"))); return i < 0 ? 100 + g.Dm.vat_tu.IndexOf(v) : i; };
            int nCot = zone.Max(z => z.Dem.Count(kv => !LaNut(kv.Key) && !LaChiThi(kv.Key) && !LaCoi(kv.Key) && (LaDau(kv.Key) || z.Dem.Any(q => LaDau(q.Key) || LaNut(q.Key)))));
            double xOm = Math.Max(831.2, 763 + (nCot - 1) * 34 + 50), xCoi = 894.7 + (xOm - 831.2);
            List<KeyValuePair<double, double>> coi = new List<KeyValuePair<double, double>>(); // (y hàng, số lượng)

            for (int i = 0; i < nz; i++)
            {
                ZoneSoDo z = zone[i];
                double y = Yz(i), xv = Xv(i), r = 3;
                Polyline doc = Net(LayerDay(g, dayTruc), false, new[] { M(xv, 574), M(xv, y + r), M(xv + r, y) });
                doc.SetBulgeAt(1, -Math.Tan(Math.PI / 8)); // góc lượn: lên rồi rẽ phải
                Chu(xv + 3.5, y + 1.5, z.Ten, 4.5, AttachmentPoint.TopLeft);
                List<Extents3d> che = new List<Extents3d>();
                bool giamSat = !z.Dem.Any(kv => LaDau(kv.Key) || LaNut(kv.Key));
                double xHet;
                if (giamSat)
                {
                    // Zone giám sát van: van OS&Y, công tắc dòng chảy, van báo động nối tiếp; chữ mô tả bên phải, không điện trở cuối dây (như trang 11).
                    double xc = 737;
                    foreach (KeyValuePair<VatTu, double> kv in z.Dem.Where(q => !LaCoi(q.Key)).OrderBy(q => Hang(q.Key))) // OS&Y trước FS, van báo động
                    {
                        double f = TlKyHieu(kv.Key.ma);
                        Extents3d e0 = HopBlock(g, kv.Key, Matrix3d.Scaling(k * f, Point3d.Origin));
                        double x = xc - e0.MinPoint.X / S;
                        Extents3d e = Dat(kv.Key, M(x, y), f);
                        che.Add(e); SoDuoi(e, kv.Value);
                        xc = PX(e.MaxPoint) + 6;
                    }
                    xHet = che.Count > 0 ? PX(che[che.Count - 1].MinPoint) + 1 : xc;
                    string ghi = z.Ghi != "" ? z.Ghi : string.Join("\n", z.Dem.Where(q => !LaCoi(q.Key)).OrderBy(q => Hang(q.Key)).Select(q => Ten(q.Key))) + (z.Kv != "" ? "\n(" + z.Kv + ")" : "");
                    Chu(Math.Max(xOm + 21.8, xc + 58), y - 6.4, ghi, 4.5, AttachmentPoint.TopCenter);
                }
                else
                {
                    int j = 0;
                    Extents3d? dau = null;
                    foreach (KeyValuePair<VatTu, double> kv in z.Dem.Where(q => !LaChiThi(q.Key) && !LaCoi(q.Key)))
                    {
                        double x = LaNut(kv.Key) ? 697 : 763 + 34 * j++;
                        Extents3d e = Dat(kv.Key, M(x, y), TlKyHieu(kv.Key.ma));
                        che.Add(e); SoDuoi(e, kv.Value);
                        if (LaDau(kv.Key) && dau == null) dau = e;
                    }
                    // Thiết bị chỉ thị: ký hiệu nâng lên trên đầu báo, nối xiên, số dưới ký hiệu; loại hành lang (HL) lệch trái như trang 11.
                    foreach (KeyValuePair<VatTu, double> kv in z.Dem.Where(q => LaChiThi(q.Key)))
                    {
                        bool trai = kv.Key.ma == "BC_CHI_THI_HL";
                        Point3d c0 = dau.HasValue ? new Point3d((dau.Value.MinPoint.X + dau.Value.MaxPoint.X) / 2, (dau.Value.MinPoint.Y + dau.Value.MaxPoint.Y) / 2, 0) : M(763, y);
                        double rd = dau.HasValue ? (dau.Value.MaxPoint.X - dau.Value.MinPoint.X) / 2 : 9 * S;
                        Point3d pTb = c0 + new Vector3d((trai ? -25 : 26) * S, (trai ? 25.6 : 27) * S, 0);
                        Extents3d e = Dat(kv.Key, pTb, TlKyHieu(kv.Key.ma));
                        // HL: hộp bao gồm cả chữ "HL" lệch phải trên; góc phải khung = đối xứng mép trái qua tâm đặt.
                        if (trai) e = new Extents3d(e.MinPoint, new Point3d(2 * pTb.X - e.MinPoint.X, e.MaxPoint.Y, 0));
                        SoDuoi(e, kv.Value);
                        double gocDau = (trai ? 120 : 60) * Math.PI / 180;
                        Net(LayerDay(g, z.Day), false, new[] { c0 + new Vector3d(rd * Math.Cos(gocDau), rd * Math.Sin(gocDau), 0), new Point3d(trai ? e.MaxPoint.X : e.MinPoint.X, e.MinPoint.Y, 0) });
                    }
                    Dat(dtcd, M(xOm, y), TlKyHieu("BC_DTCD"));
                    xHet = xOm;
                    List<string> dong = new List<string>();
                    if (z.Kv != "") dong.Add(z.Kv);
                    if (z.Dt > 0) dong.Add("DIỆN TÍCH BẢO VỆ: " + SoVN(z.Dt, "0.##") + " M²");
                    dong.Add(z.Ten);
                    // Cỡ 4.0, tâm lùi trái: dòng "DIỆN TÍCH BẢO VỆ: xxx M²" không chạm cột còi đèn.
                    Chu(xOm + 8, y + 6, z.Ghi != "" ? z.Ghi : string.Join("\n", dong), 4.0, AttachmentPoint.TopCenter);
                }
                NgangNgat(LayerDay(g, z.Day), y, xv + r, xHet, che);
                double slCoi = z.Dem.Where(kv => LaCoi(kv.Key)).Sum(kv => kv.Value);
                if (slCoi > 0) coi.Add(new KeyValuePair<double, double>(y, slCoi));
            }

            // Cột còi đèn bên phải: dây nguồn từ cạnh tủ sang rồi lên, mỗi zone một còi đèn + số lượng.
            VatTu vCoi = cot.FirstOrDefault(v => LaCoi(v));
            if (vCoi != null)
            {
                ObjectId lCoi = LayerDay(g, dayCoi);
                List<Extents3d> cheCoi = coi.Select(c => Dat(vCoi, M(xCoi, c.Key - 19), TlKyHieu(vCoi.ma))).ToList();
                for (int i = 0; i < coi.Count; i++) SoDuoi(cheCoi[i], coi[i].Value);
                double yDinh = coi.Min(c => c.Key) - 19, yd = 596.2;
                Net(lCoi, false, new[] { M(667.1, yd), M(xCoi, yd) });
                foreach (Extents3d e in cheCoi.OrderByDescending(e => PY(e.MinPoint)))
                {
                    if (PY(e.MinPoint) < yd - 0.5) Net(lCoi, false, new[] { M(xCoi, yd), M(xCoi, Math.Max(PY(e.MinPoint), yDinh)) });
                    yd = PY(e.MaxPoint);
                }
                Chu(690.6, 594.8, Ten(dayCoi), 6.4, AttachmentPoint.BottomLeft);
            }

            // Bó dây: elip quanh bó + chú thích loại dây; dòng zone dự phòng mũi tên sang trái.
            string duPhong = LayChu(o, "du_phong", "");
            double xTrai = Xv(nz - 1);
            if (duPhong != "")
            {
                double xd = xTrai - 7, yd = Yz(0) - 0.77 * dy;
                Net(LayerDay(g, dayTruc), false, new[] { M(xd, 574), M(xd, yd), M(xd - 75, yd) });
                MuiNua(xd - 75, yd, -1);
                Chu(xd - 73.7, yd + 4, duPhong, 4.5, AttachmentPoint.TopLeft);
                xTrai = xd;
            }
            double exc = (xTrai + 661.2) / 2, exa = (661.2 - xTrai) / 2 + 9;
            Them(g, new Ellipse(M(exc, 561.6), Vector3d.ZAxis, new Vector3d(exa * S, 0, 0), 6 / exa, 0, 2 * Math.PI) { LayerId = lChu });
            string tenTruc = Ten(dayTruc);
            int cat = System.Text.RegularExpressions.Regex.Match(tenTruc, @"\s\d").Index; // tách "DÂY TÍN HIỆU" / "2C ..."
            Ke(exc - exa - 70, 561.6, exc - exa, 561.6);
            Chu(exc - exa - 2, 560.6, cat > 0 ? tenTruc.Substring(0, cat) : tenTruc, 5.0, AttachmentPoint.BottomRight);
            if (cat > 0) Chu(exc - exa - 2, 563.4, tenTruc.Substring(cat + 1), 5.0, AttachmentPoint.TopRight);

            // Tủ trung tâm (hình hộp), hàng đèn zone, nút, loa; chữ tên tủ + số zone.
            int nzTu = (int)LaySo(o, "zone_tu", nz);
            KhungN(548.2, 584.3, 648.9, 735.6);
            Net(lChu, false, new[] { M(548.2, 584.3), M(566.4, 574), M(667.1, 574), M(648.9, 584.3) });
            Net(lChu, false, new[] { M(667.1, 574), M(667.1, 725.3), M(648.9, 735.6) });
            KhungN(561.7, 606.7, 636, 690.4); KhungN(563.2, 608.2, 634.5, 688.9);
            int hangDen = Math.Max(1, (nzTu + 4) / 5);
            double bd = Math.Min(12.75, 38.25 / hangDen);
            for (int i = 0; i < nzTu; i++)
            {
                double xl = 575 + (i % 5) * 12.8, yl = 612 + (i / 5) * bd;
                Polyline den = TaoPline(new List<Point3d> { M(xl - 4, yl + 1.3), M(xl + 4, yl + 1.3), M(xl + 4, yl - 1.3), M(xl - 4, yl - 1.3) }, true, lChu);
                den.SetBulgeAt(1, 1); den.SetBulgeAt(3, 1);
                Them(g, den);
                Chu(xl, yl + 2.6, "ZONE " + (i + 1), Math.Min(2.2, bd / 4), AttachmentPoint.TopCenter);
            }
            KhungN(564.5, 658.6, 633.3, 670); KhungN(566, 659.5, 598, 669); KhungN(600, 659.5, 608.5, 669); KhungN(610, 659.5, 618.5, 669); KhungN(620, 659.5, 632, 669);
            KhungN(566.5, 672, 580.5, 688); KhungN(583, 672, 632.5, 688);
            for (double yl = 674; yl <= 686.1; yl += 1.3) { Ke(568, yl, 579, yl); Ke(586, yl, 630, yl); }
            Them(g, new Circle(M(556.2, 670.6), Vector3d.ZAxis, 1.6 * S) { LayerId = lChu });
            Chu(599, 701.1, tu.ten.ToUpperInvariant(), 5.8, AttachmentPoint.TopCenter);
            Chu(598.7, 711, nzTu + " ZONE", 5.8, AttachmentPoint.TopCenter);
            Chu(490.1, 599.4, "ĐÈN BÁO CÁC ZONE", 5.1, AttachmentPoint.BottomCenter);
            Chu(490.2, 603.1, "TẠI TRUNG TÂM BÁO CHÁY", 5.1, AttachmentPoint.TopCenter);
            Ke(444.4, 600.2, 525.9, 600.2); Ke(525.9, 600.2, 571, 612); Ke(525.9, 600.2, 571, 612 + bd);
            string ghiTu = LayChu(o, "ghi_tu", "");
            if (ghiTu != "") Chu(469.6, 689.1, ghiTu, 3.8, AttachmentPoint.TopLeft);
            if (dienThoai != "")
            {
                Dat(dthoai, M(507.6, 644.7), 1);
                Chu(435.8, 626.6, dienThoai, 6.6, AttachmentPoint.TopLeft);
                Ke(429.7, 647.9, 493.5, 647.9);
            }
            // Tiếp địa, nguồn chính 220 V AC, nguồn dự phòng 24 V DC (đi xuống từ đáy tủ, rẽ trái, nửa mũi tên).
            Ke(548.2, 717, 532.6, 717); Ke(532.6, 717, 532.6, 744.5); Ke(524.6, 744.5, 540.6, 744.5); Ke(527.6, 747.3, 537.6, 747.3); Ke(530.6, 750.1, 534.6, 750.1);
            Chu(538.6, 745.9, "TIẾP ĐỊA", 5.8, AttachmentPoint.TopLeft);
            ObjectId lNg = LayerDay(g, dayNguon);
            Net(lNg, false, new[] { M(623.4, 735.6), M(623.4, 787.6), M(494, 787.6) }); MuiNua(494, 787.6, -1);
            Net(lNg, false, new[] { M(643.6, 735.6), M(643.6, 809.6), M(514.3, 809.6) }); MuiNua(514.3, 809.6, -1);
            Chu(560.6, 786.3, LayChu(o, "nguon_chinh", "NGUỒN ĐIỆN CHÍNH 220V AC\n( NGUỒN ĐIỆN ƯU TIÊN)"), 6.4, AttachmentPoint.BottomCenter);
            string[] dp = LayChu(o, "nguon_du_phong", "NGUỒN ĐIỆN DỰ PHÒNG 24V/DC").Split('\n');
            Chu(642.1, 808.7, dp[0], 6.4, AttachmentPoint.BottomRight);
            if (dp.Length > 1) Chu(642.5, 813.3, string.Join("\n", dp.Skip(1)), 6.4, AttachmentPoint.TopRight);

            // Liên động: đường ngang từ cạnh phải tủ, chữ trên đường, nửa mũi tên cuối đường.
            double yld = 617.8;
            List<string> lienDong = new List<string>();
            if (Lay(o, "lien_dong") != null)
                foreach (object x in DS(Lay(o, "lien_dong"), "lien_dong")) lienDong.Add(Convert.ToString(x, CultureInfo.InvariantCulture));
            for (int i = 0; i < lienDong.Count; i++)
            {
                int nd = lienDong[i].Split('\n').Length;
                if (i > 0) yld += 21.2 + (nd - 1) * 13.2;
                Chu(671.4, yld - 1.1, lienDong[i], 6.3, AttachmentPoint.BottomLeft);
                Ke(667.1, yld, 920, yld);
                MuiNua(920, yld, 1);
            }
            ChuM(M(830.4, Math.Max(755.2, yld + 20)), Dam("\\L" + E(LayChu(o, "tieu_de", "SƠ ĐỒ NGUYÊN LÍ\nHỆ THỐNG BÁO CHÁY"))), 10.2, AttachmentPoint.TopCenter, 0);

            // --- Bên trái: bảng ký hiệu (chỉ mã có trên sơ đồ + các loại dây đã dùng), ghi chú, bảng tính nguồn, kết luận ---
            List<VatTu> muc = cot.Concat(new[] { dtcd }).Distinct().OrderBy(Hang).ToList();
            List<VatTu> dayDung = new List<VatTu> { dayTruc };
            dayDung.AddRange(zone.Select(z => z.Day));
            if (vCoi != null) dayDung.Add(dayCoi);
            dayDung.Add(dayNguon);
            muc.AddRange(dayDung.Distinct().OrderBy(v => g.Dm.vat_tu.IndexOf(v)));
            int nh = (muc.Count + 1) / 2;
            double lt = Math.Min(20.0, Yz(nz - 1) - 17.3), rh = 22.7, lb = lt + rh * (nh + 1);
            KhungN(35.4, lt, 530, lb);
            foreach (double x in new[] { 97, 282, 320 }) Ke(x, lt, x, lb);
            for (int i = 1; i <= nh; i++) Ke(35.4, lt + rh * i, 530, lt + rh * i);
            double[] xk = { 66.2, 301 }, xTen = { 100, 323 }, xk0 = { 35.4, 282 }, xk1 = { 97, 320 };
            for (int c = 0; c < 2; c++)
            {
                Chu(xk[c], lt + rh / 2, "KÍ HIỆU", 5.8, AttachmentPoint.MiddleCenter);
                Chu(xTen[c] + 20, lt + rh / 2, "TÊN THIẾT BỊ", 5.8, AttachmentPoint.MiddleLeft);
            }
            for (int i = 0; i < muc.Count; i++)
            {
                int c = i < nh ? 0 : 1;
                double yc = lt + rh * ((i < nh ? i : i - nh) + 1) + rh / 2;
                VatTu v = muc[i];
                if (v.loai == "block")
                {
                    Extents3d e0 = HopBlock(g, v, Matrix3d.Scaling(k, Point3d.Origin));
                    Point3d q = M(xk[c], yc);
                    Dat(v, new Point3d(q.X - (e0.MinPoint.X + e0.MaxPoint.X) / 2, q.Y - (e0.MinPoint.Y + e0.MaxPoint.Y) / 2, 0), 1);
                }
                else Net(LayerDay(g, v), false, new[] { M(xk0[c] + 5, yc), M(xk1[c] - 5, yc) }).LinetypeScale = lts / 2.5;
                Chu(xTen[c], yc, Ten(v), 5.4, AttachmentPoint.MiddleLeft);
            }
            double yN = lb + 14.3;
            string ghiChu = LayChu(o, "ghi_chu", "");
            if (ghiChu != "")
            {
                Chu(58.5, yN, "*GHI CHÚ:", 9.1, AttachmentPoint.TopLeft);
                Chu(59.2, yN + 18.9, ghiChu, 9.1, AttachmentPoint.TopLeft);
                yN += 18.9 + 24.8 + 15 * (ghiChu.Split('\n').Length - 1);
            }
            if (ng != null) VeBangNguon(g, ng, tbs, nzTu, yN, S, M, Ke, ChuM);
        }

        /// Bảng tính nguồn tủ trung tâm theo phụ lục C TCVN 7568-14:2025 (công thức C.1, C.2, C.3 chép ở knowledge/tcvn7568-14-2025.md), hệ số lấy từ spec:
        /// C20 = hs[(IQ × TQ) + FC(IA × TA)]; IC = hs[(IQ × tq_nap) + (IA × TA)] / t_nap; IPSE = IQ + IC. IQ, IA gồm dòng tủ i_tu.
        static void VeBangNguon(GoiSpec g, Dictionary<string, object> ng, List<TbNguon> tbs, int nzTu, double y0, double S, Func<double, double, Point3d> M,
            Func<double, double, double, double, Polyline> Ke, Func<Point3d, string, double, AttachmentPoint, double, MText> ChuM)
        {
            double tq = LaySo(ng, "tq", 0), ta = LaySo(ng, "ta", 0), fc = LaySo(ng, "fc", 0), hs = LaySo(ng, "hs", 0), tqNap = LaySo(ng, "tq_nap", 0), tNap = LaySo(ng, "t_nap", 0), iTu = LaySo(ng, "i_tu", 0);
            if (tNap <= 0) throw new System.Exception("\"nguon.t_nap\" phải > 0");
            double iq = iTu + tbs.Sum(t => t.Sl * t.Iq) / 1000, ia = iTu + tbs.Sum(t => t.Sl * t.Ia); // mA
            double c20 = hs * (iq / 1000 * tq + fc * (ia / 1000 * ta)), ic = hs * (iq / 1000 * tqNap + ia / 1000 * ta) / tNap, ipse = iq / 1000 + ic;
            g.KqNguon = new Dictionary<string, object> { { "iq_ma", Math.Round(iq, 6) }, { "ia_ma", Math.Round(ia, 6) }, { "c20_ah", Math.Round(c20, 6) }, { "ic_a", Math.Round(ic, 6) }, { "ipse_a", Math.Round(ipse, 6) } };

            double x0 = 49.3, x1 = 196.7, x2 = 526, cw = (x2 - x1) / tbs.Count;
            double[] rh = { 28.3, 30, 11.7, 13, 12.6, 26, 10.7, 13, 26, 26.3, 24 };
            double[] y = new double[rh.Length + 1];
            y[0] = y0;
            for (int i = 0; i < rh.Length; i++) y[i + 1] = y[i] + rh[i];
            Ke(x0, y[0], x2, y[0]); Ke(x0, y[0], x0, y[rh.Length]); Ke(x2, y[0], x2, y[rh.Length]);
            for (int i = 1; i <= rh.Length; i++) Ke(x0, y[i], x2, y[i]);
            Ke(x1, y[1], x1, y[rh.Length]);
            for (int c = 1; c < tbs.Count; c++) Ke(x1 + c * cw, y[1], x1 + c * cw, y[5]);
            double ch = 4.6;
            Action<double, double, double, string, double> O = (xa, xb, r, t, c) => ChuM(M((xa + xb) / 2, (y[(int)r] + y[(int)r + 1]) / 2), t, c, AttachmentPoint.MiddleCenter, xb - xa - 3);
            string can = LayChu(ng, "can_cu", "");
            O(x0, x2, 0, Dam("BẢNG TÍNH TOÁN NGUỒN ĐIỆN TỦ TRUNG TÂM BÁO CHÁY (VỚI T" + Duoi("Q") + "=" + Sb(tq) + "h, T" + Duoi("A") + "=" + Sb(ta) + "h, F" + Duoi("C") + "=" + Sb(fc) + ")"
                + (can != "" ? "\\P" + E(can) : "")), 5.3);
            O(x0, x1, 1, Dam("THIẾT BỊ"), ch);
            O(x0, x1, 2, Dam("SỐ LƯỢNG"), ch);
            O(x0, x1, 3, Dam("DÒNG TĨNH I" + Duoi("Q") + " (µA)"), ch);
            O(x0, x1, 4, Dam("DÒNG BÁO CHÁY I" + Duoi("A") + " (mA)"), ch);
            for (int c = 0; c < tbs.Count; c++)
            {
                double xa = x1 + c * cw, xb = xa + cw;
                O(xa, xb, 1, E(tbs[c].Ten), ch);
                O(xa, xb, 2, Sb(tbs[c].Sl), ch);
                O(xa, xb, 3, tbs[c].Iq > 0 ? Sb(tbs[c].Iq) : "(Không đáng kể)", ch);
                O(xa, xb, 4, Sb(tbs[c].Sl * tbs[c].Ia), ch);
            }
            string hsT = Sb(hs), q = "I" + Duoi("Q"), a = "I" + Duoi("A");
            O(x0, x1, 5, Dam("TỔNG DÒNG TỦ TRUNG TÂM BÁO CHÁY\\P" + nzTu + " ZONE (mA)"), ch); O(x1, x2, 5, Sb(iTu), ch);
            O(x0, x1, 6, Dam("TỔNG DÒNG TĨNH " + q + " (mA)"), ch); O(x1, x2, 6, SoVN(iq, "0.###"), ch);
            O(x0, x1, 7, Dam("TỔNG DÒNG BÁO CHÁY " + a + " (mA)"), ch); O(x1, x2, 7, SoVN(ia, "0.###"), ch);
            O(x0, x1, 8, Dam("DUNG LƯỢNG ẮC QUY C" + Duoi("20") + " (Ah)\\PC" + Duoi("20") + "=" + hsT + "[(" + q + "xT" + Duoi("Q") + ")+F" + Duoi("C") + "(" + a + "xT" + Duoi("A") + ")]"), ch);
            O(x1, x2, 8, SoVN(c20, "0.#####"), ch);
            O(x0, x1, 9, Dam("DÒNG ĐIỆN NẠP I" + Duoi("C") + " (A)\\PI" + Duoi("C") + "=" + hsT + "[(" + q + "x" + Sb(tqNap) + ")+(" + a + "x" + Sb(ta) + ")]/" + Sb(tNap)), ch);
            O(x1, x2, 9, SoVN(ic, "0.###"), ch);
            O(x0, x1, 10, Dam("DÒNG ĐIỆN TỔNG (A)\\PI" + Duoi("PSE") + "=" + q + "+I" + Duoi("C")), ch);
            O(x1, x2, 10, SoVN(ipse, "0.##"), ch);
            string kl = LayChu(ng, "ket_luan", "chọn ắc quy dự phòng có dung lượng ≥ " + SoVN(Math.Ceiling(c20 * 100) / 100, "0.##") + " Ah");
            ChuM(M(58, y[rh.Length] + 17.9), E("Kết luận: " + kl), 12.1, AttachmentPoint.TopLeft, 365);
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
