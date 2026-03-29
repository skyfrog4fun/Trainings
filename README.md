# Trainings

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

## Usage

- Start the web application
- Register as a participant or trainer
- Trainers create sessions and record attendance

## Running Tests

- Run `dotnet test` in the `tests/` directory

## Contributing

Pull requests are welcome. Please see [docs/SPECIFICATION.md](docs/SPECIFICATION.md) for architecture and requirements, and [docs/DEVELOPMENT_WORKFLOW.md](docs/DEVELOPMENT_WORKFLOW.md) for the step-by-step process a code change follows from issue to production.

## License

MIT License

## Contact

For support, open an issue on GitHub.
