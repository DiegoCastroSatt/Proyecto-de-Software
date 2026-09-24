import { Component, OnInit, signal, computed, ElementRef, ViewChild, AfterViewInit, OnDestroy, Inject, PLATFORM_ID } from '@angular/core';
import { DatePipe, DecimalPipe, isPlatformBrowser } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import type { Chart as ChartType } from 'chart.js';
import { RegistroVentaService, Venta, ProductoCaja } from './registro-venta.service';

export type PeriodoVentas = 'semana' | 'mes' | 'semestre' | 'anio';
export interface ProductoMasVendido {
  productoId: number;
  nombre: string;
  unidades: number;
}

export function inicioPeriodo(periodo: PeriodoVentas, hasta: Date): Date {
  const desde = new Date(hasta);
  if (periodo === 'semana') {
    desde.setDate(desde.getDate() - 7);
  } else {
    const meses = { mes: 1, semestre: 6, anio: 12 }[periodo];
    const dia = desde.getDate();
    desde.setDate(1);
    desde.setMonth(desde.getMonth() - meses);
    const ultimoDia = new Date(desde.getFullYear(), desde.getMonth() + 1, 0).getDate();
    desde.setDate(Math.min(dia, ultimoDia));
  }
  return desde;
}

export function productosMasVendidos(
  ventas: Venta[],
  periodo: PeriodoVentas,
  limite: 5 | 10,
  hasta: Date = new Date()
): ProductoMasVendido[] {
  const desde = inicioPeriodo(periodo, hasta);
  const productos = new Map<number, ProductoMasVendido>();
  for (const venta of ventas) {
    const fecha = new Date(venta.fecha);
    if (!(fecha >= desde && fecha <= hasta)) continue;
    for (const producto of venta.productos) {
      const acumulado = productos.get(producto.productoId) ?? {
        productoId: producto.productoId,
        nombre: producto.nombre || `Producto #${producto.productoId}`,
        unidades: 0
      };
      acumulado.unidades += producto.cantidad;
      productos.set(producto.productoId, acumulado);
    }
  }
  return Array.from(productos.values())
    .sort((a, b) => b.unidades - a.unidades || a.productoId - b.productoId)
    .slice(0, limite);
}

interface ResumenMensual {
  clave: string;
  etiqueta: string;
  ingreso: number;
  cantidad: number;
}

@Component({
  selector: 'app-registro-venta',
  imports: [ReactiveFormsModule, DecimalPipe, DatePipe],
  templateUrl: './registro-venta.html',
  styleUrl: './registro-venta.css'
})
export class RegistroVentaComponent implements OnInit, AfterViewInit, OnDestroy {
  protected readonly productos = signal<ProductoCaja[]>([]);
  @ViewChild('codigo') private codigoInput?: ElementRef<HTMLInputElement>;
  @ViewChild('graficoIngresos') private graficoIngresos?: ElementRef<HTMLCanvasElement>;
  private grafico?: ChartType;
  private relojRank?: ReturnType<typeof setInterval>;
  protected readonly pendientes = signal(0);
  protected readonly total = computed(() => this.productos().reduce((s, p) => s + p.precio * p.cantidad, 0));
  protected readonly unidades = computed(() => this.productos().reduce((s, p) => s + p.cantidad, 0));
  private cola: { codigo: string; cantidad: number }[] = [];
  private procesando = false;
  ngAfterViewInit(): void {
    this.enfocar();
    void this.actualizarGrafico();
  }

  ngOnDestroy(): void {
    if (this.relojRank !== undefined) clearInterval(this.relojRank);
    this.grafico?.destroy();
  }
  private enfocar(): void { this.codigoInput?.nativeElement.focus(); }
  protected readonly periodoRank = signal<PeriodoVentas>('semana');
  protected readonly limiteRank = signal<5 | 10>(5);
  protected readonly fechaRank = signal(new Date());
  protected readonly cargandoHistorial = signal(true);
  protected readonly errorHistorial = signal(false);
  protected readonly desdeRank = computed(() => inicioPeriodo(this.periodoRank(), this.fechaRank()));
  protected readonly rank = computed(() => productosMasVendidos(
    this.ventas(), this.periodoRank(), this.limiteRank(), this.fechaRank()
  ));

  protected cambiarPeriodoRank(valor: string): void {
    if (valor === 'semana' || valor === 'mes' || valor === 'semestre' || valor === 'anio') {
      this.periodoRank.set(valor);
      this.fechaRank.set(new Date());
    }
  }

