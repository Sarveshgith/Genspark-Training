import { Component, Input, Output, EventEmitter } from '@angular/core';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-filter-panel',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './filter-panel.html'
})
export class FilterPanelComponent {
  @Input() isOpen: boolean = false;
  @Input() minPrice: number | null = null;
  @Input() maxPrice: number | null = null;
  @Input() maxPrepTime: number | null = null;
  @Input() availability: string = 'all';

  @Output() minPriceChange = new EventEmitter<number | null>();
  @Output() maxPriceChange = new EventEmitter<number | null>();
  @Output() maxPrepTimeChange = new EventEmitter<number | null>();
  @Output() availabilityChange = new EventEmitter<string>();
  @Output() apply = new EventEmitter<void>();
  @Output() clear = new EventEmitter<void>();
}
