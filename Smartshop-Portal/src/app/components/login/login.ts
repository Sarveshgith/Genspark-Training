import { Component, signal } from '@angular/core';
import { LoginModel } from '../../models/login.model';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { AuthService } from '../../services/auth.service';
import { form, FormField, minLength, required } from '@angular/forms/signals';
import { Router } from '@angular/router';

@Component({
  selector: 'app-login',
  imports: [FormsModule, ReactiveFormsModule,FormField],
  templateUrl: './login.html',
  styleUrl: './login.css',
})
export class Login {
  progress = signal(false);

  loginModel = signal<LoginModel>({
    username: '',
    password: ''
  });

  constructor(private authService: AuthService, private router: Router) {}

  loginForm = form(this.loginModel, (path) => {
    required(path.username, {message: 'Username is required'});
    required(path.password, {message: 'Password is required'});
    minLength(path.password, 10, {message: 'Password must be at least 10 characters long'});
  });

  onSubmit() {
    if(this.loginForm().invalid()){
      alert('Please fill in all required fields');
      return;
    }

    this.progress.set(true);

    this.authService.login(this.loginForm().value()).subscribe({
      next: (user) => {
        this.progress.set(false);
        console.log('Login successful:', user);
        this.router.navigate(['/']);
      },
      error: (error) => {
        alert(error.message || 'Login failed. Please try again.');
        this.progress.set(false);
      }
    });
  }
}