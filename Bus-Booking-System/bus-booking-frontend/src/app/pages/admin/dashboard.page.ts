import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { ApiService } from '../../core/api.service';

interface AdminMetrics {
  users: number;
  operators: number;
  buses: number;
  trips: number;
  tickets: number;
  seatBookings: number;
  confirmedTickets: number;
  cancelledTickets: number;
}

@Component({
  selector: 'app-admin-dashboard-page',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="page-container">
      <h2>Admin Dashboard</h2>
      <button type="button" (click)="loadMetrics()">Refresh Metrics</button>

      <div class="grid-2" *ngIf="metrics">
        <div class="card"><strong>Users:</strong> {{ metrics.users }}</div>
        <div class="card"><strong>Operators:</strong> {{ metrics.operators }}</div>
        <div class="card"><strong>Buses:</strong> {{ metrics.buses }}</div>
        <div class="card"><strong>Trips:</strong> {{ metrics.trips }}</div>
        <div class="card"><strong>Tickets:</strong> {{ metrics.tickets }}</div>
        <div class="card"><strong>Seat Bookings:</strong> {{ metrics.seatBookings }}</div>
        <div class="card"><strong>Confirmed Tickets:</strong> {{ metrics.confirmedTickets }}</div>
        <div class="card"><strong>Cancelled Tickets:</strong> {{ metrics.cancelledTickets }}</div>
      </div>

      <p class="error" *ngIf="errorMessage">{{ errorMessage }}</p>
    </div>
  `,
})
export class AdminDashboardPageComponent implements OnInit {
  private readonly api = inject(ApiService);
  private readonly cdr = inject(ChangeDetectorRef);

  metrics: AdminMetrics | null = null;
  errorMessage = '';

  ngOnInit(): void {
    this.loadMetrics();
  }

  loadMetrics(): void {
    this.errorMessage = '';
    this.api.get<AdminMetrics>('admin/dashboard/metrics').subscribe({
      next: (response) => {
        this.metrics = response;
        this.cdr.detectChanges();
      },
      error: (error) => {
        console.error(error);
        this.errorMessage = 'Unable to load metrics.';
        this.cdr.detectChanges();
      },
    });
  }
}
