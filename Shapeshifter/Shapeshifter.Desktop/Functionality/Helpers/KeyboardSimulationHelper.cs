using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace Shapeshifter.Desktop.Functionality.Helpers
{
    public static class KeyboardSimulationHelper
    {
        #region apis

        [DllImport("user32.dll")]
        static extern uint MapVirtualKey(uint uCode, uint uMapType);

        [DllImport("user32.dll")]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

        #endregion

        public static void SendKey(byte key, bool down)
        {
            byte scan = (byte)MapVirtualKey((uint)key & 0xff, 0);

            if (down)
            {
                keybd_event(key, scan, 0, UIntPtr.Zero);
            }
            else
            {
                keybd_event(key, (byte)(scan | 0x80), 2, UIntPtr.Zero);
            }
        }
    }
}
