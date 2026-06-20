import { HttpClient, HttpParams } from "@angular/common/http";
import { Injectable } from "@angular/core";
import { baseUrl } from "../enviroment";
import { CategoryModel, MenuItemModel, QueryMenuItemModel } from "../models/menu.model";
import { Observable } from "rxjs";

let categoryUrl = baseUrl + "categories/";
let menuUrl = baseUrl + "menu/";

//Category Query Model
export type isNonVeg = boolean | undefined;

@Injectable({ providedIn: "root" })
export class MenuService {

    constructor(private http: HttpClient) {}

    getMenuCategories(query?: isNonVeg): Observable<CategoryModel[]> {
        let url = categoryUrl;
        let params = new HttpParams();
        if (query !== undefined) {
            params = params.set("isNonVeg", query.toString());
        }
        return this.http.get<CategoryModel[]>(url, { params });
    }

    getMenuItems(query?: QueryMenuItemModel): Observable<MenuItemModel[]> {
}