using Visual_Window.Controllers.FileSystem.Models;

namespace Visual_Window.VSystem.FileIo.impl;

public class FileManagerService : IFileManagerService
{
    public async Task<IEnumerable<FileSystemEntry>> GetEntriesAsync(string path)
    {
        if (!Directory.Exists(path))
            throw new DirectoryNotFoundException($"目录不存在: {path}");

        var entries = new List<FileSystemEntry>();

        // 获取文件夹
        var directories = Directory.GetDirectories(path);
        foreach (var dir in directories)
        {
            var info = new DirectoryInfo(dir);
            entries.Add(new FileSystemEntry
            {
                Name = info.Name,
                Path = info.FullName,
                IsDirectory = true,
                Size = 0,
                LastModified = info.LastWriteTime
            });
        }

        // 获取文件
        var files = Directory.GetFiles(path);
        foreach (var file in files)
        {
            var info = new FileInfo(file);
            entries.Add(new FileSystemEntry
            {
                Name = info.Name,
                Path = info.FullName,
                IsDirectory = false,
                Size = info.Length,
                LastModified = info.LastWriteTime
            });
        }

        return await Task.FromResult(entries);
    }

    public async Task<List<EasyFolder>> GetChildFoldersAsync(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Path cannot be null or empty.", nameof(path));

        if (!Directory.Exists(path))
            throw new DirectoryNotFoundException($"The directory '{path}' does not exist.");

        return await Task.Run(() =>
        {
            var directories = Directory.GetDirectories(path);
            var result = new List<EasyFolder>();

            foreach (var dir in directories)
            {
                var folder = new EasyFolder
                {
                    Name = Path.GetFileName(dir),
                    Path = dir
                };
                result.Add(folder);
            }

            return result;
        });
    }


    public async Task CreateDirectoryAsync(string path, string directoryName)
    {
        if (!Directory.Exists(path))
            throw new DirectoryNotFoundException($"目录不存在: {path}");

        var newDirPath = Path.Combine(path, directoryName);
        if (Directory.Exists(newDirPath) || File.Exists(newDirPath))
            throw new IOException("目标文件夹已存在");

        Directory.CreateDirectory(newDirPath);
        await Task.CompletedTask;
    }

    public async Task DeleteEntryAsync(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
        else if (Directory.Exists(path))
        {
            Directory.Delete(path, recursive: true);
        }
        else
        {
            throw new FileNotFoundException("文件或目录不存在", path);
        }

        await Task.CompletedTask;
    }

    public async Task RenameEntryAsync(string path, string newName)
    {
        if (!File.Exists(path) && !Directory.Exists(path))
            throw new FileNotFoundException("文件或目录不存在", path);

        var parentDir = Path.GetDirectoryName(path);
        if (string.IsNullOrEmpty(parentDir))
            throw new ArgumentException("路径无效", nameof(path));

        var newPath = Path.Combine(parentDir, newName);

        if (File.Exists(newPath) || Directory.Exists(newPath))
            throw new IOException("目标名称已存在");

        if (File.Exists(path))
        {
            File.Move(path, newPath);
        }
        else
        {
            Directory.Move(path, newPath);
        }

        await Task.CompletedTask;
    }

    public Task UploadFileAsync(string targetPath, Stream fileStream, string fileName)
    {
        throw new NotImplementedException();
    }

    public async Task<Stream> DownloadFileAsync(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException("文件不存在", filePath);

        // 这里返回一个 FileStream，调用方使用完需负责关闭流
        var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, useAsync: true);
        return await Task.FromResult(stream);
    }

 
    public async Task<IEnumerable<FileSystemEntry>> SearchAsync(string rootPath, string searchPattern, bool searchChild = false)
    {
        if (!Directory.Exists(rootPath))
            throw new DirectoryNotFoundException($"目录不存在: {rootPath}");

        var results = new List<FileSystemEntry>();

        await Task.Run(() =>
        {
            RecursiveSearch(rootPath, searchPattern, results, searchChild);
        });

        return results;
    }

    private void RecursiveSearch(string currentPath, string searchPattern, List<FileSystemEntry> results, bool searchChild = false)
    {
        // 先处理当前目录下的文件
        try
        {
            foreach (var file in Directory.GetFiles(currentPath, searchPattern))
            {
                try
                {
                    var info = new FileInfo(file);
                    results.Add(new FileSystemEntry
                    {
                        Name = info.Name,
                        Path = info.FullName,
                        IsDirectory = false,
                        Size = info.Length,
                        LastModified = info.LastWriteTime
                    });
                }
                catch (UnauthorizedAccessException)
                {
                    // 无权限访问单个文件，跳过
                }
                catch (PathTooLongException)
                {
                    // 路径过长，跳过
                }
            }
        }
        catch (UnauthorizedAccessException)
        {
            // 无权限访问当前目录文件，跳过
            return;
        }
        catch (DirectoryNotFoundException)
        {
            // 目录不存在，跳过
            return;
        }
        if(!searchChild) return;

        // 处理子目录
        string[] subDirs;
        try
        {
            subDirs = Directory.GetDirectories(currentPath);
        }
        catch (UnauthorizedAccessException)
        {
            // 无权限访问子目录，跳过
            return;
        }
        catch (DirectoryNotFoundException)
        {
            // 目录不存在，跳过
            return;
        }

        foreach (var dir in subDirs)
        {
            try
            {
                var info = new DirectoryInfo(dir);
                results.Add(new FileSystemEntry
                {
                    Name = info.Name,
                    Path = info.FullName,
                    IsDirectory = true,
                    Size = 0,
                    LastModified = info.LastWriteTime
                });

                // 递归搜索子目录
                RecursiveSearch(dir, searchPattern, results);
            }
            catch (UnauthorizedAccessException)
            {
                // 无权限访问该目录，跳过
            }
            catch (PathTooLongException)
            {
                // 路径过长，跳过
            }
        }
    }


    public async Task<NormalFileAttributes> GetAttributesAsync(string path)
    {
        if (File.Exists(path))
        {
            var info = new FileInfo(path);
            return await Task.FromResult(new NormalFileAttributes()
            {
                Size = info.Length,
                CreatedTime = info.CreationTime,
                LastModifiedTime = info.LastWriteTime,
                IsReadOnly = info.IsReadOnly
            });
        }

        if (!Directory.Exists(path)) throw new FileNotFoundException("文件或目录不存在", path);
        {
            var info = new DirectoryInfo(path);
            return await Task.FromResult(new NormalFileAttributes()
            {
                Size = 0,
                CreatedTime = info.CreationTime,
                LastModifiedTime = info.LastWriteTime,
                IsReadOnly = (info.Attributes & FileAttributes.ReadOnly) != 0
            });
        }
    }
}