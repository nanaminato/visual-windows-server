namespace Visual_Window.Controllers.FileSystem.Models;

public class LightFile
{
    public string Name { get; set; }
    public string Path { get; set; }
    public bool IsDirectory { get; set; }
    public long Size { get; set; }
    public DateTime LastModified { get; set; }
}