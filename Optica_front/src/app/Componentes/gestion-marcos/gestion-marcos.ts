import { RouterLink } from '@angular/router';
import { EditorMarcoComponent } from './editor-marco';
import { disenoMarcoInicial } from './diseno-marco';
import { TipoRostro, TiposRostroService } from '../../shared/services/tipos-rostro.service';
import { Component, ViewChild, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { TipoMarco, TiposMarcoService, imagenMarco, ProductoVinculableMarco } from '../../shared/services/tipos-marco.service';

@Component({
  selector: 'app-gestion-marcos',
  imports: [FormsModule, EditorMarcoComponent, RouterLink],
  templateUrl: './gestion-marcos.html',
  styleUrl: './gestion-marcos.css',
})
export class GestionMarcosComponent {
  private readonly servicio = inject(TiposMarcoService);
  readonly vinculando = signal<number | null>(null);
  readonly productos = signal<ProductoVinculableMarco[]>([]);
  readonly cargandoProductos = signal(false);
  readonly errorProductos = signal('');
  productosSeleccionados = new Set<number>();
  filtroProductos = '';
  busquedaMarcos = '';
  readonly marcos = signal<TipoMarco[]>([]);
  readonly cargando = signal(true);
  readonly guardando = signal(false);
  readonly ocupado = signal<number | null>(null);
  readonly error = signal('');
  readonly aviso = signal('');
  readonly confirmarBorrado = signal<number | null>(null);
  readonly imagenMarco = imagenMarco;
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

  get marcosFiltrados(): TipoMarco[] {
    const normalizar = (valor: string) => valor.normalize('NFD').replace(/[\u0300-\u036f]/g, '').toLocaleLowerCase().trim();
    const busqueda = normalizar(this.busquedaMarcos);
    return this.marcos().filter(marco => normalizar(marco.nombre).includes(busqueda));
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
      next: marco => {
        this.marcos.update(marcos => [...marcos, marco].sort((a, b) => a.nombre.localeCompare(b.nombre)));
        this.nombre = ''; this.categoria = 'Armazones'; this.svg = disenoMarcoInicial; this.reinicioDiseno.update(valor => valor + 1); this.archivoNombre = ''; archivo.value = '';
        this.rostrosSeleccionados = new Set(this.rostros().map(r => r.codigo));
        this.guardando.set(false);
        this.aviso.set('Tipo de marco creado. Usa «Vincular armazones» para asociar los productos y su stock.');
      },
      error: error => { this.error.set(this.mensajeError(error)); this.guardando.set(false); },
    });
  }

  alternar(marco: TipoMarco): void {
    if (this.ocupado() !== null) return;
    this.error.set(''); this.aviso.set(''); this.ocupado.set(marco.id);
    this.servicio.cambiarEstado(marco.id, !marco.activo).subscribe({
      next: () => {
        this.marcos.update(marcos => marcos.map(item => item.id === marco.id ? { ...item, activo: !item.activo } : item));
        this.ocupado.set(null);
        this.aviso.set(marco.activo ? 'Marco desactivado: ya no se mostrará en las vistas de clientes.' : 'Marco activado. Se mostrará si sus armazones vinculados tienen stock disponible.');
      },
      error: error => { this.error.set(this.mensajeError(error)); this.ocupado.set(null); },
    });
  }

  borrar(marco: TipoMarco): void {
    if (this.ocupado() !== null || this.confirmarBorrado() !== marco.id) return;
    this.error.set(''); this.aviso.set(''); this.ocupado.set(marco.id);
    this.servicio.borrar(marco.id).subscribe({
      next: () => {
        this.marcos.update(marcos => marcos.filter(item => item.id !== marco.id));
        this.ocupado.set(null); this.confirmarBorrado.set(null);
        this.aviso.set('Tipo de marco borrado. Los productos y su stock se conservaron.');
      },
      error: error => { this.error.set(this.mensajeError(error)); this.ocupado.set(null); },
    });
  }

  abrirVinculos(marco: TipoMarco): void {
    if (this.ocupado() !== null) return;
    this.vinculando.set(marco.id); this.confirmarBorrado.set(null); this.filtroProductos = '';
    this.productos.set([]); this.productosSeleccionados = new Set();
    this.cargandoProductos.set(true); this.errorProductos.set('');
    this.servicio.productosVinculables().subscribe({
      next: productos => {
        if (this.vinculando() !== marco.id) return;
        this.productos.set(productos);
        this.productosSeleccionados = new Set(productos.filter(p => p.idTipoMarco === marco.id).map(p => p.id));
        this.cargandoProductos.set(false);
      },
      error: () => {
        if (this.vinculando() !== marco.id) return;
        this.errorProductos.set('No se pudieron cargar los productos. Vuelve a intentarlo.'); this.cargandoProductos.set(false);
      },
    });
  }

  get productosFiltrados(): ProductoVinculableMarco[] {
    const filtro = this.filtroProductos.trim().toLocaleLowerCase();
    return this.productos().filter(p => `${p.nombre} ${p.codigo} ${p.categoria} ${p.marca}`.toLocaleLowerCase().includes(filtro));
  }

  marcarProducto(id: number, marcado: boolean): void {
    if (marcado) this.productosSeleccionados.add(id); else this.productosSeleccionados.delete(id);
  }

  nombreMarco(id: number): string { return this.marcos().find(m => m.id === id)?.nombre ?? 'otro marco'; }

  guardarVinculos(marco: TipoMarco): void {
    if (this.ocupado() !== null || this.cargandoProductos() || this.errorProductos() || this.vinculando() !== marco.id) return;
    this.ocupado.set(marco.id); this.error.set(''); this.aviso.set('');
    this.servicio.vincularProductos(marco.id, [...this.productosSeleccionados]).subscribe({
      next: marcos => {
        this.marcos.set(marcos); this.vinculando.set(null); this.ocupado.set(null);
        this.aviso.set('Armazones vinculados. El stock se obtiene de esos mismos productos; no se duplicaron existencias.');
      },
      error: error => { this.error.set(this.mensajeError(error)); this.ocupado.set(null); },
    });
  }

  private mensajeError(error: HttpErrorResponse): string {
    return error.error?.mensaje || 'No se pudo guardar el cambio. Vuelve a intentarlo.';
  }
}
