namespace Shapeshifter.Desktop.Functionality.Clipboard.Data
{
    /// <summary>
    /// List of known clipboard formats which are the base types (that other types can be converted to).
    /// For more details, see http://msdn.microsoft.com/en-us/library/windows/desktop/ff729168(v=vs.85).aspx
    /// </summary>
    public struct KnownClipboardFormats
    {
        public const uint CF_BITMAP = 2;
        public const uint CF_DIB = 8;
        public const uint CF_DIBV5 = 17;
        public const uint CF_DIF = 5;
        public const uint CF_DSPBITMAP = 0x0082;
        public const uint CF_DSPENHMETAFILE = 0x008E;
        public const uint CF_DSPMETAFILEPICT = 0x0083;
        public const uint CF_DSPTEXT = 0x0081;
        public const uint CF_ENHMETAFILE = 14;
        public const uint CF_GDIOBJFIRST = 0x0300;
        public const uint CF_GDIOBJLAST = 0x03FF;
        public const uint CF_HDROP = 15;
        public const uint CF_LOCALE = 16;
        public const uint CF_METAFILEPICT = 3;
        public const uint CF_OEMTEXT = 7;
        public const uint CF_OWNERDISPLAY = 0x0080;
        public const uint CF_PALETTE = 9;
        public const uint CF_PENDATA = 10;
        public const uint CF_PRIVATEFIRST = 0x0200;
        public const uint CF_PRIVATELAST = 0x02FF;
        public const uint CF_RIFF = 11;
        public const uint CF_SYLK = 4;
        public const uint CF_TEXT = 1;
        public const uint CF_TIFF = 6;
        public const uint CF_UNICODETEXT = 13;
        public const uint CF_WAVE = 12;
    }
}