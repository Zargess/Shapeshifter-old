using Shapeshifter.Desktop.Functionality.Clipboard.DataTypes;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;

namespace Shapeshifter.Desktop.Functionality.Clipboard.Session
{
    public class ClipboardSession : IDisposable
    {
        #region apis

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool EmptyClipboard();

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool CloseClipboard();

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool OpenClipboard(IntPtr hWndNewOwner);

        [DllImport("user32.dll")]
        private static extern IntPtr GetOpenClipboardWindow();

        #endregion

        public ClipboardSession(IntPtr owner)
        {

            try
            {

                int retries = 0;
                while((IsOpen = OpenClipboard(owner)) == false && retries++ < 5)
                {
                    Thread.Sleep(25);
                }

            }
            catch (Exception)
            {
                IsOpen = false;
            }

            if(!IsOpen)
            {
                var application = (App)Application.Current;

                try
                {
                    var currentOwner = ClipboardSource.ApplicationNameFromWindowHandle(GetOpenClipboardWindow());
                    application.SignalClipboardDisturbance(currentOwner);
                } catch(ArgumentException)
                {
                    application.SignalClipboardDisturbance();
                }
            }
        }

        public bool IsOpen { get; private set; }

        #region IDisposable Members

        public void Dispose()
        {

            if (IsOpen)
            {

                int retries = 0;
                bool hasClosed = false;
                while ((hasClosed = CloseClipboard()) == false && retries++ < 5)
                {
                    Thread.Sleep(200);
                }

                if (!hasClosed)
                {
                    var lastError = Marshal.GetLastWin32Error();
                    if (lastError == 1418)
                    {

                        var application = (App) Application.Current;
                        application.SignalClipboardDisturbance();

                        //ERROR_CLIPBOARD_NOT_OPEN
                    }
                    else
                    {
                        throw new Exception("Could not close clipboard, error-code was " + lastError);
                    }
                }

            }
        }

        #endregion

        public void ClearClipboard()
        {
            if (!IsOpen)
            {
                throw new InvalidOperationException("Can't clear clipboard when it is not open");
            }

            int retries = 0;
            bool hasEmptied = false;
            while ((hasEmptied = EmptyClipboard()) == false && retries++ < 5)
            {
                Thread.Sleep(25);
            }

            if (!hasEmptied)
            {
                throw new Exception("Could not clear clipboard");
            }
        }
    }
}