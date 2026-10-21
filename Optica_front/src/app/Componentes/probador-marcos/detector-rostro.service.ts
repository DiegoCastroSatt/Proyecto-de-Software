import { Injectable, OnDestroy } from '@angular/core';
import type { FaceLandmarker, NormalizedLandmark } from '@mediapipe/tasks-vision';

export type PuntoFacial = Pick<NormalizedLandmark, 'x' | 'y'>;

@Injectable()
export class DetectorRostroService implements OnDestroy {
  private detector?: FaceLandmarker;
  private inicializando?: Promise<FaceLandmarker>;
  private modo: 'IMAGE' | 'VIDEO' = 'IMAGE';
  private destruido = false;
  private cola: Promise<unknown> = Promise.resolve();

  private iniciar(): Promise<FaceLandmarker> {
    if (this.destruido) return Promise.reject(new Error('Detector cerrado'));
    if (!this.inicializando) {
      this.inicializando = (async () => {
        const { FaceLandmarker, FilesetResolver } = await import('@mediapipe/tasks-vision');
        const archivos = await FilesetResolver.forVisionTasks('/mediapipe/wasm');
        const detector = await FaceLandmarker.createFromOptions(archivos, {
          baseOptions: { modelAssetPath: '/models/face_landmarker.task', delegate: 'CPU' },
          runningMode: 'IMAGE',
          numFaces: 1,
          minFaceDetectionConfidence: 0.6,
          minFacePresenceConfidence: 0.6,
          minTrackingConfidence: 0.6,
        });
        if (this.destruido) {
          detector.close();
          throw new Error('Detector cerrado');
        }
        this.detector = detector;
        return detector;
      })().catch((error) => {
        this.inicializando = undefined;
        throw error;
      });
    }
    return this.inicializando;
  }

  detectar(imagen: HTMLImageElement | HTMLVideoElement, tiempo?: number): Promise<PuntoFacial[]> {
    // Serializar los cambios de modo evita mezclar una foto con un fotograma pendiente.
    const tarea = this.cola.then(async () => {
      const detector = await this.iniciar();
      const modo = tiempo === undefined ? 'IMAGE' : 'VIDEO';
      if (modo !== this.modo) {
        await detector.setOptions({ runningMode: modo });
        this.modo = modo;
      }
      if (this.destruido) throw new Error('Detector cerrado');
      const resultado =
        tiempo === undefined ? detector.detect(imagen) : detector.detectForVideo(imagen, tiempo);
      return resultado.faceLandmarks[0] ?? [];
    });
    this.cola = tarea.catch(() => undefined);
    return tarea;
  }

  ngOnDestroy(): void {
    this.destruido = true;
    // Esperar cualquier cambio de modo antes de liberar los recursos de WebAssembly.
    void this.cola.finally(() => {
      this.detector?.close();
      this.detector = undefined;
    });
  }
}
