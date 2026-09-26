# Training Lifecycle Redesign — Gap Analysis (Step 3)

> Companion to `training-lifecycle-redesign.md` (the target definition). This document
> compares that target against the **current implementation**, with concrete file/method
> evidence for every gap. Each row maps to a TODO item (session todo list + the TODO table in
> the target-definition doc) so decisions can be tracked and actioned individually.

Files inspected: `TrainingDetailPage.razor`, `CreateEditTrainingPage.razor`,
`TrainerRunPage.razor`, `AttendancePage.razor`, `TrainingService.cs`,
`TrainingBlockService.cs`, `AuthorizationHelper.cs`, `Program.cs` (policies), `Training.cs`,
`TrainingBlock.cs`, `TrainingStatus.cs`, `RegistrationService.cs`.

---

## A. Actors & access control

| # | Target rule | Current implementation | Gap | TODO |
|---|---|---|---|---|
| A1 | `E` (edit) is `AT`-only | `CreateEditTrainingPage.razor` `OnInitializedAsync`: access allowed if `_isSuperAdmin \|\| _isGroupAdmin \|\| isAssignedTrainer` — **any Group Admin can fully edit any training in their group**, not just the assigned trainer | ✅ **Confirmed gap** — broader than previously noted; this is page-*and-service*-level, not just a hidden button on the detail page | `lifecycle-restrict-edit-to-at` (12) — needs to be expanded: this isn't just about hiding the Detail Page button, the `/edit` page itself must stop granting full-edit rights to `GA`/`SA`. Trainer (re)assignment is the one exception GA/SA should keep (see A2). |
| A2 | `GA`/`SA` may still assign/remove a trainer (`E (assign/remove trainer)`) | `ReassignTrainerAsync` is a separate service method already gated by its own rule (blocked once `InProgress`/`Done`); the edit form lets `GA` change the `TrainerId` dropdown and calls it | ✅ No gap — this part already matches the target, but it's currently reachable *through* the same over-privileged edit page (see A1) rather than a narrower "assign trainer" action | New: `lifecycle-narrow-ga-edit-scope` — carve out trainer (re)assignment as the only `GA`/`SA` capability on an assigned training; block the rest of the form for `GA` once a trainer is assigned. |
| A3 | `TT` (Take) is `OT`-only | `TrainingDetailPage.razor`: `_canTake` checks the literal `"Trainer"` claim (not `_isTrainer`, so `GA` is excluded unless they separately hold the Trainer claim) | ✅ No gap — already compliant | `lifecycle-verify-take-ot-only` (8) → close as verified, no code change needed. |
| A4 | `RUN`/`ATT`/`DONE` available to `AT`, `GA`, `SA` | `TrainerRunPage.razor`/`AttendancePage.razor` server-side checks already allow `isAssignedTrainer \|\| isGroupAdmin`; `TrainingDetailPage.razor`'s footer buttons only check `_isAssignedTrainer` (hides them from `GA`) | ✅ Confirmed gap, UI-only | `lifecycle-show-run-att-for-ga-sa` (6) — unchanged, still needed. |
| A5 | `RT`/`UT` restricted to actors holding the `U` role | `TrainingDetailPage.razor`: `_canRegister = IsRegistrationOpen` — no role check at all; `RegistrationService.RegisterAsync` (service layer) also has **no role check**, only capacity/status rules | ✅ Confirmed gap, both UI and service layer | `lifecycle-restrict-register-to-u` (7) — needs to cover `RegistrationService` too, not just the page. |

## B. State transitions & actions

