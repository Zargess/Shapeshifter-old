using System;
using System.Collections.Generic;
using System.Deployment.Application;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security;
using System.Threading;
using System.Web;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Resources;
using System.Windows.Threading;
using Communicator;
using Microsoft.Win32;
using Shapeshifter.Desktop.Functionality.Clipboard.Managers;
using Shapeshifter.Desktop.Functionality.Helpers;
using Shapeshifter.Desktop.Functionality.Proxy;
using Shapeshifter.Desktop.Windows;
using Application = System.Windows.Application;
using Color = System.Windows.Media.Color;
using MessageBox = System.Windows.MessageBox;
using Shapeshifter.Desktop.Localization;
using System.Globalization;

namespace Shapeshifter
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        #region apis

        [DllImport("dwmapi.dll", PreserveSig = false, SetLastError = true)]
        private static extern void DwmGetColorizationColor(out uint ColorizationColor,
                                                           [MarshalAs(UnmanagedType.Bool)] out bool
                                                               ColorizationOpaqueBlend);

        [DllImport("kernel32.dll", EntryPoint = "SetProcessWorkingSetSize", ExactSpelling = true, CharSet =
CharSet.Ansi, SetLastError = true)]
        private static extern int SetProcessWorkingSetSize(IntPtr process, int minimumWorkingSetSize, int maximumWorkingSetSize);


        #endregion

        private IList<IDisposable> _runners;

        private NotifyIcon _trayIcon;
        private FileSystemWatcher _uninstallWatcher;

        private int _updateCount;

        private static string ClickOnceApplicationDirectoryPath
        {
            get
            {
                if (MajorVersion != -1)
                {
                    return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), "Programs",
                                        "Flamefusion", "Shapeshifter");
                }
                else
                {
                    return Path.GetDirectoryName(ClickOnceApplicationExecutablePath);
                }
            }
        }

        private static string ClickOnceApplicationExecutablePath
        {
            get
            {
                if (MajorVersion != -1)
                {
                    return Path.Combine(ClickOnceApplicationDirectoryPath, "Shapeshifter.appref-ms");
                }
                else
                {
                    return Assembly.GetCallingAssembly().Location;
                }
            }
        }

        public static int MajorVersion
        {
            get
            {
                if (ApplicationDeployment.IsNetworkDeployed)
                {
                    try
                    {
                        ApplicationDeployment currentDeployment = ApplicationDeployment.CurrentDeployment;
                        if (currentDeployment != null)
                        {
                            return currentDeployment.CurrentVersion.Major;
                        }
                    }
                    catch (InvalidDeploymentException)
                    {
                    }
                }
                return -1;
            }
        }

        private void CreateTrayIcon()
        {
            StreamResourceInfo stream = GetResourceStream(new Uri("/Shapeshifter;component/Icon.ico", UriKind.Relative));
            if (stream != null)
            {
                Stream iconStream = stream.Stream;

                _trayIcon = new NotifyIcon();
                _trayIcon.Icon = new Icon(iconStream);

                //create a simple tray menu with only one item.
                var trayMenu = new ContextMenu();

                var brandUrl = SettingsHelper.GetSetting("BrandUrl");
                var brandName = SettingsHelper.GetSetting("BrandName");
                if (!string.IsNullOrEmpty(brandUrl) && !string.IsNullOrEmpty(brandName))
                {
                    trayMenu.MenuItems.Add(brandName, (sender, args) => StartWebsite(brandUrl));
                    trayMenu.MenuItems.Add("-");
                }

                trayMenu.MenuItems.Add(Language.TrayIconContextMenuSettings, OpenSettings).DefaultItem = true;
                trayMenu.MenuItems.Add("-");
                trayMenu.MenuItems.Add(Language.TrayIconContextMenuFeedback, SendFeedback);
                trayMenu.MenuItems.Add(Language.TrayIconContextMenuTranslate, HelpTranslate);
                trayMenu.MenuItems.Add("-");
                trayMenu.MenuItems.Add(Language.TrayIconContextMenuFlamefusionFacebookPage, FlamefusionFacebookPage);
                trayMenu.MenuItems.Add(Language.TrayIconContextMenuShapeshifterFacebookPage, ShapeshifterFacebookPage);
                trayMenu.MenuItems.Add("-");
                trayMenu.MenuItems.Add(Language.TrayIconContextMenuExit, PerformExit);

                if (MajorVersion == -1)
                {
                    trayMenu.MenuItems.Add("-");
                    trayMenu.MenuItems.Add("Uninstall this development build", PerformUninstall);
                }

                //add menu to tray icon and show it.
                _trayIcon.ContextMenu = trayMenu;
                _trayIcon.Visible = true;

                _trayIcon.MouseDoubleClick += OpenSettings;
                _trayIcon.BalloonTipClicked += trayIcon_BalloonTipClicked;
                _trayIcon.BalloonTipClosed += trayIcon_BalloonTipClosed;
            }
        }

        private void PerformUninstall(object sender, EventArgs e)
        {
            MessageBox.Show(
                "Shapeshifter will now self-destruct this development build and remove all files used by Shapeshifter on this machine.\n\nOnce done, you can remove the \"Shapeshifter.exe\" file at \"" +
                ClickOnceApplicationDirectoryPath + "\".", "Shapeshifter will now remove itself", MessageBoxButton.OK, MessageBoxImage.Information);
            CheckForUninstallation(true);
        }

        private void StartWebsite(string url)
        {
            var startInfo = new ProcessStartInfo("explorer.exe", "\"" + url + "\"");
            Process.Start(startInfo);
        }

        private void HelpTranslate(object sender, EventArgs e)
        {
            MessageBox.Show(
                Language.MessageBoxCrowdinContent, Language.MessageBoxCrowdinTitle, MessageBoxButton.OK, MessageBoxImage.Information);

            StartWebsite("http://crowdin.net/project/flamefusion");
        }

        private void FlamefusionFacebookPage(object sender, EventArgs e)
        {
            StartWebsite("http://facebook.com/Flamefusion");
        }

        private void ShapeshifterFacebookPage(object sender, EventArgs e)
        {
            StartWebsite("http://facebook.com/FlamefusionShapeshifter");
        }

        private void SendFeedback(object sender, EventArgs e)
        {
            StartWebsite("http://flamefusion.net/Contact?subject=Shapeshifter");
        }

        private void trayIcon_BalloonTipClosed(object sender, EventArgs e)
        {
            _trayIcon.Tag = null;
        }

        private void trayIcon_BalloonTipClicked(object sender, EventArgs e)
        {
            if ((string)_trayIcon.Tag == "Update")
            {
                _trayIcon.Tag = null;

                Process.Start(ClickOnceApplicationExecutablePath);
            }
        }

        private void OpenSettings(object sender, EventArgs e)
        {
            var settingsWindow = new Settings();
            settingsWindow.Show();
        }

        private void PerformExit()
        {
            try
            {
                if (_runners != null)
                {
                    foreach (IDisposable runner in _runners)
                    {
                        runner.Dispose();
                    }
                    _runners.Clear();
                    _runners = null;
                }

                if (_uninstallWatcher != null)
                {
                    _uninstallWatcher.Deleted -= _uninstallWatcher_Deleted;
                    _uninstallWatcher.Dispose();
                    _uninstallWatcher = null;
                }

                if (_trayIcon != null)
                {
                    _trayIcon.Visible = false;
                    _trayIcon.MouseDoubleClick -= OpenSettings;
                    _trayIcon.BalloonTipClicked -= trayIcon_BalloonTipClicked;
                    _trayIcon.BalloonTipClosed -= trayIcon_BalloonTipClosed;
                    _trayIcon.Dispose();
                    _trayIcon = null;
                }
            }
            catch (Exception)
            {
                if (Debugger.IsAttached)
                {
                    throw;
                }
            }
            finally
            {
                Process.GetCurrentProcess().Kill();
            }
        }

        private void PerformExit(object sender, EventArgs e)
        {
            CheckForUninstallation();
            PerformExit();
        }

        private void CheckForUninstallation(bool forceUninstall = false)
        {
            if (MajorVersion != -1 && _uninstallWatcher == null && Directory.Exists(ClickOnceApplicationDirectoryPath))
            {
                _uninstallWatcher = new FileSystemWatcher();
                _uninstallWatcher.Deleted += _uninstallWatcher_Deleted;
                _uninstallWatcher.Path = ClickOnceApplicationDirectoryPath;
                _uninstallWatcher.IncludeSubdirectories = true;
                _uninstallWatcher.EnableRaisingEvents = true;
            }

            try
            {

                if (forceUninstall || !File.Exists(ClickOnceApplicationExecutablePath))
                {
                    try
                    {
                        if (_trayIcon != null)
                        {
                            _trayIcon.Visible = false;
                        }

                        //perform self-destruct
                        SetStartupPath(false);

                        SettingsHelper.ResetDefaults();

                        string flamefusionDirectory =
                            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                                         "Flamefusion");
                        string softwareDirectory = Path.Combine(flamefusionDirectory, "Shapeshifter");
                        if (Directory.Exists(softwareDirectory))
                        {
                            if (Directory.GetDirectories(flamefusionDirectory).Length == 1)
                            {
                                Directory.Delete(flamefusionDirectory, true);
                            }
                            else
                            {
                                Directory.Delete(softwareDirectory, true);
                            }
                        }
                    }
                    finally
                    {
                        WebCommunicator.RegisterUninstall(Assembly.GetExecutingAssembly());

                        MessageBox.Show(
                            Language.MessageBoxShapeshifterUninstalledContent,
                            Language.MessageBoxShapeshifterUninstalledTitle, MessageBoxButton.OK, MessageBoxImage.Information);

                        PerformExit();
                    }
                }
            }
            catch (SecurityException)
            {
            }
        }

        void _uninstallWatcher_Deleted(object sender, FileSystemEventArgs e)
        {
            CheckForUninstallation();
        }

        private void UpdateTrayIconText(string text)
        {
            if (!string.IsNullOrEmpty(text) && text.Length < 64 && _trayIcon != null)
            {
                Dispatcher.Invoke(new Action(delegate { _trayIcon.Text = text; }));
            }
            else
            {
                LogHelper.Log(typeof(App),
                              "Can't set text \"" + text +
                              "\" because the length is too long, or the tray icon has not been initialized yet");
            }
        }

        private void App_Startup(object sender, StartupEventArgs e)
        {
            DispatcherUnhandledException += App_DispatcherUnhandledException;

            try
            {

                if (MajorVersion != -1)
                {

                    try
                    {
                        if (ApplicationDeployment.CurrentDeployment.ActivationUri != null && !string.IsNullOrEmpty(ApplicationDeployment.CurrentDeployment.ActivationUri.Query))
                        {

                            string queryString = ApplicationDeployment.CurrentDeployment.ActivationUri.Query;
                            var parameters = HttpUtility.ParseQueryString(queryString);

                            foreach (var key in parameters.AllKeys)
                            {
                                var value = parameters[key];
                                switch (key)
                                {

                                    case "BrandLogoUrl":
                                        SettingsHelper.SaveSetting("BrandLogoUrl", value);
                                        break;

                                    case "BrandName":
                                        SettingsHelper.SaveSetting("BrandName", value);
                                        break;

                                    case "BrandUrl":
                                        SettingsHelper.SaveSetting("BrandUrl", value);
                                        break;
                                }
                            }

                        }
                        else
                        {
                            LogHelper.Log(typeof(App), "Not invoked with any commandline arguments");
                        }

                    }
                    catch
                    {

                    }
                }
                else
                {
                    LogHelper.Log(typeof(App), "Running in developer build mode");
                }

                CheckForPreviousInstances();
                CheckForUninstallation();
                ClearLogs();
                SetHookTimeout();
                CalibrateColors();

                bool isFirstInstall = SettingsHelper.GetSetting("InstalledBefore", "0") == "0";
                if (isFirstInstall)
                {
                    SettingsHelper.SaveSetting("InstalledBefore", "1");

                    WebCommunicator.RegisterInstallAsync(Assembly.GetExecutingAssembly());
                }

                bool shouldResetSettings = SettingsHelper.GetSetting("AlwaysReset", "0") == "1";
                if (shouldResetSettings)
                {
                    SettingsHelper.ResetDefaults();
                }

                CultureInfo language = CultureInfo.CurrentCulture;
                var chosenLanguage = SettingsHelper.GetSetting("ChosenLanguage", null);
                if (chosenLanguage != null)
                {
                    language = new CultureInfo(int.Parse(chosenLanguage));
                }
                Language.Culture = language;

                //keep teasing people with the setup window until we're sure that we're set up.
                if (MajorVersion != -1 && SettingsHelper.GetSetting("LastSetupVersion", "0") != MajorVersion + "")
                {
                    var setupWindow = new Setup();
                    setupWindow.ShowDialog();
                }

                CreateTrayIcon();

                GlobalMessageListener messageHook = GlobalMessageListener.Instance;
                messageHook.AeroColorChanged += MessageHookAeroColorChanged;

                var runners = new List<IDisposable>();

                string mode = SettingsHelper.GetSetting("ManagementMode", "Mixed");
                if (mode == "Mixed" || mode == "Integrated")
                {
                    var integrated = new IntegratedClipboardManager();
                    runners.Add(integrated);

                    integrated.Show();
                }

                if (mode == "Mixed" || mode == "External")
                {
                    runners.Add(new ExternalClipboardManager());
                }

                if (!runners.Any() || runners.Any(w => w == null))
                {
                    throw new InvalidOperationException("An integration method which was not compatible is used: \"" +
                                                        mode + "\"");
                }

                _runners = runners;

                if (!NotificationHelper.ShowInstructions("Shapeshifter is now running. Try copying some stuff."))
                {

                    //get remaining translated phrases for this language
                    ShowRemainingTranslationCount();

                }

                //report that Shapeshifter has been launched today, if needed
                var lastRunDate = SettingsHelper.GetSetting("LastRunDate");
                var todayRunDate = DateTime.UtcNow.Year + "-" + DateTime.UtcNow.Month + "-" + DateTime.UtcNow.Day;
                if (lastRunDate != todayRunDate)
                {
                    SettingsHelper.SaveSetting("LastRunDate", todayRunDate);
                    WebCommunicator.RegisterRunAsync(Assembly.GetExecutingAssembly());
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show("Shapeshifter encountered a startup issue. Details below.\n\n" + ex, "Woops!", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {

                if (ApplicationDeployment.IsNetworkDeployed)
                {
                    try
                    {
                        ApplicationDeployment currentDeployment = ApplicationDeployment.CurrentDeployment;

                        UpdateTrayIconText("Shapeshifter " + currentDeployment.CurrentVersion +
                                           "\nLast update check: " + DateTime.Now.ToString("hh:mm tt", new CultureInfo("en-US")));

                        var updateThread = new Thread(delegate()
                                                          {
                                                              while (true)
                                                              {
                                                                  _updateCount++;

                                                                  try
                                                                  {
                                                                      UpdateCheckInfo updateInformation =
                                                                          currentDeployment.CheckForDetailedUpdate();
                                                                      if (updateInformation.UpdateAvailable)
                                                                      {
                                                                          currentDeployment.Update();

                                                                          if (_updateCount == 1)
                                                                          {
                                                                              Process.Start(ClickOnceApplicationExecutablePath, "Update");
                                                                          }
                                                                          else
                                                                          {
                                                                              if (_trayIcon != null)
                                                                              {
                                                                                  _trayIcon.Tag = "Update";
                                                                                  ShowBalloonTip(300000,
                                                                                                 Language.BalloonTipPendingUpdateTitle,
                                                                                                 string.Format(Language.BalloonTipPendingUpdateContent, updateInformation.AvailableVersion), ToolTipIcon.Info);
                                                                              }
                                                                          }
                                                                      }
                                                                  }
                                                                  catch (InvalidOperationException ex)
                                                                  {
                                                                      if (
                                                                          MessageBox.Show(
                                                                              "There was an update for Shapeshifter available, but it couldn't be downloaded automatically. Do you want to open the update location manually?\n\nDetails for techies follow.\n\n" +
                                                                              ex, "Woops!", MessageBoxButton.YesNo,
                                                                              MessageBoxImage.Warning) ==
                                                                          MessageBoxResult.Yes)
                                                                      {
                                                                          Process.Start(
                                                                              currentDeployment.UpdateLocation.ToString());
                                                                      }
                                                                      return;
                                                                  }
                                                                  catch (DeploymentDownloadException)
                                                                  {
                                                                  }
                                                                  catch (InvalidDeploymentException)
                                                                  {
                                                                  }

                                                                  string trayText = "Shapeshifter " + currentDeployment.CurrentVersion +
                                                                                     "\nLast update check: " + DateTime.Now.ToString("hh:mm tt", new CultureInfo("en-US"));
                                                                  UpdateTrayIconText(trayText);

                                                                  Thread.Sleep(60 * 1000 * 60 * 3); //5 minutes
                                                              }
                                                          }) { IsBackground = true, Priority = ThreadPriority.Lowest };

                        try
                        {
                            updateThread.Start();
                        }
                        catch (ThreadStateException)
                        {
                        }
                        catch (OutOfMemoryException)
                        {
                        }
                    }
                    catch (InvalidDeploymentException)
                    {
                    }

                    SetStartupPath(SettingsHelper.GetSetting("StartWithWindows", "1") == "1");
                }
                else
                {
                    if (_trayIcon != null)
                    {
                        _trayIcon.Text = "Shapeshifter developer build\nUpdate functionality disabled";
                    }
                }
            }
        }

        private void ShowRemainingTranslationCount()
        {
            var count = WebCommunicator.GetRemainingTranslationCount();
            if (count > 0)
            {
                ShowBalloonTip(30000, Language.BalloonTipTranslationHelpNeededTitle, string.Format(Language.BalloonTipTranslationHelpNeededContent, count, Language.Culture.EnglishName, Language.TrayIconContextMenuTranslate), ToolTipIcon.Info);
            }
        }

        public void ShowBalloonTip(int timeout, string title, string content, ToolTipIcon icon)
        {
            if (_trayIcon != null)
            {
                _trayIcon.ShowBalloonTip(timeout, title,
                                         content,
                                         icon);
            }
        }

        private void MessageHookAeroColorChanged(long newColor)
        {
            if (string.IsNullOrEmpty(SettingsHelper.GetSetting("AeroColor")))
            {
                Resources["AeroColor"] = GetAeroColor((uint)newColor);
            }
        }

        private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            e.Handled = true;
            WebCommunicator.ReportBug(Assembly.GetExecutingAssembly(), e.Exception);
            SignalError();
        }

        private void ClearLogs()
        {
            LogHelper.ResetDefaults();
        }

        private void SetHookTimeout()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey("Control Panel\\Desktop", true))
                {
                    if (key != null) key.SetValue("LowLevelHooksTimeout", 5000);
                }
            }
            catch (UnauthorizedAccessException)
            {
            }
            catch (SecurityException)
            {
            }
        }

        public static void SetStartupPath(bool startWithWindows)
        {
            try
            {
                using (
                    RegistryKey key =
                        Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true))
                {
                    if (key != null)
                    {
                        if (startWithWindows)
                        {
                            key.SetValue("Shapeshifter", "\"" + ClickOnceApplicationExecutablePath + "\"");
                        }
                        else
                        {
                            key.DeleteValue("Shapeshifter", false);
                        }
                    }
                }
            }
            catch (SecurityException)
            {

            }
            catch (UnauthorizedAccessException)
            {

            }
        }

        private void CheckForPreviousInstances()
        {
            var myLocation = Assembly.GetExecutingAssembly().Location;

            using (Process shapeshifterProcess = Process.GetCurrentProcess())
            {
                foreach (Process process in Process.GetProcessesByName(shapeshifterProcess.ProcessName))
                {
                    using (process)
                    {
                        if (process.Id != shapeshifterProcess.Id)
                        {
                            process.Kill();
                        }
                    }
                }
            }

            var directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                                         "Flamefusion", "Shapeshifter", "Awareness");
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var file = Path.Combine(directory, "PreviousInstances.dat");

            IList<string> lines;

            if (File.Exists(file))
            {

                lines = File.ReadLines(file).ToList();
                foreach (var line in lines.ToList())
                {
                    if (line != null)
                    {
                        var remove = line == myLocation;
                        try
                        {
                            if (File.Exists(line))
                            {
                                File.Delete(line);
                                remove = true;
                            }
                        }
                        catch (ArgumentException)
                        {

                        }
                        catch (DirectoryNotFoundException)
                        {

                        }
                        catch (IOException)
                        {

                        }
                        catch (NotSupportedException)
                        {

                        }
                        catch (UnauthorizedAccessException)
                        {

                        }

                        if (remove)
                        {
                            while (lines.Contains(line))
                            {
                                lines.Remove(line);
                            }
                        }
                    }
                }

            }
            else
            {

                lines = new List<string>();

            }

            if (!lines.Contains(myLocation))
            {
                lines.Add(myLocation);
            }

            File.WriteAllLines(file, lines);
        }

        public static Color GetAeroColor(uint color = 0)
        {
            try
            {
                if (color == 0)
                {
                    bool alphaBlend;
                    uint newColor;
                    DwmGetColorizationColor(out newColor, out alphaBlend);

                    color = newColor;
                }

                Color convertedColor = Color.FromArgb((byte)((color >> 24) & 0xFF),
                                                      (byte)((color >> 16) & 0xFF),
                                                      (byte)((color >> 8) & 0xFF),
                                                      (byte)(color & 0xFF));

                const byte threshold = 255;

                var colorChannels = new[] { convertedColor.R, convertedColor.G, convertedColor.B };

                convertedColor.A = 255;

                if ((convertedColor.R < 30 && convertedColor.B < 30 && convertedColor.G < 30) ||
                    (convertedColor.R > 225 && convertedColor.B > 225 && convertedColor.G > 225))
                {
                    //set it to default Windows 8 color theme
                    convertedColor.R = 37;
                    convertedColor.G = 97;
                    convertedColor.B = 163;
                }
                else
                {
                    for (int i = 0; i < colorChannels.Length; i++)
                    {
                        var difference = (double)(threshold - colorChannels[i]);
                        if (difference > 0)
                        {
                            var add = (byte)(difference / 5.0);
                            colorChannels[i] = Math.Min((byte)(colorChannels[i] + add), (byte)255);
                        }
                    }

                    convertedColor.R = colorChannels[0];
                    convertedColor.G = colorChannels[1];
                    convertedColor.B = colorChannels[2];
                }

                return convertedColor;
            }
            catch (Exception ex)
            {
                if (ex is DllNotFoundException || ex is EntryPointNotFoundException || ex is FileNotFoundException)
                {
                    //try the XP method instead - glass is not supported.
                    return new Color { A = 255, R = 37, G = 97, B = 163 };
                }
                else
                {
                    throw;
                }
            }
        }

        private void CalibrateColors()
        {
            string aeroColor = SettingsHelper.GetSetting("AeroColor");
            if (string.IsNullOrEmpty(aeroColor))
            {
                Resources["AeroColor"] = GetAeroColor();
            }
            else
            {
                var color = new Color();
                string[] splitCollection = aeroColor.Split(',');

                color.A = 255;

                color.R = byte.Parse(splitCollection[0]);
                color.G = byte.Parse(splitCollection[1]);
                color.B = byte.Parse(splitCollection[2]);

                Resources["AeroColor"] = color;
            }
        }

        public static void Incinerate()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            using (var process = Process.GetCurrentProcess())
            {
                SetProcessWorkingSetSize(process.Handle, -1, -1);
            }
        }

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            PerformExit();
        }

        public void SignalError()
        {
            ShowBalloonTip(10000, "An error occured", "A Shapeshifter error occured. This has been silently and anonymously reported to Flamefusion for analysis. Expect a fix within a week.", ToolTipIcon.Error);
        }

        public void SignalClipboardDisturbance(string owner = "?")
        {
            ShowBalloonTip(30000, Language.BalloonTipClipboardDisturbanceTitle, string.Format(Language.BalloonTipClipboardDisturbanceContent, owner), ToolTipIcon.Warning);
        }
    }
}