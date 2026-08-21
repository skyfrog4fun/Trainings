# Local Development Setup

This guide walks through setting up the Trainings application for local development from a fresh clone, and serves as the reference for all `appsettings.json` configuration keys.

---

## Table of Contents

1. [Prerequisites](#1-prerequisites)
2. [Clone and Restore](#2-clone-and-restore)
3. [Database](#3-database)
4. [Seed Account](#4-seed-account)
5. [Running the Application](#5-running-the-application)
6. [Mail in Development](#6-mail-in-development)
7. [Running Tests](#7-running-tests)
8. [Configuration Reference](#8-configuration-reference)

---

## 1. Prerequisites

| Requirement | Version |
|---|---|
| .NET SDK | `10.0.300` (enforced by `global.json`) |

Install the exact SDK version from [dotnet.microsoft.com/download](https://dotnet.microsoft.com/download) or via your preferred SDK manager. The `global.json` at the repository root pins the version; mismatches will cause build errors.

---

## 2. Clone and Restore

```bash
git clone <repository-url>
cd Trainings
dotnet restore
```

No additional tooling (Node, npm, etc.) is required.

---

## 3. Database

The application uses **SQLite**. No installation or manual database creation is needed.

On first startup, `DbSeeder` automatically:
1. Creates the SQLite file at the path specified in `ConnectionStrings:DefaultConnection` (default: `trainings.db` in the working directory).
2. Applies all pending EF Core migrations.
3. Seeds required lookup data (default locations, global tags).
4. Creates the initial SuperAdmin account (see [Seed Account](#4-seed-account) below).

Subsequent startups apply any new migrations automatically.

> **Note for development:** The database file is created relative to where `dotnet run` is invoked. With `dotnet run --project src/Trainings.Web`, the file lands in `src/Trainings.Web/`.

---

## 4. Seed Account

On first run, a SuperAdmin user is created using the values in `Seed:Email` and `Seed:Password` from `appsettings.json`.

| Key | Default |
|---|---|
| `Seed:Email` | `superadmin@trainings.app` |
| `Seed:Password` | `Admin123!` |

Log in with these credentials after the first startup. Change the password immediately via the profile page.

> **Important:** Override both values in `appsettings.Development.json` or via environment variables before sharing the repository or deploying to any non-local environment.

---

## 5. Running the Application

```bash
dotnet run --project src/Trainings.Web
```

The application starts on `http://localhost:5000` (HTTP) and `https://localhost:5001` (HTTPS) by default. Open either URL in the browser.

To run with hot reload:

```bash
dotnet watch --project src/Trainings.Web
```

---

## 6. Mail in Development

By default the application attempts to send real emails. For local development, suppress outgoing mail by adding the following to `src/Trainings.Web/appsettings.Development.json`:

```json
{
  "App": {
    "Modes": {
      "NoEmail": true
    }
  }
}
```

When `NoEmail` is `true`, no mail is sent. Where the UI supports it, the generated message is shown in an in-app preview modal instead. This covers registration confirmation, password reset, and test mail flows.

Mail configuration (SMTP host, port, credentials, sender address) is managed at runtime by a SuperAdmin via **⚙️ Config → Mail Configuration** in the UI. These settings are stored in the database and do not require a restart.

---

## 7. Running Tests

```bash
dotnet test
```

Or from the `tests/` directory:

```bash
dotnet test tests/
```

All tests are unit tests and require no external dependencies or running application instance.

For more on testing conventions, see [`CONTRIBUTING.md`](../../CONTRIBUTING.md).

---

## 8. Configuration Reference

All keys live in `src/Trainings.Web/appsettings.json`. Override specific keys for local development in `appsettings.Development.json` — that file is `.gitignore`d and never committed.

### `ConnectionStrings`

| Key | Default | Description |
|---|---|---|
| `DefaultConnection` | `Data Source=trainings.db` | SQLite connection string. Change the path to control where the database file is created (e.g., `Data Source=/app/data/trainings.db` in Docker). |

### `App`

| Key | Default | Description |
|---|---|---|
| `App:BaseUrl` | `https://localhost` | Base URL of the application. Used to construct absolute links in outgoing emails (e.g., email verification links, password reset links). Set to the public URL in production. |
| `App:DefaultCountry` | `CH` | ISO 3166-1 alpha-2 country code pre-selected in address/country dropdowns. |

### `App:Modes`

Runtime modes control application behavior without a code change. Both can also be toggled at runtime by a SuperAdmin via **⚙️ Config → Running Modes** — runtime overrides reset to these configured defaults on restart.

| Key | Default | Description |
|---|---|---|
| `App:Modes:ReadOnly` | `false` | When `true`, all write operations are blocked for non-SuperAdmin users. The application becomes read-only. Useful for maintenance windows. |
| `App:Modes:NoEmail` | `false` | When `true`, outgoing emails are suppressed. Where supported, the mail content is shown in an in-app preview modal. Recommended for local development. |

### `Seed`

Used only on first startup by `DbSeeder` to create the initial SuperAdmin account. Has no effect if the user already exists.

| Key | Default | Description |
|---|---|---|
| `Seed:Email` | `superadmin@trainings.app` | Email address for the initial SuperAdmin account. |
| `Seed:Password` | `Admin123!` | Password for the initial SuperAdmin account. Change before any non-local deployment. |

### `Logging`

Standard .NET logging configuration. See the [Microsoft logging docs](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/logging/) for full options.

| Key | Default | Description |
|---|---|---|
| `Logging:LogLevel:Default` | `Information` | Default log level for all categories. |
| `Logging:LogLevel:Microsoft.AspNetCore` | `Warning` | Suppresses verbose ASP.NET Core framework logs in development. |

---

## Production Deployment

For deploying to the Synology NAS via Docker, see [`docs/infrastructure/synology-nas-setup.md`](../infrastructure/synology-nas-setup.md).
