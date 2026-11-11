import { TipoMarco, imagenMarco } from '../../shared/services/tipos-marco.service';
import { DisponibilidadMarcosService } from '../../shared/services/disponibilidad-marcos.service';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Component, ElementRef, OnDestroy, ViewChild, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { DetectorRostroService } from './detector-rostro.service';
import { AjusteMarco, calcularAjuste } from './ajuste-marco';

@Component({
  selector: 'app-probador-marcos',
  imports: [RouterLink],
  templateUrl: './probador-marcos.html',
  styleUrl: './probador-marcos.css',
  providers: [DetectorRostroService],
})
export class ProbadorMarcosComponent implements OnDestroy {
  @ViewChild('video', { static: true }) video!: ElementRef<HTMLVideoElement>;
  private readonly detector = inject(DetectorRostroService);
  readonly imagenProbador = imagenProbador;
  readonly modelos = signal<TipoMarco[]>([]);
  readonly cargandoModelos = signal(true);
  readonly errorModelos = signal('');
  readonly seleccionado = signal('');
  readonly foto = signal('');
  readonly camara = signal(false);
  readonly solicitando = signal(false);
  readonly analizando = signal(false);
  readonly error = signal('');
  readonly tamano = signal(100);
  readonly ajuste = signal<AjusteMarco | null>(null);
  private stream?: MediaStream;
  private operacion = 0;
  private destruido = false;
  private fotograma = 0;
  private ultimoAnalisis = -Infinity;
  private ultimoVideo = -1;

  constructor() {
    const modelo = inject(ActivatedRoute).snapshot.queryParamMap.get('modelo');
    inject(DisponibilidadMarcosService).consultar().pipe(takeUntilDestroyed()).subscribe({
      next: disponibles => {
        const modelos = disponibles;
        this.modelos.set(modelos);
        this.seleccionado.set(modelos.find(item => item.codigo === modelo)?.codigo ?? modelos[0]?.codigo ?? '');
        this.cargandoModelos.set(false);
      },
      error: () => {
        this.errorModelos.set('No se pudo consultar el stock de marcos. Vuelve a intentarlo.');
        this.cargandoModelos.set(false);
      },
    });
  }

  get modeloActual() { return this.modelos().find(item => item.codigo === this.seleccionado()); }

  get nombreModelo(): string {
    return this.modelos().find((item) => item.codigo === this.seleccionado())?.nombre ?? '';
  }

  get estado(): string {
    if (this.solicitando()) return 'Autoriza la cámara en tu navegador';
    if (this.analizando()) return 'Buscando tus ojos…';
    if (this.ajuste()) return 'Marco alineado con tus ojos';
    if (this.foto() || this.camara())
      return 'No vemos tus ojos. Mira de frente y busca un lugar con buena luz.';
    return 'Elige una foto o enciende la cámara para empezar';
  }

  private vigente(turno: number): boolean {
    return !this.destruido && turno === this.operacion;
  }

  async cargarFoto(event: Event): Promise<void> {
    const input = event.target as HTMLInputElement;
    const archivo = input.files?.[0];
    input.value = '';
    if (!archivo) return;
    if (
      !['image/jpeg', 'image/png', 'image/webp'].includes(archivo.type) ||
      archivo.size > 10 * 1024 * 1024
    ) {
      this.error.set('Elige una foto JPG, PNG o WebP de hasta 10 MB.');
      return;
    }
    const turno = ++this.operacion;
    this.detenerCamara();
    this.error.set('');
    this.ajuste.set(null);
    this.analizando.set(true);
    const url = URL.createObjectURL(archivo);
    const imagen = new Image();
    imagen.src = url;
    try {
      await imagen.decode();
      if (!this.vigente(turno)) {
        URL.revokeObjectURL(url);
        return;
      }
      this.liberarFoto();
      this.foto.set(url);
      this.tamano.set(100);
      try {
        const puntos = await this.detector.detectar(imagen);
        if (this.vigente(turno))
          this.ajuste.set(calcularAjuste(puntos, imagen.naturalWidth, imagen.naturalHeight));
      } catch {
        if (this.vigente(turno))
          this.error.set(
            'No pudimos iniciar la detección de ojos. Vuelve a cargar la foto o prueba otro navegador.',
          );
      }
    } catch {
      URL.revokeObjectURL(url);
      if (this.vigente(turno))
        this.error.set('No se pudo abrir la fotografía. Prueba con otra imagen.');
    } finally {
      if (this.vigente(turno)) this.analizando.set(false);
    }
  }

