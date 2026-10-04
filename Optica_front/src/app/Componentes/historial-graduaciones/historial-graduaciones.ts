import { Component, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { AutocompletadoRut } from '../../shared/components/autocompletado-rut/autocompletado-rut';
import { HistorialGraduacionesService, RecetaHistorial } from './historial-graduaciones.service';

@Component({
  selector: 'app-historial-graduaciones',
  imports: [AutocompletadoRut, DatePipe],
  templateUrl: './historial-graduaciones.html',
  styleUrl: './historial-graduaciones.css',
})
export class HistorialGraduaciones {
  private readonly historialService = inject(HistorialGraduacionesService);

  protected readonly rut = signal('');
  protected readonly recetas = signal<RecetaHistorial[]>([]);
  protected readonly isLoading = signal(false);
  protected readonly errorMessage = signal('');
  protected readonly yaBusco = signal(false);

  protected buscarHistorial(): void {
    const rutValor = this.rut().trim();

    if (!rutValor) {
      this.errorMessage.set('Ingresa el RUT del cliente.');
      return;
    }

    this.isLoading.set(true);
    this.errorMessage.set('');
    this.yaBusco.set(true);

    this.historialService.obtenerHistorial(rutValor).subscribe({
      next: (resultado) => {
        this.recetas.set(resultado);
        this.isLoading.set(false);
      },
      error: (error: { error?: { mensaje?: string } }) => {
        this.recetas.set([]);
        this.errorMessage.set(error.error?.mensaje ?? 'No se pudo obtener el historial. Inténtalo nuevamente.');
        this.isLoading.set(false);
      },
    });
  }
}