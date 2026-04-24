import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from './auth.service';

export const guestGuard: CanActivateFn = () => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (!authService.isAuthenticated()) {
    return true;
  }

  const role = authService.getRole();
  if (role === 'Admin') {
    return router.createUrlTree(['/admin/dashboard']);
  }

  if (role === 'Operator') {
    return router.createUrlTree(['/operator/dashboard']);
  }

  return router.createUrlTree(['/trips']);
};
