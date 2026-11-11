import { DatePipe } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import {
  BuscarProductoService,
  HistorialStockPaginado,
  Producto
} from '../buscar-producto/buscar-producto.service';

@Component({
  selector: 'app-historial-stock',
  imports: [DatePipe],
  templateUrl: './historial-stock.html',
  styleUrl: './historial-stock.css',
})
export class HistorialStock implements OnInit {
  private readonly productoService = inject(BuscarProductoService);
  private readonly tamanoPagina = 20;

  protected readonly productos = signal<Producto[]>([]);
  protected readonly busquedaProducto = signal('');
  protected readonly productosCoincidentes = computed(() => {
    const termino = this.busquedaProducto().trim().toLocaleLowerCase();
    if (!termino || this.productoSeleccionadoId() !== null) {
      return [];
    }

    return this.productos()
      .filter(producto => `${producto.nombre} ${producto.codigo}`.toLocaleLowerCase().includes(termino))
      .slice(0, 8);
  });
  protected readonly productoSeleccionadoId = signal<number | null>(null);
  protected readonly historial = signal<HistorialStockPaginado | null>(null);
  protected readonly pagina = signal(1);
  protected readonly cargandoProductos = signal(false);
  protected readonly cargandoHistorial = signal(false);
  protected readonly errorProductos = signal('');
  protected readonly errorHistorial = signal('');

  ngOnInit(): void {
    this.cargarProductos();
    this.cargarHistorial(null, 1);
  }

  protected cargarProductos(): void {
    this.cargandoProductos.set(true);
    this.errorProductos.set('');
    this.productoService.buscar('').subscribe({
      next: productos => {
        this.productos.set(productos);
        this.cargandoProductos.set(false);
      },
      error: (respuesta: { error?: { mensaje?: string } }) => {
        this.errorProductos.set(respuesta.error?.mensaje ?? 'No se pudieron cargar los productos.');
        this.cargandoProductos.set(false);
      }
    });
  }

  protected buscarProducto(event: Event): void {
    const termino = (event.target as HTMLInputElement).value;
    this.busquedaProducto.set(termino);

    const idSeleccionado = this.productoSeleccionadoId();
    if (idSeleccionado !== null) {
      const productoSeleccionado = this.productos().find(producto => producto.id === idSeleccionado);
      if (!productoSeleccionado || termino !== this.etiquetaProducto(productoSeleccionado)) {
        this.productoSeleccionadoId.set(null);
        this.cargarHistorial(null, 1);
      }
    }
  }

  protected seleccionarProducto(producto: Producto): void {
    this.productoSeleccionadoId.set(producto.id);
    this.busquedaProducto.set(this.etiquetaProducto(producto));
    this.cargarHistorial(producto.id, 1);
  }

  protected limpiarFiltro(): void {
    this.productoSeleccionadoId.set(null);
    this.busquedaProducto.set('');
    this.cargarHistorial(null, 1);
  }

  protected etiquetaProducto(producto: Producto): string {
    return `${producto.nombre} · ${producto.codigo}`;
  }

  protected nombreProducto(idProducto: number): string {
    const producto = this.productos().find(item => item.id === idProducto);
    return producto ? this.etiquetaProducto(producto) : `Producto ${idProducto}`;
  }

  protected cambiarPagina(pagina: number): void {
    const historial = this.historial();
    if (
      !historial ||
      this.cargandoHistorial() ||
      pagina < 1 ||
      pagina > historial.totalPaginas ||
      pagina === this.pagina()
    ) {
      return;
    }

    this.cargarHistorial(this.productoSeleccionadoId(), pagina);
  }

  protected reintentarHistorial(): void {
    if (!this.cargandoHistorial()) {
      this.cargarHistorial(this.productoSeleccionadoId(), this.pagina());
    }
  }

  private cargarHistorial(idProducto: number | null, pagina: number): void {
    if (this.cargandoHistorial()) {
      return;
    }

    this.pagina.set(pagina);
    this.historial.set(null);
    this.errorHistorial.set('');
    this.cargandoHistorial.set(true);
    const consulta = idProducto === null
      ? this.productoService.consultarHistorialStockReciente(pagina, this.tamanoPagina)
      : this.productoService.consultarHistorialStock(idProducto, pagina, this.tamanoPagina);
    consulta.subscribe({
      next: historial => {
        this.historial.set(historial);
        this.cargandoHistorial.set(false);
      },
      error: (respuesta: { error?: { mensaje?: string } }) => {
        this.errorHistorial.set(respuesta.error?.mensaje ?? 'No se pudo cargar el historial de stock.');
        this.cargandoHistorial.set(false);
      }
    });
  }
}
