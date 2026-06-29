import { HttpClient } from "@angular/common/http";
import { Injectable } from "@angular/core";
import { baseUrl } from "../enviroment";
import { Observable } from "rxjs";
import { InventoryItemModel, MenuItemIngredientModel } from "../models/inventory.model";

@Injectable({
  providedIn: "root"
})
export class InventoryService {
  private inventoryUrl = baseUrl + "inventory";

  constructor(private http: HttpClient) {}

  public getInventoryItems(): Observable<InventoryItemModel[]> {
    return this.http.get<InventoryItemModel[]>(`${this.inventoryUrl}/items`);
  }

  public getIngredientsForMenuItem(menuItemId: number): Observable<MenuItemIngredientModel[]> {
    return this.http.get<MenuItemIngredientModel[]>(`${this.inventoryUrl}/menu-items/${menuItemId}/ingredients`);
  }

  public createInventoryItem(item: { name: string; unit: number; stockQuantity: number; stockThreshold: number; costPerUnit?: number | null }): Observable<InventoryItemModel> {
    return this.http.post<InventoryItemModel>(`${this.inventoryUrl}/items`, item);
  }

  public updateInventoryItem(id: number, item: { name: string; unit: number; stockQuantity: number; stockThreshold: number; costPerUnit?: number | null; isActive: boolean }): Observable<InventoryItemModel> {
    return this.http.put<InventoryItemModel>(`${this.inventoryUrl}/items/${id}`, item);
  }

  public restockInventoryItem(id: number, quantity: number): Observable<InventoryItemModel> {
    return this.http.patch<InventoryItemModel>(`${this.inventoryUrl}/items/${id}/restock`, { quantity });
  }

  public deactivateInventoryItem(id: number): Observable<void> {
    return this.http.delete<void>(`${this.inventoryUrl}/items/${id}`);
  }

  public getLowStockItems(): Observable<InventoryItemModel[]> {
    return this.http.get<InventoryItemModel[]>(`${this.inventoryUrl}/items/low-stock`);
  }

  public updateMenuItemIngredients(menuItemId: number, ingredients: { itemId: number; quantityRequired: number }[]): Observable<MenuItemIngredientModel[]> {
    return this.http.put<MenuItemIngredientModel[]>(`${this.inventoryUrl}/menu-items/${menuItemId}/ingredients`, ingredients);
  }
}
