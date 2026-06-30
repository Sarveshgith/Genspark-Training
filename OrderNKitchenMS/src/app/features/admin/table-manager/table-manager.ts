// @feature Admin | Table Manager | Comprehensive administrative control panel for dining room layouts, capacity updates, and status overrides.
import { Component, inject, OnInit, OnDestroy, signal, computed, ChangeDetectorRef, NgZone } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TableService } from '../../../core/services/table.service';
import { TableModel } from '../../../core/models/table.model';
import { SignalRService } from '../../../core/services/signalr.service';
import { AuthService } from '../../../core/services/auth.service';
import { OrderService } from '../../../core/services/order.service';
import { ToastService } from '../../../core/services/toast.service';
import { OrderModel } from '../../../core/models/order.model';
import { Subscription } from 'rxjs';

@Component({
  selector: 'app-table-manager',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './table-manager.html',
  styleUrl: './table-manager.css'
})
export class TableManager implements OnInit, OnDestroy {
  private tableService = inject(TableService);
  private authService = inject(AuthService);
  private orderService = inject(OrderService);
  public signalRService = inject(SignalRService);
  private toastService = inject(ToastService);
  private cdr = inject(ChangeDetectorRef);
  private zone = inject(NgZone);

  // Raw states
  public tables = signal<TableModel[]>([]);
  public activeOrders = signal<OrderModel[]>([]);
  public timeTrigger = signal<number>(Date.now());

  // Search & Filtering states
  public searchQuery = signal<string>('');
  public capacityFilter = signal<string>('all');
  public statusFilter = signal<string>('all');

  // Inline editing states
  public editingTableId = signal<number | null>(null);
  public editNumber = signal<number>(0);
  public editCapacity = signal<number>(0);

  // Add Table form states
  public addNumber = signal<number>(1);
  public addCapacity = signal<number>(4);
  public addStatus = signal<number>(1);

  // Deletion state
  public deleteConfirmTableId = signal<number | null>(null);

  private subscriptions: Subscription = new Subscription();
  private timeIntervalId: any;

  // Computed: KPI Stats
  public totalTablesCount = computed(() => this.tables().length);

  public totalCapacity = computed(() => {
    return this.tables().reduce((sum, t) => sum + t.capacity, 0);
  });

  public occupancyRate = computed(() => {
    const total = this.totalTablesCount();
    if (total === 0) return 0;
    const occupied = this.tables().filter(t => t.status === 2).length;
    return Math.round((occupied / total) * 100);
  });

  public availableCount = computed(() => {
    return this.tables().filter(t => t.status === 1).length;
  });

  public occupiedCount = computed(() => {
    return this.tables().filter(t => t.status === 2).length;
  });

  public reservedCount = computed(() => {
    return this.tables().filter(t => t.status === 3).length;
  });

