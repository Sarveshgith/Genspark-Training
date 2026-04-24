import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { ApiService } from '../../core/api.service';

interface SeatRow {
  seatNumber: string;
  status: string;
  reservedUntil?: string | null;
}

interface TripSeatsResponse {
  tripId: string;
  busId: string;
  routeLabel?: string;
  layout: unknown;
  seats: SeatRow[];
}

interface SeatView {
  seatNumber: string;
  femaleOnly: boolean;
  bookingStatus: 'Available' | 'Reserved' | 'Booked';
}

interface LayoutMeta {
  rows: number;
  cols: number;
}

@Component({
  selector: 'app-trip-seats-page',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="page-container">
      <h2>Seat Selection</h2>

      <div class="card seat-top-meta">
        <p><strong>Bus:</strong> {{ busId || '-' }}</p>
        <p><strong>Route:</strong> {{ routeLabel || '-' }}</p>
        <p><strong>Selected:</strong> {{ selectedSeatNumbers.length }}</p>
      </div>

      <p class="error" *ngIf="errorMessage">{{ errorMessage }}</p>

      <p *ngIf="loadingData">Loading bus layout...</p>

      <div class="bus-layout-shell" *ngIf="!loadingData && seatViews.length > 0">
        <div class="bus-layout-header">Front</div>
        <div class="seats-grid" [style.gridTemplateColumns]="gridTemplateColumns()">
          <button
            type="button"
            class="seat seat-button"
            *ngFor="let seat of seatViews"
            [class.seat-female]="seat.femaleOnly"
            [class.seat-reserved]="seat.bookingStatus === 'Reserved'"
            [class.seat-booked]="seat.bookingStatus === 'Booked'"
            [class.seat-selected]="isSelected(seat.seatNumber)"
            [disabled]="seat.bookingStatus !== 'Available'"
            (click)="toggleSeat(seat.seatNumber)"
          >
            {{ seat.seatNumber }}
          </button>
        </div>
      </div>

      <p>Selected seats: {{ selectedSeatNumbers.join(', ') || 'None' }}</p>

      <div class="legend-row">
        <span class="legend-item">Available</span>
        <span class="legend-item legend-female">Female-only</span>
        <span class="legend-item legend-reserved">Reserved</span>
        <span class="legend-item legend-booked">Booked</span>
      </div>

      <button
        type="button"
        class="seat-continue-btn"
        [disabled]="selectedSeatNumbers.length === 0"
        (click)="continueToBooking()"
      >
        Continue to Passenger Details
      </button>
    </div>
  `,
})
export class TripSeatsPageComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly api = inject(ApiService);
  private readonly cdr = inject(ChangeDetectorRef);

  tripId = '';
  busId = '';
  routeLabel = '';
  seatViews: SeatView[] = [];
  selectedSeatNumbers: string[] = [];
  layoutMeta: LayoutMeta = { rows: 0, cols: 0 };
  loadingData = false;
  errorMessage = '';

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      this.errorMessage = 'Trip id is missing.';
      this.cdr.detectChanges();
      return;
    }

    this.tripId = id;
    this.loadSeats();
  }

  loadSeats(): void {
    this.loadingData = true;
    this.api.get<TripSeatsResponse>(`trips/${this.tripId}/seats`).subscribe({
      next: (response) => {
        this.busId = response.busId;
        this.routeLabel = response.routeLabel || 'Route details unavailable';
        this.layoutMeta = this.extractLayoutMeta(response.layout);
        this.seatViews = this.mapSeats(response.layout, response.seats);
        this.loadingData = false;
        this.cdr.detectChanges();
      },
      error: (error) => {
        console.error(error);
        this.loadingData = false;
        this.errorMessage =
          (error?.error as string) ||
          (error?.error?.title as string) ||
          'Unable to load trip seats.';
        this.cdr.detectChanges();
      },
    });
  }

  toggleSeat(seatNumber: string): void {
    if (this.selectedSeatNumbers.includes(seatNumber)) {
      this.selectedSeatNumbers = this.selectedSeatNumbers.filter((seat) => seat !== seatNumber);
      return;
    }

    this.selectedSeatNumbers = [...this.selectedSeatNumbers, seatNumber];
  }

  isSelected(seatNumber: string): boolean {
    return this.selectedSeatNumbers.includes(seatNumber);
  }

  continueToBooking(): void {
    const femaleOnlySeats = this.seatViews
      .filter((seat) => seat.femaleOnly)
      .map((seat) => seat.seatNumber);

    this.router.navigate(['/booking', 'new'], {
      state: {
        tripId: this.tripId,
        selectedSeats: this.selectedSeatNumbers,
        femaleOnlySeats,
      },
    });
  }

  gridTemplateColumns(): string {
    const cols = this.layoutMeta.cols > 0 ? this.layoutMeta.cols : 4;
    return `repeat(${cols}, minmax(58px, 1fr))`;
  }

  private mapSeats(layout: unknown, seatRows: SeatRow[]): SeatView[] {
    const layoutSeats = this.extractLayoutSeats(layout);

    const bookings = new Map<string, SeatRow>();
    for (const row of seatRows) {
      bookings.set(row.seatNumber.toUpperCase(), row);
    }

    const baseSeats =
      layoutSeats.length > 0
        ? layoutSeats
        : seatRows.map((row) => ({ seatNumber: row.seatNumber, femaleOnly: false }));

    return baseSeats.map((seat) => {
      const booking = bookings.get(seat.seatNumber.toUpperCase());
      let bookingStatus: 'Available' | 'Reserved' | 'Booked' = 'Available';

      if (booking?.status === 'Reserved') {
        bookingStatus = 'Reserved';
      }

      if (booking?.status === 'Confirmed') {
        bookingStatus = 'Booked';
      }

      return {
        seatNumber: seat.seatNumber,
        femaleOnly: seat.femaleOnly,
        bookingStatus,
      };
    });
  }

  private extractLayoutSeats(layout: unknown): { seatNumber: string; femaleOnly: boolean }[] {
    if (!layout || typeof layout !== 'object') {
      return [];
    }

    const rawSeats = (layout as { seats?: unknown }).seats;
    if (!Array.isArray(rawSeats)) {
      return [];
    }

    return rawSeats
      .filter((seat) => typeof seat === 'object' && seat !== null)
      .map((seat) => {
        const item = seat as { seatNumber?: unknown; femaleOnly?: unknown };
        return {
          seatNumber: String(item.seatNumber ?? '').toUpperCase(),
          femaleOnly: Boolean(item.femaleOnly),
        };
      })
      .filter((seat) => seat.seatNumber.length > 0);
  }

  private extractLayoutMeta(layout: unknown): LayoutMeta {
    if (!layout || typeof layout !== 'object') {
      return { rows: 0, cols: 0 };
    }

    const maybeRows = (layout as { rows?: unknown }).rows;
    const maybeCols = (layout as { cols?: unknown }).cols;

    return {
      rows: Number(maybeRows) || 0,
      cols: Number(maybeCols) || 0,
    };
  }
}
