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
7. [HTTPS and Certificates](#7-https-and-certificates)
8. [Dynamic DNS Setup](#8-dynamic-dns-setup)
9. [Router Configuration](#9-router-configuration)
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
Public Domain (e.g. trainings.example.com)
    │
    ▼
Dynamic DNS Provider (Cloudflare / Synology DDNS)
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
    │  Routes trainings.example.com → localhost:8080
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
| Dynamic DNS | Cloudflare / Synology DDNS | Updates the DNS record when the ISP changes the public IP |
| Router Port Forwarding | Home Router NAT | Forwards TCP 80 and 443 from the internet to the NAS |
| NAS Reverse Proxy | DSM built-in (nginx) | Terminates TLS, routes requests to the correct container port |
| Docker Container | Synology Container Manager | Runs the ASP.NET Core application in isolation |
| SQLite Database | EF Core + SQLite | Embedded in the application; persisted in a Docker volume |

### Domain Resolution Flow

1. User browser resolves `trainings.example.com` via DNS.
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
    image: ghcr.io/<github-username>/trainings:latest
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

### 4.3 File Permissions and Security

```bash
# On the NAS, set ownership and permissions before starting the container
chown -R 1000:1000 /volume1/docker/trainings/data
chmod 700 /volume1/docker/trainings/data
chmod 600 /volume1/docker/trainings/data/trainings.db  # after first run
```

The container runs as a non-root user (UID 1000). The data directory should only be accessible to that user.

> **Important:** The `.db` file is the entire database. Do not expose it via file sharing (SMB/NFS). Only the container process should access it.

### 4.4 Backup Strategy

| Method | Description |
|---|---|
| File copy | `cp trainings.db trainings.db.bak` — safe when app is stopped |
| Online backup | Use SQLite's `.backup` command or the `VACUUM INTO` SQL statement to create a consistent copy while the app is running |
| Synology Hyper Backup | Schedule daily backups of `/volume1/docker/trainings/data/` to an external destination |

**Recommended daily backup script** (run via DSM Task Scheduler):

```bash
#!/bin/bash
BACKUP_DIR=/volume1/backups/trainings
mkdir -p "$BACKUP_DIR"
sqlite3 /volume1/docker/trainings/data/trainings.db \
  "VACUUM INTO '$BACKUP_DIR/trainings-$(date +%Y%m%d).db'"
find "$BACKUP_DIR" -name "trainings-*.db" -mtime +30 -delete
```

### 4.5 Limitations

| Limitation | Detail |
|---|---|
| Concurrent writes | SQLite uses a single writer at a time (WAL mode allows concurrent reads). Not suitable for high write concurrency. |
| Multi-user access | Only one process should access the file at a time. Do not mount the same file in two containers. |
| Replication | SQLite has no built-in replication. Use file-level backups only. |
| Maximum DB size | Practical limit ~1 TB; this app is well within limits. |

---

## 5. Web Application Container

### 5.1 Dockerfile

Place this `Dockerfile` at the root of the repository:

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

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

RUN addgroup --system --gid 1000 appgroup && \
    adduser --system --uid 1000 --ingroup appgroup appuser

COPY --from=build /app/publish .

RUN mkdir -p /app/data && \
    chown -R appuser:appgroup /app

USER appuser

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
```

### 5.3 Environment Variables

| Variable | Description | Example |
|---|---|---|
| `ASPNETCORE_ENVIRONMENT` | Runtime environment | `Production` |
| `ASPNETCORE_URLS` | Listening URL | `http://+:8080` |
| `ConnectionStrings__DefaultConnection` | SQLite connection string | `Data Source=/app/data/trainings.db` |
| `Seed__AdminEmail` | Initial admin user email | `admin@example.com` |
| `Seed__AdminPassword` | Initial admin user password | `ChangeMe!2025` |

### 5.4 Building the Image Locally

```bash
# From the repository root
docker build -t trainings:local .

# Run locally with a local data directory
mkdir -p ./local-data
docker run --rm \
  -p 8080:8080 \
  -e ASPNETCORE_ENVIRONMENT=Development \
  -e ConnectionStrings__DefaultConnection="Data Source=/app/data/trainings.db" \
  -v "$(pwd)/local-data:/app/data" \
  trainings:local
```

### 5.5 Updating the Container on the NAS

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
```

Each entry in the Reverse Proxy list maps a domain to a backend port.

---

## 7. HTTPS and Certificates

> **Prerequisite:**  
> Before requesting a Let's Encrypt certificate, your public domain (e.g. `trainings.planetfrey.ch`) must resolve to your NAS's public IP.  
> - If you do **not** have a static public IP, set up Synology DDNS first:  
>   1. Go to **Control Panel** → **External Access** → **DDNS** → **Add**.
>   2. Register a Synology DDNS hostname (e.g. `planetfrey.myds.me`).
> - In your domain registrar or DNS provider, create a **CNAME** record:  
>   - **Name:** `trainings`  
>   - **Type:** `CNAME`  
>   - **Value:** `planetfrey.myds.me`  
>   This ensures `trainings.planetfrey.ch` always points to your NAS, even if your public IP changes.

### 7.1 Requesting a Let's Encrypt Certificate

1. **Control Panel** → **Security** → **Certificate** → **Add** → **Add a new certificate**.
2. Select **Get a certificate from Let's Encrypt**.
3. Fill in:
  - **Domain name:** `trainings.planetfrey.ch`
  - **Email:** your email address
  - **Subject Alternative Name:** leave blank (or add additional subdomains)
4. Click **Done**.

DSM will request and automatically store the certificate.

> **Prerequisite:** Port 80 must be reachable from the internet for the HTTP-01 ACME challenge. Ensure your router forwards port 80 to the NAS before requesting the certificate. You can restrict port 80 again after the certificate is issued (DSM will use a TLS-ALPN challenge for renewals if port 80 is later blocked, or continue using HTTP-01 on renewal if port 80 stays open).

### 7.2 Binding the Certificate to the Reverse Proxy

1. **Control Panel** → **Security** → **Certificate** → select the certificate → **Edit** → **Services**.
2. Find the reverse proxy entry for `trainings.planetfrey.ch` and assign the certificate to it.
3. Click **OK**.

### 7.3 Automatic Renewal

DSM renews Let's Encrypt certificates automatically 30 days before expiry. No manual action is required. Ensure:
- Port 80 remains forwarded (for HTTP-01 renewal).
- The NAS has internet connectivity.


### 7.1 Requesting a Let's Encrypt Certificate

1. **Control Panel** → **Security** → **Certificate** → **Add** → **Add a new certificate**.
2. Select **Get a certificate from Let's Encrypt**.
3. Fill in:
   - **Domain name:** `trainings.example.com`
   - **Email:** your email address
   - **Subject Alternative Name:** leave blank (or add additional subdomains)
4. Click **Done**.

DSM will request and automatically store the certificate.

> **Prerequisite:** Port 80 must be reachable from the internet for the HTTP-01 ACME challenge. Ensure your router forwards port 80 to the NAS before requesting the certificate. You can restrict port 80 again after the certificate is issued (DSM will use a TLS-ALPN challenge for renewals if port 80 is later blocked, or continue using HTTP-01 on renewal if port 80 stays open).

### 7.2 Binding the Certificate to the Reverse Proxy

1. **Control Panel** → **Security** → **Certificate** → select the certificate → **Edit** → **Services**.
2. Find the reverse proxy entry for `trainings.example.com` and assign the certificate to it.
3. Click **OK**.

### 7.3 Automatic Renewal

DSM renews Let's Encrypt certificates automatically 30 days before expiry. No manual action is required. Ensure:
- Port 80 remains forwarded (for HTTP-01 renewal).
- The NAS has internet connectivity.

---

## 8. Dynamic DNS Setup

Because most home internet connections use a dynamic public IP, a Dynamic DNS (DDNS) service updates the DNS record automatically when the IP changes.

### 8.1 Option A: Synology DDNS (Simplest)

Synology provides its own DDNS service at `*.synology.me`.

1. **Control Panel** → **External Access** → **DDNS** → **Add**.
2. Service provider: **Synology**.
3. Hostname: `yourname.synology.me`.
4. Click **Test Connection** → **OK**.

**Limitation:** You get a `*.synology.me` subdomain, not a custom domain.

### 8.2 Option B: Cloudflare DDNS (Recommended)

Cloudflare is the recommended option because it also provides a CDN and DDoS protection layer.

#### Prerequisites

- A domain registered with any registrar, with nameservers pointed to Cloudflare.
- A Cloudflare account (free tier is sufficient).

#### Steps

1. In Cloudflare, create an A record:
   - **Name:** `trainings` (or `@` for the root)
   - **IPv4 address:** your current public IP
   - **Proxy status:** Proxied (orange cloud) — hides your real IP and enables Cloudflare WAF

2. Create a Cloudflare API token:
   - Cloudflare Dashboard → **Profile** → **API Tokens** → **Create Token**.
   - Use the **Edit zone DNS** template.
   - Scope to your specific zone.

3. Deploy a DDNS updater on the NAS. Use the `oznu/cloudflare-ddns` Docker image:

```yaml
# Add to docker-compose.yml or create a separate compose file
services:
  cloudflare-ddns:
    image: oznu/cloudflare-ddns:latest
    restart: unless-stopped
    environment:
      - API_KEY=${CLOUDFLARE_API_TOKEN}
      - ZONE=example.com
      - SUBDOMAIN=trainings
      - PROXIED=true
```

Add to `.env`:
```
CLOUDFLARE_API_TOKEN=your_cloudflare_api_token_here
```

### 8.3 Option C: No-IP

1. Register at [noip.com](https://www.noip.com) and create a hostname.
2. In DSM → **Control Panel** → **External Access** → **DDNS** → **Add** → select **No-IP**.
3. Enter your No-IP credentials and hostname.

### 8.4 Security Implications

| Risk | Mitigation |
|---|---|
| IP exposure | Use Cloudflare proxy (orange cloud) to hide the home IP |
| DDNS credentials compromise | Use scoped API tokens, not account-level credentials |
| DNS hijacking | Enable DNSSEC in Cloudflare |

---

## 9. Router Configuration

### 9.1 Port Forwarding Rules

Add the following NAT rules on the home router:

| Protocol | External Port | Internal IP | Internal Port | Purpose |
|---|---|---|---|---|
| TCP | 80 | NAS IP | 80 | Let's Encrypt HTTP challenge / HTTP→HTTPS redirect |
| TCP | 443 | NAS IP | 443 | HTTPS (application traffic) |

Replace `NAS IP` with the NAS's local IP address (e.g. `192.168.1.100`).

> **Tip:** Assign the NAS a static local IP (DHCP reservation on the router) to prevent the NAS IP from changing after a router reboot.

### 9.2 Finding the NAS IP

```
DSM → Control Panel → Network → Network Interface → LAN 1 → IP address
```

### 9.3 Router-Specific Instructions

Port forwarding UIs differ between router brands. Common paths:

| Brand | Menu Path |
|---|---|
| FRITZ!Box | Internet → Permit Access → Port Sharing |
| Asus | WAN → Virtual Server / Port Forwarding |
| TP-Link | Advanced → NAT Forwarding → Virtual Servers |
| Netgear | Dynamic DNS / Port Forwarding |

### 9.4 Security Best Practices

- **Only expose ports 80 and 443.** Never forward SSH (22), DSM (5000/5001), or database ports.
- **Disable UPnP** on the router to prevent containers from auto-opening ports.
- **Block inbound access to DSM** from the internet (restrict port 5000/5001 to the local subnet in the NAS firewall).
- Consider placing the NAS in a DMZ only if you fully trust the firewall rules on the NAS itself.

---

## 10. Security Considerations

### 10.1 Firewall Configuration (Summary)

The NAS firewall must block all ports except 80 and 443 from the public internet. Local subnet traffic can be permitted broadly. See [Section 2.3](#23-firewall-configuration) for the full rule set.

### 10.2 Container Isolation

- The container runs as a **non-root user** (UID 1000).
- Only port 8080 is published to the host. The container cannot reach other services on the host network unless explicitly configured.
- The Docker network `trainings-net` is isolated from other Docker networks.

### 10.3 Database Access Control

- The SQLite file is only accessible from inside the container via the bind mount.
- File permissions on the NAS restrict access to the `docker-svc` user (UID 1000).
- The database is never exposed to the internet; it is accessed only through the ASP.NET application layer.

### 10.4 Secure Credential Storage

| What | How |
|---|---|
| Admin seed password | `.env` file on NAS, not in image or source control |
| Database (SQLite) | No password needed; secured by file permissions |
| DDNS API token | `.env` file on NAS, not in source control |
| Let's Encrypt | Managed by DSM, stored in DSM keystore |

The `.env` file must have restrictive permissions:
```bash
chmod 600 /volume1/docker/trainings/.env
```

### 10.5 Least Privilege Principle

| Component | Privilege Level |
|---|---|
| Container process | Non-root (UID 1000) |
| Volume access | Only the container user |
| NAS Docker user | No admin rights |
| Cloudflare API token | Scoped to DNS edit on one zone only |
| DSM admin account | Named account, 2FA enabled, not `admin` |

### 10.6 Optional Security Enhancements

#### Cloudflare Proxy (Recommended)

Enable the Cloudflare proxy (orange cloud icon) on the DNS record. This:
- Hides the home IP address from DNS lookups.
- Adds a CDN layer.
- Enables the Cloudflare WAF (free tier provides basic protection).

#### VPN Access

For administrative access to DSM, consider using **Synology VPN Server** instead of exposing DSM ports to the internet. This keeps port 5000/5001 fully closed to the public.

1. Install **VPN Server** from Package Center.
2. Configure OpenVPN or L2TP/IPSec.
3. Connect to VPN before accessing DSM remotely.

#### Zero Trust Networking (Advanced)

Use **Cloudflare Tunnel** (`cloudflared`) to expose the application without opening any inbound ports on the router:

```yaml
services:
  cloudflared:
    image: cloudflare/cloudflared:latest
    restart: unless-stopped
    command: tunnel --no-autoupdate run
    environment:
      - TUNNEL_TOKEN=${CLOUDFLARE_TUNNEL_TOKEN}
```

With Cloudflare Tunnel:
- No port forwarding on the router is needed.
- The NAS initiates an outbound connection to Cloudflare.
- Cloudflare proxies traffic to the application.

---

## 11. Deployment Workflow

### 11.1 Overview

```
Developer → git push → GitHub Actions → Build Docker image
                                      → Push to GitHub Container Registry (ghcr.io)
                                      → SSH/webhook trigger on NAS → docker compose pull && up -d
```

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

Add Watchtower to the compose file to automatically pull and restart updated containers:

```yaml
services:
  watchtower:
    image: containrrr/watchtower
    restart: unless-stopped
    volumes:
      - /var/run/docker.sock:/var/run/docker.sock
    environment:
      - WATCHTOWER_CLEANUP=true
      - WATCHTOWER_POLL_INTERVAL=300   # check every 5 minutes
      - WATCHTOWER_INCLUDE_STOPPED=false
    command: trainings-web
```

> **Security note:** Watchtower requires access to the Docker socket, which grants root-level access. Only add trusted images and keep Watchtower updated.

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
- [ ] Set up Dynamic DNS (Section 8)
- [ ] Configure router port forwarding: 80 and 443 → NAS (Section 9)
- [ ] Request Let's Encrypt certificate in DSM (Section 7)
- [ ] Configure DSM Reverse Proxy entry (Section 6)
- [ ] Start the container: `docker compose up -d`
- [ ] Verify the application is accessible at `https://trainings.example.com`
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
| DDNS not updating | Invalid API token or network issue | Test manually, check `cloudflare-ddns` container logs |
| `docker compose pull` — permission denied | Docker socket not accessible without root | Use `sudo docker compose pull` on Synology DSM |
| `docker compose pull` — manifest unknown | Image tagged `:latest` not yet published | Ensure the GitHub Actions workflow has run and pushed the `latest` tag |
