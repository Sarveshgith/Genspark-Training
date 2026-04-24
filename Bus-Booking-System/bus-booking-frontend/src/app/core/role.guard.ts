import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from './auth.service';

export function roleGuard(allowedRoles: string[]): CanActivateFn {
  return () => {
    const authService = inject(AuthService);
    const router = inject(Router);

    if (!authService.isAuthenticated()) {
      return router.createUrlTree(['/login']);
    }

    const role = authService.getRole();
    if (allowedRoles.includes(role)) {
      return true;
    }

    if (role === 'Admin') {
      return router.createUrlTree(['/admin/dashboard']);
    }

    if (role === 'Operator') {
      return router.createUrlTree(['/operator/dashboard']);
    }

    return router.createUrlTree(['/trips']);
  };
}
