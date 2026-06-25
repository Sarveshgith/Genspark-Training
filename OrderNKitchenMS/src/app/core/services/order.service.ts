import { HttpClient, HttpParams } from "@angular/common/http";
import { Injectable } from "@angular/core";
import { baseUrl } from "../enviroment";
import { OrderModel, QueryOrderModel, GuestOrderTrackingModel } from "../models/order.model";
import { TableModel } from "../models/table.model";
import { Observable } from "rxjs";

let classUrl = baseUrl + "orders/";

@Injectable({ providedIn: "root" })
export class OrderService {

    constructor(private http: HttpClient) {}

    public getTables(): Observable<TableModel[]> {
        return this.http.get<TableModel[]>(baseUrl + "tables");
    }

    public createOrder(orderCreateDto: { tableId: number; orderItems: { menuItemId: number; quantity: number; notes: string }[] }): Observable<OrderModel> {
        return this.http.post<OrderModel>(classUrl, orderCreateDto);
    }

    public getActiveOrders(): Observable<OrderModel[]> {
        let url = classUrl + "active";
        return this.http.get<OrderModel[]>(url);
    }

    public getOrders(query?: QueryOrderModel): Observable<OrderModel[]> {
        let params = new HttpParams();
        if (query) {
            if (query.status !== undefined && query.status !== null) {
                params = params.set('status', query.status.toString());
            }
            if (query.tableId !== undefined && query.tableId !== null) {
                params = params.set('tableId', query.tableId.toString());
            }
            if (query.from) {
                params = params.set('from', query.from instanceof Date ? query.from.toISOString() : query.from);
            }
            if (query.to) {
                params = params.set('to', query.to instanceof Date ? query.to.toISOString() : query.to);
            }
            if (query.pageNumber !== undefined && query.pageNumber !== null) {
                params = params.set('pageNumber', query.pageNumber.toString());
            }
            if (query.pageSize !== undefined && query.pageSize !== null) {
                params = params.set('pageSize', query.pageSize.toString());
            }
        }
        return this.http.get<OrderModel[]>(classUrl, { params });
    }

    public trackOrder(): Observable<GuestOrderTrackingModel> {
        let url = classUrl + "track";
        return this.http.get<GuestOrderTrackingModel>(url);
    }

    public getActiveOrderByTableId(tableId: number): Observable<OrderModel> {
        let url = `${classUrl}table/${tableId}/active`;
        return this.http.get<OrderModel>(url);
    }

    public updateOrderStatus(orderId: number, status: number): Observable<void> {
        let url = `${classUrl}${orderId}/status`;
        return this.http.patch<void>(url, status);
    }

    public assignChef(orderId: number): Observable<void> {
        let url = `${classUrl}${orderId}/assign-chef`;
        return this.http.patch<void>(url, {});
    }
}
