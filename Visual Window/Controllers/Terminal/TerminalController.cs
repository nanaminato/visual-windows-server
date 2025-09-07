using System.IdentityModel.Tokens.Jwt;
using System.Runtime.InteropServices;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Visual_Window.Controllers.Terminal.Models;
using Visual_Window.Controllers.Terminal.Services;

namespace Visual_Window.Controllers.Terminal;

[ApiController]
[Route("api/v1/[controller]")]
public class TerminalController: Controller
{
    private readonly TerminalSessionManager _manager;
    private readonly string? _issuer;
    private readonly string? _audience;
    private readonly byte[] _key;
    public TerminalController(TerminalSessionManager manager,IConfiguration configuration)
    {
        _manager = manager;
        _issuer = configuration["Jwt:Issuer"];
        _audience = configuration["Jwt:Audience"];
        _key = System.Text.Encoding.UTF8.GetBytes(configuration["Jwt:SecretKey"]??"");
    }
    [Authorize(Policy = "admin")]
    [HttpPost("")]
    public async Task<IActionResult> CreateTerminal(TerminalCreateOptions options)
    {
        var session = await _manager.CreateSession(options);
        return Ok(new 
        {
            Id = session.Id
        });
    }
    [Authorize(Policy = "admin")]
    [HttpDelete("{sessionId}")]
    public async Task<IActionResult> CloseSession(string sessionId)
    {
        _manager.CloseSession(sessionId);
        return Ok();
    }
    [HttpGet("{id}")]
    public async Task GetTerminalWebSocket(string id)
    {
        if (!HttpContext.WebSockets.IsWebSocketRequest)
        {
            HttpContext.Response.StatusCode = 400;
            return;
        }

        // 1. 从查询参数中获取 token
        var token = HttpContext.Request.Query["token"].FirstOrDefault();
        if (string.IsNullOrEmpty(token))
        {
            HttpContext.Response.StatusCode = 401;
            await HttpContext.Response.WriteAsync("Token is missing.");
            return;
        }

        // 2. 验证 token，这里需要用你应用里面的 token 验证逻辑
        var principal = ValidateToken(token);
        if (principal == null)
        {
            HttpContext.Response.StatusCode = 401;
            await HttpContext.Response.WriteAsync("Invalid token.");
            return;
        }

        // 3. 检查权限（是否满足 admin 策略）
        var isAdmin = principal.IsInRole("admin") || principal.HasClaim(c => c.Type == ClaimTypes.Role && c.Value == "admin");
        if (!isAdmin)
        {
            HttpContext.Response.StatusCode = 403;
            await HttpContext.Response.WriteAsync("No permission.");
            return;
        }

        // 4. 继续处理会话
        if (!_manager.TryGetSession(id, out var session))
        {
            HttpContext.Response.StatusCode = 404;
            return;
        }

        var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
    
        // 5. 启动终端会话
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            await session.StartTerminal(webSocket);
        }
        else
        {
            await session.StartLinux(webSocket);
        }
    }

    private ClaimsPrincipal? ValidateToken(string token)
    {
        var tokenHandler = new JwtSecurityTokenHandler();

        try
        {
            var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(_key),

                ValidateIssuer = true,
                ValidIssuer = _issuer,

                ValidateAudience = true,
                ValidAudience = _audience,

                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(5)

            }, out _);

            return principal;
        }
        catch
        {
            return null;
        }
    }
}