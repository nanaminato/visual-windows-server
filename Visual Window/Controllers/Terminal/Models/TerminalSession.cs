using System.Diagnostics;
using System.Net.WebSockets;
using System.Text;
using Pty.Net;

namespace Visual_Window.Controllers.Terminal.Models;

public class TerminalSession(string id, IPtyConnection ptyConnection)
{
    private const int BufferSize = 1024 * 1024; // 1MB
    public string Id { get; } = id;
    public IPtyConnection?  PtyConnection { get; set; } = ptyConnection;
    public bool Exited { get; set; }
    public bool Connected { get; set; }
    
    private readonly object _bufferLock = new();
    private readonly byte[] _outputBuffer = new byte[BufferSize];
    private int _bufferStart = 0; // 环形缓冲区起始索引
    private int _bufferLength = 0; // 当前缓存长度
    public override string ToString()
    {
        var ptyInfo = PtyConnection != null ? PtyConnection.ToString() : "PtyConnection(null)";
        return $"TerminalSession(Id={Id}, Exited={Exited}, Connected={Connected}, {ptyInfo})";
    }


    public void AppendToBuffer(byte[] data, int offset, int count)
    {
        lock (_bufferLock)
        {
            if (count >= BufferSize)
            {
                // 如果数据比缓存还大，直接只保留最后1MB
                Array.Copy(data, offset + count - BufferSize, _outputBuffer, 0, BufferSize);
                _bufferStart = 0;
                _bufferLength = BufferSize;
            }
            else
            {
                // 计算剩余空间
                var freeSpace = BufferSize - _bufferLength;
                if (count > freeSpace)
                {
                    // 丢弃最旧的数据，腾出空间
                    var removeCount = count - freeSpace;
                    _bufferStart = (_bufferStart + removeCount) % BufferSize;
                    _bufferLength -= removeCount;
                }

                // 写入数据
                var writePos = (_bufferStart + _bufferLength) % BufferSize;

                var firstPart = Math.Min(BufferSize - writePos, count);
                Array.Copy(data, offset, _outputBuffer, writePos, firstPart);
                if (count > firstPart)
                {
                    Array.Copy(data, offset + firstPart, _outputBuffer, 0, count - firstPart);
                }
                _bufferLength += count;
            }
        }
    }

    private byte[] GetBufferSnapshot()
    {
        lock (_bufferLock)
        {
            var snapshot = new byte[_bufferLength];
            if (_bufferLength == 0) return snapshot;

            if (_bufferStart + _bufferLength <= BufferSize)
            {
                Array.Copy(_outputBuffer, _bufferStart, snapshot, 0, _bufferLength);
            }
            else
            {
                var firstPart = BufferSize - _bufferStart;
                Array.Copy(_outputBuffer, _bufferStart, snapshot, 0, firstPart);
                Array.Copy(_outputBuffer, 0, snapshot, firstPart, _bufferLength - firstPart);
            }
            return snapshot;
        }
    }
    
    public async Task StartTerminal(WebSocket webSocket)
    {
        var source = new CancellationTokenSource();
        var cancellationToken = source.Token;
        Connected = true;
        // 先发送缓存数据
        var cachedData = GetBufferSnapshot();
        if (cachedData.Length > 0)
        {
            await webSocket.SendAsync(cachedData, WebSocketMessageType.Text, true, cancellationToken);
        }
        var sendTask = Task.Run(async () =>
        {
            var buffer = new byte[1024];
            while (webSocket.State == WebSocketState.Open&&cancellationToken.IsCancellationRequested == false)
            {
                var read = await PtyConnection!.ReaderStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
                if (read > 0)
                {
                    // 写入缓存
                    AppendToBuffer(buffer, 0, read);
                    var segment = new ArraySegment<byte>(buffer, 0, read);
                    if (webSocket.State == WebSocketState.Open)
                    {
                        await webSocket.SendAsync(segment, WebSocketMessageType.Text, true, cancellationToken);
                    }
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
            while (webSocket.State == WebSocketState.Open&&cancellationToken.IsCancellationRequested == false)
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
                    await PtyConnection!.WriterStream.WriteAsync(buffer.AsMemory(0, result.Count), cancellationToken);
                    await PtyConnection.WriterStream.FlushAsync(cancellationToken);
                }
            }
        }, cancellationToken);

        await Task.WhenAny(sendTask, receiveTask);
        Connected = false;
        Console.WriteLine("websocket disconnected");
        try
        {
            await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", cancellationToken);
        }
        catch { }
    }
    
}