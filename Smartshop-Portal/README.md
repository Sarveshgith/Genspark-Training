# Smartshop Portal

A clean, minimalist Angular portal utilizing the [DummyJSON API](https://dummyjson.com) to provide user authentication, dynamic profile management, and a catalog display of products and product details.

---

## Features

### 1. Dashboard Component (`/`)
* **Welcome Message**: Displays the dynamic welcome message using the current logged-in user's name via an RxJS `BehaviorSubject`.
* **Menu Navigation**: Clean, minimalist links to explore:
  * **Products Catalog**
  * **Product Details** (direct routes)
  * **Profile Details**

### 2. Header Component (`app-header`)
* **Visibility**: Only rendered when a user session is active.
* **Dynamic Salutation**: Displays `Hello, <logged-in-user-firstname>` using RxJS.
* **Logout Button**: Logs out the current session and clears the session cache.

### 3. Session Persistence (`sessionStorage`)
* Leverages browser `sessionStorage` in `AuthService` so that sessions persist across tab refreshes and direct URL entries, but securely clean up when the browser tab is closed.

### 4. Products Catalog & Details (`/products`, `/product/:id`)
* **Products**: A minimalist, responsive grid showcasing product catalog thumbnails, price tags, ratings, and view details triggers.
* **Details**: Specific details page showing a product image gallery switcher, categories, ratings, descriptions, real-time stock warning levels, and add-to-basket logic.

### 5. Profile Page (`/profile`)
* Displays current user account details including First Name, Last Name, User ID, Gender, and Email, as well as mock activity summaries.

---

## Technical Stack & Configuration
* **Framework**: Angular v21.2+ (Standalone components, Signals-based forms, dynamic routes).
* **Styles**: Minimalist utility styling utilizing Tailwind CSS.
* **Test Suite**: Vitest configured with Angular TestBed environments.

---

## Commands & Development

### Setup
Ensure dependencies are installed:
```bash
npm install
```

### Run Server
Start a local Angular development server:
```bash
ng serve
```
Navigate to `http://localhost:4200/`.

### Production Build
Compile and bundle the production files in the `/dist` directory:
```bash
npm run build
```
