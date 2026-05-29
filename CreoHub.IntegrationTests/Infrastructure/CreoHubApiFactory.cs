using Amazon.S3;
using CreoHub.API.Controllers;
using CreoHub.Application.Pricing;
using CreoHub.Application.Services;
using CreoHub.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace CreoHub.IntegrationTests.Infrastructure;

public sealed class CreoHubApiFactory : WebApplicationFactory<AccountController>
{
    private readonly string _connectionString;

    public CreoHubApiFactory(string connectionString)
    {
        _connectionString = connectionString;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:AppDb"] = _connectionString,
                ["ConnectionStrings:GoogleClientId"] = "integration-test-client-id",
                ["ConnectionStrings:GoogleClientSecret"] = "integration-test-client-secret",
                ["ConnectionStrings:CloudflareStorageServiceURL"] = "https://example.test",
                ["ConnectionStrings:CloudflareStorageAccessKey"] = "integration-test-access-key",
                ["ConnectionStrings:CloudflareStorageSecretAccessKey"] = "integration-test-secret-key",
                ["Jwt:Issuer"] = "CreoHub.IntegrationTests",
                ["Jwt:Audience"] = "CreoHub.IntegrationTests",
                ["Jwt:Key"] = "integration-tests-secret-key-with-at-least-32-bytes",
                ["Frontend"] = "http://localhost:4321",
                ["Pricing:MinOvershoot"] = "1.2",
                ["Pricing:MaxOvershoot"] = "2.0",
                ["Pricing:CapN"] = "30",
            });
        });

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(_connectionString, npgsql =>
                    npgsql.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)));

            services.RemoveAll<IHostedService>();
            services.RemoveAll<IStorageService>();
            services.RemoveAll<IPaymentGatewayService>();
            services.RemoveAll<IAmazonS3>();

            services.AddSingleton<IStorageService, FakeStorageService>();
            services.AddSingleton<IPaymentGatewayService, FakePaymentGatewayService>();

            services.Configure<PricingConfig>(options =>
            {
                options.MinOvershoot = 1.2;
                options.MaxOvershoot = 2.0;
                options.CapN = 30;
            });

            services.AddAuthentication(TestAuthHandler.Scheme)
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                    TestAuthHandler.Scheme,
                    _ => { });

            services.PostConfigure<AuthenticationOptions>(options =>
            {
                options.DefaultAuthenticateScheme = TestAuthHandler.Scheme;
                options.DefaultChallengeScheme = TestAuthHandler.Scheme;
                options.DefaultScheme = TestAuthHandler.Scheme;
            });
        });
    }
}
