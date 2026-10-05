import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { of, firstValueFrom } from 'rxjs';
import { vi } from 'vitest';
import { RegistroVentaService } from './registro-venta.service';
import { BuscarProductoService } from '../buscar-producto/buscar-producto.service';

describe('Búsqueda manual de productos en ventas', () => {
  it('reutiliza el catálogo, busca parcialmente sin distinguir mayúsculas y mapea los resultados', async () => {
    const base = { id: 1, codigo: 'OPT-001', nombre: 'Armazón clásico', precio: 12000, stock: 3 };
    const buscar = vi.fn(() => of([base, { ...base, id: 2, codigo: 'SOL-002', nombre: 'Lentes de sol' }]));
    TestBed.configureTestingModule({ providers: [provideHttpClient(),
      { provide: BuscarProductoService, useValue: { buscar } }] });
    const servicio = TestBed.inject(RegistroVentaService);
    for (const termino of ['aRmAz', 'opt-0', 'A']) {
      const resultados = await firstValueFrom(servicio.buscarCoincidencias(termino));
      expect(resultados).toEqual([{ idProducto: 1, codigoProducto: 'OPT-001', nombre: base.nombre,
        precio: 12000, stock: 3, cantidad: 1 }]);
      expect(buscar).toHaveBeenLastCalledWith(termino);
    }
    expect(await firstValueFrom(servicio.buscarCoincidencias('inexistente'))).toEqual([]);
  });
});
