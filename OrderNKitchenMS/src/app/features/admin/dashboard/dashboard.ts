import { Component, inject, OnInit, OnDestroy, signal, computed, ChangeDetectorRef, NgZone } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { TableService } from '../../../core/services/table.service';
import { OrderService } from '../../../core/services/order.service';
import { BillService } from '../../../core/services/bill.service';
import { InventoryService } from '../../../core/services/inventory.service';
import { SignalRService } from '../../../core/services/signalr.service';
import { AuthService } from '../../../core/services/auth.service';
import { TableModel } from '../../../core/models/table.model';
import { OrderModel } from '../../../core/models/order.model';
import { BillDto } from '../../../core/models/bill.model';
import { InventoryItemModel } from '../../../core/models/inventory.model';
import { Subscription } from 'rxjs';

interface AlertLog {
  id: string;
  type: 'order' | 'bill' | 'call' | 'stock';
  message: string;
  timestamp: Date;
  tableId?: number;
  orderId?: number;
  amount?: number;
  flaggedBy?: string;
}

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.css'
})
export class Dashboard implements OnInit, OnDestroy {
  private tableService = inject(TableService);
  private orderService = inject(OrderService);
  private billService = inject(BillService);
  private inventoryService = inject(InventoryService);
  public signalRService = inject(SignalRService);
  private authService = inject(AuthService);
  private cdr = inject(ChangeDetectorRef);
  private zone = inject(NgZone);
  private router = inject(Router);

  // Core data states
  public tables = signal<TableModel[]>([]);
  public activeOrders = signal<OrderModel[]>([]);
  public bills = signal<BillDto[]>([]);
  public lowStockItems = signal<InventoryItemModel[]>([]);
  public alerts = signal<AlertLog[]>([]);

  // Selected Table details for slide-out drawer
  public selectedTableId = signal<number | null>(null);
  public selectedTableDetails = signal<TableModel | null>(null);
  public selectedTableOrder = signal<OrderModel | null>(null);
  public selectedTableBill = signal<BillDto | null>(null);
  public isDrawerLoading = signal<boolean>(false);

  // Time triggers
  public timeTrigger = signal<number>(Date.now());
  public Math = Math;
  private timeIntervalId: any;
  private simulationIntervalId: any;

  // Active Alert Feed tab state
  public activeFeedTab = signal<'alerts' | 'stock'>('alerts');

  private subscriptions: Subscription = new Subscription();

  // Computed Metric Cards
  public availableTablesCount = computed(() => {
    return this.tables().filter(t => t.status === 1).length;
  });

  public occupiedTablesCount = computed(() => {
    return this.tables().filter(t => t.status === 2).length;
  });

  public activeOrdersCount = computed(() => {
    return this.activeOrders().length;
  });

  public pendingBillsCount = computed(() => {
    return this.bills().filter(b => b.statusName === 'Pending').length;
  });

  public totalRevenueToday = computed(() => {
    return this.bills()
      .filter(b => b.statusName === 'Paid')
      .reduce((sum, b) => sum + b.totalAmount, 0);
  });

  public formattedRevenue = computed(() => {
    const rev = this.totalRevenueToday();
    if (rev >= 1000) {
      return `$${(rev / 1000).toFixed(1)}K`;
    }
    return `$${rev.toFixed(2)}`;
  });

  // Table Seating Map layout categorization
  public activeTablesSorted = computed(() => {
    return [...this.tables()].sort((a, b) => a.number - b.number);
  });

  ngOnInit(): void {
    this.fetchInitialData();
    this.setupSignalR();

    // Start UI elapsed time updates
    this.timeIntervalId = setInterval(() => {
      this.timeTrigger.set(Date.now());
    }, 10000);

    // Simulate occasional random guest waiter calls (water, napkins, assistance) to make the dashboard feel active
    this.startWaiterCallSimulation();
  }

  ngOnDestroy(): void {
    this.subscriptions.unsubscribe();
    if (this.timeIntervalId) {
      clearInterval(this.timeIntervalId);
    }
    if (this.simulationIntervalId) {
      clearInterval(this.simulationIntervalId);
    }
  }

