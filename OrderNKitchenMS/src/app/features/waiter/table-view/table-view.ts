import { Component, inject, OnInit, OnDestroy, signal, ChangeDetectorRef, NgZone } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TableService } from '../../../core/services/table.service';
import { TableModel } from '../../../core/models/table.model';
import { SignalRService } from '../../../core/services/signalr.service';
import { AuthService } from '../../../core/services/auth.service';
import { Router } from '@angular/router';
import { Subscription } from 'rxjs';

@Component({
  selector: 'app-table-view',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './table-view.html',
  styleUrl: './table-view.css',
})
export class TableView implements OnInit, OnDestroy {
  private tableService = inject(TableService);
  private authService = inject(AuthService);
  public signalRService = inject(SignalRService);
  private router = inject(Router);
  private cdr = inject(ChangeDetectorRef);
  private zone = inject(NgZone);

  public tables = signal<TableModel[]>([]);
  public timeTrigger = signal<number>(Date.now());

  private subscriptions: Subscription = new Subscription();
  private timeIntervalId: any;

  ngOnInit(): void {
    this.fetchTables();
    this.setupSignalR();

    // Update the ticking timers every 10 seconds
    this.timeIntervalId = setInterval(() => {
      this.timeTrigger.set(Date.now());
    }, 10000);
  }

  ngOnDestroy(): void {
    this.subscriptions.unsubscribe();
    if (this.timeIntervalId) {
      clearInterval(this.timeIntervalId);
    }
  }

  private setupSignalR(): void {
    const token = this.authService.getToken() ?? '';
    if (token) {
      this.signalRService.connect(token).then(() => {
        const sub = this.signalRService.tablesUpdated$.subscribe({
          next: () => {
            this.zone.run(() => {
              this.fetchTables();
            });
          }
        });
        this.subscriptions.add(sub);
      });
    }
  }

  public fetchTables(): void {
    this.tableService.getTables().subscribe({
      next: (response: TableModel[]) => {
        this.zone.run(() => {
          // Sort tables by number ascending
          const sorted = [...response].sort((a, b) => a.number - b.number);
          this.tables.set(sorted.filter(t => !t.isDeleted));
          this.cdr.detectChanges();
        });
      },
      error: (err) => {
        console.error('Failed to fetch tables:', err);
      }
    });
  }

  public getElapsedTime(createdAt: any): string {
    if (!createdAt) return '';
    // Reading timeTrigger signal to reactively update elapsed text
    const now = this.timeTrigger();
    const diffMs = now - new Date(createdAt).getTime();
    const diffMins = Math.max(0, Math.floor(diffMs / 60000));
    if (diffMins < 1) {
      const diffSecs = Math.max(0, Math.floor(diffMs / 1000));
      return `${diffSecs}s ago`;
    }
    if (diffMins < 60) {
      return `${diffMins}m ago`;
    }
    const diffHours = Math.floor(diffMins / 60);
    if (diffHours < 24) {
      return `${diffHours}h ago`;
    }
    const diffDays = Math.floor(diffHours / 24);
    return `${diffDays}d ago`;
  }

  public onTableClick(table: TableModel): void {
    if (table.status === 1) { // Available
      this.router.navigate(['/waiter/menu'], { queryParams: { tableId: table.id } });
    } else if (table.status === 2) { // Occupied
      this.router.navigate(['/waiter/active-order', table.id]);
    }
  }
}
