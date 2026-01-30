using CreoHub.API.DI;
using CreoHub.API.Models;
using CreoHub.AssetsGrabber;
using CreoHub.AssetsGrabber.Extensitions;
using CreoHub.AssetsGrabber.Grabbers;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
//builder.Services.AddOpenApi();

string rootPath = builder.Environment.ContentRootPath;

builder.Services.AddApplicationServices(builder.Configuration);
builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
        options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    })
    .AddCookie() // Куки нужны для сохранения сессии после входа
    .AddGoogle(options =>
    {
        options.ClientId = builder.Configuration.GetConnectionString("GoogleClientId");
        options.ClientSecret = builder.Configuration.GetConnectionString("GoogleClientSecret");
        ;
        options.CallbackPath = "/signin-google"; // Должен совпадать с тем, что в Google Console
    });
/*
builder.Services.AddScoped<WebAssetScout>();
builder.Services.AddScoped<HacksawGrabber>(sp => 
{
    var scout = sp.GetRequiredService<WebAssetScout>();
    var env = sp.GetRequiredService<IWebHostEnvironment>();
    
    return new HacksawGrabber(
        scout, 
        env.ContentRootPath + "/wwwroot/", 
        "http://localhost:5242"
    );
});*/

// Добавляем политику CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAstro", policy =>
    {
        policy.WithOrigins("http://localhost:4321") // Адрес вашего Astro
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials(); // Обязательно для работы с авторизацией
    });
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();
/*
var provider = new FileExtensionContentTypeProvider();
// Добавляем поддержку атласов и других игровых файлов
provider.Mappings[".atlas"] = "text/plain"; 
provider.Mappings[".json"] = "application/json";*/
app.UseCors("AllowAstro");
/*
app.UseStaticFiles(new StaticFileOptions
{
    ContentTypeProvider = provider
});*/
/*
app.MapPost("/api/grab", async (AssetGrabRequest request, HacksawGrabber grabber) =>
    {
        if (string.IsNullOrWhiteSpace(request.Url))
        {
            return Results.BadRequest(new { error = "URL is required" });
        }

        try
        {
            var result = await grabber.GetResultBundleFromUrl(request.Url);

            return Results.Ok(result);
        }
        catch (Exception ex)
        {
            return Results.Problem(ex.Message);
        }
    })
    .WithName("GrabAssets");
    //.WithOpenApi();*/
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    //app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.Run();
