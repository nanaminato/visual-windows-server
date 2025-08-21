using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Visual_Window.Controllers.FileSystem.Models;
using Visual_Window.Controllers.FileSystem.RequestBodys;
using Visual_Window.VSystem.FileIo;
using Visual_Window.VSystem.FileIo.Utils;
namespace Visual_Window.Controllers.FileSystem;

[ApiController]
[Route("api/v1/[controller]")]
public class FileSystemController : Controller
{
    private IFileManagerService fileManagerService;
    public FileSystemController(IFileManagerService fileManagerService)
    {
        this.fileManagerService = fileManagerService;
    }
    [HttpGet("roots")]
    public IActionResult GetRoots()
    {
        var roots = RootDirectoriesProvider.GetRootDirectories();
        return Ok(roots);
    }

    [HttpPost("child-folders")]
    public async Task<IActionResult> GetChildFolders(PathRequestBody requestBody)
    {
        var folder = await fileManagerService.GetChildFoldersAsync(requestBody.Path);
        return Ok(folder);
    }

    [HttpGet("special-roots")]
    public IActionResult GetSpecialRoots()
    {
        var specialFolders = new Dictionary<string, string?>
        {
            ["Desktop"] = GetSpecialFolderPath("XDG_DESKTOP_DIR", Environment.SpecialFolder.Desktop),
            ["Documents"] = GetSpecialFolderPath("XDG_DOCUMENTS_DIR", Environment.SpecialFolder.MyDocuments),
            ["Downloads"] = GetSpecialFolderPath("XDG_DOWNLOAD_DIR", null), // SpecialFolder无Downloads，单独处理
            ["Music"] = GetSpecialFolderPath("XDG_MUSIC_DIR", Environment.SpecialFolder.MyMusic),
            ["Pictures"] = GetSpecialFolderPath("XDG_PICTURES_DIR", Environment.SpecialFolder.MyPictures),
            ["Videos"] = GetSpecialFolderPath("XDG_VIDEOS_DIR", Environment.SpecialFolder.MyVideos)
        };

        return Ok(specialFolders);
    }

    private string? GetSpecialFolderPath(string xdgKey, Environment.SpecialFolder? specialFolder)
    {
        if (IsLinux())
        {
            // Linux下优先读取XDG配置
            var path = GetXdgUserDirectory(xdgKey);
            if (!string.IsNullOrEmpty(path))
                return path;

            // 如果XDG配置不存在，尝试用Environment.SpecialFolder（部分可能无效）
            if (specialFolder.HasValue)
            {
                path = Environment.GetFolderPath(specialFolder.Value);
                if (!string.IsNullOrEmpty(path))
                    return path;
            }

            // 退回到HOME目录的常见默认路径
            return GetDefaultLinuxPath(xdgKey);
        }

        // Windows直接用Environment.SpecialFolder
        if (specialFolder.HasValue)
        {
            var path = Environment.GetFolderPath(specialFolder.Value);
            if (!string.IsNullOrEmpty(path))
                return path;
        }
        else
        {
            // windows download 
            return GetKnownFolderPath(KnownFolderDownloads);
        }
        return null;
    }
    private static string? GetKnownFolderPath(Guid knownFolderId)
    {
        IntPtr outPath;
        var result = SHGetKnownFolderPath(knownFolderId, 0, IntPtr.Zero, out outPath);
        if (result >= 0)
        {
            var path = Marshal.PtrToStringUni(outPath);
            Marshal.FreeCoTaskMem(outPath);
            return path;
        }

        return null;
    }

    private bool IsLinux()
    {
        // 简单判断是否Linux
        return RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
    }

    private string? GetXdgUserDirectory(string key)
    {
        var home = Environment.GetEnvironmentVariable("HOME");
        if (string.IsNullOrEmpty(home))
            return null;

        var configPath = Path.Combine(home, ".config", "user-dirs.dirs");
        if (!System.IO.File.Exists(configPath))
            return null;
        var lines = System.IO.File.ReadAllLines(configPath);
        foreach (var line in lines)
        {
            if (!line.StartsWith(key)) continue;
            // 例如：XDG_DESKTOP_DIR="$HOME/Desktop"
            var match = Regex.Match(line, @"=\s*""\$(HOME)(.*)""");
            if (!match.Success) continue;
            var path = Path.Combine(home, match.Groups[2].Value.TrimStart('/'));
            return path;
        }
        return null;
    }

