import { Component, OnInit, inject, signal } from '@angular/core';
import { BuscarProductoService, Producto } from '../buscar-producto/buscar-producto.service';

@Component({
  selector: 'app-consultar-stock',
  imports: [],
  templateUrl: './consultar-stock.html',
  styleUrl: './consultar-stock.css',
})
export class ConsultarStock implements OnInit {
  private readonly productoService = inject(BuscarProductoService);

  protected readonly productos = signal<Producto[]>([]);
  protected readonly busqueda = signal('');
  protected readonly cargando = signal(false);
  protected readonly error = signal('');

  ngOnInit(): void {
    this.cargarProductos();
  }

  protected cargarProductos(): void {
    this.cargando.set(true);
    this.error.set('');
    this.productoService.buscar('').subscribe({
      next: productos => {
        this.productos.set(productos);
        this.cargando.set(false);
      },
      error: () => {
        this.error.set('No se pudo consultar el stock. Intenta nuevamente.');
        this.cargando.set(false);
      }
    });
  }

  protected actualizarBusqueda(event: Event): void {
    this.busqueda.set((event.target as HTMLInputElement).value);
  }

  protected productosFiltrados(): Producto[] {
    const termino = this.busqueda().trim().toLocaleLowerCase();
    if (!termino) {
      return this.productos();
    }

    return this.productos().filter(producto =>
      `${producto.nombre} ${producto.codigo} ${producto.categoria}`.toLocaleLowerCase().includes(termino)
    );
  }
}
