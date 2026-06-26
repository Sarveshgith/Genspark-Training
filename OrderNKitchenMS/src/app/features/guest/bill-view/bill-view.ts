import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { BillService } from '../../../core/services/bill.service';
import { BillDto } from '../../../core/models/bill.model';

@Component({
  selector: 'app-bill-view',
  imports: [CommonModule, FormsModule],
  templateUrl: './bill-view.html',
  styleUrl: './bill-view.css',
})
export class BillView implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private billService = inject(BillService);
  private http = inject(HttpClient);

  public orderId!: number;
  public tableId: number | null = null;
  public bill: BillDto | null = null;
  public isLoading: boolean = true;
  public isProcessingPayment: boolean = false;
  public isPaymentSuccess: boolean = false;
  public errorMessage: string | null = null;

  // Form Fields
  public cardName: string = '';
  public cardNumber: string = '';
  public cardExpiry: string = '';
  public cardCvv: string = '';

  ngOnInit() {
    this.route.queryParams.subscribe(qp => {
      const tid = qp['tableId'];
      if (tid) {
        this.tableId = parseInt(tid, 10);
      }
    });

    this.route.paramMap.subscribe(params => {
      const orderIdStr = params.get('orderId');
      if (orderIdStr) {
        this.orderId = parseInt(orderIdStr, 10);
        this.fetchBill();
      } else {
        this.errorMessage = "Invalid Order ID.";
        this.isLoading = false;
      }
    });
  }

  public fetchBill() {
    this.isLoading = true;
    this.billService.getBillByOrderId(this.orderId).subscribe({
      next: (bill) => {
        this.bill = bill;
        this.isLoading = false;
        if (bill.statusName === 'Paid') {
          this.isPaymentSuccess = true;
        }
      },
      error: (err) => {
        console.error('Failed to load bill:', err);
        this.errorMessage = "Could not retrieve bill. Please contact your server or try again.";
        this.isLoading = false;
      }
    });
  }

  // Format Card Number (adds spaces)
  public onCardNumberInput(event: any) {
    let value = event.target.value.replace(/\D/g, '');
    if (value.length > 16) {
      value = value.substring(0, 16);
    }
    const matches = value.match(/\d{4,16}/g);
    const match = (matches && matches[0]) || '';
    const parts = [];

    for (let i = 0, len = match.length; i < len; i += 4) {
      parts.push(match.substring(i, i + 4));
    }

    if (parts.length > 0) {
      this.cardNumber = parts.join(' ');
    } else {
      this.cardNumber = value;
    }
  }

  // Format Expiry Date (MM/YY)
  public onExpiryInput(event: any) {
    let value = event.target.value.replace(/\D/g, '');
    if (value.length > 4) {
      value = value.substring(0, 4);
    }
    if (value.length > 2) {
      this.cardExpiry = value.substring(0, 2) + '/' + value.substring(2);
    } else {
      this.cardExpiry = value;
    }
  }

  // Format CVV
  public onCvvInput(event: any) {
    let value = event.target.value.replace(/\D/g, '');
    if (value.length > 3) {
      value = value.substring(0, 3);
    }
    this.cardCvv = value;
  }

  public get isFormValid(): boolean {
    const rawCard = this.cardNumber.replace(/\s+/g, '');
    const rawExpiry = this.cardExpiry.replace('/', '');
    return (
      this.cardName.trim().length >= 3 &&
      rawCard.length === 16 &&
      rawExpiry.length === 4 &&
      this.cardCvv.length === 3
    );
  }

  public processPayment() {
    if (!this.bill || !this.isFormValid) return;

    this.isProcessingPayment = true;
    this.errorMessage = null;

    // Simulate bank processing
    setTimeout(() => {
      this.billService.updateBillStatus(this.bill!.id, 'Paid').subscribe({
        next: () => {
          this.isProcessingPayment = false;
          this.isPaymentSuccess = true;
          if (this.bill) {
            this.bill.statusName = 'Paid';
          }
        },
        error: (err) => {
          console.error('Payment failed:', err);
          this.errorMessage = "Payment failed. Please verify card details and try again.";
          this.isProcessingPayment = false;
        }
      });
    }, 1500);
  }

  public downloadReceipt() {
    const url = this.billService.getBillPdfUrl(this.orderId);
    this.http.get(url, { responseType: 'blob' }).subscribe({
      next: (blob) => {
        const link = document.createElement('a');
        link.href = URL.createObjectURL(blob);
        link.download = `receipt_order_${this.orderId}.pdf`;
        link.click();
        URL.revokeObjectURL(link.href);
      },
      error: (err) => {
        console.error('Failed to download PDF:', err);
        window.open(url, '_blank');
      }
    });
  }

  public goBack() {
    if (this.tableId) {
      this.router.navigate(['/guest/landing'], { queryParams: { tableId: this.tableId } });
    } else {
      this.router.navigate(['/guest/landing']);
    }
  }
}
