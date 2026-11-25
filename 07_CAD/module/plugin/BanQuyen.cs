// Bản quyền TKPCCC: mã máy (seri ổ chứa Windows; mã cũ MachineGuid vẫn nhận), kiểm key, chống lùi đồng hồ (hợp đồng ở 07_CAD/tien_do/viec_14_11.md, mục Gói C), lưu %APPDATA%\TKPCCC\ban_quyen.json, gọi web app Apps Script.
// Chặn lệnh chỉ có ở bản build "build.bat phat_hanh" (/define:BAN_QUYEN). Ngoài khối #if BAN_QUYEN không đụng AutoCAD
// nên file này biên dịch chung được vào app tạo key (module/banquyen/KeyGen.cs) và bộ test.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Management;
using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Web.Script.Serialization;
using Microsoft.Win32;
#if BAN_QUYEN
using Autodesk.AutoCAD.ApplicationServices;
using AcApp = Autodesk.AutoCAD.ApplicationServices.Core.Application;
#endif

namespace TKPCCC
{
    public static class BanQuyen
    {
        public const string PHIEN_BAN = "TKPCCC1";
        static readonly string[] LENH_TU_DO = { "PCCC", "PCCC_BANQUYEN", "PCCC_CAIDAT", "PCCC_HELP" };

        static string maMay; // đọc WMI chậm (~0,1 s): nhớ cả phiên

        /// Mã máy dạng XXXX-XXXX-XXXX-XXXX: "D" + 15 hex HOA đầu của SHA-256("DISK|" + seri ổ chứa Windows + "|TKPCCC").
        /// Không đọc được seri (máy ảo không xuất seri, RAID, WMI hỏng) thì dùng mã cũ theo MachineGuid (MaMayCu).
        public static string MaMay()
        {
            if (maMay == null)
            {
                string s = "";
                try { s = SeriODia(); } catch (System.Exception) { }
                maMay = s != "" ? Nhom("D" + Hex("DISK|" + s + "|TKPCCC").Substring(0, 15)) : MaMayCu();
            }
            return maMay;
        }

