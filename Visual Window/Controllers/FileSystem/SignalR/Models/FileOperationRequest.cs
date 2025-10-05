namespace Visual_Window.Controllers.FileSystem.SignalR.Models;

public class FileOperationRequest
{
    public string LocalOperationId { get; set; } = string.Empty;
    public string? OperationId { get; set; }
    public string[]? SourcePaths { get; set; }
    public string? DestinationPath { get; set; }
    public string? OperationType { get; set; } // copy, cut, delete, upload, serverTransfer
    public string? SourceServerId { get; set; } // 预留字段
    public string? TargetServerId { get; set; } // 预留字段
}