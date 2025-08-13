using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Pty.Net;
using Visual_Window.Controllers.Terminal.Models;

namespace Visual_Window.Controllers.Terminal.Services;

public class TerminalSessionManager
{
    private readonly ConcurrentDictionary<string, TerminalSession> _sessions = new();
    private static readonly int TestTimeoutMs = Debugger.IsAttached ? 300_000 : 5_000;

    private CancellationToken TimeoutToken { get; } = new CancellationTokenSource(TestTimeoutMs).Token;
    public async Task<TerminalSession> CreateSession()
    {
        var id = Guid.NewGuid().ToString();

        ProcessStartInfo psi;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var options = new PtyOptions
            {
                Name = "Custom terminal",
                Cols = 50,
                Rows = 25,
                Cwd = Environment.CurrentDirectory,
                App = "powershell.exe",
                ForceWinPty = true,
            };
            var terminal = await PtyProvider.SpawnAsync(options, TimeoutToken);
            var session = new TerminalSession(id, null,terminal);
            _sessions[id] = session;

            // 监听进程退出，自动移除
            terminal.ProcessExited += (s, e) =>
            {
                _sessions.TryRemove(id, out _);
                terminal.Dispose();
            };
            return session;
        }
        else
        {
            // Linux / Ubuntu
            psi = new ProcessStartInfo("script", "-qfc /bin/bash /dev/null")
            {
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                // 设置环境变量，确保bash正常工作
                Environment =
                {
                    ["TERM"] = "xterm-256color"
                }
            };
            var process = new Process { StartInfo = psi, EnableRaisingEvents = true };
            process.Start();
            var session = new TerminalSession(id, process);
            _sessions[id] = session;

            // 监听进程退出，自动移除
            process.Exited += (s, e) =>
            {
                _sessions.TryRemove(id, out _);
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
                if (!session.Process.HasExited)
                {
                    session.Process.Kill();
                }
                session.Process.Dispose();
            }
            catch { }
            return true;
        }
        return false;
    }

    public List<string> ListSessions()
    {
        return _sessions.Keys.ToList();
    }
}