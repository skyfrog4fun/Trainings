# Training Lifecycle & Actions — Target Definition (Redesign Draft)

> Status: **Draft — Step 2 of redesign procedure complete (clarified).**
> This document captures the *desired/target* behavior as specified by the product owner,
> before comparing it against the current implementation. Do not use this as a source of
> truth for current behavior yet — see `docs/architecture/AUTHORIZATION.md` and
> `TrainingDetailPage.razor` for what exists today.

## Redesign procedure

1. **Document target situation** — actors, actions, states, transitions. ✅ done
2. **Clarify** — resolve consistency issues, technical problems, and unclear definitions in
   the target definition itself. ✅ done (see "Resolved decisions" below)
3. **Gap analysis** — compare target vs. current implementation, list every gap, decide
   per-gap what to do (adopt, reject, defer). ✅ done — see
   `docs/architecture/training-lifecycle-gap-analysis.md` for the full per-file comparison.
4. **Redesign** — implement the agreed-upon changes.

---

## Actors

| Code | Meaning |
|---|---|
| `U` | User / Participant of the group |
| `AT` | Assigned trainer of **this specific training** (`Training.TrainerId == currentUserId`) |
| `OT` | Any other trainer of the group (holds the Trainer role, but is not `AT` for this training) |
| `GA` | Group Admin |
| `SA` | Super Admin |

Note: roles are not mutually exclusive of `U`. A `GA`/`OT`/`AT` who also wants to register as a
participant must separately hold the `U` (Participant) role for that group — see `RT`/`UT`
below.

## Actions

| Code | Meaning | Changes state? |
|---|---|---|
| `RT` | Register to Training | No — see separate table below |
| `UT` | Unregister from Training | No — see separate table below |
| `TT` | Take Training (as a trainer) — `OT`-only | Yes |
| `C` | Create Training (on `/trainings`, not on the detail page) | Yes |
| `E` | Edit Training (plan it / change details) — `AT`-only | Yes (sometimes) |
| `D` | Hard-Delete Training (row physically removed from DB) | Yes (terminal, row gone) |
| `CX` | Cancel Training *(**NEW**)* | Yes (terminal, `9-Cancelled`) |
| `RUN` | Run the training (go to `/run`) — `AT`, `GA`, or `SA` | Yes |
| `ATT` | Record attendance (go to `/attendance`) — `AT`, `GA`, or `SA` | No (self-loop in `3-InProgress`) |
| `DONE`| Explicit "Mark as Done / Finish" action, distinct from `ATT` — `AT`, `GA`, or `SA` | Yes |

## States

| Code | Meaning |
|---|---|
| `0` | New |
| `1` | InPlanning |
| `2` | Planned |
| `3` | InProgress |
| `4` | Done |
| `9` | Cancelled *(**NEW** — terminal, not reversible; replaces the originally-proposed `9-Deleted`)* |
| *(none)* | Hard-deleted trainings have no state — the row is removed entirely, not represented as a state. |

## State diagram

Format: `FromState -> Actor: Action -> ToState`

```
[]           -> SA|GA: C                    -> 0-New
0-New        -> OT: TT                      -> 1-InPlanning
0-New        -> SA|GA: E (assign trainer)   -> 1-InPlanning
1-InPlanning -> AT: E (remove trainer)      -> 0-New
1-InPlanning -> SA|GA: E (remove trainer)   -> 0-New
1-InPlanning -> AT: E (anything)            -> 1-InPlanning
1-InPlanning -> AT: E (mark planned)        -> 2-Planned
2-Planned    -> AT: E (anything)            -> 1-InPlanning
2-Planned    -> AT|GA|SA: RUN               -> 3-InProgress
3-InProgress -> AT|GA|SA: ATT               -> 3-InProgress   (self-loop, no state change)
3-InProgress -> AT|GA|SA: DONE              -> 4-Done
0-New        -> GA|SA: D (hard delete)      -> [] (row removed)
1-InPlanning -> GA|SA: D (hard delete)      -> [] (row removed)
0-New        -> GA|SA: CX (cancel)          -> 9-Cancelled
1-InPlanning -> GA|SA: CX (cancel)          -> 9-Cancelled
2-Planned    -> GA|SA: CX (cancel)          -> 9-Cancelled
```

