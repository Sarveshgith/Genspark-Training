import { Component, inject, OnInit, signal, computed, OnDestroy } from '@angular/core';
import { OrderService } from '../../../core/services/order.service';
import { OrderModel } from '../../../core/models/order.model';
import { OrderCard } from '../order-card/order-card';
import { AuthService } from '../../../core/services/auth.service';
import { SignalRService } from '../../../core/services/signalr.service';
import { Subscription } from 'rxjs';

@Component({
  selector: 'app-kds-board',
  imports: [OrderCard],
  templateUrl: './kds-board.html',
  styleUrl: './kds-board.css',
})
export class KdsBoard implements OnInit, OnDestroy {
  private orderService = inject(OrderService);
  public activeOrders = signal<OrderModel[]>([]);
  public errorMessage = signal<string>('');
  private authService = inject(AuthService);
  private signalRService = inject(SignalRService);

  private signalRSubscription!: Subscription;

  public pendingOrders = computed(() => {
    return this.activeOrders().filter(order => order.status === 1); // Pending = 1
  });

  public inPrepOrders = computed(() => {
    return this.activeOrders().filter(order => order.status === 2); // InPrep = 2
  });

  public readyOrders = computed(() => {
    return this.activeOrders().filter(order => order.status === 3); // Ready = 3
  });

  ngOnInit(): void {
    this.fetchActiveOrders();

    const token = this.authService.getToken() ?? '';
    this.signalRService.connect(token);

    this.signalRSubscription = this.signalRService.newOrder$.subscribe({
      next: (newOrder) => {
        this.activeOrders.update(orders => [newOrder, ...orders]);
      }
    });
  }

  fetchActiveOrders(): void {
    this.orderService.getActiveOrders().subscribe({
      next: (orders) => {
        this.activeOrders.set(orders);
      },
      error: (err) => {
        console.error('Failed to load active orders', err);
        this.errorMessage.set('Failed to load active orders. Please check connection.');
      }
    });
  }

  handleStartCooking(orderId: number): void {
    this.orderService.assignChef(orderId).subscribe({
      next: () => {
        this.fetchActiveOrders();
      },
      error: (err) => {
        console.error('Failed to assign chef and start cooking', err);
        this.errorMessage.set('Failed to assign chef and start cooking. Please try again.');
      }
    });
  }

  handleMarkReady(orderId: number): void {
    this.orderService.updateOrderStatus(orderId, 3).subscribe({ // Ready = 3
      next: () => {
        this.fetchActiveOrders();
      },
      error: (err) => {
        console.error('Failed to mark order ready', err);
        this.errorMessage.set('Failed to mark order ready. Please try again.');
      }
    });
  }

  ngOnDestroy(): void {
    if (this.signalRSubscription) {
      this.signalRSubscription.unsubscribe();
    }
    this.signalRService.disconnect();
  }
}
