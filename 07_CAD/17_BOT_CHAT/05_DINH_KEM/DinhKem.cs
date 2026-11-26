// Đính kèm: dán / kéo thả ảnh, thẻ vùng chọn CAD (handle gửi kèm). Tách từ BotChat.cs (26/11/2026: mỗi thư mục số của 07_CAD là một hạng mục tính năng, xem 07_CAD/README.md).
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using AcAp = Autodesk.AutoCAD.ApplicationServices;
using Db = Autodesk.AutoCAD.DatabaseServices;

namespace TKPCCC
{
    partial class BotChat
    {
        static bool LaAnh(string f)
        {
            return new[] { ".png", ".jpg", ".jpeg", ".gif", ".bmp", ".webp" }.Contains(Path.GetExtension(f).ToLowerInvariant());
        }

        static bool daDon;

        /// Tên file mới trong thư mục ảnh đính kèm; lần đầu mỗi phiên AutoCAD dọn ảnh quá 7 ngày.
        static string FileAnhMoi(string duoi)
        {
            Directory.CreateDirectory(THU_MUC_ANH);
            if (!daDon)
            {
                daDon = true;
                foreach (string cu in Directory.GetFiles(THU_MUC_ANH))
                    if (File.GetLastWriteTimeUtc(cu) < DateTime.UtcNow.AddDays(-7)) try { File.Delete(cu); } catch (System.Exception) { }
            }
            return Path.Combine(THU_MUC_ANH, DateTime.Now.ToString("yyyyMMdd_HHmmss_fff") + duoi);
        }

        /// Chép ảnh vào thư mục ảnh đính kèm: AI chỉ cần một --add-dir.
        static string ChepAnh(string f)
        {
            string moi = FileAnhMoi(Path.GetExtension(f).ToLowerInvariant());
            File.Copy(f, moi);
            return moi;
        }

        void ThemAnh(string f)
        {
            anh.Add(f);
            VeChip();
        }

        /// Dán: ảnh trong clipboard (chụp màn hình) hoặc file ảnh đã copy. false = không có ảnh, dán chữ như thường.
        bool DanAnh()
        {
            try
            {
                if (Clipboard.ContainsImage())
                {
                    using (Image i = Clipboard.GetImage())
                    {
                        if (i == null) return false;
                        string f = FileAnhMoi(".png");
                        i.Save(f, ImageFormat.Png);
                        ThemAnh(f);
                    }
                    return true;
                }
                if (Clipboard.ContainsFileDropList())
                {
                    List<string> ds = Clipboard.GetFileDropList().Cast<string>().Where(LaAnh).ToList();
                    foreach (string f in ds) ThemAnh(ChepAnh(f));
                    return ds.Count > 0;
                }
            }
            catch (System.Exception ex) { trangThai.Text = "Không dán được ảnh: " + ex.Message; return true; }
            return false;
        }

        void ChonAnh()
        {
            using (OpenFileDialog d = new OpenFileDialog())
            {
                d.Filter = "Ảnh|*.png;*.jpg;*.jpeg;*.gif;*.bmp;*.webp";
                d.Multiselect = true;
                if (d.ShowDialog() == DialogResult.OK) foreach (string f in d.FileNames) ThemAnh(ChepAnh(f));
            }
        }

        /// Ảnh thu nhỏ cho chip (không giữ khoá file). null = không đọc được (vd webp).
        internal static Image ThuNho(string f)
        {
            try
            {
                using (Image i = Image.FromFile(f))
                {
                    double k = Math.Min(1, Math.Min(80.0 / i.Width, 60.0 / i.Height));
                    return new Bitmap(i, Math.Max(1, (int)(i.Width * k)), Math.Max(1, (int)(i.Height * k)));
                }
            }
            catch (System.Exception) { return null; }
        }

        void QuetVung()
        {
            AcAp.Document d = AcAp.Core.Application.DocumentManager.MdiActiveDocument;
            if (d == null) { trangThai.Text = "Chưa mở bản vẽ nào."; return; }
            d.SendStringToExecute("_TKBOTCHON ", true, false, false);
        }

        /// Gắn vùng chọn (TKBOTCHON: giu = true).
        public void DatVung(AcAp.Document d, Db.ObjectId[] ids, bool giu)
        {
            vungDoc = ids.Length > 0 ? d : null;
            vungId = ids.Length > 0 ? ids : null;
            vungGiu = giu;
            VeChip();
        }

