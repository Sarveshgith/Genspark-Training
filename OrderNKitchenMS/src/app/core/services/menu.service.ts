import { HttpClient, HttpParams } from "@angular/common/http";
import { Injectable } from "@angular/core";
import { baseUrl } from "../enviroment";
import { CategoryModel, MenuItemModel, QueryMenuItemModel } from "../models/menu.model";
import { Observable } from "rxjs";

let categoryUrl = baseUrl + "categories/";
let menuUrl = baseUrl + "menu/";

@Injectable({ providedIn: "root" })
export class MenuService {

    constructor(private http: HttpClient) { }

    getMenuCategories(query?: { isNonVeg?: boolean }): Observable<CategoryModel[]> {
        let params = new HttpParams();
        if (query?.isNonVeg !== undefined && query?.isNonVeg !== null) {
            params = params.set("isNonVeg", query.isNonVeg.toString());
        }
        return this.http.get<CategoryModel[]>(categoryUrl, { params });
    }

    getMenuItems(query?: QueryMenuItemModel): Observable<MenuItemModel[]> {
        let url = menuUrl;
        let params = new HttpParams();
        if (query) {
            if (query.name !== undefined && query.name !== null) {
                params = params.set("name", query.name);
            }
            if (query.categoryId !== undefined && query.categoryId !== null) {
                params = params.set("categoryId", query.categoryId.toString());
            }
            if (query.minPrice !== undefined && query.minPrice !== null) {
                params = params.set("minPrice", query.minPrice.toString());
            }
            if (query.maxPrice !== undefined && query.maxPrice !== null) {
                params = params.set("maxPrice", query.maxPrice.toString());
            }
            if (query.isAvailable !== undefined && query.isAvailable !== null) {
                params = params.set("isAvailable", query.isAvailable.toString());
            }
            if (query.maxPreparationTime !== undefined && query.maxPreparationTime !== null) {
                params = params.set("maxPreparationTime", query.maxPreparationTime.toString());
            }
            if (query.pageNumber !== undefined && query.pageNumber !== null) {
                params = params.set("pageNumber", query.pageNumber.toString());
            }
            if (query.pageSize !== undefined && query.pageSize !== null) {
                params = params.set("pageSize", query.pageSize.toString());
            }
        }
        return this.http.get<MenuItemModel[]>(url, { params });
    }

    // Menu Item CRUD
    createMenuItem(item: { name: string; description: string; price: number; categoryId: number; imageUrl: string; preparationTime: number; isAvailable: boolean }): Observable<MenuItemModel> {
        return this.http.post<MenuItemModel>(menuUrl, item);
    }

    updateMenuItem(id: number, item: { name: string; description: string; price: number; categoryId: number; imageUrl: string; preparationTime: number; isAvailable: boolean }): Observable<MenuItemModel> {
        return this.http.put<MenuItemModel>(`${menuUrl}${id}`, item);
    }

    toggleMenuItemAvailability(id: number, isAvailable: boolean): Observable<void> {
        return this.http.patch<void>(`${menuUrl}${id}/availability`, { isAvailable });
    }

    deleteMenuItem(id: number): Observable<void> {
        return this.http.delete<void>(`${menuUrl}${id}`);
    }

    // Category CRUD
    createCategory(category: { name: string; isNonVeg: boolean }): Observable<CategoryModel> {
        return this.http.post<CategoryModel>(categoryUrl, category);
    }

    updateCategory(id: number, category: { name: string; isNonVeg: boolean }): Observable<CategoryModel> {
        return this.http.put<CategoryModel>(`${categoryUrl}${id}`, category);
    }

    deleteCategory(id: number): Observable<void> {
        return this.http.delete<void>(`${categoryUrl}${id}`);
    }
}