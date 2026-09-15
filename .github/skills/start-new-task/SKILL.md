---
name: start-new-task
description: "Use when the user wants to start a new piece of work (e.g. 'let's start a new task', 'create a new issue', 'I have a new feature/bug to work on'). Guides the user through drafting and creating a GitHub issue, then optionally creating and checking out a matching feature branch from main, ending with a ready-to-work status report."
argument-hint: "[topic] (optional short description of what the task is about)"
user-invocable: true
---

# Start New Task

Use this skill to go from an idea to a ready-to-work feature branch: create a GitHub issue,
then (optionally) branch from `main` for it.

## Hard rule: approval before every mutating action

Before running any of the following, print a one-line explanation of exactly what will happen
and STOP for explicit user approval. Never run these silently:

- `gh issue create`
- `git checkout -b`

Read-only commands (`git status`, `git branch -vv`, `git fetch`, `gh issue view`) do not
require approval.

## Procedure

### 1. Gather the topic

If the user didn't already describe what the task is about (or only gave a vague hint), ask
what the new issue should be about. Get enough detail to write a meaningful title and body
(what needs to change and why).

### 2. Draft the issue

- Draft a concise `title` and a `body` (short description, any relevant context/acceptance
  criteria) based on the user's explanation.
- Show the drafted title and body to the user and **ask for confirmation** before creating
  anything. Let them request edits and re-show the draft until approved.

### 3. Create the issue

Only after explicit approval:

```powershell
gh issue create --title "<title>" --body "<body>"
```

- Parse the resulting issue number and URL from the command output.
- Report back clearly: `Issue #NN created: <url>` (title included).

### 4. Ask: branch now, or stop here?

Ask the user whether they want to create a branch and start working now, or stop here (issue
created only, so the idea isn't lost).

- **Stop**: confirm that the issue is filed and end the skill here. No git operations.
- **Branch now**: continue to step 5.

### 5. Prepare the branch

Explain what will happen (branch name `NN-topic-slug`, created from up-to-date `main`), then
**ask for approval**. On approval:

```powershell
git checkout main
git pull origin main
git checkout -b NN-topic-slug
```

- Derive `topic-slug` from the issue title (short, kebab-case, a few words).
- `NN` is the issue number from step 3.

### 6. Verify

```powershell
git status
git branch -vv
```

- Confirm the new branch is checked out, clean, and based on the latest `main`.
- Present the branch list output to the user.

### 7. Final report

End with a message in this style:

> Everything is prepared to start work on Issue #NN — "<issue title>". Branch `NN-topic-slug`
> is ready. What should we do?

## Output contract

- Always report the issue number and URL once created.
- Never create a branch without explicit approval, even if the user said "yes" to branching
  earlier in a different context — confirm at step 5 specifically.
- If the user stops after issue creation, do not perform any git operations.

## References

- `docs/process/DEVELOPMENT_WORKFLOW.md` — overall workflow this skill starts.
- `docs/developer/cheat-sheet.md` — Git command conventions for this repo.
- `.github/skills/pr-readiness/SKILL.md` — the counterpart skill that finishes the cycle
  (verify + open PR) once work on the branch is done.
