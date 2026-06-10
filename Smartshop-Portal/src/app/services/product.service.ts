import { HttpClient } from "@angular/common/http";
import { baseUrl } from "../../environment";
import { Injectable } from "@angular/core";
import { ProductDetailModel, ProductModel } from "../models/products.model";
import { catchError, map, throwError } from "rxjs";

@Injectable({ providedIn: 'root' })
export class ProductService {
    constructor(private http: HttpClient){}

    public getProducts(){
        let url = `${baseUrl}/products`;
        return this.http.get<{ products: ProductModel[] }>(url)
            .pipe(
                map(response => response.products),
                catchError(error => {
                    console.error('Failed to fetch products:', error);
                    return throwError(() => new Error('Failed to fetch products. Please try again later.'));
                })
            );
    }
    
    public getProductById(id: number){
        let url = `${baseUrl}/products/${id}`;
        return this.http.get<ProductDetailModel>(url)
            .pipe(
                catchError(error => {
                    console.error('Failed to fetch product:', error);
                    return throwError(() => new Error('Failed to fetch product. Please try again later.'));
                })
            );
    }
}