`9-Cancelled` is terminal — no transitions out of it are defined.

No edits (`E`), cancellation (`CX`), or deletion (`D`) are possible once `3-InProgress`; all
planning must happen while `1-InPlanning`/`2-Planned`. Attendance (`ATT`) is not editable once
`4-Done` (`att_editable_after_done = false`).

## Register / Unregister (does not affect training state)

| Action | Actors | States allowed | Notes |
|---|---|---|---|
| `RT` | Anyone holding the `U` (Participant) role for the group | `0-New`, `1-InPlanning`, `2-Planned` | A `GA`/`OT`/`AT`/`SA` who wants to register must separately hold the `U` role for that group. |
| `UT` | Same as `RT` | Same as `RT` | |

---

## Resolved decisions (from clarification step)

1. **`AT` scope** — `AT` = assigned trainer of *this training* (`TrainerId == currentUserId`),
   not "any trainer of the group". `OT` = a group trainer who is not `AT` for this training.
2. **`DONE` action** — is a distinct, explicit "Mark as Done / Finish" action/button, separate
   from `ATT`. Added to the Actions table.
3. **`GA`/`SA` involvement in states 1–3** — `E` (editing/planning) is reserved for `AT` only.
   `RUN` and `ATT` (and now `DONE`) are also available to `GA`/`SA` as a fallback override (in
   addition to `AT`), e.g. if the trainer is unavailable.
4. **`RT`/`UT`** — do not cause state transitions; captured in a separate table. Only actors
   holding the `U` role for the group can register/unregister themselves, and only while
   `0-New`/`1-InPlanning`/`2-Planned`.
5. **`TT` (Take Training)** — strictly `OT`-only. A `GA` can only "take" a training if they
   also separately hold the Trainer role (i.e. acting as `OT`); a pure `GA` action goes through
   `E` (assign trainer) instead.
6. **Deleted vs. Cancelled** — split into two distinct concepts:
   - **Hard delete (`D`)**: physically removes the row from the DB. Only allowed from
     `0-New`/`1-InPlanning` (before anything is planned/has dependent data). Actors: `GA`/`SA`.
   - **Cancel (`CX`, new `9-Cancelled` state)**: for trainings that have dependent data
     (registrations, blocks) and are planned but can no longer be held for any reason. Clearly
     marked as cancelled, terminal (not reversible), allowed from `0-New`/`1-InPlanning`/
     `2-Planned` (not from `3-InProgress`). Actors: `GA`/`SA` only.
7. **No additional self-loops** — the diagram is otherwise complete. Explicitly confirmed:
   - No changes to the training or its blocks may happen while `3-InProgress` — all such
     changes must happen earlier, during `1-InPlanning`.
   - `AT` (or fallback `GA`/`SA`) tracks attendance and marks the training `4-Done`; no further
     changes are expected afterward.
8. **Feedback** (trainer overall + participant) is fully defined as part of this redesign
   cycle — see the "Feedback" section below.

---

## Feedback (Trainer & Participant)

> Replaces the existing per-block "Actual Duration + Comment" feedback entirely (see
> "Current implementation being replaced" below).

### Types

| Type | Fields | Cardinality |
|---|---|---|
| **Trainer feedback** | General comment (optional text), rating (1–5 stars, optional), submitting trainer, timestamp | Exactly **one** record per training — resubmitting overwrites the existing record |
| **Participant feedback** | General comment (optional text), rating (1–5 stars, optional), submitting user, timestamp | Exactly **one** record per participant (`U`) per training — resubmitting overwrites that user's own record |

Both feedback types are per-*training* (not per-block). The old `TrainingBlock.EffectiveDurationMinutes`
and `TrainingBlock.TrainerComment` fields are dropped entirely — no per-block time correction or
comment remains.

### Who can submit / edit

