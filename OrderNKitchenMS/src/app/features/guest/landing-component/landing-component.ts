// @feature Guest | Landing Portal | Handles entry portal for guests via table codes and coordinates active order state.
import { Component, inject, OnInit, OnDestroy, ChangeDetectorRef, NgZone, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { AuthService } from '../../../core/services/auth.service';
import { OrderService } from '../../../core/services/order.service';
import { MenuService } from '../../../core/services/menu.service';
import { BillService } from '../../../core/services/bill.service';
import { SignalRService } from '../../../core/services/signalr.service';
import { OrderTracker } from '../order-tracker/order-tracker';
import { Subscription } from 'rxjs';

@Component({
  selector: 'app-landing-component',
  imports: [CommonModule, OrderTracker, RouterLink],
  templateUrl: './landing-component.html',
  styleUrl: './landing-component.css',
})
export class LandingComponent implements OnInit, OnDestroy {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private authService = inject(AuthService);
  private orderService = inject(OrderService);
  private menuService = inject(MenuService);
  private billService = inject(BillService);
  private signalRService = inject(SignalRService);
  private cdr = inject(ChangeDetectorRef);
  private zone = inject(NgZone);
  private http = inject(HttpClient);

  public tableNumber: number = 0;
  public isLoading: boolean = true;
  public errorMessage: string | null = null;
  public orderDetails: any = null;
  public billDetails = signal<any | null>(null);
  public taxPercent: string = 'X';

  public guestState = signal<'waiting' | 'tracking' | 'bill' | 'paid' | 'session-ending'>('waiting');
  public countdownSeconds = signal<number>(10);
  public sessionEnding = signal<boolean>(false);
  private countdownInterval: any;

  private menuPriceMap: { [name: string]: number } = {};
  private menuImageMap: { [name: string]: string } = {};
  private rawOrderDetails: any = null;
  private subscriptions: Subscription = new Subscription();

  ngOnInit() {
    // Read route parameters (for secret) and query parameters (fallback)
    this.route.params.subscribe(routeParams => {
      const secret = routeParams['secret'];
      if (secret) {
        this.initializeSession(secret);
      } else {
        this.route.queryParams.subscribe(queryParams => {
          const qSecret = queryParams['secret'];
          if (qSecret) {
            this.initializeSession(qSecret);
          } else {
            this.initializeSession();
          }
        });
      }
    });
  }

  ngOnDestroy() {
    this.subscriptions.unsubscribe();
    this.signalRService.disconnect();
  }

  private initializeSession(secret?: string) {
    const existingToken = this.authService.getToken();
    const storedSecret = typeof window !== 'undefined' ? sessionStorage.getItem('tableSecret') : null;

    if (secret) {
      const isTokenValid = existingToken && !this.authService.isTokenExpired(existingToken);
      const secretMatches = storedSecret === secret;

      if (isTokenValid && secretMatches) {
        this.tableNumber = this.authService.getTableIdFromToken(existingToken!);
        this.loadMenuAndTrackOrder();
      } else {
        this.authService.logout();
        this.authService.guestLogin({ secret }).subscribe({
          next: () => {
            const token = this.authService.getToken();
            if (token) {
              this.tableNumber = this.authService.getTableIdFromToken(token);
            }
            this.loadMenuAndTrackOrder();
          },
          error: (err: any) => {
            this.errorMessage = "Session Expired. Please scan the QR code again.";
            this.isLoading = false;
            this.cdr.detectChanges();
          }
        });
      }
    } else {
      if (existingToken && !this.authService.isTokenExpired(existingToken)) {
        this.tableNumber = this.authService.getTableIdFromToken(existingToken);
        this.loadMenuAndTrackOrder();
      } else {
        this.errorMessage = "Session Expired. Please scan the QR code again.";
        this.isLoading = false;
        this.cdr.detectChanges();
      }
    }
  }

  private loadMenuAndTrackOrder() {
    this.menuService.getMenuItems({ pageSize: 100 }).subscribe({
      next: (menuItems) => {
        menuItems.forEach(item => {
          this.menuPriceMap[item.name] = item.price;
          this.menuImageMap[item.name] = item.imageUrl;
        });

        const token = this.authService.getToken();
        if (token) {
          this.signalRService.connect(token).then(() => {
            const signalSub = this.signalRService.orderUpdate$.subscribe({
              next: (updatedTrackingInfo) => {
                this.zone.run(() => {
                  this.rawOrderDetails = updatedTrackingInfo;
                  this.enrichOrderDetails();
                  if (this.rawOrderDetails && this.rawOrderDetails.orderId) {
                    this.fetchBillDetails(this.rawOrderDetails.orderId);
                  }
                  this.cdr.detectChanges();
                });
              }
            });
            this.subscriptions.add(signalSub);

            const tablesSub = this.signalRService.tablesUpdated$.subscribe({
              next: () => {
                this.zone.run(() => {
                  console.log('Table state update signal received. Refreshing guest order status...');
                  this.fetchOrderTracking();
                });
              }
            });
            this.subscriptions.add(tablesSub);

            const billGenSub = this.billService.billGenerated$.subscribe({
              next: (bill) => {
                this.zone.run(() => {
                  console.log('Real-time bill generated event received:', bill);
                  this.billDetails.set(bill);
                  this.guestState.set('bill');
                  this.cdr.detectChanges();
                });
              }
            });
            this.subscriptions.add(billGenSub);

            const billPaidSub = this.billService.billPaid$.subscribe({
              next: (bill) => {
                this.zone.run(() => {
                  console.log('Real-time bill paid event received:', bill);
                  this.billDetails.set(bill);
                  this.guestState.set('paid');
                  this.startSessionEndGracePeriod();
                  this.cdr.detectChanges();
                });
              }
            });
            this.subscriptions.add(billPaidSub);

            const sessionEndedSub = this.signalRService.guestSessionEnded$.subscribe({
              next: () => {
                this.zone.run(() => {
                  console.log('Guest session ended SignalR event received.');
                  this.startSessionEndGracePeriod();
                });
              }
            });
            this.subscriptions.add(sessionEndedSub);
          });
        }

        this.fetchOrderTracking();
      },
      error: () => {
        this.errorMessage = "Failed to load menu data. Please try again.";
        this.isLoading = false;
        this.cdr.detectChanges();
      }
    });
  }

  public fetchOrderTracking() {
    this.orderService.trackOrder().subscribe({
      next: (trackingInfo) => {
        this.zone.run(() => {
          try {
            const trackingTableId = trackingInfo.tableId ?? (trackingInfo as any).TableId;
            if (trackingTableId !== undefined && trackingTableId !== null && trackingTableId !== this.tableNumber) {
              this.errorMessage = "Access Denied: The table ID in your session does not match this table.";
              this.orderDetails = null;
              this.isLoading = false;
              this.cdr.detectChanges();
              return;
            }
            if (trackingTableId) {
              this.tableNumber = trackingTableId;
            }
            this.rawOrderDetails = trackingInfo;
            this.enrichOrderDetails();
            if (this.rawOrderDetails && this.rawOrderDetails.orderId) {
              this.fetchBillDetails(this.rawOrderDetails.orderId);
            } else {
              this.guestState.set('waiting');
            }
          } catch (e) {
            console.error('Error in next/enrich callback of fetchOrderTracking:', e);
            this.errorMessage = "Error parsing order details: " + e;
          } finally {
            this.isLoading = false;
            this.cdr.detectChanges();
          }
        });
      },
      error: (err) => {
        this.zone.run(() => {
          console.error('Order tracking fetch error:', err);
          if (err.status === 401) {
            this.errorMessage = "Session Expired. Please scan the QR code again.";
            this.orderDetails = null;
          } else if (err.status === 404) {
            this.orderDetails = null;
            this.guestState.set('waiting');
          } else {
            this.errorMessage = "Failed to retrieve order tracking information.";
          }
          this.isLoading = false;
          this.cdr.detectChanges();
        });
      }
    });
  }

  private enrichOrderDetails() {
    if (!this.rawOrderDetails) {
      this.orderDetails = null;
      return;
    }

    try {
      if (!this.rawOrderDetails.orderItems) {
        throw new Error('rawOrderDetails.orderItems is undefined or null');
      }

      const orderItems = this.rawOrderDetails.orderItems.map((item: any) => {
        const unitPrice = item.unitPrice ?? this.menuPriceMap[item.menuItemName] ?? 0;
        const dbImageUrl = this.menuImageMap[item.menuItemName];
        return {
          ...item,
          unitPrice: unitPrice,
          totalPrice: unitPrice * item.quantity,
          imageUrl: this.getItemImageUrl(item.menuItemName, dbImageUrl)
        };
      });

      const subtotal = orderItems.reduce((acc: number, item: any) => acc + item.totalPrice, 0);
      const bill = this.billDetails();
      if (bill && bill.taxRate !== undefined && bill.taxRate !== null) {
        this.taxPercent = String(bill.taxRate);
      } else {
        this.taxPercent = '8';
      }
      const parsedRate = parseFloat(this.taxPercent);
      const taxRate = isNaN(parsedRate) ? 0.08 : parsedRate / 100;
      const tax = subtotal * taxRate;
      const total = subtotal + tax;

      this.orderDetails = {
        ...this.rawOrderDetails,
        orderItems,
        subtotal,
        tax,
        total
      };
    } catch (e) {
      console.error('Error occurred in enrichOrderDetails:', e);
      this.errorMessage = "Error structuring order details: " + e;
      this.orderDetails = null;
      throw e;
    }
  }

  private getItemImageUrl(name: string, dbImageUrl?: string): string {
    if (dbImageUrl && dbImageUrl.trim() !== '' && dbImageUrl !== 'none' && dbImageUrl !== '/favicon.ico') {
      return dbImageUrl;
    }
    return '/fallback_dish.png';
  }

  public fetchBillDetails(orderId: number) {
    this.billService.getBillByOrderId(orderId).subscribe({
      next: (bill) => {
        this.zone.run(() => {
          this.billDetails.set(bill);
          if (bill) {
            if (bill.statusName === 'Paid') {
              this.guestState.set('paid');
            } else if (bill.statusName === 'Pending') {
              this.guestState.set('bill');
            } else {
              this.guestState.set('tracking');
            }
          } else {
            this.guestState.set('tracking');
          }
          this.cdr.detectChanges();
        });
      },
      error: (err) => {
        this.zone.run(() => {
          if (err?.status === 401) {
            this.errorMessage = "Session Expired. Please scan the QR code again.";
          }
          this.billDetails.set(null);
          this.guestState.set('tracking');
          this.cdr.detectChanges();
        });
      }
    });
  }

  public startSessionEndGracePeriod() {
    if (this.sessionEnding()) {
      return;
    }
    this.sessionEnding.set(true);

    if (this.billDetails() && this.billDetails().statusName === 'Paid') {
      this.guestState.set('paid');
    } else {
      this.guestState.set('session-ending');
    }

    this.countdownSeconds.set(10);
    if (this.countdownInterval) {
      clearInterval(this.countdownInterval);
    }
    this.countdownInterval = setInterval(() => {
      this.countdownSeconds.update(s => s - 1);
      if (this.countdownSeconds() <= 0) {
        clearInterval(this.countdownInterval);
        this.signalRService.disconnect();
        this.authService.logout();
        this.router.navigate(['/session-ended'], { state: { endedBySession: true, tableNumber: this.tableNumber } });
      }
      this.cdr.detectChanges();
    }, 1000);
  }

  public downloadReceipt() {
    if (!this.orderDetails) return;
    const url = this.billService.getBillPdfUrl(this.orderDetails.orderId);
    this.http.get(url, { responseType: 'blob' }).subscribe({
      next: (blob) => {
        const link = document.createElement('a');
        link.href = URL.createObjectURL(blob);
        link.download = `receipt_order_${this.orderDetails.orderId}.pdf`;
        link.click();
        URL.revokeObjectURL(link.href);
      },
      error: (err) => {
        console.error('Failed to download PDF:', err);
        window.open(url, '_blank');
      }
    });
  }

  public showWaiterModal = signal<boolean>(false);
  public selectedReasonText = signal<string>('');
  private waiterTimeoutId: any = null;

  public callWaiter() {
    this.triggerWaiterCall('ordering');
  }

  public triggerWaiterCall(reason: 'assistance' | 'ordering' | 'bill') {
    this.signalRService.guestCallWaiter(reason).then(() => {
      const text = reason === 'assistance' ? 'General Assistance'
        : reason === 'ordering' ? 'Order Help'
          : 'Request Bill';
      this.selectedReasonText.set(text);

      if (this.waiterTimeoutId) {
        clearTimeout(this.waiterTimeoutId);
      }
      this.showWaiterModal.set(true);
      this.waiterTimeoutId = setTimeout(() => {
        this.showWaiterModal.set(false);
        this.waiterTimeoutId = null;
      }, 5000);
    }).catch(err => {
      console.error("Failed to call waiter:", err);
    });
  }

  public closeWaiterModal() {
    this.showWaiterModal.set(false);
    if (this.waiterTimeoutId) {
      clearTimeout(this.waiterTimeoutId);
      this.waiterTimeoutId = null;
    }
  }
}
