// Bảng bản quyền (lệnh PCCC_BANQUYEN): email, mã máy, key, Kích hoạt / Đăng ký / Lấy key tự động. Mọi xử lý ở BanQuyen (TKPCCC.Cad.dll).
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Autodesk.AutoCAD.Runtime;
using AcApp = Autodesk.AutoCAD.ApplicationServices.Application;

[assembly: CommandClass(typeof(TKPCCC.LenhBanQuyen))]

namespace TKPCCC
{
    public class LenhBanQuyen
    {
        [CommandMethod("PCCC_BANQUYEN")]
        public void Mo()
        {
            using (BangBanQuyen f = new BangBanQuyen()) AcApp.ShowModalDialog(f);
        }
    }

    class BangBanQuyen : Form
    {
        readonly TextBox oEmail = new TextBox(), oMay = new TextBox(), oKey = new TextBox();
        readonly Label trangThai = new Label();
        int y = 36;

        public BangBanQuyen()
        {
            Text = "TKPCCC - Bản quyền";
            Font = GiaoDien.Chu;
            BackColor = Color.White;
            ForeColor = GiaoDien.ChuDam;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(480, 330);
            Controls.Add(GiaoDien.TieuDe("Bản quyền plugin TKPCCC"));

            string email, key;
            BanQuyen.DocLuu(out email, out key);
            oEmail.Text = email;
            oKey.Text = key;
            oMay.ReadOnly = true;
            oMay.Text = BanQuyen.MaMay();
            oKey.Multiline = true;
            oKey.ScrollBars = ScrollBars.Vertical;

            Dong("Email", oEmail, 24, null);
            Dong("Mã máy", oMay, 24, Nut("Chép", 70, delegate { Clipboard.SetText(oMay.Text); TB("Đã chép mã máy.", GiaoDien.ChuMo); }));
            Dong("Key", oKey, 70, null);

            FlowLayoutPanel hang = new FlowLayoutPanel();
            hang.SetBounds(12, y, 456, 40);
            hang.Controls.Add(Nut("Kích hoạt", 100, delegate { KichHoat(oKey.Text); }));
            hang.Controls.Add(Nut("Đăng ký", 100, delegate
            {
                Dictionary<string, object> d = BanQuyen.GoiWeb("dang_ky", CanEmail());
                TB(Lay(d, "tb"), GiaoDien.ChuDam);
            }));
            hang.Controls.Add(Nut("Lấy key tự động", 130, delegate
            {
                Dictionary<string, object> d = BanQuyen.GoiWeb("lay_key", CanEmail());
                string k = Lay(d, "key");
                if (k == "") TB(Lay(d, "tb") == "" ? "Chưa có key, thử lại sau ít phút." : Lay(d, "tb"), GiaoDien.Cam);
                else { oKey.Text = k; KichHoat(k); }
            }));
            string form = BanQuyen.CauHinh("google_form");
            if (form != "") hang.Controls.Add(Nut("Mở form", 90, delegate { System.Diagnostics.Process.Start(form); }));
            Controls.Add(hang);
            y += 46;

            trangThai.SetBounds(12, y, 456, 60);
            Controls.Add(trangThai);
            bool ok;
            string tt = BanQuyen.TrangThai(email, key, out ok);
            TB(tt, ok ? GiaoDien.XanhLa : GiaoDien.Do);
        }

        void Dong(string nhan, TextBox o, int cao, Button nut)
        {
            Label l = new Label();
            l.Text = nhan;
            l.Font = GiaoDien.Dam;
            l.SetBounds(12, y + 4, 70, 20);
            Controls.Add(l);
            o.SetBounds(86, y, nut == null ? 382 : 302, cao);
            Controls.Add(o);
            if (nut != null) { nut.SetBounds(394, y - 1, 74, 26); Controls.Add(nut); }
            y += cao + 10;
        }

        static NutBo Nut(string chu, int rong, Action viec)
        {
            NutBo n = new NutBo(chu, GiaoDien.XanhDam);
            n.Size = new Size(rong, 30);
            n.Click += delegate { GiaoDien.An(viec); };
            return n;
        }

        string CanEmail()
        {
            string e = BanQuyen.ChuanEmail(oEmail.Text);
            if (e.IndexOf('@') < 1) throw new System.Exception("Nhập email trước.");
            return e;
        }

        void KichHoat(string key)
        {
            string e = CanEmail();
            bool ok;
            string tt = BanQuyen.TrangThai(e, key, out ok);
            if (ok) BanQuyen.Luu(e, key); // chỉ lưu key đúng, key sai không ghi đè key cũ
            TB(ok ? "Đã kích hoạt. " + tt : tt, ok ? GiaoDien.XanhLa : GiaoDien.Do);
        }

        void TB(string s, Color mau) { trangThai.Text = s; trangThai.ForeColor = mau; }

        static string Lay(Dictionary<string, object> d, string k)
        {
            object v;
            return d != null && d.TryGetValue(k, out v) && v != null ? Convert.ToString(v) : "";
        }
    }
}
