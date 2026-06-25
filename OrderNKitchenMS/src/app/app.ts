import { Component, inject, signal } from '@angular/core';
import { RouterOutlet, Router } from '@angular/router';
import { SidebarComponent } from './features/admin/sidebar/sidebar';
import { WaiterNavbarComponent } from './features/waiter/waiter-navbar/waiter-navbar';
import { AuthService } from './core/services/auth.service';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, SidebarComponent, WaiterNavbarComponent],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App {
  protected readonly title = signal('OrderNKitchenMS');
  public authService = inject(AuthService);
  private router = inject(Router);

  public showSidebar(): boolean {
    const url = this.router.url;
    if (url.includes('/login') || url.includes('/register') || url.includes('/guest/landing') || url === '/' || url === '' || url.includes('/kitchen')) {
      return false;
    }
    if (this.authService.getRole()?.toLowerCase() === 'waiter') {
      return false;
    }
    return !!this.authService.getToken();
  }

  public showWaiterNavbar(): boolean {
    const url = this.router.url;
    if (url.includes('/login') || url.includes('/register') || url.includes('/guest/landing') || url === '/' || url === '' || url.includes('/kitchen')) {
      return false;
    }
    return this.authService.getRole()?.toLowerCase() === 'waiter' && !!this.authService.getToken();
  }
}
