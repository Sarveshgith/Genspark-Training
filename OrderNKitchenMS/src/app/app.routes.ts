import { Routes } from '@angular/router';
import { RoleGuard } from './core/guards/role.guard';
import { GuestGuard } from './core/guards/guest.guard';

export const routes: Routes = [
	{ path: '', pathMatch: 'full', redirectTo: 'login' },

	{ 
		path: 'login', 
		loadComponent: () => import('./features/auth/login/login').then(m => m.Login) 
	},

	{ 
		path: 'register', 
		loadComponent: () => import('./features/auth/register/register').then(m => m.Register) 
	},
	
	{
		path: 'kitchen',
		loadComponent: () => import('./features/chef/kds-board/kds-board').then(m => m.KdsBoard),
		canActivate: [RoleGuard],
		data: { roles: ['Chef'] }
	},

	{
		path: 'waiter/menu', 
		loadComponent: () => import('./features/waiter/menu-items/menu-items').then(m => m.MenuItems)
	},

	{
		path: 'waiter/tables',
		loadComponent: () => import('./features/waiter/table-view/table-view').then(m => m.TableView),
		canActivate: [RoleGuard],
		data: { roles: ['Waiter'] }
	},

	{
		path: 'dashboard',
		loadComponent: () => import('./features/admin/dashboard/dashboard').then(m => m.Dashboard),
		canActivate: [RoleGuard],
		data: { roles: ['Admin'] }
	},

	{
		path: 'tables',
		loadComponent: () => import('./features/admin/table-manager/table-manager').then(m => m.TableManager),
		canActivate: [RoleGuard],
		data: { roles: ['Admin'] }
	},

	{
		path: 'inventory',
		loadComponent: () => import('./features/admin/inventory-manager/inventory-manager').then(m => m.InventoryManager),
		canActivate: [RoleGuard],
		data: { roles: ['Admin'] }
	},

	{
		path: 'menu-manager',
		loadComponent: () => import('./features/admin/menu-manager/menu-manager').then(m => m.MenuManager),
		canActivate: [RoleGuard],
		data: { roles: ['Admin'] }
	},

	{
		path: 'category-manager',
		loadComponent: () => import('./features/admin/category-manager/category-manager').then(m => m.CategoryManager),
		canActivate: [RoleGuard],
		data: { roles: ['Admin'] }
	},

	{
		path: 'waiter/active-order/:tableId',
		loadComponent: () => import('./features/waiter/active-order/active-order').then(m => m.ActiveOrderComponent),
		canActivate: [RoleGuard],
		data: { roles: ['Waiter'] }
	},

	{ 
		path: 'guest/landing', 
		loadComponent: () => import('./features/guest/landing-component/landing-component').then(m => m.LandingComponent) 
	},

	{
		path: 'guest/menu',
		loadComponent: () => import('./features/waiter/menu-items/menu-items').then(m => m.MenuItems),
		canActivate: [GuestGuard]
	},

	{ path: '**', redirectTo: 'login' },
];

