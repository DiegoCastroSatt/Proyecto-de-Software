import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AvisoReposicion, ListaSolicitudesService } from './solicitudes-cliente.service';

@Component({
  selector: 'app-lista-solicitudes',
  imports: [RouterLink],
  templateUrl: './solicitudes-cliente.html',
  styleUrl: './solicitudes-cliente.css'
})
export class ListaSolicitudes implements OnInit {
  private readonly solicitudesService = inject(ListaSolicitudesService);

  protected readonly solicitudes = signal<AvisoReposicion[]>([]);
  protected readonly cargando = signal(true);
  protected readonly error = signal('');

  ngOnInit(): void {
    this.cargarSolicitudes();
  }

  protected cargarSolicitudes(): void {
    this.cargando.set(true);
    this.error.set('');
    this.solicitudesService.obtenerMisSolicitudes().subscribe({
      next: solicitudes => {
        this.solicitudes.set(solicitudes);
        this.cargando.set(false);
      },
      error: () => {
        this.error.set('No se pudieron cargar tus solicitudes. Inténtalo nuevamente.');
        this.cargando.set(false);
      }
    });
  }

  protected formatearFecha(fecha: string): string {
    return new Intl.DateTimeFormat('es-CL', {
      dateStyle: 'medium',
      timeStyle: 'short',
      timeZone: 'America/Santiago'
    }).format(new Date(fecha));
  }
}
