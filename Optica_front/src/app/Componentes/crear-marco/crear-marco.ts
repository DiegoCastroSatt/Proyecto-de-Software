import { RouterLink } from '@angular/router';
import { EditorMarcoComponent } from '../editor-marco/editor-marco';
import { disenoMarcoInicial } from '../diseño-marco/diseno-marco';
import { TipoRostro, TiposRostroService } from '../../shared/services/tipos-rostro.service';
import { Component, ViewChild, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { TipoMarco, TiposMarcoService } from '../../shared/services/tipos-marco.service';

@Component({
  selector: 'app-crear-marco',
  imports: [FormsModule, EditorMarcoComponent, RouterLink],
  templateUrl: './crear-marco.html',
  styleUrl: './crear-marco.css',
})
export class CrearMarcoComponent {
  private readonly servicio = inject(TiposMarcoService);
  readonly marcos = signal<TipoMarco[]>([]);
  readonly cargando = signal(true);
  readonly guardando = signal(false);
  readonly error = signal('');
  readonly aviso = signal('');
  readonly rostros = signal<TipoRostro[]>([]);
  readonly cargandoRostros = signal(true);
  nombre = '';
  categoria = 'Armazones';
  svg = disenoMarcoInicial;
  readonly reinicioDiseno = signal(0);
  readonly cargandoDiseno = signal(false);
  @ViewChild(EditorMarcoComponent) editor?: EditorMarcoComponent;
  archivoNombre = '';
  rostrosSeleccionados = new Set<string>();
  readonly leyendoArchivo = signal(false);

  constructor() {
    this.cargar();
    inject(TiposRostroService).activos().subscribe({
      next: rostros => { this.rostros.set(rostros); this.rostrosSeleccionados = new Set(rostros.map(r => r.codigo)); this.cargandoRostros.set(false); },
      error: () => { this.error.set('No se pudieron cargar los rostros disponibles.'); this.cargandoRostros.set(false); },
    });
  }

  cargar(): void {
    this.cargando.set(true);
    this.servicio.listar().subscribe({
      next: marcos => { this.marcos.set(marcos); this.cargando.set(false); },
      error: () => { this.error.set('No se pudieron cargar los marcos.'); this.cargando.set(false); },
    });
  }

  async elegirSvg(event: Event): Promise<void> {
    const input = event.target as HTMLInputElement;
    const archivo = input.files?.[0];
    this.archivoNombre = '';
    if (!archivo) return;
    if (!archivo.name.toLowerCase().endsWith('.svg') || archivo.size > 100_000) {
      this.error.set('Selecciona un archivo SVG de hasta 100 KB.');
      input.value = '';
      return;
    }
    this.leyendoArchivo.set(true);
    this.error.set('');
    try {
      const svg = await archivo.text();
      this.servicio.validarDiseno(svg).subscribe({
        next: resultado => {
          this.editor?.usarDiseno(resultado.svg); this.archivoNombre = archivo.name; this.leyendoArchivo.set(false);
        },
        error: error => { this.error.set(this.mensajeError(error)); this.leyendoArchivo.set(false); },
      });
    }
    catch { this.error.set('No se pudo leer la ilustración.'); this.leyendoArchivo.set(false); }

  }

  cambiarRostro(id: string, marcado: boolean): void {
    if (marcado) this.rostrosSeleccionados.add(id);
    else this.rostrosSeleccionados.delete(id);
  }

  crear(archivo: HTMLInputElement): void {
    if (this.guardando() || this.leyendoArchivo() || this.cargandoDiseno()) return;
    this.error.set(''); this.aviso.set('');
    if (!this.nombre.trim() || !this.svg || !this.rostrosSeleccionados.size) {
      this.error.set('Completa el nombre, el diseño y al menos un rostro recomendado.'); return;
    }
    this.guardando.set(true);
    this.servicio.crear({ nombre: this.nombre.trim(), categoria: this.categoria.trim(), svg: this.svg,
      rostros: [...this.rostrosSeleccionados] }).subscribe({
      next: () => {
        this.nombre = ''; this.categoria = 'Armazones'; this.svg = disenoMarcoInicial; this.reinicioDiseno.update(valor => valor + 1); this.archivoNombre = ''; archivo.value = '';
        this.rostrosSeleccionados = new Set(this.rostros().map(r => r.codigo));
        this.guardando.set(false);
        this.aviso.set('Marco creado. Vuelve a la gestión para vincular sus armazones y stock.');
      },
      error: error => { this.error.set(this.mensajeError(error)); this.guardando.set(false); },
    });
  }

  private mensajeError(error: HttpErrorResponse): string {
    return error.error?.mensaje || 'No se pudo guardar el cambio. Vuelve a intentarlo.';
  }
}
