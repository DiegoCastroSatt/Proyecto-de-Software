import { Component, OnInit, signal, computed, ElementRef, ViewChild, AfterViewInit } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RegistroVentaService, Venta, ProductoCaja } from './registro-venta.service';

@Component({
  selector: 'app-registro-venta',
  imports: [ReactiveFormsModule, DecimalPipe],
  templateUrl: './registro-venta.html',
  styleUrl: './registro-venta.css'
})
export class RegistroVentaComponent implements OnInit, AfterViewInit {
  protected readonly productos = signal<ProductoCaja[]>([]);
  @ViewChild('codigo') private codigoInput?: ElementRef<HTMLInputElement>;
  protected readonly pendientes = signal(0);
  protected readonly total = computed(() => this.productos().reduce((s, p) => s + p.precio * p.cantidad, 0));
  protected readonly unidades = computed(() => this.productos().reduce((s, p) => s + p.cantidad, 0));
  private cola: { codigo: string; cantidad: number }[] = [];
  private procesando = false;
  ngAfterViewInit(): void { this.enfocar(); }
  private enfocar(): void { this.codigoInput?.nativeElement.focus(); }
  protected readonly ventas = signal<Venta[]>([]);
  protected readonly mensaje = signal('');
  protected readonly error = signal('');
  protected readonly guardando = signal(false);
  protected readonly formulario;

  constructor(private readonly fb: FormBuilder, private readonly servicio: RegistroVentaService) {
    this.formulario = this.fb.group({
      codigoProducto: ['', Validators.required],
      cantidad: [1, [Validators.required, Validators.min(1)]]
    });
  }

  ngOnInit(): void {
    this.cargarVentas();
  }

  protected cargarVentas(): void {
    this.servicio.listar().subscribe({
      next: ventas => this.ventas.set(ventas),
      error: () => this.error.set('No se pudo cargar el historial de ventas.')
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
        if ((actual?.cantidad ?? 0) + siguiente.cantidad > 2147483647) {
          this.error.set('La cantidad del producto es demasiado grande.');
        } else {
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
    this.productos.update(items => items.map(p => p.idProducto === id
      ? { ...p, cantidad: Math.min(2147483647, Math.max(1, p.cantidad + diferencia)) } : p));
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
    this.guardando.set(true);
    this.error.set('');
    this.mensaje.set('');
    this.servicio.crear(this.productos().map(({ codigoProducto, cantidad }) => ({ codigoProducto, cantidad }))).subscribe({
      next: venta => {
        this.mensaje.set('Venta #' + venta.idVenta + ' registrada correctamente.');
        this.productos.set([]);
        this.formulario.patchValue({ codigoProducto: '', cantidad: 1 });
        this.guardando.set(false);
        this.cargarVentas();
        this.enfocar();
      },
      error: respuesta => {
        this.error.set(respuesta.error?.mensaje ?? 'No se pudo registrar la venta.');
        this.guardando.set(false);
      }
    });
  }
}
