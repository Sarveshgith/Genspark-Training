import { CommonModule } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { ChangeDetectorRef } from '@angular/core';
import { forkJoin, of } from 'rxjs';
import { catchError, map, finalize } from 'rxjs/operators';
import { ApiService } from '../../core/api.service';

interface PendingOperator {
  userId: string;
  licenseNumber: string;
  status: string;
  createdAt: string;
  user: { name: string; email: string; phone: string };
}

interface OperatorLocation {
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

interface OperatorWithLocations extends PendingOperator {
  locations: OperatorLocation[];
}

@Component({
  selector: 'app-admin-operators-page',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="page-container">
      <h2>Operators</h2>
      <button type="button" (click)="loadOperators()" [disabled]="loadingData">Refresh</button>

      <p class="error" *ngIf="errorMessage">{{ errorMessage }}</p>

      <p *ngIf="loadingData">Loading operators...</p>

      <table *ngIf="!loadingData && operators.length > 0">
        <thead>
          <tr>
            <th>Name</th>
            <th>Email</th>
            <th>Phone</th>
            <th>License</th>
            <th>Status</th>
            <th>Approval</th>
            <th>Locations</th>
            <th>Action</th>
          </tr>
        </thead>
        <tbody>
          <tr *ngFor="let operator of operators">
            <td>{{ operator.user.name }}</td>
            <td>{{ operator.user.email }}</td>
            <td>{{ operator.user.phone }}</td>
            <td>{{ operator.licenseNumber }}</td>

            <td>
              <span class="status-pill" [ngClass]="statusClass(operator.status)">
                {{ operator.status }}
              </span>
            </td>

            <td>
              <span
                class="status-pill"
                [ngClass]="isApproved(operator.status) ? 'status-approved' : 'status-not-approved'"
              >
                {{ isApproved(operator.status) ? 'Approved' : 'Not Approved' }}
              </span>
            </td>

            <td>
              <div *ngIf="operator.locations.length > 0; else noLocations">
                <div *ngFor="let loc of operator.locations">
                  <strong>{{ loc.location.city }}, {{ loc.location.state }}</strong>

                  <span
                    class="status-pill"
                    [ngClass]="
                      loc.location.status === 'Approved' ? 'status-approved' : 'status-not-approved'
                    "
                  >
                    {{ loc.location.status }}
                  </span>

                  <span
                    class="status-pill"
                    [ngClass]="loc.isActive ? 'status-approved' : 'status-rejected'"
                  >
                    {{ loc.isActive ? 'Active' : 'Inactive' }}
                  </span>
                </div>
              </div>

              <ng-template #noLocations>
                <span class="status-pill status-pending">No locations</span>
              </ng-template>
            </td>

            <td>
              <button
                type="button"
                (click)="updateStatus(operator.userId, 'Approved')"
                [disabled]="isApproved(operator.status) || processingOperatorId === operator.userId"
              >
                {{ processingOperatorId === operator.userId ? 'Processing...' : 'Approve' }}
              </button>

              <button
                type="button"
                (click)="updateStatus(operator.userId, 'Rejected')"
                [disabled]="processingOperatorId === operator.userId"
              >
                {{ processingOperatorId === operator.userId ? 'Processing...' : 'Reject' }}
              </button>
            </td>
          </tr>
        </tbody>
      </table>

      <p *ngIf="!loadingData && operators.length === 0">No operators found.</p>
    </div>
  `,
})
export class AdminOperatorsPageComponent implements OnInit {
  private readonly api = inject(ApiService);
  private readonly cdr = inject(ChangeDetectorRef);

  operators: OperatorWithLocations[] = [];
  loadingData = false;
  processingOperatorId = '';
  errorMessage = '';

  ngOnInit(): void {
    this.loadOperators();
  }

  loadOperators(): void {
    this.errorMessage = '';
    this.loadingData = true;

    this.api
      .get<PendingOperator[]>('operators')
      .pipe(finalize(() => (this.loadingData = false)))
      .subscribe({
        next: (response) => {
          if (!response || response.length === 0) {
            this.operators = [];
            return;
          }

          const requests = response.map((operator) =>
            this.api.get<OperatorLocation[]>(`operators/${operator.userId}/locations`).pipe(
              map((locations) => ({ ...operator, locations })),
              catchError((error) => {
                console.error(error);
                return of({ ...operator, locations: [] });
              }),
            ),
          );

          forkJoin(requests).subscribe({
            next: (rows) => {
              this.operators = rows;
              this.cdr.detectChanges();
            },
            error: (error) => {
              console.error(error);
              this.errorMessage = 'Unable to load operator locations.';
            },
          });
        },
        error: (error) => {
          console.error(error);
          this.errorMessage = 'Unable to load operators.';
        },
      });
  }

  updateStatus(operatorId: string, status: 'Approved' | 'Rejected'): void {
    this.processingOperatorId = operatorId;

    this.api.patch(`operators/${operatorId}/status`, { status }).subscribe({
      next: () => {
        this.processingOperatorId = '';
        this.loadOperators();
      },
      error: (error) => {
        console.error(error);
        this.processingOperatorId = '';
        this.errorMessage = 'Failed to update operator status.';
      },
    });
  }

  isApproved(status: string): boolean {
    return status === 'Approved';
  }

  statusClass(status: string): string {
    if (status === 'Approved') return 'status-approved';
    if (status === 'Rejected' || status === 'Suspended') return 'status-rejected';
    return 'status-pending';
  }
}
