import { Component, ElementRef, OnDestroy, ViewChild, inject, signal } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { DetectorRostroService } from './detector-rostro.service';
import { AjusteMarco, calcularAjuste } from './ajuste-marco';

@Component({
  selector: 'app-probador-marcos',
  templateUrl: './probador-marcos.html',
  styleUrl: './probador-marcos.css',
  providers: [DetectorRostroService],
})
export class ProbadorMarcosComponent implements OnDestroy {
  @ViewChild('video', { static: true }) video!: ElementRef<HTMLVideoElement>;
  private readonly detector = inject(DetectorRostroService);
  readonly modelos = [
    { id: 'rectangular', nombre: 'Rectangular' },
    { id: 'redondo', nombre: 'Redondo' },
    { id: 'ovalado', nombre: 'Ovalado' },
    { id: 'cuadrado', nombre: 'Cuadrado' },
    { id: 'cat-eye', nombre: 'Cat eye' },
    { id: 'aviador', nombre: 'Aviador' },
    { id: 'wayfarer', nombre: 'Wayfarer' },
    { id: 'geometrico', nombre: 'Geométrico' },
    { id: 'browline', nombre: 'Browline' },
    { id: 'semimontura', nombre: 'Semimontura' },
    { id: 'sin-montura', nombre: 'Sin montura' },
    { id: 'transparente', nombre: 'Transparente' },
    { id: 'rectangular-alto', nombre: 'Rectangular alto' },
    { id: 'aviador-angular', nombre: 'Aviador angular' },
    { id: 'cat-eye-suave', nombre: 'Cat eye suave' },
    { id: 'mariposa', nombre: 'Mariposa' },
    { id: 'oversize', nombre: 'Oversize' },
  ];
  readonly seleccionado = signal('rectangular');
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
    if (this.modelos.some((item) => item.id === modelo)) this.seleccionado.set(modelo!);
  }

  get nombreModelo(): string {
    return this.modelos.find((item) => item.id === this.seleccionado())!.nombre;
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
