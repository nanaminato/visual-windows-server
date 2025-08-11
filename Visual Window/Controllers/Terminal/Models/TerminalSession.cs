using System.Diagnostics;

namespace Visual_Window.Controllers.Terminal.Models;

public class TerminalSession
{
    public string Id { get; }
    public Process Process { get; }
    public StreamWriter InputWriter { get; }
    public StreamReader OutputReader { get; }
    public StreamReader ErrorReader { get; }

    public TerminalSession(string id, Process process)
    {
        Id = id;
        Process = process;
        InputWriter = process.StandardInput;
        OutputReader = process.StandardOutput;
        ErrorReader = process.StandardError;
    }
}