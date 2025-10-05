namespace Visual_Window.Controllers.FileSystem.SignalR;

public class FileOperationTaskInfo
{
    public string OperationId { get; set; }
    public CancellationTokenSource CancellationTokenSource { get; set; }
    // 可以扩展存储 状态、错误、进度等
}