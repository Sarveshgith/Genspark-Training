import { Component, inject, OnInit, OnDestroy, signal, ChangeDetectorRef, NgZone } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { OrderService } from '../../../core/services/order.service';
import { SignalRService } from '../../../core/services/signalr.service';
import { AuthService } from '../../../core/services/auth.service';
import { BillService } from '../../../core/services/bill.service';
import { BillDto } from '../../../core/models/bill.model';
import { HttpClient } from '@angular/common/http';
import { OrderModel } from '../../../core/models/order.model';
import { Subscription } from 'rxjs';

@Component({
  selector: 'app-active-order',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './active-order.html',
  styleUrl: './active-order.css',
})
export class ActiveOrderComponent implements OnInit, OnDestroy {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private orderService = inject(OrderService);
  private authService = inject(AuthService);
  private signalRService = inject(SignalRService);
  private billService = inject(BillService);
  private http = inject(HttpClient);
  private cdr = inject(ChangeDetectorRef);
  private zone = inject(NgZone);

  public tableId: number = 0;
  public isLoading: boolean = true;
  public errorMessage: string | null = null;
  public order: OrderModel | null = null;
  public taxPercent: string = '8';

  public bill: BillDto | null = null;
  public isGeneratingBill: boolean = false;
  public isMarkingPaid: boolean = false;
  public billError: string | null = null;
  public billSuccess: string | null = null;

  public elapsedDisplay: string = '0s';
  private timerId: any;
  private subscriptions: Subscription = new Subscription();

  ngOnInit() {
    this.route.params.subscribe(params => {
      this.tableId = Number(params['tableId']);
      if (isNaN(this.tableId) || this.tableId <= 0) {
        this.errorMessage = "Invalid Table ID.";
        this.isLoading = false;
        return;
      }
      this.fetchActiveOrder();
      this.setupSignalR();
    });

    this.timerId = setInterval(() => {
      this.updateElapsedTime();
    }, 10000);
  }

  ngOnDestroy() {
    this.subscriptions.unsubscribe();
    if (this.timerId) {
      clearInterval(this.timerId);
    }
  }

  private setupSignalR() {
    const token = this.authService.getToken() ?? '';
    if (token) {
      this.signalRService.connect(token).then(() => {
        const sub = this.signalRService.tablesUpdated$.subscribe({
          next: () => {
            this.zone.run(() => {
              this.fetchActiveOrder();
            });
          }
        });
        this.subscriptions.add(sub);
      });
    }
  }

  public fetchActiveOrder() {
    this.orderService.getActiveOrderByTableId(this.tableId).subscribe({
      next: (order) => {
        this.zone.run(() => {
          this.order = order;
          this.errorMessage = null;
          this.isLoading = false;
          this.updateElapsedTime();
          this.fetchBillForOrder();
          this.cdr.detectChanges();
        });
      },
      error: (err) => {
        this.zone.run(() => {
          this.isLoading = false;
          if (err.status === 404) {
            this.order = null;
            this.errorMessage = `No active order found for Table ID ${this.tableId}.`;
          } else {
            this.errorMessage = "Failed to fetch active order details.";
          }
          this.cdr.detectChanges();
        });
      }
    });
  }

  public fetchBillForOrder() {
    if (!this.order) return;
    this.billService.getBillByOrderId(this.order.id).subscribe({
      next: (bill) => {
        this.zone.run(() => {
          this.bill = bill;
          this.cdr.detectChanges();
        });
      },
      error: (err) => {
        this.zone.run(() => {
          this.bill = null;
          this.cdr.detectChanges();
        });
      }
    });
  }

  public generateBill() {
    if (!this.order) return;
    this.isGeneratingBill = true;
    this.billError = null;
    this.billSuccess = null;

    const payload = {
      orderId: this.order.id,
      taxRate: 8,
      discountAmount: 0
    };

    this.billService.createBill(payload).subscribe({
      next: (bill) => {
        this.zone.run(() => {
          this.bill = bill;
          this.isGeneratingBill = false;
          this.billSuccess = "Bill generated successfully!";
          this.cdr.detectChanges();
        });
      },
      error: (err) => {
        this.zone.run(() => {
          this.isGeneratingBill = false;
          this.billError = err.error?.message || err.message || "Failed to generate bill.";
          this.cdr.detectChanges();
        });
      }
    });
  }

  public markAsPaid() {
    if (!this.bill) return;
    this.isMarkingPaid = true;
    this.billError = null;
    this.billSuccess = null;

    this.billService.updateBillStatus(this.bill.id, "Paid").subscribe({
      next: () => {
        this.zone.run(() => {
          this.isMarkingPaid = false;
          this.billSuccess = "Bill marked as paid! Order completed.";
          this.bill = null;
          setTimeout(() => {
            this.router.navigate(['/waiter/tables']);
          }, 1500);
          this.cdr.detectChanges();
        });
      },
      error: (err) => {
        this.zone.run(() => {
          this.isMarkingPaid = false;
          this.billError = err.error?.message || err.message || "Failed to process payment.";
          this.cdr.detectChanges();
        });
      }
    });
  }

  public downloadPdf() {
    if (!this.order) return;
    const classUrl = this.billService.getBillPdfUrl(this.order.id);
    this.http.get(classUrl, { responseType: 'blob' }).subscribe({
      next: (blob) => {
        const fileUrl = URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = fileUrl;
        link.download = `Bill_Order_${this.order!.id}.pdf`;
        link.click();
        URL.revokeObjectURL(fileUrl);
      },
      error: (err) => {
        console.error('Failed to download bill PDF:', err);
      }
    });
  }

  private updateElapsedTime() {
    if (!this.order || !this.order.createdAt) return;
    const diffMs = Date.now() - new Date(this.order.createdAt).getTime();
    const diffMins = Math.max(0, Math.floor(diffMs / 60000));
    if (diffMins < 1) {
      const diffSecs = Math.max(0, Math.floor(diffMs / 1000));
      this.elapsedDisplay = `${diffSecs}s`;
    } else if (diffMins < 60) {
      this.elapsedDisplay = `${diffMins} min`;
    } else {
      const diffHours = Math.floor(diffMins / 60);
      if (diffHours < 24) {
        this.elapsedDisplay = `${diffHours}h`;
      } else {
        const diffDays = Math.floor(diffHours / 24);
        this.elapsedDisplay = `${diffDays}d`;
      }
    }
  }

  public get taxPercentDisplay(): string {
    if (this.bill) {
      return this.bill.taxRate.toString();
    }
    return this.taxPercent;
  }

  public get subtotal(): number {
    if (!this.order) return 0;
    return this.order.orderItems.reduce((sum, item) => sum + (item.unitPrice * item.quantity), 0);
  }

  public get tax(): number {
    const rate = parseFloat(this.taxPercentDisplay);
    return isNaN(rate) ? 0 : this.subtotal * (rate / 100);
  }

  public get total(): number {
    return this.subtotal + this.tax;
  }
}
