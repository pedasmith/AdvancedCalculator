using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


using Windows.Storage;
using Windows.Storage.Streams;
using System.Diagnostics.Tracing;

namespace AdvancedCalculator
{
    [EventSource(Name = "BestCalculator")]
    public class LogEventSource : EventSource
    {
        public LogEventSource()
        {
        }
#if WINDOWS8
        // WINDOWS8 EventSource doesn't have a Write that takes in a string; UWP does.
        // Define this trivial method differently to make up for the difference.
        [Event(1, Message = "Application: {0}", Level = EventLevel.Error)]
        public void Write(string str)
        {
            WriteEvent(1, str);
        }
#else
        [Event(1, Message = "Application: {0}", Level = EventLevel.Error)]
        public new void Write(string str)
        {
            WriteEvent(1, str);
        }
#endif
    }
    public class CalculatorLog : Shipwreck.Utilities.ILog
    {
        static LogEventSource logEventSource=null;
        public void Write(string format, params object[] args)
        {
            Init();
            bool wrote = false;
            string str = string.Format(format, args);
            logEventSource.Write(str);
            lock (logStrings)
            {
                if (logWriter == null || InFlush)
                {
                    wrote = true;
                    logStrings.Add(str);
                }
            }
            if (!wrote)
            {
                // logWriter can't be null here!
                lock (logStrings)
                {
                    foreach (string savedString in logStrings)
                    {
                        logWriter.WriteString(savedString);
                    }
                    logStrings.Clear();
                }
                logWriter.WriteString(str);
                Flush();
            }
        }

        public void WriteWithTime(string str, params object[] args)
        {
            string timestr = DateTime.Now.ToString("O");
            Write(timestr + ": " + str, args);
        }

        static List<string> logStrings = new List<string>();
        static StorageFile logFile = null;
        static IRandomAccessStream logStream = null;
        static DataWriter logWriter = null;
        static int logInitialCount = 0;
        private static async void Init()
        {
            lock (logStrings)
            {
                if (logEventSource == null) logEventSource = new LogEventSource(); 
                if (logInitialCount++ > 0) return;
            }
            StorageFolder storageFolder = ApplicationData.Current.LocalFolder; // KnownFolders.DocumentsLibrary;
            try
            {
                logFile = await storageFolder.CreateFileAsync("appCalculator.log", CreationCollisionOption.ReplaceExisting);
                logStream = await logFile.OpenAsync(FileAccessMode.ReadWrite);
            }
            catch (Exception)
            {
                // Failure, try again later
                lock (logStrings)
                {
                    logInitialCount = 0;
                }
                return;
            }
            ulong pos = logStream.Position;
            ulong size = logStream.Size;
            if (pos > size) logStream.Seek(9);
            logStream.Seek(logStream.Size);
            lock (logStrings)
            {
                logWriter = new DataWriter(logStream);
                foreach (string str in logStrings)
                {
                    logWriter.WriteString(str);
                }
                logStrings.Clear();
            }
            Flush();
        }

        static int streamFlushCount = 0;
        static bool InFlush = false;
        private static void Flush()
        {
            lock (logStrings)
            {
                InFlush = true;
            }
            logWriter.StoreAsync().AsTask().ContinueWith((storeResult) =>
            {
                logWriter.FlushAsync().AsTask().ContinueWith((result) =>
                {
                    logStream.FlushAsync().AsTask().ContinueWith((streamResult) =>
                    {
                        if (streamResult.Result)
                        {
                            streamFlushCount++;
                        }
                        lock (logStrings)
                        {
                            InFlush = false;
                        }
                    });
                });
            });
            
        }
    }
}
