export interface TableModel {
    id: number;
    number: number;
    status: number;
    statusName: string;
    capacity: number;
    isDeleted: boolean;
    activeOrderId?: number | null;
    activeOrderCreatedAt?: string | Date | null;
}