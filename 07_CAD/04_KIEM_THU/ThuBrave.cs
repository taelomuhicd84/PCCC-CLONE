// Chỉ cho test/thu_brave.ps1 (người dùng 18/11): thử 2 việc của chế độ Brave ngoài AutoCAD, hồ sơ Brave tạm (không đụng Brave của người dùng).
// 1. Extension 18_YOUTUBE/brave_khung: trang gọi requestFullscreen thì phần tử phủ đúng khung trang (Brave chạy ẩn, đọc tiêu đề trang qua cổng gỡ lỗi).
// 2. GanBrave.cs: cửa sổ Brave nhận một Form làm chủ (owner); brave.exe lần hai chỉ chuyển link rồi thoát thì vẫn tìm ra Brave chính (WMI); Tha() trả về độc lập.
using System;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;

namespace TKPCCC
{
    static class ThuBrave
    {
        static int loi;
        const string TRANG = "<!doctype html><html><head><title>CHUA</title></head><body><div id=\"v\" style=\"width:100px;height:50px\">x</div><script>" +
            "document.addEventListener('fullscreenchange',function(){var e=document.fullscreenElement;document.title='FS:'+(e?e.id+':'+e.getBoundingClientRect().width+'='+innerWidth:'none');});" +
            "setTimeout(function(){document.getElementById('v').requestFullscreen().catch(function(x){document.title='REJ:'+x.name;});},300);</script></body></html>";

        static void Kiem(bool dk, string ten)
        {
            Console.WriteLine((dk ? "DAT   " : "LOI   ") + ten);
            if (!dk) loi++;
        }

        /// Tắt mọi tiến trình Brave chạy hồ sơ `ud`.
        static void Giet(string ud)
        {
            using (ManagementObjectSearcher s = new ManagementObjectSearcher("SELECT ProcessId, CommandLine FROM Win32_Process WHERE Name='brave.exe'"))
                foreach (ManagementObject o in s.Get())
                    if ((Convert.ToString(o["CommandLine"]) ?? "").IndexOf(ud, StringComparison.OrdinalIgnoreCase) >= 0)
                        try { Process.GetProcessById(Convert.ToInt32(o["ProcessId"])).Kill(); } catch (Exception) { }
        }

        [STAThread]
        static int Main(string[] a)
        {
            string brave = a[0], ext = a[1], tam = a[2];

            // 1. Toàn màn hình trong khung: máy chủ web một trang trên cổng tạm.
            TcpListener web = new TcpListener(IPAddress.Loopback, 0);
            web.Start();
            int cong = ((IPEndPoint)web.LocalEndpoint).Port;
            Thread tw = new Thread(delegate()
            {
                try
                {
                    while (true)
                        using (TcpClient c = web.AcceptTcpClient())
                        {
                            NetworkStream st = c.GetStream();
                            st.Read(new byte[8192], 0, 8192);
                            byte[] than = Encoding.UTF8.GetBytes(TRANG);
                            byte[] dau = Encoding.ASCII.GetBytes("HTTP/1.1 200 OK\r\nContent-Type: text/html; charset=utf-8\r\nContent-Length: " + than.Length + "\r\nConnection: close\r\n\r\n");
                            st.Write(dau, 0, dau.Length);
                            st.Write(than, 0, than.Length);
                        }
                }
                catch (Exception) { } // web.Stop()
            });
            tw.IsBackground = true;
            tw.Start();
            string ud1 = Path.Combine(tam, "ud_khung");
            Process.Start(brave, "--headless=new --user-data-dir=\"" + ud1 + "\" --no-first-run --remote-debugging-port=9335 --load-extension=\"" + ext + "\" http://127.0.0.1:" + cong + "/");
            string tieuDe = "";
            for (int i = 0; i < 40 && !Regex.IsMatch(tieuDe, "^(FS|REJ)"); i++)
            {
                Thread.Sleep(500);
                try
                {
                    Match m = Regex.Match(new WebClient().DownloadString("http://127.0.0.1:9335/json/list"), "\"title\":\\s*\"((FS|REJ)[^\"]*)\"");
                    if (m.Success) tieuDe = m.Groups[1].Value;
                }
                catch (Exception) { } // Brave chưa mở cổng
            }
            Match w = Regex.Match(tieuDe, "^FS:v:(\\d+)=(\\d+)$");
            Kiem(w.Success && w.Groups[1].Value == w.Groups[2].Value, "brave_khung: requestFullscreen chi phu khung trang (tieu de '" + tieuDe + "', can FS:v:<rong>=<rong khung>)");
            Giet(ud1);
            web.Stop();

            // 2. Gán chủ: Brave chính chạy trước (không qua GanBrave), brave.exe lần hai chuyển link rồi thoát -> GanBrave dò bằng WMI.
            string ud2 = Path.Combine(tam, "ud_chu");
            Form f = new Form();
            f.Text = "TKPCCC thu chu";
            f.StartPosition = FormStartPosition.Manual;
            f.Bounds = new System.Drawing.Rectangle(0, 0, 300, 200);
            f.Show();
            string co = "--user-data-dir=\"" + ud2 + "\" --no-first-run --no-default-browser-check --window-position=60,60 --window-size=520,420 ";
            Process.Start(brave, co + "about:blank");
            Thread.Sleep(4000);
            Process p2 = Process.Start(brave, co + "about:blank");
            GanBrave.Chu = delegate { return f.Handle; };
            GanBrave.Theo(ud2, p2.Id);
            for (int i = 0; i < 150 && GanBrave.CuaSo.Count == 0; i++) { Application.DoEvents(); Thread.Sleep(100); }
            IntPtr h = GanBrave.CuaSo.Count > 0 ? GanBrave.CuaSo[0] : IntPtr.Zero;
            Kiem(h != IntPtr.Zero && GanBrave.GetWindow(h, 4) == f.Handle, "GanBrave: cua so Brave nhan Form lam chu (lan mo thu hai chi chuyen link, do Brave chinh bang WMI; " + GanBrave.CuaSo.Count + " cua so)");
            GanBrave.Tha();
            Kiem(h != IntPtr.Zero && GanBrave.GetWindow(h, 4) == IntPtr.Zero, "GanBrave.Tha: tra cua so Brave ve doc lap");
            Giet(ud2);
            f.Close();
            Console.WriteLine(loi == 0 ? "TAT CA DAT" : "CO " + loi + " LOI");
            return loi;
        }
    }
}
