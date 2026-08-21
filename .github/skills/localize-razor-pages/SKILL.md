---
name: localize-razor-pages
description: "Use when asked to check, verify, or add localization/multilanguage support for a named Razor page/component file under Components/Pages or Components/Shared."
argument-hint: "target Razor file path"
user-invocable: true
---

# SKILL: Razor Localization Check

## Purpose

Validate and improve localization quality for one named `.razor` file with a strict, approval-first workflow.
Use this skill whenever the user asks to check, verify, or add localization/multilanguage support for a page, component, or Razor file.

## Allowed Target Scope

The named file must satisfy all rules below:

1. File extension is exactly `.razor`.
2. File is under one of these paths:
  - `src/Trainings.Web/Components/Pages/**`
  - `src/Trainings.Web/Components/Shared/**`
3. Exclude framework shell and similar root component files:
  - `src/Trainings.Web/Components/App.razor`
  - `src/Trainings.Web/Components/Routes.razor`
  - `src/Trainings.Web/Components/RedirectToLogin.razor`
  - `src/Trainings.Web/Components/_Imports.razor`
  - any other file directly under `src/Trainings.Web/Components/*.razor`

If the target is out of scope, warn and ask whether to stop or continue.

## Preflight Validation

Before page analysis, verify this exists in `src/Trainings.Web/Components/_Imports.razor`:

```razor
@inject IStringLocalizer<SharedResources> Localizer
```

If missing, add it first.

## Allowed Localizer Key Patterns

Only these key patterns are allowed:

1. `[Page]_[Resource]`
2. `Enum_[List]_[Value]`
3. `Shared_[Resource]`

Rules:

1. `[Page]` is the exact Razor filename without `.razor`.
2. For components in `Components/Shared`, still use the exact filename as `[Page]` for page/component-scoped keys.
3. Key fragments should be PascalCase.

## Localization Check Workflow

Run checks in this order.

### 1. Scan existing `Localizer["..."]` keys in target file

1. List every key used in the target file.
2. Flag keys that do not match one of the three allowed patterns.
3. Propose corrected key names.

### 2. Verify key presence in all language RESX files

1. Discover all `SharedResources.*.resx` files under `src/Trainings.Web/Resources/`.
2. For each in-file key that matches an allowed pattern, verify it exists in every language RESX file.
3. If missing anywhere:
  - prefer reuse of an existing equivalent key/value first,
  - otherwise propose creating the key.

### 3. Scan for hardcoded plain text in markup and attributes

Scan for user-visible literals in:

1. element text nodes,
2. `<PageTitle>` content,
3. attributes: `title`, `alt`, `placeholder`, `aria-label`.

For each found literal:

1. try to match an existing suitable key first,
2. if no suitable key exists, propose a new `[Page]_[Resource]` key,
3. ensure proposed key name does not already exist with different meaning.

Skip:

1. text already localized via `Localizer[...]`,
2. inline C# literals in `@code` blocks,
3. non-UI values like URLs, CSS classes, pure numbers, icon glyphs.

### 4. RESX/page consistency check for page-scoped keys

1. Read all keys in RESX matching `[Page]_*` for the current target page prefix.
2. Verify each appears in the target Razor file.
3. List unused keys as cleanup candidates.
4. Ask for explicit per-key confirmation before removing any key.
5. Apply approved removals across all language RESX files in one synchronized update.

## Proposal-First Behavior

Do not mutate RESX files automatically.

For each missing or new key, ask the developer with:

1. source context (file, element/attribute, current literal),
2. suggested key name,
3. suggested English value,
4. suggested translations for every detected language RESX file.

If key exists in Razor but missing in RESX, ask the developer to confirm the English term and provide proposed translations.

## RESX Update Rules (After Approval)

When approved to edit:

1. update all language `SharedResources.*.resx` files together,
2. keep keys alphabetically ordered within prefix groups,
3. avoid duplicates,
4. preserve XML structure and encoding.

## Output Contract

Return results in this structure:

1. Target scope validation result.
2. `_Imports.razor` Localizer injection status and action taken.
3. Invalid key-pattern findings with proposed corrections.
4. Missing RESX key findings grouped by language.
5. Hardcoded text findings with reuse-or-new-key recommendations.
6. Unused `[Page]_*` RESX keys with per-key deletion checklist.
7. Explicit questions requiring developer confirmation.

## Examples of Good Key Shapes

```text
HomePage_WelcomeBack
UsersPage_ApproveButton
DashboardSystemPanel_SystemOverview
Enum_Gender_Female
Shared_Save
```

## Notes

1. Prefer minimal, meaning-based key names over duplicative near-synonyms.
2. Prefer reusing established `Shared_` or `Enum_` keys where semantics are identical.
3. If unsure whether two strings are semantically equal, ask before introducing a new key.
