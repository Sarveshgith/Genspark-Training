import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ApiService } from '../../core/api.service';
import { AuthService } from '../../core/auth.service';

interface LocationOption {
  id: string;
  city: string;
  state: string;
  status: string;
}

interface OperatorLocationRow {
  id: string;
  operatorId: string;
  locationId: string;
  address: string;
  isActive: boolean;
  location: {
    city: string;
    state: string;
    status: string;
  };
}

@Component({
  selector: 'app-operator-locations-page',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  template: `
    <div class="page-container">
      <h2>Location Management</h2>
      <p class="card">
        Request a new location first. After admin approval, you can add your office address.
      </p>

      <h3>Request New Location</h3>
      <form [formGroup]="requestLocationForm" (ngSubmit)="requestNewLocation()" class="form-card">
        <label>City</label>
        <input type="text" formControlName="city" />

        <label>State</label>
        <input type="text" formControlName="state" />

        <button type="submit" [disabled]="requestLocationForm.invalid || requestingNewLocation">
          {{ requestingNewLocation ? 'Submitting Request...' : 'Request Approval' }}
        </button>
      </form>

      <h3>Add Office Address (Approved Locations Only)</h3>
      <form [formGroup]="officeAddressForm" (ngSubmit)="addOfficeAddress()" class="form-card">
        <label>Location</label>
        <select formControlName="locationId">
          <option value="">Select approved location</option>
          <option *ngFor="let location of approvedLocations" [value]="location.id">
            {{ location.city }}, {{ location.state }} ({{ location.status }})
          </option>
        </select>

        <label>Address / Pickup Point</label>
        <input type="text" formControlName="address" />

        <button
          type="submit"
          [disabled]="officeAddressForm.invalid || requesting || approvedLocations.length === 0"
        >
          {{ requesting ? 'Adding Address...' : 'Add Office Address' }}
        </button>
      </form>

      <p *ngIf="approvedLocations.length === 0">
        No approved locations yet. Wait for admin approval after requesting a location.
      </p>
      <p class="success" *ngIf="successMessage">{{ successMessage }}</p>
      <p class="error" *ngIf="errorMessage">{{ errorMessage }}</p>

      <h3>Your Requested Locations</h3>
      <p *ngIf="loadingData">Loading operator locations...</p>
      <table *ngIf="!loadingData && operatorLocations.length > 0">
        <thead>
          <tr>
            <th>City</th>
            <th>State</th>
            <th>Address</th>
            <th>Location Approval</th>
            <th>Mapping Status</th>
          </tr>
        </thead>
        <tbody>
          <tr *ngFor="let row of operatorLocations">
            <td>{{ row.location.city }}</td>
            <td>{{ row.location.state }}</td>
            <td>{{ row.address }}</td>
            <td>
              <span
                class="status-pill"
                [ngClass]="
                  row.location.status === 'Approved' ? 'status-approved' : 'status-not-approved'
                "
              >
                {{ row.location.status }}
              </span>
            </td>
            <td>
              <span
                class="status-pill"
                [ngClass]="row.isActive ? 'status-approved' : 'status-rejected'"
              >
                {{ row.isActive ? 'Active' : 'Inactive' }}
              </span>
            </td>
          </tr>
        </tbody>
      </table>
      <p *ngIf="!loadingData && operatorLocations.length === 0">No location requests yet.</p>
    </div>
  `,
})
export class OperatorLocationsPageComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(ApiService);
  private readonly authService = inject(AuthService);
  private readonly cdr = inject(ChangeDetectorRef);

  readonly requestLocationForm = this.fb.group({
    city: ['', [Validators.required]],
    state: ['', [Validators.required]],
  });

  readonly officeAddressForm = this.fb.group({
    locationId: ['', [Validators.required]],
    address: ['', [Validators.required]],
  });

  approvedLocations: LocationOption[] = [];
  operatorLocations: OperatorLocationRow[] = [];
  requestingNewLocation = false;
  requesting = false;
  loadingData = false;
  successMessage = '';
  errorMessage = '';

  ngOnInit(): void {
    this.loadApprovedLocations();
    this.loadOperatorLocations();
  }

  requestNewLocation(): void {
    if (this.requestLocationForm.invalid || this.requestingNewLocation) {
      return;
    }

    const operatorId = this.authService.getUserId();
    if (!operatorId) {
      this.errorMessage = 'Operator identity not found. Please login again.';
      return;
    }

    this.requestingNewLocation = true;
    this.successMessage = '';
    this.errorMessage = '';

    this.api
      .post(`operators/${operatorId}/location-requests`, {
        city: this.requestLocationForm.value.city,
        state: this.requestLocationForm.value.state,
      })
      .subscribe({
        next: () => {
          this.requestingNewLocation = false;
          this.requestLocationForm.patchValue({ city: '', state: '' });
          this.successMessage = 'Location request sent for admin approval.';
          this.loadApprovedLocations();
          this.cdr.detectChanges();
        },
        error: (error) => {
          console.error(error);
          this.requestingNewLocation = false;
          this.errorMessage = this.extractError(error, 'Failed to request location approval.');
          this.cdr.detectChanges();
        },
      });
  }

  addOfficeAddress(): void {
    if (this.officeAddressForm.invalid || this.requesting) {
      return;
    }

    const operatorId = this.authService.getUserId();
    if (!operatorId) {
      this.errorMessage = 'Operator identity not found. Please login again.';
      return;
    }

    this.requesting = true;
    this.successMessage = '';
    this.errorMessage = '';

    this.api
      .post(`operators/${operatorId}/locations`, {
        locationId: this.officeAddressForm.value.locationId,
        address: this.officeAddressForm.value.address,
      })
      .subscribe({
        next: () => {
          this.requesting = false;
          this.officeAddressForm.patchValue({ locationId: '', address: '' });
          this.successMessage = 'Office address added successfully.';
          this.loadOperatorLocations();
          this.cdr.detectChanges();
        },
        error: (error) => {
          console.error(error);
          this.requesting = false;
          this.errorMessage = this.extractError(error, 'Failed to add office address.');
          this.cdr.detectChanges();
        },
      });
  }

  private loadApprovedLocations(): void {
    this.api.get<LocationOption[]>('locations').subscribe({
      next: (response) => {
        this.approvedLocations = response.filter((row) => row.status === 'Approved');
        this.cdr.detectChanges();
      },
      error: (error) => {
        console.error(error);
        this.cdr.detectChanges();
      },
    });
  }

  private loadOperatorLocations(): void {
    const operatorId = this.authService.getUserId();
    if (!operatorId) {
      return;
    }

    this.loadingData = true;
    this.api.get<OperatorLocationRow[]>(`operators/${operatorId}/locations`).subscribe({
      next: (response) => {
        this.operatorLocations = response;
        this.loadingData = false;
        this.cdr.detectChanges();
      },
      error: (error) => {
        console.error(error);
        this.loadingData = false;
        this.errorMessage = 'Unable to load your requested locations.';
        this.cdr.detectChanges();
      },
    });
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
