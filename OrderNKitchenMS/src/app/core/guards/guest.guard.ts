import { CanActivateFn, Router } from "@angular/router";
import { AuthService } from "../services/auth.service";
import { inject } from "@angular/core";

export const GuestGuard: CanActivateFn = (route, state) => {
    const authService = inject(AuthService);
    const router = inject(Router);

    const token = authService.getToken();

    if (!token) {
        return true;
    } else {
        router.navigate(['/forbidden']);
        return false;
    }
}