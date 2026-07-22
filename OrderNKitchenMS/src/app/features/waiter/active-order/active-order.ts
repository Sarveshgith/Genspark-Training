// @feature Waiter | Active Order | Allows waiters to view table details, manage order items, request the bill, and complete checkout.
import { Component, inject, OnInit, OnDestroy, signal, ChangeDetectorRef, NgZone } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { OrderService } from '../../../core/services/order.service';
import { SignalRService } from '../../../core/services/signalr.service';
import { AuthService } from '../../../core/services/auth.service';
import { BillService } from '../../../core/services/bill.service';
import { MenuService } from '../../../core/services/menu.service';
import { MenuItemModel } from '../../../core/models/menu.model';
import { BillDto } from '../../../core/models/bill.model';
import { HttpClient } from '@angular/common/http';
import { OrderModel } from '../../../core/models/order.model';
import { Subscription } from 'rxjs';

@Component({
  selector: 'app-active-order',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
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
  private menuService = inject(MenuService);
  private zone = inject(NgZone);

  // Edit / Add Items State
  public showAddItemsModal: boolean = false;
  public allItems: MenuItemModel[] = [];
  public filteredItems: MenuItemModel[] = [];
  public itemSearchQuery: string = '';
  public selectedAddItems: { [itemId: number]: { quantity: number; notes: string } } = {};
  public isAddingItemsSubmit: boolean = false;

  // Remove Item Confirm State
  public showRemoveConfirmModal: boolean = false;
  public showCancelConfirmModal: boolean = false;
  public itemToRemoveId: number | null = null;

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

  public showBillForm: boolean = false;
  public taxRateInput: number = 8;
  public discountAmountInput: number = 0;
  public isSplittingBill: boolean = false;
  public splitBetweenPeopleInput: number = 1;
  public isPaymentComplete: boolean = false;
  public processingSplits = new Set<number>();

  public elapsedDisplay: string = '0s';
  private timerId: any;
  private subscriptions: Subscription = new Subscription();

  public get lastReminderSentTime(): number | null {
    if (typeof window !== 'undefined' && window.sessionStorage && this.order) {
      const val = sessionStorage.getItem(`reminder-cooldown-${this.order.id}`);
      return val ? parseInt(val, 10) : null;
    }
    return null;
  }
  
  public set lastReminderSentTime(val: number | null) {
    if (typeof window !== 'undefined' && window.sessionStorage && this.order) {
      if (val) {
        sessionStorage.setItem(`reminder-cooldown-${this.order.id}`, val.toString());
      } else {
        sessionStorage.removeItem(`reminder-cooldown-${this.order.id}`);
      }
    }
  }

  public isReminderCooldownActive(): boolean {
    const lastSent = this.lastReminderSentTime;
    if (!lastSent) return false;
    return Date.now() - lastSent < 5 * 60 * 1000;
  }

  public get lastCookFastSentTime(): number | null {
    if (typeof window !== 'undefined' && window.sessionStorage && this.order) {
      const val = sessionStorage.getItem(`cookfast-cooldown-${this.order.id}`);
      return val ? parseInt(val, 10) : null;
    }
    return null;
  }
  
  public set lastCookFastSentTime(val: number | null) {
    if (typeof window !== 'undefined' && window.sessionStorage && this.order) {
      if (val) {
        sessionStorage.setItem(`cookfast-cooldown-${this.order.id}`, val.toString());
      } else {
        sessionStorage.removeItem(`cookfast-cooldown-${this.order.id}`);
      }
    }
  }

  public isCookFastCooldownActive(): boolean {
    const lastSent = this.lastCookFastSentTime;
    if (!lastSent) return false;
    return Date.now() - lastSent < 2 * 60 * 1000;
  }

  public sendCookFastAlert(): void {
    if (!this.order || this.isCookFastCooldownActive()) return;
    
    this.signalRService.sendKitchenMessage("cook_fast", {
      orderId: this.order.id,
      tableId: this.tableId
    })
    .then(() => {
      this.lastCookFastSentTime = Date.now();
      this.cdr.detectChanges();
    })
    .catch(err => {
      console.error("Failed to send cook fast alert:", err);
    });
  }

  public isOverdueAndInPrep(): boolean {
    if (!this.order || this.order.status !== 2) return false;
    
    const createdAtTime = new Date(this.order.createdAt).getTime();
    const elapsedMins = (Date.now() - createdAtTime) / 60000;
    
    let prepTimeLimitMins = 15;
    if (this.order.estimatedReadyAt) {
      const limitMs = new Date(this.order.estimatedReadyAt).getTime() - createdAtTime;
      prepTimeLimitMins = limitMs / 60000;
    }
    
    return elapsedMins > (prepTimeLimitMins > 0 ? prepTimeLimitMins : 15);
  }

  public sendChefReminder(): void {
    if (!this.order || this.isReminderCooldownActive()) return;
    
    this.signalRService.sendKitchenMessage("order_reminder", {
      orderId: this.order.id,
      tableId: this.tableId
    })
    .then(() => {
      this.lastReminderSentTime = Date.now();
      this.cdr.detectChanges();
    })
    .catch(err => {
      console.error("Failed to remind chef:", err);
    });
  }

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
              if (!this.isPaymentComplete) {
                this.fetchActiveOrder();
              }
            });
          }
        });
        this.subscriptions.add(sub);
      });
    }
  }

  public fetchActiveOrder() {
    if (this.isPaymentComplete) return;
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
          if (bill && bill.statusName === 'Paid') {
            this.isPaymentComplete = true;
          }
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
    this.showBillForm = true;
    this.taxRateInput = parseFloat(this.taxPercent) || 8;
    this.discountAmountInput = 0;
    this.isSplittingBill = false;
    this.splitBetweenPeopleInput = 1;
  }

  public submitGenerateBill() {
    if (!this.order) return;
    if (this.taxRateInput < 0 || this.taxRateInput > 100) {
      this.billError = "Tax rate must be between 0 and 100.";
      return;
    }
    if (this.discountAmountInput < 0 || this.discountAmountInput > this.subtotal) {
      this.billError = `Discount cannot exceed the subtotal amount (${this.subtotal}).`;
      return;
    }
    if (this.isSplittingBill && this.splitBetweenPeopleInput < 1) {
      this.billError = "Number of people to split the bill between must be at least 1.";
      return;
    }

    this.isGeneratingBill = true;
    this.billError = null;
    this.billSuccess = null;

    const payload = {
      orderId: this.order.id,
      taxRate: this.taxRateInput,
      discountAmount: this.discountAmountInput,
      splitBetweenPeople: this.isSplittingBill ? this.splitBetweenPeopleInput : 1
    };

    this.billService.createBill(payload).subscribe({
      next: (bill) => {
        this.zone.run(() => {
          this.bill = bill;
          this.isGeneratingBill = false;
          this.showBillForm = false;
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
          this.isPaymentComplete = true;
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

  public paySplit(splitId: number) {
    if (!this.bill) return;
    this.billError = null;
    this.billSuccess = null;
    this.processingSplits.add(splitId);

    this.billService.payBillSplit(splitId).subscribe({
      next: () => {
        this.zone.run(() => {
          this.processingSplits.delete(splitId);
          this.billSuccess = "Split paid successfully!";
          
          // Eagerly update the local state so the button disappears instantly
          if (this.bill && this.bill.splits) {
            const splitIndex = this.bill.splits.findIndex(s => s.id === splitId);
            if (splitIndex !== -1) {
              this.bill.splits[splitIndex].statusName = 'Paid';
            }
          }
          this.cdr.detectChanges();
        });
      },
      error: (err) => {
        this.zone.run(() => {
          this.processingSplits.delete(splitId);
          this.billError = err.error?.message || err.message || "Failed to process split payment.";
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

  // Modify / Add Items functions
  public openAddItems(): void {
    this.billError = null;
    this.billSuccess = null;
    this.menuService.getMenuItems({ pageSize: 100 }).subscribe({
      next: (items) => {
        this.zone.run(() => {
          this.allItems = items.filter(item => item.isAvailable);
          this.filteredItems = [...this.allItems];
          this.itemSearchQuery = '';
          this.selectedAddItems = {};
          this.showAddItemsModal = true;
          this.cdr.detectChanges();
        });
      },
      error: (err) => {
        console.error("Failed to load menu items for adding:", err);
        this.billError = "Failed to load menu items.";
      }
    });
  }

  public filterAddItems(): void {
    const q = this.itemSearchQuery.trim().toLowerCase();
    if (q) {
      this.filteredItems = this.allItems.filter(item => 
        item.name.toLowerCase().includes(q) || 
        item.description.toLowerCase().includes(q)
      );
    } else {
      this.filteredItems = [...this.allItems];
    }
  }

  public toggleItemSelection(item: MenuItemModel): void {
    if (this.selectedAddItems[item.id]) {
      delete this.selectedAddItems[item.id];
    } else {
      this.selectedAddItems[item.id] = { quantity: 1, notes: '' };
    }
  }

  public incrementAddQty(itemId: number): void {
    if (this.selectedAddItems[itemId]) {
      this.selectedAddItems[itemId].quantity++;
    }
  }

  public decrementAddQty(itemId: number): void {
    if (this.selectedAddItems[itemId]) {
      if (this.selectedAddItems[itemId].quantity > 1) {
        this.selectedAddItems[itemId].quantity--;
      } else {
        delete this.selectedAddItems[itemId];
      }
    }
  }

  public submitAddItems(): void {
    if (!this.order) return;
    const itemIds = Object.keys(this.selectedAddItems).map(Number);
    if (itemIds.length === 0) {
      this.billError = "Please select at least one item to add.";
      this.showAddItemsModal = false;
      return;
    }

    const payload = itemIds.map(id => ({
      menuItemId: id,
      quantity: this.selectedAddItems[id].quantity,
      notes: this.selectedAddItems[id].notes
    }));

    this.isAddingItemsSubmit = true;
    this.billError = null;
    this.billSuccess = null;

    this.orderService.addOrderItems(this.order.id, payload).subscribe({
      next: (updatedOrder) => {
        this.zone.run(() => {
          this.order = updatedOrder;
          this.showAddItemsModal = false;
          this.isAddingItemsSubmit = false;
          this.billSuccess = "Items successfully added to the order.";
          this.fetchActiveOrder();
          this.cdr.detectChanges();
        });
      },
      error: (err) => {
        this.zone.run(() => {
          this.isAddingItemsSubmit = false;
          this.showAddItemsModal = false;
          this.billError = err.error?.message || err.message || "Failed to add items to order.";
          this.cdr.detectChanges();
        });
      }
    });
  }

  public removeItem(orderItemId: number): void {
    if (!this.order) return;
    this.itemToRemoveId = orderItemId;
    this.showRemoveConfirmModal = true;
  }

  public confirmRemoveItem(): void {
    if (!this.order || this.itemToRemoveId === null) return;
    const orderItemId = this.itemToRemoveId;
    
    this.billError = null;
    this.billSuccess = null;
    this.showRemoveConfirmModal = false;

    this.orderService.removeOrderItem(this.order.id, orderItemId).subscribe({
      next: () => {
        this.zone.run(() => {
          this.billSuccess = "Item successfully removed from the order.";
          this.itemToRemoveId = null;
          this.fetchActiveOrder();
        });
      },
      error: (err) => {
        this.zone.run(() => {
          this.itemToRemoveId = null;
          this.billError = err.error?.message || err.message || "Failed to remove item from order.";
          this.cdr.detectChanges();
        });
      }
    });
  }

  public cancelOrder(): void {
    if (!this.order) return;
    this.showCancelConfirmModal = true;
  }

  public confirmCancelOrder(): void {
    if (!this.order) return;
    this.showCancelConfirmModal = false;
    this.billError = null;
    this.billSuccess = null;
    this.orderService.updateOrderStatus(this.order.id, 6).subscribe({
      next: () => {
        this.zone.run(() => {
          this.billSuccess = "Order has been cancelled.";
          this.fetchActiveOrder();
        });
      },
      error: (err) => {
        this.zone.run(() => {
          console.error("Failed to cancel order:", err);
          this.billError = err.error?.message || err.message || "Failed to cancel order.";
          this.cdr.detectChanges();
        });
      }
    });
  }
}
