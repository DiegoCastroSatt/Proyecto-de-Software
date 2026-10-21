import type { PuntoFacial } from './detector-rostro.service';

export interface AjusteMarco {
  x: number;
  y: number;
  ancho: number;
  angulo: number;
}

export function calcularAjuste(
  puntos: PuntoFacial[],
  ancho: number,
  alto: number,
): AjusteMarco | null {
  if (ancho <= 0 || alto <= 0) return null;
  const centro = (a: number, b: number): PuntoFacial | undefined => {
    if (!puntos[a] || !puntos[b]) return undefined;
    return { x: (puntos[a].x + puntos[b].x) / 2, y: (puntos[a].y + puntos[b].y) / 2 };
  };
  const ojos = [puntos[468] ?? centro(33, 133), puntos[473] ?? centro(362, 263)];
  if (!ojos[0] || !ojos[1]) return null;
  const [izquierdo, derecho] = [ojos[0], ojos[1]].sort((a, b) => a.x - b.x);
  if (
    ![izquierdo.x, izquierdo.y, derecho.x, derecho.y].every(
      (v) => Number.isFinite(v) && v >= 0 && v <= 1,
    )
  )
    return null;
  const dx = (derecho.x - izquierdo.x) * ancho;
  const dy = (derecho.y - izquierdo.y) * alto;
  const distancia = Math.hypot(dx, dy);
  if (distancia < ancho * 0.025) return null;
  // El visor mantiene 4:3 y contiene la imagen completa, incluidas las fotos verticales.
  const escala = Math.min(4 / ancho, 3 / alto);
  const visibleAncho = ancho * escala;
  const visibleAlto = alto * escala;
  return {
    x: (((4 - visibleAncho) / 2 + ((izquierdo.x + derecho.x) / 2) * visibleAncho) / 4) * 100,
    y: (((3 - visibleAlto) / 2 + ((izquierdo.y + derecho.y) / 2) * visibleAlto) / 3) * 100,
    // Las ilustraciones tienen 240 unidades de ancho y 116 entre centros de lentes.
    ancho: ((distancia * (240 / 116) * escala) / 4) * 100,
    angulo: (Math.atan2(dy, dx) * 180) / Math.PI,
  };
}
