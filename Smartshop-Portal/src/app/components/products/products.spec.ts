import { ComponentFixture, TestBed } from '@angular/core/testing';

import { Products } from './products';
import { provideRouter } from '@angular/router';
import { provideHttpClient } from '@angular/common/http';

describe('Products', () => {
  let component: Products;
  let fixture: ComponentFixture<Products>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Products],
      providers: [
        provideRouter([]),
        provideHttpClient()
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(Products);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
