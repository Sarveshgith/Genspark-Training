import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { AuthService } from '../../../core/services/auth.service';
import { RegisterModel } from '../../../core/models/auth.model';


@Component({
  selector: 'app-register',
  imports: [ReactiveFormsModule],
  templateUrl: './register.html',
  styleUrl: './register.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Register {
  private fb = inject(FormBuilder);
  private authService = inject(AuthService);

  registerForm = this.fb.group({
    email: ['', [Validators.required, Validators.email]],
    name: ['', Validators.required],
    password: ['', [Validators.required, Validators.minLength(6)]],
    roleId: [0, Validators.required],
    phoneNumber: ['', Validators.required],
    address: [''],
  });

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
          roleId: 0,
          phoneNumber: '',
          address: '',
        });
        this.isLoading.set(false);
      },
      error: (err) => {
        this.isLoading.set(false);
        this.errorMessage.set(err.message || 'Registration failed. Please check your inputs and try again.');
      }
    });
  }
}
