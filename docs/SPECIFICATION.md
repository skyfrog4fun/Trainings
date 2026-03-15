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
| **Auth**         | Cookie-based authentication with role-based authorization                |

### Human Summary

The **Trainings** application lets administrators and trainers organize training events.
Participants can register for sessions; trainers record who actually attended.
Three roles — `Admin`, `Trainer`, and `Participant` — control what each user may do.

---

## 2. Domain Model

### 2.1 Entities

```
Training
  Id             : int          (PK)
  Title          : string       (required)
  Description    : string
  Location       : string
  DateTime       : DateTime     (UTC)
  Capacity       : int          (≥ 1)
  IsActive       : bool         (default: true)
  TrainerId      : int          (FK → User)
  Trainer        : User
  Registrations  : Registration[]
  Attendances    : Attendance[]

User
  Id             : int          (PK)
  Name           : string       (required)
  Email          : string       (required, unique)
  PasswordHash   : string
  Role           : UserRole     (Admin | Trainer | Participant)
  IsActive       : bool         (default: true)
  CreatedAt      : DateTime     (UTC)
  TrainingsAsTrainer : Training[]
  Registrations      : Registration[]
  Attendances        : Attendance[]

Registration
  Id             : int          (PK)
  UserId         : int          (FK → User)
  TrainingId     : int          (FK → Training)
  RegisteredAt   : DateTime     (UTC)
  Status         : RegistrationStatus (Registered | Cancelled)

Attendance
  Id                    : int   (PK)
  UserId                : int   (FK → User)
  TrainingId            : int   (FK → Training)
  Status                : AttendanceStatus (Present | Absent)
  RecordedAt            : DateTime (UTC)
  RecordedByTrainerId   : int   (FK → User)
```

### 2.2 Enumerations

| Enum                | Values                       |
|---------------------|------------------------------|
| `UserRole`          | `Admin`, `Trainer`, `Participant` |
| `RegistrationStatus`| `Registered`, `Cancelled`    |
| `AttendanceStatus`  | `Present`, `Absent`          |

### 2.3 Relationships (ERD notation)

```
User  ||--o{  Training       : "trains (TrainerId)"
User  ||--o{  Registration   : "registers"
User  ||--o{  Attendance     : "has attendance"
Training ||--o{ Registration : "has"
Training ||--o{ Attendance   : "records"
```

---

## 3. User Roles & Permissions

| Action                         | Admin | Trainer | Participant |
|--------------------------------|:-----:|:-------:|:-----------:|
| Create / edit / delete user    |  ✔    |         |             |
| Create / edit / delete training|  ✔    |  ✔ (own)|             |
| View all trainings             |  ✔    |  ✔      |  ✔          |
| Register for a training        |       |         |  ✔          |
| Cancel own registration        |       |         |  ✔          |
| Record attendance              |  ✔    |  ✔ (own)|             |
| View attendance report         |  ✔    |  ✔ (own)|             |

---

## 4. Features & Use Cases

### UC-01 — Manage Users (Admin)
- **Actor:** Admin
- **Steps:**
  1. Navigate to user management.
  2. Create a new user with `Name`, `Email`, `Password`, and `Role`.
  3. Edit or deactivate an existing user.
- **Postcondition:** User record persisted; password stored as bcrypt hash.

### UC-02 — Manage Training Sessions (Admin / Trainer)
- **Actor:** Admin or Trainer
- **Steps:**
  1. Create a training with `Title`, `Description`, `Location`, `DateTime`, and `Capacity`.
  2. Edit or deactivate an existing training.
- **Business Rules:**
  - A Trainer may only manage their own trainings.
  - `Capacity` must be ≥ 1.
  - `DateTime` must be strictly in the future (not now or in the past) at creation time.

### UC-03 — Register for a Training (Participant)
- **Actor:** Participant
- **Steps:**
  1. Browse active trainings.
  2. Register for a training that has available capacity.
