import { CanActivateFn, Router } from "@angular/router";
import { inject } from "@angular/core";

export const SessionEndedGuard: CanActivateFn = (route, state) => {
    const router = inject(Router);
    const navigation = router.getCurrentNavigation();
    
    // Check if the route was navigated to via the redirect with state
    const hasFlag = navigation?.extras?.state?.['endedBySession'] === true;
    
    // Allow activation in either case, but the component will render differently
    // based on the presence of the flag.
    return true;
};
