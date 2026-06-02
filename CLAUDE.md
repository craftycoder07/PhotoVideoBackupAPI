# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

PixNest is a self-hosted photo/video backup API — a private, local alternative to Google Photos. Mobile devices back up media to your server over WiFi. The project/assembly name is **PixNestAPI**.

## Solution Structure

```
PixNestAPI.sln
└── src/
    ├── PixNestAPI.WebApi/       # ASP.NET Core 8 Web API (entry point)
    │   ├── Features/                     # Controllers grouped by domain
    │   │   ├── Auth/AuthController.cs
    │   │   ├── Media/MediaController.cs
    │   │   ├── Sessions/SessionController.cs
    │   │   ├── Stats/StatsController.cs
    │   │   └── Users/UserController.cs
    │   ├── Models/                       # Domain models + request/response DTOs
    │   ├── Services/                     # IMediaBackupService, IAuthService + impls
    │   ├── Data/MediaBackupDbContext.cs  # EF Core context (PostgreSQL)
    │   └── Migrations/                   # EF Core migration history
    └── PixNestAPI.Infrastructure/
        └── Logging/SerilogConfiguration.cs  # Bootstrap + full Serilog setup
```

## Key Architecture Decisions

**Data model**: `User → BackupSession → MediaItem` (cascade deletes). `User.Settings` (DeviceSettings), `User.Stats` (BackupStats), `BackupSession.SessionInfo`, and collection fields (`Errors`, `Tags`) are stored as JSON columns in PostgreSQL via EF Core value converters. `[JsonIgnore]` is applied to nav properties that would cause circular references.

**Auth**: JWT (HS256) via `JwtAuthService`. The `userId` claim is the primary identity used across all controllers — extract it with `User.FindFirst("userId")?.Value`. Refresh token storage is not yet implemented (`RefreshTokenAsync` throws `NotImplementedException`).

**Storage**: Files are stored under `{StorageSettings:BasePath}/{username}/{guid}{ext}`. Thumbnails go in `{BasePath}/Thumbnails/{mediaId}_thumb.jpg`. SkiaSharp is used for photo thumbnail generation (JPEG/PNG only); video thumbnails are not implemented.

**Configuration layering** (last wins):
1. `appsettings.json`
2. `appsettings.{Environment}.json`
3. Environment variables (`ConnectionStrings__DefaultConnection`, `Jwt__Secret`, etc.)
4. Prefixed env vars (`PixNest_Dev_*`, `PixNest_Prod_*`)

**Environments**: `Development` and `Production`. Development is the dev workflow — always run via `docker compose up` (sets `ASPNETCORE_ENVIRONMENT: Development`). Development skips HTTPS redirect (TLS terminates at the reverse proxy) and auto-applies EF Core migrations on startup. Production requires migrations to be applied manually via an idempotent SQL script.

**Logging**: Two-stage Serilog. Bootstrap logger starts first (console only). Full logger reads from `appsettings.json` Serilog section once config is built. Sinks: Console, File (rolling daily, `logs/pixnest-*.log`), and Seq (`http://localhost:5341`). Request logging respects `Serilog:RequestLogging:Enabled` and `ErrorsOnly` config keys.

## Common Commands

```bash
# Run locally (HTTPS, Swagger at https://localhost:7109)
dotnet run --project src/PixNestAPI.WebApi --launch-profile https

# Run in Development environment
dotnet run --project src/PixNestAPI.WebApi --environment Development

# Build
dotnet build PixNestAPI.sln

# EF Core migrations (run from repo root)
dotnet ef migrations add <MigrationName> --project src/PixNestAPI.WebApi
dotnet ef database update --project src/PixNestAPI.WebApi

# Generate idempotent SQL script for production deployments
dotnet ef migrations script --idempotent --project src/PixNestAPI.WebApi

# Docker Compose (requires .env file at repo root)
docker compose up --build
docker compose down
```

## Required Environment Variables

Before running locally, set these (or use a `.env` file for Docker):

| Variable | Purpose |
|---|---|
| `ConnectionStrings__DefaultConnection` | PostgreSQL connection string |
| `Jwt__Secret` | JWT signing key (≥32 chars); in Development a hardcoded fallback is used |
| `StorageSettings__BasePath` | Absolute path for media file storage |

For Docker Compose, set `POSTGRES_USER`, `POSTGRES_PASSWORD`, `JWT_SECRET`, `SEQ_ADMIN_PASSWORD`, and `MEDIA_STORAGE_PATH` in a `.env` file at the repo root.

## Docker Services

| Service | Host port | Purpose |
|---|---|---|
| `api` | 5100 | ASP.NET Core API (HTTP only) |
| `postgres` | 5433 | PostgreSQL 16 (avoids conflict with local Postgres on 5432) |
| `seq` | 5341 | Structured log UI |

Swagger UI is served at the root path (`/`) in the Development environment.