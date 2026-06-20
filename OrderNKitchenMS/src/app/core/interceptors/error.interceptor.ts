import { HttpErrorResponse, HttpInterceptorFn } from "@angular/common/http";
import { Router } from "@angular/router";
import { AuthService } from "../services/auth.service";
import { inject } from "@angular/core";
import { catchError, switchMap, throwError } from "rxjs";
import { IS_PUBLIC_API } from "./public-api.token";

export const errorInterceptor: HttpInterceptorFn = (req, next) => {

    const authService = inject(AuthService);
    const router = inject(Router);

    return next(req).pipe(
        catchError((err: HttpErrorResponse) => {

            if (err.status === 401) {
                if (req.context.get(IS_PUBLIC_API)) {
                    authService.logout();
                    router.navigate(['/login']);
                    return throwError(() => err);
                }
                else {
                    return authService.refreshToken().pipe(
                        switchMap((res: any) => {
                            const newRequest = req.clone({
                                setHeaders: {
                                    Authorization: `Bearer ${res.token}`
                                }
                            });
                            return next(newRequest);
                        }),
                        catchError((refreshErr) => {
                            authService.logout();
                            router.navigate(['/login']);
                            return throwError(() => refreshErr);
                        })
                    );
                }
            }

            if (err.status === 403) {
                router.navigate(['/forbidden']);
                return throwError(() => err);
            }

            if (err.status === 404) {
                router.navigate(['/not-found']);
                return throwError(() => err);
            }

            return throwError(() => err);
        })
    );
};