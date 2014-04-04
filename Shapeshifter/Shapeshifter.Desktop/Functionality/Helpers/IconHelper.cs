using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace Shapeshifter.Desktop.Functionality.Helpers
{
    /// <summary>
    /// Crazy API wrapper which helps fetch a full icon preview from a filepath or folderpath.
    /// </summary>
    public static class IconHelper
    {

        #region apis

        private const uint SHGFI_ICON = 0x100;
        private const uint SHGFI_LARGEICON = 0x0;

        [StructLayout(LayoutKind.Sequential)]
        private struct SHFILEINFO
        {
            public IntPtr hIcon;
            public IntPtr iIcon;
            public uint dwAttributes;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szDisplayName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
            public string szTypeName;
        };

        [DllImport("shell32.dll", SetLastError = true)]
        private static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes, ref SHFILEINFO psfi, uint cbSizeFileInfo, uint uFlags);

        [DllImport("gdi32.dll", SetLastError = true)]
        private static extern bool DeleteObject(IntPtr hObject);

        [DllImport("shell32.dll", CharSet = CharSet.Unicode, PreserveSig = false, SetLastError = true)]
        private static extern void SHCreateItemFromParsingName(
            [In] [MarshalAs(UnmanagedType.LPWStr)] string pszPath,
            [In] IntPtr pbc,
            [In] [MarshalAs(UnmanagedType.LPStruct)] Guid riid,
            [Out] [MarshalAs(UnmanagedType.Interface, IidParameterIndex = 2)] out IShellItem ppv);

        #endregion

        private struct CacheKey
        {
            public CacheKey(string path, bool allowThumbnails, int widthAndHeight)
            {
                _path = path;
                _allowThumbnails = allowThumbnails;
                _widthAndHeight = widthAndHeight;
            }

            private string _path;
            private bool _allowThumbnails;
            private int _widthAndHeight;
        }

        private static readonly Dictionary<CacheKey, BitmapSource> _iconCache;

        static IconHelper()
        {
            _iconCache = new Dictionary<CacheKey, BitmapSource>();
        }

        [HandleProcessCorruptedStateExceptions]
        public static BitmapSource GetIcon(string path, bool allowThumbnails, int widthAndHeight = 256)
        {

            if (Directory.Exists(path) || File.Exists(path))
            {

                var cacheKey = new CacheKey(path, allowThumbnails, widthAndHeight);
                if (_iconCache.ContainsKey(cacheKey))
                {

                    return _iconCache[cacheKey];

                }
                else
                {

                    IntPtr hbitmap = IntPtr.Zero;

                    try
                    {

                        IShellItem ppsi;
                        var uuid = new Guid("43826d1e-e718-42ee-bc55-a1e261c37bfe");

                        SHCreateItemFromParsingName(path, IntPtr.Zero, uuid, out ppsi);

                        var factory = (IShellItemImageFactory)ppsi;

                        var scope = (allowThumbnails ? SIIGBF.SIIGBF_THUMBNAILONLY : SIIGBF.SIIGBF_ICONONLY) | SIIGBF.SIIGBF_RESIZETOFIT;

                        try
                        {
                            factory.GetImage(new SIZE(widthAndHeight, widthAndHeight), scope, ref hbitmap);
                        }
                        catch (COMException ex)
                        {
                            if (ex.ErrorCode == -2147175936 && allowThumbnails) //no thumbnail available
                            {
                                try
                                {
                                    factory.GetImage(new SIZE(widthAndHeight, widthAndHeight), SIIGBF.SIIGBF_ICONONLY | SIIGBF.SIIGBF_RESIZETOFIT, ref hbitmap);
                                }
                                catch (COMException)
                                {
                                }
                            }
                        }

                    }
                    catch (Exception ex)
                    {
                        if (ex is DllNotFoundException || ex is EntryPointNotFoundException || ex is ArgumentException)
                        {

                            //try Windows XP method instead - the API was not supported

                            SHFILEINFO shinfo = new SHFILEINFO();

                            var result = SHGetFileInfo(path, 0, ref shinfo, (uint)Marshal.SizeOf(shinfo), SHGFI_ICON | SHGFI_LARGEICON);
                            if (result != IntPtr.Zero)
                            {

                                var icon = Icon.FromHandle(shinfo.hIcon);
                                var bitmap = icon.ToBitmap();

                                hbitmap = bitmap.GetHbitmap();

                                DeleteObject(shinfo.hIcon);

                            }

                        }
                        else if (ex is FileNotFoundException)
                        {
                        }
                        else
                        {
                            throw;
                        }
                    }

                    if (hbitmap != IntPtr.Zero)
                    {

                        var source = Imaging.CreateBitmapSourceFromHBitmap(hbitmap, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                        source.Freeze();

                        DeleteObject(hbitmap);

                        _iconCache.Add(cacheKey, source);
                        if (_iconCache.Keys.Count > 50)
                        {
                            var lastKey = _iconCache.Keys.First();
                            _iconCache.Remove(lastKey);
                        }

                        return source;

                    }
                    else
                    {

                        //the handle is 0, assume that the data comes from Windows itself.
                        var bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.UriSource = new Uri("pack://application:,,,/Shapeshifter;component/Images/WindowsIcon.png");
                        bitmap.EndInit();

                        return bitmap;
                    }

                }

            }
            return null;
        }

        #region Nested type: IShellItem

        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("43826d1e-e718-42ee-bc55-a1e261c37bfe")]
        private interface IShellItem
        {
            void BindToHandler(IntPtr pbc,
                               [MarshalAs(UnmanagedType.LPStruct)] Guid bhid,
                               [MarshalAs(UnmanagedType.LPStruct)] Guid riid,
                               out IntPtr ppv);

            void GetParent(out IShellItem ppsi);

            void GetDisplayName(SIGDN sigdnName, out IntPtr ppszName);

            void GetAttributes(uint sfgaoMask, out uint psfgaoAttribs);

            void Compare(IShellItem psi, uint hint, out int piOrder);
        };

        #endregion

        #region Nested type: IShellItemImageFactory

        [ComImport, Guid("bcc18b79-ba16-442f-80c4-8a59c30c463b"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IShellItemImageFactory
        {
            void GetImage(SIZE size, SIIGBF flags, ref IntPtr phbm);
        }

        #endregion

        #region Nested type: SIGDN

        private enum SIGDN : uint
        {
            NORMALDISPLAY = 0,
            PARENTRELATIVEPARSING = 0x80018001u,
            PARENTRELATIVEFORADDRESSBAR = 0x8001c001u,
            DESKTOPABSOLUTEPARSING = 0x80028000u,
            PARENTRELATIVEEDITING = 0x80031001u,
            DESKTOPABSOLUTEEDITING = 0x8004c000u,
            FILESYSPATH = 0x80058000u,
            URL = 0x80068000u
        }

        #endregion

        #region Nested type: SIIGBF

        [Flags]
        private enum SIIGBF
        {
            SIIGBF_RESIZETOFIT = 0,
            SIIGBF_BIGGERSIZEOK = 1,
            SIIGBF_MEMORYONLY = 2,
            SIIGBF_ICONONLY = 4,
            SIIGBF_THUMBNAILONLY = 8,
            SIIGBF_INCACHEONLY = 16
        }

        #endregion

        #region Nested type: SIZE

        [StructLayout(LayoutKind.Sequential)]
        private struct SIZE
        {
            public readonly int cx;
            public readonly int cy;

            public SIZE(int cx, int cy)
            {
                this.cx = cx;
                this.cy = cy;
            }
        }

        #endregion
    }
}