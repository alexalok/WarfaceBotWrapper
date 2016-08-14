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
        readonly string _args;
        readonly string _email;
        public readonly Queue<string> Input = new Queue<string>();
        public readonly Queue<string> Output = new Queue<string>();
        readonly string _pass;
        Process _process;
        readonly string _realm;
        int _lastQueryId;
        string _nickname;
        readonly bool _autorestart;
        int _autorestartedCount;

        public Bot(string email, string pass, string realm, string args, bool autorestart = false)
        {
            _email = email;
            _pass = pass;
            _realm = realm;
            _args = args;
            _autorestart = autorestart;
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
                if (line.Contains("<-")) //incoming query
                {
                    ExtractQueryId(line);
                }
                if (line.Contains("it's over"))
                {
                    Output.Enqueue("NO_PING");
                    return;
                }
                if (line.Contains("Custom query error"))
                    Input.Enqueue("quit");
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
                    _process.StandardInput.WriteLine(Format.FormatQuery(query: Input.Dequeue(), lastQueryId: _lastQueryId, nickname: _nickname));
            }
            if (!_autorestart || _isForceStopping /*|| _autorestartedCount > 5*/)
            {
                Output.Enqueue("EOL");
                _isForceStopping = false;
            }
            else
            {
                _autorestartedCount++;
                Output.Enqueue("AUTORESTART #" + _autorestartedCount);
                Run();
            }
        }

        void ExtractQueryId(string query)
        {
            var match = new Regex(@"id='uid(\d{8,8})'").Match(query);
            if (!match.Success)
                return;
            if (!Int32.TryParse(match.Groups[1].Value, out _lastQueryId))
            {
                //Log.ThreadSafeLog("Error while parsing query ID");
            }
        }

        bool _isForceStopping;
        public void Stop()
        {
            Log.ThreadSafeLog("Stopping bot", "main");
            _isForceStopping = true;
            _process?.StandardInput.Close();
        }
    }
}