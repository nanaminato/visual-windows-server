using Microsoft.AspNetCore.SignalR;
using Visual_Window.Controllers.FileSystem.SignalR.Models;

namespace Visual_Window.Controllers.FileSystem.SignalR.Services;

public class FileOperationService
{
    private readonly IHubContext<FileOperationsHub> _hubContext;

    public FileOperationService(IHubContext<FileOperationsHub> hubContext)
    {
        _hubContext = hubContext;
    }

    // copy to , cut to , delete
    public async Task StartFileOperationAsync(FileOperationRequest request, CancellationToken cancellationToken)
    {
        var opId = request.OperationId;
        var totalFiles = request.SourcePaths!.Length;
        if (totalFiles == 0)
        {
            await _hubContext.Clients.All.SendAsync("FileOperationCompleted", new { operationId = opId });
            return;
        }

        var progressPerFile = 100.0 / totalFiles;
        double cumulativeProgress = 0;

        for (var i = 0; i < totalFiles; i++)
        {
            var src = request.SourcePaths[i];
            var fileName = Path.GetFileName(src);

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                switch (request.OperationType)
                {
                    case "copy" :
                        if (string.IsNullOrWhiteSpace(request.DestinationPath))
                            throw new ArgumentException("DestinationPath is required for copy operation.");

                        var destCopy = Path.Combine(request.DestinationPath, fileName);
                        await CopyFileWithProgressAsync(src, destCopy, fileProgress =>
                        {
                            // 计算整体进度：已完成文件 + 当前文件进度
                            var totalProgress = (int)(cumulativeProgress + fileProgress * progressPerFile / 100);
                            _hubContext.Clients.All.SendAsync("FileOperationProgress", new
                            {
                                operationId = opId,
                                progress = totalProgress,
                                currentFile = fileName
                            });
                        }, cancellationToken);
                        break;

                    case "cut":
                        if (string.IsNullOrWhiteSpace(request.DestinationPath))
                            throw new ArgumentException("DestinationPath is required for cut operation.");

                        var destCut = Path.Combine(request.DestinationPath, fileName);
                        // 复制
                        await CopyFileWithProgressAsync(src, destCut, fileProgress =>
                        {
                            var totalProgress = (int)(cumulativeProgress + fileProgress * progressPerFile / 100);
                            _hubContext.Clients.All.SendAsync("FileOperationProgress", new
                            {
                                operationId = opId,
                                progress = totalProgress,
                                currentFile = fileName
                            });
                        }, cancellationToken);

                        // 删除源文件
                        File.Delete(src);
                        break;

                    case "delete":
                        // 删除单个文件，模拟进度（删除操作一般瞬时完成，这里进度分配可以简单处理）
                        File.Delete(src);

                        // 发送“文件删除完成”进度，增加对应份额
                        var deleteProgress = (int)((i + 1) * progressPerFile);
                        await _hubContext.Clients.All.SendAsync("FileOperationProgress", new
                        {
                            operationId = opId,
                            progress = deleteProgress,
                            currentFile = fileName
                        });
                        break;

                    default:
                        throw new NotSupportedException($"Unsupported operation type: {request.OperationType}");
                }

                cumulativeProgress += progressPerFile;
            }
            catch (OperationCanceledException)
            {
                await _hubContext.Clients.All.SendAsync("FileOperationCancelled", new { operationId = opId });
                return;
            }
            catch (Exception ex)
            {
                await _hubContext.Clients.All.SendAsync("FileOperationError", new
                {
                    operationId = opId,
                    message = ex.Message,
                    file = src
                });
                return; // 失败后退出，按你需求也可以继续
            }
        }

        await _hubContext.Clients.All.SendAsync("FileOperationCompleted", new { operationId = opId });
    }

    private async Task CopyFileWithProgressAsync(string sourceFile, string destFile, Action<int> progressCallback, CancellationToken cancellationToken)
    {
        const int bufferSize = 81920; // 80 KB
        await using var sourceStream = File.OpenRead(sourceFile);
        await using var destStream = File.OpenWrite(destFile);
        var buffer = new byte[bufferSize];

        var totalBytes = sourceStream.Length;
        long totalRead = 0;

        int read;
        while ((read = await sourceStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
        {
            await destStream.WriteAsync(buffer, 0, read, cancellationToken);
            totalRead += read;
            var progress = (int)(totalRead * 100 / totalBytes);
            progressCallback(progress);
        }
        var creationTime = File.GetCreationTime(sourceFile);
        var lastAccessTime = File.GetLastAccessTime(sourceFile);
        var lastWriteTime = File.GetLastWriteTime(sourceFile);

        File.SetCreationTime(destFile, creationTime);
        File.SetLastAccessTime(destFile, lastAccessTime);
        File.SetLastWriteTime(destFile, lastWriteTime);
    }
}