- **Business Rules:**
  - A participant cannot register for the same training twice.
  - Registration is not allowed when `RegisteredCount >= Capacity`.

### UC-04 — Cancel Registration (Participant)
- **Actor:** Participant
- **Steps:**
  1. View own registrations.
  2. Cancel a registration that is in `Registered` status.
- **Postcondition:** `RegistrationStatus` set to `Cancelled`.

### UC-05 — Record Attendance (Admin / Trainer)
- **Actor:** Admin or Trainer
- **Steps:**
  1. Select a training session.
  2. For each registered participant, mark `Present` or `Absent`.
- **Business Rules:**
  - Only users with an active `Registered` registration may have attendance recorded.
  - The trainer recording attendance must be the trainer of that session (or Admin).

### UC-06 — View Attendance Report (Admin / Trainer)
- **Actor:** Admin or Trainer
- **Steps:**
  1. Select a training session.
  2. View a list of attendees with their `AttendanceStatus`.

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
  Task<TrainingDto?>          GetByIdAsync(int id)
  Task<IEnumerable<TrainingDto>> GetAllAsync()
  Task<IEnumerable<TrainingDto>> GetActiveAsync()
  Task<IEnumerable<TrainingDto>> GetByTrainerIdAsync(int trainerId)
  Task<TrainingDto>            CreateAsync(CreateTrainingDto dto)
  Task                         UpdateAsync(UpdateTrainingDto dto)
  Task                         DeleteAsync(int id)

IUserService
  Task<UserDto?>               GetByIdAsync(int id)
  Task<UserDto?>               GetByEmailAsync(string email)
  Task<IEnumerable<UserDto>>   GetAllAsync()
  Task<IEnumerable<UserDto>>   GetByRoleAsync(UserRole role)
  Task<UserDto>                CreateAsync(CreateUserDto dto)
  Task                         UpdateAsync(UpdateUserDto dto)
  Task                         DeleteAsync(int id)
  Task<bool>                   ValidatePasswordAsync(string email, string password)

IRegistrationService
  Task<RegistrationDto?>       GetByIdAsync(int id)
  Task<IEnumerable<RegistrationDto>> GetByTrainingIdAsync(int trainingId)
  Task<IEnumerable<RegistrationDto>> GetByUserIdAsync(int userId)
  Task<RegistrationDto>        RegisterAsync(int userId, int trainingId)
  Task                         CancelAsync(int registrationId)

IAttendanceService
  Task<IEnumerable<AttendanceDto>> GetByTrainingIdAsync(int trainingId)
  Task<AttendanceDto>              RecordAsync(RecordAttendanceDto dto)
  Task                             UpdateAsync(UpdateAttendanceDto dto)
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
    trigger: RecordAsync
    condition: "trainer is not Admin AND trainer.Id != training.TrainerId"
    action: throw UnauthorizedAccessException("Only the assigned trainer or an Admin may record attendance.")
```

---

## 8. Non-Functional Requirements

| ID     | Category      | Requirement                                                                 |
|--------|---------------|-----------------------------------------------------------------------------|
| NFR-01 | Security      | Passwords must be stored using a one-way hash (bcrypt or equivalent).       |
| NFR-02 | Security      | All routes except `/Login` require authenticated session.                   |
| NFR-03 | Availability  | Application must handle concurrent registrations without double-booking.    |
| NFR-04 | Maintainability | Follow Clean Architecture; no cross-layer dependency violations.           |
| NFR-05 | Testability   | Application and Domain layers must be testable without Infrastructure.      |

---

## 9. Glossary

| Term         | Definition                                                                   |
|--------------|------------------------------------------------------------------------------|
| Training     | A scheduled session with a title, location, date/time, capacity, and trainer |
| Trainer      | A user responsible for conducting and managing a training session            |
| Participant  | A user who can register for and attend training sessions                     |
| Registration | The act of a participant reserving a spot in a training                      |
| Attendance   | A record of whether a registered participant was present or absent           |
| Capacity     | The maximum number of participants allowed in a training session             |
