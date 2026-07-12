import { Component, inject, OnInit, signal, computed, OnDestroy } from '@angular/core';
import { OrderService } from '../../../core/services/order.service';
import { OrderModel } from '../../../core/models/order.model';
import { OrderCard } from '../order-card/order-card';
import { AuthService } from '../../../core/services/auth.service';
import { SignalRService } from '../../../core/services/signalr.service';
import { AudioService } from '../../../core/services/audio.service';
import { InventoryService } from '../../../core/services/inventory.service';
import { Router } from '@angular/router';
import { KdsDrawer, KdsMessage } from '../kds-drawer/kds-drawer';
import { Subscription } from 'rxjs';

@Component({
  selector: 'app-kds-board',
  standalone: true,
  imports: [OrderCard, KdsDrawer],
  templateUrl: './kds-board.html',
  styleUrl: './kds-board.css',
})
export class KdsBoard implements OnInit, OnDestroy {
  private orderService = inject(OrderService);
  private authService = inject(AuthService);
  public signalRService = inject(SignalRService);
  private audioService = inject(AudioService);
  private inventoryService = inject(InventoryService);
  private router = inject(Router);

  public pendingOrders = signal<OrderModel[]>([]);
  public inPrepOrders = signal<OrderModel[]>([]);
  public readyOrders = signal<OrderModel[]>([]);

  public timerMap = signal<{ [orderId: number]: number }>({});

  public unreadCount = signal<number>(0);
  public toastMessage = signal<string | null>(null);
  public errorMessage = signal<string>('');
  
  public currentTime = signal<Date>(new Date());
  public drawerContent = signal<'inventory' | 'messages' | 'summary' | null>(null);
  public showLogoutConfirm = signal<boolean>(false);
  public isStatsVisible = signal<boolean>(false);
  public chefName = signal<string>('Chef');

  public messages = signal<KdsMessage[]>([
    {
      id: 1,
      text: "Welcome to Ambrosia KDS. Live connection active.",
      timestamp: new Date(),
      read: true
    }
  ]);
  public unreadMessageCount = signal<number>(0);
  public lowStockCount = signal<number>(0);

  private logoutTimeoutId: any;

  private subscriptions = new Subscription();
  private clockIntervalId: any;
  private toastTimeoutId: any;

  public totalActiveOrders = computed(() => {
    return this.pendingOrders().length + this.inPrepOrders().length + this.readyOrders().length;
  });

  public activeOrdersForInventory = computed(() => {
    return [...this.pendingOrders(), ...this.inPrepOrders(), ...this.readyOrders()];
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

  ngOnInit(): void {
    this.fetchActiveOrders();
    this.fetchLowStockCount();

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
            this.audioService.playNotificationChime();
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
          else if (statusStr === 'Pending') {
            this.fetchActiveOrders();
            this.audioService.playNotificationChime();
          }
        }
      })
    );

    this.subscriptions.add(
      this.signalRService.adminAlert$.subscribe({
        next: (alert) => {
          const text = alert.message || `Alert: ${alert.itemName || 'item'} is low on stock!`;
          this.messages.update(msgs => [
            {
              id: Date.now() + Math.random(),
              text: text,
              timestamp: new Date(),
              read: this.drawerContent() === 'messages',
              orderId: alert.orderId || undefined,
              tableId: alert.tableId || undefined
            },
            ...msgs
          ]);
          if (this.drawerContent() !== 'messages') {
            this.unreadMessageCount.update(c => c + 1);
          }
          this.audioService.playNewOrderChime();
          this.fetchLowStockCount();
        }
      })
    );

    this.subscriptions.add(
      this.signalRService.kitchenMessage$.subscribe({
        next: (notif) => {
          this.messages.update(msgs => [
            {
              id: Date.now() + Math.random(),
              text: `${notif.title}: ${notif.message}`,
              timestamp: new Date(notif.createdAt || Date.now()),
              read: this.drawerContent() === 'messages',
              orderId: notif.orderId || undefined,
              tableId: notif.tableId || undefined
            },
            ...msgs
          ]);
          if (this.drawerContent() !== 'messages') {
            this.unreadMessageCount.update(c => c + 1);
          }
          this.audioService.playNewOrderChime();
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
    // Assign chef and transition status to InPrep (2)
    this.orderService.assignChef(orderId).subscribe({
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

  fetchLowStockCount(): void {
    this.inventoryService.getLowStockItems().subscribe({
      next: (items) => {
        this.lowStockCount.set(items.length);
      },
      error: (err) => {
        console.error('Failed to load low stock count', err);
      }
    });
  }

  openDrawer(content: 'inventory' | 'messages' | 'summary'): void {
    // If tapping the already open tab, close it
    if (this.drawerContent() === content) {
      this.closeDrawer();
      return;
    }
    
    this.drawerContent.set(content);
    if (content === 'messages') {
      this.markAllMessagesRead();
    } else if (content === 'inventory') {
      this.fetchLowStockCount();
    }
  }

  closeDrawer(): void {
    this.drawerContent.set(null);
  }

  openOrder(orderId: number): void {
    this.closeDrawer();
    
    // Smooth scroll to the card element
    setTimeout(() => {
      const element = document.getElementById(`order-card-${orderId}`);
      if (element) {
        element.scrollIntoView({ behavior: 'smooth', block: 'center' });
        // Pulse the element by adding a class temporarily
        element.classList.add('pulse-highlight');
        setTimeout(() => {
          element.classList.remove('pulse-highlight');
        }, 3000);
      }
    }, 100);
  }

  markAllMessagesRead(): void {
    this.messages.update(msgs => msgs.map(m => ({ ...m, read: true })));
    this.unreadMessageCount.set(0);
  }

  triggerLogout(): void {
    this.showLogoutConfirm.set(true);
    if (this.logoutTimeoutId) {
      clearTimeout(this.logoutTimeoutId);
    }
    this.logoutTimeoutId = setTimeout(() => {
      this.showLogoutConfirm.set(false);
    }, 3000);
  }

  confirmLogout(): void {
    if (this.logoutTimeoutId) {
      clearTimeout(this.logoutTimeoutId);
    }
    this.showLogoutConfirm.set(false);
    this.authService.logout();
    this.router.navigate(['/login']);
  }

  cancelLogout(): void {
    if (this.logoutTimeoutId) {
      clearTimeout(this.logoutTimeoutId);
    }
    this.showLogoutConfirm.set(false);
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
    if (this.logoutTimeoutId) {
      clearTimeout(this.logoutTimeoutId);
    }
    this.signalRService.disconnect();
  }
}
