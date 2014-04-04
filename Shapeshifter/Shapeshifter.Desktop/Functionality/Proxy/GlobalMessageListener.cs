using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using Shapeshifter.Desktop.Functionality.Clipboard.Data;
using Shapeshifter.Desktop.Functionality.Clipboard.DataTypes;
using Shapeshifter.Desktop.Functionality.Clipboard.Factories;
using Shapeshifter.Desktop.Functionality.Clipboard.Session;
using Shapeshifter.Desktop.Functionality.Factories;
using Shapeshifter.Desktop.Functionality.Helpers;
using System.Windows.Input;

namespace Shapeshifter.Desktop.Functionality.Proxy
{
    internal class GlobalMessageListener : Window, IDisposable
    {
        #region apis

        private const int WM_CLIPBOARDUPDATE = 0x031D;
        private const int WM_DWMCOLORIZATIONCOLORCHANGED = 0x0320;

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool AddClipboardFormatListener(IntPtr hwnd);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool RemoveClipboardFormatListener(IntPtr hwnd);

        [DllImport("user32.dll")]
        static extern uint GetClipboardSequenceNumber();

        #endregion

        #region Delegates

        public delegate void AeroColorChangedEventHandler(long newColor);

        public delegate void ClipboardDataAddedEventHandler(ClipboardItem clipboardItem);

        public delegate void ClipboardDataRemovedEventHandler(int index);

        public delegate void ClipboardDataSelectedEventHandler(int index);

        public delegate void ClipboardDataSwappedEventHandler(int oldPosition, int newPosition);

        #endregion

        private static GlobalMessageListener _instance;

        private readonly IList<ClipboardItem> _clipboardItems;

        private bool _activatedBefore;

        private int _selectedClipboardItemIndex;
        private int _lastSnapshotActivity;

        private uint _lastSequenceNumber;

        private bool _ignoreClipboardActivity;

        private GlobalMessageListener()
        {
            Title = "Shapeshifter message listener";
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            Width = 0;
            Height = 0;
            ShowInTaskbar = false;

            _clipboardItems = new List<ClipboardItem>();

            Activated += GlobalMessageListener_Activated;
        }

        public void SwapClipboardItemPositions(int oldPosition, int newPosition)
        {

            if (oldPosition < _clipboardItems.Count && oldPosition > -1)
            {

                var oldItem = _clipboardItems[oldPosition];
                _clipboardItems.Remove(oldItem);

                var targetPosition = newPosition;
                if (targetPosition > oldPosition)
                {
                    targetPosition--;
                }

                _clipboardItems.Insert(targetPosition, oldItem);

                if (ClipboardDataSwapped != null)
                {
                    ClipboardDataSwapped(oldPosition, newPosition);
                }

            }

        }

        public int SelectedClipboardItemIndex
        {
            get { lock (this) return _selectedClipboardItemIndex; }
            set
            {
                lock (this)
                {
                    if (!IgnoreClipboardActivity && value < _clipboardItems.Count && value > -1)
                    {
                        IgnoreClipboardActivity = true;

                        _selectedClipboardItemIndex = value;

                        ClipboardItem selectedItem = _clipboardItems[value];

                        ClipboardSnapshot snapshot = selectedItem.Snapshot;
                        snapshot.Inject();

                        if (ClipboardDataSelected != null)
                        {
                            ClipboardDataSelected(_selectedClipboardItemIndex);
                        }

                        IgnoreClipboardActivity = false;
                    }
                }
            }
        }

        public bool IgnoreClipboardActivity
        {
            get { return _ignoreClipboardActivity; }
            set
            {
                _ignoreClipboardActivity = value;
                Trace.WriteLine("Ignore clipboard activity has been set to " + value);
            }
        }

        private bool IsReadyForSnapshotActions
        {
            get
            {
                int currentTick = Environment.TickCount;
                bool result = currentTick - _lastSnapshotActivity > 75;
                if (result)
                {
                    ResetSnapshotReadiness();
                }
                return result;
            }
        }

        public void ResetSnapshotReadiness()
        {
            _lastSnapshotActivity = Environment.TickCount;
        }

        public static GlobalMessageListener Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new GlobalMessageListener();
                    _instance.Show();
                }

