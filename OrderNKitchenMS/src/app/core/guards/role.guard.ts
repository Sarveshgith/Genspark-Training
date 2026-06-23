import { CanActivateFn, Router } from "@angular/router";
import { AuthService } from "../services/auth.service";
import { inject } from "@angular/core";

export const RoleGuard: CanActivateFn = (route, state) => {
    const authService = inject(AuthService);
    const router = inject(Router);

    const expectedRoles = route.data['roles'] as string[];
    const userRole = authService.getRole();

    if (userRole && expectedRoles.includes(userRole)) {
        return true;
    } else {
        router.navigate(['/forbidden']);
        return false;
    }
}