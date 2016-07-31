using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Wrapper
{
    internal class Bot
    {
        private string _args;
        private string _confFile;
        private readonly string _email;
        public readonly Queue<string> Input = new Queue<string>();
        public readonly Queue<string> Output = new Queue<string>();
        private readonly string _pass;
        private Process _process;
        private readonly string _realm;
        private string _token;
        private string _userId;

        public Bot(string email, string pass, string realm, string args)
        {
            _email = email;
            _pass = pass;
            _realm = realm;
            _args = args;
        }

        public void Run()
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
                Output.Enqueue("INTERNAL_ERROR");
                return;
            }
            while (!wrapperProcess.StandardOutput.EndOfStream)
            {
                string line = wrapperProcess.StandardOutput.ReadLine() ?? "";
                Output.Enqueue(line);
                if (!line.Contains("SUCCESS")) continue;
                var lines = line.Split('|');
                _token = lines[1];
                _userId = lines[2];
                _confFile = lines[3];
            }
            if (string.IsNullOrWhiteSpace(_token) || string.IsNullOrWhiteSpace(_userId) ||
                string.IsNullOrWhiteSpace(_confFile))
            {
                Output.Enqueue("AUTH_FAILED");
                return;
            }
#if DEBUG
            Output.Enqueue($"Token: {_token}\nUserId: {_userId}\nConfig: {_confFile}");
#endif
            string arguments = $"-t {_token} -i {_userId} -f {_confFile} " + _args;
            string fileName;
            if (Common.IsLinux)
                fileName = Environment.CurrentDirectory + "/bot/wb";
            else
                fileName = @"bot\wb.exe";
            var botProcessInfo = new ProcessStartInfo
            {
                Arguments = arguments,
                FileName = fileName,
                RedirectStandardOutput = true,
                RedirectStandardInput = true,
                RedirectStandardError = true,
                UseShellExecute = false
                //WorkingDirectory = Environment.CurrentDirectory + @"\bot"
            };
            _process = Process.Start(botProcessInfo);
            if (_process == null)
            {
                Output.Enqueue("INTERNAL_ERROR");
                return;
            }
            while (!_process.StandardOutput.EndOfStream)
            {
                string line = _process.StandardOutput.ReadLine() ?? "";
                if (_isWorkarounding)
                    _isWorkarounding = false;
                if (line.Contains("<starttls xmlns='urn:ietf:params:xml:ns:xmpp-tls'/>"))
                {
                    WorkaroundRaceConditionBug();
                }
                if (line.Contains("it's over"))
                {
                    Output.Enqueue("ITS_OVER");
                    return;
                }
                if (!Common.IsLinux)
                    line = new Regex(@"\u001b\[[\w\d;]+").Replace(line, "");
                if (line.Contains("NICKNAME is"))
                {
                    string nickname = new Regex(@"NICKNAME is (.*)").Match(line).Groups[1].Value;
                    Output.Enqueue($"NICKNAME|{nickname}");
                }
                if (line.Contains("KREDITS is"))
                {
                    Output.Enqueue("ENABLED");
                }
                if (!line.Contains("CMD#"))
                    Output.Enqueue(line);
                if (Input.Count > 0)
                    _process.StandardInput.WriteLine(Input.Dequeue());
            }
            Output.Enqueue("EOL");
        }

        //If a race condition happens, bot will exit... 
        private bool _isWorkarounding;
        private async Task WorkaroundRaceConditionBug()
        {
            _isWorkarounding = true;
            Log.ThreadSafeLog("Workarounding race condition");
            await Task.Delay(TimeSpan.FromSeconds(10));
            if (_isWorkarounding)
            {
                Log.ThreadSafeLog("Bad things happened, a bot is aborting...");
                _process.Kill();
            }
                
        }
    }
}