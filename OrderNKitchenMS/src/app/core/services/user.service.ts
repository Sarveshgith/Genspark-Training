import { HttpClient, HttpParams } from "@angular/common/http";
import { Injectable, signal } from "@angular/core";
import { baseUrl } from "../enviroment";
import { Observable } from "rxjs";

export interface UserModel {
  id: number;
  email: string;
  name: string;
  roleId: number;
  roleName: string;
  phoneNumber?: string;
  address?: string;
  isDeleted?: string;
  isPending?: boolean;
}

export interface RoleModel {
  id: number;
  roleValue: number;
  name: string;
}

@Injectable({ providedIn: "root" })
export class UserService {
  private classUrl = baseUrl + "users";

  public pendingUsersCount = signal<number>(0);

  constructor(private http: HttpClient) {}

  public updatePendingCount(): void {
    this.getUsers({ pageNumber: 1, pageSize: 1000, isPending: true }).subscribe({
      next: (users) => {
        this.pendingUsersCount.set(users.length);
      },
      error: (err) => {
        console.error('Failed to update pending users count:', err);
      }
    });
  }

  public getUsers(query?: { search?: string; roleId?: number; pageNumber?: number; pageSize?: number; isPending?: boolean }): Observable<UserModel[]> {
    let params = new HttpParams();
    if (query) {
      if (query.search) {
        params = params.set('search', query.search);
      }
      if (query.roleId !== undefined && query.roleId !== null) {
        params = params.set('roleId', query.roleId.toString());
      }
      if (query.pageNumber !== undefined && query.pageNumber !== null) {
        params = params.set('pageNumber', query.pageNumber.toString());
      }
      if (query.pageSize !== undefined && query.pageSize !== null) {
        params = params.set('pageSize', query.pageSize.toString());
      }
      if (query.isPending !== undefined && query.isPending !== null) {
        params = params.set('isPending', query.isPending.toString());
      }
    }
    return this.http.get<UserModel[]>(this.classUrl, { params });
  }

  public getRoles(): Observable<RoleModel[]> {
    return this.http.get<RoleModel[]>(`${this.classUrl}/roles`);
  }

  public updateUser(id: number, user: { email: string; name: string; phoneNumber?: string; address?: string }): Observable<UserModel> {
    return this.http.put<UserModel>(`${this.classUrl}/${id}`, user);
  }

  public approveUser(id: number): Observable<UserModel> {
    return this.http.patch<UserModel>(`${this.classUrl}/${id}/approve`, {});
  }

  public updateUserRole(id: number, roleId: number): Observable<UserModel> {
    return this.http.patch<UserModel>(`${this.classUrl}/${id}/role`, { roleId });
  }

  public deleteUser(id: number): Observable<void> {
    return this.http.delete<void>(`${this.classUrl}/${id}`);
  }
}
