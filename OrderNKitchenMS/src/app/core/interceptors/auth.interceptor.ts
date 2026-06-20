import { HttpInterceptorFn } from "@angular/common/http";
import { AuthService } from "../services/auth.service";
import { inject } from "@angular/core";
import { IS_PUBLIC_API } from "./public-api.token";

export const authInterceptor: HttpInterceptorFn = (req, next) => {

    const authService = inject(AuthService);
    const token = authService.getToken();

    if (req.context.get(IS_PUBLIC_API) || !token) {
        return next(req);
    }

    const authReq = req.clone({
        setHeaders: {
            Authorization: `Bearer ${token}`
        }
    });

    return next(authReq);
}