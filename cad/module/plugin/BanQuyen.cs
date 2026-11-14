// Bản quyền TKPCCC: mã máy, kiểm key (hợp đồng ở cad/tien_do/viec_14_11.md, mục Gói C), lưu %APPDATA%\TKPCCC\ban_quyen.json, gọi web app Apps Script.
// Chặn lệnh chỉ có ở bản build "build.bat phat_hanh" (/define:BAN_QUYEN). Ngoài khối #if BAN_QUYEN không đụng AutoCAD
// nên file này biên dịch chung được vào app tạo key (module/banquyen/KeyGen.cs) và bộ test.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
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

        /// 16 ký tự hex HOA đầu của SHA-256(MachineGuid + "|TKPCCC"), dạng XXXX-XXXX-XXXX-XXXX.
        public static string MaMay()
        {
            string guid;
            using (RegistryKey g = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
            using (RegistryKey k = g.OpenSubKey(@"SOFTWARE\Microsoft\Cryptography"))
                guid = Convert.ToString(k.GetValue("MachineGuid"));
            byte[] h;
            using (SHA256 s = SHA256.Create()) h = s.ComputeHash(Encoding.UTF8.GetBytes(guid + "|TKPCCC"));
            string x = BitConverter.ToString(h).Replace("-", "").Substring(0, 16);
            return x.Substring(0, 4) + "-" + x.Substring(4, 4) + "-" + x.Substring(8, 4) + "-" + x.Substring(12, 4);
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

        /// null = hợp lệ; khác null = lý do từ chối.
        public static string Kiem(string key, string email, string may, string khoaCongXml, out DateTime han)
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
            if (f[2] != (may ?? "").Trim().ToUpperInvariant()) return "Key cấp cho máy khác (" + f[2] + ").";
            if (!DateTime.TryParseExact(f[3], "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out han)) return "Key sai hạn.";
            if (han < DateTime.Today) return "Key đã hết hạn ngày " + han.ToString("dd/MM/yyyy") + ".";
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
            string loi = Kiem(key, email, MaMay(), KhoaCongKhai(), out han);
            hopLe = loi == null;
            return hopLe ? "Hợp lệ tới hết ngày " + han.ToString("dd/MM/yyyy") + "." : loi;
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

        /// Cấu hình cạnh DLL (build.bat chép từ cad/data/ban_quyen/cau_hinh_ban_quyen.json): web_app, google_form.
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
            using (WebClient w = new WebClient())
            {
                w.Encoding = Encoding.UTF8;
                string s = w.DownloadString(url + (url.Contains("?") ? "&" : "?") + "hd=" + hd
                    + "&email=" + Uri.EscapeDataString(ChuanEmail(email)) + "&may=" + Uri.EscapeDataString(MaMay()));
                return new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(s);
            }
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
