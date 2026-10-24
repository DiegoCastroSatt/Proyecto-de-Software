import { DisenoDesdeCero, FormaLenteDiseno, PiezaMarco, configuracionDesdeCero, generarMarcoDesdeCero, nombresPiezas, ajustarDisenoMarco, ajustesDisenoIniciales, disenoMarcoInicial } from '../diseno/diseno-marco';
import { Component, EventEmitter, Input, OnChanges, Output, SimpleChanges, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TipoMarco, TiposMarcoService } from '../../shared/services/tipos-marco.service';

@Component({
  selector: 'app-editor-marco', imports: [FormsModule],
  templateUrl: './editor-marco.html', styleUrl: './editor-marco.css',
})
export class EditorMarcoComponent implements OnChanges {
  private readonly servicio = inject(TiposMarcoService);
  @Input() disenos: TipoMarco[] = [];
  @Input() reinicio = 0;
  @Output() disenoChange = new EventEmitter<string>();
  @Output() cargandoChange = new EventEmitter<boolean>();
  readonly cargando = signal(false);
  readonly error = signal('');
  readonly vistaPrevia = signal(this.urlImagen(disenoMarcoInicial));
  ajustes = { ...ajustesDisenoIniciales };
  elegido = '';
  piezas: PiezaMarco[] = [];
  nuevo: DisenoDesdeCero = { ...configuracionDesdeCero };
  readonly nombresPiezas = nombresPiezas;
  readonly formasLentes = signal<FormaLenteDiseno[]>([]);
  readonly errorFormas = signal('');

  constructor() { this.cargarFormas(); }

  cargarFormas(): void {
    this.errorFormas.set('');
    this.servicio.listarFormas().subscribe({
      next: formas => {
        this.formasLentes.set(formas);
        if (!formas.some(forma => forma.id === this.nuevo.forma)) this.nuevo.forma = formas[0]?.id ?? '';
        if (this.elegido === 'nuevo') this.actualizar();
      },
      error: () => this.errorFormas.set('No se pudieron cargar las formas guardadas.'),
    });
  }

  get formaSeleccionada(): FormaLenteDiseno | undefined { return this.formasLentes().find(forma => forma.id === this.nuevo.forma); }

  seleccionarForma(id: string): void { this.nuevo.forma = id; this.actualizar(); }

  private base = disenoMarcoInicial;
  private solicitud = 0;

  ngOnChanges(cambios: SimpleChanges): void {
    if (cambios['reinicio'] && !cambios['reinicio'].firstChange) this.restablecer();
  }

  elegir(id: string): void {
    if (!id) { this.restablecer(); return; }
    if (id === 'nuevo') { this.crearDesdeCero(); return; }
    const solicitud = ++this.solicitud;
    this.cargando.set(true); this.cargandoChange.emit(true); this.error.set('');
    this.servicio.obtenerDiseno(Number(id)).subscribe({
      next: svg => {
        if (solicitud !== this.solicitud) return;
        this.usarDiseno(svg, id); this.terminarCarga();
      },
      error: () => {
        if (solicitud !== this.solicitud) return;
        this.elegido = ''; this.error.set('No se pudo cargar ese diseño. Puedes usar otro o el diseño simple.'); this.terminarCarga();
      },
    });
  }

  usarDiseno(svg: string, origen = 'archivo'): void {
    try {
      const generado = ajustarDisenoMarco(svg, ajustesDisenoIniciales);
      ++this.solicitud; this.elegido = origen; this.base = svg; this.ajustes = { ...ajustesDisenoIniciales };
      this.error.set(''); this.vistaPrevia.set(this.urlImagen(generado)); this.disenoChange.emit(generado);
    } catch {
      this.error.set('No se pudo usar esa ilustración. Elige uno de los diseños disponibles.');
    }
  }

  actualizar(): void {
    if (this.elegido === 'nuevo') {
      if (this.piezas.includes('lentes') && !this.formaSeleccionada) {
        this.disenoChange.emit(''); this.error.set('Selecciona una forma disponible para los lentes.'); return;
      }
      this.base = generarMarcoDesdeCero(this.nuevo, this.piezas, this.formasLentes());
      if (!this.piezas.length) {
        this.vistaPrevia.set(this.urlImagen(this.base)); this.disenoChange.emit(''); this.error.set(''); return;
      }
    }
    try {
      const svg = ajustarDisenoMarco(this.base, this.ajustes);
      this.vistaPrevia.set(this.urlImagen(svg)); this.disenoChange.emit(svg); this.error.set('');
    } catch { this.error.set('No se pudo actualizar el diseño. Prueba con otro diseño base.'); }
  }

  crearDesdeCero(): void {
    ++this.solicitud; this.elegido = 'nuevo'; this.piezas = [];
    this.nuevo = { ...configuracionDesdeCero, forma: this.formasLentes()[0]?.id ?? '' }; this.ajustes = { ...ajustesDisenoIniciales };
    this.error.set(''); this.terminarCarga(); this.actualizar();
  }

  agregarPieza(pieza: PiezaMarco): void {
    if (this.elegido !== 'nuevo' || this.piezas.includes(pieza)) return;
    this.piezas = [...this.piezas, pieza]; this.actualizar();
  }

  quitarPieza(pieza: PiezaMarco): void {
    this.piezas = this.piezas.filter(item => item !== pieza); this.actualizar();
  }

  restablecer(): void {
    ++this.solicitud; this.elegido = ''; this.base = disenoMarcoInicial;
    this.ajustes = { ...ajustesDisenoIniciales }; this.error.set(''); this.terminarCarga(); this.actualizar();
  }

  private terminarCarga(): void { this.cargando.set(false); this.cargandoChange.emit(false); }
  private urlImagen(svg: string): string { return `data:image/svg+xml;charset=utf-8,${encodeURIComponent(svg)}`; }
}
