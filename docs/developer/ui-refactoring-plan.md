# UI Refactoring Plan

> Forward-looking structural/behavioral conventions for pages are now documented in
> `docs/developer/razor-page-style-guide.md`. Use that guide (not this historical log)
> when refactoring pages going forward.

## Decisions Made

- All CSS consolidated into a single `app.css`; `MainLayout.razor.css` and `ReconnectModal.razor.css` deleted
- No inline `style="..."` attributes — replaced with CSS utility classes in `app.css`
- Button classes: 3 overlapping variants unified into the standard `btn-primary`
- Inline forms (Users, Groups) → dedicated create/edit pages
- UsersPage "View" (eye) button → new `/users/{id}` read-only page
- Groups "View" (eye) button already points to `/groups/{slug}/members` — no change needed
- `MailConfigPage` — left as-is (out of scope)
- `NavMenu.razor` was dead code (zero references) — deleted

---

## Phase 1 — Quick Wins ✅ Done

### 1a. Remove all inline `style=` attributes ✅
New utility classes added to `app.css` and applied:

| Class | Rule | Used in |
|---|---|---|
| `.icon-display` | `font-size: 3rem` | `ConfirmEmail.razor` ×2 |
| `.scroll-container-md` | `max-height: 340px; overflow-y: auto` | `CreateEditTrainingPage.razor` |
| `.scroll-container-sm` | `max-height: 220px; overflow-y: auto` | `UsersPage.razor` |
| `.progress-thin` | `height: 6px` | `TrainerRunPage.razor` |
| `.modal-overlay-brand` | `background-color: rgba(var(--brand-tradition-rgb), 0.5)` | `EmailPreviewModal.razor` |

### 1b. Unify button classes ✅
- Removed `.dashboard-btn-primary` from `app.css`
- Removed `.btn-app-primary` from `app.css`
- Updated `Home.razor`, `UsersPage.razor`, `GroupsPage.razor` → all use `btn-primary`
- `.btn-app-outline`, `.btn-app-outline-*`, `.btn-app-icon` kept (used for card action icon buttons)

### 1c. Remove dead CSS ✅
- Removed `.page`, `.sidebar`, `.top-row`, `.nav-menu`, `.content` and their media queries from `app.css` (~40 lines of unused default Blazor template CSS)
- `main { overflow-x: hidden; }` retained

### 1d. Fix StatisticsPage ✅
- Added `IStringLocalizer<SharedResources>` to `StatisticsPage.razor`
- Replaced 5 custom inline stat cards with the existing `<DashboardStatCard>` component
- All hardcoded English strings replaced with Localizer keys
- 9 new keys added to `SharedResources.en.resx` and `SharedResources.de.resx`:
  `StatisticsActiveUsers`, `StatisticsUsersNotInGroup`, `StatisticsTotalRegistrations`,
  `StatisticsAnnualIndicators`, `StatisticsAvgUsersPerGroup`, `StatisticsAvgParticipantsPerTraining`,
  `StatisticsScopeGlobal`, `StatisticsScopeManagedGroups`

### 1e. Delete NavMenu.razor ✅
- `src/Trainings.Web/Components/Layout/NavMenu.razor` deleted (zero references in codebase)

### 1f. Consolidate all CSS into app.css ✅
- Content of `MainLayout.razor.css` appended to `app.css` under `/* === Navbar & Layout === */`
  - All 5 `::deep` pseudo-selectors removed (not needed in global CSS)
- Content of `ReconnectModal.razor.css` appended under `/* === Reconnect Modal === */`
- Both `.razor.css` files deleted

---

## Phase 2 — Shared Reusable Components ✅ Done

### 2a. `LoadingSpinner.razor` ✅
- **New file:** `src/Trainings.Web/Components/Shared/LoadingSpinner.razor`
- Parameters: `string? Label`, `bool Inline`, `bool Small`, `string ContainerClass`, `string? SpinnerClass`
- Renders: `<div class="text-center my-4"><div class="spinner-border" role="status">…</div></div>`
- Replace the identical spinner pattern in **all** pages
- Inline/button spinners also standardized via `Inline="true" Small="true"`

