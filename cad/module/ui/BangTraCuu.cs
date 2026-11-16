// Nút "Tra cứu TCVN" của tab Khoang dầm (ý từ video plugin 16/11: bảng luật ngay tại chỗ thao tác, khỏi mở PDF).
// Nội dung đọc lúc chạy từ MOD_dau_bao_khoi_khoang_dam.md cạnh DLL (build.bat chép từ skills/ gốc repo, nguồn duy nhất, nguyên văn 5.9.1.1.6):
// mục "## 2." thành bảng, mục "## 3." (khoảng cách luôn phải đạt) thành đoạn chữ bên dưới. Chỉ giao diện.
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace TKPCCC
{
    static class BangTraCuu
    {
        const string FILE = "MOD_dau_bao_khoi_khoang_dam.md";
        static Form dang;

        public static void Mo()
        {
            if (dang != null && !dang.IsDisposed) { dang.Activate(); return; }
            string f = Path.Combine(Path.GetDirectoryName(typeof(Lenh).Assembly.Location), FILE);
            if (!File.Exists(f)) throw new Exception("Thiếu " + FILE + " cạnh DLL: chạy lại cad\\build.bat.");
            List<string[]> hang = new List<string[]>();
            StringBuilder kc = new StringBuilder();
            string tieuDe = "", muc = "";
            foreach (string d in File.ReadAllLines(f, Encoding.UTF8))
            {
                if (d.StartsWith("# ")) { tieuDe = d.Substring(2).Replace("MODULE:", "").Trim(); continue; }
                if (d.StartsWith("## ")) { muc = d.Substring(3, Math.Min(2, d.Length - 3)); continue; }
                string s = d.Replace("**", "").Trim();
                if (muc == "2." && s.StartsWith("|") && !s.StartsWith("|---") && !s.StartsWith("| TH"))
                {
                    string[] o = s.Trim('|').Split('|').Select(x => x.Trim()).ToArray();
                    if (o.Length >= 4) hang.Add(o);
                }
                else if (muc == "3." && s != "") kc.AppendLine(s.TrimStart('-', ' ').Trim());
            }

            Form fm = new Form();
            fm.Text = "Tra cứu TCVN — " + tieuDe;
            fm.Size = new Size(760, 560);
            fm.MinimumSize = new Size(480, 360);
            fm.StartPosition = FormStartPosition.CenterScreen;
            fm.BackColor = GiaoDien.Nen;
            fm.ForeColor = GiaoDien.ChuDam;
            fm.Font = GiaoDien.Chu;
            fm.ShowInTaskbar = false;

            Label dau = new Label();
            dau.Dock = DockStyle.Top;
            dau.Height = 44;
            dau.Text = tieuDe;
            dau.Font = new Font("Segoe UI", 11f, FontStyle.Bold);
            dau.ForeColor = Color.White;
            dau.BackColor = GiaoDien.MauDuong;
            dau.TextAlign = ContentAlignment.MiddleLeft;
            dau.Padding = new Padding(12, 0, 0, 0);

            DataGridView g = GiaoDien.Bang();
            g.Columns.Add(GiaoDien.Cot("TH", 40, true));
            g.Columns.Add(GiaoDien.Cot("Điều kiện (h trần, d dầm, S ô)", 240, false));
            g.Columns.Add(GiaoDien.Cot("Cách đặt đầu báo", 300, false));
            g.Columns.Add(GiaoDien.Cot("Mục", 50, true));
            g.Columns[2].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            g.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
            g.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
            foreach (string[] h in hang) g.Rows.Add(h[0], h[1], h[2], h[3]);
            g.AccessibleName = "Bảng khoang dầm 5.9.1.1.6";

            TextBox ghi = new TextBox();
            ghi.Multiline = true;
            ghi.ReadOnly = true;
            ghi.ScrollBars = ScrollBars.Vertical;
            ghi.Dock = DockStyle.Bottom;
            ghi.Height = 150;
            ghi.BackColor = GiaoDien.The;
            ghi.ForeColor = GiaoDien.ChuDam;
            ghi.BorderStyle = BorderStyle.None;
            ghi.Text = (kc.Length > 0 ? "KHOẢNG CÁCH LUÔN PHẢI ĐẠT\r\n" + kc.ToString().Replace("\n", "\r\n").Replace("\r\r", "\r") : "")
                + "\r\nNguồn: " + f;

            Panel duoi = new Panel();
            duoi.Dock = DockStyle.Bottom;
            duoi.Height = 44;
            duoi.BackColor = GiaoDien.Nen;
            NutBo dong = new NutBo("ĐÓNG", GiaoDien.MauDuong);
            dong.Size = new Size(110, 30);
            dong.Anchor = AnchorStyles.Right | AnchorStyles.Top;
            dong.Location = new Point(duoi.Width - 122, 7);
            dong.Click += delegate { fm.Close(); };
            duoi.Controls.Add(dong);
            duoi.Resize += delegate { dong.Left = duoi.Width - 122; };
            fm.CancelButton = dong;

            fm.Controls.Add(g);
            fm.Controls.Add(ghi);
            fm.Controls.Add(duoi);
            fm.Controls.Add(dau);
            dang = fm;
            fm.Show(); // không chặn AutoCAD: vừa xem bảng vừa bấm bản vẽ
        }
    }
}
