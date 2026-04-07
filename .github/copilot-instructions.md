# Copilot / AI Assistant Instructions

These instructions apply to all AI-assisted contributions (GitHub Copilot, Copilot Chat, and similar tools) in this repository.

---

## Language Policy

| Context | Language |
|---|---|
| Conversation / chat replies | **Same language the user wrote in** |
| Source code identifiers (variables, classes, methods, properties) | **English (US)** |
| Source code comments | **English (US)** |
| Documentation files (Markdown, XML docs) | **English (US)** |
| User-facing UI strings (labels, messages) | Project locale (currently **English (US)**; localization may be added later) |

> **Example:** If a user asks a question in German, reply in German — but any generated code must still use English identifiers and English comments.

---

## Clarification Before Implementation

1. **Ask clarifying questions first** when the requirement is ambiguous or incomplete.  
2. **Present a concise plan** (file list + key decisions) and wait for approval before writing code.  
3. Keep changes minimal and surgical; do not refactor unrelated code.

---

## C# Conventions

### Naming
- **PascalCase** for types, methods, properties, events, and public members.
- **camelCase** for local variables and parameters.
- **_camelCase** (underscore prefix) for private instance fields.
- **PascalCase** for constants; avoid `ALL_CAPS`.
- Interface names start with `I` (e.g., `ITrainingRepository`).
- Async methods end with `Async` (e.g., `GetSessionsAsync`).

### Nullability
- Nullable reference types are **enabled** project-wide (`<Nullable>enable</Nullable>`).
- Never suppress nullable warnings with `!` unless the null-state is genuinely impossible and a brief comment explains why.
- Prefer `??` and null-conditional operators over explicit null checks where readable.

### Async / Await
- All I/O-bound operations must be `async` and return `Task` / `Task<T>`.
- Pass `CancellationToken` through all async call chains.
- Do not use `Task.Result` or `.Wait()` — this can deadlock in Blazor Server.

### General Style
- Prefer `var` when the type is obvious from the right-hand side.
- Use expression-bodied members for single-line implementations.
- Keep methods short and focused (single responsibility).
- Use `ArgumentNullException.ThrowIfNull` for parameter guards.
- Always open and close curly braces `{ }` even for single statements after `if`, `switch`, etc.

---

## Project Architecture (Clean Architecture)

```
Trainings.Domain          ← Entities, value objects, domain logic (no external deps)
Trainings.Application     ← Use cases, interfaces, DTOs
Trainings.Infrastructure  ← EF Core, persistence, external services
Trainings.Web             ← Blazor Server UI, DI composition root
```

- Dependencies flow **inward only** (Domain ← Application ← Infrastructure/Web).
- Do not reference `Infrastructure` or `Web` projects from `Domain` or `Application`.

---

## Testing

- Use **xUnit** with **FluentAssertions** and **Moq** (already in use).
- Name tests: `MethodName_StateUnderTest_ExpectedBehavior`.
- Unit tests go in `tests/Trainings.*.Tests/`.
- Mock external dependencies; do not use real databases in unit tests.

---

## Formatting

- Follow `.editorconfig` for indentation, line endings, and encoding.
- Run `dotnet format` before committing to ensure consistent style.

---

## Third-Party Libraries

- Prefer using pure .NET / Blazor / built-in functionality whenever possible instead of adding third-party libraries or NuGet packages.
- If a third-party library is suggested, explicitly name it and explain the advantages over a built-in or custom solution.

## What to Avoid

- Do not add new NuGet packages without discussing trade-offs first.
- Do not commit secrets, connection strings, or credentials.
- Do not suppress build warnings globally; fix the root cause instead.
