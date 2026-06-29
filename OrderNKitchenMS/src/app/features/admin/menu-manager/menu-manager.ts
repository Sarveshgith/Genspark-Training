// @feature Admin | Menu Manager | Administration panel to create, update, toggle availability, and archive restaurant menu items.
import { Component, inject, OnInit, signal, computed, ChangeDetectorRef, NgZone } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MenuService } from '../../../core/services/menu.service';
import { MenuItemModel, CategoryModel } from '../../../core/models/menu.model';

@Component({
  selector: 'app-menu-manager',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './menu-manager.html',
  styleUrl: './menu-manager.css'
})
export class MenuManager implements OnInit {
  private menuService = inject(MenuService);
  private cdr = inject(ChangeDetectorRef);
  private zone = inject(NgZone);

  // Raw states
  public menuItems = signal<MenuItemModel[]>([]);
  public categories = signal<CategoryModel[]>([]);
  public errorMessage = signal<string>('');
  public successMessage = signal<string>('');

  // Filters
  public searchQuery = signal<string>('');
  public categoryFilter = signal<string>('all');
  public availabilityFilter = signal<string>('all'); // 'all', 'available', 'disabled'

  // Modal Form States
  public isEditMode = signal<boolean>(false);
  public selectedItemId = signal<number | null>(null);
  
  public formName: string = '';
  public formDescription: string = '';
  public formPrice: number = 0;
  public formCategoryId: number = 0;
  public formImageUrl: string = '';
  public formPrepTime: number = 10;
  public formIsAvailable: boolean = true;

  // Computed: KPI Summary stats
  public totalItemsCount = computed(() => this.menuItems().length);
  
  public availableCount = computed(() => {
    return this.menuItems().filter(item => item.isAvailable).length;
  });

  public disabledCount = computed(() => {
    return this.menuItems().filter(item => !item.isAvailable).length;
  });

  public vegCount = computed(() => {
    return this.menuItems().filter(item => {
      const cat = this.categories().find(c => c.id === item.categoryId);
      return cat ? !cat.isNonVeg : true;
    }).length;
  });

  public nonVegCount = computed(() => {
    return this.menuItems().filter(item => {
      const cat = this.categories().find(c => c.id === item.categoryId);
      return cat ? cat.isNonVeg : false;
    }).length;
  });

  // Computed: Filtered list
  public filteredMenuItems = computed(() => {
    let list = this.menuItems();

    const query = this.searchQuery().trim().toLowerCase();
    if (query) {
      list = list.filter(item => item.name.toLowerCase().includes(query) || item.description.toLowerCase().includes(query));
    }

    const catId = this.categoryFilter();
    if (catId !== 'all') {
      list = list.filter(item => item.categoryId === parseInt(catId, 10));
    }

    const avail = this.availabilityFilter();
    if (avail === 'available') {
      list = list.filter(item => item.isAvailable);
    } else if (avail === 'disabled') {
      list = list.filter(item => !item.isAvailable);
    }

    return list;
  });

  ngOnInit(): void {
    this.fetchData();
  }

  public fetchData(): void {
    // Fetch Categories
    this.menuService.getMenuCategories().subscribe({
      next: (cats: CategoryModel[]) => {
        this.zone.run(() => {
          this.categories.set(cats.filter(c => !c.isDeleted));
          this.cdr.detectChanges();
        });
      },
      error: (err) => {
        console.error('Failed to fetch categories:', err);
      }
    });

    // Fetch Menu Items
    this.menuService.getMenuItems().subscribe({
      next: (items: MenuItemModel[]) => {
        this.zone.run(() => {
          this.menuItems.set(items);
          this.cdr.detectChanges();
        });
      },
      error: (err) => {
        this.errorMessage.set('Failed to load menu items.');
        console.error('Failed to load menu items:', err);
      }
    });
  }

