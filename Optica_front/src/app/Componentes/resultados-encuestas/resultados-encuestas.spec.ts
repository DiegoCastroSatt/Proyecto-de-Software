import { PLATFORM_ID } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { ResultadosEncuestasComponent } from './resultados-encuestas';
import {
  ComentariosEncuestasPaginados,
  EncuestasService,
  ResumenEncuestas,
} from '../../shared/services/encuestas.service';

const resumenConRespuestas: ResumenEncuestas = {
  enviadas: 186,
  respondidas: 71,
  tasaRespuesta: 38.17,
  promedioGeneral: 4.43,
  promedioTrato: 4.7,
  promedioPuntualidad: 3.6,
  distribucionGeneral: [
    { estrellas: 5, cantidad: 43 },
    { estrellas: 4, cantidad: 18 },
    { estrellas: 3, cantidad: 6 },
    { estrellas: 2, cantidad: 3 },
    { estrellas: 1, cantidad: 1 },
  ],
  evolucionMensual: [
    {
      mes: '2026-10',
      respuestas: 30,
      promedioGeneral: 4.1,
      promedioTrato: 4.6,
      promedioPuntualidad: 3.3,
    },
    {
      mes: '2026-11',
      respuestas: 41,
      promedioGeneral: 4.6,
      promedioTrato: 4.8,
      promedioPuntualidad: 3.9,
    },
  ],
};

const comentarios: ComentariosEncuestasPaginados = {
  elementos: [
    {
      id: 1,
      mes: '2026-11',
      calificacionGeneral: 5,
      calificacionTrato: 5,
      calificacionPuntualidad: 4,
      comentario: 'Me explicaron muy bien.',
      comentarioOculto: false,
    },
    {
      id: 2,
      mes: '2026-10',
      calificacionGeneral: 3,
      calificacionTrato: 5,
      calificacionPuntualidad: 2,
      comentario: 'Tuve que esperar.',
      comentarioOculto: false,
    },
    {
      id: 3,
      mes: '2026-10',
      calificacionGeneral: 1,
      calificacionTrato: 1,
      calificacionPuntualidad: 1,
      comentario: null,
      comentarioOculto: true,
    },
  ],
  total: 34,
  pagina: 1,
  tamanoPagina: 10,
};

