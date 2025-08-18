using Visual_Window.Models.Programs;

namespace Visual_Window.Services.VSystem;

public class ProgramDefines
{
    public static List<ProgramConfig> GetProgramConfigs()
    {
        List<ProgramConfig> programConfigs =
        [
            new()
            {
                AppId = "file-explorer",
                AppName = "文件浏览器",
                AppType = AppType.SystemApp,
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
                AppId = "terminal",
                AppName = "终端",
                Stateful = true,
                AppType = AppType.SystemApp,
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
                AppId = "docker",
                AppName = "Docker 管理",
                AppType = AppType.NormalApp,
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
            new()
            {
                AppId = "micro-window",
                AppName = "micro-window",
                AppType = AppType.NormalApp,
                Icon = new AppIcon()
                {
                    IconType = IconType.AssetsIcon,
                    Name = "micro.png"
                },
                IsSingleton = false,
                PreferredSize = new PreferredSize()
                {
                    Width = 800,
                    Height = 600
                }
            }
        ];
        return programConfigs;
    }
}