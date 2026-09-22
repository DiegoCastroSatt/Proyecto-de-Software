import { Component, OnInit, signal, computed, ElementRef, ViewChild, AfterViewInit, OnDestroy, Inject, PLATFORM_ID } from '@angular/core';
import { DecimalPipe, isPlatformBrowser } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import type { Chart as ChartType } from 'chart.js';
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
export class RegistroVentaComponent implements OnInit, AfterViewInit, OnDestroy {
  protected readonly productos = signal<ProductoCaja[]>([]);
  @ViewChild('codigo') private codigoInput?: ElementRef<HTMLInputElement>;
  @ViewChild('graficoIngresos') private graficoIngresos?: ElementRef<HTMLCanvasElement>;
  private grafico?: ChartType;
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
    this.grafico?.destroy();
  }
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
    this.cargarVentas();
  }

  protected cargarVentas(): void {
    this.servicio.listar().subscribe({
      next: ventas => {
        this.ventas.set(ventas);
        void this.actualizarGrafico();
      },
      error: () => this.error.set('No se pudo cargar el historial de ventas.')
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
