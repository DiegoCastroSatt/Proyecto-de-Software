import {
  Component,
  ElementRef,
  OnDestroy,
  OnInit,
  PLATFORM_ID,
  computed,
  effect,
  inject,
  signal,
  viewChild,
} from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import type { Chart as ChartType } from 'chart.js';
import {
  ComentarioEncuesta,
  EncuestasService,
  EvolucionMensualEncuestas,
  ResumenEncuestas,
} from '../../shared/services/encuestas.service';

const TAMANO_PAGINA = 10;
const MESES_POR_DEFECTO = 3;

@Component({
  selector: 'app-resultados-encuestas',
  standalone: true,
  templateUrl: './resultados-encuestas.html',
  styleUrl: './resultados-encuestas.css',
})
export class ResultadosEncuestasComponent implements OnInit, OnDestroy {
  private readonly encuestas = inject(EncuestasService);
  private readonly platformId = inject(PLATFORM_ID);
  private readonly lienzoEvolucion = viewChild<ElementRef<HTMLCanvasElement>>('graficoEvolucion');
  private grafico?: ChartType;
  private revisionGrafico = 0;
  private destruido = false;

  readonly estrellas = [5, 4, 3, 2, 1] as const;

  readonly desde = signal(fechaIso(restarMeses(new Date(), MESES_POR_DEFECTO)));
  readonly hasta = signal(fechaIso(new Date()));
  readonly errorFechas = signal('');

  readonly resumen = signal<ResumenEncuestas | null>(null);
  readonly cargandoResumen = signal(false);
  readonly errorResumen = signal('');

  readonly comentarios = signal<ComentarioEncuesta[]>([]);
  readonly totalComentarios = signal(0);
  readonly pagina = signal(1);
  readonly filtroCalificacion = signal<number | null>(null);
  readonly soloConComentario = signal(true);
  readonly cargandoComentarios = signal(false);
  readonly errorComentarios = signal('');
  readonly cambiandoVisibilidad = signal<number | null>(null);
  readonly errorVisibilidad = signal('');

  readonly totalPaginas = computed(() =>
    Math.max(1, Math.ceil(this.totalComentarios() / TAMANO_PAGINA)),
  );
  readonly rangoMostrado = computed(() => {
    const total = this.totalComentarios();
    if (total === 0) return '';
    const inicio = (this.pagina() - 1) * TAMANO_PAGINA + 1;
    const fin = Math.min(this.pagina() * TAMANO_PAGINA, total);
    return `Mostrando ${inicio}–${fin} de ${total}`;
  });

  readonly preguntas = computed(() => {
    const resumen = this.resumen();
    return [
      { texto: 'Satisfacción general', promedio: resumen?.promedioGeneral ?? null },
      { texto: 'Trato del personal', promedio: resumen?.promedioTrato ?? null },
      { texto: 'Puntualidad', promedio: resumen?.promedioPuntualidad ?? null },
    ];
  });

  readonly distribucion = computed(() => {
    const resumen = this.resumen();
    const total = resumen?.respondidas ?? 0;
    return this.estrellas.map((estrellas) => {
      const cantidad =
        resumen?.distribucionGeneral.find((item) => item.estrellas === estrellas)?.cantidad ?? 0;
      return { estrellas, cantidad, porcentaje: total > 0 ? (cantidad / total) * 100 : 0 };
    });
  });

  constructor() {
    // El lienzo solo existe cuando hay respuestas; se dibuja cada vez que aparece o cambian los datos.
    effect(() => {
      const lienzo = this.lienzoEvolucion()?.nativeElement;
      const evolucion = this.resumen()?.evolucionMensual ?? [];
      if (lienzo) void this.actualizarGrafico(lienzo, evolucion);
    });
  }

  ngOnInit(): void {
    this.buscar();
  }

  ngOnDestroy(): void {
    this.destruido = true;
    this.grafico?.destroy();
  }

  cambiarDesde(evento: Event): void {
    this.desde.set((evento.target as HTMLInputElement).value);
  }

  cambiarHasta(evento: Event): void {
    this.hasta.set((evento.target as HTMLInputElement).value);
  }

  buscar(): void {
    if (!this.desde() || !this.hasta()) {
      this.errorFechas.set('Selecciona una fecha de inicio y una de término.');
      return;
    }
    if (this.desde() > this.hasta()) {
      this.errorFechas.set('La fecha de inicio no puede ser posterior a la fecha de término.');
      return;
    }
    this.errorFechas.set('');
    this.cargarResumen();
    this.pagina.set(1);
    this.cargarComentarios();
  }

  cambiarCalificacion(evento: Event): void {
    const valor = (evento.target as HTMLSelectElement).value;
    this.filtroCalificacion.set(valor ? Number(valor) : null);
    this.pagina.set(1);
    this.cargarComentarios();
  }

  cambiarSoloConComentario(evento: Event): void {
    this.soloConComentario.set((evento.target as HTMLInputElement).checked);
    this.pagina.set(1);
    this.cargarComentarios();
  }

  irAPagina(pagina: number): void {
    if (pagina < 1 || pagina > this.totalPaginas() || pagina === this.pagina()) return;
    this.pagina.set(pagina);
    this.cargarComentarios();
  }

  cargarResumen(): void {
    this.cargandoResumen.set(true);
    this.errorResumen.set('');
    this.encuestas.obtenerResumen({ desde: this.desde(), hasta: this.hasta() }).subscribe({
      next: (resumen) => {
        this.resumen.set(resumen);
        this.cargandoResumen.set(false);
      },
      error: () => {
        this.resumen.set(null);
        this.errorResumen.set('No pudimos cargar el resumen de encuestas.');
        this.cargandoResumen.set(false);
      },
    });
  }