                return _instance;
            }
        }

        public IntPtr WindowHandle { get; private set; }

        #region IDisposable Members

        public void Dispose()
        {
            if (WindowHandle != IntPtr.Zero &&
                !RemoveClipboardFormatListener(WindowHandle))
            {
                throw new Exception("Couldn't modify clipboard viewer chain");
            }
        }

        #endregion

        public event ClipboardDataAddedEventHandler ClipboardDataAdded;
        public event ClipboardDataSelectedEventHandler ClipboardDataSelected;
        public event ClipboardDataRemovedEventHandler ClipboardDataRemoved;
        public event ClipboardDataSwappedEventHandler ClipboardDataSwapped;
        public event AeroColorChangedEventHandler AeroColorChanged;

        public void RemoveClipboardData(int index)
        {
            IgnoreClipboardActivity = true;

            if (index > -1 && index < _clipboardItems.Count)
            {
                ClipboardItem item = _clipboardItems[index];
                _clipboardItems.Remove(item);

                if (ClipboardDataRemoved != null)
                {
                    ClipboardDataRemoved(index);
                }

                IgnoreClipboardActivity = false;

                if (_clipboardItems.Count == 0)
                {
                    //no items left, clear the clipboard

                    //let's not set the session owner to anything else than the already used owner at the time.
                    using (var session = new ClipboardSession(WindowHandle))
                    {
                        if (session.IsOpen)
                        {
                            session.ClearClipboard();
                        }
                    }
                }
                else
                {
                    //the thing we just removed - was it selected?
                    if (index == _selectedClipboardItemIndex && _clipboardItems.Count > 0)
                    {
                        SelectedClipboardItemIndex = 0;
                    }
                }
            }
            else
            {
                IgnoreClipboardActivity = false;
            }
        }

        private void GlobalMessageListener_Activated(object sender, EventArgs e)
        {
            if (!_activatedBefore)
            {
                _activatedBefore = true;
                Hide();
            }
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            var helper = new WindowInteropHelper(this);
            WindowHandle = helper.Handle;

            //install clipboard hooks
            var communicationHook = (HwndSource)PresentationSource.FromVisual(this);
            if (communicationHook == null)
            {
                throw new Exception("Could not install the message hook");
            }

            communicationHook.AddHook(OnWindowMessage);

            if (!AddClipboardFormatListener(WindowHandle))
            {
                throw new Exception("Could not install the clipboard hook");
            }
        }

        private IntPtr OnWindowMessage(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            try
            {
                if (msg == WM_CLIPBOARDUPDATE)
                {

                    var sequenceNumber = GetClipboardSequenceNumber();
                    if (IsReadyForSnapshotActions && sequenceNumber != _lastSequenceNumber && !IgnoreClipboardActivity)
                    {
                        _lastSequenceNumber = sequenceNumber;

                        IgnoreClipboardActivity = true;

                        //handle clipboard data
                        ClipboardSnapshot snapshot = ClipboardSnapshot.CreateSnapshot(hwnd);
                        if (snapshot.ContainsData &&
                            hwnd != snapshot.OutsideClipboardOwnerHandle)
                        {
                            Trace.WriteLine("A snapshot with data was discovered from source [" +
                                            snapshot.OutsideClipboardOwnerHandle + "]");

                            var factory = (ClipboardItemFactory) null;
                            IEnumerable<uint> priorities = snapshot.FormatPriorities;
                            foreach (uint format in priorities)
                            {
                                if (format == KnownClipboardFormats.CF_DIB || format == KnownClipboardFormats.CF_DIBV5 ||
                                    format == KnownClipboardFormats.CF_DIF ||
                                    format == KnownClipboardFormats.CF_DSPBITMAP ||
                                    format == KnownClipboardFormats.CF_DSPMETAFILEPICT ||
                                    format == KnownClipboardFormats.CF_DSPENHMETAFILE ||
                                    format == KnownClipboardFormats.CF_ENHMETAFILE)
                                {
                                    Trace.WriteLine("The snapshot is an image snapshot");

                                    //images
                                    factory = new ClipboardImageFactory(snapshot);
                                    break;
                                }
                                else if (format == KnownClipboardFormats.CF_DSPTEXT ||
                                         format == KnownClipboardFormats.CF_UNICODETEXT)
                                {
                                    Trace.WriteLine("The snapshot is a text snapshot");

                                    //text
                                    factory = new ClipboardTextFactory(snapshot);
                                    break;
                                }
                                else if (format == KnownClipboardFormats.CF_HDROP)
                                {
                                    Trace.WriteLine("The snapshot is a file snapshot");

                                    //files
                                    factory = new ClipboardFileFactory(snapshot);
                                    break;
                                }
                            }

                            if (factory == null)
                            {
                                Trace.WriteLine("The snapshot is of no known format (custom data)");

                                factory = new ClipboardCustomDataFactory(snapshot);
                            }

                            ClipboardItem clipboardItem = factory.CreateNewClipboardItem(WindowHandle,
                                                                                         _clipboardItems.Take(100));
                            if (clipboardItem != null)
                            {

                                if (_clipboardItems.Contains(clipboardItem))
                                {
                                    SwapClipboardItemPositions(_clipboardItems.IndexOf(clipboardItem), 0);
                                }
                                else
                                {
                                    _clipboardItems.Insert(0, clipboardItem);
                                    if (ClipboardDataAdded != null)
                                    {
                                        ClipboardDataAdded(clipboardItem);
                                    }

                                    switch (_clipboardItems.Count)
                                    {
                                        case 1:
                                            NotificationHelper.ShowInstructions(
                                                "You have 1 item in your clipboard now. Try copying just one more.");
                                            break;

                                        case 2:
                                            string mode = SettingsHelper.GetSetting("ManagementMode", "Mixed");
                                            if (mode == "Integrated")
                                            {
                                                NotificationHelper.ShowInstructions(
                                                    "Nicely done! Now try holding down CTRL + V (it's important that you hold it down).");
                                            }
                                            else if (mode == "External")
                                            {
                                                NotificationHelper.ShowInstructions(
                                                    "Nicely done! Now note the Shapeshifter windows on your taskbar. They represent the clipboard data.");
                                            }
                                            else if (mode == "Mixed")
                                            {
                                                NotificationHelper.ShowInstructions(
                                                    "Nicely done! Now try holding down CTRL + V (it's important that you hold it down), or clicking the Shapeshifter icon on your taskbar.");
                                            }
                                            break;
                                    }
                                }

                                if (SettingsHelper.GetSetting("UseIncineration", "1") == "1")
                                {
                                    App.Incinerate();
                                }
                            }

                        }

                        IgnoreClipboardActivity = false;

                    }
                }
                else if (msg == WM_DWMCOLORIZATIONCOLORCHANGED)
                {
                    if (AeroColorChanged != null)
                    {
                        AeroColorChanged(wParam.ToInt64());
                    }
                }
            }
            catch (NullReferenceException ex)
            {
                if(Debugger.IsAttached)
                {
                    Debugger.Break();
                }
                //caught due to a bug in .NET under Windows 7 when closing the application from tray icon
            }

            return IntPtr.Zero;
        }
    }
}