        /// Mã máy cũ (trước 15/11/2026): 16 hex HOA đầu của SHA-256(MachineGuid + "|TKPCCC"). Key đã cấp theo mã này vẫn nhận.
        public static string MaMayCu()
        {
            string guid;
            using (RegistryKey g = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
            using (RegistryKey k = g.OpenSubKey(@"SOFTWARE\Microsoft\Cryptography"))
                guid = Convert.ToString(k.GetValue("MachineGuid"));
            return Nhom(Hex(guid + "|TKPCCC").Substring(0, 16));
        }

        static string Hex(string s)
        {
            using (SHA256 h = SHA256.Create()) return BitConverter.ToString(h.ComputeHash(Encoding.UTF8.GetBytes(s))).Replace("-", "");
        }

        static string Nhom(string x) { return x.Substring(0, 4) + "-" + x.Substring(4, 4) + "-" + x.Substring(8, 4) + "-" + x.Substring(12, 4); }

        /// Seri ổ vật lý chứa %SystemDrive% (không lấy ổ đầu danh sách: tránh USB, ổ ngoài): Win32_LogicalDisk → phân vùng → Win32_DiskDrive.
        /// Đọc được bằng quyền thường. keygen.py đọc cùng chuỗi liên kết này nên in ra cùng mã máy.
        static string SeriODia()
        {
            string o = (Environment.GetEnvironmentVariable("SystemDrive") ?? "C:").TrimEnd('\\');
            using (ManagementObjectSearcher pv = new ManagementObjectSearcher("ASSOCIATORS OF {Win32_LogicalDisk.DeviceID='" + o + "'} WHERE AssocClass=Win32_LogicalDiskToPartition"))
                foreach (ManagementObject p in pv.Get())
                    using (ManagementObjectSearcher od = new ManagementObjectSearcher("ASSOCIATORS OF {Win32_DiskPartition.DeviceID='" + p["DeviceID"] + "'} WHERE AssocClass=Win32_DiskDriveToDiskPartition"))
                        foreach (ManagementObject d in od.Get())
                        {
                            string s = ChuanSeri(Convert.ToString(d["SerialNumber"]));
                            if (s != "") return s;
                        }
            return "";
        }

        /// Chỉ giữ A-Z 0-9 (bỏ khoảng trắng đệm, dấu chấm, gạch dưới mà driver chèn); toàn 0 hoặc rỗng = không có seri.
        /// shortcut: driver ATA đời cũ trả seri đảo cặp byte; gặp máy như vậy thì mã máy đổi khi đổi driver, cấp lại key.
        public static string ChuanSeri(string s)
        {
            StringBuilder b = new StringBuilder();
            foreach (char c in (s ?? "").ToUpperInvariant()) if ((c >= '0' && c <= '9') || (c >= 'A' && c <= 'Z')) b.Append(c);
            return b.ToString().Trim('0') == "" ? "" : b.ToString();
        }

        public static string ChuanEmail(string email) { return (email ?? "").Trim().ToLowerInvariant(); }

        public static string NoiDung(string email, string may, string han)
        {
            return PHIEN_BAN + "|" + ChuanEmail(email) + "|" + (may ?? "").Trim().ToUpperInvariant() + "|" + han;
        }

        public static string B64u(byte[] b) { return Convert.ToBase64String(b).TrimEnd('=').Replace('+', '-').Replace('/', '_'); }

        public static byte[] TuB64u(string s)
        {
            s = s.Trim().TrimEnd('=').Replace('-', '+').Replace('_', '/');
            return Convert.FromBase64String(s + new string('=', (4 - s.Length % 4) % 4));
        }

        /// PROV_RSA_AES (24): CSP mặc định cũ không ký được SHA-256.
        public static RSACryptoServiceProvider Rsa(string xml)
        {
            RSACryptoServiceProvider r = new RSACryptoServiceProvider(new CspParameters(24));
            r.PersistKeyInCsp = false;
            r.FromXmlString(xml);
            return r;
        }

        /// Ký bằng khoá riêng (XML). Chỉ app tạo key và bộ test gọi.
        public static string Ky(string khoaRiengXml, string email, string may, string han)
        {
            byte[] nd = Encoding.UTF8.GetBytes(NoiDung(email, may, han));
            using (RSACryptoServiceProvider r = Rsa(khoaRiengXml)) return B64u(nd) + "." + B64u(r.SignData(nd, "SHA256"));
        }

        public const string SAI_MAY = "Key cấp cho máy khác";

        /// null = hợp lệ; khác null = lý do từ chối. Hạn so với hôm nay của đồng hồ máy.
        public static string Kiem(string key, string email, string may, string khoaCongXml, out DateTime han)
        {
            return Kiem(key, email, may, khoaCongXml, DateTime.Today, out han);
        }

        /// Như trên, hạn so với homNay (plugin truyền NgayKiem(): lùi đồng hồ không kéo dài được key).
        public static string Kiem(string key, string email, string may, string khoaCongXml, DateTime homNay, out DateTime han)
        {
            han = DateTime.MinValue;
            if (string.IsNullOrEmpty(khoaCongXml)) return "Bản build này không kèm khoá công khai (chỉ bản phát hành có).";
            if (string.IsNullOrWhiteSpace(key)) return "Chưa có key.";
            string[] p = key.Trim().Split('.');
            if (p.Length != 2) return "Key sai dạng.";
            byte[] nd, ky;
            try { nd = TuB64u(p[0]); ky = TuB64u(p[1]); }
            catch (FormatException) { return "Key sai dạng."; }
            using (RSACryptoServiceProvider r = Rsa(khoaCongXml))
                if (!r.VerifyData(nd, "SHA256", ky)) return "Key không hợp lệ (chữ ký sai).";
            string[] f = Encoding.UTF8.GetString(nd).Split('|');
            if (f.Length != 4 || f[0] != PHIEN_BAN) return "Key sai phiên bản.";
            if (f[1] != ChuanEmail(email)) return "Key cấp cho email khác (" + f[1] + ").";
            if (f[2] != (may ?? "").Trim().ToUpperInvariant()) return SAI_MAY + " (" + f[2] + ").";
            if (!DateTime.TryParseExact(f[3], "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out han)) return "Key sai hạn.";
            if (han < homNay) return "Key đã hết hạn ngày " + han.ToString("dd/MM/yyyy") + ".";
            return null;
        }

        /// Khoá công khai nhúng vào DLL lúc build phat_hanh (csc /resource:...,khoa_cong_khai.xml).
        public static string KhoaCongKhai()
        {
            using (Stream s = Assembly.GetExecutingAssembly().GetManifestResourceStream("khoa_cong_khai.xml"))
            {
                if (s == null) return null;
                using (StreamReader r = new StreamReader(s, Encoding.UTF8)) return r.ReadToEnd();
            }
        }

        static string FileLuu { get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TKPCCC", "ban_quyen.json"); } }

        public static void DocLuu(out string email, out string key)
        {
            email = ""; key = "";
            try
            {
                Dictionary<string, object> d = new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(File.ReadAllText(FileLuu, Encoding.UTF8));
                object v;
                if (d.TryGetValue("email", out v)) email = Convert.ToString(v);
                if (d.TryGetValue("key", out v)) key = Convert.ToString(v);
            }
            catch (System.Exception) { } // chưa có file hoặc file hỏng = chưa có bản quyền
        }

        public static void Luu(string email, string key)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FileLuu));
            File.WriteAllText(FileLuu, new JavaScriptSerializer().Serialize(new Dictionary<string, object> { { "email", ChuanEmail(email) }, { "key", (key ?? "").Trim() } }), new UTF8Encoding(false));
            daKiem = null;
        }

        /// Trạng thái để hiện cho người dùng; hopLe = true khi key đúng máy, đúng email, còn hạn.
        public static string TrangThai(string email, string key, out bool hopLe)
        {
            DateTime han;
            string loi = KiemMayNay(key, email, KhoaCongKhai(), NgayKiem(), out han);
            hopLe = loi == null;
            return hopLe ? "Hợp lệ tới hết ngày " + han.ToString("dd/MM/yyyy") + "." : loi;
        }

        /// Kiểm key cho máy này: mã máy mới (seri ổ đĩa) rồi mã cũ (MachineGuid), để key cấp trước 15/11/2026 vẫn dùng được.
        public static string KiemMayNay(string key, string email, string khoaCongXml, DateTime homNay, out DateTime han)
        {
            string loi = Kiem(key, email, MaMay(), khoaCongXml, homNay, out han);
            if (loi != null && loi.StartsWith(SAI_MAY) && MaMay() != MaMayCu() && Kiem(key, email, MaMayCu(), khoaCongXml, homNay, out han) == null) return null;
            return loi;
        }

        // ---------- Chống lùi đồng hồ: mốc "ngày lớn nhất đã thấy" ở HKCU\Software\TKPCCC, giá trị NgayBanQuyen ----------
        // shortcut: xoá giá trị registry thì mất mốc (chỉ chặn lùi giờ thông thường); muốn chặn hơn thì hỏi giờ web app khi có mạng.

        /// Chuỗi lưu mốc: "yyyy-MM-dd." + 16 hex HMAC-SHA256 (khoá theo mã máy: sửa tay ngày hoặc chép sang máy khác thì hỏng chữ ký).
        public static string DauNgay(DateTime d, string may)
        {
            string n = d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            using (HMACSHA256 h = new HMACSHA256(Encoding.UTF8.GetBytes("TKPCCC-NGAY|" + may)))
                return n + "." + BitConverter.ToString(h.ComputeHash(Encoding.UTF8.GetBytes(n))).Replace("-", "").Substring(0, 16);
        }

        /// Ngày dùng để so hạn = max(hôm nay, mốc đã lưu nếu chữ ký đúng).
        public static DateTime NgayHieuLuc(DateTime homNay, string luu, string may)
        {
            DateTime d;
            string[] p = (luu ?? "").Split('.');
            if (p.Length == 2 && DateTime.TryParseExact(p[0], "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out d)
                && DauNgay(d, may) == luu && d > homNay) return d;
            return homNay;
        }

        /// Ngày so hạn của plugin; hôm nay mới hơn mốc thì ghi mốc mới.
        static DateTime NgayKiem()
        {
            DateTime nay = DateTime.Today;
            try
            {
                using (RegistryKey k = Registry.CurrentUser.CreateSubKey(@"Software\TKPCCC"))
                {
                    string luu = Convert.ToString(k.GetValue("NgayBanQuyen"));
                    DateTime n = NgayHieuLuc(nay, luu, MaMay());
                    if (n == nay && luu != DauNgay(nay, MaMay())) k.SetValue("NgayBanQuyen", DauNgay(nay, MaMay()));
                    return n;
                }
            }
            catch (System.Exception) { return nay; } // registry bị chặn (chính sách máy): so theo đồng hồ
        }

        static bool? daKiem;

        /// Bản quyền đã lưu còn hợp lệ không (nhớ kết quả tới lần Luu sau).
        public static bool HopLe()
        {
            if (daKiem == null)
            {
                string e, k; bool ok;
                try { DocLuu(out e, out k); TrangThai(e, k, out ok); }
                catch (System.Exception) { ok = false; }
                daKiem = ok;
            }
            return daKiem.Value;
        }

        /// Hàng đợi MCP (Cau.cs) có được chạy spec không: bản thường luôn được.
        public static bool ChoChay()
        {
#if BAN_QUYEN
            return HopLe();
#else
            return true;
#endif
        }

        /// Cấu hình cạnh DLL (build.bat chép từ 07_CAD/data/ban_quyen/cau_hinh_ban_quyen.json): web_app, google_form.
        public static string CauHinh(string ten)
        {
            try
            {
                string f = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "cau_hinh_ban_quyen.json");
                object v;
                if (new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(File.ReadAllText(f, Encoding.UTF8)).TryGetValue(ten, out v))
                    return Convert.ToString(v).Trim();
            }
            catch (System.Exception) { }
            return "";
        }

        /// Gọi web app Apps Script: hd = "dang_ky" | "lay_key". Trả JSON đã đọc ({ok, tb, key}).
        public static Dictionary<string, object> GoiWeb(string hd, string email)
        {
            string url = CauHinh("web_app");
            if (url == "") throw new System.Exception("Chưa cấu hình địa chỉ web app (cau_hinh_ban_quyen.json cạnh DLL).");
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
            HttpWebRequest r = (HttpWebRequest)WebRequest.Create(url + (url.Contains("?") ? "&" : "?") + "hd=" + hd
                + "&email=" + Uri.EscapeDataString(ChuanEmail(email)) + "&may=" + Uri.EscapeDataString(MaMay()));
            r.Timeout = r.ReadWriteTimeout = 15000; // mạng hỏng thì báo sau 15 s, không treo tới 100 s
            using (WebResponse w = r.GetResponse())
            using (StreamReader sr = new StreamReader(w.GetResponseStream(), Encoding.UTF8))
                return new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(sr.ReadToEnd());
        }

        /// Gọi một lần trong KhoiDong.Initialize. Bản thường: không làm gì.
        public static void Chan()
        {
#if BAN_QUYEN
            try
            {
                AcApp.DocumentManager.DocumentLockModeChanged += delegate(object s, DocumentLockModeChangedEventArgs e)
                {
                    // Lúc lệnh kết thúc tên có "#" đứng đầu: chỉ chặn lúc lệnh bắt đầu.
                    string ten = (e.GlobalCommandName ?? "").ToUpperInvariant();
                    if (!ten.StartsWith("PCCC") || Array.IndexOf(LENH_TU_DO, ten) >= 0 || HopLe()) return;
                    e.Veto();
                    if (e.Document != null) e.Document.Editor.WriteMessage("\n[TKPCCC] Chưa có bản quyền. Gõ PCCC_BANQUYEN để kích hoạt.");
                };
            }
            catch (System.Exception) { }
#endif
        }
    }
}
