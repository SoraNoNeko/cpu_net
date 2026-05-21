using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace SetupBootstrapper.Helpers
{
    /// <summary>
    /// 基于 Windows Shell IFileDialog (Vista+) 的文件夹选择对话框。
    /// 性能与界面均优于 WinForms 的 FolderBrowserDialog。
    /// </summary>
    public class FolderPicker
    {
        public string Title { get; set; } = "选择文件夹";
        public string InitialDirectory { get; set; } = "";
        public string ResultPath { get; private set; } = "";

        public bool ShowDialog(Window? owner = null)
        {
            var dialog = (IFileDialog)new FileOpenDialogRCW();

            dialog.SetOptions(FILEOPENDIALOGOPTIONS.FOS_PICKFOLDERS
                            | FILEOPENDIALOGOPTIONS.FOS_FORCEFILESYSTEM
                            | FILEOPENDIALOGOPTIONS.FOS_DONTADDTORECENT);
            dialog.SetTitle(Title);

            if (!string.IsNullOrEmpty(InitialDirectory))
            {
                try
                {
                    var item = ShellItemFromPath(InitialDirectory);
                    if (item != null)
                        dialog.SetFolder(item);
                }
                catch { /* 忽略无效初始路径 */ }
            }

            IntPtr hwnd = owner != null ? new WindowInteropHelper(owner).Handle : IntPtr.Zero;
            int hr = dialog.Show(hwnd);

            if (hr == 0) // S_OK
            {
                dialog.GetResult(out IShellItem? resultItem);
                if (resultItem != null)
                {
                    resultItem.GetDisplayName(SIGDN.SIGDN_FILESYSPATH, out var path);
                    ResultPath = path ?? "";
                    return true;
                }
            }

            return false;
        }

        private static IShellItem? ShellItemFromPath(string path)
        {
            int hr = SHCreateItemFromParsingName(path, IntPtr.Zero,
                new Guid(ShellItemGuid), out IShellItem? item);
            return hr == 0 ? item : null;
        }

        private const string ShellItemGuid = "43826D1E-E718-42EE-BC55-A1E261C37BFE";

        [DllImport("shell32.dll", CharSet = CharSet.Unicode, PreserveSig = true)]
        private static extern int SHCreateItemFromParsingName(
            [MarshalAs(UnmanagedType.LPWStr)] string pszPath,
            IntPtr pbc,
            [MarshalAs(UnmanagedType.LPStruct)] Guid riid,
            [MarshalAs(UnmanagedType.Interface)] out IShellItem? ppv);

        [ComImport, Guid("DC1C5A9C-E88A-4dde-A5A1-60F82A20AEF7")]
        private class FileOpenDialogRCW { }

        // IFileDialog 完整 vtable（与 Windows SDK shobjidl_core.h 完全一致）
        [ComImport, Guid("42f85136-db7e-439c-85f1-e4075d135fc8"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IFileDialog
        {
            // IModalWindow::Show
            [PreserveSig] int Show([In] IntPtr parent);

            void SetFileTypes([In] uint cFileTypes, [In] ref COMDLG_FILTERSPEC rgFilterSpec);
            void SetFileTypeIndex([In] uint iFileType);
            void GetFileTypeIndex(out uint piFileType);
            void Advise([MarshalAs(UnmanagedType.Interface)] IFileDialogEvents? pfde, out uint pdwCookie);
            void Unadvise([In] uint dwCookie);
            void SetOptions([In] FILEOPENDIALOGOPTIONS fos);
            void GetOptions(out FILEOPENDIALOGOPTIONS pfos);
            void SetDefaultFolder([MarshalAs(UnmanagedType.Interface)] IShellItem? psi);
            void SetFolder([MarshalAs(UnmanagedType.Interface)] IShellItem? psi);
            void GetFolder([MarshalAs(UnmanagedType.Interface)] out IShellItem? ppsi);
            void GetCurrentSelection([MarshalAs(UnmanagedType.Interface)] out IShellItem? ppsi);
            void SetFileName([In, MarshalAs(UnmanagedType.LPWStr)] string pszName);
            void GetFileName([MarshalAs(UnmanagedType.LPWStr)] out string pszName);
            void SetTitle([In, MarshalAs(UnmanagedType.LPWStr)] string pszTitle);
            void SetOkButtonLabel([In, MarshalAs(UnmanagedType.LPWStr)] string pszText);
            void SetFileNameLabel([In, MarshalAs(UnmanagedType.LPWStr)] string pszLabel);
            void GetResult([MarshalAs(UnmanagedType.Interface)] out IShellItem? ppsi);
            void AddPlace([MarshalAs(UnmanagedType.Interface)] IShellItem? psi, FDAP fdap);
            void SetDefaultExtension([In, MarshalAs(UnmanagedType.LPWStr)] string pszDefaultExtension);
            void Close([MarshalAs(UnmanagedType.Error)] int hr);
            void SetClientGuid([In] ref Guid guid);
            void ClearClientData();
            void SetFilter([MarshalAs(UnmanagedType.Interface)] object? pFilter);
        }

        // 仅为完整 vtable 占位的空接口（Advise/Unadvise 需要）
        [ComImport, Guid("973510DB-7D7F-452B-8975-74A85828D354"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IFileDialogEvents { }

        [ComImport, Guid(ShellItemGuid), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IShellItem
        {
            void BindToHandler(IntPtr pbc, [MarshalAs(UnmanagedType.LPStruct)] Guid bhid,
                [MarshalAs(UnmanagedType.LPStruct)] Guid riid, out IntPtr ppv);
            void GetParent([MarshalAs(UnmanagedType.Interface)] out IShellItem? ppsi);
            void GetDisplayName(SIGDN sigdnName, [MarshalAs(UnmanagedType.LPWStr)] out string ppszName);
            void GetAttributes(uint sfgaoMask, out uint psfgaoAttribs);
            void Compare([MarshalAs(UnmanagedType.Interface)] IShellItem? psi, uint hint, out int piOrder);
        }

        [Flags]
        private enum FILEOPENDIALOGOPTIONS : uint
        {
            FOS_OVERWRITEPROMPT = 0x00000002,
            FOS_STRICTFILETYPES = 0x00000004,
            FOS_NOCHANGEDIR = 0x00000008,
            FOS_PICKFOLDERS = 0x00000020,
            FOS_FORCEFILESYSTEM = 0x00000040,
            FOS_ALLNONSTORAGEITEMS = 0x00000080,
            FOS_NOVALIDATE = 0x00000100,
            FOS_ALLOWMULTISELECT = 0x00000200,
            FOS_PATHMUSTEXIST = 0x00000800,
            FOS_FILEMUSTEXIST = 0x00001000,
            FOS_CREATEPROMPT = 0x00002000,
            FOS_SHAREAWARE = 0x00004000,
            FOS_NOREADONLYRETURN = 0x00008000,
            FOS_NOTESTFILECREATE = 0x00010000,
            FOS_HIDEMRUPLACES = 0x00020000,
            FOS_HIDEPINNEDPLACES = 0x00040000,
            FOS_NODEREFERENCELINKS = 0x00100000,
            FOS_OKBUTTONNEEDSINTERACTION = 0x00200000,
            FOS_DONTADDTORECENT = 0x02000000,
            FOS_FORCESHOWHIDDEN = 0x10000000,
            FOS_DEFAULTNOMINIMODE = 0x20000000,
            FOS_FORCEPREVIEWPANEON = 0x40000000,
            FOS_SUPPORTSTREAMABLEITEMS = 0x80000000
        }

        private enum FDAP { FDAP_BOTTOM = 0, FDAP_TOP = 1 }

        private enum SIGDN : uint
        {
            SIGDN_NORMALDISPLAY = 0,
            SIGDN_PARENTRELATIVEPARSING = 0x80018001,
            SIGDN_PARENTRELATIVEFORADDRESSBAR = 0x8001c001,
            SIGDN_URL = 0x80068000,
            SIGDN_FILESYSPATH = 0x80058000
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct COMDLG_FILTERSPEC
        {
            [MarshalAs(UnmanagedType.LPWStr)] public string pszName;
            [MarshalAs(UnmanagedType.LPWStr)] public string pszSpec;
        }
    }
}
