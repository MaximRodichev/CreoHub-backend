using System.Text;
using CreoHub.Application.Commands.AccountCommands;
using CreoHub.Application.Repositories;
using CreoHub.Infrastructure.Persistence;
using CreoHub.Infrastructure.Persistence.Repositories;
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
            opt.UseNpgsql(configuration.GetConnectionString("AppDb")));
        
        // Репозитории
        services.AddScoped<IAccountRepository, AccountRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IShopRepository, ShopRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<ITagRepository, TagRepository>();
        services.AddScoped<IPriceRepository, PriceRepository>();

        services.AddScoped<JwtService>();
        
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // MediatR
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssemblyContaining<AuthAccountHandler>());
        
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

        return services;
    }
}