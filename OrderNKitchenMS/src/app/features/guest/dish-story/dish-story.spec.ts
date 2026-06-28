import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { DishStory } from './dish-story';
import { baseUrl } from '../../../core/enviroment';

describe('DishStory', () => {
  let component: DishStory;
  let fixture: ComponentFixture<DishStory>;
  let httpMock: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HttpClientTestingModule, DishStory],
    }).compileComponents();

    fixture = TestBed.createComponent(DishStory);
    component = fixture.componentInstance;
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should not load story if order has no items', () => {
    component.order = { orderId: 1, orderItems: [] };
    fixture.detectChanges();

    expect(component.selectedDish()).toBe('');
    expect(component.storyText()).toBe('');
  });

  it('should load story for a random dish when order has items', () => {
    component.order = {
      orderId: 45,
      orderItems: [
        { menuItemName: 'Wagyu Ribeye' }
      ]
    };
    
    // Trigger lifecycle hooks
    fixture.detectChanges();

    expect(component.selectedDish()).toBe('Wagyu Ribeye');
    expect(component.isLoading()).toBe(true);

    const req = httpMock.expectOne(`${baseUrl}genai/dish-story?dishName=Wagyu%20Ribeye`);
    expect(req.request.method).toBe('GET');
    req.flush('A legendary cut of beef cooked to perfection.');

    expect(component.isLoading()).toBe(false);
    expect(component.storyText()).toBe('A legendary cut of beef cooked to perfection.');
  });
});
