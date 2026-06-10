import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ProductDetails } from './product-details';
import { provideRouter } from '@angular/router';
import { provideHttpClient } from '@angular/common/http';

describe('ProductDetails', () => {
  let component: ProductDetails;
  let fixture: ComponentFixture<ProductDetails>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ProductDetails],
      providers: [
        provideRouter([]),
        provideHttpClient()
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(ProductDetails);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