  // Toggling availability
  public toggleAvailability(item: MenuItemModel): void {
    const targetStatus = !item.isAvailable;
    this.errorMessage.set('');
    this.successMessage.set('');

    this.menuService.toggleMenuItemAvailability(item.id, targetStatus).subscribe({
      next: () => {
        this.zone.run(() => {
          this.successMessage.set(`Successfully updated availability of "${item.name}".`);
          this.fetchData();
          this.clearMessagesAfterDelay();
        });
      },
      error: (err) => {
        this.zone.run(() => {
          this.errorMessage.set(err.error?.message || `Failed to change availability of "${item.name}".`);
          this.clearMessagesAfterDelay();
        });
        console.error('Failed to toggle availability:', err);
      }
    });
  }

  // Open Form modal
  public openAddModal(dialog: HTMLDialogElement): void {
    this.isEditMode.set(false);
    this.selectedItemId.set(null);
    this.errorMessage.set('');
    this.successMessage.set('');

    // Set form defaults
    this.formName = '';
    this.formDescription = '';
    this.formPrice = 0;
    this.formImageUrl = '';
    this.formPrepTime = 10;
    this.formIsAvailable = true;

    const activeCats = this.categories();
    if (activeCats.length > 0) {
      this.formCategoryId = activeCats[0].id;
    } else {
      this.formCategoryId = 0;
    }

    dialog.showModal();
  }

  public openEditModal(item: MenuItemModel, dialog: HTMLDialogElement): void {
    this.isEditMode.set(true);
    this.selectedItemId.set(item.id);
    this.errorMessage.set('');
    this.successMessage.set('');

    this.formName = item.name;
    this.formDescription = item.description;
    this.formPrice = item.price;
    this.formCategoryId = item.categoryId;
    this.formImageUrl = item.imageUrl;
    this.formPrepTime = item.preparationTime;
    this.formIsAvailable = item.isAvailable;

    dialog.showModal();
  }

  // Form Submit
  public onFormSubmit(dialog: HTMLDialogElement): void {
    const name = this.formName.trim();
    const description = this.formDescription.trim();
    const price = this.formPrice;
    const categoryId = this.formCategoryId;
    const imageUrl = this.formImageUrl.trim();
    const preparationTime = this.formPrepTime;
    const isAvailable = this.formIsAvailable;

    if (!name) {
      this.errorMessage.set('Menu item name is required.');
      return;
    }
    if (price < 0) {
      this.errorMessage.set('Price cannot be negative.');
      return;
    }
    if (categoryId <= 0) {
      this.errorMessage.set('Please assign a valid category.');
      return;
    }
    if (preparationTime <= 0) {
      this.errorMessage.set('Preparation time must be greater than 0 minutes.');
      return;
    }

    const payload = {
      name,
      description,
      price,
      categoryId,
      imageUrl,
      preparationTime,
      isAvailable
    };

    const request$ = this.isEditMode() && this.selectedItemId()
      ? this.menuService.updateMenuItem(this.selectedItemId()!, payload)
      : this.menuService.createMenuItem(payload);

    request$.subscribe({
      next: () => {
        this.zone.run(() => {
          this.fetchData();
          dialog.close();
          this.errorMessage.set('');
        });
      },
      error: (err) => {
        this.errorMessage.set(err.error?.message || 'An error occurred while saving the menu item.');
        console.error('Failed to save menu item:', err);
      }
    });
  }

  // Delete Menu Item
  public deleteMenuItem(item: MenuItemModel): void {
    if (!confirm(`Are you sure you want to delete/archive "${item.name}"? This will remove it from waiter and guest menus.`)) {
      return;
    }

    this.menuService.deleteMenuItem(item.id).subscribe({
      next: () => {
        this.zone.run(() => {
          this.fetchData();
        });
      },
      error: (err) => {
        alert(err.error?.message || 'Failed to delete menu item.');
        console.error('Failed to delete menu item:', err);
      }
    });
  }

  // Helper: clear toast messages
  private clearMessagesAfterDelay(): void {
    setTimeout(() => {
      this.zone.run(() => {
        this.errorMessage.set('');
        this.successMessage.set('');
      });
    }, 6000);
  }

  // Dialog backdrop close (light-dismiss)
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
