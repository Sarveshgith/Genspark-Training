// @feature Chef | KDS Board | Kitchen Display System board tracking active orders in preparation stages (Pending, Prep, Ready).
import { Component, inject, OnInit, signal, computed, OnDestroy } from '@angular/core';
import { OrderService } from '../../../core/services/order.service';
import { OrderModel } from '../../../core/models/order.model';
import { OrderCard } from '../order-card/order-card';
import { AuthService } from '../../../core/services/auth.service';
import { SignalRService } from '../../../core/services/signalr.service';
import { AudioService } from '../../../core/services/audio.service';
import { Subscription } from 'rxjs';

@Component({
  selector: 'app-kds-board',
  standalone: true,
  imports: [OrderCard],
  templateUrl: './kds-board.html',
  styleUrl: './kds-board.css',
})
export class KdsBoard implements OnInit, OnDestroy {
  private orderService = inject(OrderService);
  private authService = inject(AuthService);
  public signalRService = inject(SignalRService);
  private audioService = inject(AudioService);

  public pendingOrders = signal<OrderModel[]>([]);
  public inPrepOrders = signal<OrderModel[]>([]);
  public readyOrders = signal<OrderModel[]>([]);

  public timerMap = signal<{ [orderId: number]: number }>({});

  public unreadCount = signal<number>(0);
  public toastMessage = signal<string | null>(null);
  public errorMessage = signal<string>('');
  
  public currentTime = signal<Date>(new Date());
  public isSummaryOpen = signal<boolean>(false);
  public chefName = signal<string>('Chef');

  private subscriptions = new Subscription();
  private clockIntervalId: any;
  private toastTimeoutId: any;

  public totalActiveOrders = computed(() => {
    return this.pendingOrders().length + this.inPrepOrders().length + this.readyOrders().length;
  });

  public avgInPrepTime = computed(() => {
    const prepOrders = this.inPrepOrders();
    if (prepOrders.length === 0) return '00:00';

    const now = this.currentTime().getTime();
    let totalSeconds = 0;

    prepOrders.forEach(o => {
      const startTime = this.timerMap()[o.id] || new Date(o.createdAt).getTime();
      const diff = Math.floor((now - startTime) / 1000);
      totalSeconds += Math.max(0, diff);
    });

    const avgSeconds = Math.floor(totalSeconds / prepOrders.length);
    const mins = Math.floor(avgSeconds / 60);
    const secs = avgSeconds % 60;
    return `${mins.toString().padStart(2, '0')}:${secs.toString().padStart(2, '0')}`;
  });

  public activeItemSummary = computed(() => {
    const summary: { [key: string]: number } = {};
    const activeList = [...this.pendingOrders(), ...this.inPrepOrders()];
    
    activeList.forEach(order => {
      order.orderItems.forEach(item => {
        summary[item.menuItemName] = (summary[item.menuItemName] || 0) + item.quantity;
      });
    });

    return Object.entries(summary)
      .map(([name, qty]) => ({ name, qty }))
      .sort((a, b) => b.qty - a.qty);
  });

  ngOnInit(): void {
    this.fetchActiveOrders();

    const token = this.authService.getToken() ?? '';
    this.signalRService.connect(token);

    this.subscriptions.add(
      this.authService.user$.subscribe(user => {
        if (user) {
          this.chefName.set(user.name);
        }
      })
    );

    this.subscriptions.add(
      this.signalRService.newOrder$.subscribe({
        next: (newOrder) => {
          if (newOrder.status === 1) {
            this.pendingOrders.update(orders => {
              if (orders.some(o => o.id === newOrder.id)) return orders;
              const updated = [...orders, newOrder];
              return this.sortByAge(updated);
            });
            this.unreadCount.update(c => c + 1);
            this.audioService.playNewOrderChime();
          } else if (newOrder.status === 2) {
            this.inPrepOrders.update(orders => {
              if (orders.some(o => o.id === newOrder.id)) return orders;
              const updated = [...orders, newOrder];
              return this.sortByAge(updated);
            });
            this.timerMap.update(map => ({ ...map, [newOrder.id]: Date.now() }));
          }
        }
      })
    );

    this.subscriptions.add(
      this.signalRService.orderUpdate$.subscribe({
        next: (trackingInfo) => {
          const orderId = trackingInfo.orderId;
          const statusStr = trackingInfo.status;

          if (statusStr === 'Cancelled') {
            this.pendingOrders.update(orders => orders.filter(o => o.id !== orderId));
            this.inPrepOrders.update(orders => orders.filter(o => o.id !== orderId));
            this.readyOrders.update(orders => orders.filter(o => o.id !== orderId));
            this.timerMap.update(map => {
              const newMap = { ...map };
              delete newMap[orderId];
              return newMap;
            });

            this.showToast(`Order #${orderId} cancelled`);
          } 
          else if (statusStr === 'InPrep') {
            if (this.inPrepOrders().some(o => o.id === orderId)) {
              return;
            }
            let foundOrder: OrderModel | undefined;
            this.pendingOrders.update(orders => {
              const idx = orders.findIndex(o => o.id === orderId);
              if (idx > -1) {
                foundOrder = { ...orders[idx], status: 2, statusName: 'InPrep' };
                return orders.filter(o => o.id !== orderId);
              }
              return orders;
            });

            if (foundOrder) {
              const order = foundOrder;
              this.inPrepOrders.update(orders => {
                if (orders.some(o => o.id === orderId)) return orders;
                return this.sortByAge([...orders, order]);
              });
              // Start cooking timer
              this.timerMap.update(map => ({ ...map, [orderId]: Date.now() }));
            } else {
              this.fetchActiveOrders();
            }
          } 
          else if (statusStr === 'Ready') {
            // Avoid redundant update if already processed locally
            if (this.readyOrders().some(o => o.id === orderId)) {
              return;
            }

            // Move from InPrep to Ready
            let foundOrder: OrderModel | undefined;
            this.inPrepOrders.update(orders => {
              const idx = orders.findIndex(o => o.id === orderId);
              if (idx > -1) {
                foundOrder = { ...orders[idx], status: 3, statusName: 'Ready' };
                return orders.filter(o => o.id !== orderId);
              }
              return orders;
            });

            if (foundOrder) {
              const order = foundOrder;
              this.readyOrders.update(orders => {
                if (orders.some(o => o.id === orderId)) return orders;
                return this.sortByAge([...orders, order]);
              });
              // Clear timer
              this.timerMap.update(map => {
                const newMap = { ...map };
                delete newMap[orderId];
                return newMap;
              });
            } else {
              this.fetchActiveOrders();
            }
          } 
          else if (statusStr === 'Served' || statusStr === 'Completed') {
            // Clear from ready column
            this.readyOrders.update(orders => orders.filter(o => o.id !== orderId));
          }
        }
      })
    );

    // Start clock interval for ticking timers & average preparation computations
    this.clockIntervalId = setInterval(() => {
      this.currentTime.set(new Date());
    }, 1000);
  }

