using Visual_Window.Models.Apps;

namespace Visual_Window.Repositories.VSystem;

public class ProjectAppDefines
{
    public static List<AppWindowConfig> GetAppWindowConfigs()
    {
        List<AppWindowConfig> appWindowConfigs =
        [
            new()
            {
                AppId = "file-browser",
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
                    Width = 800,
                    Height = 600
                }
            },
            new()
            {
                AppId = "terminal",
                AppName = "终端",
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
        return appWindowConfigs;
    }
}