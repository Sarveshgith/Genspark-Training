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
}