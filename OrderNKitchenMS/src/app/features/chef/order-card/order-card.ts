import { Component, input, output, signal, computed, OnInit, OnDestroy, inject } from '@angular/core';
import { OrderModel } from '../../../core/models/order.model';
import { CurrencyPipe, DatePipe, DecimalPipe } from '@angular/common';
import { OrderTimer } from '../order-timer/order-timer';
import { SignalRService } from '../../../core/services/signalr.service';

@Component({
  selector: 'app-order-card',
  standalone: true,
  imports: [CurrencyPipe, DatePipe, DecimalPipe, OrderTimer],
  templateUrl: './order-card.html',
  styleUrl: './order-card.css',
})
export class OrderCard implements OnInit, OnDestroy {
  private signalRService = inject(SignalRService);

  public order = input.required<OrderModel>();
  public cookingStartTime = input<number | undefined>();

  public startCooking = output<number>();
  public markReady = output<number>();

  public isModalOpen = signal<boolean>(false);
  public elapsedSeconds = signal<number>(0);
  
  // Composer state
  public isComposerOpen = signal<boolean>(false);
  public selectedMessageType = signal<'order_delayed' | 'item_substitution_needed' | 'low_stock_warning_floor'>('order_delayed');
  public substituteItemInput = signal<string>('');
  public composerNote = signal<string>('');
  
  private touchTimeoutId: any;
  private timerId: any;

  public ngOnInit(): void {
    const tick = () => {
      let start: number;
      if (this.order().status === 2 && this.cookingStartTime()) {
        start = this.cookingStartTime()!;
      } else {
        start = new Date(this.order().createdAt).getTime();
      }
      const diff = Math.floor((Date.now() - start) / 1000);
      this.elapsedSeconds.set(diff >= 0 ? diff : 0);
    };

    tick();
    this.timerId = setInterval(tick, 1000);
  }

  public prepTimeMinutes = computed(() => {
    const order = this.order();
    if (order.estimatedReadyAt) {
      const diffMs = new Date(order.estimatedReadyAt).getTime() - new Date(order.createdAt).getTime();
      const mins = Math.round(diffMs / 60000);
      return mins > 0 ? mins : 15;
    }
    return 15;
  });

  public progressPercentage = computed(() => {
    if (this.order().status !== 2) return 0;
    const limitSeconds = this.prepTimeMinutes() * 60;
    const percent = (this.elapsedSeconds() / limitSeconds) * 100;
    return Math.min(100, Math.max(0, percent));
  });

  public isOverdue = computed(() => {
    if (this.order().status !== 2) return false;
    const limitSeconds = this.prepTimeMinutes() * 60;
    return this.elapsedSeconds() > (1.5 * limitSeconds);
  });

  public openModal(event: MouseEvent): void {
    const target = event.target as HTMLElement;
    if (target.tagName.toLowerCase() === 'button' || target.closest('button')) {
      return;
    }
    this.isModalOpen.set(true);
  }

  public closeModal(): void {
    this.isModalOpen.set(false);
  }

  public openComposer(event?: MouseEvent): void {
    if (event) {
      event.stopPropagation();
    }
    this.isComposerOpen.set(true);
  }

  public closeComposer(): void {
    this.isComposerOpen.set(false);
    this.selectedMessageType.set('order_delayed');
    this.substituteItemInput.set('');
    this.composerNote.set('');
  }

  public onMessageTypeChange(event: any): void {
    this.selectedMessageType.set(event.target.value);
  }

  public onSubstituteItemChange(event: any): void {
    this.substituteItemInput.set(event.target.value);
  }

  public onNoteChange(event: any): void {
    this.composerNote.set(event.target.value);
  }

  public sendComposerMessage(): void {
    const type = this.selectedMessageType();
    const tableId = this.order().tableId;
    
    const payload: any = {
      orderId: this.order().id,
      note: this.composerNote()
    };

    if (type === 'item_substitution_needed') {
      payload.substituteItem = this.substituteItemInput();
    }

    this.signalRService.sendFloorMessage(type, tableId, payload)
      .then(() => {
        this.closeComposer();
      })
      .catch(err => {
        console.error('Failed to send floor message:', err);
      });
  }

  public onTouchStart(event: TouchEvent): void {
    this.touchTimeoutId = setTimeout(() => {
      this.openComposer();
    }, 600);
  }

  public onTouchEnd(): void {
    if (this.touchTimeoutId) {
      clearTimeout(this.touchTimeoutId);
      this.touchTimeoutId = null;
    }
  }

  public onAction(event: MouseEvent): void {
    event.stopPropagation();
    if (this.order().status === 1) {
      this.startCooking.emit(this.order().id);
    } else if (this.order().status === 2 || this.order().status === 3) {
      this.markReady.emit(this.order().id);
    }
  }

  public ngOnDestroy(): void {
    if (this.timerId) {
      clearInterval(this.timerId);
    }
  }
}
