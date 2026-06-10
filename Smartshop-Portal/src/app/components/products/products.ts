import { Component, signal } from '@angular/core';
import { ProductModel } from '../../models/products.model';
import { ProductService } from '../../services/product.service';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-products',
  imports: [RouterLink],
  templateUrl: './products.html',
  styleUrl: './products.css',
})
export class Products {
  products = signal<ProductModel[]>([]);

  constructor(private productService: ProductService) {
    this.productService.getProducts().subscribe({
      next: (response: ProductModel[]) => {
        console.log('Products fetched successfully:', response);
        this.products.set(response);
      },
      error: (error: any) => {
        console.error('Failed to fetch products:', error);
        alert('Failed to fetch products. Please try again later.');
      }
    })
  }
}
