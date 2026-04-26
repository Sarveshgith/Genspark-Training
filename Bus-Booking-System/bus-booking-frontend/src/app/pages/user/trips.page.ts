import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { ApiService } from '../../core/api.service';
import { TripSummary } from '../../core/models';

interface LocationOption {
  id: string;
  city: string;
  state: string;
  status: string;
}

@Component({
  selector: 'app-trips-page',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  template: `
    <div class="page-container">
      <h2>Trips</h2>

      <form [formGroup]="filterForm" (ngSubmit)="searchTrips()" class="form-card">
        <label>From</label>
        <select formControlName="fromLocationId">
          <option value="">Any</option>
          <option *ngFor="let location of locations" [value]="location.id">
            {{ location.city }}, {{ location.state }}
          </option>
        </select>

        <label>To</label>
        <select formControlName="toLocationId">
          <option value="">Any</option>
          <option *ngFor="let location of locations" [value]="location.id">
            {{ location.city }}, {{ location.state }}
          </option>
        </select>

        <label>Date</label>
        <input type="date" formControlName="date" />

        <button type="submit" [disabled]="searching">
          {{ searching ? 'Searching...' : 'Search' }}
        </button>
      </form>

      <p class="error" *ngIf="errorMessage">{{ errorMessage }}</p>

      <p *ngIf="searching">Loading trips...</p>
      <table *ngIf="!searching && trips.length > 0">
        <thead>
          <tr>
            <th>Bus</th>
            <th>Route</th>
            <th>Departure</th>
            <th>Arrival</th>
            <th>Price</th>
            <th>Status</th>
            <th>Action</th>
          </tr>
        </thead>
        <tbody>
          <tr *ngFor="let trip of trips">
            <td>{{ trip.busId }}</td>
            <td>{{ trip.routeLabel || trip.routeId }}</td>
            <td>{{ trip.departureTime | date: 'short' }}</td>
            <td>{{ trip.arrivalTime | date: 'short' }}</td>
            <td>{{ trip.pricePerSeat }}</td>
            <td>{{ trip.status }}</td>
            <td>
              <a
                *ngIf="isBookableStatus(trip.status)"
                [routerLink]="['/trips', trip.id, 'seats']"
              >
                Select Seats
              </a>
              <span *ngIf="!isBookableStatus(trip.status)">Not available</span>
            </td>
          </tr>
        </tbody>
      </table>

      <p *ngIf="trips.length === 0">No trips found.</p>
    </div>
  `,
})
export class TripsPageComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(ApiService);
  private readonly cdr = inject(ChangeDetectorRef);

  readonly filterForm = this.fb.group({
    fromLocationId: [''],
    toLocationId: [''],
    date: [''],
  });

  locations: LocationOption[] = [];
  trips: TripSummary[] = [];
  searching = false;
  errorMessage = '';

  ngOnInit(): void {
    this.loadLocations();
    this.searchTrips();
  }

  loadLocations(): void {
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

  searchTrips(): void {
    this.errorMessage = '';

    const fromLocationId = this.filterForm.value.fromLocationId ?? '';
    const toLocationId = this.filterForm.value.toLocationId ?? '';
    if (fromLocationId && toLocationId && fromLocationId === toLocationId) {
      this.errorMessage = 'From and To locations cannot be same.';
      return;
    }

    this.searching = true;
    this.api
      .get<TripSummary[]>('trips', {
        fromId: fromLocationId,
        toId: toLocationId,
        date: this.filterForm.value.date,
      })
      .subscribe({
        next: (response) => {
          this.trips = response.filter((trip) => this.isBookableStatus(trip.status));
          this.searching = false;
          this.cdr.detectChanges();
        },
        error: (error) => {
          console.error(error);
          this.searching = false;
          this.errorMessage = 'Unable to load trips.';
          this.cdr.detectChanges();
        },
      });
  }

  isBookableStatus(status: string): boolean {
    return status === 'Scheduled' || status === 'Active';
  }
}
