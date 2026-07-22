export interface BillSplitDto {
    id: number;
    amount: number;
    statusName: string;
}

export interface BillDto {
    id: number;
    orderId: number;
    subTotal: number;
    taxRate: number;
    discountAmount: number;
    totalAmount: number;
    statusName: string;
    createdAt: string;
    splits: BillSplitDto[];
}

export interface BillCreateDto {
    orderId: number;
    taxRate: number;
    discountAmount: number;
    splitBetweenPeople: number;
}