  async activarCamara(): Promise<void> {
    if (this.solicitando() || this.camara()) return;
    if (!navigator.mediaDevices?.getUserMedia) {
      this.error.set(
        'La cámara no está disponible. Usa HTTPS o localhost, o carga una fotografía.',
      );
      return;
    }
    const turno = ++this.operacion;
    this.analizando.set(false);
    this.solicitando.set(true);
    this.error.set('');
    try {
      const stream = await navigator.mediaDevices.getUserMedia({
        video: { facingMode: 'user' },
        audio: false,
      });
      if (!this.vigente(turno)) {
        stream.getTracks().forEach((track) => track.stop());
        return;
      }
      this.stream = stream;
      this.video.nativeElement.srcObject = stream;
      await this.video.nativeElement.play();
      if (!this.vigente(turno)) return;
      this.liberarFoto();
      this.ajuste.set(null);
      this.camara.set(true);
      this.tamano.set(100);
      this.analizando.set(true);
      this.ultimoAnalisis = -Infinity;
      this.ultimoVideo = -1;
      stream.getVideoTracks().forEach(
        (track) =>
          (track.onended = () => {
            if (this.stream === stream) {
              ++this.operacion;
              this.detenerCamara();
              this.error.set(
                'La cámara se desconectó. Puedes activarla otra vez o cargar una foto.',
              );
            }
          }),
      );
      this.fotograma = requestAnimationFrame((tiempo) => void this.seguirOjos(turno, tiempo));
    } catch (error) {
      if (!this.vigente(turno)) return;
      this.detenerCamara();
      this.error.set(
        error instanceof DOMException && error.name === 'NotAllowedError'
          ? 'No se autorizó la cámara. Permite su acceso en el navegador o carga una fotografía.'
          : 'No se pudo iniciar la cámara. Revisa que esté conectada y que otra aplicación no la esté usando.',
      );
    } finally {
      if (this.vigente(turno)) this.solicitando.set(false);
    }
  }

  private async seguirOjos(turno: number, tiempo: number): Promise<void> {
    if (!this.vigente(turno) || !this.camara()) return;
    const video = this.video.nativeElement;
    // Limitar la detección a 12 veces por segundo y omitir fotogramas repetidos.
    if (
      video.readyState >= 2 &&
      video.currentTime !== this.ultimoVideo &&
      tiempo - this.ultimoAnalisis >= 80
    ) {
      this.ultimoAnalisis = tiempo;
      this.ultimoVideo = video.currentTime;
      try {
        const puntos = await this.detector.detectar(video, performance.now());
        if (!this.vigente(turno)) return;
        this.ajuste.set(calcularAjuste(puntos, video.videoWidth, video.videoHeight));
        this.analizando.set(false);
      } catch {
        if (!this.vigente(turno)) return;
        this.detenerCamara();
        this.error.set(
          'No pudimos iniciar el seguimiento de ojos. Prueba otra vez o usa otro navegador.',
        );
        return;
      }
    }
    if (this.vigente(turno) && this.camara()) {
      this.fotograma = requestAnimationFrame((t) => void this.seguirOjos(turno, t));
    }
  }

  limpiar(): void {
    ++this.operacion;
    this.detenerCamara();
    this.liberarFoto();
    this.error.set('');
    this.tamano.set(100);
  }

