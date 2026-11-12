import { Component, OnInit, inject, signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { SelectorEstrellasComponent } from '../../shared/components/selector-estrellas/selector-estrellas';
import { EncuestaPublica, EncuestasService } from '../../shared/services/encuestas.service';

type EstadoEncuesta = 'cargando' | 'formulario' | 'gracias' | 'error';
type ControlCalificacion = 'calificacionGeneral' | 'calificacionTrato' | 'calificacionPuntualidad';

export const LARGO_MAXIMO_COMENTARIO = 1000;

const MENSAJE_ENLACE_INVALIDO = 'Este enlace no es válido o la encuesta ya fue respondida.';
const MENSAJE_ENLACE_VENCIDO =
  'Este enlace venció. Las encuestas pueden responderse hasta 14 días después de recibir el correo.';

/** Control de una pregunta calificada con estrellas: obligatoria y entre 1 y 5. */
function crearControlCalificacion(): FormControl<number | null> {
  return new FormControl<number | null>(null, [
    Validators.required,
    Validators.min(1),
    Validators.max(5),
  ]);
}

@Component({
  selector: 'app-responder-encuesta',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, SelectorEstrellasComponent],
  templateUrl: './responder-encuesta.html',
  styleUrl: './responder-encuesta.css',
})
export class ResponderEncuestaComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly encuestas = inject(EncuestasService);
  private token = '';

  readonly largoMaximo = LARGO_MAXIMO_COMENTARIO;
  readonly preguntas: ReadonlyArray<{ control: ControlCalificacion; texto: string }> = [
    {
      control: 'calificacionGeneral',
      texto: 'En general, ¿qué tan satisfecho/a quedaste con tu atención?',
    },
    {
      control: 'calificacionTrato',
      texto: '¿Cómo evaluarías el trato y la amabilidad del personal?',
    },
    { control: 'calificacionPuntualidad', texto: '¿Te atendimos a la hora que tenías reservada?' },
  ];

  readonly estado = signal<EstadoEncuesta>('cargando');
  readonly mensajeError = signal('');
  readonly errorFormulario = signal('');
  readonly enviando = signal(false);
  readonly atencion = signal('');
  readonly largoComentario = signal(0);

  readonly formulario = new FormGroup({
    calificacionGeneral: crearControlCalificacion(),
    calificacionTrato: crearControlCalificacion(),
    calificacionPuntualidad: crearControlCalificacion(),
    comentario: new FormControl('', {
      nonNullable: true,
      validators: [Validators.maxLength(LARGO_MAXIMO_COMENTARIO)],
    }),
  });

  ngOnInit(): void {
    this.token = this.route.snapshot.queryParamMap.get('token')?.trim() ?? '';
    if (!this.token) {
      this.mostrarError(MENSAJE_ENLACE_INVALIDO);
      return;
    }

    this.formulario.controls.comentario.valueChanges.subscribe((valor) =>
      this.largoComentario.set(valor.length),
    );
    this.formulario.valueChanges.subscribe(() => this.errorFormulario.set(''));

    this.encuestas.obtenerEncuesta(this.token).subscribe({
      next: (encuesta) => {
        this.atencion.set(formatearAtencion(encuesta));
        this.estado.set('formulario');
      },
      error: (error: HttpErrorResponse) =>
        this.mostrarError(
          mensajeEnlace(error) ?? 'No pudimos cargar la encuesta. Inténtalo de nuevo más tarde.',
        ),
    });
  }

  enviar(): void {
    if (this.enviando()) return;
    if (this.formulario.invalid) {
      this.formulario.markAllAsTouched();
      this.errorFormulario.set(
        this.formulario.controls.comentario.invalid
          ? `El comentario puede tener hasta ${LARGO_MAXIMO_COMENTARIO} caracteres.`
          : 'Califica las tres preguntas antes de enviar.',
      );
      return;
    }

    const valores = this.formulario.getRawValue();
    const comentario = valores.comentario.trim();
    this.enviando.set(true);
    this.encuestas
      .responder(this.token, {
        calificacionGeneral: valores.calificacionGeneral!,
        calificacionTrato: valores.calificacionTrato!,
        calificacionPuntualidad: valores.calificacionPuntualidad!,
        comentario: comentario || null,
      })
      .subscribe({
        next: () => {
          this.enviando.set(false);
          this.estado.set('gracias');
        },
        error: (error: HttpErrorResponse) => {
          this.enviando.set(false);
          const mensaje = mensajeEnlace(error);
          if (mensaje) {
            this.mostrarError(mensaje);
            return;
          }
          this.errorFormulario.set(
            typeof error.error?.mensaje === 'string'
              ? error.error.mensaje
              : 'No pudimos enviar tu respuesta. Inténtalo de nuevo.',
          );
        },
      });
  }

  private mostrarError(mensaje: string): void {
    this.mensajeError.set(mensaje);
    this.estado.set('error');
  }
}

/** Traduce los estados de un enlace inutilizable a un mensaje para el cliente. */
function mensajeEnlace(error: HttpErrorResponse): string | null {
  if (error.status === 404 || error.status === 409) return MENSAJE_ENLACE_INVALIDO;
  if (error.status === 410) return MENSAJE_ENLACE_VENCIDO;
  return null;
}

/** Devuelve, por ejemplo, "martes 10 de noviembre, 10:30". */
export function formatearAtencion(encuesta: EncuestaPublica): string {
  const [anio, mes, dia] = encuesta.fechaAtencion.split('-').map(Number);
  // Se construye en hora local para que la fecha no cambie por la zona horaria.
  const fecha = new Date(anio, mes - 1, dia);
  const texto = new Intl.DateTimeFormat('es-CL', {
    weekday: 'long',
    day: 'numeric',
    month: 'long',
  }).format(fecha);
  return `${texto}, ${encuesta.horaInicio.slice(0, 5)}`;
}
