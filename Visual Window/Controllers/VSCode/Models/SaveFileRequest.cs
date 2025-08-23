namespace Visual_Window.Controllers.VSCode.Models;

public class SaveFileRequest
{
    public string Path { get; set; }
    public string Content { get; set; }
    public string Encoding { get; set; }
    public string LineEnding { get; set; }
}