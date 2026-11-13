import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of } from 'rxjs';

import { BuscarProductoAdmin } from './buscar-producto-admin';
import { BuscarProductoService } from '../buscar-producto/buscar-producto.service';

describe('BuscarProductoAdmin', () => {
  let component: BuscarProductoAdmin;
  let fixture: ComponentFixture<BuscarProductoAdmin>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [BuscarProductoAdmin],
      providers: [{
        provide: BuscarProductoService,
        useValue: {
          buscarPagina: () => of({ productos: [], haySiguiente: false }),
          obtenerCatalogo: () => of([])
        }
      }]
    }).compileComponents();

    fixture = TestBed.createComponent(BuscarProductoAdmin);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
