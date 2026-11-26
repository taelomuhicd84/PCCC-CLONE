// Khởi động TKPCCC.UI.dll và lệnh mở palette. Tách từ BangBlock.cs (26/11/2026: mỗi thư mục số của 07_CAD là một hạng mục tính năng, xem 07_CAD/README.md).
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Windows;
using AcApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace TKPCCC
{
    /// PCCC, PCCC_BLOCK, PCCC_HELP đều mở palette chính (BangChinh.cs), khác nhau ở phân hệ hiện ra.
    public class LenhUi
    {
        static void Mo(string trang, string lenh)
        {
            Document doc = AcApp.DocumentManager.MdiActiveDocument;
            try
            {
                string tb = BangChinh.Mo(trang);
                if (doc != null && lenh != null) doc.Editor.WriteMessage("\n[TKPCCC] " + lenh + ": " + tb);
            }
            catch (System.Exception ex) { if (doc != null) doc.Editor.WriteMessage("\n[TKPCCC] Lỗi: " + ex.Message); }
        }

        [CommandMethod("PCCC")] public void MoPccc() { Mo(null, "PCCC"); }
        [CommandMethod("PCCC_BLOCK")] public void MoBang() { Mo(BangChinh.THU_VIEN, "PCCC_BLOCK"); }
        [CommandMethod("PCCC_BOTRI")] public void MoBoTri() { Mo(BangChinh.BO_TRI, "PCCC_BOTRI"); }
        [CommandMethod("PCCC_TIENICH")] public void MoTienIch() { Mo(BangChinh.TIEN_ICH, "PCCC_TIENICH"); }

        /// PCCC_HELP (lệnh ở TKPCCC.Cad.dll) gọi qua Lenh.MoBangLenh.
        public static void MoBangLenh() { Mo(BangChinh.LENH, null); }
    }

    /// Nạp cùng TKPCCC.UI.dll: PCCC_HELP mở bảng lệnh, đăng ký phím tắt người dùng đã lưu.
    public class KhoiDongUi : IExtensionApplication
    {
        public void Initialize()
        {
            Lenh.MoBangLenh = LenhUi.MoBangLenh;
            Lenh.ChonVatTu = BangChonVatTu.Chon;
            Lenh.NenSang = GiaoDien.ChuDe != "toi"; // ảnh block vẽ đúng nền ngay lần đầu (bảng chọn mã có thể mở trước palette)
            string loi = PhimTat.Nap();
            Document doc = AcApp.DocumentManager.MdiActiveDocument;
            if (loi != "" && doc != null) doc.Editor.WriteMessage("\n[TKPCCC] Phím tắt bỏ qua: " + loi);
        }

        public void Terminate() { }
    }

}
