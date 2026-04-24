import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ApiService } from '../../core/api.service';

interface LocationRow {
  id: string;
  city: string;
  state: string;
  status: string;
  createdAt: string;
}

@Component({
  selector: 'app-admin-locations-page',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  template: `
    <div class="page-container">
      <h2>Location Management</h2>

      <form [formGroup]="form" (ngSubmit)="createLocation()" class="form-card">
        <label>City</label>
        <input type="text" formControlName="city" />

        <label>State</label>
        <input type="text" formControlName="state" />

        <label>Status</label>
        <select formControlName="status">
          <option value="Approved">Approved</option>
          <option value="Pending">Pending</option>
          <option value="Rejected">Rejected</option>
        </select>

        <button type="submit" [disabled]="form.invalid || creating">
          {{ creating ? 'Adding Location...' : 'Add Location' }}
        </button>
      </form>

      <p class="error" *ngIf="errorMessage">{{ errorMessage }}</p>

      <h3>All Locations</h3>
      <p *ngIf="loadingData">Loading locations...</p>
      <table *ngIf="!loadingData && locations.length > 0">
        <thead>
          <tr>
            <th>City</th>
            <th>State</th>
            <th>Status</th>
            <th>Created</th>
            <th>Actions</th>
          </tr>
        </thead>
        <tbody>
          <tr *ngFor="let row of locations">
            <td>{{ row.city }}</td>
            <td>{{ row.state }}</td>
            <td>
              <span class="status-pill" [ngClass]="statusClass(row.status)">{{ row.status }}</span>
            </td>
            <td>{{ row.createdAt | date: 'short' }}</td>
            <td>
              <button
                type="button"
                class="btn-approve"
                (click)="updateStatus(row, 'Approved')"
                [disabled]="processingLocationId === row.id || row.status === 'Approved'"
              >
                Approve
              </button>
              <button
                type="button"
                class="btn-reject"
                (click)="updateStatus(row, 'Rejected')"
                [disabled]="processingLocationId === row.id || row.status === 'Rejected'"
              >
                Reject
              </button>
            </td>
          </tr>
        </tbody>
      </table>
      <p *ngIf="!loadingData && locations.length === 0">No locations found.</p>
    </div>
  `,
})
export class AdminLocationsPageComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(ApiService);
  private readonly cdr = inject(ChangeDetectorRef);

  readonly form = this.fb.group({
    city: ['', [Validators.required]],
    state: ['', [Validators.required]],
    status: ['Approved', [Validators.required]],
  });

  locations: LocationRow[] = [];
  creating = false;
  loadingData = false;
  processingLocationId = '';
  errorMessage = '';

  ngOnInit(): void {
    this.loadLocations();
  }

  createLocation(): void {
    if (this.form.invalid || this.creating) {
      return;
    }

    this.creating = true;
    this.errorMessage = '';

    this.api
      .post('locations', {
        city: this.form.value.city,
        state: this.form.value.state,
        status: this.form.value.status,
      })
      .subscribe({
        next: () => {
          this.creating = false;
          this.form.patchValue({ city: '', state: '', status: 'Approved' });
          this.loadLocations();
          this.cdr.detectChanges();
        },
        error: (error) => {
          console.error(error);
          this.creating = false;
          this.errorMessage = this.extractError(error, 'Failed to create location.');
          this.cdr.detectChanges();
        },
      });
  }

  loadLocations(): void {
    this.loadingData = true;
    this.errorMessage = '';
    this.api.get<LocationRow[]>('locations').subscribe({
      next: (response) => {
        this.locations = response;
        this.loadingData = false;
        this.cdr.detectChanges();
      },
      error: (error) => {
        console.error(error);
        this.loadingData = false;
        this.errorMessage = 'Unable to load locations.';
        this.cdr.detectChanges();
      },
    });
  }

  updateStatus(row: LocationRow, status: 'Approved' | 'Rejected'): void {
    this.processingLocationId = row.id;
    this.errorMessage = '';

    this.api
      .patch(`locations/${row.id}`, {
        city: row.city,
        state: row.state,
        status,
      })
      .subscribe({
        next: () => {
          this.processingLocationId = '';
          this.loadLocations();
          this.cdr.detectChanges();
        },
        error: (error) => {
          console.error(error);
          this.processingLocationId = '';
          this.errorMessage = this.extractError(error, 'Failed to update location status.');
          this.cdr.detectChanges();
        },
      });
  }

  statusClass(status: string): string {
    if (status === 'Approved') {
      return 'status-approved';
    }

    if (status === 'Rejected') {
      return 'status-rejected';
    }

    return 'status-pending';
  }

  private extractError(error: unknown, fallback: string): string {
    const payload = (error as { error?: unknown })?.error;
    if (typeof payload === 'string' && payload.trim()) {
      return payload;
    }

    const model = payload as { title?: string; errors?: Record<string, string[]> };
    if (model?.errors) {
      return Object.entries(model.errors)
        .flatMap(([key, msgs]) => msgs.map((msg) => `${key}: ${msg}`))
        .join(' | ');
    }

    return model?.title ?? fallback;
  }
}
