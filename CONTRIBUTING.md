# Contributing to Trainings

Thank you for contributing! Please read this guide before opening a pull request.

For deeper context see:
- [`docs/SPECIFICATION.md`](docs/SPECIFICATION.md) — functional requirements and data model.
- [`docs/DEVELOPMENT_WORKFLOW.md`](docs/DEVELOPMENT_WORKFLOW.md) — branching strategy and deployment process.

---

## Language Policy

| Context | Language |
|---|---|
| Source code identifiers (variables, classes, methods) | **English (US)** |
| Source code comments | **English (US)** |
| Documentation and Markdown files | **English (US)** |
| Commit messages and PR descriptions | **English (US)** |
| User-facing UI strings | Project locale (currently English US) |

---

## Branching Strategy

- Branch from `main`.
- Name branches descriptively: `feature/session-export`, `fix/attendance-null-ref`, `chore/update-deps`.
- Keep branches short-lived; merge or rebase frequently.

---

## Pull Requests

1. Open a PR against `main`.
2. Fill in the PR description: **what** changed, **why**, and any **testing notes**.
3. Link the related issue (e.g., `Closes #42`).
4. All CI checks must pass before merge.
5. At least one review approval is required.

---

## Code Style and Formatting

- Follow the rules in [`.editorconfig`](.editorconfig).
- Run `dotnet format` before pushing to auto-fix formatting issues.
- Follow C# conventions documented in [`.github/copilot-instructions.md`](.github/copilot-instructions.md).

---

## Tests

- Add or update tests for every functional change.
- Tests live in `tests/Trainings.*.Tests/`.
- Run the full suite before pushing:
  ```bash
  dotnet test
  ```
- Coverage is tracked via **coverlet**; do not lower existing coverage without justification.

---

## Commits

- Write clear, imperative commit messages: `Add session export endpoint`, `Fix null reference in attendance service`.
- Keep commits focused — one logical change per commit.

---

## Local Setup

```bash
# Restore dependencies
dotnet restore

# Run the application
dotnet run --project src/Trainings.Web

# Run tests
dotnet test
```

See [`README.md`](README.md) for full environment setup (Docker, environment variables, etc.).
