---
name: Razor Localization Routing
description: "Use when user asks to check, verify, or add localization/multilanguage support for a Razor page/component; invoke the localize-razor-pages skill workflow."
applyTo:
  - "src/Trainings.Web/Components/Pages/**/*.razor"
  - "src/Trainings.Web/Components/Shared/**/*.razor"
---

When a request is about localization or multilanguage support for a Razor page/component/file,
route the work through the `localize-razor-pages` skill.

Trigger terms include:

- check localization
- verify localization
- add localization
- multilanguage support
- i18n or l10n in Razor UI

Operational guardrails:

1. Only proceed automatically when the named target is under `Components/Pages` or `Components/Shared`.
2. Do not use this workflow for root component shell files like `App.razor`, `Routes.razor`, `RedirectToLogin.razor`, `_Imports.razor`, or other `Components/*.razor` files.
3. If target is out of scope, warn and ask the user whether to continue or stop.
4. Follow proposal-first behavior for RESX additions/removals; request user confirmation before edits.
