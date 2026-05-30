# Tubestead

Self-hosted, YouTube-style video platform for your own hardware. Upload your
videos through a clean web UI, have them processed for smooth in-browser
playback, and browse / watch / re-download them — all from a single container
you run on a Windows box or a NAS.

This is a **host-your-own-videos** platform (think private YouTube / PeerTube),
not a downloader or archiver. Your media lives on your disk; nobody else's.

> **Status: Milestone 1 complete.** First-run setup wizard, cookie-based auth
> with Admin/Viewer roles, and the SPA-served-by-API foundation are in place.
> Uploads, transcoding, playback, and Docker packaging land in M2–M5 (see
> [Roadmap](#roadmap)).

---

## Tech stack

| Layer | Choice |
| --- | --- |
| Backend | ASP.NET Core (.NET 10) Web API, controllers |
| Database | EF Core — **SQLite by default**, SQL Server via connection string |
| Auth | ASP.NET Core Identity, **cookie-based** (same-origin SPA), Admin/Viewer roles |
| Background jobs | Hangfire (SQLite-backed) — *wired in M2* |
| Media | ffmpeg / ffprobe via FFMpegCore, HLS renditions — *M3* |
| Uploads | tus resumable protocol (tusdotnet / tus-js-client) — *M2* |
| Frontend | Vite + React + TypeScript, TanStack Query, React Router, Tailwind |
| Player | vidstack + hls.js — *M3* |
| Packaging | Single container: API serves the built SPA (one origin, no CORS) — *M5* |

### Why these choices

- **Cookie auth, not browser-stored JWT.** The SPA is served same-origin in
  production, so HttpOnly cookies are simpler and safer (no token in JS reach).
  If origins are ever split, swap in JWT bearer — the API contract wouldn't change.
- **SQLite default, SQL Server optional.** Zero-config first boot from a single
  file; point `TUBESTEAD_DB_PROVIDER=SqlServer` + a connection string at an
  existing server later. The DB lives on **local disk**, never the NAS share
  (SQLite over SMB/NFS corrupts).
- **Original-only transcoding by default.** Low-power NAS CPUs can take minutes
  per minute of video to transcode. By default an upload is made watchable
  immediately (faststart remux + thumbnail) and quality renditions are opt-in.

---

## Repository layout

```
Tubestead/
├─ src/
│  ├─ Tubestead.Api/             ASP.NET Core API + serves the built SPA (wwwroot)
│  ├─ Tubestead.Domain/          Entities & enums (no framework dependencies)
│  └─ Tubestead.Infrastructure/  EF Core, Identity, settings service, migrations
├─ client/                       Vite + React + TypeScript SPA
├─ tests/
│  └─ Tubestead.Tests/           xUnit unit + integration tests
├─ Tubestead.sln
├─ .env.example                  Documented optional overrides (never required)
└─ README.md
```

---

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js 20+](https://nodejs.org/) and npm
- ffmpeg / ffprobe on `PATH` — only needed once media processing lands (M3);
  bundled into the Docker image for production.

---

## Dev quickstart

Two terminals. The API runs on `:5099`; the Vite dev server on `:5173` and
proxies `/api` to the API, so the browser sees a single origin (no CORS).

**Terminal 1 — API**
```bash
dotnet run --project src/Tubestead.Api
# → http://localhost:5099
```

**Terminal 2 — client**
```bash
cd client
npm install        # first time only
npm run dev
# → http://localhost:5173
```

Open **http://localhost:5173**. On a fresh database you'll land on the
**first-run setup wizard**: create the admin account, name the site, choose a
media path and transcoding mode, review, finish. You're then signed in as admin.

The SQLite database and data-protection keys are created under
`src/Tubestead.Api/bin/Debug/net10.0/data/`. Delete that folder to start over
from the wizard.

### Run the tests

```bash
dotnet test
```

Covers settings precedence (stored > env > default) and the full setup → cookie
auth → login flow, including the "setup can't run twice" and "anonymous is
rejected" guards.

---

## Production build (single origin)

In production the API serves the compiled React app from its `wwwroot`, so
everything is one process on one port.

```bash
# 1) Build the SPA straight into the API's wwwroot
cd client && npm run build

# 2) Run the API in production
cd ..
ASPNETCORE_ENVIRONMENT=Production TUBESTEAD_PORT=8080 dotnet run --project src/Tubestead.Api
# → http://localhost:8080  (SPA + API together)
```

A `docker compose up` path that does both steps in one image arrives in **M5**.

---

## Configuration

Tubestead is configured through the **setup wizard** and the **in-app admin UI**;
no file editing is required for the happy path. Environment variables exist only
as overrides.

**Precedence (highest wins):** setup wizard / admin UI (stored in DB) →
environment variable → built-in default.

| Setting (DB key) | Env override | Default | Notes |
| --- | --- | --- | --- |
| `site.name` | `TUBESTEAD_SITE_NAME` | `Tubestead` | Branding |
| `site.publicUrl` | `TUBESTEAD_PUBLIC_URL` | *(empty)* | Public base URL behind a proxy |
| `media.path` | `TUBESTEAD_MEDIA_PATH` | `/media` | Media root (your NAS share) |
| `uploads.maxBytes` | `TUBESTEAD_MAX_UPLOAD_BYTES` | 50 GiB | Max upload size |
| `uploads.allowedExtensions` | `TUBESTEAD_ALLOWED_EXTENSIONS` | `.mp4,.mov,.mkv,…` | Accepted file types |
| `transcode.mode` | `TUBESTEAD_TRANSCODE_MODE` | `original-only` | `original-only` or `auto-renditions` |
| `transcode.presets` | `TUBESTEAD_TRANSCODE_PRESETS` | `1080p,720p,480p` | Rendition ladder |
| `registration.open` | `TUBESTEAD_REGISTRATION_OPEN` | `false` | Self-registration toggle |
| — | `TUBESTEAD_PORT` | *(launch settings)* | Internal HTTP port |
| — | `TUBESTEAD_DB_PROVIDER` | `Sqlite` | `Sqlite` or `SqlServer` |
| — | `TUBESTEAD_DB_CONNECTION` | `…/data/tubestead.db` | DB connection string |
| — | `TUBESTEAD_DATA_PATH` | `<app>/data` | Local data (db, keys) — **not** the NAS share |

See [`.env.example`](.env.example) for the full, commented list.

### Reverse proxy & NAS (preview)

The API already honors `X-Forwarded-*` headers (trusts the proxy in front of
it) and serves plain HTTP internally — terminate TLS at Nginx Proxy Manager /
Cloudflare. Linuxserver-style `PUID` / `PGID` / `UMASK` for NAS-friendly file
ownership and the two-volume layout (local DB + bind-mounted media) are
documented and wired up as part of the **M5** Docker packaging.

---

## Roadmap

**Milestones**
- **M1 — Foundations ✅** Solution scaffold, EF Core + SQLite (provider
  abstraction), Identity cookie auth + roles, first-run setup wizard, SPA served
  by API.
- **M2 — Uploads.** Resumable tus uploads with progress, storage layout, video
  lifecycle + processing-status plumbing (Hangfire), reverse-proxy/base-URL config.
- **M3 — Processing & playback.** ffmpeg metadata/thumbnail/faststart, async HLS
  renditions (original-only by default, optional hardware acceleration), vidstack
  player with seeking and quality switching.
- **M4 — Library & download.** Browse grid, video page, download original +
  renditions, title search, tags/playlists.
- **M5 — Admin & packaging.** Admin settings UI with guided wizards, user
  create/invite, Dockerfile + docker-compose (PUID/PGID/UMASK, volumes), thorough
  deploy docs.

**Deferred (not in MVP)**
- Subtitles / captions
- View counts & watch history
- Comments / likes
- Multi-channel
- S3-compatible storage backend
- Live streaming
