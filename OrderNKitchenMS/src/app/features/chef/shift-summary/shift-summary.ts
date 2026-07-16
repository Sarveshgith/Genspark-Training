// @feature Chef | Shift Summary | A slide-up sheet component displaying today's kitchen performance indicators and statistics.
import { Component, inject, signal, Input, Output, EventEmitter, OnInit, OnChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { OrderService } from '../../../core/services/order.service';

@Component({
  selector: 'app-shift-summary',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './shift-summary.html',
  styleUrl: './shift-summary.css'
})
export class ShiftSummary implements OnInit, OnChanges {
  private orderService = inject(OrderService);

  @Input() isOpen = false;
  @Input() isInline = false;
  @Output() close = new EventEmitter<void>();

  public isLoading = signal<boolean>(false);
  public error = signal<string | null>(null);

  // Statistics signals
  public completedCount = signal<number>(0);
  public avgPrepTime = signal<string>('0 mins');
  public overdueCount = signal<number>(0);
  public totalActiveCount = signal<number>(0);

  ngOnInit() {
    if (this.isOpen) {
      this.loadShiftStats();
    }
  }

  ngOnChanges() {
    if (this.isOpen) {
      this.loadShiftStats();
    }
  }

  public loadShiftStats() {
    this.isLoading.set(true);
    this.error.set(null);

    this.orderService.getOrders({ pageSize: 1000 }).subscribe({
      next: (orders) => {
        const today = new Date().toDateString();
        
        // Filter by today's date
        const todayOrders = orders.filter(o => new Date(o.createdAt).toDateString() === today);
        this.totalActiveCount.set(todayOrders.length);

        // Prepared/Completed (Ready, Served, or Completed) orders today
        const completed = todayOrders.filter(o => o.status === 3 || o.status === 4 || o.status === 5);
        this.completedCount.set(completed.length);

        // Average preparation time logic
        if (completed.length === 0) {
          this.avgPrepTime.set('0 mins');
        } else {
          let totalPrepTime = 0;
          completed.forEach(o => {
            const start = new Date(o.createdAt).getTime();
            const end = o.estimatedReadyAt ? new Date(o.estimatedReadyAt).getTime() : (start + 15 * 60 * 1000);
            const diffMins = Math.round((end - start) / 60000);
            totalPrepTime += diffMins > 0 ? diffMins : 15;
          });
          const avg = Math.round(totalPrepTime / completed.length);
          this.avgPrepTime.set(`${avg} mins`);
        }

        // Overdue count calculation (InPrep orders exceeding their estimates)
        let overdue = 0;
        todayOrders.forEach(o => {
          if (o.status === 2) { // InPrep
            const start = new Date(o.createdAt).getTime();
            const limitMins = o.estimatedReadyAt ? Math.round((new Date(o.estimatedReadyAt).getTime() - start) / 60000) : 15;
            const elapsedMins = (Date.now() - start) / 60000;
            if (elapsedMins > (limitMins > 0 ? limitMins : 15)) {
              overdue++;
            }
          }
        });
        this.overdueCount.set(overdue);
        this.isLoading.set(false);
      },
      error: (err) => {
        console.error('Failed to load shift stats:', err);
        this.error.set('Failed to retrieve shift summary data.');
        this.isLoading.set(false);
      }
    });
  }

  public handleClose() {
    this.close.emit();
  }
}
