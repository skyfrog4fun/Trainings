# Razor Page Style Guide

This document defines the structural and behavioral pattern that CRUD-style pages in
`src/Trainings.Web/Components/Pages` should follow. It is derived from
`Components/Pages/Locations/LocationsPage.razor`, which is the current reference
implementation for a "list + inline create/edit form + delete confirmation" page.

Use this guide when refactoring existing pages or creating new ones so that behavior,
markup structure, and code organization stay consistent across the app.

See also:
- `docs/razor-naming-conventions.md` — file/component naming rules.
- `docs/developer/ui-refactoring-plan.md` — historical log of prior UI refactoring phases.

## 1. Page skeleton

Directives and injects appear at the top of the file, in this fixed order:

```razor
@page "/config/locations"
@implements IDisposable

@using System.ComponentModel.DataAnnotations
@using System.Globalization

@rendermode InteractiveServer

@attribute [Authorize(Policy = "SuperAdmin")]

@inject ILocationService LocationService
@inject IGroupService GroupService
@inject ...
```

- `@page` first, `@implements IDisposable` immediately after (omit only if the page truly
  has no disposable state, e.g. no `CancellationTokenSource`).
- `@using` statements next, only for types not already covered by `_Imports.razor`.
- `@rendermode InteractiveServer` explicit on every interactive page.
- `@attribute [Authorize(...)]` for access control, placed before injects.
- `@inject` services last, one per line, application services before generic/infrastructure
  services (e.g. `ScrollService`, `NavigationManager`).

## 2. Page header

Every page starts its markup with:

```razor
<PageTitle>@Localizer["PageName_PageTitle"]</PageTitle>

<ToastComponent @ref="_toast" CloseLabel="@Localizer["Shared_Close"]" />

<div class="container px-2 px-md-4">
    <PageTitleComponent Icon="@AppIcons.SomeIcon"
                         Text="@Localizer["PageName_Heading"]"
                         ButtonText="@Localizer["Shared_New"]"
                         ButtonIcon="@AppIcons.Plus"
                         OnButtonClick="ShowCreateForm" />
    ...
</div>
```

Omit `ButtonText`/`ButtonIcon`/`OnButtonClick` on `PageTitleComponent` for read-only or
non-creatable pages.

## 3. Inline create/edit form pattern

Prefer the **inline form toggle** pattern for simple entities (as opposed to a dedicated
create/edit route) when the form is short and always used in the context of the list page:

```razor
@if (_showForm)
{
    <ScrollAnchor Id="entity-edit-form" />
    <CardComponent IsCollapsable="false">
        <Title>@(_editId == 0 ? Localizer["PageName_CreateTitle"] : Localizer["PageName_EditTitle"])</Title>
        <Body>
            <EditForm Model="_formModel" OnValidSubmit="Save">
                <DataAnnotationsValidator />
                <!-- fields, each with a <ValidationMessage> -->
                <div class="mt-4 d-flex justify-content-between">
                    <div>
                        <button type="submit" class="btn btn-primary me-2"><i class="@AppIcons.FloppyDisk me-1" aria-hidden="true"></i>@Localizer["PageName_SaveButton"]</button>
                        <button type="button" class="btn btn-secondary" @onclick="Cancel"><i class="@AppIcons.XMark me-1" aria-hidden="true"></i>@Localizer["PageName_CancelButton"]</button>
                    </div>
                    @if (_editId != 0)
                    {
                        <button type="button" class="btn btn-outline-danger" @onclick="() => Delete(_editId)"><i class="@AppIcons.Trash me-1" aria-hidden="true"></i>@Localizer["Shared_Delete"]</button>
                    }
                </div>
            </EditForm>
        </Body>
    </CardComponent>
}
```

For larger/multi-section forms (e.g. training block editing) or forms shared across
multiple entry points, extract a dedicated `*CreateEditPage.razor` route instead — do not
force a large form inline. See `ui-refactoring-plan.md` Phase 4 for that pattern.

### Form model

Define a private nested form model class inside `@code`, not the DTO directly, so
UI-only validation stays out of the domain/application layer:

```csharp
private class EntityFormModel : IValidatableObject
{
    public string Name { get; set; } = string.Empty;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var localizer = validationContext.GetService(typeof(IStringLocalizer<SharedResources>)) as IStringLocalizer<SharedResources>;

        if (string.IsNullOrWhiteSpace(Name))
        {
            yield return new ValidationResult(GetMessage(localizer, "PageName_NameRequired"), new[] { nameof(Name) });
        }
    }

    private static string GetMessage(IStringLocalizer<SharedResources>? localizer, string key) =>
        localizer is null ? key : localizer[key].Value;
}
```

Use this pattern whenever validation needs cross-field logic or localized messages that
`[Required]`/`[StringLength]` attributes alone cannot express.