describe('ResultadosEncuestasComponent', () => {
  let fixture: ComponentFixture<ResultadosEncuestasComponent>;
  let component: ResultadosEncuestasComponent;
  const encuestas = {
    obtenerResumen: vi.fn(),
    obtenerComentarios: vi.fn(),
    cambiarVisibilidadComentario: vi.fn(),
  };

  beforeEach(async () => {
    vi.resetAllMocks();
    encuestas.obtenerResumen.mockReturnValue(of(resumenConRespuestas));
    encuestas.obtenerComentarios.mockReturnValue(of(comentarios));
    await TestBed.configureTestingModule({
      imports: [ResultadosEncuestasComponent],
      providers: [
        { provide: EncuestasService, useValue: encuestas },
        // En pruebas no se dibuja el gráfico: jsdom no implementa canvas.
        { provide: PLATFORM_ID, useValue: 'server' },
      ],
    }).compileComponents();
    fixture = TestBed.createComponent(ResultadosEncuestasComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  function texto(): string {
    return fixture.nativeElement.textContent as string;
  }

  it('carga el resumen y los comentarios de los últimos tres meses', () => {
    const filtro = encuestas.obtenerResumen.mock.calls[0][0];
    const desde = new Date(`${filtro.desde}T00:00:00`);
    const hasta = new Date(`${filtro.hasta}T00:00:00`);
    const meses =
      (hasta.getFullYear() - desde.getFullYear()) * 12 + hasta.getMonth() - desde.getMonth();

    expect(meses).toBe(3);
    expect(encuestas.obtenerComentarios).toHaveBeenCalledWith(
      expect.objectContaining({
        desde: filtro.desde,
        hasta: filtro.hasta,
        calificacion: null,
        soloConComentario: true,
        pagina: 1,
        tamanoPagina: 10,
      }),
    );
  });

  it('muestra las métricas, los promedios por pregunta y la distribución', () => {
    expect(texto()).toContain('186');
    expect(texto()).toContain('71');
    expect(texto()).toContain('38%');
    expect(texto()).toContain('4,4');
    expect(texto()).toContain('Trato del personal');
    expect(texto()).toContain('3,6');
    expect(fixture.nativeElement.querySelector('canvas')).not.toBeNull();
  });

  it('muestra los comentarios con su mes, sin datos del cliente, y la paginación', () => {
    expect(texto()).toContain('Me explicaron muy bien.');
    expect(texto()).toContain('Noviembre 2026');
    expect(texto()).toContain('Mostrando 1–10 de 34');
  });

  it('muestra un mensaje cuando no hay respuestas en el periodo', () => {
    encuestas.obtenerResumen.mockReturnValueOnce(
      of({
        ...resumenConRespuestas,
        respondidas: 0,
        tasaRespuesta: 0,
        promedioGeneral: null,
        promedioTrato: null,
        promedioPuntualidad: null,
        distribucionGeneral: [],
        evolucionMensual: [],
      }),
    );
    component.cargarResumen();
    fixture.detectChanges();

    expect(texto()).toContain('Aún no hay respuestas en este periodo.');
    expect(fixture.nativeElement.querySelector('canvas')).toBeNull();
  });

  it('filtra los comentarios por calificación y vuelve a la primera página', () => {
    component.irAPagina(2);
    const select = fixture.nativeElement.querySelector('select') as HTMLSelectElement;
    select.value = '5';
    select.dispatchEvent(new Event('change'));

    expect(encuestas.obtenerComentarios).toHaveBeenLastCalledWith(
      expect.objectContaining({ calificacion: 5, pagina: 1 }),
    );
  });

  it('permite ver también las respuestas sin comentario', () => {
    const check = fixture.nativeElement.querySelector('input[type="checkbox"]') as HTMLInputElement;
    check.checked = false;
    check.dispatchEvent(new Event('change'));

    expect(encuestas.obtenerComentarios).toHaveBeenLastCalledWith(
      expect.objectContaining({ soloConComentario: false }),
    );
  });

  it('avanza de página con el botón Siguiente', () => {
    const siguiente = Array.from(fixture.nativeElement.querySelectorAll('.paginacion button')).find(
      (boton) => (boton as HTMLButtonElement).textContent?.includes('Siguiente'),
    ) as HTMLButtonElement;
    siguiente.click();

    expect(encuestas.obtenerComentarios).toHaveBeenLastCalledWith(
      expect.objectContaining({ pagina: 2 }),
    );
  });

  it('no busca si la fecha de inicio es posterior a la de término', () => {
    encuestas.obtenerResumen.mockClear();
    component.desde.set('2026-12-01');
    component.hasta.set('2026-11-01');
    component.buscar();
    fixture.detectChanges();

    expect(encuestas.obtenerResumen).not.toHaveBeenCalled();
    expect(texto()).toContain('La fecha de inicio no puede ser posterior');
  });

  function botonesVisibilidad(): HTMLButtonElement[] {
    return Array.from(fixture.nativeElement.querySelectorAll('.boton-visibilidad'));
  }

  it('muestra los comentarios ocultos sin su texto y con el botón Mostrar', () => {
    expect(texto()).toContain('Comentario oculto por el personal.');
    expect(botonesVisibilidad().map((boton) => boton.textContent?.trim())).toEqual([
      'Ocultar',
      'Ocultar',
      'Mostrar',
    ]);
    expect(botonesVisibilidad()[0].getAttribute('aria-label')).toBe(
      'Ocultar comentario de Noviembre 2026',
    );
  });

  it('oculta un comentario sin recargar la lista', () => {
    encuestas.cambiarVisibilidadComentario.mockReturnValue(of(undefined));
    encuestas.obtenerComentarios.mockClear();
    botonesVisibilidad()[0].click();
    fixture.detectChanges();

    expect(encuestas.cambiarVisibilidadComentario).toHaveBeenCalledWith(1, true);
    expect(encuestas.obtenerComentarios).not.toHaveBeenCalled();
    expect(texto()).not.toContain('Me explicaron muy bien.');
    expect(botonesVisibilidad()[0].textContent?.trim()).toBe('Mostrar');
  });

  it('vuelve a mostrar un comentario recargando la lista para obtener su texto', () => {
    encuestas.cambiarVisibilidadComentario.mockReturnValue(of(undefined));
    encuestas.obtenerComentarios.mockClear();
    botonesVisibilidad()[2].click();

    expect(encuestas.cambiarVisibilidadComentario).toHaveBeenCalledWith(3, false);
    expect(encuestas.obtenerComentarios).toHaveBeenCalledTimes(1);
  });

  it('informa si no se pudo ocultar el comentario', () => {
    encuestas.cambiarVisibilidadComentario.mockReturnValue(throwError(() => ({ status: 500 })));
    botonesVisibilidad()[0].click();
    fixture.detectChanges();

    expect(texto()).toContain('No pudimos ocultar el comentario.');
    expect(texto()).toContain('Me explicaron muy bien.');
  });

  it('muestra un error si no se puede cargar el resumen', () => {
    encuestas.obtenerResumen.mockReturnValueOnce(throwError(() => ({ status: 500 })));
    component.cargarResumen();
    fixture.detectChanges();

    expect(texto()).toContain('No pudimos cargar el resumen de encuestas.');
  });
});
