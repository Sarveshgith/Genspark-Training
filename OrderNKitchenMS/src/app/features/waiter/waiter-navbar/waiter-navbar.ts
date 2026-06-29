// @feature Waiter | Navigation Bar | Standardized top navigation bar for the waiter portal featuring route links and logout.
import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterLink, RouterLinkActive } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-waiter-navbar',
  standalone: true,
  imports: [CommonModule, RouterLink, RouterLinkActive],
  templateUrl: './waiter-navbar.html',
  styleUrl: './waiter-navbar.css'
})
export class WaiterNavbarComponent {
  public authService = inject(AuthService);
  private router = inject(Router);

  // Responsive state
  public isMobileMenuOpen = signal<boolean>(false);

  public toggleMobileMenu(): void {
    this.isMobileMenuOpen.set(!this.isMobileMenuOpen());
  }

  public closeMobileMenu(): void {
    this.isMobileMenuOpen.set(false);
  }

  public logout(): void {
    this.closeMobileMenu();
    this.authService.logout();
    this.router.navigate(['/login']);
  }
}
