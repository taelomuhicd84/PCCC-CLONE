// Bảng lệnh PCCC (lệnh PCCC_HELP trong AutoCAD có giao diện): mọi lệnh với việc làm, cách dùng, tìm được; sửa phím tắt ngay trong bảng.
// Phím tắt lưu ở phim_tat.txt cạnh DLL (mỗi dòng PHIMTAT=LENH) và được đăng ký thành lệnh thật khi nạp plugin:
// KHÔNG sửa acad.pgp của người dùng. Danh sách lệnh lấy từ Lenh.DS_LENH (TKPCCC.Cad.dll).
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;
using AcApp = Autodesk.AutoCAD.ApplicationServices.Application;
using AcUtils = Autodesk.AutoCAD.Internal.Utils;

namespace TKPCCC
{
    /// Phím tắt người dùng tự đặt cho lệnh PCCC.
    static class PhimTat
    {
        const string NHOM = "TKPCCC_PHIMTAT";
        /// lệnh -> phím tắt
        public static readonly Dictionary<string, string> TheoLenh = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public static string FilePhimTat { get { return Path.Combine(Path.GetDirectoryName(typeof(PhimTat).Assembly.Location), "phim_tat.txt"); } }

        /// Đọc phim_tat.txt, đăng ký từng phím tắt. Trả mô tả các dòng bỏ qua ("" = không có).
        public static string Nap()
        {
            List<string> boQua = new List<string>();
            if (!File.Exists(FilePhimTat)) return "";
            foreach (string dong in File.ReadAllLines(FilePhimTat, Encoding.UTF8))
            {
                string d = dong.Trim();
                if (d == "" || d.StartsWith("#")) continue;
                string[] p = d.Split('=');
                string loi = p.Length != 2 ? "sai dạng PHIMTAT=LENH" : Kiem(p[1].Trim(), p[0].Trim());
                if (loi != null) { boQua.Add(d + " (" + loi + ")"); continue; }
                DangKy(p[1].Trim().ToUpperInvariant(), p[0].Trim().ToUpperInvariant());
            }
            return string.Join("; ", boQua);
        }

        /// Lý do không đặt được phím tắt a cho lệnh, null = được.
        public static string Kiem(string lenh, string a)
        {
            a = a.ToUpperInvariant();
            if (!Lenh.DS_LENH.Any(l => string.Equals(l[0], lenh, StringComparison.OrdinalIgnoreCase))) return "không có lệnh " + lenh;
            if (!Regex.IsMatch(a, "^[A-Z0-9_]{1,20}$")) return "chỉ dùng chữ A-Z, số, gạch dưới, tối đa 20 ký tự";
            if (Lenh.DS_LENH.Any(l => l[0] == a)) return "trùng tên lệnh PCCC";
            foreach (KeyValuePair<string, string> p in TheoLenh)
                if (p.Value == a && !string.Equals(p.Key, lenh, StringComparison.OrdinalIgnoreCase)) return "đã là phím tắt của " + p.Key;
            string cu;
            bool cuaMinh = TheoLenh.TryGetValue(lenh, out cu) && cu == a;
            if (!cuaMinh && AcUtils.IsCommandNameInUse(a) != Autodesk.AutoCAD.Internal.CommandTypeFlags.NoneCmd) return "trùng lệnh có sẵn của AutoCAD";
            string pgp = TrongPgp(a);
            if (pgp != null) return "trùng phím tắt AutoCAD " + a + " = " + pgp + " (acad.pgp)";
            return null;
        }

        /// Lệnh mà acad.pgp đang gán cho phím tắt a (null = chưa gán). Chỉ đọc, không sửa acad.pgp.
        static string TrongPgp(string a)
        {
            string f;
            try { f = HostApplicationServices.Current.FindFile("acad.pgp", null, FindFileHint.Default); }
            catch (System.Exception) { return null; }
            if (string.IsNullOrEmpty(f) || !File.Exists(f)) return null;
            foreach (string dong in File.ReadAllLines(f))
            {
                int phay = dong.IndexOf(',');
                if (dong.StartsWith(";") || phay <= 0) continue;
                if (string.Equals(dong.Substring(0, phay).Trim(), a, StringComparison.OrdinalIgnoreCase)) return dong.Substring(phay + 1).Trim().TrimStart('*');
            }
            return null;
        }

        static void DangKy(string lenh, string a)
        {
            // Phím tắt là một lệnh thật trong nhóm riêng: chạy thì gửi lệnh PCCC thật.
            AcUtils.AddCommand(NHOM, a, a, CommandFlags.Modal, delegate
            {
                Document doc = AcApp.DocumentManager.MdiActiveDocument;
                if (doc != null) doc.SendStringToExecute(lenh + "\n", true, false, true);
            });
            TheoLenh[lenh] = a;
        }

