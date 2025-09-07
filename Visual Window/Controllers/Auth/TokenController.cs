using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Visual_Window.Controllers.Auth;
[ApiController]
[Route("api/[controller]")]
public class TokenController: Controller
{
    [HttpPost("check")]
    [Authorize(Policy = "admin")]
    
    public Task<IActionResult> CheckToken()
    {
        return Task.FromResult<IActionResult>(Ok());
    }
}