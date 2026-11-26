// Tuyến ống / dây (thao tác tuyen, PCCC_TUYEN), ống nhánh tự chọn DN. Tách từ Rai.cs (26/11/2026: mỗi thư mục số của 07_CAD là một hạng mục tính năng, xem 07_CAD/README.md).
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
        /// Thứ tự rắn bò qua các điểm: gom thành hàng theo Y, đổi chiều mỗi hàng. Trong 4 góc xuất phát, lấy góc gần điểm "gan" nhất
        /// (không có thì đi từ trên xuống, từ trái sang).
        static List<Point3d> RanBo(List<Point3d> ps, Point3d? gan, double saiSo)
        {
            List<List<Point3d>> hang = Gom(ps, p => p.Y, saiSo);
            foreach (List<Point3d> h in hang) h.Sort((p, q) => p.X.CompareTo(q.X));
            List<Point3d> tot = null;
            double dTot = double.MaxValue;
            for (int k = 0; k < 4; k++)
            {
                bool tuTren = (k & 1) == 0, trai = (k & 2) == 0;
                List<Point3d> thu = new List<Point3d>();
                foreach (List<Point3d> h in tuTren ? Enumerable.Reverse(hang) : (IEnumerable<List<Point3d>>)hang)
                {
                    thu.AddRange(trai ? (IEnumerable<Point3d>)h : Enumerable.Reverse(h));
                    trai = !trai;
                }
                double d = gan.HasValue ? gan.Value.DistanceTo(thu[0]) : k;
                if (d < dTot) { dTot = d; tot = thu; }
            }
            return tot;
        }

        /// Dây nối tiếp: MỘT đường qua lần lượt mọi điểm, hết nhóm (phòng) này sang nhóm kế, mỗi nhóm bắt đầu từ góc gần điểm vừa tới.
        static List<List<Point3d>> TuyenNoiTiep(List<List<Point3d>> nhom, Point3d? tu, Point3d? den, double saiSo)
        {
            List<Point3d> d = new List<Point3d>();
            if (tu.HasValue) d.Add(tu.Value);
            foreach (List<Point3d> ps in nhom)
                if (ps.Count > 0) d.AddRange(RanBo(ps, d.Count > 0 ? d[d.Count - 1] : (Point3d?)null, saiSo));
            if (den.HasValue) d.Add(den.Value);
            List<Point3d> gon = new List<Point3d>();
            foreach (Point3d p in d)
                if (gon.Count == 0 || gon[gon.Count - 1].DistanceTo(p) > 1e-6) gon.Add(p);
            List<List<Point3d>> kq = new List<List<Point3d>>();
            if (gon.Count >= 2) kq.Add(gon);
            return kq;
        }

        /// Xương cá: MỘT ống góp vuông góc ống chính tới hàng xa nhất + mỗi hàng một nhánh song song ống chính, từ thiết bị ngoài cùng tới ống góp.
        /// Ống góp đi trùng cột thiết bị gần "tu" nhất (không có "tu" thì cột gần giữa), hoặc qua điểm "qua".
        /// Chỉ chọn cột có chân ống góp nằm trong đoạn ống chính; không cột nào được thì lấy cột chân gần đoạn nhất.
        /// Hoà thì lấy cột có chân X nhỏ hơn, rồi Y nhỏ hơn (WCS): đảo chiều vẽ "truc" không đổi kết quả.
        /// Chân ống góp (kể cả theo "qua") ra ngoài CẢ polyline ống chính thì nối thêm một đoạn về điểm gần nhất của ống chính.
        /// dem != null (PCCC_TUYEN TuDong): nhánh cắt tại từng đầu phun, ống góp cắt tại từng hàng; dem nhận, theo thứ tự đoạn, số đầu phun phía sau đoạn đó.
        static List<List<Point3d>> TuyenXuongCa(List<Point3d> ps, List<Point3d> truc, Point3d? tu, Point3d? qua, double saiSo, List<int> dem = null)
        {
            Point3d tam = new Point3d(ps.Average(p => p.X), ps.Average(p => p.Y), 0);
            int i0 = DoanGan(truc, tam);
            Point3d A = truc[i0], B = truc[i0 + 1];
            Vector3d u = (B - A).GetNormal(), n = new Vector3d(-u.Y, u.X, 0);
            Func<Point3d, double> fa = p => (p - A).DotProduct(u), fd = p => (p - A).DotProduct(n);
            Func<double, double> ngoai = x => (A + u * x).DistanceTo(ChanTruc(truc, A + u * x)); // chân ống góp cách ống chính (cả polyline) bao nhiêu
            double af;
            if (qua.HasValue) af = fa(qua.Value);
            else
            {
                double dich = tu.HasValue ? fa(tu.Value) : ps.Average(fa);
                List<double> cot = Gom(ps, fa, saiSo).Select(c => c.Average(fa)).ToList();
                bool coTrong = cot.Any(x => ngoai(x) <= saiSo);
                if (coTrong) cot = cot.Where(x => ngoai(x) <= saiSo).ToList();
                Func<double, double> lech = coTrong ? (Func<double, double>)(x => Math.Abs(x - dich)) : ngoai;
                double tot = cot.Min(lech);
                af = cot.Where(x => lech(x) <= tot + saiSo).OrderBy(x => Math.Round((A + u * x).X / saiSo)).ThenBy(x => (A + u * x).Y).First();
            }
            List<List<Point3d>> hang = Gom(ps, fd, saiSo), kq = new List<List<Point3d>>();
            double thap = Math.Min(0, hang.Min(h => h.Average(fd))), cao = Math.Max(0, hang.Max(h => h.Average(fd)));
            if (dem != null)
            {
                // Nước đi: ống chính -> chân ống góp -> ống góp (ra hai phía ống chính) -> nhánh (ra hai phía ống góp). Mỗi đoạn mang số đầu phun ở phía ngoài nó.
                Action<Point3d, Point3d, int> them = (p, q, c) => { if (p.DistanceTo(q) > 1e-6) { kq.Add(new List<Point3d> { p, q }); dem.Add(c); } };
                if (ngoai(af) > 1e-6) them(ChanTruc(truc, A + u * af), A + u * af, ps.Count);
                foreach (int sg in new[] { 1, -1 })
                {
                    List<List<Point3d>> hs = hang.Where(h => h.Average(fd) * sg > saiSo).OrderBy(h => Math.Abs(h.Average(fd))).ToList();
                    int con = hs.Sum(h => h.Count);
                    double truoc = 0;
                    foreach (List<Point3d> h in hs) { double d = h.Average(fd); them(A + u * af + n * truoc, A + u * af + n * d, con); con -= h.Count; truoc = d; }
                }
                foreach (List<Point3d> h in hang)
                {
                    double d = h.Average(fd);
                    foreach (int sg in new[] { 1, -1 })
                    {
                        // Đầu phun đứng ngay trên ống góp (lệch ≤ saiSo) nối thẳng vào ống góp, không thuộc nhánh nào.
                        List<double> x = h.Select(fa).Where(a => (a - af) * sg > saiSo).OrderBy(a => Math.Abs(a - af)).ToList();
                        double truoc = af;
                        for (int i = 0; i < x.Count; i++) { them(A + u * truoc + n * d, A + u * x[i] + n * d, x.Count - i); truoc = x[i]; }
                    }
                }
                return kq;
            }
            if (cao - thap > 1e-6) kq.Add(new List<Point3d> { A + u * af + n * thap, A + u * af + n * cao });
            if (ngoai(af) > 1e-6) kq.Add(new List<Point3d> { A + u * af, ChanTruc(truc, A + u * af) });
            foreach (List<Point3d> h in hang)
            {
                double d = h.Average(fd), a0 = Math.Min(h.Min(fa), af), a1 = Math.Max(h.Max(fa), af);
                if (a1 - a0 > 1e-6) kq.Add(new List<Point3d> { A + u * a0 + n * d, A + u * a1 + n * d });
            }
            return kq;
        }

        /// Mỗi cột thiết bị (cột = cùng vị trí dọc ống chính) một nhánh vuông góc ống chính, từ ống chính tới thiết bị xa nhất của cột
        /// (ống chính chạy sát hoặc xuyên qua phòng thì nhánh đi cả hai phía). Chân nhánh ra ngoài ống chính thì nối thêm như xuong_ca:
        /// các chân cùng phía cùng điểm nối chỉ một đoạn nối, từ chân xa nhất (không vẽ chồng).
        static List<List<Point3d>> TuyenCot(List<Point3d> ps, List<Point3d> truc, double saiSo)
        {
            Point3d tam = new Point3d(ps.Average(p => p.X), ps.Average(p => p.Y), 0);
            int i0 = DoanGan(truc, tam);
            Point3d A = truc[i0], B = truc[i0 + 1];
            Vector3d u = (B - A).GetNormal(), n = new Vector3d(-u.Y, u.X, 0);
            Func<Point3d, double> fa = p => (p - A).DotProduct(u), fd = p => (p - A).DotProduct(n);
            List<List<Point3d>> kq = new List<List<Point3d>>(), noi = new List<List<Point3d>>();
            foreach (List<Point3d> c in Gom(ps, fa, saiSo))
            {
                double x = c.Average(fa), thap = Math.Min(0, c.Min(fd)), cao = Math.Max(0, c.Max(fd));
                if (cao - thap > 1e-6) kq.Add(new List<Point3d> { A + u * x + n * thap, A + u * x + n * cao });
                Point3d F = A + u * x, T = ChanTruc(truc, F);
                if (F.DistanceTo(T) <= 1e-6) continue;
                // Cùng điểm nối T, cùng phía và T nằm trên đường trục (F, F' và T thẳng hàng): giữ đoạn dài hơn, đoạn dài phủ đoạn ngắn.
                // T ngoài đường trục quá saiSo (đỉnh của ống chính có bậc; bậc vẽ tay vài phần mm vẫn coi là trên trục) thì mỗi chân một đoạn nối, không thì nhánh ngắn bị treo.
                int j = Math.Abs(fd(T)) > saiSo ? -1 : noi.FindIndex(d => d[1].DistanceTo(T) <= 1e-6 && (d[0] - T).DotProduct(F - T) > 0);
                if (j < 0) noi.Add(new List<Point3d> { F, T });
                else if (F.DistanceTo(T) > noi[j][0].DistanceTo(T)) noi[j] = new List<Point3d> { F, T };
            }
            kq.AddRange(noi);
            return kq;
        }

        /// Mỗi điểm một đoạn vuông góc vào đoạn ống chính gần nhất (đầu phun hành lang, tủ vách tường).
        static List<List<Point3d>> TuyenNhanh(List<Point3d> ps, List<Point3d> truc)
        {
            List<List<Point3d>> kq = new List<List<Point3d>>();
            foreach (Point3d p in ps)
            {
                int i = DoanGan(truc, p);
                Point3d c = ChanVuongGoc(p, truc[i], truc[i + 1]);
                if (p.DistanceTo(c) > 1e-6) kq.Add(new List<Point3d> { p, c });
            }
            return kq;
        }

        /// Ghi chữ (vd DN50) dọc từng đoạn thẳng đủ dài, lệch về một phía của ống; chữ luôn đọc từ trái sang hoặc từ dưới lên.
        /// Bỏ chữ khi trong bán kính (cỡ chữ × số ký tự) đã có chữ cùng nội dung (daCo: điểm đặt các chữ đó, chữ mới ghi vào đây).
        static void NhanDoan(GoiSpec g, List<Point3d> d, string chu, double cao, ObjectId layer, List<Point3d> daCo)
        {
            for (int i = 0; i + 1 < d.Count; i++)
            {
                Vector3d v = d[i + 1] - d[i];
                if (v.Length < cao * chu.Length * 1.5) continue;
                double a = Math.Atan2(v.Y, v.X);
                if (a > Math.PI / 2 + 1e-9) a -= Math.PI;
                else if (a <= -Math.PI / 2 + 1e-9) a += Math.PI;
                Point3d giua = d[i] + v * 0.5 + new Vector3d(-Math.Sin(a), Math.Cos(a), 0) * (cao * 0.9);
                if (daCo.Any(p => p.DistanceTo(giua) < cao * chu.Length)) continue;
                daCo.Add(giua);
                Them(g, TaoChu(g, giua, chu, cao, a * 180 / Math.PI, true, layer));
            }
        }

        /// Điểm đặt mọi chữ có nội dung "chu" (không phân biệt hoa thường) đang có trong không gian vẽ.
        static List<Point3d> ChuCungNoiDung(GoiSpec g, string chu)
        {
            List<Point3d> kq = new List<Point3d>();
            foreach (ObjectId id in g.Ms)
            {
                if (id.ObjectClass.DxfName != "MTEXT" && id.ObjectClass.DxfName != "TEXT") continue;
                Entity e = g.Tr.GetObject(id, OpenMode.ForRead) as Entity;
                MText mt = e as MText;
                DBText t = e as DBText;
                string s = mt != null ? mt.Text : t != null ? t.TextString : null;
                if (s != null && string.Equals(s.Trim(), chu, StringComparison.OrdinalIgnoreCase)) kq.Add(mt != null ? mt.Location : t.Position);
            }
            return kq;
        }

        static Point3d? DiemTuyChon(GoiSpec g, Dictionary<string, object> o, string k)
        {
            return Lay(o, k) == null ? (Point3d?)null : P(g, Lay(o, k), k);
        }

        static void OpTuyen(GoiSpec g, Dictionary<string, object> o)
        {
            string ma = LayChu(o, "ma", ""), kieu = LayChu(o, "kieu", "noi_tiep");
            VatTu v = TimMa(g.Dm, ma);
            if (v == null || v.loai != "tuyen") throw new System.Exception("mã ống/dây không có trong danh mục: " + ma);
            List<List<Point3d>> nhom = new List<List<Point3d>>();
            List<List<Point2d>> vungNhom = new List<List<Point2d>>(); // đa giác vùng của từng nhóm; null khi điểm lấy từ "p"
            if (Lay(o, "p") != null) { nhom.Add(DsP(g, Lay(o, "p"), "p")); vungNhom.Add(null); }
            else if (g.DiemRai != null)
                for (int j = 0; j < g.DiemRai.Count; j++)
                    if (g.DiemRai[j].Count > 0) { nhom.Add(g.DiemRai[j]); vungNhom.Add(g.VungRai[j]); }
            if (nhom.Count == 0) throw new System.Exception("tuyen cần \"p\" [[x,y],...] hoặc đứng ngay sau một thao tác rai có thiết bị");
            List<Point3d> truc = Lay(o, "truc") == null ? null : DsP(g, Lay(o, "truc"), "truc");
            if ((kieu == "xuong_ca" || kieu == "nhanh" || kieu == "cot") && (truc == null || truc.Count < 2))
                throw new System.Exception("tuyen kiểu \"" + kieu + "\" cần \"truc\" [[x,y],[x,y],...] là ống chính");
            Point3d? tu = DiemTuyChon(g, o, "tu"), den = DiemTuyChon(g, o, "den");
            double m = HeSoMet(g.Db), saiSo = LaySo(o, "sai_so", 0.1 / m);
            // "qua": một hoặc nhiều điểm; mỗi điểm dùng cho vùng chứa nó (điểm lấy từ "p" thì một nhóm, nhận mọi điểm).
            Point3d?[] qua = new Point3d?[nhom.Count];
            if (Lay(o, "qua") != null)
                foreach (Point3d q in DsP(g, Lay(o, "qua"), "qua"))
                {
                    int j = -1;
                    for (int k = 0; k < nhom.Count && j < 0; k++) if (vungNhom[k] == null || TrongDaGiac(vungNhom[k], new Point2d(q.X, q.Y))) j = k;
                    if (j < 0) throw new System.Exception(string.Format(CultureInfo.InvariantCulture, "điểm \"qua\" [{0},{1}] không nằm trong vùng nào của rai trước đó", Lt(q.X - g.Gx), Lt(q.Y - g.Gy)));
                    if (qua[j].HasValue) throw new System.Exception("hai điểm \"qua\" cùng nằm trong một vùng: mỗi vùng một điểm");
                    qua[j] = q;
                }

            List<List<Point3d>> doan = new List<List<Point3d>>();
            if (kieu == "noi_tiep") doan = TuyenNoiTiep(nhom, tu, den, saiSo);
            else if (kieu == "xuong_ca") for (int j = 0; j < nhom.Count; j++) doan.AddRange(TuyenXuongCa(nhom[j], truc, tu, qua[j], saiSo));
            else if (kieu == "nhanh") foreach (List<Point3d> ps in nhom) doan.AddRange(TuyenNhanh(ps, truc));
            else if (kieu == "cot") foreach (List<Point3d> ps in nhom) doan.AddRange(TuyenCot(ps, truc, saiSo));
            else if (kieu == "tia")
            {
                // Đèn báo phòng: phòng có bao nhiêu đầu báo thì bấy nhiêu nét thẳng từ đầu báo ra đèn (07_CAD/01_TRI_THUC_VE/ve/tu-duy-di-day-di-ong.md).
                if (!den.HasValue) throw new System.Exception("tuyen kiểu \"tia\" cần \"den\" [x,y] (vd vị trí đèn báo phòng)");
                foreach (List<Point3d> ps in nhom)
                    foreach (Point3d p in ps)
                        if (p.DistanceTo(den.Value) > 1e-6) doan.Add(new List<Point3d> { p, den.Value });
            }
            else throw new System.Exception("tuyen: \"kieu\" phải là noi_tiep, tia, xuong_ca, cot hoặc nhanh");

            ObjectId layer = DamBaoLayer(g.Db, g.Tr, TIEN_TO_LAYER + v.ma, MauHe(v.he));
            bool nhan = LayCo(o, "nhan", false);
            ObjectId lNhan = nhan ? DamBaoLayer(g.Db, g.Tr, LAYER_NHAN, 7) : ObjectId.Null;
            string chu = v.ma.StartsWith("ONG_", StringComparison.OrdinalIgnoreCase) ? v.ma.Substring(4) : v.ma;
            double caoChu = LaySo(o, "cao", LaySo(NhomKyHieu("chu"), "cao", 250)), dai = 0;
            List<Point3d> daCo = nhan ? ChuCungNoiDung(g, chu) : null;
            foreach (List<Point3d> d in doan)
            {
                Polyline pl = TaoPline(d, false, layer);
                Them(g, pl);
                dai += pl.Length;
                if (nhan) NhanDoan(g, d, chu, caoChu, lNhan, daCo);
            }
            g.KqTuyen.Add(new Dictionary<string, object> { { "kieu", kieu }, { "doan", doan.Count }, { "m", Math.Round(dai * m, 2) } });
        }

        // ---------- Lệnh gõ tay ----------

        /// PCCC_TUYEN: ống nhánh sprinkler kiểu xương cá cho một phòng. Chọn đầu phun, chọn ống chính, rồi BẤM chỗ đặt ống góp
        /// (người dùng dặn 2026-11-11: tự chọn vị trí ống góp, không để máy chọn cột đầu phun).
        static void TuyenTay()
        {
            Document doc = AcApp.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;
            FileDanhMuc dm = DocDanhMuc();
            // Người dùng 16/11 (ý từ video VPTTOOL): TuDong = mỗi đoạn lấy DN theo số đầu phun phía sau, bảng 03_DU_LIEU/ong_nhanh_dn.json, chọn mức áp suất đầu phun.
            PromptStringOptions o0 = new PromptStringOptions("\nMã ống nhánh (vd ONG_DN25), TuDong = DN theo số đầu phun phía sau, ? = danh sách: ");
            o0.AllowSpaces = false;
            PromptResult r0 = ed.GetString(o0);
            if (r0.Status != PromptStatus.OK || r0.StringResult.Trim() == "") return;
            string s0 = r0.StringResult.Trim();
            bool tuDong = s0.Equals("TuDong", StringComparison.OrdinalIgnoreCase) || s0.Equals("T", StringComparison.OrdinalIgnoreCase);
            VatTu v = null;
            string muc = "";
            List<KeyValuePair<string, int>> bang = null;
            if (tuDong)
            {
                PromptKeywordOptions ko = new PromptKeywordOptions("\nÁp suất đầu phun: Den05 = đến 0,5 MPa (thường dùng), Tren05 = 0,5 MPa trở lên");
                ko.Keywords.Add("Den05"); ko.Keywords.Add("Tren05");
                ko.Keywords.Default = "Den05";
                ko.AllowNone = true;
                PromptResult kr = ed.GetKeywords(ko);
                if (kr.Status == PromptStatus.Cancel) return;
                muc = kr.StringResult == "Tren05" ? "Tren05" : "Den05";
                string tenMuc;
                bang = BangDn(muc, out tenMuc);
                ed.WriteMessage("\n[TKPCCC] Cỡ ống theo bảng ong_nhanh_dn.json, mức \"" + tenMuc + "\": "
                    + string.Join(", ", bang.Where(x => x.Value != 0).Select(x => x.Key.Substring(4) + (x.Value < 0 ? " trên" : " ≤ " + x.Value))) + " đầu phun.");
            }
            else
            {
                v = dm.vat_tu.FirstOrDefault(x => x.loai == "tuyen" && string.Equals(x.ma, s0, StringComparison.OrdinalIgnoreCase));
                if (v == null) v = HoiMa(ed, dm, "tuyen", "Mã ống nhánh (vd ONG_DN25)");
                if (v == null) return;
            }
            PromptSelectionOptions c1 = new PromptSelectionOptions();
            c1.MessageForAdding = "\nChọn các đầu phun của một phòng: ";
            PromptSelectionResult r1 = ed.GetSelection(c1, new SelectionFilter(new TypedValue[] { new TypedValue((int)DxfCode.Start, "INSERT") }));
            if (r1.Status != PromptStatus.OK) return;
            // Chọn bằng GetSelection (không GetEntity) để kịch bản accoreconsole trả lời được bằng "L".
            PromptSelectionOptions c2 = new PromptSelectionOptions();
            c2.MessageForAdding = "\nChọn ống chính (polyline hoặc line): ";
            c2.SingleOnly = true;
            PromptSelectionResult r2 = ed.GetSelection(c2, new SelectionFilter(new TypedValue[] { new TypedValue((int)DxfCode.Start, "LWPOLYLINE,LINE") }));
            if (r2.Status != PromptStatus.OK || r2.Value.Count == 0) return;
            PromptPointOptions po = new PromptPointOptions("\nBấm vị trí ống góp (Enter = cột đầu phun gần giữa phòng): ");
            po.AllowNone = true;
            PromptPointResult pr = ed.GetPoint(po);
            if (pr.Status != PromptStatus.OK && pr.Status != PromptStatus.None) return;
            Point3d? qua = pr.Status == PromptStatus.OK ? pr.Value.TransformBy(ed.CurrentUserCoordinateSystem) : (Point3d?)null;

            double m = HeSoMet(db), dai = 0;
            int soDoan = 0;
            HashSet<string> thay = new HashSet<string>();
            Dictionary<string, int> theoMa = new Dictionary<string, int>(); // TuDong: số đoạn theo mã ống
            List<List<Point2d>> vungCo = VungCoSan(db);
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                List<Point3d> ps, truc = new List<Point3d>();
                List<KeyValuePair<Point3d, string>> dau = new List<KeyValuePair<Point3d, string>>();
                BoiCanh bc = new BoiCanh { Tr = tr, Dm = DocDanhMuc(), Alias = DocAlias() };
                foreach (ObjectId id in r1.Value.GetObjectIds())
                {
                    BlockReference br = tr.GetObject(id, OpenMode.ForRead) as BlockReference;
                    // Chỉ block là thiết bị (có mã vật tư): quét cửa sổ quanh phòng không lấy nhầm đồ nội thất.
                    string ma = br == null || br is Table ? null : MaThietBi(br, bc);
                    if (ma != null) dau.Add(new KeyValuePair<Point3d, string>(new Point3d(br.Position.X, br.Position.Y, 0), ma));
                }
                int boDau, soNhom;
                ps = LocDauPhun(dau, vungCo, 0.6 / m, out boDau, out soNhom);
                if (boDau > 0)
                    ed.WriteMessage("\n[TKPCCC] tuyen_loc giu=" + ps.Count + " bo=" + boDau + " nhom=" + soNhom
                        + " (đầu phun khác loại hoặc ở vùng " + LAYER_VUNG + " tách rời: chỉ nối nhóm đông nhất; cần nối nhóm khác thì chọn riêng).");
                Entity e = (Entity)tr.GetObject(r2.Value.GetObjectIds()[0], OpenMode.ForRead);
                Polyline pl = e as Polyline;
                Line ln = e as Line;
                if (pl != null) for (int i = 0; i < pl.NumberOfVertices; i++) truc.Add(pl.GetPoint3dAt(i));
                else if (ln != null) { truc.Add(ln.StartPoint); truc.Add(ln.EndPoint); }
                if (ps.Count == 0 || truc.Count < 2) throw new System.Exception("cần ít nhất một đầu phun (block) và một ống chính có 2 điểm trở lên");
                BlockTableRecord ms = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);
                List<int> dem = tuDong ? new List<int>() : null;
                List<List<Point3d>> ds = TuyenXuongCa(ps, truc, null, qua, 0.1 / m, dem);
                ObjectId lNhan = tuDong ? DamBaoLayer(db, tr, LAYER_NHAN, 7) : ObjectId.Null;
                double cao = LaySo(NhomKyHieu("chu"), "cao", 250) * 0.001 / m;
                string maTruoc = null;
                Point3d? cuoiTruoc = null;
                for (int i = 0; i < ds.Count; i++)
                {
                    List<Point3d> d = ds[i];
                    VatTu vd = tuDong ? MaDn(dm, bang, dem[i], thay) : v;
                    Polyline ong = TaoPline(d, false, DamBaoLayer(db, tr, TIEN_TO_LAYER + vd.ma, MauHe(vd.he)));
                    ms.AppendEntity(ong);
                    tr.AddNewlyCreatedDBObject(ong, true);
                    dai += ong.Length;
                    soDoan++;
                    if (!tuDong) continue;
                    theoMa[vd.ma] = (theoMa.ContainsKey(vd.ma) ? theoMa[vd.ma] : 0) + 1;
                    // Chữ DN ở đoạn đầu mỗi tuyến và chỗ đổi cỡ (đoạn nối tiếp cùng cỡ không ghi lại).
                    if (!cuoiTruoc.HasValue || cuoiTruoc.Value.DistanceTo(d[0]) > 1e-6 || vd.ma != maTruoc)
                        ChuDn(tr, ms, d[0], d[d.Count - 1], vd.ma.StartsWith("ONG_", StringComparison.OrdinalIgnoreCase) ? vd.ma.Substring(4) : vd.ma, cao, lNhan);
                    maTruoc = vd.ma;
                    cuoiTruoc = d[d.Count - 1];
                }
                tr.Commit();
            }
            foreach (string t in thay) ed.WriteMessage("\n[TKPCCC] " + t);
            ed.WriteMessage("\n[TKPCCC] PCCC_TUYEN " + (tuDong ? "TuDong " + muc : v.ma) + " doan=" + soDoan + " m=" + Math.Round(dai * m, 2).ToString(CultureInfo.InvariantCulture)
                + (tuDong ? " " + string.Join(" ", bang.Where(x => theoMa.ContainsKey(x.Key)).Select(x => x.Key.Substring(4) + "=" + theoMa[x.Key])) : ""));
        }

        /// PCCC_TUYEN (KOCHAM 16/11: khung chọn phòng chữ L dính đầu phun WC, hành lang): nhóm đầu phun theo (cụm vùng PCCC_VUNG có hộp bao cách nhau ≤ ke,
        /// mã thiết bị), giữ nhóm đông nhất; đầu phun ngoài mọi vùng cùng mã thì giữ. Khoang dầm kề nhau (qua dầm ≤ ke) là một cụm.
        static List<Point3d> LocDauPhun(List<KeyValuePair<Point3d, string>> dau, List<List<Point2d>> vung, double ke, out int bo, out int soNhom)
        {
            int n = vung.Count;
            double[][] hb = vung.Select(d => new[] { d.Min(p => p.X), d.Min(p => p.Y), d.Max(p => p.X), d.Max(p => p.Y) }).ToArray();
            int[] cha = Enumerable.Range(0, n).ToArray();
            Func<int, int> goc = null;
            goc = i => cha[i] == i ? i : (cha[i] = goc(cha[i]));
            for (int i = 0; i < n; i++)
                for (int j = i + 1; j < n; j++)
                    if (hb[i][0] <= hb[j][2] + ke && hb[j][0] <= hb[i][2] + ke && hb[i][1] <= hb[j][3] + ke && hb[j][1] <= hb[i][3] + ke) cha[goc(i)] = goc(j);
            int[] nhom = dau.Select(kv =>
            {
                Point2d q = new Point2d(kv.Key.X, kv.Key.Y);
                for (int i = 0; i < n; i++) if (TrongDaGiac(vung[i], q)) return goc(i);
                return -1;
            }).ToArray();
            Dictionary<string, int> dem = new Dictionary<string, int>();
            for (int i = 0; i < dau.Count; i++) { string k = nhom[i] + "|" + dau[i].Value; int c; dem.TryGetValue(k, out c); dem[k] = c + 1; }
            soNhom = dem.Count;
            if (dem.Count == 0) { bo = 0; return new List<Point3d>(); }
            string tot = dem.OrderByDescending(kv => kv.Key.StartsWith("-1|") ? 0 : kv.Value).ThenByDescending(kv => kv.Value).First().Key;
            int gTot = int.Parse(tot.Substring(0, tot.IndexOf('|')), CultureInfo.InvariantCulture);
            string maTot = tot.Substring(tot.IndexOf('|') + 1);
            List<Point3d> kq = Enumerable.Range(0, dau.Count).Where(i => dau[i].Value == maTot && (nhom[i] == gTot || nhom[i] == -1)).Select(i => dau[i].Key).ToList();
            bo = dau.Count - kq.Count;
            return kq;
        }

        /// Bảng DN theo số đầu phun (03_DU_LIEU/ong_nhanh_dn.json cạnh DLL), mức "Den05" / "Tren05": { mã ống theo DN tăng dần, số đầu tối đa } (0 = cỡ không dùng, -1 = không giới hạn).
        static List<KeyValuePair<string, int>> BangDn(string muc, out string ten)
        {
            string f = Path.Combine(ThuMuc, "ong_nhanh_dn.json");
            if (!File.Exists(f)) throw new System.Exception("Thiếu " + f + ": chạy lại 07_CAD\\build.bat.");
            Dictionary<string, object> d;
            try { d = new JavaScriptSerializer().DeserializeObject(File.ReadAllText(f, Encoding.UTF8)) as Dictionary<string, object>; }
            catch (System.Exception ex) { throw new System.Exception("ong_nhanh_dn.json sai cú pháp: " + ex.Message); }
            Dictionary<string, object> cacMuc = d == null ? null : Lay(d, "muc") as Dictionary<string, object>;
            Dictionary<string, object> mm = cacMuc == null ? null : Lay(cacMuc, muc) as Dictionary<string, object>;
            IList ma = d == null ? null : Lay(d, "ma") as IList, td = mm == null ? null : Lay(mm, "toi_da") as IList;
            if (ma == null || td == null || ma.Count != td.Count) throw new System.Exception("ong_nhanh_dn.json: thiếu mức \"" + muc + "\", hoặc \"ma\" và \"toi_da\" lệch số cột.");
            ten = LayChu(mm, "ten", muc);
            List<KeyValuePair<string, int>> kq = new List<KeyValuePair<string, int>>();
            for (int i = 0; i < ma.Count; i++) kq.Add(new KeyValuePair<string, int>(Convert.ToString(ma[i]), td[i] == null ? -1 : Convert.ToInt32(td[i], CultureInfo.InvariantCulture)));
            return kq;
        }

        /// Cỡ nhỏ nhất chở được soDau đầu phun; mã chưa có trong danh mục (vd ONG_DN20) thì lấy cỡ lớn kế tiếp có trong danh mục, ghi vào thay.
        static VatTu MaDn(FileDanhMuc dm, List<KeyValuePair<string, int>> bang, int soDau, HashSet<string> thay)
        {
            int i = bang.FindIndex(x => x.Value != 0 && (x.Value < 0 || x.Value >= soDau));
            if (i < 0) i = bang.Count - 1;
            for (int j = i; j < bang.Count; j++)
            {
                VatTu v = dm.vat_tu.FirstOrDefault(x => x.loai == "tuyen" && string.Equals(x.ma, bang[j].Key, StringComparison.OrdinalIgnoreCase));
                if (v == null) continue;
                if (j > i) thay.Add(bang[i].Key + " chưa có trong vat_tu.json: dùng " + v.ma + " (thêm mã vào 03_DU_LIEU/vat_tu.json nếu cần đúng cỡ).");
                return v;
            }
            throw new System.Exception("Không mã ống nào từ " + bang[i].Key + " trở lên có trong vat_tu.json.");
        }

        /// Chữ DN song song đoạn ống, ở giữa đoạn, phía trên (góc đọc được: -90° < góc ≤ 90°), layer PCCC_NHAN (PCCC_BOC không đếm).
        static void ChuDn(Transaction tr, BlockTableRecord ms, Point3d a, Point3d b, string chu, double cao, ObjectId lop)
        {
            Vector3d t = b - a;
            if (t.Length < 1e-9) return;
            double g = Math.Atan2(t.Y, t.X);
            if (g > Math.PI / 2 + 1e-9) g -= Math.PI; else if (g <= -Math.PI / 2 + 1e-9) g += Math.PI;
            MText c = new MText();
            c.Contents = chu;
            c.TextHeight = cao;
            c.Attachment = AttachmentPoint.BottomCenter;
            c.Rotation = g;
            c.Location = a + t * 0.5 + new Vector3d(-Math.Sin(g), Math.Cos(g), 0) * cao * 0.4;
            c.LayerId = lop;
            ms.AppendEntity(c);
            tr.AddNewlyCreatedDBObject(c, true);
        }

    }
}
