using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Visual_Window.Controllers.Code.Models;
using Visual_Window.Controllers.FileSystem.RequestBodys;

namespace Visual_Window.Controllers.Code;

[ApiController]
[Authorize(Policy = "admin")]
[Route("api/v1/[controller]")]
public class CodeController: Controller
{
    [HttpPost]
    public IActionResult GetSubFiles(PathRequestBody pathRequestBody)
    {
        // 省略参数校验和安全检查
        var rootPath = string.IsNullOrEmpty(pathRequestBody.Path);
        if (rootPath)
        {
            return NotFound("File not found in RequestBody");
        }
        var result = GetDirectoryTree(pathRequestBody.Path);
        return Ok(result);
    }

    private List<FileNode> GetDirectoryTree(string path)
    {
        var list = new List<FileNode>();
        foreach (var dir in Directory.GetDirectories(path))
        {
            list.Add(new FileNode
            {
                Name = Path.GetFileName(dir),
                Path = dir,
                IsFolder = true,
                Children = GetDirectoryTree(dir)
            });
        }
        foreach (var file in Directory.GetFiles(path))
        {
            list.Add(new FileNode
            {
                Name = Path.GetFileName(file),
                Path = file,
                IsFolder = false,
            });
        }
        return list;
    }
    [HttpPost("open")]
    public IActionResult OpenFile(PathRequestBody pathRequestBody)
    {
        if (!System.IO.File.Exists(pathRequestBody.Path))
            return NotFound("文件不存在");

        var bytes = System.IO.File.ReadAllBytes(pathRequestBody.Path);

        var fileName = Path.GetFileName(pathRequestBody.Path);

        return Ok(new
        {
            path = pathRequestBody.Path,
            name = fileName,
            content = Convert.ToBase64String(bytes),
        });
    }

    
    [HttpPost("save")]
    public IActionResult SaveFile(SaveFileRequest request)
    {
        try
        {
            var bytes = Convert.FromBase64String(request.Content);

            System.IO.File.WriteAllBytes(request.Path, bytes);

            return Ok(new { success = true, message = "文件保存成功" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    private string DetectLineEnding(string content)
    {
        if (content.Contains("\r\n")) return "CRLF";
        if (content.Contains("\r")) return "CR";
        return "LF";
    }

    private string NormalizeLineEndings(string content, string lineEnding)
    {
        string newLine = lineEnding switch
        {
            "CRLF" => "\r\n",
            "CR" => "\r",
            _ => "\n"
        };
        return content.Replace("\r\n", "\n").Replace("\r", "\n").Replace("\n", newLine);
    }
}
