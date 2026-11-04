import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of } from 'rxjs';

import { HistorialStock } from './historial-stock';
import { BuscarProductoService } from '../buscar-producto/buscar-producto.service';

describe('HistorialStock', () => {
  let component: HistorialStock;
  let fixture: ComponentFixture<HistorialStock>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HistorialStock],
      providers: [{
        provide: BuscarProductoService,
        useValue: {
          buscar: () => of([]),
          consultarHistorialStock: () => of({
            pagina: 1,
            tamanoPagina: 20,
            totalRegistros: 0,
            totalPaginas: 0,
            movimientos: []
          }),
          consultarHistorialStockReciente: () => of({
            pagina: 1,
            tamanoPagina: 20,
            totalRegistros: 0,
            totalPaginas: 0,
            movimientos: []
          })
        }
      }]
    }).compileComponents();

    fixture = TestBed.createComponent(HistorialStock);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
