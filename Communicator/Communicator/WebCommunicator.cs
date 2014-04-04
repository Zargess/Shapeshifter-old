using System;
using System.Collections.Generic;
using System.Deployment.Application;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace Communicator
{
    public static class WebCommunicator
    {

        private static void RunAsync(ThreadStart @delegate)
        {
            var thread = new Thread(@delegate);
            thread.Priority = ThreadPriority.Lowest;
            thread.IsBackground = true;
            thread.Start();
        }

        public static void ReportBug(Assembly assembly, Exception ex)
        {
            if (!Debugger.IsAttached)
            {
                if (ApplicationDeployment.IsNetworkDeployed)
                {
                    try
                    {
                        ApplicationDeployment currentDeployment = ApplicationDeployment.CurrentDeployment;
                        if (currentDeployment != null)
                        {
                            RunAsync(delegate()
                            {

                                try
                                {

                                    using (var client = new WebClient())
                                    {
                                        client.Headers[HttpRequestHeader.ContentType] =
                                            "application/x-www-form-urlencoded";

                                        var encodedData = "software=" + assembly.GetName().Name + "&message=" +
                                                          HttpUtility.UrlEncode(ex.GetType().FullName + ": " +
                                                                                ex.Message) + "&stacktrace=" +
                                                          HttpUtility.UrlEncode(ex.StackTrace) + "&version=" + currentDeployment.CurrentVersion;
                                        var uploadData = Encoding.Default.GetBytes(encodedData);

                                        client.UploadData("http://flamefusion.net/api/ReportBug", "POST", uploadData);
                                    }

                                }
                                catch (Exception)
                                {

                                }

                            });
                        }
                    }
                    catch (InvalidDeploymentException)
                    {
                    }
                }
            }
        }

        public static int GetRemainingTranslationCount()
        {
            try
            {
                using (var downloader = new WebClient())
                {
                    var count =
                        int.Parse(downloader.DownloadString(
                            "http://flamefusion.net/api/GetNeededTranslationCount?localization=" +
                            CultureInfo.CurrentCulture.Name));
                    return count;
                }
            }
            catch (Exception)
            {
                return -1;
            }
        }

        public static void RegisterInstallAsync(Assembly assembly)
        {
            if (!Debugger.IsAttached)
            {
                RunAsync(delegate()
                             {
                                 try
                                 {
                                     using (var downloader = new WebClient())
                                     {
                                         downloader.DownloadString(
                                             "http://flamefusion.net/api/InstallSoftware?software=" +
                                             assembly.GetName().Name);
                                     }
                                 }
                                 catch (Exception)
                                 {
                                 }
                             });
            }
        }

        public static void RegisterUninstall(Assembly assembly)
        {
            if (!Debugger.IsAttached)
            {
                try
                {
                    using (var downloader = new WebClient())
                    {
                        downloader.DownloadString(
                            "http://flamefusion.net/api/UninstallSoftware?software=" +
                            assembly.GetName().Name);
                    }
                }
                catch (Exception)
                {
                }
            }
        }

        public static void RegisterRunAsync(Assembly assembly)
        {
            if (!Debugger.IsAttached)
            {
                RunAsync(delegate()
                             {
                                 try
                                 {
                                     using (var downloader = new WebClient())
                                     {
                                         downloader.DownloadString(
                                             "http://flamefusion.net/api/RunSoftware?software=" +
                                             assembly.GetName().Name);
                                     }
                                 }
                                 catch (Exception)
                                 {
                                 }
                             });
            }
        }

    }
}
