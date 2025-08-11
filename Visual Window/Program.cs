using Visual_Window.Controllers.Terminal.Services;
using Visual_Window.VSystem.FileIo;
using Visual_Window.VSystem.FileIo.impl;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddCors(options => options.AddPolicy("CorsPolicy",
    set =>
    {
        set.SetIsOriginAllowed(origin => true)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    }));
// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddScoped<IFileManagerService, FileManagerService>();
builder.Services.AddSingleton<TerminalSessionManager>();
var app = builder.Build();
app.UseWebSockets();
app.UseCors("CorsPolicy");
// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

// app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();