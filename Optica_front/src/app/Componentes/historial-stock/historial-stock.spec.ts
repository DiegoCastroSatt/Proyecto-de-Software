import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { HistorialStock } from './historial-stock';
import { BuscarProductoService, HistorialStockPaginado, Producto } from '../buscar-producto/buscar-producto.service';

describe('HistorialStock', () => {
  let component: HistorialStock;
  let fixture: ComponentFixture<HistorialStock>;
  let productoServiceMock: {
    buscar: ReturnType<typeof vi.fn>;
    consultarHistorialStock: ReturnType<typeof vi.fn>;
    consultarHistorialStockReciente: ReturnType<typeof vi.fn>;
  };

const mockProductos: Producto[] = [
  { id: 1,
    codigo: 'ARM-001',
    nombre: 'Armazon 1',
    marca: 'Rayban',
    modelo: 'Aviator',
    color: 'Negro',
    categoria: 'Armazones',
    precio: 67000,
    stock: 10,
    stockMinimo: 5,
    estado: 'Disponible',
    tieneVentas: true
  },

    {
    id: 2,
    codigo: 'LEN-002',
    nombre: 'Lentes Polarizados',
    marca: 'Oakley',
    modelo: 'Holbrook',
    color: 'Negro',
    categoria: 'Lentes de sol',
    precio: 69000,
    stock: 5,
    stockMinimo: 2,
    estado: 'Disponible',
    tieneVentas: false
  }
];

const mockHistorial: HistorialStockPaginado = {
  pagina: 1,
  tamanoPagina: 20,
  totalRegistros: 2,
  totalPaginas: 2,
  movimientos: [
    {
      idCambio: 1,
      idProducto: 1,
      motivoMovimiento: 'Nuevo producto',
      idResponsable: 1,
      nombreResponsable: 'Juan Pérez',
      tipoStock: 'Normal',
      valorAnterior: 0,
      valorActual: 5,
      fechaCambio: '2026-11-06T10:00:00Z'
    },

    {
      idCambio: 2,
      idProducto: 2,
      motivoMovimiento: "Ajuste de stock minimo",
      idResponsable: 1,
      nombreResponsable: 'Juan Pérez',
      tipoStock: 'Minimo',
      valorAnterior: 5,
      valorActual: 3,
      fechaCambio: '2026-11-06T10:00:00Z'
    }
  ]
};

const mockFiltro: HistorialStockPaginado = {
  pagina: 1,
  tamanoPagina: 20,
  totalRegistros: 1,
  totalPaginas: 1,
  movimientos: [
    {
      idCambio: 1,
      idProducto: 1,
      motivoMovimiento: 'Nuevo producto',
      idResponsable: 1,
      nombreResponsable: 'Juan Pérez',
      tipoStock: 'Normal',
      valorAnterior: 0,
      valorActual: 5,
      fechaCambio: '2026-11-06T10:00:00Z'
    }
  ]
};

beforeEach(async () => {
  productoServiceMock = {
    buscar: vi.fn(() => of(mockProductos)),
    consultarHistorialStock: vi.fn(() => of(mockFiltro)),
    consultarHistorialStockReciente: vi.fn(() => of(mockHistorial))
  };

  await TestBed.configureTestingModule({
    imports: [HistorialStock],
    providers: [{ provide: BuscarProductoService, useValue: productoServiceMock }]
  }).compileComponents();

  fixture = TestBed.createComponent(HistorialStock);
  component = fixture.componentInstance;
  fixture.detectChanges();
  await fixture.whenStable();
});

it('debería crear el componente', () => {
  expect(component).toBeTruthy();
  expect(productoServiceMock.buscar).toHaveBeenCalled();
  expect(productoServiceMock.consultarHistorialStockReciente).toHaveBeenCalledWith(1, 20);

  const tabla = fixture.nativeElement as HTMLElement;
  expect(tabla.textContent).toContain('Armazon 1 · ARM-001');
  expect(tabla.textContent).toContain('Nuevo producto');
  expect(tabla.textContent).toContain('Ajuste de stock minimo');
  expect(tabla.textContent).toContain('Mínimo');
  expect(tabla.textContent).toContain('Juan Pérez');
});

it('filtra el historial al seleccionar un producto sugerido', async () => {
  productoServiceMock.consultarHistorialStock.mockImplementation(
    (idProducto: number, pagina: number) => of({ ...mockFiltro, pagina, totalPaginas: 2, totalRegistros: 2 })
  );
  const input = fixture.nativeElement.querySelector('#buscar-producto-historial') as HTMLInputElement;
  input.value = 'Armazon 1';
  input.dispatchEvent(new Event('input'));
  fixture.detectChanges();
  await fixture.whenStable();

  const sugerencias = fixture.nativeElement.querySelectorAll(
    '.product-suggestions button'
  ) as NodeListOf<HTMLButtonElement>;
  expect(sugerencias.length).toBe(1);
  expect(sugerencias[0].textContent).toContain('Armazon 1 · ARM-001');

  sugerencias[0].click();
  fixture.detectChanges();
  await fixture.whenStable();

  expect(productoServiceMock.consultarHistorialStock).toHaveBeenCalledWith(1, 1, 20);
  expect(fixture.nativeElement.textContent).toContain('Armazon 1 · ARM-001');
  expect(fixture.nativeElement.textContent).not.toContain('Lentes Polarizados · LEN-002');

  const siguiente = fixture.nativeElement.querySelector('.paginacion-listado button[aria-label="Página siguiente"]') as HTMLButtonElement;
  siguiente.click();
  fixture.detectChanges();
  await fixture.whenStable();

  expect(productoServiceMock.consultarHistorialStock).toHaveBeenLastCalledWith(1, 2, 20);
});

it('informa que la búsqueda no tiene resultados', async () => {
  const input = fixture.nativeElement.querySelector('#buscar-producto-historial') as HTMLInputElement;
  input.value = 'inexistente';
  input.dispatchEvent(new Event('input'));
  fixture.detectChanges();
  await fixture.whenStable();

  const status = fixture.nativeElement.querySelector('.status') as HTMLElement;
  expect(status?.textContent).toContain('No se encontraron productos coincidentes.');
});

it('permite avanzar y retroceder páginas del historial reciente', async () => {
  productoServiceMock.consultarHistorialStockReciente.mockImplementation(
    (pagina: number) => of({ ...mockHistorial, pagina, totalPaginas: 3 })
  );
  const siguiente = fixture.nativeElement.querySelector('.paginacion-listado button[aria-label="Página siguiente"]') as HTMLButtonElement;
  siguiente.click();
  fixture.detectChanges();
  await fixture.whenStable();

  expect(productoServiceMock.consultarHistorialStockReciente).toHaveBeenLastCalledWith(2, 20);
  expect(fixture.nativeElement.querySelector('.paginacion-listado [aria-current="page"]')?.textContent?.trim()).toBe('2');

  const anterior = fixture.nativeElement.querySelector('.paginacion-listado button[aria-label="Página anterior"]') as HTMLButtonElement;
  anterior.click();
  fixture.detectChanges();
  await fixture.whenStable();

  expect(productoServiceMock.consultarHistorialStockReciente).toHaveBeenLastCalledWith(1, 20);
});

it('muestra el estado vacío cuando la consulta no devuelve movimientos', async () => {
  productoServiceMock.consultarHistorialStockReciente.mockReturnValueOnce(of({
    pagina: 2,
    tamanoPagina: 20,
    totalRegistros: 0,
    totalPaginas: 0,
    movimientos: []
  }));
  const siguiente = fixture.nativeElement.querySelector('.paginacion-listado button[aria-label="Página siguiente"]') as HTMLButtonElement;
  siguiente.click();
  fixture.detectChanges();
  await fixture.whenStable();

  expect(fixture.nativeElement.textContent).toContain('No hay movimientos recientes de stock.');
  expect(fixture.nativeElement.querySelector('table')).toBeNull();
});

it('informa errores al consultar el historial y permite reintentar', async () => {
  productoServiceMock.consultarHistorialStockReciente.mockReturnValueOnce(
    throwError(() => ({ error: { mensaje: 'No fue posible consultar el historial.' } }))
  );
  const siguiente = fixture.nativeElement.querySelector('.paginacion-listado button[aria-label="Página siguiente"]') as HTMLButtonElement;
  siguiente.click();
  fixture.detectChanges();
  await fixture.whenStable();

  expect(fixture.nativeElement.querySelector('[role="alert"]')?.textContent)
    .toContain('No fue posible consultar el historial.');

  const reintentar = fixture.nativeElement.querySelector('[role="alert"] button') as HTMLButtonElement;
  reintentar.click();
  fixture.detectChanges();
  await fixture.whenStable();

  expect(productoServiceMock.consultarHistorialStockReciente).toHaveBeenLastCalledWith(2, 20);
  expect(fixture.nativeElement.querySelector('[role="alert"]')).toBeNull();
  expect(fixture.nativeElement.querySelector('table')).not.toBeNull();
});

});