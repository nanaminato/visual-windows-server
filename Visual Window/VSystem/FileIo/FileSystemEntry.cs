namespace Visual_Window.VSystem.FileIo;

public class FileSystemEntry
{
    public string Name { get; set; }
    public string Path { get; set; }
    public bool IsDirectory { get; set; }
    public long Size { get; set; }
    public DateTime LastModified { get; set; }
}