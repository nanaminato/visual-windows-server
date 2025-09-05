using System.Collections.Concurrent;
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

        // 根据平台设置默认的终端名称和应用程序
        var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        var defaultApp = isWindows ? "powershell.exe" : "/bin/bash";
        var name = isWindows ? "terminal" : "bash";

        var options = new PtyOptions
        {
            Name = name,
            Cols = 1000,
            Rows = 25,
            Cwd = terminalCreateOptions.Cwd ?? Environment.CurrentDirectory,
            App = terminalCreateOptions.App ?? defaultApp,
            ForceWinPty = true,
        };

        var tokenSource = new CancellationTokenSource();
        var terminal = await PtyProvider.SpawnAsync(options, tokenSource.Token);
        var session = new TerminalSession(id,  terminal);
        _sessions[id] = session;

        // 监听进程退出，自动移除
        terminal.ProcessExited += (s, e) =>
        {
            _sessions.TryRemove(id, out _);
            terminal.Dispose();
        };

        return session;
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
                if (!session.Exited)
                {
                    session.PtyConnection?.Kill();
                    session.PtyConnection?.Dispose();
                    session.Exited = true;
                }
            }
            catch { }
            return true;
        }
        return false;
    }

    public ConcurrentDictionary<string, TerminalSession> GetSessions()
    {
        return _sessions;
    }

}