---
name: ui-ux-testing
description: Use when asked to test, review, or check the UI/UX of the Trainings Blazor app (any page, feature, or role), including "test the UI", "check UX of X", "walk through Y as a Trainer", or exploratory browser-based QA. Handles starting the app and logging in as a specific role via playwright-cli.
allowed-tools: Bash(playwright-cli:*) Bash(dotnet:*) Bash(curl:*)
---

# UI/UX Testing for Trainings (Blazor Server)

Exploratory, browser-based review of the app using `playwright-cli` (see the
`playwright-cli` skill for the full command reference). This skill covers the
parts specific to this repo: starting the app, logging in as the right role,
and what to check.

## 1. Ensure the app is running

Dev URL: **http://localhost:5040** (default `http` launch profile; HTTPS is
not set up locally, don't use `https://localhost:7240`).

```powershell
# Check if it's already up
curl.exe -s -o NUL -w "%{http_code}" http://localhost:5040
```

- If it responds (2xx/3xx/401/etc., i.e. not a connection error): app is
  already running, reuse it. Do not start a second instance.
- If it's not reachable: start it detached so it survives this session, then
  poll until it responds (retry every 2-3s, up to ~60s):

```powershell
dotnet run --project src/Trainings.Web --launch-profile http
```

Run this as a **detached background process** (it must keep running after the
task ends). Do not stop it afterwards unless the user asks — leave it running
for follow-up testing.

## 2. Roles and credentials

The app has 4 practical roles for testing (system role is only `SuperAdmin`/
`User`; Admin/Trainer/Participant are per-group roles, see
`docs/architecture/SPECIFICATION.md`):

| Role | Abilities (short) |
|------|--------------------|
| **SuperAdmin** | Full access: all groups, users, system mail config, running modes, notification dashboard. |
| **GroupAdmin** | Manages users/approvals/settings for their own group(s); sees pending join requests. |
| **Trainer** | Creates/edits trainings and blocks, records attendance, views attendance reports — only for groups where they hold Trainer/Admin. |
| **Participant** | Registers for trainings in approved groups, views their own registrations/attendance. |

Credentials live in the repo-root **`.env`** file (gitignored), next to the
existing SuperAdmin seed values, so test data/users can grow over time:

```
SEED_ADMIN_EMAIL=...          # SuperAdmin (already exists / auto-seeded)
SEED_ADMIN_PASSWORD=...

TEST_GROUPADMIN_EMAIL=...
TEST_GROUPADMIN_PASSWORD=...

TEST_TRAINER_EMAIL=...
TEST_TRAINER_PASSWORD=...

TEST_PARTICIPANT_EMAIL=...
TEST_PARTICIPANT_PASSWORD=...
```

- SuperAdmin always exists (`DbSeeder`). GroupAdmin/Trainer/Participant
  accounts do **not** auto-seed — if their `.env` values don't correspond to
  a real user yet, log in as SuperAdmin once and create them via
  **Users → Create user**, assigning the matching group role, so future runs
  can reuse them.
- Never print `.env` contents to the terminal/chat; read values only as
  needed to fill the login form.
- If `.env` is missing a role you need, ask the user for it rather than
  inventing credentials.

## 3. Logging in (session reuse to avoid the login wall)

Store authenticated sessions per role under `.playwright-cli/` (already
gitignored) so you don't have to log in through the UI every time:

```
.playwright-cli/auth-superadmin.json
.playwright-cli/auth-groupadmin.json
.playwright-cli/auth-trainer.json
.playwright-cli/auth-participant.json
```

Flow:

1. If `auth-<role>.json` exists, `playwright-cli state-load .playwright-cli/auth-<role>.json`,
   then `goto http://localhost:5040` and check the snapshot for a logged-in
   page (e.g. dashboard, language dropdown showing) vs. redirect to `/login`.
2. If it doesn't exist or the session expired (redirected to `/login`):
   - `playwright-cli goto http://localhost:5040/login`
   - `playwright-cli snapshot` to get refs
   - `playwright-cli fill <emailRef> "<email from .env>"`
   - `playwright-cli fill <passwordRef> "<password from .env>" --submit`
   - `playwright-cli snapshot` to confirm login succeeded (redirected off `/login`)
   - `playwright-cli state-save .playwright-cli/auth-<role>.json`
3. Reuse the saved state for the rest of the session; only re-login if a
   check unexpectedly lands back on `/login`.

Switch roles by loading a different `auth-<role>.json` (open a new tab or
close/reopen the session with `-s=<role>` if testing multiple roles side by
side).

## 4. Browser mode

Default to **headless** (`playwright-cli open` without a visible-browser
flag). Switch to a visible/headed browser only when the user explicitly asks
to watch, or when you ask the user to follow along interactively (e.g.
`playwright-cli show --annotate`).

## 5. What to check

For the requested area/page, walk the relevant flow and check:

1. **Functional flow** — the primary user action(s) work end-to-end (submit
   forms, navigate, expected data appears) with the given role's permissions.
2. **Console & network errors** — `playwright-cli console` and
   `playwright-cli requests` after interacting; flag JS errors, failed
   requests (4xx/5xx), unhandled exceptions.
3. **Responsive layout** — check both a desktop size (default) and mobile
   (`playwright-cli resize 390 844` or `--mobile`/`--device` on `open`);
   look for overflow, unreadable text, broken/overlapping elements.
4. **Localization (EN/DE)** — switch language via the language dropdown
   (`#language-menu-button`) or directly navigate to
   `/culture/set?culture=de&returnUrl=<currentPath>` (and `culture=en` to
   revert); check for missing translations (raw resource keys), untranslated
   strings, or layout breakage from longer German text.
5. **Accessibility** — from the snapshot, check for missing/incorrect ARIA
   labels, heading structure, keyboard focus order, and sufficient
   button/link labeling (the snapshot's accessibility tree surfaces most of
   this directly).

## 6. Reporting

Summarize findings as a short list grouped by severity (Blocker / Issue /
Nitpick), each with: page/area, role used, what was expected vs. observed,
and a screenshot filename if one was taken (`playwright-cli screenshot
--filename=...`, saved under `.playwright-cli/`). Don't fix code unless asked
— this skill is for review/testing, not implementation.
