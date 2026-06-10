import { Component, OnInit } from '@angular/core';
import { AuthService } from '../../services/auth.service';
import { AsyncPipe } from '@angular/common';
import { Router, RouterLink } from '@angular/router';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [AsyncPipe, RouterLink],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.css'
})
export class Dashboard implements OnInit {
  user$;

  constructor(private authService: AuthService, private router: Router) {
    this.user$ = this.authService.currentUser$;
  }

  ngOnInit() {
    this.authService.currentUser$.subscribe(user => {
      if (!user) {
        this.router.navigate(['/login']);
      }
    });
  }
}
