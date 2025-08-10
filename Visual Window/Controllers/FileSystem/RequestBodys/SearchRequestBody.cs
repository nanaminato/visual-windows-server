namespace Visual_Window.Controllers.FileSystem.RequestBodys;

public class SearchRequestBody
{
    public string RootPath { get; set; }
    public string SearchPattern { get; set; }
    public bool SearchChild { get; set; }
}