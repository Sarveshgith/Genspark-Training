# NotificationApp

A PostgreSQL-backed console application for managing users and sending notifications through email or SMS flows. The project is organized into clear layers so the database, repository, service, and UI concerns stay separated.

## Layer Checklist

### Data Layer
- [x] `DbConnectionFactory` centralizes PostgreSQL connection creation.
- [x] `DatabaseInitializer` creates the required tables on startup.
- [x] Database schema supports persistent users and notifications.
- [x] Connection settings are isolated in one place for easy environment updates.

### Interface Layer
- [x] `IRepository<K, T>` provides the shared CRUD contract.
- [x] `IUserRepository` extends the common contract with user-specific lookup methods.
- [x] `INotificationRepository` extends the common contract with notification-specific query methods.
- [x] `INotification` keeps the sending behavior decoupled from storage.

### Repository Layer
- [x] `UserRepository` performs CRUD for users.
- [x] `UserRepository` supports lookup by email and phone number.
- [x] `NotificationRepository` performs CRUD for notifications.
- [x] `NotificationRepository` supports lookup by user id.
- [x] Repository code uses SQL instead of in-memory collections.

### Model Layer
- [x] `User` stores `Id`, `Name`, `Email`, and `PhoneNo`.
- [x] `Notification` stores `UserId`, `Message`, `NotifType`, and `SentDate`.
- [x] Models are kept lightweight and focused on data representation.
- [x] Compare/equality helper methods were removed to keep the models simpler.

### Service Layer
- [x] `NotificationService` coordinates user creation, updates, deletes, and listing.
- [x] `NotificationService` coordinates notification creation and sending.
- [x] Validation rules are applied before data is stored.
- [x] Lookup helpers are exposed for email and phone-based retrieval.

### Presentation Layer
- [x] `Program.cs` provides the console menu and user flow.
- [x] Startup initializes the database before user interaction begins.
- [x] Users can create, list, update, and delete records from the console.
- [x] Users can send email or SMS notifications from the console.

## Key Features

- [x] PostgreSQL persistence for users and notifications.
- [x] Separate read and write responsibilities across layers.
- [x] Lookup support by email and phone number.
- [x] Notification records include user id, message, type, and sent date.
- [x] Console-driven workflow with clear menu options.
- [x] Validation for name, email, and phone number inputs.
- [x] Cascading delete behavior between users and notifications.

## Database Tables

- [x] `users` table stores identity, contact data, and timestamps.
- [x] `notifications` table stores the message, notification type, sent date, and owning user id.

## How to Run

```bash
cd /Users/sarveswaranb/Proggies/Presidio/Genspark-Training/NotificationApp
dotnet restore
dotnet build
dotnet run
```

## Notes

- [x] The application uses PostgreSQL via `Npgsql`.
- [x] The data layer is intentionally separated from the service and presentation layers.
- [x] The current console UI is suitable for demonstration and classroom-style workflows.
