import { Component, inject, signal } from '@angular/core';
import { RouterOutlet, Router } from '@angular/router';
import { SidebarComponent } from './features/admin/sidebar/sidebar';
import { BottomNavComponent } from './features/admin/bottom-nav/bottom-nav';
import { WaiterNavbarComponent } from './features/waiter/waiter-navbar/waiter-navbar';
import { ToastContainerComponent } from './core/components/toast-container/toast-container';
import { AuthService } from './core/services/auth.service';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, SidebarComponent, BottomNavComponent, WaiterNavbarComponent, ToastContainerComponent],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App {
  protected readonly title = signal('OrderNKitchenMS');
  public authService = inject(AuthService);
  private router = inject(Router);

  public showSidebar(): boolean {
    return this.authService.getRole()?.toLowerCase() === 'admin' && !!this.authService.getToken();
  }

  public showWaiterNavbar(): boolean {
    return this.authService.getRole()?.toLowerCase() === 'waiter' && !!this.authService.getToken();
  }
}