| Type | Submitter | Eligibility | Editable? |
|---|---|---|---|
| Trainer feedback | `AT` only (no `GA`/`SA` fallback for this action) | Only once training is `4-Done` | Editable any time afterward (no lock) |
| Participant feedback | Any registered `U` for the training, regardless of attendance status (`Present`/`Absent`/`PartiallyPresent` all qualify) | Only once training is `4-Done` | Editable any time afterward by that same user (no lock) |

Both are **fully optional** — never mandatory, never gate the `DONE` action or any other
transition.

### Visibility

| Data | Visible to |
|---|---|
| Trainer feedback (comment + rating) | `AT`, `OT`, `GA`, `SA` — **not** visible to `U` |
| Individual participant feedback (comment + rating) | The submitting `U` sees their **own** entry (with their own identity); `AT`/`OT`/`GA`/`SA` see **all** participant feedback content but **without** the submitting user's name (anonymized to staff) |
| Aggregated participant rating (avg stars + number of feedbacks) | Everyone: `U`, `AT`, `OT`, `GA`, `SA` |

### UI location

A new dedicated page/route (e.g. `/trainings/{slug}/{id}/feedback`), separate from
`TrainingDetailPage`, `/run`, and `/attendance`. Only reachable once the training is `4-Done`.
Renders differently per actor per the eligibility/visibility rules above (submit form for `AT`
if they haven't submitted yet or want to edit theirs; submit form for any registered `U`;
anonymized list + aggregate for staff; own entry + aggregate for `U`).

### Current implementation being replaced

- `TrainingBlock.EffectiveDurationMinutes` (`int?`) and `TrainingBlock.TrainerComment`
  (`string?`) — entered today via `TrainerRunPage.razor`'s post-last-block "complete" phase
  (the form shown *after* stepping through all blocks and clicking "Finish Training"), saved
  via `TrainingBlockService.UpdateExecutionAsync` (no status guard, no visibility anywhere
  except `EffectiveDurationMinutes` shown as "Actual Minutes" on `TrainingDetailPage.razor`
  block cards). **Only this post-run feedback-capture form is dropped** — the block-by-block
  run flow itself (stepping through blocks one at a time, progress bar, "Next Block") is
  unaffected and stays exactly as-is; see TODO item 15 for the precise scope of that change.
- Today, "Finalize Attendance" on `/attendance` (`LockAttendanceAsync`) is what actually sets
  `Training.Status = Done` — confirming the gap already tracked in TODO item 4 (need an
  explicit, separate `DONE` action). Feedback entry (new dedicated page) only becomes reachable
  once that `DONE` transition has happened.

---

## Run Page & Training Completion Flow (`/run`, `/attendance`, Detail Page)

### `/run` is purely navigational — never changes training state

`/run` steps through blocks one at a time and must **never** call any state-changing service —
not even to start or finish the training. Its only job is letting `AT`/`GA`/`SA` walk through
the planned content, forward and backward.

- Navigation model: index `0..N` over the blocks, where `N` (one past the last real block) is
  a **virtual terminal step**, not a data-entry form.
- A **"Previous"** button is added (does not exist today) alongside "Next", so the trainer can
  move both directions through real blocks. "Next" disappears on the terminal step (nothing
  further to move to).
- Entry gating by training status:
  - `New`/`InPlanning` — unchanged: warning that the training isn't planned yet.
  - `Planned` (**dry run**) — the existing fixed "Ready to Start" intro screen stays as the
    only place to trigger `RUN`/`StartTrainingAsync` (**not** from an arbitrary block
    position). From that same intro screen, a new **"Preview blocks"** link lets the trainer
    browse all blocks (forward/back) *before* starting, purely for recap — no state change.
  - `InProgress` (**live**) — normal block browsing (existing behavior, plus "Previous").
  - `Done` — block browsing remains available as a **read-only recap** (per your decision), not
    inaccessible.
- **Terminal step content varies by status:**
  - Dry run (`Planned`): "Plan reviewed" message; no Attendance/Feedback links (irrelevant
    pre-start) — the trainer returns to the intro screen to actually start.
  - Live (`InProgress`): "Training session complete!" message with a link to `/attendance`
    (always enabled) and a link to `/feedback` (only enabled once `Status == Done`, since
    feedback requires that — see "Feedback" section above).
  - Read-only recap (`Done`): same terminal message as live, both links enabled (attendance
    view + feedback).

