// Cài đặt người dùng dùng chung giữa plugin (TKPCCC.Cad.dll) và palette (TKPCCC.UI.dll, GiaoDien.cs gọi lại lớp này):
// %APPDATA%\TKPCCC\cai_dat.json, JSON phẳng khoá -> chuỗi. Biến môi trường TKPCCC_CAI_DAT trỏ file khác (bộ test).
// Người dùng 16/11: bảng layer dây báo cháy / ống chữa cháy thêm, xoá được (trang Cài đặt); chọn dây cho từng loại thiết bị (trang Báo cháy);
// chọn kiểu dim và cách dim tự động sau khi rải (trang Cài đặt).
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace TKPCCC
{
    public static class CaiDat
    {
        public static string FileCaiDat
        {
            get
            {
                string e = Environment.GetEnvironmentVariable("TKPCCC_CAI_DAT");
                if (!string.IsNullOrEmpty(e)) return e;
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TKPCCC", "cai_dat.json");
            }
        }

        public static Dictionary<string, string> DocTatCa()
        {
            Dictionary<string, string> d = new Dictionary<string, string>();
            try
            {
                if (File.Exists(FileCaiDat))
                    foreach (Match m in Regex.Matches(File.ReadAllText(FileCaiDat, Encoding.UTF8), "\"([A-Za-z_0-9]+)\"\\s*:\\s*\"([^\"\\\\]*)\""))
                        d[m.Groups[1].Value] = m.Groups[2].Value;
            }
            catch (System.Exception) { } // file hỏng: dùng mặc định
            return d;
        }

        public static string Doc(string khoa, string macDinh)
        {
            string v;
            return DocTatCa().TryGetValue(khoa, out v) && v != "" ? v : macDinh;
        }

        /// Ghi một khoá, giữ các khoá khác (giá trị bỏ dấu " và \ để JSON luôn hợp lệ).
        public static void Luu(string khoa, string giaTri)
        {
            Dictionary<string, string> d = DocTatCa();
            d[khoa] = (giaTri ?? "").Replace("\"", "").Replace("\\", "");
            List<string> dong = new List<string>();
            foreach (KeyValuePair<string, string> p in d) dong.Add("  \"" + p.Key + "\": \"" + p.Value + "\"");
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(FileCaiDat));
                File.WriteAllText(FileCaiDat, "{\r\n" + string.Join(",\r\n", dong) + "\r\n}\r\n", new UTF8Encoding(false));
            }
            catch (System.Exception) { } // không ghi được: giá trị vẫn dùng trong phiên này nếu nơi gọi giữ lại
        }

        // ---------- Bảng layer dây, ống: khoá "lop_day" = các dòng "ten|mau|net|nhom|ghi_chu" nối bằng ";" ----------
        // Layer tên PCCC_<mã vật tư loại tuyen> (vd PCCC_DAY_BC) thì PCCC_BOC cộng chiều dài vào mã đó.
        public static readonly string[][] LOP_MAC_DINH = {
            new[] { "PCCC_DAY_BC_10", "1", "Continuous", "BC", "Tín hiệu đầu báo, nút ấn thường" },
            new[] { "PCCC_DAY_BC", "6", "Continuous", "BC", "Mạch vòng địa chỉ (module, đầu báo, nút ấn địa chỉ)" },
            new[] { "PCCC_DAY_NGUON_15_FR", "5", "Continuous", "BC", "Nguồn chuông, còi đèn" },
            new[] { "PCCC_ONG_DN100", "1", "Continuous", "CC", "Ống chính chữa cháy" },
            new[] { "PCCC_ONG_DN50", "1", "Continuous", "CC", "Ống nhánh họng vách tường" },
            new[] { "PCCC_ONG_DN25", "3", "Continuous", "CC", "Ống nhánh sprinkler" },
        };

        public static List<string[]> LopDay()
        {
            string s = Doc("lop_day", "");
            if (s == "") return LOP_MAC_DINH.Select(x => (string[])x.Clone()).ToList();
            List<string[]> kq = new List<string[]>();
            foreach (string dong in s.Split(';'))
            {
                string[] f = dong.Split('|');
                if (f.Length < 1 || f[0].Trim() == "") continue;
                string[] r = new string[5];
                for (int i = 0; i < 5; i++) r[i] = i < f.Length ? f[i].Trim() : "";
                if (r[1] == "") r[1] = "7";
                if (r[2] == "") r[2] = "Continuous";
                if (r[3] == "") r[3] = "BC";
                kq.Add(r);
            }
            return kq;
        }

        public static void LuuLopDay(IEnumerable<string[]> ds)
        {
            Luu("lop_day", string.Join(";", ds.Where(r => r.Length > 0 && (r[0] ?? "").Trim() != "")
                .Select(r => string.Join("|", Enumerable.Range(0, 5).Select(i => i < r.Length ? (r[i] ?? "").Replace(";", ",").Replace("|", "/").Trim() : "")))));
        }

        // ---------- Dây theo vai trò thiết bị (trang Báo cháy): khoá "day_<vai trò>" = tên layer trong bảng ----------
        // { khoá, nhãn, layer mặc định }
        public static readonly string[][] VAI_TRO = {
            new[] { "dau_bao_thuong", "Đầu báo thường", "PCCC_DAY_BC_10" },
            new[] { "nut_nhan_thuong", "Nút nhấn thường", "PCCC_DAY_BC_10" },
            new[] { "dau_bao_dc", "Đầu báo địa chỉ", "PCCC_DAY_BC" },
            new[] { "nut_nhan_dc", "Nút nhấn địa chỉ", "PCCC_DAY_BC" },
            new[] { "zm", "Module ZM", "PCCC_DAY_BC" },
            new[] { "cm", "Module CM", "PCCC_DAY_BC" },
            new[] { "r2ml", "Module R2ML", "PCCC_DAY_BC" },
            new[] { "ngoai_vi", "Module ngoại vi khác", "PCCC_DAY_BC" },
            new[] { "chuong", "Chuông (dây nguồn)", "PCCC_DAY_NGUON_15_FR" },
        };

        public static string DayVaiTro(string vaiTro)
        {
            string[] r = VAI_TRO.FirstOrDefault(x => x[0] == vaiTro);
            return Doc("day_" + vaiTro, r != null ? r[2] : "PCCC_DAY_BC");
        }

        // ---------- Dim sau khi rải: kiểu dim (DimStyle, "" = hiện hành), cách dim, layer ----------
        public const string DIM_CHUOI = "chuoi", DIM_CHUOI_CHEO = "chuoi_cheo", DIM_KHONG = "khong";
        public static string DimKieu() { return Doc("dim_kieu", ""); }
        public static string DimDang() { string d = Doc("dim_dang", DIM_CHUOI); return d == DIM_CHUOI_CHEO || d == DIM_KHONG ? d : DIM_CHUOI; }
        public static string DimLop() { return Doc("dim_lop", "PCCC_KT"); }
    }
}
