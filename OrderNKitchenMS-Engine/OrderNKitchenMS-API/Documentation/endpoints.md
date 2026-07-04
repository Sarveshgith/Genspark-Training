# API Endpoints and Usages

This document covers all REST API endpoints exposed by the **Order & Kitchen Management System**, their HTTP methods, authorization requirements, payloads, and response structures.

---

## 1. Authentication Endpoints (`api/auth`)
Handles signup, login, refresh, and anonymous guest sessions.

| HTTP Verb | Route | Auth / Policy | Description |
| :--- | :--- | :--- | :--- |
| `POST` | `/api/auth/register` | `[AllowAnonymous]` | Registers a new user (admin, chef, deliveryman, customer, waiter). |
| `POST` | `/api/auth/login` | `[AllowAnonymous]` | Authenticates credentials and returns access/refresh JWT tokens. |
| `POST` | `/api/auth/guest-login` | `[AllowAnonymous]` | Generates a guest token associated with a specific Table ID (passed as query param `tableId`). |
| `POST` | `/api/auth/refresh` | `[AllowAnonymous]` | Exchanges an expired access token using a valid Refresh Token. |

### DTO Payload Schemas
* **UserRegisterDto**: `{ name, email, password, phoneNumber?, address?, roleId }`
* **UserLoginDto**: `{ email, password }`
* **TokenRefreshDto**: `{ refreshToken }`

---

## 2. User Endpoints (`api/users`)
User profiles management and role retrievals.

| HTTP Verb | Route | Auth / Policy | Description |
| :--- | :--- | :--- | :--- |
| `GET` | `/api/users` | `AdminOnly` | List all users, supporting query filters `Search` and `RoleId`. |
| `GET` | `/api/users/roles` | `AdminOnly` | Retrieves all available roles. |
| `GET` | `/api/users/{id}` | `AdminOnly` | Retrieves a specific user profile by ID. |
| `GET` | `/api/users/me` | `[Authorize]` | Retrieves the profile details of the currently authenticated user. |
| `PUT` | `/api/users/{id}` | `AdminOnly` | Updates user details (name, email, phone, address, role). |
| `PATCH`| `/api/users/{id}/approve`| `AdminOnly` | Approves a registered staff user account. |
| `PATCH`| `/api/users/{id}/role` | `AdminOnly` | Promotes/updates a user's role ID (Admins cannot change their own role). |
| `PATCH`| `/api/users/{id}/password`| `[Authorize]` | Changes the user's password. Users can only update their own password. |
| `DELETE`| `/api/users/{id}` | `AdminOnly` | Soft-deletes a user (sets `IsDeleted` to true). |

---

## 3. Table Endpoints (`api/tables`)
Restaurant layout configuration, seating management, and QR code generations.

| HTTP Verb | Route | Auth / Policy | Description |
| :--- | :--- | :--- | :--- |
| `GET` | `/api/tables` | `[Authorize]` | List all tables. |
| `GET` | `/api/tables/{id}` | `[Authorize]` | Retrieve a specific table by ID. |
| `GET` | `/api/tables/qrcode` | `AdminOrWaiter` | Generates a table session QR code containing the deep link to the guest session (expects query param `tableId`). |
| `POST` | `/api/tables` | `AdminOnly` | Add a new table. |
| `PUT` | `/api/tables/{id}` | `AdminOnly` | Update table properties (number, capacity). |
| `PATCH`| `/api/tables/{id}/status`| `AdminOnly` | Adjust status (Available = 1, Occupied = 2, Reserved = 3). |
| `PATCH`| `/api/tables/{id}/regenerate-secret`| `AdminOnly` | Regenerates/rotates the unique security secret of a table. |
| `DELETE`| `/api/tables/{id}` | `AdminOnly` | Remove/soft-delete a table from service. |

---

## 4. Category Endpoints (`api/categories`)
Menu categories administration.

