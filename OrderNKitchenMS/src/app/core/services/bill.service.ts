import { HttpClient } from "@angular/common/http";
import { Injectable, signal } from "@angular/core";
import { baseUrl } from "../enviroment";
import { Observable } from "rxjs";
import { BillCreateDto, BillDto } from "../models/bill.model";
import { SignalRService } from "./signalr.service";

@Injectable({ providedIn: "root" })
export class BillService {
  private classUrl = baseUrl + "bills";

  private _bill = signal<BillDto | null>(null);
  public get bill() {
    return this._bill.asReadonly();
  }

  public get billGenerated$(): Observable<any> {
    return this.signalR.billGenerated$;
  }

  public get billPaid$(): Observable<any> {
    return this.signalR.billPaid$;
  }

  public get billSplitPaid$(): Observable<{ splitId: number, amount: number }> {
    return this.signalR.billSplitPaid$;
  }

  constructor(private http: HttpClient, private signalR: SignalRService) {
    this.signalR.billGenerated$.subscribe((bill: BillDto) => {
      this._bill.set(bill);
    });
    this.signalR.billPaid$.subscribe((bill: BillDto) => {
      if (this._bill() && this._bill()!.id === bill.id) {
        this._bill.set(bill);
      }
    });
    this.signalR.billSplitPaid$.subscribe((data: { splitId: number, amount: number }) => {
      const currentBill = this._bill();
      if (currentBill && currentBill.splits) {
        const splitIndex = currentBill.splits.findIndex(s => s.id === data.splitId);
        if (splitIndex !== -1) {
          const updatedSplits = [...currentBill.splits];
          updatedSplits[splitIndex] = { ...updatedSplits[splitIndex], statusName: "Paid" };
          this._bill.set({ ...currentBill, splits: updatedSplits });
        }
      }
    });
  }

  public createBill(billCreateDto: BillCreateDto): Observable<BillDto> {
    return this.http.post<BillDto>(this.classUrl, billCreateDto);
  }

  public getBillByOrderId(orderId: number): Observable<BillDto> {
    return this.http.get<BillDto>(`${this.classUrl}/order/${orderId}`);
  }

  public getAllBills(): Observable<BillDto[]> {
    return this.http.get<BillDto[]>(this.classUrl);
  }

  public updateBillStatus(billId: number, status: string): Observable<void> {
    return this.http.patch<void>(`${this.classUrl}/${billId}/status`, JSON.stringify(status), {
      headers: { 'Content-Type': 'application/json' }
    });
  }

  public getBillPdfUrl(orderId: number): string {
    return `${this.classUrl}/order/${orderId}/pdf`;
  }

  public payBillSplit(splitId: number): Observable<void> {
    return this.http.patch<void>(`${this.classUrl}/splits/${splitId}/pay`, {});
  }
}
