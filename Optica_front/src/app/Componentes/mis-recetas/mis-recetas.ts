import { Component, OnInit, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { HistorialGraduacionesService, RecetaHistorial } from '../historial-graduaciones/historial-graduaciones.service';
import { obtenerOrigenApi } from '../../shared/services/api-origin';

@Component({
  selector: 'app-mis-recetas',
  imports: [DatePipe],
  templateUrl: './mis-recetas.html',
  styleUrl: './mis-recetas.css',
})
export class MisRecetas implements OnInit {
  protected readonly apiBase = obtenerOrigenApi();
  private readonly historialService = inject(HistorialGraduacionesService);

  protected readonly recetas = signal<RecetaHistorial[]>([]);
  protected readonly isLoading = signal(false);
  protected readonly errorMessage = signal('');
  protected readonly yaBusco = signal(false);

  ngOnInit(): void {
    this.cargarMisRecetas();
  }

  protected cargarMisRecetas(): void {
    this.isLoading.set(true);
    this.errorMessage.set('');

    this.historialService.obtenerMisRecetas().subscribe({
      next: (resultado) => {
        this.recetas.set(resultado);
        this.isLoading.set(false);
        this.yaBusco.set(true);
      },
      error: (error: { error?: { mensaje?: string } }) => {
        this.recetas.set([]);
        this.errorMessage.set(error.error?.mensaje ?? 'No se pudo obtener tus recetas. Inténtalo nuevamente.');
        this.isLoading.set(false);
        this.yaBusco.set(true);
      },
    });
  }
}
