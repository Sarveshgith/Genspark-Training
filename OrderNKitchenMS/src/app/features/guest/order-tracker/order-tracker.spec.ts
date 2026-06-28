import { ComponentFixture, TestBed } from '@angular/core/testing';

import { OrderTracker } from './order-tracker';

describe('OrderTracker', () => {
  let component: OrderTracker;
  let fixture: ComponentFixture<OrderTracker>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [OrderTracker],
    }).compileComponents();

    fixture = TestBed.createComponent(OrderTracker);
    component = fixture.componentInstance;
    component.order = { orderId: 123, status: 'Pending', orderItems: [] };
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
