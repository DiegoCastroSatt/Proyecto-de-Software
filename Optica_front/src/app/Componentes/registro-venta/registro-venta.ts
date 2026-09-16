import { Component, OnInit, signal, computed, ElementRef, ViewChild, AfterViewInit } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RegistroVentaService, Venta, ProductoCaja } from './registro-venta.service';

interface ResumenMensual {
  clave: string;
  etiqueta: string;
  ingreso: number;
  cantidad: number;
}

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
  protected readonly meses = computed<ResumenMensual[]>(() => this.resumirMeses(this.ventas()));
  protected readonly mesActual = computed(() => {
    const clave = this.claveMes(new Date());
    return this.meses().find(mes => mes.clave === clave) ?? {
      clave,
      etiqueta: new Intl.DateTimeFormat('es-CL', { month: 'long', year: 'numeric' }).format(new Date()),
      ingreso: 0,
      cantidad: 0
    };
  });
  protected readonly maximoMensual = computed(() => Math.max(...this.meses().map(mes => mes.ingreso), 1));
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

  protected alturaBarra(ingreso: number): number {
    return Math.max(ingreso > 0 ? 8 : 0, ingreso / this.maximoMensual() * 100);
  }

  protected mostrarInforme(): void {
    const url = URL.createObjectURL(this.crearInformePdf());
    window.open(url, '_blank', 'noopener,noreferrer');
    setTimeout(() => URL.revokeObjectURL(url), 60000);
  }

  protected descargarInforme(): void {
    const url = URL.createObjectURL(this.crearInformePdf());
    const enlace = document.createElement('a');
    enlace.href = url;
    enlace.download = `informe-ventas-${this.claveMes(new Date())}.pdf`;
    enlace.click();
    URL.revokeObjectURL(url);
  }

  private crearInformePdf(): Blob {
    const moneda = new Intl.NumberFormat('es-CL', { style: 'currency', currency: 'CLP', maximumFractionDigits: 0 });
    const fecha = new Intl.DateTimeFormat('es-CL', { dateStyle: 'long' }).format(new Date());
    const lineas = [
      'INFORME DE VENTAS',
      `Centro Optico San Francisco - ${fecha}`,
      '',
      `Ingreso del mes: ${moneda.format(this.mesActual().ingreso)}`,
      `Ventas del mes: ${this.mesActual().cantidad}`,
      '',
      'INGRESOS DE LOS ULTIMOS 12 MESES',
      ...this.meses().map(mes => `${mes.etiqueta.toUpperCase()}: ${moneda.format(mes.ingreso)} (${mes.cantidad} ventas)`),
      '',
      'DETALLE DE VENTAS',
      ...this.ventas().flatMap(venta => [
        `Venta #${venta.idVenta} | ${new Intl.DateTimeFormat('es-CL', { dateStyle: 'short', timeStyle: 'short' }).format(new Date(venta.fecha))} | ${moneda.format(venta.total)}`,
        ...venta.productos.map(producto => `  ${producto.nombre ?? 'Producto'} x ${producto.cantidad} - ${moneda.format(producto.subtotal)}`),
        ''
      ])
    ];
    return this.generarPdf(lineas);
  }

  private generarPdf(lineas: string[]): Blob {
    const porPagina = 48;
    const paginas = Array.from({ length: Math.max(1, Math.ceil(lineas.length / porPagina)) }, (_, indice) =>
      lineas.slice(indice * porPagina, (indice + 1) * porPagina));
    const objetos: string[] = [];
    objetos[1] = '<< /Type /Catalog /Pages 2 0 R >>';
    const idsPaginas = paginas.map((_, indice) => 4 + indice * 2);
    objetos[2] = `<< /Type /Pages /Kids [${idsPaginas.map(id => `${id} 0 R`).join(' ')}] /Count ${paginas.length} >>`;
    objetos[3] = '<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>';
    paginas.forEach((pagina, indice) => {
      const idPagina = idsPaginas[indice];
      const idContenido = idPagina + 1;
      const contenido = ['BT', '/F1 10 Tf', '45 800 Td', '14 TL', ...pagina.flatMap((linea, numero) => [
        numero ? 'T*' : '', `(${this.textoPdf(linea)}) Tj`
      ]).filter(Boolean), 'ET'].join('\n');
      objetos[idPagina] = `<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 3 0 R >> >> /Contents ${idContenido} 0 R >>`;
      objetos[idContenido] = `<< /Length ${contenido.length} >>\nstream\n${contenido}\nendstream`;
    });
    let documento = '%PDF-1.4\n';
    const posiciones = [0];
    for (let id = 1; id < objetos.length; id++) {
      posiciones[id] = documento.length;
      documento += `${id} 0 obj\n${objetos[id]}\nendobj\n`;
    }
    const inicioXref = documento.length;
    documento += `xref\n0 ${objetos.length}\n0000000000 65535 f \n`;
    for (let id = 1; id < objetos.length; id++) documento += `${String(posiciones[id]).padStart(10, '0')} 00000 n \n`;
    documento += `trailer\n<< /Size ${objetos.length} /Root 1 0 R >>\nstartxref\n${inicioXref}\n%%EOF`;
    return new Blob([new TextEncoder().encode(documento)], { type: 'application/pdf' });
  }

  private textoPdf(texto: string): string {
    return texto.normalize('NFD').replace(/[\u0300-\u036f]/g, '').replace(/[^\x20-\x7E]/g, '').replace(/([\\()])/g, '\\$1');
  }

  private resumirMeses(ventas: Venta[]): ResumenMensual[] {
    const hoy = new Date();
    return Array.from({ length: 12 }, (_, indice) => {
      const fecha = new Date(hoy.getFullYear(), hoy.getMonth() - 11 + indice, 1);
      const clave = this.claveMes(fecha);
      const delMes = ventas.filter(venta => this.claveMes(new Date(venta.fecha)) === clave);
      return {
        clave,
        etiqueta: new Intl.DateTimeFormat('es-CL', { month: 'short' }).format(fecha).replace('.', ''),
        ingreso: delMes.reduce((suma, venta) => suma + venta.total, 0),
        cantidad: delMes.length
      };
    });
  }

  private claveMes(fecha: Date): string {
    return `${fecha.getFullYear()}-${String(fecha.getMonth() + 1).padStart(2, '0')}`;
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
