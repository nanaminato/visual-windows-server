using System.Diagnostics;
using System.Net.WebSockets;
using System.Text;
using Pty.Net;

namespace Visual_Window.Controllers.Terminal.Models;

public class TerminalSession
{
    private const int BufferSize = 1024 * 1024; // 1MB
    public string Id { get; }
    public IPtyConnection?  PtyConnection { get; set; }
    private int _connected;

    public bool Connected
    {
        get => Interlocked.CompareExchange(ref _connected, 0, 0) == 1;
        set => Interlocked.Exchange(ref _connected, value ? 1 : 0);
    }

    public Process? Process { get; }
    public StreamWriter InputWriter { get; }
    public StreamReader OutputReader { get; }
    public StreamReader ErrorReader { get; }
    
    public TerminalSession(string id,IPtyConnection? ptyConnection = null, Process? process=null)
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
    
    private readonly object _bufferLock = new();
    private readonly byte[] _outputBuffer = new byte[BufferSize];
    private int _bufferStart = 0; // 环形缓冲区起始索引
    private int _bufferLength = 0; // 当前缓存长度
    public override string ToString()
    {
        var ptyInfo = PtyConnection != null ? PtyConnection.ToString() : "PtyConnection(null)";
        return $"TerminalSession(Id={Id}, Connected={Connected}, {ptyInfo})";
    }


    private void AppendToBuffer(byte[] data, int offset, int count)
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
        try
        {
            // 先发送缓存数据
            var cachedData = GetBufferSnapshot();
            if (cachedData.Length > 0)
            {
                try
                {
                    await webSocket.SendAsync(cachedData, WebSocketMessageType.Text, true, cancellationToken);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error sending cached data: {ex}");
                    await source.CancelAsync();
                }
            }

            var sendTask = Task.Run(async () =>
            {
                try
                {
                    var buffer = new byte[1024];
                    while (!cancellationToken.IsCancellationRequested)
                    {
                        var read = await PtyConnection!.ReaderStream.ReadAsync(buffer, 0, buffer.Length,
                            cancellationToken);
                        if (read > 0)
                        {
                            // 写入缓存
                            AppendToBuffer(buffer, 0, read);
                            var segment = new ArraySegment<byte>(buffer, 0, read);
                            if (webSocket.State == WebSocketState.Open)
                            {
                                await webSocket.SendAsync(segment, WebSocketMessageType.Text, true,
                                    cancellationToken);
                            }
                            else
                            {
                                await source.CancelAsync();
                            }
                        }
                        else
                        {
                            await source.CancelAsync();
                        }
                    }
                }
                catch (ObjectDisposedException)
                {
                    Console.WriteLine("send cancelled");
                    /* 正常取消 */  
                }
                catch (OperationCanceledException)
                {
                    Console.WriteLine("send cancelled");
                    /* 正常取消 */
                }
                catch (WebSocketException)
                {
                    Console.WriteLine("Receive canceled");
                    /* 正常取消 */
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"SendTask exception: {ex}");
                    // 这里可以考虑取消整个连接
                    
                }
                await source.CancelAsync();
                Console.WriteLine("SendTask finished");
            }, cancellationToken);

            var receiveTask = Task.Run(async () =>
            {
                try
                {
                    var buffer = new byte[1024];
                    while (!cancellationToken.IsCancellationRequested)
                    {
                        var result = await webSocket.ReceiveAsync(buffer, cancellationToken);
                        if (result.MessageType == WebSocketMessageType.Close)
                        {
                            await source.CancelAsync();
                            await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Client closed",
                                CancellationToken.None);
                            Console.WriteLine("Client closed");
                            break;
                        }

                        if (result.MessageType == WebSocketMessageType.Text)
                        {
                            await PtyConnection!.WriterStream.WriteAsync(buffer.AsMemory(0, result.Count),
                                cancellationToken);
                            await PtyConnection.WriterStream.FlushAsync(cancellationToken);
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    Console.WriteLine("Receive canceled");
                    /* 正常取消 */
                }
                catch (WebSocketException)
                {
                    Console.WriteLine("Receive canceled");
                    /* 正常取消 */
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Receive exception: {ex}");
                    // 这里可以考虑取消整个连接
                }
                await source.CancelAsync();
                Console.WriteLine("Receive finished");
                Console.WriteLine(webSocket.CloseStatusDescription);
            }, cancellationToken);

            await Task.WhenAll(sendTask, receiveTask);
            Console.WriteLine("all finished");
        }
        finally
        {
            Connected = false;
            Console.WriteLine("websocket disconnected");
            try
            {
                if (webSocket.State == WebSocketState.Open || webSocket.State == WebSocketState.CloseReceived)
                {
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine($"Error closing websocket: {ex}");
            }
        }
        
    }
    public async Task StartLinux(WebSocket webSocket)
    {
        try
        {
            var source = new CancellationTokenSource();
            var cancellationToken = source.Token;
            var cachedData = GetBufferSnapshot();
            Connected = true;
            if (cachedData.Length > 0)
            {
                await webSocket.SendAsync(cachedData, WebSocketMessageType.Text, true, cancellationToken);
            }

            var sendTask = Task.Run(async () =>
            {
                try
                {
                    var buffer = new byte[1024];
                    while (!cancellationToken.IsCancellationRequested)
                    {
                        var read = await OutputReader.BaseStream.ReadAsync(buffer, cancellationToken);
                        if (read > 0)
                        {
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
                }
                catch (OperationCanceledException)
                {
                    Console.WriteLine("Receive canceled");
                    /* 正常取消 */
                }
                catch (WebSocketException)
                {
                    Console.WriteLine("Receive canceled");
                    /* 正常取消 */
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
                await source.CancelAsync();
                Console.WriteLine("sender exited");
            }, cancellationToken);

            var receiveTask = Task.Run(async () =>
            {
                try
                {
                    var buffer = new byte[1024];
                    while (!cancellationToken.IsCancellationRequested)
                    {
                        var result = await webSocket.ReceiveAsync(buffer, cancellationToken);
                        if (result.MessageType == WebSocketMessageType.Close)
                        {
                            await source.CancelAsync();
                            await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Client closed",
                                CancellationToken.None);
                            Console.WriteLine("Client closed");
                            break;
                        }

                        if (result.MessageType == WebSocketMessageType.Text)
                        {
                            var input = Encoding.UTF8.GetString(buffer, 0, result.Count);
                            await InputWriter.WriteAsync(input);
                            await InputWriter.FlushAsync(cancellationToken);
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    Console.WriteLine("Receive canceled");
                    /* 正常取消 */
                }
                catch (WebSocketException)
                {
                    Console.WriteLine("Receive canceled");
                    /* 正常取消 */
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
                await source.CancelAsync();
            }, cancellationToken);
            await Task.WhenAll(sendTask, receiveTask);
            Console.WriteLine("all finished");
        }
        finally
        {
            Connected = false;
            Console.WriteLine("websocket disconnected");
            try
            {
                if (webSocket.State == WebSocketState.Open || webSocket.State == WebSocketState.CloseReceived)
                {
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine($"Error closing websocket: {ex}");
            }
        }
        
    }
    
    
}