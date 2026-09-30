import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of } from 'rxjs';

import { ConsultarStock } from './consultar-stock';
import { BuscarProductoService, Producto } from '../buscar-producto/buscar-producto.service';

describe('ConsultarStock', () => {
  let component: ConsultarStock;
  let fixture: ComponentFixture<ConsultarStock>;
  let actualizarStock: ReturnType<typeof vi.fn>;
  const productos: Producto[] = [
    {
      id: 1, codigo: 'AR-001', nombre: 'Armazón clásico', marca: 'Óptica', modelo: 'C1',
      color: 'Negro', categoria: 'Armazones', precio: 25000, stock: 4, stockMinimo: 1,
      estado: 'Disponible', tieneVentas: false
    },
    {
      id: 2, codigo: 'LE-002', nombre: 'Lentes de sol', marca: 'Óptica', modelo: 'S2',
      color: 'Negro', categoria: 'Lentes de sol', precio: 32000, stock: 0, stockMinimo: 1,
      estado: 'Agotado', tieneVentas: false
    }
  ];

  beforeEach(async () => {
    actualizarStock = vi.fn((id: number, stock: number, stockMinimo: number) =>
      of({ ...productos.find(producto => producto.id === id)!, stock, stockMinimo })
    );
    await TestBed.configureTestingModule({
      imports: [ConsultarStock],
      providers: [{
        provide: BuscarProductoService,
        useValue: { buscar: () => of(productos), actualizarStock }
      }]
    }).compileComponents();

    fixture = TestBed.createComponent(ConsultarStock);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('muestra las existencias y señala productos sin stock', () => {
    const tabla = fixture.nativeElement as HTMLElement;

    expect(tabla.textContent).toContain('4 unidades');
    expect(tabla.textContent).toContain('Sin stock');
    expect(tabla.textContent).toContain('AR-001');
    expect(tabla.textContent).toContain('LE-002');
  });

  it('filtra productos por nombre o código sin ocultar el estado de stock', () => {
    const input = fixture.nativeElement.querySelector('#buscar-stock') as HTMLInputElement;
    input.value = 'LE-002';
    input.dispatchEvent(new Event('input'));
    fixture.detectChanges();

    const texto = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(texto).toContain('Sin stock');
    expect(texto).not.toContain('AR-001');
  });

  it('actualiza existencias y stock mínimo desde el listado', () => {
    component['editarStock'](productos[0]);
    component['borradorStock'].set({ stock: 7, stockMinimo: 2 });
    component['guardarStock'](productos[0]);
    fixture.detectChanges();

    expect(actualizarStock).toHaveBeenCalledWith(1, 7, 2);
    expect(component['productos']()[0]).toMatchObject({ stock: 7, stockMinimo: 2 });
    expect(component['productoEditando']()).toBeNull();
  });
});
