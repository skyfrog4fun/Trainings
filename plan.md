# Trainings Workflow Rework — Plan

## Goal

Rework the Trainings feature (create, plan, run, attendance, registration) to match the
following lifecycle, and split page/route responsibilities strictly by role.

## State machine

```
New (created, unassigned)
   │  Trainer "Take" (or Admin pre-assigns at creation)
   ▼
InPlanning (has trainer, being edited)
   │  Trainer/Admin "Confirm Planned"
   ▼
Planned (confirmed; still editable, stays Planned on edit)
   │  Assigned Trainer only — "Start"  (closes registration)
   ▼
InProgress (day-of; trainer manages participants)
   │  Assigned Trainer/Admin — "Finalize attendance"
   ▼
Done (attendance locked; everything else stays editable)
```

- `TrainerId` is nullable. A training can exist unassigned (`New`).
- GroupAdmin can optionally pick a Trainer already at creation (skips "Take").
- GroupAdmin can reassign/clear the trainer at any time.
- Trainer can release (un-assign) themselves, moving the training back to `New`.
- Registration: allowed in `New` / `InPlanning` / `Planned`; blocked once `InProgress` or `Done`.
- After `Done`: only `Attendance` records + the lock are immutable; all other fields
  (Title, Description, Location, Date/Time, Capacity, Trainer) remain editable.
