import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { FormArray, FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { ApiService } from '../../core/api.service';

interface BookingDetail {
  id: string;
  bookingRef: string;
  busId: string;
  route: string;
  status: string;
  paymentStatus: string;
  baseAmount: number;
  totalAmount: number;
  createdAt: string;
  seats: Array<{
    id: string;
    seatNumber: string;
    passengerName: string;
    passengerAge: number;
    passengerGender: string;
    status: string;
  }>;
}

@Component({
  selector: 'app-booking-page',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  template: `
    <div class="page-container">
      <h2>Booking</h2>

      <p class="error" *ngIf="errorMessage">{{ errorMessage }}</p>
      <p class="success" *ngIf="successMessage">{{ successMessage }}</p>

      <div *ngIf="isNewBookingMode && selectedSeats.length > 0">
        <h3>Passenger Details</h3>
        <form [formGroup]="form" (ngSubmit)="reserveForPayment()" class="form-card">
          <div formArrayName="passengers">
            <div
              class="card"
              *ngFor="let control of passengers.controls; let i = index"
              [formGroupName]="i"
            >
              <h4>Seat {{ selectedSeats[i] }}</h4>

              <label>Name</label>
              <input type="text" formControlName="name" />

              <label>Age</label>
              <input type="number" formControlName="age" min="1" />

              <label>Gender</label>
              <select formControlName="gender">
                <option value="Male">Male</option>
                <option value="Female">Female</option>
                <option value="Other">Other</option>
              </select>
            </div>
          </div>

          <button type="submit" [disabled]="form.invalid || loading">
            {{ loading ? 'Reserving...' : 'Reserve Seats and Continue to Payment' }}
          </button>
        </form>
      </div>

      <div *ngIf="bookingDetail">
        <h3>Booking Summary</h3>
        <div class="card">
          <p><strong>Ticket ID:</strong> {{ bookingDetail.id }}</p>
          <p><strong>Booking Ref:</strong> {{ bookingDetail.bookingRef }}</p>
          <p><strong>Bus ID:</strong> {{ bookingDetail.busId }}</p>
          <p><strong>Route:</strong> {{ bookingDetail.route }}</p>
          <p><strong>Status:</strong> {{ bookingDetail.status }}</p>
          <p><strong>Payment:</strong> {{ bookingDetail.paymentStatus }}</p>
          <p><strong>Base Fare:</strong> {{ bookingDetail.baseAmount }}</p>
          <p><strong>Total:</strong> {{ bookingDetail.totalAmount }}</p>
        </div>

        <button
          type="button"
          (click)="goToPayment()"
          *ngIf="bookingDetail.paymentStatus !== 'Success'"
        >
          Continue to Payment
        </button>

        <table *ngIf="bookingDetail.seats.length > 0">
          <thead>
            <tr>
              <th>Seat</th>
              <th>Name</th>
              <th>Age</th>
              <th>Gender</th>
              <th>Status</th>
            </tr>
          </thead>
          <tbody>
            <tr *ngFor="let seat of bookingDetail.seats">
              <td>{{ seat.seatNumber }}</td>
              <td>{{ seat.passengerName }}</td>
              <td>{{ seat.passengerAge }}</td>
              <td>{{ seat.passengerGender }}</td>
              <td>{{ seat.status }}</td>
            </tr>
          </tbody>
        </table>

        <button
          type="button"
          (click)="cancelBooking()"
          *ngIf="bookingDetail.status !== 'Cancelled'"
        >
          Cancel Booking
        </button>
      </div>
    </div>
  `,
})
export class BookingPageComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(ApiService);
  private readonly cdr = inject(ChangeDetectorRef);

  isNewBookingMode = false;
  ticketId = '';
  tripId = '';
  selectedSeats: string[] = [];
  femaleOnlySeats = new Set<string>();

  loading = false;
  errorMessage = '';
  successMessage = '';
  bookingDetail: BookingDetail | null = null;

  readonly form = this.fb.group({
    passengers: this.fb.array([]),
  });

  get passengers(): FormArray {
    return this.form.get('passengers') as FormArray;
  }

  ngOnInit(): void {
    const routeTicketId = this.route.snapshot.paramMap.get('ticketId');
    if (!routeTicketId) {
      this.errorMessage = 'Ticket id is missing.';
      this.cdr.detectChanges();
      return;
    }

    this.ticketId = routeTicketId;
    if (routeTicketId === 'new') {
      this.setupNewBooking();
      return;
    }

    this.loadBooking(routeTicketId);
  }

  reserveForPayment(): void {
    if (this.form.invalid || this.loading) {
      return;
    }

    this.errorMessage = '';
    this.successMessage = '';

    const passengers = this.passengers.controls.map((control) => {
      const group = control as FormGroup;
      return {
        name: String(group.get('name')?.value ?? ''),
        age: Number(group.get('age')?.value ?? 0),
        gender: String(group.get('gender')?.value ?? 'Male'),
      };
    });

    for (let index = 0; index < this.selectedSeats.length; index += 1) {
      const seat = this.selectedSeats[index];
      const passenger = passengers[index];
      if (this.femaleOnlySeats.has(seat.toUpperCase()) && passenger.gender !== 'Female') {
        this.errorMessage = `Seat ${seat} is female-only. Please assign a female passenger.`;
        return;
      }
    }

    this.loading = true;

    const reservePayload = {
      tripId: this.tripId,
      seats: this.selectedSeats.map((seat, index) => ({
        seatNumber: seat,
        passengerName: passengers[index].name,
        passengerAge: passengers[index].age,
        passengerGender: passengers[index].gender,
      })),
    };

    this.api.post<{ id: string }>('bookings/reserve', reservePayload).subscribe({
      next: (reserveResponse) => {
        const createdTicketId = reserveResponse.id;
        this.loading = false;
        this.successMessage = 'Seats reserved. Complete payment to confirm booking.';
        this.cdr.detectChanges();
        this.router.navigate(['/booking', createdTicketId, 'payment']);
      },
      error: (error) => {
        console.error(error);
        this.loading = false;
        this.errorMessage = error?.error ?? 'Failed to reserve seats.';
        this.cdr.detectChanges();
      },
    });
  }

  cancelBooking(): void {
    if (!this.bookingDetail) {
      return;
    }

    this.api.post(`bookings/${this.bookingDetail.id}/cancel`, {}).subscribe({
      next: () => {
        this.successMessage = 'Booking cancelled.';
        this.cdr.detectChanges();
        this.loadBooking(this.bookingDetail!.id);
      },
      error: (error) => {
        console.error(error);
        this.errorMessage = 'Failed to cancel booking.';
        this.cdr.detectChanges();
      },
    });
  }

  goToPayment(): void {
    if (!this.bookingDetail) {
      return;
    }

    this.router.navigate(['/booking', this.bookingDetail.id, 'payment']);
  }

  private setupNewBooking(): void {
    this.isNewBookingMode = true;

    const state = history.state as {
      tripId?: string;
      selectedSeats?: string[];
      femaleOnlySeats?: string[];
    };

    if (!state.tripId || !Array.isArray(state.selectedSeats) || state.selectedSeats.length === 0) {
      this.errorMessage = 'No seat selection found. Please select seats first.';
      this.cdr.detectChanges();
      return;
    }

    this.tripId = state.tripId;
    this.selectedSeats = state.selectedSeats;
    this.femaleOnlySeats = new Set((state.femaleOnlySeats ?? []).map((seat) => seat.toUpperCase()));

    for (let index = 0; index < this.selectedSeats.length; index += 1) {
      this.passengers.push(
        this.fb.group({
          name: ['', [Validators.required]],
          age: [18, [Validators.required, Validators.min(1)]],
          gender: ['Male', [Validators.required]],
        }),
      );
    }
  }

  private loadBooking(ticketId: string): void {
    this.api.get<BookingDetail>(`bookings/${ticketId}`).subscribe({
      next: (response) => {
        this.bookingDetail = response;
        this.cdr.detectChanges();
      },
      error: (error) => {
        console.error(error);
        this.errorMessage = 'Unable to load booking details.';
        this.cdr.detectChanges();
      },
    });
  }
}
