namespace Visual_Window.VSystem.FileIo;

public class NormalFileAttributes
{
    public long Size { get; set; }
    public DateTime CreatedTime { get; set; }
    public DateTime LastModifiedTime { get; set; }
    public bool IsReadOnly { get; set; }
    // 可根据需求添加更多属性
}