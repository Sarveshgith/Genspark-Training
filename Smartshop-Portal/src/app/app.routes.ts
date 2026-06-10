import { Routes } from '@angular/router';
import { authGuard } from './guards/auth.guard';

export const routes: Routes = [
    {
        path: 'login',
        loadComponent: () => import('./components/login/login').then(m => m.Login),
    },
    {
        path: '',
        loadComponent: () => import('./components/dashboard/dashboard').then(m => m.Dashboard),
        canActivate: [authGuard]
    },
    {
        path: 'products',
        loadComponent: () => import('./components/products/products').then(m => m.Products),
        canActivate: [authGuard]
    },
    {
        path: 'product/:id',
        loadComponent: () => import('./components/product-details/product-details').then(m => m.ProductDetails),
        canActivate: [authGuard]
    },
    {
        path: 'profile',
        loadComponent: () => import('./components/profile/profile').then(m => m.Profile),
        canActivate: [authGuard]
    },
    { path: '', redirectTo: 'login', pathMatch: 'full' }
];
