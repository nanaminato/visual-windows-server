using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Visual_Window.Controllers.Auth.Models;

namespace Visual_Window.Controllers.Auth;

[ApiController]
[Route("api/[controller]")]
public class AuthController(IConfiguration configuration) : Controller
{
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] SignBody body)
    {
        var username = body.Username;
        var password = body.Password;
        var res = await IsValidUser(username!, password!);
        if (res.Item1)
        {
            var token = GenerateToken(username!, res.Item2, res.Item3);
            return Ok(new { Token = token, Id = res.Item2, Role = res.Item3 });
        }

        return Unauthorized();
    }

    private async Task<(bool, int, string)> IsValidUser(string username, string password)
    {
        var users = configuration.GetSection("users").Get<List<User>>();

        if (users == null)
            return (false, 0, string.Empty);

        var user = users.FirstOrDefault(u => u.Username == username && u.Password == password);

        if (user == null)
            return (false, 0, string.Empty);

        if (user.Role != "admin" && user.Role != "user")
            return (false, 0, string.Empty);

        return (true, user.Id, user.Role);
    }

    private string GenerateToken(string username, int id, string role)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:SecretKey"]!));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, username),
            new(ClaimTypes.NameIdentifier, id.ToString()),
            new(ClaimTypes.Role, role)
        };

        var token = new JwtSecurityToken(
            issuer: configuration["Jwt:Issuer"],
            audience: configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.Now.AddDays(30),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private new class User
    {
        public string Username { get; set; } = null!;
        public string Password { get; set; } = null!;
        public int Id { get; set; }
        public string Role { get; set; } = null!;
    }
}