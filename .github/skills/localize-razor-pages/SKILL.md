# SKILL: Localize Razor Pages

## Purpose

Audit Blazor razor pages for hardcoded strings, add `Page_Action`-scoped resource keys to both
`SharedResources.en.resx` and `SharedResources.de.resx`, and wire up `@Localizer[...]` calls in the
razor files. Use this skill whenever you are asked to localize pages or remove hardcoded UI text.

---

## Key Naming Convention

- Format: `Page_Action` — always page-scoped, even when a shared key with the same value already exists.
- Page prefix = the razor file name without extension (e.g. `LoginPage`, `ForgotPassword`, `Register`, `AnonymousLayout`).
- Action suffix = a short PascalCase description of the element role (e.g. `Title`, `SuccessMessage`, `EmailLabel`, `EmailPlaceholder`, `BackToLogin`, `ButtonLogin`).

Examples:
```
Login_LogoAlt
Login_InitialSetupTitle
ForgotPassword_EmailPlaceholder
Register_Gender
ResetPassword_ConfirmPasswordLabel
AnonymousLayout_OpenMenuAriaLabel
```

---

## What to Extract

Extract **all** hardcoded strings in razor files:

| Source                          | Example                                      |
|---------------------------------|----------------------------------------------|
| Element text content            | `<h4>Forgot Password</h4>`                   |
| `<PageTitle>` text              | `<PageTitle>Reset Password</PageTitle>`      |
| `aria-label="..."` attributes   | `aria-label="Open menu"`                     |
| `alt="..."` attributes          | `alt="Logo"`                                 |
| `placeholder="..."` attributes  | `placeholder="you@example.com"`              |
| `title="..."` attributes        | `title="Mark as Planned"`                    |
| Text inside `@if` / `@foreach`  | dev-only banners, conditional alerts         |
| `<strong>`, `<span>` inline     | `<strong>Initial Setup</strong>`             |

---

## What to Skip

- **StylePage.razor** — internal dev style guide with intentional demo content.
- **Error.razor** — ASP.NET scaffold error page; not part of the main user flow.
- Inline C# string literals inside `@code { }` blocks (e.g. validation messages set in code — those belong in a separate localization pass).
- Content that is already using `@Localizer[...]`.

---

## Localizer Usage Patterns

### Markup text
```razor
<h4 class="card-title">@Localizer["ForgotPassword_Title"]</h4>
<p>@Localizer["ConfirmEmail_ReviewPending"]</p>
```

### HTML attributes
```razor
<input placeholder="@Localizer["Register_EmailPlaceholder"]" />
<button aria-label="@Localizer["AnonymousLayout_OpenMenuAriaLabel"]">
<img alt="@Localizer["Login_LogoAlt"]" />
```

### Inside `@if` / mixed content
```razor
<strong>@Localizer["Login_InitialSetupTitle"]</strong><br />
@Localizer["Login_InitialSetupDescription"]<br />
<strong>@Localizer["Login_InitialSetupEmailLabel"]</strong> @SeedEmail
```

---

## Resource File Pattern

Add new entries to **both** `src/Trainings.Web/Resources/SharedResources.en.resx` and
`src/Trainings.Web/Resources/SharedResources.de.resx` before the closing `</root>` tag.

```xml
<data name="Page_Action" xml:space="preserve">
  <value>English text here</value>
</data>
```

- EN values: exact original text from the razor file.
- DE values: casual, short German translation. Use `du` (informal). Keep it concise.

---

## Step-by-Step Process

1. **Audit** — read the target razor file(s) and list every hardcoded string with its element type.
2. **Plan keys** — derive `Page_Action` names for each string.
3. **Add to .resx** — insert all new keys into both EN and DE files. Group by page prefix for readability.
4. **Update razor** — replace each hardcoded string with `@Localizer["Key"]` (or attribute form).
5. **Verify** — run `dotnet build` (0 errors), check for duplicate keys in both `.resx` files, run `dotnet format`.

### Duplicate key check (PowerShell)
```powershell
$path = "src/Trainings.Web/Resources/SharedResources.en.resx"
Select-String -Path $path -Pattern '<data name="([^"]+)"' |
  ForEach-Object { $_.Matches[0].Groups[1].Value } |
  Group-Object | Where-Object { $_.Count -gt 1 }
```
Repeat for the DE file. No output = no duplicates.

---

## Reference Examples

The following files are complete, correct examples of this pattern:

- `src/Trainings.Web/Components/Layout/AnonymousLayout.razor` — aria-labels and logo alt
- `src/Trainings.Web/Components/Pages/LoginPage.razor` — conditional dev-only block
- `src/Trainings.Web/Components/Pages/Register.razor` — labels, placeholders, select options
- `src/Trainings.Web/Components/Pages/ResetPassword.razor` — full end-to-end example

---

## Remaining Pages (Known Hardcoded Text)

These pages still contain hardcoded strings and should be localized in future sprints using this skill:

| Page                          | Notes                                              |
|-------------------------------|----------------------------------------------------|
| `AttendancePage.razor`        | Status labels, button text, alert messages         |
| `CreateEditTrainingPage.razor`| Form labels, placeholders, button text             |
| `GroupMembersPage.razor`      | Table headers, role options, action buttons        |
| `IconOverviewPage.razor`      | Page title, table headers                          |
| `LocationsPage.razor`         | Form labels, section titles, table headers         |
| `MailConfigPage.razor`        | Large number of labels, descriptions, badge text   |
| `MyRegistrationsPage.razor`   | Filter options, empty state messages               |
| `NotFound.razor`              | Two short strings                                  |