- Auto title when left blank: `"Training of {localized date}"` (culture from group's country).
- Mandatory at creation: Group, Date, Location, Capacity. Start/Duration default from the
  Group's settings (editable). Trainer, Title, Description are optional.

## Authorization

- Create (`/trainings/{slug}/create`): `GroupAdmin` policy only (SuperAdmin included). Trainers
  cannot reach this page.
- Edit/plan actions: `GroupTrainer` policy at the route level, plus an in-page check that the
  acting user is the **assigned trainer** or a **GroupAdmin** of that specific group.
- "Start" action (`Planned` → `InProgress`): assigned Trainer only (not Admin).
- "Finalize attendance" (`InProgress`/last step → `Done`): assigned Trainer or GroupAdmin.
- Everything is scoped per group: a user only sees/acts on a training if they hold an
  Admin/Trainer/Participant claim for that specific `GroupId` (SuperAdmin bypasses all scoping).

## Routing

All reworked pages live under `/trainings/{group-slug}/...`:

| Route | Page | Access |
|---|---|---|
| `/trainings` | List (cross-group) | GroupMember — card: Title, Location, Start–End, Trainer name/"Unassigned", Date, Status, "x / y participants", Register/Unregister button |
| `/trainings/{slug}/create` | Create | GroupAdmin/SuperAdmin only |
| `/trainings/{slug}/{id}` | Detail | GroupMember of that group; limited preview while status < `Planned` (no description/blocks/participant list); full detail once `Planned`+; Trainer/Admin always see full |
| `/trainings/{slug}/{id}/edit` | Plan/Edit | Assigned Trainer or GroupAdmin — Take/Release/Reassign/Confirm-Planned actions |
| `/trainings/{slug}/{id}/run` | Day-of run page | Assigned Trainer only — "Start" button |
| `/trainings/{slug}/{id}/attendance` | Attendance | Assigned Trainer/Admin — "Finalize" button → `Done` |

`/plan-training`, `/trainer-trainings` (trainer's cross-group dashboards) stay, linking into the
new per-group routes.

## Phases

### Phase 1 — Domain
- `TrainingStatus`: rename `Planning` → `InPlanning` (same ordinal, no data break), add
  `InProgress = 3`, `Done = 4`.
- `Training.TrainerId` / `Trainer` become nullable (`int?` / `User?`).
- EF Core migration: nullable FK + new enum values.
- Data backfill for existing rows (legacy trainings always had a mandatory trainer):
  existing `New` → `InPlanning`, existing `Planning` → `InPlanning`, existing `Planned` stays
  `Planned`. No rows deleted, purely a status remap.

### Phase 2 — Application
- `CreateTrainingDto` updates (optional Trainer/Title/Description; auto-title fallback).
- New use-cases: `TakeAsync`, `ReleaseAsync`, `ReassignAsync`, `ConfirmPlannedAsync`,
  `StartAsync`, `FinalizeAttendanceAsync`.
- `RegistrationService`: drop the 4-week/4-day window; gate purely on status
  (`New`/`InPlanning`/`Planned` = open, `InProgress`/`Done` = closed).
- Editing stays allowed in every status, including `Done` (attendance excluded).

### Phase 3 — Authorization
- Create page restricted to `GroupAdmin` only.
- In-page "assigned trainer or group admin" guard for edit/plan/run/attendance actions.

### Phase 4 — Web / Routing
- Route restructure to `/trainings/{group-slug}/...`.
- Split `CreateEditTrainingPage` into a GroupAdmin-only Create page and a Trainer/Admin
  Edit/Plan page (Take/Release/Reassign/Confirm-Planned actions).
- Revamp `TrainingsPage` list cards (per spec above) and `TrainingDetailPage`
  (limited preview vs full detail).
- `TrainerRunPage`: add "Start" action.
- `AttendancePage`: add "Finalize" action → `Done`.

### Phase 5 — Tests & Docs
- Domain tests for status transitions/guards.
- Application tests for Take/Release/Reassign/ConfirmPlanned/Start/Finalize and updated
  registration rules.
- Update `docs/architecture/SPECIFICATION.md` (status table, use cases UC-04/06/08).
- Run `dotnet-verify` skill before considering the rework done.

## Status

- [x] Phase 1 — Domain (`TrainingStatus` reworked, `TrainerId` nullable, migration
      `20260910205148_ReworkTrainingStatusAndNullableTrainer` incl. legacy-data backfill,
      cascading DTO/UI nullability fixes, domain tests updated)
- [x] Phase 2 — Application (`TakeAsync`, `ReleaseTrainerAsync`, `ReassignTrainerAsync`,
      `ConfirmPlannedAsync`, `StartAsync` added to `ITrainingService`; `LockAttendanceAsync`
      now also transitions status to `Done`; `CreateAsync` auto-generates the fallback title
      and sets initial status based on whether a Trainer was pre-assigned; `RegistrationService`
      registration/cancel windows are now purely status-based (`New`/`InPlanning`/`Planned` open,
      `InProgress`/`Done` closed) — the old 4-week/4-day date window was removed; tests added/updated)
- [x] Phase 3 — Authorization (`CreateEditTrainingPage`: create mode now denies access
      unless the user is SuperAdmin or GroupAdmin of at least one group, restricting the
      Group dropdown to Admin-owned groups; edit mode denies access unless SuperAdmin,
      GroupAdmin of the training's group, or the assigned Trainer; `LoadTraining` refactored
      to accept the already-fetched `TrainingDto` instead of re-fetching by id.
      `TrainerRunPage` and `AttendancePage` now both deny access unless the caller is
      SuperAdmin/GroupAdmin of the training's group or the assigned Trainer — previously any
      Trainer/Admin of the group could open/run/mark-attendance for any training in that
      group. Added `_AccessDenied` resx keys (EN/DE) for all three pages. No Application/
      Domain changes — build, `dotnet test` (109 passed), and `dotnet format --verify-no-changes`
      all clean.)
- [x] Phase 4 — Web / Routing (Added slug-scoped alias routes alongside the existing
      numeric-id routes for Detail/Edit/Create/Run/Attendance — `/trainings/{Slug}/{Id}`,
      `/trainings/{Slug}/create`, etc. — so links are group-friendly while old links keep
      working; `TrainingDto`/mapping now expose `GroupSlug`.
      `TrainingDetailPage`: added a `_fullDetailAllowed` gate (Description/Blocks/Participants
      hidden — limited preview only — for non-trainer/admin viewers while status < `Planned`);
      added a "Take" button/action (unassigned `New` training, GroupTrainer only) using
      `TakeAsync`; Run/Attendance links now only show once the training has reached the
      relevant status (`Planned`+ for Run, `InProgress`+ for Attendance); trainer/admin now
      also gets an Edit link even when not the assigned trainer (GroupAdmin path).
      `CreateEditTrainingPage`: Trainer is no longer a mandatory field (GroupAdmin can leave
      it blank per spec); trainer reassignment is now routed through `ReassignTrainerAsync`
      (separate from the plain field Update) so the guarded status-transition rules apply;
      replaced the old ad-hoc "Mark as Planned"/"Revert to Planning" (`SetStatusAsync`) buttons
      with `ConfirmPlannedAsync` ("Confirm Planned", only when `InPlanning` + trainer assigned)
      and `ReleaseTrainerAsync` ("Release Trainer", only for the assigned trainer, only while
      `InPlanning`/`Planned`); create route now also accepts `/trainings/{Slug}/create` and
      pre-selects/locks in the matching group.
      `TrainerRunPage`: added a `Planned`-status "Start Training" gate (calls `StartAsync`)
      before the block-run flow is reachable; blocked entirely with a message while
      `New`/`InPlanning`.
      `AttendancePage`: "Finalize" is now gated on `Status == InProgress` (was a raw
      date/time comparison) to match the new lifecycle.
      `TrainingsPage`/`TrainingList`: cards show a status badge and "Unassigned" when no
      trainer is set; added a "Take" action for GroupTrainer/Admin/SuperAdmin users on
      unassigned (`New`) trainings; "Add" button now correctly gated on GroupAdmin/SuperAdmin
      only (was previously any GroupTrainer).
      Added all new EN/DE resx keys. Verified: build 0 errors, `dotnet test` 109 passed,
      `dotnet format --verify-no-changes` clean.)
- [x] Phase 5 — Tests & Docs (Domain/Application lifecycle tests were already added in Phases
      1–2 — 109 total tests covering status transitions, `TakeAsync`/`ReleaseTrainerAsync`/
      `ReassignTrainerAsync`/`ConfirmPlannedAsync`/`StartAsync`/`LockAttendanceAsync` guards, and
      the purely status-based registration/cancel rules. `docs/architecture/SPECIFICATION.md`
      updated: added the `TrainingStatus` enum row, a full lifecycle diagram/table under the
      `Training` entity section, rewrote UC-04 (create + lifecycle actions split into UC-04a),
      UC-06 (status-based registration window + limited-preview visibility rule), and UC-08
      (Finalize gated on `InProgress`, not a date check); extended `ITrainingService`'s
      documented surface and added BR-017…BR-023 to the machine-readable business-rules block
      for the new lifecycle/registration-window rules. Ran the `dotnet-verify` quality gate:
      restore ✅, `build -c Release /warnaserror` ✅ (0 warnings/errors), `dotnet test` ✅
      (109/109 passed), `dotnet format --verify-no-changes` ✅ (clean), vulnerable-package scan
      ✅ (none), deprecated-package scan ✅ (none). Trainings rework is complete end-to-end.)
