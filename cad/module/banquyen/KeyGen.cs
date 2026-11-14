// App tạo key cho chủ plugin (cad\bin\TKPCCC.KeyGen.exe). Biên dịch chung với module/plugin/BanQuyen.cs (cùng hợp đồng key).
// Không đối số: cửa sổ WinForms. Có đối số (bộ test và người quen dòng lệnh):
//   tao <thư mục khoá công khai> <thư mục khoá riêng>   tạo cặp khoá RSA 2048 (không ghi đè)
//   ky <khoa_rieng.xml> <email> <mã máy> <yyyy-MM-dd>    in key
//   kiem <khoa_cong_khai.xml> <key> <email> <mã máy>     mã thoát 0 = hợp lệ, 1 = từ chối (in lý do)
//   may                                                  in mã máy này
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;

namespace TKPCCC
{
    static class KeyGen
    {
        const string CONG = "khoa_cong_khai.xml", RIENG = "khoa_rieng.xml", PEM = "khoa_rieng.pem";

        [STAThread]
        static int Main(string[] a)
        {
            if (a.Length == 0)
            {
                Application.EnableVisualStyles();
                Application.Run(new CuaSo());
                return 0;
            }
            try
            {
                if (a[0] == "tao" && a.Length == 3) { TaoCapKhoa(a[1], a[2], false); return 0; }
                if (a[0] == "ky" && a.Length == 5) { Console.WriteLine(BanQuyen.Ky(File.ReadAllText(a[1]), a[2], a[3], a[4])); return 0; }
                if (a[0] == "may") { Console.WriteLine(BanQuyen.MaMay()); return 0; }
                if (a[0] == "kiem" && a.Length == 5)
                {
                    DateTime han;
                    string loi = BanQuyen.Kiem(a[2], a[3], a[4], File.ReadAllText(a[1]), out han);
                    Console.WriteLine(loi ?? "HOP LE " + han.ToString("yyyy-MM-dd"));
                    return loi == null ? 0 : 1;
                }
                Console.Error.WriteLine("Sai doi so.");
            }
            catch (Exception ex) { Console.Error.WriteLine(ex.Message); }
            return 2;
        }

