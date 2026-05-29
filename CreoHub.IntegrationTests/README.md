# CreoHub integration tests

These tests run the real `CreoHub.API` pipeline against a temporary PostgreSQL
database managed by Testcontainers.

Prerequisites:

- Docker Desktop is installed and running.
- .NET 9 SDK is installed and available as `dotnet`.

Run:

```powershell
dotnet test .\CreoHub.IntegrationTests\CreoHub.IntegrationTests.csproj
```

The test fixture:

- starts `postgres:16-alpine`;
- applies EF Core migrations;
- resets data between tests with Respawn;
- replaces auth, storage, payment gateway, and hosted background services with
  test doubles;
- creates an empty local `ffmpeg` directory so API startup does not download
  FFmpeg during tests.
