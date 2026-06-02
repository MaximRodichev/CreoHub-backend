using System.Threading.RateLimiting;
using Creohub.AutoSlot;
using CreoHub.API.Configuration;
using CreoHub.API.DI;
using CreoHub.API.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Serilog;
using Xabe.FFmpeg;
using Xabe.FFmpeg.Downloader;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    // Отключаем шум от EF Core и ASP.NET
    .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", Serilog.Events.LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate: 
        "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
//builder.Services.AddOpenApi();

string rootPath = builder.Environment.ContentRootPath;

builder.Services.AddApplicationServices(builder.Configuration);
builder.Services.AddAutoSlot();
builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
        options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    })
    .AddCookie() // Куки нужны для сохранения сессии после входа
    .AddGoogle(options =>
    {
        options.ClientId     = builder.Configuration.GetConnectionString("GoogleClientId");
        options.ClientSecret = builder.Configuration.GetConnectionString("GoogleClientSecret");
        options.CallbackPath = "/signin-google"; // Должен совпадать с тем, что в Google Console

        // Correlation cookie должен шариться между api.creohub.xyz (сюда приходит callback)
        // и creohub.xyz (frontend-proxy ставит cookie браузеру). Без общего домена
        // ASP.NET не находит cookie на callback → OAuth state не проходит, линковка ломается.
        var corrDomain = builder.Configuration["CookieDomain"];
        if (!string.IsNullOrWhiteSpace(corrDomain))
            options.CorrelationCookie.Domain = corrDomain;

        // Если пользователь отменяет Google OAuth — редиректим тихо, без страницы ошибки
        options.Events.OnRemoteFailure = ctx =>
        {
            var frontend = builder.Configuration["Frontend"] ?? "";
            ctx.Response.Redirect($"{frontend}/signin?cancelled=1");
            ctx.HandleResponse();
            return Task.CompletedTask;
        };
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

var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>()
    ?? [builder.Configuration["Frontend"]!];

builder.Services.AddCors(options =>
{
    // Основной маркетплейс — Frontend из конфига (+ dev origins из AllowedOrigins)
    options.AddPolicy("AllowAstro", policy =>
    {
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });

    // AutoSlot панель — "null" для CEF file:// origin
    // Применяется только к /autoslot/* контроллерам через [EnableCors("AllowPanel")]
    options.AddPolicy("AllowPanel", policy =>
    {
        policy.AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddControllersWithViews();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ── Rate limiting ─────────────────────────────────────────────────────────────
// Защита auth эндпоинтов от брутфорса: 10 запросов в минуту с одного IP.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddPolicy("auth", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit          = 10,
                Window               = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit           = 0,
            }));
});
builder.WebHost.ConfigureKestrel((context, options) =>
{
    options.Limits.MaxRequestBodySize = FileLimits.MaxUpload;
});
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = FileLimits.MaxUpload;
});
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});
var app = builder.Build();
/*
app.Use((context, next) =>
{
    context.Request.Scheme = "https";
    return next();
});
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});
*/

app.UseSerilogRequestLogging();
app.UseStaticFiles(); // обслуживает /_content/Creohub.AutoSlot/ и wwwroot
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
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.UseAutoSlot();

app.UseMiddleware<SerilogUserActivityMiddleware>();

app.MapGet("/", () => Results.Ok("CreoHub API"));

app.MapControllers();
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.MapSwagger();
}

var ffmpegPath = Path.Combine(Directory.GetCurrentDirectory(), "ffmpeg");
if (!Directory.Exists(ffmpegPath))
{
    Directory.CreateDirectory(ffmpegPath);
    await FFmpegDownloader.GetLatestVersion(FFmpegVersion.Official, ffmpegPath);
}
FFmpeg.SetExecutablesPath(ffmpegPath);

app.Run();
