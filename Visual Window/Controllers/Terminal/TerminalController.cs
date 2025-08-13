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

    [HttpGet("")]
    public IActionResult GetTerminals()
    {
        var list = _manager.ListSessions();
        return Ok(list);
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

    [HttpDelete("{id}")]
    public IActionResult DeleteTerminal(string id)
    {
        if (_manager.CloseSession(id))
        {
            return Ok();
        }

        return NotFound();
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

        var cancellationToken = HttpContext.RequestAborted;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var sendTask = Task.Run(async () =>
            {
                var buffer = new byte[1024];
                while (!cancellationToken.IsCancellationRequested&&!session.Exited)
                {
                    int read = await session.PtyConnection.ReaderStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
                    if (read > 0)
                    {
                        var segment = new ArraySegment<byte>(buffer, 0, read);
                        await webSocket.SendAsync(segment, WebSocketMessageType.Text, true, cancellationToken);
                    }
                    else
                    {
                        await Task.Delay(50, cancellationToken);
                    }
                }
            }, cancellationToken);

            var receiveTask = Task.Run(async () =>
            {
                var buffer = new byte[1024];
                while (!cancellationToken.IsCancellationRequested&&!session.Exited)
                {
                    var result = await webSocket.ReceiveAsync(buffer, cancellationToken);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Client closed", cancellationToken);
                        break;
                    }

                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        // var input = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        await session.PtyConnection.WriterStream.WriteAsync(buffer.AsMemory(0, result.Count), cancellationToken);
                        await session.PtyConnection.WriterStream.FlushAsync(cancellationToken);
                    }
                }
            }, cancellationToken);

            await Task.WhenAny(sendTask, receiveTask);

            try
            {
                await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", cancellationToken);
            }
            catch { }
        }else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            var sendTask = Task.Run(async () =>
            {
                var buffer = new char[1024];
                while (!cancellationToken.IsCancellationRequested && !session.Process.HasExited)
                {
                    int read = await session.OutputReader.ReadAsync(buffer, 0, buffer.Length);
                    if (read > 0)
                    {
                        var data = new string(buffer, 0, read);
                        var bytes = Encoding.UTF8.GetBytes(data);
                        await webSocket.SendAsync(bytes, WebSocketMessageType.Text, true, cancellationToken);
                    }
                    else
                    {
                        await Task.Delay(50, cancellationToken);
                    }
                }
            }, cancellationToken);

            var receiveTask = Task.Run(async () =>
            {
                var buffer = new byte[1024];
                while (!cancellationToken.IsCancellationRequested && !session.Process.HasExited)
                {
                    var result = await webSocket.ReceiveAsync(buffer, cancellationToken);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Client closed", cancellationToken);
                        break;
                    }

                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        var input = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        await session.InputWriter.WriteAsync(input);
                        await session.InputWriter.FlushAsync();
                    }
                }
            }, cancellationToken);

            await Task.WhenAny(sendTask, receiveTask);

            try
            {
                await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", cancellationToken);
            }
            catch { }
        }
    }
}