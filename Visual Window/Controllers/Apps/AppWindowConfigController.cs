using Microsoft.AspNetCore.Mvc;
using Visual_Window.Repositories.VSystem;

namespace Visual_Window.VSystem;

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