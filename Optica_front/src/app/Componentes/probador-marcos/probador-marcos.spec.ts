import { of } from 'rxjs';
import { DisponibilidadMarcosService } from '../../shared/services/disponibilidad-marcos.service';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap } from '@angular/router';
import { vi, afterEach } from 'vitest';
import { ProbadorMarcosComponent } from './probador-marcos';
import { DetectorRostroService, PuntoFacial } from './detector-rostro.service';

describe('ProbadorMarcosComponent', () => {
  const detectar = vi.fn<(...args: unknown[]) => Promise<PuntoFacial[]>>().mockResolvedValue([]);

  function crear(modelo = 'redondo') {
    TestBed.configureTestingModule({
      imports: [ProbadorMarcosComponent],
      providers: [
        { provide: DisponibilidadMarcosService, useValue: { consultar: () => of(['rectangular', 'redondo', 'aviador'].map((codigo, i) => ({ id: i + 1, codigo, nombre: codigo === 'aviador' ? 'Aviador' : codigo, categoria: codigo, activo: true, rostros: ['ovalado'], stock: 1 }))) } },
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { queryParamMap: convertToParamMap({ modelo }) } },
        },
      ],
    });
    TestBed.overrideComponent(ProbadorMarcosComponent, {
      set: { providers: [{ provide: DetectorRostroService, useValue: { detectar } }] },
    });
    const fixture = TestBed.createComponent(ProbadorMarcosComponent);
    fixture.detectChanges();
    return fixture;
  }

  afterEach(() => {
    TestBed.resetTestingModule();
    vi.restoreAllMocks();
    vi.unstubAllGlobals();
    detectar.mockReset().mockResolvedValue([]);
  });

  it('conserva el modelo recomendado y permite cambiarlo sin alterar el ajuste', () => {
    const fixture = crear();
    expect(fixture.componentInstance.seleccionado()).toBe('redondo');
    fixture.componentInstance.tamano.set(115);
    const boton = [...fixture.nativeElement.querySelectorAll('.modelo')].find((elemento: Element) =>
      elemento.textContent?.includes('Aviador'),
    ) as HTMLButtonElement;
    boton.click();
    fixture.detectChanges();
    expect(fixture.componentInstance.seleccionado()).toBe('aviador');
    expect(fixture.componentInstance.tamano()).toBe(115);
    expect(fixture.nativeElement.querySelectorAll('input[type=range]').length).toBe(1);
  });

  it('usa un modelo válido si la URL solicita uno incompatible', () => {
    expect(crear('desconocido').componentInstance.seleccionado()).toBe('rectangular');
  });

  it('no solicita cámara al entrar y explica un permiso rechazado', async () => {
    const getUserMedia = vi
      .fn()
      .mockRejectedValue(new DOMException('Rechazado', 'NotAllowedError'));
    vi.stubGlobal('navigator', { mediaDevices: { getUserMedia } });
    const fixture = crear();
    expect(getUserMedia).not.toHaveBeenCalled();
    await fixture.componentInstance.activarCamara();
    expect(getUserMedia).toHaveBeenCalledWith({ video: { facingMode: 'user' }, audio: false });
    expect(fixture.componentInstance.error()).toContain('No se autorizó');
    expect(fixture.componentInstance.solicitando()).toBe(false);
  });

  it('apaga un permiso que llega después de abandonar el probador', async () => {
    let resolver!: (stream: MediaStream) => void;
    const stop = vi.fn();
    vi.stubGlobal('navigator', {
      mediaDevices: {
        getUserMedia: () => new Promise<MediaStream>((resolve) => (resolver = resolve)),
      },
    });
    const fixture = crear();
    const pendiente = fixture.componentInstance.activarCamara();
    fixture.destroy();
    resolver({ getTracks: () => [{ stop }] } as unknown as MediaStream);
    await pendiente;
    expect(stop).toHaveBeenCalledOnce();
  });

  it('apaga la cámara al borrar y al salir', async () => {
    const stop = vi.fn();
    const track = { stop, onended: null };
    const stream = { getTracks: () => [track], getVideoTracks: () => [track] };
    vi.stubGlobal('navigator', {
      mediaDevices: { getUserMedia: vi.fn().mockResolvedValue(stream) },
    });
    vi.stubGlobal('requestAnimationFrame', vi.fn().mockReturnValue(1));
    vi.stubGlobal('cancelAnimationFrame', vi.fn());
    const fixture = crear();
    vi.spyOn(fixture.componentInstance.video.nativeElement, 'play').mockResolvedValue();
    await fixture.componentInstance.activarCamara();
    expect(fixture.componentInstance.camara()).toBe(true);
    fixture.componentInstance.limpiar();
    expect(stop).toHaveBeenCalledOnce();
    expect(fixture.componentInstance.video.nativeElement.srcObject).toBeNull();
    await fixture.componentInstance.activarCamara();
    fixture.destroy();
    expect(stop).toHaveBeenCalledTimes(2);
  });

  it('rechaza archivos no compatibles antes de abrirlos', async () => {
    const fixture = crear();
    await fixture.componentInstance.cargarFoto({
      target: {
        files: [new File(['texto'], 'foto.svg', { type: 'image/svg+xml' })],
        value: 'foto.svg',
      },
    } as unknown as Event);
    expect(fixture.componentInstance.error()).toContain('JPG, PNG o WebP');
    expect(fixture.componentInstance.foto()).toBe('');
  });
  it('alinea una foto y libera su URL al quitarla', async () => {
    const puntos: PuntoFacial[] = Array.from({ length: 478 }, () => ({ x: 0.5, y: 0.5 }));
    puntos[468] = { x: 0.35, y: 0.4 };
    puntos[473] = { x: 0.65, y: 0.4 };
    detectar.mockResolvedValue(puntos);
    const createObjectURL = vi.fn().mockReturnValue('blob:prueba');
    const revokeObjectURL = vi.fn();
    vi.stubGlobal('URL', { createObjectURL, revokeObjectURL });
    vi.stubGlobal(
      'Image',
      class {
        src = '';
        naturalWidth = 800;
        naturalHeight = 600;
        decode() {
          return Promise.resolve();
        }
      },
    );
    const fixture = crear();
    await fixture.componentInstance.cargarFoto({
      target: { files: [new File(['foto'], 'foto.jpg', { type: 'image/jpeg' })], value: '' },
    } as unknown as Event);
    fixture.detectChanges();
    expect(fixture.componentInstance.ajuste()?.x).toBeCloseTo(50);
    expect(fixture.componentInstance.ajuste()?.y).toBeCloseTo(40);
    expect(fixture.nativeElement.querySelector('.marco')).toBeTruthy();
    fixture.componentInstance.limpiar();
    expect(revokeObjectURL).toHaveBeenCalledWith('blob:prueba');
    expect(fixture.componentInstance.ajuste()).toBeNull();
  });

  it('oculta el marco si no detecta ojos', async () => {
    vi.stubGlobal('URL', { createObjectURL: () => 'blob:sin-rostro', revokeObjectURL: vi.fn() });
    vi.stubGlobal(
      'Image',
      class {
        src = '';
        naturalWidth = 800;
        naturalHeight = 600;
        decode() {
          return Promise.resolve();
        }
      },
    );
    const fixture = crear();
    await fixture.componentInstance.cargarFoto({
      target: { files: [new File(['foto'], 'foto.jpg', { type: 'image/jpeg' })], value: '' },
    } as unknown as Event);
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('.marco')).toBeNull();
    expect(fixture.componentInstance.estado).toContain('No vemos tus ojos');
    expect(fixture.nativeElement.querySelector('input[type=range]').disabled).toBe(true);
  });

  it('descarta la detección de una foto que fue retirada', async () => {
    let resolver!: (puntos: PuntoFacial[]) => void;
    detectar.mockImplementation(() => new Promise((resolve) => (resolver = resolve)));
    vi.stubGlobal('URL', { createObjectURL: () => 'blob:retirada', revokeObjectURL: vi.fn() });
    vi.stubGlobal(
      'Image',
      class {
        src = '';
        naturalWidth = 800;
        naturalHeight = 600;
        decode() {
          return Promise.resolve();
        }
      },
    );
    const fixture = crear();
    const tarea = fixture.componentInstance.cargarFoto({
      target: { files: [new File(['foto'], 'foto.jpg', { type: 'image/jpeg' })], value: '' },
    } as unknown as Event);
    await Promise.resolve();
    fixture.componentInstance.limpiar();
    resolver(Array.from({ length: 478 }, (_, i) => ({ x: i === 468 ? 0.35 : 0.65, y: 0.4 })));
    await tarea;
    expect(fixture.componentInstance.ajuste()).toBeNull();
    expect(fixture.componentInstance.foto()).toBe('');
  });

  it('sigue los ojos en cámara y retira el marco cuando se pierde el rostro', async () => {
    let siguiente!: FrameRequestCallback;
    vi.stubGlobal(
      'requestAnimationFrame',
      vi.fn((callback: FrameRequestCallback) => {
        siguiente = callback;
        return 1;
      }),
    );
    vi.stubGlobal('cancelAnimationFrame', vi.fn());
    const track = { stop: vi.fn(), onended: null };
    vi.stubGlobal('navigator', {
      mediaDevices: {
        getUserMedia: vi
          .fn()
          .mockResolvedValue({ getTracks: () => [track], getVideoTracks: () => [track] }),
      },
    });
    const fixture = crear();
    const video = fixture.componentInstance.video.nativeElement;
    vi.spyOn(video, 'play').mockResolvedValue();
    Object.defineProperties(video, {
      readyState: { value: 2 },
      videoWidth: { value: 640 },
      videoHeight: { value: 480 },
      currentTime: { value: 1, configurable: true },
    });
    detectar.mockResolvedValueOnce(
      Array.from({ length: 478 }, (_, i) => ({ x: i === 468 ? 0.35 : 0.65, y: 0.4 })),
    );
    await fixture.componentInstance.activarCamara();
    siguiente(100);
    await Promise.resolve();
    fixture.detectChanges();
    expect(fixture.componentInstance.ajuste()?.x).toBeCloseTo(50);
    expect(fixture.nativeElement.querySelector('.medio').classList.contains('espejo')).toBe(true);
    Object.defineProperty(video, 'currentTime', { value: 2 });
    siguiente(200);
    await Promise.resolve();
    fixture.detectChanges();
    expect(fixture.componentInstance.ajuste()).toBeNull();
    expect(fixture.nativeElement.querySelector('.marco')).toBeNull();
  });

  it('permite liberar el componente durante el renderizado del servidor', () => {
    vi.stubGlobal('cancelAnimationFrame', undefined);
    expect(() => crear().destroy()).not.toThrow();
  });
});
