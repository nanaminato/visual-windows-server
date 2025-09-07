using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Visual_Window.Controllers.FileSystem.Services;
using Visual_Window.Controllers.FileSystem.Services.impl;
using Visual_Window.Controllers.Terminal.Services;
using Visual_Window.Models;

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
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddControllersWithViews();
builder.Services.AddScoped<IFileManagerService, FileManagerService>();
builder.Services.AddSingleton<TerminalSessionManager>();
builder.Services.AddControllers().AddNewtonsoftJson(options =>
{
    options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
});
builder.Services.AddSignalR();
builder.Services.AddSwaggerGen();

var jswSettings = builder.Configuration.GetSection("Jwt").Get<JwtSettings>();
var secretKey = Encoding.UTF8.GetBytes(jswSettings!.SecretKey!);
builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jswSettings.Issuer,
            ValidAudience = jswSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(secretKey)
        };
        options.Events = new JwtBearerEvents();
    });
builder.Services.TryAddEnumerable(
    ServiceDescriptor.Singleton<IPostConfigureOptions<JwtBearerOptions>,
        ConfigureJwtBearerOptions>());

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("admin", policy => policy.RequireRole(["admin"]));
    options.AddPolicy("user", policy => policy.RequireRole(["user"]));
});

var app = builder.Build();
app.UseWebSockets();
app.UseCors("CorsPolicy");


if (!app.Environment.IsDevelopment())
{
    
    app.UseHsts();
}
else
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
        // 你可以自定义 Swagger UI 的路径和其他设置
    });
}

// app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();
app.MapFallbackToFile("{*path:nonfile}", "index.html");
app.MapControllers();
app.Run();