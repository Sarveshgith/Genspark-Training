import { Component, Input, OnInit, OnDestroy, OnChanges, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';

@Component({
  selector: 'app-order-tracker',
  imports: [CommonModule],
  templateUrl: './order-tracker.html',
  styleUrl: './order-tracker.css',
})
export class OrderTracker implements OnInit, OnDestroy, OnChanges {
  @Input() order: any;
  @Input() tableNumber!: number;
  @Input() bill: any = null;

  private router = inject(Router);

  public showWaiterModal: boolean = false;
  public showBillModal: boolean = false;

  private timerId: any;
  public orderSignal = signal<any>(null);
  public elapsedSeconds = signal<number>(0);

  public estimatedSeconds = computed(() => {
    const order = this.orderSignal();
    return (order?.estimatedTimeMinutes || 0) * 60;
  });

  public isOverdue = computed(() => {
    const est = this.estimatedSeconds();
    return est > 0 && this.elapsedSeconds() > est;
  });

  public timeElapsedDisplay = computed(() => {
    const elapsed = this.elapsedSeconds();
    const diffMins = Math.floor(elapsed / 60);
    if (diffMins < 1) {
      return `${elapsed}s`;
    } else {
      return `${diffMins} min`;
    }
  });

  public waitTimeProgressPercent = computed(() => {
    const est = this.estimatedSeconds();
    if (est <= 0) return 0;
    return Math.round((this.elapsedSeconds() / est) * 100);
  });

  public waitTimeProgressPercentCapped = computed(() => {
    return Math.min(100, this.waitTimeProgressPercent());
  });

  public ngOnInit() {
    this.orderSignal.set(this.order);
    this.updateTimeElapsed();
    this.timerId = setInterval(() => {
      this.updateTimeElapsed();
    }, 10000);
  }

  public ngOnDestroy() {
    if (this.timerId) {
      clearInterval(this.timerId);
    }
  }

  public ngOnChanges() {
    this.orderSignal.set(this.order);
    this.updateTimeElapsed();
  }

  private updateTimeElapsed() {
    const order = this.orderSignal();
    if (!order || !order.createdAt) {
      this.elapsedSeconds.set(0);
      return;
    }
    const created = new Date(order.createdAt).getTime();
    const now = new Date().getTime();
    const diffMs = now - created;
    this.elapsedSeconds.set(Math.max(0, Math.floor(diffMs / 1000)));
  }

  public get progressPercent(): number {
    if (!this.order) return 0;

    switch (this.order.status) {
      case 'Pending':
        return 15;
      case 'InPrep':
        return 50;
      case 'Ready':
        return 80;
      case 'Served':
      case 'Completed':
        return 100;
      case 'Cancelled':
        return 0;
      default:
        return 15;
    }
  }

  public get isConfirmed(): boolean {
    if (!this.order) return false;
    return ['Pending', 'InPrep', 'Ready', 'Served', 'Completed'].includes(this.order.status);
  }

  public get isInKitchen(): boolean {
    if (!this.order) return false;
    return ['InPrep', 'Ready', 'Served', 'Completed'].includes(this.order.status);
  }

  public get isServed(): boolean {
    if (!this.order) return false;
    return ['Served', 'Completed'].includes(this.order.status);
  }

  public get statusDisplay(): string {
    if (!this.order) return 'Pending';

    switch (this.order.status) {
      case 'Pending':
        return 'Confirmed';
      case 'InPrep':
        return 'Preparing';
      case 'Ready':
        return 'Plating';
      case 'Served':
      case 'Completed':
        return 'Served';
      case 'Cancelled':
        return 'Cancelled';
      default:
        return this.order.status;
    }
  }

  private waiterTimeoutId: any;
  private billTimeoutId: any;

  public callWaiter() {
    if (this.waiterTimeoutId) {
      clearTimeout(this.waiterTimeoutId);
    }
    this.showWaiterModal = true;
    this.waiterTimeoutId = setTimeout(() => {
      this.showWaiterModal = false;
      this.waiterTimeoutId = null;
    }, 4000);
  }

  public closeWaiterModal() {
    this.showWaiterModal = false;
    if (this.waiterTimeoutId) {
      clearTimeout(this.waiterTimeoutId);
      this.waiterTimeoutId = null;
    }
  }

  public requestBill() {
    if (this.billTimeoutId) {
      clearTimeout(this.billTimeoutId);
    }
    this.showBillModal = true;
    this.billTimeoutId = setTimeout(() => {
      this.showBillModal = false;
      this.billTimeoutId = null;
    }, 4000);
  }

  public closeBillModal() {
    this.showBillModal = false;
    if (this.billTimeoutId) {
      clearTimeout(this.billTimeoutId);
      this.billTimeoutId = null;
    }
  }
}
