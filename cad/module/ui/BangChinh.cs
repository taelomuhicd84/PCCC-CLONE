// Palette chính (lệnh PCCC; PCCC_BLOCK, PCCC_HELP mở đúng phân hệ): đầu trang [PCCC] + tên + phiên bản, ô tìm lọc thẻ, thanh dọc chọn phân hệ
// (Quy trình vẽ · Tiện ích · Báo cháy · Chữa cháy · Thống kê · Thư viện · Lệnh · Cài đặt; nút ☰ đầu trang: menu phân hệ + thu gọn thanh dọc, nhớ `ben_an`), chân trang trạng thái cầu nối MCP + bản vẽ. Chỉ giao diện: lệnh vẽ gửi bằng tên qua GiaoDien.GuiLenh.
// Bot chat, YouTube là plugin riêng từ 17/11 (người dùng: tách 3 plugin cho đỡ rối): cad/module/botchat (TKBOTCHAT), cad/module/youtube (TKYOUTUBE).
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Windows;
using AcApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace TKPCCC
{
    public class BangChinh : UserControl
    {
        public const string QUY_TRINH = "Quy trình vẽ", TIEN_ICH = "Tiện ích", BO_TRI = "Báo cháy", CHUA_CHAY = "Chữa cháy", THONG_KE = "Thống kê", THU_VIEN = "Thư viện", LENH = "Lệnh", CAI_DAT = "Cài đặt";
        public const string ADIM = "ADIM"; // trang con Dim đối tượng (không có nút ở thanh dọc)
        static PaletteSet ps;
        static BangChinh nd;

        /// Mở palette (trang null = giữ trang đang xem). Trả câu báo ngắn cho dòng lệnh.
        public static string Mo(string trang)
        {
            if (ps == null)
            {
                nd = new BangChinh();
                ps = new PaletteSet("TKPCCC - PCCC", new Guid("3E7A2C91-5B4D-4F18-8D6E-0C9A1B2F7E54"));
                ps.MinimumSize = new Size(340, 420);
                ps.Add("PCCC", nd);
                ps.DockEnabled = DockSides.Left | DockSides.Right;
                ps.Visible = true;
                ps.Size = new Size(400, 700);
                ps.Dock = DockSides.Left;
                AcApp.DocumentManager.DocumentActivated += delegate { if (ps.Visible && nd.block != null) GiaoDien.An(nd.block.DemLai); };
            }
            ps.Visible = true;
            nd.ChonTrang(trang ?? nd.dangXem ?? QUY_TRINH);
            if (trang == THU_VIEN) return nd.soBlock + " block trong bảng. Bấm đúp một dòng để đặt thiết bị.";
            return "palette TKPCCC, phân hệ " + nd.dangXem + ".";
        }

        static readonly string[] DS_PHAN_HE = { QUY_TRINH, TIEN_ICH, BO_TRI, CHUA_CHAY, THONG_KE, THU_VIEN, LENH, CAI_DAT };
        readonly TextBox tim = new TextBox();
        readonly Panel noiDung = new Panel();
        readonly FlowLayoutPanel ben = new FlowLayoutPanel();
        readonly Label chan = new Label();
        readonly Dictionary<string, Control> trang = new Dictionary<string, Control>();
        readonly Dictionary<string, NutBen> nutBen = new Dictionary<string, NutBen>();
        readonly FlowLayoutPanel dsThe = new FlowLayoutPanel();
        readonly List<TheCongCu> the = new List<TheCongCu>();
        string dangXem;
        BangBlock block;
        BangLenh bangLenh;
        BangTienIch tienIch;
        BangAdim adim;
        readonly Dictionary<string, BangTienIch> luoiKhac = new Dictionary<string, BangTienIch>(); // Chữa cháy, Thống kê
        int soBlock;

        BangChinh()
        {
            Font = GiaoDien.Chu;
            BackColor = GiaoDien.Nen;
            GiaoDien.DangMo.Add(this);

            // Thứ tự Add quyết định cách xếp Dock: Fill trước, rồi cạnh trái, rồi trên/dưới (trên/dưới thêm sau nên phủ hết chiều ngang).
            noiDung.Dock = DockStyle.Fill;
            Controls.Add(noiDung);

            ben.Dock = DockStyle.Left;
            ben.Width = 66;
            ben.FlowDirection = FlowDirection.TopDown;
            ben.WrapContents = false;
            ben.AutoScroll = true; // palette kéo thấp hơn 9 nút thì cuộn, không mất nút cuối
            ben.BackColor = GiaoDien.Nen;
            ben.Padding = new Padding(0, 6, 0, 0);
            // Thanh biểu tượng (ý từ video plugin 16/11): mỗi phân hệ một ô màu có biểu tượng, nhãn ngắn bên dưới.
            string[] ds = DS_PHAN_HE;
            string[] icon = { "📐", "🔧", "🔔", "💧", "📊", "📚", "⌨", "⚙" };
            Color[] mau = { GiaoDien.MauLa, GiaoDien.MauDuong, GiaoDien.MauDo, GiaoDien.MauCam, GiaoDien.MauTim, GiaoDien.MauLuc, GiaoDien.MauXam, GiaoDien.MauXam };
            for (int i = 0; i < ds.Length; i++)
            {
                string ten = ds[i];
                NutBen n = new NutBen(ten, icon[i], mau[i]);
                n.Click += delegate { GiaoDien.An(delegate { ChonTrang(ten); }); };
                nutBen[ten] = n;
                ben.Controls.Add(n);
            }
            Controls.Add(ben);
            ben.Visible = GiaoDien.DocCaiDat("ben_an", "0") != "1"; // người dùng 17/11: thu gọn thanh dọc, chuyển phân hệ bằng nút ☰

            // Đang xem Tiện ích thì ô tìm lọc thẻ tiện ích (tìm theo mã tắt SZ, CKC...); trang khác thì về Quy trình lọc thẻ lệnh.
            tim.TextChanged += delegate
            {
                if (dangXem == TIEN_ICH) { tienIch.Tim(tim.Text); return; }
                if (luoiKhac.ContainsKey(dangXem ?? "")) { luoiKhac[dangXem].Tim(tim.Text); return; }
                if (dangXem != QUY_TRINH && tim.Text != "") ChonTrang(QUY_TRINH);
                LocThe();
            };
            Controls.Add(GiaoDien.OTim(tim, "Tìm công cụ theo tên hoặc lệnh..."));
            Controls.Add(DauTrang());

            chan.Dock = DockStyle.Bottom;
            chan.Height = 22;
            chan.BackColor = GiaoDien.Nen;
            chan.ForeColor = GiaoDien.ChuMo;
            chan.Font = GiaoDien.Nho;
            chan.TextAlign = ContentAlignment.MiddleLeft;
            chan.Padding = new Padding(6, 0, 0, 0);
            Controls.Add(chan);
            System.Windows.Forms.Timer dongHo = new System.Windows.Forms.Timer();
            dongHo.Interval = 2000;
            dongHo.Tick += delegate { CapNhatChan(); };
            dongHo.Start();
            CapNhatChan();

            TaoQuyTrinh();
            GiaoDien.DaDoiChuDe += delegate { foreach (NutBen n in nutBen.Values) n.Invalidate(); };
        }

        Control DauTrang()
        {
            Panel p = new Panel();
            p.Dock = DockStyle.Top;
            p.Height = 46;
            p.BackColor = GiaoDien.Nen;
            Label huy = new Label();
            huy.Text = "PCCC";
            huy.Font = GiaoDien.Dam;
            huy.BackColor = GiaoDien.Xanh;
            huy.ForeColor = Color.White;
            huy.TextAlign = ContentAlignment.MiddleCenter;
            huy.SetBounds(8, 10, 48, 26);
            Label ten = new Label();
            ten.Text = "TKPCCC – Thiết kế PCCC";
            ten.Font = new Font("Segoe UI", 10.5f, FontStyle.Bold);
            ten.ForeColor = GiaoDien.ChuDam;
            ten.AutoSize = true;
            ten.Location = new Point(62, 5);
            Label pb = new Label();
            string dll = typeof(Lenh).Assembly.Location;
            pb.Text = "bản " + (File.Exists(dll) ? File.GetLastWriteTime(dll).ToString("yyyy.MM.dd") : "?");
            pb.Font = GiaoDien.Nho;
            pb.ForeColor = GiaoDien.ChuMo;
            pb.AutoSize = true;
            pb.Location = new Point(64, 26);
            p.Controls.AddRange(new Control[] { huy, ten, pb });
            // ☰: menu chuyển phân hệ + ẩn/hiện thanh dọc (khi thanh dọc ẩn đây là đường duy nhất để chuyển phân hệ).
            Label menu = new Label();
            menu.Text = "☰";
            menu.Font = new Font("Segoe UI Symbol", 14f);
            menu.ForeColor = GiaoDien.ChuDam;
            menu.TextAlign = ContentAlignment.MiddleCenter;
            menu.Dock = DockStyle.Right;
            menu.Width = 40;
            menu.Cursor = Cursors.Hand;
            menu.AccessibleName = "Menu phân hệ";
            menu.AccessibleRole = AccessibleRole.ButtonMenu;
            menu.Click += delegate { GiaoDien.An(delegate { MenuPhanHe().Show(menu, new Point(menu.Width, menu.Height), ToolStripDropDownDirection.BelowLeft); }); };
            p.Controls.Add(menu);
            return p;
        }

        ContextMenuStrip MenuPhanHe()
        {
            ContextMenuStrip m = new ContextMenuStrip();
            ToolStripMenuItem an = new ToolStripMenuItem(ben.Visible ? "Thu gọn thanh dọc" : "Hiện thanh dọc");
            an.Click += delegate { GiaoDien.An(delegate { ben.Visible = !ben.Visible; GiaoDien.LuuCaiDat("ben_an", ben.Visible ? "0" : "1"); }); };
            m.Items.Add(an);
            m.Items.Add(new ToolStripSeparator());
            foreach (string t in DS_PHAN_HE)
            {
                string ten = t;
                ToolStripMenuItem i = new ToolStripMenuItem(ten);
                i.Checked = ten == dangXem;
                i.Click += delegate { GiaoDien.An(delegate { ChonTrang(ten); }); };
                m.Items.Add(i);
            }
            m.Closed += delegate { m.BeginInvoke((Action)m.Dispose); }; // menu dựng lại mỗi lần bấm: bỏ bản cũ
            return m;
        }

        void CapNhatChan()
        {
            string mcp;
            try
            {
                bool song = File.Exists(HangDoi.Song) && (DateTime.UtcNow - File.GetLastWriteTimeUtc(HangDoi.Song)).TotalSeconds < 6;
                bool dang = Directory.Exists(HangDoi.Dang) && Directory.GetFiles(HangDoi.Dang, "*.json").Length > 0;
                mcp = !song ? "chưa chạy" : dang ? "đang vẽ" : "sẵn sàng";
            }
            catch (System.Exception) { mcp = "?"; }
            Document doc = AcApp.DocumentManager.MdiActiveDocument;
            chan.Text = "Cầu nối MCP: " + mcp + "   ·   Bản vẽ: " + (doc == null ? "(không có)" : Path.GetFileName(doc.Name));
        }

        public void ChonTrang(string ten)
        {
            Control c;
            if (!trang.TryGetValue(ten, out c))
            {
                c = TaoTrang(ten);
                c.Dock = DockStyle.Fill;
                trang[ten] = c;
                noiDung.Controls.Add(c);
            }
            foreach (Control k in trang.Values) k.Visible = k == c;
            dangXem = ten;
            foreach (KeyValuePair<string, NutBen> n in nutBen) { n.Value.Chon = n.Key == ten; n.Value.Invalidate(); }
            if (ten == THU_VIEN) soBlock = block.Nap();
            if (ten == LENH) bangLenh.Nap();
            if (ten == ADIM) adim.NapKieu();
        }

        Control TaoTrang(string ten)
        {
            if (ten == THU_VIEN) return block = new BangBlock();
            if (ten == LENH) return bangLenh = new BangLenh();
            if (ten == CAI_DAT) return TrangCaiDat();
            if (ten == BO_TRI) return new BangBoTri();
            if (ten == ADIM) return adim = new BangAdim(delegate { ChonTrang(TIEN_ICH); });
            if (ten == CHUA_CHAY) return luoiKhac[ten] = new BangTienIch(PhanHe.CHUA_CHAY, PhanHe.NHOM_CC, PhanHe.NUT_CC, "ChuaChay", delegate(string t) { ChonTrang(t); });
            if (ten == THONG_KE) return luoiKhac[ten] = new BangTienIch(PhanHe.THONG_KE, PhanHe.NHOM_TK, PhanHe.NUT_TK, "ThongKe", delegate(string t) { ChonTrang(t); });
            if (ten == TIEN_ICH) return tienIch = new BangTienIch(delegate(string t) { ChonTrang(t); });
            return dsThe;
        }

        // ---------- Quy trình vẽ: 6 thẻ bước + thẻ kích thước; khi tìm thì hiện thêm thẻ của mọi lệnh PCCC ----------

        void TaoQuyTrinh()
        {
            dsThe.FlowDirection = FlowDirection.TopDown;
            dsThe.WrapContents = false;
            dsThe.AutoScroll = true;
            dsThe.BackColor = GiaoDien.Nen;
            dsThe.Padding = new Padding(6, 6, 6, 6);
            dsThe.Resize += delegate { foreach (TheCongCu t in the) t.Width = RongThe(); };
            Them(1, "Trục", "Lưới trục theo nhịp X, Y; có PDF thì nhập làm nền trước", new[] { "PCCC_TRUC", "PDFIMPORT" }, null);
            Them(2, "Tường", "Tường theo tim, nhập chiều dày", new[] { "PCCC_TUONG" }, null);
            Them(3, "Mặt bằng chi tiết", "Vách ngăn (PCCC_TUONG, chọn Vách), cửa đi trên tường", new[] { "PCCC_TUONG", "PCCC_CUA" }, null);
            Them(4, "Thiết bị PCCC", "Rải đầu báo, đầu phun theo lưới; đặt từng thiết bị", new[] { BO_TRI, "PCCC_RAI", "PCCC_DAT" },
                new Action[] { delegate { ChonTrang(BO_TRI); }, null, null }); // Thư viện đã có ở thanh dọc: bỏ huy hiệu cho thẻ không tràn
            Them(5, "Dây, ống", "Dây báo cháy, ống nhánh theo thiết bị đã rải; vẽ ống, dây tay", new[] { "PCCC_TUYEN", "PCCC_ONG" }, null);
            Them(6, "Sơ đồ nguyên lý", "Nhờ bot chat vẽ từ mặt bằng đang mở", new[] { "TKBOTCHAT" },
                new Action[] { delegate { GiaoDien.MoBotChat("so_do"); } });
            Them(0, "Kích thước tự động", "Quét cửa sổ, chuỗi kích thước theo thiết bị (trang 5 bước)", new[] { "PCCC_ADIM" }, new Action[] { delegate { ChonTrang(ADIM); } });
            Them(0, "Kích thước tiếp tục", "Nối tiếp chuỗi kích thước vừa tạo", new[] { "PCCC_CDIM" }, null);

            foreach (string[] l in Lenh.DS_LENH) Them(-1, l[1], l[2], new[] { l[0] }, null); // chỉ hiện khi tìm
            LocThe();
        }

        int RongThe() { return Math.Max(200, dsThe.ClientSize.Width - dsThe.Padding.Horizontal - 2); }

        void Them(int so, string ten, string moTa, string[] nhan, Action[] viec)
        {
            Action[] v = new Action[nhan.Length];
            for (int i = 0; i < nhan.Length; i++)
            {
                string l = nhan[i];
                v[i] = viec != null && viec[i] != null ? viec[i] : delegate { GiaoDien.GuiLenh(l + "\n", null, null); };
            }
            TheCongCu t = new TheCongCu(so, ten, moTa, nhan, v);
            t.Width = RongThe();
            the.Add(t);
            dsThe.Controls.Add(t);
        }

        void LocThe()
        {
            string k = Lenh.BoDau(tim.Text.Trim());
            int n = 0;
            dsThe.SuspendLayout();
            foreach (TheCongCu t in the)
            {
                t.Visible = k == "" ? t.So >= 0 : t.ChuTim.Contains(k);
                if (t.Visible) n++;
            }
            dsThe.ResumeLayout();
            dsThe.Text = "The: " + n; // thu_gui.ps1 đọc số thẻ (chữ của cửa sổ)
        }

        // ---------- Cài đặt ----------

        Control TrangCaiDat()
        {
            FlowLayoutPanel f = new FlowLayoutPanel();
            f.FlowDirection = FlowDirection.TopDown;
            f.WrapContents = false;
            f.AutoScroll = true;
            f.BackColor = GiaoDien.Nen;
            f.Padding = new Padding(10, 8, 10, 8);
            Func<string, Label> nhan = delegate(string t)
            {
                Label l = new Label();
                l.Text = t;
                l.Font = GiaoDien.Dam;
                l.ForeColor = GiaoDien.ChuDam;
                l.AutoSize = true;
                l.Margin = new Padding(0, 10, 0, 4);
                return l;
            };
            f.Controls.Add(nhan("Giao diện"));
            f.Controls.Add(NhomChon(new[] { "toi", "sang", "claude" }, new[] { "Tối (mặc định)", "Sáng (xanh trắng)", "Claude (màu Claude Code)" },
                GiaoDien.ChuDe, delegate(string g) { GiaoDien.DatChuDe(g); }));
            // Người dùng 16/11: chọn loại dim; bảng layer dây báo cháy, ống chữa cháy thêm / xoá được.
            f.Controls.Add(nhan("Kích thước (dim) tự động sau khi rải"));
            f.Controls.Add(CaiDim());
            f.Controls.Add(nhan("Layer dây báo cháy, ống chữa cháy"));
            f.Controls.Add(BangLopDay());
            f.Controls.Add(nhan("Bản quyền"));
            NutBo bq = new NutBo("Bản quyền...", GiaoDien.XanhDam);
            bq.Size = new Size(160, 32);
            bq.Click += delegate { GiaoDien.GuiLenh("PCCC_BANQUYEN\n", null, null); };
            f.Controls.Add(bq);
            Label goi = new Label();
            goi.Text = "Cài đặt lưu ở " + GiaoDien.FileCaiDat;
            goi.Font = GiaoDien.Nho;
            goi.ForeColor = GiaoDien.ChuMo;
            goi.MaximumSize = new Size(280, 0);
            goi.AutoSize = true;
            goi.Margin = new Padding(0, 14, 0, 0);
            f.Controls.Add(goi);
            return f;
        }

        const string KIEU_DIM_MAC_DINH = "(PCCC_KT mặc định)";

        /// Kiểu dim (DimStyle của bản vẽ đang mở), cách dim, layer dim: PCCC_RAI đọc lúc rải (CaiDat.DimKieu / DimDang / DimLop).
        static Control CaiDim()
        {
            FlowLayoutPanel p = new FlowLayoutPanel();
            p.FlowDirection = FlowDirection.TopDown;
            p.AutoSize = true;
            p.WrapContents = false;
            p.BackColor = GiaoDien.Nen;
            Func<string, Label> ghi = delegate(string t)
            {
                Label l = new Label();
                l.Text = t;
                l.ForeColor = GiaoDien.ChuDam;
                l.AutoSize = true;
                l.Margin = new Padding(0, 4, 0, 2);
                return l;
            };
            p.Controls.Add(ghi("Kiểu dim (DimStyle):"));
            ComboBox kieu = new ComboBox();
            kieu.DropDownStyle = ComboBoxStyle.DropDownList;
            kieu.Width = 260;
            kieu.AccessibleName = "Kiểu dim";
            Action nap = delegate
            {
                string dang = CaiDat.DimKieu();
                kieu.Items.Clear();
                kieu.Items.Add(KIEU_DIM_MAC_DINH);
                try { foreach (string s in Lenh.DsKieuDim()) kieu.Items.Add(s); } catch (System.Exception) { }
                if (dang != "" && !kieu.Items.Contains(dang)) kieu.Items.Add(dang); // kiểu chưa có trong bản vẽ này: vẫn giữ lựa chọn
                kieu.SelectedItem = dang == "" ? KIEU_DIM_MAC_DINH : dang;
            };
            nap();
            kieu.SelectedIndexChanged += delegate
            {
                string s = kieu.SelectedItem == null ? "" : kieu.SelectedItem.ToString();
                CaiDat.Luu("dim_kieu", s == KIEU_DIM_MAC_DINH ? "" : s);
            };
            kieu.DropDown += delegate { string chon = kieu.SelectedItem as string; nap(); if (chon != null) kieu.SelectedItem = chon; }; // bản vẽ khác: đọc lại
            p.Controls.Add(kieu);
            p.Controls.Add(ghi("Cách dim:"));
            p.Controls.Add(NhomChon(new[] { CaiDat.DIM_CHUOI, CaiDat.DIM_CHUOI_CHEO, CaiDat.DIM_KHONG },
                new[] { "Chuỗi X + Y (trùng hàng, trùng cột chỉ dim 1 lần)", "Chuỗi X + Y và chéo góc tới thiết bị gần nhất", "Không tự dim" },
                CaiDat.DimDang(), delegate(string d) { CaiDat.Luu("dim_dang", d); }));
            p.Controls.Add(ghi("Layer dim:"));
            TextBox lop = new TextBox();
            lop.Width = 200;
            lop.Text = CaiDat.DimLop();
            lop.AccessibleName = "Layer dim";
            lop.Leave += delegate { CaiDat.Luu("dim_lop", lop.Text.Trim()); };
            p.Controls.Add(lop);
            return p;
        }

        /// Bảng layer dây, ống (CaiDat.LopDay): sửa ô trực tiếp, Thêm / Xoá dòng, lấy layer có sẵn trong bản vẽ, Lưu. Trang Báo cháy chọn dây từ nhóm BC.
        static Control BangLopDay()
        {
            Panel p = new Panel();
            p.Size = new Size(300, 330);
            p.BackColor = GiaoDien.Nen;
            DataGridView g = GiaoDien.Bang();
            g.Dock = DockStyle.None;
            g.Location = new Point(0, 0);
            g.Size = new Size(300, 190);
            g.SelectionMode = DataGridViewSelectionMode.CellSelect;
            string[] tieuDe = { "Layer", "Màu", "Kiểu nét", "Nhóm", "Ghi chú" };
            int[] rong = { 130, 40, 80, 50, 160 };
            for (int i = 0; i < tieuDe.Length; i++)
            {
                DataGridViewColumn c;
                if (i == 3)
                {
                    DataGridViewComboBoxColumn cb = new DataGridViewComboBoxColumn();
                    cb.Items.AddRange("BC", "CC");
                    cb.FlatStyle = FlatStyle.Flat;
                    c = cb;
                }
                else c = new DataGridViewTextBoxColumn();
                c.HeaderText = tieuDe[i];
                c.Width = rong[i];
                c.SortMode = DataGridViewColumnSortMode.NotSortable;
                g.Columns.Add(c);
            }
            Action<IEnumerable<string[]>> nap = delegate(IEnumerable<string[]> ds) { g.Rows.Clear(); foreach (string[] r in ds) g.Rows.Add(r[0], r[1], r[2], r[3] == "CC" ? "CC" : "BC", r[4]); };
            nap(CaiDat.LopDay());
            p.Controls.Add(g);

            Label tb = new Label();
            tb.Font = GiaoDien.Nho;
            tb.ForeColor = GiaoDien.ChuMo;
            tb.Location = new Point(0, 296);
            tb.Size = new Size(300, 34);
            tb.Text = "Layer tên PCCC_<mã dây/ống> (vd PCCC_DAY_BC) thì PCCC_BOC cộng chiều dài vào mã đó. Xoá dòng trùng rồi bấm Lưu.";
            p.Controls.Add(tb);
            Func<string, int, int, int, EventHandler, NutBo> nut = delegate(string t, int x, int y, int w, EventHandler bam)
            {
                NutBo n = new NutBo(t, GiaoDien.Xanh);
                n.Location = new Point(x, y);
                n.Size = new Size(w, 26);
                n.Click += bam;
                p.Controls.Add(n);
                return n;
            };
            nut("Thêm dòng", 0, 196, 72, delegate { g.Rows.Add("PCCC_", "7", "Continuous", "BC", ""); g.CurrentCell = g.Rows[g.Rows.Count - 1].Cells[0]; g.BeginEdit(true); });
            nut("Xoá dòng", 76, 196, 72, delegate
            {
                List<int> xoa = g.SelectedCells.Cast<DataGridViewCell>().Select(c => c.RowIndex).Distinct().OrderByDescending(i => i).ToList();
                foreach (int i in xoa) g.Rows.RemoveAt(i);
            });
            nut("Mặc định", 152, 196, 70, delegate { nap(CaiDat.LOP_MAC_DINH); });
            NutBo luu = nut("Lưu", 226, 196, 74, delegate
            {
                GiaoDien.An(delegate
                {
                    List<string[]> ds = new List<string[]>();
                    foreach (DataGridViewRow r in g.Rows)
                    {
                        string[] d = Enumerable.Range(0, 5).Select(i => Convert.ToString(r.Cells[i].Value ?? "").Trim()).ToArray();
                        if (d[0] == "") continue;
                        int mau;
                        if (!int.TryParse(d[1], out mau) || mau < 1 || mau > 255) throw new System.Exception("Layer " + d[0] + ": màu phải là số ACI 1–255 (đang là \"" + d[1] + "\").");
                        if (ds.Any(x => string.Equals(x[0], d[0], StringComparison.OrdinalIgnoreCase))) throw new System.Exception("Layer " + d[0] + " lặp hai dòng: xoá một dòng.");
                        ds.Add(d);
                    }
                    CaiDat.LuuLopDay(ds);
                    tb.Text = "Đã lưu " + ds.Count + " layer. Trang Báo cháy đọc lại khi mở.";
                });
            });
            // Lấy layer có sẵn trong bản vẽ (layer báo cháy của mẫu người dùng, vd $.1.FA-FR-SMOKE).
            ComboBox banVe = new ComboBox();
            banVe.DropDownStyle = ComboBoxStyle.DropDownList;
            banVe.Location = new Point(0, 228);
            banVe.Width = 222;
            banVe.AccessibleName = "Layer trong bản vẽ";
            List<string[]> lopBv = new List<string[]>();
            banVe.DropDown += delegate
            {
                try { lopBv = Lenh.DsLopBanVe(); } catch (System.Exception) { lopBv = new List<string[]>(); }
                banVe.Items.Clear();
                foreach (string[] l in lopBv) banVe.Items.Add(l[0]);
            };
            p.Controls.Add(banVe);
            nut("Thêm", 226, 226, 74, delegate
            {
                string[] l = lopBv.FirstOrDefault(x => x[0] == (banVe.SelectedItem as string));
                if (l == null) return;
                foreach (DataGridViewRow r in g.Rows) if (string.Equals(Convert.ToString(r.Cells[0].Value), l[0], StringComparison.OrdinalIgnoreCase)) return;
                g.Rows.Add(l[0], l[1], l[2], "BC", "Layer có sẵn trong bản vẽ");
            });
            Label gy = new Label();
            gy.Font = GiaoDien.Nho;
            gy.ForeColor = GiaoDien.ChuMo;
            gy.Location = new Point(0, 258);
            gy.Size = new Size(300, 34);
            gy.Text = "Chọn layer có sẵn trong bản vẽ rồi bấm Thêm (bấm Lưu để ghi). Sửa ô: bấm đúp.";
            p.Controls.Add(gy);
            return p;
        }

        static Control NhomChon(string[] ma, string[] chu, string dangChon, Action<string> doi)
        {
            FlowLayoutPanel p = new FlowLayoutPanel();
            p.FlowDirection = FlowDirection.TopDown;
            p.AutoSize = true;
            p.BackColor = GiaoDien.Nen;
            for (int i = 0; i < ma.Length; i++)
            {
                string m = ma[i];
                RadioButton r = new RadioButton();
                r.Text = chu[i];
                r.AutoSize = true;
                r.ForeColor = GiaoDien.ChuDam;
                r.Checked = m == dangChon;
                r.CheckedChanged += delegate { if (r.Checked) GiaoDien.An(delegate { doi(m); }); };
                p.Controls.Add(r);
            }
            return p;
        }
    }

    /// Nút của thanh dọc bên trái: ô biểu tượng màu + nhãn ngắn; đang chọn thì nền thẻ, vạch nhấn trái, viền trắng quanh ô.
    class NutBen : Control
    {
        public bool Chon;
        readonly string icon;
        readonly Color mau;
        bool tren;

        public NutBen(string chu, string icon, Color mau)
        {
            Text = chu;
            this.icon = icon;
            this.mau = mau;
            Size = new Size(66, 60); // 9 phân hệ x 61 px: palette cao 700 còn cuộn được (ben.AutoScroll)
            Margin = new Padding(0, 0, 0, 1);
            Cursor = Cursors.Hand;
            DoubleBuffered = true;
            AccessibleRole = AccessibleRole.PageTab;
            AccessibleName = chu;
            MouseEnter += delegate { tren = true; Invalidate(); };
            MouseLeave += delegate { tren = false; Invalidate(); };
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.Clear(Chon || tren ? GiaoDien.The : GiaoDien.Nen);
            if (Chon) using (SolidBrush b = new SolidBrush(mau)) g.FillRectangle(b, 0, 8, 3, Height - 16);
            GiaoDien.VeVuong(g, new Rectangle((Width - 34) / 2, 5, 34, 34), mau, icon, GiaoDien.Icon, Chon);
            string ngan = Text == BangChinh.QUY_TRINH ? "Quy trình" : Text;
            TextRenderer.DrawText(g, ngan, GiaoDien.NhoDam, new Rectangle(0, 41, Width, 16), Chon ? GiaoDien.ChuDam : GiaoDien.ChuMo,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.Top | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
        }
    }

    /// Thẻ công cụ: số bước (vòng tròn nhấn), tên đậm, mô tả một dòng chữ mờ, huy hiệu tên lệnh góc phải (bấm = chạy). Bấm thân thẻ = huy hiệu đầu.
    class TheCongCu : Control
    {
        public readonly int So; // 1..6 bước, 0 thẻ thêm, -1 thẻ lệnh chỉ hiện khi tìm
        public readonly string Ten, MoTa, ChuTim;

        public TheCongCu(int so, string ten, string moTa, string[] nhan, Action[] viec)
        {
            So = so;
            Ten = ten;
            MoTa = moTa;
            // Huy hiệu kèm phím tắt người dùng (ý lấy từ addin mẫu 14/11: tìm được theo phím tắt). shortcut: sửa phím tắt thì mở lại palette thẻ mới đổi.
            string[] phim = nhan.Select(l => { string pt; return PhimTat.TheoLenh.TryGetValue(l, out pt) ? pt : ""; }).ToArray();
            ChuTim = Lenh.BoDau(ten + " " + moTa + " " + string.Join(" ", nhan) + " " + string.Join(" ", phim));
            Height = 64;
            Margin = new Padding(0, 0, 0, 6);
            DoubleBuffered = true;
            Cursor = Cursors.Hand;
            AccessibleName = ten;
            BackColor = GiaoDien.Nen;
            Click += delegate { GiaoDien.An(viec[0]); };
            for (int i = nhan.Length - 1; i >= 0; i--) // xếp từ phải sang trái
            {
                Label h = new Label();
                h.Text = nhan[i] + (phim[i] != "" ? " · " + phim[i] : "");
                h.Font = GiaoDien.Nho;
                h.AutoSize = true;
                h.Padding = new Padding(4, 1, 4, 1);
                h.BackColor = GiaoDien.Nen;
                h.ForeColor = GiaoDien.Xanh;
                h.Cursor = Cursors.Hand;
                h.Anchor = AnchorStyles.Top | AnchorStyles.Right;
                Action v = viec[i];
                h.Click += delegate { GiaoDien.An(v); };
                Controls.Add(h);
            }
            Resize += delegate { XepHuy(); };
        }

        void XepHuy()
        {
            int x = Width - 8;
            foreach (Control h in Controls)
            {
                x -= h.Width;
                h.Location = new Point(x, Height - h.Height - 7);
                x -= 4;
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.Clear(Parent != null ? Parent.BackColor : GiaoDien.Nen);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (GraphicsPath p = GiaoDien.BoGoc(new Rectangle(0, 0, Width - 1, Height - 1), 8))
            {
                using (SolidBrush b = new SolidBrush(GiaoDien.The)) g.FillPath(b, p);
                using (Pen v = new Pen(GiaoDien.Vien)) g.DrawPath(v, p);
            }
            int x = 10;
            if (So > 0)
            {
                GiaoDien.VeTron(g, new Rectangle(10, 10, 26, 26), GiaoDien.MauNhom[(So - 1) % GiaoDien.MauNhom.Length], So.ToString(), GiaoDien.Dam, false);
                x = 44;
            }
            TextFormatFlags dong = TextFormatFlags.Left | TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine;
            TextRenderer.DrawText(g, Ten, GiaoDien.Dam, new Rectangle(x, 8, Width - x - 8, 18), GiaoDien.ChuDam, dong);
            TextRenderer.DrawText(g, MoTa, GiaoDien.Nho, new Rectangle(x, 26, Width - x - 8, 16), GiaoDien.ChuMo, dong);
        }
    }
}
