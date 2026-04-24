import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../core/auth.service';

@Component({
  selector: 'app-login-page',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  template: `
    <div class="page-container">
      <h2>Login</h2>

      <form [formGroup]="form" (ngSubmit)="submit()" class="form-card">
        <label>Email</label>
        <input type="email" formControlName="email" />

        <label>Password</label>
        <input type="password" formControlName="password" />

        <button type="submit" [disabled]="form.invalid || loading">
          {{ loading ? 'Logging in...' : 'Login' }}
        </button>
      </form>

      <p class="error" *ngIf="errorMessage">{{ errorMessage }}</p>
      <p>New user? <a routerLink="/register">Register here</a></p>
    </div>
  `,
})
export class LoginPageComponent {
  private readonly fb = inject(FormBuilder);
  private readonly authService = inject(AuthService);

  loading = false;
  errorMessage = '';

  readonly form = this.fb.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required]],
  });

  submit(): void {
    if (this.form.invalid || this.loading) {
      return;
    }

    this.errorMessage = '';
    this.loading = true;

    const email = this.form.value.email ?? '';
    const password = this.form.value.password ?? '';

    this.authService.login({ email, password }).subscribe({
      next: () => {
        this.loading = false;
        this.authService.redirectByRole();
      },
      error: (error) => {
        console.error(error);
        this.errorMessage = this.extractErrorMessage(error);
        this.loading = false;
      },
    });
  }

  private extractErrorMessage(error: unknown): string {
    const payload = (error as { error?: unknown })?.error;

    if (typeof payload === 'string' && payload.trim().length > 0) {
      return payload;
    }

    const model = payload as {
      title?: string;
      errors?: Record<string, string[]>;
    };

    const entries = model?.errors ? Object.entries(model.errors) : [];
    if (entries.length > 0) {
      return entries
        .flatMap(([key, messages]) => messages.map((message) => `${key}: ${message}`))
        .join(' | ');
    }

    return model?.title ?? 'Login failed.';
  }
}
