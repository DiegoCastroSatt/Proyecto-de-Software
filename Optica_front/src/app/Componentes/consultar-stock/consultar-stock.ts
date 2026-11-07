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
  protected readonly errorStock = signal('');
  protected readonly productoEditando = signal<number | null>(null);
  protected readonly borradorStock = signal({ stock: 0, stockMinimo: 0 });
  protected readonly motivoCambio = signal('');
  protected readonly guardandoStock = signal(false);

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

  protected editarStock(producto: Producto): void {
    this.errorStock.set('');
    this.borradorStock.set({ stock: producto.stock, stockMinimo: producto.stockMinimo });
    this.motivoCambio.set('');
    this.productoEditando.set(producto.id);
  }

  protected actualizarMotivoCambio(event: Event): void {
    this.motivoCambio.set((event.target as HTMLInputElement).value);
  }

  protected actualizarBorradorStock(campo: 'stock' | 'stockMinimo', event: Event): void {
    const valor = (event.target as HTMLInputElement).valueAsNumber;
    this.borradorStock.update(borrador => ({ ...borrador, [campo]: valor }));
  }

  protected cancelarEdicionStock(): void {
    this.productoEditando.set(null);
    this.motivoCambio.set('');
    this.errorStock.set('');
  }

  protected guardarStock(producto: Producto): void {
    const { stock, stockMinimo } = this.borradorStock();
    if (!Number.isInteger(stock) || stock < 0 || !Number.isInteger(stockMinimo) || stockMinimo < 0) {
      this.errorStock.set('Ingresa cantidades enteras iguales o mayores a cero.');
      return;
    }
    const motivoCambio = this.motivoCambio().trim();
    if (!motivoCambio || motivoCambio.length > 255) {
      this.errorStock.set('Ingresa el motivo del cambio (máximo 255 caracteres).');
      return;
    }

    this.guardandoStock.set(true);
    this.errorStock.set('');
    this.productoService.actualizarStock(producto.id, stock, stockMinimo, motivoCambio).subscribe({
      next: actualizado => {
        this.productos.update(productos => productos.map(item => item.id === actualizado.id ? actualizado : item));
        this.productoEditando.set(null);
        this.motivoCambio.set('');
        this.guardandoStock.set(false);
      },
      error: (respuesta: { error?: { mensaje?: string } }) => {
        this.errorStock.set(respuesta.error?.mensaje ?? 'No se pudo actualizar el stock. Intenta nuevamente.');
        this.guardandoStock.set(false);
      }
    });
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
