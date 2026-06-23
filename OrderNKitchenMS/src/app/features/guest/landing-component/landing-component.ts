import { Component, inject, OnInit, OnDestroy, ChangeDetectorRef, NgZone } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { OrderService } from '../../../core/services/order.service';
import { MenuService } from '../../../core/services/menu.service';
import { SignalRService } from '../../../core/services/signalr.service';
import { OrderTracker } from '../order-tracker/order-tracker';
import { Subscription } from 'rxjs';

@Component({
  selector: 'app-landing-component',
  imports: [CommonModule, OrderTracker],
  templateUrl: './landing-component.html',
  styleUrl: './landing-component.css',
})
export class LandingComponent implements OnInit, OnDestroy {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private authService = inject(AuthService);
  private orderService = inject(OrderService);
  private menuService = inject(MenuService);
  private signalRService = inject(SignalRService);
  private cdr = inject(ChangeDetectorRef);
  private zone = inject(NgZone);

  public tableNumber: number = 0;
  public isLoading: boolean = true;
  public errorMessage: string | null = null;
  public orderDetails: any = null;

  private menuPriceMap: { [name: string]: number } = {};
  private rawOrderDetails: any = null;
  private subscriptions: Subscription = new Subscription();

  ngOnInit() {
    this.route.queryParams.subscribe(params => {
      const tableIdParam = params['tableId'];
      if (!tableIdParam) {
        this.errorMessage = "Table Number is required to track orders.";
        this.isLoading = false;
        this.cdr.detectChanges();
        return;
      }

      this.tableNumber = parseInt(tableIdParam, 10);
      if (isNaN(this.tableNumber) || this.tableNumber <= 0) {
        this.errorMessage = "Invalid Table Number.";
        this.isLoading = false;
        this.cdr.detectChanges();
        return;
      }

      this.initializeSession();
    });
  }

  ngOnDestroy() {
    this.subscriptions.unsubscribe();
    this.signalRService.disconnect();
  }

  private initializeSession() {
    const existingToken = this.authService.getToken();

    if (!existingToken) {
      this.authService.guestLogin({ tableId: this.tableNumber }).subscribe({
        next: () => {
          this.loadMenuAndTrackOrder();
        },
        error: (err: any) => {
          this.errorMessage = "Failed to create a guest session. Please scan the QR code again.";
          this.isLoading = false;
          this.cdr.detectChanges();
        }
      });
    } else {
      this.loadMenuAndTrackOrder();
    }
  }

  private loadMenuAndTrackOrder() {
    this.menuService.getMenuItems({ pageSize: 100 }).subscribe({
      next: (menuItems) => {
        menuItems.forEach(item => {
          this.menuPriceMap[item.name] = item.price;
        });

        const token = this.authService.getToken();
        if (token) {
          this.signalRService.connect(token).then(() => {
            const signalSub = this.signalRService.orderUpdate$.subscribe({
              next: (updatedTrackingInfo) => {
                this.zone.run(() => {
                  console.log('Real-time order update received:', updatedTrackingInfo);
                  this.rawOrderDetails = updatedTrackingInfo;
                  this.enrichOrderDetails();
                  this.cdr.detectChanges();
                });
              }
            });
            this.subscriptions.add(signalSub);
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
            console.log('Order tracking data retrieved successfully:', trackingInfo);
            this.rawOrderDetails = trackingInfo;
            this.enrichOrderDetails();
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
          if (err.status === 404) {
            this.orderDetails = null;
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
        const unitPrice = this.menuPriceMap[item.menuItemName] ?? 0;
        return {
          ...item,
          unitPrice: unitPrice,
          totalPrice: unitPrice * item.quantity,
          imageUrl: this.getItemImageUrl(item.menuItemName)
        };
      });

      const subtotal = orderItems.reduce((acc: number, item: any) => acc + item.totalPrice, 0);
      const tax = subtotal * 0.08;
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
      throw e; // rethrow to be caught by fetchOrderTracking's try-catch
    }
  }

  private getItemImageUrl(name: string): string {
    const sanitized = name.toLowerCase().trim();
    if (sanitized.includes('wagyu') || sanitized.includes('steak')) {
      return '/wagyu_ribeye.png';
    }
    if (sanitized.includes('salad') || sanitized.includes('beet')) {
      return '/beet_salad.png';
    }
    if (sanitized.includes('spritz') || sanitized.includes('drink') || sanitized.includes('cocktail')) {
      return '/summer_spritz.png';
    }
    // Standard default fallback
    return '/favicon.ico';
  }
}
