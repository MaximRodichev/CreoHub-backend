namespace CreoHub.IntegrationTests.Infrastructure;

[Collection(IntegrationTestCollection.Name)]
public abstract class IntegrationTestBase : IAsyncLifetime
{
    protected IntegrationTestFixture Fixture { get; }

    protected IntegrationTestBase(IntegrationTestFixture fixture)
    {
        Fixture = fixture;
    }

    public virtual Task InitializeAsync()
        => Fixture.ResetDatabaseAsync();

    public virtual Task DisposeAsync()
        => Task.CompletedTask;
}
