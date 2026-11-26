// Người dùng 18/11 (ảnh hộp "Browse For Folder" + thanh địa chỉ Explorer): "đổi cách đi tới thư mục dự án, thả link folder để nhảy tới nhanh nhất".
// FolderBrowserDialog của .NET 4 là hộp cây kiểu cũ, không dán được đường dẫn. Dùng hộp chọn thư mục kiểu Explorer (IFileOpenDialog + FOS_PICKFOLDERS):
// có thanh địa chỉ (Ctrl+L dán đường dẫn) và ô "Folder". Clipboard đang có đường dẫn thư mục (copy ở thanh địa chỉ Explorer) thì mở thẳng tới đó:
// chỉ cần bấm "Select Folder". Không mở được hộp mới (COM lỗi) thì quay về FolderBrowserDialog.
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace TKPCCC
{
    static class ChonThuMuc
    {
        [ComImport, Guid("DC1C5A9C-E88A-4dde-A5A1-60F82A20AEF7")]
        class FileOpenDialogCom { }

        [ComImport, Guid("43826D1E-E718-42EE-BC55-A1E261C37BFE"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        interface IShellItem
        {
            void BindToHandler(IntPtr pbc, ref Guid bhid, ref Guid riid, out IntPtr ppv);
            void GetParent(out IShellItem ppsi);
            void GetDisplayName(uint sigdn, [MarshalAs(UnmanagedType.LPWStr)] out string ten);
            void GetAttributes(uint mask, out uint attr);
            void Compare(IShellItem psi, uint hint, out int order);
        }

        [ComImport, Guid("42f85136-db7e-439c-85f1-e4075d135fc8"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        interface IFileDialog
        {
            [PreserveSig] int Show(IntPtr chu);
            void SetFileTypes(uint n, IntPtr loc);
            void SetFileTypeIndex(uint i);
            void GetFileTypeIndex(out uint i);
            void Advise(IntPtr pfde, out uint cookie);
            void Unadvise(uint cookie);
            void SetOptions(uint fos);
            void GetOptions(out uint fos);
            void SetDefaultFolder(IShellItem psi);
            void SetFolder(IShellItem psi);
            void GetFolder(out IShellItem psi);
            void GetCurrentSelection(out IShellItem psi);
            void SetFileName([MarshalAs(UnmanagedType.LPWStr)] string ten);
            void GetFileName([MarshalAs(UnmanagedType.LPWStr)] out string ten);
            void SetTitle([MarshalAs(UnmanagedType.LPWStr)] string ten);
            void SetOkButtonLabel([MarshalAs(UnmanagedType.LPWStr)] string ten);
            void SetFileNameLabel([MarshalAs(UnmanagedType.LPWStr)] string ten);
            void GetResult(out IShellItem psi);
            void AddPlace(IShellItem psi, int fdap);
            void SetDefaultExtension([MarshalAs(UnmanagedType.LPWStr)] string duoi);
            void Close(int hr);
            void SetClientGuid(ref Guid g);
            void ClearClientData();
            void SetFilter(IntPtr loc);
        }

        [DllImport("shell32.dll", CharSet = CharSet.Unicode, PreserveSig = false)]
        static extern void SHCreateItemFromParsingName(string duongDan, IntPtr pbc, [MarshalAs(UnmanagedType.LPStruct)] Guid riid, out IShellItem item);

        const uint FOS_PICKFOLDERS = 0x20, FOS_FORCEFILESYSTEM = 0x40, FOS_NOCHANGEDIR = 0x8, FOS_PATHMUSTEXIST = 0x800;
        const uint SIGDN_FILESYSPATH = 0x80058000;

        /// Đường dẫn thư mục đang có trong clipboard (copy ở thanh địa chỉ Explorer, có thể kèm ngoặc kép); null nếu không phải thư mục có thật.
        public static string TuClipboard()
        {
            try
            {
                if (Clipboard.ContainsFileDropList())
                    foreach (string f in Clipboard.GetFileDropList()) if (Directory.Exists(f)) return f;
                if (!Clipboard.ContainsText()) return null;
                string t = Clipboard.GetText().Trim().Trim('"').Trim();
                return t.Length < 260 && t.IndexOf('\n') < 0 && Directory.Exists(t) ? Path.GetFullPath(t) : null;
            }
            catch (Exception) { return null; } // clipboard đang bị chương trình khác giữ
        }

        /// Hộp chọn thư mục kiểu Explorer. Mở ở thư mục trong clipboard (nếu có), không thì batDau. null = bỏ.
        public static string Hoi(IWin32Window chu, string tieuDe, string batDau)
        {
            string moTai = TuClipboard() ?? (batDau != null && Directory.Exists(batDau) ? batDau : null);
            try
            {
                IFileDialog d = (IFileDialog)new FileOpenDialogCom();
                try
                {
                    uint fos;
                    d.GetOptions(out fos);
                    d.SetOptions(fos | FOS_PICKFOLDERS | FOS_FORCEFILESYSTEM | FOS_NOCHANGEDIR | FOS_PATHMUSTEXIST);
                    d.SetTitle(tieuDe + "  (dán đường dẫn: Ctrl+L)");
                    if (moTai != null)
                        try
                        {
                            IShellItem i;
                            SHCreateItemFromParsingName(moTai, IntPtr.Zero, typeof(IShellItem).GUID, out i);
                            d.SetFolder(i);
                        }
                        catch (COMException) { } // thư mục không mở được: hộp mở chỗ mặc định
                    if (d.Show(chu == null ? IntPtr.Zero : chu.Handle) != 0) return null; // 0x800704C7 = người dùng bấm Cancel
                    IShellItem kq;
                    d.GetResult(out kq);
                    string f;
                    kq.GetDisplayName(SIGDN_FILESYSPATH, out f);
                    return f;
                }
                finally { Marshal.ReleaseComObject(d); }
            }
            catch (Exception) // COMException / InvalidCastException: máy không có hộp kiểu mới
            {
                using (FolderBrowserDialog d = new FolderBrowserDialog())
                {
                    d.Description = tieuDe;
                    if (moTai != null) d.SelectedPath = moTai;
                    return d.ShowDialog(chu) == DialogResult.OK ? d.SelectedPath : null;
                }
            }
        }
    }
}
