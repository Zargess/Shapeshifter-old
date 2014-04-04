using System.Drawing;
using System.IO;
using System.Windows.Media.Imaging;
using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Shell;
using Shapeshifter.Desktop.Functionality.Clipboard.Data;
using Shapeshifter.Desktop.Functionality.Clipboard.Session;

namespace Shapeshifter.Desktop.Functionality.Proxy
{
    public class ClipboardThumbnailItem : Window
    {

        #region apis

        [DllImport("gdi32.dll")]
        static extern bool DeleteObject(IntPtr hObject);

        [DllImport("dwmapi.dll", PreserveSig = true, SetLastError = true)]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd,
                                                       DwmWindowAttribute dwmAttribute,
                                                       IntPtr pvAttribute,
                                                       uint cbAttribute);

        [DllImport("dwmapi.dll", PreserveSig = true, SetLastError = true)]
        private static extern int DwmSetIconicThumbnail(IntPtr hwnd,
                                                       IntPtr hbmp,
                                                       uint dwSITFlags);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hwnd, int index);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);

        [Guid("56FDF344-FD6D-11d0-958A-006097C9A090")]
        [ClassInterface(ClassInterfaceType.None)]
        [ComImport]
        private class TaskbarList { }

        [Flags]
        private enum DwmWindowAttribute : uint
        {
            DWMWA_NCRENDERING_ENABLED = 1,
            DWMWA_NCRENDERING_POLICY,
            DWMWA_TRANSITIONS_FORCEDISABLED,
            DWMWA_ALLOW_NCPAINT,
            DWMWA_CAPTION_BUTTON_BOUNDS,
            DWMWA_NONCLIENT_RTL_LAYOUT,
            DWMWA_FORCE_ICONIC_REPRESENTATION,
            DWMWA_FLIP3D_POLICY,
            DWMWA_EXTENDED_FRAME_BOUNDS,
            DWMWA_HAS_ICONIC_BITMAP,
            DWMWA_DISALLOW_PEEK,
            DWMWA_EXCLUDED_FROM_PEEK,
            DWMWA_LAST
        }

        [Flags]
        private enum THBFLAGS
        {
            THBF_ENABLED = 0x00000000,
            THBF_DISABLED = 0x00000001,
            THBF_DISMISSONCLICK = 0x00000002,
            THBF_NOBACKGROUND = 0x00000004,
            THBF_HIDDEN = 0x00000008,
            THBF_NONINTERACTIVE = 0x00000010
        }

        private enum TBPFLAG
        {
            TBPF_NOPROGRESS = 0,
            TBPF_INDETERMINATE = 0x1,
            TBPF_NORMAL = 0x2,
            TBPF_ERROR = 0x4,
            TBPF_PAUSED = 0x8
        }

        private enum STPFLAG
        {
            STPF_NONE = 0x0,
            STPF_USEAPPTHUMBNAILALWAYS = 0x1,
            STPF_USEAPPTHUMBNAILWHENACTIVE = 0x2,
            STPF_USEAPPPEEKALWAYS = 0x4,
            STPF_USEAPPPEEKWHENACTIVE = 0x8
        }

        private enum THBMASK
        {
            THB_BITMAP = 0x1,
            THB_ICON = 0x2,
            THB_TOOLTIP = 0x4,
            THB_FLAGS = 0x8
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct THUMBBUTTON
        {

            private const int THBN_CLICKED = 0x1800;

            [MarshalAs(UnmanagedType.U4)]
            private THBMASK dwMask;

            private uint iId;

            private uint iBitmap;

            private IntPtr hIcon;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            private string szTip;

            [MarshalAs(UnmanagedType.U4)]
            private THBFLAGS dwFlags;

        }

        [ComImport]
        [Guid("c43dc798-95d1-4bea-9030-bb99e2983a1a")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface ITaskbarList4
        {

            // ITaskbarList
            [PreserveSig]
            void HrInit();

            [PreserveSig]
            void AddTab(IntPtr hwnd);

            [PreserveSig]
            void DeleteTab(IntPtr hwnd);

            [PreserveSig]
            void ActivateTab(IntPtr hwnd);

            [PreserveSig]
            void SetActiveAlt(IntPtr hwnd);


            // ITaskbarList2
            [PreserveSig]
            void MarkFullscreenWindow(
                IntPtr hwnd,
                [MarshalAs(UnmanagedType.Bool)] bool fFullscreen);


            // ITaskbarList3
            [PreserveSig]
            void SetProgressValue(IntPtr hwnd, UInt64 ullCompleted, UInt64 ullTotal);

            [PreserveSig]
            void SetProgressState(IntPtr hwnd, TBPFLAG tbpFlags);

            [PreserveSig]
            void RegisterTab(IntPtr hwndTab, IntPtr hwndMDI);

            [PreserveSig]
            void UnregisterTab(IntPtr hwndTab);

            [PreserveSig]
            void SetTabOrder(IntPtr hwndTab, IntPtr hwndInsertBefore);

            [PreserveSig]
            void SetTabActive(IntPtr hwndTab, IntPtr hwndInsertBefore, uint dwReserved);

            [PreserveSig]
            IntPtr ThumbBarAddButtons(
                IntPtr hwnd,
                uint cButtons,
                [MarshalAs(UnmanagedType.LPArray)] THUMBBUTTON[] pButtons);

            [PreserveSig]
            IntPtr ThumbBarUpdateButtons(
                IntPtr hwnd,
                uint cButtons,
                [MarshalAs(UnmanagedType.LPArray)] THUMBBUTTON[] pButtons);

            [PreserveSig]
            void ThumbBarSetImageList(IntPtr hwnd, IntPtr himl);

            [PreserveSig]
            void SetOverlayIcon(
              IntPtr hwnd,
              IntPtr hIcon,
              [MarshalAs(UnmanagedType.LPWStr)] string pszDescription);

            [PreserveSig]
            void SetThumbnailTooltip(
                IntPtr hwnd,
                [MarshalAs(UnmanagedType.LPWStr)] string pszTip);

            [PreserveSig]
            void SetThumbnailClip(
                IntPtr hwnd,
                ref Int32Rect prcClip);

            // ITaskbarList4
            void SetTabProperties(IntPtr hwndTab, STPFLAG stpFlags);

        }

        private const int WM_DWMSENDICONICTHUMBNAIL = 0x0323;

        private const int WS_EX_NOACTIVATE = 0x08000000;

        private const int GWL_EXSTYLE = -20;

        private const int WM_ACTIVATE = 0x0006;
        private const int WM_CLOSE = 0x0010;

        #endregion

        public delegate void ThumbnailActivatedEventHandler(ClipboardThumbnailItem sender);
        public event ThumbnailActivatedEventHandler ThumbnailActivated;

        public delegate void ThumbnailClosedEventHandler(ClipboardThumbnailItem sender);
        public event ThumbnailClosedEventHandler ThumbnailClosed;

        //HACK: this is Windows Forms types, not WPF
        private readonly Bitmap _preview;

        private readonly ITaskbarList4 _taskbarList;

        private readonly ClipboardSnapshot _snapshot;

        public IntPtr ThumbnailHandle { get; private set; }

        public ClipboardThumbnailItem(BitmapSource icon, BitmapSource preview, string title, ClipboardSnapshot snapshot)
        {
            ShowActivated = false;
            Focusable = false;
            Icon = icon;
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            Title = title;
            Width = 0;
            Height = 0;
            Owner = GlobalMessageListener.Instance;
            Top = -short.MaxValue;
            Left = -short.MaxValue;

            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri("pack://application:,,,/Shapeshifter;component/Images/Overlay.png");
            bitmap.EndInit();

            TaskbarItemInfo = new TaskbarItemInfo()
            {
                Overlay = bitmap
            };

            _snapshot = snapshot;

            _preview = BitmapFromBitmapSource(preview);

            _taskbarList = (ITaskbarList4)new TaskbarList();
            _taskbarList.HrInit();
        }

        private Bitmap BitmapFromBitmapSource(BitmapSource source)
        {
            using (var outStream = new MemoryStream())
            {

                BitmapEncoder encoder = new BmpBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(source));
                encoder.Save(outStream);

                var bitmap = new Bitmap(outStream);

                return bitmap;

            }

        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            //install clipboard hooks
            var communicationHook = (HwndSource)PresentationSource.FromVisual(this);
            if (communicationHook == null)
            {
                throw new Exception("Could not install the thumbnail manager");
            }

            communicationHook.AddHook(OnWindowMessage);

            var helper = new WindowInteropHelper(this);
            ThumbnailHandle = helper.Handle;

            int style = GetWindowLong(ThumbnailHandle, GWL_EXSTYLE);
            SetWindowLong(ThumbnailHandle, GWL_EXSTYLE, style | WS_EX_NOACTIVATE);

            var @true = Marshal.AllocHGlobal(sizeof(int));
            Marshal.WriteInt32(@true, 1); // true

            DwmSetWindowAttribute(ThumbnailHandle, DwmWindowAttribute.DWMWA_HAS_ICONIC_BITMAP, @true,
                                  sizeof(int));
            DwmSetWindowAttribute(ThumbnailHandle, DwmWindowAttribute.DWMWA_FORCE_ICONIC_REPRESENTATION, @true,
                                  sizeof(int));
            DwmSetWindowAttribute(ThumbnailHandle, DwmWindowAttribute.DWMWA_EXCLUDED_FROM_PEEK, @true,
                                  sizeof(int));
            DwmSetWindowAttribute(ThumbnailHandle, DwmWindowAttribute.DWMWA_DISALLOW_PEEK, @true,
                                  sizeof(int));

            _taskbarList.RegisterTab(ThumbnailHandle, _snapshot.InsideClipboardOwnerHandle);
            _taskbarList.SetTabProperties(ThumbnailHandle, STPFLAG.STPF_USEAPPTHUMBNAILALWAYS);

        }

        public void MoveThumbnail(IntPtr insertBefore)
        {
            _taskbarList.SetTabOrder(ThumbnailHandle, insertBefore);
        }

        private IntPtr OnWindowMessage(IntPtr hwnd, int msg, IntPtr wparam, IntPtr lparam, ref bool handled)
        {
            if (msg == WM_DWMSENDICONICTHUMBNAIL)
            {

                var previewPointer = _preview.GetHbitmap();
                DwmSetIconicThumbnail(hwnd, previewPointer, 0);
                DeleteObject(previewPointer);

            }
            else if (msg == WM_ACTIVATE)
            {
                handled = true;

                if (ThumbnailActivated != null)
                {

                    ThumbnailActivated(this);

                }

            } else if(msg == WM_CLOSE)
            {

                if (ThumbnailClosed != null)
                {

                    ThumbnailClosed(this);

                }

            }
            return IntPtr.Zero;
        }
    }
}
