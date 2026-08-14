# Localization Key Audit — `Localizer["..."]` vs `[Page]_[Resource]` Convention

Generated as part of TODO: *"Search on all pages for `Localizer[".."]` and check if pattern `[Page]_[Resource]` is used. If not, change it to the new pattern."*

> **Naming conventions used in this report**
> - **Shared keys** (used across 2+ pages/components): prefix `Shared_[Resource]`. This replaces the previously-proposed `Common_` prefix (e.g. `Common_Save` → `Shared_Save`).
> - **Enum/list-value keys** (fixed sets of values such as gender, yes/no decisions, roles): pattern `Enum_[List]_[Value]` (e.g. `Enum_Gender_Male`, `Enum_Gender_Female`, `Enum_Decision_Yes`, `Enum_Decision_No`).
> - **Page-specific keys**: `[Page]_[Resource]`, unchanged.
>
> Please confirm before renaming — this report is audit-only.

## Summary

| Classification | Meaning | Approx. Count |
|---|---|---|
| ✅ OK | Already follows `[Page]_[Resource]` | ~230 |
| ⚠️ Page violation | Used in exactly one file, not prefixed with that page/component name | ~55 |
| 🔶 Shared candidate | Used across 2+ files; recommend `Shared_[Resource]` | ~40 |
| 🔢 Enum/list candidate | Represents a fixed set of values; recommend `Enum_[List]_[Value]` | ~5 |

Files that are **already fully compliant** (all keys page-prefixed): `ConfirmEmailPage.razor`, `ForgotPasswordPage.razor`, `LoginPage.razor`, `RegisterPage.razor`, `ResetPasswordPage.razor`, `ErrorPage.razor`, `IconOverviewPage.razor`, `NotFoundPage.razor`, `MailConfigPage.razor`, `GroupCreateEditPage.razor` (except `Save`/`Cancel`, now `Shared_Save`/`Shared_Cancel`), `GroupMembersPage.razor`, `LocationsPage.razor`, `AttendancePage.razor`, `CreateEditTrainingPage.razor` (except `MinutesUnit` → `Shared_MinutesUnit`), `TrainerRunPage.razor`, `TrainerTrainingsPage.razor`, `TrainingDetailPage.razor` (except shared keys → `Shared_*`).

Files with the **most violations** (legacy flat/shared keys not yet migrated): `MainLayout.razor`, `GroupsPage.razor`, `HomePage.razor`, `StatisticsPage.razor`, `TrainingsPage.razor`, `UserInformationPage.razor`, `UsersPage.razor`, `UserDetailPage.razor` (partially), `DashboardNotificationPanel.razor`, `DashboardPlanningPanel.razor`, `DashboardSystemPanel.razor`, `TrainingList.razor`, `MyRegistrationsPage.razor` (mostly OK, shares `MinutesUnit` → `Shared_MinutesUnit`).

---

## Enum / List-Value Cross-Reference

These keys represent a fixed set of values belonging to a logical list/enum, rather than a plain page or shared label. Recommended pattern: `Enum_[List]_[Value]`.

| Current Key | Used In | Suggested Name |
|---|---|---|
| Gender: Male | UserCreateEditPage | `Enum_Gender_Male` |
| Gender: Female | UserCreateEditPage | `Enum_Gender_Female` |
| Gender: Other | UserCreateEditPage | `Enum_Gender_Other` |
| `Yes` | UserDetailPage, UsersPage | `Enum_Decision_Yes` |
| `No` | UserDetailPage, UsersPage, GroupsPage | `Enum_Decision_No` |
| `SuperAdmin` | HomePage (role label) | `Enum_Role_SuperAdmin` |
| `GroupAdmin` | HomePage (role label) | `Enum_Role_GroupAdmin` |
| `Trainer` (as a role, not the "Trainer" column label) | HomePage, DashboardPlanningPanel | `Enum_Role_Trainer` |
| Role dropdown options | UserCreateEditPage | `Enum_Role_User` / `Enum_Role_SuperAdmin` (reuse) |

> Note: `Trainer`/`SuperAdmin`/`GroupAdmin` appear both as role-enum values and, in some contexts, as generic column headers (e.g. the "Trainer" column in `DashboardPlanningPanel`). Only the role-enum usages should move to `Enum_Role_*`; a plain "Trainer" column header, if not an enum value, stays a `Shared_Trainer` label. See per-file notes below for the specific classification chosen per occurrence.

---

## Shared-Candidate Cross-Reference (used in 2+ files)