| HTTP Verb | Route | Auth / Policy | Description |
| :--- | :--- | :--- | :--- |
| `GET` | `/api/categories` | `[AllowAnonymous]` | List all active categories. Filter by `isNonVeg` query param. |
| `GET` | `/api/categories/{id}` | `[AllowAnonymous]` | Get details of a single category. |
| `POST` | `/api/categories` | `AdminOnly` | Create a new category. |
| `PUT` | `/api/categories/{id}` | `AdminOnly` | Update category parameters (Name, IsNonVeg). |
| `DELETE`| `/api/categories/{id}` | `AdminOnly` | Delete a category. |

---

## 5. Menu Endpoints (`api/menu`)
Dishes and drinks catalog management.

| HTTP Verb | Route | Auth / Policy | Description |
| :--- | :--- | :--- | :--- |
| `GET` | `/api/menu` | `[AllowAnonymous]` | List all menu items. Filters: `Search`, `CategoryId`, `IsAvailable`. |
| `GET` | `/api/menu/{id}` | `[AllowAnonymous]` | Get details of a menu item. |
| `POST` | `/api/menu` | `AdminOnly` | Create a new menu item. |
| `PUT` | `/api/menu/{id}` | `AdminOnly` | Update a menu item's details. |
| `PATCH`| `/api/menu/{id}/availability` | `AdminOrChef` | Manually override item availability (`IsManuallyDisabled`). |
| `DELETE`| `/api/menu/{id}` | `AdminOnly` | Soft-deletes a menu item. |

---

## 6. Order Endpoints (`api/orders`)
Orders placing, processing, and assignments.

| HTTP Verb | Route | Auth / Policy | Description |
| :--- | :--- | :--- | :--- |
| `GET` | `/api/orders` | `AdminOnly` | List and paginate all system orders. Filters: `TableId`, `Status`, `From`, `To`. |
| `GET` | `/api/orders/active` | `AllStaff` | Get all active, unpaid orders in the system (neither completed nor cancelled). |
| `GET` | `/api/orders/{id}` | `AdminOnly` | Retrieve details of an order by ID. |
| `GET` | `/api/orders/me` | `CanPlaceOrder` | Get the active order for the table associated with the guest's session token. |
| `POST` | `/api/orders` | `CanPlaceOrder` | Create a new order for the table. Deducts stock. |
| `POST` | `/api/orders/{orderId}/items`| `CanPlaceOrder` | Append new menu items to an existing pending order. Deducts stock. |
| `DELETE`| `/api/orders/{orderId}/items/{itemId}` | `CanPlaceOrder` | Delete a specific order item from a pending order. Restores stock. |
| `PATCH`| `/api/orders/{orderId}/status` | `AllStaff` | Update order status (Pending $\rightarrow$ InPrep $\rightarrow$ Ready $\rightarrow$ Served). Cancellation restores stock. |
| `PATCH`| `/api/orders/{orderId}/assign-chef` | `AdminOrChef` | Assign an order to the currently authenticated Chef (sets status to InPrep). |
| `PATCH`| `/api/orders/{orderId}/assign-waiter` | `CanPlaceOrder` | Assign an order to the currently authenticated Waiter. |
| `GET` | `/api/orders/track` | `GuestSession` | Retrieves live tracking details of the guest's active order. |

* **OrderCreateDto**: `{ tableId, orderItems: [ { menuItemId, quantity, notes } ] }`

---

## 7. Bill Endpoints (`api/bills`)
Checkout invoicing and payment settlements.

| HTTP Verb | Route | Auth / Policy | Description |
| :--- | :--- | :--- | :--- |
| `POST` | `/api/bills` | `AdminOrWaiter` | Generate a new bill for a `Ready` order. |
| `GET` | `/api/bills` | `AdminOnly` | Get all generated bills. |
| `GET` | `/api/bills/order/{orderId}` | `All` | Get billing information for a specific order. |
| `PUT` | `/api/bills/{id}` | `AdminOnly` | Update bill details. |
| `PATCH`| `/api/bills/{id}/status` | `All` | Update status (Pending $\rightarrow$ Paid or Failed). Payment completes order and releases table. |
| `GET` | `/api/bills/order/{orderId}/pdf`| `All` | Generates and downloads the PDF invoice file for the order. |

