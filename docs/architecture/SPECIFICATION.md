# Application Specification — Trainings

> **Version:** 1.0.16
> **Language:** English (US)
> **Primary audience:** AI agents and automated tooling
> **Secondary audience:** Human developers and stakeholders

---

## 1. Overview

| Field            | Value                                                                    |
|------------------|--------------------------------------------------------------------------|
| **Application**  | Trainings                                                                |
| **Purpose**      | Plan training sessions, manage registrations, and track attendance for sport organizations with multiple groups |
| **Architecture** | ASP.NET Core Blazor Server, Clean Architecture (Domain / Application / Infrastructure / Web) |
| **Persistence**  | Relational database via Entity Framework Core                            |
| **Auth**         | Cookie-based authentication with **policy-based authorization**; self-registration with email confirmation and per-group admin approval |

### Human Summary

The **Trainings** application lets sport organizations manage training events across multiple groups.
Users self-register, confirm their email, and request membership in one or more groups.
A group admin (or SuperAdmin) approves or declines each request independently.
Users are **active from creation** and can log in immediately to see their request statuses and update their profile.
Once accepted into a group, participants see that group's trainings and can register for sessions.
Trainers create trainings for their own groups; admins manage group members and mail configuration.
The SuperAdmin manages system-level settings, all groups, and all users.

Two system-level roles exist on the `User` entity: **SuperAdmin** and **User** (regular).
All other role distinctions (Admin, Trainer, Participant) are per-group via `GroupMembership`.

---

## 2. Domain Model

> Entity field details are defined in the POCO classes under `src/Trainings.Domain/Entities/`.

### 2.1 Enumerations

| Enum                       | Values                                        | Notes |
|----------------------------|-----------------------------------------------|-------|
| `UserRole`                 | `SuperAdmin`, `User`                          | **Changed:** removed Admin, Trainer, Participant — those are now per-group roles |
| `GroupMemberRole`          | `Admin`, `Trainer`, `Participant`              | Unchanged; represents a user's role within a specific group |
| `GroupMembershipStatus`    | `Pending`, `Approved`, `Declined`              | **New:** lifecycle status of a group membership request |
| `TrainingStatus`           | `New`, `InPlanning`, `Planned`, `InProgress`, `Done` | **Changed:** full training lifecycle (see below); replaces the earlier two-state `New`/`Planned` model |
| `RegistrationStatus`       | `Registered`, `Cancelled`                      | Unchanged |
| `AttendanceStatus`         | `Present`, `Absent`                            | Unchanged |
| `Gender`                   | `Male`, `Female`, `Other`                      | Unchanged |
| `NotificationAction`       | `PasswordReset`, `Registration`, `EmailConfirmation`, `WelcomeMail`, `GroupApproval`, `GroupRejection`, `TestEmail` | **New:** categorizes notification log entries |

### 2.2 Entity Changes

#### `User` — Changes

| Field      | Change | Detail |
|------------|--------|--------|
| `Role`     | **Changed** | Type becomes `UserRole` with values `SuperAdmin` / `User` only |
| `IsActive` | **Clarified** | Defaults to `true` on creation. Only set to `false` when an Admin or SuperAdmin explicitly deactivates the user. Never set to `false` by registration or approval workflow. |

> **Key rule:** A newly registered user can log in immediately, see their group request statuses on the dashboard, and update their profile — even while approval is pending.

#### `GroupMembership` — Changes (replaces `PendingGroupRequest`)

The separate `PendingGroupRequest` entity is **removed**. Its lifecycle is now handled by `GroupMembership` with a status field.

| Field                | Type                    | Notes |
|----------------------|-------------------------|-------|
| `Id`                 | `int` (PK)              | |
| `UserId`             | `int` (FK → User)       | |
| `GroupId`            | `int` (FK → Group)      | |
| `Role`               | `GroupMemberRole`        | Default: `Participant` for self-registration |
| `Status`             | `GroupMembershipStatus`  | **New.** `Pending` on request, `Approved` on acceptance, `Declined` on rejection |
| `IsActive`           | `bool`                  | `true` when membership is active. Can be deactivated by Admin. |
| `RequestedAt`        | `DateTime`              | **New.** UTC timestamp of the original request |
| `ApprovedAt`         | `DateTime?`             | **New.** UTC timestamp when approved (null if not yet approved) |
| `DeclinedAt`         | `DateTime?`             | **New.** UTC timestamp when declined (null if not declined) |
| `JoinedAt`           | `DateTime`              | Legacy field from prior schema. Prefer `RequestedAt`/`ApprovedAt` for new logic. Retained to avoid a breaking migration. |

> **History:** Rows are never deleted. A declined user can re-apply, creating a new `GroupMembership` row with `Status = Pending`.

#### `Group` — Changes

| Field          | Type       | Notes |
|----------------|------------|-------|
| `Slug`         | `string`   | **New.** URL-friendly identifier, auto-generated from `Name`. Must be unique. |
| `Identifier`   | `string`   | **New.** Human-readable short code. Must be unique. Validation error on duplicate. |

> Navigation collection `PendingRequests` is removed; pending requests are now `Memberships.Where(m => m.Status == Pending)`.

#### `Training` — Changes

| Field       | Change | Detail |
|-------------|--------|--------|
| `GroupId`   | **Now required** | Every training belongs to exactly one group. `int` (non-nullable FK → Group). |
| `TrainerId` | **Now nullable** | A training can exist with no trainer assigned yet (`Status = New`). `int?` (nullable FK → User). |
| `Status`    | **Changed** | `TrainingStatus` lifecycle: `New` → `InPlanning` → `Planned` → `InProgress` → `Done` (see lifecycle below). |

##### Training lifecycle

```
New ──(Trainer takes it)──► InPlanning ──(Trainer confirms)──► Planned ──(Trainer starts)──► InProgress ──(Attendance finalized/locked)──► Done
 ▲                               │
 └────(Trainer/Admin releases────┘
       or reassigns trainer)
```