  public fetchInitialData(): void {
    // 1. Fetch tables
    this.tableService.getTables().subscribe({
      next: (data) => {
        this.zone.run(() => {
          this.tables.set(data.filter(t => !t.isDeleted));
          this.cdr.detectChanges();
        });
      },
      error: (err) => console.error('Failed to fetch dashboard tables:', err)
    });

    // 2. Fetch active orders
    this.orderService.getActiveOrders().subscribe({
      next: (orders) => {
        this.zone.run(() => {
          this.activeOrders.set(orders);
          this.cdr.detectChanges();
        });
      },
      error: (err) => console.error('Failed to fetch active orders:', err)
    });

    // 3. Fetch bills
    this.billService.getAllBills().subscribe({
      next: (data) => {
        this.zone.run(() => {
          this.bills.set(data);
          this.cdr.detectChanges();
        });
      },
      error: (err) => console.error('Failed to fetch bills list:', err)
    });

    // 4. Fetch low stock items
    this.inventoryService.getLowStockItems().subscribe({
      next: (items) => {
        this.zone.run(() => {
          this.lowStockItems.set(items);
          this.cdr.detectChanges();
        });
      },
      error: (err) => console.error('Failed to fetch low stock items:', err)
    });
  }

  private setupSignalR(): void {
    const token = this.authService.getToken() ?? '';
    if (!token) return;

    this.signalRService.connect(token).then(() => {
      // 1. Receive new order alert
      const subOrder = this.signalRService.newOrder$.subscribe({
        next: (order) => {
          this.zone.run(() => {
            this.fetchInitialData();
            this.addAlertLog({
              id: `order-new-${Date.now()}`,
              type: 'order',
              message: `New Order #${order.id} placed for Table ${order.tableNumber}.`,
              timestamp: new Date(),
              tableId: order.tableId,
              orderId: order.id
            });
            // If the selected table is currently open in detail panel, refresh details
            if (this.selectedTableId() === order.tableId) {
              this.openTableDetails(this.selectedTableId()!);
            }
          });
        }
      });
      this.subscriptions.add(subOrder);

      // 2. Receive order status update
      const subOrderUpdate = this.signalRService.orderUpdate$.subscribe({
        next: (update) => {
          this.zone.run(() => {
            this.fetchInitialData();
            this.addAlertLog({
              id: `order-up-${Date.now()}`,
              type: 'order',
              message: `Order #${update.orderId} updated to state: ${update.status || 'Updated'}.`,
              timestamp: new Date(),
              orderId: update.orderId
            });
            // Refresh if selected
            if (this.selectedTableOrder()?.id === update.orderId) {
              this.openTableDetails(this.selectedTableId()!);
            }
          });
        }
      });
      this.subscriptions.add(subOrderUpdate);

      // 3. Receive table layout updates
      const subTables = this.signalRService.tablesUpdated$.subscribe({
        next: () => {
          this.zone.run(() => {
            this.fetchInitialData();
            if (this.selectedTableId()) {
              this.openTableDetails(this.selectedTableId()!);
            }
          });
        }
      });
      this.subscriptions.add(subTables);

      // 4. Receive bill generation update
      const subBillGen = this.signalRService.billGenerated$.subscribe({
        next: (bill) => {
          this.zone.run(() => {
            this.fetchInitialData();
            const table = this.tables().find(t => t.activeOrderId === bill.orderId);
            const tableNum = table ? table.number : 'Unknown';
            this.addAlertLog({
              id: `bill-gen-${Date.now()}`,
              type: 'bill',
              message: `Bill Generated for Table ${tableNum}. Amount: $${bill.totalAmount.toFixed(2)}.`,
              timestamp: new Date(),
              orderId: bill.orderId,
              amount: bill.totalAmount
            });
            if (this.selectedTableId() && table && table.id === this.selectedTableId()) {
              this.openTableDetails(this.selectedTableId()!);
            }
          });
        }
      });
      this.subscriptions.add(subBillGen);

      // 5. Receive bill paid update
      const subBillPaid = this.signalRService.billPaid$.subscribe({
        next: (bill) => {
          this.zone.run(() => {
            this.fetchInitialData();
            const table = this.tables().find(t => t.activeOrderId === bill.orderId);
            const tableNum = table ? table.number : 'Unknown';
            this.addAlertLog({
              id: `bill-paid-${Date.now()}`,
              type: 'bill',
              message: `Payment Received for Table ${tableNum}. Amount: $${bill.totalAmount.toFixed(2)}.`,
              timestamp: new Date(),
              orderId: bill.orderId,
              amount: bill.totalAmount
            });
            if (this.selectedTableId() && table && table.id === this.selectedTableId()) {
              this.closeDrawer();
            }
          });
        }
      });
      this.subscriptions.add(subBillPaid);

      // 6. Receive stock warnings and chef notifications
      const subAdminAlert = this.signalRService.adminAlert$.subscribe({
        next: (alert) => {
          this.zone.run(() => {
            this.fetchInitialData();
            this.addAlertLog({
              id: `stock-${Date.now()}`,
              type: 'stock',
              message: alert.message || `Stock Alert: ${alert.itemName} is running low!`,
              timestamp: new Date(),
              flaggedBy: alert.flaggedBy
            });
            this.activeFeedTab.set('stock');
          });
        }
      });
      this.subscriptions.add(subAdminAlert);
    });
  }

