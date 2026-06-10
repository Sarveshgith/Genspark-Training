import { Component } from '@angular/core';
import { AuthService } from '../../services/auth.service';
import { AsyncPipe } from '@angular/common';

@Component({
  selector: 'app-profile',
  imports: [AsyncPipe],
  templateUrl: './profile.html',
  styleUrl: './profile.css',
})
export class Profile {
  
  user$;

  constructor(private authService: AuthService){
    this.user$ = this.authService.currentUser$;
  }

  logout() {
    this.authService.logout();
  }
}
