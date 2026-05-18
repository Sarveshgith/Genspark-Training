# MultiTierArchi-NotifApp: 3-Tier Notification System Documentation

## Architecture Overview

### 1. PRESENTATION LAYER (ConsoleUI.cs)
**Functions:**
- `Run()` - Main loop with auto-advance state management
- `AddOrSelectUser()` - Add or select existing user
- `ChooseNotificationType()` - Select Email/SMS
- `CaptureNotificationMessage()` - Input message text
- `SendNotification()` - Trigger send with validation
- `DisplayCurrentDetails()` - Show current user & notification
- `PromptContinue()` - Y/N to auto-advance to next step
- `ShowMenu()` - Display 7 menu options

**Features:**
- ✓ Sequential menu flow (1→2→3→4→5→6→7)
- ✓ Auto-advance on "y" response
- ✓ State management (currentUser, currentNotification)
- ✓ List users / notifications separately
- ✓ Display sent notification with type

---

### 2. BUSINESS LOGIC LAYER (NotificationService.cs)
**Functions:**
- `CreateUser(name, email, phoneNo)` - Validate & store user
- `GetUsers()` - Fetch all users (read-only)
- `PrintUsers()` - Display formatted user list
- `CreateNotification(type, message)` - Validate notification
- `SendNotification(sender, user, notification)` - Execute send
- `GetSender(type, user, message)` - Pick Email/SMS based on rules
- `CreateSender(typeName)` - Use reflection to instantiate sender

**Features:**
- ✓ Message length validation (5+ chars)
- ✓ Email/Phone validation before send
- ✓ SMS limit (160 chars max)
- ✓ Auto-set sent timestamp
- ✓ Polymorphic sender selection

---

### 3. DATA ACCESS LAYER (Repositories)
**Classes:**
- `Repository<T>` - Generic base (Create, GetAll)
- `UserRepository` - User storage + sort by email/name
- `NotificationRepository` - Notification storage + sort by date

**Features:**
- ✓ In-memory List<T> storage
- ✓ Type-safe generic operations
- ✓ No database dependency

---

### 4. MODELS (User.cs, Notification.cs)
- `User` - Name, Email, PhoneNo properties
- `Notification` - NotifType, Message, SentTime properties

### 5. INTERFACES & HELPERS
- `INotificationSender` - SendNotif(User, Notification) contract
- `EmailNotificationSender`, `SmsNotificationSender` - Implementations
- `Validation` - Static helper for email, phone, message, type

---