These keys appear in multiple files and should be renamed with the `Shared_` prefix.

| Current Key | Used In | Suggested Name |
|---|---|---|
| `Dashboard` | MainLayout, HomePage | `Shared_Dashboard` |
| `Groups` | MainLayout, GroupsPage, HomePage, StatisticsPage | `Shared_Groups` |
| `Users` | MainLayout, HomePage, UsersPage | `Shared_Users` |
| `Trainings` | MainLayout, HomePage, StatisticsPage, TrainingsPage | `Shared_Trainings` |
| `Add` | GroupsPage, TrainingsPage, UsersPage | `Shared_Add` |
| `View` | GroupsPage, TrainingsPage, HomePage-ish("View All") | `Shared_View` |
| `Edit` | GroupsPage, TrainingsPage, UsersPage | `Shared_Edit` |
| `Delete` | GroupsPage, TrainingsPage | `Shared_Delete` |
| `Cancel` | TrainingsPage, GroupCreateEditPage | `Shared_Cancel` |
| `Active` | GroupsPage, UsersPage | `Shared_Active` |
| `Date` | HomePage, DashboardPlanningPanel | `Shared_Date` |
| `Status` | DashboardNotificationPanel, DashboardPlanningPanel, UserDetailPage(ish) | `Shared_Status` |
| `Actions` | HomePage, DashboardPlanningPanel | `Shared_Actions` |
| `Email` | UserInformationPage, UsersPage | `Shared_Email` |
| `Country` | UserInformationPage, UsersPage | `Shared_Country` |
| `SelectCountry` | UserInformationPage | (only 1 file, page-violation, but a `*_SelectCountry` pattern already exists on multiple pages like `GroupCreateEdit_SelectCountry`, `UserCreateEdit_SelectCountry`, `LocationsPage_SelectCountry` — consider `Shared_SelectCountry` since the concept repeats) |
| `Mobile` | UserInformationPage, UsersPage | `Shared_Mobile` |
| `City` | UserInformationPage, UsersPage | `Shared_City` |
| `Group` | HomePage, DashboardPlanningPanel, UserDetailPage(ish) | `Shared_Group` |
| `Trainer` (as generic column/label, not role enum) | DashboardPlanningPanel | `Shared_Trainer` |
| `ClearFilters` | TrainingsPage, MyRegistrationsPage(`MyRegistrationsPage_ClearFilters`) | `Shared_ClearFilters` |
| `RegisterForTraining` / `Register` / `Unregister` | HomePage, TrainingsPage, TrainingDetailPage(`TrainingDetailPage_Register`/`_Unregister`) | keep page-specific, but consider a shared verb-only variant if labels are literally identical |
| `Save` | UserInformationPage, GroupCreateEditPage, UserCreateEditPage | `Shared_Save` |
| `MinutesUnit` | CreateEditTrainingPage, MyRegistrationsPage | `Shared_MinutesUnit` |

---

## Per-File Classification

### Layout files

**`Layout/AnonymousLayout.razor`** — ✅ all OK (`AnonymousLayout_Logo`, `AnonymousLayout_Language`, `AnonymousLayout_DarkMode`, `AnonymousLayout_OpenMenu`, `AnonymousLayout_Login`, `AnonymousLayout_Register`, `AnonymousLayout_ForgotPassword`)

**`Layout/MainLayout.razor`** — ⚠️ mostly violations (this is the nav menu, so many keys are naturally "shared" across the whole app):
| Key | Classification | Suggested |
|---|---|---|
| `Language` | shared-candidate | `Shared_Language` |
| `DarkMode` | shared-candidate | `Shared_DarkMode` |
| `OpenMenu` | shared-candidate | `Shared_OpenMenu` |
| `Dashboard` | shared-candidate | `Shared_Dashboard` |
| `Groups` | shared-candidate | `Shared_Groups` |
| `Users` | shared-candidate | `Shared_Users` |
| `Trainings` | shared-candidate | `Shared_Trainings` |
| `MyRegistrations` | page-violation (single use) → could be `Shared_MyRegistrations` if reused elsewhere as nav label | `Shared_MyRegistrations` |
| `PlanTraining` | page-violation | `Shared_PlanTraining` |
| `Statistics` | shared-candidate (also used in StatisticsPage) | `Shared_Statistics` |
| `UserInformation` | page-violation | `Shared_UserInformation` |
| `Config` | page-violation | `Shared_Config` |
| `Locations` | page-violation | `Shared_Locations` |
| `Icons` | page-violation | `Shared_Icons` |
| `Logout` | page-violation | `Shared_Logout` |

