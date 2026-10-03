import { CommonModule, isPlatformBrowser } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, DestroyRef, inject, OnInit, PLATFORM_ID, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { AgendaReservasService, ReservaAgenda } from './agenda-reservas.service';

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
    this.reservaSeleccionada.set(reserva);
  }

  cerrarDetalle(): void {
    this.reservaSeleccionada.set(null);
    this.mensajeCancelacion.set(null);
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
