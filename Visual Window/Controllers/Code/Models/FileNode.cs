namespace Visual_Window.Controllers.Code.Models;

public class FileNode
{
    public string Name { get; set; }
    public string Path { get; set; }
    public bool IsFolder { get; set; } // "file" or "folder"
    public List<FileNode>? Children { get; set; }
}