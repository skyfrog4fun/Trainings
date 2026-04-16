# Application Specification — Trainings

> **Language:** English (US)
> **Primary audience:** AI agents and automated tooling
> **Secondary audience:** Human developers and stakeholders

---

## 1. Overview

| Field            | Value                                                                    |
|------------------|--------------------------------------------------------------------------|
| **Application**  | Trainings                                                                |
| **Purpose**      | Plan training sessions, manage registrations, and track attendance       |
| **Architecture** | ASP.NET Core Blazor Server, Clean Architecture (Domain / Application / Infrastructure / Web) |
| **Persistence**  | Relational database via Entity Framework Core                            |
| **Auth**         | Cookie-based authentication with role-based authorization; self-registration with email confirmation and admin approval |

### Human Summary

The **Trainings** application lets administrators and trainers organize training events.
Participants can self-register, confirm their email, and await admin approval before accessing the system.
Once active, participants can register for sessions; trainers record who actually attended and structure sessions using a library of reusable training blocks.
Trainings can be scoped to groups. Tags help categorize training content.
Four roles — `SuperAdmin`, `Admin`, `Trainer`, and `Participant` — control what each user may do.

---

## 2. Domain Model

> Entity field details are defined in the POCO classes under `src/Trainings.Domain/Entities/`.

### 2.1 Enumerations

| Enum                | Values                                        |
|---------------------|-----------------------------------------------|
| `UserRole`          | `SuperAdmin`, `Admin`, `Trainer`, `Participant` |
| `RegistrationStatus`| `Registered`, `Cancelled`                     |
| `AttendanceStatus`  | `Present`, `Absent`                           |
| `Gender`            | `Male`, `Female`, `Other`                     |
| `GroupMemberRole`   | `Admin`, `Trainer`, `Participant`             |

### 2.2 Relationships (ERD notation)

```
User         ||--o{  Training            : "trains (TrainerId)"
User         ||--o{  Registration        : "registers"
User         ||--o{  Attendance          : "has attendance"
User         ||--o{  GroupMembership     : "is member of"
User         ||--o{  PendingGroupRequest : "requests to join"
Training     ||--o{  Registration        : "has"
Training     ||--o{  Attendance          : "records"
Training     ||--o{  TrainingBlock       : "structured by"
Training     }o--||  Group               : "belongs to (optional)"
Group        ||--o{  GroupMembership     : "has members"
Group        ||--o{  PendingGroupRequest : "has pending requests"
Group        ||--o{  Tag                 : "owns"
TrainingBlock||--o{  TrainingBlockTag    : "tagged with"
Tag          ||--o{  TrainingBlockTag    : "applied to"
```

---

## 3. User Roles & Permissions

| Action                          | SuperAdmin | Admin | Trainer | Participant |
|---------------------------------|:----------:|:-----:|:-------:|:-----------:|
| Create / edit / delete user     |    ✔       |  ✔    |         |             |
| Approve / reject user sign-up   |    ✔       |  ✔    |         |             |
| Create / edit / delete training |    ✔       |  ✔    |  ✔ (own)|             |
| Manage groups                   |    ✔       |  ✔    |         |             |
| View all trainings              |    ✔       |  ✔    |  ✔      |  ✔          |
| Register for a training         |            |       |         |  ✔          |
| Cancel own registration         |            |       |         |  ✔          |
| Record attendance               |    ✔       |  ✔    |  ✔ (own)|             |
| View attendance report          |    ✔       |  ✔    |  ✔ (own)|             |
| Manage training blocks          |    ✔       |  ✔    |  ✔ (own)|             |
| Manage tags                     |    ✔       |  ✔    |         |             |

---

## 4. Features & Use Cases

### UC-01 — Manage Users (Admin / SuperAdmin)
- **Actor:** Admin or SuperAdmin
- **Steps:**
  1. Navigate to user management.
  2. Create a new user with `FirstName`, `LastName`, `Email`, `Password`, `Role`, `Gender`, `Birthday`, `Mobile`, `City`.
  3. Edit or deactivate an existing user.
- **Postcondition:** User record persisted; password stored as bcrypt hash.

