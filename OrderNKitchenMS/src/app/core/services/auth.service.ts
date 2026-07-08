import { Injectable } from "@angular/core";
import { LoginModel, LoginResponseModel, UserModel, RegisterModel } from "../models/auth.model";
import { baseUrl } from "../enviroment";
import { HttpClient, HttpContext, HttpParams } from "@angular/common/http";
import { BehaviorSubject, catchError, Observable, tap, throwError, filter, take } from "rxjs";
import { IS_PUBLIC_API, SKIP_REFRESH } from "../interceptors/public-api.token";

let classUrl = baseUrl + "auth/";

@Injectable({ providedIn: "root" })
export class AuthService {

    constructor(private http: HttpClient) { }

    private getInitialUser(): UserModel | null {
        if (typeof window !== 'undefined' && window.sessionStorage) {
            const userJson = sessionStorage.getItem('user');
            return userJson ? JSON.parse(userJson) as UserModel : null;
        }
        return null;
    }

    private userSubject = new BehaviorSubject<UserModel | null>(this.getInitialUser());
    public user$ = this.userSubject.asObservable();

    private isRefreshing = false;
    private refreshTokenSubject = new BehaviorSubject<LoginResponseModel | null>(null);

    public getToken(): string | null {
        if (typeof window !== 'undefined' && window.sessionStorage) {
            return sessionStorage.getItem('token');
        }
        return null;
    }

    public getRefreshToken(): string | null {
        if (typeof window !== 'undefined' && window.sessionStorage) {
            return sessionStorage.getItem('refreshToken');
        }
        return null;
    }

    public getRole(): string | null {
        const user = this.userSubject.value;
        return user ? user.roleName : null;
    }

    public getCurrentUserId(): number | null {
        const user = this.userSubject.value;
        return user ? user.id : null;
    }

    public login(loginModel: LoginModel) {
        let url = classUrl + "login";
        return this.http.post<LoginResponseModel>(url, loginModel, {
            context: new HttpContext().set(IS_PUBLIC_API, true)
        })
            .pipe(
                tap((response: LoginResponseModel) => {
                    if (typeof window !== 'undefined' && window.sessionStorage) {
                        sessionStorage.setItem('user', JSON.stringify(response.user));
                        sessionStorage.setItem('token', response.token);
                        sessionStorage.setItem('refreshToken', response.refreshToken);
                    }
                    this.userSubject.next(response.user);
                }),
                catchError(error => {
                    console.error('Login failed:', error);
                    let msg = error.error?.detail || error.error?.Detail || 'Login failed. Please check your credentials and try again.';
                    if (msg.includes('<html>') || msg.includes('System.Data') || msg.includes('SqliteException') || msg.includes('Microsoft.AspNetCore') || msg.includes('stack trace') || msg.length > 200) {
                        msg = 'An unexpected server error occurred. Please try again later.';
                    }
                    return throwError(() => new Error(msg));
                })
            );
    }

    public register(registerModel: RegisterModel) {
        let url = classUrl + "register";
        return this.http.post<UserModel>(url, registerModel, {
            context: new HttpContext().set(IS_PUBLIC_API, true)
        })
            .pipe(
                catchError(error => {
                    console.error('Registration failed:', error);
                    return throwError(() => new Error('Registration failed. Please check your inputs and try again.'));
                })
            );
    }

    //Refresh Token Concurrency Lock Implementation
    public refreshToken(): Observable<LoginResponseModel> {
        if (this.isRefreshing) {
            return this.refreshTokenSubject.pipe(
                filter(res => res !== null),
                take(1)
            ) as Observable<LoginResponseModel>;
        }

        const refreshToken = this.getRefreshToken();
        if (!refreshToken) {
            return throwError(() => new Error("No refresh token available"));
        }

        this.isRefreshing = true;
        this.refreshTokenSubject.next(null);

        let url = classUrl + "refresh";
        return this.http.post<LoginResponseModel>(url, { refreshToken }, {
            context: new HttpContext()
                .set(IS_PUBLIC_API, true)
                .set(SKIP_REFRESH, true)
        })
            .pipe(
                tap((response: LoginResponseModel) => {
                    this.isRefreshing = false;
                    if (typeof window !== 'undefined' && window.sessionStorage) {
                        sessionStorage.setItem('user', JSON.stringify(response.user));
                        sessionStorage.setItem('token', response.token);
                        sessionStorage.setItem('refreshToken', response.refreshToken);
                    }
                    this.userSubject.next(response.user);

                    const successSubject = this.refreshTokenSubject;
                    this.refreshTokenSubject = new BehaviorSubject<LoginResponseModel | null>(null);
                    successSubject.next(response);
                    successSubject.complete();
                }),
                catchError(error => {
                    this.isRefreshing = false;
                    const errSubject = this.refreshTokenSubject;
                    this.refreshTokenSubject = new BehaviorSubject<LoginResponseModel | null>(null);
                    errSubject.error(error);
                    console.error('Refresh token failed:', error);
                    return throwError(() => error);
                })
            );
    }

    public guestLogin(query: { secret: string }): Observable<{ token: string }> {
        let url = classUrl + "guest-login";
        return this.http.post<{ token: string }>(url, { secret: query.secret }, {
            context: new HttpContext().set(IS_PUBLIC_API, true)
        }).pipe(tap((response: { token: string }) => {
            if (typeof window !== 'undefined' && window.sessionStorage) {
                sessionStorage.setItem('token', response.token);
                sessionStorage.setItem('tableSecret', query.secret);
            }
        }),
            catchError(error => {
                console.error('Guest login failed:', error);
                return throwError(() => new Error('Guest login failed. Please try again.'));
            })
        );
    }

    public getTableIdFromToken(token: string): number {
        try {
            const payload = JSON.parse(atob(token.split('.')[1]));
            return parseInt(payload.tableId, 10);
        } catch {
            return 0;
        }
    }

    public isTokenExpired(token: string): boolean {
        try {
            const payload = JSON.parse(atob(token.split('.')[1]));
            if (!payload.exp) return true;
            const expiry = payload.exp * 1000;
            return Date.now() >= expiry;
        } catch {
            return true;
        }
    }

    public logout() {
        if (typeof window !== 'undefined' && window.sessionStorage) {
            sessionStorage.removeItem('user');
            sessionStorage.removeItem('token');
            sessionStorage.removeItem('refreshToken');
            sessionStorage.removeItem('tableSecret');
        }
        this.userSubject.next(null);
    }
}