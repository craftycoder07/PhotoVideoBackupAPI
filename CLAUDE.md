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

**Environments**: `Development` and `Production`, fully isolated — separate Compose projects, volumes, networks, Postgres clusters, credentials, container names and media folders. Both can run at once.

**Naming convention: production carries no marker anywhere; development is the variant that gains one.** The plain `PixNest` spelling always means production.

| | Production | Development |
|---|---|---|
| Compose file | `docker-compose.yml` | `docker-compose.override.yml` |
| Compose project | `pixnestapi` | `pixnestapi-dev` |
| Containers | `pixnest-api`, `pixnest-postgres`, `pixnest-seq` | `pixnest-dev-*` |
| Volumes | `pixnestapi_*` | `pixnestapi-dev_*` |
| Database | `PixNest` | `PixNestDev` |
| Media folder | `PixNest Media Backup` | `PixNest Media Backup Dev` |
| Log files | `pixnest-.log` | `pixnest-dev-.log` |
| Host ports | none | 5100 api, 5432 pg, 80 seq |

Compose merges `docker-compose.override.yml` automatically when no `-f` is given, so the shortest command is the development one. The override holds only the differences and inherits everything else; it also uses `!override` / `!reset` to drop the production-only external `caddy-net`. **It is not deployed to the server** — there the bare `docker compose up -d` is production.

**Env files**: `.env` holds whatever that machine runs — development values on a dev laptop, production values on the server — so it never carries a marker either. Compose auto-loads `.env` and nothing else. `.env.prod` exists only on a dev machine, to exercise production locally:

```bash
docker compose --env-file .env.prod -f docker-compose.yml up -d
```

Omitting `--env-file` there would start production containers with development credentials. On a first `up` that gets baked into the new Postgres volume permanently, since `POSTGRES_PASSWORD` is only honoured while initialising an empty data directory.

**Migrations**: both environments auto-apply pending EF Core migrations on startup (bounded retry, 5 attempts). This is safe only because the deployment is a single instance — no replicas or rolling deploys to race the migration. Set `Database:MigrateOnStartup=false` to opt out and apply `dotnet ef migrations script --idempotent` by hand instead.

**Reverse proxy**: the app never calls `UseHttpsRedirection` — Kestrel only ever listens on HTTP (`ASPNETCORE_URLS: http://+:8080`) and holds no certificate, so TLS termination and the http→https redirect belong to Caddy. Redirecting in-process would bounce the request back to the proxy, which forwards it over HTTP again — an infinite loop. `UseForwardedHeaders` is the **first** middleware, honouring `X-Forwarded-Proto` and `X-Forwarded-For` so the scheme, `Request.IsHttps` and client IPs are the client's, not the proxy's. Its `KnownNetworks`/`KnownProxies` trust lists are cleared (Caddy has a dynamic container IP, and the loopback default would silently drop the headers); that is safe **only** because `docker-compose.yml` publishes no API port. Publishing it would let anything on the LAN spoof `X-Forwarded-*` — the two must stay in sync.

**Startup validation**: outside Development, a missing `ConnectionStrings:DefaultConnection`, or a missing or under-32-character `Jwt:Secret`, throws at startup; the process logs `Fatal` and exits non-zero. Neither has a placeholder value in `appsettings.json` any more, so the guards actually fire.

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

# Development — the override merges automatically, no flags needed
docker compose up -d --build
docker compose down

# Production — on the server this is the whole story (its .env holds production values)
docker compose -f docker-compose.yml up -d --build
docker compose -f docker-compose.yml logs -f api

# Production from a DEV machine — must pass the production env file explicitly
docker compose --env-file .env.prod -f docker-compose.yml up -d --build

# Production + loopback-only admin ports (pgAdmin 5433, Seq 5342) — debugging only
docker compose --env-file .env.prod -f docker-compose.yml -f docker-compose.debug.yml up -d

# Inspect either database directly
docker exec -it pixnest-dev-postgres psql -U "$POSTGRES_USER" -d PixNestDev
docker exec -it pixnest-postgres     psql -U "$POSTGRES_USER" -d PixNest
```

## Required Environment Variables

Before running locally, set these (or use a `.env` file for Docker):

| Variable | Purpose |
|---|---|
| `ConnectionStrings__DefaultConnection` | PostgreSQL connection string. Required — startup fails without it |
| `Jwt__Secret` | JWT signing key (≥32 chars). Required outside Development, where a hardcoded fallback is used |
| `StorageSettings__BasePath` | Absolute path for media file storage (`/app/media` inside the container) |

For Docker Compose, set `POSTGRES_USER`, `POSTGRES_PASSWORD`, `JWT_SECRET`, `SEQ_ADMIN_PASSWORD`, and `MEDIA_STORAGE_PATH` in `.env` (development) and `.env.prod` (production) at the repo root. Neither is committed. Because the two environments are separate Postgres clusters, their credentials are genuinely independent — a password that works on one is rejected by the other.

## Docker Services

Production (`docker-compose.yml`) — containers `pixnest-*`, database `PixNest` — publishes **no** host ports. Caddy reaches the API as `http://api:8080` over the external `caddy-net`; Postgres and Seq are reachable only from inside the stack. If Caddy runs on the host rather than in Docker, publish `127.0.0.1:5100:8080` instead and drop `caddy-net` — see the comments in that file.

Development (`docker-compose.override.yml`) — containers `pixnest-dev-*`, database `PixNestDev`:

| Service | Host port | Purpose |
|---|---|---|
| `api` | 5100 | ASP.NET Core API (HTTP only) |
| `postgres` | 5432 | PostgreSQL 16 |
| `seq` | 80 | Structured log UI |

`docker-compose.debug.yml` is an opt-in override for **production**, adding loopback-only admin ports (`127.0.0.1:5433` Postgres, `127.0.0.1:5342` Seq) so pgAdmin or a browser can reach a running production stack without exposing anything beyond the local machine. The non-standard numbers keep it clear of development's 5432/80 so both stacks can run at once. On the server, reach those through an SSH tunnel rather than binding them publicly.

Swagger UI is served at the root path (`/`) in the Development environment.