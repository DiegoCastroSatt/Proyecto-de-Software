import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of } from 'rxjs';

import { BuscarReservas } from './buscar-reservas';
import { AgendaReservasService } from './agenda-reservas.service';

describe('BuscarReservas', () => {
  let component: BuscarReservas;
  let fixture: ComponentFixture<BuscarReservas>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [BuscarReservas],
      providers: [{ provide: AgendaReservasService, useValue: { listar: () => of([]) } }],
    }).compileComponents();

    fixture = TestBed.createComponent(BuscarReservas);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
