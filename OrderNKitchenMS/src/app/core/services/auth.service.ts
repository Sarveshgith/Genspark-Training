import { Injectable } from "@angular/core";
import { LoginModel, LoginResponseModel, UserModel, RegisterModel } from "../models/auth.model";
import { baseUrl } from "../enviroment";
import { HttpClient, HttpContext, HttpParams } from "@angular/common/http";
import { BehaviorSubject, catchError, Observable, tap, throwError } from "rxjs";
import { IS_PUBLIC_API } from "../interceptors/public-api.token";

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
                    return throwError(() => new Error('Login failed. Please check your credentials and try again.'));
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

    public refreshToken(): Observable<LoginResponseModel> {
        const refreshToken = this.getRefreshToken();
        if (!refreshToken) {
            return throwError(() => new Error("No refresh token available"));
        }
        let url = classUrl + "refresh";
        return this.http.post<LoginResponseModel>(url, { refreshToken }, {
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
                    console.error('Refresh token failed:', error);
                    return throwError(() => error);
                })
            );
    }

    public guestLogin(query: any): any {
        let url = classUrl + "guest-login";
        let params = new HttpParams().set('tableId', query.tableId.toString());
        return this.http.post(url, null, {
            params,
            context: new HttpContext().set(IS_PUBLIC_API, true)
        }).pipe(tap((response: any) => {
            if (typeof window !== 'undefined' && window.sessionStorage) {
                sessionStorage.setItem('token', response.token);
            }
            }),
            catchError(error => {
                console.error('Guest login failed:', error);
                return throwError(() => new Error('Guest login failed. Please try again.'));
            })
        );
    }

    public logout() {
        if (typeof window !== 'undefined' && window.sessionStorage) {
            sessionStorage.removeItem('user');
            sessionStorage.removeItem('token');
            sessionStorage.removeItem('refreshToken');
        }
        this.userSubject.next(null);
    }
}