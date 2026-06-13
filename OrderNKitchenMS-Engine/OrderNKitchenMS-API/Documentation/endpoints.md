# API Endpoints and Usages

This document covers all REST API endpoints exposed by the **Order & Kitchen Management System**, their HTTP methods, authorization requirements, payloads, and response structures.

---

## 1. Authentication Endpoints (`api/auth`)
Handles signup, login, refresh, and anonymous guest sessions.

| HTTP Verb | Route | Auth / Policy | Description |
| :--- | :--- | :--- | :--- |
| `POST` | `/api/auth/register` | `[AllowAnonymous]` | Registers a new user (admin, chef, deliveryman, customer). |
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
| `PATCH`| `/api/users/{id}/password`| `[Authorize]` | Changes the user's password. Users can only update their own password. |
| `DELETE`| `/api/users/{id}` | `AdminOnly` | Soft-deletes a user (sets `IsDeleted` to true). |

---

## 3. Table Endpoints (`api/tables`)
Restaurant layout configuration and seating management.

| HTTP Verb | Route | Auth / Policy | Description |
| :--- | :--- | :--- | :--- |
| `GET` | `/api/tables` | `[Authorize]` | List all tables. |
| `GET` | `/api/tables/{id}` | `[Authorize]` | Retrieve a specific table by ID. |
| `POST` | `/api/tables` | `AdminOnly` | Add a new table. |
| `PUT` | `/api/tables/{id}` | `AdminOnly` | Update table properties (number, capacity). |
| `PATCH`| `/api/tables/{id}/status`| `AdminOnly` | Adjust status (Available = 1, Occupied = 2, Reserved = 3). |
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
| `GET` | `/api/orders/active` | `AdminOrChef` | Get all active, unpaid orders in the system (neither completed nor cancelled). |
| `GET` | `/api/orders/{id}` | `AdminOnly` | Retrieve details of an order by ID. |
| `GET` | `/api/orders/me` | `CanPlaceOrder` | Get the active order for the table associated with the guest's session token. |
| `POST` | `/api/orders` | `CanPlaceOrder` | Create a new order for the table from claims. Deducts stock. |
| `POST` | `/api/orders/{id}/items`| `CanPlaceOrder` | Append new menu items to an existing pending order. Deducts stock. |
| `DELETE`| `/api/orders/{id}/items/{itemId}` | `CanPlaceOrder` | Delete a specific order item from a pending order. Restores stock. |
| `PATCH`| `/api/orders/{id}/status` | `AdminOrChef` | Update order status (Pending $\rightarrow$ InPrep $\rightarrow$ Ready $\rightarrow$ Served). Cancellation restores stock. |
| `PATCH`| `/api/orders/{id}/assign` | `AdminOrChef` | Assign an order to a staff user (e.g., a waiter or chef). |

* **OrderCreateDto**: `{ orderItems: [ { menuItemId, quantity, notes } ] }`

---

## 7. Bill Endpoints (`api/bills`)
Checkout invoicing and payment settlements.

| HTTP Verb | Route | Auth / Policy | Description |
| :--- | :--- | :--- | :--- |
| `POST` | `/api/bills` | `AdminOnly` | Generate a new bill for a `Ready` order. |
| `GET` | `/api/bills` | `AdminOnly` | Get all generated bills. |
| `GET` | `/api/bills/order/{orderId}` | `CanPlaceOrder` | Get billing information for a specific order. |
| `PUT` | `/api/bills/{id}` | `AdminOnly` | Update bill details. |
| `PATCH`| `/api/bills/{id}/status` | `AdminOnly` | Update status (Pending $\rightarrow$ Paid or Failed). Payment completes order and releases table. |
| `GET` | `/api/bills/order/{orderId}/pdf`| `CanPlaceOrder` | Generates and downloads the PDF invoice file for the order. |

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
