import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ApiService } from '../../core/api.service';
import { TripSummary } from '../../core/models';

interface BusOption {
  id: string;
  vehicleNumber: string;
  status: string;
}

interface LocationOption {
  id: string;
  city: string;
  state: string;
  status: string;
}

@Component({
  selector: 'app-operator-trips-page',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  template: `
    <div class="page-container">
      <h2>Trip Management</h2>

      <form [formGroup]="form" (ngSubmit)="createTrip()" class="form-card">
        <label>Bus</label>
        <select formControlName="busId">
          <option value="">Select bus</option>
          <option *ngFor="let bus of buses" [value]="bus.id">
            {{ bus.vehicleNumber }}
          </option>
        </select>

        <label>From Location</label>
        <select formControlName="fromId">
          <option value="">Select source</option>
          <option *ngFor="let location of locations" [value]="location.id">
            {{ location.city }}, {{ location.state }}
          </option>
        </select>

        <label>To Location</label>
        <select formControlName="toId">
          <option value="">Select destination</option>
          <option *ngFor="let location of locations" [value]="location.id">
            {{ location.city }}, {{ location.state }}
          </option>
        </select>

        <label>Departure Time</label>
        <input type="datetime-local" formControlName="departureTime" [attr.min]="minDateTimeLocal" />

        <label>Arrival Time</label>
        <input type="datetime-local" formControlName="arrivalTime" [attr.min]="form.value.departureTime || minDateTimeLocal" />

        <label>Price Per Seat</label>
        <input type="number" formControlName="pricePerSeat" min="1" />

        <label>Status</label>
        <select formControlName="status">
          <option value="Scheduled">Scheduled</option>
          <option value="Active">Active</option>
        </select>

        <button type="submit" [disabled]="form.invalid || creating">
          {{ creating ? 'Creating Trip...' : 'Create Trip' }}
        </button>
      </form>

      <p class="error" *ngIf="errorMessage">{{ errorMessage }}</p>

      <h3>Your Trips</h3>
      <p *ngIf="loadingData">Loading trips...</p>
      <table *ngIf="!loadingData && trips.length > 0">
        <thead>
          <tr>
            <th>Trip ID</th>
            <th>Route ID</th>
            <th>Status</th>
            <th>Departure</th>
            <th>Arrival</th>
            <th>Price</th>
            <th>Actions</th>
          </tr>
        </thead>
        <tbody>
          <tr *ngFor="let trip of trips">
            <td>{{ trip.id }}</td>
            <td>{{ trip.routeId }}</td>
            <td>{{ trip.status }}</td>
            <td>{{ trip.departureTime | date: 'short' }}</td>
            <td>{{ trip.arrivalTime | date: 'short' }}</td>
            <td>{{ trip.pricePerSeat }}</td>
            <td>
              <button
                type="button"
                (click)="cancelTrip(trip.id)"
                [disabled]="trip.status === 'Cancelled' || cancellingTripId === trip.id"
              >
                {{ cancellingTripId === trip.id ? 'Cancelling...' : 'Cancel Trip' }}
              </button>
            </td>
          </tr>
        </tbody>
      </table>
    </div>
  `,
})
export class OperatorTripsPageComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(ApiService);
  private readonly cdr = inject(ChangeDetectorRef);

  readonly form = this.fb.group({
    busId: ['', [Validators.required]],
    fromId: ['', [Validators.required]],
    toId: ['', [Validators.required]],
    departureTime: ['', [Validators.required]],
    arrivalTime: ['', [Validators.required]],
    pricePerSeat: [500, [Validators.required, Validators.min(1)]],
    status: ['Scheduled', [Validators.required]],
  });

  buses: BusOption[] = [];
  locations: LocationOption[] = [];
  trips: TripSummary[] = [];
  creating = false;
  cancellingTripId = '';
  loadingData = false;
  errorMessage = '';
  readonly minDateTimeLocal = this.toLocalDateTimeInput(new Date());

  ngOnInit(): void {
    this.loadBuses();
    this.loadLocations();
    this.loadTrips();
  }

  createTrip(): void {
    if (this.form.invalid || this.creating) {
      return;
    }

    const departureRaw = this.form.value.departureTime;
    const arrivalRaw = this.form.value.arrivalTime;
    if (!departureRaw || !arrivalRaw) {
      this.errorMessage = 'Departure and arrival times are required.';
      return;
    }

    const fromId = this.form.value.fromId ?? '';
    const toId = this.form.value.toId ?? '';

    if (!fromId || !toId) {
      this.errorMessage = 'From and To locations are required.';
      return;
    }

    if (fromId === toId) {
      this.errorMessage = 'From and To locations cannot be the same.';
      return;
    }

    const departure = new Date(departureRaw);
    const arrival = new Date(arrivalRaw);
    const now = new Date();

    if (Number.isNaN(departure.getTime()) || Number.isNaN(arrival.getTime())) {
      this.errorMessage = 'Departure and arrival times are invalid.';
      return;
    }

    if (departure <= now) {
      this.errorMessage = 'Departure time cannot be in the past.';
      return;
    }

    if (arrival <= departure) {
      this.errorMessage = 'Arrival time must be later than departure time.';
      return;
    }

    const durationMinutes = (arrival.getTime() - departure.getTime()) / 60000;
    if (durationMinutes < 15) {
      this.errorMessage = 'Trip duration must be at least 15 minutes.';
      return;
    }

    this.creating = true;
    this.api
      .post('trips', {
        busId: this.form.value.busId,
        fromId,
        toId,
        status: this.form.value.status,
        departureTime: departure.toISOString(),
        arrivalTime: arrival.toISOString(),
        pricePerSeat: this.form.value.pricePerSeat,
      })
      .subscribe({
        next: () => {
          this.creating = false;
          this.errorMessage = '';
          this.loadTrips();
          this.cdr.detectChanges();
        },
        error: (error) => {
          console.error(error);
          this.creating = false;
          this.errorMessage = this.extractError(error, 'Failed to create trip.');
          this.cdr.detectChanges();
        },
      });
  }

  cancelTrip(tripId: string): void {
    if (this.cancellingTripId) {
      return;
    }

    this.cancellingTripId = tripId;
    this.errorMessage = '';
    this.api.patch(`trips/${tripId}/cancel`, {}).subscribe({
      next: () => {
        this.cancellingTripId = '';
        this.loadTrips();
        this.cdr.detectChanges();
      },
      error: (error) => {
        console.error(error);
        this.cancellingTripId = '';
        this.errorMessage = this.extractError(error, 'Failed to cancel trip.');
        this.cdr.detectChanges();
      },
    });
  }

  private loadBuses(): void {
    this.api.get<BusOption[]>('buses').subscribe({
      next: (response) => {
        this.buses = response.filter((bus) => bus.status === 'Approved');
        this.cdr.detectChanges();
      },
      error: (error) => {
        console.error(error);
        this.cdr.detectChanges();
      },
    });
  }

  private loadLocations(): void {
    this.api.get<LocationOption[]>('locations').subscribe({
      next: (response) => {
        this.locations = response.filter((location) => location.status === 'Approved');
        this.cdr.detectChanges();
      },
      error: (error) => {
        console.error(error);
        this.cdr.detectChanges();
      },
    });
  }

  private loadTrips(): void {
    this.loadingData = true;
    this.api.get<TripSummary[]>('trips').subscribe({
      next: (response) => {
        this.trips = response;
        this.loadingData = false;
        this.cdr.detectChanges();
      },
      error: (error) => {
        console.error(error);
        this.loadingData = false;
        this.errorMessage = 'Unable to load trips.';
        this.cdr.detectChanges();
      },
    });
  }

  private extractError(error: unknown, fallback: string): string {
    if (typeof error === 'object' && error !== null && 'error' in error) {
      const payload = (error as { error?: unknown }).error;
      if (typeof payload === 'string') {
        return payload;
      }

      if (typeof payload === 'object' && payload !== null && 'message' in payload) {
        const message = (payload as { message?: unknown }).message;
        if (typeof message === 'string' && message.trim().length > 0) {
          return message;
        }
      }
    }

    return fallback;
  }

  private toLocalDateTimeInput(value: Date): string {
    const local = new Date(value.getTime() - value.getTimezoneOffset() * 60000);
    return local.toISOString().slice(0, 16);
  }
}
