import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of } from 'rxjs';

import { BuscarProductoCliente } from './buscar-producto-cliente';
import { BuscarProductoService } from '../buscar-producto/buscar-producto.service';

describe('BuscarProductoCliente', () => {
  let component: BuscarProductoCliente;
  let fixture: ComponentFixture<BuscarProductoCliente>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [BuscarProductoCliente],
      providers: [{ provide: BuscarProductoService, useValue: { buscar: () => of([]) } }]
    }).compileComponents();

    fixture = TestBed.createComponent(BuscarProductoCliente);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
