import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ApiService } from '../../core/api.service';
import { AuthService } from '../../core/auth.service';

interface LayoutOption {
  id: string;
  totalSeats: number;
  config: {
    rows?: number;
    cols?: number;
    seats?: Array<{ seatNumber: string; femaleOnly: boolean }>;
  };
}

interface BusRow {
  id: string;
  operatorId: string;
  layoutId: string;
  vehicleNumber: string;
  status: string;
  createdAt: string;
}

interface OperatorStatusResponse {
  status: string;
}

@Component({
  selector: 'app-operator-buses-page',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  template: `
    <div class="page-container">
      <h2>Bus Management</h2>

      <p class="error" *ngIf="!isOperatorApproved">
        Your operator account is {{ operatorStatus || 'Pending' }}. Bus creation is enabled only after admin approval.
      </p>

      <form [formGroup]="form" (ngSubmit)="createBus()" class="form-card">
        <label>Vehicle Number</label>
        <input type="text" formControlName="vehicleNumber" [disabled]="!isOperatorApproved" />

        <label>Layout</label>
        <select formControlName="layoutId" [disabled]="!isOperatorApproved">
          <option value="">Select layout</option>
          <option *ngFor="let layout of layouts" [value]="layout.id">
            {{ layoutDisplayName(layout) }}
          </option>
        </select>

        <label>Status</label>
        <select formControlName="status" [disabled]="!isOperatorApproved">
          <option value="Pending">Pending</option>
          <option value="Inactive">Inactive</option>
        </select>

        <button type="submit" [disabled]="form.invalid || creating || !isOperatorApproved">
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
            <th>Layout</th>
            <th>Status</th>
            <th>Approval</th>
            <th>Created</th>
          </tr>
        </thead>
        <tbody>
          <tr *ngFor="let bus of buses">
            <td>{{ bus.vehicleNumber }}</td>
            <td>{{ layoutNameById(bus.layoutId) }}</td>
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
  operatorStatus = '';
  creating = false;
  loadingData = false;
  errorMessage = '';

  get isOperatorApproved(): boolean {
    return this.operatorStatus === 'Approved';
  }

  ngOnInit(): void {
    this.loadOperatorStatus();
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

    if (!this.isOperatorApproved) {
      this.errorMessage = 'Bus creation is available only for approved operators.';
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
          this.errorMessage = this.extractError(error, 'Failed to create bus.');
          this.cdr.detectChanges();
        },
      });
  }

  loadOperatorStatus(): void {
    const operatorId = this.authService.getUserId();
    if (!operatorId) {
      return;
    }

    this.api.get<OperatorStatusResponse>(`operators/${operatorId}`).subscribe({
      next: (response) => {
        this.operatorStatus = response.status;
        this.cdr.detectChanges();
      },
      error: (error) => {
        console.error(error);
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

  layoutDisplayName(layout: LayoutOption): string {
    const rows = layout.config?.rows;
    const cols = layout.config?.cols;
    const femaleSeats = layout.config?.seats?.filter((seat) => seat.femaleOnly).length ?? 0;

    if (typeof rows === 'number' && typeof cols === 'number') {
      return `${rows}x${cols}f${femaleSeats} (${layout.totalSeats} seats)`;
    }

    return `${layout.id} (${layout.totalSeats} seats)`;
  }

  layoutNameById(layoutId: string): string {
    const layout = this.layouts.find((row) => row.id === layoutId);
    if (!layout) {
      return layoutId;
    }

    return this.layoutDisplayName(layout);
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
}
