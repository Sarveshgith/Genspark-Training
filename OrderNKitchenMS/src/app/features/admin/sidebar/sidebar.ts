// @feature Admin | Navigation Sidebar | Collapsible sidebar dashboard menu containing management routes for Admin functions.
import { Component, inject, signal, OnInit, OnDestroy } from '@angular/core';
import { Router, RouterLink, RouterLinkActive } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { SignalRService } from '../../../core/services/signalr.service';
import { UserService } from '../../../core/services/user.service';
import { Subscription } from 'rxjs';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-sidebar',
  standalone: true,
  imports: [RouterLink, RouterLinkActive, CommonModule],
  templateUrl: './sidebar.html',
  styleUrl: './sidebar.css'
})
export class SidebarComponent implements OnInit, OnDestroy {
  public authService = inject(AuthService);
  public router = inject(Router);
  private signalRService = inject(SignalRService);
  public userService = inject(UserService);

  // Alert feed state
  public isAlertFeedOpen = signal<boolean>(false);
  public alerts = signal<any[]>([]);

  private sub = new Subscription();

  ngOnInit(): void {
    const role = this.authService.getRole()?.toLowerCase();
    const token = this.authService.getToken();
    
    if (role === 'admin' && token) {
      this.userService.updatePendingCount();
      this.signalRService.connect(token);
      
      this.sub.add(
        this.signalRService.adminAlert$.subscribe(alert => {
          this.alerts.update(list => [alert, ...list]);
        })
      );
    }
  }

  toggleAlertFeed(): void {
    this.isAlertFeedOpen.update(val => !val);
  }

  dismissAlert(index: number): void {
    this.alerts.update(list => list.filter((_, i) => i !== index));
  }

  clearAllAlerts(): void {
    this.alerts.set([]);
  }

  logout(): void {
    this.authService.logout();
    this.router.navigate(['/login']);
  }

  ngOnDestroy(): void {
    this.sub.unsubscribe();
  }
}

