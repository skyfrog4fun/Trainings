---
name: version-update
description: "Use when you need to update release versions by taking the current source version from Directory.Build.props <Version>, then applying a target version from chat (for example '/version-update to vX.Y.Z') or auto-incrementing the last version group when no target is provided."
argument-hint: "to vX.Y.Z"
user-invocable: true
---

# Version Update

Use this skill to perform repeatable, low-noise version updates with minimal context load.

## When To Use

- Release version increments (for example `vX.Y.A` to `vX.Y.B`)
- Current version must come from `Directory.Build.props` property `<Version>`
- Target version may be passed from chat (for example `/version-update to vX.Y.Z`)
- If no target is provided, increment the last version group automatically (for example `1.2.3` to `1.2.4`)
- You want to search only tracked source files
- You want to avoid generated/build artifacts (`.vs`, `bin`, `obj`)

## Input Rules

- If chat includes `/version-update to vX.Y.Z`, pass `vX.Y.Z` into `-ToVersion`.
- If chat does not include a target version, run the update without `-ToVersion` and let the script auto-increment the last group.
- Current/source version is always read from `Directory.Build.props` and is never passed manually.

## Procedure

1. Preview current version locations from `Directory.Build.props` source version:

```powershell
powershell -File ./.github/skills/version-update/scripts/find-version-locations.ps1
```

2. Apply update using an explicit target version:

```powershell
powershell -File ./.github/skills/version-update/scripts/bump-version.ps1 -ToVersion "vX.Y.Z"
```

3. Or apply update with auto-incremented patch version:

```powershell
powershell -File ./.github/skills/version-update/scripts/bump-version.ps1
```

4. Verify old values are gone:

```powershell
# Replace X.Y.A with the source version read from Directory.Build.props
git grep -n -E "vX\.Y\.A|X\.Y\.A" -- . ":(exclude).vs/**" ":(exclude)**/bin/**" ":(exclude)**/obj/**"
```

5. Verify new values exist:

```powershell
# Replace X.Y.B with the target version (explicit or auto-incremented)
git grep -n -E "vX\.Y\.B|X\.Y\.B" -- . ":(exclude).vs/**" ":(exclude)**/bin/**" ":(exclude)**/obj/**"
```

## References

- Finder script: [find-version-locations.ps1](./scripts/find-version-locations.ps1)
- Bump script: [bump-version.ps1](./scripts/bump-version.ps1)
