using Microsoft.AspNetCore.Mvc;
using Visual_Window.Controllers.Terminal.Services;

namespace Visual_Window.Controllers.Window;
[ApiController]
[Route("api/v1/[controller]")]
public class ResumeController: Controller
{
    private TerminalSessionManager _terminalManager;

    public ResumeController(TerminalSessionManager terminalManager) 
    {
        _terminalManager = terminalManager;
    }
    
    [HttpGet("terminals")]
    public async Task<IActionResult> GetResumableTerminals()
    {
        var result = new List<string>();
        foreach (var (sessionId,session) in _terminalManager.GetSessions())
        {
            if (session.Exited)
            {
                _terminalManager.CloseSession(sessionId);
            }
            result.Add(sessionId);
        }
        return Ok(new
        {
            Terminals = result
        });
    }
}