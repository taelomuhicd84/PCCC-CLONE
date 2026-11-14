// Bảng block PCCC (lệnh PCCC_BLOCK): PaletteSet liệt kê block của thư viện theo nhóm hệ, kèm ký hiệu, tên, đơn vị, số lượng đang có trong bản vẽ.
// Bấm đúp một dòng hoặc nút tròn xanh = PCCC_DAT <mã>; nút tròn cam = PCCC_RAI <mã>.
// DLL riêng (TKPCCC.UI.dll) vì cần acmgd.dll, thứ accoreconsole không nạp được. KHÔNG xử lý bản vẽ ở đây: danh mục, ảnh xem trước,
// đếm và việc thêm block do TKPCCC.Cad.dll làm (cad/module/plugin/Block.cs), nhờ vậy phần đó thử được bằng cad/test/chay_thu.ps1.
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Windows;
using AcApp = Autodesk.AutoCAD.ApplicationServices.Application;

[assembly: CommandClass(typeof(TKPCCC.LenhUi))]
[assembly: ExtensionApplication(typeof(TKPCCC.KhoiDongUi))]

namespace TKPCCC
{
    /// PCCC, PCCC_BLOCK, PCCC_HELP đều mở palette chính (BangChinh.cs), khác nhau ở phân hệ hiện ra.
    public class LenhUi
    {
        static void Mo(string trang, string lenh)
        {
            Document doc = AcApp.DocumentManager.MdiActiveDocument;
            try
            {
                string tb = BangChinh.Mo(trang);
                if (doc != null && lenh != null) doc.Editor.WriteMessage("\n[TKPCCC] " + lenh + ": " + tb);
            }
            catch (System.Exception ex) { if (doc != null) doc.Editor.WriteMessage("\n[TKPCCC] Lỗi: " + ex.Message); }
        }

        [CommandMethod("PCCC")] public void MoPccc() { Mo(null, "PCCC"); }
        [CommandMethod("PCCC_BLOCK")] public void MoBang() { Mo(BangChinh.THU_VIEN, "PCCC_BLOCK"); }
        [CommandMethod("PCCC_BOTRI")] public void MoBoTri() { Mo(BangChinh.BO_TRI, "PCCC_BOTRI"); }
        [CommandMethod("PCCC_TIENICH")] public void MoTienIch() { Mo(BangChinh.TIEN_ICH, "PCCC_TIENICH"); }

        /// PCCC_HELP (lệnh ở TKPCCC.Cad.dll) gọi qua Lenh.MoBangLenh.
        public static void MoBangLenh() { Mo(BangChinh.LENH, null); }
    }

    /// Nạp cùng TKPCCC.UI.dll: PCCC_HELP mở bảng lệnh, đăng ký phím tắt người dùng đã lưu.
    public class KhoiDongUi : IExtensionApplication
    {
        public void Initialize()
        {
            Lenh.MoBangLenh = LenhUi.MoBangLenh;
            Lenh.ChonVatTu = BangChonVatTu.Chon;
            string loi = PhimTat.Nap();
            Document doc = AcApp.DocumentManager.MdiActiveDocument;
            if (loi != "" && doc != null) doc.Editor.WriteMessage("\n[TKPCCC] Phím tắt bỏ qua: " + loi);
        }

        public void Terminate() { }
    }

    public class BangBlock : UserControl
    {
        readonly DataGridView ds = GiaoDien.Bang();
        readonly TextBox loc = new TextBox();
        readonly ComboBox chonHe = new ComboBox();
        readonly CheckBox chiCo = new CheckBox();
        readonly PictureBox xem = new PictureBox();
        readonly Label tenXem = new Label();
        readonly OTong tong = new OTong("Thiết bị trong bản vẽ");
        readonly NutBo nutDat = new NutBo("Đặt thiết bị", GiaoDien.XanhDam);
        readonly NutBo nutRai = new NutBo("Rải theo lưới", GiaoDien.XanhDam);
        readonly Dictionary<string, Image> anh = new Dictionary<string, Image>();
        List<VatTu> tatCa = new List<VatTu>();
        Dictionary<string, string> he = new Dictionary<string, string>();
        Dictionary<string, double> soLuong = new Dictionary<string, double>();

        const int C_NUT = 0, C_ANH = 1, C_TEN = 2, C_DVT = 3, C_SL = 4;

