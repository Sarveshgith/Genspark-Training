# Main System Workflows

This document maps out the core business workflows of the **Order & Kitchen Management System**.

---

## 1. Core Order Lifecycle Workflow

The following sequence diagram illustrates the lifecycle of a dining table order from creation to payment, highlighting stock changes and state transitions.

```mermaid
sequenceDiagram
    autonumber
    actor Guest as Guest/Waiter
    participant API as Order & Bills Controller
    participant Service as Order/Bill Service
    participant ItemServ as Item Service
    participant DB as AppDbContext (PostgreSQL)

    Guest->>API: Place Order (TableId, MenuItemIds, Quantities)
    API->>Service: CreateOrderAsync(tableId, orderDto)
    Service->>ItemServ: ValidateStockForOrderItemsAsync(items)
    ItemServ->>DB: Check active items & stock availability
    DB-->>ItemServ: Return items stock
    alt Insufficient Stock / Inactive Ingredient
        ItemServ-->>Service: Throw BusinessRuleException
        Service-->>Guest: 400 Bad Request
    else Stock is Valid
        Service->>DB: Begin DB Transaction
        Service->>DB: Change Table Status -> Occupied
        Service->>DB: Create Order Entity (Status = Pending)
        Service->>ItemServ: UpdateStockByMenuItemIdAsync(Id, Qty, isAdd=false)
        ItemServ->>DB: Deduct ingredient StockQuantity
        Service->>DB: SaveChangesAsync()
        Service->>DB: Commit Transaction
        DB-->>Guest: 200 OK (OrderDto)
    end

    Note over Guest, DB: Order is visible in Kitchen Queue (OrderStatus = Pending)

    actor Chef
    Chef->>API: Patch Order Status -> InPrep (Cooking)
    API->>DB: Save new status
    Chef->>API: Patch Order Status -> Ready (Cooked)
    API->>DB: Save new status

    Note over Guest, DB: Bill can now be generated (Only for status = Ready)

    actor Waiter
    Waiter->>API: Patch Order Status -> Served (On Table)
    API->>DB: Save new status

    actor Admin
    Admin->>API: Create Bill (OrderId, Tax, Discount)
    API->>Service: CreateBillAsync()
    Service->>DB: Save Bill Entity (Status = Pending)
    DB-->>Admin: 200 OK (BillDto)

    Admin->>API: Patch Bill Status -> Paid
    API->>Service: UpdateBillStatusAsync(billId, "Paid")
    Service->>Service: Update Order Status -> Completed
    Service->>DB: Save Status Changes
    Note over DB: Trigger release_table_on_order_complete fires in DB
    DB->>DB: Set Table Status -> Available
    Service-->>Admin: 200 OK (Settled)
```

### Order Item Modifications & Cancellations
* **Adding Items**: Done while the order is `Pending`. Calls `AddOrderItemsAsync()`, validates stock, deducts stock for new items, and increases the order's `TotalAmount`.
* **Removing Items**: Done while the order is `Pending`. Calls `RemoveOrderItemAsync()`, restores stock, deletes the item, and subtracts from the order's `TotalAmount`.
* **Cancellation**: If an order is cancelled (`Status = Cancelled`) while `Pending` or `InPrep`, the `OrderService` iterates over all ordered items and calls `UpdateStockByMenuItemIdAsync(..., isAdd=true)` to restore ingredient quantities.

---

## 2. Stock and Inventory Workflow

The inventory system manages raw ingredients (e.g., flour, milk, chicken) and maps them to menu items.

```mermaid
flowchart TD
    A[Admin creates raw Item] --> B[Admin maps Items to MenuItem via MenuItemIngredient]
    B --> C{StockQuantity > RequiredQuantity?}
    C -- No --> D[MenuItem IsAvailable = false]
    C -- Yes --> E[MenuItem IsAvailable = true]

    F[Guest places Order] --> G[Deduct Item StockQuantity]
    G --> H{StockQuantity < StockThreshold?}
    H -- Yes --> I[Trigger Low Stock Alert]
    H -- No --> J[No Action]

    G --> K{StockQuantity < RequiredQuantity for next Order?}
    K -- Yes --> L[Propagate: Re-evaluate MenuItem IsAvailable -> false]
    K -- No --> M[MenuItem remains Available]

    N[Order Cancelled] --> O[Restore Item StockQuantity]
    O --> P[Propagate: Re-evaluate MenuItem IsAvailable -> true]
```

### Key Behaviors:
1. **Low-Stock Alerts**: When an item's `StockQuantity` falls below its `StockThreshold`, it shows in the `/items/low-stock` endpoint.
2. **Propagated Availability Check**: When stock values change (during ordering, restocking, item updates), all menu items mapping to that ingredient are re-evaluated. If any mapped ingredient is insufficient or deactivated, the `MenuItem`'s `IsAvailable` flag is set to `false`.

---

## 3. Guest & Waiter Interaction Flow (Updated)

In this revised model, **Guest Sessions** are read-only and restricted to live food tracking and requesting assistance, while the **Waiter** handles order placement and modifications. Real-time updates are pushed dynamically to the Guest UI using SignalR.