  protected cambiarLimiteRank(valor: string): void {
    if (valor === '5' || valor === '10') this.limiteRank.set(Number(valor) as 5 | 10);
  }

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
  protected readonly maximoMensual = computed(() => Math.max(...this.meses().map(mes => mes.ingreso), 0));
  protected readonly mensaje = signal('');
  protected readonly error = signal('');
  protected readonly guardando = signal(false);
  protected readonly formulario;

  constructor(
    private readonly fb: FormBuilder,
    private readonly servicio: RegistroVentaService,
    @Inject(PLATFORM_ID) private readonly platformId: object
  ) {
    this.formulario = this.fb.group({
      codigoProducto: ['', Validators.required],
      cantidad: [1, [Validators.required, Validators.min(1)]]
    });
  }

  ngOnInit(): void {
    if (isPlatformBrowser(this.platformId)) {
      this.relojRank = setInterval(() => this.fechaRank.set(new Date()), 60_000);
    }
    this.cargarVentas();
  }

  protected cargarVentas(): void {
    this.cargandoHistorial.set(true);
    this.errorHistorial.set(false);
    this.servicio.listar().subscribe({
      next: ventas => {
        this.ventas.set(ventas);
        this.fechaRank.set(new Date());
        this.cargandoHistorial.set(false);
        void this.actualizarGrafico();
      },
      error: () => {
        this.cargandoHistorial.set(false);
        this.errorHistorial.set(true);
        this.error.set('No se pudo cargar el historial de ventas.');
      }
    });
  }

  protected mostrarInforme(): void {
    void this.abrirPdf(false);
  }

  protected descargarInforme(): void {
    void this.abrirPdf(true);
  }

  private async abrirPdf(descargar: boolean): Promise<void> {
    const url = URL.createObjectURL(await this.crearInformePdf());
    if (descargar) {
      const enlace = document.createElement('a');
      enlace.href = url;
      enlace.download = `informe-ventas-${this.claveMes(new Date())}.pdf`;
      enlace.click();
      URL.revokeObjectURL(url);
      return;
    }
    window.open(url, '_blank', 'noopener,noreferrer');
    setTimeout(() => URL.revokeObjectURL(url), 60000);
  }

  private async crearInformePdf(): Promise<Blob> {
    const { jsPDF } = await import('jspdf');
    const moneda = new Intl.NumberFormat('es-CL', { style: 'currency', currency: 'CLP', maximumFractionDigits: 0 });
    const documento = new jsPDF({ unit: 'pt', format: 'a4' });
    const lineas = [
      'INFORME DE VENTAS',
      `Centro Óptico San Francisco · ${new Intl.DateTimeFormat('es-CL', { dateStyle: 'long' }).format(new Date())}`,
      '',
      `Ingreso del mes: ${moneda.format(this.mesActual().ingreso)}`,
      `Ventas del mes: ${this.mesActual().cantidad}`,
      '',
      'Ingresos de los últimos 12 meses',
      ...this.meses().map(mes => `${mes.etiqueta}: ${moneda.format(mes.ingreso)} (${mes.cantidad} ventas)`)
    ];
    let posicionY = 48;

    lineas.forEach(linea => {
      if (posicionY > 790) {
        documento.addPage();
        posicionY = 48;
      }
      documento.text(linea, 40, posicionY);
      posicionY += 18;
    });

    return documento.output('blob');
  }

  private async actualizarGrafico(): Promise<void> {
    if (!isPlatformBrowser(this.platformId)) return;
    const lienzo = this.graficoIngresos?.nativeElement;
    if (!lienzo) return;

    const { Chart, registerables } = await import('chart.js');
    Chart.register(...registerables);

    this.grafico?.destroy();
    const meses = this.meses();
    this.grafico = new Chart(lienzo, {
      type: 'bar',
      data: {
        labels: meses.map(mes => mes.etiqueta),
        datasets: [{
          label: 'Ingresos',
          data: meses.map(mes => mes.ingreso),
          backgroundColor: '#1d4a47',
          borderColor: '#bf8542',
          borderWidth: 1,
          borderRadius: 4
        }]
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        plugins: { legend: { display: false } },
        scales: { y: { beginAtZero: true } }
      }
    });
  }

  private resumirMeses(ventas: Venta[]): ResumenMensual[] {
    const hoy = new Date();
    return Array.from({ length: 12 }, (_, indice) => {
      const fecha = new Date(hoy.getFullYear(), hoy.getMonth() - 11 + indice, 1);
      const clave = this.claveMes(fecha);
      const delMes = ventas.filter(venta => this.claveMes(new Date(venta.fecha)) === clave);
      return {
        clave,
        etiqueta: new Intl.DateTimeFormat('es-CL', { month: 'short', year: 'numeric' }).format(fecha).replace('.', ''),
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
