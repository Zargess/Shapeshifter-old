using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace Shapeshifter.Desktop.Functionality.Helpers
{
    public static class LogHelper
    {

        public static void Log(Type type, string text)
        {

            Trace.WriteLine(text);

            var directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                                         "Flamefusion", "Shapeshifter", "Logs");
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var file = Path.Combine(directory, type.FullName + ".log");
            try
            {
                using (var writer = File.AppendText(file))
                {
                    var now = DateTime.Now;
                    writer.WriteLine(now.Year + "-" + now.Month.ToString().PadLeft(2, '0') + "-" +
                                     now.Day.ToString().PadLeft(2, '0') + " " + now.Hour.ToString().PadLeft(2, '0') +
                                     ":" + now.Minute.ToString().PadLeft(2, '0') + ":" +
                                     now.Second.ToString().PadLeft(2, '0') + "." +
                                     now.Millisecond.ToString().PadLeft(3, '0') + ": " + text);
                }
            } catch(UnauthorizedAccessException)
            {
                //ignore unauthorized access
            } catch(IOException)
            {
                //ignore not being able to create file or write to it
            }

        }

        public static void ResetDefaults()
        {

            var directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                                         "Flamefusion", "Shapeshifter", "Logs");
            if (Directory.Exists(directory))
            {
                try
                {
                    Directory.Delete(directory, true);
                } catch(IOException)
                {
                    
                } catch(UnauthorizedAccessException)
                {
                    
                } catch(ArgumentException)
                {
                    
                }
            }
        }
    }
}
