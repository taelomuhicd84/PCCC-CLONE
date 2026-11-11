// Bảng block PCCC (lệnh PCCC_BLOCK): PaletteSet liệt kê block của thư viện kèm ảnh, mã, tên, hệ. Bấm đúp một dòng = PCCC_DAT <mã>.
// DLL riêng (TKPCCC.UI.dll) vì cần acmgd.dll, thứ accoreconsole không nạp được. KHÔNG xử lý bản vẽ ở đây: danh mục, ảnh xem trước
// và việc thêm block do TKPCCC.Cad.dll làm (cad/module/plugin/Block.cs), nhờ vậy phần đó thử được bằng cad/test/chay_thu.ps1.
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Windows;
using AcApp = Autodesk.AutoCAD.ApplicationServices.Application;

[assembly: CommandClass(typeof(TKPCCC.LenhUi))]

namespace TKPCCC
{
    public class LenhUi
    {
        static PaletteSet bang;
        static BangBlock noiDung;

        [CommandMethod("PCCC_BLOCK")]
        public void MoBang()
        {
            Document doc = AcApp.DocumentManager.MdiActiveDocument;
            try
            {
                if (bang == null)
                {
                    noiDung = new BangBlock();
                    bang = new PaletteSet("TKPCCC - Block PCCC", new Guid("5D3C1E0A-7B42-4C9E-9A61-2F8E4B7D1C35"));
                    bang.MinimumSize = new Size(280, 300);
                    bang.Add("Block", noiDung);
                }
                int n = noiDung.Nap();
                bang.Visible = true;
                doc.Editor.WriteMessage("\n[TKPCCC] PCCC_BLOCK: " + n + " block trong bảng. Bấm đúp một dòng để đặt thiết bị.");
            }
            catch (System.Exception ex) { doc.Editor.WriteMessage("\n[TKPCCC] Lỗi: " + ex.Message); }
        }
    }

    public class BangBlock : UserControl
    {
        readonly ListView ds = new ListView();
        readonly TextBox loc = new TextBox();
        readonly ImageList anh = new ImageList();
        List<VatTu> tatCa = new List<VatTu>();
        Dictionary<string, string> he = new Dictionary<string, string>();

        public BangBlock()
        {
            anh.ImageSize = new Size(40, 40);
            anh.ColorDepth = ColorDepth.Depth32Bit;
            ds.View = View.Details;
            ds.FullRowSelect = true;
            ds.MultiSelect = false;
            ds.HideSelection = false;
            ds.SmallImageList = anh;
            ds.Columns.Add("Mã", 170);
            ds.Columns.Add("Tên", 230);
            ds.Columns.Add("Hệ", 150);
            ds.Dock = DockStyle.Fill;
            ds.DoubleClick += delegate { Dat(); };
            ds.KeyDown += delegate(object s, KeyEventArgs e) { if (e.KeyCode == Keys.Enter) Dat(); };
            loc.Dock = DockStyle.Top;
            loc.TextChanged += delegate { Loc(); };

            Button them = new Button(), napLai = new Button();
            them.Text = "Thêm từ bản vẽ";
            them.AutoSize = true;
            them.Click += delegate { GuiLenh("PCCC_THEMBLOCK\n", "PCCC_THEMBLOCK"); };
            napLai.Text = "Nạp lại";
            napLai.AutoSize = true;
            napLai.Click += delegate { NapCoBaoLoi(); };
            FlowLayoutPanel nut = new FlowLayoutPanel();
            nut.Dock = DockStyle.Bottom;
            nut.AutoSize = true;
            nut.Controls.Add(them);
            nut.Controls.Add(napLai);

            // Thứ tự Add quyết định cách xếp Dock: điều khiển Fill phải thêm trước.
            Controls.Add(ds);
            Controls.Add(loc);
            Controls.Add(nut);
        }

        /// Đọc lại danh mục + ảnh xem trước từ TKPCCC.Cad.dll. Trả số block.
        public int Nap()
        {
            string thuMucAnh;
            tatCa = Lenh.DanhMucBlock(out he, out thuMucAnh);
            anh.Images.Clear();
            foreach (VatTu v in tatCa)
            {
                string f = Path.Combine(thuMucAnh, v.ma + ".png");
                if (!File.Exists(f)) continue;
                // Đọc qua bộ nhớ để không giữ khoá file: PCCC_LIB, PCCC_THEMBLOCK còn vẽ lại ảnh.
                using (MemoryStream ms = new MemoryStream(File.ReadAllBytes(f)))
                using (Image im = Image.FromStream(ms))
                    anh.Images.Add(v.ma, new Bitmap(im));
            }
            Loc();
            return tatCa.Count;
        }

        void NapCoBaoLoi()
        {
            try { Nap(); }
            catch (System.Exception ex) { MessageBox.Show(ex.Message, "TKPCCC"); }
        }

        /// Lọc theo ô tìm: khớp một phần mã, tên hoặc tên hệ (không phân biệt hoa thường).
        void Loc()
        {
            string k = loc.Text.Trim().ToLowerInvariant();
            ds.BeginUpdate();
            ds.Items.Clear();
            foreach (VatTu v in tatCa)
            {
                string tenHe;
                if (!he.TryGetValue(v.he ?? "", out tenHe)) tenHe = v.he ?? "";
                if (k != "" && !(v.ma + " " + v.ten + " " + tenHe).ToLowerInvariant().Contains(k)) continue;
                ds.Items.Add(new ListViewItem(new string[] { v.ma, v.ten, tenHe }, v.ma));
            }
            ds.EndUpdate();
        }

        void Dat()
        {
            if (ds.SelectedItems.Count > 0) GuiLenh("PCCC_DAT\n" + ds.SelectedItems[0].Text + "\n", null);
        }

        /// Gửi lệnh cho bản vẽ đang mở. napSau = tên lệnh mà khi nó kết thúc thì nạp lại bảng (thêm block xong phải thấy block mới).
        void GuiLenh(string lenh, string napSau)
        {
            Document doc = AcApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            if (napSau != null)
            {
                CommandEventHandler xong = null;
                xong = delegate(object s, CommandEventArgs e)
                {
                    if (e.GlobalCommandName != napSau) return;
                    doc.CommandEnded -= xong;
                    doc.CommandCancelled -= xong;
                    doc.CommandFailed -= xong;
                    NapCoBaoLoi();
                };
                doc.CommandEnded += xong;
                doc.CommandCancelled += xong;
                doc.CommandFailed += xong;
            }
            // \x03\x03 = Esc hai lần: huỷ lệnh đang dở trước khi chạy lệnh mới.
            doc.SendStringToExecute("\x03\x03" + lenh, true, false, true);
            try { Autodesk.AutoCAD.Internal.Utils.SetFocusToDwgView(); } // trả bàn phím về vùng vẽ để bấm điểm, gõ tiếp
            catch (System.Exception) { }
        }
    }
}
