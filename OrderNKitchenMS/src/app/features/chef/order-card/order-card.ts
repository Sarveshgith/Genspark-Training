import { Component, input, output, signal, computed, OnInit, OnDestroy } from '@angular/core';
import { OrderModel } from '../../../core/models/order.model';
import { CurrencyPipe, DatePipe, DecimalPipe } from '@angular/common';
import { OrderTimer } from '../order-timer/order-timer';

@Component({
  selector: 'app-order-card',
  standalone: true,
  imports: [CurrencyPipe, DatePipe, DecimalPipe, OrderTimer],
  templateUrl: './order-card.html',
  styleUrl: './order-card.css',
})
export class OrderCard implements OnInit, OnDestroy {
  public order = input.required<OrderModel>();
  public cookingStartTime = input<number | undefined>();

  public startCooking = output<number>();
  public markReady = output<number>();

  public isModalOpen = signal<boolean>(false);
  public elapsedSeconds = signal<number>(0);
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

  // Veg / non-veg / spicy diet badges dynamically calculated
  public dietBadges = computed(() => {
    const items = this.order().orderItems;
    const badges: string[] = [];
    
    let hasNonVeg = false;
    let hasSpicy = false;
    
    const nonVegKeywords = [
      'chicken', 'beef', 'pork', 'mutton', 'fish', 'meat', 'prawn', 'shrimp', 
      'lamb', 'bacon', 'pepperoni', 'salami', 'wagyu', 'wings', 'duck', 'turkey', 'salmon'
    ];
    const spicyKeywords = [
      'spicy', 'hot', 'chili', 'chilli', 'jalapeno', 'szechuan', 'pepper', 'curry', 'wasabi'
    ];
    
    items.forEach(item => {
      const nameLower = item.menuItemName.toLowerCase();
      const notesLower = (item.notes || '').toLowerCase();
      
      if (nonVegKeywords.some(kw => nameLower.includes(kw) || notesLower.includes(kw))) {
        hasNonVeg = true;
      }
      if (spicyKeywords.some(kw => nameLower.includes(kw) || notesLower.includes(kw))) {
        hasSpicy = true;
      }
    });

    if (hasNonVeg) {
      badges.push('Non-Veg');
    } else {
      badges.push('Veg');
    }

    if (hasSpicy) {
      badges.push('Spicy');
    }

    return badges;
  });

  // Estimated preparation time in minutes
  public prepTimeMinutes = computed(() => {
    const order = this.order();
    if (order.estimatedReadyAt) {
      const diffMs = new Date(order.estimatedReadyAt).getTime() - new Date(order.createdAt).getTime();
      const mins = Math.round(diffMs / 60000);
      return mins > 0 ? mins : 15; // default fallback if 0 or negative
    }
    return 15; // default prep estimate
  });

  // Progress percentage of In Prep cooking: progress bar filling as time elapses
  public progressPercentage = computed(() => {
    if (this.order().status !== 2) return 0;
    const limitSeconds = this.prepTimeMinutes() * 60;
    const percent = (this.elapsedSeconds() / limitSeconds) * 100;
    return Math.min(100, Math.max(0, percent));
  });

  // Card border turns orange when overdue (> 1.5x prep time)
  public isOverdue = computed(() => {
    if (this.order().status !== 2) return false;
    const limitSeconds = this.prepTimeMinutes() * 60;
    return this.elapsedSeconds() > (1.5 * limitSeconds);
  });

  public openModal(event: MouseEvent): void {
    const target = event.target as HTMLElement;
    // Don't open the modal if clicking buttons
    if (target.tagName.toLowerCase() === 'button' || target.closest('button')) {
      return;
    }
    this.isModalOpen.set(true);
  }

  public closeModal(): void {
    this.isModalOpen.set(false);
  }

  public onPrint(event: MouseEvent): void {
    event.stopPropagation();
    console.log(`Printing ticket for Order #${this.order().id}`);
    alert(`Order Ticket #${this.order().id}\nTable: T-${this.order().tableNumber}\nItems count: ${this.order().orderItems.length}`);
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
