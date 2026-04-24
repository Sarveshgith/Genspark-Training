import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { forkJoin } from 'rxjs';
import { ApiService } from '../../core/api.service';

@Component({
  selector: 'app-operator-dashboard-page',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="page-container">
      <h2>Operator Dashboard</h2>
      <button type="button" (click)="loadSummary()">Refresh</button>

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
  private readonly cdr = inject(ChangeDetectorRef);

  summary: { layouts: number; buses: number; trips: number } | null = null;
  errorMessage = '';

  ngOnInit(): void {
    this.loadSummary();
  }

  loadSummary(): void {
    this.errorMessage = '';

    forkJoin({
      layouts: this.api.get<unknown[]>('bus-layouts'),
      buses: this.api.get<unknown[]>('buses'),
      trips: this.api.get<unknown[]>('trips'),
    }).subscribe({
      next: (response) => {
        this.summary = {
          layouts: response.layouts.length,
          buses: response.buses.length,
          trips: response.trips.length,
        };
        this.cdr.detectChanges();
      },
      error: (error) => {
        console.error(error);
        this.errorMessage = 'Unable to load operator summary.';
        this.cdr.detectChanges();
      },
    });
  }
}
