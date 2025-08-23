using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Mvc;
using Visual_Window.Controllers.FileSystem.Models;

namespace Visual_Window.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class SystemInfoController: Controller
{
    [HttpGet]
    public IActionResult GetSystemInfo()
    {
        var systemInfo = new SystemInfo();
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            systemInfo.Platform = OSPlatform.Windows.ToString();
        }else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            systemInfo.Platform = OSPlatform.Linux.ToString();
        }else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            systemInfo.Platform = OSPlatform.OSX.ToString();
        }else if (RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD))
        {
            systemInfo.Platform = OSPlatform.FreeBSD.ToString();
        }
        return Ok(systemInfo);
    }
}