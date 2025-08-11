using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Visual_Window.Controllers.Terminal.Models;

namespace Visual_Window.Controllers.Terminal.Services;

public class TerminalSessionManager
{
    private readonly ConcurrentDictionary<string, TerminalSession> _sessions = new();

    public TerminalSession CreateSession()
    {
        var id = Guid.NewGuid().ToString();

        ProcessStartInfo psi;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            psi = new ProcessStartInfo("powershell.exe")
            {
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
        }
        else
        {
            // Linux / Ubuntu
            psi = new ProcessStartInfo("/bin/bash")
            {
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
        }

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