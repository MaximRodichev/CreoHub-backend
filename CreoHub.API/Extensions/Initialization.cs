using System.Text;
using System.Threading.Channels;
using Amazon.S3;
using CreoHub.API.Services;
using CreoHub.Application.Commands.AccountCommands;
using CreoHub.Application.Pricing;
using CreoHub.Application.Repositories;
using CreoHub.Application.Services;
using CreoHub.Domain.Entities;
using CreoHub.Infrastructure.Persistence;
using CreoHub.Infrastructure.Persistence.Repositories;
using CreoHub.Infrastructure.Persistence.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace CreoHub.API.DI;

public static class Initialization
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Используем строку подключения из конфигурации (appsettings.json)
        services.AddDbContext<AppDbContext>(opt =>
            opt.UseNpgsql(configuration.GetConnectionString("AppDb"), o =>
            {
                o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
            }));
        
        var r2Config = new AmazonS3Config
        {
            // Твой Endpoint из панели Cloudflare (Account ID)
            ServiceURL = configuration.GetConnectionString("CloudflareStorageServiceURL"),
            // Важно для R2: подпись должна быть совместимой
            ForcePathStyle = true 
        };
        
        services.AddSingleton<IAmazonS3>(sp => 
        {
            return new AmazonS3Client(
                configuration.GetConnectionString("CloudflareStorageAccessKey"), 
                configuration.GetConnectionString("CloudflareStorageSecretAccessKey"), 
                r2Config);
        });
        services.AddSingleton<Channel<Guid>>(Channel.CreateUnbounded<Guid>());
        services.AddSingleton<IVideoOptimizationQueueService, VideoOptimizationQueueService>();
        services.AddSingleton<IOptimizationProgressService, OptimizationProgressService>();

        // ── Event tracking ──────────────────────────────────────────────────
        services.AddSingleton<Channel<UserEvent>>(
            Channel.CreateUnbounded<UserEvent>(
                new UnboundedChannelOptions { SingleReader = true }));
        services.AddSingleton<IEventTracker, EventTrackerService>();

        // ── Notification services ───────────────────────────────────────────
        // TelegramNotificationService is Transient (HttpClient lifecycle managed by AddHttpClient)
        services.AddHttpClient<TelegramNotificationService>();
        services.AddScoped<SmtpNotificationService>();
        services.AddScoped<INotificationService, CompositeNotificationService>();
        services.AddScoped<IInAppNotificationRepository, InAppNotificationRepository>();
        services.AddScoped<IPendingUploadRepository, PendingUploadRepository>();
        services.AddScoped<IShopRequestRepository, ShopRequestRepository>();
        services.AddScoped<IShopFollowRepository, ShopFollowRepository>();


        services.AddScoped<IStorageService, R2StorageService>();
        // Репозитории
        services.AddScoped<IAccountRepository, AccountRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IShopRepository, ShopRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<ITagRepository, TagRepository>();
        services.AddScoped<IPriceRepository, PriceRepository>();
        services.AddScoped<IProductBundleRepository, ProductBundleRepository>();
        services.AddScoped<IStorageService, R2StorageService>();
        services.AddScoped<IStorageObjectRepository, StorageObjectRepository>();
        services.AddScoped<IMediaProductRepository, MediaProductRepository>();
        services.AddScoped<IContentFileRepository, ContentFileRepository>();
        services.AddScoped<IContentAccessRepository, ContentAccessRepository>();
        services.AddScoped<IUserTransactionRepository, UserTransactionRepository>();
        services.AddScoped<IShopTransactionRepository, ShopTransactionRepository>();
        services.AddScoped<IUserBalanceRepository, UserBalanceRepository>();
        services.AddScoped<IShopBalanceRepository, ShopBalanceRepository>();
        services.AddScoped<ICartRepository, CartRepository>();
        services.AddScoped<ISubscriptionPromoCodeRepository, SubscriptionPromoCodeRepository>();
        services.AddScoped<IAdminRepository, AdminRepository>();
        services.AddScoped<IProductStatusLogRepository, ProductStatusLogRepository>();
        services.AddScoped<IProductEditHistoryRepository, ProductEditHistoryRepository>();
        services.AddScoped<IUserEventRepository, UserEventRepository>();
        services.AddScoped<IBroadcastJobRepository, BroadcastJobRepository>();

        services.AddScoped<IStorageObjectRepository, StorageObjectRepository>();
        services.AddScoped<IVideoConversionService, VideoConversionService>();
        services.AddScoped<IThumbnailGenerationService, ThumbnailGenerationService>();
        services.AddScoped<ITelegramAuthVerifier, TelegramAuthVerifier>();
        
        
        services.AddScoped<JwtService>();
        
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        
        services.AddHostedService<VideoOptimizationBackgroundService>();
        services.AddHostedService<EventTrackerBackgroundService>();
        services.AddHostedService<ExpiredTransactionCleanupService>();
        services.AddHostedService<OrderCleanupService>();
        services.AddHostedService<BroadcastBackgroundService>();
        services.AddHostedService<OrphanedStorageCleanupService>();
        services.AddHostedService<ThumbnailBackfillBackgroundService>();
        
        // MediatR
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssemblyContaining<AuthAccountHandler>());
        
        services.Configure<PricingConfig>(
            configuration.GetSection(PricingConfig.SectionName));

        services.AddLogging();

        // AutoMapper
        services.AddAutoMapper(cfg => 
        {
            // Ваши настройки здесь
        }, new Type[] { typeof(AuthAccountHandler) });
        
        
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = configuration["Jwt:Issuer"],
                    ValidAudience = configuration["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Key"]))
                };
                /*
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        // Название куки, в которую ты записал JWT
                        var accessToken = context.Request.Cookies["jwt_token"];

                        if (!string.IsNullOrEmpty(accessToken))
                        {
                            context.Token = accessToken;
                        }
                        return Task.CompletedTask;
                    }
                };*/
            });

        services.AddAuthorization(options =>
        {
            options.AddPolicy("Admin", policy =>
                policy.RequireRole("Admin"));
        });

        services.AddHttpClient<IPaymentGatewayService, OxaPayService>();
        
        return services;
    }
}