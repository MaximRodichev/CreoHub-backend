using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CreoHub.IntegrationTests.Infrastructure;

public sealed class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string Scheme = "IntegrationTest";
    public const string UserIdHeader = "X-Test-UserId";
    public const string UserNameHeader = "X-Test-UserName";
    public const string UserRoleHeader = "X-Test-UserRole";

    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(UserIdHeader, out var userIdValues))
            return Task.FromResult(AuthenticateResult.NoResult());

        var userId = userIdValues.ToString();
        if (string.IsNullOrWhiteSpace(userId) || !Guid.TryParse(userId, out _))
            return Task.FromResult(AuthenticateResult.Fail("Invalid test user id."));

        var userName = Request.Headers.TryGetValue(UserNameHeader, out var nameValues)
            ? nameValues.ToString()
            : "Integration Test User";

        var role = Request.Headers.TryGetValue(UserRoleHeader, out var roleValues)
            ? roleValues.ToString()
            : "User";

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new(ClaimTypes.Name, userName),
            new(ClaimTypes.Email, $"{userId}@integration.test"),
            new(ClaimTypes.Role, role),
        };

        var identity = new ClaimsIdentity(claims, Scheme);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
