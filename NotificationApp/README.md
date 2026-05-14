# NotificationApp

A PostgreSQL-backed console application for managing users and sending notifications through email or SMS flows. Built with Entity Framework Core for data access and organized into clear layers: Data, Interface, Repository, Model, Service, and Presentation.

## Architecture Overview

NotificationApp follows a **6-layer architecture** pattern:
1. **Data Layer**: Entity Framework Core DbContext and migrations
2. **Interface Layer**: Repository contracts and service interfaces
3. **Repository Layer**: EF Core-based data access with LINQ queries
4. **Model Layer**: User and Notification entities
5. **Service Layer**: Business logic and validation
6. **Presentation Layer**: Console UI interaction

---

## Layer Checklist

### Data Layer ✓
- [x] `NotifContext` provides Entity Framework Core DbContext for PostgreSQL
- [x] Connection string configured for localhost PostgreSQL database
- [x] Fluent API configuration for entity relationships and constraints
- [x] Automatic migrations via `Database.Migrate()` on application startup
- [x] Cascading delete configured for user-notification relationships

### Interface Layer ✓
- [x] `IRepository<K, T>` provides the shared CRUD contract
- [x] `IUserRepository` extends common contract with lookup methods (GetByEmail, GetByPhone)
- [x] `INotificationRepository` extends common contract with GetByUserId method
- [x] `INotification` keeps sending behavior decoupled from storage
- [x] Contracts are implementation-agnostic

### Repository Layer ✓
- [x] `UserRepository` implements CRUD operations via LINQ to Entities
- [x] `UserRepository` supports lookup by email and phone number
- [x] `NotificationRepository` implements CRUD operations via LINQ to Entities
- [x] `NotificationRepository` supports lookup by user id
- [x] All queries use Entity Framework Core LINQ syntax (no raw SQL)
- [x] Repositories accept DbContext via constructor injection

### Model Layer ✓
- [x] `User` entity with `Id` (primary key), `Name`, `Email`, `PhoneNo` properties
- [x] `Notification` entity with `Id` (primary key), `UserId` (foreign key), `Message`, `NotifType`, `SentDate`
- [x] Models are lightweight and focused on data representation
- [x] No comparison operators or equality overrides in models
- [x] EF Core attributes and conventions handle mapping

### Service Layer ✓
- [x] `NotificationService` orchestrates CRUD operations for users and notifications
- [x] Validation rules applied before data storage
- [x] Constructor injection of repositories for loose coupling
- [x] Methods for creating, updating, deleting, and listing users
- [x] Methods for creating notifications and sending via email/SMS

### Presentation Layer ✓
- [x] `Program.cs` handles application initialization and DbContext setup
- [x] `Program.cs` wires repositories and services with dependency injection
- [x] `InteractService` provides clean console menu and user interaction
- [x] `InteractService` handles all menu options and user input validation
- [x] Separation of concerns: initialization in Program.cs, UI in InteractService

---

## Initialization Flow

The application follows a clean initialization pattern in `Program.cs`:

```csharp
// 1. Create and migrate database
using var context = new NotifContext();
context.Database.Migrate();

// 2. Instantiate repositories with DbContext
var userRepo = new UserRepository(context);
var notifRepo = new NotificationRepository(context);

// 3. Create service with injected repositories
var service = new NotificationService(userRepo, notifRepo);

// 4. Start user interaction through presentation layer
var interact = new InteractService(service);
interact.StartInteraction();
```

This ensures:
- **Separation of Concerns**: Data, business logic, and UI are isolated
- **Loose Coupling**: Services depend on abstractions (interfaces), not concrete classes
- **Testability**: Each layer can be tested independently by mocking dependencies
- **Clean Shutdown**: The `using` statement ensures proper DbContext disposal

---

## Key Features

- [x] **PostgreSQL Persistence** via Entity Framework Core
- [x] **LINQ-Based Queries** for all data access operations
- [x] **Automatic Migrations** applied on startup
- [x] **Layered Architecture** with clear separation of concerns
- [x] **Lookup Support** by email and phone number
- [x] **Notification Records** with user id, message, type, and sent date
- [x] **Console UI** with menu-driven workflow
- [x] **Input Validation** for name, email, and phone number
- [x] **Cascading Deletes** when users are removed
- [x] **Constructor Injection** for all dependencies

---

## Project Structure