| # | Target rule | Current implementation | Gap | TODO |
|---|---|---|---|---|
| B1 | Editing a `2-Planned` training (`AT: E (anything)`) reverts it to `1-InPlanning` | `CreateEditTrainingPage.razor` `SaveTrainingAsync` → `TrainingService.UpdateAsync` passes `Status = _status` **unchanged** — saving edits to a `Planned` training leaves it `Planned` | ✅ **New gap found** — no auto-revert exists at all | New: `lifecycle-revert-planned-on-edit` |
| B2 | Reassigning the trainer while `Planned` should also revert to `InPlanning` (it's an edit) | `TrainingService.ReassignTrainerAsync`: `training.Status = newTrainerId.HasValue ? (training.Status == New ? InPlanning : training.Status) : New;` — if status was `Planned`, it **stays `Planned`** after a trainer swap | ✅ **New gap found** | New: `lifecycle-revert-planned-on-reassign` |
| B3 | Only `AT` marks a training `Planned` (`AT: E (mark planned)`) | `CreateEditTrainingPage.razor`: `_showConfirmPlanned` has no actor check beyond already being allowed on the page — since `GA` has full page access (A1), `GA` can also click "Confirm Planned" | ✅ Confirmed gap, inherits from A1 | Covered by `lifecycle-narrow-ga-edit-scope`. |
| B4 | No edits/cancel/delete possible once `3-InProgress` | `CreateEditTrainingPage.razor` access check does **not** look at `training.Status` at all — an assigned trainer (or `GA`) can open `/edit` and call `UpdateAsync` for an `InProgress` or even `Done` training | ✅ **Confirmed gap** (previously listed as "to verify" — now confirmed real) | `lifecycle-enforce-no-edit-inprogress` (9) — upgrade from "verify" to "implement guard". |
| B5 | Hard delete (`D`) only from `New`/`InPlanning` | `TrainingService.DeleteAsync` calls the repository unconditionally — **no status check at all** | ✅ Confirmed gap | `lifecycle-restrict-hard-delete` (3) — unchanged. |
| B6 | `Cancel` (`CX`) action + `9-Cancelled` state | Does not exist anywhere in the domain/service/UI | ✅ Confirmed gap (net-new feature) | `lifecycle-add-cancel-action` (2), `lifecycle-add-cancelled-state` (1) — unchanged. |
| B7 | `DONE` is a distinct action from `ATT` | `AttendancePage.razor` `FinalizeAttendanceAsync` → `TrainingService.LockAttendanceAsync` sets `AttendanceLocked = true` **and** `Status = Done` in the same call — today they are the same action | ✅ Confirmed gap | `lifecycle-add-done-action` (4), `lifecycle-attendance-rename-finalize` (23), `lifecycle-detail-mark-as-done-shortcut` (24) — unchanged. |
| B8 | `SetStatusAsync` should not allow arbitrary/invalid transitions | `ITrainingService.SetStatusAsync(trainingId, status)` is fully generic — sets any status with no transition validation; currently unused by any page but is a latent risk since it bypasses every rule above | ✅ **New gap found** (defensive/code-quality) | New: `lifecycle-guard-setstatus-transitions` |

## C. Feedback

| # | Target rule | Current implementation | Gap | TODO |
|---|---|---|---|---|
| C1 | Per-block `EffectiveDurationMinutes`/`TrainerComment` dropped entirely | `TrainingBlock` entity, `TrainingBlockDtos.cs`, `TrainingBlockService.UpdateExecutionAsync`, `TrainerRunPage.razor` "complete" phase all still implement this | ✅ Confirmed gap (to be removed) | `lifecycle-feedback-drop-per-block` (12) — unchanged. |
| C2 | `TrainerFeedback`/`ParticipantFeedback` entities, dedicated page, visibility/eligibility rules | None of this exists | ✅ Confirmed gap (net-new feature) | `lifecycle-feedback-trainer-entity` (13), `lifecycle-feedback-participant-entity` (14), `lifecycle-feedback-new-page` (16), `lifecycle-feedback-visibility-rules` (17), `lifecycle-feedback-eligibility-rules` (18) — unchanged. |

## D. Run page & completion flow

| # | Target rule | Current implementation | Gap | TODO |
|---|---|---|---|---|
| D1 | `/run` never changes training state | `TrainerRunPage.razor` `StartTrainingAsync()` **does** call `TrainingService.StartAsync` (a state change) directly from `/run`'s "Ready to Start" screen | ⚠️ **Clarification needed** — is `Start Training` (Planned→InProgress) exempt from "never changes state", since it's the one deliberate `RUN` action itself? The target's own Actions table lists `RUN` as changing state (`2-Planned -> AT\|GA\|SA: RUN -> 3-InProgress`), so this looks consistent, not a gap — flagging only so it's explicit that "`/run` never changes state" refers to the block-browsing part, not the initial `RUN` trigger. | No code change; doc wording clarification recommended. |
| D2 | "Previous" button for backward block navigation | `TrainerRunPage.razor` only has "Next Block" | ✅ Confirmed gap | `lifecycle-run-previous-button` (19) — unchanged. |
| D3 | Virtual terminal step, content varies by status | Today's "complete" phase is a fixed feedback-capture form regardless of status, and `_phase` isn't tied to `Training.Status` at all (resets on navigation) | ✅ Confirmed gap | `lifecycle-run-terminal-step` (20) — unchanged. |
| D4 | Dry-run block browsing while `Planned` | `TrainerRunPage.razor`: `Status == Planned` shows only the "Ready to Start" card — no block browsing at all before starting | ✅ Confirmed gap | `lifecycle-run-dry-run-planned` (21) — unchanged. |
| D5 | Read-only block browsing after `Done` | Not applicable today since blocks are only shown during `InProgress`; no explicit block browsing exists for `Done` | ✅ Confirmed gap | `lifecycle-run-readonly-after-done` (22) — unchanged. |
| D6 | `/attendance` "Finalize Training" as primary `DONE` trigger | Exists today as "Finalize Attendance" (see B7) — needs rename + to remain the primary trigger | ✅ Confirmed gap (rename + confirm semantics) | `lifecycle-attendance-rename-finalize` (23) — unchanged. |
| D7 | Detail-page "Mark as Done" shortcut with confirmation dialog | Does not exist | ✅ Confirmed gap (net-new) | `lifecycle-detail-mark-as-done-shortcut` (24) — unchanged. |

---

## New gaps discovered during this analysis (not previously tracked)

| Todo id | Title | Why it matters | Status |
|---|---|---|---|
| `lifecycle-narrow-ga-edit-scope` | Narrowing GA's edit scope to trainer (re)assignment only | Currently `GA` has full, unrestricted edit rights on any training in their group — the single biggest deviation from "planning is `AT`-only" found in this pass. Affects A1, A2, B3. | ✅ Done |
| `lifecycle-revert-planned-on-edit` | Reverting Planned training to InPlanning on any edit | `UpdateAsync` never demotes status; a `Planned` training silently stays `Planned` even after its content changes, contradicting the state diagram. | ✅ Done |
| `lifecycle-revert-planned-on-reassign` | Reverting Planned training to InPlanning on trainer reassignment | `ReassignTrainerAsync` only promotes `New → InPlanning`; it never demotes `Planned → InPlanning` when the trainer changes. | ✅ Done |
| `lifecycle-guard-setstatus-transitions` | Guarding SetStatusAsync against invalid transitions | Fully generic status setter with zero transition validation; currently unused by any page, but a foot-gun for future callers (including the new `DONE`/`Cancel` work) — should validate against the state diagram or be replaced by purpose-specific methods only. | ✅ Done |

All four gaps above were **adopted** and are now implemented (see
`training-lifecycle-redesign.md`'s TODO list, items 1–24, all marked done). The state-machine
guard lives in `TrainingService.IsValidTransition` and mirrors the state diagram exactly.

---

## Next steps

- ~~Decide, per gap above, whether to **adopt** (implement as described), **reject** (leave as
  current behavior, update the target doc instead), or **defer** (explicitly out of scope for
  now).~~ All four gaps were adopted.
- ~~Once decided, proceed to step 4 (redesign/implementation) for the adopted items.~~ Step 4
  (implementation) is complete: domain/DB, Application/Infrastructure services, and all
  affected Razor pages (`TrainingDetailPage`, `TrainerRunPage`, `AttendancePage`,
  `CreateEditTrainingPage`, new `FeedbackPage`) have been updated, an EF Core migration was
  generated and applied, and the full test suite passes.

