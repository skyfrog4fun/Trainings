# AGENTS

This file is the canonical instruction source for AI-assisted work in this repository.

## Scope

- Applies to all AI agents and automation tools used in this repository.
- Use this file as the primary source of project behavior and coding constraints.

## Language Policy

- Use **English (US)** for all responses, generated code comments, and documentation updates.
- Use **English (US)** identifiers in source code (types, methods, variables, properties).
- Keep user-facing UI text in the project locale (currently English US).

## Implementation Workflow

1. Ask clarifying questions when requirements are ambiguous.
2. Present a concise implementation plan before making code changes.
3. Keep changes minimal, focused, and scoped to the request.
4. Do not refactor unrelated areas.

## C# Conventions

### Naming

- Use `PascalCase` for types, methods, properties, events, and public members.
- Use `camelCase` for local variables and parameters.
- Use `_camelCase` for private instance fields.
- Use `PascalCase` for constants; avoid `ALL_CAPS`.
- Prefix interfaces with `I`.
- Suffix async methods with `Async`.

### Nullability

- Nullable reference types are enabled project-wide.
- Do not suppress nullable warnings with `!` unless null-state is impossible and documented inline.
- Prefer `??` and null-conditional operators when readable.

### Async and Await

- Use async APIs for I/O-bound work and return `Task` or `Task<T>`.
- Pass `CancellationToken` through async call chains.
- Do not use `.Result` or `.Wait()`.

### General Style

- Prefer `var` when the assigned type is obvious.
- Keep methods focused and single-purpose.
- Use `ArgumentNullException.ThrowIfNull` for guard clauses.
- Always use braces for control blocks.

## Architecture Constraints

The solution follows Clean Architecture:

```text
Trainings.Domain          <- Entities, value objects, domain logic
Trainings.Application     <- Use cases, interfaces, DTOs
Trainings.Infrastructure  <- EF Core, persistence, external services
Trainings.Web             <- Blazor Server UI and DI composition root
```

- Dependencies must flow inward only.
- Do not reference `Infrastructure` or `Web` from `Domain` or `Application`.

## Testing and Formatting

- Use xUnit with FluentAssertions and Moq.
- Place unit tests in `tests/Trainings.*.Tests/`.
- Mock external dependencies in unit tests.
- Run `dotnet test` for validation.
- Follow `.editorconfig`.
- Run `dotnet format` before finalizing changes.

## UI and Dependency Rules

- Reuse shared collapsible card styles from `src/Trainings.Web/wwwroot/app.css`.
- Prefer built-in .NET and Blazor functionality before introducing new packages.
- Do not add new NuGet dependencies without explicit trade-off discussion.
- Use Font Awesome Free icons only.

## Security and Safety Rules

- Do not commit secrets, credentials, or connection strings.
- Do not suppress build warnings globally.

## Authoritative References

- `README.md` for setup and repository navigation.
- `docs/architecture/SPECIFICATION.md` for domain behavior, business rules, and architecture authority.
- `docs/process/DEVELOPMENT_WORKFLOW.md` for issue-to-production delivery flow.
- `docs/developer/cheat-sheet.md` for operational command shortcuts.

Consult only the references relevant to the task to keep context focused.