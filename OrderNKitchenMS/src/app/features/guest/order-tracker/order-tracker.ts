import { Component, Input, OnInit, OnDestroy, OnChanges } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-order-tracker',
  imports: [CommonModule],
  templateUrl: './order-tracker.html',
  styleUrl: './order-tracker.css',
})
export class OrderTracker implements OnInit, OnDestroy, OnChanges {
  @Input() order: any;
  @Input() tableNumber!: number;

  public showWaiterModal: boolean = false;
  public showBillModal: boolean = false;

  private timerId: any;
  public timeElapsedDisplay: string = '0 min';

  public ngOnInit() {
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
    this.updateTimeElapsed();
  }

  private updateTimeElapsed() {
    if (!this.order || !this.order.createdAt) {
      this.timeElapsedDisplay = '0 min';
      return;
    }
    const created = new Date(this.order.createdAt).getTime();
    const now = new Date().getTime();
    const diffMs = now - created;
    const diffMins = Math.max(0, Math.floor(diffMs / 60000));

    if (diffMins < 1) {
      const diffSecs = Math.max(0, Math.floor(diffMs / 1000));
      this.timeElapsedDisplay = `${diffSecs}s`;
    } else {
      this.timeElapsedDisplay = `${diffMins} min`;
    }
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
