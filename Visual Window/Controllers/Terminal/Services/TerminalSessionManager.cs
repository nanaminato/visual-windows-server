using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Pty.Net;
using Visual_Window.Controllers.Terminal.Models;

namespace Visual_Window.Controllers.Terminal.Services;

public class TerminalSessionManager
{
    private readonly ConcurrentDictionary<string, TerminalSession> _sessions = new();
    public async Task<TerminalSession> CreateSession(TerminalCreateOptions terminalCreateOptions)
    {
        var id = Guid.NewGuid().ToString();
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            
            try{
                // 根据平台设置默认的终端名称和应用程序
                var defaultApp = "powershell.exe";
                var name = "terminal";

                var options = new PtyOptions
                {
                    Name = name,
                    Cols = 80,
                    Rows = 25,
                    Cwd = terminalCreateOptions.Cwd ?? Environment.CurrentDirectory,
                    App = terminalCreateOptions.App ?? defaultApp,
                    ForceWinPty = true,
                };

                var tokenSource = new CancellationTokenSource();
                var terminal = await PtyProvider.SpawnAsync(options, tokenSource.Token);
                var session = new TerminalSession(id, terminal);
                _sessions[id] = session;
            
                // 监听进程退出，自动移除
                terminal.ProcessExited += (s, e) =>
                {
                    try
                    {
                        _sessions.TryRemove(id, out _);
                        Console.WriteLine("Ipty connection exited and removed from session");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                };

                return session;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }
        }
        else
        {
            ProcessStartInfo psi;
            // Linux / Ubuntu
            psi = new ProcessStartInfo("script", $"-qfc {terminalCreateOptions.App??"/bin/bash"} /dev/null")
            {
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                // 设置环境变量，确保bash正常工作
                WorkingDirectory = terminalCreateOptions.Cwd ?? Environment.CurrentDirectory,
                Environment =
                {
                    ["TERM"] = "xterm-256color"
                }
            };
            var process = new Process { StartInfo = psi, EnableRaisingEvents = true };
            process.Start();
            var session = new TerminalSession(id,null, process);
            _sessions[id] = session;

            // 监听进程退出，自动移除
            process.Exited += (s, e) =>
            {
                CloseSession(id);
                process.Dispose();
            };
            return session;
        }
    }


    public bool TryGetSession(string id, out TerminalSession session)
    {
        return _sessions.TryGetValue(id, out session);
    }

    public bool CloseSession(string id)
    {
        if (_sessions.TryRemove(id, out var session))
        {
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    Console.WriteLine("Ipty connection open-> try close");
                    session.PtyConnection?.Dispose();
                    Console.WriteLine("Ipty connection dispose");
                    session.PtyConnection?.Kill();
                    Console.WriteLine("Ipty connection kill");
                }
                else
                {
                    session.Process?.Kill();
                }
                
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return true;
        }
        return false;
    }

    public ConcurrentDictionary<string, TerminalSession> GetSessions()
    {
        return _sessions;
    }

}