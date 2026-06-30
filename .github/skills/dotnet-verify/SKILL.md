---
name: dotnet-verify
description: "Use when you need a strict local .NET verification that is at least as strict as CI: restore, build with warnings as errors, test, format verification, and package security checks."
user-invocable: true
---

# .NET Quality Gate

Use this skill when you want a strict, repeatable local verification pass over a .NET solution or project.

## Strictness policy

- Local quality gate must be at least as strict as CI.
- Treat vulnerable and deprecated packages as errors locally.
- If a command exits successfully but reports findings that violate this policy, treat the step as failed.

## Default target

- If no path is provided, use the repository solution file (`Trainings.slnx`).
- Use a project file only when intentionally scoping checks to one project.

## Order of checks

Run the checks in this order:

1. `dotnet restore <path-to-solution-or-project>`
2. `dotnet build <path-to-solution-or-project> -c Release --no-restore /warnaserror`
3. `dotnet test <path-to-solution-or-project> -c Release --no-build --verbosity normal` when test projects are present
4. `dotnet format <path-to-solution-or-project> --verify-no-changes`
5. `dotnet list <path-to-solution-or-project> package --vulnerable --include-transitive`
6. `dotnet list <path-to-solution-or-project> package --deprecated --include-transitive`

Fail the gate if step 5 or step 6 reports any package findings.

## Notes

- Keep this sequence aligned with CI semantics while enforcing stricter local package policy.
- Keep `--no-restore` on build because restore already ran in step 1.
- Keep `--no-build` on test because build already ran in step 2.
- Test-step rule:
	- Solution target: run tests.
	- Project target: run tests only when the target project name ends with `.Tests`.
- Output contract:
	- Report each step as PASS or FAIL.
	- Stop on first hard failure.
