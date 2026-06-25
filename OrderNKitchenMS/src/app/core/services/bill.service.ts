import { HttpClient } from "@angular/common/http";
import { Injectable } from "@angular/core";
import { baseUrl } from "../enviroment";
import { Observable } from "rxjs";
import { BillCreateDto, BillDto } from "../models/bill.model";

@Injectable({ providedIn: "root" })
export class BillService {
  private classUrl = baseUrl + "bills";

  constructor(private http: HttpClient) { }

  public createBill(billCreateDto: BillCreateDto): Observable<BillDto> {
    return this.http.post<BillDto>(this.classUrl, billCreateDto);
  }

  public getBillByOrderId(orderId: number): Observable<BillDto> {
    return this.http.get<BillDto>(`${this.classUrl}/order/${orderId}`);
  }

  public updateBillStatus(billId: number, status: string): Observable<void> {
    return this.http.patch<void>(`${this.classUrl}/${billId}/status`, JSON.stringify(status), {
      headers: { 'Content-Type': 'application/json' }
    });
  }

  public getBillPdfUrl(orderId: number): string {
    return `${this.classUrl}/order/${orderId}/pdf`;
  }
}
