# Trainings

Version: **v1.0.7**

## Overview

Trainings is an application for planning and conducting training sessions, as well as reviewing past trainings. Registered participants can sign up or withdraw from sessions and view an overview of their trainings. Trainers create and manage training events, and record definitive attendance.

## Features

- Organize and schedule training sessions
- Participant registration and cancellation
- Trainers manage sessions and attendance
- Overview of past and upcoming trainings
- Role-based access: Admin, Trainer, Participant

## Installation & Setup

- Requires .NET 10 SDK
- Clone the repository
- Run `dotnet restore` in the root directory
- Start with `dotnet run --project src/Trainings.Web`

## Project Structure

- `src/Trainings.Domain`: Core entities and domain logic
- `src/Trainings.Application`: Application services and DTOs
- `src/Trainings.Infrastructure`: Data access and authentication
- `src/Trainings.Web`: Web frontend (Blazor Server)
- `tests/`: Unit tests

## Running Modes

Configure the startup defaults in `src/Trainings.Web/appsettings.json` under `App:Modes`:

- `ReadOnly`: blocks write operations for everyone except SuperAdmins
- `NoEmail`: suppresses outgoing mail and shows the generated message in an in-app preview modal where supported

These values are loaded once at startup. A SuperAdmin can override both modes at any time via **⚙️ Config → Running Modes** without restarting the application. Runtime overrides take effect immediately for all users and are automatically reset to the configured defaults on the next application restart.

## Mail Traffic

The application currently sends these mail types:

- **Account / verification mail**: sent after self-registration and when an admin resends account setup for a user; contains the email verification link and is valid for 3 days
- **Password reset mail**: sent from the "Forgot Password" flow; contains a 1-hour reset link
- **Admin registration notification**: sent to SuperAdmins when a new self-registration is waiting for approval
- **Test mail**: sent from the mail configuration page

## Usage

- Start the web application
- Register as a participant or trainer
- Trainers create sessions and record attendance

## Running Tests

- Run `dotnet test` in the `tests/` directory

## Deployment

### Manual Publish to NAS

SSH into the NAS and run:

```bash
cd /volume1/docker/trainings
sudo docker compose pull
sudo docker compose up -d --remove-orphans
sudo docker image prune -f
```

### Restarting the Container

```bash
cd /volume1/docker/trainings
sudo docker compose restart trainings-web
```

### Checking the Application Log

```bash
# Show the last 100 log lines
sudo docker logs trainings-web --tail 100

# Follow the log in real time (press Ctrl+C to stop)
sudo docker logs trainings-web --follow
```

## Contributing

Pull requests are welcome. Please read [`CONTRIBUTING.md`](CONTRIBUTING.md) for the contribution workflow, language policy, and coding conventions.

For deeper context:
- [`docs/SPECIFICATION.md`](docs/SPECIFICATION.md) — architecture and functional requirements.
- [`docs/DEVELOPMENT_WORKFLOW.md`](docs/DEVELOPMENT_WORKFLOW.md) — step-by-step process from issue to production.
- [`.github/copilot-instructions.md`](.github/copilot-instructions.md) — conventions for AI-assisted contributions.

## License

MIT License

## Contact

For support, open an issue on GitHub.