  private detenerCamara(): void {
    if (typeof cancelAnimationFrame !== 'undefined') cancelAnimationFrame(this.fotograma);
    this.stream?.getTracks().forEach((track) => {
      track.onended = null;
      track.stop();
    });
    this.stream = undefined;
    if (this.video) this.video.nativeElement.srcObject = null;
    this.camara.set(false);
    this.solicitando.set(false);
    this.analizando.set(false);
    this.ajuste.set(null);
  }

  private liberarFoto(): void {
    if (this.foto()) URL.revokeObjectURL(this.foto());
    this.foto.set('');
  }

  ngOnDestroy(): void {
    this.destruido = true;
    this.limpiar();
  }
}

// Contorno izquierdo de los modelos de catálogo, en las coordenadas del visor original.
const lentes = new Map([
  ['rectangular', 'M24 42 Q22 37 30 37 H91 Q100 37 98 45 L94 73 Q93 79 85 79 H38 Q30 79 28 72 Z'],
  ['redondo', 'M98 60 A36 36 0 1 1 26 60 A36 36 0 1 1 98 60 Z'],
  ['ovalado', 'M100 60 A39 28 0 1 1 22 60 A39 28 0 1 1 100 60 Z'],
  ['cuadrado', 'M30 29 Q25 29 25 36 L28 83 Q28 89 35 89 H87 Q95 89 95 81 L98 36 Q98 29 90 29 Z'],
  ['cat-eye', 'M16 29 Q54 43 91 39 Q102 39 99 53 L92 75 Q85 88 57 85 Q32 82 25 62 Z'],
  ['aviador', 'M23 39 Q23 28 42 29 L89 36 Q102 39 98 54 Q88 91 66 94 Q39 96 27 69 Q22 57 23 39 Z'],
  ['wayfarer', 'M19 34 Q60 27 101 35 L92 77 Q89 85 78 86 H43 Q33 85 29 76 Z'],
  ['geometrico', 'M23 44 L43 25 H78 L100 44 L94 76 L76 90 H43 L26 74 Z'],
  ['browline', 'M23 42 Q58 28 97 41 L94 65 Q89 87 61 88 Q34 86 27 67 Z'],
  ['semimontura', 'M24 42 Q60 27 98 42 L94 70 Q89 87 62 87 Q35 86 28 69 Z'],
  ['sin-montura-cuadrado', 'M27 37 Q27 32 35 32 H91 Q100 32 98 42 L93 77 Q92 85 84 85 H39 Q30 85 29 77 Z'],
  ['sin-montura', 'M100 60 A39 28 0 1 1 22 60 A39 28 0 1 1 100 60 Z'],
  ['transparente', 'M25 35 Q61 26 95 35 Q102 38 99 49 L94 77 Q91 86 80 86 H43 Q31 86 28 75 L23 48 Q21 39 25 35 Z'],
  ['rectangular-alto', 'M25 32 Q23 27 32 27 H91 Q100 27 98 36 L94 82 Q93 90 84 90 H39 Q30 90 28 81 Z'],
  ['aviador-angular', 'M24 32 L88 37 L100 48 L84 86 L61 95 L34 83 L23 58 Z'],
  ['cat-eye-suave', 'M20 35 Q55 42 88 40 Q101 41 98 57 Q93 83 65 85 Q37 86 28 65 Z'],
  ['mariposa', 'M14 27 Q42 30 94 44 Q103 47 98 63 Q91 89 64 91 Q33 92 24 67 Z'],
  ['oversize', 'M19 28 Q54 17 95 30 Q106 35 100 63 L91 89 Q88 97 70 98 H43 Q25 96 22 81 L17 48 Q15 34 19 28 Z'],
]);

const monturaFina = new Set(['redondo', 'ovalado', 'geometrico', 'aviador', 'aviador-angular', 'semimontura']);
const bisagras = new Map([
  ['redondo', 30], ['ovalado', 29], ['sin-montura', 29], ['sin-montura-cuadrado', 28],
  ['cat-eye', 20], ['wayfarer', 21], ['mariposa', 18], ['oversize', 17],
]);

