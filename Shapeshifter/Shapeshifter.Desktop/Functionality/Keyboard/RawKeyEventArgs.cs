using System;
using System.Windows.Input;

namespace Shapeshifter.Desktop.Functionality.Keyboard
{
    /// <summary>
    /// Raw KeyEvent arguments.
    /// </summary>
    public class RawKeyEventArgs : EventArgs
    {
        /// <summary>
        /// The key pressed.
        /// </summary>
        public readonly Key Key;

        /// <summary>
        /// Create raw keyevent arguments.
        /// </summary>
        /// <param name="keyCode"></param>
        /// <param name="isSystemKey"></param>
        public RawKeyEventArgs(int keyCode)
        {
            Key = KeyInterop.KeyFromVirtualKey(keyCode);
        }
    }
}