### Where `DONE` is triggered

Two trigger points, both restricted to `AT`/`GA`/`SA`:

1. **`/attendance`** — rename the existing "Finalize Attendance" button to **"Finalize
   Training"**. Behavior is unchanged from today's `LockAttendanceAsync`: it locks attendance
   (`AttendanceLocked = true`) **and** sets `Status = Done` in the same call. This remains the
   primary, expected path (trainer records attendance, then finalizes).
2. **`TrainingDetailPage`** footer — a new **"Mark as Done"** shortcut, visible only when
   `Status == InProgress`, for the case where attendance is being skipped or handled
   separately. If no attendance has been recorded yet, show a **confirmation dialog** ("You
   haven't recorded attendance — are you sure you want to mark this training as Done?
   Attendance will no longer be editable afterward.") before proceeding. If confirmed (or if
   attendance already has entries), it locks attendance (`AttendanceLocked = true`, even if
   empty) and sets `Status = Done`, mirroring path 1's effect.

> **Assumption to confirm later:** the shortcut is documented above as always locking
> attendance (same end-state as path 1), with the confirmation dialog only guarding against
> *accidentally* skipping attendance entry — not as a way to leave attendance unlocked. Flag if
> that's not what you intended.

---

## Next steps

- Produce the formal gap-analysis table (step 3) comparing this target definition against
  `TrainingDetailPage.razor`, `TrainerRunPage.razor`, `AttendancePage.razor`,
  `CreateEditTrainingPage.razor`, and the `TrainingStatus` enum / service layer.
- See the preliminary TODO list of implied application changes below.

## Preliminary TODO list (potential application changes)

> Derived directly from the resolved decisions above. This is a first pass, not yet a formal
> gap analysis against the actual code — items may be merged, split, or dropped once step 3 is
> done. Tracked in the session todo list for follow-up.

