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
}

@Injectable({ providedIn: "root" })
export class UserService {
  private classUrl = baseUrl + "users";

  constructor(private http: HttpClient) {}

  public getUsers(query?: { search?: string; roleId?: number }): Observable<UserModel[]> {
    let params = new HttpParams();
    if (query) {
      if (query.search) {
        params = params.set('search', query.search);
      }
      if (query.roleId !== undefined && query.roleId !== null) {
        params = params.set('roleId', query.roleId.toString());
      }
    }
    return this.http.get<UserModel[]>(this.classUrl, { params });
  }
}
