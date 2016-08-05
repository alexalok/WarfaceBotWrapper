using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace BotWrapper
{
    internal static class Log
    {
        private static bool logInProgress;
        public static void ThreadSafeLog(string message, string logFile = null)
        {
            if (logInProgress)
                Thread.Sleep(100);
            logInProgress = true;
            var line = DateTime.Now.ToString("[dd-MM HH:mm:ss]") + " " + message;
            Console.WriteLine(line);
            if (logFile != null)
            {
                string filePath = Common.IsLinux ? $@"logs/{logFile}.log" : $@"logs\{logFile}.log";
                using (var sw = new StreamWriter(filePath, true))
                {
                    sw.WriteLine(line);
                }
            }
            logInProgress = false;
        }
    }
}
