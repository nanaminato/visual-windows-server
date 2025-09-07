using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Visual_Window.Controllers.Programs;

[ApiController]
[Route("api/v1/[controller]")]
public class ProgramConfigController: Controller
{

    [HttpGet("installed-apps")]
    public IActionResult GetAllInstalledApps()
    {
        return Ok(ProgramDefines.GetProgramConfigs());
    }
}