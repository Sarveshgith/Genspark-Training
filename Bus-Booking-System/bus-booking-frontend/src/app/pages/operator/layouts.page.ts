import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ApiService } from '../../core/api.service';

interface LayoutResponse {
  id: string;
  totalSeats: number;
  config: {
    rows?: number;
    cols?: number;
    seats?: Array<{ seatNumber: string; femaleOnly: boolean }>;
  };
  createdAt: string;
}

@Component({
  selector: 'app-operator-layouts-page',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  template: `
    <div class="page-container">
      <h2>Bus Layouts</h2>

      <form [formGroup]="form" (ngSubmit)="createLayout()" class="form-card">
        <label>Rows</label>
        <input type="number" formControlName="rows" min="1" />

        <label>Columns</label>
        <input type="number" formControlName="cols" min="1" />

        <label>Female-only seats (comma separated, e.g. A1,A2)</label>
        <input type="text" formControlName="femaleSeats" />

        <button type="submit" [disabled]="form.invalid || creating">
          {{ creating ? 'Creating Layout...' : 'Create Layout' }}
        </button>
      </form>

      <p class="error" *ngIf="errorMessage">{{ errorMessage }}</p>

      <h3>Existing Layouts</h3>
      <p *ngIf="loadingData">Loading layouts...</p>
      <table *ngIf="!loadingData && layouts.length > 0">
        <thead>
          <tr>
            <th>Name</th>
            <th>ID</th>
            <th>Total Seats</th>
            <th>Created</th>
          </tr>
        </thead>
        <tbody>
          <tr *ngFor="let layout of layouts">
            <td>{{ layoutName(layout) }}</td>
            <td>{{ layout.id }}</td>
            <td>{{ layout.totalSeats }}</td>
            <td>{{ layout.createdAt | date: 'short' }}</td>
          </tr>
        </tbody>
      </table>
    </div>
  `,
})
export class OperatorLayoutsPageComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(ApiService);
  private readonly cdr = inject(ChangeDetectorRef);

  readonly form = this.fb.group({
    rows: [5, [Validators.required, Validators.min(1)]],
    cols: [4, [Validators.required, Validators.min(1)]],
    femaleSeats: [''],
  });

  layouts: LayoutResponse[] = [];
  creating = false;
  loadingData = false;
  errorMessage = '';

  ngOnInit(): void {
    this.loadLayouts();
  }

  createLayout(): void {
    if (this.form.invalid || this.creating) {
      return;
    }

    const rows = this.form.value.rows ?? 1;
    const cols = this.form.value.cols ?? 1;
    const femaleSeatSet = new Set(
      (this.form.value.femaleSeats ?? '')
        .split(',')
        .map((value) => value.trim().toUpperCase())
        .filter((value) => value.length > 0),
    );

    const seats: { seatNumber: string; femaleOnly: boolean }[] = [];
    for (let r = 0; r < rows; r += 1) {
      for (let c = 1; c <= cols; c += 1) {
        const seatNumber = `${String.fromCharCode(65 + r)}${c}`;
        seats.push({ seatNumber, femaleOnly: femaleSeatSet.has(seatNumber) });
      }
    }

    this.creating = true;
    this.api
      .post('bus-layouts', {
        totalSeats: rows * cols,
        config: {
          rows,
          cols,
          seats,
        },
      })
      .subscribe({
        next: () => {
          this.creating = false;
          this.errorMessage = '';
          this.loadLayouts();
          this.cdr.detectChanges();
        },
        error: (error) => {
          console.error(error);
          this.creating = false;
          this.errorMessage = 'Failed to create layout.';
          this.cdr.detectChanges();
        },
      });
  }

  loadLayouts(): void {
    this.loadingData = true;
    this.api.get<LayoutResponse[]>('bus-layouts').subscribe({
      next: (response) => {
        this.layouts = response;
        this.loadingData = false;
        this.cdr.detectChanges();
      },
      error: (error) => {
        console.error(error);
        this.loadingData = false;
        this.errorMessage = 'Unable to load layouts.';
        this.cdr.detectChanges();
      },
    });
  }

  layoutName(layout: LayoutResponse): string {
    const rows = layout.config?.rows;
    const cols = layout.config?.cols;
    const femaleSeats = layout.config?.seats?.filter((seat) => seat.femaleOnly).length ?? 0;

    if (typeof rows === 'number' && typeof cols === 'number') {
      return `${rows}x${cols}f${femaleSeats}`;
    }

    return `Custom-${layout.id.slice(0, 8)}`;
  }
}
