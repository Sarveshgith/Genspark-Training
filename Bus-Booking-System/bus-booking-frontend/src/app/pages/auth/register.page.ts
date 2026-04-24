import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../core/auth.service';

@Component({
  selector: 'app-register-page',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  template: `
    <div class="page-container">
      <h2>Register</h2>

      <form [formGroup]="form" (ngSubmit)="submit()" class="form-card">
        <label>Name</label>
        <input type="text" formControlName="name" />

        <label>Email</label>
        <input type="email" formControlName="email" />

        <label>Phone</label>
        <input type="text" formControlName="phone" />

        <label>Password</label>
        <input type="password" formControlName="password" />

        <label>Role</label>
        <select formControlName="role">
          <option value="User">User</option>
          <option value="Operator">Operator</option>
        </select>

        <div *ngIf="isOperator()">
          <label>License Number</label>
          <input type="text" formControlName="licenseNumber" />
        </div>

        <button type="submit" [disabled]="form.invalid || loading">
          {{ loading ? 'Submitting...' : 'Register' }}
        </button>
      </form>

      <p class="error" *ngIf="errorMessage">{{ errorMessage }}</p>
      <p>Already have an account? <a routerLink="/login">Login</a></p>
    </div>
  `,
})
export class RegisterPageComponent {
  private readonly fb = inject(FormBuilder);
  private readonly authService = inject(AuthService);

  loading = false;
  errorMessage = '';

  readonly form = this.fb.group({
    name: ['', [Validators.required]],
    email: ['', [Validators.required, Validators.email]],
    phone: ['', [Validators.required]],
    password: ['', [Validators.required, Validators.minLength(8)]],
    role: ['User', [Validators.required]],
    licenseNumber: [''],
  });

  isOperator(): boolean {
    return this.form.value.role === 'Operator';
  }

  submit(): void {
    if (this.form.invalid || this.loading) {
      return;
    }

    if (this.isOperator() && !(this.form.value.licenseNumber ?? '').trim()) {
      this.errorMessage = 'License number is required for operator registration.';
      return;
    }

    this.errorMessage = '';
    this.loading = true;

    this.authService
      .register({
        name: this.form.value.name ?? '',
        email: this.form.value.email ?? '',
        phone: this.form.value.phone ?? '',
        password: this.form.value.password ?? '',
        role: this.form.value.role ?? 'User',
        licenseNumber: this.form.value.licenseNumber ?? '',
      })
      .subscribe({
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

    return model?.title ?? 'Registration failed.';
  }
}
