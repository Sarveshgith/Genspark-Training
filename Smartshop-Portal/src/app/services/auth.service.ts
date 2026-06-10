import { HttpClient } from "@angular/common/http";
import { LoginModel } from "../models/login.model";
import { baseUrl } from "../../environment";
import { Injectable } from "@angular/core";
import { UserModel } from "../models/user.model";
import { BehaviorSubject, catchError, tap, throwError } from "rxjs";
import { Router } from "@angular/router";

@Injectable({ providedIn: 'root' })
export class AuthService {
    constructor(private http: HttpClient, private router: Router) { }

    private getInitialUser(): UserModel | null {
        if (typeof window !== 'undefined' && window.sessionStorage) {
            const stored = sessionStorage.getItem('user');
            return stored ? JSON.parse(stored) : null;
        }
        return null;
    }

    private userSubject = new BehaviorSubject<UserModel | null>(this.getInitialUser());

    currentUser$ = this.userSubject.asObservable();

    public login(loginModel: LoginModel) {
        let url = `${baseUrl}/auth/login`;
        return this.http.post<UserModel>(url, loginModel)
            .pipe(tap((user: UserModel) => { 
                if (typeof window !== 'undefined' && window.sessionStorage) {
                    sessionStorage.setItem('user', JSON.stringify(user));
                }
                this.userSubject.next(user); 
            }),
                catchError(error => {
                    console.error('Login failed:', error);
                    return throwError(() => new Error('Login failed. Please check your credentials and try again.'));
                })
            );
    }

    public logout() {
        if (typeof window !== 'undefined' && window.sessionStorage) {
            sessionStorage.removeItem('user');
        }
        this.userSubject.next(null);
        this.router.navigate(['/login']);
    }
}