  fetchActiveOrders(): void {
    this.orderService.getActiveOrders().subscribe({
      next: (orders) => {
        const sorted = this.sortByAge(orders);
        this.pendingOrders.set(sorted.filter(o => o.status === 1));
        this.inPrepOrders.set(sorted.filter(o => o.status === 2));
        this.readyOrders.set(sorted.filter(o => o.status === 3));

        // Sync existing inPrep order timers
        const currentTimers = { ...this.timerMap() };
        sorted.filter(o => o.status === 2).forEach(o => {
          if (!currentTimers[o.id]) {
            currentTimers[o.id] = new Date(o.createdAt).getTime(); // Fallback to createdAt
          }
        });
        this.timerMap.set(currentTimers);
      },
      error: (err) => {
        console.error('Failed to load active orders', err);
        this.errorMessage.set('Failed to load active orders. Please check connection.');
      }
    });
  }

  handleStartCooking(orderId: number): void {
    // PATCH status=2
    this.orderService.updateOrderStatus(orderId, 2).subscribe({
      next: () => {
        // Move from pending to inPrep immediately for instant local UI update!
        let foundOrder: OrderModel | undefined;
        this.pendingOrders.update(orders => {
          const idx = orders.findIndex(o => o.id === orderId);
          if (idx > -1) {
            foundOrder = { ...orders[idx], status: 2, statusName: 'InPrep' };
            return orders.filter(o => o.id !== orderId);
          }
          return orders;
        });

        if (foundOrder) {
          const order = foundOrder;
          this.inPrepOrders.update(orders => {
            if (orders.some(o => o.id === orderId)) return orders;
            return this.sortByAge([...orders, order]);
          });
          // Start cooking timer
          this.timerMap.update(map => ({ ...map, [orderId]: Date.now() }));
        }
      },
      error: (err) => {
        console.error('Failed to start cooking', err);
        this.errorMessage.set('Failed to transition order status. Please try again.');
      }
    });
  }

  handleMarkReady(orderId: number): void {
    // PATCH status=3
    this.orderService.updateOrderStatus(orderId, 3).subscribe({
      next: () => {
        // Move from InPrep to Ready immediately for instant local UI update!
        let foundOrder: OrderModel | undefined;
        this.inPrepOrders.update(orders => {
          const idx = orders.findIndex(o => o.id === orderId);
          if (idx > -1) {
            foundOrder = { ...orders[idx], status: 3, statusName: 'Ready' };
            return orders.filter(o => o.id !== orderId);
          }
          return orders;
        });

        if (foundOrder) {
          const order = foundOrder;
          this.readyOrders.update(orders => {
            if (orders.some(o => o.id === orderId)) return orders;
            return this.sortByAge([...orders, order]);
          });
          // Clear timer
          this.timerMap.update(map => {
            const newMap = { ...map };
            delete newMap[orderId];
            return newMap;
          });
        }
      },
      error: (err) => {
        console.error('Failed to mark order ready', err);
        this.errorMessage.set('Failed to transition order status. Please try again.');
      }
    });
  }

  resetUnread(): void {
    this.unreadCount.set(0);
  }

  toggleSummary(): void {
    this.isSummaryOpen.update(val => !val);
  }

  private sortByAge(orders: OrderModel[]): OrderModel[] {
    return [...orders].sort((a, b) => new Date(a.createdAt).getTime() - new Date(b.createdAt).getTime());
  }


  private showToast(message: string): void {
    this.toastMessage.set(message);
    if (this.toastTimeoutId) {
      clearTimeout(this.toastTimeoutId);
    }
    this.toastTimeoutId = setTimeout(() => {
      this.toastMessage.set(null);
    }, 4000);
  }

  ngOnDestroy(): void {
    this.subscriptions.unsubscribe();
    if (this.clockIntervalId) {
      clearInterval(this.clockIntervalId);
    }
    if (this.toastTimeoutId) {
      clearTimeout(this.toastTimeoutId);
    }
    this.signalRService.disconnect();
  }
}
