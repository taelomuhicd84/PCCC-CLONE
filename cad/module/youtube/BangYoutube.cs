// Plugin YouTube (bundle riêng TKPCCC_YouTube.bundle, lệnh TKYOUTUBE mở cửa sổ riêng co giãn, thu nhỏ được; tách khỏi palette PCCC 17/11): xem video hướng dẫn ngay trong CAD bằng WebView2 (Edge; runtime có sẵn trên Windows 10/11).
// Chromium có sẵn của AutoCAD 2021 (AcCef) là bản 77 đời 2019, YouTube hiện nay không chạy được nên không dùng.
// Thư viện WebView2 (module/youtube/webview2, Microsoft, giấy phép BSD, gói NuGet Microsoft.Web.WebView2 1.0.2903.40) build.bat chép cạnh TKPCCC.YouTube.dll.
// Dữ liệu trình duyệt ở %LOCALAPPDATA%\TKPCCC\WebView2 (mặc định nằm cạnh acad.exe trong Program Files: không ghi được). Link cuối lưu `yt_cuoi`.
// Người dùng 17/11: chọn trình xem `yt_trinh_duyet` = webview2 (trong palette) | brave (Brave không nhúng được: mở cửa sổ Brave riêng, xem MoBrave).
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace TKPCCC
{
    class BangYoutube : UserControl
    {
        const string TRANG_CHU = "https://www.youtube.com/";
        static bool daGan, daDatLoader;
        readonly TextBox o = new TextBox();
        readonly Panel khung = new Panel();
        readonly Label doiTrinh;
        WebView2 web;
        bool brave;

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
            doiTrinh = Link("", delegate { DatTrinh(!brave); });
            nut.Controls.AddRange(new Control[]
            {
                Link("◀ Lùi", delegate { if (SanSang() && web.CanGoBack) web.GoBack(); }),
                Link("Trang chủ", delegate { Mo(TRANG_CHU); }),
                Link("Mở bằng trình duyệt", delegate { Process.Start(SanSang() ? web.CoreWebView2.Source : TRANG_CHU); }),
                doiTrinh,
            });
            Controls.Add(nut);
            o.KeyDown += delegate(object s, KeyEventArgs e) { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; Xem(); } };
            Controls.Add(GiaoDien.OTim(o, "Dán link YouTube hoặc gõ từ khoá, Enter để xem"));

            DatTrinh(GiaoDien.DocCaiDat("yt_trinh_duyet", "webview2") == "brave");
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

        /// Đổi trình xem, lưu `yt_trinh_duyet`. Brave: bỏ WebView2 (đỡ RAM), khung chỉ còn hướng dẫn.
        void DatTrinh(bool laBrave)
        {
            brave = laBrave;
            GiaoDien.LuuCaiDat("yt_trinh_duyet", brave ? "brave" : "webview2");
            doiTrinh.Text = brave ? "Xem trong CAD" : "Xem bằng Brave";
            khung.Controls.Clear();
            if (web != null) { web.Dispose(); web = null; }
            if (brave)
            {
                string exe = HangDoi.TimBrave();
                Bao("Chế độ Brave: dán link hoặc gõ từ khoá ở ô trên rồi Enter (hoặc bấm Trang chủ): mở thành tab mới trong cửa sổ Brave riêng của TKPCCC "
                    + "(lướt web, thêm tab như Brave thường; nút toàn màn hình chỉ phủ khung cửa sổ, Esc để thoát). Lần đầu cửa sổ áp mép phải AutoCAD, chỉnh cỡ một lần là Brave nhớ.\n\n"
                    + (exe == null ? "CHƯA thấy Brave trên máy: cài ở https://brave.com/download/ (hoặc Hub TKPCCC, nhóm YouTube)." : "Brave: " + exe)
                    + "\n\nBấm \"Xem trong CAD\" để quay lại xem ngay trong palette (WebView2).");
                return;
            }
            GanThuVien();
            try { TaoWeb(); }
            catch (Exception ex) { Loi(ex.Message); } // thiếu DLL WebView2: báo, không làm hỏng palette
        }

        [DllImport("user32.dll")] static extern bool GetWindowRect(IntPtr h, out RECT r);
        [StructLayout(LayoutKind.Sequential)] struct RECT { public int Trai, Tren, Phai, Duoi; }

        /// Người dùng 18/11: Brave lướt web được (cửa sổ thường có tab + thanh địa chỉ, không còn --app chỉ một trang); link mở thêm = tab mới trong cửa sổ đó.
        /// Hồ sơ riêng %LOCALAPPDATA%\TKPCCC\Brave = tiến trình Brave riêng, nên cờ dòng lệnh luôn có hiệu lực dù Brave chính đang mở:
        /// nạp extension brave_khung (cạnh DLL) để nút toàn màn hình (YouTube) chỉ phủ khung cửa sổ, không nhảy cả màn hình.
        /// Lần đầu (chưa có hồ sơ) đặt cửa sổ rộng 480 px áp mép phải AutoCAD; sau đó Brave tự nhớ vị trí, cỡ người dùng chỉnh.
        void MoBrave(string url)
        {
            string exe = HangDoi.TimBrave();
            if (exe == null) { Process.Start("https://brave.com/download/"); return; }
            string hoSo = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TKPCCC", "Brave");
            string ext = Path.Combine(Path.GetDirectoryName(typeof(BangYoutube).Assembly.Location), "brave_khung");
            string co = "--user-data-dir=\"" + hoSo + "\" --no-first-run --no-default-browser-check";
            if (Directory.Exists(ext)) co += " --load-extension=\"" + ext + "\"";
            IntPtr h = Autodesk.AutoCAD.ApplicationServices.Application.MainWindow.Handle;
            RECT r;
            if (!Directory.Exists(hoSo) && GetWindowRect(h, out r))
            {
                Rectangle man = Screen.FromHandle(h).WorkingArea;
                int tren = Math.Max(r.Tren, man.Top), duoi = Math.Min(r.Duoi, man.Bottom), phai = Math.Min(r.Phai, man.Right), rong = 480;
                co += " --window-position=" + (phai - rong) + "," + tren + " --window-size=" + rong + "," + (duoi - tren);
            }
            Process.Start(exe, co + " \"" + url + "\"");
            GiaoDien.LuuCaiDat("yt_cuoi", url);
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
                if (!daDatLoader) { CoreWebView2Environment.SetLoaderDllFolderPath(thuMuc); daDatLoader = true; } // gọi lại sau khi đã tạo môi trường thì WebView2 báo lỗi
                WebView2 w = web = new WebView2();
                w.Dock = DockStyle.Fill;
                khung.Controls.Add(w);
                string duLieu = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TKPCCC", "WebView2");
                CoreWebView2Environment env = await CoreWebView2Environment.CreateAsync(null, duLieu);
                await w.EnsureCoreWebView2Async(env);
                if (w != web) return; // đã đổi sang Brave trong lúc chờ (w đã Dispose)
                // Link mở cửa sổ mới (target=_blank): mở luôn trong tab này.
                w.CoreWebView2.NewWindowRequested += delegate(object s, CoreWebView2NewWindowRequestedEventArgs e) { e.Handled = true; w.CoreWebView2.Navigate(e.Uri); };
                w.CoreWebView2.SourceChanged += delegate { GiaoDien.LuuCaiDat("yt_cuoi", w.CoreWebView2.Source); };
                Mo(GiaoDien.DocCaiDat("yt_cuoi", TRANG_CHU));
            }
            catch (Exception ex) { if (!brave) Loi(ex.Message); } // async void: lỗi không được thoát ra ngoài (làm sập AutoCAD)
        }

        bool SanSang() { return web != null && web.CoreWebView2 != null; }

        void Mo(string url)
        {
            if (brave) { MoBrave(url); return; }
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
            Bao("Không mở được trình xem YouTube trong CAD.\n\nCần Microsoft Edge WebView2 Runtime (Windows 10/11 thường có sẵn; tải ở trang Microsoft nếu thiếu) "
                + "và 3 file WebView2 cạnh TKPCCC.YouTube.dll (chạy lại cad\\build.bat).\n\nVẫn xem được bằng nút \"Mở bằng trình duyệt\" hoặc \"Xem bằng Brave\".\n\nChi tiết: " + chiTiet);
        }

        void Bao(string chu)
        {
            khung.Controls.Clear();
            Label l = new Label();
            l.Dock = DockStyle.Fill;
            l.Padding = new Padding(10);
            l.ForeColor = GiaoDien.ChuDam;
            l.Text = chu;
            khung.Controls.Add(l);
        }
    }
}
