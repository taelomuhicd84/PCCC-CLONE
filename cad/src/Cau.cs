// Cầu nối MCP -> AutoCAD đang mở: cứ 250 ms xem hàng đợi, có việc và AutoCAD đang rảnh thì gọi PCCC_SPEC trên luồng chính.
// Chỉ bật trong acad.exe (KhoiDong.Initialize); accoreconsole không có vòng lặp thông điệp nên không dùng.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.Script.Serialization;
using Autodesk.AutoCAD.ApplicationServices;
using AcApp = Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace TKPCCC
{
    public static class Cau
    {
        static System.Windows.Forms.Timer dongHo;
        static int nhip;

        public static void BatDau()
        {
            if (dongHo != null) return;
            HangDoi.TaoThuMuc();
            dongHo = new System.Windows.Forms.Timer();
            dongHo.Interval = 250;
            dongHo.Tick += Nhip;
            dongHo.Start();
        }

        static void Nhip(object s, EventArgs e)
        {
            try
            {
                Document doc = AcApp.DocumentManager.MdiActiveDocument;
                if (nhip++ % 8 == 0) // khoảng 2 giây một lần: báo còn sống cho máy chủ MCP
                    HangDoi.GhiAnToan(HangDoi.Song, new JavaScriptSerializer().Serialize(new Dictionary<string, object> {
                        { "luc", DateTime.UtcNow.ToString("o") }, { "ban_ve", doc == null ? "" : Path.GetFileName(doc.Name) } }));
                if (doc == null) return;
                string f = Directory.GetFiles(HangDoi.Vao, "*.json").OrderBy(x => x).FirstOrDefault();
                if (f == null) return;
                // Người dùng đang dở lệnh khác: chờ. Máy chủ MCP tự báo "đang bận" khi quá giờ.
                if (Convert.ToInt32(AcApp.GetSystemVariable("CMDACTIVE")) != 0) return;
                string dang = Path.Combine(HangDoi.Dang, Path.GetFileName(f));
                File.Move(f, dang); // chuyển trước khi chạy để một việc không chạy hai lần
                doc.SendStringToExecute("PCCC_SPEC\n" + dang + "\n", true, false, false);
            }
            catch (System.Exception) { } // nhịp sau thử lại; lỗi nền không được làm phiền người đang vẽ
        }
    }
}
