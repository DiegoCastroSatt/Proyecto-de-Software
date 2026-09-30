import { Component, OnInit, signal, computed, ElementRef, ViewChild, AfterViewInit, OnDestroy, Inject, PLATFORM_ID } from '@angular/core';
import { DatePipe, DecimalPipe, isPlatformBrowser } from '@angular/common';
import { RouterLink } from '@angular/router';
import type { Chart as ChartType } from 'chart.js';
import { RegistroVentaService, Venta } from '../registro-venta/registro-venta.service';

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
  const productos = new Map<string, ProductoMasVendido>();
  for (const venta of ventas) {
    const fecha = new Date(venta.fecha);
    if (!(fecha >= desde && fecha <= hasta)) continue;
    for (const producto of venta.productos) {
      const clave = producto.productoId === null ? `eliminado:${producto.nombre}` : `id:${producto.productoId}`;
      const acumulado = productos.get(clave) ?? {
        productoId: producto.productoId ?? 0,
        nombre: producto.nombre || (producto.productoId === null ? 'Producto eliminado' : `Producto #${producto.productoId}`),
        unidades: 0
      };
      acumulado.unidades += producto.cantidad;
      productos.set(clave, acumulado);
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
  selector: 'app-ver-ventas',
  imports: [DecimalPipe, DatePipe, RouterLink],
  templateUrl: './ver-ventas.html',
  styleUrls: ['../registro-venta/registro-venta.css', './ver-ventas.css']
})
export class VerVentasComponent implements OnInit, AfterViewInit, OnDestroy {
  @ViewChild('graficoIngresos') private graficoIngresos?: ElementRef<HTMLCanvasElement>;
  private grafico?: ChartType;
  private relojRank?: ReturnType<typeof setInterval>;
  private destruido = false;
  private revisionGrafico = 0;
  protected readonly error = signal('');
  constructor(private readonly servicio: RegistroVentaService,
    @Inject(PLATFORM_ID) private readonly platformId: object) {}
  ngAfterViewInit(): void { void this.actualizarGrafico(); }
  ngOnDestroy(): void {
    this.destruido = true;
    if (this.relojRank !== undefined) clearInterval(this.relojRank);
    this.grafico?.destroy();
  }
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
  ngOnInit(): void {
    if (isPlatformBrowser(this.platformId)) {
      this.relojRank = setInterval(() => this.fechaRank.set(new Date()), 60_000);
    }
    this.cargarVentas();
  }

  protected cargarVentas(): void {
    this.error.set('');
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
    const ancho = documento.internal.pageSize.getWidth();
    const alto = documento.internal.pageSize.getHeight();
    const margen = 42;
    const contenido = ancho - margen * 2;
    const verde = '#1d4a47';
    const gris = '#657873';
    const meses = this.meses();
    const fecha = new Intl.DateTimeFormat('es-CL', { dateStyle: 'long' }).format(new Date());
    const texto = (valor: string, x: number, y: number, tamano = 10, color = verde,
      negrita = false, alineacion: 'left' | 'right' = 'left', maxAncho = Infinity) => {
      documento.setFont('helvetica', negrita ? 'bold' : 'normal');
      documento.setTextColor(color);
      documento.setFontSize(tamano);
      while (documento.getTextWidth(valor) > maxAncho && documento.getFontSize() > 12) {
        documento.setFontSize(documento.getFontSize() - 1);
      }
      documento.text(valor, x, y, { align: alineacion });
    };
    const fondo = (x: number, y: number, ancho: number, alto: number, color: string, radio = 0) => {
      documento.setFillColor(color);
      documento.roundedRect(x, y, ancho, alto, radio, radio, 'F');
    };
    const fila = (valores: string[], y: number, alto: number, color: string, negrita = false,
      tinta = verde, desplazamiento = 16) => {
      fondo(margen, y, contenido, alto, color);
      const columnas = [margen + 14, ancho - margen - 170, ancho - margen - 14];
      valores.forEach((valor, i) => texto(valor, columnas[i], y + desplazamiento, 10, tinta, negrita, i ? 'right' : 'left'));
    };

    documento.setProperties({ title: 'Informe de ventas - Óptica San Francisco', author: 'Centro Óptico San Francisco' });
    fondo(0, 0, ancho, 142, verde);
    fondo(0, 142, ancho, 4, '#bf8542');
    texto('CENTRO ÓPTICO SAN FRANCISCO', margen, 39, 10, '#e5c58d', true);
    texto('Informe de ventas', margen, 79, 28, '#ffffff', true);
    texto(`Emitido el ${fecha}`, margen, 108, 10, '#ffffff');
    texto('Moneda: pesos chilenos (CLP)', margen, 125, 10, '#ffffff');

    const actual = this.mesActual();
    const tarjetas = [
      ['INGRESOS DEL MES', moneda.format(actual.ingreso), actual.etiqueta],
      ['VENTAS DEL MES', String(actual.cantidad), 'Ventas registradas en el mes actual']
    ];
    tarjetas.forEach(([titulo, valor, detalle], indice) => {
      const x = margen + indice * (contenido + 16) / 2;
      const anchoTarjeta = (contenido - 16) / 2;
      fondo(x, 170, anchoTarjeta, 100, '#eef3f0', 6);
      texto(titulo, x + 16, 193, 10, gris);
      texto(valor, x + 16, 226, 23, verde, true, 'left', anchoTarjeta - 32);
      texto(detalle, x + 16, 249, 9, gris);
    });
    texto('Ingresos de los últimos 12 meses', margen, 307, 15, verde, true);
    texto(`${meses[0]?.etiqueta ?? ''} a ${meses[meses.length - 1]?.etiqueta ?? ''}`, margen, 325, 9, gris);
    fila(['MES', 'VENTAS', 'INGRESOS (CLP)'], 342, 28, verde, true, '#ffffff', 18);
    meses.forEach((mes, i) => fila(
      [mes.etiqueta, String(mes.cantidad), moneda.format(mes.ingreso)],
      370 + i * 25, 25, i % 2 === 0 ? '#f2f5f3' : '#ffffff', mes.clave === actual.clave
    ));
    const y = 370 + meses.length * 25;
    fila(['TOTAL DEL PERÍODO', String(meses.reduce((s, m) => s + m.cantidad, 0)),
      moneda.format(meses.reduce((s, m) => s + m.ingreso, 0))], y, 32, '#e1eae5', true, verde, 21);
    texto('Resumen elaborado con las ventas registradas en el sistema.', margen, y + 55, 9, gris);
    documento.setDrawColor('#d5dfd9');
    documento.line(margen, alto - 48, ancho - margen, alto - 48);
    texto('Óptica San Francisco | Informe administrativo', margen, alto - 30, 8, gris);
    texto('Página 1 de 1', ancho - margen, alto - 30, 8, gris, false, 'right');

    return documento.output('blob');
  }

  private async actualizarGrafico(): Promise<void> {
    if (!isPlatformBrowser(this.platformId)) return;
    const lienzo = this.graficoIngresos?.nativeElement;
    if (!lienzo) return;

    const revision = ++this.revisionGrafico;
    const { Chart, registerables } = await import('chart.js');
    if (this.destruido || revision !== this.revisionGrafico) return;
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

}
