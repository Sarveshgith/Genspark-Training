export interface BillDto {
    id: number;
    orderId: number;
    subTotal: number;
    taxRate: number;
    discountAmount: number;
    totalAmount: number;
    statusName: string;
    createdAt: string;
}

export interface BillCreateDto {
    orderId: number;
    taxRate: number;
    discountAmount: number;
}