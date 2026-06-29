import { Injectable } from "@angular/core";
import { HttpClient } from "@angular/common/http";
import { Observable } from "rxjs";
import { TableModel } from "../models/table.model";
import { baseUrl } from "../enviroment";

@Injectable({ providedIn: 'root' })
export class TableService {

    constructor(private http: HttpClient) { }

    public getTables(): Observable<TableModel[]> {
        return this.http.get<TableModel[]>(baseUrl + "tables");
    }

    public getTableByNumber(tableNumber: number): Observable<TableModel> {
        return this.http.get<TableModel>(baseUrl + "tables/" + tableNumber);
    }

    public createTable(table: { number: number; capacity: number; status?: number }): Observable<TableModel> {
        return this.http.post<TableModel>(baseUrl + "tables", table);
    }

    public updateTable(id: number, table: { number: number; capacity: number }): Observable<TableModel> {
        return this.http.put<TableModel>(baseUrl + "tables/" + id, table);
    }

    public updateTableStatus(id: number, status: number): Observable<void> {
        return this.http.patch<void>(baseUrl + "tables/" + id + "/status", { status });
    }

    public deleteTable(id: number): Observable<void> {
        return this.http.delete<void>(baseUrl + "tables/" + id);
    }
}