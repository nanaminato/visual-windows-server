using System.Runtime.InteropServices;

namespace Visual_Window.VSystem.FileIo.Utils;

public class RootDirectoriesProvider
{
    public static IEnumerable<string> GetRootDirectories()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            foreach (var drive in DriveInfo.GetDrives())
            {
                yield return drive.Name;
            }
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            yield return "/";
        }
        else
        {
            throw new PlatformNotSupportedException("Unsupported OS platform");
        }
    }
}