        public BangBlock()
        {
            Font = GiaoDien.Chu;
            BackColor = GiaoDien.The;

            ds.ReadOnly = true;
            ds.RowTemplate.Height = 50;
            ds.Columns.Add(GiaoDien.Cot("", 62, true));
            DataGridViewImageColumn cAnh = new DataGridViewImageColumn();
            cAnh.HeaderText = "Ký hiệu";
            cAnh.Width = 62;
            cAnh.ImageLayout = DataGridViewImageCellLayout.Zoom;
            cAnh.DefaultCellStyle.NullValue = null; // block chưa có ảnh: ô trống, không hiện dấu X đỏ
            cAnh.DefaultCellStyle.Padding = new Padding(4);
            cAnh.DefaultCellStyle.BackColor = cAnh.DefaultCellStyle.SelectionBackColor = Color.Black; // ảnh nền đen như màn hình CAD
            ds.Columns.Add(cAnh);
            DataGridViewTextBoxColumn cTen = GiaoDien.Cot("Tên thiết bị", 200, false);
            cTen.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            ds.Columns.Add(cTen);
            ds.Columns.Add(GiaoDien.Cot("Đơn vị", 58, true));
            DataGridViewTextBoxColumn cSl = GiaoDien.Cot("Số lượng", 70, true);
            cSl.DefaultCellStyle.Font = GiaoDien.Dam;
            ds.Columns.Add(cSl);
            // Palette hẹp: bỏ cột Đơn vị, thu cột số lượng để cột tên còn chỗ.
            Resize += delegate
            {
                bool hep = Width < 640;
                ds.Columns[3].Visible = !hep;
                cSl.Width = hep ? 46 : 70;
                cSl.HeaderText = hep ? "SL" : "Số lượng";
            };
            ds.RowPrePaint += VeNhom;
            ds.CellPainting += VeO;
            ds.CellMouseClick += BamO;
            ds.CellToolTipTextNeeded += delegate(object s, DataGridViewCellToolTipTextNeededEventArgs e)
            {
                if (e.ColumnIndex == C_NUT && e.RowIndex >= 0) e.ToolTipText = "Xanh: đặt thiết bị (PCCC_DAT)\nCam: rải theo lưới trong phòng (PCCC_RAI)";
            };
            ds.CellDoubleClick += delegate(object s, DataGridViewCellEventArgs e) { if (e.RowIndex >= 0 && e.ColumnIndex != C_NUT) Dat(); };
            ds.KeyDown += delegate(object s, KeyEventArgs e) { if (e.KeyCode == Keys.Enter) { Dat(); e.Handled = true; } };
            ds.SelectionChanged += delegate { XemTruoc(); };

            loc.TextChanged += delegate { Loc(); };

            // Thứ tự Add quyết định cách xếp Dock: điều khiển Fill phải thêm trước.
            Panel trai = new Panel();
            trai.Dock = DockStyle.Fill;
            trai.Controls.Add(ds);
            trai.Controls.Add(GiaoDien.OTim(loc, "Tìm block theo mã, tên hoặc hệ..."));
            trai.Controls.Add(GiaoDien.TieuDe("Thư viện block PCCC"));
            Controls.Add(trai);

            FlowLayoutPanel hd = GiaoDien.KhungHanhDong(this, 190);
            nutDat.Click += delegate { Dat(); };
            nutRai.Click += delegate { Rai(); };
            NutBo them = new NutBo("Thêm từ bản vẽ", GiaoDien.XanhDam);
            them.Click += delegate { GiaoDien.GuiLenh("PCCC_THEMBLOCK\n", "PCCC_THEMBLOCK", delegate { Nap(); }); };
            NutBo dem = new NutBo("Đếm lại", GiaoDien.XanhDam);
            dem.Click += delegate { GiaoDien.An(DemLai); };
            NutBo napLai = new NutBo("Nạp lại thư viện", GiaoDien.XanhDam);
            napLai.Click += delegate { GiaoDien.An(delegate { Nap(); }); };
            foreach (NutBo b in new[] { nutDat, nutRai, them, dem, napLai }) GiaoDien.Them(hd, b, 32);
            chonHe.DropDownStyle = ComboBoxStyle.DropDownList;
            chonHe.FlatStyle = FlatStyle.Flat;
            chonHe.BackColor = GiaoDien.The;
            chonHe.ForeColor = chiCo.ForeColor = GiaoDien.ChuDam;
            chonHe.SelectedIndexChanged += delegate { Loc(); };
            GiaoDien.Them(hd, chonHe, 0);
            chiCo.Text = "Chỉ block có trong bản vẽ";
            chiCo.CheckedChanged += delegate { Loc(); };
            GiaoDien.Them(hd, chiCo, 24);
            xem.SizeMode = PictureBoxSizeMode.Zoom;
            xem.BorderStyle = BorderStyle.FixedSingle;
            xem.BackColor = Color.Black; // ảnh block vẽ nền đen như màn hình CAD
            GiaoDien.Them(hd, xem, 150);
            tenXem.Font = GiaoDien.Dam;
            tenXem.ForeColor = GiaoDien.ChuDam;
            tenXem.TextAlign = ContentAlignment.TopCenter;
            GiaoDien.Them(hd, tenXem, 34);
            GiaoDien.Them(hd, tong, 80);
        }

