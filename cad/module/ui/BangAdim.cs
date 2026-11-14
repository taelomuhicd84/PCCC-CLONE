// Trang con "Dim đối tượng tự động" (ADIM 5 bước, spec addin mẫu 14/11): mở từ thẻ ADIM (Tiện ích, Quy trình), nút ✕ quay lại.
// Chỉ chọn trước: layer đặt dim, DimStyle, loại đối tượng, phương đo; bấm BẮT ĐẦU thì gửi -LAYER / -DIMSTYLE rồi PCCC_ADIM điền sẵn loại đối tượng,
// phương đo đặt qua Lenh.AdimPhuong (câu hỏi phương nằm sau bước quét chọn). Hai mép tường (bước 1, 2), quét chọn, điểm đặt dim: bấm trên bản vẽ khi lệnh hỏi.
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Autodesk.AutoCAD.ApplicationServices;
using Db = Autodesk.AutoCAD.DatabaseServices; // bí danh: DatabaseServices cũng có Font, FlowDirection trùng tên WinForms
using AcApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace TKPCCC
{
    class BangAdim : UserControl
    {
        readonly FlowLayoutPanel f = new FlowLayoutPanel();
        readonly TextBox lop = new TextBox();
        readonly ComboBox kieu = new ComboBox();
        readonly List<RadioButton> loai = new List<RadioButton>(), phuong = new List<RadioButton>();
        static readonly string[] LOAI = { "Block", "Line", "Pline", "Mline", "Tatca" }, LOAI_CHU = { "● Block / Thiết bị", "— Đoạn thẳng (Line)", "〰 Polyline", "═ Mline", "✦ Tất cả" };
        static readonly string[] PHUONG = { "X", "Y", "Tudong" }, PHUONG_CHU = { "↔ Phương X (ngang)", "↕ Phương Y (đứng)", "✦ Tự động nhận diện" };

        public BangAdim(Action dong)
        {
            BackColor = GiaoDien.Nen;
            f.Dock = DockStyle.Fill;
            f.FlowDirection = FlowDirection.TopDown;
            f.WrapContents = false;
            f.AutoScroll = true;
            f.BackColor = GiaoDien.Nen;
            f.Padding = new Padding(8, 6, 8, 8);
            Controls.Add(f);

            Panel dau = new Panel();
            dau.Size = new Size(290, 54);
            dau.BackColor = GiaoDien.Nen;
            Label ten = new Label();
            ten.Text = "DIM ĐỐI TƯỢNG TỰ ĐỘNG";
            ten.Font = new Font("Segoe UI", 10.5f, FontStyle.Bold);
            ten.ForeColor = GiaoDien.Cam;
            ten.AutoSize = true;
            ten.Location = new Point(0, 2);
            Label phu = new Label();
            phu.Text = "Quy trình 5 bước: dim tự động Line, Pline, Mline, Block và nối chuỗi dim";
            phu.Font = GiaoDien.Nho;
            phu.ForeColor = GiaoDien.ChuMo;
            phu.AutoSize = true;
            phu.MaximumSize = new Size(250, 0);
            phu.Location = new Point(0, 22);
            NutBo x = new NutBo("✕", GiaoDien.Xanh);
            x.SetBounds(258, 2, 30, 26);
            x.AccessibleName = "Đóng, quay lại Tiện ích";
            x.Click += delegate { GiaoDien.An(dong); };
            dau.Controls.AddRange(new Control[] { ten, phu, x });
            f.Controls.Add(dau);
            f.Controls.Add(Chu("Bước 1 & 2: khi lệnh hỏi, bấm 2 mép tường / trục giới hạn (hoặc Enter bỏ qua để chỉ dim khoảng cách giữa các đối tượng).", GiaoDien.ChuDam));

            Muc("Layer", "Chọn layer đặt đường dim");
            lop.Width = 140;
            lop.BackColor = GiaoDien.The;
            lop.ForeColor = GiaoDien.ChuDam;
            lop.BorderStyle = BorderStyle.FixedSingle;
            lop.AccessibleName = "Layer đặt dim (trống = hiện hành)";
            lop.Text = GiaoDien.DocCaiDat("adim_lop", "DIM");
            kieu.DropDownStyle = ComboBoxStyle.DropDownList;
            kieu.Width = 140;
            kieu.AccessibleName = "Kiểu kích thước (DimStyle)";
            FlowLayoutPanel h1 = Hang();
            h1.Controls.Add(lop);
            h1.Controls.Add(kieu);
            f.Controls.Add(h1);
            FlowLayoutPanel chip = Hang();
            foreach (string t in new[] { "DIM", "PCCC_DIM", "0_DIM", "⚡ Hiện hành" })
            {
                string ma = t.StartsWith("⚡") ? "" : t;
                Label l = new Label();
                l.Text = t;
                l.Font = GiaoDien.Nho;
                l.AutoSize = true;
                l.Padding = new Padding(6, 2, 6, 2);
                l.Margin = new Padding(0, 0, 4, 4);
                l.BackColor = GiaoDien.The;
                l.ForeColor = GiaoDien.ChuDam;
                l.Cursor = Cursors.Hand;
                l.Click += delegate { lop.Text = ma; };
                chip.Controls.Add(l);
            }
            f.Controls.Add(chip);

            Muc("Bước 3", "Chọn loại đối tượng cần quét để dim");
            f.Controls.Add(NhomChon(LOAI, LOAI_CHU, GiaoDien.DocCaiDat("adim_loai", "Block"), loai));
            Muc("Bước 4", "Chọn hướng đo và vị trí đặt dim");
            f.Controls.Add(NhomChon(PHUONG, PHUONG_CHU, GiaoDien.DocCaiDat("adim_phuong", "Tudong"), phuong));

            Muc("Bước 5", "Quét đối tượng và đặt điểm dim");
            f.Controls.Add(Chu("Quét cửa sổ chọn một lần cả lưới thiết bị → bấm điểm đặt dim. Các điểm thẳng hàng (≤ 10 mm) gom một mốc: không sinh dim 0.", GiaoDien.ChuMo));
            NutBo chay = new NutBo("✓  BẮT ĐẦU DIM TỰ ĐỘNG (ADIM)", GiaoDien.Cam);
            chay.Size = new Size(290, 38);
            chay.Margin = new Padding(0, 8, 0, 4);
            chay.Click += delegate { GiaoDien.An(BatDau); };
            f.Controls.Add(chay);
            NutBo tiep = new NutBo("DIM TIẾP TỤC (CDIM)", GiaoDien.Xanh);
            tiep.Size = new Size(290, 30);
            tiep.Click += delegate { GiaoDien.GuiLenh("PCCC_CDIM\n", null, null); };
            f.Controls.Add(tiep);
            f.Resize += delegate { int w = Math.Max(200, f.ClientSize.Width - f.Padding.Horizontal - 4); chay.Width = w; tiep.Width = w; };
            f.Text = "Adim: " + LOAI.Length; // thu_gui.ps1 nhận trang
        }

        /// Nạp danh sách DimStyle của bản vẽ đang mở (gọi mỗi lần mở trang).
        public void NapKieu()
        {
            string chon = kieu.SelectedItem as string ?? GiaoDien.DocCaiDat("adim_kieu", "");
            kieu.Items.Clear();
            Document doc = AcApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            string dang = "";
            using (Db.Transaction tr = doc.Database.TransactionManager.StartOpenCloseTransaction())
            {
                Db.DimStyleTable bang = (Db.DimStyleTable)tr.GetObject(doc.Database.DimStyleTableId, Db.OpenMode.ForRead);
                foreach (Db.ObjectId id in bang)
                {
                    Db.DimStyleTableRecord r = (Db.DimStyleTableRecord)tr.GetObject(id, Db.OpenMode.ForRead);
                    kieu.Items.Add(r.Name);
                    if (id == doc.Database.Dimstyle) dang = r.Name;
                }
                tr.Commit();
            }
            kieu.SelectedItem = kieu.Items.Contains(chon) ? chon : dang;
        }

        void BatDau()
        {
            string l = lop.Text.Trim(), k = kieu.SelectedItem as string ?? "";
            string lo = LOAI[loai.FindIndex(r => r.Checked)], ph = PHUONG[phuong.FindIndex(r => r.Checked)];
            if (l.IndexOfAny(new[] { '<', '>', '/', '\\', '"', ':', ';', '?', '*', '|', ',', '=', '`' }) >= 0) throw new System.Exception("Tên layer có ký tự không hợp lệ: " + l);
            GiaoDien.LuuCaiDat("adim_lop", l);
            GiaoDien.LuuCaiDat("adim_kieu", k);
            GiaoDien.LuuCaiDat("adim_loai", lo);
            GiaoDien.LuuCaiDat("adim_phuong", ph);
            Lenh.AdimPhuong = ph;
            // Tên có dấu cách: -LAYER, -DIMSTYLE coi dấu cách là Enter, nên bọc ngoặc kép.
            GiaoDien.GuiLenh((l != "" ? "_.-LAYER\n_M\n\"" + l + "\"\n\n" : "") + (k != "" ? "_.-DIMSTYLE\n_R\n\"" + k + "\"\n" : "") + "PCCC_ADIM\n" + lo + "\n",
                "PCCC_ADIM", delegate { Lenh.AdimPhuong = null; });
        }

        void Muc(string so, string ten)
        {
            FlowLayoutPanel h = Hang();
            h.Margin = new Padding(0, 10, 0, 2);
            Label s = new Label();
            s.Text = so;
            s.Font = GiaoDien.Nho;
            s.AutoSize = true;
            s.Padding = new Padding(4, 1, 4, 1);
            s.BackColor = GiaoDien.Cam;
            s.ForeColor = Color.White;
            s.Margin = new Padding(0, 0, 6, 0);
            Label t = new Label();
            t.Text = ten;
            t.Font = GiaoDien.Dam;
            t.ForeColor = GiaoDien.ChuDam;
            t.AutoSize = true;
            h.Controls.Add(s);
            h.Controls.Add(t);
            f.Controls.Add(h);
        }

        static FlowLayoutPanel Hang()
        {
            FlowLayoutPanel p = new FlowLayoutPanel();
            p.AutoSize = true;
            p.MaximumSize = new Size(296, 0);
            p.BackColor = GiaoDien.Nen;
            p.Margin = new Padding(0, 2, 0, 2);
            return p;
        }

        static Label Chu(string t, Color mau)
        {
            Label l = new Label();
            l.Text = t;
            l.Font = GiaoDien.Nho;
            l.ForeColor = mau;
            l.AutoSize = true;
            l.MaximumSize = new Size(290, 0);
            l.UseMnemonic = false;
            l.Margin = new Padding(0, 2, 0, 2);
            return l;
        }

        static FlowLayoutPanel NhomChon(string[] ma, string[] chu, string dang, List<RadioButton> ds)
        {
            FlowLayoutPanel p = Hang();
            for (int i = 0; i < ma.Length; i++)
            {
                RadioButton r = new RadioButton();
                r.Text = chu[i];
                r.Appearance = Appearance.Button; // dạng nút bấm như chip của spec
                r.FlatStyle = FlatStyle.Flat;
                r.FlatAppearance.CheckedBackColor = GiaoDien.Cam;
                r.Font = GiaoDien.Nho;
                r.AutoSize = true;
                r.ForeColor = GiaoDien.ChuDam;
                r.BackColor = GiaoDien.The;
                r.Margin = new Padding(0, 0, 4, 4);
                r.Checked = ma[i] == dang;
                ds.Add(r);
                p.Controls.Add(r);
            }
            if (!ds.Any(r => r.Checked)) ds[0].Checked = true;
            return p;
        }
    }
}
