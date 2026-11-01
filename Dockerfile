# BUILD_CONFIGURATION is passed by Rider when launching in Debug mode (default: Release).
ARG BUILD_CONFIGURATION=Release

# Stage 1: Restore — copy project files and restore NuGet packages.
# Copying only .sln and .csproj files first maximises layer cache reuse:
# subsequent builds skip restore entirely if no project files changed.
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS restore
WORKDIR /src

COPY PixNestAPI.sln ./
COPY src/PixNestAPI.Domain/PixNestAPI.Domain.csproj \
     src/PixNestAPI.Domain/
COPY src/PixNestAPI.Application/PixNestAPI.Application.csproj \
     src/PixNestAPI.Application/
COPY src/PixNestAPI.WebApi/PixNestAPI.WebApi.csproj \
     src/PixNestAPI.WebApi/
COPY src/PixNestAPI.Infrastructure/PixNestAPI.Infrastructure.csproj \
     src/PixNestAPI.Infrastructure/

# Restore only the WebApi project (and its transitive project refs), not the whole
# solution — the solution includes the test project, which is intentionally not
# copied into the production image.
RUN dotnet restore src/PixNestAPI.WebApi/PixNestAPI.WebApi.csproj

# Stage 2: Build
FROM restore AS build
ARG BUILD_CONFIGURATION
COPY src/ src/
RUN dotnet build src/PixNestAPI.WebApi/PixNestAPI.WebApi.csproj \
    -c ${BUILD_CONFIGURATION} --no-restore

# Stage 3: Publish
FROM build AS publish
ARG BUILD_CONFIGURATION
RUN dotnet publish src/PixNestAPI.WebApi/PixNestAPI.WebApi.csproj \
    -c ${BUILD_CONFIGURATION} --no-restore --no-build \
    -o /app/publish

# Stage 4: Runtime — slim ASP.NET image, no SDK overhead
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Run as non-root for security
RUN addgroup --system appgroup && adduser --system --ingroup appgroup appuser

# curl exists solely for the HEALTHCHECK below. The aspnet:8.0 base is Debian 12 and
# ships NEITHER curl nor wget, so without this the health check can never pass and the
# container is reported unhealthy forever. Installed before USER appuser (apt needs
# root); curl itself runs fine unprivileged. ~10 MB on a 350 MB base.
RUN apt-get update \
 && apt-get install -y --no-install-recommends curl \
 && rm -rf /var/lib/apt/lists/*

# Create log, media and Data Protection directories with correct ownership.
# The Data Protection path MUST exist in the image: docker-compose mounts a named volume
# there, and Docker creates a missing mount point as root:root — which appuser (uid 100)
# cannot write, so the key ring fails on every start with UnauthorizedAccessException.
# Creating it here means Docker initialises the fresh volume with appuser ownership.
RUN mkdir -p /app/logs /app/media /app/.aspnet/DataProtection-Keys && \
    chown -R appuser:appgroup /app

COPY --from=publish --chown=appuser:appgroup /app/publish .

USER appuser

# Port is controlled at runtime via ASPNETCORE_URLS env var in docker-compose
EXPOSE 8080

# Probes the same /health endpoint Caddy forwards. start-period covers
# migration-on-startup, which retries up to 5 times before the app serves traffic.
HEALTHCHECK --interval=30s --timeout=5s --start-period=40s --retries=3 \
    CMD curl -fsS http://127.0.0.1:8080/health || exit 1

ENTRYPOINT ["dotnet", "PixNestAPI.WebApi.dll"]
