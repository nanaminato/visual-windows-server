using System.Net.WebSockets;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Visual_Window.Controllers.Terminal.Services;

namespace Visual_Window.Controllers.Terminal;

[ApiController]
[Route("api/v1/[controller]")]
public class TerminalController: Controller
{
    private readonly TerminalSessionManager _manager;
    public TerminalController(TerminalSessionManager manager)
    {
        _manager = manager;
    }
    [HttpPost("")]
    public async Task<IActionResult> CreateTerminal()
    {
        var session = await _manager.CreateSession();
        return Ok(new 
        {
            Id = session.Id
        });
    }
    [HttpDelete("{sessionId}")]
    public async Task<IActionResult> CloseSession(string sessionId)
    {
        _manager.CloseSession(sessionId);
        return Ok();
    }
    [HttpGet("{id}")]
    public async Task GetTerminalWebSocket(string id)
    {
        if (!HttpContext.WebSockets.IsWebSocketRequest)
        {
            HttpContext.Response.StatusCode = 400;
            return;
        }

        if (!_manager.TryGetSession(id, out var session))
        {
            HttpContext.Response.StatusCode = 404;
            return;
        }

        var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();

        //var cancellationToken = CancellationTokenSource.CreateLinkedTokenSource(session.CancellationTokenSource.Token).Token;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            await session.StartWindow(webSocket);
        }else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            await session.StartLinux(webSocket);
        }
    }
}