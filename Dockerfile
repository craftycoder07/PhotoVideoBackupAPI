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

RUN dotnet restore

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

# Create log and media directories with correct ownership
RUN mkdir -p /app/logs /app/media && \
    chown -R appuser:appgroup /app

COPY --from=publish --chown=appuser:appgroup /app/publish .

USER appuser

# Port is controlled at runtime via ASPNETCORE_URLS env var in docker-compose
EXPOSE 8080

ENTRYPOINT ["dotnet", "PixNestAPI.WebApi.dll"]
