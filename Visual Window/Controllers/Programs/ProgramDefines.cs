using Visual_Window.Controllers.Programs.Models;

namespace Visual_Window.Controllers.Programs;

public class ProgramDefines
{
    public static List<ProgramConfig> GetProgramConfigs()
    {
        List<ProgramConfig> programConfigs =
        [
            new()
            {
                ProgramId = "file-explorer",
                ProgramName = "文件浏览器",
                ProgramType = AppType.SystemApp,
                Icon = new AppIcon()
                {
                    IconType = IconType.MaterialIcon,
                    Name = "folder"
                },
                IsSingleton = false,
                PreferredSize = new PreferredSize()
                {
                    Width = 1200,
                    Height = 800
                }
            },
            new()
            {
                ProgramId = "code-space",
                ProgramName = "Code Space",
                ProgramType = AppType.SystemApp,
                Icon = new AppIcon()
                {
                    IconType = IconType.AssetsIcon,
                    Name = "code-space.svg"
                },
                IsSingleton = false,
                PreferredSize = new PreferredSize()
                {
                    Width = 800,
                    Height = 600
                }
            },
            new()
            {
                ProgramId = "terminal",
                ProgramName = "终端",
                Stateful = true,
                ProgramType = AppType.SystemApp,
                Icon = new AppIcon()
                {
                    IconType = IconType.MaterialIcon,
                    Name = "terminal"
                },
                IsSingleton = false,
                PreferredSize = new PreferredSize()
                {
                    Width = 800,
                    Height = 600
                }
            },
            new()
            {
                ProgramId = "docker",
                ProgramName = "Docker 管理",
                ProgramType = AppType.NormalApp,
                Icon = new AppIcon()
                {
                    IconType = IconType.AssetsIcon,
                    Name = "docker.png"
                },
                IsSingleton = true,
                PreferredSize = new PreferredSize()
                {
                    Width = 800,
                    Height = 600
                }
            },
        ];
        return programConfigs;
    }
}