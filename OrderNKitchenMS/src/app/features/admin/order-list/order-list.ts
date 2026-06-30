import { Component, inject, OnInit, OnDestroy, signal, computed, ChangeDetectorRef, NgZone } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { OrderService } from '../../../core/services/order.service';
import { TableService } from '../../../core/services/table.service';
import { SignalRService } from '../../../core/services/signalr.service';
import { AuthService } from '../../../core/services/auth.service';
import { OrderModel, QueryOrderModel } from '../../../core/models/order.model';
import { TableModel } from '../../../core/models/table.model';
import { Subscription } from 'rxjs';

@Component({
  selector: 'app-order-list',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './order-list.html',
  styleUrl: './order-list.css'
})
export class OrderList implements OnInit, OnDestroy {
  private orderService = inject(OrderService);
  private tableService = inject(TableService);
  public signalRService = inject(SignalRService);
  private authService = inject(AuthService);
  private cdr = inject(ChangeDetectorRef);
  private zone = inject(NgZone);

  // Lists
  public orders = signal<OrderModel[]>([]);
  public tables = signal<TableModel[]>([]);

  // Expanded row tracking
  public expandedOrderId = signal<number | null>(null);

  // States
  public isLoading = signal<boolean>(false);
  public errorMessage = signal<string>('');

  // Filters
  public statusFilter = signal<number | null>(null);
  public tableFilter = signal<number | null>(null);
  public fromDateFilter = signal<string>('');
  public toDateFilter = signal<string>('');

  // Pagination
  public pageNumber = signal<number>(1);
  public pageSize = signal<number>(10);
  public hasMore = signal<boolean>(true);

  private subscriptions: Subscription = new Subscription();

  ngOnInit(): void {
    this.fetchDropdowns();
    this.fetchOrders();
    this.setupSignalR();
  }

  ngOnDestroy(): void {
    this.subscriptions.unsubscribe();
  }

  private setupSignalR(): void {
    const token = this.authService.getToken() ?? '';
    if (token) {
      this.signalRService.connect(token).then(() => {
        const sub = this.signalRService.orderUpdate$.subscribe({
          next: () => {
            this.zone.run(() => {
              this.fetchOrders();
            });
          }
        });
        const subNew = this.signalRService.newOrder$.subscribe({
          next: () => {
            this.zone.run(() => {
              this.fetchOrders();
            });
          }
        });
        this.subscriptions.add(sub);
        this.subscriptions.add(subNew);
      });
    }
  }

  public fetchDropdowns(): void {
    // 1. Tables
    this.tableService.getTables().subscribe({
      next: (data) => {
        this.zone.run(() => {
          this.tables.set(data.filter(t => !t.isDeleted));
          this.cdr.detectChanges();
        });
      },
      error: (err) => console.error('Failed to fetch filter tables:', err)
    });
  }

  public fetchOrders(): void {
    this.isLoading.set(true);
    this.errorMessage.set('');

    const query: QueryOrderModel = {
      pageNumber: this.pageNumber(),
      pageSize: this.pageSize()
    };

    if (this.statusFilter() !== null) {
      query.status = this.statusFilter();
    }
    if (this.tableFilter() !== null) {
      query.tableId = this.tableFilter();
    }
    if (this.fromDateFilter()) {
      query.from = new Date(this.fromDateFilter());
    }
    if (this.toDateFilter()) {
      query.to = new Date(this.toDateFilter());
    }

    this.orderService.getOrders(query).subscribe({
      next: (data) => {
        this.zone.run(() => {
          this.orders.set(data);
          this.hasMore.set(data.length === this.pageSize());
          this.isLoading.set(false);
          this.cdr.detectChanges();
        });
      },
      error: (err) => {
        this.zone.run(() => {
          this.errorMessage.set('Failed to retrieve orders. Please check search parameters.');
          this.isLoading.set(false);
          this.cdr.detectChanges();
        });
        console.error('Failed to get admin orders:', err);
      }
    });
  }

  // Filters application
  public applyFilters(): void {
    this.pageNumber.set(1);
    this.fetchOrders();
  }

  public resetFilters(): void {
    this.statusFilter.set(null);
    this.tableFilter.set(null);
    this.fromDateFilter.set('');
    this.toDateFilter.set('');
    this.pageNumber.set(1);
    this.fetchOrders();
  }

  // Row expand toggle
  public toggleExpandOrder(orderId: number): void {
    if (this.expandedOrderId() === orderId) {
      this.expandedOrderId.set(null);
    } else {
      this.expandedOrderId.set(orderId);
    }
  }

  // Manual Status Modification
  public updateStatus(orderId: number, statusVal: string): void {
    const statusNum = parseInt(statusVal, 10);
    this.orderService.updateOrderStatus(orderId, statusNum).subscribe({
      next: () => {
        this.zone.run(() => {
          this.fetchOrders();
        });
      },
      error: (err) => {
        alert(err.error?.message || 'Failed to modify order status.');
        console.error('Failed status update:', err);
      }
    });
  }
  // Pagination triggers
  public nextPage(): void {
    if (this.hasMore() && !this.isLoading()) {
      this.pageNumber.update(p => p + 1);
      this.fetchOrders();
    }
  }

  public prevPage(): void {
    if (this.pageNumber() > 1 && !this.isLoading()) {
      this.pageNumber.update(p => p - 1);
      this.fetchOrders();
    }
  }

  public getStatusName(status: number): string {
    switch (status) {
      case 1: return 'Pending';
      case 2: return 'InPrep';
      case 3: return 'Ready';
      case 4: return 'Served';
      case 5: return 'Completed';
      case 6: return 'Cancelled';
      default: return 'Unknown';
    }
  }
}
