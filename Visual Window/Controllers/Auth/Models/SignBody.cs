using Newtonsoft.Json;

namespace Visual_Window.Controllers.Auth.Models;

public class SignBody
{

    [JsonProperty("username")]
    public string? Username { get; set; }

    [JsonProperty("password")]
    public string? Password { get; set; }
}