import { RouterLink } from '@angular/router';
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { TipoMarco, TiposMarcoService, imagenMarco, ProductoVinculableMarco } from '../../shared/services/tipos-marco.service';

@Component({
  selector: 'app-gestion-marcos',
  imports: [FormsModule, RouterLink],
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
  readonly ocupado = signal<number | null>(null);
  readonly error = signal('');
  readonly aviso = signal('');
  readonly confirmarBorrado = signal<number | null>(null);
  readonly imagenMarco = imagenMarco;
  constructor() { this.cargar(); }

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
