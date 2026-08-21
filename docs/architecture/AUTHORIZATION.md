# Authorization Model

This document describes the authentication and authorization architecture of the Trainings application.

---

## Table of Contents

1. [Overview](#1-overview)
2. [User Identity and System Roles](#2-user-identity-and-system-roles)
3. [Claims](#3-claims)
4. [Group Membership Lifecycle](#4-group-membership-lifecycle)
5. [Policies](#5-policies)
6. [Page Access Matrix](#6-page-access-matrix)
7. [Implementation Reference](#7-implementation-reference)

---

## 1. Overview

- **Authentication:** Cookie-based authentication (`CookieAuthenticationDefaults`). The session cookie expires after **8 hours**.
- **Authorization:** Policy-based authorization. No `[Authorize(Roles = ...)]` attributes are used — all access decisions go through named policies.
- **Login path:** `/login` | **Logout path:** `/logout`

---

## 2. User Identity and System Roles

Every user has a `UserRole` on the `User` entity with two possible values:

| Value | Description |
|---|---|
| `SuperAdmin` | Full system-wide access. Bypasses all group-level restrictions. |
| `User` | Regular user. Access is determined by per-group memberships. |

System role is assigned at account creation and can only be changed by a SuperAdmin.

> A newly registered user has the `User` role and is **active from creation** (`IsActive = true`). They can log in immediately to view request statuses and update their profile, even while group membership approval is pending.

---

## 3. Claims

Claims are issued into the authentication cookie on login and reflect the user's current roles and approved group memberships.

| Claim Type | Claim Value | Issued when |
|---|---|---|
| `SuperAdmin` | `"true"` | User's `UserRole` is `SuperAdmin` |
| `GroupRole::{groupId}` | `"Admin"`, `"Trainer"`, or `"Participant"` | User has an **approved** `GroupMembership` for that group with the corresponding role |

One `GroupRole::{groupId}` claim is issued per approved group membership. A user in three groups as Trainer gets three separate claims.

> Claims are defined in [`src/Trainings.Web/Auth/AppClaimTypes.cs`](../../src/Trainings.Web/Auth/AppClaimTypes.cs).

---

## 4. Group Membership Lifecycle

A user joins a group by sending a membership request. The request moves through three states:

```
Pending ──► Approved   (group Admin or SuperAdmin approves)
        └─► Declined   (group Admin or SuperAdmin declines)
```

| Status | Effect on claims | User can access group content |
|---|---|---|
| `Pending` | No `GroupRole` claim issued | No |
| `Approved` | `GroupRole::{groupId}` claim issued on next login | Yes |
| `Declined` | No `GroupRole` claim issued | No |

> Claims reflect the membership state **at login time**. If a membership is approved or revoked while the user is logged in, the change takes effect on the next login (cookie reissue).

---

## 5. Policies

Five named policies are registered in `Program.cs`:

| Policy | Grants access to | Logic |
|---|---|---|
| `SuperAdmin` | SuperAdmins only | `SuperAdmin` claim exists with value `"true"` |
| `GroupAdmin` | Group admins and SuperAdmins | `SuperAdmin` claim = `"true"` **OR** any `GroupRole::*` claim with value `"Admin"` |
| `GroupTrainer` | Trainers, admins, and SuperAdmins | `SuperAdmin` claim = `"true"` **OR** any `GroupRole::*` claim with value `"Admin"` or `"Trainer"` |
| `GroupMember` | Any group member and SuperAdmins | `SuperAdmin` claim = `"true"` **OR** any `GroupRole::*` claim with value `"Admin"`, `"Trainer"`, or `"Participant"` |
| `Authenticated` | Any logged-in user | `RequireAuthenticatedUser()` — no specific claim required |

The policies are hierarchical: `SuperAdmin` ⊆ `GroupAdmin` ⊆ `GroupTrainer` ⊆ `GroupMember` ⊆ `Authenticated`.

---

## 6. Page Access Matrix

| Page | Required Policy | Who can access |
|---|---|---|
| `Home` | `Authenticated` | Any logged-in user |
| `UserInformationPage` | `Authenticated` | Any logged-in user |
| `TrainingsPage` | `GroupMember` | Any approved group member + SuperAdmins |
| `TrainingDetailPage` | `GroupMember` | Any approved group member + SuperAdmins |
| `MyRegistrationsPage` | `GroupMember` | Any approved group member + SuperAdmins |
| `AttendancePage` | `GroupTrainer` | Trainers, admins + SuperAdmins |
| `CreateEditTrainingPage` | `GroupTrainer` | Trainers, admins + SuperAdmins |
| `StatisticsPage` | `GroupAdmin` | Group admins + SuperAdmins |
| `GroupMembersPage` | `GroupAdmin` | Group admins + SuperAdmins |
| `GroupsPage` | `GroupAdmin` | Group admins + SuperAdmins |
| `UsersPage` | `GroupAdmin` | Group admins + SuperAdmins |
| `LocationsPage` | `SuperAdmin` | SuperAdmins only |
| `MailConfigPage` | `SuperAdmin` | SuperAdmins only |
| `IconOverviewPage` | `SuperAdmin` | SuperAdmins only |

---

## 7. Implementation Reference

| Concern | Location |
|---|---|
| Claim type constants | [`src/Trainings.Web/Auth/AppClaimTypes.cs`](../../src/Trainings.Web/Auth/AppClaimTypes.cs) |
| Policy registrations | [`src/Trainings.Web/Program.cs`](../../src/Trainings.Web/Program.cs) — `AddAuthorization(...)` block |
| Auth state provider | [`src/Trainings.Web/Auth/RevalidatingAuthStateProvider.cs`](../../src/Trainings.Web/Auth/RevalidatingAuthStateProvider.cs) |
| Infrastructure auth services | [`src/Trainings.Infrastructure/Auth/`](../../src/Trainings.Infrastructure/Auth/) |
| Domain enums | [`src/Trainings.Domain/Enums/`](../../src/Trainings.Domain/Enums/) — `UserRole`, `GroupMemberRole`, `GroupMembershipStatus` |
| Full domain model | [`docs/architecture/SPECIFICATION.md`](SPECIFICATION.md) |
