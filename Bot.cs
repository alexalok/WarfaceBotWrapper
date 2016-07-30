using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Wrapper
{
    class Bot
    {
        public string Email;
        public string Pass;
        public string Realm;
        public string Token;
        public string UserId;
        public string ConfFile;
        public Queue<string> Output = new Queue<string>();
        public Queue<string> Input = new Queue<string>();
        public string Args;
        public Process Process;

        public Bot(string email, string pass, string realm, string args)
        {
            Email = email;
            Pass = pass;
            Realm = realm;
            Args = args;
        }

        public void Run()
        {
            ProcessStartInfo wrapperProcessInfo;
            if (Common.IsLinux)
            {
                wrapperProcessInfo = new ProcessStartInfo
                {
                    Arguments = $"{Realm} {Email} {Pass}",
                    FileName = Environment.CurrentDirectory + @"/wrapper.sh",
                    RedirectStandardOutput = true,
                    UseShellExecute = false
                };
            }
            else
            {
                wrapperProcessInfo = new ProcessStartInfo
                {
                    Arguments = $@"wrapper.sh {Realm} {Email} {Pass}",
                    FileName = "sh",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
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
                var line = wrapperProcess.StandardOutput.ReadLine() ?? "";
                if (!line.Contains("SUCCESS")) continue;
                var lines = line.Split('|');
                Token = lines[1];
                UserId = lines[2];
                ConfFile = lines[3];
            }
            if (String.IsNullOrWhiteSpace(Token) || String.IsNullOrWhiteSpace(UserId) ||
                String.IsNullOrWhiteSpace(ConfFile))
            {
                Output.Enqueue("AUTH_FAILED");
                return;
            }
#if DEBUG
            Output.Enqueue($"Token: {Token}\nUserId: {UserId}\nConfig: {ConfFile}");
#endif
            var arguments = $"-t {Token} -i {UserId} -f {ConfFile} " + Args;
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
                UseShellExecute = false,
                //WorkingDirectory = Environment.CurrentDirectory + @"\bot"
            };
            Process = Process.Start(botProcessInfo);
            if (Process == null)
            {
                Output.Enqueue("INTERNAL_ERROR");
                return;
            }
            while (!Process.StandardOutput.EndOfStream)
            {
                var line = Process.StandardOutput.ReadLine() ?? "";
                line = line.Replace("\u001b[1;31m", "").Replace("\u001b[0m", "").Replace("\u001b[1;32m","");
                if (line.Contains("NICKNAME is"))
                {
                    var nickname = new Regex(@"NICKNAME is (.*)").Match(line).Groups[1].Value;
                    Output.Enqueue($"NICKNAME|{nickname}");
                }
                if (line.Contains("KREDITS is"))
                {
                    Output.Enqueue("ENABLED");
                }
                if (!line.Contains("CMD#"))
                    Output.Enqueue(line);
                if (Input.Count > 0)
                    Process.StandardInput.WriteLine(Input.Dequeue());
            }
            Output.Enqueue("EOL");
        }
    }
}