> Since `MainLayout` is effectively the global nav, its labels are inherently app-wide/shared rather than page-specific — all recommended as `Shared_*`.

### Anonymous auth pages — ✅ all compliant
`ConfirmEmailPage.razor`, `ForgotPasswordPage.razor`, `LoginPage.razor`, `RegisterPage.razor`, `ResetPasswordPage.razor` all use `[PageName]_[Resource]` consistently (e.g. `Login_PageTitle`, `ForgotPassword_EmailLabel`, `ResetPassword_Button`). No action needed.

### `Pages/ErrorPage.razor` — ✅ compliant (`ErrorPage_PageTitle`, `ErrorPage_Heading`, etc.)

### `Pages/Hidden/IconOverviewPage.razor` — ✅ compliant (`IconOverviewPage_PageTitle`, `IconOverviewPage_ColIcon`, etc.)

### `Pages/NotFoundPage.razor` — ✅ compliant
| Key | Classification | Suggested |
|---|---|---|
| `NotFound_Heading` | OK | — |
| `NotFound_Message` | OK | — |

(Already compliant; included for completeness.)

### `Pages/Configuration/MailConfigPage.razor` — ✅ fully compliant (79 keys, all `MailConfigPage_*`)

### `Pages/Groups/GroupCreateEditPage.razor` — mostly ✅ compliant
All `GroupCreateEdit_*` except:
- `Save`, `Cancel` — shared-candidates → `Shared_Save`, `Shared_Cancel`.

### `Pages/Groups/GroupMembersPage.razor` — ✅ fully compliant (all `GroupMembersPage_*`)

### `Pages/Groups/GroupsPage.razor` — ⚠️ heavy violations
| Key | Classification | Suggested |
|---|---|---|
| `Groups` | shared-candidate | `Shared_Groups` |
| `Add` | shared-candidate | `Shared_Add` |
| `Active` | shared-candidate | `Shared_Active` |
| `No` | enum/list candidate | `Enum_Decision_No` |
| `View` | shared-candidate | `Shared_View` |
| `Edit` | shared-candidate | `Shared_Edit` |
| `Delete` | shared-candidate | `Shared_Delete` |
| `NoGroupsYet` | page-violation | `GroupsPage_NoGroupsYet` |

### `Pages/HomePage.razor` — ⚠️ heavy violations (dashboard aggregates many concepts)
| Key | Classification | Suggested |
|---|---|---|
| `Dashboard` | shared-candidate | `Shared_Dashboard` |
| `Welcome back` | page-violation | `HomePage_WelcomeBack` |
| `EmailVerified` | page-violation | `HomePage_EmailVerified` |
| `EmailNotVerified` | page-violation | `HomePage_EmailNotVerified` |
| `ResendVerification` | page-violation | `HomePage_ResendVerification` |
| `Groups` | shared-candidate | `Shared_Groups` |
| `Users` | shared-candidate | `Shared_Users` |
| `Trainings` | shared-candidate | `Shared_Trainings` |
| `MyNextTrainingsEnrolledAssigned` | page-violation | `HomePage_MyNextTrainingsEnrolledAssigned` |
| `PendingApprovals` | page-violation | `HomePage_PendingApprovals` |
| `Upcoming Trainings` | page-violation | `HomePage_UpcomingTrainings` |
| `View All` | page-violation | `HomePage_ViewAll` |
| `OpenTrainingDetails` | page-violation | `HomePage_OpenTrainingDetails` |
| `NoUpcomingTrainings` | page-violation | `HomePage_NoUpcomingTrainings` |
| `Quick Access` | page-violation | `HomePage_QuickAccess` |
| `New Group` | page-violation | `HomePage_NewGroup` |
| `New User` | page-violation | `HomePage_NewUser` |
| `New Training` | page-violation | `HomePage_NewTraining` |
| `NextTrainingsAvailableToAttend` | page-violation | `HomePage_NextTrainingsAvailableToAttend` |
| `Date` | shared-candidate | `Shared_Date` |
| `Group` | shared-candidate | `Shared_Group` |
| `Name` | page-violation | `HomePage_Name` |
| `Actions` | shared-candidate | `Shared_Actions` |
| `TrainingFull` | page-violation | `HomePage_TrainingFull` |
| `RegisterForTraining` | page-violation | `HomePage_RegisterForTraining` |
| `BrowseTrainings` | page-violation | `HomePage_BrowseTrainings` |
| `NoAvailableUpcomingTrainings` | page-violation | `HomePage_NoAvailableUpcomingTrainings` |
| `MyTrainingsToPlan` | page-violation | `HomePage_MyTrainingsToPlan` |
| `Plan` | page-violation | `HomePage_Plan` |
| `AllTrainings` | page-violation | `HomePage_AllTrainings` |
| `NoTrainingsToPlan` | page-violation | `HomePage_NoTrainingsToPlan` |
| `OutstandingUsersToApprove` | page-violation | `HomePage_OutstandingUsersToApprove` |
| `Review` | page-violation | `HomePage_Review` |
| `NoOutstandingApprovals` | page-violation | `HomePage_NoOutstandingApprovals` |
| `User` | page-violation | `HomePage_User` |
| `SuperAdmin` | enum/list candidate (role value) | `Enum_Role_SuperAdmin` |
| `GroupAdmin` | enum/list candidate (role value) | `Enum_Role_GroupAdmin` |
| `Trainer` | enum/list candidate (role value) | `Enum_Role_Trainer` |
| `UnknownGroup` | page-violation | `HomePage_UnknownGroup` |
| `Unknown` | page-violation | `HomePage_Unknown` |
| `VerificationEmailSent` | page-violation | `HomePage_VerificationEmailSent` |
| `FailedToResendVerificationEmail` | page-violation | `HomePage_FailedToResendVerificationEmail` |
| `SuccessfullyRegistered` | page-violation | `HomePage_SuccessfullyRegistered` |
| `GroupsCount` | page-violation | `HomePage_GroupsCount` |
| `AllGroups` | page-violation | `HomePage_AllGroups` |
| `UsersCount` | page-violation | `HomePage_UsersCount` |

