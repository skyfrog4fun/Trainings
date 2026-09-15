---
name: pr-readiness
description: "Use when the user wants to verify a feature branch is ready for a pull request back to GitHub (e.g. 'make this branch ready for a PR', 'check guardrails before PR', 'run pr-readiness') and, if all checks pass, open the PR. Combines sync check against origin/main, a version-bump check, the dotnet-verify quality gate, an automated code-review pass, and gh pr create — with an explicit approval gate before every Git-mutating action."
argument-hint: "[target-branch] (defaults to main)"
user-invocable: true
---

# PR Readiness

Use this skill to check whether the current branch fulfills all guardrails required for a
GitHub pull request (PR Guardian CI parity + review), and to open the PR once everything is
green.

## Hard rule: approval before every Git-mutating action

Before running any of the following, print a one-line explanation of exactly what will happen
and STOP for explicit user approval. Never run these silently, even in autopilot/background
mode:

- `git merge`, `git rebase`
- `git push`
- `gh pr create`

Never run `git add`, `git stage`, or `git commit` yourself, even with approval — staging and
committing is always a manual step performed by the user after reviewing the diff (see
`AGENTS.md` § Git Workflow Rules). Propose a commit message and describe what changed, then
stop and wait for the user to commit before continuing.

Read-only commands (`git status`, `git fetch`, `git log`, `git diff`, `gh auth status`) do not
require approval.

## Procedure

### 1. Status check

```powershell
git status
git branch -vv
```

- Confirm the working tree is clean before proceeding. If it is dirty, stop and ask the user
  how to handle uncommitted changes (do not assume stash/commit/discard).
- Identify the current branch and its upstream.

### 2. Sync check against `origin/<target-branch>`

Target branch defaults to `main` unless the user specifies otherwise.

```powershell
git fetch origin
git rev-list --left-right --count origin/<target-branch>...HEAD
```

- If `origin/<target-branch>` has commits the current branch doesn't (first number > 0), the
  branch has diverged.
- **Always ask the user** whether to `merge origin/<target-branch>` into the current branch or
  `rebase` the current branch onto it. Do not default to either — briefly explain the
  trade-off first:
  - Merge: safe, preserves history, simple conflict resolution, adds a merge commit.
  - Rebase: linear history, rewrites local commits, requires a force-push afterwards.
- Only run the chosen command after explicit approval. If conflicts occur, stop and hand
  control back to the user — do not attempt automatic conflict resolution.
- If not diverged, report "up to date with origin/<target-branch>" and continue.

### 3. Version check

Compare the `<Version>` in `Directory.Build.props` on the current branch against the version on
`origin/<target-branch>`:

```powershell
git show origin/<target-branch>:Directory.Build.props
```

Read `<Version>` from both (local working copy and the `git show` output above) and compare
them as dotted numeric groups (e.g. `1.4.2` vs `1.4.3`), not as strings.

- **Local version > target branch version:** PASS. Continue.
- **Local version <= target branch version** (lower, or equal despite this branch containing
  changes): report this clearly as a blocking issue — merging as-is would ship without a
  version bump.
  - Suggest the next version by auto-incrementing the last numeric group of the target
    branch's version (same rule as the `version-update` skill: e.g. target has `1.4.3` →
    suggest `1.4.4`).
  - **Ask the user** how to proceed: accept the suggested version, supply a different target
    version, or skip the bump (e.g. because a version bump is intentionally handled in a
    separate PR). Do not decide silently.
  - If the user accepts a version, invoke the `version-update` skill
    (`.github/skills/version-update/scripts/bump-version.ps1 -ToVersion "vX.Y.Z"`) to apply it
    consistently across all tracked references, then show the diff and stop for the user to
    review, stage, and commit it themselves (per the Fix loop / commit rule below).

### 4. Quality gate (CI parity)

Invoke the `dotnet-verify` skill against the repository solution (`Trainings.slnx`). This
covers, in order: restore, build with `/warnaserror`, test, `dotnet format
--verify-no-changes`, vulnerable/deprecated package audit — matching the **PR Guardian**
workflow (see `docs/process/DEVELOPMENT_WORKFLOW.md`).

Report each step PASS/FAIL. Stop on first hard failure and surface the failing output.

### 5. Automated code review

Launch a `code-review` sub-agent scoped to the diff between the current branch and
`origin/<target-branch>`:

```powershell
git diff origin/<target-branch>...HEAD
```

Ask it to report only high-confidence bugs, security issues, and logic errors (ignore style).
Present findings using the standard severity table format (🔴 CRITICAL / 🟠 HIGH / 🟡 MEDIUM /
⚪ LOW).

### 6. Fix loop

If step 4 or step 5 surfaced fixable issues:

- Propose the fix and show the diff.
- **Ask approval** before applying.
- After applying, re-run the affected quality-gate step(s) to confirm the fix.
- Do not stage or commit the fix yourself. Propose a commit message, then stop and ask the
  user to review, stage, and commit it (see `AGENTS.md` § Git Workflow Rules). Wait for
  confirmation that the commit exists before continuing to step 7.

### 7. Push

If there are local commits not on the remote (new commits, merge/rebase, or fix commits the
user has committed):

- Explain what will be pushed (branch name, commit list, whether `--force-with-lease` is
  needed because of a rebase).
- **Ask approval**, then push.

### 8. gh authentication check

```powershell
gh auth status
```

- If not authenticated, stop here and tell the user to run `gh auth login`. Do not attempt to
  authenticate on their behalf — this is out of scope for the skill.

### 9. PR creation

- Ask the user (each time): draft PR or ready-for-review PR.
- Ask the user (each time): whether to link an issue with `Closes #N`. You may suggest a
  candidate issue number parsed from the branch name (e.g. branch `63-groups` → issue `#63`)
  as a hint only — never assume it's correct.
- Build a PR title and summary from the commit log (`git log origin/<target-branch>..HEAD
  --oneline`), show it to the user, **ask approval**, then run:

```powershell
gh pr create --base <target-branch> --title "<title>" --body "<body>" [--draft]
```

- Report the resulting PR URL.

## Output contract

- Summarize each step (status/sync, version check, quality gate, review, push, PR) as PASS /
  FAIL / SKIPPED.
- Never mark the branch "PR-ready" if any quality-gate step failed, the version check is
  unresolved, or a required step was skipped.
- Always end with either the PR URL, or a clear statement of what's blocking PR creation.

## References

- `docs/process/DEVELOPMENT_WORKFLOW.md` — PR Guardian CI checks this skill must match.
- `.github/skills/dotnet-verify/SKILL.md` — reused quality gate.
- `.github/skills/version-update/SKILL.md` — reused for applying version bumps.
- `docs/developer/cheat-sheet.md` — Git command conventions for this repo.
