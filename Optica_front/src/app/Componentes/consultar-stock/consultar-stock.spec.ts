import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ConsultarStock } from './consultar-stock';

describe('ConsultarStock', () => {
  let component: ConsultarStock;
  let fixture: ComponentFixture<ConsultarStock>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ConsultarStock],
    }).compileComponents();

    fixture = TestBed.createComponent(ConsultarStock);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
