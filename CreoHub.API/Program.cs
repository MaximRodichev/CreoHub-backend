using CreoHub.API.Models;
using CreoHub.AssetsGrabber;
using Microsoft.AspNetCore.StaticFiles;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddScoped<WebAssetScout>();
builder.Services.AddScoped<HacksawGrabber>();

// Добавляем политику CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()   // Разрешить запросы с любого сайта
            .AllowAnyMethod()   // Разрешить GET, POST, PUT и т.д.
            .AllowAnyHeader();  // Разрешить любые заголовки (Content-Type и т.д.)
    });
});

var app = builder.Build();
var provider = new FileExtensionContentTypeProvider();
// Добавляем поддержку атласов и других игровых файлов
provider.Mappings[".atlas"] = "text/plain"; 
provider.Mappings[".json"] = "application/json";
app.UseCors("AllowAll");
app.UseStaticFiles(new StaticFileOptions
{
    ContentTypeProvider = provider
});

app.MapPost("/api/grab", async (AssetGrabRequest request, HacksawGrabber grabber, IWebHostEnvironment env) =>
    {
        if (string.IsNullOrWhiteSpace(request.Url))
        {
            return Results.BadRequest(new { error = "URL is required" });
        }

        try
        {   
            var _basePath = env.WebRootPath;
            var result = await grabber.CreateBundleSpineItemList(request.Url, _basePath, "hacksaw");
            
            return Results.Ok(result);
        }
        catch (Exception ex)
        {
            return Results.Problem(ex.Message);
        }
    })
    .WithName("GrabAssets")
    .WithOpenApi();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.Run();
