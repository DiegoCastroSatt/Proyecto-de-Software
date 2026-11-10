import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ReporteStock } from './reporte-stock';

describe('ReporteStock', () => {
  let component: ReporteStock;
  let fixture: ComponentFixture<ReporteStock>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ReporteStock],
    }).compileComponents();

    fixture = TestBed.createComponent(ReporteStock);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
