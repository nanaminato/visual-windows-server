using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;
using Visual_Window.Controllers.FileSystem.SignalR.Models;
using Visual_Window.Controllers.FileSystem.SignalR.Services;

namespace Visual_Window.Controllers.FileSystem.SignalR;

public class FileOperationManager
{
    private readonly FileOperationService _fileOperationService;
    private readonly ConcurrentDictionary<string, FileOperationTaskInfo> _tasks = new();

    public FileOperationManager(IHubContext<FileOperationsHub> hubContext, FileOperationService fileOperationService)
    {
        _fileOperationService = fileOperationService;
    }

    public bool TryStartOperation(FileOperationRequest request)
    {
        var cts = new CancellationTokenSource();
        request.OperationId ??= Guid.NewGuid().ToString();
        var taskInfo = new FileOperationTaskInfo
        {
            OperationId = request.OperationId,
            CancellationTokenSource = cts
        };

        if (!_tasks.TryAdd(request.OperationId, taskInfo))
            return false; // 已存在同ID任务

        _ = RunOperationAsync(request, cts.Token).ContinueWith(t =>
        {
            _tasks.TryRemove(request.OperationId, out _);
        }, cts.Token);

        return true;
    }

    public bool CancelOperation(string operationId)
    {
        if (_tasks.TryGetValue(operationId, out var taskInfo))
        {
            taskInfo.CancellationTokenSource.Cancel();
            return true;
        }
        return false;
    }

    private async Task RunOperationAsync(FileOperationRequest request, CancellationToken token)
    {
        await _fileOperationService.StartFileOperationAsync(request, token);
    }
}