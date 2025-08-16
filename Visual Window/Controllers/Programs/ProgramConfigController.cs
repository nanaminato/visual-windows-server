using Microsoft.AspNetCore.Mvc;
using Visual_Window.Services.VSystem;

namespace Visual_Window.Controllers.Programs;

[ApiController]
[Route("api/v1/[controller]")]
public class ProgramConfigController: Controller
{
    public ProgramConfigController()
    {
        
    }

    [HttpGet("installed-apps")]
    public IActionResult GetAllInstalledApps()
    {
        return Ok(ProgramDefines.GetProgramConfigs());
    }
}