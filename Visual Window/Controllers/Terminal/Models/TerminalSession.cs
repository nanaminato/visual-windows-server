using System.Diagnostics;
using Pty.Net;

namespace Visual_Window.Controllers.Terminal.Models;

public class TerminalSession
{
    public string Id { get; }
    public Process? Process { get; }
    public StreamWriter InputWriter { get; }
    public StreamReader OutputReader { get; }
    public StreamReader ErrorReader { get; }
    public IPtyConnection?  PtyConnection { get; set; }
    public bool Exited { get; set; }

    public TerminalSession(string id, Process? process, IPtyConnection? ptyConnection = null)
    {
        Id = id;
        Process = process;
        if (process != null)
        {
            InputWriter = process.StandardInput;
            OutputReader = process.StandardOutput;
            ErrorReader = process.StandardError;
        }
        PtyConnection = ptyConnection;
    }
}