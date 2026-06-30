// @feature Admin | Category Manager | Administration interface for creating, modifying, and deactivating menu categories.
import { Component, inject, OnInit, signal, computed, ChangeDetectorRef, NgZone } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MenuService } from '../../../core/services/menu.service';
import { ToastService } from '../../../core/services/toast.service';
import { CategoryModel } from '../../../core/models/menu.model';

@Component({
  selector: 'app-category-manager',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './category-manager.html',
  styleUrl: './category-manager.css'
})
export class CategoryManager implements OnInit {
  private menuService = inject(MenuService);
  private toastService = inject(ToastService);
  private cdr = inject(ChangeDetectorRef);
  private zone = inject(NgZone);

  // States
  public categories = signal<CategoryModel[]>([]);

  // Search & Filtering
  public searchQuery = signal<string>('');
  public typeFilter = signal<string>('all'); // 'all', 'veg', 'non-veg'

  // Inline editing states
  public editingCategoryId = signal<number | null>(null);
  public deleteConfirmCategoryId = signal<number | null>(null);
  public editName: string = '';
  public editIsNonVeg: boolean = false;

  // Add Category form states
  public addName: string = '';
  public addIsNonVeg: boolean = false;

  // Computed: Filtered Categories
  public filteredCategories = computed(() => {
    let list = this.categories().filter(c => !c.isDeleted);

    const query = this.searchQuery().trim().toLowerCase();
    if (query) {
      list = list.filter(c => c.name.toLowerCase().includes(query));
    }

    const filter = this.typeFilter();
    if (filter === 'veg') {
      list = list.filter(c => !c.isNonVeg);
    } else if (filter === 'non-veg') {
      list = list.filter(c => c.isNonVeg);
    }

    return list;
  });

  ngOnInit(): void {
    this.fetchCategories();
  }

  public fetchCategories(): void {
    this.menuService.getMenuCategories().subscribe({
      next: (response: CategoryModel[]) => {
        this.zone.run(() => {
          this.categories.set(response);
          this.cdr.detectChanges();
        });
      },
      error: (err) => {
        this.zone.run(() => {
          this.toastService.error('Failed to fetch categories. Please try again later.');
        });
        console.error('Failed to fetch categories:', err);
      }
    });
  }

  // Add Category
  public onAddCategorySubmit(dialog: HTMLDialogElement): void {
    const name = this.addName.trim();
    const isNonVeg = this.addIsNonVeg;

    if (!name) {
      this.toastService.error('Category name is required.');
      return;
    }

    // Check duplicate
    if (this.categories().some(c => c.name.toLowerCase() === name.toLowerCase() && c.isNonVeg === isNonVeg && !c.isDeleted)) {
      this.toastService.error(`A ${isNonVeg ? 'Non-Veg' : 'Veg'} category with the name "${name}" already exists.`);
      return;
    }

    this.menuService.createCategory({ name, isNonVeg }).subscribe({
      next: () => {
        this.zone.run(() => {
          this.toastService.success(`Category "${name}" created successfully.`);
          this.fetchCategories();
          this.addName = '';
          this.addIsNonVeg = false;
          dialog.close();
        });
      },
      error: (err) => {
        this.zone.run(() => {
          this.toastService.error(err.error?.message || 'Failed to create category.');
        });
        console.error('Failed to create category:', err);
      }
    });
  }

  // Inline Edit states
  public startEdit(category: CategoryModel): void {
    this.editingCategoryId.set(category.id);
    this.editName = category.name;
    this.editIsNonVeg = category.isNonVeg;
  }

  public cancelEdit(): void {
    this.editingCategoryId.set(null);
  }

  public saveEdit(categoryId: number): void {
    const name = this.editName.trim();
    const isNonVeg = this.editIsNonVeg;

    if (!name) {
      this.toastService.error('Category name cannot be empty.');
      return;
    }

    // Check duplicate
    if (this.categories().some(c => c.name.toLowerCase() === name.toLowerCase() && c.isNonVeg === isNonVeg && c.id !== categoryId && !c.isDeleted)) {
      this.toastService.error(`Conflict: A ${isNonVeg ? 'Non-Veg' : 'Veg'} category with the name "${name}" already exists.`);
      return;
    }

    this.menuService.updateCategory(categoryId, { name, isNonVeg }).subscribe({
      next: () => {
        this.zone.run(() => {
          this.toastService.success(`Category details updated successfully.`);
          this.editingCategoryId.set(null);
          this.fetchCategories();
        });
      },
      error: (err) => {
        this.zone.run(() => {
          this.toastService.error(err.error?.message || 'Failed to update category.');
        });
        console.error('Failed to update category:', err);
      }
    });
  }

  // Toggle IsNonVeg directly
  public toggleIsNonVeg(category: CategoryModel): void {
    const newIsNonVeg = !category.isNonVeg;
    this.menuService.updateCategory(category.id, {
      name: category.name,
      isNonVeg: newIsNonVeg
    }).subscribe({
      next: () => {
        this.zone.run(() => {
          this.toastService.success(`Category "${category.name}" changed to ${newIsNonVeg ? 'Non-Veg' : 'Veg'}.`);
          this.fetchCategories();
        });
      },
      error: (err) => {
        this.zone.run(() => {
          this.toastService.error(err.error?.message || 'Failed to toggle category type.');
        });
        console.error('Failed to toggle category type:', err);
      }
    });
  }

  // Delete Category
  public triggerDelete(id: number, deleteDialog: HTMLDialogElement): void {
    this.deleteConfirmCategoryId.set(id);
    deleteDialog.showModal();
  }

  public getConfirmDeleteCategoryName(): string {
    const id = this.deleteConfirmCategoryId();
    return this.categories().find(c => c.id === id)?.name || '';
  }

  public confirmDelete(deleteDialog: HTMLDialogElement): void {
    const id = this.deleteConfirmCategoryId();
    if (!id) return;
    const category = this.categories().find(c => c.id === id);

    this.menuService.deleteCategory(id).subscribe({
      next: () => {
        this.zone.run(() => {
          this.toastService.success(`Category "${category?.name || 'Item'}" archived successfully.`);
          deleteDialog.close();
          this.deleteConfirmCategoryId.set(null);
          this.fetchCategories();
        });
      },
      error: (err) => {
        this.zone.run(() => {
          this.toastService.error(err.error?.message || 'Failed to delete category.');
        });
        console.error('Failed to delete category:', err);
        deleteDialog.close();
      }
    });
  }

  // Dialog management
  public openModal(dialog: HTMLDialogElement): void {
    dialog.showModal();
  }

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
