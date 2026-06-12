# Entity-Relationship (ER) Diagram

This document illustrates the Entity-Relationship schema for the **Order & Kitchen Management System**, utilizing a Mermaid diagram and field explanations.

## Mermaid ER Diagram

```mermaid
erDiagram
    ROLES ||--o{ USERS : "has"
    CATEGORIES ||--o{ MENU_ITEMS : "contains"
    MENU_ITEMS ||--o{ MENU_ITEM_INGREDIENTS : "requires"
    ITEMS ||--o{ MENU_ITEM_INGREDIENTS : "used_in"
    TABLES ||--o{ ORDERS : "associated_with"
    USERS ||--o{ ORDERS : "assigned_to"
    ORDERS ||--o{ ORDER_ITEMS : "has"
    MENU_ITEMS ||--o{ ORDER_ITEMS : "includes"
    ORDERS ||--|| BILLS : "generates"

    USERS {
        int Id PK
        string Name
        string Email
        string PasswordHash
        string PhoneNumber
        string Address
        int RoleId FK
        bool IsDeleted
        DateTime CreatedAt
        DateTime UpdatedAt
    }

    ROLES {
        int Id PK
        int Name "UserRole Enum (Admin=1, Customer=2, Chef=3, Deliveryman=4)"
        DateTime CreatedAt
        DateTime UpdatedAt
    }

    TABLES {
        int Id PK
        int Number "Unique Table Number"
        int Status "TableStatus Enum (Available=1, Occupied=2, Reserved=3)"
        int Capacity
        bool IsDeleted
        DateTime CreatedAt
        DateTime UpdatedAt
    }

    CATEGORIES {
        int Id PK
        string Name
        bool IsNonVeg
        bool IsDeleted
        DateTime CreatedAt
        DateTime UpdatedAt
    }

    MENU_ITEMS {
        int Id PK
        int CategoryId FK
        string Name
        string Description
        decimal Price
        string ImageUrl
        int PreparationTime "in minutes"
        bool IsAvailable
        bool IsManuallyDisabled
        bool IsDeleted
        DateTime CreatedAt
        DateTime UpdatedAt
    }

    MENU_ITEM_INGREDIENTS {
        int Id PK
        int MenuItemId FK
        int ItemId FK
        decimal QuantityRequired
        DateTime CreatedAt
        DateTime UpdatedAt
    }

    ITEMS {
        int Id PK
        string Name
        int Unit "ItemUnit Enum (Grams=1, Milliliters=2, Units=3, Kilograms=4, Liters=5)"
        decimal StockQuantity
        decimal StockThreshold
        decimal CostPerUnit
        bool IsActive
        DateTime CreatedAt
        DateTime UpdatedAt
    }

    ORDERS {
        int Id PK
        int TableId FK
        int AssignedUserId FK "Optional"
        int Status "OrderStatus Enum (Pending=1, InPrep=2, Ready=3, Served=4, Completed=5, Cancelled=6)"
        decimal TotalAmount
        DateTime CompletedAt "Optional"
        DateTime CreatedAt
        DateTime UpdatedAt
    }

    ORDER_ITEMS {
        int Id PK
        int OrderId FK
        int MenuItemId FK
        int Quantity
        decimal UnitPrice
        string Notes
        DateTime CreatedAt
        DateTime UpdatedAt
    }

    BILLS {
        int Id PK
        int OrderId FK "Unique"
        decimal SubTotal
        decimal TaxRate "0.00 to 100.00"
        decimal DiscountAmount
        decimal TotalAmount
        int Status "BillStatus Enum (Pending=1, Paid=2, Failed=3)"
        DateTime CreatedAt
        DateTime UpdatedAt
    }
```

## Entity Descriptions

### 1. Roles
Defines the authorization role levels inside the system.
* **Id**: Primary Key (1 = Admin, 2 = Customer, 3 = Chef, 4 = Deliveryman).
* **Name**: `UserRole` enum representing the name of the role.

### 2. Users
User accounts for customers, admins, chefs, and delivery personnel.
* **RoleId**: Foreign Key mapping to the `Roles` table.
* **IsDeleted**: Soft deletion flag.

### 3. Tables
Physical dining tables available in the restaurant.
* **Number**: Unique number assigned to the table.
* **Status**: `TableStatus` enum indicating whether the table is Available (1), Occupied (2), or Reserved (3).

### 4. Categories
Food/drink classification categories (e.g., Starters, Main Course, Beverages).
* **IsNonVeg**: Boolean helper flag to distinguish vegetarian categories from non-vegetarian categories.

### 5. MenuItems
Dishes and drinks listed on the menu.
* **IsAvailable**: Computed availability based on ingredient stock.
* **IsManuallyDisabled**: Chef or Admin manual toggle to make a dish unavailable.

### 6. Items
Raw inventory items/ingredients (e.g., Flour, Milk, Eggs, Chicken) used to make menu items.
* **Unit**: `ItemUnit` enum for raw stock (Grams, Milliliters, etc.).
* **StockQuantity**: Real-time remaining stock of the ingredient.
* **StockThreshold**: Reorder limit; triggers low-stock alerts.

### 7. MenuItemIngredients
The recipe map that links menu items to raw inventory ingredients.
* **QuantityRequired**: The numeric decimal value of the ingredient consumed when one portion of the menu item is ordered.

### 8. Orders
Table orders placed by customers or waitstaff.
* **Status**: `OrderStatus` tracking (Pending, InPrep, Ready, Served, Completed, Cancelled).
* **TotalAmount**: Running sum of the ordered item prices.

### 9. OrderItems
Individual items requested in an order.
* **Quantity**: Amount ordered.
* **Notes**: Special cooking/service instructions.

### 10. Bills
Billing invoices generated when an order status reaches `Ready`.
* **TotalAmount**: Calculated formula: `SubTotal * (1 + TaxRate/100) - DiscountAmount`.
* **Status**: `BillStatus` tracking (Pending, Paid, Failed).
