import { Component, inject } from '@angular/core';
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

  public logout(): void {
    this.authService.logout();
    this.router.navigate(['/login']);
  }
}
