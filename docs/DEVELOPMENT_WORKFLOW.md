# Development Workflow

This document describes how a code change travels from idea to production.

---

## Stages

### 1. Create an Issue

- Open a GitHub issue describing the change (bug, feature, improvement).
- Add relevant labels (e.g. `bug`, `enhancement`, `documentation`).
- The issue serves as the single source of truth for *why* the change is needed.

**What a good issue contains:**

| Field | Guidance |
|---|---|
| **Title** | Short, imperative sentence (e.g. "Add capacity validation to TrainingService"). |
| **Description** | One paragraph explaining the *problem* or *desired behaviour*. Include screenshots or error messages when relevant. |
| **Steps to reproduce** | (Bug reports only) Numbered list of exact steps that trigger the problem. |
| **Expected vs actual behaviour** | (Bug reports only) What should happen and what currently happens. |
| **Acceptance criteria** | Bullet list of conditions that must be true for the issue to be considered done (e.g. "Unit test covers the new validation rule"). |
| **Related resources** | Links to the relevant section in [SPECIFICATION.md](SPECIFICATION.md), related issues, or external references. |

**Practical checklist before submitting:**

- [ ] The title clearly identifies *what* is being changed.
- [ ] The description explains *why* the change is needed.
- [ ] Acceptance criteria are written so a reviewer can objectively verify them.
- [ ] At least one label has been applied.
- [ ] No implementation details are prescribed — the issue describes the *what*, not the *how*.

> **Note:** Keep issues scoped to a single change. If you spot multiple problems, open separate issues so each can be tracked, reviewed, and closed independently.

### 2. Implementation

- Create a feature branch from `main` (e.g. `feature/my-change` or `fix/my-bug`).
- Implement the change following the [Clean Architecture](SPECIFICATION.md) guidelines.
- Keep changes focused and limited to what is described in the issue.

### 3. Local Testing

- Run the unit tests:
  ```bash
  dotnet test
  ```
- Run the application and verify the change manually:
  ```bash
  dotnet run --project src/Trainings.Web
  ```
- Ensure code formatting is correct:
  ```bash
  dotnet format --verify-no-changes
  ```

### 4. Create a Pull Request

- Push the feature branch and open a PR targeting `main`.
- Reference the issue in the PR description (e.g. `Closes #<issue-number>`).
- Provide a short summary of what was changed and why.

### 5. Automated CI Checks (PR Guardian)

The **PR Guardian** workflow runs automatically on every PR and verifies:

| Check | Command |
|---|---|
| Restore | `dotnet restore` |
| Build (warnings as errors) | `dotnet build -c Release /warnaserror` |
| Unit tests | `dotnet test -c Release` |
| Code format | `dotnet format --verify-no-changes` |
| Package audit (warn-only) | `dotnet list package --vulnerable` |

All checks must pass before the PR can be merged.

### 6. Code Review

- At least one team member reviews the PR.
- Review covers correctness, adherence to architecture, and test coverage.
- Requested changes are addressed with additional commits on the same branch.

### 7. Merge to Main

- Once all checks pass and the PR is approved, it is merged into `main`.
- The feature branch is deleted after merging.
- The linked issue is closed automatically when the PR is merged.

### 8. Deployment to Production

- Merging to `main` triggers the **Build and Publish Docker Image** workflow.
- A new Docker image is built and pushed to the GitHub Container Registry (GHCR).
- The production environment pulls the new image and restarts the application.

---

## Summary

```
Issue created
    │
    ▼
Feature branch + implementation
    │
    ▼
Local tests pass (dotnet test + manual verification)
    │
    ▼
Pull Request opened (references issue)
    │
    ▼
PR Guardian CI checks pass
    │
    ▼
Code review approved
    │
    ▼
Merged to main → issue closed
    │
    ▼
Docker image published → deployed to production
```