### UC-02 — User Self-Registration
- **Actor:** Anonymous visitor
- **Steps:**
  1. Fill in the registration form with personal details and email.
  2. Confirm email via confirmation link.
  3. Admin approves (or rejects) the pending account.
- **Postcondition:** User account becomes active after approval and email confirmation.

### UC-03 — Manage Training Sessions (Admin / Trainer)
- **Actor:** Admin or Trainer
- **Steps:**
  1. Create a training with `Title`, `Description`, `Location`, `DateTime`, `Capacity`, and optionally assign it to a `Group`.
  2. Edit or deactivate an existing training.
- **Business Rules:**
  - A Trainer may only manage their own trainings.
  - `Capacity` must be ≥ 1.
  - `DateTime` must be strictly in the future (not now or in the past) at creation time.

### UC-04 — Manage Training Blocks (Admin / Trainer)
- **Actor:** Admin or Trainer
- **Steps:**
  1. Add ordered blocks to a training session (title, description, planned duration, tags).
  2. Reorder, edit, or delete existing blocks.
  3. Copy a block from one training to another.
  4. Browse the shared block library to reuse blocks across trainings.
- **Postcondition:** Training plan is structured into time-boxed segments.

### UC-05 — Register for a Training (Participant)
- **Actor:** Participant
- **Steps:**
  1. Browse active trainings.
  2. Register for a training that has available capacity.
- **Business Rules:**
  - A participant cannot register for the same training twice.
  - Registration is not allowed when `RegisteredCount >= Capacity`.

### UC-06 — Cancel Registration (Participant)
- **Actor:** Participant
- **Steps:**
  1. View own registrations.
  2. Cancel a registration that is in `Registered` status.
- **Postcondition:** `RegistrationStatus` set to `Cancelled`.

### UC-07 — Record Attendance (Admin / Trainer)
- **Actor:** Admin or Trainer
- **Steps:**
  1. Select a training session.
  2. For each registered participant, mark `Present` or `Absent`.
- **Business Rules:**
  - Only users with an active `Registered` registration may have attendance recorded.
  - The trainer recording attendance must be the trainer of that session (or Admin).

### UC-08 — View Attendance Report (Admin / Trainer)
- **Actor:** Admin or Trainer
- **Steps:**
  1. Select a training session.
  2. View a list of attendees with their `AttendanceStatus`.

### UC-09 — Manage Groups (Admin)
- **Actor:** Admin
- **Steps:**
  1. Create a group with a name and optional description.
  2. Add or remove members, assigning each a `GroupMemberRole`.
  3. Trainings can be scoped to a group, limiting visibility to group members.

### UC-10 — Manage Tags (Admin)
- **Actor:** Admin
- **Steps:**
  1. Create tags optionally scoped to a group.
  2. Tags can be applied to training blocks to categorize content.

### UC-11 — Password Reset
- **Actor:** Any authenticated or unauthenticated user
- **Steps:**
  1. Request a password reset link via email.
  2. Follow the link and set a new password.

---

## 5. Technical Architecture

```
┌─────────────────────────────────────────┐
│  Trainings.Web  (Blazor Server)         │
│  - Razor components, pages, auth UI     │
└───────────────────┬─────────────────────┘
                    │ calls
┌───────────────────▼─────────────────────┐
│  Trainings.Application                  │
│  - Services: Training / User /          │
│    Registration / Attendance            │
│  - DTOs, Interfaces                     │
└───────────────────┬─────────────────────┘
                    │ calls
┌───────────────────▼─────────────────────┐
│  Trainings.Infrastructure               │
│  - EF Core DbContext                    │
│  - Repositories                         │
│  - Password hasher                      │
└───────────────────┬─────────────────────┘
                    │ depends on
┌───────────────────▼─────────────────────┐
│  Trainings.Domain                       │
│  - Entities, Enums, Interfaces          │
└─────────────────────────────────────────┘
```

### Layer Responsibilities

| Layer            | Responsibility                                                           |
|------------------|--------------------------------------------------------------------------|
| `Domain`         | Core entities, enumerations, repository interfaces — no external deps    |
| `Application`    | Business logic, use-case orchestration, DTO mapping                      |
| `Infrastructure` | EF Core persistence, auth, concrete repository implementations           |
| `Web`            | Blazor Server UI, cookie auth middleware, DI registration                |

