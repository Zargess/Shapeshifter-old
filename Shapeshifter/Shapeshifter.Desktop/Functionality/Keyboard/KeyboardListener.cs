using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Threading;

namespace Shapeshifter.Desktop.Functionality.Keyboard
{
    /// <summary>
    /// Allows reading and modification of keyboard messages within Windows.
    /// <remarks>Uses WH_KEYBOARD_LL.</remarks>
    /// </summary>
    public class KeyboardListener : IDisposable
    {
        #region apis

        private const int WH_KEYBOARD_LL = 13;

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, KeyboardHookDelegate lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, UIntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        private enum KeyEvent
        {
            WM_KEYDOWN = 256,
            WM_KEYUP = 257,
            WM_SYSKEYUP = 261,
            WM_SYSKEYDOWN = 260
        }

        #endregion

        private Dispatcher _dispatcher;

        /// <summary>
        /// Asynchronous callback hook.
        /// </summary>
        /// <param name="nCode"></param>
        /// <param name="wParam"></param>
        /// <param name="lParam"></param>
        public delegate IntPtr KeyboardHookDelegate(int nCode, UIntPtr wParam, IntPtr lParam);

        /// <summary>
        /// Event to be invoked asynchronously (BeginInvoke) each time key is pressed.
        /// </summary>
        private readonly KeyboardCallback _hookedKeyboardCallback;

        /// <summary>
        /// Contains the hooked callback in runtime.
        /// </summary>
        private readonly KeyboardHookDelegate _hookedKeyboardHook;

        /// <summary>
        /// Hook ID
        /// </summary>
        private readonly IntPtr _hookId = IntPtr.Zero;

        /// <summary>
        /// Creates global keyboard listener.
        /// </summary>
        public KeyboardListener()
        {
            // We have to store the HookCallback, so that it is not garbage collected runtime
            _hookedKeyboardHook = LowLevelKeyboardProc;

            // Set the hook
            _hookId = SetHook(_hookedKeyboardHook);

            // Assign the asynchronous callback event
            _hookedKeyboardCallback = KeyboardListener_KeyboardCallbackAsync;
        }

        #region IDisposable Members

        /// <summary>
        /// Disposes the hook.
        /// <remarks>This call is required as it calls the UnhookWindowsHookEx.</remarks>
        /// </summary>
        public void Dispose()
        {
            UnhookWindowsHookEx(_hookId);
        }

        #endregion

        /// <summary>
        /// Destroys global keyboard listener.
        /// </summary>
        ~KeyboardListener()
        {
            Dispose();
        }

        /// <summary>
        /// Fired when any of the keys is pressed down.
        /// </summary>
        public event RawKeyEventHandler KeyDown;

        /// <summary>
        /// Fired when any of the keys is released.
        /// </summary>
        public event RawKeyEventHandler KeyUp;

        private IntPtr SetHook(KeyboardHookDelegate hook)
        {
            using (Process currentProcess = Process.GetCurrentProcess())
            {
                using (ProcessModule curModule = currentProcess.MainModule)
                {
                    _dispatcher = Dispatcher.CurrentDispatcher;
                    return SetWindowsHookEx(WH_KEYBOARD_LL, hook,
                                            GetModuleHandle(curModule.ModuleName), 0);
                }
            }
        }

        /// <summary>
        /// Actual callback hook.
        /// <remarks>Calls asynchronously the asyncCallback.</remarks>
        /// </summary>
        /// <param name="nCode"></param>
        /// <param name="wParam"></param>
        /// <param name="lParam"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private IntPtr LowLevelKeyboardProc(int nCode, UIntPtr wParam, IntPtr lParam)
        {
            bool block = false;

            if (nCode >= 0)
            {
                if (wParam.ToUInt32() == (int)KeyEvent.WM_KEYDOWN ||
                    wParam.ToUInt32() == (int)KeyEvent.WM_KEYUP ||
                    wParam.ToUInt32() == (int)KeyEvent.WM_SYSKEYDOWN ||
                    wParam.ToUInt32() == (int)KeyEvent.WM_SYSKEYUP)
                {
                    _hookedKeyboardCallback((KeyEvent)wParam.ToUInt32(), Marshal.ReadInt32(lParam), ref block);
                }
            }

            if (block)
            {
                return new IntPtr(-1);
            }

            return CallNextHookEx(_hookId, nCode, wParam, lParam);
        }

        /// <summary>
        /// HookCallbackAsync procedure that calls accordingly the KeyDown or KeyUp events.
        /// </summary>
        /// <param name="keyEvent">The type of press performed.</param>
        /// <param name="vkCode">The keycode pressed.</param>
        /// <param name="block">Wether or not the key should be blocked.</param>
        private void KeyboardListener_KeyboardCallbackAsync(KeyEvent keyEvent, int vkCode, ref bool block)
        {
            try
            {
                lock (typeof(KeyboardListener))
                {
                    switch (keyEvent)
                    {
                        // KeyDown events
                        case KeyEvent.WM_KEYDOWN:
                            if (KeyDown != null)
                            {
                                var insideBlock = false;
                                _dispatcher.Invoke(new Action(
                                                       () => KeyDown(this, new RawKeyEventArgs(vkCode),
                                                                     ref insideBlock)));
                                block = insideBlock;
                            }
                            break;

                        // KeyUp events
                        case KeyEvent.WM_KEYUP:
                            if (KeyUp != null)
                            {
                                var insideBlock = false;
                                _dispatcher.Invoke(new Action(
                                                       () => KeyUp(this, new RawKeyEventArgs(vkCode),
                                                                     ref insideBlock)));
                                block = insideBlock;
                            }
                            break;
                    }
                }
            }
            catch (InvalidOperationException)
            {
                //dispatcher is being suspended - ignore.
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Woops! Something strange happened to Shapeshifter's keyboard mechanism. Your problem has been reported and we are looking into the issue. Details for techies below.\n\n" + ex.ToString(),
                    "Nasty!", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #region Nested type: KeyboardCallback

        private delegate void KeyboardCallback(KeyEvent keyEvent, int vkCode, ref bool block);

        #endregion
    }
}