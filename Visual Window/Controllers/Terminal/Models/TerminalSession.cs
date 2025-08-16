using System.Diagnostics;
using Pty.Net;

namespace Visual_Window.Controllers.Terminal.Models;

public class TerminalSession
{
    private const int BufferSize = 1024 * 1024; // 1MB
    public string Id { get; }
    public Process? Process { get; }
    public StreamWriter InputWriter { get; }
    public StreamReader OutputReader { get; }
    public StreamReader ErrorReader { get; }
    public IPtyConnection?  PtyConnection { get; set; }
    public CancellationTokenSource CancellationTokenSource { get; set; }
    public bool Exited { get; set; }
    
    // 新增：1MB缓存区，线程安全
    private readonly object _bufferLock = new();
    private readonly byte[] _outputBuffer = new byte[BufferSize];
    private int _bufferStart = 0; // 环形缓冲区起始索引
    private int _bufferLength = 0; // 当前缓存长度
    public TerminalSession(string id, Process? process, IPtyConnection? ptyConnection = null, CancellationTokenSource? cancellationTokenSource = null)
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
        CancellationTokenSource = cancellationTokenSource ?? new CancellationTokenSource();
    }
    // 新增：写入缓存
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
                int freeSpace = BufferSize - _bufferLength;
                if (count > freeSpace)
                {
                    // 丢弃最旧的数据，腾出空间
                    int removeCount = count - freeSpace;
                    _bufferStart = (_bufferStart + removeCount) % BufferSize;
                    _bufferLength -= removeCount;
                }

                // 写入数据
                int writePos = (_bufferStart + _bufferLength) % BufferSize;

                int firstPart = Math.Min(BufferSize - writePos, count);
                Array.Copy(data, offset, _outputBuffer, writePos, firstPart);
                if (count > firstPart)
                {
                    Array.Copy(data, offset + firstPart, _outputBuffer, 0, count - firstPart);
                }
                _bufferLength += count;
            }
        }
    }

    // 新增：获取缓存数据（按顺序）
    public byte[] GetBufferSnapshot()
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
                int firstPart = BufferSize - _bufferStart;
                Array.Copy(_outputBuffer, _bufferStart, snapshot, 0, firstPart);
                Array.Copy(_outputBuffer, 0, snapshot, firstPart, _bufferLength - firstPart);
            }
            return snapshot;
        }
    }
}