| Status       | Meaning | Who moves it here |
|--------------|---------|--------------------|
| `New`        | Created by a GroupAdmin (or SuperAdmin), no trainer assigned yet, or a trainer released themselves. | `CreateAsync` (no trainer given) / `ReleaseTrainerAsync` / `ReassignTrainerAsync(null)` |
| `InPlanning` | A trainer has taken (or been assigned) the training and is preparing it (blocks, details, location, etc.). | `TakeAsync` / `ReassignTrainerAsync` (trainer set) / `CreateAsync` (trainer pre-assigned) |
| `Planned`    | The assigned trainer confirmed the training is ready. | `ConfirmPlannedAsync` |
| `InProgress` | The assigned trainer manually started the session; registration/cancellation is closed. | `StartAsync` |
| `Done`       | Attendance has been finalized and locked; no further changes to registrations or attendance are allowed. | `LockAttendanceAsync` |

- **Creation (`CreateAsync`)**: A GroupAdmin (or SuperAdmin) creates a training specifying `Group`, `Date`/`Start`, `Location`, and `MaxParticipants` as mandatory fields (`Start`, `Duration`, `End` default from the Group's settings). `Trainer`, `Title`, and `Description` may be left blank — an unspecified `Title` is auto-generated (e.g. `"Training of 10.09.2026"`). If no `Trainer` is given the training starts at `New`; if a `Trainer` is pre-assigned it starts at `InPlanning`.
- **Take (`TakeAsync`)**: Any Trainer of the training's group can self-assign to an unassigned (`New`) training, moving it to `InPlanning`.
- **Release (`ReleaseTrainerAsync`)**: The currently assigned trainer can un-assign themselves while `InPlanning` or `Planned`, moving the training back to `New`. Not allowed once `InProgress` or `Done`.
- **Reassign (`ReassignTrainerAsync`)**: GroupAdmin/SuperAdmin can assign, change, or clear the trainer at any time before `InProgress`/`Done`.
- **Confirm Planned (`ConfirmPlannedAsync`)**: The assigned trainer (or GroupAdmin/SuperAdmin) confirms planning is complete, moving `InPlanning` → `Planned`.
- **Start (`StartAsync`)**: Only the assigned trainer can manually start the training, moving `Planned` → `InProgress` and closing registration/cancellation.
- **Lock attendance (`LockAttendanceAsync`)**: Only allowed once `InProgress`; finalizing attendance moves the training to `Done`, after which no registrations, attendance changes, or edits are permitted.

> A trainer who wants the same training in two groups must create it separately for each group (copy is out of scope). Registration/cancellation is open while `Status` is `New`, `InPlanning`, or `Planned`, and closed once `InProgress` or `Done` — the previous date-window-based rule (4 weeks / 4 days) was removed in favor of a purely status-based rule.

##### Visibility while unplanned

- A participant of an accepted group sees a training in their list **as soon as it is created** by the GroupAdmin (even while `New`/unassigned), and **can register** for it while registration is open (`New`/`InPlanning`/`Planned`).
- However, the **full detail view** (description, training blocks, participant list) is only shown once the training reaches `Planned` or later, or to the assigned Trainer / GroupAdmin / SuperAdmin at any status. Other viewers see a limited preview (title, group, date/time, location, status) until planning is finished.

#### `MailConfiguration` — **New Entity**

System-level SMTP configurations defined by SuperAdmin.

| Field                | Type       | Notes |
|----------------------|------------|-------|
| `Id`                 | `int` (PK) | |
| `Name`               | `string`   | Display name (e.g., "Primary SMTP", "Backup Brevo") |
| `Host`               | `string`   | SMTP server hostname |
| `Port`               | `int`      | SMTP port |
| `Username`           | `string`   | SMTP username |
| `Password`           | `string`   | SMTP password (encrypted at rest using ASP.NET Core Data Protection API) |
| `FromAddress`        | `string`   | Sender email address |
| `Priority`           | `int`      | System-level priority (1 = highest). Unique. |
| `IsActive`           | `bool`     | |
| `FailureCount`       | `int`      | **Counter:** incremented on each send failure |
| `LastFailedOn`       | `DateTime?`| **Timestamp:** last failure date |
| `CreatedAt`          | `DateTime` | |

#### `GroupMailConfiguration` — **New Entity**

Per-group override of which mail configurations to use and in what priority.

| Field                | Type       | Notes |
|----------------------|------------|-------|
| `Id`                 | `int` (PK) | |
| `GroupId`            | `int` (FK → Group) | |
| `MailConfigurationId`| `int` (FK → MailConfiguration) | |
| `Priority`           | `int`      | Group-specific priority (1 = highest). Unique per group. |

> If a group has no `GroupMailConfiguration` rows, the system-level defaults (by `MailConfiguration.Priority`) are used. If a group has overrides, only those are used in the group's own priority order.

#### `NotificationLog` — **New Entity**

Immutable audit log for all sent (and failed) notifications.

| Field                | Type                   | Notes |
|----------------------|------------------------|-------|
| `Id`                 | `int` (PK)             | |
| `Action`             | `NotificationAction`   | What triggered the notification |
| `RecipientEmail`     | `string`               | Target email address |
| `UserId`             | `int?` (FK → User)     | Recipient user (nullable for edge cases) |
| `MailConfigurationId`| `int?` (FK → MailConfiguration) | Which config was used (null if none available) |
| `GroupId`            | `int?` (FK → Group)    | Group context (null for system-level like password reset) |
| `IsSuccess`          | `bool`                 | |
| `ErrorMessage`       | `string?`              | Error details on failure; null on success |
| `CreatedAt`          | `DateTime`             | |

> **No email on failure.** On failure: increment `MailConfiguration.FailureCount`, set `LastFailedOn`, and write a `NotificationLog` entry.

#### `SlugRedirect` — **New Entity**

Generic redirect history for renamed slugs (groups, or any future sluggable entity).

| Field          | Type       | Notes |
|----------------|------------|-------|
| `Id`           | `int` (PK) | |
| `OldSlug`      | `string`   | The previous slug value |
| `NewSlug`      | `string`   | The slug it was renamed to |
| `EntityType`   | `string`   | E.g., `"Group"` — allows reuse for any entity |
| `ChangedAt`    | `DateTime` | When the rename occurred |

> On 404: consult `SlugRedirect` for the `EntityType`. If multiple redirects exist (chain renames), use the latest `ChangedAt` entry.

### 2.3 Relationships (ERD notation)

```
User              ||--o{  Training              : "trains (TrainerId)"
User              ||--o{  Registration          : "registers"
User              ||--o{  Attendance            : "has attendance"
User              ||--o{  GroupMembership       : "is member of / requests to join"
Training          ||--o{  Registration          : "has"
Training          ||--o{  Attendance            : "records"
Training          ||--o{  TrainingBlock         : "structured by"
Training          }o--||  Group                 : "belongs to (required)"
Group             ||--o{  GroupMembership       : "has members"
Group             ||--o{  GroupMailConfiguration: "has mail configs"
Group             ||--o{  Tag                   : "owns"
MailConfiguration ||--o{  GroupMailConfiguration: "assigned to groups"
MailConfiguration ||--o{  NotificationLog       : "used by"
TrainingBlock     ||--o{  TrainingBlockTag      : "tagged with"
Tag               ||--o{  TrainingBlockTag      : "applied to"
```

> `PendingGroupRequest` entity is **removed** — replaced by `GroupMembership` with `Status = Pending`.

---

## 3. Authorization Model

### 3.1 Policy-Based Authorization

The application uses **ASP.NET Core policy-based authorization**. Role-based `[Authorize(Roles = "...")]` attributes are replaced with named policies.

#### Claims Strategy

On login, the authentication cookie includes:
1. `ClaimTypes.NameIdentifier` — the user's `Id`
2. `ClaimTypes.Email` — the user's email
3. `"SuperAdmin"` = `"true"` — only if `User.Role == SuperAdmin`
4. `"GroupRole::{groupId}"` = `"Admin"` / `"Trainer"` / `"Participant"` — one claim per **approved** `GroupMembership` row

#### Defined Policies

| Policy Name        | Requirement                                                             |
|--------------------|-------------------------------------------------------------------------|
| `SuperAdmin`       | Has `SuperAdmin = true` claim                                           |
| `GroupAdmin`       | Has at least one `GroupRole::*` = `Admin` claim, **or** is SuperAdmin   |
| `GroupTrainer`     | Has at least one `GroupRole::*` = `Trainer` claim, **or** is GroupAdmin  |
| `GroupMember`      | Has at least one `GroupRole::*` claim with `Status = Approved`, **or** is SuperAdmin |
| `Authenticated`    | Any logged-in user (including users with only pending requests)          |

#### Group-Scoped Checks

Policies cannot know which specific group a page/action refers to. For group-specific authorization (e.g., "is this user Admin of Group 5?"), use **service-level checks** via an `IAuthorizationHelper` that inspects the user's `GroupRole::{groupId}` claims at runtime. Pages pass the current group ID to the helper before executing group-scoped operations.

### 3.2 Permissions Matrix

All permissions below refer to **per-group roles** from `GroupMembership` unless marked "system-level."

| Action                                 | SuperAdmin | Group Admin | Group Trainer | Group Participant |
|----------------------------------------|:----------:|:-----------:|:-------------:|:-----------------:|
| **System-level**                       |            |             |               |                   |
| Manage system mail configurations      |    ✔       |             |               |                   |
| Manage all groups (create/edit/delete) |    ✔       |             |               |                   |
| View all users across all groups       |    ✔       |             |               |                   |
| View SuperAdmin dashboard (mail stats) |    ✔       |             |               |                   |
| Approve/decline any group request      |    ✔       |             |               |                   |
| Reset counter on notification dashboard|    ✔       |             |               |                   |
| **Group-scoped**                       |            |             |               |                   |
| Approve/decline group requests         |    ✔       |  ✔ (own)    |               |                   |
| Create/edit/deactivate users in group  |    ✔       |  ✔ (own)    |               |                   |
| Assign group mail configuration        |    ✔       |  ✔ (own)    |               |                   |
| View users page (own group's members)  |    ✔       |  ✔ (own)    |               |                   |
| Create/edit/delete training for group  |    ✔       |  ✔ (own)    | ✔ (own group) |                   |
| Record/view attendance                 |    ✔       |  ✔ (own)    | ✔ (own trng)  |                   |
| Manage tags                            |    ✔       |  ✔ (own)    |               |                   |
| Register for a training                |            |             |               | ✔ (own group)     |
| Cancel own registration                |            |             |               | ✔                 |
| View trainings of accepted groups      |    ✔       |  ✔ (own)    | ✔ (own group) | ✔ (own group)     |
| View own group request statuses        |    ✔       |  ✔          | ✔             | ✔                 |
| Update own profile                     |    ✔       |  ✔          | ✔             | ✔                 |
| **Trigger email actions (Admin only)** |            |             |               |                   |
| Send account verification mail to user |    ✔       |  ✔ (own)    |               |                   |

---

## 4. Features & Use Cases

### UC-01 — User Self-Registration

- **Actor:** Anonymous visitor
- **Steps:**
  1. Fill in the registration form: `FirstName`, `LastName`, `Email`, `Password`, `Gender`, `Birthday`, `Mobile`, `City`, `WelcomeMessage`.
  2. Select one or more groups to request membership (each defaults to role `Participant`).
  3. Receive a neutral account email with the confirmation link; click to confirm email.
  4. Log in immediately — `IsActive = true` from creation.
  5. On the dashboard, see the status of each group request (Pending / Approved / Declined).
- **Postcondition:** User record created with `IsActive = true`, `Role = User`. One `GroupMembership` row per requested group with `Status = Pending`.
- **Business Rule:** User can update profile and re-apply to declined groups while other requests are still pending.

### UC-02 — Approve / Decline Group Requests

- **Actor:** Group Admin or SuperAdmin
- **Steps:**
  1. Navigate to the users/requests page.
  2. Group Admins see only pending requests for their own groups. SuperAdmin sees all.
  3. Approve or decline each request.
  4. On approval: set `GroupMembership.Status = Approved`, `ApprovedAt = now`. User gains access to group trainings.
  5. On decline: set `GroupMembership.Status = Declined`, `DeclinedAt = now`. The user is **never deleted**.
  6. The membership request is updated for the managed group(s).
- **Postcondition:** `GroupMembership` row updated.
- **Business Rule:** If a group has no Admin, only the SuperAdmin can process requests for that group.

### UC-03 — Manage Users (Admin / SuperAdmin)

- **Actor:** Group Admin or SuperAdmin
- **Steps:**
  1. Navigate to user management.
  2. Group Admin sees only users of their groups and pending requests for their groups. The group column shows which group(s) each user belongs to.
  3. SuperAdmin sees all users across all groups with group information displayed.
  4. Create a new user and assign one or more groups. Group Admins can only choose from groups they manage.
  5. Edit or deactivate an existing user.
  6. **Trigger email actions** with a single account-mail action that sends a neutral verification/setup email.
  7. Password reset mail is a separate self-service flow ("Forgot Password").
  8. Users created by Admin are **not automatically notified**. The Admin explicitly triggers the account mail.
- **Postcondition:** User record persisted; password stored as bcrypt hash.
- **Visual indicator:** Show whether the user has verified their email and/or reset their password.

### UC-04 — Manage Training Sessions (GroupAdmin / Trainer / SuperAdmin)

- **Actor:** Group Admin or SuperAdmin (create); Group Trainer, the assigned Trainer, Group Admin, or SuperAdmin (edit/lifecycle actions)
- **Steps:**
  1. GroupAdmin (or SuperAdmin) creates a training with `Group`, `Location`, `Date` (mandatory); `Start`, `Duration`, `End` default from the Group; `Trainer`, `Title`, `Description` are optional.
  2. Any Trainer of the group can **Take** an unassigned training (`New` → `InPlanning`).
  3. The assigned Trainer edits the training while planning (`Trainer`, `Title`, `Description`, `MaxParticipants`, `Start`/`Duration`/`End`, `Location` restricted to the group's allowed locations including "Other").
  4. The assigned Trainer (or GroupAdmin/SuperAdmin) **releases** the trainer assignment (`InPlanning`/`Planned` → `New`) or GroupAdmin/SuperAdmin **reassigns** the trainer directly.
  5. The assigned Trainer **confirms planning is done** (`InPlanning` → `Planned`).
  6. The assigned Trainer **starts** the training on the day (`Planned` → `InProgress`), closing registration.
- **Business Rules:**
  - `GroupId` is required — every training belongs to exactly one group.
  - Only GroupAdmin/SuperAdmin can create a training; a Trainer cannot access the create page.
  - A Trainer may only take/edit trainings in their own group(s), and only the currently assigned Trainer (or GroupAdmin/SuperAdmin) may edit a training once assigned.
  - `MaxParticipants` (Capacity) must be ≥ 1.
  - `Date`/`Start` must be strictly in the future at creation time.
  - No changes are allowed once `Status = Done`.
  - Copying a training across groups is supported via block-library and cross-training copy features.

### UC-04a — Training Lifecycle Actions (Trainer / GroupAdmin / SuperAdmin)

See the **Training lifecycle** table above (`New → InPlanning → Planned → InProgress → Done`) for the full state machine and the actions (`TakeAsync`, `ReleaseTrainerAsync`, `ReassignTrainerAsync`, `ConfirmPlannedAsync`, `StartAsync`, `LockAttendanceAsync`) that transition it.

### UC-05 — Manage Training Blocks (Trainer / Admin)

- **Actor:** Group Trainer or Group Admin or SuperAdmin
- **Steps:**
  1. Add ordered blocks to a training session (title, description, planned duration, tags).
  2. Reorder, edit, or delete existing blocks.
  3. Copy a block from one training to another.
  4. Browse the shared block library to reuse blocks across trainings.
- **Postcondition:** Training plan is structured into time-boxed segments.

### UC-06 — Register for a Training (Participant)

- **Actor:** Group Participant (with `Status = Approved` in the group)
- **Steps:**
  1. Browse active trainings **of their accepted groups only**. No other trainings are visible; trainings appear as soon as the GroupAdmin creates them, regardless of trainer assignment/planning state.
  2. Register for a training that has available capacity and open registration.
- **Business Rules:**
  - A participant cannot register for the same training twice.
  - Registration is not allowed when `RegisteredCount >= Capacity`.
  - Only users with `GroupMembership.Status = Approved` in the training's group can see or register.
  - Registration/cancellation is open while `Status` is `New`, `InPlanning`, or `Planned`; closed once `InProgress` or `Done`. Registering is allowed even before a trainer is assigned or planning is finished.
  - The full detail page (description, blocks, participant list) is only visible once `Status >= Planned`, or at any status to the assigned Trainer / GroupAdmin / SuperAdmin; other viewers see a limited preview.

### UC-07 — Cancel Registration (Participant)

- **Actor:** Participant
- **Steps:**
  1. View own registrations.
  2. Cancel a registration that is in `Registered` status.
- **Postcondition:** `RegistrationStatus` set to `Cancelled`.

### UC-08 — Record Attendance (Trainer / Admin)

- **Actor:** Group Trainer or Group Admin or SuperAdmin
- **Steps:**
  1. Select a training session (only reachable once the assigned Trainer has started it, i.e. `Status = InProgress`).
  2. For each registered participant, mark `Present` or `Absent`.
  3. Finalize the attendance sheet, locking it and moving the training to `Status = Done`.
- **Business Rules:**
  - Only users with an active `Registered` registration may have attendance recorded.
  - The trainer recording attendance must be the assigned trainer of that session (or a group Admin / SuperAdmin).
  - Attendance drafts may be saved any time while `Status = InProgress`.
  - Finalizing attendance requires `Status = InProgress`; once finalized the training becomes `Done` and no further attendance or registration changes are allowed.

### UC-09 — View Attendance Report (Trainer / Admin)

- **Actor:** Group Trainer or Group Admin or SuperAdmin
- **Steps:**
  1. Select a training session.
  2. View a list of attendees with their `AttendanceStatus`.

### UC-10 — Manage Groups (SuperAdmin)

- **Actor:** SuperAdmin
- **Steps:**
  1. Create a group with `Name`, `Identifier` (unique short code), optional `Description`. `Slug` is auto-generated from `Name`.
  2. Edit group details. **Warning shown** when changing the name: "Changing the group name will update the URL slug. Old URLs will redirect but may break external bookmarks."
  3. On rename: old slug is saved to `SlugRedirect` table with the new slug and the date of change. Note: this also affects search-engine-indexed URLs for public-facing pages.
  4. Deactivate or delete a group.
- **Business Rules:**
  - `Name` and `Identifier` must be unique. Duplicate → validation error, SuperAdmin must choose a different value.
  - Slug must be unique. On 404, check `SlugRedirect` for the entity type and redirect to the latest matching new slug.

### UC-11 — Manage Tags (Admin)

- **Actor:** Group Admin or SuperAdmin
- **Steps:**
  1. Create tags optionally scoped to a group.
  2. Tags can be applied to training blocks to categorize content.

### UC-12 — Password Reset

- **Actor:** Any user (authenticated or not)
- **Steps:**
  1. Request a password reset link via email.
  2. Follow the link and set a new password.

### UC-13 — Manage System Mail Configuration (SuperAdmin)

- **Actor:** SuperAdmin
- **Steps:**
  1. Define one or more SMTP mail configurations with `Name`, `Host`, `Port`, `Username`, `Password`, `FromAddress`.
  2. Assign each a `Priority` (1 = highest). Priority 1 is tried first; on failure, priority 2 is tried, etc.
  3. View failure counters and `LastFailedOn` per configuration.
  4. Activate / deactivate individual configurations.
- **Usage:** System-level configs are used for all mail types: account verification, password reset, admin registration notification, and test emails.

### UC-14 — Manage Group Mail Configuration (Group Admin)

- **Actor:** Group Admin or SuperAdmin
- **Steps:**
  1. Assign one or more system-level mail configurations to a group.
  2. Set a group-specific `Priority` for each assigned configuration.
  3. If a group has its own assignments, only those are used (in the group's priority order). If none are assigned, system defaults apply.
- **Visual indicator:** Groups with at least one mail configuration show a mail icon (📧). Groups without show no icon.

### UC-15 — SuperAdmin Dashboard — Notification Stats

- **Actor:** SuperAdmin
- **Steps:**
  1. View the dashboard section showing:
     - Latest notification log entries.
     - Green counter: successful messages within the last 30 days.
     - Red counter: failed messages within the last 30 days.
     - Total counters (since beginning): green and red.
   2. Use the notification log ID as the reference pointer when reviewing recent mail activity.
- **Data source:** `NotificationLog` table.

---

## 5. UI & Navigation

### 5.1 Navigation Menu (NavMenu)

```
🏠 Dashboard
📅 Trainings
📋 My Registrations       ← visible to users with at least one approved Participant membership
🎯 My Trainings           ← visible to users with at least one Trainer membership
───────────────────
👥 Users                  ← visible to GroupAdmin and SuperAdmin
📂 Groups                 ← visible to SuperAdmin
───────────────────
⚙️ Config                 ← visible to SuperAdmin (and GroupAdmin for group-specific settings)
───────────────────
🚪 Logout
v1.0.11
```

### 5.2 Role Badges

Display all assigned roles per user as compact colored badges:
- **P** (Participant) — e.g., blue
- **T** (Trainer) — e.g., green
- **A** (Admin) — e.g., orange
- **S** (SuperAdmin) — e.g., red

Display order: P, T, A (S shown separately for SuperAdmin users).

### 5.3 Dashboard Content per Role

| Role              | Dashboard Shows |
|-------------------|-----------------|
| **Participant**   | Group request statuses (Pending / Approved / Declined per group). Upcoming trainings for accepted groups. |
| **Trainer**       | Upcoming trainings they lead. |
| **Group Admin**   | Pending requests for their groups. Member counts. Recent trainings. Mail-related info (if group has mail config). |
| **SuperAdmin**    | All of the above + notification stats section (UC-15). |

### 5.4 Login Page — Getting Started Info Box

On a fresh installation (only the default seeded SuperAdmin exists):
- The login page displays an info box: *"This is a new installation. Log in with the default SuperAdmin account: **[email]** / **[password]** to get started."*
- The default email and password are read from `appsettings.json` under `Seed:Email` and `Seed:Password` — **single source of truth** used by both the `DbSeeder` and the login page info box.
- The info box **disappears** once the SuperAdmin account has been changed, updated, or deleted (i.e., when the seeded SuperAdmin's email or password no longer matches the values in `appsettings.json`).
- **Security note:** The default password `Admin123!` is intentionally simple for first-time setup. The SuperAdmin **must** change it immediately. Production deployments behind public URLs should override the `Seed` section with stronger credentials via environment variables or user secrets before first launch.

### 5.5 Global Running Modes

The application supports two global runtime switches:

- **Read Only** — blocks write operations for everyone except SuperAdmin.
- **No E-Mail** — suppresses outgoing email and shows the generated message in an in-app preview where the flow supports it.

The initial values are loaded from `App:Modes` in `appsettings.json` at startup.
A SuperAdmin can override both modes at any time via **⚙️ Config → Running Modes** without restarting the application.
Runtime overrides take effect immediately for all users and are automatically reset to the configured defaults on the next application restart.

When one or more runtime switches are active, the signed-in UI shows a visible mode indicator banner.

---

## 6. Seeding

The database seeder creates **exactly one** user:

| Field      | Value                                       |
|------------|---------------------------------------------|
| Email      | Read from `appsettings.json` → `Seed:Email` |
| Password   | Read from `appsettings.json` → `Seed:Password` |
| Role       | `SuperAdmin`                                |
| IsActive   | `true`                                      |
| EmailConfirmedAt | `DateTime.UtcNow`                     |

No other users, groups, or sample data are seeded.

### `appsettings.json` Seed Section

```json
{
  "Seed": {
    "Email": "superadmin@trainings.app",
    "Password": "Admin123!"
  }
}
```

> This section is the single source of truth for the default SuperAdmin credentials.

---

## 7. Technical Architecture

```
┌─────────────────────────────────────────┐
│  Trainings.Web  (Blazor Server)         │
│  - Razor components, pages, auth UI     │
│  - Policy-based authorization           │
└───────────────────┬─────────────────────┘
                    │ calls
┌───────────────────▼─────────────────────┐
│  Trainings.Application                  │
│  - Services: Training / User /          │
│    Registration / Attendance /          │
│    MailConfiguration / Notification     │
│  - DTOs, Interfaces                     │
└───────────────────┬─────────────────────┘
                    │ calls
┌───────────────────▼─────────────────────┐
│  Trainings.Infrastructure               │
│  - EF Core DbContext                    │
│  - Repositories                         │
│  - Password hasher, SMTP service        │
└───────────────────┬─────────────────────┘
                    │ depends on
┌───────────────────▼─────────────────────┐
│  Trainings.Domain                       │
│  - Entities, Enums, Interfaces          │
└─────────────────────────────────────────┘
```

### Layer Responsibilities

| Layer            | Responsibility                                                           |
|------------------|--------------------------------------------------------------------------|
| `Domain`         | Core entities, enumerations, repository interfaces — no external deps    |
| `Application`    | Business logic, use-case orchestration, DTO mapping                      |
| `Infrastructure` | EF Core persistence, auth, SMTP, concrete repository implementations     |
| `Web`            | Blazor Server UI, cookie auth middleware, policy registration, DI        |

---

## 8. Service Interfaces (Application Layer)

```csharp
ITrainingService
  Task<TrainingDto?>                   GetByIdAsync(int id, CancellationToken ct)
  Task<IEnumerable<TrainingDto>>       GetAllAsync(CancellationToken ct)
  Task<IEnumerable<TrainingDto>>       GetActiveAsync(CancellationToken ct)
  Task<IEnumerable<TrainingDto>>       GetByTrainerIdAsync(int trainerId, CancellationToken ct)
  Task<IEnumerable<TrainingDto>>       GetByGroupIdAsync(int groupId, CancellationToken ct)
  Task<IEnumerable<TrainingDto>>       GetForUserAsync(int userId, CancellationToken ct)
    // Returns trainings only from groups where user has Status = Approved
  Task<TrainingDto>                    CreateAsync(CreateTrainingDto dto, CancellationToken ct)
  Task                                 UpdateAsync(UpdateTrainingDto dto, CancellationToken ct)
  Task                                 DeleteAsync(int id, CancellationToken ct)
  // Lifecycle actions (see Training lifecycle table above)
  Task<TrainingDto>                    TakeAsync(int trainingId, int trainerId, CancellationToken ct)
  Task                                 ReleaseTrainerAsync(int trainingId, int requestingUserId, CancellationToken ct)
  Task                                 ReassignTrainerAsync(int trainingId, int? newTrainerId, CancellationToken ct)
  Task                                 ConfirmPlannedAsync(int trainingId, CancellationToken ct)
  Task                                 StartAsync(int trainingId, int requestingTrainerId, CancellationToken ct)
  Task                                 SetStatusAsync(int trainingId, TrainingStatus status, CancellationToken ct)
  Task                                 LockAttendanceAsync(int trainingId, CancellationToken ct)
    // Marks attendance locked and moves Status to Done
  // Training blocks
  Task<IEnumerable<TrainingBlockDto>>  GetBlocksAsync(int trainingId, CancellationToken ct)
  Task<TrainingBlockDto>               AddBlockAsync(CreateTrainingBlockDto dto, CancellationToken ct)
  Task                                 UpdateBlockAsync(UpdateTrainingBlockDto dto, CancellationToken ct)
  Task                                 DeleteBlockAsync(int blockId, CancellationToken ct)
  Task                                 CopyBlockAsync(int sourceBlockId, int targetTrainingId, CancellationToken ct)
  Task<IEnumerable<TrainingBlockDto>>  GetAllBlocksLibraryAsync(CancellationToken ct)

IUserService
  Task<UserDto?>                       GetByIdAsync(int id, CancellationToken ct)
  Task<UserDto?>                       GetByEmailAsync(string email, CancellationToken ct)
  Task<IEnumerable<UserDto>>           GetAllAsync(CancellationToken ct)
  Task<IEnumerable<UserDto>>           GetByGroupIdAsync(int groupId, CancellationToken ct)
  Task<UserDto>                        CreateAsync(CreateUserDto dto, CancellationToken ct)
    // Admin creates user in a group with selected role(s)
  Task                                 UpdateAsync(UpdateUserDto dto, CancellationToken ct)
  Task                                 DeactivateAsync(int id, CancellationToken ct)
  Task<bool>                           ValidatePasswordAsync(string email, string password, CancellationToken ct)
  Task                                 ChangePasswordAsync(int userId, string newPassword, CancellationToken ct)
  Task                                 SendEmailVerificationAsync(int userId, CancellationToken ct)
    // Admin-triggered: sends "verify your email" link
  Task                                 SendAccountMailAsync(int userId, CancellationToken ct)
    // Admin-triggered: sends the neutral account verification/setup email

IRegistrationService
  Task<IEnumerable<RegistrationDto>>   GetByUserIdAsync(int userId, CancellationToken ct)
  Task<IEnumerable<RegistrationDto>>   GetByTrainingIdAsync(int trainingId, CancellationToken ct)
  Task<RegistrationDto>                RegisterAsync(int userId, int trainingId, CancellationToken ct)
  Task                                 CancelAsync(int userId, int trainingId, CancellationToken ct)
  Task<bool>                           IsRegisteredAsync(int userId, int trainingId, CancellationToken ct)

IAttendanceService
  Task<IEnumerable<AttendanceDto>>     GetByTrainingIdAsync(int trainingId, CancellationToken ct)
  Task<IEnumerable<AttendanceDto>>     GetByUserIdAsync(int userId, CancellationToken ct)
  Task                                 RecordAttendanceAsync(int userId, int trainingId, AttendanceStatus status, int recordedByTrainerId, CancellationToken ct)

IGroupService
  Task<IEnumerable<GroupDto>>          GetAllAsync(CancellationToken ct)
  Task<GroupDto?>                      GetByIdAsync(int id, CancellationToken ct)
  Task<GroupDto?>                      GetBySlugAsync(string slug, CancellationToken ct)
  Task<GroupDto>                       CreateAsync(CreateGroupDto dto, CancellationToken ct)
  Task                                 UpdateAsync(UpdateGroupDto dto, CancellationToken ct)
    // Warns on name change; saves old slug to SlugRedirect
  Task                                 DeleteAsync(int id, CancellationToken ct)
  Task<IEnumerable<GroupMembershipDto>>GetMembersAsync(int groupId, CancellationToken ct)
  Task<IEnumerable<GroupMembershipDto>>GetPendingRequestsAsync(int groupId, CancellationToken ct)
  Task<IEnumerable<GroupMembershipDto>>GetPendingRequestsForAdminAsync(int adminUserId, CancellationToken ct)
    // Returns pending requests only for groups where the user is Admin
  Task                                 AddMemberAsync(AddGroupMemberDto dto, CancellationToken ct)
  Task                                 ApproveMembershipAsync(int membershipId, CancellationToken ct)
  Task                                 DeclineMembershipAsync(int membershipId, CancellationToken ct)
  Task                                 DeactivateMemberAsync(int membershipId, CancellationToken ct)
  Task<IEnumerable<GroupDto>>          GetGroupsForUserAsync(int userId, CancellationToken ct)

IUserRegistrationService
  Task<UserDto>                        RegisterAsync(RegisterRequestDto dto, CancellationToken ct)
    // Creates user with IsActive = true, Role = User; creates GroupMembership rows with Status = Pending
  Task                                 ConfirmEmailAsync(string token, CancellationToken ct)
  Task<IEnumerable<GroupMembershipDto>>GetPendingApprovalsAsync(CancellationToken ct)
    // SuperAdmin: all pending; GroupAdmin: only own groups
  Task<IEnumerable<GroupMembershipDto>>GetPendingApprovalsForGroupsAsync(IEnumerable<int> groupIds, CancellationToken ct)

IPasswordResetService
  Task                                 RequestResetAsync(string email, CancellationToken ct)
  Task                                 ResetPasswordAsync(string token, string newPassword, CancellationToken ct)

IMailConfigurationService
  Task<IEnumerable<MailConfigurationDto>>  GetAllAsync(CancellationToken ct)
  Task<MailConfigurationDto?>              GetByIdAsync(int id, CancellationToken ct)
  Task<MailConfigurationDto>               CreateAsync(CreateMailConfigurationDto dto, CancellationToken ct)
  Task                                     UpdateAsync(UpdateMailConfigurationDto dto, CancellationToken ct)
  Task                                     DeleteAsync(int id, CancellationToken ct)
  Task<IEnumerable<MailConfigurationDto>>  GetForGroupAsync(int groupId, CancellationToken ct)
    // Returns group-specific overrides (or system defaults if none)
  Task                                     AssignToGroupAsync(int groupId, int mailConfigId, int priority, CancellationToken ct)
  Task                                     RemoveFromGroupAsync(int groupMailConfigId, CancellationToken ct)

INotificationLogService
  Task<IEnumerable<NotificationLogDto>>    GetRecentAsync(int count, CancellationToken ct)
  Task<NotificationStatsDto>               GetStatsAsync(DateTime? cutoffDate, CancellationToken ct)
    // Returns: SuccessLast30Days, FailedLast30Days, TotalSuccess, TotalFailed (filtered by cutoff)
  Task                                     SetCounterCutoffAsync(DateTime cutoffDate, CancellationToken ct)

IEmailService
  Task                                 SendPasswordResetAsync(string toEmail, string resetLink, CancellationToken ct)
  Task                                 SendEmailConfirmationAsync(string toEmail, string confirmLink, CancellationToken ct)
  Task                                 SendGroupApprovalAsync(string toEmail, string groupName, string appLink, CancellationToken ct)
  Task                                 SendGroupRejectionAsync(string toEmail, string groupName, string appLink, CancellationToken ct)
  Task                                 SendWelcomeWithResetLinkAsync(string toEmail, string resetLink, CancellationToken ct)
  Task                                 SendTestEmailAsync(string toEmail, CancellationToken ct)
  // All methods: resolve mail config by priority (group-level first, then system-level).
  // On failure: increment FailureCount, set LastFailedOn on MailConfiguration, log to NotificationLog.
  // On success: log to NotificationLog.

ISlugRedirectService
  Task<string?>                        ResolveRedirectAsync(string oldSlug, string entityType, CancellationToken ct)
    // Returns the latest NewSlug for the given OldSlug and EntityType, or null if not found

ITagService
  Task<IEnumerable<TagDto>>            GetAllAsync(CancellationToken ct)
  Task<IEnumerable<TagDto>>            GetByGroupAsync(int? groupId, CancellationToken ct)
  Task<TagDto>                         CreateAsync(CreateTagDto dto, CancellationToken ct)
  Task                                 DeleteAsync(int id, CancellationToken ct)
```

---

## 9. Business Rules (Machine-Readable)

```yaml
rules:
  - id: BR-001
    entity: Registration
    trigger: RegisterAsync
    condition: "registeredCount >= training.Capacity"
    action: throw InvalidOperationException("Training is at full capacity.")

  - id: BR-002
    entity: Registration
    trigger: RegisterAsync
    condition: "existingRegistration.Status == Registered"
    action: throw InvalidOperationException("User is already registered for this training.")

  - id: BR-003
    entity: Training
    trigger: CreateAsync
    condition: "dto.DateTime <= DateTime.UtcNow"
    action: throw ArgumentException("Training date must be strictly in the future.")

  - id: BR-004
    entity: Training
    trigger: CreateAsync | UpdateAsync
    condition: "dto.Capacity < 1"
    action: throw ArgumentException("Capacity must be at least 1.")

  - id: BR-005
    entity: Training
    trigger: CreateAsync
    condition: "dto.GroupId is null or missing"
    action: throw ArgumentException("Every training must belong to a group.")

  - id: BR-006
    entity: Attendance
    trigger: RecordAttendanceAsync
    condition: "recorder is not group Admin/SuperAdmin AND recorder.Id != training.TrainerId"
    action: throw UnauthorizedAccessException("Only the assigned trainer, a group Admin, or SuperAdmin may record attendance.")

  - id: BR-007
    entity: User
    trigger: Self-Registration
    condition: always
    action: "User.IsActive = true, User.Role = User. GroupMembership rows created with Status = Pending."

  - id: BR-008
    entity: User
    trigger: ChangePasswordAsync
    condition: always
    action: password is hashed before persistence; plain-text is never stored

  - id: BR-009
    entity: GroupMembership
    trigger: DeclineMembershipAsync
    condition: always
    action: "Status = Declined, DeclinedAt = now. User is NOT deleted. User can re-apply later (new row)."

  - id: BR-010
    entity: GroupMembership
    trigger: ApproveMembershipAsync
    condition: always
    action: "Status = Approved, ApprovedAt = now. User gains access to group trainings."

  - id: BR-011
    entity: Group
    trigger: UpdateAsync (name change)
    condition: "Name changed"
    action: "Old slug saved to SlugRedirect. New slug generated from new name. Warning shown to Admin."

  - id: BR-012
    entity: Group
    trigger: CreateAsync
    condition: "Name or Identifier already exists"
    action: throw ValidationException("Name/Identifier must be unique.")

  - id: BR-013
    entity: MailConfiguration
    trigger: SendAsync (any email)
    condition: "SMTP send fails"
    action: "Increment FailureCount, set LastFailedOn. Write NotificationLog with IsSuccess = false. Try next priority config. Do NOT send failure notification email."

  - id: BR-014
    entity: MailConfiguration
    trigger: SendAsync (any email)
    condition: "SMTP send succeeds"
    action: "Write NotificationLog with IsSuccess = true."

  - id: BR-015
    entity: Training
    trigger: CreateAsync
    condition: "Trainer does not have GroupMemberRole.Trainer in dto.GroupId"
    action: throw UnauthorizedAccessException("Trainer can only create trainings for their own groups.")

  - id: BR-016
    entity: Registration
    trigger: RegisterAsync
    condition: "User does not have GroupMembership.Status == Approved in training.GroupId"
    action: throw UnauthorizedAccessException("User can only register for trainings in their accepted groups.")

  - id: BR-017
    entity: Registration
    trigger: RegisterAsync | CancelAsync
    condition: "training.Status == InProgress OR training.Status == Done"
    action: throw InvalidOperationException("Registration is closed for this training.")

  - id: BR-018
    entity: Training
    trigger: TakeAsync
    condition: "training.Status != New OR training.TrainerId != null"
    action: throw InvalidOperationException("Only an unassigned New training can be taken.")

  - id: BR-019
    entity: Training
    trigger: ReleaseTrainerAsync
    condition: "training.Status == InProgress OR training.Status == Done OR requestingUserId != training.TrainerId"
    action: throw InvalidOperationException/UnauthorizedAccessException("Only the assigned trainer may release, and only before the training starts.")

  - id: BR-020
    entity: Training
    trigger: ReassignTrainerAsync
    condition: "training.Status == InProgress OR training.Status == Done"
    action: throw InvalidOperationException("Trainer cannot be reassigned once the training is in progress or done.")

  - id: BR-021
    entity: Training
    trigger: ConfirmPlannedAsync
    condition: "training.Status != InPlanning OR training.TrainerId == null"
    action: throw InvalidOperationException("A trainer must be assigned and planning must be in progress to confirm.")

  - id: BR-022
    entity: Training
    trigger: StartAsync
    condition: "training.Status != Planned OR training.TrainerId != requestingTrainerId"
    action: throw InvalidOperationException/UnauthorizedAccessException("Only the assigned trainer may start a Planned training.")

  - id: BR-023
    entity: Attendance
    trigger: LockAttendanceAsync
    condition: "training.Status != InProgress"
    action: throw InvalidOperationException("Attendance can only be finalized while the training is in progress.")
```

---

## 10. Non-Functional Requirements

| ID     | Category        | Requirement                                                                 |
|--------|-----------------|-----------------------------------------------------------------------------|
| NFR-01 | Security        | Passwords must be stored using a one-way hash (bcrypt or equivalent).       |
| NFR-02 | Security        | All routes except `/Login`, `/Register`, and password-reset pages require authenticated session. |
| NFR-03 | Security        | Use policy-based authorization. No role-string-based `[Authorize(Roles = "...")]` attributes. |
| NFR-04 | Security        | SMTP passwords stored encrypted at rest in `MailConfiguration`.             |
| NFR-05 | Availability    | Application must handle concurrent registrations without double-booking.    |
| NFR-06 | Maintainability | Follow Clean Architecture; no cross-layer dependency violations.            |
| NFR-07 | Testability     | Application and Domain layers must be testable without Infrastructure.      |
| NFR-08 | Observability   | All email send attempts (success and failure) must be logged to `NotificationLog`. |
| NFR-09 | UX              | New self-registered users can log in immediately and see their group request statuses. |
| NFR-10 | Configuration   | Default SuperAdmin credentials are defined in `appsettings.json` (`Seed` section) as the single source of truth. |

---

## 11. Glossary

| Term                  | Definition                                                                   |
|-----------------------|------------------------------------------------------------------------------|
| Training              | A scheduled session with a title, location, date/time, capacity, and trainer, belonging to exactly one group |
| Trainer               | A per-group role: a user responsible for conducting training sessions in that group |
| Participant           | A per-group role: a user who can register for and attend training sessions in that group |
| Admin (Group Admin)   | A per-group role: manages users, approvals, and settings for their group(s)  |
| SuperAdmin            | System-level role on the `User` entity: full access to all groups and system config |
| Registration          | The act of a participant reserving a spot in a training                       |
| Attendance            | A record of whether a registered participant was present or absent            |
| Capacity              | The maximum number of participants allowed in a training session              |
| Group                 | An organizational unit containing members, trainings, tags, and mail config   |
| GroupMembership       | A user's relationship with a group: includes role, status (Pending/Approved/Declined), and date history |
| MailConfiguration     | A system-level SMTP configuration with priority, managed by SuperAdmin        |
| GroupMailConfiguration| A per-group override assigning specific mail configurations with group-specific priority |
| NotificationLog       | An immutable audit record of every email send attempt (success or failure)    |
| SlugRedirect          | A historical record mapping old URL slugs to new ones after entity renames    |
| TrainingBlock         | A time-boxed segment of a training session (title, duration, tags)            |
| Tag                   | A label that can be applied to training blocks to categorize content          |
| Slug                  | A URL-friendly identifier auto-generated from an entity name (e.g., group name) |
