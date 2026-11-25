// Người dùng 18/11: "browser brave phải mở trong cad, khi đó tôi thao tác trong cad vẫn hiển thị ở bên cad".
// Brave không nhúng được (không có SDK nhúng; SetParent làm con thì khung tab tự vẽ của Chromium hỏng): gán cửa sổ AutoCAD làm CHỦ (owner) cho mọi cửa sổ
// trình duyệt của hồ sơ Brave riêng (%LOCALAPPDATA%\TKPCCC\Brave) như hộp thoại không chặn của CAD: luôn nằm trên AutoCAD, bấm vào bản vẽ không bị che,
// thu nhỏ / hiện lại theo AutoCAD. Không tham chiếu AutoCAD (cửa sổ chủ truyền qua `Chu`) để test/thu_brave.ps1 thử được ngoài AutoCAD.
// shortcut: chủ khác tiến trình thì Windows nối hàng đợi nhập của hai bên (Brave treo thì AutoCAD cũng đứng theo); gặp thì đổi sang palette WebView2.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace TKPCCC
{
    static class GanBrave
    {
        public static Func<IntPtr> Chu;                        // cửa sổ chủ (AutoCAD)
        public static readonly List<IntPtr> CuaSo = new List<IntPtr>(); // cửa sổ Brave đã gán chủ
        static Timer t;
        static int pid, ungVien;
        static bool daTimWmi;
        static string hoSo;

        delegate bool EnumProc(IntPtr h, IntPtr l);
        [DllImport("user32.dll")] static extern bool EnumWindows(EnumProc f, IntPtr l);
        [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr h, out uint p);
        [DllImport("user32.dll")] static extern bool IsWindowVisible(IntPtr h);
        [DllImport("user32.dll")] static extern bool IsWindow(IntPtr h);
        [DllImport("user32.dll")] public static extern IntPtr GetWindow(IntPtr h, uint lenh); // 4 = GW_OWNER
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern int GetClassName(IntPtr h, StringBuilder s, int n);
        [DllImport("user32.dll")] static extern bool GetWindowRect(IntPtr h, out RECT r);
        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")] static extern IntPtr SetWindowLongPtr(IntPtr h, int i, IntPtr v); // -8 = GWLP_HWNDPARENT (chủ)
        [StructLayout(LayoutKind.Sequential)] struct RECT { public int Trai, Tren, Phai, Duoi; }

        /// Gọi sau mỗi lần chạy brave.exe với hồ sơ riêng (pidVuaChay = tiến trình vừa chạy): canh 0,8 s một lần tới khi Brave hồ sơ đó tắt.
        public static void Theo(string hoSoBrave, int pidVuaChay)
        {
            hoSo = hoSoBrave;
            ungVien = pidVuaChay;
            daTimWmi = false;
            if (t == null)
            {
                t = new Timer();
                t.Interval = 800;
                t.Tick += delegate { try { Quet(); } catch (Exception) { } }; // timer trên luồng AutoCAD: lỗi không được thoát ra
            }
            t.Start();
        }

        static bool Song(int id)
        {
            if (id == 0) return false;
            try { return !Process.GetProcessById(id).HasExited; } catch (Exception) { return false; }
        }

        /// Tiến trình chính (không có --type=) của Brave chạy hồ sơ riêng. Chỉ dùng khi brave.exe vừa chạy chỉ chuyển link cho Brave đang mở rồi thoát.
        static int TimWmi()
        {
            using (ManagementObjectSearcher s = new ManagementObjectSearcher("SELECT ProcessId, CommandLine FROM Win32_Process WHERE Name='brave.exe'"))
                foreach (ManagementObject o in s.Get())
                {
                    string dong = Convert.ToString(o["CommandLine"]) ?? "";
                    if (dong.IndexOf(hoSo, StringComparison.OrdinalIgnoreCase) >= 0 && dong.IndexOf("--type=", StringComparison.Ordinal) < 0)
                        return Convert.ToInt32(o["ProcessId"]);
                }
            return 0;
        }

        /// Một lượt: gán chủ cho mọi cửa sổ trình duyệt (lớp Chrome_WidgetWin_1, hiện, chưa có chủ, ≥ 200 x 150) của Brave hồ sơ riêng. Trả số cửa sổ mới gán.
        public static int Quet()
        {
            if (!Song(pid))
            {
                pid = Song(ungVien) ? ungVien : 0;
                if (pid == 0 && !daTimWmi) { daTimWmi = true; pid = TimWmi(); }
                if (pid == 0) { if (t != null) t.Stop(); return 0; } // Brave hồ sơ riêng đã tắt: nghỉ tới lần mở sau
            }
            IntPtr chu = Chu == null ? IntPtr.Zero : Chu();
            if (chu == IntPtr.Zero) return 0;
            CuaSo.RemoveAll(h => !IsWindow(h));
            int n = 0;
            EnumWindows(delegate(IntPtr h, IntPtr l)
            {
                uint p;
                GetWindowThreadProcessId(h, out p);
                if (p != pid || !IsWindowVisible(h) || GetWindow(h, 4) != IntPtr.Zero) return true;
                StringBuilder lop = new StringBuilder(64);
                GetClassName(h, lop, lop.Capacity);
                RECT r;
                if (lop.ToString() != "Chrome_WidgetWin_1" || !GetWindowRect(h, out r) || r.Phai - r.Trai < 200 || r.Duoi - r.Tren < 150) return true;
                SetWindowLongPtr(h, -8, chu);
                CuaSo.Add(h);
                n++;
                return true;
            }, IntPtr.Zero);
            return n;
        }

        /// Trả các cửa sổ Brave về độc lập (AutoCAD sắp đóng: cửa sổ chủ mất thì cửa sổ được sở hữu bị huỷ theo).
        public static void Tha()
        {
            if (t != null) t.Stop();
            foreach (IntPtr h in CuaSo) if (IsWindow(h)) SetWindowLongPtr(h, -8, IntPtr.Zero);
            CuaSo.Clear();
        }
    }
}