### 2b. `ActionAlert.razor` ✅
- **New file:** `src/Trainings.Web/Components/Shared/ActionAlert.razor`
- Parameters: `bool? IsSuccess`, `string? Message`
- Renders `alert-success` or `alert-danger`; hidden when `Message` is null
- Replace in: `UsersPage`, `GroupsPage`, `Home`, `CreateEditTrainingPage`, `TrainerRunPage`

### 2c. `PageHeader.razor` ✅
- **New file:** `src/Trainings.Web/Components/Shared/PageHeader.razor`
- Parameters: `string Title`, `RenderFragment? Actions` (child slot)
- Replaces: `<div class="d-flex justify-content-between align-items-center my-3"><h2>…</h2> …</div>`
- Replace in: `UsersPage`, `GroupsPage`, `StatisticsPage`, `TrainerRunPage` header sections

### 2d. `BackButton.razor` ✅
- **New file:** `src/Trainings.Web/Components/Shared/BackButton.razor`
- Parameters: `string Href`, `string? Label`
- Renders: `<a href="…" class="btn btn-sm btn-outline-secondary"><i …></i> Label</a>`
- Replace in: `CreateEditTrainingPage`, `TrainerRunPage`, `AttendancePage`

---

## Phase 3 — Fix View vs Edit UX ✅ Done

### 3a. Create `UserDetailPage.razor` ✅
- **New file:** `src/Trainings.Web/Components/Pages/UserDetailPage.razor`
- Route: `@page "/users/{Id:int}"`
- Read-only view: profile, group memberships, registration history
- Actions: Edit button → `/users/{id}/edit`, Back → `/users`
- Re-uses existing service calls already present in `UsersPage`

### 3b. Wire eye button in `UsersPage` → `/users/{id}` ✅
- Change the "View" eye button from `@onclick="() => EditUser(user)"` to `<a href="/users/@user.Id">`

---

## Phase 4 — Extract Inline Forms to Dedicated Pages ✅ Done

### 4a. Create `UserCreateEditPage.razor` ✅
- **New file:** `src/Trainings.Web/Components/Pages/UserCreateEditPage.razor`
- Routes: `@page "/users/new"` and `@page "/users/{Id:int}/edit"`
- Moves the full user form (~200 lines of markup) out of `UsersPage`
- On save: redirect to `/users/{id}` (view) or `/users` (list)
- Back button → `/users`

### 4b. Simplify `UsersPage.razor` ✅
- Remove: `_showForm`, all form fields, `ShowCreateForm()`, `SaveUser()`, `CancelForm()`, etc.
- "Add" header button → `<a href="/users/new">`
- Edit pen button → `<a href="/users/@user.Id/edit">`
- Eye button → `<a href="/users/@user.Id">`
- Keep: list rendering, pending approvals table, `LoadUsers`, `SendAccountMail`, `ToggleActive`, `DeleteUserAsync`, `ApproveUser`, `RejectUser`

### 4c. Create `GroupCreateEditPage.razor` ✅
- **New file:** `src/Trainings.Web/Components/Pages/GroupCreateEditPage.razor`
- Routes: `@page "/groups/new"` and `@page "/groups/{Id:int}/edit"`
- Moves the full group form out of `GroupsPage`
- Back button → `/groups`

### 4d. Simplify `GroupsPage.razor` ✅
- Remove: `_showForm`, all form fields, related state and methods
- "Add" header button → `<a href="/groups/new">`
- Edit pen button → `<a href="/groups/@group.Id/edit">`
- Eye button unchanged → `/groups/@group.Slug/members` ✓

---

## Phase 5 — Extract Training Block Editor ✅ Done