### `Pages/Locations/LocationsPage.razor` — ✅ fully compliant (all `LocationsPage_*`)

### `Pages/Reporting/StatisticsPage.razor` — ⚠️ violations
| Key | Classification | Suggested |
|---|---|---|
| `Statistics` | page-violation | `StatisticsPage_Statistics` |
| `Groups` | shared-candidate | `Shared_Groups` |
| `StatisticsActiveUsers` | needs underscore | `StatisticsPage_ActiveUsers` |
| `StatisticsUsersNotInGroup` | needs underscore | `StatisticsPage_UsersNotInGroup` |
| `Trainings` | shared-candidate | `Shared_Trainings` |
| `StatisticsTotalRegistrations` | needs underscore | `StatisticsPage_TotalRegistrations` |
| `StatisticsAnnualIndicators` | needs underscore | `StatisticsPage_AnnualIndicators` |
| `StatisticsAvgUsersPerGroup` | needs underscore | `StatisticsPage_AvgUsersPerGroup` |
| `StatisticsAvgParticipantsPerTraining` | needs underscore | `StatisticsPage_AvgParticipantsPerTraining` |
| `StatisticsScopeGlobal` | needs underscore | `StatisticsPage_ScopeGlobal` |
| `StatisticsScopeManagedGroups` | needs underscore | `StatisticsPage_ScopeManagedGroups` |

### `Pages/Trainings/AttendancePage.razor` — ✅ fully compliant (all `AttendancePage_*`)

### `Pages/Trainings/CreateEditTrainingPage.razor` — mostly ✅ compliant
All `CreateEditTraining_*` except `MinutesUnit` → `Shared_MinutesUnit`.

### `Pages/Trainings/MyRegistrationsPage.razor` — mostly ✅ compliant
All `MyRegistrationsPage_*` except `MinutesUnit` → `Shared_MinutesUnit`.

### `Pages/Trainings/TrainerRunPage.razor` — ✅ fully compliant (all `TrainerRunPage_*`)

### `Pages/Trainings/TrainerTrainingsPage.razor` — ✅ fully compliant (all `TrainerTrainingsPage_*`)

### `Pages/Trainings/TrainingDetailPage.razor` — mostly ✅ compliant
All `TrainingDetailPage_*` except shared keys → `Shared_*`.

