import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { of, firstValueFrom } from 'rxjs';
import { vi } from 'vitest';
import { RegistroVentaService } from './registro-venta.service';
import { BuscarProductoService } from '../buscar-producto/buscar-producto.service';

describe('Búsqueda manual de productos en ventas', () => {
  it('envía el RUT opcional sin alterar las ventas sin cliente', () => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    const servicio = TestBed.inject(RegistroVentaService); const http = TestBed.inject(HttpTestingController);
    const productos = [{ codigoProducto: 'A', cantidad: 1 }];
    servicio.crear(productos).subscribe(); const libre = http.expectOne('http://localhost:8080/api/Ventas');
    expect(libre.request.body).toEqual({ productos }); libre.flush({});
    servicio.crear(productos, ' 12.345.678-5 ').subscribe(); const vinculada = http.expectOne('http://localhost:8080/api/Ventas');
    expect(vinculada.request.body).toEqual({ productos, rutCliente: '12.345.678-5' }); vinculada.flush({}); http.verify();
  });

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
