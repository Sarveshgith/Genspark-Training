import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { ApiService } from '../../core/api.service';

interface PendingBus {
  id: string;
  operatorId: string;
  layoutId: string;
  vehicleNumber: string;
  status: string;
  createdAt: string;
}

@Component({
  selector: 'app-admin-buses-page',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="page-container">
      <h2>Buses</h2>
      <button type="button" (click)="loadBuses()" [disabled]="loadingData">Refresh</button>

      <p class="error" *ngIf="errorMessage">{{ errorMessage }}</p>

      <p *ngIf="loadingData">Loading buses...</p>
      <table *ngIf="!loadingData && buses.length > 0">
        <thead>
          <tr>
            <th>Bus ID</th>
            <th>Operator ID</th>
            <th>Layout ID</th>
            <th>Vehicle Number</th>
            <th>Status</th>
            <th>Approval</th>
            <th>Action</th>
          </tr>
        </thead>
        <tbody>
          <tr *ngFor="let bus of buses">
            <td>{{ bus.id }}</td>
            <td>{{ bus.operatorId }}</td>
            <td>{{ bus.layoutId }}</td>
            <td>{{ bus.vehicleNumber }}</td>
            <td>
              <span class="status-pill" [ngClass]="statusClass(bus.status)">{{ bus.status }}</span>
            </td>
            <td>
              <span
                class="status-pill"
                [ngClass]="isApproved(bus.status) ? 'status-approved' : 'status-not-approved'"
              >
                {{ isApproved(bus.status) ? 'Approved' : 'Not Approved' }}
              </span>
            </td>
            <td>
              <button
                type="button"
                (click)="approve(bus.id, 'Approved')"
                [disabled]="isApproved(bus.status) || processingBusId === bus.id"
              >
                {{ processingBusId === bus.id ? 'Processing...' : 'Approve' }}
              </button>
              <button
                type="button"
                (click)="approve(bus.id, 'Rejected')"
                [disabled]="processingBusId === bus.id"
              >
                {{ processingBusId === bus.id ? 'Processing...' : 'Reject' }}
              </button>
            </td>
          </tr>
        </tbody>
      </table>

      <p *ngIf="buses.length === 0">No buses found.</p>
    </div>
  `,
})
export class AdminBusesPageComponent implements OnInit {
  private readonly api = inject(ApiService);
  private readonly cdr = inject(ChangeDetectorRef);

  buses: PendingBus[] = [];
  loadingData = false;
  processingBusId = '';
  errorMessage = '';

  ngOnInit(): void {
    this.loadBuses();
  }

  loadBuses(): void {
    this.errorMessage = '';
    this.loadingData = true;
    this.api.get<PendingBus[]>('buses').subscribe({
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

  approve(busId: string, status: 'Approved' | 'Rejected'): void {
    this.processingBusId = busId;
    this.api.patch(`buses/${busId}/approve`, { status }).subscribe({
      next: () => {
        this.processingBusId = '';
        this.loadBuses();
        this.cdr.detectChanges();
      },
      error: (error) => {
        console.error(error);
        this.processingBusId = '';
        this.errorMessage = 'Failed to update bus status.';
        this.cdr.detectChanges();
      },
    });
  }

  isApproved(status: string): boolean {
    return status === 'Approved';
  }

  statusClass(status: string): string {
    if (status === 'Approved') {
      return 'status-approved';
    }

    if (status === 'Rejected' || status === 'Cancelled') {
      return 'status-rejected';
    }

    return 'status-pending';
  }
}
