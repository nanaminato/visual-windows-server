namespace Visual_Window.Controllers.FileSystem.Models;

public class DriverInfo
{
    public string Name { get; set; }         // 驱动器名称，比如 "C:\"
    public string DriveType { get; set; }    // 驱动器类型，比如 Fixed, Removable, Network 等
    public string? Format { get; set; }       // 文件系统格式，比如 NTFS, FAT32
    public long TotalSize { get; set; }      // 总容量，字节
    public long AvailableFreeSpace { get; set; } // 可用空间，字节
    public string? VolumeLabel { get; set; }  // 卷标
}