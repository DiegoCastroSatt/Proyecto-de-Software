import { Component, signal, computed, ElementRef, ViewChild, AfterViewInit, DestroyRef, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Subject, timer, of, switchMap, catchError, map } from 'rxjs';
import { DecimalPipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RegistroVentaService, ProductoCaja } from './registro-venta.service';

@Component({
  selector: 'app-registro-venta',
  imports: [ReactiveFormsModule, DecimalPipe, RouterLink],
  templateUrl: './registro-venta.html',
  styleUrl: './registro-venta.css'
})
export class RegistroVentaComponent implements AfterViewInit {
  private readonly destroyRef = inject(DestroyRef);
  private readonly consultas = new Subject<string>();
  protected readonly terminoBusqueda = signal('');
  protected readonly coincidencias = signal<ProductoCaja[]>([]);
  protected readonly buscando = signal(false);
  protected readonly errorBusqueda = signal('');
  protected buscarProductos(termino: string): void {
    this.terminoBusqueda.set(termino);
    this.coincidencias.set([]);
    this.errorBusqueda.set('');
    this.buscando.set(!!termino.trim());
    this.consultas.next(termino.trim());
  }
  protected disponibles(producto: ProductoCaja): number {
    return Math.max(0, producto.stock - (this.productos().find(p => p.idProducto === producto.idProducto)?.cantidad ?? 0));
  }
  protected seleccionarProducto(producto: ProductoCaja): void {
    if (this.guardando() || !this.disponibles(producto)) return;
    this.formulario.controls.codigoProducto.setValue(producto.codigoProducto);
    this.agregar();
  }
  protected readonly productos = signal<ProductoCaja[]>([]);
  @ViewChild('codigo') private codigoInput?: ElementRef<HTMLInputElement>;
  protected readonly pendientes = signal(0);
  protected readonly total = computed(() => this.productos().reduce((s, p) => s + p.precio * p.cantidad, 0));
  protected readonly unidades = computed(() => this.productos().reduce((s, p) => s + p.cantidad, 0));
  private cola: { codigo: string; cantidad: number }[] = [];
  private procesando = false;
  ngAfterViewInit(): void { this.enfocar(); }
  private enfocar(): void { this.codigoInput?.nativeElement.focus(); }
  protected readonly mensaje = signal('');
  protected readonly error = signal('');
  protected readonly guardando = signal(false);
  protected readonly formulario;
  constructor(private readonly fb: FormBuilder, private readonly servicio: RegistroVentaService) {
    this.formulario = this.fb.group({
      codigoProducto: ['', Validators.required],
      cantidad: [1, [Validators.required, Validators.min(1)]]
    });
    this.formulario.controls.codigoProducto.valueChanges.pipe(
      takeUntilDestroyed(this.destroyRef)
    ).subscribe(termino => this.buscarProductos(termino ?? ''));
    this.consultas.pipe(
      switchMap(termino => !termino ? of({ productos: [] as ProductoCaja[], error: '' }) : timer(250).pipe(
        switchMap(() => this.servicio.buscarCoincidencias(termino)),
        map(productos => ({ productos, error: '' })),
        catchError(() => of({ productos: [] as ProductoCaja[], error: 'No se pudieron buscar los productos. Vuelve a escribir para reintentar.' }))
      )),
      takeUntilDestroyed(this.destroyRef)
    ).subscribe(resultado => {
      this.coincidencias.set(resultado.productos);
      this.errorBusqueda.set(resultado.error);
      this.buscando.set(false);
    });
  }
  protected agregar(): void {
    if (this.guardando()) return;
    const { codigoProducto, cantidad } = this.formulario.getRawValue();
    const codigo = codigoProducto?.trim() ?? '';
    const unidades = Number(cantidad);
    if (!codigo || !Number.isInteger(unidades) || unidades < 1 || unidades > 2147483647) {
      this.error.set('Ingresa un código y una cantidad entera mayor que cero.');
      return;
    }
    this.cola.push({ codigo, cantidad: unidades });
    this.pendientes.update(n => n + 1);
    this.formulario.reset({ codigoProducto: '', cantidad: 1 });
    this.mensaje.set('');
    this.enfocar();
    this.procesarCola();
  }

  private procesarCola(): void {
    if (this.procesando || !this.cola.length) return;
    this.procesando = true;
    const siguiente = this.cola.shift()!;
    this.servicio.buscar(siguiente.codigo).subscribe({
      next: producto => {
        const actual = this.productos().find(p => p.idProducto === producto.idProducto);
        const disponible = producto.stock - (actual?.cantidad ?? 0);
        this.productos.update(items => items.map(p => p.idProducto === producto.idProducto ? { ...p, stock: producto.stock } : p));
        if (disponible <= 0) {
          this.error.set(`${producto.nombre}: Sin productos en stock`);
        } else if (siguiente.cantidad > disponible) {
          this.error.set(`${producto.nombre}: stock insuficiente. Puedes agregar ${disponible} unidad(es).`);
        } else {
          this.error.set('');
          this.productos.update(items => actual
            ? items.map(p => p.idProducto === producto.idProducto ? { ...p, cantidad: p.cantidad + siguiente.cantidad } : p)
            : [...items, { ...producto, cantidad: siguiente.cantidad }]);
        }
        this.finalizarConsulta();
      },
      error: respuesta => {
        this.error.set(`${siguiente.codigo}: ${respuesta.error?.mensaje ?? 'No se pudo consultar el producto.'}`);
        this.finalizarConsulta();
      }
    });
  }

  private finalizarConsulta(): void {
    this.procesando = false;
    this.pendientes.update(n => n - 1);
    this.procesarCola();
  }

  protected cambiarCantidad(id: number, diferencia: number): void {
    if (this.guardando() || this.pendientes()) return;
    const producto = this.productos().find(p => p.idProducto === id);
    if (producto && diferencia > 0 && producto.cantidad >= producto.stock) {
      this.error.set(`${producto.nombre}: Sin productos en stock`);
      return;
    }
    this.error.set('');
    this.productos.update(items => items.map(p => p.idProducto === id
      ? { ...p, cantidad: Math.max(1, Math.min(p.stock, p.cantidad + diferencia)) } : p));
  }

  protected quitar(codigo: string): void {
    if (!this.guardando() && !this.pendientes()) this.productos.update(items => items.filter(p => p.codigoProducto !== codigo));
  }

  protected registrar(): void {
    if (this.guardando() || this.pendientes()) return;
    if (this.formulario.controls.codigoProducto.value?.trim()) {
      this.error.set('Agrega el código pendiente a la venta antes de registrar.');
      return;
    }
    if (!this.productos().length) {
      this.error.set('Agrega al menos un producto a la venta.');
      return;
    }
    if (this.productos().some(p => p.cantidad > p.stock)) {
      this.error.set('Revisa las cantidades: superan el stock disponible.');
      return;
    }
    this.guardando.set(true);
    this.error.set('');
    this.mensaje.set('');
    this.servicio.crear(this.productos().map(({ codigoProducto, cantidad }) => ({ codigoProducto, cantidad }))).subscribe({
      next: venta => {
        this.mensaje.set('Venta #' + venta.idVenta + ' registrada correctamente.');
        this.productos.set([]);
        this.formulario.patchValue({ codigoProducto: '', cantidad: 1 });
        this.guardando.set(false);
        this.enfocar();
      },
      error: respuesta => {
        this.error.set(respuesta.error?.mensaje ?? 'No se pudo registrar la venta.');
        this.guardando.set(false);
      }
    });
  }
}
