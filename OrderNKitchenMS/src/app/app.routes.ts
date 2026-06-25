import { Routes } from '@angular/router';
import { Login } from './features/auth/login/login';
import { Register } from './features/auth/register/register';
import { KdsBoard } from './features/chef/kds-board/kds-board';
import { RoleGuard } from './core/guards/role.guard';
import { LandingComponent } from './features/guest/landing-component/landing-component';
import { MenuItems } from './features/waiter/menu-items/menu-items';
import { TableView } from './features/waiter/table-view/table-view';
import { ActiveOrderComponent } from './features/waiter/active-order/active-order';

export const routes: Routes = [
	{ path: '', pathMatch: 'full', redirectTo: 'login' },

	{ path: 'login', component: Login },

	{ path: 'register', component: Register },
	
	{
		path: 'kitchen',
		component: KdsBoard,
		canActivate: [RoleGuard],
		data: { roles: ['Chef'] }
	},

	{
		path: 'waiter/menu', component: MenuItems
	},

	{
		path: 'waiter/tables',
		component: TableView,
		canActivate: [RoleGuard],
		data: { roles: ['Waiter'] }
	},

	{
		path: 'waiter/active-order/:tableId',
		component: ActiveOrderComponent,
		canActivate: [RoleGuard],
		data: { roles: ['Waiter'] }
	},

	{ path: 'guest/landing', component: LandingComponent },

	{ path: '**', redirectTo: 'login' },
];

