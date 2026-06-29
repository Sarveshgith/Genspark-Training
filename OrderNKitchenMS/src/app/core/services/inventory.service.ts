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
}
