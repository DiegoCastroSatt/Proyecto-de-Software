import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { DisponibilidadMarcosService } from './disponibilidad-marcos.service';
import { TipoMarco, TiposMarcoService } from './tipos-marco.service';

describe('Tipos de marco persistentes', () => {
  beforeEach(() => TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] }));
  afterEach(() => TestBed.inject(HttpTestingController).verify());

  it('consulta la lista pública filtrada por actividad y stock en el servidor', () => {
    let tipos: TipoMarco[] = [];
    TestBed.inject(DisponibilidadMarcosService).consultar().subscribe(valor => tipos = valor);
    TestBed.inject(HttpTestingController).expectOne('http://localhost:8080/api/Marcos/disponibles')
      .flush([{ id: 5, codigo: 'nuevo', nombre: 'Nuevo', stock: 2, rostros: ['ovalado'] }]);
    expect(tipos[0].codigo).toBe('nuevo');
  });

  it('guarda SVG y rostros y permite desactivar y borrar el tipo', () => {
    const servicio = TestBed.inject(TiposMarcoService);
    const http = TestBed.inject(HttpTestingController);
    const datos = { nombre: 'Nuevo', categoria: 'Nuevo', svg: '<svg/>', rostros: ['ovalado'] };
    servicio.crear(datos).subscribe();
    const crear = http.expectOne('http://localhost:8080/api/Marcos');
    expect(crear.request.method).toBe('POST'); expect(crear.request.body).toEqual(datos); crear.flush({});
    servicio.cambiarEstado(5, false).subscribe();
    const estado = http.expectOne('http://localhost:8080/api/Marcos/5/estado');
    expect(estado.request.method).toBe('PATCH'); expect(estado.request.body).toEqual({ activo: false }); estado.flush(null);
    servicio.borrar(5).subscribe();
    const borrar = http.expectOne('http://localhost:8080/api/Marcos/5');
    expect(borrar.request.method).toBe('DELETE'); borrar.flush(null);
  });
});
