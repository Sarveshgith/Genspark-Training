import { Component } from '@angular/core';
import { AuthService } from '../../services/auth.service';
import { AsyncPipe } from '@angular/common';

@Component({
  selector: 'app-header',
  standalone: true,
  imports: [AsyncPipe],
  templateUrl: './header.html',
  styleUrl: './header.css'
})
export class Header {
  user$;

  constructor(private authService: AuthService) {
    this.user$ = this.authService.currentUser$;
  }

  logout() {
    this.authService.logout();
  }
}
