import { ComponentFixture, TestBed } from '@angular/core/testing';

import { OrderCard } from './order-card';

describe('OrderCard', () => {
  let component: OrderCard;
  let fixture: ComponentFixture<OrderCard>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [OrderCard],
    }).compileComponents();

    fixture = TestBed.createComponent(OrderCard);
    component = fixture.componentInstance;
    fixture.componentRef.setInput('order', {
      id: 1,
      tableId: 1,
      tableNumber: 5,
      status: 1,
      statusName: 'Pending',
      totalAmount: 25.50,
      completedAt: null,
      createdAt: new Date(),
      assignedChefId: null,
      assignedChefName: null,
      assignedWaiterId: 1,
      assignedWaiterName: 'John Doe',
      estimatedReadyAt: null,
      orderItems: [
        {
          id: 1,
          orderId: 1,
          menuItemId: 10,
          menuItemName: 'Pizza',
          quantity: 2,
          unitPrice: 12.75,
          notes: 'No onions',
          createdAt: new Date()
        }
      ]
    });
    fixture.detectChanges();
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
