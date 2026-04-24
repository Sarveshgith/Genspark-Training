import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { ApiService } from '../../core/api.service';

interface PaymentSummary {
  id: string;
  bookingRef: string;
  busId: string;
  route: string;
  baseAmount: number;
  convenienceFee: number;
  serviceFee: number;
  taxAmount: number;
  total: number;
  paymentStatus: string;
  status: string;
  seats: string[];
}

@Component({
  selector: 'app-payment-page',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  template: `
    <div class="page-container">
      <h2>Payment</h2>

      <p class="error" *ngIf="errorMessage">{{ errorMessage }}</p>
      <p class="success" *ngIf="successMessage">{{ successMessage }}</p>

      <p *ngIf="loadingData">Loading payment summary...</p>

      <div class="card" *ngIf="summary && !loadingData">
        <p><strong>Ticket ID:</strong> {{ summary.id }}</p>
        <p><strong>Booking Ref:</strong> {{ summary.bookingRef }}</p>
        <p><strong>Bus ID:</strong> {{ summary.busId }}</p>
        <p><strong>Route:</strong> {{ summary.route }}</p>
        <p><strong>Seats:</strong> {{ summary.seats.join(', ') }}</p>
      </div>

      <div class="card" *ngIf="summary && !loadingData">
        <h3>Fare Breakdown</h3>
        <p>Base Fare: {{ summary.baseAmount }}</p>
        <p>Convenience Fee: {{ summary.convenienceFee }}</p>
        <p>Service Fee: {{ summary.serviceFee }}</p>
        <p>Tax: {{ summary.taxAmount }}</p>
        <p>
          <strong>Total Payable: {{ summary.total }}</strong>
        </p>
      </div>

      <form [formGroup]="form" (ngSubmit)="payNow()" class="form-card" *ngIf="summary">
        <label>Payment Reference</label>
        <input type="text" formControlName="paymentRef" />

        <button
          type="submit"
          [disabled]="form.invalid || paying || summary.paymentStatus === 'Success'"
        >
          {{ paying ? 'Processing Payment...' : 'Pay Now' }}
        </button>
      </form>

      <a *ngIf="summary" [routerLink]="['/booking', summary.id]">Back to Booking</a>
    </div>
  `,
})
export class PaymentPageComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(ApiService);
  private readonly cdr = inject(ChangeDetectorRef);

  readonly form = this.fb.group({
    paymentRef: [`PAY-${Date.now()}`, [Validators.required]],
  });

  ticketId = '';
  summary: PaymentSummary | null = null;
  loadingData = false;
  paying = false;
  errorMessage = '';
  successMessage = '';

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('ticketId');
    if (!id) {
      this.errorMessage = 'Ticket id is missing.';
      this.cdr.detectChanges();
      return;
    }

    this.ticketId = id;
    this.loadSummary();
  }

  loadSummary(): void {
    this.loadingData = true;
    this.errorMessage = '';

    this.api.get<PaymentSummary>(`bookings/${this.ticketId}/payment-summary`).subscribe({
      next: (response) => {
        this.summary = response;
        this.loadingData = false;
        this.cdr.detectChanges();
      },
      error: (error) => {
        console.error(error);
        this.errorMessage = 'Unable to load payment summary.';
        this.loadingData = false;
        this.cdr.detectChanges();
      },
    });
  }

  payNow(): void {
    if (this.form.invalid || this.paying || !this.summary) {
      return;
    }

    this.paying = true;
    this.errorMessage = '';
    this.successMessage = '';

    this.api
      .post<{ id: string }>(`bookings/${this.ticketId}/pay`, {
        paymentRef: this.form.value.paymentRef,
      })
      .subscribe({
        next: (response) => {
          this.paying = false;
          this.successMessage = 'Payment successful. Booking confirmed and email sent.';
          this.cdr.detectChanges();
          this.router.navigate(['/booking', response.id]);
        },
        error: (error) => {
          console.error(error);
          this.paying = false;
          this.errorMessage = (error?.error as string) || 'Payment failed.';
          this.cdr.detectChanges();
        },
      });
  }
}
