// @feature Admin | Inventory Manager | Kitchen inventory stock levels tracking, restock management, ingredient CRUD, and recipe mapping constructors.
import { Component, inject, OnInit, signal, computed, ChangeDetectorRef, NgZone } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { InventoryService } from '../../../core/services/inventory.service';
import { MenuService } from '../../../core/services/menu.service';
import { ToastService } from '../../../core/services/toast.service';
import { InventoryItemModel, MenuItemIngredientModel } from '../../../core/models/inventory.model';
import { MenuItemModel } from '../../../core/models/menu.model';

@Component({
  selector: 'app-inventory-manager',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './inventory-manager.html',
  styleUrl: './inventory-manager.css'
})
export class InventoryManager implements OnInit {
  private inventoryService = inject(InventoryService);
  private menuService = inject(MenuService);
  private toastService = inject(ToastService);
  private cdr = inject(ChangeDetectorRef);
  private zone = inject(NgZone);

  // Active Tab: 'inventory' | 'recipes'
  public activeTab = signal<string>('inventory');

  // Raw states
  public inventoryItems = signal<InventoryItemModel[]>([]);
  public menuItems = signal<MenuItemModel[]>([]);

  // Search & Filtering
  public searchQuery = signal<string>('');
  public stockFilter = signal<string>('all'); // 'all', 'low-stock', 'active', 'inactive'

  // Edit / Create Modals States
  public isEditMode = signal<boolean>(false);
  public selectedItemId = signal<number | null>(null);
  public deleteConfirmItemId = signal<number | null>(null);
  public formName: string = '';
  public formUnit: number = 1;
  public formQuantity: number = 0;
  public formThreshold: number = 5;
  public formCost: number | null = null; // placeholder
  public formIsActive: boolean = true;

  // Inline Quick Restock State
  public restockingItemId = signal<number | null>(null);
  public restockQtyInput: number = 10;

  // Recipe Mapping Drawer States
  public isRecipeDrawerOpen = signal<boolean>(false);
  public selectedRecipeMenuItemId = signal<number | null>(null);
  public selectedRecipeMenuItemName = signal<string>('');
  
  // Local draft array for mapping ingredients within drawer
  public recipeDraft = signal<{ itemId: number; itemName: string; unitName: string; quantityRequired: number }[]>([]);
  
  // Drawer Add mappings
  public drawerAddIngredientId: number = 0;
  public drawerAddQuantity: number = 1;

  // Computed Unit Types
  public unitMap: { [key: number]: string } = {
    1: 'Grams',
    2: 'Milliliters',
    3: 'Pieces',
    4: 'Kilograms',
    5: 'Liters'
  };

  // Computed: Low Stock list for notification banner ticker
  public lowStockItems = computed(() => {
    return this.inventoryItems().filter(item => item.isActive && item.stockQuantity < item.stockThreshold);
  });

  // Computed: KPI Statistics
  public totalIngredientsCount = computed(() => this.inventoryItems().length);
  
  public activeIngredientsCount = computed(() => {
    return this.inventoryItems().filter(i => i.isActive).length;
  });

  public lowStockCount = computed(() => this.lowStockItems().length);

  // Computed: Filtered Stock list
  public filteredInventoryItems = computed(() => {
    let list = this.inventoryItems();

    const query = this.searchQuery().trim().toLowerCase();
    if (query) {
      list = list.filter(item => item.name.toLowerCase().includes(query));
    }

    const filter = this.stockFilter();
    if (filter === 'low-stock') {
      list = list.filter(item => item.isActive && item.stockQuantity < item.stockThreshold);
    } else if (filter === 'active') {
      list = list.filter(item => item.isActive);
    } else if (filter === 'inactive') {
      list = list.filter(item => !item.isActive);
    }

    return list;
  });

  // Computed: Active items only for recipe selection mapping
  public activeInventoryItemsForRecipes = computed(() => {
    return this.inventoryItems().filter(item => item.isActive);
  });

  ngOnInit(): void {
    this.fetchData();
  }

  public fetchData(): void {
    // Fetch Inventory
    this.inventoryService.getInventoryItems().subscribe({
      next: (items: InventoryItemModel[]) => {
        this.zone.run(() => {
          this.inventoryItems.set(items);
          this.cdr.detectChanges();
        });
      },
      error: (err) => {
        this.zone.run(() => {
          this.toastService.error('Failed to load inventory stock.');
        });
        console.error('Failed to load inventory:', err);
      }
    });

    // Fetch Menu items (for Recipe Mapping tab)
    this.menuService.getMenuItems().subscribe({
      next: (items: MenuItemModel[]) => {
        this.zone.run(() => {
          this.menuItems.set(items);
          this.cdr.detectChanges();
        });
      },
      error: (err) => {
        console.error('Failed to load menu items:', err);
      }
    });
  }

