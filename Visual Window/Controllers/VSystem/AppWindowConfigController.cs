using Microsoft.AspNetCore.Mvc;
using WebApplication1.Repositories.VSystem;

namespace WebApplication1.Controllers.VSystem;

[ApiController]
[Route("api/v1/[controller]")]
public class AppWindowConfigController: Controller
{
    public AppWindowConfigController()
    {
        
    }

    [HttpGet("installed-apps")]
    public IActionResult GetAllInstalledApps()
    {
        return Ok(ProjectAppDefines.GetAppWindowConfigs());
    }
}