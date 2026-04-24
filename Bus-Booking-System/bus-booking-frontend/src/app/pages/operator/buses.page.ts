import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ApiService } from '../../core/api.service';
import { AuthService } from '../../core/auth.service';

interface LayoutOption {
  id: string;
  totalSeats: number;
}

interface BusRow {
  id: string;
  operatorId: string;
  layoutId: string;
  vehicleNumber: string;
  status: string;
  createdAt: string;
}

@Component({
  selector: 'app-operator-buses-page',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  template: `
    <div class="page-container">
      <h2>Bus Management</h2>

      <form [formGroup]="form" (ngSubmit)="createBus()" class="form-card">
        <label>Vehicle Number</label>
        <input type="text" formControlName="vehicleNumber" />

        <label>Layout</label>
        <select formControlName="layoutId">
          <option value="">Select layout</option>
          <option *ngFor="let layout of layouts" [value]="layout.id">
            {{ layout.id }} ({{ layout.totalSeats }} seats)
          </option>
        </select>

        <label>Status</label>
        <select formControlName="status">
          <option value="Pending">Pending</option>
          <option value="Inactive">Inactive</option>
        </select>

        <button type="submit" [disabled]="form.invalid || creating">
          {{ creating ? 'Adding Bus...' : 'Add Bus' }}
        </button>
      </form>

      <p class="error" *ngIf="errorMessage">{{ errorMessage }}</p>

      <h3>Your Buses</h3>
      <p *ngIf="loadingData">Loading buses...</p>
      <table *ngIf="!loadingData && buses.length > 0">
        <thead>
          <tr>
            <th>Vehicle Number</th>
            <th>Layout ID</th>
            <th>Status</th>
            <th>Approval</th>
            <th>Created</th>
          </tr>
        </thead>
        <tbody>
          <tr *ngFor="let bus of buses">
            <td>{{ bus.vehicleNumber }}</td>
            <td>{{ bus.layoutId }}</td>
            <td>
              <span class="status-pill" [ngClass]="statusClass(bus.status)">{{ bus.status }}</span>
            </td>
            <td>
              <span
                class="status-pill"
                [ngClass]="bus.status === 'Approved' ? 'status-approved' : 'status-not-approved'"
              >
                {{ bus.status === 'Approved' ? 'Approved' : 'Not Approved' }}
              </span>
            </td>
            <td>{{ bus.createdAt | date: 'short' }}</td>
          </tr>
        </tbody>
      </table>
    </div>
  `,
})
export class OperatorBusesPageComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(ApiService);
  private readonly authService = inject(AuthService);
  private readonly cdr = inject(ChangeDetectorRef);

  readonly form = this.fb.group({
    vehicleNumber: ['', [Validators.required]],
    layoutId: ['', [Validators.required]],
    status: ['Pending', [Validators.required]],
  });

  layouts: LayoutOption[] = [];
  buses: BusRow[] = [];
  creating = false;
  loadingData = false;
  errorMessage = '';

  ngOnInit(): void {
    this.loadLayouts();
    this.loadBuses();
  }

  createBus(): void {
    if (this.form.invalid || this.creating) {
      return;
    }

    const operatorId = this.authService.getUserId();
    if (!operatorId) {
      this.errorMessage = 'Operator session missing user id.';
      return;
    }

    this.creating = true;
    this.api
      .post('buses', {
        operatorId,
        layoutId: this.form.value.layoutId,
        vehicleNumber: this.form.value.vehicleNumber,
        status: this.form.value.status,
      })
      .subscribe({
        next: () => {
          this.creating = false;
          this.errorMessage = '';
          this.form.patchValue({ vehicleNumber: '' });
          this.loadBuses();
          this.cdr.detectChanges();
        },
        error: (error) => {
          console.error(error);
          this.creating = false;
          this.errorMessage = 'Failed to create bus.';
          this.cdr.detectChanges();
        },
      });
  }

  loadLayouts(): void {
    this.api.get<LayoutOption[]>('bus-layouts').subscribe({
      next: (response) => {
        this.layouts = response;
        this.cdr.detectChanges();
      },
      error: (error) => {
        console.error(error);
        this.cdr.detectChanges();
      },
    });
  }

  loadBuses(): void {
    this.loadingData = true;
    this.api.get<BusRow[]>('buses').subscribe({
      next: (response) => {
        this.buses = response;
        this.loadingData = false;
        this.cdr.detectChanges();
      },
      error: (error) => {
        console.error(error);
        this.loadingData = false;
        this.errorMessage = 'Unable to load buses.';
        this.cdr.detectChanges();
      },
    });
  }

  statusClass(status: string): string {
    if (status === 'Approved') {
      return 'status-approved';
    }

    if (status === 'Rejected' || status === 'Inactive') {
      return 'status-rejected';
    }

    return 'status-pending';
  }
}
