// Hàng đợi bằng file giữa máy chủ MCP (cad/mcp) và plugin AutoCAD (cad/src). Biên dịch chung vào cả hai.
// vao = việc chờ, dang = plugin đã nhận, ra = kết quả, song.json = plugin còn sống. Không mở cổng mạng.
using System;
using System.IO;
using System.Text;

namespace TKPCCC
{
    public static class HangDoi
    {
        /// Mặc định %LOCALAPPDATA%\TKPCCC\cad; đặt biến môi trường TKPCCC_CAD_HANG_DOI để đổi (bộ test dùng thư mục tạm).
        public static string Goc
        {
            get
            {
                string e = Environment.GetEnvironmentVariable("TKPCCC_CAD_HANG_DOI");
                if (!string.IsNullOrEmpty(e)) return Path.GetFullPath(e).TrimEnd('\\');
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TKPCCC", "cad");
            }
        }
        public static string Vao { get { return Path.Combine(Goc, "vao"); } }
        public static string Dang { get { return Path.Combine(Goc, "dang"); } }
        public static string Ra { get { return Path.Combine(Goc, "ra"); } }
        public static string Song { get { return Path.Combine(Goc, "song.json"); } }

        public static void TaoThuMuc()
        {
            Directory.CreateDirectory(Vao);
            Directory.CreateDirectory(Dang);
            Directory.CreateDirectory(Ra);
        }

        /// Ghi qua file tạm rồi đổi tên: bên đọc không bao giờ thấy file viết dở.
        public static void GhiAnToan(string f, string noiDung)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(f));
            string tam = f + ".tam";
            File.WriteAllText(tam, noiDung, new UTF8Encoding(false));
            if (File.Exists(f)) File.Replace(tam, f, null); else File.Move(tam, f);
        }

        /// brave.exe (cài cho mọi người hoặc cho riêng người dùng), không có thì null. Dùng chung Hub + phân hệ YouTube (file này biên dịch vào cả hai).
        public static string TimBrave()
        {
            string duoi = Path.Combine("BraveSoftware", "Brave-Browser", "Application", "brave.exe");
            foreach (Environment.SpecialFolder g in new[] { Environment.SpecialFolder.ProgramFiles, Environment.SpecialFolder.ProgramFilesX86, Environment.SpecialFolder.LocalApplicationData })
            {
                string f = Path.Combine(Environment.GetFolderPath(g), duoi);
                if (File.Exists(f)) return f;
            }
            return null;
        }
    }
}
