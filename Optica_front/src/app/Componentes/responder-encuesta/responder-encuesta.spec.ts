import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { ResponderEncuestaComponent, formatearAtencion } from './responder-encuesta';
import { EncuestasService } from '../../shared/services/encuestas.service';

describe('ResponderEncuestaComponent', () => {
  let fixture: ComponentFixture<ResponderEncuestaComponent>;
  let token: string | null;
  const encuestas = { obtenerEncuesta: vi.fn(), responder: vi.fn() };

  beforeEach(async () => {
    vi.resetAllMocks();
    token = 'token-seguro';
    encuestas.obtenerEncuesta.mockReturnValue(
      of({ fechaAtencion: '2026-11-10', horaInicio: '10:30:00' }),
    );
    encuestas.responder.mockReturnValue(of({ mensaje: 'Gracias.' }));
    await TestBed.configureTestingModule({
      imports: [ResponderEncuestaComponent],
      providers: [
        provideRouter([]),
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { queryParamMap: { get: () => token } } },
        },
        { provide: EncuestasService, useValue: encuestas },
      ],
    }).compileComponents();
  });

  function crearComponente(): void {
    fixture = TestBed.createComponent(ResponderEncuestaComponent);
    fixture.detectChanges();
  }

  function texto(): string {
    return fixture.nativeElement.textContent as string;
  }

  function calificarTodo(): void {
    const grupos = fixture.nativeElement.querySelectorAll(
      '[role="radiogroup"]',
    ) as NodeListOf<HTMLElement>;
    grupos.forEach((grupo, indice) => {
      (grupo.querySelectorAll('button')[indice + 2] as HTMLButtonElement).click();
    });
    fixture.detectChanges();
  }

  function enviar(): void {
    (fixture.nativeElement.querySelector('form') as HTMLFormElement).dispatchEvent(
      new Event('submit', { bubbles: true, cancelable: true }),
    );
    fixture.detectChanges();
  }

  it('carga la encuesta con el token y muestra la fecha de la atención y las tres preguntas', () => {
    token = ' token-seguro ';
    crearComponente();

    expect(encuestas.obtenerEncuesta).toHaveBeenCalledWith('token-seguro');
    expect(texto()).toContain('Cuéntanos cómo te fue');
    expect(texto()).toContain('10 de noviembre, 10:30');
    expect(fixture.nativeElement.querySelectorAll('[role="radiogroup"]').length).toBe(3);
    expect(texto()).toContain('¿Te atendimos a la hora que tenías reservada?');
    expect(texto()).toContain('Otros comentarios');
  });

  it('no llama a la API si falta el token', () => {
    token = '  ';
    crearComponente();

    expect(encuestas.obtenerEncuesta).not.toHaveBeenCalled();
    expect(texto()).toContain('Enlace no disponible');
    expect(texto()).toContain('no es válido');
  });

  it('informa cuando el enlace venció', () => {
    encuestas.obtenerEncuesta.mockReturnValueOnce(throwError(() => ({ status: 410 })));
    crearComponente();

    expect(texto()).toContain('Este enlace venció');
  });

  it('informa cuando el enlace no existe o ya fue usado', () => {
    encuestas.obtenerEncuesta.mockReturnValueOnce(throwError(() => ({ status: 404 })));
    crearComponente();

    expect(texto()).toContain('ya fue respondida');
  });

  it('no envía si falta calificar alguna pregunta', () => {
    crearComponente();
    enviar();

    expect(encuestas.responder).not.toHaveBeenCalled();
    expect(texto()).toContain('Califica las tres preguntas antes de enviar.');
  });

  it('envía las calificaciones y el comentario y muestra el agradecimiento', () => {
    crearComponente();
    calificarTodo();
    const comentario = fixture.nativeElement.querySelector('textarea') as HTMLTextAreaElement;
    comentario.value = '  Muy buena atención  ';
    comentario.dispatchEvent(new Event('input'));
    fixture.detectChanges();

    expect(texto()).toContain('22 / 1000');
    enviar();

    expect(encuestas.responder).toHaveBeenCalledWith('token-seguro', {
      calificacionGeneral: 3,
      calificacionTrato: 4,
      calificacionPuntualidad: 5,
      comentario: 'Muy buena atención',
    });
    expect(texto()).toContain('Gracias por tu opinión');
    expect(texto()).toContain('anónima');
  });

  it('envía el comentario vacío como null', () => {
    crearComponente();
    calificarTodo();
    enviar();

    expect(encuestas.responder.mock.calls[0][1].comentario).toBeNull();
  });

  it('muestra que la encuesta ya fue respondida si la API rechaza un segundo envío', () => {
    encuestas.responder.mockReturnValueOnce(throwError(() => ({ status: 409 })));
    crearComponente();
    calificarTodo();
    enviar();

    expect(texto()).toContain('Enlace no disponible');
    expect(texto()).toContain('ya fue respondida');
  });

  it('mantiene el formulario y muestra el error si la API rechaza los datos', () => {
    encuestas.responder.mockReturnValueOnce(
      throwError(() => ({ status: 400, error: { mensaje: 'Calificación no válida.' } })),
    );
    crearComponente();
    calificarTodo();
    enviar();

    expect(texto()).toContain('Calificación no válida.');
    expect(fixture.nativeElement.querySelector('form')).not.toBeNull();
  });
});

describe('formatearAtencion', () => {
  it('no cambia el día por la zona horaria', () => {
    expect(formatearAtencion({ fechaAtencion: '2026-11-01', horaInicio: '09:00' })).toContain(
      '1 de noviembre, 09:00',
    );
  });
});
