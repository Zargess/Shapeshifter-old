using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Media.Imaging;
using System.IO;
using Shapeshifter.Desktop.Functionality.Helpers;

namespace Shapeshifter.Desktop.Functionality.Clipboard.DataTypes
{
    public class ClipboardSource
    {
        #region apis

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        #endregion

        private ClipboardSource()
        {
        }

        public string ApplicationName { get; set; }

        public BitmapSource Icon { get; set; }

        public static BitmapSource IconFromWindowHandle(IntPtr hwnd, int widthAndHeight = 16)
        {

            Win32Exception exception = null;
            var module = GetProcessModuleFromWindowHandle(hwnd, ref exception);

            if (exception == null)
            {
                return IconFromProcessModule(module, widthAndHeight); //the filepath of the application
            }
            else
            {
                return null;
            }

        }

        private static BitmapSource IconFromProcessModule(ProcessModule module, int widthAndHeight = 16)
        {
            return IconHelper.GetIcon(module.FileName, false, widthAndHeight);
        }

        private static ProcessModule GetProcessModuleFromWindowHandle(IntPtr hwnd, ref Win32Exception exception)
        {
            uint pid;
            GetWindowThreadProcessId(hwnd, out pid); //use this Windows API to transform a window handle into a process ID

            using (Process process = Process.GetProcessById((int)pid))
            {
                try
                {
                    return process.MainModule;
                }
                catch (Win32Exception ex)
                {
                    exception = ex;
                    return null;
                }
            }
        }

        public static string ApplicationNameFromWindowHandle(IntPtr hwnd)
        {

            if (hwnd != IntPtr.Zero)
            {
                Win32Exception exception = null;
                var module = GetProcessModuleFromWindowHandle(hwnd, ref exception);
                if (exception == null)
                {
                    return module.FileVersionInfo.FileDescription; //the name of the application
                }
            }

            return null;

        }

        public static ClipboardSource SourceFromWindowHandle(IntPtr hwnd)
        {

            var result = new ClipboardSource();

            if (hwnd != IntPtr.Zero)
            {

                Win32Exception exception = null;
                var module = GetProcessModuleFromWindowHandle(hwnd, ref exception);

                if (exception == null)
                {
                    result.Icon = IconFromProcessModule(module); //the filepath of the application

                    try
                    {
                        result.ApplicationName = module.FileVersionInfo.FileDescription; //the name of the application
                    } catch(FileNotFoundException)
                    {
                        result.ApplicationName = module.ModuleName;
                    }
                }
                else
                {

                    if (exception.NativeErrorCode == -2147467259)
                    {
                        //unable to enumerate the process modules
                        result.ApplicationName = "Unknown source";
                    }
                    else if (exception.NativeErrorCode == 5)
                    {
                        //access denied
                        result.ApplicationName = "Unknown source";
                    }
                    else
                    {
                        throw new Win32Exception("An error occured while retrieving the source of the clipboard data [" + exception.NativeErrorCode + "]", exception);
                    }
                }

            } else
            {
                
                //the handle is 0, assume that the data comes from Windows itself.
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri("pack://application:,,,/Shapeshifter;component/Images/WindowsIcon.png"); 
                bitmap.EndInit(); 

                result.ApplicationName = "Windows";
                result.Icon = bitmap;

            }

            return result;
        }
    }
}