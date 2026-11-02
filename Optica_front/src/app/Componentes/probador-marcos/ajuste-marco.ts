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
  if (!Number.isFinite(ancho) || !Number.isFinite(alto) || ancho <= 0 || alto <= 0)
    return null;
  const valido = (punto: PuntoFacial | undefined): punto is PuntoFacial =>
    !!punto && [punto.x, punto.y].every(v => Number.isFinite(v) && v >= 0 && v <= 1);
  const centro = (a: number, b: number): PuntoFacial | undefined => {
    if (!puntos[a] || !puntos[b]) return undefined;
    return { x: (puntos[a].x + puntos[b].x) / 2, y: (puntos[a].y + puntos[b].y) / 2 };
  };
  const ojos = [puntos[468] ?? centro(33, 133), puntos[473] ?? centro(362, 263)];
  if (!ojos[0] || !ojos[1]) return null;
  const [izquierdo, derecho] = [ojos[0], ojos[1]].sort((a, b) => a.x - b.x);
  if (!valido(izquierdo) || !valido(derecho)) return null;
  const dx = (derecho.x - izquierdo.x) * ancho;
  const dy = (derecho.y - izquierdo.y) * alto;
  const distancia = Math.hypot(dx, dy);
  if (distancia < ancho * 0.025) return null;
  // Los puntos 234 y 454 delimitan los laterales del rostro en la malla facial.
  const lateralIzquierdo = puntos[234];
  const lateralDerecho = puntos[454];
  const anchoRostro = valido(lateralIzquierdo) && valido(lateralDerecho)
    ? Math.hypot(
        (lateralDerecho.x - lateralIzquierdo.x) * ancho,
        (lateralDerecho.y - lateralIzquierdo.y) * alto,
      )
    : 0;
  // Si no se detecta un contorno válido, conservar la estimación basada en los ojos.
  const anchoMarco = anchoRostro > distancia ? anchoRostro : distancia * (240 / 116);
  // El visor mantiene 4:3 y contiene la imagen completa, incluidas las fotos verticales.
  const escala = Math.min(4 / ancho, 3 / alto);
  const visibleAncho = ancho * escala;
  const visibleAlto = alto * escala;
  return {
    x: (((4 - visibleAncho) / 2 + ((izquierdo.x + derecho.x) / 2) * visibleAncho) / 4) * 100,
    y: (((3 - visibleAlto) / 2 + ((izquierdo.y + derecho.y) / 2) * visibleAlto) / 3) * 100,
    ancho: ((anchoMarco * escala) / 4) * 100,
    angulo: (Math.atan2(dy, dx) * 180) / Math.PI,
  };
}
