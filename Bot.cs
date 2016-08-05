using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BotWrapper
{
    public class Bot
    {
        private readonly string _args;
        private readonly string _email;
        public readonly Queue<string> Input = new Queue<string>();
        public readonly Queue<string> Output = new Queue<string>();
        private readonly string _pass;
        private Process _process;
        private readonly string _realm;
        private int lastQueryId;
        private string _nickname;

        public Bot(string email, string pass, string realm, string args)
        {
            _email = email;
            _pass = pass;
            _realm = realm;
            _args = args;
        }

        public void Run()
        {
            var wrapperInfo = new Wrapper(_realm, _email, _pass).GetWrapperInfo();
            if (wrapperInfo == null)
            {
                Output.Enqueue("WRAPPER_ERROR");
                return;
            }
            string arguments = $"-t {wrapperInfo.Value.Token} -i {wrapperInfo.Value.UserId} -f {wrapperInfo.Value.ConfFile} " + _args;
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
            };
            _process = Process.Start(botProcessInfo);
            if (_process == null)
            {
                Output.Enqueue("BOT_ERROR");
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
                if (line.Contains("<-")) //incoming query
                {
                    ExtractQueryId(line);
                }
                if (line.Contains("it's over"))
                {
                    Output.Enqueue("NO_PING");
                    return;
                }
                if (!Common.IsLinux)
                    line = new Regex(@"\u001b\[[\w\d;]+").Replace(line, "");
                if (line.Contains("NICKNAME is"))
                {
                    _nickname = new Regex(@"NICKNAME is (.*)").Match(line).Groups[1].Value;
                    Output.Enqueue($"NICKNAME|{_nickname}");
                }
                if (line.Contains("KREDITS is"))
                {
                    Output.Enqueue("ENABLED");
                }
                if (!line.Contains("CMD#"))
                    Output.Enqueue(line);
                if (Input.Count > 0)
                    _process.StandardInput.WriteLine(Format.FormatQuery(query: Input.Dequeue(), lastQueryId: lastQueryId, nickname: _nickname));
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

        private void ExtractQueryId(string query)
        {
            var match = new Regex(@"id='uid(\d{8,8})'").Match(query);
            if (!match.Success)
                return;
            if (!Int32.TryParse(match.Groups[1].Value, out lastQueryId))
            {
                //Log.ThreadSafeLog("Error while parsing query ID");
            }
        }

        public void Stop()
        {
            _process.StandardInput.Close();
        }
    }
}