* **BillCreateDto**: `{ orderId, taxRate, discountAmount }`

---

## 8. Inventory Endpoints (`api/inventory`)
Raw stock elements and recipe mapping (ingredients).

| HTTP Verb | Route | Auth / Policy | Description |
| :--- | :--- | :--- | :--- |
| `GET` | `/api/inventory/items` | `AdminOrChef` | List all raw stock items/ingredients. |
| `GET` | `/api/inventory/items/{id}`| `AdminOrChef` | Get a specific ingredient item. |
| `GET` | `/api/inventory/items/low-stock` | `AdminOrChef` | Retrieve list of items that are below their threshold limit. |
| `POST` | `/api/inventory/items` | `AdminOrChef` | Create a new raw stock item. |
| `PUT` | `/api/inventory/items/{id}`| `AdminOrChef` | Update raw item properties (threshold, stock quantities, cost, units). |
| `PATCH`| `/api/inventory/items/{id}/restock` | `AdminOrChef` | Targeted restock operation to increase stock quantity. |
| `DELETE`| `/api/inventory/items/{id}`| `AdminOrChef` | Soft-deletes/deactivates an item (sets `IsActive = false`). |
| `GET` | `/api/inventory/menu-items/{menuItemId}/ingredients` | `AdminOrChef` | Retrieve recipe mappings (ingredients needed) for a menu item. |
| `POST` | `/api/inventory/menu-items/{menuItemId}/ingredients` | `AdminOrChef` | Add new ingredients mapping to a menu item. |
| `PUT` | `/api/inventory/menu-items/{menuItemId}/ingredients` | `AdminOrChef` | Clear existing ingredients mapping and replace with a new list. |
| `DELETE`| `/api/inventory/menu-items/{menuItemId}/ingredients/{ingredientId}` | `AdminOrChef` | Delete a single mapped ingredient from a menu item. |

* **ItemCreateDto**: `{ name, unit, stockQuantity, stockThreshold, costPerUnit? }`
* **ItemRestockDto**: `{ quantity }`
* **MenuItemIngredientCreateDto**: `[ { itemId, quantityRequired } ]`

---

## 9. Reports Endpoints (`api/reports`)
Provides structured sales analysis, KPIs, and dashboard stats for administrators.

| HTTP Verb | Route | Auth / Policy | Description |
| :--- | :--- | :--- | :--- |
| `GET` | `/api/reports/revenue/daily` | `AdminOnly` | Returns daily total earnings, order counts, and date timestamps (supports optional `date` query filter). |
| `GET` | `/api/reports/revenue/range` | `AdminOnly` | Retrieves daily total revenue splits between a specified start and end date. |
| `GET` | `/api/reports/orders/summary` | `AdminOnly` | Aggregates completed, cancelled, and total order counts, and SLA metrics over a date range. |
| `GET` | `/api/reports/menu/top-items` | `AdminOnly` | Lists top-selling menu items by ordered quantity (supports query parameter `limit`). |
| `GET` | `/api/reports/menu/category-performance` | `AdminOnly` | Evaluates total sales quantities and revenue amounts across each menu category. |
| `GET` | `/api/reports/kitchen/sla` | `AdminOnly` | Audits kitchen performance, showing average preparation times and percentage of orders cooked within targets. |
| `GET` | `/api/reports/tables/turnover` | `AdminOnly` | Summarizes guest counts, tables occupied frequency, and seat utilization metrics on a target date. |

---

## 10. Generative AI Endpoints (`api/genai`)
Integrates Gemini AI to enhance customer experience.

| HTTP Verb | Route | Auth / Policy | Description |
| :--- | :--- | :--- | :--- |
| `GET` | `/api/genai/dish-story` | `[AllowAnonymous]` | Generates a creative background story, nutritional facts, or dining trivia for a menu item (passed as query param `dishName`). |