| # | Area | Change | Status |
|---|---|---|---|
| 1 | `TrainingStatus` enum / DB | Add `Cancelled` state (replacing the originally-proposed generic `Deleted` state). | ✅ Done |
| 2 | Training service / domain | Add explicit `Cancel` operation, restricted to `GA`/`SA`, only from `New`/`InPlanning`/`Planned`; terminal, not reversible. | ✅ Done |
| 3 | Training service / domain | Restrict hard-delete to `New`/`InPlanning` only (currently no status restriction found). | ✅ Done |
| 4 | Training service / domain | Add explicit `DONE` action ("Mark as Done / Finish"), distinct from attendance finalization, allowed for `AT`/`GA`/`SA` from `InProgress`. Triggered from `/attendance` ("Finalize Training") and `TrainingDetailPage` ("Mark as Done" shortcut) — see "Run Page & Training Completion Flow" section for details. | ✅ Done |
| 5 | `TrainingDetailPage.razor` | Restrict `Edit` button to `AT` only (currently shown to any `GA`/assigned trainer via `ShowManagementRow`); Group Admin should no longer see/use `Edit` once a trainer is assigned (except via a still-to-design "assign/remove trainer" flow). | ✅ Done |
| 6 | `TrainingDetailPage.razor` / `TrainerRunPage.razor` / `AttendancePage.razor` | Show `Run` and `Attendance` actions/buttons for `GA`/`SA` too, not just the assigned trainer (server-side already allows `GA` in `TrainerRunPage`/`AttendancePage` per `IsGroupAdmin`; the Detail Page footer button currently hides them for `GA`). | ✅ Done |
| 7 | `TrainingDetailPage.razor` | Restrict Register/Unregister (`RT`/`UT`) to actors holding the `U`/Participant role for the group (currently `_canRegister` is granted to any group member regardless of role, once registration is open). | ✅ Done |
| 8 | `TrainingDetailPage.razor` | Confirm/align `Take Training` (`TT`) stays `OT`-only (current `_canTake` literal-claim check already matches this — verify no regression). | ✅ Done |
| 9 | Domain / validation | Enforce no edits/cancel/delete once `3-InProgress` (verify current `CreateEditTrainingPage`/service layer doesn't allow edits mid-progress). | ✅ Done |
| 10 | Domain / validation | Enforce attendance immutability once `4-Done` (`AttendanceLocked` already exists — verify it's tied correctly to the new `DONE` action). | ✅ Done |
| 11 | UI/UX | Redesign the Detail Page footer layout to avoid the left/right zig-zag alignment noted earlier, once the button set per role/state is finalized. | ✅ Done |
| 12 | Domain / DB | Drop `TrainingBlock.EffectiveDurationMinutes` and `TrainingBlock.TrainerComment` fields/columns (migration); remove `UpdateTrainingBlockExecutionDto` duration/comment usage. | ✅ Done |
| 13 | Domain / DB | Add `TrainerFeedback` entity/table: one row per training (Comment, Rating 1–5, TrainerId, SubmittedAt), upserted by `AT` only, only once `4-Done`. | ✅ Done |
| 14 | Domain / DB | Add `ParticipantFeedback` entity/table: one row per participant per training (Comment, Rating 1–5, UserId, SubmittedAt), upserted by the submitting `U` only, only once `4-Done`, regardless of attendance status. | ✅ Done |
| 15 | `TrainerRunPage.razor` | Remove the post-last-block feedback-capture form (Actual Duration + Trainer Comment, superseded by item 12). Replace with the terminal virtual step described in "Run Page & Training Completion Flow" (items 19–22 below); `/run` must never call any state-changing service. | ✅ Done |
| 16 | New page | Add `/trainings/{slug}/{id}/feedback` page: submit form for `AT` (trainer feedback) and any registered `U` (participant feedback); anonymized participant feedback list + aggregate (avg stars + count) for `AT`/`OT`/`GA`/`SA`; own entry + aggregate for `U`. Only reachable once `4-Done`. | ✅ Done |
| 17 | Service layer | Enforce visibility rules server-side: trainer feedback never returned to `U`; participant feedback returned to staff without submitter identity; aggregate available to everyone. | ✅ Done |
| 18 | Service layer | Enforce eligibility: both feedback types only allowed once training `Status == Done`; fully optional (no gating of `DONE` or any other transition); editable indefinitely after first submission (upsert, no lock). | ✅ Done |
| 19 | `TrainerRunPage.razor` | Add a "Previous" button so the trainer can navigate backward through blocks, not just forward. | ✅ Done |
| 20 | `TrainerRunPage.razor` | Add a virtual terminal step (index `N`, one past the last block) whose content depends on `Training.Status`: dry-run "Plan reviewed" message (`Planned`), live "Training session complete!" with `/attendance` + `/feedback` links (`InProgress`), or read-only recap with both links enabled (`Done`). | ✅ Done |
| 21 | `TrainerRunPage.razor` | Allow block browsing (dry run) while `Status == Planned`, via a new "Preview blocks" link on the existing "Ready to Start" intro screen; `Start Training` remains triggerable only from that fixed intro screen, not from within the block browser. | ✅ Done |
| 22 | `TrainerRunPage.razor` | Allow block browsing to remain available (read-only recap) once `Status == Done`, instead of becoming inaccessible. | ✅ Done |
| 23 | `AttendancePage.razor` | Rename "Finalize Attendance" button to "Finalize Training"; behavior unchanged (locks attendance + sets `Status = Done` in one call) — this is the primary `DONE` trigger. | ✅ Done |
| 24 | `TrainingDetailPage.razor` | Add a "Mark as Done" shortcut in the footer, visible only when `Status == InProgress`, for `AT`/`GA`/`SA`. If no attendance has been recorded yet, show a confirmation dialog before proceeding; on confirm (or if attendance already exists), lock attendance and set `Status = Done` — see the "Assumption to confirm later" note in the "Run Page & Training Completion Flow" section. | ✅ Done |

**Implementation note (item 4):** implemented as a single "Mark as Done" action that always
locks attendance via the existing `LockAttendanceAsync` (matching `/attendance`'s "Finalize
Training"), rather than a separate status-only transition — see the assumption recorded above.