  cargarComentarios(): void {
    this.cargandoComentarios.set(true);
    this.errorComentarios.set('');
    this.encuestas
      .obtenerComentarios({
        desde: this.desde(),
        hasta: this.hasta(),
        calificacion: this.filtroCalificacion(),
        soloConComentario: this.soloConComentario(),
        pagina: this.pagina(),
        tamanoPagina: TAMANO_PAGINA,
      })
      .subscribe({
        next: (respuesta) => {
          this.comentarios.set(respuesta.elementos);
          this.totalComentarios.set(respuesta.total);
          this.cargandoComentarios.set(false);
        },
        error: () => {
          this.comentarios.set([]);
          this.totalComentarios.set(0);
          this.errorComentarios.set('No pudimos cargar los comentarios.');
          this.cargandoComentarios.set(false);
        },
      });
  }

  /** Oculta un comentario inapropiado o lo vuelve a mostrar. Las calificaciones no cambian. */
  cambiarVisibilidad(item: ComentarioEncuesta): void {
    if (this.cambiandoVisibilidad() !== null) return;
    const oculto = !item.comentarioOculto;
    this.cambiandoVisibilidad.set(item.id);
    this.errorVisibilidad.set('');
    this.encuestas.cambiarVisibilidadComentario(item.id, oculto).subscribe({
      next: () => {
        this.cambiandoVisibilidad.set(null);
        // Al mostrarlo de nuevo se recarga la página para obtener el texto, que la API no envía si está oculto.
        if (!oculto) {
          this.cargarComentarios();
          return;
        }
        this.comentarios.update((lista) =>
          lista.map((actual) =>
            actual.id === item.id
              ? { ...actual, comentario: null, comentarioOculto: true }
              : actual,
          ),
        );
      },
      error: () => {
        this.cambiandoVisibilidad.set(null);
        this.errorVisibilidad.set(
          oculto
            ? 'No pudimos ocultar el comentario. Inténtalo de nuevo.'
            : 'No pudimos mostrar el comentario. Inténtalo de nuevo.',
        );
      },
    });
  }

  formatearPromedio(promedio: number | null | undefined): string {
    if (promedio === null || promedio === undefined) return '—';
    return promedio.toLocaleString('es-CL', { minimumFractionDigits: 1, maximumFractionDigits: 1 });
  }

  formatearPorcentaje(valor: number): string {
    return `${Math.round(valor)}%`;
  }

  /** Ancho de la barra de promedio, de 0 a 100. */
  anchoPromedio(promedio: number | null): number {
    return promedio === null ? 0 : (promedio / 5) * 100;
  }

  estrellasTexto(cantidad: number): string {
    return '★'.repeat(cantidad);
  }

  textoMes(mes: string): string {
    return formatearMes(mes, 'long');
  }

  private async actualizarGrafico(
    lienzo: HTMLCanvasElement,
    evolucion: EvolucionMensualEncuestas[],
  ): Promise<void> {
    if (!isPlatformBrowser(this.platformId)) return;
    const revision = ++this.revisionGrafico;
    const { Chart, registerables } = await import('chart.js');
    if (this.destruido || revision !== this.revisionGrafico || !lienzo.isConnected) return;
    Chart.register(...registerables);

    this.grafico?.destroy();
    Chart.getChart(lienzo)?.destroy();
    const serie = (
      etiqueta: string,
      color: string,
      valor: (mes: EvolucionMensualEncuestas) => number,
    ) => ({
      label: etiqueta,
      data: evolucion.map(valor),
      borderColor: color,
      backgroundColor: color,
      tension: 0.3,
    });
    this.grafico = new Chart(lienzo, {
      type: 'line',
      data: {
        labels: evolucion.map((mes) => formatearMes(mes.mes, 'short')),
        datasets: [
          serie('Satisfacción general', '#1d4a47', (mes) => mes.promedioGeneral),
          serie('Trato del personal', '#bf8542', (mes) => mes.promedioTrato),
          serie('Puntualidad', '#6352a3', (mes) => mes.promedioPuntualidad),
        ],
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        scales: { y: { min: 1, max: 5, ticks: { stepSize: 1 } } },
        plugins: {
          tooltip: {
            callbacks: {
              label: (contexto) =>
                `${contexto.dataset.label}: ${this.formatearPromedio(Number(contexto.raw))}`,
              afterBody: (elementos) => {
                const respuestas = evolucion[elementos[0]?.dataIndex ?? 0]?.respuestas ?? 0;
                return `${respuestas} ${respuestas === 1 ? 'respuesta' : 'respuestas'}`;
              },
            },
          },
        },
      },
    });
  }
}

function restarMeses(fecha: Date, meses: number): Date {
  const resultado = new Date(fecha);
  resultado.setMonth(resultado.getMonth() - meses);
  return resultado;
}

/** Fecha local en formato yyyy-MM-dd. */
function fechaIso(fecha: Date): string {
  const mes = String(fecha.getMonth() + 1).padStart(2, '0');
  const dia = String(fecha.getDate()).padStart(2, '0');
  return `${fecha.getFullYear()}-${mes}-${dia}`;
}

/** Convierte "2026-11" en "noviembre 2026" o "nov 2026". */
function formatearMes(mes: string, formato: 'long' | 'short'): string {
  const [anio, numeroMes] = mes.split('-').map(Number);
  const texto = new Intl.DateTimeFormat('es-CL', { month: formato, year: 'numeric' })
    .format(new Date(anio, numeroMes - 1, 1))
    .replace(' de ', ' ')
    .replace('.', '');
  return texto.charAt(0).toUpperCase() + texto.slice(1);
}
