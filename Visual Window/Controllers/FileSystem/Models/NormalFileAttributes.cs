namespace Visual_Window.Controllers.FileSystem.Models;

public class NormalFileAttributes
{
    public long Size { get; set; }
    public DateTime CreatedTime { get; set; }
    public DateTime LastModifiedTime { get; set; }
    public bool IsReadOnly { get; set; }
    
}