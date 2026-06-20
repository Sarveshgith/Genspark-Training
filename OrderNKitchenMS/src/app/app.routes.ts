import { Routes } from '@angular/router';
import { Login } from './features/auth/login/login';
import { Register } from './features/auth/register/register';
import { KdsBoard } from './features/chef/kds-board/kds-board';

export const routes: Routes = [
	{ path: '', pathMatch: 'full', redirectTo: 'login' },
	{ path: 'login', component: Login },
	{ path: 'register', component: Register },
    { path: 'chef/orders', component: KdsBoard },
	{ path: '**', redirectTo: 'login' },
];