        /// Đọc lại danh mục + ảnh xem trước từ TKPCCC.Cad.dll, đếm bản vẽ. Trả số block.
        public int Nap()
        {
            string thuMucAnh;
            tatCa = Lenh.DanhMucBlock(out he, out thuMucAnh);
            foreach (Image im in anh.Values) im.Dispose();
            anh.Clear();
            foreach (VatTu v in tatCa)
            {
                string f = Path.Combine(thuMucAnh, v.ma + ".png");
                if (!File.Exists(f)) continue;
                // Đọc qua bộ nhớ để không giữ khoá file: PCCC_LIB, PCCC_THEMBLOCK còn vẽ lại ảnh.
                using (MemoryStream ms = new MemoryStream(File.ReadAllBytes(f)))
                using (Image im = Image.FromStream(ms))
                    anh[v.ma] = new Bitmap(im);
            }
            string cu = chonHe.SelectedIndex > 0 ? (string)chonHe.SelectedItem : null;
            chonHe.Items.Clear();
            chonHe.Items.Add("Tất cả hệ");
            foreach (string h in tatCa.Select(TenHe).Distinct()) chonHe.Items.Add(h);
            chonHe.SelectedIndex = cu != null && chonHe.Items.Contains(cu) ? chonHe.Items.IndexOf(cu) : 0; // gọi Loc()
            DemLai();
            return tatCa.Count;
        }

        /// Đếm lại số lượng theo mã trong bản vẽ đang mở.
        public void DemLai()
        {
            Document doc = AcApp.DocumentManager.MdiActiveDocument;
            soLuong = new Dictionary<string, double>();
            if (doc != null)
                using (doc.LockDocument())
                    soLuong = Lenh.DemBanVe(doc.Database);
            Loc();
        }

        string TenHe(VatTu v)
        {
            string t;
            return he.TryGetValue(v.he ?? "", out t) ? t : (v.he ?? "");
        }

        double SoLuong(VatTu v)
        {
            double n;
            soLuong.TryGetValue(v.ma, out n);
            return n;
        }

        /// Lọc theo ô tìm (một phần mã, tên hoặc tên hệ, không phân biệt hoa thường), hệ đang chọn, ô "chỉ block có trong bản vẽ".
        void Loc()
        {
            VatTu dangChon = Chon();
            string k = loc.Text.Trim().ToLowerInvariant();
            string heChon = chonHe.SelectedIndex > 0 ? (string)chonHe.SelectedItem : null;
            ds.SuspendLayout();
            ds.Rows.Clear();
            int soDong = 0;
            double tongSl = 0;
            int soLoai = 0;
            foreach (IGrouping<string, VatTu> nhom in tatCa.GroupBy(TenHe))
            {
                List<VatTu> hien = nhom.Where(v => (heChon == null || nhom.Key == heChon)
                    && (k == "" || (v.ma + " " + v.ten + " " + nhom.Key).ToLowerInvariant().Contains(k))
                    && (!chiCo.Checked || SoLuong(v) > 0)).ToList();
                if (hien.Count == 0) continue;
                int r = ds.Rows.Add();
                ds.Rows[r].Tag = nhom.Key + "  (" + hien.Count + ")";
                ds.Rows[r].Height = 26;
                foreach (VatTu v in hien)
                {
                    double sl = SoLuong(v);
                    Image im;
                    anh.TryGetValue(v.ma, out im);
                    r = ds.Rows.Add("", im, v.ten, v.dvt ?? "", sl > 0 ? sl.ToString("0.##") : "0");
                    DataGridViewRow d = ds.Rows[r];
                    d.Tag = v;
                    d.Cells[C_SL].Style.ForeColor = d.Cells[C_SL].Style.SelectionForeColor = sl > 0 ? GiaoDien.Do : GiaoDien.ChuMo;
                    if (v == dangChon) d.Selected = true;
                    soDong++;
                    tongSl += sl;
                    if (sl > 0) soLoai++;
                }
            }
            if (Chon() == null) ChonDongDau();
            ds.ResumeLayout();
            ds.Text = "Block: " + soDong; // thu_gui.ps1 đọc số dòng (chữ của cửa sổ) qua UI Automation
            tong.Dat(tongSl.ToString("0.##"), soLoai + " loại / " + tatCa.Count + " block thư viện");
            XemTruoc();
        }