### `Pages/Trainings/TrainingsPage.razor` — ⚠️ heavy violations
| Key | Classification | Suggested |
|---|---|---|
| `Trainings` | shared-candidate | `Shared_Trainings` |
| `Add` | shared-candidate | `Shared_Add` |
| `AllGroups` | page-violation | `TrainingsPage_AllGroups` |
| `AllTrainers` | page-violation | `TrainingsPage_AllTrainers` |
| `ClearFilters` | shared-candidate | `Shared_ClearFilters` |
| `View` | shared-candidate | `Shared_View` |
| `Edit` | shared-candidate | `Shared_Edit` |
| `Cancel` | shared-candidate | `Shared_Cancel` |
| `Delete` | shared-candidate | `Shared_Delete` |
| `TrainingCount` | page-violation | `TrainingsPage_TrainingCount` |
| `RegistrationCancelled` | page-violation | `TrainingsPage_RegistrationCancelled` |
| `SuccessfullyRegistered` | page-violation | `TrainingsPage_SuccessfullyRegistered` |
| `TrainingDeleted` | page-violation | `TrainingsPage_TrainingDeleted` |

### `Pages/Users/UserCreateEditPage.razor` — mostly ✅ compliant
All `UserCreateEdit_*` except:
- Gender values → `Enum_Gender_Male`, `Enum_Gender_Female`, `Enum_Gender_Other`.
- `Save`, `Cancel` — shared-candidates → `Shared_Save`, `Shared_Cancel`.
- `AssignUserToManagedGroup` — ⚠️ page-violation → `UserCreateEdit_AssignUserToManagedGroup`
- `PasswordValidationMinLength`, `PasswordValidationUppercase`, `PasswordValidationLowercase`, `PasswordValidationDigit`, `PasswordValidationSpecialCharacter` — shared-candidates (validation messages likely reused wherever passwords are set) → `Shared_PasswordValidationMinLength`, etc. (or `UserCreateEdit_` if confirmed single-use)

### `Pages/Users/UserDetailPage.razor` — mostly ✅ compliant
All `UserDetailPage_*` except:
- `Yes` / `No` — enum/list candidates → `Enum_Decision_Yes` / `Enum_Decision_No`

### `Pages/Users/UserInformationPage.razor` — ⚠️ violations
| Key | Classification | Suggested |
|---|---|---|
| `UserInformationPage_*` (PageTitle, UnableToLoadProfile, ProfileTitle, GroupMembershipsTitle, ColRequested, NoMemberships, UpdatedSuccess, UpdateFailed) | OK | — |
| `FirstName` | page-violation | `UserInformationPage_FirstName` |
| `LastName` | page-violation | `UserInformationPage_LastName` |
| `Email` | shared-candidate | `Shared_Email` |
| `Mobile` | shared-candidate | `Shared_Mobile` |
| `City` | shared-candidate | `Shared_City` |
| `Country` | shared-candidate | `Shared_Country` |
| `SelectCountry` | shared-candidate | `Shared_SelectCountry` |
| `WelcomeMessageBio` | page-violation | `UserInformationPage_WelcomeMessageBio` |
| `Save` | shared-candidate | `Shared_Save` |
| `Group` | shared-candidate | `Shared_Group` |
| `Role` | shared-candidate | `Shared_Role` |
| `Status` | shared-candidate | `Shared_Status` |
| `GroupNumberFallback` | page-violation | `UserInformationPage_GroupNumberFallback` |

### `Pages/Users/UsersPage.razor` — ⚠️ heavy violations
| Key | Classification | Suggested |
|---|---|---|
| `Users` | shared-candidate | `Shared_Users` |
| `Add` | shared-candidate | `Shared_Add` |
| `ReadOnlyModeSuperAdminOnly` | page-violation | `UsersPage_ReadOnlyModeSuperAdminOnly` |
| `PendingApprovals` | page-violation | `UsersPage_PendingApprovals` |
| `Name` | page-violation | `UsersPage_Name` |
| `RequestedGroup` | page-violation | `UsersPage_RequestedGroup` |
| `RegistrationDate` | page-violation | `UsersPage_RegistrationDate` |
| `ToggleDetails` | page-violation | `UsersPage_ToggleDetails` |
| `Email` | shared-candidate | `Shared_Email` |
| `Country` | shared-candidate | `Shared_Country` |
| `City` | shared-candidate | `Shared_City` |
| `Mobile` | shared-candidate | `Shared_Mobile` |
| `Gender` | page-violation | `UsersPage_Gender` |
| `EmailVerified` | page-violation | `UsersPage_EmailVerified` |
| `Yes` / `No` | enum/list candidates | `Enum_Decision_Yes` / `Enum_Decision_No` |
| `Active` | shared-candidate | `Shared_Active` |
| `View` | shared-candidate | `Shared_View` |
| `Edit` | shared-candidate | `Shared_Edit` |
| `SendAccountMail` | page-violation | `UsersPage_SendAccountMail` |
| `DeleteUser` | page-violation | `UsersPage_DeleteUser` |
| `Deactivate` | page-violation | `UsersPage_Deactivate` |
| `Activate` | page-violation | `UsersPage_Activate` |
| `UserCannotBeDeletedDueToRegistrations` | page-violation | `UsersPage_UserCannotBeDeletedDueToRegistrations` |
| `UserWasDeleted` | page-violation | `UsersPage_UserWasDeleted` |
| `FailedMessage` | page-violation | `UsersPage_FailedMessage` |
| `AccountEmailSentTo` | page-violation | `UsersPage_AccountEmailSentTo` |
| `NoEmailModePreviewShown` | page-violation | `UsersPage_NoEmailModePreviewShown` |

