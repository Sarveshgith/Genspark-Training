import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ApiService } from '../../core/api.service';

interface MyBookingRow {
  id: string;
  bookingRef: string;
  busId: string;
  route: string;
  status: string;
  paymentStatus: string;
  baseAmount: number;
  totalAmount: number;
  createdAt: string;
}

@Component({
  selector: 'app-my-bookings-page',
  standalone: true,
  imports: [CommonModule, RouterLink],
  template: `
    <div class="page-container">
      <h2>My Bookings</h2>
      <button type="button" (click)="loadBookings()">Refresh</button>

      <p class="error" *ngIf="errorMessage">{{ errorMessage }}</p>

      <table *ngIf="bookings.length > 0">
        <thead>
          <tr>
            <th>Booking Ref</th>
            <th>Bus</th>
            <th>Route</th>
            <th>Status</th>
            <th>Payment</th>
            <th>Total</th>
            <th>Created</th>
            <th>Action</th>
          </tr>
        </thead>
        <tbody>
          <tr *ngFor="let booking of bookings">
            <td>{{ booking.bookingRef }}</td>
            <td>{{ booking.busId }}</td>
            <td>{{ booking.route }}</td>
            <td>{{ booking.status }}</td>
            <td>{{ booking.paymentStatus }}</td>
            <td>{{ booking.totalAmount }}</td>
            <td>{{ booking.createdAt | date: 'short' }}</td>
            <td><a [routerLink]="['/booking', booking.id]">View</a></td>
          </tr>
        </tbody>
      </table>

      <p *ngIf="bookings.length === 0">No bookings found.</p>
    </div>
  `,
})
export class MyBookingsPageComponent implements OnInit {
  private readonly api = inject(ApiService);
  private readonly cdr = inject(ChangeDetectorRef);

  bookings: MyBookingRow[] = [];
  errorMessage = '';

  ngOnInit(): void {
    this.loadBookings();
  }

  loadBookings(): void {
    this.errorMessage = '';
    this.api.get<MyBookingRow[]>('bookings/my').subscribe({
      next: (response) => {
        this.bookings = response;
        this.cdr.detectChanges();
      },
      error: (error) => {
        console.error(error);
        this.errorMessage = 'Unable to load bookings.';
        this.cdr.detectChanges();
      },
    });
  }
}
