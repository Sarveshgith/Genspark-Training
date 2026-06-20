import { Component, input, output, signal } from '@angular/core';
import { OrderModel } from '../../../core/models/order.model';
import { CurrencyPipe, DatePipe } from '@angular/common';

@Component({
  selector: 'app-order-card',
  imports: [CurrencyPipe, DatePipe],
  templateUrl: './order-card.html',
  styleUrl: './order-card.css',
})
export class OrderCard {
  public order = input.required<OrderModel>();
  public startCooking = output<number>();
  public markReady = output<number>();
  public isModalOpen = signal<boolean>(false);

  public openModal(event: MouseEvent): void {
    const target = event.target as HTMLElement;
    if (target.tagName.toLowerCase() === 'button') {
      return;
    }
    this.isModalOpen.set(true);
  }

  public closeModal(): void {
    this.isModalOpen.set(false);
  }

  public onStartCooking(): void {
    this.startCooking.emit(this.order().id);
  }

  public onMarkReady(): void {
    this.markReady.emit(this.order().id);
  }
}
