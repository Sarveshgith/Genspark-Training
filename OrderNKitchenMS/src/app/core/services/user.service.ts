import { HttpClient, HttpParams } from "@angular/common/http";
import { Injectable } from "@angular/core";
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
}

export interface RoleModel {
  id: number;
  roleValue: number;
  name: string;
}

@Injectable({ providedIn: "root" })
export class UserService {
  private classUrl = baseUrl + "users";

  constructor(private http: HttpClient) {}

  public getUsers(query?: { search?: string; roleId?: number; pageNumber?: number; pageSize?: number }): Observable<UserModel[]> {
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
    }
    return this.http.get<UserModel[]>(this.classUrl, { params });
  }

  public getRoles(): Observable<RoleModel[]> {
    return this.http.get<RoleModel[]>(`${this.classUrl}/roles`);
  }

  public updateUser(id: number, user: { email: string; name: string; roleId: number; phoneNumber?: string; address?: string }): Observable<UserModel> {
    return this.http.put<UserModel>(`${this.classUrl}/${id}`, user);
  }

  public deleteUser(id: number): Observable<void> {
    return this.http.delete<void>(`${this.classUrl}/${id}`);
  }
}
