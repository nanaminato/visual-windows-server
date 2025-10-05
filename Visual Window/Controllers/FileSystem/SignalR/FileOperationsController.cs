using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Visual_Window.Controllers.FileSystem.SignalR.Models;

namespace Visual_Window.Controllers.FileSystem.SignalR;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/fileops")]
public class FileOperationsController : ControllerBase
{
    private readonly FileOperationManager _operationManager;

    public FileOperationsController(FileOperationManager operationManager)
    {
        _operationManager = operationManager;
    }

    [HttpPost("start")]
    public IActionResult StartOperation([FromBody] FileOperationRequest request)
    {
        if (_operationManager.TryStartOperation(request))
            return Ok(new { operationId = request.OperationId });
        return Conflict("操作ID已存在");
    }

    [HttpPost("cancel")]
    public IActionResult CancelOperation([FromQuery] string operationId)
    {
        if (_operationManager.CancelOperation(operationId))
            return Ok();
        return NotFound("找不到指定任务");
    }
}