        /// Đặt (a rỗng = bỏ) phím tắt cho lệnh, đăng ký lại và ghi phim_tat.txt. Ném lỗi khi không hợp lệ.
        public static void Dat(string lenh, string a)
        {
            a = (a ?? "").Trim().ToUpperInvariant();
            string cu;
            if (TheoLenh.TryGetValue(lenh, out cu))
            {
                if (cu == a) return;
                if (a != "") { string loi0 = Kiem(lenh, a); if (loi0 != null) throw new System.Exception(a + ": " + loi0); }
                AcUtils.RemoveCommand(NHOM, cu);
                TheoLenh.Remove(lenh);
            }
            else if (a != "")
            {
                string loi = Kiem(lenh, a);
                if (loi != null) throw new System.Exception(a + ": " + loi);
            }
            if (a != "") DangKy(lenh, a);
            StringBuilder sb = new StringBuilder("# Phim tat lenh PCCC (PCCC_HELP sua va ghi file nay). Moi dong: PHIMTAT=LENH\r\n");
            foreach (KeyValuePair<string, string> p in TheoLenh.OrderBy(x => x.Key)) sb.Append(p.Value + "=" + p.Key + "\r\n");
            File.WriteAllText(FilePhimTat, sb.ToString(), Encoding.UTF8);
        }
    }

    public class BangLenh : UserControl
    {
        readonly DataGridView ds = GiaoDien.Bang();
        readonly TextBox loc = new TextBox();
        readonly OTong tong = new OTong("Lệnh PCCC");
        const int C_NUT = 0, C_LENH = 1, C_PHIM = 2, C_VIEC = 3, C_CACH = 4;

        public BangLenh()
        {
            Font = GiaoDien.Chu;
            BackColor = Color.White;

            ds.RowTemplate.Height = 40;
            ds.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
            ds.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
            ds.EditMode = DataGridViewEditMode.EditOnKeystrokeOrF2;
            ds.Columns.Add(GiaoDien.Cot("", 36, true));
            DataGridViewTextBoxColumn cLenh = GiaoDien.Cot("Lệnh", 130, false);
            cLenh.DefaultCellStyle.Font = GiaoDien.Dam;
            cLenh.DefaultCellStyle.WrapMode = DataGridViewTriState.False;
            ds.Columns.Add(cLenh);
            DataGridViewTextBoxColumn cPhim = GiaoDien.Cot("Phím tắt", 78, true);
            cPhim.ReadOnly = false;
            cPhim.DefaultCellStyle.Font = GiaoDien.Dam;
            cPhim.DefaultCellStyle.ForeColor = GiaoDien.Do;
            cPhim.DefaultCellStyle.BackColor = Color.FromArgb(255, 250, 230);
            cPhim.ToolTipText = "Bấm đúp hoặc gõ để sửa, Enter để lưu; xoá trắng = bỏ phím tắt";
            ds.Columns.Add(cPhim);
            DataGridViewTextBoxColumn cViec = GiaoDien.Cot("Việc làm", 180, false);
            cViec.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            cViec.FillWeight = 45;
            cViec.MinimumWidth = 160;
            ds.Columns.Add(cViec);
            DataGridViewTextBoxColumn cCach = GiaoDien.Cot("Cách dùng", 220, false);
            cCach.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            cCach.FillWeight = 55;
            cCach.MinimumWidth = 220;
            cCach.DefaultCellStyle.ForeColor = GiaoDien.ChuMo;
            ds.Columns.Add(cCach);
            ds.CellPainting += delegate(object s, DataGridViewCellPaintingEventArgs e)
            {
                if (e.RowIndex < 0 || e.ColumnIndex != C_NUT) return;
                e.PaintBackground(e.ClipBounds, (e.State & DataGridViewElementStates.Selected) != 0);
                GiaoDien.Tron(e.Graphics, e.CellBounds.X + 7, e.CellBounds.Y + e.CellBounds.Height / 2, GiaoDien.XanhLa, "▶");
                e.Handled = true;
            };
            ds.CellToolTipTextNeeded += delegate(object s, DataGridViewCellToolTipTextNeededEventArgs e)
            {
                if (e.ColumnIndex == C_NUT && e.RowIndex >= 0) e.ToolTipText = "Chạy lệnh";
            };
            ds.CellMouseClick += delegate(object s, DataGridViewCellMouseEventArgs e) { if (e.RowIndex >= 0 && e.ColumnIndex == C_NUT) Chay(); };
            ds.CellDoubleClick += delegate(object s, DataGridViewCellEventArgs e)
            {
                if (e.RowIndex < 0 || e.ColumnIndex == C_NUT) return;
                if (e.ColumnIndex == C_PHIM) ds.BeginEdit(true);
                else Chay();
            };
            ds.CellValidating += KiemPhim;
            ds.CellEndEdit += LuuPhim;

            loc.TextChanged += delegate { Loc(); };

            Panel trai = new Panel();
            trai.Dock = DockStyle.Fill;
            trai.Controls.Add(ds);
            trai.Controls.Add(GiaoDien.OTim(loc, "Tìm lệnh theo tên, phím tắt, việc làm..."));
            trai.Controls.Add(GiaoDien.TieuDe("Lệnh PCCC"));
            Controls.Add(trai);

            FlowLayoutPanel hd = GiaoDien.KhungHanhDong(this, 190);
            NutBo chay = new NutBo("Chạy lệnh", GiaoDien.XanhDam);
            chay.Click += delegate { Chay(); };
            NutBo sua = new NutBo("Sửa phím tắt", GiaoDien.XanhDam);
            sua.Click += delegate
            {
                if (ds.CurrentRow == null) return;
                ds.CurrentCell = ds.CurrentRow.Cells[C_PHIM];
                ds.BeginEdit(true);
            };
            NutBo moFile = new NutBo("Mở file phím tắt", GiaoDien.XanhDam);
            moFile.Click += delegate
            {
                GiaoDien.An(delegate
                {
                    if (!File.Exists(PhimTat.FilePhimTat)) throw new System.Exception("Chưa đặt phím tắt nào (" + PhimTat.FilePhimTat + ").");
                    System.Diagnostics.Process.Start("notepad.exe", "\"" + PhimTat.FilePhimTat + "\"");
                });
            };
            foreach (NutBo b in new[] { chay, sua, moFile }) GiaoDien.Them(hd, b, 32);
            Label goiY = new Label();
            goiY.Text = "Sửa ô Phím tắt (nền vàng) rồi Enter: lưu cho lần sau, dùng được ngay. Xoá trắng = bỏ phím tắt. Không sửa acad.pgp; không nhận phím tắt trùng lệnh hay phím tắt sẵn có của AutoCAD.";
            goiY.ForeColor = GiaoDien.ChuMo;
            goiY.Font = GiaoDien.Nho;
            GiaoDien.Them(hd, goiY, 110);
            GiaoDien.Them(hd, tong, 80);
        }

