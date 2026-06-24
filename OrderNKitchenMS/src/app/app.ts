import { Component, inject, signal } from '@angular/core';
import { RouterOutlet, Router } from '@angular/router';
import { SidebarComponent } from './core/components/sidebar/sidebar';
import { AuthService } from './core/services/auth.service';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, SidebarComponent],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App {
  protected readonly title = signal('OrderNKitchenMS');
  public authService = inject(AuthService);
  private router = inject(Router);

  public showSidebar(): boolean {
    const url = this.router.url;
    // Don't show sidebar on login, register, guest landing, or root route
    if (url.includes('/login') || url.includes('/register') || url.includes('/guest/landing') || url === '/' || url === '' || url.includes('/kitchen')) {
      return false;
    }
    // Show sidebar if token is present
    return !!this.authService.getToken();
  }
}
