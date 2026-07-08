// @feature Waiter | Navigation Bar | Standardized top navigation bar for the waiter portal featuring route links and logout.
import { Component, inject, signal, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterLink, RouterLinkActive } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { SignalRService } from '../../../core/services/signalr.service';
import { AudioService } from '../../../core/services/audio.service';
import { Subscription } from 'rxjs';

@Component({
  selector: 'app-waiter-navbar',
  standalone: true,
  imports: [CommonModule, RouterLink, RouterLinkActive],
  templateUrl: './waiter-navbar.html',
  styleUrl: './waiter-navbar.css'
})
export class WaiterNavbarComponent implements OnInit, OnDestroy {
  public authService = inject(AuthService);
  private router = inject(Router);
  private signalRService = inject(SignalRService);
  private audioService = inject(AudioService);

  // Responsive state
  public isMobileMenuOpen = signal<boolean>(false);

  // Notification drawer state
  public isNotificationDrawerOpen = signal<boolean>(false);
  public notifications = signal<any[]>([]);
  public unreadCount = signal<number>(0);

  private sub = new Subscription();

  ngOnInit() {
    const token = this.authService.getToken();
    if (token) {
      this.signalRService.connect(token);
      this.sub.add(
        this.signalRService.floorMessage$.subscribe(notification => {
          this.notifications.update(list => [notification, ...list]);
          this.audioService.playNotificationChime();
          if (!this.isNotificationDrawerOpen()) {
            this.unreadCount.update(c => c + 1);
          }
        })
      );
    }
  }

  ngOnDestroy() {
    this.sub.unsubscribe();
  }

  public toggleNotificationDrawer() {
    this.isNotificationDrawerOpen.update(val => !val);
    if (this.isNotificationDrawerOpen()) {
      this.unreadCount.set(0);
    }
  }

  public handleNotificationClick(notif: any) {
    this.isNotificationDrawerOpen.set(false);
    if (notif.tableId) {
      // Force reload if already on the same tableId route
      this.router.navigateByUrl('/', { skipLocationChange: true }).then(() => {
        this.router.navigate(['/waiter/active-order', notif.tableId]);
      });
    }
  }

  public toggleMobileMenu(): void {
    this.isMobileMenuOpen.set(!this.isMobileMenuOpen());
  }

  public closeMobileMenu(): void {
    this.isMobileMenuOpen.set(false);
  }

  public logout(): void {
    this.closeMobileMenu();
    this.signalRService.disconnect();
    this.authService.logout();
    this.router.navigate(['/login']);
  }
}
