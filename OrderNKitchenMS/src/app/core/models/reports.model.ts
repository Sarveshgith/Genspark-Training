export interface DailyRevenueModel {
    totalOrders: number;
    totalRevenue: number;
    avgOrderValue: number;
    cancelledOrders: number;
}

export interface RangeRevenueModel {
    date: string;
    totalOrders: number;
    totalRevenue: number;
    avgOrderValue: number;
    cancelledOrders: number;
}

export interface TopSellingItemModel {
    itemName: string;
    category: string;
    totalQtySold: number;
    totalRevenue: number;
}

export interface CategoryPerformanceModel {
    categoryName: string;
    orderCount: number;
    totalRevenue: number;
}

export interface KitchenSlaModel {
    withinSLA: number;
    breachedSLA: number;
    slaPercentage: number;
    avgPrepTimeMinutes: number;
}

export interface TableTurnoverModel {
    tableId: number;
    tableNumber: number;
    completedOrdersCount: number;
}