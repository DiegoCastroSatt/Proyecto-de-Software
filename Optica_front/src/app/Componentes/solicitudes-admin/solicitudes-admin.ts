import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { AvisoReposicionAdmin, SolicitudesAdminService } from './solicitudes-admin.service';

type FiltroEstado = 'Todas' | 'Abierta' | 'Cerrada';

@Component({
  selector: 'app-solicitudes',
  imports: [],
  templateUrl: './solicitudes-admin.html',
  styleUrl: './solicitudes-admin.css'
})
export class Solicitudes implements OnInit {
  private readonly solicitudesService = inject(SolicitudesAdminService);

  protected readonly solicitudes = signal<AvisoReposicionAdmin[]>([]);
  protected readonly cargando = signal(true);
  protected readonly error = signal('');
  protected readonly filtroEstado = signal<FiltroEstado>('Todas');
  protected readonly abiertas = computed(() => this.solicitudes().filter(item => item.estado === 'Abierta').length);
  protected readonly cerradas = computed(() => this.solicitudes().filter(item => item.estado === 'Cerrada').length);
  protected readonly solicitudesFiltradas = computed(() => {
    const filtro = this.filtroEstado();
    return filtro === 'Todas'
      ? this.solicitudes()
      : this.solicitudes().filter(item => item.estado === filtro);
  });

  ngOnInit(): void {
    this.cargarSolicitudes();
  }

  protected cargarSolicitudes(): void {
    this.cargando.set(true);
    this.error.set('');
    this.solicitudesService.obtenerTodas().subscribe({
      next: solicitudes => {
        this.solicitudes.set(solicitudes);
        this.cargando.set(false);
      },
      error: () => {
        this.error.set('No se pudieron cargar las solicitudes. Inténtalo nuevamente.');
        this.cargando.set(false);
      }
    });
  }

  protected cambiarFiltro(event: Event): void {
    const valor = (event.target as HTMLSelectElement).value;
    if (valor === 'Todas' || valor === 'Abierta' || valor === 'Cerrada') {
      this.filtroEstado.set(valor);
    }
  }

  protected formatearFecha(fecha: string): string {
    return new Intl.DateTimeFormat('es-CL', {
      dateStyle: 'medium',
      timeStyle: 'short',
      timeZone: 'America/Santiago'
    }).format(new Date(fecha));
  }
}