    private string? GetDefaultLinuxPath(string xdgKey)
    {
        var home = Environment.GetEnvironmentVariable("HOME");
        if (string.IsNullOrEmpty(home))
            return null;

        return xdgKey switch
        {
            "XDG_DESKTOP_DIR" => Path.Combine(home, "Desktop"),
            "XDG_DOCUMENTS_DIR" => Path.Combine(home, "Documents"),
            "XDG_DOWNLOAD_DIR" => Path.Combine(home, "Downloads"),
            "XDG_MUSIC_DIR" => Path.Combine(home, "Music"),
            "XDG_PICTURES_DIR" => Path.Combine(home, "Pictures"),
            "XDG_VIDEOS_DIR" => Path.Combine(home, "Videos"),
            _ => null
        };
    }
    // Downloads folder GUID
    private static readonly Guid KnownFolderDownloads = new Guid("374DE290-123F-4565-9164-39C4925E467B");

    [DllImport("shell32.dll")]
    private static extern int SHGetKnownFolderPath([MarshalAs(UnmanagedType.LPStruct)] Guid rfid, uint dwFlags, IntPtr hToken, out IntPtr ppszPath);

    [HttpGet("drivers")]
    public async Task<IActionResult> GetDriverInfo()
    {
        // 获取所有驱动器
        var drives = DriveInfo.GetDrives();

        var list = new List<DriverInfo>();

        foreach (var d in drives)
        {
            try
            {
                if (d.IsReady)
                {
                    var info = new DriverInfo
                    {
                        Name = d.Name,
                        DriveType = d.DriveType.ToString(),
                        Format = d.DriveFormat,
                        TotalSize = d.TotalSize,
                        AvailableFreeSpace = d.AvailableFreeSpace,
                        VolumeLabel = d.VolumeLabel
                    };
                    list.Add(info);
                }
                else
                {
                    // 未就绪的驱动器也可以返回一些基础信息
                    var info = new DriverInfo
                    {
                        Name = d.Name,
                        DriveType = d.DriveType.ToString(),
                        Format = null,
                        TotalSize = 0,
                        AvailableFreeSpace = 0,
                        VolumeLabel = ""
                    };
                    list.Add(info);
                }
            }
            catch (Exception ex)
            {
                // 捕获异常后仍然返回驱动器基本信息
                var info = new DriverInfo
                {
                    Name = d.Name,
                    DriveType = d.DriveType.ToString(),
                    Format = null,
                    TotalSize = 0,
                    AvailableFreeSpace = 0,
                    VolumeLabel = ""
                };
                list.Add(info);
            }
        }

        return Ok(list);
    }
    
    
    [HttpPost("entries")]
    public async Task<IActionResult> GetEntries(PathRequestBody requestBody)
    {
        try
        {
            return Ok(await fileManagerService.GetEntriesAsync(requestBody.Path));
        }
        catch (DirectoryNotFoundException e)
        {
            return NotFound(e.Message);
        }
    }

    [HttpPut("create-directory")]
    public async Task<IActionResult> CreateDirectoryAsync(CreateDirectoryRequestBody requestBody)
    {
        try
        {
            await fileManagerService.CreateDirectoryAsync(requestBody.Path, requestBody.DirectoryName);
            return Ok();
        }
        catch (DirectoryNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (IOException e)
        {
            return StatusCode(500, e.Message);
        }
    }

    [HttpDelete("delete-entry")]
    public async Task<IActionResult> DeleteEntry(PathRequestBody requestBody)
    {
        try
        {
            await fileManagerService.DeleteEntryAsync(requestBody.Path);
            return Ok();
        }
        catch (FileNotFoundException e)
        {
            return NotFound(e.Message);
        }
    }

    [HttpPost("rename-entry")]
    public async Task<IActionResult> RenameEntry(RenameRequestBody requestBody)
    {
        try
        {
            await fileManagerService.RenameEntryAsync(requestBody.Path, requestBody.NewName);
            return Ok();
        }
        catch (DirectoryNotFoundException e)
        {
            return NotFound(e.Message);
        }
        catch (ArgumentException e)
        {
            return BadRequest(e.Message);
        }
        catch (IOException e)
        {
            return StatusCode(500, e.Message);
        }
    }

    [HttpGet("download-file")]
    public async Task<IActionResult> DownloadFile(PathRequestBody requestBody)
    {
        try
        {
            var stream = await fileManagerService.DownloadFileAsync(requestBody.Path);
            return Ok(stream);
        }
        catch (FileNotFoundException e)
        {
            return NotFound(e.Message);
        }
    }

    [HttpPost("search-files")]
    public async Task<IActionResult> SearchFiles(SearchRequestBody requestBody)
    {
        try
        {
            var result = await fileManagerService.
                SearchAsync(requestBody.RootPath, requestBody.SearchPattern, requestBody.SearchChild);
            return Ok(result);
        }
        catch (DirectoryNotFoundException e)
        {
            return NotFound(e.Message);
        }
    }

    [HttpPost("get-attributes")]
    public async Task<IActionResult> GetAttributes(PathRequestBody requestBody)
    {
        try
        {
            var attributes = await fileManagerService.GetAttributesAsync(requestBody.Path);
            return Ok(attributes);
        }
        catch (FileNotFoundException e)
        {
            return NotFound(e.Message);
        }
    }
}