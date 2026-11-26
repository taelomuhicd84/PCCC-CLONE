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
                // Gói spec trước còn chạy (in ảnh, nhập pdf, lệnh PCCC_* có bơm thông điệp): không chạy lồng vào giữa nó.
                if (Lenh.DangSpec || (doc.CommandInProgress ?? "").StartsWith("PCCC", StringComparison.OrdinalIgnoreCase)) return;
                // Người dùng 26/11: "ra lệnh ở tab đó, qua tab khác thao tác, tab cũ vẫn thực thi". Trước đây tab đang hiện dở lệnh (CMDACTIVE) là
                // cả hàng đợi đứng, việc của tab cũ chờ tới khi máy chủ MCP báo "đang bận". Nay chỉ việc của tab đang hiện mới chờ lệnh của người dùng;
                // việc khoá bản vẽ khác chạy nền ngay (Lenh.ChayNen), chỉ chờ khi chính bản đó còn dở lệnh.
                bool banHien = Convert.ToInt32(AcApp.GetSystemVariable("CMDACTIVE")) != 0;
                foreach (string f in Directory.GetFiles(HangDoi.Vao, "*.json").OrderBy(x => x))
                {
                    if (!BanQuyen.ChoChay())
                    {
                        // Trả lỗi ngay để máy chủ MCP không chờ tới hết giờ.
                        HangDoi.GhiAnToan(Path.Combine(HangDoi.Ra, Path.GetFileName(f)), "{\"ok\":false,\"ve\":0,\"xoa\":0,\"loi\":[{\"i\":-1,\"op\":\"spec\",\"ly_do\":\"Chưa có bản quyền TKPCCC: gõ PCCC_BANQUYEN trong AutoCAD.\"}]}");
                        File.Delete(f);
                        return;
                    }
                    // Việc khoá "ban_ve" (Bot chat gắn bản vẽ lúc gửi lời nhắn): chạy trên đúng bản vẽ đó dù người dùng đã sang tab khác.
                    Document dich = doc;
                    string banVe = BanVeCuaViec(f);
                    if (banVe != null)
                    {
                        dich = Lenh.TimBanVe(banVe);
                        if (dich == null)
                        {
                            HangDoi.GhiAnToan(Path.Combine(HangDoi.Ra, Path.GetFileName(f)), new JavaScriptSerializer().Serialize(new Dictionary<string, object> {
                                { "ok", false }, { "ve", 0 }, { "xoa", 0 }, { "loi", new object[] { new Dictionary<string, object> { { "i", -1 }, { "op", "spec" },
                                { "ly_do", "bản vẽ \"" + banVe + "\" không còn mở (đã đóng hoặc lưu tên khác): không vẽ gì. Bản vẽ đang hiện: \"" + Path.GetFileName(doc.Name) + "\"" } } } } }));
                            File.Delete(f);
                            return;
                        }
                    }
                    bool laHien = dich.UnmanagedObject == doc.UnmanagedObject;
                    // Bản đích đang dở lệnh: việc này chờ nhịp sau (máy chủ MCP tự báo "đang bận" khi quá giờ), xét việc kế trong hàng.
                    if (laHien ? banHien : (dich.CommandInProgress ?? "") != "") continue;
                    string dang = Path.Combine(HangDoi.Dang, Path.GetFileName(f));
                    File.Move(f, dang); // chuyển trước khi chạy để một việc không chạy hai lần
                    if (laHien) doc.SendStringToExecute("PCCC_SPEC\n" + dang + "\n", true, false, false);
                    else
                    {
                        string kq = Lenh.ChayNen(dich, dang);
                        if (!banHien) doc.Editor.WriteMessage(kq + "\n"); // người dùng đang dở lệnh: không chen chữ vào dấu nhắc của họ
                    }
                    return;
                }
            }
            catch (System.Exception) { } // nhịp sau thử lại; lỗi nền không được làm phiền người đang vẽ
        }

        /// "ban_ve" của gói việc; null = không khoá hoặc file hỏng (PCCC_SPEC tự báo lỗi cú pháp).
        static string BanVeCuaViec(string f)
        {
            try
            {
                Dictionary<string, object> d = new JavaScriptSerializer { MaxJsonLength = int.MaxValue }.DeserializeObject(File.ReadAllText(f)) as Dictionary<string, object>;
                object v;
                return d != null && d.TryGetValue("ban_ve", out v) && v is string && (string)v != "" ? (string)v : null;
            }
            catch (System.Exception) { return null; }
        }
    }
}
