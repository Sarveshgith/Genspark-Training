import { Routes } from '@angular/router';
import { Login } from './features/auth/login/login';
import { Register } from './features/auth/register/register';
import { KdsBoard } from './features/chef/kds-board/kds-board';
import { RoleGuard } from './core/guards/role.guard';
import { LandingComponent } from './features/guest/landing-component/landing-component';
import { MenuItems } from './features/waiter/menu-items/menu-items';

export const routes: Routes = [
	{ path: '', pathMatch: 'full', redirectTo: 'login' },

	{ path: 'login', component: Login },

	{ path: 'register', component: Register },

	{
		path: 'chef',
		component: KdsBoard,
		canActivate: [RoleGuard],
		data: { roles: ['Chef'] },
		children: [
			{ path: '', redirectTo: 'kds', pathMatch: 'full' },
			{ path: 'kds-board', component: KdsBoard }
		]
	},

	{
		path: 'waiter/menu', component: MenuItems
	},

	{ path: 'guest/landing', component: LandingComponent },

	{ path: '**', redirectTo: 'login' },
];

