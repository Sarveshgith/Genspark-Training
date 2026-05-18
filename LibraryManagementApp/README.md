# Library Management App

Console-based library management system built with .NET, Entity Framework Core, and PostgreSQL.

## Overview

This project is organized into clear layers so each responsibility stays focused:

- `Presentation` handles console input and output.
- `Services` contains business rules and validation.
- `Repository` handles database access.
- `Models` defines the entities stored in the database.
- `Contexts` configures the EF Core database context.
- `Enums` stores fixed value sets used across the app.
- `Interfaces` defines contracts for repositories.
- `Utils` provides shared helper classes.
- `Migrations` tracks database schema changes.
- `Documentation` stores supporting design notes.

## Features

- User login with admin and member flows
- Add and manage members, categories, books, and book copies
- Borrow and return books with overdue fine calculation
- Track overdue borrowings and unpaid fines
- View borrow history and fine history from a single `History` menu
- Pay unpaid fines from the member menu by selecting a fine index
- Custom exceptions for validation and domain errors (clear error messages and types)
- Explicit transaction handling for critical multi-step operations (e.g. borrow)
- Dates are stored in UTC to maintain compatibility with PostgreSQL timestamptz
- Uses a database function (`calculate_member_fine`) for fine calculation, called from the repository

- Admin reports: current borrowings, overdue list, members with pending fines, and "most borrowed" books
- Uses a PostgreSQL database function `get_most_borrowed_books()` (queried from the repository) for the most-borrowed report
- Application logging with Serilog: timestamped, daily-rolling log files are written to `Logs/library-management-.log`
- Service-level informational logs added for major operations (fetch/add/update/process) to aid debugging and audits
- Exception refactor: clearer, domain-specific exception types (e.g. `InvalidInputException`, `ConflictException`, `UnauthorizedException`, `AuthenticationException`, `NotFoundException`)

## Project Structure Checklist

### `Program`

- [x] Starts the application
- [x] Creates the shared `LibraryDbContext`
- [x] Instantiates repositories and services
- [x] Launches the console interaction flow

### `Contexts`

- [x] Configures the PostgreSQL connection
- [x] Exposes `DbSet` properties for all entities
- [x] Defines entity relationships and delete behavior
- [x] Seeds initial admin and membership data

### `Models`

- [x] Represents the database entities
- [x] Defines relationships between members, books, copies, borrows, fines, and memberships
- [x] Holds audit fields such as `CreatedAt`

### `Enums`

- [x] Stores fixed states such as `MemberStatus`, `BorrowStatus`, `BookCopyStatus`, and `MembershipType`
- [x] Keeps status values consistent across services, repositories, and UI prompts

### `Interfaces`

- [x] Defines repository contracts
- [x] Makes data access easier to replace or extend
- [x] Keeps service dependencies predictable

### `Repository`

- [x] Implements the generic base repository
- [x] Handles common CRUD operations
- [x] Contains specialized repositories for books, copies, members, memberships, borrows, categories, and fines
- [x] Executes database reads and writes

### `Services`

- [x] Contains the business logic for members, books, and borrows
- [x] Validates IDs, statuses, limits, and borrowing rules
- [x] Orchestrates multi-step operations such as borrow and return
- [x] Uses explicit transaction handling for borrow operations

### `Presentation`

- [x] Handles the console menus and user prompts
- [x] Routes users into admin or member flows after login
- [x] Displays success and error messages
- [x] Converts console input into service calls

### `Utils`

- [x] Provides shared console helpers and validators
- [x] Keeps repetitive input and output code out of the presentation layer

### `Migrations`

- [x] Stores EF Core migration history
- [x] Describes schema changes over time
- [x] Lets the database be rebuilt consistently

### `Documentation`

- [x] Holds design notes and supporting project documentation

## How The Layers Work Together

1. The user interacts with `Presentation/InteractService`.
2. The presentation layer calls the relevant `Services` class.
3. The service validates the request and applies business rules.
4. The service uses `Repository` classes to read or write data.
5. The repositories use `LibraryDbContext` to persist changes in PostgreSQL.

## Borrow Flow Example

- [x] Read member and book details
- [x] Check membership status
- [x] Check unpaid fines
- [x] Check active borrow limit
- [x] Check whether the same book is already borrowed
- [x] Find an available book copy
- [x] Start a transaction
- [x] Mark the copy as borrowed
- [x] Create the borrow record
- [x] Commit the transaction

## Return Flow Example

- [x] Load the borrow record
- [x] Mark the return date
- [x] Calculate overdue days if needed
- [x] Create a fine when applicable
- [x] Show the fine amount immediately when the returned book is overdue
- [x] Mark the borrow as returned
- [x] Make the book copy available again

## Member History And Fines

- `History` opens a submenu with `Borrow history` and `Fine history`
- `Fine history` shows all fine records for the logged-in member
- `Pay Fine` lists unpaid fines and lets the member pay one by selecting its index

## Reports (Admin)

- Access the `Reports` menu from the admin flow to view:
	- Current active borrowings
	- Overdue borrowings
	- Members with pending fines
	- Most-borrowed books (uses the `get_most_borrowed_books()` DB function)

## Prerequisites

- .NET SDK installed
- PostgreSQL running locally
- Database configured in `Contexts/LibraryDbContext.cs`

## Run The App

```bash
dotnet run
```

## Notes

- Dates are stored using UTC to stay compatible with PostgreSQL `timestamp with time zone` columns.
- The app currently uses a single shared `LibraryDbContext` across repositories and services.
- If you change the schema, add a new EF Core migration instead of editing old migrations.

- Logging: Serilog is configured to emit timestamped logs to console and to daily-rolling files under `Logs/` (file name pattern `library-management-.log`). Keep `Logs/` writable for logging to work.
- Database functions: Some heavy aggregates and fine calculations are implemented as server-side functions and are called from repository code to improve performance and keep reporting logic close to the data.
- Exceptions: The codebase now uses more specific exception types for clearer error handling and better mapping of user-facing messages.