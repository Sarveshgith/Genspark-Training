import { Component, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { ProductDetailModel } from '../../models/products.model';
import { ProductService } from '../../services/product.service';

@Component({
  selector: 'app-product-details',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './product-details.html',
  styleUrl: './product-details.css',
})
export class ProductDetails {
  product = signal<ProductDetailModel>(null!);
  selectedImage = signal<string>('');
  isLoading = signal<boolean>(true);

  constructor(
    private productService: ProductService,
    private route: ActivatedRoute
  ) {
    const idParam = this.route.snapshot.paramMap.get('id');
    const productId = idParam ? parseInt(idParam, 10) : 1;

    this.productService.getProductById(productId).subscribe({
      next: (response: ProductDetailModel) => {
        console.log('Product details fetched successfully:', response);
        this.product.set(response);

        if (response.thumbnail) {
          this.selectedImage.set(response.thumbnail);
        }
        
        this.isLoading.set(false);
      },
      error: (error: any) => {
        console.error('Failed to fetch product details:', error);
        alert('Failed to fetch product details. Please try again later.');
        this.isLoading.set(false);
      }
    });
  }

  changeDisplayImage(imgUrl: string) {
    this.selectedImage.set(imgUrl);
  }
}