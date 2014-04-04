using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.IO;

#if !NETFX_CORE

using System.Threading;
using System.Windows;
using System.Windows.Media.Imaging;
using Shapeshifter.Desktop.Functionality.Clipboard.Session;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.ExceptionServices;

#else

using Windows.UI.Xaml.Media.Imaging;
using Windows.ApplicationModel.DataTransfer;

using MetroClipboard = Windows.ApplicationModel.DataTransfer.Clipboard;

#endif

namespace Shapeshifter.Desktop.Functionality.Clipboard.Data
{
    public class ClipboardSnapshot
    {

#if !NETFX_CORE
        private readonly Dictionary<uint, byte[]> _snapshot;
#else
        private readonly Dictionary<string, byte[]> _snapshot;
#endif

#if !NETFX_CORE

        #region Desktop APIs

        #region gdi

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int nWidth, int nHeight);

        [DllImport("gdi32.dll", SetLastError = true)]
        private static extern IntPtr CreateCompatibleDC(IntPtr hdc);

        [DllImport("gdi32.dll", ExactSpelling = true, PreserveSig = true, SetLastError = true)]
        private static extern IntPtr SelectObject(IntPtr hdc, IntPtr hgdiobj);

        [DllImport("gdi32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool BitBlt(IntPtr hdc, int nXDest, int nYDest, int nWidth, int nHeight, IntPtr hdcSrc, int nXSrc, int nYSrc, TernaryRasterOperations dwRop);

        [DllImport("gdi32.dll", SetLastError = true)]
        private static extern IntPtr SetEnhMetaFileBits(uint cbBuffer, byte[] lpData);

        [DllImport("gdi32.dll", SetLastError = true)]
        private static extern bool DeleteEnhMetaFile(IntPtr hemf);

        [DllImport("gdi32.dll", SetLastError = true)]
        private static extern uint GetEnhMetaFileBits(IntPtr hemf, uint cbBuffer,
                                                      [Out] byte[] lpbBuffer);

        private enum TernaryRasterOperations : uint
        {
            /// <summary>dest = source</summary>
            SRCCOPY = 0x00CC0020,
            /// <summary>dest = source OR dest</summary>
            SRCPAINT = 0x00EE0086,
            /// <summary>dest = source AND dest</summary>
            SRCAND = 0x008800C6,
            /// <summary>dest = source XOR dest</summary>
            SRCINVERT = 0x00660046,
            /// <summary>dest = source AND (NOT dest)</summary>
            SRCERASE = 0x00440328,
            /// <summary>dest = (NOT source)</summary>
            NOTSRCCOPY = 0x00330008,
            /// <summary>dest = (NOT src) AND (NOT dest)</summary>
            NOTSRCERASE = 0x001100A6,
            /// <summary>dest = (source AND pattern)</summary>
            MERGECOPY = 0x00C000CA,
            /// <summary>dest = (NOT source) OR dest</summary>
            MERGEPAINT = 0x00BB0226,
            /// <summary>dest = pattern</summary>
            PATCOPY = 0x00F00021,
            /// <summary>dest = DPSnoo</summary>
            PATPAINT = 0x00FB0A09,
            /// <summary>dest = pattern XOR dest</summary>
            PATINVERT = 0x005A0049,
            /// <summary>dest = (NOT dest)</summary>
            DSTINVERT = 0x00550009,
            /// <summary>dest = BLACK</summary>
            BLACKNESS = 0x00000042,
            /// <summary>dest = WHITE</summary>
            WHITENESS = 0x00FF0062,
            /// <summary>
            /// Capture window as seen on screen.  This includes layered windows 
            /// such as WPF windows with AllowsTransparency="true"
            /// </summary>
            CAPTUREBLT = 0x40000000
        }

        #endregion

        [DllImport("user32.dll")]
        private static extern IntPtr GetClipboardOwner();

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("kernel32.dll")]
        static extern IntPtr GlobalLock(IntPtr hMem);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int CountClipboardFormats();

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint EnumClipboardFormats(uint format);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr GetClipboardData(uint uFormat);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern UIntPtr GlobalSize(IntPtr hMem);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetClipboardData(uint uFormat, IntPtr hMem);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool IsClipboardFormatAvailable(uint format);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetClipboardFormatName(uint format, [Out] StringBuilder
                                                                          lpszFormatName, int cchMaxCount);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GlobalAlloc(uint uFlags, UIntPtr dwBytes);

        [StructLayout(LayoutKind.Explicit)]
        private struct BITMAPV5HEADER
        {
            [FieldOffset(0)]
            public uint bV5Size;
            [FieldOffset(4)]
            public int bV5Width;
            [FieldOffset(8)]
            public int bV5Height;
            [FieldOffset(12)]
            public ushort bV5Planes;
            [FieldOffset(14)]
            public ushort bV5BitCount;
            [FieldOffset(16)]
            public uint bV5Compression;
            [FieldOffset(20)]
            public uint bV5SizeImage;
            [FieldOffset(24)]
            public int bV5XPelsPerMeter;
            [FieldOffset(28)]
            public int bV5YPelsPerMeter;
            [FieldOffset(32)]
            public uint bV5ClrUsed;
            [FieldOffset(36)]
            public uint bV5ClrImportant;
            [FieldOffset(40)]
            public uint bV5RedMask;
            [FieldOffset(44)]
            public uint bV5GreenMask;
            [FieldOffset(48)]
            public uint bV5BlueMask;
            [FieldOffset(52)]
            public uint bV5AlphaMask;
            [FieldOffset(56)]
            public uint bV5CSType;
            [FieldOffset(60)]
            public CIEXYZTRIPLE bV5Endpoints;
            [FieldOffset(96)]
            public uint bV5GammaRed;
            [FieldOffset(100)]
            public uint bV5GammaGreen;
            [FieldOffset(104)]
            public uint bV5GammaBlue;
            [FieldOffset(108)]
            public uint bV5Intent;
            [FieldOffset(112)]
            public uint bV5ProfileData;
            [FieldOffset(116)]
            public uint bV5ProfileSize;
            [FieldOffset(120)]
            public uint bV5Reserved;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct CIEXYZ
        {
            public uint ciexyzX; //FXPT2DOT30
            public uint ciexyzY; //FXPT2DOT30
            public uint ciexyzZ; //FXPT2DOT30
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct CIEXYZTRIPLE
        {
            public CIEXYZ ciexyzRed;
            public CIEXYZ ciexyzGreen;
            public CIEXYZ ciexyzBlue;
        }

        public string GetClipboardFormatName(uint format)
        {
            try
            {
                var formatNameBuilder = new StringBuilder();
                GetClipboardFormatName(format, formatNameBuilder, 128);
                return formatNameBuilder.ToString();
            }
            catch (Exception)
            {
                return "Unknown format";
            }
        }

        private static BitmapSource CreateBitmapSourceFromBitmap(Bitmap bitmap)
        {
            if (bitmap == null)
                throw new ArgumentNullException("bitmap");

            IntPtr hBitmap = bitmap.GetHbitmap();

            try
            {
                return System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                    hBitmap,
                    IntPtr.Zero,
                    Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());
            }
            finally
            {
                DeleteObject(hBitmap);
            }
        }

        #endregion

#else

        #region Metro APIs



        #endregion

#endif

        /// <summary>
        /// Returns a sequence of formats in priored order.
        /// </summary>
#if !NETFX_CORE
        public IEnumerable<uint> FormatPriorities
#else
        public IEnumerable<string> FormatPriorities
#endif
        {
            get { return _snapshot.Keys; }
        }

#if !NETFX_CORE

        /// <summary>
        /// The owner of the clipboard when others paste data.
        /// </summary>
        public IntPtr OutsideClipboardOwnerHandle { get; private set; }

        /// <summary>
        /// The internal owner that the clipboard is set to when Shapeshifter pastes data.
        /// </summary>
        public IntPtr InsideClipboardOwnerHandle { get; private set; }

#endif

        /// <summary>
        /// Gets a boolean indicating wether or not the snapshot contains data.
        /// </summary>
        public bool ContainsData
        {
            get { return _snapshot.Keys.Count > 0; }
        }

        /// <summary>
        /// The owner of the clipboard when fetching the data.
        /// </summary>
#if !NETFX_CORE
        /// <param name="insideClipboardOwner"></param>
        private ClipboardSnapshot(IntPtr insideClipboardOwner)
        {
            _snapshot = new Dictionary<uint, byte[]>();
            InsideClipboardOwnerHandle = insideClipboardOwner;
        }
#else
        private ClipboardSnapshot()
        {
            _snapshot = new Dictionary<string, byte[]>();
        }
#endif

#if !NETFX_CORE
        /// <summary>
        /// Inserts some data into the snapshot given the address to the data in memory.
        /// </summary>
        /// <param name="format"></param>
        /// <param name="pointer"></param>
        [HandleProcessCorruptedStateExceptions]
        private void InsertDataPointer(uint format, IntPtr pointer)
        {

            if (format == KnownClipboardFormats.CF_ENHMETAFILE)
            {
                uint size = GetEnhMetaFileBits(pointer, 0, null);

                Trace.WriteLine("Size is " + size);

                var buffer = new byte[size];
                GetEnhMetaFileBits(pointer, size, buffer);
                DeleteEnhMetaFile(pointer);

                _snapshot.Add(format, buffer);
            }
            else if (format == KnownClipboardFormats.CF_DIBV5)
            {

                var infoHeader =
                    (BITMAPV5HEADER)Marshal.PtrToStructure(pointer, typeof(BITMAPV5HEADER));
                var dataSize = infoHeader.bV5SizeImage + infoHeader.bV5Size;

                var buffer = new byte[dataSize];
                Marshal.Copy(pointer, buffer, 0, (int)dataSize);
                //using (
                //    var bitmap = new Bitmap((int)infoHeader.bV5Width, (int)infoHeader.bV5Height,
                //                            (int)(infoHeader.bV5SizeImage / infoHeader.bV5Height),
                //                            PixelFormat.Format32bppArgb,
                //                            new IntPtr(pointer.ToInt32() + infoHeader.bV5Size)))
                //{

                //    if (!_snapshot.ContainsKey(ThirdPartyClipboardFormats.CF_PNG))
                //    {
                //        var bitmapSource = CreateBitmapSourceFromBitmap(bitmap);
                //        var encoder = new PngBitmapEncoder();

                //        encoder.Interlace = PngInterlaceOption.On;
                //        encoder.Frames.Add(BitmapFrame.Create(bitmapSource));

                //        using (var stream = new MemoryStream())
                //        {
                //            encoder.Save(stream);

                //            var bytes = stream.ToArray();
                //            _snapshot.Add(ThirdPartyClipboardFormats.CF_PNG, bytes);
                //        }
                //    }

                //    //backwards compatibility - for those without transparency
                //    //using (var outerBitmap = new Bitmap(bitmap.Width, bitmap.Height, PixelFormat.Format24bppRgb))
                //    //{

                //    //    using (var graphics = Graphics.FromImage(outerBitmap))
                //    //    {

                //    //        graphics.Clear(Color.White);

                //    //    }

                //    //    //TODO: insertion point
                //    //    if (!_snapshot.ContainsKey(KnownClipboardFormats.CF_BITMAP))
                //    //    {

                //    //        using (var stream = new MemoryStream())
                //    //        {

                //    //            outerBitmap.Save(stream, ImageFormat.Bmp);

                //    //            var bytes = stream.ToArray();
                //    //            _snapshot.Add(KnownClipboardFormats.CF_BITMAP, bytes);

                //    //        }

                //    //    }

                //    //}
                //}

                _snapshot.Add(format, buffer);

            }
            else if (!_snapshot.ContainsKey(format))
            {

                UIntPtr globalSize = GlobalSize(pointer);

                if (globalSize == UIntPtr.Zero)
                {
                    throw new Exception("Could not fetch the size of an in-memory clipboard data object");
                }

                var dataSize = (int)globalSize.ToUInt32();

                try
                {

                    var buffer = new byte[dataSize];
                    Marshal.Copy(pointer, buffer, 0, dataSize);

                    _snapshot.Add(format, buffer);

                }
                catch (AccessViolationException)
                {
                    throw new InvalidOperationException("A clipboard format (" + GetClipboardFormatName(format) + ") with the ID " + format + " could not be retrieved since an inproper reading method was used");
                }
            }
        }

#else

        private void InsertData(string format, object data)
        {
            _snapshot.Add(format, data);
        }

#endif

#if !NETFX_CORE
        public static ClipboardSnapshot CreateSnapshot(IntPtr clipboardOwner)
#else
        public static ClipboardSnapshot CreateSnapshot()
#endif
        {

#if !NETFX_CORE
            var snapshot = new ClipboardSnapshot(clipboardOwner);

            int formatCount = CountClipboardFormats();
            if (formatCount > 0)
            {
#else
            var snapshot = new ClipboardSnapshot();
#endif

#if !NETFX_CORE

                using (var session = new ClipboardSession(clipboardOwner))
                {

                    Trace.WriteLine("Performing clipboard snapshot");

                    if (session.IsOpen)
                    {

                        var clipboardOwnerHandle = GetClipboardOwner();
                        var foregroundWindowHandle = GetForegroundWindow();

                        snapshot.OutsideClipboardOwnerHandle = clipboardOwnerHandle != IntPtr.Zero
                                                                   ? clipboardOwnerHandle
                                                                   : foregroundWindowHandle;

                        if (snapshot.OutsideClipboardOwnerHandle != IntPtr.Zero &&
                            snapshot.OutsideClipboardOwnerHandle != clipboardOwner)
                        {

                            var format = (uint)0;
                            while (0 != (format = EnumClipboardFormats(format)))
                            {

                                if (IsClipboardFormatAvailable(format))
                                {

                                    if ((format != KnownClipboardFormats.CF_BITMAP &&
                                         format != KnownClipboardFormats.CF_METAFILEPICT &&
                                         format != KnownClipboardFormats.CF_TEXT &&
                                         format != KnownClipboardFormats.CF_OEMTEXT &&
                                         format != KnownClipboardFormats.CF_LOCALE)
                                        &&
                                (format != AvoidedClipboardFormats.CF_SYSTEM_DRAWING_BITMAP))
                                    {
#else

            var content = MetroClipboard.GetContent();
            foreach (var format in content.AvailableFormats)
            {
#endif

                                        //Get pointer to clipboard data in the selected format

#if !NETFX_CORE
                                        int retries = 0;
                                        IntPtr clipboardDataPointer;
                                        while ((clipboardDataPointer = GetClipboardData(format)) == IntPtr.Zero &&
                                               retries++ < 5)
                                        {
                                            Thread.Sleep(100);
                                        }

                                        if (clipboardDataPointer != IntPtr.Zero)
                                        {
                                            snapshot.InsertDataPointer(format, clipboardDataPointer);
                                        }

#else

                var clipboardData = content.GetDataAsync(format);
                snapshot.InsertData(format, clipboardData);

#endif

#if !NETFX_CORE
                                    }
                                }
                            }
#else
            }
#endif

#if !NETFX_CORE

                        }
                    }

                }

            }

#endif
            return snapshot;
        }

#if !NETFX_CORE
        public bool HasFormat(uint format)
        {
            return _snapshot.ContainsKey(format);
        }
#else
        public bool HasFormat(string format)
        {
            return _snapshot.ContainsKey(format);
        }
#endif

#if !NETFX_CORE
        public IntPtr FetchDataPointer(uint format)
        {
            if (HasFormat(format))
            {
                byte[] bytes = _snapshot[format];

                IntPtr unmanagedPointer;
                if (format == KnownClipboardFormats.CF_ENHMETAFILE)
                {
                    unmanagedPointer = SetEnhMetaFileBits((uint)bytes.Length, bytes);
                }
                else
                {
                    unmanagedPointer = GlobalAlloc(0x0000, new UIntPtr((uint)bytes.Length));
                    unmanagedPointer = GlobalLock(unmanagedPointer);
                    Marshal.Copy(bytes, 0, unmanagedPointer, bytes.Length);
                }

                return unmanagedPointer;
            }
            else
            {
                return IntPtr.Zero;
            }
        }
#endif

#if !NETFX_CORE
        public byte[] FetchData(uint format)
#else
        public object FetchData(string format)
#endif
        {
            if (HasFormat(format))
            {
                return _snapshot[format];
            }
            else
            {
                throw new KeyNotFoundException("The clipboard snapshot doesn't contain a key with that format");
            }
        }

#if !NETFX_CORE
        [HandleProcessCorruptedStateExceptions]
#endif
        public void Inject()
        {

#if !NETFX_CORE
            using (var session = new ClipboardSession(InsideClipboardOwnerHandle))
            {

                if (session.IsOpen)
                {

                    session.ClearClipboard();
#else
            MetroClipboard.Clear();
#endif

#if !NETFX_CORE
                    foreach (uint format in FormatPriorities)
#else
            var package = new DataPackage();
            foreach (string format in FormatPriorities)
#endif
                    {

#if !NETFX_CORE
                        IntPtr dataPointer = FetchDataPointer(format);

                        try
                        {
                            int retries = 0;
                            IntPtr clipboardInjectResult;
                            while ((clipboardInjectResult = SetClipboardData(format, dataPointer)) == IntPtr.Zero &&
                                   retries++ < 5)
                            {
                                Thread.Sleep(100);
                            }

                            if (clipboardInjectResult == IntPtr.Zero)
                            {
                                if (Debugger.IsAttached)
                                {
                                    MessageBox.Show(
                                        "Clipboard format could not be injected. The ID is " + format +
                                        ", and has the name \"" + GetClipboardFormatName(format) + "\".",
                                        "Clipboard format inject error", MessageBoxButton.OK,
                                        MessageBoxImage.Warning);
                                }
                                else
                                {

                                    int error = Marshal.GetLastWin32Error();
                                    if (error != 0)
                                    {
                                        if (error == 1418)
                                        {
                                            var application = (App) Application.Current;
                                            application.SignalClipboardDisturbance();
                                            //ERROR_CLIPBOARD_NOT_OPEN
                                        }
                                        else
                                        {

                                            throw new Exception("A Win32 error occured with the error-code " + error);

                                        }
                                    }

                                }
                            }
                        }
                        catch (ExternalException ex)
                        {
                            throw new Exception("An external exception occured with the error-code " + ex.ErrorCode);
                        }

#else
                var data = FetchData(format);
                package.SetData(format, data);
#endif

                    }

#if !NETFX_CORE
                }
            }
#else
            MetroClipboard.SetContent(package);
            MetroClipboard.Flush();
#endif
        }
    }
}