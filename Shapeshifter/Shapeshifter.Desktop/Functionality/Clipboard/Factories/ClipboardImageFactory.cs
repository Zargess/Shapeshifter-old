using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using Shapeshifter.Desktop.Functionality.Clipboard.Data;
using Shapeshifter.Desktop.Functionality.Clipboard.DataTypes;

#if !NETFX_CORE
using Shapeshifter.Desktop.Functionality.Clipboard.Factories;
using PixelFormat = System.Drawing.Imaging.PixelFormat;
using System.Drawing;

using Shapeshifter.Desktop.Functionality.Helpers;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Diagnostics;
using System.Drawing.Imaging;

#endif

namespace Shapeshifter.Desktop.Functionality.Factories
{
    public sealed class ClipboardImageFactory : ClipboardItemFactory
    {

#if !NETFX_CORE

        #region Desktop APIs

        [DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);

        #region Nested type: BITMAPFILEHEADER

        [StructLayout(LayoutKind.Sequential, Pack = 2)]
        private struct BITMAPFILEHEADER
        {
            public static readonly short BM = 0x4d42;

            public short bfType;
            public int bfSize;
            public short bfReserved1;
            public short bfReserved2;
            public int bfOffBits;
        }

        #endregion

        #region Nested type: BITMAPINFOHEADER

        [StructLayout(LayoutKind.Sequential)]
        private struct BITMAPINFOHEADER
        {
            public readonly int biSize;
            public readonly int biWidth;
            public readonly int biHeight;
            public readonly short biPlanes;
            public readonly short biBitCount;
            public readonly int biCompression;
            public readonly int biSizeImage;
            public readonly int biXPelsPerMeter;
            public readonly int biYPelsPerMeter;
            public readonly int biClrUsed;
            public readonly int biClrImportant;
        }

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

        #endregion

#endif

        public ClipboardImageFactory(ClipboardSnapshot snapshot = null)
            : base(snapshot)
        {
        }

#if NETFX_CORE
        protected async override Task<ClipboardItem> CreateNew(ClipboardSnapshot snapshot)
#else
        protected override ClipboardItem CreateNew(ClipboardSnapshot snapshot)
#endif
        {

            try
            {

                if (snapshot.HasFormat(KnownClipboardFormats.CF_DIBV5))
                //device independent bitmap (with transparency - yay!)
                {
                    IntPtr pointer = IntPtr.Zero;

                    try
                    {

                        pointer = snapshot.FetchDataPointer(KnownClipboardFormats.CF_DIBV5);

                        var infoHeader =
                            (BITMAPV5HEADER)Marshal.PtrToStructure(pointer, typeof(BITMAPV5HEADER));

                        using (
                            var bitmap = new Bitmap((int)infoHeader.bV5Width, (int)infoHeader.bV5Height,
                                                    (int)(infoHeader.bV5SizeImage / infoHeader.bV5Height),
                                                    PixelFormat.Format32bppArgb,
                                                    new IntPtr(pointer.ToInt64() + infoHeader.bV5Size)))
                        {

                            var bitmapSource = new RenderTargetBitmap((int)infoHeader.bV5Width,
                                                                      (int)infoHeader.bV5Height,
                                                                      96, 96, PixelFormats.Pbgra32);
                            var visual = new DrawingVisual();
                            var drawingContext = visual.RenderOpen();

                            drawingContext.DrawImage(CreateBitmapSourceFromBitmap(bitmap),
                                                     new Rect(0, 0, (int)infoHeader.bV5Width,
                                                              (int)infoHeader.bV5Height));

                            drawingContext.Close();

                            bitmapSource.Render(visual);

                            var result = new ClipboardImage();
                            result.Image = bitmapSource;

                            return result;

                        }

                    }
                    finally
                    {
                        if (pointer != IntPtr.Zero)
                        {
                            Marshal.FreeHGlobal(pointer);
                        }
                    }

                }
            }
            catch (ArgumentException)
            {
                //this dibv5 format was faulty. let's roll back to normal dib.
            }

            //TODO: do we need the below or don't we? will the above work for all formats?
            if (snapshot.HasFormat(KnownClipboardFormats.CF_DIB)) //device independent bitmap
            {
                byte[] buffer = snapshot.FetchData(KnownClipboardFormats.CF_DIB);

                var infoHeader =
                    ApiHelper.ByteArrayToStructure<BITMAPINFOHEADER>(buffer);

                int fileHeaderSize = Marshal.SizeOf(typeof(BITMAPFILEHEADER));
                int infoHeaderSize = infoHeader.biSize;
                int fileSize = fileHeaderSize + infoHeader.biSize + infoHeader.biSizeImage;

                var fileHeader = new BITMAPFILEHEADER();
                fileHeader.bfType = BITMAPFILEHEADER.BM;
                fileHeader.bfSize = fileSize;
                fileHeader.bfReserved1 = 0;
                fileHeader.bfReserved2 = 0;
                fileHeader.bfOffBits = fileHeaderSize + infoHeaderSize + infoHeader.biClrUsed * 4;

                byte[] fileHeaderBytes =
                    ApiHelper.StructureToByteArray(fileHeader);

                var bitmapStream = new MemoryStream();
                bitmapStream.Write(fileHeaderBytes, 0, fileHeaderSize);
                bitmapStream.Write(buffer, 0, buffer.Length);
                bitmapStream.Seek(0, SeekOrigin.Begin);

                BitmapFrame bitmap = BitmapFrame.Create(bitmapStream);

                var result = new ClipboardImage();
                result.Image = bitmap;

                return result;
            }

            if(snapshot.HasFormat(KnownClipboardFormats.CF_ENHMETAFILE))
            {
                byte[] buffer = snapshot.FetchData(KnownClipboardFormats.CF_ENHMETAFILE);
                using (var memoryStream = new MemoryStream(buffer))
                {
                    using(var metafile = new Metafile(memoryStream))
                    {
                        using (var bitmap = new Bitmap(metafile.Width, metafile.Height))
                        {
                            using (var graphics = Graphics.FromImage(bitmap))
                            {
                                graphics.DrawImage(metafile, 0, 0);

                                var result = new ClipboardImage();
                                result.Image = CreateBitmapSourceFromBitmap(bitmap);

                                return result;
                            }
                        }
                    }
                }
            }

            var formatResult = string.Empty;
            foreach (var format in snapshot.FormatPriorities)
            {
                formatResult += format + ", ";
            }
            if (!string.IsNullOrEmpty(formatResult))
            {
                formatResult = formatResult.Substring(0, formatResult.Length - 2);
            }

            throw new InvalidOperationException("None of the image formats from " + ClipboardSource.ApplicationNameFromWindowHandle(snapshot.OutsideClipboardOwnerHandle) + " were known (" + formatResult + ")");

        }
    }
}