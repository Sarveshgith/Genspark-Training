import { HttpClient, HttpParams } from "@angular/common/http";
import { Injectable } from "@angular/core";
import { baseUrl } from "../enviroment";
import { Observable } from "rxjs";
import { CategoryPerformanceModel, DailyRevenueModel, KitchenSlaModel, RangeRevenueModel, TableTurnoverModel, TopSellingItemModel } from "../models/reports.model";

@Injectable({ providedIn: "root" })
export class ReportService {
  private classUrl = baseUrl + "reports";

  constructor(private http: HttpClient) { }

  public getDailyRevenue(date?: string): Observable<DailyRevenueModel> {
    let params = new HttpParams();
    if (date) {
      params = params.set('date', date);
    }
    return this.http.get<DailyRevenueModel>(`${this.classUrl}/revenue/daily`, { params });
  }

  public getRangeRevenue(from: string, to: string): Observable<RangeRevenueModel[]> {
    let params = new HttpParams()
      .set('from', from)
      .set('to', to);
    return this.http.get<RangeRevenueModel[]>(`${this.classUrl}/revenue/range`, { params });
  }

  public getTopSellingItems(limit: number = 5): Observable<TopSellingItemModel[]> {
    let params = new HttpParams().set('limit', limit.toString());
    return this.http.get<TopSellingItemModel[]>(`${this.classUrl}/menu/top-items`, { params });
  }

  public getCategoryPerformance(): Observable<CategoryPerformanceModel[]> {
    return this.http.get<CategoryPerformanceModel[]>(`${this.classUrl}/menu/category-performance`);
  }

  public getKitchenSla(): Observable<KitchenSlaModel> {
    return this.http.get<KitchenSlaModel>(`${this.classUrl}/kitchen/sla`);
  }

  public getTableTurnover(date?: string): Observable<TableTurnoverModel[]> {
    let params = new HttpParams();
    if (date) {
      params = params.set('date', date);
    }
    return this.http.get<TableTurnoverModel[]>(`${this.classUrl}/tables/turnover`, { params });
  }
}