        void ChonDongDau()
        {
            ds.ClearSelection();
            foreach (DataGridViewRow d in ds.Rows)
                if (d.Tag is VatTu) { d.Selected = true; ds.CurrentCell = d.Cells[C_TEN]; return; }
        }

        VatTu Chon()
        {
            return ds.SelectedRows.Count > 0 ? ds.SelectedRows[0].Tag as VatTu : null;
        }

        void XemTruoc()
        {
            VatTu v = Chon();
            Image im = null;
            if (v != null) anh.TryGetValue(v.ma, out im);
            xem.Image = im;
            tenXem.Text = v == null ? "" : v.ma;
            nutDat.Enabled = nutRai.Enabled = v != null;
        }

        /// Dòng tiêu đề nhóm: dải nền xanh suốt chiều ngang, chữ trắng đậm.
        void VeNhom(object s, DataGridViewRowPrePaintEventArgs e)
        {
            string ten = ds.Rows[e.RowIndex].Tag as string;
            if (ten == null) return;
            Rectangle r = new Rectangle(e.RowBounds.X, e.RowBounds.Y, ds.Columns.GetColumnsWidth(DataGridViewElementStates.Visible), e.RowBounds.Height);
            using (SolidBrush b = new SolidBrush(GiaoDien.Xanh)) e.Graphics.FillRectangle(b, r);
            r.X += 8;
            TextRenderer.DrawText(e.Graphics, ten, GiaoDien.Dam, r, Color.White, TextFormatFlags.VerticalCenter | TextFormatFlags.Left);
            e.Handled = true;
        }

        /// Cột nút: hai nút tròn màu; cột tên: tên đậm + mã nhỏ màu xám bên dưới.
        void VeO(object s, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0 || (e.ColumnIndex != C_NUT && e.ColumnIndex != C_TEN)) return;
            VatTu v = ds.Rows[e.RowIndex].Tag as VatTu;
            if (v == null) return;
            e.PaintBackground(e.ClipBounds, (e.State & DataGridViewElementStates.Selected) != 0);
            Rectangle o = e.CellBounds;
            if (e.ColumnIndex == C_NUT)
            {
                GiaoDien.Tron(e.Graphics, o.X + 6, o.Y + o.Height / 2, GiaoDien.XanhLa, "+");
                GiaoDien.Tron(e.Graphics, o.X + 33, o.Y + o.Height / 2, GiaoDien.Cam, "R");
            }
            else
            {
                Rectangle tren = new Rectangle(o.X + 4, o.Y + 6, o.Width - 8, o.Height / 2 - 4);
                Rectangle duoi = new Rectangle(o.X + 4, o.Y + o.Height / 2 + 1, o.Width - 8, o.Height / 2 - 6);
                TextRenderer.DrawText(e.Graphics, v.ten, GiaoDien.Dam, tren, GiaoDien.ChuDam, TextFormatFlags.Left | TextFormatFlags.Bottom | TextFormatFlags.EndEllipsis);
                TextRenderer.DrawText(e.Graphics, v.ma, GiaoDien.Nho, duoi, GiaoDien.ChuMo, TextFormatFlags.Left | TextFormatFlags.Top | TextFormatFlags.EndEllipsis);
            }
            e.Handled = true;
        }

        void BamO(object s, DataGridViewCellMouseEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex != C_NUT || !(ds.Rows[e.RowIndex].Tag is VatTu)) return;
            if (e.X >= 4 && e.X <= 30) Dat();
            else if (e.X >= 31 && e.X <= 58) Rai();
        }

        void Dat()
        {
            VatTu v = Chon();
            if (v != null) GiaoDien.GuiLenh("PCCC_DAT\n" + v.ma + "\n", "PCCC_DAT", DemLai);
        }

        void Rai()
        {
            VatTu v = Chon();
            if (v != null) GiaoDien.GuiLenh("PCCC_RAI\n" + v.ma + "\n", "PCCC_RAI", DemLai);
        }
    }
}
