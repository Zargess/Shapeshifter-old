using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using Shapeshifter.Desktop.Functionality.Clipboard.DataTypes;
using Shapeshifter.Desktop.Functionality.Clipboard.Session;
using Shapeshifter.Desktop.Functionality.Helpers;
using Shapeshifter.Desktop.Functionality.Keyboard;
using Shapeshifter.Desktop.Functionality.Proxy;
using FormsCursor = System.Windows.Forms.Cursor;
using FormsScreen = System.Windows.Forms.Screen;

namespace Shapeshifter.Desktop.Windows
{
    /// <summary>
    /// Interaction logic for IntegratedClipboardManager.xaml
    /// </summary>
    public partial class IntegratedClipboardManager : Window, IDisposable
    {

        #region apis

        private const int WS_EX_NOACTIVATE = 0x08000000;
        private const int GWL_EXSTYLE = -20;

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hwnd, int index);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);

        #endregion

        private Thread _checkWindowShowThresholdThread;

        private KeyboardListener _keyboardHook;
        private GlobalMessageListener _messageHook;

        private int _lastInjectionActivity;

        private bool _isRightControlDown;
        private bool _isLeftControlDown;
        private bool _isVDown;

        private bool _wasRightControlDownOnKeyUp;
        private bool _wasLeftControlDownOnKeyUp;
        private bool _wasVDownOnKeyUp;

        private bool _wasRightControlDownOnKeyDown;
        private bool _wasLeftControlDownOnKeyDown;
        private bool _wasVDownOnKeyDown;

        private bool _handled;

        private bool _hasShownBefore;

        private IntPtr _windowHandle;

        private bool IsReadyForInjectionActions
        {
            get
            {
                var currentTick = Environment.TickCount;
                var result = currentTick - _lastInjectionActivity > 25;
                if (result)
                {
                    _lastInjectionActivity = currentTick;
                }
                return result;
            }
        }

        public IntegratedClipboardManager()
        {
            InitializeComponent();
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            base.OnMouseWheel(e);
            if (e.Delta > 0)
            {
                SelectPreviousClipboardItem();
            }
            else
            {
                SelectNextClipboardItem();
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
            base.OnMouseMove(e);
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            var helper = new WindowInteropHelper(this);
            _windowHandle = helper.Handle;

            //install clipboard hooks
            _messageHook = GlobalMessageListener.Instance;
            _messageHook.ClipboardDataAdded += MessageHookClipboardDataAdded;
            _messageHook.ClipboardDataRemoved += MessageHookClipboardDataRemoved;
            _messageHook.ClipboardDataSelected += MessageHookClipboardDataSelected;
            _messageHook.ClipboardDataSwapped += MessageHookClipboardDataSwapped;

            //install keyboard hooks
            _keyboardHook = new KeyboardListener();
            _keyboardHook.KeyDown += KeyboardHookKeyDown;
            _keyboardHook.KeyUp += KeyboardHookKeyUp;

            //not activatable
            //int exStyle = GetWindowLong(_windowHandle, GWL_EXSTYLE);
            //SetWindowLong(_windowHandle, GWL_EXSTYLE, exStyle | WS_EX_NOACTIVATE);

            RepositionWindow();

            Show();
            Activate();
            Focus();
        }

        private void RepositionWindow()
        {
            Top = 0;

            var cursorPosition = FormsCursor.Position;
            foreach (var screen in FormsScreen.AllScreens)
            {
                if (cursorPosition.X >= screen.Bounds.Left && cursorPosition.X <= screen.Bounds.Left + screen.Bounds.Width &&
                    cursorPosition.Y >= screen.Bounds.Top && cursorPosition.Y <= screen.Bounds.Top + screen.Bounds.Height)
                {
                    Left = screen.Bounds.Left + screen.Bounds.Width - Width;
                    Top = screen.Bounds.Top;
                    MaxHeight = screen.Bounds.Height;
                    break;
                }
            }
        }

        void MessageHookClipboardDataSwapped(int oldPosition, int newPosition)
        {

            var oldItem = (ListBoxItem)lstClipboardContents.Items[oldPosition];
            lstClipboardContents.Items.Remove(oldItem);

            var targetPosition = newPosition;
            if (targetPosition > oldPosition)
            {
                targetPosition--;
            }

            oldItem.IsSelected = true;

            lstClipboardContents.Items.Insert(targetPosition, oldItem);

        }

        void MessageHookClipboardDataSelected(int index)
        {

            var listBoxItem = (ListBoxItem)lstClipboardContents.Items[index];
            listBoxItem.IsSelected = true;

            lstClipboardContents.ScrollIntoView(listBoxItem);

        }

        void MessageHookClipboardDataRemoved(int index)
        {
            lstClipboardContents.Items.RemoveAt(index);
        }

        private void MessageHookClipboardDataAdded(ClipboardItem clipboardData)
        {
            ListBoxItem listBoxItem = clipboardData.BindToListBoxItem();

            listBoxItem.Tag = clipboardData.Snapshot;
            listBoxItem.IsSelected = true;

            lstClipboardContents.Items.Insert(0, listBoxItem);
        }

        private void KeyboardHookKeyUp(object sender, RawKeyEventArgs args, ref bool handled)
        {

            _wasVDownOnKeyUp = _isVDown;
            _wasRightControlDownOnKeyUp = _isRightControlDown;
            _wasLeftControlDownOnKeyUp = _isLeftControlDown;

            if (args.Key == Key.V)
            {
                _isVDown = false;
            }
            else if (args.Key == Key.LeftCtrl)
            {
                _isLeftControlDown = false;
            }
            else if (args.Key == Key.RightCtrl)
            {
                _isRightControlDown = false;
            }

            if (!_messageHook.IgnoreClipboardActivity)
            {

                var previousHandled = (_wasLeftControlDownOnKeyUp || _wasRightControlDownOnKeyUp) && _wasVDownOnKeyUp; ;
                CheckHook();
                handled = _handled;

                if (handled && args.Key == Key.Escape)
                {

                    KillActiveWindowShowThresholdThreads();

                    if (IsVisible)
                    {

                        Hide();
                    }

                    using (var session = new ClipboardSession(_messageHook.WindowHandle))
                    {
                        if (session.IsOpen)
                        {
                            session.ClearClipboard();
                        }
                    }

                }

                if (!handled && previousHandled)
                {
                    //we need to paste - there was some kind of release.

                    _messageHook.IgnoreClipboardActivity = true;

                    KillActiveWindowShowThresholdThreads();

                    if (IsVisible)
                    {
                        Hide();

                        _messageHook.SwapClipboardItemPositions(lstClipboardContents.SelectedIndex, 0);
                    }

                    var sendPasteThread = new Thread(delegate()
                                                         {

                                                             Thread.Sleep(50);

                                                             //paste data out
                                                             SendPasteCombination();

                                                             _wasLeftControlDownOnKeyDown
                                                                 = false;
                                                             _wasLeftControlDownOnKeyUp
                                                                 = false;
                                                             _wasRightControlDownOnKeyDown
                                                                 = false;
                                                             _wasRightControlDownOnKeyUp
                                                                 = false;
                                                             _wasVDownOnKeyDown = false;
                                                             _wasVDownOnKeyUp = false;

                                                             _isVDown = false;

                                                             var returnIgnoreThread = new Thread(delegate()
                                                             {

                                                                 Thread.Sleep(50);
                                                                 _messageHook.IgnoreClipboardActivity = false;

                                                             });
                                                             returnIgnoreThread.IsBackground = true;
                                                             returnIgnoreThread.Start();

                                                         });
                    sendPasteThread.IsBackground = true;
                    sendPasteThread.Start();

                }

            }

        }

        private void CheckHook()
        {
            _handled = (_isLeftControlDown || _isRightControlDown) && _isVDown;
        }

        private void SendPasteCombination()
        {

            _messageHook.ResetSnapshotReadiness();

            LogHelper.Log(typeof(IntegratedClipboardManager), "Sending CTRL + V keystroke to foreground window");

            bool wasLeftControlDown = _isLeftControlDown;
            bool wasRightControlDown = _isRightControlDown;

            if (_isLeftControlDown && _isRightControlDown)
            {
                Trace.WriteLine("Both CTRL keys are down");

                KeyboardSimulationHelper.SendKey(0xA3, false); // release right ctrl
            }
            if (!_isLeftControlDown && !_isRightControlDown)
            {
                Trace.WriteLine("No CTRL keys are down");

                KeyboardSimulationHelper.SendKey(0xA2, true); // press left ctrl
            }

            if (!_isVDown)
            {
                Trace.WriteLine("V is not down");

                KeyboardSimulationHelper.SendKey(0x56, true); // press v
                KeyboardSimulationHelper.SendKey(0x56, false); // release v
            }

            KeyboardSimulationHelper.SendKey(0xA3, false); // release right ctrl
            KeyboardSimulationHelper.SendKey(0xA2, false); // release left ctrl

            KeyboardSimulationHelper.SendKey(0xA1, false); // release right shift
            KeyboardSimulationHelper.SendKey(0xA0, false); // release left shift

            if (wasLeftControlDown)
            {
                KeyboardSimulationHelper.SendKey(0xA2, true); // press left ctrl
            }
            else if (wasRightControlDown)
            {
                KeyboardSimulationHelper.SendKey(0xA3, true); // release right ctrl
            }

        }

        private void SelectNextClipboardItem()
        {
            if (IsReadyForInjectionActions && lstClipboardContents.Items.Count > 0)
            {
                int targetIndex = lstClipboardContents.SelectedIndex + 1;
                if (targetIndex == lstClipboardContents.Items.Count)
                {
                    targetIndex = 0;
                }
                _messageHook.SelectedClipboardItemIndex = targetIndex;
            }
        }

        private void SelectPreviousClipboardItem()
        {
            if (IsReadyForInjectionActions && lstClipboardContents.Items.Count > 0)
            {
                int targetIndex = lstClipboardContents.SelectedIndex - 1;
                if (targetIndex == -1)
                {
                    targetIndex = lstClipboardContents.Items.Count - 1;
                }
                _messageHook.SelectedClipboardItemIndex = targetIndex;
            }
        }

        private void KeyboardHookKeyDown(object sender, RawKeyEventArgs args, ref bool handled)
        {

            _wasVDownOnKeyDown = _isVDown;
            _wasRightControlDownOnKeyDown = _isRightControlDown;
            _wasLeftControlDownOnKeyDown = _isLeftControlDown;

            if (args.Key == Key.V)
            {
                _isVDown = true;
            }
            else if (args.Key == Key.LeftCtrl)
            {
                _isLeftControlDown = true;
            }
            else if (args.Key == Key.RightCtrl)
            {
                _isRightControlDown = true;
            }

            if (!_messageHook.IgnoreClipboardActivity)
            {

                var previousHandled = (_wasLeftControlDownOnKeyDown || _wasRightControlDownOnKeyDown) && _wasVDownOnKeyDown; ;
                CheckHook();
                handled = _handled;

                if (_handled)
                {

                    if (args.Key == Key.PrintScreen)
                    {

                        var path = ScreenshotHelper.SaveScreenshot(0, 0, (int)SystemParameters.PrimaryScreenWidth,
                                                                   (int)SystemParameters.PrimaryScreenHeight);
                        Process.Start(path);
                    }

                    if (IsVisible)
                    {

                        if (args.Key == Key.Down)
                        {

                            SelectNextClipboardItem();

                        }
                        else if (args.Key == Key.Up)
                        {

                            SelectPreviousClipboardItem();

                        }

                        if (args.Key == Key.Delete)
                        {

                            //delete an entry - delete was pressed while holding down CTRL + V
                            _messageHook.RemoveClipboardData(_messageHook.SelectedClipboardItemIndex);

                        }

                    }
                    else if (!previousHandled)
                    {

                        KillActiveWindowShowThresholdThreads();

                        //start thread to monitor if it is time to show the GUI
                        _checkWindowShowThresholdThread = new Thread(delegate()
                                                                         {
                                                                             Thread.Sleep(150);

                                                                             if (_handled)
                                                                             {

                                                                                 _lastInjectionActivity =
                                                                                     Environment.TickCount;

                                                                                 LogHelper.Log(
                                                                                     typeof(IntegratedClipboardManager),
                                                                                     "Showing the user interface");

                                                                                 Dispatcher.Invoke(
                                                                                     new Action(delegate()
                                                                                                    {

                                                                                                        RepositionWindow
                                                                                                            ();
                                                                                                        Show();
                                                                                                        Activate();
                                                                                                        Focus();

                                                                                                        NotificationHelper.ShowInstructions
                                                                                                            ("The Shapeshifter integrated mode interface is now visible. Use the Up and Down keys to choose an item, and release CTRL + V to paste. Hit the Delete key to delete an item.");
                                                                                                    }
                                                                                 ));

                                                                             }

                                                                         });
                        _checkWindowShowThresholdThread.IsBackground = true;
                        _checkWindowShowThresholdThread.Priority = ThreadPriority.Lowest;
                        _checkWindowShowThresholdThread.Start();
                    }

                }

            }
        }

        private void KillActiveWindowShowThresholdThreads()
        {
            if (_checkWindowShowThresholdThread != null)
            {
                if (_checkWindowShowThresholdThread.IsAlive)
                {
                    _checkWindowShowThresholdThread.Abort();
                    _checkWindowShowThresholdThread.Join(); //wait for the thread to abort
                }
                _checkWindowShowThresholdThread = null;
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            //remove clipboard hooks
            if (_messageHook != null)
            {
                _messageHook.ClipboardDataAdded -= MessageHookClipboardDataAdded;
                _messageHook.ClipboardDataRemoved -= MessageHookClipboardDataRemoved;
                _messageHook.ClipboardDataSelected -= MessageHookClipboardDataSelected;
                _messageHook.ClipboardDataSwapped -= MessageHookClipboardDataSwapped;
            }

            //remove keyboard hooks
            if (_keyboardHook != null)
            {
                using (_keyboardHook)
                {
                    _keyboardHook.KeyDown -= KeyboardHookKeyDown;
                    _keyboardHook.KeyUp -= KeyboardHookKeyUp;
                }
            }

            base.OnClosing(e);
        }

        public void Dispose()
        {
            Close();
        }

        private void lstClipboardContents_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_messageHook.IgnoreClipboardActivity && lstClipboardContents.SelectedIndex > -1)
            {
                _messageHook.SelectedClipboardItemIndex = lstClipboardContents.SelectedIndex;
            }
        }

        private void Window_Activated(object sender, EventArgs e)
        {
            if (!_hasShownBefore)
            {
                _hasShownBefore = true;
                Hide();
            }
        }
    }
}