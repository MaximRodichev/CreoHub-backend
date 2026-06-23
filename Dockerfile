# ─────────────────────────────────────────────────────────────
#  CreoHub.API (включает Razor-библиотеку Creohub.AutoSlot)
#  Свой Dockerfile на актуальной .NET 9 базе (bookworm) — apt живой,
#  curl ставится; обходит сломанный авто-шаг хостинга на EOL-Debian.
# ─────────────────────────────────────────────────────────────

# ── build ──
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore CreoHub.API/CreoHub.API.csproj
RUN dotnet publish CreoHub.API/CreoHub.API.csproj -c Release -o /app/publish /p:UseAppHost=false

# ── runtime ──
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app

# curl + ca-certificates: на случай healthcheck'ов хостинга и https-запросов
RUN apt-get update \
 && apt-get install -y --no-install-recommends curl ca-certificates \
 && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

EXPOSE 8080
# Слушаем порт из $PORT (если хостинг его задаёт), иначе 8080
ENTRYPOINT ["sh", "-c", "ASPNETCORE_URLS=http://+:${PORT:-8080} dotnet CreoHub.API.dll"]
