using CreoHub.Application.Commands.AccountCommands;
using CreoHub.Application.Repositories;
using CreoHub.Infrastructure.Persistence;
using CreoHub.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CreoHub.Tests;

public static class Initializer
{
    public static ServiceCollection InitServices()
    {
        var services = new ServiceCollection();

        services.AddDbContext<AppDbContext>(opt =>
            opt.UseNpgsql("Host=localhost;Port=5432;Database=creohub_test;Username=postgres;Password=1234"));
        
        services.AddScoped<IAccountRepository, AccountRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IShopRepository, ShopRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<ITagRepository, TagRepository>();
        services.AddScoped<IPriceRepository, PriceRepository>();
        
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssemblyContaining<AuthAccountHandler>());
        
        services.AddLogging();

        services.AddAutoMapper(cfg =>
        {
        }, typeof(AuthAccountHandler)); // или Assembly Application

        return services;
    }
}