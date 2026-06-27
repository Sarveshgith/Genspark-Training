import { Component, inject, OnInit, OnDestroy, signal, ChangeDetectorRef, NgZone } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TableService } from '../../../core/services/table.service';
import { TableModel } from '../../../core/models/table.model';
import { SignalRService } from '../../../core/services/signalr.service';
import { AuthService } from '../../../core/services/auth.service';
import { OrderService } from '../../../core/services/order.service';
import { OrderModel } from '../../../core/models/order.model';
import { Router } from '@angular/router';
import { Subscription } from 'rxjs';

@Component({
  selector: 'app-table-view',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './table-view.html',
  styleUrl: './table-view.css',
})
export class TableView implements OnInit, OnDestroy {
  private tableService = inject(TableService);
  private authService = inject(AuthService);
  private orderService = inject(OrderService);
  public signalRService = inject(SignalRService);
  private router = inject(Router);
  private cdr = inject(ChangeDetectorRef);
  private zone = inject(NgZone);

  public tables = signal<TableModel[]>([]);
  public activeOrders = signal<OrderModel[]>([]);
  public timeTrigger = signal<number>(Date.now());

  private subscriptions: Subscription = new Subscription();
  private timeIntervalId: any;

  ngOnInit(): void {
    this.fetchTables();
    this.setupSignalR();

    this.timeIntervalId = setInterval(() => {
      this.timeTrigger.set(Date.now());
    }, 10000);
  }

  ngOnDestroy(): void {
    this.subscriptions.unsubscribe();
    if (this.timeIntervalId) {
      clearInterval(this.timeIntervalId);
    }
  }

  private setupSignalR(): void {
    const token = this.authService.getToken() ?? '';
    if (token) {
      this.signalRService.connect(token).then(() => {
        const sub1 = this.signalRService.tablesUpdated$.subscribe({
          next: () => {
            this.zone.run(() => {
              this.fetchTables();
            });
          }
        });
        const sub2 = this.signalRService.orderUpdate$.subscribe({
          next: () => {
            this.zone.run(() => {
              this.fetchTables();
            });
          }
        });
        this.subscriptions.add(sub1);
        this.subscriptions.add(sub2);
      });
    }
  }

  public fetchTables(): void {
    this.orderService.getActiveOrders().subscribe({
      next: (orders: OrderModel[]) => {
        this.zone.run(() => {
          this.activeOrders.set(orders);
          this.cdr.detectChanges();
        });
      },
      error: (err) => {
        console.error('Failed to fetch active orders:', err);
      }
    });

    this.tableService.getTables().subscribe({
      next: (response: TableModel[]) => {
        this.zone.run(() => {
          // Sort tables by number ascending
          const sorted = [...response].sort((a, b) => a.number - b.number);
          this.tables.set(sorted.filter(t => !t.isDeleted));
          this.cdr.detectChanges();
        });
      },
      error: (err) => {
        console.error('Failed to fetch tables:', err);
      }
    });
  }

  public getElapsedTime(createdAt: any): string {
    if (!createdAt) return '';
    // Reading timeTrigger signal to reactively update elapsed text
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
    const diffDays = Math.floor(diffHours / 24);
    return `${diffDays}d ago`;
  }

  public onTableClick(table: TableModel): void {
    if (table.status === 1) { // Available
      this.router.navigate(['/waiter/menu'], { queryParams: { tableId: table.id } });
    } else if (table.status === 2) { // Occupied
      this.router.navigate(['/waiter/active-order', table.id]);
    }
  }

  public getReadyOrderForTable(tableId: number): OrderModel | undefined {
    return this.activeOrders().find(o => o.tableId === tableId && o.status === 3);
  }

  public serveOrder(event: MouseEvent, orderId: number): void {
    event.stopPropagation(); // prevent navigation on card click
    this.orderService.updateOrderStatus(orderId, 4).subscribe({
      next: () => {
        this.fetchTables();
      },
      error: (err) => {
        console.error('Failed to mark order served:', err);
      }
    });
  }
}
