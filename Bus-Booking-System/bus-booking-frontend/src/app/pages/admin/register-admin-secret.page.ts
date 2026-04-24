import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ApiService } from '../../core/api.service';

@Component({
  selector: 'app-register-admin-secret-page',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  template: `
    <div class="page-container">
      <h2>Admin Registration (Restricted)</h2>
      <p class="card">
        This page is intentionally not linked in navigation. Access is protected by Admin role and a
        long route slug.
      </p>

      <form [formGroup]="form" (ngSubmit)="submit()" class="form-card">
        <label>Name</label>
        <input type="text" formControlName="name" />

        <label>Email</label>
        <input type="email" formControlName="email" />

        <label>Phone</label>
        <input type="text" formControlName="phone" />

        <label>Password</label>
        <input type="password" formControlName="password" />

        <button type="submit" [disabled]="form.invalid || loading">
          {{ loading ? 'Creating...' : 'Create Admin' }}
        </button>
      </form>

      <p class="success" *ngIf="successMessage">{{ successMessage }}</p>
      <p class="error" *ngIf="errorMessage">{{ errorMessage }}</p>
    </div>
  `,
})
export class RegisterAdminSecretPageComponent {
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(ApiService);

  loading = false;
  errorMessage = '';
  successMessage = '';

  readonly form = this.fb.group({
    name: ['', [Validators.required]],
    email: ['', [Validators.required, Validators.email]],
    phone: ['', [Validators.required]],
    password: ['', [Validators.required, Validators.minLength(8)]],
  });

  submit(): void {
    if (this.form.invalid || this.loading) {
      return;
    }

    this.loading = true;
    this.errorMessage = '';
    this.successMessage = '';

    this.api
      .post('auth/admin/register', {
        name: this.form.value.name,
        email: this.form.value.email,
        phone: this.form.value.phone,
        password: this.form.value.password,
      })
      .subscribe({
        next: () => {
          this.loading = false;
          this.successMessage = 'Admin user created successfully.';
          this.form.reset();
        },
        error: (error) => {
          console.error(error);
          this.loading = false;
          this.errorMessage = this.extractErrorMessage(error);
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

    return model?.title ?? 'Failed to create admin user.';
  }
}
