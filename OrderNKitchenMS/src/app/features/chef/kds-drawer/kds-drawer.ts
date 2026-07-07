import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { InventorySheet } from '../inventory-sheet/inventory-sheet';
import { ShiftSummary } from '../shift-summary/shift-summary';
import { OrderModel } from '../../../core/models/order.model';

export interface KdsMessage {
  id: number;
  text: string;
  timestamp: Date;
  read: boolean;
}

@Component({
  selector: 'app-kds-drawer',
  standalone: true,
  imports: [CommonModule, InventorySheet, ShiftSummary],
  templateUrl: './kds-drawer.html',
  styleUrl: './kds-drawer.css'
})
export class KdsDrawer {
  @Input() content: 'inventory' | 'messages' | 'summary' | null = null;
  @Input() activeOrders: OrderModel[] = [];
  @Input() messages: KdsMessage[] = [];
  @Output() close = new EventEmitter<void>();
  @Output() markAllRead = new EventEmitter<void>();

  public handleClose(): void {
    this.close.emit();
  }

  public getTitle(): string {
    switch (this.content) {
      case 'inventory': return 'Active Ingredients';
      case 'messages': return 'System Messages';
      case 'summary': return 'Shift Summary';
      default: return '';
    }
  }
}