  // Quick Inline Restock
  public startRestock(item: InventoryItemModel, event: MouseEvent): void {
    event.stopPropagation();
    this.restockingItemId.set(item.id);
    this.restockQtyInput = 10;
  }

  public cancelRestock(event: MouseEvent): void {
    event.stopPropagation();
    this.restockingItemId.set(null);
  }

  public saveRestock(itemId: number, event: MouseEvent): void {
    event.stopPropagation();
    const qty = this.restockQtyInput;
    if (qty <= 0) {
      this.toastService.error('Restock quantity must be positive.');
      return;
    }

    this.inventoryService.restockInventoryItem(itemId, qty).subscribe({
      next: () => {
        this.zone.run(() => {
          this.restockingItemId.set(null);
          this.toastService.success('Stock level updated successfully.');
          this.fetchData();
        });
      },
      error: (err) => {
        this.zone.run(() => {
          this.toastService.error(err.error?.message || 'Failed to restock item.');
        });
        console.error('Failed to restock item:', err);
      }
    });
  }

  // CRUD: Open Dialogs
  public openAddModal(dialog: HTMLDialogElement): void {
    this.isEditMode.set(false);
    this.selectedItemId.set(null);

    this.formName = '';
    this.formUnit = 1;
    this.formQuantity = 0;
    this.formThreshold = 5;
    this.formCost = null;
    this.formIsActive = true;

    dialog.showModal();
  }

  public openEditModal(item: InventoryItemModel, dialog: HTMLDialogElement): void {
    this.isEditMode.set(true);
    this.selectedItemId.set(item.id);

    this.formName = item.name;
    this.formUnit = item.unit;
    this.formQuantity = item.stockQuantity;
    this.formThreshold = item.stockThreshold;
    this.formCost = item.costPerUnit;
    this.formIsActive = item.isActive;

    dialog.showModal();
  }

  // CRUD: Form Submit
  public onFormSubmit(dialog: HTMLDialogElement): void {
    const name = this.formName.trim();
    const unit = this.formUnit;
    const stockQuantity = this.formQuantity;
    const stockThreshold = this.formThreshold;
    const costPerUnit = this.formCost;
    const isActive = this.formIsActive;

    if (!name) {
      this.toastService.error('Name is required.');
      return;
    }
    if (stockQuantity < 0) {
      this.toastService.error('Stock quantity cannot be negative.');
      return;
    }
    if (stockThreshold < 0) {
      this.toastService.error('Safety threshold cannot be negative.');
      return;
    }
    if (costPerUnit !== null && costPerUnit < 0) {
      this.toastService.error('Unit cost cannot be negative.');
      return;
    }

    const payload = { name, unit, stockQuantity, stockThreshold, costPerUnit, isActive };

    const request$ = this.isEditMode() && this.selectedItemId()
      ? this.inventoryService.updateInventoryItem(this.selectedItemId()!, payload)
      : this.inventoryService.createInventoryItem(payload);

    request$.subscribe({
      next: () => {
        this.zone.run(() => {
          this.toastService.success(this.isEditMode() ? 'Inventory item updated successfully.' : 'New inventory ingredient created successfully.');
          this.fetchData();
          dialog.close();
        });
      },
      error: (err) => {
        this.zone.run(() => {
          this.toastService.error(err.error?.message || 'Error occurred while saving item.');
        });
        console.error('Failed to save item:', err);
      }
    });
  }

  // CRUD: Toggle Active status directly
  public toggleActive(item: InventoryItemModel): void {
    const targetStatus = !item.isActive;

    // Use update put to toggle
    this.inventoryService.updateInventoryItem(item.id, {
      name: item.name,
      unit: item.unit,
      stockQuantity: item.stockQuantity,
      stockThreshold: item.stockThreshold,
      costPerUnit: item.costPerUnit,
      isActive: targetStatus
    }).subscribe({
      next: () => {
        this.zone.run(() => {
          this.toastService.success(`Ingredient "${item.name}" is now ${targetStatus ? 'Active' : 'Inactive'}.`);
          this.fetchData();
        });
      },
      error: (err) => {
        this.zone.run(() => {
          this.toastService.error(err.error?.message || 'Failed to toggle item status.');
        });
        console.error('Failed to toggle active status:', err);
      }
    });
  }

  // CRUD: Deactivate directly (soft delete / active toggle to false)
  public triggerDelete(id: number, deleteDialog: HTMLDialogElement): void {
    this.deleteConfirmItemId.set(id);
    deleteDialog.showModal();
  }

  public getConfirmDeleteIngredientName(): string {
    const id = this.deleteConfirmItemId();
    return this.inventoryItems().find(inv => inv.id === id)?.name || '';
  }