## 4. Delete confirmation pattern

Never delete directly from a button click. Always go through `ConfirmDialogComponent`:

```razor
<ConfirmDialogComponent IsVisible="_showDeleteConfirm" Title="@Localizer["PageName_DeleteConfirmTitle"]" Message="@Localizer["PageName_DeleteConfirmMessage"]"
                        ConfirmText="@Localizer["Shared_Delete"]" CancelText="@Localizer["Shared_Cancel"]"
                        OnConfirm="ConfirmDelete" OnCancel="CancelDelete" />
```

```csharp
private void Delete(int id)
{
    _deleteId = id;
    _showDeleteConfirm = true;
}

private async Task ConfirmDelete()
{
    _showDeleteConfirm = false;
    await EntityService.DeleteAsync(_deleteId, _cts.Token);
    if (_editId == _deleteId)
    {
        _showForm = false;
    }
    await LoadData();
}

private void CancelDelete() => _showDeleteConfirm = false;
```

## 5. Data loading & mutation pattern

- One `LoadData()` method fetches everything the page needs, called from
  `OnInitializedAsync` and again after every successful mutation (create, update, delete,
  or any secondary save such as group access).
- Every service call passes `_cts.Token`.
- After a successful save (not delete), show the shared toast:

```csharp
private async Task ShowSavedToast()
{
    if (_toast is not null)
    {
        await _toast.Show(Localizer["PageName_ChangesSaved"]);
    }
}
```

- `Save()` branches on `_editId == 0` (create) vs. else (update), building the
  `Create*Dto`/`Update*Dto` explicitly field-by-field — do not reuse the form model as the
  DTO.

## 6. Scroll-to-form pattern

When showing the create/edit form (`ShowCreateForm()` or `Edit(entity)`), request a scroll
to the anchor so the user's viewport follows the form:

```csharp
private void ShowCreateForm()
{
    _editId = 0;
    _formModel = new EntityFormModel { /* defaults */ };
    _showForm = true;
    ScrollService.RequestScroll("entity-edit-form");
}
```

```csharp
protected override async Task OnAfterRenderAsync(bool firstRender)
{
    await ScrollService.ScrollToPendingAsync();
}
```

The anchor id passed to `ScrollService.RequestScroll` must match the `<ScrollAnchor Id="...">`
in the markup.

## 7. Cancellation token & disposal

Every page with async service calls owns a single `CancellationTokenSource`:

```csharp
private readonly CancellationTokenSource _cts = new();

public void Dispose()
{
    _cts.Cancel();
    _cts.Dispose();
}
```

Pass `_cts.Token` to every service call. Do not create ad-hoc tokens per method.

## 8. Localization rules

- No hardcoded UI strings — everything goes through `@Localizer["Key"]`.
- Resource keys are namespaced `PageName_Thing` (e.g. `LocationsPage_NameLabel`,
  `LocationsPage_DeleteConfirmTitle`).
- Reuse `Shared_*` keys (`Shared_New`, `Shared_Delete`, `Shared_Active`, `Shared_Close`,
  `Shared_Cancel`, ...) for generic labels instead of duplicating a page-specific key.
- Add new keys to both `SharedResources.en.resx` and `SharedResources.de.resx` in the same
  change.

## 9. Icons

- Use `AppIcons.*` constants for Font Awesome class names — never hardcode
  `fa-solid fa-...` strings or inline SVG.
- Icon markup inside buttons: `<i class="@AppIcons.X me-1" aria-hidden="true"></i>` followed
  by the localized label text.

## 10. Refactoring checklist

When bringing an existing page in line with this guide, verify:

- [ ] Directive/inject order matches §1.
- [ ] `<PageTitle>`, `<ToastComponent>`, `<PageTitleComponent>` header present (§2).
- [ ] Inline form uses `CardComponent` + `EditForm` + `DataAnnotationsValidator`, or has
      been extracted to a dedicated `*CreateEditPage.razor` if it's large/shared (§3).
- [ ] Form model is a private nested class implementing `IValidatableObject` when custom
      validation is needed, not the DTO itself (§3).
- [ ] Deletes go through `ConfirmDialogComponent`, never a direct delete on click (§4).
- [ ] Single `LoadData()` reload method used consistently after mutations (§5).
- [ ] `ShowSavedToast()`-style toast shown after successful saves (§5).
- [ ] `ScrollAnchor`/`ScrollService` used when toggling the form visible (§6).
- [ ] `CancellationTokenSource` + `IDisposable` present for any page issuing async service
      calls (§7).
- [ ] No hardcoded strings; all keys added to both `.en.resx` and `.de.resx` (§8).
- [ ] Icons use `AppIcons.*`, no raw Font Awesome class strings (§9).
- [ ] File and component names conform to `docs/razor-naming-conventions.md`.
