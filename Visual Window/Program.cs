using Visual_Window.Controllers.FileSystem.Services;
using Visual_Window.Controllers.FileSystem.Services.impl;
using Visual_Window.Controllers.Terminal.Services;
// export DOTNET_EnableWriteXorExecute=0
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
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();
app.MapFallbackToFile("{*path:nonfile}", "index.html");
app.MapControllers();
app.Run();