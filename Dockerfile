# Stage 1: Restore — copy project files and restore NuGet packages.
# Copying only .sln and .csproj files first maximises layer cache reuse:
# subsequent builds skip restore entirely if no project files changed.
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS restore
WORKDIR /src

COPY PhotoVideoBackupAPI.sln ./
COPY src/PhotoVideoBackupAPI.WebApi/PhotoVideoBackupAPI.WebApi.csproj \
     src/PhotoVideoBackupAPI.WebApi/
COPY src/PhotoVideoBackupAPI.Infrastructure/PhotoVideoBackupAPI.Infrastructure.csproj \
     src/PhotoVideoBackupAPI.Infrastructure/

RUN dotnet restore

# Stage 2: Build
FROM restore AS build
COPY src/ src/
RUN dotnet build src/PhotoVideoBackupAPI.WebApi/PhotoVideoBackupAPI.WebApi.csproj \
    -c Release --no-restore

# Stage 3: Publish
FROM build AS publish
RUN dotnet publish src/PhotoVideoBackupAPI.WebApi/PhotoVideoBackupAPI.WebApi.csproj \
    -c Release --no-restore --no-build \
    -o /app/publish

# Stage 4: Runtime — slim ASP.NET image, no SDK overhead
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Run as non-root for security
RUN addgroup --system appgroup && adduser --system --ingroup appgroup appuser

# Create log and media directories with correct ownership
RUN mkdir -p /app/logs /app/media && \
    chown -R appuser:appgroup /app

COPY --from=publish --chown=appuser:appgroup /app/publish .

USER appuser

# Port is controlled at runtime via ASPNETCORE_URLS env var in docker-compose
EXPOSE 8080

ENTRYPOINT ["dotnet", "PhotoVideoBackupAPI.WebApi.dll"]
