namespace Visual_Window.Models.Apps;

public class AppWindowConfig
{
    public string AppId
    {
        get;
        set;
    }

    public string AppName
    {
        get;
        set;
    }

    public AppIcon Icon
    {
        get;
        set;
    }

    public bool IsSingleton
    {
        get;
        set;
    }

    public PreferredSize PreferredSize
    {
        get;
        set;
    }

    public AppType AppType
    {
        get;
        set;
    }
}

public class AppIcon
{
    public IconType IconType
    {
        get;
        set;
    }

    public string Name
    {
        get;
        set;
    }
}

public enum IconType
{
    MaterialIcon = 1,// angular material 框架icon
    LinkIcon = 2,//外部链接icon
    AssetsIcon = 3,//项目资源icon
    ServerIcon = 4,//第三方app，指定之后，后端将图片放置到指定位置
}

public enum AppType
{
    SystemApp = 1,// 系统程序
    NormalApp = 2,// 集成到本项目的程序
    WebApp = 3,// 其他项目的程序，通过frame访问
}

public class PreferredSize
{
    public double Width
    {
        get;
        set;
    }

    public double Height
    {
        get;
        set;
    }
}