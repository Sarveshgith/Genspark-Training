import { ComponentFixture, TestBed } from '@angular/core/testing';

import { KdsBoard } from './kds-board';

describe('KdsBoard', () => {
  let component: KdsBoard;
  let fixture: ComponentFixture<KdsBoard>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [KdsBoard],
    }).compileComponents();

    fixture = TestBed.createComponent(KdsBoard);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