        public void Nap() { Loc(); }

        void Loc()
        {
            string k = loc.Text.Trim().ToLowerInvariant();
            ds.Rows.Clear();
            foreach (string[] l in Lenh.DS_LENH)
            {
                string a;
                PhimTat.TheoLenh.TryGetValue(l[0], out a);
                if (k != "" && !(l[0] + " " + a + " " + l[1] + " " + l[2]).ToLowerInvariant().Contains(k)) continue;
                ds.Rows.Add("", l[0], a ?? "", l[1], l[2]);
            }
            ds.Text = "Lenh: " + ds.Rows.Count; // thu_gui.ps1 đọc số dòng (chữ của cửa sổ) qua UI Automation
            tong.Dat(Lenh.DS_LENH.Length.ToString(), PhimTat.TheoLenh.Count + " phím tắt đã đặt");
        }

        void KiemPhim(object s, DataGridViewCellValidatingEventArgs e)
        {
            if (e.ColumnIndex != C_PHIM || !ds.IsCurrentCellInEditMode) return;
            string a = (Convert.ToString(e.FormattedValue) ?? "").Trim();
            string lenh = (string)ds.Rows[e.RowIndex].Cells[C_LENH].Value;
            string cu;
            PhimTat.TheoLenh.TryGetValue(lenh, out cu);
            if (a == "" || string.Equals(a, cu, StringComparison.OrdinalIgnoreCase)) return;
            string loi = PhimTat.Kiem(lenh, a);
            if (loi == null) return;
            MessageBox.Show(a.ToUpperInvariant() + ": " + loi + ".\nSửa lại hoặc bấm Esc để huỷ.", "TKPCCC - Phím tắt");
            e.Cancel = true;
        }

        void LuuPhim(object s, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex != C_PHIM) return;
            DataGridViewCell o = ds.Rows[e.RowIndex].Cells[C_PHIM];
            string lenh = (string)ds.Rows[e.RowIndex].Cells[C_LENH].Value;
            GiaoDien.An(delegate { PhimTat.Dat(lenh, Convert.ToString(o.Value)); });
            string a;
            PhimTat.TheoLenh.TryGetValue(lenh, out a);
            o.Value = a ?? ""; // chữ hoa; lưu lỗi thì trả về phím tắt cũ
            tong.Dat(Lenh.DS_LENH.Length.ToString(), PhimTat.TheoLenh.Count + " phím tắt đã đặt");
        }

        void Chay()
        {
            if (ds.CurrentRow == null) return;
            GiaoDien.GuiLenh((string)ds.CurrentRow.Cells[C_LENH].Value + "\n", null, null);
        }
    }
}
