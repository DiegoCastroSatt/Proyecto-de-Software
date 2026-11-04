import { ComponentFixture, TestBed } from '@angular/core/testing';

import { HistorialStock } from './historial-stock';

describe('HistorialStock', () => {
  let component: HistorialStock;
  let fixture: ComponentFixture<HistorialStock>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HistorialStock],
    }).compileComponents();

    fixture = TestBed.createComponent(HistorialStock);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
