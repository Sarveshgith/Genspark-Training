import { Component, inject, signal, OnInit, OnDestroy } from '@angular/core';
import { Router, RouterLink, RouterLinkActive } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { SignalRService } from '../../../core/services/signalr.service';
import { UserService } from '../../../core/services/user.service';
import { Subscription } from 'rxjs';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-bottom-nav',
  standalone: true,
  imports: [RouterLink, RouterLinkActive, CommonModule],
  templateUrl: './bottom-nav.html',
  styleUrl: './bottom-nav.css'
})
export class BottomNavComponent implements OnInit, OnDestroy {
  public authService = inject(AuthService);
  public router = inject(Router);
  private signalRService = inject(SignalRService);
  public userService = inject(UserService);

  // Bottom Sheet Visibility
  public isBottomSheetOpen = signal<boolean>(false);

  // Alert Feed Drawer Visibility (inside More sheet or bottom nav)
  public isAlertFeedOpen = signal<boolean>(false);
  public alerts = signal<any[]>([]);

  private sub = new Subscription();

  ngOnInit(): void {
    const role = this.authService.getRole()?.toLowerCase();
    const token = this.authService.getToken();

    if (role === 'admin' && token) {
      this.userService.updatePendingCount();
      // Connect to Hub (idempotent in SignalRService)
      this.signalRService.connect(token);

      this.sub.add(
        this.signalRService.adminAlert$.subscribe(alert => {
          this.alerts.update(list => [alert, ...list]);
        })
      );
    }
  }

  toggleBottomSheet(): void {
    this.isBottomSheetOpen.update(val => !val);
  }

  closeBottomSheet(): void {
    this.isBottomSheetOpen.set(false);
  }

  toggleAlertFeed(): void {
    this.isAlertFeedOpen.update(val => !val);
  }

  dismissAlert(index: number, event: MouseEvent): void {
    event.stopPropagation();
    this.alerts.update(list => list.filter((_, i) => i !== index));
  }

  clearAllAlerts(event: MouseEvent): void {
    event.stopPropagation();
    this.alerts.set([]);
  }

  logout(): void {
    this.closeBottomSheet();
    this.signalRService.disconnect();
    this.authService.logout();
    this.router.navigate(['/login']);
  }

  ngOnDestroy(): void {
    this.sub.unsubscribe();
  }
}
