# Synology NAS Container Hosting Setup

## ASP.NET Core + SQLite on Synology DS224+ with Docker

This guide provides step-by-step instructions to deploy the Trainings web application on a Synology NAS using Docker containers and SQLite. It is written so the entire environment can be recreated from scratch on a new machine without prior knowledge of the original setup.

---

## Table of Contents

1. [Architecture Overview](#1-architecture-overview)
2. [NAS Preparation](#2-nas-preparation)
3. [Container Architecture](#3-container-architecture)
4. [Database Setup (SQLite)](#4-database-setup-sqlite)
5. [Web Application Container](#5-web-application-container)
6. [Reverse Proxy Configuration](#6-reverse-proxy-configuration)
7. [Dynamic DNS Setup](#7-dynamic-dns-setup)
8. [Router Configuration](#8-router-configuration)
9. [HTTPS and Certificates](#9-https-and-certificates)
10. [Security Considerations](#10-security-considerations)
11. [Deployment Workflow](#11-deployment-workflow)
12. [Backup Strategy](#12-backup-strategy)

---

## 1. Architecture Overview

### Full Architecture Diagram

```
Internet
    │
    ▼
Public Domain (trainings.planetfrey.ch)
    │
    ▼
Dynamic DNS Provider (Synology DDNS: skyfrog.myds.me)
    │  (maps domain to current public IP)
    ▼
Home Router
    │  Port 80  → NAS IP:80
    │  Port 443 → NAS IP:443
    ▼
Synology DS224+ (DSM)
    │
    ▼
DSM Reverse Proxy (built-in, port 443)
    │  SSL termination with Let's Encrypt certificate
    │  Routes trainings.planetfrey.ch → localhost:8080
    ▼
Docker Container: trainings-web (port 8080)
    │
    ├── ASP.NET Core 10 Blazor Application
    └── SQLite database file
            /app/data/trainings.db
            (mounted from NAS volume)
```

### Layer Explanations

| Layer | Technology | Role |
|---|---|---|
| Public Domain | DNS A/CNAME record | Resolves the domain to the current public IP |
| Dynamic DNS | Synology DDNS | Updates the DNS record when the ISP changes the public IP |
| Router Port Forwarding | Home Router NAT | Forwards TCP 80 and 443 from the internet to the NAS |
| NAS Reverse Proxy | DSM built-in (nginx) | Terminates TLS, routes requests to the correct container port |
| Docker Container | Synology Container Manager | Runs the ASP.NET Core application in isolation |
| SQLite Database | EF Core + SQLite | Embedded in the application; persisted in a Docker volume |

### Domain Resolution Flow

1. User browser resolves `trainings.planetfrey.ch` via DNS.
2. The Dynamic DNS provider has updated the A record to point to the current home router public IP.
3. The router forwards port 443 traffic to the NAS.
4. The DSM Reverse Proxy terminates TLS using the Let's Encrypt certificate and forwards HTTP traffic to the container on port 8080.
5. The ASP.NET Core application reads and writes the SQLite database file at `/app/data/trainings.db`, which is a Docker bind mount to a persistent folder on the NAS.

### SQLite Database Location

| Context | Path |
|---|---|
| Inside container | `/app/data/trainings.db` |
| On NAS host | `/volume1/docker/trainings/data/trainings.db` |
| Connection string | `Data Source=/app/data/trainings.db` |

The database file is **not** baked into the Docker image. It lives in a persistent bind mount so it survives container restarts and image updates.

---

## 2. NAS Preparation


### 2.1 System Information (as of 24.03.2026)

- **DSM version:** 7.3.2-86009 Update 3
- **NAS URL:** [https://skynas24:5001](https://skynas24:5001)
- **Container Manager (Docker Inc) version:** 24.0.2-1606
- **LAN subnet:** 192.168.1.0/24 (subnet mask 255.255.255.0)

#### Install Container Manager
1. Open **DSM** → **Package Center**.
2. Search for **Container Manager** and click **Install**.
3. After installation, open **Container Manager** from the main menu.

#### During installation
- When prompted to "Configure bridge network", choose **Custom** and set **Subnet ID** to `172.20.0.1/24`.

### 2.2 Recommended DSM Configuration

| Setting | Recommended Value |
|---|---|
| DSM version | 7.3.2-86009 Update 3 |
| Automatic DSM updates | Security patches only |
| 2-Factor Authentication | Enabled for all admin accounts |
| Default admin account | Disabled (create a named admin account) |
| SSH service | Disabled unless required (see 2.5) |


To deactivate the default `admin` account:
1. **Control Panel** → **User & Group** → select `admin` → **Edit** → **Deactivate this account**.


### 2.3 Firewall Configuration

1. **Control Panel** → **Security** → **Firewall** → **Enable Firewall**.
2. Click **Manage firewall profile** and create a new profile named `docker`.
3. Add rules in this order (rules are evaluated top to bottom):

| Priority | Source | Port | Action |
|---|---|---|---|
| 1 | All | 443 | Allow |
| 2 | All | 80 | Allow |
| 3 | 192.168.1.0/Subnet 255.255.0.0 | All | Allow |
| 4 | All | All | Deny |

> **Note:** Port 80 is needed for Let's Encrypt HTTP-01 certificate validation. After the certificate is issued, you may restrict port 80 to only the Let's Encrypt validation service, or redirect it to 443.

### 2.3.1 Verifying Port Reachability

The NAS firewall alone is not sufficient — the **home router** must also forward ports 80 and 443 to the NAS. See [Section 9.1](#91-port-forwarding-rules) for router port forwarding setup.

After configuring both the NAS firewall and the router, verify that the ports are reachable from the internet:

1. Go to **[portchecker.co](https://portchecker.co)** from any device (or use your phone on mobile data — not the home Wi-Fi).
2. Enter your public IP address (find it at [whatismyip.com](https://www.whatismyip.com)) and test port **80**, then port **443**.
3. Both should report **Open**.

If a port shows as **closed** or **timed out**:
- Check that the port forwarding rule exists on the router (see below).
- Check that the NAS firewall rule allows that port (rules 1 and 2 in the table above).
- Some ISPs (including Sunrise on certain residential plans) block inbound port 80. If port forwarding is correctly configured but port 80 remains blocked, contact your ISP.

> **Sunrise Internet Box:** The router admin UI is typically at `http://192.168.1.1` or `http://internetbox.home`. Port forwarding is under **Network** → **Port Forwarding** ("NAT / Portweiterleitung"). Add TCP rules for external ports 80 and 443 pointing to the NAS LAN IP.

### 2.4 User Permissions

Create a dedicated non-admin user to own the container data:

1. **Control Panel** → **User & Group** → **Create**.
2. Fill in the following fields:
  - **Name:** `docker-svc`
  - **Email:** [your private mail] (required field)
  - **Password:** Set a very long and strong password. *(Note: Remember it!)*
  - **Untick** "Send a notification mail to the newly created user"
  - **Tick** "Disallow the user to change account password"
3. Assign only to the group `users` (required). Do **not** assign to any additional groups.
4. Grant read/write access only to `/volume1/docker/`.
5. **Quota:** Set a quota for the user so that the used memory is restricted. For volume `docker`, set the limit to **500MB**.
6. **Assign application permissions:** Set **Deny** to all applications.

All container data (volumes, config) should be stored under `/volume1/docker/`.

### 2.5 SSH Configuration (Optional)

SSH is currently **disabled**. It is not configured yet, but may be enabled later for advanced troubleshooting.

If you enable SSH in the future, be sure to harden it:
1. **Control Panel** → **Terminal & SNMP** → **Enable SSH service**.
2. Change the port from 22 to a non-standard port (e.g. 2222).
3. In `/etc/ssh/sshd_config` set:
  ```
  PermitRootLogin no
  PasswordAuthentication no
  PubkeyAuthentication yes
  ```
4. Add your SSH public key via **Control Panel** → **User & Group** → **Advanced** → **User Home** and place it in `~/.ssh/authorized_keys`.
5. Disable SSH again when not needed.

---

## 3. Container Architecture

### 3.1 Overview

A single Docker container runs both the ASP.NET Core web application and the embedded SQLite database. There is no separate database container needed.

```
trainings-web container
├── ASP.NET Core 10 Blazor app (port 8080)
└── reads/writes /app/data/trainings.db
        ↕ bind mount
NAS: /volume1/docker/trainings/data/trainings.db
```

### 3.2 Persistent Volume

The SQLite database file is stored on the NAS host filesystem (not inside the container layer) so that:

- Data survives container restarts, recreation, and image updates.
- Backups can be taken by copying a single file on the NAS.

Host path: `/volume1/docker/trainings/data/`

Create this directory before starting the container:

```bash
mkdir -p /volume1/docker/trainings/data
```

### 3.3 Container Networking

The container is attached to a user-defined bridge network. This isolates it from other containers and allows the DSM Reverse Proxy to reach it on port 8080 via `localhost`.

```
DSM Reverse Proxy → host port 8080 → container port 8080
```

Because the reverse proxy runs on the NAS host (not inside Docker), publishing port 8080 to the host is sufficient.

### 3.4 Restart Policy

The container is configured with `restart: unless-stopped` so it automatically recovers from:
- Application crashes
- NAS reboots
- Docker daemon restarts

It does **not** restart if you explicitly stop it (e.g. during maintenance).

### 3.5 Secrets Handling

Sensitive values (admin seed credentials, etc.) are passed via environment variables at runtime or via a `.env` file that is **never committed to source control**.

A `.env.example` file documents which variables are required. See [Section 5](#5-web-application-container) for the full list.

### 3.6 docker-compose.yml

Place this file in `/volume1/docker/trainings/` on the NAS:

```yaml
# /volume1/docker/trainings/docker-compose.yml
services:
  trainings-web:
    image: ghcr.io/skyfrog4fun/trainings:latest
    container_name: trainings-web
    restart: unless-stopped
    ports:
      - "8080:8080"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ASPNETCORE_URLS=http://+:8080
      - ConnectionStrings__DefaultConnection=Data Source=/app/data/trainings.db
      - Seed__AdminEmail=${SEED_ADMIN_EMAIL}
      - Seed__AdminPassword=${SEED_ADMIN_PASSWORD}
    volumes:
      - /volume1/docker/trainings/data:/app/data
    networks:
      - trainings-net
    healthcheck:
      test: ["CMD-SHELL", "curl -f http://localhost:8080/ || exit 1"]
      interval: 30s
      timeout: 10s
      retries: 3
      start_period: 15s

networks:
  trainings-net:
    driver: bridge
```

**Note:**  
- When running this on your Synology NAS, always use the `/volume1/...` Linux path for volumes.  
- If you want to edit files from Windows, map the NAS share (e.g. `\\skynas24\docker\trainings`) as a network drive.  
- Do **not** change the `/volume1/...` path in the YAML; Docker on the NAS only understands Linux paths, not Windows UNC paths.

Create a `.env` file in the same directory (never commit this file):

```bash
# /volume1/docker/trainings/.env
SEED_ADMIN_EMAIL=admin@example.com
SEED_ADMIN_PASSWORD=ChangeMe!2025
```

---

## 4. Database Setup (SQLite)

### 4.1 Embedded in the ASP.NET Application

SQLite is file-based and requires no separate server process. The EF Core provider writes directly to a `.db` file. This makes it ideal for low-traffic, single-user or small-team applications.

The connection string is:
```
Data Source=/app/data/trainings.db
```

EF Core Migrations are applied automatically on startup by `DbSeeder`.

### 4.2 Persistent Storage Path

| Environment | Path |
|---|---|
| Development | `trainings.db` (next to executable) |
| Production (container) | `/app/data/trainings.db` |
| Production (NAS host) | `/volume1/docker/trainings/data/trainings.db` |

The `/app/data` directory inside the container is a bind mount to the NAS.

### 4.3 How to Start a Bash Session on the NAS

You need a bash session on the NAS to run host-level commands (e.g. setting file permissions, managing Docker volumes). There are two ways to do this:

1. Enable SSH in DSM: **Control Panel** → **Terminal & SNMP** → tick **Enable SSH service** → **Apply**.
2. From any terminal on your local machine, connect:

   ```bash
   ssh your-admin-user@skynas24
   # or use the NAS IP address directly
   ssh your-admin-user@192.168.1.x
   ```

3. You will land in a bash shell as your DSM user. To run commands as root, prefix them with `sudo`. The exact commands to run are listed in [Section 4.4](#44-file-permissions-and-security).

4. **Disable SSH again** when you are done (re-open **Control Panel** → **Terminal & SNMP** → untick **Enable SSH service** → **Apply**).

> **Security note:** Follow the hardening steps in [Section 2.5](#25-ssh-configuration-optional) before enabling SSH on a production NAS. Never leave SSH enabled when not in use.


### 4.4 File Permissions and Security

```bash
# Lock down the data directory
sudo chown -R 1654:1654 /volume1/docker/trainings/data
sudo chmod 700 /volume1/docker/trainings/data

# Create a blank placeholder file and set its permissions now
sudo touch /volume1/docker/trainings/data/trainings.db
sudo chown 1654:1654 /volume1/docker/trainings/data/trainings.db
sudo chmod 600 /volume1/docker/trainings/data/trainings.db
```

The container runs as a non-root user (UID 1654, the built-in `app` user from the .NET aspnet base image — this is a fixed value set by Microsoft and does not change between builds). The data directory and database file should only be accessible to that user.

> **Note:** Creating the blank placeholder file upfront lets you set permissions correctly before the container ever starts. EF Core Migrations will initialize the SQLite database inside the existing empty file on first startup — no data is lost.

> **Important:** The `.db` file is the entire database. Do not expose it via file sharing (SMB/NFS). Only the container process should access it.

### 4.5 Backup Strategy

| Method | Description |
|---|---|
| File copy | `cp trainings.db trainings.db.bak` — safe when app is stopped |
| Online backup | Use SQLite's `.backup` command or the `VACUUM INTO` SQL statement to create a consistent copy while the app is running |
| Synology Hyper Backup | Schedule daily backups of `/volume1/docker/trainings/data/` to an external destination |

**Recommended daily backup script via DSM Task Scheduler:**

**Step 1 — Create the `backups` shared folder in DSM**

The script writes backups to `/volume1/backups/trainings/`. The top-level `backups` directory **must** be registered as a DSM Shared Folder before the script runs. If the script creates it first as a raw directory, DSM will refuse to register it later with the error _"A folder with the same name already exists on this volume"_.

1. **Control Panel** → **Shared Folder** → **Create** → **Create Shared Folder**.
2. Fill in:
   - **Name:** `backups`
   - **Location:** `Volume 1`
3. On the permissions step, grant your named admin user **Read/Write**. `docker-svc` does not need access here.
4. Click **Apply**.

The `trainings` subdirectory inside it will be created automatically by the script on first run.

**Step 2 — Create the scheduled task**

1. **Control Panel** → **Task Scheduler** → **Create** → **Scheduled Task** → **User-defined script**.
2. On the **General** tab:
   - **Task name:** `Trainings DB Backup`
   - **User:** `root`
   - **Enabled:** ticked

   > **Why `root`?** The database file is owned by UID 1654 (the container's `app` user) with permissions `600` — only its owner and root can read it. The `sqlite3` binary is also only reliably available to root on DSM. Running as your admin account or `docker-svc` will fail because DSM task accounts do not have a full shell environment and `docker-svc` has no application permissions by design (see [Section 2.4](#24-user-permissions)).

3. On the **Schedule** tab:
   - **Run on the following days:** Daily
   - **Time:** `02:00` (or any quiet off-peak time)

4. On the **Task Settings** tab, paste the script below into the **User-defined script** box.

5. Optionally tick **Send run details by email** and enter your email address to receive a report (including errors) after each run.

6. Click **OK**.

**Step 3 — The script**

```bash
#!/bin/bash
set -euo pipefail

DB=/volume1/docker/trainings/data/trainings.db
BACKUP_DIR=/volume1/backups/trainings
KEEP_DAYS=30

mkdir -p "$BACKUP_DIR"

# VACUUM INTO creates a consistent, defragmented copy — safe while the app is running
sqlite3 "$DB" "VACUUM INTO '$BACKUP_DIR/trainings-$(date +%Y%m%d-%H%M%S).db'"

# Remove backups older than KEEP_DAYS days
find "$BACKUP_DIR" -name "trainings-*.db" -mtime +"$KEEP_DAYS" -delete

echo "Backup completed: $(date)"
```

**Step 4 — Run the task manually to verify**

1. In **Task Scheduler**, select the `Trainings DB Backup` task and click **Run**.
2. Wait a few seconds, then check the result:
   - Select the task → **Action** → **View Result**
   - The output should end with `Backup completed: <timestamp>` and show exit code `0`.
   - If you see an error, the most common cause is that `trainings.db` does not yet exist (the container has never started) — start the container first and let EF Core create the database, then re-run the task.
3. Verify the backup file is visible in three ways:
   - **File Station** → `backups` → `trainings` → you should see `trainings-YYYYMMDD-HHMMSS.db`
   - **Windows Explorer** → `\\skynas24\backups\trainings` → same file
   - **SSH:** `ls /volume1/backups/trainings/`

### 4.6 Limitations

| Limitation | Detail |
|---|---|
| Concurrent writes | SQLite uses a single writer at a time (WAL mode allows concurrent reads). Not suitable for high write concurrency. |
| Multi-user access | Only one process should access the file at a time. Do not mount the same file in two containers. |
| Replication | SQLite has no built-in replication. Use file-level backups only. |
| Maximum DB size | Practical limit ~1 TB; this app is well within limits. |

---

## 5. Web Application Container

### 5.1 Dockerfile

Save the content below as a file named exactly `Dockerfile` (no extension) in the repository root — i.e. `Trainings/Dockerfile`, the same level as `Trainings.slnx`:

```dockerfile
# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/Trainings.Web/Trainings.Web.csproj", "src/Trainings.Web/"]
COPY ["src/Trainings.Application/Trainings.Application.csproj", "src/Trainings.Application/"]
COPY ["src/Trainings.Domain/Trainings.Domain.csproj", "src/Trainings.Domain/"]
COPY ["src/Trainings.Infrastructure/Trainings.Infrastructure.csproj", "src/Trainings.Infrastructure/"]
RUN dotnet restore "src/Trainings.Web/Trainings.Web.csproj"

COPY . .
RUN dotnet publish "src/Trainings.Web/Trainings.Web.csproj" \
    --configuration Release \
    --output /app/publish \
    --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

RUN mkdir -p /app/data && \
    chown -R app:app /app

USER app

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "Trainings.Web.dll"]
```

### 5.2 .dockerignore

Place `.dockerignore` at the root of the repository to keep build contexts small:

```
**/.git
**/.gitignore
**/bin
**/obj
**/.vs
**/*.user
**/node_modules
docs
*.md
.env
**/.env
*.env
```

### 5.3 Environment Variables

These variables are already wired into the `docker-compose.yml` from [Section 3.6](#36-docker-composeyml). You do **not** set them manually — they are read automatically when the container starts.

- The non-secret variables (`ASPNETCORE_ENVIRONMENT`, `ASPNETCORE_URLS`, `ConnectionStrings__DefaultConnection`) are hardcoded directly in `docker-compose.yml`.
- The secret variables (`Seed__AdminEmail`, `Seed__AdminPassword`) are referenced as `${SEED_ADMIN_EMAIL}` / `${SEED_ADMIN_PASSWORD}` in `docker-compose.yml` and must be defined in the `.env` file on the NAS (see [Section 3.6](#36-docker-composeyml)).

| Variable | Description | Where set |
|---|---|---|
| `ASPNETCORE_ENVIRONMENT` | Runtime environment | `Production` |
| `ASPNETCORE_URLS` | Listening URL | `http://+:8080` |
| `ConnectionStrings__DefaultConnection` | SQLite connection string | `Data Source=/app/data/trainings.db` |
| `Seed__AdminEmail` | Initial admin user email | `admin@example.com` |
| `Seed__AdminPassword` | Initial admin user password | `ChangeMe!2025` |

| `ASPNETCORE_ENVIRONMENT` | Runtime environment (`Production`) | `docker-compose.yml` |
| `ASPNETCORE_URLS` | Listening URL (`http://+:8080`) | `docker-compose.yml` |
| `ConnectionStrings__DefaultConnection` | SQLite connection string | `docker-compose.yml` |
| `Seed__AdminEmail` | Initial admin user email | `.env` file on NAS |
| `Seed__AdminPassword` | Initial admin user password | `.env` file on NAS |

> **Important:** Replace the placeholder values in `.env` with strong, unique credentials before starting the container for the first time. The seed admin account is created on first startup — if the password is weak it cannot easily be changed afterwards without direct database access.

### 5.4 Prerequisites: Docker Desktop on Your Development Machine

Before you can build or test the image locally, you need Docker installed on your Windows machine.

1. Download and install **Docker Desktop for Windows** from [https://www.docker.com/products/docker-desktop](https://www.docker.com/products/docker-desktop).
2. The installer does not prompt for a backend selection. When the installer finishes, **restart Windows** when prompted.
3. After the reboot, Docker Desktop starts automatically and may trigger WSL 2 setup. If prompted, open **Windows Terminal** and run `wsl` — Windows will install or update WSL 2 automatically. Once that completes, run `wsl --update` to confirm WSL is at the latest version. Wait until the Docker Desktop system tray icon shows **"Docker Desktop is running"**.
4. Verify the installation in a terminal:
   ```powershell
   docker --version
   docker compose version
   ```
   Both commands should print a version number without errors.

> **Note:** Docker Desktop only needs to be installed on your **development machine**. The NAS runs Docker through Container Manager — not Docker Desktop.

### 5.5 Building the Image Locally

Run these commands in a **Windows terminal (PowerShell or cmd) on your development machine**, from the repository root (`Trainings/` — the folder containing `Dockerfile` and `Trainings.slnx`). Docker Desktop must be running.

> **Prerequisite:** The `ghcr.io/skyfrog4fun/trainings` package must be set to **Public** on GitHub before the NAS can pull it without authentication. Go to **github.com/skyfron4fun** → **Packages** → **trainings** → **Package settings** → **Change visibility** → **Public**.

> **Note:** On Synology DSM, Docker commands require `sudo` because the `docker` group is not available to non-root users.

```bash
# SSH into the NAS (or use DSM Task Scheduler)
cd /volume1/docker/trainings

# Pull the latest image
sudo docker compose pull

# Recreate the container with the new image (zero-downtime for SQLite apps)
sudo docker compose up -d --remove-orphans

# Remove old images
sudo docker image prune -f
```

---

## 6. Reverse Proxy Configuration

The DSM built-in Reverse Proxy uses nginx internally and provides a UI for configuration.

### 6.1 Steps to Configure

1. **Control Panel** → **Login Portal** → **Advanced** → **Reverse Proxy** → **Create**.
2. Fill in the form:

| Field | Value |
|---|---|
| Description | `Trainings Web App` |
| Source Protocol | `HTTPS` |
| Source Hostname | `trainings.planetfrey.ch` |
| Source Port | `443` |
| Enable HSTS | Yes |
| Destination Protocol | `HTTP` |
| Destination Hostname | `localhost` |
| Destination Port | `8080` |

3. On the **Custom Header** tab, add:

| Header | Value |
|---|---|
| `X-Real-IP` | `$remote_addr` |
| `X-Forwarded-For` | `$proxy_add_x_forwarded_for` |
| `X-Forwarded-Proto` | `$scheme` |

4. Enable **WebSocket support** (required for Blazor Server SignalR):
   - On the **Custom Header** tab, click **Create** → **WebSocket**.

5. Click **Save**.

### 6.2 Multiple Applications Example

trainings.planetfrey.ch  → localhost:8080  (Trainings app)  
blog.planetfrey.ch       → localhost:8090  (e.g. a static blog container)

For example, if you have a second web application (such as a static site or another containerized app), create a new reverse proxy entry:

| Source Hostname         | Source Port | Destination Hostname | Destination Port | Description         |
|------------------------|-------------|---------------------|------------------|---------------------|
| trainings.planetfrey.ch | 443         | localhost           | 8080             | Trainings app       |
| blog.planetfrey.ch      | 443         | localhost           | 8090             | Blog/static website |

Each entry maps a different domain/subdomain to a specific backend container port.  
**Note:** Do not use the DSM management interface (ports 5000/5001) as a reverse proxy target; DSM manages its own reverse proxy and should not be exposed via custom entries. 

Each entry in the Reverse Proxy list maps a domain to a backend port.

## 7. Dynamic DNS Setup

Because most home internet connections use a dynamic public IP, a Dynamic DNS (DDNS) service updates the DNS record automatically when the IP changes.

Synology provides a built-in DDNS service at `*.myds.me`, which is the simplest option and requires no external accounts.

### 7.1 Registering the Synology DDNS Hostname

1. **Control Panel** → **External Access** → **DDNS** → **Add**.
2. Service provider: **Synology**.
3. Hostname: `skyfrog.myds.me`.
4. Click **Test Connection** → **OK**.

### 7.2 Pointing Your Custom Domain to the NAS

After registering the Synology DDNS hostname, create a **CNAME** record at your domain registrar (wherever `planetfrey.ch` is managed):

| Field | Value |
|---|---|
| Name | `trainings` |
| Type | `CNAME` |
| Value | `skyfrog.myds.me` |

This ensures `trainings.planetfrey.ch` always resolves to your NAS's current public IP, even when the IP changes.

> **Note:** DNS propagation can take a few minutes to a few hours. Verify the CNAME resolves correctly before requesting the Let's Encrypt certificate: run `nslookup trainings.planetfrey.ch` and confirm it returns your public IP.

### 7.3 Security Implications

| Risk | Mitigation |
|---|---|
| Public IP exposure | Your real IP is directly exposed — keep router firewall rules tight (ports 80 and 443 only) |
| DNS hijacking | Use HTTPS with a valid certificate (Let's Encrypt via DSM) |

---

## 8. Router Configuration

### 8.1 Port Forwarding Rules

Add the following NAT rules on the home router:

| Protocol | External Port | Internal IP | Internal Port | Purpose |
|---|---|---|---|---|
| TCP | 80 | NAS IP | 80 | Let's Encrypt HTTP challenge / HTTP→HTTPS redirect |
| TCP | 443 | NAS IP | 443 | HTTPS (application traffic) |

Replace `NAS IP` with the NAS's local IP address (e.g. `192.168.1.100`).

> **Tip:** Assign the NAS a static local IP (DHCP reservation on the router) to prevent the NAS IP from changing after a router reboot.

### 8.2 Finding the NAS IP

```
DSM → Control Panel → Network → Network Interface → LAN 1 → IP address
```

### 8.3 Router-Specific Instructions

Port forwarding UIs differ between router brands. Common paths:

| Brand | Menu Path |
|---|---|
| FRITZ!Box | Internet → Permit Access → Port Sharing |
| Asus | WAN → Virtual Server / Port Forwarding |
| TP-Link | Advanced → NAT Forwarding → Virtual Servers |
| Netgear | Dynamic DNS / Port Forwarding |

### 8.4 Security Best Practices

- **Only expose ports 80 and 443.** Never forward SSH (22), DSM (5000/5001), or database ports.
- **Disable UPnP** on the router to prevent containers from auto-opening ports.
- **Block inbound access to DSM** from the internet (restrict port 5000/5001 to the local subnet in the NAS firewall).
- Consider placing the NAS in a DMZ only if you fully trust the firewall rules on the NAS itself.

---

## 9. HTTPS and Certificates

> **Prerequisites — complete these before requesting a certificate:**
> 1. Synology DDNS registered and `trainings.planetfrey.ch` CNAME resolves to your NAS's public IP — see [Section 7](#7-dynamic-dns-setup).
> 2. Router port forwarding for ports 80 and 443 configured and verified open — see [Section 8](#8-router-configuration) and [Section 2.3.1](#231-verifying-port-reachability).

### 9.1 Requesting a Let's Encrypt Certificate

1. **Control Panel** → **Security** → **Certificate** → **Add** → **Add a new certificate**.
2. Select **Get a certificate from Let's Encrypt**.
  - **Important:** When prompted, **do not tick "Set as default certificate"**. This ensures the new certificate is only used for the specified domain and does not override the default certificate for DSM or other services.
3. Fill in:
  - **Domain name:** `trainings.planetfrey.ch`
  - **Email:** your email address
  - **Subject Alternative Name:** leave blank
4. Click **Done**.

DSM will request and automatically store the certificate.

### 9.2 Binding the Certificate to the Reverse Proxy

1. **Control Panel** → **Security** → **Certificate** → select the certificate → **Edit** → **Services**.
2. Find the reverse proxy entry for `trainings.planetfrey.ch` and assign the certificate to it.
3. Click **OK**.

### 9.3 Automatic Renewal

DSM renews Let's Encrypt certificates automatically 30 days before expiry. No manual action is required. Ensure:
- Port 80 remains forwarded (for HTTP-01 renewal).
- The NAS has internet connectivity.

---

## 10. Security Considerations

### 10.1 Firewall Configuration (Summary)

The NAS firewall must block all ports except 80 and 443 from the public internet. Local subnet traffic can be permitted broadly. See [Section 2.3](#23-firewall-configuration) for the full rule set.

### 10.2 Container Isolation

- The container runs as a **non-root user** (UID 1654, the built-in `app` user from the .NET aspnet base image).
- Only port 8080 is published to the host. The container cannot reach other services on the host network unless explicitly configured.
- The Docker network `trainings-net` is isolated from other Docker networks.

### 10.3 Database Access Control

- The SQLite file is only accessible from inside the container via the bind mount.
- File permissions on the NAS restrict access to the container's `app` user (UID 1654).
- The database is never exposed to the internet; it is accessed only through the ASP.NET application layer.

### 10.4 Secure Credential Storage

| What | How |
|---|---|
| Admin seed password | `.env` file on NAS, not in image or source control |
| Database (SQLite) | No password needed; secured by file permissions |
| Let's Encrypt | Managed by DSM, stored in DSM keystore |

The `.env` file must have restrictive permissions:
```bash
sudo chmod 600 /volume1/docker/trainings/.env
```

### 10.5 Least Privilege Principle

| Component | Privilege Level |
|---|---|
| Container process | Non-root (UID 1654, built-in `app` user) |
| Volume access | Only the container user |
| NAS Docker user | No admin rights |
| DSM admin account | Named account, 2FA enabled, not `admin` |

### 10.6 Deployment Strategy: NAS Pulls, GitHub Never Pushes

A common alternative for automated deployment is for GitHub Actions to SSH into the NAS after a Docker image is pushed and run `docker compose pull && up -d`. **This approach is intentionally not used here** because it requires the NAS to have SSH (port 22) permanently reachable from the internet — which contradicts the firewall rule in [Section 10.1](#101-firewall-configuration-summary) that allows only ports 80 and 443 from external sources.

Instead, deployment is **pull-based**: the NAS reaches out to the registry, not the other way around. Watchtower runs inside Docker on the NAS, polls the GitHub Container Registry (ghcr.io) every few minutes, and automatically pulls and restarts the container when a new image is available. No inbound firewall rule changes are required.

| Approach | NAS port 22 open to internet? | Preferred |
|---|---|---|
| GitHub Actions SSHes into NAS | Yes | ✗ |
| NAS polls ghcr.io (Watchtower) | No | ✓ |

### 10.7 Optional Security Enhancements

#### VPN Access

For administrative access to DSM, consider using **Synology VPN Server** instead of exposing DSM ports to the internet. This keeps port 5000/5001 fully closed to the public.

1. Install **VPN Server** from Package Center.
2. Configure OpenVPN or L2TP/IPSec.
3. Connect to VPN before accessing DSM remotely.

---

## 11. Deployment Workflow

### 11.1 Overview

```
Developer → git push → GitHub Actions → Build Docker image
                                      → Push to GitHub Container Registry (ghcr.io)

NAS (Watchtower) → polls ghcr.io every 5 minutes
                 → detects new image → docker pull → container restart
```

> The NAS initiates all outbound connections. No inbound SSH or webhook port is required.

### 11.2 GitHub Actions: Build and Publish Docker Image

Create `.github/workflows/docker-publish.yml`:

```yaml
name: Build and Publish Docker Image

on:
  push:
    branches: [main, master]
    tags: ["v*.*.*"]

env:
  REGISTRY: ghcr.io
  IMAGE_NAME: ${{ github.repository }}

jobs:
  build-and-push:
    runs-on: ubuntu-latest
    permissions:
      contents: read
      packages: write

    steps:
      - name: Checkout repository
        uses: actions/checkout@v4

      - name: Log in to GitHub Container Registry
        uses: docker/login-action@v3
        with:
          registry: ${{ env.REGISTRY }}
          username: ${{ github.actor }}
          password: ${{ secrets.GITHUB_TOKEN }}

      - name: Extract Docker metadata
        id: meta
        uses: docker/metadata-action@v5
        with:
          images: ${{ env.REGISTRY }}/${{ env.IMAGE_NAME }}
          tags: |
            type=raw,value=latest,enable={{is_default_branch}}
            type=ref,event=branch
            type=semver,pattern={{version}}
            type=semver,pattern={{major}}.{{minor}}
            type=sha

      - name: Build and push Docker image
        uses: docker/build-push-action@v6
        with:
          context: .
          push: true
          tags: ${{ steps.meta.outputs.tags }}
          labels: ${{ steps.meta.outputs.labels }}
```

### 11.3 Updating the Container on the NAS

After a new image is pushed to ghcr.io, update the NAS manually or automatically.

**Option A: Manual update (SSH)**
```bash
cd /volume1/docker/trainings
sudo docker compose pull
sudo docker compose up -d --remove-orphans
sudo docker image prune -f
```

**Option B: Automated update with Watchtower**
Watchtower polls the container registry at a configurable interval, pulls new image versions, and restarts the affected containers automatically. Follow the steps below to install and configure it on the NAS.

#### Step 1 – Authenticate with the GitHub Container Registry

Watchtower needs registry credentials so it can pull private images.  
SSH into the NAS and run:

```bash
echo "<YOUR_GITHUB_TOKEN>" | sudo docker login ghcr.io \
  -u <your-github-username> --password-stdin
```

The credentials are saved to `/root/.docker/config.json` and will be used by Watchtower automatically.

> Create a fine-grained Personal Access Token (PAT) with **`read:packages`** scope at
> **GitHub → Settings → Developer settings → Personal access tokens**.

#### Step 2 – Add Watchtower to the compose file

Open `/volume1/docker/trainings/docker-compose.yml` and add the `watchtower` service:

```yaml
services:
  trainings-web:
    image: ghcr.io/<your-github-username>/trainings:main
    container_name: trainings-web
    restart: unless-stopped
    # … rest of the existing service definition …

  watchtower:
    image: containrrr/watchtower
    container_name: watchtower
    restart: unless-stopped
    volumes:
      - /var/run/docker.sock:/var/run/docker.sock
      - /root/.docker/config.json:/config.json:ro   # Watchtower reads registry credentials from /config.json by default
    environment:
      - WATCHTOWER_CLEANUP=true            # remove old images after update
      - WATCHTOWER_POLL_INTERVAL=300       # check for updates every 5 minutes
      - WATCHTOWER_INCLUDE_STOPPED=false   # only watch running containers
      - WATCHTOWER_NOTIFICATIONS=shoutrrr  # optional – see Step 5 for notifications
    command: trainings-web                 # only watch this container
```
> **Tip:** Omit the `command:` line entirely if you want Watchtower to monitor *all* containers on the host.

#### Step 3 – Start Watchtower

```bash
cd /volume1/docker/trainings
sudo docker compose pull watchtower   # pull the latest Watchtower image
sudo docker compose up -d watchtower  # start only the new service
```

Verify the container is running:

```bash
sudo docker ps | grep watchtower
```

#### Step 4 – Verify it is working

Check Watchtower logs to confirm it started correctly and can reach the registry:

```bash
sudo docker logs watchtower --tail 50
```

Expected output on first run (no update available yet):

```
time="…" level=info msg="Watchtower 1.x.x"
time="…" level=info msg="Starting Watchtower and scheduling first run: …"
time="…" level=info msg="Checking all containers (except explicitly disabled with label)"
time="…" level=info msg="Session done" Failed=0 Scanned=1 Updated=0 Fresh=1
```

When a new image is published and Watchtower detects it, you will see:

```
time="…" level=info msg="Found new ghcr.io/…/trainings:main image (sha256:…)"
time="…" level=info msg="Stopping /trainings-web (…) with SIGTERM"
time="…" level=info msg="Creating /trainings-web"
time="…" level=info msg="Session done" Failed=0 Scanned=1 Updated=1 Fresh=0
```

#### Step 5 – (Optional) Enable update notifications

Watchtower can send notifications via e-mail, Slack, Telegram, or any URL supported by [shoutrrr](https://containrrr.dev/shoutrrr). Example for a generic webhook:

```yaml
environment:
  - WATCHTOWER_NOTIFICATION_URL=generic://your-webhook-host/path
```

See the [Watchtower notification docs](https://containrrr.dev/watchtower/notifications/) for all supported providers.

#### Step 6 – (Optional) Use a scheduled time instead of polling

Replace the poll interval with a cron expression to update only at a specific time (e.g. 03:00 every day):

```yaml
environment:
  - WATCHTOWER_CLEANUP=true
  - WATCHTOWER_SCHEDULE=0 0 3 * * *   # Watchtower extended cron: sec min hour day month weekday
```

Remove `WATCHTOWER_POLL_INTERVAL` when using `WATCHTOWER_SCHEDULE`.

#### Troubleshooting

| Symptom | Likely cause | Fix |
|---|---|---|
| `unauthorized` in logs | Missing or expired registry credentials | Re-run `docker login` (Step 1) |
| Container not restarted after push | Wrong container name in `command:` | Check `docker ps --format '{{.Names}}'` and match exactly |
| Old image not removed | `WATCHTOWER_CLEANUP` not set | Add `WATCHTOWER_CLEANUP=true` to environment |
| Watchtower restarts itself | No fixed tag pinned for Watchtower image | Pin a specific version, e.g. `containrrr/watchtower:1.7.1` |

> **Security note:** Watchtower requires access to the Docker socket, which grants root-level access to the host. Mount the Docker socket **read-only** where possible, limit the containers Watchtower monitors using the `command:` argument or container labels (`com.centurylinklabs.watchtower.enable=false`), and keep the Watchtower image itself up to date.


### 11.4 GitHub Packages: Making the Image Accessible

The GitHub Container Registry package is private by default. Either:

- Make it public: GitHub → **Packages** → select image → **Package settings** → **Change visibility** → **Public**.
- Or authenticate on the NAS:
  ```bash
  echo $GITHUB_TOKEN | docker login ghcr.io -u <github-username> --password-stdin
  ```
  Create a fine-grained Personal Access Token with `read:packages` scope.

---

## 12. Backup Strategy

### 12.1 SQLite Database

The database file is the most critical asset. Back it up daily.

| Method | Command / Tool | Notes |
|---|---|---|
| Online hot backup | `sqlite3 trainings.db "VACUUM INTO 'backup.db'"` | Safe while app is running |
| File copy (cold) | `cp trainings.db trainings.db.bak` | Stop app first |
| Synology Hyper Backup | Schedule task targeting `/volume1/docker/trainings/data/` | Encrypts and stores off-site |

**Automated daily backup** (add to DSM Task Scheduler → User-Defined Script):

```bash
#!/bin/bash
set -e
DB=/volume1/docker/trainings/data/trainings.db
BACKUP_DIR=/volume1/backups/trainings
KEEP_DAYS=30

mkdir -p "$BACKUP_DIR"
sqlite3 "$DB" "VACUUM INTO '$BACKUP_DIR/trainings-$(date +%Y%m%d-%H%M%S).db'"
find "$BACKUP_DIR" -name "trainings-*.db" -mtime +"$KEEP_DAYS" -delete
echo "Backup completed: $(date)"
```

To add this to DSM Task Scheduler:
1. **Control Panel** → **Task Scheduler** → **Create** → **Scheduled Task** → **User-Defined Script**.
2. Set the schedule (e.g. daily at 02:00).
3. Paste the script.
4. Set **User** to `root` (required for sqlite3).

### 12.2 Container Configuration

Back up these files on the NAS:

| File | Purpose |
|---|---|
| `/volume1/docker/trainings/docker-compose.yml` | Container definition |
| `/volume1/docker/trainings/.env` | Runtime secrets (back up securely, not to public storage) |

These files are small and can be included in Synology Hyper Backup or stored in a private git repository.

### 12.3 Persistent Volumes

| Volume | Path | Backup method |
|---|---|---|
| SQLite database | `/volume1/docker/trainings/data/` | Hyper Backup + daily script |
| Container config | `/volume1/docker/trainings/` | Hyper Backup |

### 12.4 Restore Procedure

```bash
# 1. Stop the container
cd /volume1/docker/trainings
docker compose stop

# 2. Replace the database file
cp /volume1/backups/trainings/trainings-20250101-020000.db \
   /volume1/docker/trainings/data/trainings.db

# 3. Restart the container
docker compose up -d
```

### 12.5 Offsite Backup

Configure Synology Hyper Backup to replicate to:
- A Synology C2 Storage subscription (Synology's own cloud)
- An Amazon S3 bucket
- Another NAS or Synology device at a different location

Encrypt the backup with a strong passphrase and store the passphrase separately from the backup.

---

## Appendix A: Quick Start Checklist

Use this checklist when setting up the environment on a new NAS:

- [ ] Install Container Manager from Package Center
- [ ] Create `docker-svc` user with access to `/volume1/docker/`
- [ ] Configure NAS firewall (allow 80, 443, local subnet; deny all else)
- [ ] Disable default `admin` account, enable 2FA on admin account
- [ ] Create `/volume1/docker/trainings/data/` directory
- [ ] Create `/volume1/docker/trainings/docker-compose.yml` (from Section 3.6)
- [ ] Create `/volume1/docker/trainings/.env` with secrets (from Section 3.5)
- [ ] Set permissions: `chmod 600 /volume1/docker/trainings/.env`
- [ ] Set up Dynamic DNS and CNAME record (Section 7)
- [ ] Configure router port forwarding: 80 and 443 → NAS (Section 8)
- [ ] Verify ports 80 and 443 are open from the internet (Section 2.3.1)
- [ ] Configure DSM Reverse Proxy entry (Section 6)
- [ ] Request Let's Encrypt certificate in DSM (Section 9)
- [ ] Start the container: `docker compose up -d`
- [ ] Verify the application is accessible at `https://trainings.planetfrey.ch`
- [ ] Set up automated backup task in DSM Task Scheduler (Section 12.1)
- [ ] Configure Synology Hyper Backup for offsite backups (Section 12.5)

## Appendix B: Troubleshooting

| Problem | Likely Cause | Resolution |
|---|---|---|
| Certificate request fails | Port 80 not reachable | Check router port forwarding and NAS firewall |
| Container not starting | Wrong image name or port conflict | Check `docker compose logs trainings-web` |
| Database file not found | Volume not mounted correctly | Verify bind mount path in docker-compose.yml |
| 502 Bad Gateway | Container not running or wrong port | Check container is running: `docker ps` |
| Blazor SignalR disconnects | WebSocket not enabled on proxy | Enable WebSocket support in DSM Reverse Proxy |
| `docker compose pull` — permission denied | Docker socket not accessible without root | Use `sudo docker compose pull` on Synology DSM |
| `docker compose pull` — manifest unknown | Image tagged `:latest` not yet published | Ensure the GitHub Actions workflow has run and pushed the `latest` tag |