function ilustrar(codigo: string, lente: string): string {
  const sinMontura = codigo.startsWith('sin-montura');
  const fina = monturaFina.has(codigo);
  const metal = fina || sinMontura;
  const claro = codigo === 'transparente';
  const colores = metal ? ['#5e4834', '#f4dbab', '#99704b']
    : claro ? ['#9a7f69', '#fff0dd', '#c8a98a']
    : ['#17191d', '#727071', '#322e2c'];
  const grosor = sinMontura ? 0.85 : fina ? 2.5 : 5.5;
  const bisagra = bisagras.get(codigo) ?? 24;
  const puenteSuperior = codigo === 'aviador' || codigo === 'aviador-angular'
    ? 'M89 37 Q120 28 151 37' : '';
  const ceja = codigo === 'browline' || codigo === 'semimontura'
    ? '<path d="M24 42 Q60 27 98 42" fill="none" stroke="url(#aro)" stroke-width="6" stroke-linecap="round"/>' : '';
  const svg = `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 240 120">
    <defs>
      <linearGradient id="aro" x2="0%" y2="100%">
        <stop stop-color="${colores[0]}"/>
        <stop offset=".22" stop-color="${colores[1]}"/>
        <stop offset=".46" stop-color="${colores[2]}"/>
        <stop offset="1" stop-color="${colores[0]}"/>
      </linearGradient>
      <linearGradient id="cristal" x2="0%" y2="100%">
        <stop stop-color="#edfaff" stop-opacity=".32"/>
        <stop offset=".45" stop-color="#edfaff" stop-opacity=".06"/>
        <stop offset=".78" stop-color="#add4e5" stop-opacity=".03"/>
        <stop offset="1" stop-color="#9ec6d2" stop-opacity=".15"/>
      </linearGradient>
      <linearGradient id="brillo" x2="100%" y2="100%">
        <stop stop-color="white" stop-opacity=".42"/>
        <stop offset="1" stop-color="white" stop-opacity="0"/>
      </linearGradient>
      <clipPath id="recorte"><path d="${lente}"/></clipPath>
      <g id="lado">
        <path d="${lente}" fill="url(#cristal)"/>
        <path d="M18 57 Q53 35 98 42 L97 46 Q56 41 19 63Z" fill="url(#brillo)" clip-path="url(#recorte)"/>
        <path d="${lente}" fill="none" stroke="#171719" stroke-opacity=".28" stroke-width="${grosor + 1.4}"/>
        <path d="${lente}" fill="none" stroke="url(#aro)" stroke-width="${grosor}" opacity="${claro ? .72 : 1}"/>
        ${ceja}
        <path d="M${bisagra} 44 Q17 42 9 40" fill="none" stroke="url(#aro)" stroke-width="${metal ? 2.6 : 4.8}" stroke-linecap="round"/>
        <ellipse cx="${bisagra}" cy="44" rx="2.5" ry="1.35" fill="#e8cfa6" stroke="#42372c" stroke-width=".6"/>
      </g>
    </defs>
    <path d="M98 49 Q120 37 142 49 ${puenteSuperior}" fill="none" stroke="url(#aro)" stroke-width="${metal ? 2.5 : 4.8}" stroke-linecap="round"/>
    <use href="#lado"/>
    <use href="#lado" transform="translate(240 0) scale(-1 1)"/>
  </svg>`;
  return `data:image/svg+xml,${encodeURIComponent(svg)}`;
}

const imagenesCatalogo = new Map(
  [...lentes].map(([codigo, lente]) => [codigo, ilustrar(codigo, lente)]),
);

function imagenProbador(modelo: TipoMarco): string {
  return imagenesCatalogo.get(modelo.codigo) ?? imagenMarco(modelo.id);
}
