// Phân hệ YouTube của palette PCCC: xem video hướng dẫn ngay trong CAD bằng WebView2 (Edge; runtime có sẵn trên Windows 10/11).
// Chromium có sẵn của AutoCAD 2021 (AcCef) là bản 77 đời 2019, YouTube hiện nay không chạy được nên không dùng.
// Thư viện WebView2 (module/ui/webview2, Microsoft, giấy phép BSD, gói NuGet Microsoft.Web.WebView2 1.0.2903.40) build.bat chép cạnh TKPCCC.UI.dll.
// Dữ liệu trình duyệt ở %LOCALAPPDATA%\TKPCCC\WebView2 (mặc định nằm cạnh acad.exe trong Program Files: không ghi được). Link cuối lưu `yt_cuoi`.
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace TKPCCC
{
    class BangYoutube : UserControl
    {
        const string TRANG_CHU = "https://www.youtube.com/";
        static bool daGan;
        readonly TextBox o = new TextBox();
        readonly Panel khung = new Panel();
        WebView2 web;

        public BangYoutube()
        {
            Font = GiaoDien.Chu;
            BackColor = GiaoDien.Nen;
            khung.Dock = DockStyle.Fill;
            khung.BackColor = GiaoDien.The;
            Controls.Add(khung);

            FlowLayoutPanel nut = new FlowLayoutPanel();
            nut.Dock = DockStyle.Top;
            nut.Height = 30;
            nut.BackColor = GiaoDien.Nen;
            nut.Padding = new Padding(6, 0, 6, 0);
            nut.Controls.AddRange(new Control[]
            {
                Link("◀ Lùi", delegate { if (SanSang() && web.CanGoBack) web.GoBack(); }),
                Link("Trang chủ", delegate { Mo(TRANG_CHU); }),
                Link("Mở bằng trình duyệt", delegate { Process.Start(SanSang() ? web.CoreWebView2.Source : TRANG_CHU); }),
            });
            Controls.Add(nut);
            o.KeyDown += delegate(object s, KeyEventArgs e) { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; Xem(); } };
            Controls.Add(GiaoDien.OTim(o, "Dán link YouTube hoặc gõ từ khoá, Enter để xem"));

            GanThuVien();
            try { TaoWeb(); }
            catch (Exception ex) { Loi(ex.Message); } // thiếu DLL WebView2: báo, không làm hỏng palette
            VisibleChanged += delegate { if (!Visible) Dung(); };
        }

        static Label Link(string chu, Action viec)
        {
            Label h = new Label();
            h.Text = chu;
            h.Font = GiaoDien.Nho;
            h.AutoSize = true;
            h.Padding = new Padding(4, 3, 4, 3);
            h.Margin = new Padding(0, 3, 6, 0);
            h.BackColor = GiaoDien.The;
            h.ForeColor = GiaoDien.Xanh;
            h.Cursor = Cursors.Hand;
            h.Click += delegate { GiaoDien.An(viec); };
            return h;
        }

        /// DLL WebView2 nằm cạnh TKPCCC.UI.dll nhưng AutoCAD dò thư viện ở thư mục acad.exe: chỉ đường trước khi JIT chạm kiểu WebView2 (TaoWeb).
        static void GanThuVien()
        {
            if (daGan) return;
            daGan = true;
            string thuMuc = Path.GetDirectoryName(typeof(BangYoutube).Assembly.Location);
            AppDomain.CurrentDomain.AssemblyResolve += delegate(object s, ResolveEventArgs e)
            {
                string ten = new AssemblyName(e.Name).Name, f = Path.Combine(thuMuc, ten + ".dll");
                return ten.StartsWith("Microsoft.Web.WebView2") && File.Exists(f) ? Assembly.LoadFrom(f) : null;
            };
        }

        async void TaoWeb()
        {
            try
            {
                string thuMuc = Path.GetDirectoryName(typeof(BangYoutube).Assembly.Location);
                CoreWebView2Environment.SetLoaderDllFolderPath(thuMuc);
                web = new WebView2();
                web.Dock = DockStyle.Fill;
                khung.Controls.Add(web);
                string duLieu = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TKPCCC", "WebView2");
                CoreWebView2Environment env = await CoreWebView2Environment.CreateAsync(null, duLieu);
                await web.EnsureCoreWebView2Async(env);
                // Link mở cửa sổ mới (target=_blank): mở luôn trong tab này.
                web.CoreWebView2.NewWindowRequested += delegate(object s, CoreWebView2NewWindowRequestedEventArgs e) { e.Handled = true; web.CoreWebView2.Navigate(e.Uri); };
                web.CoreWebView2.SourceChanged += delegate { GiaoDien.LuuCaiDat("yt_cuoi", web.CoreWebView2.Source); };
                Mo(GiaoDien.DocCaiDat("yt_cuoi", TRANG_CHU));
            }
            catch (Exception ex) { Loi(ex.Message); } // async void: lỗi không được thoát ra ngoài (làm sập AutoCAD)
        }

        bool SanSang() { return web != null && web.CoreWebView2 != null; }

        void Mo(string url)
        {
            if (!SanSang()) return;
            try { web.CoreWebView2.Navigate(url); }
            catch (ArgumentException) { web.CoreWebView2.Navigate(TRANG_CHU); } // link hỏng
        }

        /// Link (có http, youtube.com/, youtu.be/) thì mở thẳng; chữ khác thì tìm trên YouTube.
        void Xem()
        {
            string t = o.Text.Trim();
            if (t == "") return;
            bool link = t.StartsWith("http", StringComparison.OrdinalIgnoreCase) || t.Contains("youtube.com/") || t.Contains("youtu.be/");
            Mo(link ? (t.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? t : "https://" + t) : TRANG_CHU + "results?search_query=" + Uri.EscapeDataString(t));
        }

        /// Rời phân hệ thì dừng video (không thì tiếng vẫn chạy khi đang vẽ).
        void Dung()
        {
            if (SanSang()) web.CoreWebView2.ExecuteScriptAsync("document.querySelectorAll('video').forEach(function(v){v.pause()})");
        }

        void Loi(string chiTiet)
        {
            khung.Controls.Clear();
            Label l = new Label();
            l.Dock = DockStyle.Fill;
            l.Padding = new Padding(10);
            l.ForeColor = GiaoDien.ChuDam;
            l.Text = "Không mở được trình xem YouTube trong CAD.\n\nCần Microsoft Edge WebView2 Runtime (Windows 10/11 thường có sẵn; tải ở trang Microsoft nếu thiếu) "
                + "và 3 file WebView2 cạnh TKPCCC.UI.dll (chạy lại cad\\build.bat).\n\nVẫn xem được bằng nút \"Mở bằng trình duyệt\".\n\nChi tiết: " + chiTiet;
            khung.Controls.Add(l);
        }
    }
}