  // Computed: Filtered Tables List
  public filteredTables = computed(() => {
    let list = this.tables();

    // 1. Search Query filter (by table number)
    const query = this.searchQuery().trim();
    if (query) {
      list = list.filter(t => t.number.toString().includes(query));
    }

    // 2. Capacity filter
    const cap = this.capacityFilter();
    if (cap !== 'all') {
      const capNum = parseInt(cap, 10);
      if (capNum === 6) {
        list = list.filter(t => t.capacity >= 6);
      } else {
        list = list.filter(t => t.capacity === capNum);
      }
    }

    // 3. Status filter
    const status = this.statusFilter();
    if (status !== 'all') {
      const statusNum = parseInt(status, 10);
      list = list.filter(t => t.status === statusNum);
    }

    return list;
  });

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
        this.zone.run(() => {
          this.toastService.error('Failed to fetch tables. Please try again later.');
        });
        console.error('Failed to fetch tables:', err);
      }
    });
  }

  // Elapsed time calculation
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

  // CRUD: Open Dialogs
  public openModal(dialog: HTMLDialogElement): void {
    dialog.showModal();
  }

  public closeModal(dialog: HTMLDialogElement): void {
    dialog.close();
  }

  // Fallback handler for clicking the backdrop (light-dismiss)
  public onDialogClick(event: MouseEvent, dialog: HTMLDialogElement): void {
    if (event.target === dialog) {
      const rect = dialog.getBoundingClientRect();
      const isDialogContent = (
        rect.top <= event.clientY &&
        event.clientY <= rect.top + rect.height &&
        rect.left <= event.clientX &&
        event.clientX <= rect.left + rect.width
      );
      if (!isDialogContent) {
        dialog.close();
      }
    }
  }

  // CRUD: Add new table
  public onAddTableSubmit(dialog: HTMLDialogElement): void {
    const tableNum = this.addNumber();
    const capacity = this.addCapacity();
    const status = this.addStatus();

    if (!tableNum || tableNum <= 0) {
      this.toastService.error('Please provide a valid table number.');
      return;
    }

    if (!capacity || capacity <= 0) {
      this.toastService.error('Table capacity must be at least 1 guest.');
      return;
    }

    // Check if table number already exists
    if (this.tables().some(t => t.number === tableNum)) {
      this.toastService.error(`Table number ${tableNum} already exists.`);
      return;
    }

    this.tableService.createTable({ number: tableNum, capacity, status }).subscribe({
      next: () => {
        this.zone.run(() => {
          this.toastService.success(`Table #${tableNum} created successfully.`);
          this.fetchTables();
          dialog.close();
          // Reset form defaults
          this.addNumber.set(this.getSuggestedTableNumber());
          this.addCapacity.set(4);
          this.addStatus.set(1);
        });
      },
      error: (err) => {
        this.zone.run(() => {
          this.toastService.error(err.error?.message || 'Error occurred while creating table.');
        });
        console.error('Failed to create table:', err);
      }
    });
  }

  // Suggest next consecutive table number
  private getSuggestedTableNumber(): number {
    const list = this.tables();
    if (list.length === 0) return 1;
    const max = Math.max(...list.map(t => t.number));
    return max + 1;
  }

  // CRUD: Inline Edit State
  public startEdit(table: TableModel): void {
    this.editingTableId.set(table.id);
    this.editNumber.set(table.number);
    this.editCapacity.set(table.capacity);
  }

  public cancelEdit(): void {
    this.editingTableId.set(null);
  }

  public saveEdit(tableId: number): void {
    const num = this.editNumber();
    const cap = this.editCapacity();

    if (!num || num <= 0) {
      this.toastService.error('Table number must be positive.');
      return;
    }

    if (!cap || cap <= 0) {
      this.toastService.error('Table capacity must be at least 1 guest.');
      return;
    }

    // Check conflict (exclude current table being edited)
    if (this.tables().some(t => t.number === num && t.id !== tableId)) {
      this.toastService.error(`Conflict: Table number ${num} already exists.`);
      return;
    }

    this.tableService.updateTable(tableId, { number: num, capacity: cap }).subscribe({
      next: () => {
        this.zone.run(() => {
          this.toastService.success(`Table #${num} details updated successfully.`);
          this.editingTableId.set(null);
          this.fetchTables();
        });
      },
      error: (err) => {
        this.zone.run(() => {
          this.toastService.error(err.error?.message || 'Failed to update table details.');
        });
        console.error('Failed to update table details:', err);
      }
    });
  }

  // CRUD: Status Override Toggle/Select
  public onStatusOverride(tableId: number, event: Event): void {
    const selectEl = event.target as HTMLSelectElement;
    const statusVal = parseInt(selectEl.value, 10);
    const tableNum = this.tables().find(t => t.id === tableId)?.number;

    this.tableService.updateTableStatus(tableId, statusVal).subscribe({
      next: () => {
        this.zone.run(() => {
          this.toastService.success(`Table #${tableNum || tableId} status changed to ${this.getStatusName(statusVal)}.`);
          this.fetchTables();
        });
      },
      error: (err) => {
        this.zone.run(() => {
          this.toastService.error(err.error?.message || 'Failed to change table status.');
        });
        console.error('Failed to override status:', err);
        // Refresh to revert select element selection to correct status
        this.fetchTables();
      }
    });
  }

  // CRUD: Delete Table
  public triggerDelete(tableId: number, deleteDialog: HTMLDialogElement): void {
    this.deleteConfirmTableId.set(tableId);
    deleteDialog.showModal();
  }

  public getConfirmDeleteTable(): TableModel | undefined {
    const id = this.deleteConfirmTableId();
    return this.tables().find(t => t.id === id);
  }

  public confirmDelete(deleteDialog: HTMLDialogElement): void {
    const id = this.deleteConfirmTableId();
    if (!id) return;
    const tableNum = this.tables().find(t => t.id === id)?.number;

    this.tableService.deleteTable(id).subscribe({
      next: () => {
        this.zone.run(() => {
          this.toastService.success(`Table #${tableNum || id} archived successfully.`);
          deleteDialog.close();
          this.deleteConfirmTableId.set(null);
          this.fetchTables();
        });
      },
      error: (err) => {
        this.zone.run(() => {
          this.toastService.error(err.error?.message || 'Failed to delete table.');
        });
        console.error('Failed to delete table:', err);
        deleteDialog.close();
      }
    });
  }

  public getStatusName(status: number): string {
    switch (status) {
      case 1: return 'Available';
      case 2: return 'Occupied';
      case 3: return 'Reserved';
      default: return 'Unknown';
    }
  }

  public overrideStatus(tableId: number, statusVal: any): void {
    const status = parseInt(statusVal, 10);
    const tableNum = this.tables().find(t => t.id === tableId)?.number;
    this.tableService.updateTableStatus(tableId, status).subscribe({
      next: () => {
        this.zone.run(() => {
          this.toastService.success(`Table #${tableNum || tableId} status updated to ${this.getStatusName(status)}.`);
          this.fetchTables();
        });
      },
      error: (err) => {
        this.zone.run(() => {
          this.toastService.error(err.error?.message || 'Failed to change table status.');
        });
        console.error('Failed to override status:', err);
        this.fetchTables();
      }
    });
  }
}
