import { Component, inject, signal, Input, Output, EventEmitter, OnChanges, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { forkJoin, of } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { InventoryService } from '../../../core/services/inventory.service';
import { SignalRService } from '../../../core/services/signalr.service';
import { OrderModel } from '../../../core/models/order.model';
import { InventoryItemModel, MenuItemIngredientModel } from '../../../core/models/inventory.model';

@Component({
  selector: 'app-inventory-sheet',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './inventory-sheet.html',
  styleUrl: './inventory-sheet.css'
})
export class InventorySheet implements OnChanges {
  private inventoryService = inject(InventoryService);
  private signalRService = inject(SignalRService);

  @Input() isOpen = false;
  @Input() activeOrders: OrderModel[] = [];
  @Output() close = new EventEmitter<void>();

  public isLoading = signal<boolean>(false);
  public error = signal<string | null>(null);

  // List of computed active ingredients
  public activeIngredients = signal<InventoryItemModel[]>([]);

  // Track flagged items to disable buttons and avoid duplicates
  public flaggedItemIds = signal<Set<number>>(new Set<number>());

  // Track flagging state per item ID for loading feedback
  public flaggingStates = signal<{ [key: number]: boolean }>({});

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['isOpen'] && this.isOpen) {
      this.loadActiveIngredients();
    }
  }

  public loadActiveIngredients(): void {
    this.isLoading.set(true);
    this.error.set(null);

    // Extract unique active menu item IDs from currently active orders
    const menuItemsSet = new Set<number>();
    this.activeOrders.forEach(order => {
      if (order.orderItems) {
        order.orderItems.forEach(item => {
          menuItemsSet.add(item.menuItemId);
        });
      }
    });

    const activeMenuItemIds = Array.from(menuItemsSet);

    if (activeMenuItemIds.length === 0) {
      this.activeIngredients.set([]);
      this.isLoading.set(false);
      return;
    }

    // Parallel fetch: Inventory list + Ingredients list for each active menu item
    const inventory$ = this.inventoryService.getInventoryItems().pipe(
      catchError(err => {
        console.error('Failed to load inventory items', err);
        throw err;
      })
    );

    const ingredientCalls = activeMenuItemIds.map(menuItemId =>
      this.inventoryService.getIngredientsForMenuItem(menuItemId).pipe(
        catchError(err => {
          console.warn(`Failed to load ingredients for menu item ${menuItemId}`, err);
          return of([] as MenuItemIngredientModel[]); // Fallback to empty array if single item fails
        })
      )
    );

    forkJoin([inventory$, forkJoin(ingredientCalls)]).subscribe({
      next: ([inventoryItems, ingredientsListGroup]) => {
        // Flatten ingredients mapping
        const allActiveIngredients = ingredientsListGroup.reduce(
          (acc, val) => acc.concat(val),
          [] as MenuItemIngredientModel[]
        );

        // Find unique inventory item IDs required by active menu items
        const activeItemIds = new Set<number>(
          allActiveIngredients.map(ing => ing.itemId)
        );

        // Filter full inventory list to only these active ingredients
        const filtered = inventoryItems.filter(item => activeItemIds.has(item.id) && item.isActive);
        
        // Sort: items below threshold first, then alphabetically
        filtered.sort((a, b) => {
          const aBelow = a.stockQuantity < a.stockThreshold;
          const bBelow = b.stockQuantity < b.stockThreshold;
          if (aBelow && !bBelow) return -1;
          if (!aBelow && bBelow) return 1;
          return a.name.localeCompare(b.name);
        });

        this.activeIngredients.set(filtered);
        this.isLoading.set(false);
      },
      error: (err) => {
        console.error('Failed to resolve active ingredients data:', err);
        this.error.set('Failed to load current inventory levels.');
        this.isLoading.set(false);
      }
    });
  }

  public flagToAdmin(item: InventoryItemModel): void {
    const id = item.id;
    
    // Set loading state
    this.flaggingStates.update(states => ({ ...states, [id]: true }));

    this.signalRService.flagLowStockToAdmin(item.id, item.name, item.stockQuantity, item.unitName)
      .then(() => {
        this.flaggedItemIds.update(set => {
          const next = new Set(set);
          next.add(id);
          return next;
        });
      })
      .catch(err => {
        console.error('Failed to flag item:', err);
      })
      .finally(() => {
        this.flaggingStates.update(states => ({ ...states, [id]: false }));
      });
  }

  public isBelowThreshold(item: InventoryItemModel): boolean {
    return item.stockQuantity < item.stockThreshold;
  }

  public handleClose(): void {
    this.close.emit();
  }
}
