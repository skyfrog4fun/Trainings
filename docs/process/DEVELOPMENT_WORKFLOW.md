# Development Workflow

This document describes how a code change travels from idea to production.

Two AI skills automate large parts of this cycle while keeping an explicit approval gate
before every Git-mutating action:

| Skill | Covers |
|---|---|
| `start-new-task` | Creating the issue (stage 1) and the branch setup in stage 2 |
| `pr-readiness` | Local testing, quality gate, automated review, and PR creation (stages 3–4) |

Review, approval, and the merge itself (stages 6–7) are always done by a human on GitHub.

---

## Stages

### 1. Create an Issue

- Open a GitHub issue describing the change (bug, feature, improvement).
- Assign yourself and add relevant labels (e.g. `bug`, `enhancement`, `documentation`).
- The issue serves as the single source of truth for *why* the change is needed.
- The `start-new-task` AI skill can drive this step end-to-end: it drafts the issue title/body,
  creates the issue on confirmation, suggests fitting labels from the repo's full label list
  (you approve or pick differently), assigns you, and can immediately prepare the matching
  branch (see below).

### 2. Implementation

- Create a feature branch from `main`, named after the issue number: `NN-short-desc`
  (e.g. `65-update-workflow-docs` for issue #65).
- Implement the change following the [Clean Architecture](../architecture/SPECIFICATION.md) guidelines.
- Keep changes focused and limited to what is described in the issue.

```bash
git checkout main
git pull origin main
git checkout -b NN-short-desc
```

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
- The `pr-readiness` AI skill automates stages 3–4: it checks the branch is in sync with `main`,
  runs the quality gate locally (CI parity with stage 5), runs an automated code review pass,
  and opens the PR — with an approval gate before every Git-mutating action.

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

- Once all checks pass and the PR is approved, it is merged into `main` **on GitHub**
  (review/approve/merge is a deliberate human action, not automated).
- The feature branch is deleted automatically after merging (`delete_branch_on_merge` repo
  setting is enabled; see "Branch Protection" below).
- The linked issue is closed automatically when the PR is merged.
- Locally, clean up the now-merged branch:

```bash
git checkout main
git pull origin main
git branch -d NN-short-desc
```

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

## Branch Protection

`main` is protected by a GitHub **ruleset** ("Main Branch Protection", repo Settings →
Rules → Rulesets). This is GitHub-side configuration, not part of the repository's tracked
files — there is no diff/PR history for it, changes are only visible in GitHub's UI/API
(`gh api repos/skyfrog4fun/Trainings/rulesets`).

Current rules for `main`:

| Rule | Effect |
|---|---|
| Require a pull request before merging | No direct pushes to `main`; all changes go through a PR |
| Require status checks (`Build/Test/Quality Gates`, i.e. PR Guardian) | PR cannot be merged unless CI passes |
| Block force-pushes (non fast-forward) | History on `main` cannot be rewritten |
| Block branch deletion | `main` cannot be deleted |

Required approving reviews are set to `0` (solo-maintainer repo) — review still happens, but
GitHub doesn't block the merge button on it. Revisit if collaborators join.

Repo setting `delete_branch_on_merge` is also enabled, so merging a PR auto-deletes the
remote feature branch (you still need `git branch -d NN-short-desc` locally, per stage 7).

## References

- `.github/skills/start-new-task/SKILL.md` — creates the issue and prepares the branch.
- `.github/skills/pr-readiness/SKILL.md` — verifies the branch and opens the PR.
- `docs/developer/cheat-sheet.md` — concrete Git command sequence for this cycle.
