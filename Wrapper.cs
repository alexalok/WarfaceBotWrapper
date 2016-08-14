using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace BotWrapper
{
    class Wrapper
    {
        readonly string _realm;
        readonly string _email;
        readonly string _pass;
        public Wrapper(string realm, string email, string pass)
        {
            _realm = realm;
            _email = email;
            _pass = pass;
        }

        string _confFile;
        string _token;
        string _userId;
        public WrapperInfo? GetWrapperInfo()
        {
            ProcessStartInfo wrapperProcessInfo;
            if (Common.IsLinux)
            {
                wrapperProcessInfo = new ProcessStartInfo
                {
                    Arguments = $"{_realm} {_email} {_pass}",
                    FileName = Environment.CurrentDirectory + @"/wrapper.sh",
                    RedirectStandardOutput = true,
                    UseShellExecute = false
                };
            }
            else
            {
                wrapperProcessInfo = new ProcessStartInfo
                {
                    Arguments = $@"wrapper.sh {_realm} {_email} {_pass}",
                    FileName = "sh",
                    RedirectStandardOutput = true,
                    UseShellExecute = false
                };
            }
            var wrapperProcess = Process.Start(wrapperProcessInfo);
            if (wrapperProcess == null)
            {
                Log.ThreadSafeLog("Unable to launch wrapper.sh process");
                return null;
            }
            while (!wrapperProcess.StandardOutput.EndOfStream)
            {
                string line = wrapperProcess.StandardOutput.ReadLine() ?? "";
                Log.ThreadSafeLog("Wrapper.sh: " + line);
                if (!line.Contains("SUCCESS")) continue;
                var lines = line.Split('|');
                if (lines.Length != 4)
                    break;
                _token = lines[1];
                _userId = lines[2];
                _confFile = lines[3];
            }
            if (string.IsNullOrWhiteSpace(_token) || string.IsNullOrWhiteSpace(_userId) ||
                string.IsNullOrWhiteSpace(_confFile))
            {
                Log.ThreadSafeLog("Wrapper.sh: " + "AUTH_FAILED");
                return null;
            }
#if DEBUG
            Log.ThreadSafeLog("Wrapper.sh: " + $"Token: {_token}\nUserId: {_userId}\nConfig: {_confFile}");
#endif
            return new WrapperInfo
            {
                ConfFile = _confFile,
                UserId = _userId,
                Token = _token
            };
        }
    }

    struct WrapperInfo
    {
        public string Token;
        public string UserId;
        public string ConfFile;
    }
}
