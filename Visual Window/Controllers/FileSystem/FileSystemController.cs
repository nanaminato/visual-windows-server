using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Visual_Window.Controllers.FileSystem.RequestBodys;
using Visual_Window.Controllers.FileSystem.Services;

namespace Visual_Window.Controllers.FileSystem;

[ApiController]
[Authorize(Policy = "admin")]
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
        var roots = DirectoriesProvider.GetRootDirectories();
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
        var specialFolders = DirectoriesProvider.GetSpecialFolders();
        return Ok(specialFolders);
    }

    [HttpGet("drivers")]
    public async Task<IActionResult> GetDriverInfo()
    {
        var list = DirectoriesProvider.GetDrives();
        return Ok(list);
    }
    
    [HttpPost("entries")]
    public async Task<IActionResult> GetEntries(PathRequestBody requestBody)
    {
        try
        {
            var files = await fileManagerService.GetEntriesAsync(requestBody.Path);
            return Ok(files);
        }
        catch (DirectoryNotFoundException e)
        {
            return NotFound(e.Message);
        }
        catch (UnauthorizedAccessException e)
        {
            return Forbid(e.Message); // 或者返回 StatusCode(403, e.Message);
        }
        catch (Exception e)
        {
            // 其他未预期错误
            return StatusCode(500, $"服务器内部错误: {e.Message}");
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
        catch (Exception e)
        {
            return BadRequest(e.Message);
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

    [HttpPost("download")]
    public async Task<IActionResult> getFile(PathRequestBody requestBody)
    {
        try
        {
            var stream = await fileManagerService.DownloadFileAsync(requestBody.Path);
            var contentType = "application/octet-stream"; // 根据实际情况设置
            var fileName = Path.GetFileName(requestBody.Path);

            // 返回一个文件流，浏览器会弹出下载窗口（前提是请求环境支持）
            return File(stream, contentType, fileName);
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