---

## 6. Service Interfaces (Application Layer)

```csharp
ITrainingService
  Task<TrainingDto?>                   GetByIdAsync(int id)
  Task<IEnumerable<TrainingDto>>       GetAllAsync()
  Task<IEnumerable<TrainingDto>>       GetActiveAsync()
  Task<IEnumerable<TrainingDto>>       GetByTrainerIdAsync(int trainerId)
  Task<TrainingDto>                    CreateAsync(CreateTrainingDto dto)
  Task                                 UpdateAsync(UpdateTrainingDto dto)
  Task                                 DeleteAsync(int id)
  // Training blocks
  Task<IEnumerable<TrainingBlockDto>>  GetBlocksAsync(int trainingId, CancellationToken ct)
  Task<TrainingBlockDto>               AddBlockAsync(CreateTrainingBlockDto dto, CancellationToken ct)
  Task                                 UpdateBlockAsync(UpdateTrainingBlockDto dto, CancellationToken ct)
  Task                                 DeleteBlockAsync(int blockId, CancellationToken ct)
  Task                                 CopyBlockAsync(int sourceBlockId, int targetTrainingId, CancellationToken ct)
  Task<IEnumerable<TrainingBlockDto>>  GetAllBlocksLibraryAsync(CancellationToken ct)

IUserService
  Task<UserDto?>                       GetByIdAsync(int id)
  Task<UserDto?>                       GetByEmailAsync(string email)
  Task<IEnumerable<UserDto>>           GetAllAsync()
  Task<IEnumerable<UserDto>>           GetByRoleAsync(UserRole role)
  Task<UserDto>                        CreateAsync(CreateUserDto dto)
  Task                                 UpdateAsync(UpdateUserDto dto)
  Task                                 DeleteAsync(int id)
  Task<bool>                           ValidatePasswordAsync(string email, string password)
  Task                                 ChangePasswordAsync(int userId, string newPasswordHash)

IRegistrationService
  Task<IEnumerable<RegistrationDto>>   GetByUserIdAsync(int userId)
  Task<IEnumerable<RegistrationDto>>   GetByTrainingIdAsync(int trainingId)
  Task<RegistrationDto>                RegisterAsync(int userId, int trainingId)
  Task                                 CancelAsync(int userId, int trainingId)
  Task<bool>                           IsRegisteredAsync(int userId, int trainingId)

IAttendanceService
  Task<IEnumerable<AttendanceDto>>     GetByTrainingIdAsync(int trainingId)
  Task<IEnumerable<AttendanceDto>>     GetByUserIdAsync(int userId)
  Task                                 RecordAttendanceAsync(int userId, int trainingId, AttendanceStatus status, int recordedByTrainerId)

IGroupService
  Task<IEnumerable<GroupDto>>          GetAllAsync(CancellationToken ct)
  Task<GroupDto?>                      GetByIdAsync(int id, CancellationToken ct)
  Task<GroupDto>                       CreateAsync(CreateGroupDto dto, CancellationToken ct)
  Task                                 UpdateAsync(UpdateGroupDto dto, CancellationToken ct)
  Task                                 DeleteAsync(int id, CancellationToken ct)
  Task<IEnumerable<GroupMembershipDto>>GetMembersAsync(int groupId, CancellationToken ct)
  Task                                 AddMemberAsync(AddGroupMemberDto dto, CancellationToken ct)
  Task                                 RemoveMemberAsync(int membershipId, CancellationToken ct)
  Task<IEnumerable<GroupDto>>          GetGroupsForUserAsync(int userId, CancellationToken ct)

ITagService
  Task<IEnumerable<TagDto>>            GetAllAsync(CancellationToken ct)
  Task<IEnumerable<TagDto>>            GetByGroupAsync(int? groupId, CancellationToken ct)
  Task<TagDto>                         CreateAsync(CreateTagDto dto, CancellationToken ct)
  Task                                 DeleteAsync(int id, CancellationToken ct)

IUserRegistrationService
  Task<UserDto>                        RegisterAsync(RegisterRequestDto dto, CancellationToken ct)
  Task                                 ConfirmEmailAsync(string token, CancellationToken ct)
  Task                                 ApproveUserAsync(int userId, int adminUserId, CancellationToken ct)
  Task                                 RejectUserAsync(int userId, int adminUserId, CancellationToken ct)
  Task<IEnumerable<UserDto>>           GetPendingApprovalsAsync(CancellationToken ct)

IPasswordResetService
  Task                                 RequestResetAsync(string email, CancellationToken ct)
  Task                                 ResetPasswordAsync(string token, string newPassword, CancellationToken ct)

IEmailService
  Task                                 SendPasswordResetAsync(string toEmail, string resetLink, CancellationToken ct)
  Task                                 SendEmailConfirmationAsync(string toEmail, string confirmLink, CancellationToken ct)
  Task                                 SendAdminNewParticipantNotificationAsync(string adminEmail, string userName, CancellationToken ct)
  Task                                 SendTestEmailAsync(string toEmail, CancellationToken ct)
```