  private addAlertLog(log: AlertLog): void {
    const list = [log, ...this.alerts()];
    // Cap log items in memory at 40 logs
    if (list.length > 40) {
      list.pop();
    }
    this.alerts.set(list);
    this.cdr.detectChanges();
  }

  private startWaiterCallSimulation(): void {
    // Generate simulated requests to add live activity
    const callMessages = [
      'requested assistance.',
      'requested extra napkins.',
      'requested water refilling.',
      'requested utensils.',
      'requested table cleaning.'
    ];

    this.simulationIntervalId = setInterval(() => {
      this.zone.run(() => {
        // Only run if there are occupied tables
        const occupied = this.tables().filter(t => t.status === 2);
        if (occupied.length > 0) {
          const randomTable = occupied[Math.floor(Math.random() * occupied.length)];
          const randomMsg = callMessages[Math.floor(Math.random() * callMessages.length)];
          
          this.addAlertLog({
            id: `call-${Date.now()}`,
            type: 'call',
            message: `Table ${randomTable.number} ${randomMsg}`,
            timestamp: new Date(),
            tableId: randomTable.id
          });
        }
      });
    }, 45000); // every 45s
  }

  public openTableDetails(tableId: number): void {
    const table = this.tables().find(t => t.id === tableId);
    if (!table) return;

    this.selectedTableId.set(tableId);
    this.selectedTableDetails.set(table);
    this.selectedTableOrder.set(null);
    this.selectedTableBill.set(null);

    // If occupied, load order and bill details
    if (table.status === 2) {
      this.isDrawerLoading.set(true);
      
      this.orderService.getActiveOrderByTableId(tableId).subscribe({
        next: (order) => {
          this.zone.run(() => {
            this.selectedTableOrder.set(order);
            
            // Check if there is an active bill generated
            if (order && order.id) {
              this.billService.getBillByOrderId(order.id).subscribe({
                next: (bill) => {
                  this.zone.run(() => {
                    this.selectedTableBill.set(bill);
                    this.isDrawerLoading.set(false);
                    this.cdr.detectChanges();
                  });
                },
                error: () => {
                  this.selectedTableBill.set(null);
                  this.isDrawerLoading.set(false);
                  this.cdr.detectChanges();
                }
              });
            } else {
              this.isDrawerLoading.set(false);
              this.cdr.detectChanges();
            }
          });
        },
        error: (err) => {
          console.error('Failed to load table active order details:', err);
          this.isDrawerLoading.set(false);
          this.cdr.detectChanges();
        }
      });
    }
  }

  public closeDrawer(): void {
    this.selectedTableId.set(null);
    this.selectedTableDetails.set(null);
    this.selectedTableOrder.set(null);
    this.selectedTableBill.set(null);
  }

  public getElapsedTime(createdAt: any): string {
    if (!createdAt) return '';
    const now = this.timeTrigger();
    const diffMs = now - new Date(createdAt).getTime();
    const diffMins = Math.max(0, Math.floor(diffMs / 60000));
    if (diffMins < 1) {
      const diffSecs = Math.max(0, Math.floor(diffMs / 1000));
      return `${diffSecs}s ago`;
    }
    if (diffMins < 60) {
      return `${diffMins}m ago`;
    }
    const diffHours = Math.floor(diffMins / 60);
    if (diffHours < 24) {
      return `${diffHours}h ago`;
    }
    return `${Math.floor(diffHours / 24)}d ago`;
  }

  public clearAlerts(): void {
    this.alerts.set([]);
  }
}
