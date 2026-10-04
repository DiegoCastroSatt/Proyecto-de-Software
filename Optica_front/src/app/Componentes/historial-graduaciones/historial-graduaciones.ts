import { Component, computed, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { AutocompletadoRut } from '../../shared/components/autocompletado-rut/autocompletado-rut';
import { GraduacionHistorial, HistorialGraduacionesService, RecetaHistorial } from './historial-graduaciones.service';

interface FilaComparacion {
  campo: string;
  valorA: number | null;
  valorB: number | null;
  cambio: boolean;
}

interface GrupoComparacionOjo {
  ojo: string;
  etiqueta: string;
  filas: FilaComparacion[];
}

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
  protected readonly seleccionadas = signal<RecetaHistorial[]>([]);

  protected readonly comparacion = computed(() => {
    const [recetaA, recetaB] = [...this.seleccionadas()].sort(
      (a, b) => new Date(a.fecha).getTime() - new Date(b.fecha).getTime(),
    );

    if (!recetaA || !recetaB) {
      return null;
    }

    const ojos: Array<{ ojo: string; etiqueta: string }> = [
      { ojo: 'OD', etiqueta: 'Ojo derecho (OD)' },
      { ojo: 'OI', etiqueta: 'Ojo izquierdo (OI)' },
    ];

    const porOjo: GrupoComparacionOjo[] = ojos.map(({ ojo, etiqueta }) => {
      const graduacionA = this.buscarGraduacion(recetaA, ojo);
      const graduacionB = this.buscarGraduacion(recetaB, ojo);

      const campos: Array<{ campo: string; valorA: number | null; valorB: number | null }> = [
        { campo: 'Esfera', valorA: graduacionA?.esfera ?? null, valorB: graduacionB?.esfera ?? null },
        { campo: 'Cilindro', valorA: graduacionA?.cilindro ?? null, valorB: graduacionB?.cilindro ?? null },
        { campo: 'Eje', valorA: graduacionA?.eje ?? null, valorB: graduacionB?.eje ?? null },
        { campo: 'Adición', valorA: graduacionA?.adicion ?? null, valorB: graduacionB?.adicion ?? null },
      ];

      return {
        ojo,
        etiqueta,
        filas: campos.map((campo) => ({ ...campo, cambio: campo.valorA !== campo.valorB })),
      };
    });

    return { fechaA: recetaA.fecha, fechaB: recetaB.fecha, porOjo };
  });

  protected buscarHistorial(): void {
    const rutValor = this.rut().trim();

    if (!rutValor) {
      this.errorMessage.set('Ingresa el RUT del cliente.');
      return;
    }

    this.isLoading.set(true);
    this.errorMessage.set('');
    this.yaBusco.set(true);
    this.seleccionadas.set([]);

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
  
  protected estaSeleccionada(receta: RecetaHistorial): boolean {
    return this.seleccionadas().some((r) => r.id === receta.id);
  }

  protected toggleSeleccion(receta: RecetaHistorial): void {
    if (this.estaSeleccionada(receta)) {
      this.seleccionadas.set(this.seleccionadas().filter((r) => r.id !== receta.id));
      return;
    }

    if (this.seleccionadas().length >= 2) {
      return;
    }

    this.seleccionadas.set([...this.seleccionadas(), receta]);
  }

  protected limpiarSeleccion(): void {
    this.seleccionadas.set([]);
  }

  private buscarGraduacion(receta: RecetaHistorial, ojo: string): GraduacionHistorial | undefined {
    return receta.graduaciones.find((g) => g.ojo === ojo);
  }
}