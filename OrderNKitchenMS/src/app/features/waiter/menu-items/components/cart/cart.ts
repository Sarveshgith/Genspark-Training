import { Component, Input, Output, EventEmitter } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TableModel } from '../../../../../core/models/table.model';
import { CartItem } from '../../menu-items';

@Component({
  selector: 'app-cart-sidebar',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './cart.html'
})
export class CartSidebarComponent {
  @Input() cartItems: CartItem[] = [];
  @Input() tables: TableModel[] = [];
  @Input() selectedTableId: number | null = null;
  @Input() isSubmitting: boolean = false;
  @Input() checkoutSuccess: string | null = null;
  @Input() checkoutError: string | null = null;
  @Input() taxPercent: string = 'X';

  @Output() selectedTableIdChange = new EventEmitter<number | null>();
  @Output() updateQuantity = new EventEmitter<{ itemId: number, newQty: number }>();
  @Output() updateNotes = new EventEmitter<{ itemId: number, notes: string }>();
  @Output() removeFromCart = new EventEmitter<number>();
  @Output() submitOrder = new EventEmitter<void>();

  onTableChange(tableId: any): void {
    const val = tableId ? Number(tableId) : null;
    this.selectedTableIdChange.emit(val);
  }

  getCartTotal(): number {
    return this.cartItems.reduce((sum, item) => sum + (item.menuItem.price * item.quantity), 0);
  }
}
