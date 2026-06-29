// @feature Authentication | Registration Page | Interface allowing creation of new staff or guest user accounts with role selection.
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { AbstractControl, FormBuilder, ReactiveFormsModule, ValidationErrors, ValidatorFn, Validators } from '@angular/forms';
import { AuthService } from '../../../core/services/auth.service';
import { RegisterModel } from '../../../core/models/auth.model';
import { Router, RouterLink } from '@angular/router';

const passwordMatchValidator: ValidatorFn = (control: AbstractControl): ValidationErrors | null => {
  const password = control.get('password')?.value;
  const confirmPassword = control.get('confirmPassword')?.value;
  return password === confirmPassword ? null : { passwordMismatch: true };
};

@Component({
  selector: 'app-register',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './register.html',
  styleUrl: './register.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Register {
  private fb = inject(FormBuilder);
  private authService = inject(AuthService);
  private router = inject(Router);

  registerForm = this.fb.group({
    email: ['', [Validators.required, Validators.email]],
    name: ['', Validators.required],
    password: ['', [
      Validators.required,
      Validators.minLength(8),
      Validators.pattern(/^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^A-Za-z0-9]).+$/)
    ]],
    confirmPassword: ['', Validators.required],
    roleId: [null as number | null, Validators.required],
    phoneNumber: ['', [
      Validators.required,
      Validators.pattern(/^(\+91[\s-]?)?[6-9]\d{9}$/)
    ]],
    address: [''],
  }, { validators: passwordMatchValidator });

  readonly controls = this.registerForm.controls;

  errorMessage = signal<string | null>(null);
  isLoading = signal<boolean>(false);

  onSubmit() {
    if (this.registerForm.invalid) {
      this.registerForm.markAllAsTouched();
      return;
    }

    this.isLoading.set(true);
    this.errorMessage.set(null);

    const registerData: RegisterModel = {
      email: this.registerForm.value.email ?? '',
      name: this.registerForm.value.name ?? '',
      password: this.registerForm.value.password ?? '',
      roleId: this.registerForm.value.roleId ?? 0,
      phoneNumber: this.registerForm.value.phoneNumber ?? '',
      address: this.registerForm.value.address?.trim() || null,
    };

    this.authService.register(registerData).subscribe({
      next: (user) => {
        console.log('Registration successful:', user);
        alert('Registration successful!');
        this.registerForm.reset({
          email: '',
          name: '',
          password: '',
          confirmPassword: '',
          roleId: null,
          phoneNumber: '',
          address: '',
        });
        this.isLoading.set(false);
        this.router.navigate(['/login']);
      },
      error: (err) => {
        this.isLoading.set(false);
        this.errorMessage.set(err.message || 'Registration failed. Please check your inputs and try again.');
      }
    });
  }
}
