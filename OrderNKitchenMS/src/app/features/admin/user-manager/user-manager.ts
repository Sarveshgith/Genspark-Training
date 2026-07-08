// @feature Admin | User Manager | Administration dashboard component to manage system users, edit profile details, assign roles, and soft-delete accounts.
import { Component, inject, OnInit, signal, ChangeDetectorRef, NgZone } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { UserService, UserModel, RoleModel } from '../../../core/services/user.service';
import { ToastService } from '../../../core/services/toast.service';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-user-manager',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './user-manager.html',
  styleUrl: './user-manager.css'
})
export class UserManager implements OnInit {
  public userService = inject(UserService);
  private toastService = inject(ToastService);
  public authService = inject(AuthService);
  private cdr = inject(ChangeDetectorRef);
  private zone = inject(NgZone);

  // Lists
  public users = signal<UserModel[]>([]);
  public roles = signal<RoleModel[]>([]);

  // Tabs State
  public activeTab = signal<'approved' | 'pending'>('approved');

  // Search & Filtering State
  public searchQuery = signal<string>('');
  public roleFilter = signal<number | null>(null);

  // Pagination State
  public pageNumber = signal<number>(1);
  public pageSize = signal<number>(10);
  public hasMore = signal<boolean>(false);
  public isLoading = signal<boolean>(false);

  // Edit Form State
  public selectedUser = signal<UserModel | null>(null);
  public editName: string = '';
  public editEmail: string = '';
  public editPhoneNumber: string = '';
  public editAddress: string = '';

  // Delete State
  public userToDelete = signal<UserModel | null>(null);
  public userToReject = signal<UserModel | null>(null);

  // Role Change Confirmation State
  public roleChangeUser = signal<UserModel | null>(null);
  public roleChangeTargetRoleId = signal<number>(0);
  public roleChangeTargetRoleName = signal<string>('');

  ngOnInit(): void {
    this.fetchRoles();
    this.fetchUsers();
  }

  public fetchRoles(): void {
    this.userService.getRoles().subscribe({
      next: (response: RoleModel[]) => {
        this.zone.run(() => {
          const filtered = response.filter(r => r.name.toLowerCase() !== 'customer' && r.name.toLowerCase() !== 'deliveryman');
          this.roles.set(filtered);
          this.cdr.detectChanges();
        });
      },
      error: (err) => {
        console.error('Failed to fetch roles:', err);
      }
    });
  }

  public fetchUsers(): void {
    this.isLoading.set(true);
    const filterRoleId = this.roleFilter();
    const isPendingValue = this.activeTab() === 'pending';
    
    const query = {
      search: this.searchQuery().trim(),
      roleId: filterRoleId !== null ? filterRoleId : undefined,
      isPending: isPendingValue,
      pageNumber: this.pageNumber(),
      pageSize: this.pageSize()
    };

    this.userService.getUsers(query).subscribe({
      next: (response: UserModel[]) => {
        this.zone.run(() => {
          const activeUsers = response.filter(u => u.isDeleted !== 'True' && u.isDeleted !== 'true');
          this.users.set(activeUsers);
          this.hasMore.set(response.length === this.pageSize());
          this.isLoading.set(false);
          this.cdr.detectChanges();
        });
      },
      error: (err) => {
        this.zone.run(() => {
          this.toastService.error('Failed to fetch users.');
          this.isLoading.set(false);
          this.cdr.detectChanges();
        });
        console.error('Failed to fetch users:', err);
      }
    });
  }

  public onSearchInput(value: string): void {
    this.searchQuery.set(value);
    this.pageNumber.set(1);
    this.fetchUsers();
  }

  public onRoleFilterChange(value: string): void {
    const roleId = value === 'all' ? null : Number(value);
    this.roleFilter.set(roleId);
    this.pageNumber.set(1);
    this.fetchUsers();
  }

  public nextPage(): void {
    if (this.hasMore() && !this.isLoading()) {
      this.pageNumber.update(p => p + 1);
      this.fetchUsers();
    }
  }

  public prevPage(): void {
    if (this.pageNumber() > 1 && !this.isLoading()) {
      this.pageNumber.update(p => p - 1);
      this.fetchUsers();
    }
  }

  // Dialog Handling
  public openEditModal(user: UserModel, dialog: HTMLDialogElement): void {
    this.selectedUser.set(user);
    this.editName = user.name;
    this.editEmail = user.email;
    this.editPhoneNumber = user.phoneNumber || '';
    this.editAddress = user.address || '';
    dialog.showModal();
  }