### `Shared/DashboardNotificationPanel.razor` — ⚠️ all violations
| Key | Suggested |
|---|---|
| `NotificationActivity` | `DashboardNotificationPanel_NotificationActivity` |
| `ResetPointer` | `DashboardNotificationPanel_ResetPointer` |
| `LogId` | `DashboardNotificationPanel_LogId` |
| `DateUtc` | `DashboardNotificationPanel_DateUtc` |
| `Action` | `DashboardNotificationPanel_Action` |
| `Recipient` | `DashboardNotificationPanel_Recipient` |
| `Status` | `Shared_Status` (shared-candidate) |
| `Error` | `DashboardNotificationPanel_Error` |
| `NoNotificationActivitySinceReset` | `DashboardNotificationPanel_NoNotificationActivitySinceReset` |

### `Shared/DashboardPlanningPanel.razor` — ⚠️ all violations
| Key | Suggested |
|---|---|
| `PlanningOverview` | `DashboardPlanningPanel_PlanningOverview` |
| `Date` | `Shared_Date` (shared-candidate) |
| `Location` | `DashboardPlanningPanel_Location` (or `Shared_Location` if reused) |
| `Trainer` | `Shared_Trainer` (shared-candidate; column label, not the role enum) |
| `Status` | `Shared_Status` (shared-candidate) |
| `Actions` | `Shared_Actions` (shared-candidate) |

### `Shared/DashboardSystemPanel.razor` — ⚠️ all violations
| Key | Suggested |
|---|---|
| `SystemOverview` | `DashboardSystemPanel_SystemOverview` |
| `MailConfigurationCount` | `DashboardSystemPanel_MailConfigurationCount` |
| `OpenMailConfigurationOverview` | `DashboardSystemPanel_OpenMailConfigurationOverview` |
| `CreateMailConfiguration` | `DashboardSystemPanel_CreateMailConfiguration` |
| `LocationsCount` | `DashboardSystemPanel_LocationsCount` |
| `OpenLocationsOverview` | `DashboardSystemPanel_OpenLocationsOverview` |
| `CreateLocation` | `DashboardSystemPanel_CreateLocation` |
| `GroupsCount` | `DashboardSystemPanel_GroupsCount` |
| `OpenGroupsOverview` | `DashboardSystemPanel_OpenGroupsOverview` |
| `CreateGroup` | `DashboardSystemPanel_CreateGroup` |
| `UsersCount` | `DashboardSystemPanel_UsersCount` |
| `OpenUsersOverview` | `DashboardSystemPanel_OpenUsersOverview` |
| `CreateUser` | `DashboardSystemPanel_CreateUser` |
| `TrainingsCount` | `DashboardSystemPanel_TrainingsCount` |
| `OpenTrainingsOverview` | `DashboardSystemPanel_OpenTrainingsOverview` |
| `CreateTraining` | `DashboardSystemPanel_CreateTraining` |

### `Shared/TrainingList.razor` — ⚠️ violations
| Key | Suggested |
|---|---|
| `InPlanning` | `TrainingList_InPlanning` |
| `DurationInMinutes` | `TrainingList_DurationInMinutes` |

---

## Recommended Next Steps (follow-up TODOs, not done in this pass)

1. Confirm the naming conventions in this report: `Shared_` prefix for cross-page keys, `Enum_[List]_[Value]` for fixed-set/enum values.
2. Rename all flagged keys in both `.razor` files and `SharedResources.en.resx` / `SharedResources.de.resx` (keeping English/German values in sync).
3. Re-run this audit after renaming to confirm 100% compliance.
4. Continue with the other localization TODOs (resx completeness check, cleanup of unused keys, translation review).