        public static string ThuMucCong { get { return Path.GetFullPath(Path.Combine(Application.StartupPath, "..", "data", "ban_quyen")); } }
        public static string ThuMucRieng { get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "TKPCCC_KHOA"); } }

        /// Ghi khoá công khai XML + khoá riêng XML và PEM PKCS#8. Đã có file mà ghiDe = false thì báo lỗi, không ghi.
        public static void TaoCapKhoa(string tmCong, string tmRieng, bool ghiDe)
        {
            string fc = Path.Combine(tmCong, CONG), fr = Path.Combine(tmRieng, RIENG), fp = Path.Combine(tmRieng, PEM);
            if (!ghiDe && (File.Exists(fc) || File.Exists(fr) || File.Exists(fp))) throw new Exception("Đã có khoá cũ, không ghi đè.");
            using (RSACryptoServiceProvider r = new RSACryptoServiceProvider(2048, new CspParameters(24)))
            {
                r.PersistKeyInCsp = false;
                Directory.CreateDirectory(tmCong);
                Directory.CreateDirectory(tmRieng);
                File.WriteAllText(fc, r.ToXmlString(false));
                File.WriteAllText(fr, r.ToXmlString(true));
                File.WriteAllText(fp, Pem(r.ExportParameters(true)));
            }
        }

        /// PEM "PRIVATE KEY" (PKCS#8 bọc PKCS#1) tự mã hoá DER: .NET 4.8 không có ExportPkcs8PrivateKey.
        static string Pem(RSAParameters k)
        {
            byte[] pkcs1 = Der(0x30, Int(new byte[] { 0 }), Int(k.Modulus), Int(k.Exponent), Int(k.D), Int(k.P), Int(k.Q), Int(k.DP), Int(k.DQ), Int(k.InverseQ));
            byte[] rsaOid = { 0x06, 0x09, 0x2A, 0x86, 0x48, 0x86, 0xF7, 0x0D, 0x01, 0x01, 0x01, 0x05, 0x00 }; // 1.2.840.113549.1.1.1 + NULL
            byte[] pkcs8 = Der(0x30, Int(new byte[] { 0 }), Der(0x30, rsaOid), Der(0x04, pkcs1));
            string b = Convert.ToBase64String(pkcs8);
            StringBuilder s = new StringBuilder("-----BEGIN PRIVATE KEY-----\n");
            for (int i = 0; i < b.Length; i += 64) s.Append(b.Substring(i, Math.Min(64, b.Length - i))).Append('\n');
            return s.Append("-----END PRIVATE KEY-----\n").ToString();
        }

        static byte[] Int(byte[] v)
        {
            int i = 0;
            while (i < v.Length - 1 && v[i] == 0) i++;
            List<byte> b = new List<byte>();
            if ((v[i] & 0x80) != 0) b.Add(0); // số dương: thêm 0 khi bit cao bật
            for (; i < v.Length; i++) b.Add(v[i]);
            return Der(0x02, b.ToArray());
        }

        static byte[] Der(byte tag, params byte[][] phan)
        {
            List<byte> nd = new List<byte>();
            foreach (byte[] p in phan) nd.AddRange(p);
            List<byte> o = new List<byte> { tag };
            int n = nd.Count;
            if (n < 128) o.Add((byte)n);
            else
            {
                List<byte> l = new List<byte>();
                for (; n > 0; n >>= 8) l.Insert(0, (byte)(n & 0xFF));
                o.Add((byte)(0x80 | l.Count));
                o.AddRange(l);
            }
            o.AddRange(nd);
            return o.ToArray();
        }

        class CuaSo : Form
        {
            readonly TextBox oEmail = new TextBox(), oMay = new TextBox(), oKey = new TextBox();
            readonly DateTimePicker oHan = new DateTimePicker();

            public CuaSo()
            {
                Text = "TKPCCC - Tạo key bản quyền";
                Font = new Font("Segoe UI", 9f);
                FormBorderStyle = FormBorderStyle.FixedDialog;
                MaximizeBox = false;
                ClientSize = new Size(520, 330);

                GroupBox g1 = Nhom("1. Cặp khoá (làm một lần)", 8, 90);
                Label l = new Label();
                l.Text = "Khoá riêng: " + ThuMucRieng + "\nKhoá công khai: " + ThuMucCong;
                l.SetBounds(10, 20, 490, 32);
                g1.Controls.Add(l);
                Them(g1, "Tạo cặp khoá mới", 10, 56, 150, TaoKhoa);
                Them(g1, "Chép khoá riêng PEM", 168, 56, 160, delegate { Clipboard.SetText(File.ReadAllText(Path.Combine(ThuMucRieng, PEM))); MessageBox.Show("Đã chép. Dán vào Script Property KHOA_RIENG của Apps Script."); });
                Them(g1, "Mở thư mục khoá", 336, 56, 150, delegate { System.Diagnostics.Process.Start(ThuMucRieng); });

                GroupBox g2 = Nhom("2. Ký key tay", 106, 214);
                O(g2, "Email", oEmail, 22);
                O(g2, "Mã máy", oMay, 52);
                oHan.Format = DateTimePickerFormat.Custom;
                oHan.CustomFormat = "dd/MM/yyyy";
                oHan.Value = DateTime.Today.AddDays(365);
                O(g2, "Hạn", oHan, 82);
                Them(g2, "Ký key", 90, 112, 100, delegate
                {
                    oKey.Text = BanQuyen.Ky(File.ReadAllText(Path.Combine(ThuMucRieng, RIENG)), oEmail.Text, oMay.Text, oHan.Value.ToString("yyyy-MM-dd"));
                });
                Them(g2, "Chép key", 198, 112, 100, delegate { if (oKey.Text != "") Clipboard.SetText(oKey.Text); });
                oKey.Multiline = true;
                oKey.ReadOnly = true;
                oKey.SetBounds(10, 146, 490, 58);
                g2.Controls.Add(oKey);
            }

            void TaoKhoa()
            {
                bool co = File.Exists(Path.Combine(ThuMucRieng, RIENG)) || File.Exists(Path.Combine(ThuMucCong, CONG));
                if (co && MessageBox.Show("ĐÃ CÓ cặp khoá. Tạo mới thì MỌI key đã cấp mất hiệu lực, phải build phát hành lại và đổi KHOA_RIENG trong Apps Script.\n\nVẫn tạo mới (ghi đè)?",
                    "Cảnh báo", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) != DialogResult.Yes) return;
                TaoCapKhoa(ThuMucCong, ThuMucRieng, true);
                MessageBox.Show("Đã tạo.\nKhoá công khai: " + Path.Combine(ThuMucCong, CONG) + " (commit được)\nKhoá riêng: " + ThuMucRieng
                    + " (GIỮ KÍN, sao lưu ra USB; không đưa vào repo)\n\nTiếp theo: chép PEM vào Apps Script, chạy cad\\build.bat phat_hanh.");
            }

            GroupBox Nhom(string t, int top, int cao)
            {
                GroupBox g = new GroupBox();
                g.Text = t;
                g.SetBounds(8, top, 504, cao);
                Controls.Add(g);
                return g;
            }

            static void O(GroupBox g, string nhan, Control o, int top)
            {
                Label l = new Label();
                l.Text = nhan;
                l.SetBounds(10, top + 3, 76, 20);
                g.Controls.Add(l);
                o.SetBounds(90, top, 410, 24);
                g.Controls.Add(o);
            }

            static void Them(GroupBox g, string chu, int x, int top, int rong, Action viec)
            {
                Button b = new Button();
                b.Text = chu;
                b.SetBounds(x, top, rong, 28);
                b.Click += delegate { try { viec(); } catch (Exception ex) { MessageBox.Show(ex.Message, "TKPCCC"); } };
                g.Controls.Add(b);
            }
        }
    }
}
