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

## 3. Guest Flow

This flow allows restaurant customers to scan a table QR code and interact with the ordering system anonymously.

```mermaid
sequenceDiagram
    actor Guest as Customer (Guest)
    participant QR as Table QR Code / URL
    participant UI as Angular Frontend
    participant API as Auth / Orders Controller

    Guest->>QR: Scan QR Code (e.g., Table 5)
    QR->>UI: Redirect to Table ordering page (?tableId=5)
    UI->>API: Guest Login Request (POST api/auth/guest-login?tableId=5)
    API-->>UI: Return Guest JWT Session Token (TableId=5 claim)
    Note over UI: Store Guest Token in Local Storage

    UI->>API: Fetch Menu (GET api/menu?isAvailable=true)
    API-->>UI: Return list of available dishes
    Guest->>UI: Browse & add dishes to cart
    Guest->>UI: Confirm and Place Order
    UI->>API: Create Order (POST api/orders)
    Note over API: Extracts TableId=5 from Token claims
    API-->>UI: 200 OK (Order Created)

    loop Track Status
        UI->>API: Get my table's active order (GET api/orders/me)
        API-->>UI: Return Order status (Pending, InPrep, Ready, Served)
    end

    Guest->>UI: Request Bill
    UI->>API: Download Bill PDF (GET api/bills/order/{orderId}/pdf)
    API-->>Guest: PDF download
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
