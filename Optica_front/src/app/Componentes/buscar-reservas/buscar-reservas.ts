import { CommonModule, isPlatformBrowser } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, DestroyRef, inject, OnInit, PLATFORM_ID, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { AgendaReservasService, HorarioDisponible, ReservaAgenda } from './agenda-reservas.service';

@Component({
  selector: 'app-buscar-reservas',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './buscar-reservas.html',
  styleUrl: './buscar-reservas.css',
})
export class BuscarReservas implements OnInit {
  private readonly agendaService = inject(AgendaReservasService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly isBrowser = isPlatformBrowser(inject(PLATFORM_ID));

  readonly cargando = signal(true);
  readonly error = signal<string | null>(null);
  readonly mostrarHistorial = signal(false);
  readonly reservas = signal<ReservaAgenda[]>([]);
  readonly reservaSeleccionada = signal<ReservaAgenda | null>(null);
  readonly cancelando = signal(false);
  readonly mensajeCancelacion = signal<string | null>(null);
  readonly mensajeExitoReprogramacion = signal<string | null>(null);
  readonly mostrarReprogramacion = signal(false);
  readonly fechaNueva = signal('');
  readonly horariosDisponibles = signal<HorarioDisponible[]>([]);
  readonly horarioSeleccionado = signal<number | null>(null);
  readonly cargandoHorarios = signal(false);
  readonly reprogramando = signal(false);
  readonly mensajeReprogramacion = signal<string | null>(null);

  ngOnInit(): void {
    this.cargarReservas(false);
    if (this.isBrowser) {
      const refreshId = setInterval(() => this.cargarReservas(this.mostrarHistorial()), 60_000);
      this.destroyRef.onDestroy(() => clearInterval(refreshId));
    }
  }

  mostrarPorAtender(): void {
    this.mostrarHistorial.set(false);
    this.reservaSeleccionada.set(null);
    this.cargarReservas(false);
  }

  mostrarAtendidas(): void {
    this.mostrarHistorial.set(true);
    this.reservaSeleccionada.set(null);
    this.cargarReservas(true);
  }

  abrirDetalle(reserva: ReservaAgenda): void {
    this.mensajeExitoReprogramacion.set(null);
    this.reservaSeleccionada.set(reserva);
  }

  cerrarDetalle(): void {
    if (this.reprogramando()) return;
    this.reservaSeleccionada.set(null);
    this.mensajeCancelacion.set(null);
    this.cerrarReprogramacion();
  }

  abrirReprogramacion(reserva: ReservaAgenda): void {
    if (reserva.estado === 'Realizada' || reserva.estado === 'Cancelada') return;
    this.fechaNueva.set(reserva.fecha.slice(0, 10));
    this.horarioSeleccionado.set(null);
    this.horariosDisponibles.set([]);
    this.mensajeReprogramacion.set(null);
    this.mostrarReprogramacion.set(true);
    this.cargarDisponibilidad(reserva.id);
  }

  cerrarReprogramacion(): void {
    if (this.reprogramando()) return;
    this.mostrarReprogramacion.set(false);
    this.mensajeReprogramacion.set(null);
  }

  cambiarFecha(fecha: string, idReserva: number): void {
    this.fechaNueva.set(fecha);
    this.horarioSeleccionado.set(null);
    this.cargarDisponibilidad(idReserva);
  }

  confirmarReprogramacion(reserva: ReservaAgenda): void {
    const idHorario = this.horarioSeleccionado();
    if (!idHorario || this.reprogramando()) return;
    this.reprogramando.set(true);
    this.mensajeReprogramacion.set(null);
    this.agendaService.reprogramar(reserva.id, idHorario).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.reprogramando.set(false);
        this.mostrarReprogramacion.set(false);
        const horario = this.horariosDisponibles().find(item => item.idHorario === idHorario);
        if (horario) {
          const actualizada = { ...reserva, fecha: horario.fecha, horaInicio: horario.hora, horaFin: horario.horaFin };
          this.reservaSeleccionada.set(actualizada);
          this.reservas.update(items => items.map(item => item.id === reserva.id ? actualizada : item));
        }
        this.mensajeExitoReprogramacion.set('La reserva fue reprogramada correctamente.');
      },
      error: (error: HttpErrorResponse) => {
        this.reprogramando.set(false);
        this.horarioSeleccionado.set(null);
        this.mensajeReprogramacion.set(error.error?.mensaje ?? 'No se pudo reprogramar la reserva. Actualiza la disponibilidad e inténtalo nuevamente.');
        this.cargarDisponibilidad(reserva.id, true);
      },
    });
  }

  private cargarDisponibilidad(idReserva: number, conservarMensaje = false): void {
    const fecha = this.fechaNueva();
    if (!fecha) {
      this.horariosDisponibles.set([]);
      return;
    }
    this.cargandoHorarios.set(true);
    if (!conservarMensaje) this.mensajeReprogramacion.set(null);
    this.agendaService.disponibles(fecha, idReserva).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (horarios) => {
        this.horariosDisponibles.set(horarios);
        this.cargandoHorarios.set(false);
      },
      error: (error: HttpErrorResponse) => {
        this.horariosDisponibles.set([]);
        this.cargandoHorarios.set(false);
        this.mensajeReprogramacion.set(error.error?.mensaje ?? 'No se pudo cargar la disponibilidad.');
      },
    });
  }

  fechaMinima(): string {
    const hoy = new Date();
    const mes = String(hoy.getMonth() + 1).padStart(2, '0');
    const dia = String(hoy.getDate()).padStart(2, '0');
    return `${hoy.getFullYear()}-${mes}-${dia}`;
  }

  cancelarReserva(reserva: ReservaAgenda): void {
    if (!this.isBrowser || this.cancelando() || reserva.estado === 'Cancelada' || reserva.estado === 'Realizada') return;

    const fecha = new Date(`${reserva.fecha.slice(0, 10)}T${reserva.horaInicio}`);
    const fechaTexto = new Intl.DateTimeFormat('es-CL', { dateStyle: 'long' }).format(fecha);
    const horaTexto = reserva.horaInicio.slice(0, 5);
    if (!window.confirm(`¿Confirmas cancelar la hora de ${reserva.nombreCliente} del ${fechaTexto} a las ${horaTexto}? El horario quedará disponible para otra persona.`)) return;

    this.cancelando.set(true);
    this.mensajeCancelacion.set(null);
    this.agendaService.cancelar(reserva.id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.cancelando.set(false);
        this.cerrarDetalle();
        this.cargarReservas(this.mostrarHistorial());
      },
      error: (error: HttpErrorResponse) => {
        this.cancelando.set(false);
        this.mensajeCancelacion.set(error.error?.mensaje ?? 'No se pudo cancelar la hora. Actualiza la agenda e inténtalo nuevamente.');
        this.cargarReservas(this.mostrarHistorial());
      },
    });
  }

  private cargarReservas(historialAtendidas: boolean): void {
    this.cargando.set(true);
    this.error.set(null);

    this.agendaService.listar(historialAtendidas).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (reservas) => {
        this.reservas.set(reservas);
        this.cargando.set(false);
      },
      error: () => {
        this.error.set('No se pudieron cargar las reservas. Inténtalo nuevamente.');
        this.cargando.set(false);
      },
    });
  }
}
