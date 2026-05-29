using System.Net;
using CreoHub.IntegrationTests.Infrastructure;

namespace CreoHub.IntegrationTests;

public sealed class ApiSmokeTests : IntegrationTestBase
{
    public ApiSmokeTests(IntegrationTestFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task Root_returns_api_marker()
    {
        using var client = Fixture.CreateClient();

        var response = await client.GetAsync("/");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("\"CreoHub API\"", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Profile_without_auth_returns_unauthorized()
    {
        using var client = Fixture.CreateClient();

        var response = await client.GetAsync("/api/account/profile");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Profile_with_seeded_user_reads_from_postgres()
    {
        var user = await DatabaseSeeder.CreateUserAsync(Fixture.Services);
        using var client = Fixture.CreateClient(user.Id);

        var response = await client.GetAsync("/api/account/profile");
        using var json = await JsonAssert.ReadJsonAsync(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(0, json.RootElement.RequiredProperty("status").GetInt32());
        Assert.Equal(user.Id, json.RootElement.RequiredProperty("data").RequiredProperty("id").GetGuid());
    }
}
