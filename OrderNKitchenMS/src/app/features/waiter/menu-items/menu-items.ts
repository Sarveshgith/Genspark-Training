import { Component, inject, signal, computed, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { forkJoin, of } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { MenuService } from '../../../core/services/menu.service';
import { MenuItemModel, CategoryModel } from '../../../core/models/menu.model';
import { AuthService } from '../../../core/services/auth.service';
import { OrderService } from '../../../core/services/order.service';
import { TableModel } from '../../../core/models/order.model';
import { CartSidebarComponent } from './components/cart/cart';
import { FilterPanelComponent } from './components/filter-panel/filter-panel';

export interface CartItem {
  menuItem: MenuItemModel;
  quantity: number;
  notes: string;
}

@Component({
  selector: 'app-menu-items',
  imports: [FormsModule, CartSidebarComponent, FilterPanelComponent],
  templateUrl: './menu-items.html',
  styleUrl: './menu-items.css',
})
export class MenuItems implements OnInit {
  public menuService = inject(MenuService);
  public orderService = inject(OrderService);
  public authService = inject(AuthService);

  // Readonly computed signals as requested
  public readonly isWaiter = computed(() => this.authService.getRole()?.toLowerCase() === 'waiter');
  public readonly canOrder = computed(() => this.isWaiter());
  public readonly canViewCart = computed(() => this.isWaiter());

  public menuItems = signal<MenuItemModel[]>([]);
  public categories = signal<CategoryModel[]>([]);
  public tables = signal<TableModel[]>([]);

  // Category selection name
  public selectedCategory = signal<string>('All');
  public searchQuery = signal<string>('');

  // Veg/Non-Veg toggle: 'all' | 'veg' | 'non-veg'
  public vegToggle = signal<string>('all');

  // Advanced filter form state
  public filterMinPrice = signal<number | null>(null);
  public filterMaxPrice = signal<number | null>(null);
  public filterMaxPrepTime = signal<number | null>(null);
  public filterAvailability = signal<string>('all'); // 'all' | 'available' | 'outofstock'

  // Expandable filter panel state
  public isFilterPanelOpen = signal<boolean>(false);

  // Guest details modal state
  public isModalOpen = signal(false);
  public selectedItem = signal<MenuItemModel | null>(null);

  // Waiter cart state
  public cartItems = signal<CartItem[]>([]);
  public selectedTableId = signal<number | null>(null);

  // Status/Feedback properties
  public checkoutSuccess = signal<string | null>(null);
  public checkoutError = signal<string | null>(null);
  public isSubmitting = signal<boolean>(false);

  // Extract unique category names for tab display
  public uniqueCategoryNames = computed(() => {
    const names = this.categories().map(c => c.name);
    return Array.from(new Set(names));
  });

  // Check if advanced filters are active (non-default values)
  public areFiltersActive = computed(() => {
    return this.filterMinPrice() !== null ||
           this.filterMaxPrice() !== null ||
           this.filterMaxPrepTime() !== null ||
           this.filterAvailability() !== 'all';
  });

  ngOnInit(): void {
    this.fetchCategories();
    if (this.isWaiter()) {
      this.fetchTables();
    }
  }

  fetchMenuItems(): void {
    const search = this.searchQuery().trim();
    const minPrice = this.filterMinPrice();
    const maxPrice = this.filterMaxPrice();
    const maxPrepTime = this.filterMaxPrepTime();
    const availability = this.filterAvailability();
    const vegStatus = this.vegToggle();
    const catName = this.selectedCategory();

    const baseQuery: any = {
      pageSize: 100
    };

    if (search) {
      baseQuery.name = search;
    }
    if (minPrice !== null && minPrice !== undefined) {
      baseQuery.minPrice = minPrice;
    }
    if (maxPrice !== null && maxPrice !== undefined) {
      baseQuery.maxPrice = maxPrice;
    }
    if (maxPrepTime !== null && maxPrepTime !== undefined) {
      baseQuery.maxPreparationTime = maxPrepTime;
    }
    if (availability === 'available') {
      baseQuery.isAvailable = true;
    } else if (availability === 'outofstock') {
      baseQuery.isAvailable = false;
    }

    if (catName !== 'All') {
      const matchingCats = this.categories().filter(c => c.name.toLowerCase() === catName.toLowerCase());
      
      if (vegStatus === 'veg') {
        const vegCat = matchingCats.find(c => !c.isNonVeg);
        if (vegCat) {
          baseQuery.categoryId = vegCat.id;
          this.executeFetch(baseQuery);
        } else {
          this.menuItems.set([]);
        }
      } else if (vegStatus === 'non-veg') {
        const nonVegCat = matchingCats.find(c => c.isNonVeg);
        if (nonVegCat) {
          baseQuery.categoryId = nonVegCat.id;
          this.executeFetch(baseQuery);
        } else {
          this.menuItems.set([]);
        }
      } else {
        if (matchingCats.length > 0) {
          const requests = matchingCats.map(cat => {
            const catQuery = { ...baseQuery, categoryId: cat.id };
            return this.menuService.getMenuItems(catQuery).pipe(
              catchError(err => {
                console.error(`Failed to fetch items for category ID ${cat.id}`, err);
                return of([] as MenuItemModel[]);
              })
            );
          });

          forkJoin(requests).subscribe({
            next: (results) => {
              const combined = results.reduce((acc, curr) => acc.concat(curr), []);
              combined.sort((a, b) => a.name.localeCompare(b.name));
              this.menuItems.set(combined);
            },
            error: (err) => {
              console.error('Error combining category fetches:', err);
            }
          });
        } else {
          this.menuItems.set([]);
        }
      }
    } else {
      this.menuService.getMenuItems(baseQuery).subscribe({
        next: (items) => {
          let filtered = items;
          if (vegStatus === 'veg') {
            filtered = items.filter(item => {
              const cat = this.categories().find(c => c.id === item.categoryId);
              return cat ? !cat.isNonVeg : true;
            });
          } else if (vegStatus === 'non-veg') {
            filtered = items.filter(item => {
              const cat = this.categories().find(c => c.id === item.categoryId);
              return cat ? cat.isNonVeg : false;
            });
          }
          this.menuItems.set(filtered);
        },
        error: (err) => {
          console.error('Failed to load menu items', err);
        }
      });
    }
  }

  executeFetch(query: any): void {
    this.menuService.getMenuItems(query).subscribe({
      next: (items) => {
        this.menuItems.set(items);
      },
      error: (err) => {
        console.error('Failed to load menu items', err);
      }
    });
  }

  fetchCategories(): void {
    this.menuService.getMenuCategories().subscribe({
      next: (cats) => {
        console.log('Categories fetched:', cats);
        this.categories.set(cats);
        this.fetchMenuItems();
      },
      error: (err) => {
        console.error('Failed to load categories', err);
        this.fetchMenuItems();
      }
    });
  }

  fetchTables(): void {
    this.orderService.getTables().subscribe({
      next: (tbls) => {
        console.log('Tables fetched:', tbls);
        this.tables.set(tbls.filter(t => !t.isDeleted));
      },
      error: (err) => {
        console.error('Failed to load tables', err);
      }
    });
  }

  selectCategory(categoryName: string): void {
    this.selectedCategory.set(categoryName);
    this.fetchMenuItems();
  }

  setVegToggle(status: string): void {
    this.vegToggle.set(status);
    this.fetchMenuItems();
  }

  toggleFilterPanel(): void {
    this.isFilterPanelOpen.set(!this.isFilterPanelOpen());
  }

  applyFilters(): void {
    this.fetchMenuItems();
    this.isFilterPanelOpen.set(false);
  }

  clearFilters(): void {
    this.filterMinPrice.set(null);
    this.filterMaxPrice.set(null);
    this.filterMaxPrepTime.set(null);
    this.filterAvailability.set('all');
    this.vegToggle.set('all');
    this.selectedCategory.set('All');
    this.searchQuery.set('');
    this.fetchMenuItems();
    this.isFilterPanelOpen.set(false);
  }

  handleClick(item: MenuItemModel): void {
    console.log('Item clicked:', item);
    if (this.isWaiter()) {
      if (!item.isAvailable) return;
      
      const currentCart = this.cartItems();
      const existingItem = currentCart.find(cartItem => cartItem.menuItem.id === item.id);
      
      if (existingItem) {
        this.updateQuantity(item.id, existingItem.quantity + 1);
      } else {
        this.cartItems.set([...currentCart, { menuItem: item, quantity: 1, notes: '' }]);
      }
      this.checkoutSuccess.set(null);
      this.checkoutError.set(null);
    } else {
      this.selectedItem.set(item);
      this.isModalOpen.set(true);
    }
  }

  updateQuantity(itemId: number, newQty: number): void {
    if (newQty <= 0) {
      this.removeFromCart(itemId);
      return;
    }
    const currentCart = this.cartItems();
    const updatedCart = currentCart.map(item => {
      if (item.menuItem.id === itemId) {
        return { ...item, quantity: newQty };
      }
      return item;
    });
    this.cartItems.set(updatedCart);
  }

  updateNotes(itemId: number, notes: string): void {
    const currentCart = this.cartItems();
    const updatedCart = currentCart.map(item => {
      if (item.menuItem.id === itemId) {
        return { ...item, notes: notes };
      }
      return item;
    });
    this.cartItems.set(updatedCart);
  }

  removeFromCart(itemId: number): void {
    const currentCart = this.cartItems();
    this.cartItems.set(currentCart.filter(item => item.menuItem.id !== itemId));
  }

  getCartTotal(): number {
    return this.cartItems().reduce((sum, item) => sum + (item.menuItem.price * item.quantity), 0);
  }

  closeModal(event?: MouseEvent): void {
    if (event) {
      const target = event.target as HTMLElement;
      if (target.classList.contains('modal-backdrop')) {
        this.isModalOpen.set(false);
      }
    } else {
      this.isModalOpen.set(false);
    }
  }

  submitWaiterOrder(): void {
    const tableId = this.selectedTableId();
    const cart = this.cartItems();

    if (!tableId) {
      this.checkoutError.set('Please select a Table number.');
      return;
    }

    if (cart.length === 0) {
      this.checkoutError.set('Your cart is empty.');
      return;
    }

    const orderItems = cart.map(item => ({
      menuItemId: item.menuItem.id,
      quantity: item.quantity,
      notes: item.notes
    }));

    const payload = {
      tableId: Number(tableId),
      orderItems: orderItems
    };

    this.isSubmitting.set(true);
    this.checkoutSuccess.set(null);
    this.checkoutError.set(null);

    this.orderService.createOrder(payload).subscribe({
      next: (res) => {
        console.log('Order created successfully:', res);
        this.checkoutSuccess.set(`Order placed successfully for Table ${res.tableNumber}!`);
        this.cartItems.set([]);
        this.selectedTableId.set(null);
        this.isSubmitting.set(false);
      },
      error: (err) => {
        console.error('Failed to create order:', err);
        this.checkoutError.set(err.error?.message || err.message || 'Failed to place order. Please try again.');
        this.isSubmitting.set(false);
      }
    });
  }
}