### 5a. Create `TrainingBlockEditor.razor` ✅
- **New file:** `src/Trainings.Web/Components/Shared/TrainingBlockEditor.razor`
- Parameters: `int TrainingId`, `bool IsWriteBlocked`
- Self-contained: injects `ITrainingService` and `ITagService` directly
- Contains: block list, inline block editor, block library panel (~450 lines of markup currently in `CreateEditTrainingPage`)

### 5b. Simplify `CreateEditTrainingPage.razor` ✅
- Replace block section with `<TrainingBlockEditor TrainingId="@_persistedTrainingId.Value" IsWriteBlocked="@IsWriteBlocked" />`
- Expected reduction: 861 → ~350 lines

---

## Phase 6 — Dashboard Decomposition ✅ Done

### 6a. Extract `DashboardSystemOverview.razor` ✅
- **New file:** `src/Trainings.Web/Components/Shared/DashboardSystemOverview.razor`
- Parameters: counts for mail configs, locations, groups, users, trainings
- Add CSS class `.system-row` to replace the 5× repeated `d-flex justify-content-between align-items-center py-2 border-bottom` pattern in `Home.razor`

### 6b. Extract `DashboardPlanningOverview.razor` ✅
- **New file:** `src/Trainings.Web/Components/Shared/DashboardPlanningOverview.razor`
- Parameters: `Dictionary<string, List<TrainingDto>>`, expanded toggle, reset callback
- Removes the collapsible planning table from `Home.razor`

### 6c. Extract `DashboardNotificationActivity.razor` ✅
- **New file:** `src/Trainings.Web/Components/Shared/DashboardNotificationActivity.razor`
- Parameters: log list, success/failure counts, reset callback
- Removes the collapsible notification log from `Home.razor`

---

## File Inventory

### Files Modified in Phase 1
| File | Change |
|---|---|
| `wwwroot/app.css` | Dead CSS removed, button classes unified, utility classes added, navbar + reconnect modal CSS appended |
| `Components/Pages/Home.razor` | `dashboard-btn-primary` → `btn-primary` |
| `Components/Pages/UsersPage.razor` | `btn-app-primary` → `btn-primary`, inline style removed |
| `Components/Pages/GroupsPage.razor` | `btn-app-primary` → `btn-primary` |
| `Components/Pages/StatisticsPage.razor` | Full rewrite: localizer added, DashboardStatCard used |
| `Components/Pages/ConfirmEmail.razor` | Inline style → `.icon-display` |
| `Components/Pages/CreateEditTrainingPage.razor` | Inline style → `.scroll-container-md` |
| `Components/Pages/TrainerRunPage.razor` | Inline style → `.progress-thin` |
| `Components/Shared/EmailPreviewModal.razor` | Inline style → `.modal-overlay-brand` |
| `Resources/SharedResources.en.resx` | 9 new Statistics keys |
| `Resources/SharedResources.de.resx` | 9 new Statistics keys |

### Files Deleted in Phase 1
| File | Reason |
|---|---|
| `Components/Layout/NavMenu.razor` | Dead code — zero references |
| `Components/Layout/MainLayout.razor.css` | Merged into `app.css` |
| `Components/Layout/ReconnectModal.razor.css` | Merged into `app.css` |

### New Files (Phases 2–6)
| File | Phase |
|---|---|
| `Components/Shared/LoadingSpinner.razor` | 2a |
| `Components/Shared/ActionAlert.razor` | 2b |
| `Components/Shared/PageHeader.razor` | 2c |
| `Components/Shared/BackButton.razor` | 2d |
| `Components/Pages/UserDetailPage.razor` | 3a |
| `Components/Pages/UserCreateEditPage.razor` | 4a |
| `Components/Pages/GroupCreateEditPage.razor` | 4c |
| `Components/Shared/TrainingBlockEditor.razor` | 5a |
| `Components/Shared/DashboardSystemOverview.razor` | 6a |
| `Components/Shared/DashboardPlanningOverview.razor` | 6b |
| `Components/Shared/DashboardNotificationActivity.razor` | 6c |
