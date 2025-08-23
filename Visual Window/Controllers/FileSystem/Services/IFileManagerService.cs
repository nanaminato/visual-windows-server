using Visual_Window.Controllers.FileSystem.Models;

namespace Visual_Window.Controllers.FileSystem.Services;

public interface IFileManagerService
{
    // 获取指定路径下的文件和文件夹列表
    Task<IEnumerable<LightFile>> GetEntriesAsync(string path);
    
    Task<List<EasyFolder>> GetChildFoldersAsync(string path);
    // 创建新文件夹
    Task CreateDirectoryAsync(string path, string directoryName);

    // 删除文件或文件夹
    Task DeleteEntryAsync(string path);

    // 重命名文件或文件夹
    Task RenameEntryAsync(string path, string newName);

    // 上传文件（传入目标路径和文件流）
    Task UploadFileAsync(string targetPath, Stream fileStream, string fileName);

    // 下载文件（返回文件流）
    Task<Stream> DownloadFileAsync(string filePath);

    // 搜索文件或文件夹
    Task<IEnumerable<LightFile>> SearchAsync(string rootPath, string searchPattern, bool sarchChild = false);

    // 获取文件或文件夹属性
    Task<NormalFileAttributes> GetAttributesAsync(string path);
}
