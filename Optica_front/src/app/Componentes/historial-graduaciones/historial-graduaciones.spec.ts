import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { vi } from 'vitest';

import { HistorialGraduaciones } from './historial-graduaciones';
import { HistorialGraduacionesService, RecetaHistorial } from './historial-graduaciones.service';
import { ClienteService } from '../clientes/cliente.service';

describe('HistorialGraduaciones', () => {
  let fixture: ComponentFixture<HistorialGraduaciones>;
  let rut: string | null;
  let datos: RecetaHistorial[];
  const servicioHistorial = { obtenerHistorial: vi.fn() };
  const servicioClientes = { buscar: vi.fn() };

  beforeEach(async () => {
    vi.resetAllMocks();
    rut = '12.345.678-5';
    datos = [
      { id: 1, fecha: '2025-01-01', graduaciones: [{ ojo: 'OD', esfera: -1, cilindro: -0.5, eje: 90, adicion: null }] },
      { id: 2, fecha: '2026-01-01', graduaciones: [{ ojo: 'OD', esfera: -2, cilindro: -0.5, eje: 90, adicion: null }] },
      { id: 3, fecha: '2026-02-01', graduaciones: [{ ojo: 'OD', esfera: -3, cilindro: -0.5, eje: 90, adicion: null }] }
    ];
    servicioHistorial.obtenerHistorial.mockImplementation(() => of(datos));
    servicioClientes.buscar.mockReturnValue(of([]));
    await TestBed.configureTestingModule({
      imports: [HistorialGraduaciones],
      providers: [
        provideRouter([]),
        { provide: ActivatedRoute, useValue: { snapshot: {
          data: {}, queryParamMap: { get: () => rut }
        } } },
        { provide: HistorialGraduacionesService, useValue: servicioHistorial },
        { provide: ClienteService, useValue: servicioClientes }
      ]
    }).compileComponents();
  });

  function crearComponente(): void {
    fixture = TestBed.createComponent(HistorialGraduaciones);
    fixture.detectChanges();
  }

  it('busca automáticamente el RUT recibido en la URL', () => {
    crearComponente();

    expect(servicioHistorial.obtenerHistorial).toHaveBeenCalledWith('12.345.678-5');
    expect(fixture.nativeElement.querySelectorAll('.receta-card')).toHaveLength(3);
  });

  it('impide comparar más de dos recetas y muestra cambios entre las seleccionadas', () => {
    crearComponente();
    const casillas = fixture.nativeElement.querySelectorAll('input[type="checkbox"]') as NodeListOf<HTMLInputElement>;
    casillas[0].click();
    casillas[1].click();
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('Comparación de recetas');
    expect(fixture.nativeElement.querySelectorAll('.comparacion-tabla tr.cambio').length).toBeGreaterThan(0);
    expect(casillas[2].disabled).toBe(true);
  });

  it('muestra un error si se busca sin ingresar RUT', () => {
    rut = null;
    crearComponente();
    const entrada = fixture.nativeElement.querySelector('app-autocompletado-rut input') as HTMLInputElement;
    entrada.value = '';
    entrada.dispatchEvent(new Event('input'));
    fixture.detectChanges();
    (fixture.nativeElement.querySelector('.busqueda-card button') as HTMLButtonElement).click();
    fixture.detectChanges();

    expect(servicioHistorial.obtenerHistorial).not.toHaveBeenCalled();
    expect(fixture.nativeElement.textContent).toContain('Ingresa el RUT del cliente.');
  });

  it('muestra el error entregado por la API y limpia resultados anteriores', () => {
    servicioHistorial.obtenerHistorial.mockReturnValueOnce(throwError(() => ({ error: { mensaje: 'RUT no encontrado.' } })));
    crearComponente();

    expect(fixture.nativeElement.textContent).toContain('RUT no encontrado.');
    expect(fixture.nativeElement.querySelectorAll('.receta-card')).toHaveLength(0);
  });
});
