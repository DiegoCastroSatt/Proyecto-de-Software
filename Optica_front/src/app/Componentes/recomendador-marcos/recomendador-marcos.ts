import { TipoRostro, TiposRostroService, imagenRostro } from '../../shared/services/tipos-rostro.service';
import { TipoMarco, imagenMarco } from '../../shared/services/tipos-marco.service';
import { DisponibilidadMarcosService } from '../../shared/services/disponibilidad-marcos.service';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-recomendador-marcos',
  imports: [RouterLink],
  templateUrl: './recomendador-marcos.html',
  styleUrl: './recomendador-marcos.css',
})
export class RecomendadorMarcosComponent {
  readonly rostros = signal<TipoRostro[]>([]);
  readonly cargandoRostros = signal(true);
  readonly errorRostros = signal('');
  readonly imagenRostro = imagenRostro;

  readonly imagenMarco = imagenMarco;
  readonly disponibles = signal<TipoMarco[]>([]);
  readonly cargando = signal(true);
  readonly errorStock = signal('');

  constructor() {
    inject(TiposRostroService).activos().pipe(takeUntilDestroyed()).subscribe({
      next: rostros => { this.rostros.set(rostros); this.cargandoRostros.set(false); },
      error: () => { this.errorRostros.set('No se pudieron cargar los tipos de rostro. Vuelve a intentarlo.'); this.cargandoRostros.set(false); },
    });
    inject(DisponibilidadMarcosService).consultar().pipe(takeUntilDestroyed()).subscribe({
      next: tipos => { this.disponibles.set(tipos); this.cargando.set(false); },
      error: () => {
        this.errorStock.set('No se pudo consultar el stock de marcos. Vuelve a intentarlo.');
        this.cargando.set(false);
      },
    });
  }

  readonly seleccionado = signal<string | null>(null);
  seleccionar(id: string): void {
    this.seleccionado.set(id);
  }
  get recomendacion() {
    const rostro = this.rostros().find((rostro) => rostro.codigo === this.seleccionado());
    return rostro ? { ...rostro, marcos: this.disponibles().filter(marco => marco.rostros.includes(rostro.codigo)) } : undefined;
  }
}
