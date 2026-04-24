import { Routes } from '@angular/router';
import { authGuard } from './core/auth.guard';
import { guestGuard } from './core/guest.guard';
import { roleGuard } from './core/role.guard';
import { LoginPageComponent } from './pages/auth/login.page';
import { RegisterPageComponent } from './pages/auth/register.page';
import { AdminDashboardPageComponent } from './pages/admin/dashboard.page';
import { AdminOperatorsPageComponent } from './pages/admin/operators.page';
import { AdminBusesPageComponent } from './pages/admin/buses.page';
import { RegisterAdminSecretPageComponent } from './pages/admin/register-admin-secret.page';
import { AdminLocationsPageComponent } from './pages/admin/locations.page';
import { OperatorDashboardPageComponent } from './pages/operator/dashboard.page';
import { OperatorLayoutsPageComponent } from './pages/operator/layouts.page';
import { OperatorBusesPageComponent } from './pages/operator/buses.page';
import { OperatorTripsPageComponent } from './pages/operator/trips.page';
import { OperatorLocationsPageComponent } from './pages/operator/locations.page';
import { TripsPageComponent } from './pages/user/trips.page';
import { TripSeatsPageComponent } from './pages/user/trip-seats.page';
import { BookingPageComponent } from './pages/user/booking.page';
import { PaymentPageComponent } from './pages/user/payment.page';
import { MyBookingsPageComponent } from './pages/user/my-bookings.page';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'login' },

  {
    path: 'login',
    component: LoginPageComponent,
    canActivate: [guestGuard],
  },
  {
    path: 'register',
    component: RegisterPageComponent,
    canActivate: [guestGuard],
  },

  {
    path: 'admin/dashboard',
    component: AdminDashboardPageComponent,
    canActivate: [authGuard, roleGuard(['Admin'])],
  },
  {
    path: 'admin/operators',
    component: AdminOperatorsPageComponent,
    canActivate: [authGuard, roleGuard(['Admin'])],
  },
  {
    path: 'admin/buses',
    component: AdminBusesPageComponent,
    canActivate: [authGuard, roleGuard(['Admin'])],
  },
  {
    path: 'admin/locations',
    component: AdminLocationsPageComponent,
    canActivate: [authGuard, roleGuard(['Admin'])],
  },
  {
    path: 'admin/secure/internal-admin-registration-x9p2k4n7m1q8v6t3z0',
    component: RegisterAdminSecretPageComponent,
    canActivate: [authGuard, roleGuard(['Admin'])],
  },

  {
    path: 'operator/dashboard',
    component: OperatorDashboardPageComponent,
    canActivate: [authGuard, roleGuard(['Operator'])],
  },
  {
    path: 'operator/layouts',
    component: OperatorLayoutsPageComponent,
    canActivate: [authGuard, roleGuard(['Operator'])],
  },
  {
    path: 'operator/buses',
    component: OperatorBusesPageComponent,
    canActivate: [authGuard, roleGuard(['Operator'])],
  },
  {
    path: 'operator/trips',
    component: OperatorTripsPageComponent,
    canActivate: [authGuard, roleGuard(['Operator'])],
  },
  {
    path: 'operator/locations',
    component: OperatorLocationsPageComponent,
    canActivate: [authGuard, roleGuard(['Operator'])],
  },

  {
    path: 'trips',
    component: TripsPageComponent,
    canActivate: [authGuard, roleGuard(['User'])],
  },
  {
    path: 'trips/:id/seats',
    component: TripSeatsPageComponent,
    canActivate: [authGuard, roleGuard(['User'])],
  },
  {
    path: 'booking/:ticketId',
    component: BookingPageComponent,
    canActivate: [authGuard, roleGuard(['User'])],
  },
  {
    path: 'booking/:ticketId/payment',
    component: PaymentPageComponent,
    canActivate: [authGuard, roleGuard(['User'])],
  },
  {
    path: 'my-bookings',
    component: MyBookingsPageComponent,
    canActivate: [authGuard, roleGuard(['User'])],
  },

  { path: '**', redirectTo: 'login' },
];
