import { CommonModule, isPlatformBrowser } from '@angular/common';
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