  public confirmDelete(deleteDialog: HTMLDialogElement): void {
    const id = this.deleteConfirmItemId();
    if (!id) return;
    const item = this.inventoryItems().find(inv => inv.id === id);

    this.inventoryService.deactivateInventoryItem(id).subscribe({
      next: () => {
        this.zone.run(() => {
          this.toastService.success(`Ingredient "${item?.name || 'Item'}" deactivated successfully.`);
          deleteDialog.close();
          this.deleteConfirmItemId.set(null);
          this.fetchData();
        });
      },
      error: (err) => {
        this.zone.run(() => {
          this.toastService.error(err.error?.message || 'Failed to deactivate ingredient.');
        });
        console.error('Failed to deactivate ingredient:', err);
        deleteDialog.close();
      }
    });
  }

  // Recipe Drawer mapping operations
  public openRecipeDrawer(menuItem: MenuItemModel): void {
    this.selectedRecipeMenuItemId.set(menuItem.id);
    this.selectedRecipeMenuItemName.set(menuItem.name);
    this.recipeDraft.set([]);

    // Set default add dropdown selection
    const activeItems = this.activeInventoryItemsForRecipes();
    if (activeItems.length > 0) {
      this.drawerAddIngredientId = activeItems[0].id;
    }
    this.drawerAddQuantity = 1;

    // Fetch mapped ingredients
    this.inventoryService.getIngredientsForMenuItem(menuItem.id).subscribe({
      next: (ingredients: MenuItemIngredientModel[]) => {
        this.zone.run(() => {
          const draftList = ingredients.map(ing => {
            // Match with inventory name & unit
            const match = this.inventoryItems().find(inv => inv.id === ing.itemId);
            return {
              itemId: ing.itemId,
              itemName: ing.itemName || match?.name || `Ingredient #${ing.itemId}`,
              unitName: match?.unitName || '',
              quantityRequired: ing.quantityRequired
            };
          });
          this.recipeDraft.set(draftList);
          this.isRecipeDrawerOpen.set(true);
          this.cdr.detectChanges();
        });
      },
      error: (err) => {
        this.zone.run(() => {
          this.toastService.error('Failed to load recipe mapping.');
        });
        console.error('Failed to load recipe mapping:', err);
      }
    });
  }

  public closeRecipeDrawer(): void {
    this.isRecipeDrawerOpen.set(false);
    this.selectedRecipeMenuItemId.set(null);
  }

  public addIngredientToDraft(): void {
    const itemId = this.drawerAddIngredientId;
    const qty = this.drawerAddQuantity;

    if (!itemId) {
      this.toastService.error('Please select an ingredient.');
      return;
    }
    if (qty <= 0) {
      this.toastService.error('Required quantity must be positive.');
      return;
    }

    const match = this.inventoryItems().find(inv => inv.id === itemId);
    if (!match) return;

    this.recipeDraft.update(draft => {
      // Check duplicate
      const existing = draft.find(d => d.itemId === itemId);
      if (existing) {
        existing.quantityRequired += qty;
        return [...draft];
      } else {
        return [...draft, {
          itemId,
          itemName: match.name,
          unitName: match.unitName,
          quantityRequired: qty
        }];
      }
    });
  }

  public removeIngredientFromDraft(itemId: number): void {
    this.recipeDraft.update(draft => draft.filter(d => d.itemId !== itemId));
  }

  public saveRecipe(): void {
    const id = this.selectedRecipeMenuItemId();
    if (!id) return;

    const payload = this.recipeDraft().map(d => ({
      itemId: d.itemId,
      quantityRequired: d.quantityRequired
    }));

    this.inventoryService.updateMenuItemIngredients(id, payload).subscribe({
      next: () => {
        this.zone.run(() => {
          this.toastService.success(`Recipe for "${this.selectedRecipeMenuItemName()}" updated successfully.`);
          this.closeRecipeDrawer();
          this.fetchData();
        });
      },
      error: (err) => {
        this.zone.run(() => {
          this.toastService.error(err.error?.message || 'Failed to save recipe updates.');
        });
        console.error('Failed to save recipe mappings:', err);
      }
    });
  }

  // Dialog Helpers
  public closeModal(dialog: HTMLDialogElement): void {
    dialog.close();
  }

  public onDialogClick(event: MouseEvent, dialog: HTMLDialogElement): void {
    if (event.target === dialog) {
      const rect = dialog.getBoundingClientRect();
      const isDialogContent = (
        rect.top <= event.clientY &&
        event.clientY <= rect.top + rect.height &&
        rect.left <= event.clientX &&
        event.clientX <= rect.left + rect.width
      );
      if (!isDialogContent) {
        dialog.close();
      }
    }
  }
}
