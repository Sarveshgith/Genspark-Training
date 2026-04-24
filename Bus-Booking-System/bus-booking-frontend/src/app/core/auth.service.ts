import { Injectable, inject } from '@angular/core';
import { Router } from '@angular/router';
import { Observable, tap } from 'rxjs';
import { ApiService } from './api.service';
import { AuthResponse } from './models';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly api = inject(ApiService);
  private readonly router = inject(Router);

  private readonly tokenKey = 'bbs_token';
  private readonly roleKey = 'bbs_role';
  private readonly userIdKey = 'bbs_user_id';
  private readonly emailKey = 'bbs_user_email';

  login(payload: { email: string; password: string }): Observable<AuthResponse> {
    return this.api.post<AuthResponse>('auth/login', payload).pipe(
      tap((response) => {
        this.setSession(response);
      }),
    );
  }

  register(payload: {
    name: string;
    email: string;
    phone: string;
    password: string;
    role: string;
    licenseNumber?: string;
  }): Observable<AuthResponse> {
    return this.api.post<AuthResponse>('auth/register', payload).pipe(
      tap((response) => {
        this.setSession(response);
      }),
    );
  }

  setSession(response: AuthResponse): void {
    localStorage.setItem(this.tokenKey, response.accessToken);
    localStorage.setItem(this.roleKey, response.role);
    localStorage.setItem(this.userIdKey, response.userId);
    localStorage.setItem(this.emailKey, response.email);
  }

  clearSession(): void {
    localStorage.removeItem(this.tokenKey);
    localStorage.removeItem(this.roleKey);
    localStorage.removeItem(this.userIdKey);
    localStorage.removeItem(this.emailKey);
  }

  logout(): void {
    this.clearSession();
    this.router.navigate(['/login']);
  }

  isAuthenticated(): boolean {
    return !!this.getToken();
  }

  getToken(): string | null {
    return localStorage.getItem(this.tokenKey);
  }

  getRole(): string {
    return localStorage.getItem(this.roleKey) ?? '';
  }

  getUserId(): string {
    return localStorage.getItem(this.userIdKey) ?? '';
  }

  getEmail(): string {
    return localStorage.getItem(this.emailKey) ?? '';
  }

  redirectByRole(): void {
    const role = this.getRole();
    if (role === 'Admin') {
      this.router.navigate(['/admin/dashboard']);
      return;
    }

    if (role === 'Operator') {
      this.router.navigate(['/operator/dashboard']);
      return;
    }

    this.router.navigate(['/trips']);
  }
}
