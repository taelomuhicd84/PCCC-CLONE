// Plugin YouTube (bundle riêng TKPCCC_YouTube.bundle, người dùng 17/11: tách khỏi plugin PCCC). Lệnh TKYOUTUBE mở cửa sổ riêng:
// thu nhỏ (nút _), phóng to, kéo giãn bốn cạnh; AutoCAD nhớ vị trí và kích thước (ShowModelessDialog persistSizeAndPosition).
using System;
using System.Drawing;
using System.Windows.Forms;
using Autodesk.AutoCAD.Runtime;
using AcApp = Autodesk.AutoCAD.ApplicationServices.Application;

[assembly: CommandClass(typeof(TKPCCC.LenhYoutube))]

namespace TKPCCC
{
    public class LenhYoutube
    {
        static Form cs;

        [CommandMethod("TKYOUTUBE")]
        public void Mo()
        {
            GiaoDien.An(delegate
            {
                if (cs != null && !cs.IsDisposed)
                {
                    if (cs.WindowState == FormWindowState.Minimized) cs.WindowState = FormWindowState.Normal;
                    cs.Visible = true;
                    cs.Activate();
                    return;
                }
                cs = new Form();
                cs.Text = "TKPCCC - YouTube";
                cs.FormBorderStyle = FormBorderStyle.Sizable;
                cs.MinimizeBox = true;
                cs.MaximizeBox = true;
                cs.ShowInTaskbar = false;
                cs.MinimumSize = new Size(260, 180);
                cs.Size = new Size(520, 400);
                cs.StartPosition = FormStartPosition.CenterScreen;
                cs.BackColor = GiaoDien.Nen;
                BangYoutube b = new BangYoutube();
                b.Dock = DockStyle.Fill;
                cs.Controls.Add(b);
                // Đóng cửa sổ: dừng video, giải phóng WebView2 (mở lại thì tạo mới).
                cs.FormClosed += delegate { b.Dispose(); cs = null; };
                AcApp.ShowModelessDialog(null, cs, true);
            });
        }
    }
}
