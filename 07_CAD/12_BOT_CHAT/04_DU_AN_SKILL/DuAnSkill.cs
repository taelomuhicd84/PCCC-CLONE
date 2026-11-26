// Thư mục dự án (--add-dir), chỗ chứa skill, menu skill. Tách từ BotChat.cs (26/11/2026: mỗi thư mục số của 07_CAD là một hạng mục tính năng, xem 07_CAD/README.md).
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using AcAp = Autodesk.AutoCAD.ApplicationServices;
using Db = Autodesk.AutoCAD.DatabaseServices;

namespace TKPCCC
{
    partial class BotChat
    {
        void ThemThuMuc()
        {
            string d = ChonThuMuc.Hoi(this, "Thư mục gắn kèm để AI đọc làm ngữ cảnh", ThuMucLam());
            if (d == null || thuMuc.Contains(d, StringComparer.OrdinalIgnoreCase)) return;
            thuMuc.Add(d);
            LuuDs();
        }

        void ThemSkill()
        {
            string d = ChonThuMuc.Hoi(this, "Chỗ chứa skill (skills, plugin có skills, hoặc dự án có .claude\\skills)",
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".claude"));
            if (d == null || skill.Contains(d, StringComparer.OrdinalIgnoreCase)) return;
            skill.Add(d);
            LuuDs();
        }

        // ---------- Model ----------

        /// Thư mục dự án đang chọn; null = mặc định (<repo>\cad).
        string DuAn()
        {
            string d = GiaoDien.DocCaiDat("chat_du_an", "");
            return d == "" ? null : d;
        }

        /// Thư mục làm việc của CLI: dự án đang chọn (còn tồn tại), không thì <repo>\cad.
        string ThuMucLam()
        {
            string d = DuAn();
            return d != null && Directory.Exists(d) ? d : ThuMucCad();
        }

        void VeDuAn()
        {
            string d = DuAn();
            nutDuAn.Text = (d == null ? "TKPCCC CAD" : Path.GetFileName(d.TrimEnd('\\'))) + "  ▾";
            goiY.SetToolTip(nutDuAn, "Thư mục dự án: " + (ThuMucLam() ?? "?") + "\nAI làm việc trong thư mục này: đọc CLAUDE.md, dùng skill .claude\\skills"
                + "\nĐổi nhanh: kéo thư mục từ Explorer thả vào đây, hoặc copy đường dẫn rồi bấm \"Chọn thư mục…\" (hộp mở sẵn tới đó)");
            if (nutDuAn.Parent != null) // xếp lại giữa thanh đầu
                nutDuAn.Location = new Point(Math.Max(6, Math.Min((nutDuAn.Parent.Width - nutDuAn.Width) / 2, nutDuAn.Parent.Width - 100 - nutDuAn.Width)), 7);
        }

        /// Đổi dự án (d null = mặc định 07_CAD\): nhớ 8 thư mục gần nhất, mở hội thoại mới.
        void DoiDuAn(string d)
        {
            if (p != null) { trangThai.Text = "Đang chạy: đợi xong (hoặc Dừng) rồi mới đổi dự án."; return; }
            DatDuAn(d);
            HoiThoaiMoi();
            trangThai.Text = "Thư mục dự án: " + (ThuMucLam() ?? "?");
        }

        void DatDuAn(string d)
        {
            if (d != null)
            {
                duAnDs.RemoveAll(x => string.Equals(x, d, StringComparison.OrdinalIgnoreCase));
                duAnDs.Insert(0, d);
                if (duAnDs.Count > 8) duAnDs.RemoveRange(8, duAnDs.Count - 8);
                GiaoDien.LuuCaiDat("chat_du_an_ds", string.Join("|", duAnDs));
            }
            GiaoDien.LuuCaiDat("chat_du_an", d ?? "");
            VeDuAn();
        }

        void MenuDuAn()
        {
            ContextMenuStrip m = new ContextMenuStrip();
            string dang = DuAn();
            m.Items.Add(Nhom("Thư mục dự án (AI đọc CLAUDE.md, skill .claude\\skills)"));
            ToolStripMenuItem mac = new ToolStripMenuItem("TKPCCC CAD (mặc định)", null, delegate { DoiDuAn(null); });
            mac.Checked = dang == null;
            m.Items.Add(mac);
            foreach (string d in duAnDs)
            {
                string dd = d;
                ToolStripMenuItem i = new ToolStripMenuItem(Path.GetFileName(d.TrimEnd('\\')), null, delegate { DoiDuAn(dd); });
                i.ToolTipText = d;
                i.Checked = string.Equals(d, dang, StringComparison.OrdinalIgnoreCase);
                m.Items.Add(i);
            }
            m.Items.Add(new ToolStripSeparator());
            m.Items.Add("Chọn thư mục…  (copy đường dẫn trước: mở thẳng tới đó)", null, delegate
            {
                GiaoDien.An(delegate
                {
                    string d = ChonThuMuc.Hoi(this, "Thư mục dự án (AI đọc CLAUDE.md, dùng skill .claude\\skills)", ThuMucLam());
                    if (d != null) DoiDuAn(d);
                });
            });
            string cb = ChonThuMuc.TuClipboard();
            if (cb != null) m.Items.Add("Dùng đường dẫn đã copy: " + cb, null, delegate { DoiDuAn(cb); });
            HienMenu(m, nutDuAn);
        }

        // ---------- Hội thoại, lịch sử, link phiên ----------

        /// Menu "/": skill của thư mục dự án, của <repo>\cad (luôn gắn), thư mục gắn kèm, chỗ chứa skill. Chọn = chèn "/tên " đầu ô nhập.
        void MenuSkill()
        {
            List<string> ten = new List<string>();
            string lam = ThuMucLam(), cad = ThuMucCad();
            foreach (string d in new[] { lam, cad }.Concat(thuMuc).Where(d => d != null && Directory.Exists(d)))
                ten.AddRange(KhoPhien.TenSkill(Path.Combine(d, ".claude", "skills"), null));
            foreach (string d in skill.Where(Directory.Exists))
            {
                string tienTo, ms;
                KhoPhien.CoSkill(d, out tienTo, out ms);
                ten.AddRange(KhoPhien.TenSkill(ms, tienTo));
            }
            ContextMenuStrip m = new ContextMenuStrip();
            foreach (string t in ten.Distinct().OrderBy(t => t))
            {
                string tt = t;
                m.Items.Add("/" + t, null, delegate { DatCau("/" + tt + " " + nhap.Text.TrimStart()); });
            }
            if (m.Items.Count == 0) m.Items.Add("(chưa có skill: + → Chỗ chứa skill, hoặc chọn dự án có .claude\\skills)").Enabled = false;
            HienMenu(m, khungSoan);
        }

    }
}