```
NotificationApp/
├── Contexts/
│   └── NotifContext.cs              # EF Core DbContext and entity mappings
├── Interfaces/
│   ├── IRepository.cs               # Generic CRUD contract
│   ├── IUserRepository.cs           # User-specific repository contract
│   └── INotificationRepository.cs   # Notification-specific repository contract
├── Repository/
│   ├── UserRepository.cs            # User CRUD and lookups
│   └── NotificationRepository.cs    # Notification CRUD and user lookups
├── Models/
│   ├── User.cs                      # User entity
│   ├── Notification.cs              # Notification entity
│   ├── EmailNotification.cs         # Email sending implementation
│   └── SMSNotification.cs           # SMS sending implementation
├── Services/
│   └── NotificationService.cs       # Business logic orchestration
├── Presentation/
│   └── InteractService.cs           # Console UI and user interaction
├── Program.cs                       # Application entry point and initialization
└── README.md                        # This file
```

---

## Database Schema

### users table
- `id` (integer, primary key)
- `name` (text, required)
- `email` (text, required, unique)
- `phone_no` (text, required, unique)

### notifications table
- `id` (integer, primary key)
- `user_id` (integer, foreign key → users.id, ON DELETE CASCADE)
- `message` (text, required)
- `notif_type` (text, required)
- `sent_date` (timestamp, required)

---

## How to Run

### Prerequisites
- .NET 10.0 SDK or later
- PostgreSQL running on localhost with credentials: `sarvesh` / `Sarvesh_dev@1`

### Setup and Execution

```bash
# Navigate to project directory
cd /Users/sarveswaranb/Proggies/Presidio/Genspark-Training/NotificationApp

# Restore NuGet packages
dotnet restore

# Build the application
dotnet build

# Run the application
dotnet run
```

The application will:
1. Create or connect to the `notif_app` database
2. Apply EF Core migrations to create/update tables
3. Display the console menu for user interaction

---

## Usage Workflow

1. **Start the application** - `dotnet run`
2. **See the menu** with 7 options
3. **Create users** - Option 1 (validates name, email, phone)
4. **List users** - Option 2 (shows all stored users)
5. **Update user** - Option 3 (select by index, update fields)
6. **Delete user** - Option 4 (select by index, cascades delete to notifications)
7. **Send notifications** - Options 5 & 6 (email or SMS)
8. **Exit** - Option 7

---

## Menu Options

```
Notification System
1) Create user
2) List users
3) Update user
4) Delete user
5) Send Email
6) Send SMS
7) Exit
```

---

## Technology Stack

- **.NET**: Version 10.0
- **Language**: C# 12
- **ORM**: Entity Framework Core 10.0.8
- **Database**: PostgreSQL
- **Provider**: Npgsql.EntityFrameworkCore.PostgreSQL 10.0.1
- **Architecture**: Repository Pattern with Dependency Injection, 6-Layer Separation

---

## Design Patterns Used

1. **Repository Pattern**: Abstracts data access logic with IRepository<K,T>
2. **Dependency Injection**: Constructor-based injection of repositories and services
3. **Service Locator Pattern**: NotificationService coordinates multiple operations
4. **Strategy Pattern**: INotification interface allows different sending strategies (Email/SMS)
5. **Domain-Driven Design**: Models represent business entities (User, Notification)

---

## Future Enhancements

1. **Authentication & Authorization**: User login/password with role-based access control
2. **Notification History**: Delivery status tracking and retry logic
3. **Environment Variables**: Move connection string to configuration
4. **Async Operations**: Convert operations to async/await for better scalability
5. **Pagination**: Support for listing large datasets
6. **Reporting**: Statistics and analytics dashboards
7. **Email Templates**: Customizable email and SMS templates
8. **Audit Trails**: Track all user actions with timestamps
9. **Unit Tests**: Comprehensive test coverage for repositories and services
10. **Logging**: Structured logging with Serilog or similar
11. **Web API**: RESTful API endpoints for integration
12. **Background Jobs**: Scheduled notification sending with Hangfire
13. **Import/Export**: Bulk user and notification operations
14. **Data Validation**: Server-side validation with FluentValidation

---

## Development Notes

- The application uses constructor injection for all repositories and services
- Entity Framework Core handles connection pooling automatically
- Migrations are applied automatically on startup
- No raw SQL is used; all queries use LINQ to Entities
- Models are intentionally lightweight without unnecessary methods
- InteractService encapsulates all console UI logic for clean separation

---

## Building and Testing

```bash
# Clean build
dotnet clean && dotnet build

# Run with verbose output
dotnet run --verbose

# Check for code issues
dotnet build --no-restore
```

---

## Contact & Support

For questions or issues, refer to the project documentation or consult the development team.
