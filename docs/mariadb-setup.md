# MariaDB Setup – Synology NAS 224+

This document describes how to set up two MariaDB instances (Production and Development) on a **Synology NAS DS224+** using Docker and Docker Compose via **Container Manager**.

---

## Prerequisites

| Requirement | Details |
|---|---|
| **Hardware** | Synology NAS DS224+ |
| **DSM Version** | DSM 7.2 or later |
| **Package** | Container Manager (installed via Package Center) |
| **Network** | NAS reachable on your local network (static IP recommended) |

---

## Architecture Decision: Two Instances

Two separate MariaDB instances are used to cleanly separate Production and Development data:

| Instance | Container name | Host port | Database | Purpose |
|---|---|---|---|---|
| **Production** | `trainings-mariadb-prod` | `3306` | `trainings_prod` | Live application data |
| **Development** | `trainings-mariadb-dev` | `3307` | `trainings_dev` | Local development & testing |

Both instances run inside isolated Docker networks and use named volumes for persistent storage.

---

## Step 1 – Enable SSH on the Synology NAS

1. Open **DSM** → **Control Panel** → **Terminal & SNMP**.
2. Check **Enable SSH service** and click **Apply**.
3. Connect via SSH: `ssh admin@<nas-ip>`

---

## Step 2 – Install Container Manager

1. Open **Package Center** in DSM.
2. Search for **Container Manager** and click **Install**.
3. After installation, open **Container Manager**.

---

## Step 3 – Upload the Docker Compose files

Copy the repository files to the NAS via SSH or File Station:

```
/volume1/docker/trainings/
  docker-compose.yml          ← Production instance
  docker-compose.dev.yml      ← Development instance
  db/
    init/                     ← Optional SQL init scripts (empty by default)
```

Via SSH:
```bash
scp docker-compose.yml admin@<nas-ip>:/volume1/docker/trainings/
scp docker-compose.dev.yml admin@<nas-ip>:/volume1/docker/trainings/
```

---

## Step 4 – Configure passwords

Edit `docker-compose.yml` (production) and replace all `CHANGE_ME_*` placeholders:

| Variable | Purpose |
|---|---|
| `MYSQL_ROOT_PASSWORD` | MariaDB root user password |
| `MYSQL_PASSWORD` | Password for the application user (`trainings_user`) |

Edit `docker-compose.dev.yml` (development) in the same way:

| Variable | Purpose |
|---|---|
| `MYSQL_ROOT_PASSWORD` | MariaDB root user password (dev) |
| `MYSQL_PASSWORD` | Password for the dev application user (`trainings_dev_user`) |

> **Security note:** Never commit passwords to version control. Use environment files (`.env`) or a secret manager for production deployments.

---

## Step 5 – Start the containers

SSH into the NAS and navigate to the project folder:

```bash
cd /volume1/docker/trainings
```

Start **Production** instance:
```bash
docker compose up -d
```

Start **Development** instance:
```bash
docker compose -f docker-compose.dev.yml up -d
```

Verify both containers are running:
```bash
docker ps
```

Expected output:
```
CONTAINER ID   IMAGE          COMMAND                  STATUS          PORTS                    NAMES
xxxxxxxxxxxx   mariadb:11.4   "docker-entrypoint.s…"   Up (healthy)   0.0.0.0:3306->3306/tcp   trainings-mariadb-prod
xxxxxxxxxxxx   mariadb:11.4   "docker-entrypoint.s…"   Up (healthy)   0.0.0.0:3307->3306/tcp   trainings-mariadb-dev
```

---

## Step 6 – Configure application connection strings

Update the connection strings with the actual NAS IP address and the passwords you chose.

**`appsettings.json`** (Production):
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=192.168.1.x;Port=3306;Database=trainings_prod;Uid=trainings_user;Pwd=<your-prod-password>;"
  }
}
```

**`appsettings.Development.json`** (Development):
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=192.168.1.x;Port=3307;Database=trainings_dev;Uid=trainings_dev_user;Pwd=<your-dev-password>;"
  }
}
```

Replace `192.168.1.x` with the actual static IP of your Synology NAS.

> **Tip:** For local development you can also use an `.env` file or user secrets (`dotnet user-secrets`) to avoid storing credentials in source files.

---

## Step 7 – Database schema creation

The application uses Entity Framework Core with `EnsureCreatedAsync` to automatically create all tables on the first startup. No manual schema setup is needed.

If you later switch to EF Core migrations, run:
```bash
dotnet ef migrations add InitialCreate --project src/Trainings.Infrastructure --startup-project src/Trainings.Web
dotnet ef database update --project src/Trainings.Infrastructure --startup-project src/Trainings.Web
```

---

## Useful management commands

```bash
# View logs for the production container
docker logs -f trainings-mariadb-prod

# Open an interactive MariaDB shell (production)
docker exec -it trainings-mariadb-prod mariadb -u trainings_user -p trainings_prod

# Open an interactive MariaDB shell (development)
docker exec -it trainings-mariadb-dev mariadb -u trainings_dev_user -p trainings_dev

# Stop production instance
docker compose down

# Stop development instance
docker compose -f docker-compose.dev.yml down

# Create a manual backup
docker exec trainings-mariadb-prod mysqldump -u root -p trainings_prod > backup_prod.sql
```

---

## Firewall / Network Security

On the Synology NAS, restrict access to the MariaDB ports:

1. Open **DSM** → **Control Panel** → **Security** → **Firewall**.
2. Add a rule to allow TCP port `3306` and `3307` **only from trusted IP ranges** (e.g. your development machine or the application server).
3. Block both ports from external/internet access.

---

## Backup Strategy

- Use **Hyper Backup** (DSM package) to schedule regular backups of the Docker volumes:
  - `/volume1/@docker/volumes/trainings_prod_data`
  - `/volume1/@docker/volumes/trainings_dev_data`
- Alternatively schedule a cron job to run `mysqldump` and store the dump on the NAS or a remote destination.
