import { Component, Input, OnInit, OnChanges, SimpleChanges, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { baseUrl } from '../../../core/enviroment';

@Component({
  selector: 'app-dish-story',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './dish-story.html',
  styleUrl: './dish-story.css'
})
export class DishStory implements OnInit, OnChanges {
  @Input() order: any;

  private http = inject(HttpClient);

  public selectedDish = signal<string>('');
  public storyText = signal<string>('');
  public isLoading = signal<boolean>(false);
  public errorMessage = signal<string | null>(null);

  private loadedOrderId: number | null = null;
  private availableDishes: string[] = [];

  ngOnInit() {
    this.processOrder();
  }

  ngOnChanges(changes: SimpleChanges) {
    if (changes['order']) {
      this.processOrder();
    }
  }

  private processOrder() {
    if (!this.order || !this.order.orderItems || this.order.orderItems.length === 0) {
      this.reset();
      return;
    }

    const currentOrderId = this.order.orderId;
    
    // Extract unique dish names from the order items
    const dishes = this.order.orderItems
      .map((item: any) => item.menuItemName)
      .filter((name: string) => !!name);

    this.availableDishes = Array.from(new Set(dishes)) as string[];

    if (this.availableDishes.length === 0) {
      this.reset();
      return;
    }

    // Only load story if the order ID has changed or if we haven't loaded one yet
    if (currentOrderId !== this.loadedOrderId || !this.selectedDish()) {
      this.loadedOrderId = currentOrderId;
      this.pickRandomDishAndLoadStory();
    }
  }

  private reset() {
    this.selectedDish.set('');
    this.storyText.set('');
    this.errorMessage.set(null);
    this.isLoading.set(false);
    this.loadedOrderId = null;
    this.availableDishes = [];
  }

  public pickRandomDishAndLoadStory() {
    if (this.availableDishes.length === 0) return;

    // Pick a random dish from the list
    const randomIndex = Math.floor(Math.random() * this.availableDishes.length);
    const chosen = this.availableDishes[randomIndex];

    this.loadStoryForDish(chosen);
  }

  public loadStoryForDish(dishName: string) {
    this.selectedDish.set(dishName);
    this.isLoading.set(true);
    this.errorMessage.set(null);
    this.storyText.set('');

    const url = `${baseUrl}genai/dish-story?dishName=${encodeURIComponent(dishName)}`;

    this.http.get(url, { responseType: 'text' }).subscribe({
      next: (story) => {
        this.storyText.set(story);
        this.isLoading.set(false);
      },
      error: (err) => {
        console.error('Failed to retrieve dish story:', err);
        this.errorMessage.set('Our culinary historian is busy in the pantry. Click below to try again.');
        this.isLoading.set(false);
      }
    });
  }

  public get hasMultipleDishes(): boolean {
    return this.availableDishes.length > 1;
  }
}