        /// Đối tượng đang chọn trên bản vẽ đổi: có thì thành vùng chọn; bỏ chọn hết thì bỏ chip (trừ vùng TKBOTCHON).
        public void ChonDoi(AcAp.Document d, Db.ObjectId[] ids)
        {
            if (ids.Length > 0)
            {
                if (vungId != null && vungDoc == d && ids.SequenceEqual(vungId)) return;
                DatVung(d, ids, false);
            }
            else if (vungId != null && !vungGiu && vungDoc == d) DatVung(d, ids, false);
        }

        static string So(double x) { return Math.Round(x).ToString(CultureInfo.InvariantCulture); }

        /// Mô tả vùng chọn gửi kèm lời nhắn: tên bản vẽ, số đối tượng, hộp bao, thành phần (loại / block / layer), handle. "" = không có vùng.
        string MoTaVung(bool claude)
        {
            if (vungId == null) return "";
            AcAp.Document d = vungDoc;
            if (!AcAp.Core.Application.DocumentManager.Cast<AcAp.Document>().Contains(d)) return ""; // bản vẽ đã đóng
            List<Db.ObjectId> ids = vungId.Where(i => i.IsValid && !i.IsErased).ToList();
            if (ids.Count == 0) return "";
            Dictionary<string, int> dem = new Dictionary<string, int>();
            Db.Extents3d? hop = null;
            try
            {
                using (d.LockDocument())
                using (Db.Transaction tr = d.Database.TransactionManager.StartTransaction())
                {
                    foreach (Db.ObjectId id in ids)
                    {
                        Db.Entity e = tr.GetObject(id, Db.OpenMode.ForRead) as Db.Entity;
                        if (e == null) continue;
                        string k = id.ObjectClass.DxfName;
                        Db.BlockReference br = e as Db.BlockReference;
                        if (br != null)
                            k += " " + ((Db.BlockTableRecord)tr.GetObject(br.IsDynamicBlock ? br.DynamicBlockTableRecord : br.BlockTableRecord, Db.OpenMode.ForRead)).Name;
                        k += " (layer " + e.Layer + ")";
                        int c;
                        dem.TryGetValue(k, out c);
                        dem[k] = c + 1;
                        try
                        {
                            Db.Extents3d x = e.GeometricExtents;
                            if (hop.HasValue) { Db.Extents3d h = hop.Value; h.AddExtents(x); hop = h; } else hop = x;
                        }
                        catch (System.Exception) { }
                    }
                    tr.Commit();
                }
            }
            catch (System.Exception) { } // bản vẽ đang bận (lệnh đang chạy): vẫn gửi số đối tượng + handle
            StringBuilder s = new StringBuilder();
            s.Append("Vùng tôi đang quét chọn trên bản vẽ \"" + Path.GetFileName(d.Name) + "\": " + ids.Count + " đối tượng");
            if (hop.HasValue)
                s.Append(", hộp bao [" + So(hop.Value.MinPoint.X) + "," + So(hop.Value.MinPoint.Y) + "," + So(hop.Value.MaxPoint.X) + "," + So(hop.Value.MaxPoint.Y) + "]");
            s.Append(".\n");
            if (dem.Count > 0)
                s.Append("Thành phần: " + string.Join("; ", dem.OrderByDescending(x => x.Value).Take(15).Select(x => x.Value + " " + x.Key)) + (dem.Count > 15 ? "; ..." : "") + ".\n");
            s.Append("Handle: " + string.Join(",", ids.Take(TOI_DA_HANDLE).Select(i => i.Handle.ToString()))
                + (ids.Count > TOI_DA_HANDLE ? " (còn " + (ids.Count - TOI_DA_HANDLE) + " đối tượng, lọc thêm bằng hop + l)" : "") + "\n");
            if (claude)
                s.Append("Đọc chi tiết: cad_chay {\"ops\":[{\"op\":\"doc\",\"gi\":\"dt\",\"h\":[...]}]}; xem hình: op anh với hop trên. "
                    + "Sửa: op sua (h, doi, xoay, tl, tam, l_moi, mau, t), xoa (h) hoặc vẽ mới. Chỉ đụng tới vùng này trừ khi tôi bảo khác.\n");
            return s.Append("\n").ToString();
        }

        /// Đính kèm đi theo lời nhắn vừa gửi; hội thoại tiếp vẫn nhớ (--resume).
        void BoDinhKem()
        {
            anh.Clear();
            vungId = null;
            vungDoc = null;
            VeChip();
        }

    }
}
