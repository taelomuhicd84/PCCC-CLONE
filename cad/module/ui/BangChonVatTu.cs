// Bảng chọn thiết bị/vật tư: hỏi mã (PCCC_RAI, PCCC_DAT, PCCC_TUYEN, PCCC_VUNGBV...) mà gõ "?" hoặc chữ khớp nhiều mã thì mở bảng này
// thay cho in danh sách ra dòng lệnh. Lenh.HoiMa (TKPCCC.Cad.dll) gọi qua Lenh.ChonVatTu, KhoiDongUi gán.
// Gõ để lọc (không cần dấu), mũi tên lên/xuống, Enter hoặc bấm đúp = chọn, Esc = đóng (dòng lệnh hỏi lại).
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using AcApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace TKPCCC
{
    class BangChonVatTu : Form
    {
        readonly DataGridView ds = GiaoDien.Bang();
        readonly TextBox loc = new TextBox();
        readonly List<VatTu> tatCa;
        readonly Dictionary<string, Image> anh = new Dictionary<string, Image>();
        VatTu chon;

        const int C_ANH = 0, C_TEN = 1;

        public static VatTu Chon(string tieuDe, List<VatTu> dsVatTu, string chuLoc)
        {
            using (BangChonVatTu f = new BangChonVatTu(tieuDe, dsVatTu, chuLoc))
            {
                AcApp.ShowModalDialog(f);
                return f.chon;
            }
        }

        BangChonVatTu(string tieuDe, List<VatTu> dsVatTu, string chuLoc)
        {
            tatCa = dsVatTu;
            Text = "TKPCCC - " + tieuDe;
            Font = GiaoDien.Chu;
            BackColor = GiaoDien.The;
            StartPosition = FormStartPosition.CenterScreen;
            Size = new Size(520, 640);
            MinimumSize = new Size(360, 320);
            MinimizeBox = MaximizeBox = false;
            ShowInTaskbar = false;

            foreach (VatTu v in tatCa)
            {
                string f = Path.Combine(Lenh.ThuMucAnh, v.ma + ".png");
                if (!File.Exists(f)) continue;
                // Đọc qua bộ nhớ để không giữ khoá file (PCCC_LIB còn vẽ lại ảnh).
                using (MemoryStream ms = new MemoryStream(File.ReadAllBytes(f)))
                using (Image im = Image.FromStream(ms))
                    anh[v.ma] = new Bitmap(im);
            }
            FormClosed += delegate { foreach (Image im in anh.Values) im.Dispose(); };

            ds.ReadOnly = true;
            ds.RowTemplate.Height = anh.Count > 0 ? 50 : 40;
            DataGridViewImageColumn cAnh = new DataGridViewImageColumn();
            cAnh.HeaderText = "Ký hiệu";
            cAnh.Width = 62;
            cAnh.ImageLayout = DataGridViewImageCellLayout.Zoom;
            cAnh.DefaultCellStyle.NullValue = null; // không có ảnh: ô trống, không hiện dấu X đỏ
            cAnh.DefaultCellStyle.Padding = new Padding(4);
            cAnh.DefaultCellStyle.BackColor = cAnh.DefaultCellStyle.SelectionBackColor = Color.Black; // ảnh nền đen như màn hình CAD
            cAnh.Visible = anh.Count > 0; // ống, dây: không có ký hiệu
            ds.Columns.Add(cAnh);
            DataGridViewTextBoxColumn cTen = GiaoDien.Cot("Tên thiết bị / mã", 200, false);
            cTen.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            ds.Columns.Add(cTen);
            ds.Columns.Add(GiaoDien.Cot("Đơn vị", 58, true));
            ds.CellPainting += VeTen;
            ds.CellDoubleClick += delegate(object s, DataGridViewCellEventArgs e) { if (e.RowIndex >= 0) Xong(); };
            loc.TextChanged += delegate { Loc(); };

            Panel duoi = new Panel();
            duoi.Dock = DockStyle.Bottom;
            duoi.Height = 46;
            duoi.BackColor = GiaoDien.Nen;
            duoi.Padding = new Padding(8);
            NutBo nutChon = new NutBo("Chọn", GiaoDien.XanhDam);
            NutBo nutDong = new NutBo("Đóng", GiaoDien.ChuMo);
            nutChon.Click += delegate { Xong(); };
            nutDong.Click += delegate { Close(); };
            Label goiY = new Label();
            goiY.Text = "Enter = chọn · Esc = đóng";
            goiY.ForeColor = GiaoDien.ChuMo;
            goiY.TextAlign = ContentAlignment.MiddleLeft;
            goiY.Dock = DockStyle.Fill;
            foreach (NutBo b in new[] { nutChon, nutDong }) { b.Dock = DockStyle.Right; b.Width = 96; }
            duoi.Controls.Add(goiY);
            duoi.Controls.Add(nutDong);
            duoi.Controls.Add(new Panel { Dock = DockStyle.Right, Width = 8 });
            duoi.Controls.Add(nutChon);

            // Thứ tự Add quyết định cách xếp Dock: điều khiển Fill phải thêm trước.
            Controls.Add(ds);
            Controls.Add(GiaoDien.OTim(loc, "Gõ một phần tên hoặc mã (không cần dấu)..."));
            Controls.Add(GiaoDien.TieuDe(tieuDe + "  (" + tatCa.Count + ")"));
            Controls.Add(duoi);

            Load += delegate
            {
                loc.Text = chuLoc ?? "";
                Loc();
                ActiveControl = loc;
                loc.SelectionStart = loc.Text.Length;
            };
        }

        /// Lọc như Lenh.HoiMa: chữ thường không dấu, một phần mã hoặc tên.
        void Loc()
        {
            string k = Lenh.BoDau(loc.Text.Trim());
            ds.SuspendLayout();
            ds.Rows.Clear();
            foreach (VatTu v in tatCa)
            {
                if (k != "" && !Lenh.BoDau(v.ma).Contains(k) && !Lenh.BoDau(v.ten).Contains(k)) continue;
                Image im;
                anh.TryGetValue(v.ma, out im);
                ds.Rows[ds.Rows.Add(im, v.ten, v.dvt ?? "")].Tag = v;
            }
            if (ds.Rows.Count > 0) ds.CurrentCell = ds.Rows[0].Cells[C_TEN];
            ds.ResumeLayout();
            ds.Text = "Vật tư: " + ds.Rows.Count; // thu_gui.ps1 đọc số dòng qua UI Automation như bảng PCCC_BLOCK
        }

        void Xong()
        {
            chon = ds.CurrentRow != null ? ds.CurrentRow.Tag as VatTu : null;
            if (chon == null) return;
            DialogResult = DialogResult.OK;
            Close();
        }

        /// Enter, Esc, lên/xuống xử lý ở đây (trước DataGridView, nó tự dời dòng khi Enter) để ô tìm vẫn giữ con trỏ.
        protected override bool ProcessCmdKey(ref Message m, Keys k)
        {
            if (k == Keys.Enter) { Xong(); return true; }
            if (k == Keys.Escape) { Close(); return true; }
            if ((k == Keys.Down || k == Keys.Up) && loc.Focused && ds.Rows.Count > 0)
            {
                int i = (ds.CurrentRow == null ? 0 : ds.CurrentRow.Index) + (k == Keys.Down ? 1 : -1);
                ds.CurrentCell = ds.Rows[Math.Max(0, Math.Min(ds.Rows.Count - 1, i))].Cells[C_TEN];
                return true;
            }
            return base.ProcessCmdKey(ref m, k);
        }

        /// Cột tên: tên đậm + mã nhỏ màu xám bên dưới (như bảng PCCC_BLOCK).
        void VeTen(object s, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex != C_TEN) return;
            VatTu v = ds.Rows[e.RowIndex].Tag as VatTu;
            if (v == null) return;
            e.PaintBackground(e.ClipBounds, (e.State & DataGridViewElementStates.Selected) != 0);
            Rectangle o = e.CellBounds;
            Rectangle tren = new Rectangle(o.X + 4, o.Y + 4, o.Width - 8, o.Height / 2 - 2);
            Rectangle duoi = new Rectangle(o.X + 4, o.Y + o.Height / 2 + 1, o.Width - 8, o.Height / 2 - 4);
            TextRenderer.DrawText(e.Graphics, v.ten, GiaoDien.Dam, tren, GiaoDien.ChuDam, TextFormatFlags.Left | TextFormatFlags.Bottom | TextFormatFlags.EndEllipsis);
            TextRenderer.DrawText(e.Graphics, v.ma, GiaoDien.Nho, duoi, GiaoDien.ChuMo, TextFormatFlags.Left | TextFormatFlags.Top | TextFormatFlags.EndEllipsis);
            e.Handled = true;
        }
    }
}