  public onEditSubmit(dialog: HTMLDialogElement): void {
    const user = this.selectedUser();
    if (!user) return;

    if (!this.editName.trim()) {
      this.toastService.error('Name is required.');
      return;
    }

    if (!this.editEmail.trim()) {
      this.toastService.error('Email is required.');
      return;
    }

    const payload = {
      name: this.editName.trim(),
      email: this.editEmail.trim(),
      phoneNumber: this.editPhoneNumber.trim() || undefined,
      address: this.editAddress.trim() || undefined
    };

    this.userService.updateUser(user.id, payload).subscribe({
      next: () => {
        this.zone.run(() => {
          this.toastService.success('User updated successfully.');
          dialog.close();
          this.fetchUsers();
        });
      },
      error: (err) => {
        this.zone.run(() => {
          this.toastService.error('Failed to update user.');
        });
        console.error('Failed to update user:', err);
      }
    });
  }

  public triggerDelete(user: UserModel, dialog: HTMLDialogElement): void {
    this.userToDelete.set(user);
    dialog.showModal();
  }

  public confirmDelete(dialog: HTMLDialogElement): void {
    const user = this.userToDelete();
    if (!user) return;

    this.userService.deleteUser(user.id).subscribe({
      next: () => {
        this.zone.run(() => {
          this.toastService.success('User deleted successfully.');
          dialog.close();
          this.fetchUsers();
          this.userService.updatePendingCount();
        });
      },
      error: (err) => {
        this.zone.run(() => {
          this.toastService.error('Failed to delete user.');
        });
        console.error('Failed to delete user:', err);
      }
    });
  }

  public setActiveTab(tab: 'approved' | 'pending'): void {
    this.activeTab.set(tab);
    this.pageNumber.set(1);
    this.fetchUsers();
  }

  public approveUser(user: UserModel): void {
    this.userService.approveUser(user.id).subscribe({
      next: () => {
        this.zone.run(() => {
          this.toastService.success(`${user.name} approved successfully.`);
          this.fetchUsers();
          this.userService.updatePendingCount();
        });
      },
      error: (err) => {
        this.zone.run(() => {
          this.toastService.error('Failed to approve user.');
        });
        console.error('Failed to approve user:', err);
      }
    });
  }

  public triggerReject(user: UserModel, dialog: HTMLDialogElement): void {
    this.userToReject.set(user);
    dialog.showModal();
  }

  public confirmReject(dialog: HTMLDialogElement): void {
    const user = this.userToReject();
    if (!user) return;

    this.userService.deleteUser(user.id).subscribe({
      next: () => {
        this.zone.run(() => {
          this.toastService.success(`${user.name} rejected successfully.`);
          dialog.close();
          this.fetchUsers();
          this.userService.updatePendingCount();
        });
      },
      error: (err) => {
        this.zone.run(() => {
          this.toastService.error('Failed to reject user.');
        });
        console.error('Failed to reject user:', err);
      }
    });
  }

  public getRoleIdFromRoleName(roleName: string): number {
    const role = this.roles().find(r => r.name.toLowerCase() === roleName.toLowerCase());
    return role ? role.id : 0;
  }

  public onRoleSelect(user: UserModel, event: Event, dialog: HTMLDialogElement): void {
    const selectElement = event.target as HTMLSelectElement;
    const newRoleId = Number(selectElement.value);

    const currentRoleId = this.getRoleIdFromRoleName(user.roleName);
    if (newRoleId === currentRoleId) return;

    const targetRole = this.roles().find(r => r.id === newRoleId);
    if (!targetRole) return;

    this.roleChangeUser.set(user);
    this.roleChangeTargetRoleId.set(newRoleId);
    this.roleChangeTargetRoleName.set(targetRole.name);

    dialog.showModal();
  }

  public cancelRoleChange(dialog: HTMLDialogElement): void {
    dialog.close();
    this.fetchUsers();
  }

  public confirmRoleChange(dialog: HTMLDialogElement): void {
    const user = this.roleChangeUser();
    const roleId = this.roleChangeTargetRoleId();
    if (!user || !roleId) return;

    this.userService.updateUserRole(user.id, roleId).subscribe({
      next: () => {
        this.zone.run(() => {
          this.toastService.success(`Role for ${user.name} updated to ${this.roleChangeTargetRoleName()}.`);
          dialog.close();
          this.fetchUsers();
        });
      },
      error: (err) => {
        this.zone.run(() => {
          const errMsg = err.error?.detail || err.error?.Detail || 'Failed to update user role.';
          this.toastService.error(errMsg);
          dialog.close();
          this.fetchUsers();
        });
        console.error('Failed to update role:', err);
      }
    });
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

  public getRoleBadgeClass(roleName: string): string {
    const role = roleName.toLowerCase();
    switch (role) {
      case 'admin':
        return 'bg-amber-100 text-amber-800 border border-amber-200';
      case 'chef':
        return 'bg-red-100 text-red-800 border border-red-200';
      case 'waiter':
        return 'bg-blue-100 text-blue-800 border border-blue-200';
      case 'customer':
        return 'bg-emerald-100 text-emerald-800 border border-emerald-200';
      case 'deliveryman':
        return 'bg-purple-100 text-purple-800 border border-purple-200';
      default:
        return 'bg-slate-100 text-slate-800 border border-slate-200';
    }
  }
}

