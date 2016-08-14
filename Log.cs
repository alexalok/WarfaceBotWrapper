using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace BotWrapper
{
    static class Log
    {
        static readonly Object LogLock = new Object();
        public static void ThreadSafeLog(string message, string logFile = null)
        {
            string line = DateTime.Now.ToString("[dd-MM HH:mm:ss]") + " " + message;
            try
            {
                Console.WriteLine(line);
            }
            catch (Exception) { }

            if (logFile == null) return;
            lock (LogLock)
            {
                if (!Directory.Exists("logs"))
                    Directory.CreateDirectory("logs");
                string filePath = Common.IsLinux ? $@"logs/{logFile}.log" : $@"logs\{logFile}.log";
                using (var sw = new StreamWriter(filePath, true))
                {
                    sw.WriteLine(line);
                }
            }
        }
    }
}
