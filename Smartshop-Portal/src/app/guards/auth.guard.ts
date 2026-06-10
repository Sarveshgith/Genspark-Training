import { inject } from "@angular/core";
import { CanActivateFn, Router } from "@angular/router";

export const authGuard: CanActivateFn = () => {
    
    const router= inject(Router);

    const isAuthenticated = (): boolean => {
        if(typeof window !== 'undefined' && window.sessionStorage) {
        const user = sessionStorage.getItem('user');
        return !!user;
        }
        return false;
    } 
    
    if(isAuthenticated()){
        return true;
    }

    return router.createUrlTree(['/login']);
}