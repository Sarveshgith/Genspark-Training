import { Routes } from '@angular/router';
import { RoleGuard } from './core/guards/role.guard';
import { GuestGuard } from './core/guards/guest.guard';
import { SessionEndedGuard } from './core/guards/session-ended.guard';

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
		path: 'pending-approval',
		loadComponent: () => import('./features/auth/pending-approval/pending-approval').then(m => m.PendingApproval)
	},

	{
		path: 'kitchen',
		loadComponent: () => import('./features/chef/kds-board/kds-board').then(m => m.KdsBoard),
		canActivate: [RoleGuard],
		data: { roles: ['Chef'] }
	},

	{
		path: 'waiter/menu',
		loadComponent: () => import('./features/waiter/menu-items/menu-items').then(m => m.MenuItems),
		canActivate: [RoleGuard],
		data: { roles: ['Waiter'] }
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
		path: 'orders',
		loadComponent: () => import('./features/admin/order-list/order-list').then(m => m.OrderList),
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
		path: 'users',
		loadComponent: () => import('./features/admin/user-manager/user-manager').then(m => m.UserManager),
		canActivate: [RoleGuard],
		data: { roles: ['Admin'] }
	},

	{
		path: 'reports',
		loadComponent: () => import('./features/admin/reports/reports').then(m => m.ReportsComponent),
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
		loadComponent: () => import('./features/guest/landing-component/landing-component').then(m => m.LandingComponent),
		canActivate: [GuestGuard]
	},

	{
		path: 'guest/menu',
		loadComponent: () => import('./features/waiter/menu-items/menu-items').then(m => m.MenuItems),
		canActivate: [GuestGuard]
	},

	{
		path: 'guest/:secret',
		loadComponent: () => import('./features/guest/landing-component/landing-component').then(m => m.LandingComponent)
	},

	{
		path: 'session-ended',
		loadComponent: () => import('./features/guest/session-ended/session-ended').then(m => m.SessionEndedComponent),
		canActivate: [SessionEndedGuard]
	},

	{ path: '**', redirectTo: 'login' },
];