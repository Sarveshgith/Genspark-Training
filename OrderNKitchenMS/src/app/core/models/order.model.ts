export interface OrderModel {
    id: number;
    tableId: number;
    tableNumber: number;
    status: number;
    statusName: string;
    totalAmount: number;
    completedAt: Date | null;
    createdAt: Date;
    assignedChefId: number | null;
    assignedChefName: string | null;
    assignedWaiterId: number | null;
    assignedWaiterName: string | null;
    estimatedReadyAt: Date | null;
    orderItems: OrderItemModel[];
}

export interface OrderItemModel {
    id: number;
    orderId: number;
    menuItemId: number;
    menuItemName: string;
    quantity: number;
    unitPrice: number;
    notes: string;
    createdAt: Date;
}

export interface QueryOrderModel {
    status?: number | null;
    tableId?: number | null;
    from?: string | Date | null;
    to?: string | Date | null;
    pageNumber?: number;
    pageSize?: number;
}

export interface GuestOrderTrackingModel {
    orderId: number;
    tableId: number;
    status: string;
    queuePosition: number;
    estimatedReadyAt: Date | string | null;
    estimatedTimeMinutes: number;
    orderItems: OrderItemTrackingModel[];
}

export interface OrderItemTrackingModel {
    menuItemName: string;
    quantity: number;
    notes: string;
}