```mermaid
sequenceDiagram
    autonumber
    actor Guest as Customer (Guest)
    actor Waiter as Waiter (Staff)
    actor Chef as Chef (Kitchen)
    participant UI as Guest Mobile App / UI
    participant API as Order & Kitchen API
    participant SigR as SignalR Hub
    actor Admin as Admin (Office)

    Guest->>UI: Scan Table QR (e.g., Table 5)
    UI->>API: Guest Login (POST api/auth/guest-login?tableId=5)
    API-->>UI: Return Guest JWT (TableId=5 claim)
    UI->>SigR: Connect to SignalR Hub (Table 5 Room)

    Guest->>Waiter: Call Waiter & Verbally Order Dishes
    Waiter->>API: Create Order (POST api/orders) with TableId=5 (WaiterId in JWT)
    API->>API: Save Order (Status = Pending, Compute EstimatedReadyAt)
    API-->>Waiter: 200 OK (OrderDto)
    API->>SigR: Broadcast order_updated Event
    SigR-->>UI: Real-Time Order Info (Estimated time, queue pos, items)

    Note over Guest, Chef: Order is in Chef's pending queue

    Chef->>API: Start Cooking (PATCH api/orders/{id}/assign-chef) (ChefId in JWT)
    API->>API: Save Order (Status = InPrep)
    API->>SigR: Broadcast order_updated Event
    SigR-->>UI: Status changes to InPrep (Cooking)

    Chef->>API: Finish Cooking (PATCH api/orders/{id}/status) [Status = Ready]
    API->>API: Save Order (Status = Ready)
    API->>SigR: Broadcast order_updated Event
    SigR-->>UI: Status changes to Ready

    Waiter->>API: Deliver Food to Table (PATCH api/orders/{id}/status) [Status = Served]
    API->>API: Save Order (Status = Served)
    API->>SigR: Broadcast order_updated Event
    SigR-->>UI: Status changes to Served

    Note over Guest: Customer Eats Food

    Guest->>UI: Click "Request Assistance" (e.g., call waiter or ask for bill)
    UI->>SigR: Send request_assistance Message
    SigR-->>Waiter: Notify Waiter (Real-time alert on Waiter's device)

    Waiter->>API: Generate Bill (POST api/bills/order/{orderId})
    API->>API: Create Bill Entity (Status = Pending)
    API-->>Waiter: Bill generated
    API->>SigR: Broadcast bill_generated Event
    SigR-->>UI: UI transitions to Bill Paying Page (UPI QR / Card)

    Guest->>UI: Scan UPI QR & Complete Payment
    Admin->>API: Update Bill Status -> Paid (PATCH api/bills/{id}/status) [Paid]
    API->>API: Update Order Status -> Completed, release table -> Available
    API-->>Admin: 200 OK
    API->>SigR: Broadcast bill_paid Event
    SigR-->>UI: UI shows Payment Successful page, logs out Guest
```

---

## 4. Admin Flow

Administrators manage system configuration and monitor performance.

```mermaid
flowchart LR
    A[Admin Login] --> B[Admin Dashboard]
    B --> C[Manage Users & Roles]
    B --> D[Manage Tables Setup]
    B --> E[Manage Menu & Ingredients]
    B --> F[Billing and Payments Settlement]
    B --> G[Reports & Analytics]

    G --> G1[Daily Revenue]
    G --> G2[Kitchen SLAs / Prep times]
    G --> G3[Top-selling Menu items]
```
* **User Management**: Creating and updating accounts for Chefs, Waiters, or Admins.
* **Table Setup**: Modifying the table numbers, capacities, or soft-deleting unused tables.
* **Menu Adjustments**: Adding new menu items, updating pricing, and manually overriding availability using `IsManuallyDisabled`.

---

## 5. Core Stability & Unique Features

The system implements several unique mechanisms to ensure business logic consistency, billing fairness, and database safety:

### Strict Status Transition Lock
Order status updates follow a strictly enforced chain: `Pending` $\rightarrow$ `In Prep` (Cooking) $\rightarrow$ `Ready` $\rightarrow$ `Served` $\rightarrow$ `Completed`.
The system blocks arbitrary jumps (e.g. going from `Pending` straight to `Completed`, which would bypass the kitchen completely). Cancellations are also guarded and only allowed for orders that have not yet been finalised.

### Transaction-Safe Atomic Stock Protection
To prevent multiple parallel orders from "double-selling" the same raw ingredients when stock is low, ingredient deduction is performed as a single-operation database query. This ensures that stock is updated atomically; if another order deducts the last of an ingredient a millisecond earlier, the simultaneous order will immediately fail with a stock error instead of causing negative inventory.

### Price Lock Protection (Historical Billing)
To ensure customer trust, the price of each item is snapshot and saved on the order at the moment of placement. If an admin updates a menu item's price in the system while a guest is dining, the guest's final bill is calculated using the original prices from their order snapshot rather than the updated menu prices.

### Other Unique Features
* **Auto-Release Table on Payment**: Automatically frees table resources immediately when a bill is paid.
* **Propagated Availability Check**: Auto-updates menu availability whenever stock quantities or recipe ingredients change.
* **Real-time Room Broadcasting**: Pushes instant state updates to guests, waiters, and chefs.
* **Estimated Ready Time Calculator**: Computes preparation times dynamically using active queue position.
* **Table-Bound Session Security**: Restricts guest tokens to accessing details only for their specific table.
* **Low-Stock Alerting**: Proactively flags ingredients when stock drops below threshold values.
* **Soft-Delete Safety Guards**: Retains database reference integrity by soft-deleting entities.
