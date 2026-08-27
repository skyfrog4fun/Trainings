# Razor Component Naming Conventions

> For structural/behavioral conventions (page layout, CRUD form pattern, validation,
> delete confirmation, etc.), see `docs/developer/razor-page-style-guide.md`.

This document defines the naming convention for all `*.razor` files in this repository. It applies to `src/Trainings.Web` (Blazor Web App) and to any future project that adds Razor components. All new pages/components MUST follow these rules, and any renamed/refactored files should be brought into compliance as part of that change.

## 1. Pages (routable components)

Any component that contains a `@page` directive MUST have its file name end with the suffix `Page`.

This applies uniformly, regardless of whether the page is a simple static view or a complex CRUD/form page.

**Examples**
- `GroupCreateEditPage.razor` ✅
- `GroupsPage.razor` ✅
- `NotFoundPage.razor` ✅ (was `NotFound.razor`)
- `HomePage.razor` ✅ (was `Home.razor`)

## 2. Layouts

The suffix `Layout` is reserved for components that derive from `LayoutComponentBase` (`@inherits LayoutComponentBase`) and are used via `@layout` or as a `DefaultLayout`.

Files that merely live inside a `Layout/` folder but are **not** layout classes (e.g., a reconnect/overlay dialog) do **not** get the `Layout` suffix. They follow the Shared UI building block rule (§3) instead.

**Examples**
- `MainLayout.razor` ✅
- `AnonymousLayout.razor` ✅
- `Layout/ReconnectModal.razor` ✅ — not a layout class, correctly named with the `Modal` role suffix instead.

## 3. Shared / reusable UI building blocks

Reusable, non-routable components MUST be named with a suffix that reflects their UI role:

| Suffix | Use for |
|---|---|
| `Button` | clickable button-like elements (e.g., `BackButton`, `DashboardQuickAccessButton`) |
| `Alert` | inline message/alert boxes (e.g., `ActionAlert`) |
| `Spinner` | loading indicators (e.g., `LoadingSpinner`) |
| `Modal` | dialog/modal overlays (e.g., `ReconnectModal`, `EmailPreviewModal`) |
| `Card` | small, single-purpose stat/summary cards (e.g., `DashboardStatCard`) |
| `Panel` | larger collapsible/dashboard sections that aggregate multiple items (e.g., `DashboardNotificationPanel`, `DashboardPlanningPanel`, `DashboardSystemPanel`) |
| `Header` | page/section header blocks (e.g., `PageHeader`) |
| `List` | components rendering a collection of items (e.g., `TrainingList`) |
| `Editor` | components providing inline editing UI (e.g., `TrainingBlockEditor`) |

When none of the existing suffixes fit, choose the closest matching UI-role noun (e.g., `Badge`, `Table`, `Dropdown`, `Tooltip`) and add it to this table.

### Logic-only components (no visual role)

A small number of components exist purely for behavior (no distinctive markup role of their own), such as `RedirectToLogin.razor`. These are named for their purpose/behavior instead of a UI-role suffix and are documented here as an explicit exception. Do not force a role suffix onto components like this — it would misrepresent what they do.

## 4. Small presentational elements

Buttons, badges, icons, headers, and similar small presentational elements always carry their role suffix per §3 — there are no exceptions for "small" components.

## 5. Documented exceptions (no suffix required)

The following files are ASP.NET Core Blazor framework/template conventions and are exempt from the rules above:

- `App.razor` — Blazor root host document.
- `Routes.razor` — Blazor router root.
- `_Imports.razor` — Razor implicit-usings file (name is fixed by tooling).
- `ResourcePreloader` — built-in ASP.NET Core Blazor framework component (ships in `Microsoft.AspNetCore.Components.Web`); not a file in this repository, referenced only via markup tag.

Any future file mandated by an ASP.NET Core Blazor project template or first-party tooling convention should be added to this list.

## Compliance examples

| Current/legacy name | Compliant name | Rule |
|---|---|---|
| `NotFound.razor` | `NotFoundPage.razor` | §1 Pages |
| `Home.razor` | `HomePage.razor` | §1 Pages |
| `Error.razor` | `ErrorPage.razor` | §1 Pages |
| `ConfirmEmail.razor` | `ConfirmEmailPage.razor` | §1 Pages |
| `ForgotPassword.razor` | `ForgotPasswordPage.razor` | §1 Pages |
| `Register.razor` | `RegisterPage.razor` | §1 Pages |
| `ResetPassword.razor` | `ResetPasswordPage.razor` | §1 Pages |
| `DashboardNotificationActivity.razor` | `DashboardNotificationPanel.razor` | §3 Panel |
| `DashboardPlanningOverview.razor` | `DashboardPlanningPanel.razor` | §3 Panel |
| `DashboardSystemOverview.razor` | `DashboardSystemPanel.razor` | §3 Panel |

## Going forward

All renames listed in the compliance table above have been applied to the codebase (including their `@page`/`typeof(...)`/component-tag references), and the full solution has been verified to build, pass all tests, and pass formatting/package checks after the rename. This convention is now the enforced baseline — every new page or component added to the project must follow these rules from the start:

- New routable components: name them `<Feature><Purpose>Page.razor`.
- New layout classes: name them `<Purpose>Layout.razor` and ensure `@inherits LayoutComponentBase`.
- New shared components: pick the closest UI-role suffix from §3, extending the table when a genuinely new role is introduced.
- Logic-only components without a visual role: name for behavior/purpose and note them as an exception here if ambiguity could arise.
- The exceptions list in §5 is frozen — do not add to it without updating this document first, and only for files mandated by ASP.NET Core Blazor project templates or first-party tooling conventions.