---

## 7. Business Rules (Machine-Readable)

```yaml
rules:
  - id: BR-001
    entity: Registration
    trigger: RegisterAsync
    condition: "registeredCount >= training.Capacity"
    action: throw InvalidOperationException("Training is at full capacity.")

  - id: BR-002
    entity: Registration
    trigger: RegisterAsync
    condition: "existingRegistration.Status == Registered"
    action: throw InvalidOperationException("User is already registered for this training.")

  - id: BR-003
    entity: Training
    trigger: CreateAsync
    condition: "dto.DateTime <= DateTime.UtcNow"
    action: throw ArgumentException("Training date must be strictly in the future (not now or in the past).")

  - id: BR-004
    entity: Training
    trigger: CreateAsync | UpdateAsync
    condition: "dto.Capacity < 1"
    action: throw ArgumentException("Capacity must be at least 1.")

  - id: BR-005
    entity: Attendance
    trigger: RecordAttendanceAsync
    condition: "trainer is not Admin AND trainer.Id != training.TrainerId"
    action: throw UnauthorizedAccessException("Only the assigned trainer or an Admin may record attendance.")

  - id: BR-006
    entity: User
    trigger: RegisterAsync (self-registration)
    condition: "email not confirmed OR admin approval pending"
    action: account inactive until email confirmed and admin approves

  - id: BR-007
    entity: User
    trigger: ChangePasswordAsync
    condition: always
    action: password is hashed before persistence; plain-text is never stored
```

---

## 8. Non-Functional Requirements

| ID     | Category      | Requirement                                                                 |
|--------|---------------|-----------------------------------------------------------------------------|
| NFR-01 | Security      | Passwords must be stored using a one-way hash (bcrypt or equivalent).       |
| NFR-02 | Security      | All routes except `/Login`, `/Register`, and password-reset pages require authenticated session. |
| NFR-03 | Availability  | Application must handle concurrent registrations without double-booking.    |
| NFR-04 | Maintainability | Follow Clean Architecture; no cross-layer dependency violations.           |
| NFR-05 | Testability   | Application and Domain layers must be testable without Infrastructure.      |
| NFR-06 | Security      | New self-registered accounts must confirm their email and receive admin approval before accessing the system. |

---

## 9. Glossary

| Term           | Definition                                                                   |
|----------------|------------------------------------------------------------------------------|
| Training       | A scheduled session with a title, location, date/time, capacity, and trainer |
| Trainer        | A user responsible for conducting and managing a training session            |
| Participant    | A user who can register for and attend training sessions                     |
| Registration   | The act of a participant reserving a spot in a training                      |
| Attendance     | A record of whether a registered participant was present or absent           |
| Capacity       | The maximum number of participants allowed in a training session             |
| Group          | An organizational unit that can contain members and scope trainings          |
| GroupMembership| A user's membership in a group, with an associated role                      |
| TrainingBlock  | A time-boxed segment of a training session (title, duration, tags)           |
| Tag            | A label that can be applied to training blocks to categorize content         |
