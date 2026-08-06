# Local Run Setup and Smoke Validation

## Prerequisites

- .NET 10 SDK
- Node.js (with npm)
- SQL Server (Express or Developer) — tested on IND-L-BACHDH\SQLEXPRESS (SQL Server 2025)

## Database

### Recommended local database

PTKD_DEV

### Create database (if not exists)

```sql
sqlcmd -S . -E -C -Q "CREATE DATABASE PTKD_DEV"
```

### Connection string

```
Server=.;Database=PTKD_DEV;Trusted_Connection=True;TrustServerCertificate=True
```

### Configure user secrets (one-time)

From repo root:

```powershell
cd src\backend\PTKD.Api
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=.;Database=PTKD_DEV;Trusted_Connection=True;TrustServerCertificate=True"
```

The DbMigrator shares the same user-secrets ID, so the same connection string applies.

### Run migrations

From repo root:

```powershell
dotnet run --project src/backend/PTKD.DbMigrator
```

This applies V0001 through V0015 to PTKD_DEV. Expected result: 52 tables, 15 SchemaVersions rows.

Dry-run mode (preview only):

```powershell
dotnet run --project src/backend/PTKD.DbMigrator -- --dry-run
```

### Verify migrations

```sql
sqlcmd -S . -E -C -d PTKD_DEV -Q "SELECT COUNT(*) AS tables FROM sys.tables; SELECT COUNT(*) AS versions FROM dbo.SchemaVersions"
```

Expected: 52 tables, 15 versions.

## Bootstrap admin user

The bootstrap tool creates the initial admin account. It reads configuration from environment variables only (passwords are never stored in files).

```powershell
$env:CONNECTION_STRING = "Server=.;Database=PTKD_DEV;Trusted_Connection=True;TrustServerCertificate=True"
$env:BOOTSTRAP_ADMIN_PASSWORD = "<your-chosen-password>"
$env:BOOTSTRAP_ADMIN_EMAIL = "<your-email>"
dotnet run --project src/backend/PTKD.Bootstrap
```

Optional environment variables:
- `BOOTSTRAP_ADMIN_NAME` — defaults to "System Administrator"
- `BOOTSTRAP_ADMIN_CODE` — defaults to "admin"

The admin account is created with `must_change_password = 1`. On first login, the UI will redirect to a password change screen.

### Verify bootstrap

```sql
sqlcmd -S . -E -C -d PTKD_DEV -Q "SELECT is_bootstrapped FROM dbo.Security_Bootstrap_State"
```

Expected: `1` (true).

## Backend API

### Start

From repo root:

```powershell
dotnet run --project src/backend/PTKD.Api
```

### Endpoints

| Endpoint | URL |
|----------|-----|
| API base | http://localhost:5057/api/v2 |
| Health check | http://localhost:5057/api/v2/health |
| Swagger UI | http://localhost:5057/swagger |
| Login | POST http://localhost:5057/api/v2/auth/login |

### Verify

```powershell
Invoke-WebRequest -Uri "http://localhost:5057/api/v2/health" -UseBasicParsing
```

Expected: HTTP 200 with `{"status":"Healthy","entries":[{"name":"sql_server","status":"Healthy",...}]}`.

### Notes

- Runs in Development environment by default (from launchSettings.json).
- CORS allows `http://localhost:5173` (frontend dev server).
- JWT RSA signing key is generated in-memory at startup — tokens do not survive API restart.
- Swagger UI may show "Failed to load API definition" for organization endpoints that are gated to dev-only configuration. The API itself functions correctly.

## Frontend

### Install dependencies (one-time)

```powershell
cd src\frontend
npm install
```

### Start dev server

```powershell
cd src\frontend
npm run dev
```

### Endpoints

| Endpoint | URL |
|----------|-----|
| Frontend | http://localhost:5173 |
| Login page | http://localhost:5173/login |

### API connection

The frontend connects to the backend via `VITE_API_BASE_URL`. Default fallback (no `.env` file needed):

```
http://localhost:5057/api/v2
```

To override, create `src/frontend/.env`:

```
VITE_API_BASE_URL=http://localhost:5057/api/v2
```

## Login flow

1. Open http://localhost:5173 — redirects to /login.
2. Enter the admin username (default code: `admin`) and the password set during bootstrap.
3. First login triggers a mandatory password change screen (`must_change_password = 1`).
4. After password change, the user is authenticated with a JWT access token and an HttpOnly refresh token cookie.

## Quick start summary

```powershell
# 1. Migrate database
dotnet run --project src/backend/PTKD.DbMigrator

# 2. Bootstrap admin (set env vars first — see section above)
dotnet run --project src/backend/PTKD.Bootstrap

# 3. Start backend (terminal 1)
dotnet run --project src/backend/PTKD.Api

# 4. Start frontend (terminal 2)
cd src\frontend
npm run dev

# 5. Open browser
# http://localhost:5173
```

## Smoke validation results (2026-08-06)

| Check | Result |
|-------|--------|
| PTKD_DEV database exists | PASS |
| Migrations V0001–V0015 applied | PASS — 52 tables, 15 SchemaVersions rows |
| Backend starts | PASS — `dotnet run --project src/backend/PTKD.Api` |
| Health check responds | PASS — HTTP 200, SQL Server healthy |
| Frontend starts | PASS — `npm run dev` on port 5173 |
| Frontend serves login page | PASS — "PTKD ERP" heading, username/password form rendered |
| Swagger UI loads | PARTIAL — page loads but org API definition fetch fails (known dev-only gate) |
| Bootstrap admin created | NOT EXECUTED — requires PO to set admin password via environment variable |

## Database summary

| Database | Purpose | Use for local run |
|----------|---------|-------------------|
| PTKD_DEV | Local development | Yes — recommended |
| PTKD_TEST_PHASE1A2 | Automated tests | No — test runner only |
| PTKD_PROD | Production | No — never for local dev |
