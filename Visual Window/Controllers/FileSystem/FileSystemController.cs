using Microsoft.AspNetCore.Mvc;
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