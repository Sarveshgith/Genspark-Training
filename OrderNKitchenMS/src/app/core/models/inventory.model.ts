export interface InventoryItemModel {
    id: number;
    name: string;
    unit: number;
    unitName: string;
    stockQuantity: number;
    stockThreshold: number;
    costPerUnit: number | null;
    isActive: boolean;
    createdAt: string;
    updatedAt: string | null;
}

export interface MenuItemIngredientModel {
    id: number;
    menuItemId: number;
    itemId: number;
    itemName: string;
    quantityRequired: number;
}
