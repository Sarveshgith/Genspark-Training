import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { forkJoin } from 'rxjs';
import { ApiService } from '../../core/api.service';
import { AuthService } from '../../core/auth.service';

@Component({
  selector: 'app-operator-dashboard-page',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="page-container">
      <h2>Operator Dashboard</h2>
      <button type="button" (click)="loadDashboard()">Refresh</button>

      <div class="card" *ngIf="operatorStatus">
        <strong>Approval Status:</strong>
        <span class="status-pill" [ngClass]="statusClass(operatorStatus)">{{ operatorStatus }}</span>
      </div>

      <div class="grid-2" *ngIf="summary">
        <div class="card"><strong>Layouts:</strong> {{ summary.layouts }}</div>
        <div class="card"><strong>Buses:</strong> {{ summary.buses }}</div>
        <div class="card"><strong>Trips:</strong> {{ summary.trips }}</div>
      </div>

      <p class="error" *ngIf="errorMessage">{{ errorMessage }}</p>
    </div>
  `,
})
export class OperatorDashboardPageComponent implements OnInit {
  private readonly api = inject(ApiService);
  private readonly authService = inject(AuthService);
  private readonly cdr = inject(ChangeDetectorRef);

  summary: { layouts: number; buses: number; trips: number } | null = null;
  operatorStatus = '';
  errorMessage = '';

  ngOnInit(): void {
    this.loadDashboard();
  }

  loadDashboard(): void {
    this.errorMessage = '';
    const operatorId = this.authService.getUserId();
    if (!operatorId) {
      this.errorMessage = 'Operator session missing user id.';
      this.cdr.detectChanges();
      return;
    }

    forkJoin({
      layouts: this.api.get<unknown[]>('bus-layouts'),
      buses: this.api.get<unknown[]>('buses'),
      trips: this.api.get<unknown[]>('trips'),
      operator: this.api.get<{ status: string }>(`operators/${operatorId}`),
    }).subscribe({
      next: (response) => {
        this.summary = {
          layouts: response.layouts.length,
          buses: response.buses.length,
          trips: response.trips.length,
        };
        this.operatorStatus = response.operator.status;
        this.cdr.detectChanges();
      },
      error: (error) => {
        console.error(error);
        this.errorMessage = 'Unable to load operator summary.';
        this.cdr.detectChanges();
      },
    });
  }

  statusClass(status: string): string {
    if (status === 'Approved') {
      return 'status-approved';
    }

    if (status === 'Rejected' || status === 'Suspended') {
      return 'status-rejected';
    }

    return 'status-pending';
  }
}
